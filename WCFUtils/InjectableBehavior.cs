using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace WCFUtils {
    public class InjectableBehaviorAttribute : Attribute, IServiceBehavior {
        public static Func<Type, IInstanceProvider> InstanceFabric { get; set; }
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase,
            Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) {
            var serviceBehavior = serviceDescription.Behaviors.Find<ServiceBehaviorAttribute>();
            serviceBehavior.InstanceContextMode = InstanceContextMode.PerCall;// currently avalilable
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) {
            Type serviceType = serviceDescription.ServiceType;
            IInstanceProvider instanceProvider = InstanceFabric(serviceType);

            foreach(ChannelDispatcher dispatcher in serviceHostBase.ChannelDispatchers) {
                foreach(EndpointDispatcher endpointDispatcher in dispatcher.Endpoints) {
                    DispatchRuntime dispatchRuntime = endpointDispatcher.DispatchRuntime;
                    dispatchRuntime.InstanceProvider = instanceProvider;
                    //dispatchRuntime.InstanceContextInitializers.Add(new UnityInstanceContextInitializer()) 
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) {
        }
    }
    public class InjectableBehaviorExtensionElement : BehaviorExtensionElement {
        public override Type BehaviorType {
            get { return typeof(InjectableBehaviorAttribute); }
        }

        protected override object CreateBehavior() {
            return new InjectableBehaviorAttribute();
        }
    }
}
