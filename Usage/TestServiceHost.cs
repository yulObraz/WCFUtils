using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WCFUtils.Usage {
    public class TestServiceHost : NewtonsoftJsonServiceHost {
        public TestServiceHost(Type serviceType, bool addMeta, params Uri[] baseAddresses)
            : base(serviceType, addMeta, baseAddresses) {

        }
        protected override void ConfigureEndPoint(System.ServiceModel.Description.ServiceEndpoint point) {
            var serializer = point.NewtonsoftSettings().JsonSerializer;
            serializer.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
            serializer.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
            serializer.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            base.ConfigureEndPoint(point);
        }
    }

}
