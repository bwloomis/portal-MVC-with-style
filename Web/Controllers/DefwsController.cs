using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Linq;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Text;

using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Abstract;
using Data.Concrete;

using UAS.Business;
using UAS.DataDTO;
using Assmnts.Business;
using System.Data.Common;
using System.Data;


namespace Assmnts.Controllers
{
    /*
     * The DEF3 Web Services Controller
     * 
     * This can be accessed from a variety of sources via HTTP GET/POST (browser, program, etc.).
     * Workflow requires the client to perform the Login method here or on the login screen before attempting to use other methods.
     * 
     */
    public partial class DefwsController : Controller
    {

        private IFormsRepository formsRepo;

        public DefwsController(IFormsRepository fr)
        {
            // Initialized by Infrastructure.Ninject
            formsRepo = fr;

        }

        [HttpGet]
        public ActionResult Index()
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account");
            }
            Session["userId"] = "0";
            Debug.WriteLine("* * *  DefwsController:Index method  * * *");

            try
            {
                TestUploadMultipleAssmntXmls();
            }
            catch (Exception e)
            {
                return Content(e.StackTrace);
            }
            return View("testCreateRspVars");

        }

        [HttpGet]
        public string GetSisIdFromTrackingNumber( int formId, int trackingNumber )
        {
            List<int> result = new List<int>();
            def_ItemVariables ivTrackNum = formsRepo.GetItemVariableByIdentifier( "sis_track_num" );

            foreach (def_FormResults fr in formsRepo.GetFormResultsByFormId(formId).ToList())
            {
                def_ResponseVariables rvTrackNum = formsRepo.GetResponseVariablesByFormResultItemVarId(fr.formResultId, ivTrackNum.itemVariableId);
                if (rvTrackNum != null && rvTrackNum.rspInt.HasValue && rvTrackNum.rspInt.Value == trackingNumber)
                    result.Add(fr.formResultId);
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Count(); i++)
            {
                sb.Append(result[i]);
                if (i + 1 < result.Count())
                    sb.Append(", ");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Login for WebServices.  
        /// Establishes a session if User has Application Permissions.
        /// </summary>
        /// <param name="userId">UAS User Login</param>
        /// <param name="pwrd">UAS User Password</param>
        /// <returns>string message of login status.</returns>
        [HttpGet]
        public string Login(string userId, string pwrd)
        {
            Debug.WriteLine("Login  userId: " + userId + "     pwrd: " + pwrd);

            LoginInfo loginInfo = new LoginInfo();
            loginInfo.RememberMe = false;

            loginInfo.LoginID = userId;
            loginInfo.Password = pwrd;

            LoginStatus loginStatus = UAS_Business_Functions.VerifyUser(loginInfo);
            SessionHelper.LoginStatus = loginStatus;
            // SessionHelper.LoginStatus.EnterpriseID = 1;
            // SessionHelper.LoginStatus.GroupID = 3;

            if ((loginStatus.Status == 'A') &&
                    (loginStatus.UserID > 0) &&
                // !string.IsNullOrEmpty(loginStatus.PermissionSet)
                    (loginStatus.appGroupPermissions.Count > 0)
                )
            {
                loginInfo.IsLoggedIn = true;
                SessionHelper.IsUserLoggedIn = true;
                SessionHelper.LoginInfo = loginInfo;
                SessionHelper.LoginStatus = loginStatus;
               return "You are now logged in.";
            }
            else
            {
                return "Invalid user name or password.";
            }

            // *** RRB 10/29/15 Shouldn't this be returning a boolean or integer so it can be tested for a valid login ??

        }

        [HttpGet]
        public string GetEnterprise()
        {
            return SessionHelper.LoginStatus.EnterpriseID.ToString();
        }

        [HttpGet]
        public string GetUser()
        {
            return SessionHelper.LoginStatus.UserID.ToString();
        }
        
        [HttpGet]
        public string GetApplication()
        {
            return UAS.Business.Constants.APPLICATIONID.ToString();
        }
        
        [HttpPost]
        public string VerifyUser(string userId, string pwrd, string entId, string grpId)
        {
            Debug.WriteLine("Verify userId: " + userId + "     pwrd: " + pwrd);
            int iEntId, iGrpId;
            try
            {
                iEntId = Int32.Parse(entId);
            }
            catch
            {
                return "Enterprise ID must be a number.";
            }

            try 
            {
                iGrpId = Int32.Parse(grpId);
            }
            catch 
            {
                return "Group ID must be a number.";
            }
            
            bool groupCorrect = false, entCorrect = false;
            LoginInfo loginInfo = new LoginInfo();
            loginInfo.RememberMe = false;

            loginInfo.LoginID = userId;
            loginInfo.Password = pwrd;

            LoginStatus loginStatus = UAS_Business_Functions.VerifyUser(loginInfo);

            if (loginStatus != null)
            {
                if (loginStatus.EnterpriseID == iEntId)
                {
                    entCorrect = true;
                }

                if (loginStatus.appGroupPermissions != null 
                    && loginStatus.appGroupPermissions.Count > 0
                    && loginStatus.appGroupPermissions[0].groupPermissionSets.Count > 0
                    && loginStatus.appGroupPermissions[0].groupPermissionSets[0].GroupID == iGrpId)
                {
                    groupCorrect = true;

                }
            }

            if ( (loginStatus != null) && (loginStatus.Status == 'A') &&
                     (loginStatus.UserID > 0) &&
                     // !string.IsNullOrEmpty(loginStatus.PermissionSet)
                     (loginStatus.appGroupPermissions.Count > 0) && 
                     entCorrect && 
                     groupCorrect
               )
            {
                using (var context = DataContext.getUasDbContext())
                {
                    string enterprise = context.uas_Enterprise.Where(e => e.EnterpriseID == loginStatus.EnterpriseID).Select(e => e.EnterpriseDescription).SingleOrDefault();

                    return "The User Login: " + userId + " is valid for " + loginStatus.FirstName + " " + loginStatus.LastName + "  Enterprise: " + enterprise;
                }
            }
            else
            {
                if (loginStatus.appGroupPermissions != null)
                {
                    if (loginStatus.appGroupPermissions.Count == 0 || loginStatus.appGroupPermissions[0].ApplicationID != UAS.Business.Constants.APPLICATIONID || loginStatus.appGroupPermissions[0].groupPermissionSets.Count == 0)
                    {
                        return "User not authorized for this application.";
                    }

                    if (!groupCorrect || !entCorrect)
                    {
                        return "User is not authorized for the Enterprise or Group.";
                    }
                
                }
                
                return "Invalid UserId and/or password";
            }
            
        }

        /*
         * Creates a new FormResultId record
         * Future enhancement will require FKs for Enterprise, Group, etc.
         * 
         */
        [HttpGet]
        public ActionResult CreateNewFormResult( string formId )
        {

            def_FormResults frmRes = FormResults.CreateNewFormResultModel(formId);

            ContentResult result = new ContentResult();
            try
            {
                int formRsltId = formsRepo.AddFormResult(frmRes);
                result.Content = formRsltId.ToString();

                //test CreateNewResponseValues
                /*
                Dictionary<string, string> responsesByIdentifer = new Dictionary<string, string>();
                responsesByIdentifer.Add("sis_cl_addr_line1", "I am the walrus.");
                CreateNewResponseValues(formRsltId, responsesByIdentifer);
                 */
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Defws.CreateNewFormResult exception:" + excptn.Message);
            }

            return result;      // return the FormResult id just added
        }

        /*
         * Creates a new FormResultId record with additional attributes:
         *  Enterprise, Group, User, etc.
         * 
         */
        [HttpGet]
        public ActionResult CreateNewFormResultFull(string formId, string enterprise_id, string group_id, string subject, string interviewer)
        {
            def_FormResults frmRes = FormResults.CreateNewFormResultModel(formId);

            // Add the FKs
            if (!String.IsNullOrEmpty(enterprise_id))
            {
                frmRes.EnterpriseID = Convert.ToInt32(enterprise_id);
            }

            if (!String.IsNullOrEmpty(group_id))
            {
                frmRes.GroupID = Convert.ToInt32(group_id);
            }

            if (!String.IsNullOrEmpty(subject))
            {
                frmRes.subject = Convert.ToInt32(subject);
            }

            if (!String.IsNullOrEmpty(interviewer))
            {
                frmRes.interviewer = Convert.ToInt32(interviewer);
            }

            ContentResult result = new ContentResult();
            try
            {
                int formRsltId = formsRepo.AddFormResult(frmRes);
                result.Content = formRsltId.ToString();
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Defws.CreateNewFormResultFull exception:" + excptn.Message);
            }

            return result;      // return the FormResult id just added
        }

        
        
        /*
         * Method to add ResponseValues to a FormResult
         * 
         */
        [HttpPost]
        public string CreateNewResponseValues(string frmRsltId, Dictionary<string, string> responsesByIdentifier)
        {
            // Debugging code
            /*
            Debug.WriteLine("frmRsltId: " + frmRsltId);
            foreach (KeyValuePair<string,string> idntVal in responsesByIdentifier)
            {
                Debug.WriteLine("responsesByIdentifier: " + idntVal.Key + "," + idntVal.Value);
            }


            if (1 == 1)
                return new EmptyResult();
            */

            string returnMsg = formsRepo.CreateNewResponseValues(frmRsltId, responsesByIdentifier);

            return returnMsg;
        }

        public def_FormResults CopyAssessment(int formResultId)
        {
            def_FormResults oldFormResult = formsRepo.GetFormResultById(formResultId);
            
            def_FormResults copyFormResult = FormResults.CreateNewFormResultModel(oldFormResult.formId.ToString());

            copyFormResult.archived = oldFormResult.archived;
            copyFormResult.assigned = oldFormResult.assigned;
            copyFormResult.dateUpdated = oldFormResult.dateUpdated;
            copyFormResult.deleted = oldFormResult.deleted;
            copyFormResult.EnterpriseID = oldFormResult.EnterpriseID;
            copyFormResult.formId = oldFormResult.formId;
            copyFormResult.formStatus = oldFormResult.formStatus;
            copyFormResult.GroupID = oldFormResult.GroupID;
            copyFormResult.interviewer = oldFormResult.interviewer;
            copyFormResult.locked = oldFormResult.locked;
            copyFormResult.reviewStatus = ReviewStatus.PRE_QA;
            copyFormResult.sessionStatus = oldFormResult.sessionStatus;
            copyFormResult.statusChangeDate = oldFormResult.statusChangeDate;
            copyFormResult.subject = oldFormResult.subject;
            copyFormResult.training = oldFormResult.training;

            formsRepo.AddFormResult(copyFormResult);

            CopyAssessmentData(oldFormResult.formResultId, copyFormResult.formResultId);

            return copyFormResult;
        }

        private void CopyAssessmentData(int oldFormResultId, int copyFormResultId)
        {
            try
            {

                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText 
                            = "SELECT SELECT IR.itemId, RV.itemVariableId, RV.rspValue from def_ItemResults IR JOIN def_FormResults FR on FR.formResultId = IR.formResultId JOIN def_ResponseVariables RV on RV.itemResultId = IR.itemResultId WHERE FR.formResultId = " + oldFormResultId + " ORDER BY itemId";
                        command.CommandType = CommandType.Text;
                        DataTable dt = new DataTable();
                        dt.Load(command.ExecuteReader());

                        if (dt != null)
                        {
                            SaveAssessmentFromDataTable(copyFormResultId, dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  CreateFormResultJSON exception: " + ex.Message);
            }
        }

        private void SaveAssessmentFromDataTable(int formResultId, DataTable dt)
        {
            int prevItemId = -1;

            def_ItemResults itemResult = null;
            foreach (DataRow row in dt.Rows)
            {
                int currItemId = Int32.Parse(row["itemId"].ToString());
                int itemVariableId = Int32.Parse(row["itemVariableId"].ToString());
                string rspValue = row["rspValue"].ToString();

                if (currItemId != prevItemId)
                {
                    itemResult = new def_ItemResults();
                    itemResult.dateUpdated = DateTime.Now;
                    itemResult.itemId = currItemId;
                    itemResult.sessionStatus = 0;
                    itemResult.formResultId = formResultId;

                    formsRepo.AddItemResultNoSave(itemResult);
                }

                if (itemResult != null) { 
                    def_ResponseVariables responseVariable = new def_ResponseVariables();
                    responseVariable.itemVariableId = itemVariableId;
                    responseVariable.rspValue = rspValue;

                    def_ItemVariables itemVariable = formsRepo.GetItemVariableById(itemVariableId);

                    if (itemVariable != null)
                    {
                        formsRepo.ConvertValueToNativeType(itemVariable, responseVariable);

                        itemResult.def_ResponseVariables.Add(responseVariable);
                    }

                }
                prevItemId = currItemId;

            }

            formsRepo.Save();
        }
    }
}
