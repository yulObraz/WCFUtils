using System;
using System.IO;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;

namespace WCFUtils
{
    class NewtonsoftJsonClientFormatter : IClientMessageFormatter
    {
        private static TraceSource traceSource = new TraceSource("WCFUtils");

        OperationDescription operation;
        Uri operationUri;
        ServiceEndpoint endpoint;
        public NewtonsoftJsonClientFormatter(OperationDescription operation, ServiceEndpoint endpoint)
        {
            this.operation = operation;
            string endpointAddress = endpoint.Address.Uri.ToString();
            if (!endpointAddress.EndsWith("/"))
            {
                endpointAddress = endpointAddress + "/";
            }
            this.operationUri = new Uri(endpointAddress + operation.Name);
            this.endpoint = endpoint;
        }
        
        public object DeserializeReply(Message message, object[] parameters)
        {
            object bodyFormatProperty;
            if (!message.Properties.TryGetValue(WebBodyFormatMessageProperty.Name, out bodyFormatProperty) ||
                (bodyFormatProperty as WebBodyFormatMessageProperty).Format != WebContentFormat.Raw)
            {
                throw new InvalidOperationException("Incoming messages must have a body format of Raw. Is a ContentTypeMapper set on the WebHttpBinding?");
            }

            XmlDictionaryReader bodyReader = message.GetReaderAtBodyContents();
            Newtonsoft.Json.JsonSerializer serializer = endpoint.NewtonsoftSettings().JsonSerializer;
            bodyReader.ReadStartElement("Binary");
            byte[] body = bodyReader.ReadContentAsBase64();
            try {
                using(MemoryStream ms = new MemoryStream(body)) {
                    using(StreamReader sr = new StreamReader(ms)) {
                        Type returnType = this.operation.Messages[1].Body.ReturnValue.Type;
                        var result = serializer.Deserialize(sr, returnType);

                        if(traceSource.Switch.ShouldTrace(TraceEventType.Information)) {
                            traceSource.TraceEvent(TraceEventType.Information, 1004, System.Text.Encoding.UTF8.GetString(body));
                        }

                        return result;
                    }
                }
            } catch {
                traceSource.TraceEvent(TraceEventType.Error, 1005, System.Text.Encoding.UTF8.GetString(body));
                throw;
            }
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            byte[] body;
            Newtonsoft.Json.JsonSerializer serializer = endpoint.NewtonsoftSettings().JsonSerializer;
            
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
                {
                    using (Newtonsoft.Json.JsonWriter writer = new Newtonsoft.Json.JsonTextWriter(sw))
                    {
                        if (parameters.Length == 1)
                        {
                            // Single parameter, assuming bare
                            serializer.Serialize(sw, parameters[0]);
                        }
                        else
                        {
                            writer.WriteStartObject();
                            for (int i = 0; i < this.operation.Messages[0].Body.Parts.Count; i++)
                            {
                                writer.WritePropertyName(this.operation.Messages[0].Body.Parts[i].Name);
                                serializer.Serialize(writer, parameters[i]);
                            }

                            writer.WriteEndObject();
                        }

                        writer.Flush();
                        sw.Flush();
                        body = ms.ToArray();
                    }
                }
            }
            if(traceSource.Switch.ShouldTrace(TraceEventType.Information)) {
                traceSource.TraceEvent(TraceEventType.Information, 1004, System.Text.Encoding.UTF8.GetString(body));
            }

            Message requestMessage = Message.CreateMessage(messageVersion, operation.Messages[0].Action, new RawBodyWriter(body));
            requestMessage.Headers.To = operationUri;
            requestMessage.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Raw));
            HttpRequestMessageProperty reqProp = new HttpRequestMessageProperty();
            reqProp.Headers[HttpRequestHeader.ContentType] = "application/json";
            requestMessage.Properties.Add(HttpRequestMessageProperty.Name, reqProp);
            return requestMessage;
        }
    }
}
