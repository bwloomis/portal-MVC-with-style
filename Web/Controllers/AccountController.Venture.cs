using AJBoggs.Sis.Domain;
using Assmnts.Infrastructure;

using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using System.Xml;

using UAS.Business;
using UAS.DataDTO;
//using WebService;
using VistaDB.DDA;
using System.Data.Common;


namespace Assmnts.Controllers
{

    public partial class AccountController : Controller
    {
        /// <summary>
        /// Class to setup custom Venture settings.
        /// </summary>
        /// <param name="assemblyFileVersion"></param>
        /// <remarks>
        /// Setup the database contexts.
        /// </remarks>
        private void SetupVentureMode(string assemblyFileVersion)
        {
            // Setup the database contexts in the web.config to use the VistaDB database in the User Local AppData directory.
            //   This is done due to Windows Permission issues in the main Venture directory.
                try
                {
                    string localAppDataPath = Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%");
                    string dataDirectory = Path.Combine(localAppDataPath, @"AAIDD\Venture", assemblyFileVersion);
                    Debug.WriteLine("* * * AccountController.SetupVentureMode dataDirectory:" + dataDirectory);

                    if (!Directory.Exists(dataDirectory))
                    {
                        Debug.WriteLine("* * * AccountController.SetupVentureMode dataDirectory doesn't exist !!");
                    }

                    // Set the DataDirectory that is used by the web.config settings
                    AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);
                }
                catch (Exception xcptn)
                {
                    Debug.WriteLine("* * * AccountController.SetupVentureMode Exception: " + xcptn.Message);
                }

            Debug.WriteLine("AccountController.SetupVentureMode  SessionHelper.VentureMode: " + SessionHelper.IsVentureMode.ToString());

        }

        //
        // GET: /Account/LoginVenture
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult LoginVenture(FormCollection frmCollection)
        {
            Debug.WriteLine("* * * AccountController.Venture LoginVenture  * * *");

            try
            {
                UAS.DataDTO.LoginInfo loginInfo = new UAS.DataDTO.LoginInfo();
                loginInfo.RememberMe = false;

                CookieData cookieData = UAS.Business.HttpFunctions.GetUserCookie();
                if (cookieData.LoginID != null)
                {
                    loginInfo.LoginID = cookieData.LoginID;
                    loginInfo.RememberMe = true;
                }

                // * * * Need to use Cookie data if available.
                loginInfo.LoginID = frmCollection["userName"];
                loginInfo.Password = frmCollection["pwrd"];

                LoginStatus loginStatus = null;
                loginStatus = LoginVentureLogic(loginInfo);
                Debug.WriteLine("* * * AccountController LoginVenture  next * * *");
             
                string userName = String.Empty;
                if (loginStatus != null)
                {
                    Debug.WriteLine("LoginStatus ErrorMessage: " + loginStatus.ErrorMessage);
                    Debug.WriteLine("LoginStatus Status: " + loginStatus.Status);
                    userName = loginStatus.FirstName + " " + loginStatus.LastName;
                    Debug.WriteLine("LoginStatus Name: " + userName);
                    // Debug.WriteLine("LoginStatus Permissions: " + loginStatus.PermissionSet);
                }
                else
                {
                    Debug.WriteLine("loginStatus is NULL !!!");
                    throw new Exception("Invalid username or password.");
                }

                loginInfo.SessionData = Session.SessionID;

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
                }

                if (loginInfo.IsLoggedIn)
                {
                    SessionHelper.SessionTotalTimeoutMinutes = Business.Timeout.GetTotalTimeoutMinutes(SessionHelper.LoginStatus.EnterpriseID);
                    // return RedirectToAction("Index", "Home");
                    SessionHelper.Write("justLoggedIn", true);
                    Assessments assmnts = new Assessments(formsRepo);
                    assmnts.DeleteOldUploadedAssessments();

                    if (SessionHelper.Read<bool>("NoServer") == true)
                    {
                        return RedirectToAction("Index", "Search");
                    }
                    return RedirectToAction("Index", "DataSync");
                  
                }

                SessionHelper.IsUserLoggedIn = false;
                ViewBag.ErrorMessage = loginStatus.ErrorMessage;
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("LoginVenture exception: " + xcptn.Message);
                ViewBag.ErrorMessage = xcptn.Message;
            }

            return View("loginVenture");

        }

        private LoginStatus LoginVentureLogic(LoginInfo loginInfo)
        {
            LoginStatus loginStatus = null;
            Debug.WriteLine("AccountController.Venture LoginVentureLogic Authenticate User: " + loginInfo.LoginID);

            //Check for domain in email address
            if (loginInfo.LoginID != null)
            {
                // 6/14/16 BR - Removed to allow emails to log in to Venture as with the web site.
                //if (loginInfo.LoginID.Contains("@"))
                //{
                //    string before = loginInfo.LoginID.Substring(0, loginInfo.LoginID.IndexOf("@"));
                //    string mid = loginInfo.LoginID.Substring(loginInfo.LoginID.IndexOf("@") + 1);
                //    if (mid.Contains("."))
                //    {
                //        mid = mid.Substring(0, mid.IndexOf("."));
                //    }
                //    loginInfo.LoginID = before;
                //    loginInfo.Domain = mid;
                //}

                //string passkey = UtilityFunction.EncryptPassKey(loginInfo.LoginID);
                string passkey = UtilityFunction.EncryptPassKey(loginInfo.LoginID);
                string pwd = UtilityFunction.EncryptPassword(loginInfo.Password);
                // Debug.WriteLine("Authenticate User  webclient.AuthenticateUser");
                PackingFunction();

                string validation = String.Empty;
                try
                {
                    validation = UAS.Business.LocalClient.AuthenticateLocalUser(passkey, loginInfo.Domain, loginInfo.LoginID, loginInfo.Password);
                }
                catch (Exception excptn)
                {
                    validation = "<record><errormessage>" + excptn.Message + "</errormessage></record>";
                    Debug.WriteLine("Authenticate User  Venture exception: " + excptn.Message);
                }
                Debug.WriteLine("Authenticate User Venture validation: " + validation);
                
                if ( !string.IsNullOrEmpty(validation) )
                {
                    XmlDocument xDoc = new XmlDocument();
                    loginStatus = new LoginStatus();
                    
                    try
                    {
                        xDoc.LoadXml(validation);
                        loginStatus.UserID = Convert.ToInt32(xDoc.GetElementsByTagName("userid")[0].InnerText);
                        loginStatus.EnterpriseID = Convert.ToInt32(xDoc.GetElementsByTagName("enterprise_id")[0].InnerText);

                        /* Copied over by LAK, 1/28/2015
                         * Deleted by RRB 12/20/14 - Group is part of the groupPermissionSets
                        string groupId = xDoc.GetElementsByTagName("enterprise_id")[0].InnerText;
                        if ( !String.IsNullOrEmpty(groupId) )
                        {
                            loginStatus.GroupID = Convert.ToInt32(groupId);
                        }
                         */
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
                            loginStatus.appGroupPermissions[0].authorizedGroups = new int[] { 0 }.ToList();
                        }
                        // loginStatus.PermissionSet = xDoc.GetElementsByTagName("permissions")[0].InnerText;
                        if (loginStatus.appGroupPermissions.Count == 0 || loginStatus.appGroupPermissions[0].groupPermissionSets.Count == 0)
                        {
                            Exception e = new Exception(@"User not authorized for this application.");
                            e.Data["noAuth"] = true;
                            throw e;
                        }

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
                            string msg = "AuthenticateUser XML conversion exception: " + excptn.Message;
                            Debug.WriteLine(msg);
                            loginStatus.ErrorMessage = "Invalid username or password: " + excptn.Message;
                        }
                    }
                    Session["UserIsAdm"] = loginStatus.IsAdmin.ToString();
                }
            }

            return loginStatus;
        }

        /// <summary>
        /// Packs the Vista database to correct fragmenting and improve integrity.  All connections must be closed for this process to work.
        /// </summary>
        private void PackingFunction()
        {
            Debug.WriteLine("* * * Account Venture PackingFunction");
            try
            {
                using (DbConnection connection = Data.Concrete.UasAdo.GetUasAdoConnection())
                {
                    IVistaDBDDA DDAObj = VistaDB.DDA.VistaDBEngine.Connections.OpenDDA();

                    // Rebuild the Vista DB path.  IVistaDBDDA doesn't like the | character in the web config set up.
                    string localAppData = Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%");
                    string ventureVersion = Session["venture_version"].ToString();
                    string databaseName = connection.DataSource.Substring(connection.DataSource.LastIndexOf(@"\") + 1);
                    string connectionPath = (connection.DataSource.Contains("|")) ? Path.Combine(localAppData, @"AAIDD\Venture", ventureVersion, databaseName)
                        : connection.DataSource;

                    Debug.WriteLine("* * * Account Venture PackingFunction database path: " + connectionPath);

                    DDAObj.PackDatabase(connectionPath, "P@ssword1", true, new OperationCallbackDelegate(this.OnPackInfo));
                }
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("* * * Account Venture PackingFunction error: " + excptn.Message);
            }
        }

        private void OnPackInfo(VistaDB.DDA.IVistaDBOperationCallbackStatus operationDelegate)
        {
            if (operationDelegate.Progress < 0) return;
            int ProgressPercent = operationDelegate.Progress;
            string ProgressText = String.Concat("Performing ", operationDelegate.Operation.ToString(), " on ", operationDelegate.ObjectName.ToString(), ":");
        }
        
        [HttpGet]
        [AllowAnonymous]
        public ActionResult VentureLogout()
        {
            Session.RemoveAll();
            Session.Abandon();
            
            return RedirectToAction("Index", "Account");
        }

        
    }
}
