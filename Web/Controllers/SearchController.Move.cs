using System;
using System.Diagnostics;
using System.Web.Mvc;
using System.Linq;

using Assmnts.Models;
using Data.Concrete;
using UAS.Business;
using Assmnts.Infrastructure;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using Assmnts.UasServiceRef;
using System.Configuration;



namespace Assmnts.Controllers
{
    public partial class SearchController : Controller
    {
        [HttpPost]
        public ActionResult Move(int formResultId)
        {
            SearchModel model = new SearchModel();
            
            if (model.move == true)
            {
                MoveModel moveModel = new MoveModel(formResultId, formsRepo);

                
                return View("Move", moveModel);
                
            }

            return RedirectToAction("Index", "Search");
        }

        [HttpPost]
        public ActionResult ChangeEnterpriseForGroups(int formResultId, int entId)
        {
            MoveModel moveModel = new MoveModel(formResultId, formsRepo);

            moveModel.GetGroups(entId);
                
            return View("MoveGroup", moveModel);
      
        }

        [HttpPost]
        public ActionResult ChangeEnterpriseForUsers(int formResultId, int entId)
        {
           MoveModel moveModel = new MoveModel(formResultId, formsRepo);

            moveModel.GetUsers(entId);

            return View("MoveUser", moveModel);

        }

        [HttpPost]
        public ActionResult ChangeGroupForUsers(int formResultId, int groupId, int entId = -1)
        {
            MoveModel moveModel = new MoveModel(formResultId, formsRepo);

            moveModel.GetUsersByGroup(groupId, entId);

            return View("MoveUser", moveModel);
        }

        [HttpPost]
        public int SelectGroupByUser(int userId)
        {
            using (var context = DataContext.getUasDbContext())
            {
                int? groupId = context.uas_GroupUserAppPermissions.Where(g => g.UserID == userId && g.ApplicationID == UAS.Business.Constants.APPLICATIONID).Select(g => g.GroupID).FirstOrDefault();

                if (groupId == null)
                {
                    groupId = -1;
                }

                return (int)groupId;
            }
                        
        }
        [HttpPost]
        public string MoveAssessment(int formResultId, int userId, int groupId, int enterpriseId)
        {
            try
            {
                def_FormResults formResult = formsRepo.GetFormResultById(formResultId);
                int prevUser = (int) formResult.assigned;
                if (userId > -1)
                {
                    formResult.assigned = userId;
                }
                else
                {
                    formResult.assigned = null;
                }
                if (groupId > -1) // a group has been selected from the drop down list
                {
                    formResult.GroupID = groupId;
                }
                else if (groupId == -2) // empty selection in drop down list (null group) 
                {
                    formResult.GroupID = null;
                }
                if (enterpriseId > -1)
                {
                    formResult.EnterpriseID = enterpriseId;
                }
                formsRepo.SaveFormResults(formResult);
                EmailUpdate(formResultId, prevUser, userId);

                if (ventureMode == false)
                {
                    AccessLogging.InsertAccessLogRecord(formsRepo, formResult.formResultId, (int)AccessLogging.accessLogFunctions.MOVE, "Move assessment.");
                }

                return "Assessment has been successfully moved.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MoveAssessment exception: " + ex.Message);
                return "Error: " + ex.Message;
            }
        }

        /// <summary>
        /// Assembles an email describing a move, then passes it to AJBoggs.SIS for sending.
        /// </summary>
        /// <param name="formResultId"></param>
        /// <param name="prevUserId"></param>
        /// <param name="nextUserId"></param>
        public void EmailUpdate(int formResultId, int prevUserId, int nextUserId)
        {
            // initialize message vars and create message.
            AuthenticationClient webclient = new AuthenticationClient();
            def_ResponseVariables firstName = null;
            try
            {
                firstName = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "sis_cl_first_nm");
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("MoveAssessment EmailUpdate: " + excptn.Message);
            }

            def_ResponseVariables lastName = null;
            try
            {
                lastName = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "sis_cl_last_nm");
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("MoveAssessment EmailUpdate: " + excptn.Message);
            }

            def_ResponseVariables city = null;
            try
            {
                city = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "sis_cl_city");
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("MoveAssessment EmailUpdate: " + excptn.Message);
            }

            UserDisplay prevUser = webclient.GetUserDisplay(prevUserId);
            UserDisplay nextUser = webclient.GetUserDisplay(nextUserId);
            UserDisplay assignBy = webclient.GetUserDisplay(SessionHelper.LoginStatus.UserID);

            string toEmails = String.Empty;
            toEmails = createEmailString(prevUser, toEmails);
            toEmails = createEmailString(nextUser, toEmails);
            toEmails = createEmailString(assignBy, toEmails);
            string fromEmail = ConfigurationManager.AppSettings["AcctLockoutEmail"].ToString();

            string emailSubject = "Assignment Change Notification";
            string emailBody = "A SIS Assessment has been reassigned.\n\nSIS Id: " + formResultId;
            if (firstName != null || lastName != null || city != null)
            {
                emailBody += "\nClient: " + ((firstName != null)? firstName.rspValue.Substring(0,1) + ". " : String.Empty) 
                    + ((lastName != null)? lastName.rspValue.Substring(0,1) + "." : String.Empty)
                    + ((city != null) ? " from " + city.rspValue : String.Empty);
            }

            emailBody += "\n\nAssessment has been reassigned to: " + nextUser.FirstName + " " + nextUser.LastName + 
                "\nUser Id:\t" + nextUser.LoginID +
                "\nOrg:\t" + nextUser.GroupName;

            if (nextUser.Phones.Count > 0)
            {
                emailBody += "\nPhone:\t" + nextUser.Phones[0].Phone;
            }
            if (nextUser.Emails.Count > 0)
            {
                emailBody += "\nEmail:\t" + nextUser.Emails[0].Email;
            }

            emailBody += "\n\nAssessment was previously assigned to: " + prevUser.FirstName + " " + prevUser.LastName + 
                "\nUser Id:\t" + prevUser.LoginID +
                "\nOrg:\t" + prevUser.GroupName;

            if (prevUser.Phones.Count > 0)
            {
                emailBody += "\nPhone:\t" + prevUser.Phones[0].Phone;
            }
            if (prevUser.Emails.Count > 0)
            {
                emailBody += "\nEmail:\t" + prevUser.Emails[0].Email;
            }

            emailBody += "\n\nAssessment was reassigned by: " + assignBy.FirstName + " " + assignBy.LastName + 
                "\nUser Id:\t" + assignBy.LoginID +
                "\nOrg:\t" + assignBy.GroupName;

            if (assignBy.Phones.Count > 0)
            {
                emailBody += "\nPhone:\t" + assignBy.Phones[0].Phone;
            }
            if (assignBy.Emails.Count > 0)
            {
                emailBody += "\nEmail:\t" + assignBy.Emails[0].Email;
            }

            AJBoggs.Sis.Domain.Assessments emailServer = new AJBoggs.Sis.Domain.Assessments();
            emailServer.SendEmail(toEmails, fromEmail, emailBody, emailSubject, false);
        }

        private string createEmailString(UserDisplay user, string emailString) {
            foreach(var e in user.Emails) {
                if (!String.IsNullOrEmpty(emailString)) {
                    emailString += ";";
                }

                if (e != null && !String.IsNullOrEmpty(e.Email)) {
                    emailString += e.Email;
                    break;
                }
            }

            return emailString;
        }
    }
}