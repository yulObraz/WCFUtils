﻿using System.ServiceModel;
using System.ServiceModel.Web;

namespace WCFUtils.Usage
{
    [ServiceContract]
    public interface ITestService
    {
        [WebGet, OperationContract]
        Person GetPerson();
        [WebInvoke, OperationContract]
        Pet EchoPet(Pet pet);
        [WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest), OperationContract]
        int Add(int x, int y);
    }
}
