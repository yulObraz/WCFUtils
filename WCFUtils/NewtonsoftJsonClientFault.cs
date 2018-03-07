using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Net;
using System.Xml;
using System.IO;

namespace WCFUtils {
    class NewtonsoftJsonClientFaultMessageInspector : IClientMessageInspector {
        private ServiceEndpoint endpoint;
        public NewtonsoftJsonClientFaultMessageInspector(ServiceEndpoint endpoint) {
            this.endpoint = endpoint;

        }
        public object BeforeSendRequest(ref Message request, IClientChannel channel) {
            HttpRequestMessageProperty property = null;
            if(request.Properties.ContainsKey(HttpRequestMessageProperty.Name)) {
                property = (request.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty);
            }
            bool exists = property != null;
            if(!exists) {
                property = new HttpRequestMessageProperty();
            }
            property.Headers[NewtonsoftJsonErrorHandler.ClientHeader] = "Used";
            if(!exists) {
                request.Properties[HttpRequestMessageProperty.Name] = property;
            }
            return null;
        }

        /// <summary>
        /// Enables inspection or modification of a message after a reply message is received but prior to passing it back to the client application.
        /// </summary>
        /// <param name="reply">The message to be transformed into types and handed back to the client application.</param>
        /// <param name="correlationState">Correlation state data.</param>
        public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState) {
            HttpResponseMessageProperty property = null;
            if(reply.Properties.ContainsKey(HttpResponseMessageProperty.Name)) {
                property = (reply.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty);
            }
            if(property != null) {
                var errorStatus = property.Headers[NewtonsoftJsonErrorHandler.ClientHeader];
                if(errorStatus != null) {
                    XmlDictionaryReader bodyReader = reply.GetReaderAtBodyContents();
                    Newtonsoft.Json.JsonSerializer serializer = endpoint.NewtonsoftSettings().JsonSerializer;
                    bodyReader.ReadStartElement("Binary");
                    byte[] body = bodyReader.ReadContentAsBase64();
                    using(MemoryStream ms = new MemoryStream(body)) {
                        using(StreamReader sr = new StreamReader(ms)) {
                            var result = (CommonFault)serializer.Deserialize(sr, typeof(CommonFault));
                            throw new FaultException<CommonFault>(result, result.Error);
                        }
                    }
                }
            }
        }
    }
}
