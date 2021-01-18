using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace LitikoRestService
{
    public class Response
    {
        public string ErrorMessage;
        public string ResponseResult;
    }


    [ServiceContract]
    public interface ILitikoRestService
    {

        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json,
         UriTemplate = "GetOperatorByPhone", ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped)]
        Response GetOperatorByPhone(string PhoneNumber);

        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json,
         UriTemplate = "GetPhoneByCertThumbprint", ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped)]
        Response GetPhoneByCertThumbprint(string CertificateThumbprint);

        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json,
         UriTemplate = "GetAllPositions", ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped)]
        Response GetAllPositions(string PhoneNumber);

        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json,
         UriTemplate = "SignVodafone", ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped)]
        Response SignVodafone(string HashData, string PhoneNumber, int PositionId, string Service, string DisplayMessage = "Підписання даних в Директум");

        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json,
         UriTemplate = "SignData", ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped)]
        Response SignData(string HashData, string PhoneNumber, int PositionId = 0, string Service = "", string DisplayMessage = "Підписання даних в Директум");

        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json,
         UriTemplate = "SignKyivstar", ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped)]
        Response SignKyivstar(string HashData, string PhoneNumber);


        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json,
         UriTemplate = "test", ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped)]
        string Test1();

        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json,
         UriTemplate = "secret", ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped)]
        string Secret();
    }
}