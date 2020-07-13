using Assmnts.Filters;
using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Abstract;
using Data.Concrete;

using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using System.Web;
using System.Web.Configuration;
using System.Web.Management;
using System.Web.Mvc;
using System.Web.Security;
using Assmnts.UasServiceRef;
using UAS.Business;
using UAS.DataDTO;

using WebMatrix.WebData;
using WebService;


namespace Assmnts.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public partial class AccountController : Controller
    {
        private IFormsRepository formsRepo;

        public AccountController(IFormsRepository fr)
        {
            // Initialized by Infrastructure.Ninject
            formsRepo = fr;
        }

        /// <summary>
        /// Displays the login screen for SIS or Venture
        /// </summary>
        /// <returns>View</returns>
        [AllowAnonymous]
        public ActionResult Index()
        {
            Debug.WriteLine(" * * * Assmnts Connection String: " + formsRepo.GetContext().Database.Connection.ConnectionString);

            // Default to the SIS-A / UAS login screen
            LoginInfo loginInfo = new UAS.DataDTO.LoginInfo();
            CookieData cookieData = UAS.Business.HttpFunctions.GetUserCookie();
            loginInfo.RememberMe = false;
            if (cookieData.LoginID != null)
            {
                loginInfo.LoginID = cookieData.LoginID;
                Debug.WriteLine("cookieData.LoginID: " + cookieData.LoginID);
                loginInfo.RememberMe = true;
            }

            // Get the Assembly File Version
            // *** NOTE: this must be the same as the Venture.exe File Assembly Version for the same VistaDB to be found
            // ***      It is used to determine the directory of the VistaDB database file.
            System.Reflection.Assembly CurrentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(CurrentAssembly.Location);
            Debug.WriteLine("* * * AccountController Assembly File Version: " + fileVersionInfo.FileVersion);
            ViewBag.Version = fileVersionInfo.FileVersion;

            // -- Test venture log in by uncommenting below, and it loads the venture login view
            //return View("loginVenture"); 

            // Check if VentureMode
            SessionHelper.IsVentureMode = false;
            string strVentureMode = ConfigurationManager.AppSettings["VentureMode"];
            if (!String.IsNullOrEmpty(strVentureMode))
            {
                SessionHelper.IsVentureMode = Convert.ToBoolean(strVentureMode);
                if (SessionHelper.IsVentureMode)
                {
                    SetupVentureMode(fileVersionInfo.FileVersion);
                    SessionHelper.Write("venture_version", fileVersionInfo.FileVersion);
                }
            }

            // Test for SingleSignOn mode
            string strSingleSignOnMode = ConfigurationManager.AppSettings["SingleSignOnMode"];
            if (!String.IsNullOrEmpty(strSingleSignOnMode))
            {
                bool SsoMode = Convert.ToBoolean(strSingleSignOnMode);
                if (SsoMode)
                    return RedirectToAction("SsoLogout", "Account");
            }

            string loginView = (SessionHelper.IsVentureMode) ? "loginVenture" : "loginSIS";
            return View(loginView);
            
            // The return below can be used to facilitate testing and bypass the security system
            // return RedirectToAction("Index", "Results", null);
        }

        //
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }


        [AllowAnonymous]
        public bool IsLoggedIn(string returnUrl)
        {
            return SessionHelper.IsUserLoggedIn;
        }
        // POST: /Account/AcceptPost
        [HttpPost]
        [AllowAnonymous]
        public ActionResult AcceptPost(string UserID, string Password, string SisFunction, string sisId = null)
        {
            try
            {
                LoginInfo loginInfo = new LoginInfo();

                loginInfo.LoginID = UserID;
                loginInfo.Password = Password;

                LoginStatus loginStatus = null;
                if (SessionHelper.IsVentureMode)
                {
                    loginStatus = LoginVentureLogic(loginInfo);
                    Debug.WriteLine("* * * AccountController LoginUAS  next * * *");
                }
                else
                {
                    Debug.WriteLine("* * * AccountController LoginUAS VerifyUser WebService next * * *");
                    loginStatus = UAS_Business_Functions.VerifyUser(loginInfo);
                }

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

                //SessionHelper.Groups = 

                // Session["UserEnvironment"] = userEnv;
                // Session["UserFirstLast"] = userName;

                if (loginInfo.IsLoggedIn)
                {
                    SessionHelper.SessionTotalTimeoutMinutes = Business.Timeout.GetTotalTimeoutMinutes(SessionHelper.LoginStatus.EnterpriseID);

                    if (SisFunction == "Search")
                        return RedirectToAction("Index", "Search");

                    if (SisFunction == "Assessment")
                    {
                        int formResultId = Convert.ToInt32(sisId);
                        def_FormResults fr = formsRepo.GetFormResultById(formResultId);
                        int formId = fr.formId;
                        // get the sectionId of the first section of the first part based on the formId
                        def_Forms frm = formsRepo.GetFormById(formId);
                        def_Parts prt = formsRepo.GetFormParts(frm)[0];
                        int partId = prt.partId;
                        SessionHelper.SessionForm = new SessionForm();
                        SessionHelper.SessionForm.formResultId = formResultId;
                        SessionHelper.SessionForm.formId = formId;
                        SessionHelper.SessionForm.partId = partId;
                        Session["part"] = partId;
                        Session["form"] = formId;
                        Session["formResult"] = formResultId;
                        def_Sections sct = formsRepo.GetSectionsInPart(prt)[0];

                        return RedirectToAction("Template", "Results", new { sectionId = sct.sectionId.ToString(), partId = partId.ToString()});
                    }
                }

                SessionHelper.IsUserLoggedIn = false;
                ViewBag.ErrorMessage = loginStatus.ErrorMessage;
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("LoginUAS exception: " + xcptn.Message);
                ViewBag.ErrorMessage = xcptn.Message;
            }

            return View("loginSIS");

        }
        
        
        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid && WebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
            {
                return RedirectToLocal(returnUrl);
            }

            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "The user name or password provided is incorrect.");
            return View(model);
        }



        //
        // GET: /Account/LoginUAS
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult LoginUAS(FormCollection frmCollection)
        {
            try
            {
                LoginInfo loginInfo = new LoginInfo();
                loginInfo.RememberMe = false;

                CookieData cookieData = HttpFunctions.GetUserCookie();
                if (cookieData.LoginID != null)
                {
                    loginInfo.LoginID = cookieData.LoginID;
                    loginInfo.RememberMe = true;
                }

                // * * * Need to use Cookie data if available.
                loginInfo.LoginID = frmCollection["userName"];
                loginInfo.Password = frmCollection["pwrd"];
                loginInfo.SessionData = Session.SessionID;

                LoginStatus loginStatus = null;
                if (SessionHelper.IsVentureMode)
                {
                    loginStatus = LoginVentureLogic(loginInfo);
                    Debug.WriteLine("* * * AccountController LoginUAS  next * * *");
                }
                else
                {
                    AuthenticationClient authClient = new AuthenticationClient();
                    var webUser = WSBusiness.GetUserByUserName(loginInfo.LoginID);

                    var lockoutDuration = authClient.GetEntAppConfigByEntAppAndEnum(webUser.EnterpriseID, 0, "Lockout_Duration");
                    if (lockoutDuration == null)
                    {
                        lockoutDuration = authClient.GetEntAppConfigByEntAppAndEnum(0, 0, "Lockout_Duration");
                    }
                    var lockoutTime = int.Parse(lockoutDuration.ConfigValue);

                    //get last login time
                    DateTime? lastLogin = WSBusiness.GetLastLoginTime(loginInfo.LoginID);
                    var duration = lastLogin.Value.AddMinutes(lockoutTime);
                   
                    
                    // the lockout duration has passed
                    if (DateTime.Now >= duration)
                    {
                        // reset login attempts
                        WSBusiness.ResetLoginAttempts(webUser.UserID);
                        WSBusiness.UpdateSentLockoutEmail(webUser.UserID, false);
                    }

                    Debug.WriteLine("* * * AccountController LoginUAS VerifyUser WebService next * * *");
                    loginStatus = UAS_Business_Functions.VerifyUser(loginInfo);
                }

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
                }


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

                //SessionHelper.Groups = 

                // Session["UserEnvironment"] = userEnv;
                // Session["UserFirstLast"] = userName;

                if (loginInfo.IsLoggedIn)
                {

                    Dictionary<int, string> forms = Business.Forms.GetFormsDictionaryThrow(formsRepo);

                    if (forms == null || forms.Count == 0)
                    {
                        Debug.WriteLine("Exception: TemplateAssmntNavMenu.cs: AddFormsDictionary: No forms configured for use.");
                        throw new Exception("You are not configured to use any SIS forms.");
                    }

                    SessionHelper.SessionTotalTimeoutMinutes = Business.Timeout.GetTotalTimeoutMinutes(SessionHelper.LoginStatus.EnterpriseID);

                    SessionHelper.UasAdminUrl = ConfigurationManager.AppSettings["UASAdminURL"] + "/Site/AdmLogin";
                    
                    
                    if (NeedChangePassword(SessionHelper.LoginStatus.UserID))
                    {
                        return RedirectToAction("PrefPass", "Search");
                    }


                    if (!string.IsNullOrWhiteSpace(frmCollection["recaptcha_response_field"]))
                    {
                        if (ValidCaptcha(frmCollection))
                        {
                            return RedirectToAction("Index", "Search");
                        }
                        else
                        {
                            ViewBag.ShowCaptcha = true;
                            ViewBag.ErrorMessage = "Invalid captcha response";
                            return View("loginSIS");
                        }
                    }

                    //return RedirectToAction("Index", "Home");
                    return RedirectToAction("Index", "Search");
                }

                SessionHelper.IsUserLoggedIn = false;

                //oliver 4-10-15 error messages should be less informative to improve security (Bug 12514)
                ViewBag.ErrorMessage = "Invalid username and/or password";//loginStatus.ErrorMessage;

                // check if captcha is needed
                var uasUser = WSBusiness.GetUserByUserName(loginInfo.LoginID);
                AuthenticationClient webClient = new AuthenticationClient();
                var captchaConfig = webClient.GetEntAppConfigByEntAppAndEnum(loginStatus.EnterpriseID, 0, "RECAPTCHA");
                if (captchaConfig == null)
                {
                    captchaConfig = webClient.GetEntAppConfigByEntAppAndEnum(0, 0, "RECAPTCHA");
                }
                var failedLoginConfig = webClient.GetEntAppConfigByEntAppAndEnum(loginStatus.EnterpriseID, 0,
                    "Failed_Logins");
                if (failedLoginConfig == null)
                {
                    failedLoginConfig = webClient.GetEntAppConfigByEntAppAndEnum(0, 0,"Failed_Logins");
                }
                var failedLoginAttempts = int.Parse(failedLoginConfig.ConfigValue);
                if (uasUser.LoginAttempts >= failedLoginAttempts)
                {
                    // send email and lock out the account
                    ViewBag.ErrorMessage = "You have reached the limit on failed attempts.  Please contact your supervisor.";
                    var userEmail = WSBusiness.GetUserEmailByUserName(uasUser.UserName);
                    if (userEmail != null && !string.IsNullOrWhiteSpace(userEmail.EmailAddress) && uasUser.SentLockoutEmail == false)
                    {
                        AJBoggs.Sis.Domain.Assessments emailer = new AJBoggs.Sis.Domain.Assessments();
                        emailer.SendEmail(userEmail.EmailAddress, ConfigurationManager.AppSettings["AcctLockoutEmail"].ToString(),
                            "Someone has made too many consecutive failed login attempts with your www.sis-online.org account.  As a result, the account has been deactivated.  If you did not make these failed login attempts, please notify your manager.",
                            "SIS Account Locked Due to Excessive Login Failures", false);
                        // update sent lockout email
                        WSBusiness.UpdateSentLockoutEmail(uasUser.UserID, true);
                    }
                }
                else
                {
                    var displayCaptchaCount = int.Parse(captchaConfig.ConfigValue);
                    if (uasUser.LoginAttempts >= displayCaptchaCount)
                    {
                        ViewBag.ShowCaptcha = true;
                    }
                }
            }
            catch(Exception xcptn)
            {
                Debug.WriteLine("LoginUAS exception: " + xcptn.Message);
                ViewBag.ErrorMessage = xcptn.Message;
            }

            return View("loginSIS");

        }

        //private bool SendEmail(string toAddress, string mailBody, string mailSubject, bool isHtml)
        //{
        //    bool status = true;
        //    string smtpUser = ConfigurationManager.AppSettings["SmtpUser"].ToString();
        //    string smtpPass = ConfigurationManager.AppSettings["SmtpPass"].ToString();
        //    string smtpHost = ConfigurationManager.AppSettings["AcctHost"].ToString();
        //    int smtpPort = Convert.ToInt32(ConfigurationManager.AppSettings["SmtpPort"]);
        //    string fromAddress = ConfigurationManager.AppSettings["AcctLockoutEmail"].ToString();


        //    try
        //    {
        //        SmtpClient mySmtpClient = new SmtpClient(smtpHost);

        //        // set smtp-client with basicAuthentication
        //        mySmtpClient.UseDefaultCredentials = false;
        //        System.Net.NetworkCredential basicAuthenticationInfo = new System.Net.NetworkCredential(smtpUser, smtpPass);
        //        mySmtpClient.Credentials = basicAuthenticationInfo;
        //        mySmtpClient.Port = smtpPort;

        //        // add from,to mailaddresses
        //        MailAddress @from = new MailAddress(fromAddress);
        //        MailAddress recepient = new MailAddress(toAddress);
        //        MailMessage myMail = new System.Net.Mail.MailMessage(@from, recepient);

        //        // set subject and encoding
        //        myMail.Subject = mailSubject;
        //        myMail.SubjectEncoding = System.Text.Encoding.UTF8;

        //        // set body-message and encoding
        //        myMail.Body = mailBody;
        //        myMail.BodyEncoding = System.Text.Encoding.UTF8;
        //        // text or html
        //        myMail.IsBodyHtml = isHtml;

        //        mySmtpClient.Send(myMail);

        //    }
        //    catch (SmtpException ex)
        //    {
        //        //WriteLog(String.Format("SMTP Exception Occured: {0}", ex.Message), TAP.DAL.Enum.LogLevel_Enum.ERROR);
        //        status = false;
        //    }
        //    catch (Exception ex)
        //    {
        //        //WriteLog(String.Format("Exception Occured: {0}", ex.Message), TAP.DAL.Enum.LogLevel_Enum.ERROR);
        //        status = false;
        //    }

        //    return status;
        //}

        private bool ValidCaptcha(FormCollection frmCollection)
        {
            if (!string.IsNullOrEmpty(frmCollection["recaptcha_response_field"]))
            {
                string PRIVATE_KEY = string.Empty;
                string CAPTCHA_URL = string.Empty;

                if ((System.Configuration.ConfigurationManager.AppSettings["RecaptchaPrivateKey"] != null))
                {
                    PRIVATE_KEY = System.Configuration.ConfigurationManager.AppSettings["RecaptchaPrivateKey"].ToString();
                }

                if ((System.Configuration.ConfigurationManager.AppSettings["RecpatchaVerifyUrl"] != null))
                {
                    CAPTCHA_URL = System.Configuration.ConfigurationManager.AppSettings["RecpatchaVerifyUrl"].ToString();
                }

                var request = HttpWebRequest.Create(CAPTCHA_URL);

                string body = String.Format("privatekey={0}&remoteip={1}&challenge={2}&response={3}", PRIVATE_KEY,
                    Request.UserHostAddress == "::1" ? "127.0.0.1" : Request.UserHostAddress,
                    frmCollection["recaptcha_challenge_field"], frmCollection["recaptcha_response_field"]);

                dynamic bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = bodyBytes.Length;

                System.IO.Stream s = default(System.IO.Stream);
                s = request.GetRequestStream();
                s.Write(bodyBytes, 0, bodyBytes.Length);
                s.Close();

                HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
                System.IO.Stream s2 = resp.GetResponseStream();
                StreamReader sr = new StreamReader(s2);
                string respString = sr.ReadToEnd();
                sr.Close();

                string[] respParams = respString.Split(new char[] { (char)10 });
                if ((bool.Parse(respParams[0])))
                {
                   return true;
                }
            }
            return false;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult LogoutUAS()
        {
            bool isUASLogout = false;
            try
            {
                Debug.WriteLine("* Account.LogoutUAS *");

                if (SessionHelper.IsUserLoggedIn)
                {
                    ViewBag.ErrorMessage = "You have successfully logged off.";
                }

                if (SessionHelper.IsVentureMode == true)
                {
                    return VentureLogout();
                }
                
                string result = String.Empty;
                if (SessionHelper.LoginInfo != null)
                {
                    result = UAS_Business_Functions.UserLogout(SessionHelper.LoginInfo);
                }
                Debug.WriteLine("     Account.LogoutUAS   result: " + result);

                isUASLogout = SessionHelper.IsUASLogin;
                SessionHelper.IsUserLoggedIn = false;
                SessionHelper.LoginInfo = null;
                SessionHelper.LoginStatus = null;

                FormsAuthentication.SignOut();
                Session.Clear();
                Session.RemoveAll();
                Session.Abandon();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LogoutUAS error: " + ex.Message);
            }
            // Session["UserEnvironment"] = null;
            // Session["LoginStatus"] = null;

            // loginInfo.Message = "You successfully logged off.";
            // ubf.UserLogout(loginInfo);


            // model.Message = "You successfully logged off.<br /><br />";
            // return View("LogoutCRS", model);
            //Response.AddHeader("REFRESH", "5;URL=LogoutUAS");

            if (isUASLogout)
            {
                string url = WebConfigurationManager.AppSettings["UASAdminURL"] + "Account/LogoutSIS";
                return new RedirectResult(url);
            }
                
            return View("loginSIS");

        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            ForgotPasswordModel frgtPsswd = new ForgotPasswordModel();

                  
            return PartialView("_ForgotPassword", frgtPsswd);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult VentureForgotPassword()
        {
            string forgot = SessionHelper.Read<string>("JustChanged");
            
            // Just reset password
            if (SessionHelper.Read<string>("JustChanged") == "true")
            {
                SessionHelper.Write("VentureForgot", "false");
            }
            else
            {
                SessionHelper.Write("VentureForgot", "true");
            }
            return View("loginSIS");
        }
        
        [HttpPost]
        [AllowAnonymous]
        public string CheckForUser(string userNameOrEmail)
        {

            using (var context = DataContext.getUasDbContext())
            {

                uas_User result;
                try
                {
                    result = context.uas_User.Where(u => u.LoginID == userNameOrEmail && u.StatusFlag == "A").Select(u => u).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(" Account.CheckForUser Exception: " + ex.Message);
                    return "false";
                }

                if (result == null)
                {
                    List<int> emailUsersSearchList;
                    try
                    {
                        emailUsersSearchList = context.uas_UserEmail.Where(e => e.EmailAddress == userNameOrEmail).Select(e => e.UserID).ToList();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(" Account.CheckForUser Exception: " + ex.Message);
                        return "false";
                    }

                    foreach (var userId in emailUsersSearchList)
                    {

                        try
                        {
                            result = context.uas_User.Where(u => u.UserID == userId && u.StatusFlag == "A").Select(u => u).FirstOrDefault();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(" Account.CheckForUser Exception: " + ex.Message);
                            return "false";
                        }

                        if (result != null)
                        {
                            break;
                        }
                    }
                }

                // Return false if no User found.
                string usr = (result == null) ? "false" : getUserQuestions(result.UserID);
                return usr;

            }
        }

        [HttpPost]
        [AllowAnonymous]
        public string CheckUserAnswers(string results)
        {
           List<object> answers = fastJSON.JSON.ToObject<List<object>>(results);

           Dictionary<string, string> answersDict = new Dictionary<string, string>();
                 
           foreach (object o in answers)
           {
               Dictionary<string, object> d = (Dictionary<string, object>)o;
               answersDict.Add(d["name"].ToString(), d["value"].ToString());
           }


           using (var context = DataContext.getUasDbContext())
           {
               int userId = Int32.Parse(answersDict["user"]);
               
               List<string> aList = context.uas_SecretQuestionAnswer.Where(a => a.UserID == userId)
                   .OrderBy(a => a.uas_SecretQuestion.SortOrder)
                   .Select(a => a.Answer).ToList();

               int i = 0;
               foreach (string answer in aList)
               {
                   if (answersDict["q" + i.ToString()] == answer)
                   {
                       i++;
                       continue;
                   }
                   return "false";
               }

               return "true";
                     
           }
           

        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult displaySetPassword() {
            return PartialView("_SetForgotPassword");
        }
        
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public string SetNewPassword(int userId, string newPassword) {
            using (var context = DataContext.getUasDbContext())
            {
                uas_User user = context.uas_User.Where(u => u.UserID == userId).Select(u => u).FirstOrDefault();

                user.Password = UAS.Business.UtilityFunction.EncryptPassword(newPassword);
                user.ModifiedBy = userId;
                user.ModifiedDate = DateTime.Now;

                context.Entry(user).State = System.Data.Entity.EntityState.Modified;

                try
                {
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Debug.Write(ex.Message);
                    return "false";
                }
            }

            SessionHelper.Write("JustChanged", "true");

            return "true";
        }

        private string getUserQuestions(int userId)
        {
            try
            {
                using (var context = DataContext.getUasDbContext())
                {
                    List<int> qIds = context.uas_SecretQuestionAnswer.Where(a => a.UserID == userId).Select(a => a.SecretQuestionID).ToList();

                    List<int> allQIds = context.uas_SecretQuestion.Select(q => q.SecretQuestionID).ToList();

                    List<int> missingAnswersQIds = allQIds.Except(qIds).ToList();

                    List<int> presentQIds = allQIds.Except(missingAnswersQIds).ToList();

                    if (presentQIds.Count < 2)
                    {
                        throw new Exception("Missing answer(s) to question(s).");
                    }
                    
                    string questions = userId + "::";


                    List<uas_SecretQuestion> qList = new List<uas_SecretQuestion>();
                    foreach (int qId in qIds)
                    {
                        uas_SecretQuestion question = context.uas_SecretQuestion.Where(q => q.SecretQuestionID == qId).Select(q => q).FirstOrDefault();
                        if (question != null)
                        {
                            qList.Add(question);
                        }
                    }

                    qList = qList.OrderBy(q => q.SortOrder).Select(q => q).ToList();

                    int i = 0;
                    foreach (uas_SecretQuestion q in qList)
                    {
                        i++;
                        questions += q.Question;

                        if (i != qList.Count())
                        {
                            questions += "\n";
                        }

                    }
                    return questions;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Get User Questions: " + ex.Message);
                return "qError";
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public void xferUAS()
        {
            Debug.WriteLine("Account xferUAS()");
            string baseAdminUrl = WebConfigurationManager.AppSettings["UASAdminURL"];
            string url = baseAdminUrl;// +"/Account/Login";

            // The secret key known by both parties
            string secretKey = SessionHelper.LoginStatus.UserKey;

            string currentUTCtime = DateTime.UtcNow.ToString();

            // The content to sign is the identity token (the username) appended with the timestamp and secret key
            string contentToSignString = HttpUtility.UrlEncode(SessionHelper.LoginInfo.LoginID) + currentUTCtime + secretKey;

            // Create the MD5 hasher and a UTF8Encoding class
            var hasher = new MD5CryptoServiceProvider();
            var encoder = new UTF8Encoding();

            // Create the hash
            byte[] contentToSignData = encoder.GetBytes(contentToSignString);
            byte[] signatureData = hasher.ComputeHash(contentToSignData);
            string signatureString = Convert.ToBase64String(signatureData);


            Response.Cookies["UserInfo"]["LoginId"] = HttpUtility.UrlEncode(SessionHelper.LoginInfo.LoginID);
            Response.Cookies["UserInfo"]["TimeStamp"] = currentUTCtime;
            Response.Cookies["UserInfo"]["Signature"] = signatureString;

            //Response.Cookies["UserInfo"].Domain = "uas.netto.com";//".sis-online.org";

            url += "site/admlogin";

            Debug.WriteLine("Account xferUAS url: " + url);

            Response.Redirect(url);

        }
        


        [AllowAnonymous]
        public ActionResult LoginAdmin()
        {
            return View();
        }
        //
        // POST: /Account/LogOff

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            WebSecurity.Logout();

            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/Register

        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                try
                {
                    WebSecurity.CreateUserAndAccount(model.UserName, model.Password);
                    WebSecurity.Login(model.UserName, model.Password);
                    return RedirectToAction("Index", "Home");
                }
                catch (MembershipCreateUserException e)
                {
                    ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/Disassociate

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Disassociate(string provider, string providerUserId)
        {
            string ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
            ManageMessageId? message = null;

            // Only disassociate the account if the currently logged in user is the owner
            if (ownerAccount == User.Identity.Name)
            {
                // Use a transaction to prevent the user from deleting their last login credential
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
                {
                    bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
                    if (hasLocalAccount || OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Count > 1)
                    {
                        OAuthWebSecurity.DeleteAccount(provider, providerUserId);
                        scope.Complete();
                        message = ManageMessageId.RemoveLoginSuccess;
                    }
                }
            }

            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage

        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : "";
            ViewBag.HasLocalPassword = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Manage(LocalPasswordModel model)
        {
            bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            ViewBag.HasLocalPassword = hasLocalAccount;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasLocalAccount)
            {
                if (ModelState.IsValid)
                {
                    // ChangePassword will throw an exception rather than return false in certain failure scenarios.
                    bool changePasswordSucceeded;
                    try
                    {
                        changePasswordSucceeded = WebSecurity.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword);
                    }
                    catch (Exception)
                    {
                        changePasswordSucceeded = false;
                    }

                    if (changePasswordSucceeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                    }
                }
            }
            else
            {
                // User does not have a local password so remove any validation errors caused by a missing
                // OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        WebSecurity.CreateAccount(User.Identity.Name, model.NewPassword);
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("", String.Format("Unable to create local account. An account with the name \"{0}\" may already exist.", User.Identity.Name));
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/ExternalLogin

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            return new ExternalLoginResult(provider, Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback

        [AllowAnonymous]
        public ActionResult ExternalLoginCallback(string returnUrl)
        {
            AuthenticationResult result = OAuthWebSecurity.VerifyAuthentication(Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
            if (!result.IsSuccessful)
            {
                return RedirectToAction("ExternalLoginFailure");
            }

            if (OAuthWebSecurity.Login(result.Provider, result.ProviderUserId, createPersistentCookie: false))
            {
                return RedirectToLocal(returnUrl);
            }

            if (User.Identity.IsAuthenticated)
            {
                // If the current user is logged in add the new account
                OAuthWebSecurity.CreateOrUpdateAccount(result.Provider, result.ProviderUserId, User.Identity.Name);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // User is new, ask for their desired membership name
                string loginData = OAuthWebSecurity.SerializeProviderUserId(result.Provider, result.ProviderUserId);
                ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(result.Provider).DisplayName;
                ViewBag.ReturnUrl = returnUrl;
                return View("ExternalLoginConfirmation", new RegisterExternalLoginModel { UserName = result.UserName, ExternalLoginData = loginData });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLoginConfirmation(RegisterExternalLoginModel model, string returnUrl)
        {
            string provider = null;
            string providerUserId = null;

            if (User.Identity.IsAuthenticated || !OAuthWebSecurity.TryDeserializeProviderUserId(model.ExternalLoginData, out provider, out providerUserId))
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                // Insert a new user into the database
                using (UsersContext db = new UsersContext())
                {
                    Assmnts.Models.UserProfile user = db.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());
                    // Check if user already exists
                    if (user == null)
                    {
                        // Insert name into the profile table
                        db.UserProfiles.Add(new Assmnts.Models.UserProfile { UserName = model.UserName });
                        db.SaveChanges();

                        OAuthWebSecurity.CreateOrUpdateAccount(provider, providerUserId, model.UserName);
                        OAuthWebSecurity.Login(provider, providerUserId, createPersistentCookie: false);

                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        ModelState.AddModelError("UserName", "User name already exists. Please enter a different user name.");
                    }
                }
            }

            ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(provider).DisplayName;
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // GET: /Account/ExternalLoginFailure

        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        [AllowAnonymous]
        [ChildActionOnly]
        public ActionResult ExternalLoginsList(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return PartialView("_ExternalLoginsListPartial", OAuthWebSecurity.RegisteredClientData);
        }

        [ChildActionOnly]
        public ActionResult RemoveExternalLogins()
        {
            ICollection<OAuthAccount> accounts = OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name);
            List<ExternalLogin> externalLogins = new List<ExternalLogin>();
            foreach (OAuthAccount account in accounts)
            {
                AuthenticationClientData clientData = OAuthWebSecurity.GetOAuthClientData(account.Provider);

                externalLogins.Add(new ExternalLogin
                {
                    Provider = account.Provider,
                    ProviderDisplayName = clientData.DisplayName,
                    ProviderUserId = account.ProviderUserId,
                });
            }

            ViewBag.ShowRemoveButton = externalLogins.Count > 1 || OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            return PartialView("_RemoveExternalLoginsPartial", externalLogins);
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult LoginFromUASAdmin()
        {
            string LoginId = Server.HtmlEncode(Request.Cookies["UserInfo"]["LoginId"]);
            string TimeStamp = Request.Cookies["UserInfo"]["TimeStamp"];
            string Signature = Request.Cookies["UserInfo"]["Signature"];


            LoginInfo loginInfo = new LoginInfo();


            loginInfo.RememberMe = false;


            loginInfo.LoginID = LoginId;
            loginInfo.Password = null;
            loginInfo.TimeStamp = TimeStamp;
            loginInfo.Signature = Signature;

            LoginStatus loginStatus = null;

            try
            {


                loginStatus = UAS_Business_Functions.VerifyUser(loginInfo);


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
                    SessionHelper.SessionTotalTimeoutMinutes = Business.Timeout.GetTotalTimeoutMinutes(SessionHelper.LoginStatus.EnterpriseID);
                    SessionHelper.UasAdminUrl = WebConfigurationManager.AppSettings["UASAdminURL"] + "/Site/Limited/limited_Users.aspx";
                    SessionHelper.IsUASLogin = true;
                }

                if (loginInfo.IsLoggedIn)
                {
                    return RedirectToAction("Index", "Home");
                    //return RedirectToAction("Index", "Search");
                }

                SessionHelper.IsUserLoggedIn = false;
                ViewBag.ErrorMessage = loginStatus.ErrorMessage;
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("LoginUAS exception: " + xcptn.Message);
                ViewBag.ErrorMessage = xcptn.Message;
            }

            return View("loginSIS");
        }

        #region Helpers
        private bool NeedChangePassword(int userId)
        {
            using (var context = DataContext.getUasDbContext())
            {
                bool change = context.uas_User.Where(u => u.UserID == userId).SingleOrDefault().ChangePassword;
                return change;
            }
        }
        
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }

        internal class ExternalLoginResult : ActionResult
        {
            public ExternalLoginResult(string provider, string returnUrl)
            {
                Provider = provider;
                ReturnUrl = returnUrl;
            }

            public string Provider { get; private set; }
            public string ReturnUrl { get; private set; }

            public override void ExecuteResult(ControllerContext context)
            {
                OAuthWebSecurity.RequestAuthentication(Provider, ReturnUrl);
            }
        }

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion
    }
}
