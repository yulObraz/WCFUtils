using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.ServiceModel.Description;

namespace WCFUtils {
    public class NewtonsoftSettings : IEndpointBehavior {
        private JsonSerializer _jsonSerializer;
        public JsonSerializer JsonSerializer {
            get {
                if(_jsonSerializer == null) {
                    _jsonSerializer = DefaultJsonSerializer();
                }
                return _jsonSerializer;
            }
            set { _jsonSerializer = value; }
        }
        public static JsonSerializer DefaultJsonSerializer() { 
            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializer.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            return serializer;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters) {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime) {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher) {
        }

        public void Validate(ServiceEndpoint endpoint) {
        }
    }
    public static class NewtonsoftSettingsExtension {
        public static NewtonsoftSettings NewtonsoftSettings(this ServiceEndpoint endpoint) {
            var settings = endpoint.Behaviors.Find<NewtonsoftSettings>();
            if(settings == null) {
                settings = new NewtonsoftSettings();
                endpoint.Behaviors.Add(settings);
            }
            return settings;
        }
    }
}
