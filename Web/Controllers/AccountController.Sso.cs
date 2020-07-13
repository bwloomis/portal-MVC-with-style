using Assmnts.Infrastructure;
using Assmnts.UasServiceRef;

using Data.Concrete;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using System.Web.Configuration;
using System.Xml;

using UAS.Business;
using UAS.DataDTO;


namespace Assmnts.Controllers
{

    public partial class AccountController : Controller
    {

        [HttpGet]
        [AllowAnonymous]
        public ActionResult SsoLogin(string sessionId = "", string appId = "")
        {
            string portalSessionId = sessionId;
            // Portal Session is saved to be used when using SSO to SecureEmail or other systems that use UAS SSO
            SessionHelper.PortalSession = sessionId;

            string ipAddress = UtilityFunction.GetUserIPAddress();
            string appSessionId = Session.SessionID;

            int applicationId = UAS.Business.Constants.APPLICATIONID;    // Get the Default Application Id (probably SIS or ADAP)
            try
            {
                if (String.IsNullOrEmpty(appId))
                    appId = "3";        // Default to ADAP - this should proabably be deleted.
                applicationId = Convert.ToInt32(appId);
            }
            catch (Exception excpt)
            {
                Debug.WriteLine("SsoLogin - failed getting applicationId: " + excpt.Message);
                // think DEF (Assmnts) has a specific Error screen that should be used.
                ViewBag.ErrorMessage = "Request.Cookies error: " + excpt.Message;
                return View("loginSIS");
            }

            AuthenticationClient webclient = new AuthenticationClient();

            string authResult = string.Empty;
            try
            {
                authResult = webclient.SsoLogin(portalSessionId, ipAddress, applicationId, appSessionId);

                Debug.WriteLine("SsoLogin XML result: " + authResult);
            }
            catch (Exception excptn)
            {
                authResult = "<record><errormessage>" + excptn.Message + "</errormessage></record>";
                Debug.WriteLine("SsoLogin webservice exception: " + excptn.Message);
                ViewBag.ErrorMessage = excptn.Message;
                return View("loginSIS");
            }

            try 
            {
                UAS.DataDTO.LoginStatus loginStatus = ProcessSsoAuth(authResult);

                UAS.DataDTO.LoginInfo loginInfo = FillLoginInfo(loginStatus);

                string userName = String.Empty;
                Debug.WriteLine("LoginStatus ErrorMessage: " + loginStatus.ErrorMessage);
                Debug.WriteLine("LoginStatus Status: " + loginStatus.Status);
                userName = loginStatus.FirstName + " " + loginStatus.LastName;
                Debug.WriteLine("LoginStatus Name: " + userName);


                if ((loginStatus.Status == 'A') &&
                     (loginStatus.UserID > 0) &&
                     (loginStatus.appGroupPermissions.Count > 0)
                   )
                {
                    loginInfo.IsLoggedIn = true;
                    SessionHelper.IsUserLoggedIn = true;
                    SessionHelper.LoginInfo = loginInfo;
                    SessionHelper.LoginStatus = loginStatus;

                    var userContext = webclient.GetUserContextLightweight(loginStatus.EnterpriseID, loginStatus.UserID);
                    if (userContext == null)
                    {
                        throw new Exception(String.Format("Unable to get UserContext for UserId = {0}.", loginStatus.UserID));
                    }
                    SessionHelper.UserSecurityContext = new UserSecurityContext
                    {
                        UserContext = userContext,
                    };

                    string clientUserId = Request["userId"] as string;
                    Session.Add("clientUserId", clientUserId);

                    if (applicationId == 3)
                    {
                        return RedirectToAction("Index", "Adap");
                    }
                    return RedirectToAction("Index", "Search");
                }
                
                SessionHelper.IsUserLoggedIn = false;

                ViewBag.ErrorMessage = loginStatus.ErrorMessage;
            }
            catch(Exception xcptn)
            {
                Debug.WriteLine("SsoLogin exception: " + xcptn.Message);
                ViewBag.ErrorMessage = xcptn.Message;
            }

            return View("loginSIS");
      
        }
        
        /// <summary>
        /// Takes a LoginStatus and creates corresponding LoginInfo.
        /// </summary>
        /// <param name="loginStatus"></param>
        /// <returns></returns>
        private LoginInfo FillLoginInfo(LoginStatus loginStatus)
        {
            LoginInfo loginInfo = new LoginInfo();
            
            using (var context = DataContext.getUasDbContext())
            {
                uas_User result = null;
                try
                {
                    result = context.uas_User.Where(u => u.UserID == loginStatus.UserID).Select(u => u).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("FillLoginInfo exception: " + ex.Message);                     
                }

                if (result != null)
                {
                    loginInfo.LoginID = result.LoginID;
                }

                loginInfo.SessionData = Session.SessionID;
            }

            return loginInfo;
        }
        /// <summary>
        /// Takes validation XML and creates a login status.
        /// </summary>
        /// <param name="authResult"></param>
        /// <returns></returns>
        private LoginStatus ProcessSsoAuth(string authResult)
        {
            LoginStatus loginStatus = new LoginStatus();    
            if (!string.IsNullOrEmpty(authResult))
            {
                    XmlDocument xDoc = new XmlDocument();
                    try
                    {
                        xDoc.LoadXml(authResult);
                        loginStatus.UserID = Convert.ToInt32(xDoc.GetElementsByTagName("userid")[0].InnerText);
                        loginStatus.EnterpriseID = Convert.ToInt32(xDoc.GetElementsByTagName("enterprise_id")[0].InnerText);

                        // Fill with dummy for now - should probably be deleted from the structure.
                        loginStatus.GroupID = 0;

                        loginStatus.appGroupPermissions = new List<AppGroupPermissions>();
                        AppGroupPermissions agp = new AppGroupPermissions();
                        agp.groupPermissionSets = new List<GroupPermissionSet>();
                        string appId = xDoc.GetElementsByTagName("applicationid")[0].InnerText;
                        if (!String.IsNullOrEmpty(appId))
                        {
                            agp.ApplicationID = Convert.ToInt32(appId);
                        }
                        loginStatus.appGroupPermissions.Add(agp);
                        
                        // Get the Group Security Sets
                        XmlNode nodeAppPerms = xDoc.SelectSingleNode("record/application_permissions");
                        XmlNodeList xnlGrpPrmList = nodeAppPerms.SelectNodes("group_permission");

                        bool enterpriseWideGroup = false;
                        foreach (XmlNode xnGrpPrm in xnlGrpPrmList)
                        {
                            GroupPermissionSet gps = new GroupPermissionSet();
                            gps.GroupID = Convert.ToInt32(xnGrpPrm.Attributes.GetNamedItem("id").Value);
                            if (gps.GroupID == 0)
                            {
                                enterpriseWideGroup = true;
                            }
                            gps.PermissionSet = xnGrpPrm.InnerText;
                            loginStatus.appGroupPermissions[0].groupPermissionSets.Add(gps);
                        }

                        if (loginStatus.appGroupPermissions.Count == 0 || loginStatus.appGroupPermissions[0].groupPermissionSets.Count == 0)
                        {
                            Exception e = new Exception(@"User not authorized for this application.");
                            e.Data["noAuth"] = true;
                            throw e;
                        }

                        XmlNode nodeAuthGroups = nodeAppPerms.SelectSingleNode("authorizedGroups");
                        XmlNodeList xnlAuthGroupList = nodeAuthGroups.SelectNodes("groupId");

                        if (enterpriseWideGroup == false)
                        {
                            List<int> authGroups = new List<int>();
                            foreach (XmlNode xnAuthGroup in xnlAuthGroupList)
                            {
                                int grp = Convert.ToInt32(xnAuthGroup.InnerText);
                                authGroups.Add(grp);
                            }

                            loginStatus.appGroupPermissions[0].authorizedGroups = authGroups;//.ToArray();
                        }
                        else
                        {
                            loginStatus.appGroupPermissions[0].authorizedGroups = new int[1] {0}.ToList();
                        }
                        // loginStatus.PermissionSet = xDoc.GetElementsByTagName("permissions")[0].InnerText;

                        loginStatus.Status = Convert.ToChar(xDoc.GetElementsByTagName("statusflag")[0].InnerText.Substring(0, 1));
                        loginStatus.UserKey = xDoc.GetElementsByTagName("userkey")[0].InnerText;
                        loginStatus.EmailAddress = xDoc.GetElementsByTagName("useremail")[0].InnerText;
                        loginStatus.FirstName = xDoc.GetElementsByTagName("userfirstname")[0].InnerText;
                        loginStatus.LastName = xDoc.GetElementsByTagName("userlastname")[0].InnerText;
                        Debug.WriteLine("AuthenticateUser FirstName LastName: " + loginStatus.FirstName + " " + loginStatus.LastName);
                        loginStatus.SecureDomain = Convert.ToBoolean(xDoc.GetElementsByTagName("securedomain")[0].InnerText);
                        loginStatus.IsAdmin = xDoc.GetElementsByTagName("role")[0].InnerText.ToLower().Contains("admin") ? true : false;
                        loginStatus.ErrorMessage = xDoc.GetElementsByTagName("errormessage")[0].InnerText;
                        Debug.WriteLine("AuthenticateUser ls.ErrorMessage: " + loginStatus.ErrorMessage);
                    }
                    catch (Exception excptn)
                    {
                        if (excptn.Data.Contains("noAuth"))
                        {
                            loginStatus.ErrorMessage = excptn.Message;
                            Debug.WriteLine("Not authorized: " + excptn.Message);
                        }
                        else
                        {
                            string msg = "ProcessSsoAuth XML conversion exception: " + excptn.Message;
                            Debug.WriteLine(msg);
                            loginStatus.ErrorMessage = excptn.Message;
                        }
                        
                    }
                    Session["UserIsAdm"] = loginStatus.IsAdmin.ToString();
            }
                
            return loginStatus;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult SsoLogout()
        {
            try
            {
                int appId = 3;      // ADAP
                using (AuthenticationClient auth = new AuthenticationClient())
                {
                    auth.SsoLogout(SessionHelper.LoginStatus.UserID, SessionHelper.LoginInfo.SessionData, appId, UtilityFunction.GetUserIPAddress());
                }
                SessionHelper.IsUserLoggedIn = false;
                SessionHelper.UserId = -1;
                SessionHelper.LoginStatus = null;

            }
            catch (Exception e)
            {
                Debug.WriteLine("* * *  AccountController Logout Exception: " + e.Message);
            }

            SessionHelper.DestroySession();

            string basePortalUrl = WebConfigurationManager.AppSettings["UASAdminURL"];
            return Redirect(new Uri(new Uri(basePortalUrl), "Portal").ToString());

        }

    }
}
