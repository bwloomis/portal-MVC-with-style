using Assmnts.Infrastructure;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UAS.Business;

namespace Assmnts.Business
{
    public class WebServiceActivity
    {
        
        /// <summary>
        /// These correspond to the functions in the def_WebServiceActivityFunctions table.
        /// </summary>
        public enum webServiceActivityFunctions
        {
            UPLOAD = 1,
            COMPLETE = 2,
            REVIEW = 3,
            APPROVE = 4,
            DELETE = 5,
        };

        /// <summary>
        /// Inserts a web service activity record. The enterprise ID is for the logged in user. The group ID is for their first authorized group.
        /// IssuedDateTime is set to the current time.
        /// </summary>
        /// <param name="formsRepo"></param>
        /// <param name="activityId">Corresponding to the webServiceAcivityFunctions enum values</param>
        /// <param name="activityParamaters">ex: formresult=1000125</param>
        /// <param name="requestFailed">If the web service request failed</param>
        /// <param name="requestServiceMsg">Web service message</param>
        /// <param name="dateTimeServiced"></param>
        /// <param name="sentBy"></param>
        public static void InsertWebServiceActivityRecord(IFormsRepository formsRepo, int activityId, string activityParamaters, bool requestFailed, string requestServiceMsg = null, DateTime? dateTimeServiced = null, string sentBy = null)
        {
            def_WebServiceActivity webServiceActivity = new def_WebServiceActivity()
            {
                EnterpriseID = SessionHelper.LoginStatus.EnterpriseID,
                GroupID = SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0],
                IssuedDateTime = DateTime.Now,
                IssuedByUserID = SessionHelper.LoginStatus.UserID,
                ActivityID = activityId,
                ActivityParameters = activityParamaters,
                RequestFailed = requestFailed,
                RequestServiceMsg = requestServiceMsg,
                DateTimeServiced = dateTimeServiced,
                SentBy = sentBy
            };

            formsRepo.AddWebServiceActivity(webServiceActivity);
        }

       /// <summary>
       /// Looks at uas_GroupUserAppPermissions integration webservice permission. If it is enabled, returns true.
       /// 
       /// Stores enabled status in session variable to speed it up next time it is run.
       /// </summary>
       /// <returns></returns>
        public static bool IsWebServiceEnabled()
        {
           string webServiceEnabled = SessionHelper.Read<string>("webservice_enabled");
           
            if (webServiceEnabled != null) {
                if (webServiceEnabled.ToLower() == "true") 
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            
            if (UAS_Business_Functions.hasPermission(PermissionConstants.WS_ACCESS, PermissionConstants.INTEGRATION))
            {
                SessionHelper.Write("webservice_enabled", "true");
                return true;
            } 
            else 
            {
                SessionHelper.Write("webservice_enabled", "false");
                return false;
            }
        }

        /// <summary>
        /// Function to add onto later for webservice integration. Currently just inserts an activity record saying that
        /// the web service is not yet implemented.
        /// 
        /// 4/29 BR - Functionality for updating WebServiceActivity after certain functions have been performed on Assessments
        /// has been moved to triggers within the data.  This method is now obsolete.
        /// </summary>
        /// <param name="formsRepo"></param>
        /// <param name="activityId"></param>
        /// <param name="activityParamaters"></param>
        [Obsolete]
        internal static void CallWebService(IFormsRepository formsRepo, int activityId, string activityParamaters)
        {
            bool requestFailed = true;
            string requestServiceMsg = "Web service not yet implemented";
            DateTime? dateTimeServiced = null;
            string sentBy = null;
            
            //InsertWebServiceActivityRecord(formsRepo, activityId, activityParamaters, requestFailed, requestServiceMsg, dateTimeServiced, sentBy);
        }
    }
}