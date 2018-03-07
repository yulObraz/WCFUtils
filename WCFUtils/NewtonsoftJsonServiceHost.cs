using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.ServiceModel.Activation;

namespace WCFUtils {
    public class NewtonsoftJsonServiceFactory : ServiceHostFactory {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected override System.ServiceModel.ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses) {
            try {
                return new NewtonsoftJsonServiceHost(serviceType, false, baseAddresses);
            } catch(Exception ex) {
                _log.ErrorFormat("{0}: {1}", MethodBase.GetCurrentMethod().Name, ex);
                throw;
            }
        }
    }
    public class NewtonsoftJsonServiceHost : ServiceHost {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private bool addMeta;
        public NewtonsoftJsonServiceHost(Type serviceType, bool addMeta, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses) {
            this.addMeta = addMeta;

        }
        protected override void ApplyConfiguration() {
            base.ApplyConfiguration();

            AddMetaBehavior(this.BaseAddresses);

            foreach(Uri baseAddress in this.BaseAddresses) {
                _log.InfoFormat("{0}", baseAddress);
                WebHttpBinding webBinding = CreateBinding(baseAddress);
                webBinding.ContentTypeMapper = WCFUtils.RawContentTypeMapper.Instance;
                _log.InfoFormat("webBinding = {0} array={1}", webBinding.Name, webBinding.ReaderQuotas.MaxArrayLength);
                _log.InfoFormat("{0}", this.Description.Endpoints.Count);
                var address = baseAddress.ToString().TrimEnd('/');
                var point = this.AddServiceEndpoint(this.ImplementedContracts.First().Key, webBinding, address + "/json", new Uri("json", UriKind.Relative));

                var newtBeh = new NewtonsoftJsonBehavior();
                // test this
                newtBeh.DefaultOutgoingResponseFormat = System.ServiceModel.Web.WebMessageFormat.Json;
                newtBeh.DefaultOutgoingRequestFormat = System.ServiceModel.Web.WebMessageFormat.Json;
                newtBeh.AutomaticFormatSelectionEnabled = false;
                newtBeh.FaultExceptionEnabled = true;
                newtBeh.HelpEnabled = false;
                // test this
                //name="" helpEnabled="true" faultExceptionEnabled="true" automaticFormatSelectionEnabled="false" defaultOutgoingResponseFormat="Json"
                point.Behaviors.Add(newtBeh);
                ConfigureEndPoint(point);
                _log.InfoFormat("webBinding = {0} array={1}", webBinding.Name, webBinding.ReaderQuotas.MaxArrayLength);
                _log.InfoFormat("{0}", this.Description.Endpoints.Count);
            }
        }

        protected static WebHttpBinding CreateBinding(Uri baseAddress) {
            var binding = new WebHttpBinding(baseAddress.Scheme == Uri.UriSchemeHttp ? WebHttpSecurityMode.None : WebHttpSecurityMode.Transport);
            binding.ReaderQuotas.MaxArrayLength = binding.ReaderQuotas.MaxArrayLength * 1024;
            return binding;
        }
        private void AddMetaBehavior(System.Collections.ObjectModel.ReadOnlyCollection<Uri> uris) {
            if(addMeta) {
                ServiceMetadataBehavior mexBehavior = this.Description.Behaviors.Find<ServiceMetadataBehavior>();
                if(mexBehavior == null) {
                    mexBehavior = new ServiceMetadataBehavior();
                    mexBehavior.HttpsGetEnabled = uris.Any(it => it.Scheme == Uri.UriSchemeHttps);
                    this.Description.Behaviors.Add(mexBehavior);
                }
            }
        }

        protected virtual void ConfigureEndPoint(System.ServiceModel.Description.ServiceEndpoint point) {
            //this.Authentication.ServiceAuthenticationManager = new UserBasicAuthorizationManager();
            //if(point.ListenUri.Scheme == Uri.UriSchemeHttp) {
            //    //webBinding.Security.Mode = WebHttpSecurityMode.None;
            //} else if(point.ListenUri.Scheme == Uri.UriSchemeHttps) {
            //    //webBinding.Security.Mode = WebHttpSecurityMode.Transport;
            //}
        }

        public static ChannelFactory<T> GetClientFactory<T>(Uri url, string username, string password, Action<ServiceEndpoint> configure = null) where T : class {
            WebHttpBinding webBinding = CreateBinding(url);
            //_log.InfoFormat("{0}: name={1} quotas={2}", MethodBase.GetCurrentMethod().Name, webBinding.Name, webBinding.ReaderQuotas.MaxArrayLength);
            webBinding.ContentTypeMapper = WCFUtils.RawContentTypeMapper.Instance;
            webBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            webBinding.Security.Transport.Realm = "WCFUtils";

            int minSize = 128 * 1024 * 1024;
            if(webBinding.MaxReceivedMessageSize < minSize) {
                webBinding.MaxReceivedMessageSize = minSize;
            }
            var endpoint = new EndpointAddress(url.ToString().TrimEnd('/') + "/json");
            var restServiceEndpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(T)), webBinding, endpoint);
            if(configure != null) {
                configure(restServiceEndpoint);
            }
            var newtBeh = new NewtonsoftJsonBehavior();
            //_log.InfoFormat("{0}: defFormat={1} auto={2}", MethodBase.GetCurrentMethod().Name, newtBeh.DefaultOutgoingRequestFormat, newtBeh.AutomaticFormatSelectionEnabled);
            newtBeh.DefaultOutgoingResponseFormat = System.ServiceModel.Web.WebMessageFormat.Json;
            newtBeh.DefaultOutgoingRequestFormat = System.ServiceModel.Web.WebMessageFormat.Json;
            newtBeh.AutomaticFormatSelectionEnabled = false;
            //newtBeh.FaultExceptionEnabled = true;
            //newtBeh.HelpEnabled = false;
            
            restServiceEndpoint.Behaviors.Add(newtBeh);
            var factory = new ChannelFactory<T>(restServiceEndpoint);
            factory.Credentials.UserName.UserName = username;
            factory.Credentials.UserName.Password = password;
            return factory;
        }
    }
}
