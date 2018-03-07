using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Net;
using System.ServiceModel;

namespace WCFUtils {
    [DataContract]
    public class CommonFault {
        public CommonFault() { }
        [DataMember]
        public HttpStatusCode Status { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public string Error { get; set; }

        public CommonFault(string error) {
            this.Type = "common";
            this.Error = error;
            this.Status = HttpStatusCode.BadRequest;
        }
        public CommonFault(string error, HttpStatusCode status) {
            this.Type = "common";
            this.Error = error;
            this.Status = status;
        }

        public static FaultException Exception<T>(T detail) where T : CommonFault {
            return new CommonFaultException<T>(detail);
        }
    }
    [DataContract]
    public class AuthorizationFault : CommonFault {
        public static HttpStatusCode AuthValue = HttpStatusCode.Unauthorized;
        public AuthorizationFault(string error)
            : base(error) {
            Type = "authorization";
            Status = HttpStatusCode.Unauthorized;
        }
    }
    public class CommonFaultException<TDetail> : FaultException<TDetail> where TDetail: CommonFault {
        public CommonFaultException(TDetail detail)
            : base(detail, (detail as CommonFault).Error) {
        }
    }
}
