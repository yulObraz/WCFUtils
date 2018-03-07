using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.IO;
using System.Net;
using log4net;
using System.Reflection;
using System.Web;
using System.ServiceModel.Description;
using System.Collections.ObjectModel;

namespace WCFUtils {
    public class LogErrorHandlerAttribute : Attribute, IServiceBehavior, IErrorHandler {
        static ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public virtual bool HandleError(Exception e) {
            try {
                string methodName = GetMethodName();
                _log.InfoFormat("{0}: service exception {1}: {2}", MethodBase.GetCurrentMethod().Name, methodName, e);
            } catch { }
            return false;
        }
        private static string GetMethodName() {
            string methodName = null;
            try {
                var operationContext = OperationContext.Current;
                string bindingName = operationContext.EndpointDispatcher.ChannelDispatcher.BindingName;

                if(bindingName.Contains("WebHttpBinding")) {                    //REST request
                    methodName = (string)operationContext.IncomingMessageProperties["HttpOperationName"];
                } else {                    //SOAP request
                    string action = operationContext.IncomingMessageHeaders.Action;
                    methodName = operationContext.EndpointDispatcher.DispatchRuntime.Operations.FirstOrDefault(o => o.Action == action).Name;
                }
                // Insert your own error-handling here if (operation == null)
                methodName = operationContext.Host.Description.ServiceType.Name + ": " + methodName;
            } catch {
            }
            return methodName;
        }
        public virtual void ProvideFault(Exception error, MessageVersion ver, ref System.ServiceModel.Channels.Message msg) {
        }
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) {
        }
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) {
            foreach(ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers) {
                channelDispatcher.ErrorHandlers.Insert(0, this);
            }
        }
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) {
        }
    }

    public class NewtonsoftJsonErrorHandler : IErrorHandler {
        public static string ClientHeader = "ClientUsedHeader";
        ServiceEndpoint endpoint;
        public NewtonsoftJsonErrorHandler(ServiceEndpoint endpoint) {
            this.endpoint = endpoint;
        }
        static ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public bool HandleError(Exception error) {
            try {
                _log.InfoFormat("{0} {1}: {2}", MethodBase.GetCurrentMethod().Name, error.Data["method"], error);
            } catch(Exception ex) {
                _log.InfoFormat("{0}: {1} {2}", MethodBase.GetCurrentMethod().Name, ex, error);
            }
            return true;//should not abort the session
        }
        public virtual CommonFault CreateCommonFault(object detail) {
            return null;
        }
        public virtual CommonFault GetCommonFault(Exception error){
            if(error is FaultException) {
                if(error.GetType().IsAssignableToGenericType(typeof(FaultException<>))) {
                    var detail = error.GetType().GetProperty("Detail").GetGetMethod().Invoke(error, null);
                    if(detail is CommonFault) {
                        return detail as CommonFault;
                    }
                    var common = CreateCommonFault(detail);
                    if(common != null) {
                        return common;
                    }
                } else if(/*(error as FaultException).Code.IsSenderFault &&*/ (error as FaultException).Code.SubCode.Name == "InvalidSecurity") {
                    return new AuthorizationFault(error.Message);
                }
                return new CommonFault(error.Message, HttpStatusCode.BadRequest);
            } else {
                return new CommonFault("Please contact administrator or try later.", HttpStatusCode.InternalServerError);
            }
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault) {
            try {
                error.Data.Add("method", (string)OperationContext.Current.IncomingMessageProperties["HttpOperationName"]);
            } catch {
            }
            bool inApi = false;
            try {
                var request = OperationContext.Current.RequestContext.RequestMessage.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
                inApi = request.Headers[ClientHeader] == "Used";
            } catch {
            }

            CommonFault detail = GetCommonFault(error);
            // create a fault message containing our FaultContract object
            fault = SerializeReply(version, detail);

            var rmp = (fault.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty) ?? new HttpResponseMessageProperty();
            if(inApi) {
                rmp.Headers[ClientHeader] = detail.Status.ToString();
            } else {
                rmp.StatusCode = detail.Status == HttpStatusCode.Unauthorized ? AuthorizationFault.AuthValue : detail.Status;
                rmp.StatusDescription = HttpWorkerRequest.GetStatusDescription((int)rmp.StatusCode) + " " + detail.Error;// put appropraite description here..
            }
            fault.Properties[HttpResponseMessageProperty.Name] = rmp;
        }
        public Message SerializeReply(MessageVersion messageVersion, object result) {
            byte[] body;
            Newtonsoft.Json.JsonSerializer serializer = endpoint.NewtonsoftSettings().JsonSerializer;
            using(MemoryStream ms = new MemoryStream()) {
                using(StreamWriter sw = new StreamWriter(ms, Encoding.UTF8)) {
                    using(Newtonsoft.Json.JsonWriter writer = new Newtonsoft.Json.JsonTextWriter(sw)) {
                        serializer.Serialize(writer, result);
                        sw.Flush();
                        body = ms.ToArray();
                    }
                }
            }
            //fault = Message.CreateMessage(version, "", detail, new System.Runtime.Serialization.Json.DataContractJsonSerializer(detail.GetType()));
            Message replyMessage = Message.CreateMessage(messageVersion, "", new RawBodyWriter(body));
            replyMessage.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Raw));
            HttpResponseMessageProperty respProp = new HttpResponseMessageProperty();
            respProp.Headers[HttpResponseHeader.ContentType] = "application/json";
            replyMessage.Properties.Add(HttpResponseMessageProperty.Name, respProp);
            return replyMessage;
        }
      
    }
    internal static class ReflectionExtension{
      public static bool IsAssignableToGenericType(this Type givenType, Type genericType) {
            if(givenType == null || genericType == null) {
                return false;
            }

            return givenType == genericType
              || givenType.MapsToGenericTypeDefinition(genericType)
              || givenType.HasInterfaceThatMapsToGenericTypeDefinition(genericType)
              || givenType.BaseType.IsAssignableToGenericType(genericType);
        }

        private static bool HasInterfaceThatMapsToGenericTypeDefinition(this Type givenType, Type genericType) {
            return givenType
              .GetInterfaces()
              .Where(it => it.IsGenericType)
              .Any(it => it.GetGenericTypeDefinition() == genericType);
        }

        private static bool MapsToGenericTypeDefinition(this Type givenType, Type genericType) {
            return genericType.IsGenericTypeDefinition
              && givenType.IsGenericType
              && givenType.GetGenericTypeDefinition() == genericType;
        }
    }
}
