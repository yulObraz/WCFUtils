using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Net;

namespace WCFUtils {
    class NewtonsoftJsonBasicClientMessageInspector : IClientMessageInspector {
        private ServiceEndpoint endpoint;
        public NewtonsoftJsonBasicClientMessageInspector(ServiceEndpoint endpoint) {
            this.endpoint = endpoint;

        }
        public object BeforeSendRequest(ref Message request, IClientChannel channel) {
            var credentialBehaviour = endpoint.Behaviors.Find<ClientCredentials>();
            if(credentialBehaviour != null) {
                var userName = credentialBehaviour.UserName;

                HttpRequestMessageProperty property = null;
                if(request.Properties.ContainsKey(HttpRequestMessageProperty.Name)) {
                    property = (request.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty);
                }
                bool exists = property != null;
                if(!exists) {
                    property = new HttpRequestMessageProperty();
                }
                if(property.Headers[HttpRequestHeader.Authorization] == null) {
                    property.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(userName.UserName + ":" + userName.Password));
                }
                if(!exists) {
                    request.Properties[HttpRequestMessageProperty.Name] = property;
                }
            }
            return null;
        }

        /// <summary>
        /// Enables inspection or modification of a message after a reply message is received but prior to passing it back to the client application.
        /// </summary>
        /// <param name="reply">The message to be transformed into types and handed back to the client application.</param>
        /// <param name="correlationState">Correlation state data.</param>
        public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState) {
            // Nothing special here
        }
    }
}
