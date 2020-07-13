using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using WebService;

namespace Assmnts
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IWebService" in both code and config file together.
    [ServiceContract]
    public interface IWebService
    {
        [OperationContract]
        WSConstants.RETURN_CODE GetDataFormResultIDs(string userID, string passwd, string formResultIDsXML, ref List<KeyValuePair<string, string>> errors, ref string data);

        [OperationContract]
        WSConstants.RETURN_CODE SaveDataGetKeys(string userID, string passwd, string data, ref List<KeyValuePair<string, string>> errors, ref List<KeyValuePair<int, string>> keys);

        [OperationContract]
        WSConstants.RETURN_CODE GetPdfReport(string userID, string passwd, int formResultID, ref Byte[] pdf);
        
        [OperationContract]
        WSConstants.RETURN_CODE GetFormResultIdsFromTrackingNumber(string userID, string passwd, string trackingNum, ref string data);

        [OperationContract]
        WSConstants.RETURN_CODE IsCompleteFormResultId(string userID, string passwd, int formResultID, ref bool complete);

        [OperationContract]
        WSConstants.RETURN_CODE GetStatusId(string userID, string passwd, int formResultID, ref WSConstants.FORM_STATUS status);

        [OperationContract]
        WSConstants.RETURN_CODE ChangeStatusId(string userID, string passwd, int formResultID, WSConstants.FORM_STATUS status);

        [OperationContract]
        WSConstants.RETURN_CODE GetTrackingNumberFormResultId(string userID, string passwd, int formResultID, ref string trackingNum);
        
        [OperationContract]
        WSConstants.RETURN_CODE DeleteAssessmentFormResultID(string userID, string passwd, int formResultID);
    
        [OperationContract]
        WSConstants.RETURN_CODE VerifyHost();
       
        [OperationContract]
        WSConstants.RETURN_CODE AuthenticateUser(string loginId, string password, ref int subId, ref int entId);

        [OperationContract]
        WSConstants.RETURN_CODE GetFormSchema(string userID, string passwd, string form, bool nested, ref string schema);

        [OperationContract]
        WSConstants.RETURN_CODE GetUpdates(string userID, string passwd, ref string data);

        [OperationContract]
        WSConstants.RETURN_CODE GetUserInfo(string userID, string passwd, string getUserID, ref string info, ref string error);

        [OperationContract]
        WSConstants.RETURN_CODE Create_User(string userID, string passwd, string userData, ref string error);

        [OperationContract]
        WSConstants.RETURN_CODE Update_user(string userID, string passwd, string userData, ref string error);

        [OperationContract]
        WSConstants.RETURN_CODE Delete_user(string userID, string passwd, string userIDToDelete, ref string error);

        [OperationContract]
        WSConstants.RETURN_CODE GetAllUsers(string userID, string passwd, ref string users, ref string error);


    }
}
