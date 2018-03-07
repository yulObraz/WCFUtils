using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;
using System.Linq;
using System.Diagnostics;

namespace WCFUtils
{
    class NewtonsoftJsonDispatchFormatter : IDispatchMessageFormatter
    {
        private static ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static TraceSource traceSource = new TraceSource("WCFUtils");
        OperationDescription operation;
        ServiceEndpoint endpoint;
        Dictionary<string, int> parameterNames;
        public NewtonsoftJsonDispatchFormatter(OperationDescription operation, ServiceEndpoint endpoint, bool isRequest)
        {
            this.operation = operation;
            this.endpoint = endpoint;
            if (isRequest)
            {
                int operationParameterCount = operation.Messages[0].Body.Parts.Count;
                if (operationParameterCount > 1)
                {
                    this.parameterNames = new Dictionary<string, int>();
                    for (int i = 0; i < operationParameterCount; i++)
                    {
                        this.parameterNames.Add(operation.Messages[0].Body.Parts[i].Name, i);
                    }
                }
            }
        }
        private static ILog _applog = LogManager.GetLogger("WCFUtils.Calls");

        public void DeserializeRequest(Message message, object[] parameters)
        {
            try {
                string ip = null;
                string via = null;
                string userAgent = null;
                object xxx;
                if(message.Properties.TryGetValue(RemoteEndpointMessageProperty.Name, out xxx)) {
                    var endpoint = xxx as RemoteEndpointMessageProperty;
                    if(endpoint != null) {
                        ip = endpoint.Address;
                    }
                }
                if(message.Properties.Via != null) {
                    via = message.Properties.Via.ToString();
                }
                if(message.Properties.TryGetValue(HttpRequestMessageProperty.Name, out xxx)) {
                    var request = xxx as HttpRequestMessageProperty;
                    if(request != null) {
                        userAgent = request.Headers[HttpRequestHeader.UserAgent];
                    }
                }
                _applog.InfoFormat("{0}: {1} {2} {3}", ip, via, operation.Name, userAgent);
                if(traceSource.Switch.ShouldTrace(TraceEventType.Information)) {
                    traceSource.TraceEvent(TraceEventType.Information, 1006, string.Format("{0}: {1} {2} {3}", ip, via, operation.Name, userAgent));
                }
                //message.Properties.Keys.ToList().ForEach(it => _log.InfoFormat("{0}={1}", it, message.Properties[it]));
                //message.Headers.ToList().ForEach(it => _log.InfoFormat("{0}={1} {2} {3}", it.Name, it.Namespace,it.Actor, message.Headers.GetHeader<string>(it.Name, it.Namespace)));
            } catch(Exception ex) {
                _log.InfoFormat("{0}: error {1}", MethodBase.GetCurrentMethod().Name, ex);
                //traceSource.TraceEvent(TraceEventType.Error, 1000, ex);
                if(traceSource.Switch.ShouldTrace(TraceEventType.Warning)) {
                    traceSource.TraceEvent(TraceEventType.Warning, 1007, ex.Message);
                }

            }
            object bodyFormatProperty;
            if (!message.Properties.TryGetValue(WebBodyFormatMessageProperty.Name, out bodyFormatProperty) ||
                (bodyFormatProperty as WebBodyFormatMessageProperty).Format != WebContentFormat.Raw)
            {
                throw new InvalidOperationException("Incoming messages must have a body format of Raw. Is a ContentTypeMapper set on the WebHttpBinding?");
            }

            XmlDictionaryReader bodyReader = message.GetReaderAtBodyContents();
            bodyReader.ReadStartElement("Binary");
            byte[] rawBody = bodyReader.ReadContentAsBase64();

            MemoryStream ms = new MemoryStream(rawBody);
            StreamReader sr = new StreamReader(ms);
            try {
                Newtonsoft.Json.JsonSerializer serializer = endpoint.NewtonsoftSettings().JsonSerializer;
                if(parameters.Length == 1) {
                    // single parameter, assuming bare
                    parameters[0] = serializer.Deserialize(sr, operation.Messages[0].Body.Parts[0].Type);
                } else {
                    // multiple parameter, needs to be wrapped
                    Newtonsoft.Json.JsonReader reader = new Newtonsoft.Json.JsonTextReader(sr);
                    reader.Read();
                    if(reader.TokenType != Newtonsoft.Json.JsonToken.StartObject) {
                        throw new InvalidOperationException("Input needs to be wrapped in an object");
                    }

                    reader.Read();
                    while(reader.TokenType == Newtonsoft.Json.JsonToken.PropertyName) {
                        string parameterName = reader.Value as string;
                        reader.Read();
                        if(this.parameterNames.ContainsKey(parameterName)) {
                            int parameterIndex = this.parameterNames[parameterName];
                            parameters[parameterIndex] = serializer.Deserialize(reader, this.operation.Messages[0].Body.Parts[parameterIndex].Type);
                        } else {
                            reader.Skip();
                        }

                        reader.Read();
                    }

                    reader.Close();
                }
                if(traceSource.Switch.ShouldTrace(TraceEventType.Information)) {
                    traceSource.TraceEvent(TraceEventType.Information, 1002, System.Text.Encoding.UTF8.GetString(rawBody));
                }

                sr.Close();
                ms.Close();
            } catch {
                traceSource.TraceEvent(TraceEventType.Error, 1001, System.Text.Encoding.UTF8.GetString(rawBody));
                throw;
            }
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            byte[] body;
            Newtonsoft.Json.JsonSerializer serializer = endpoint.NewtonsoftSettings().JsonSerializer;

            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
                {
                    using (Newtonsoft.Json.JsonWriter writer = new Newtonsoft.Json.JsonTextWriter(sw))
                    {
                        serializer.Serialize(writer, result);
                        sw.Flush();
                        body = ms.ToArray();
                    }
                }
            }
            if(traceSource.Switch.ShouldTrace(TraceEventType.Information)) {
                traceSource.TraceEvent(TraceEventType.Information, 1000, System.Text.Encoding.UTF8.GetString(body));
            }

            Message replyMessage = Message.CreateMessage(messageVersion, operation.Messages[1].Action, new RawBodyWriter(body));
            replyMessage.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Raw));
            HttpResponseMessageProperty respProp = new HttpResponseMessageProperty();
            respProp.Headers[HttpResponseHeader.ContentType] = "application/json";
            replyMessage.Properties.Add(HttpResponseMessageProperty.Name, respProp);
            return replyMessage;
        }
    }
}
