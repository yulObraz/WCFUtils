using System.ServiceModel;
using System.ServiceModel.Web;

namespace WCFUtils.Usage
{
    [ServiceContract]
    [ServiceKnownType(typeof(AuthorizationFault))]
    public interface ITestService
    {
        [WebGet, OperationContract, FaultContract(typeof(CommonFault))]
        Person GetPerson();
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare ), OperationContract, FaultContract(typeof(CommonFault))]
        Pet EchoPet(Pet pet);
        [WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        [OperationContract, FaultContract(typeof(CommonFault))]
        int Add(int x, int y);
        [WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest, Method = "GET", UriTemplate = "Add?x={x}&y={y}")]
        [OperationContract, FaultContract(typeof(CommonFault))]
        int AddGet(int x, int y);
        [WebInvoke()]
        [OperationContract, FaultContract(typeof(CommonFault))]
        int Throw(int type);
        [WebInvoke(Method = "GET", UriTemplate="Throw?type={type}")]
        [OperationContract, FaultContract(typeof(CommonFault))]
        int ThrowGet(int type);
    }
}
