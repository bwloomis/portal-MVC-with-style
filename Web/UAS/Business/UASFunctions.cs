using Assmnts;
using Assmnts.Infrastructure;
using Assmnts.UasServiceRef;

using Data.Concrete;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Xml;

using UAS.DataDTO;

namespace UAS.Business
{
    public static class UAS_Business_Functions
    {
        #region - constants
        #endregion - END constants

        #region - private declarations
        //private MembershipUser usr;
        //private UAS_WebService.UASWeb WSF = new UAS_WebService.UASWeb();
        
        // private string primarydomain = Constants.CRSDOMAIN;
        #endregion - end private declarations

        #region - private functions
        private static DataDTO.LoginStatus AuthenticateUser(DataDTO.LoginInfo loginInfo)
        {
            Debug.WriteLine("Authenticate User: " + loginInfo.LoginID);

            DataDTO.LoginStatus loginStatus = new DataDTO.LoginStatus();

            //Check for domain in email address
            if (loginInfo.LoginID != null)
            {
                /*  RRB 7/16/15 Removed to allow login with Email address
                if (loginInfo.LoginID.Contains("@"))
                {
                    string before = loginInfo.LoginID.Substring(0, loginInfo.LoginID.IndexOf("@"));
                    string mid = loginInfo.LoginID.Substring(loginInfo.LoginID.IndexOf("@") + 1);
                    if (mid.Contains("."))
                    {
                        mid = mid.Substring(0, mid.IndexOf("."));
                    }
                    loginInfo.LoginID = before;
                    loginInfo.Domain = mid;
                }
                */
                //DataDTO.SessionData SD=new DataDTO.SessionData ();
                string passkey = UtilityFunction.EncryptPassKey(loginInfo.LoginID);
                string pwd = null;
                if (!string.IsNullOrEmpty(loginInfo.Password)) 
                {
                    pwd = UtilityFunction.EncryptPassword(loginInfo.Password);
                } 
                    
                // Debug.WriteLine("Authenticate User  webclient.AuthenticateUser");
                string validation = String.Empty;
                try
                {
                    AuthenticationClient webclient = new AuthenticationClient();
                    
                    validation = webclient.AuthenticateUser(passkey, Constants.PROGRAM, loginInfo.Domain,
                                                        loginInfo.LoginID, pwd, loginInfo.SessionData, loginInfo.TimeStamp, loginInfo.Signature, "");
                    webclient.Close();
                }
                catch (Exception excptn)
                {
                    validation = "<record><errormessage>" + excptn.Message + "</errormessage></record>";
                    Debug.WriteLine("Authenticate User  exception: " + excptn.Message);
                }
                Debug.WriteLine("Authenticate User  validation: " + validation);

                if (!string.IsNullOrEmpty(validation))
                {
                    XmlDocument xDoc = new XmlDocument();
                    try
                    {
                        xDoc.LoadXml(validation);
                        loginStatus.UserID = Convert.ToInt32(xDoc.GetElementsByTagName("userid")[0].InnerText);
                        loginStatus.EnterpriseID = Convert.ToInt32(xDoc.GetElementsByTagName("enterprise_id")[0].InnerText);

                        /*
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

                            loginStatus.appGroupPermissions[0].authorizedGroups = authGroups.ToList();
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
                            string msg = "AuthenticateUser XML conversion exception: " + excptn.Message;
                            Debug.WriteLine(msg);
                            loginStatus.ErrorMessage = "Invalid username or password.";
                        }
                        
                    }
                    if (HttpContext.Current != null)
                    {
                        HttpContext.Current.Session["UserIsAdm"] = loginStatus.IsAdmin.ToString();
                    }
                } else {
                    loginStatus.ErrorMessage = "Authentication error.";
                }
            }

            return loginStatus;
        }


        private static string PassEncrypt(string inline)
        {
            string outline = string.Empty;
            return outline;
        }

        #endregion - END private functions

        #region - public functions

        public static GroupUserAppPermissions GetGroupUserAppPermissionsByGroupUserId(int groupId, int userId)
        {
            AuthenticationClient webclient = new AuthenticationClient();
            Application app = webclient.GetApplicationByName(UAS.Business.Constants.PROGRAM);
            GroupUserAppPermissions guap = webclient.GetGroupUserAppPermissionsByGroupUserAppId(groupId, userId, app.ApplicationID );
            webclient.Close();
            return guap;
            
        }

        public static string UserLogout(DataDTO.LoginInfo uInfo)
        {
            //unset so that timeout isn't called multiple times
            HttpContext.Current.Session["TimeSet"] = null;

            AuthenticationClient webclient = new AuthenticationClient();
            string totaluser = uInfo.Domain + "\\" + uInfo.LoginID;
            string passkey = UtilityFunction.EncryptPassKey(totaluser);
            string validation = webclient.SignUserOut(passkey, totaluser);

            return validation;
        }


        public static bool IsUserLoggedIn(DataDTO.LoginInfo loginInfo)
        {
            bool loggedin = false;
            string domainuser = HttpFunctions.GetUserIdentity();
            if (!string.IsNullOrEmpty(domainuser))
            {
                if (loginInfo.LoginID.Equals(domainuser))
                    loginInfo.LoginID = domainuser;
            }

            if (!string.IsNullOrEmpty(loginInfo.LoginID))
            {
                string passkey = UtilityFunction.EncryptPassKey(loginInfo.LoginID);
                AuthenticationClient webclient = new AuthenticationClient();
                string validation = webclient.CheckUserLoginStatus(passkey, loginInfo.LoginID);
                webclient.Close();
                if (!string.IsNullOrEmpty(validation))
                {
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.LoadXml(validation);
                    loggedin = Convert.ToBoolean(xDoc.GetElementsByTagName("loggedin")[0].InnerText);
                }
            }

            return loggedin;

            //bool result = false;
            ////DataDTO.CookieData CD = GetCookie();
            //DataDTO.SessionData SD = GetSession();
            //if (HttpContext.Current.User.Identity.IsAuthenticated)
            //{
            //    usr = Membership.GetUser();
            //    //if (usr.UserName != null)
            //    //{
            //    //    result = WSF.IsValidLogin();
            //    //}
            //    //else if (SD.LoginID != null)
            //    //{
            //    //    result = WSF.IsValidLogin();
            //    //}

            //}
            //else
            //{
            //    if (SD.LoginID != null)
            //    {
            //        //result = WSF.IsValidLogin();
            //    }
            //}
            //return result;
        }



        public static DataDTO.LoginStatus VerifyUser(UAS.DataDTO.LoginInfo uInfo)
        {
            DataDTO.LoginStatus x = AuthenticateUser(uInfo);
            return x;
        }

        public static List<DataDTO.SecurityQuestionInfo> GetSecurityQuestions()
        {
            List<DataDTO.SecurityQuestionInfo> lsq = new List<DataDTO.SecurityQuestionInfo>();
            DataDTO.SecurityQuestionInfo sq = new DataDTO.SecurityQuestionInfo();
            sq.Question = "Question #1"; sq.QuestionID = 1; sq.Answer = "Answer #1";
            lsq.Add(sq);
            sq = new DataDTO.SecurityQuestionInfo();
            sq.Question = "Question #2"; sq.QuestionID = 1; sq.Answer = "Answer #2";
            lsq.Add(sq);

            return lsq;
        }


        /*
         * *** This code is not currently used in DEF3
         * 
         
        public List<DataDTO.GroupInfo> GetGroups()
        {
            List<DataDTO.GroupInfo> grps = new List<DataDTO.GroupInfo>();
            DataDTO.GroupInfo grp = new DataDTO.GroupInfo();

            grp.GroupID = 1; grp.GroupName = "CRS"; grp.GroupDescription = "Congressional Research Service"; grp.GroupTypeID = 1;
            grps.Add(grp);
            grp = new DataDTO.GroupInfo();
            grp.GroupID = 2; grp.GroupName = "HLC"; grp.GroupDescription = "House of Legislative Council"; grp.GroupTypeID = 1;
            grps.Add(grp);
            grp = new DataDTO.GroupInfo();
            grp.GroupID = 3; grp.GroupName = "CBO"; grp.GroupDescription = "Congressional Budget Office"; grp.GroupTypeID = 1;
            grps.Add(grp);
            grp = new DataDTO.GroupInfo();
            grp.GroupID = 4; grp.GroupName = "GAO"; grp.GroupDescription = "Government Accountability Office"; grp.GroupTypeID = 1;
            grps.Add(grp);
            grp = new DataDTO.GroupInfo();
            grp.GroupID = 5; grp.GroupName = "GPO"; grp.GroupDescription = "Government Printing Office"; grp.GroupTypeID = 1;
            grps.Add(grp);
            grp = new DataDTO.GroupInfo();
            grp.GroupID = 6; grp.GroupName = "SLC"; grp.GroupDescription = "Senatre Legislative Council"; grp.GroupTypeID = 1;
            grps.Add(grp);
            grp = new DataDTO.GroupInfo();
            grp.GroupID = 7; grp.GroupName = "Other"; grp.GroupDescription = "Other"; grp.GroupTypeID = 1;
            grps.Add(grp);

            return grps;
        }
        */
        /*
        public List<string> GetPrefixes()
        {
            List<string> pref = new List<string>();
            pref.Add("Mr"); pref.Add("Ms"); pref.Add("Mrs"); pref.Add("Miss"); pref.Add("Dr"); pref.Add("Master");
            pref.Add("Rev"); pref.Add("Prof"); pref.Add("Hon"); pref.Add("Pres"); pref.Add("Gov");
            return pref;
        }
        */

        public static DataDTO.LoginInfo DomainValidity(DataDTO.LoginInfo uInfo)
        {
            DataDTO.LoginInfo iInfo = new DataDTO.LoginInfo();
            Debug.WriteLine("UASFunctions - DomainValidity - SecureDomain:" + uInfo.SecureDomain.ToString());
            iInfo.CnfPassword = uInfo.CnfPassword;
            iInfo.CookieData = uInfo.CookieData;
            iInfo.IsLoggedIn = uInfo.IsLoggedIn;
            iInfo.LoginID = uInfo.LoginID;
            iInfo.Message = uInfo.Message;
            iInfo.Password = uInfo.Password;
            iInfo.RememberMe = uInfo.RememberMe;
            iInfo.SessionData = uInfo.SessionData;
            if (String.IsNullOrEmpty(iInfo.LoginID)) iInfo.LoginID = String.Empty;
            if (String.IsNullOrEmpty(iInfo.Domain)) iInfo.Domain = String.Empty;

            /*
            string username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            if (HttpContext.Current.Request.ServerVariables["REMOTE_USER"] != null)
                username = HttpContext.Current.Request.ServerVariables["REMOTE_USER"].ToString();
            if (HttpContext.Current.Request.ServerVariables["LOGON_USER"] != null)
                username = HttpContext.Current.Request.ServerVariables["LOGON_USER"].ToString();
            */

            string username = HttpContext.Current.Request.ServerVariables["LOGON_USER"].ToString();
            Debug.WriteLine("HttpContext.Current.Request.ServerVariables[LOGON_USER]: " + username);

            // string username = "(HC)" + HttpContext.User.Identity.Name;
            // Debug.WriteLine("HttpContext.User.Identity.Name: " + username);

            if (String.IsNullOrEmpty(username))
            {
                username = HttpContext.Current.Request.ServerVariables["REMOTE_USER"].ToString();
                Debug.WriteLine("Request.ServerVariables[REMOTE_USER]: " + username);
            }

            if (String.IsNullOrEmpty(username))
            {
                username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                Debug.WriteLine("System.Security.Principal.WindowsIdentity.GetCurrent().Name: " + username);
            }

            Debug.WriteLine("UASFunctions - DomainValidity - WindowsIdentity Name username:" + username);
            string domain = string.Empty;
            if (username.Contains(@"\"))
            {
                domain = username.Substring(0, username.IndexOf(@"\"));
                username = username.Substring(username.IndexOf(@"\") + 1);
            }

            //if (domain == primarydomain) { iInfo.ISCRS = true; }
            iInfo.Domain = domain;
            iInfo.LoginID = username;
            if (username != uInfo.LoginID)
            {
                iInfo.LoginID = uInfo.LoginID;
            }

            // string secureDomain = ReadWriteConfig("OVERWRITE_DOMAIN");
            string secureDomain = String.Empty;
            // Debug.WriteLine("secureDomain:" + secureDomain);
            int isSecureDomain = string.Compare(secureDomain, domain, true);
            // int isSecureDomain = webclient.SecureDomain(domain);
            Debug.WriteLine("secureDomain:" + secureDomain + "     domain:" + domain + "     isSecureDomain:" + isSecureDomain.ToString());
            uInfo.SecureDomain = (isSecureDomain == 0) ? true : false;
            if (uInfo.SecureDomain)
            {
                iInfo.ISCRS = true;
                iInfo.SecureDomain = true;
                iInfo.LoginID = username;
            }

            Debug.WriteLine("SecureDomain:" + uInfo.SecureDomain.ToString());
            Debug.WriteLine("UASFunctions - DomainValidity - username:" + username + "    domain: " + domain);

            return iInfo;
        }

        public static int ReadFormsTimeout()
        {
            int tout = 0;
            //in order to ensure this runs only once
            if (HttpContext.Current.Session["TimeSet"] != null)
            {
                int.TryParse(HttpContext.Current.Session["TimeSet"].ToString(), out tout);
                return tout;
            }

            // Timeout should only come from the UAS, not the TAP web.config.
            /*
            if (ConfigurationManager.AppSettings["TimeOut"] != null)
            {
                int.TryParse(ConfigurationManager.AppSettings["TimeOut"].ToString(), out tout);
            }
            */

            // *** RRB 1/7/2015 - this needs to be pulled from one of the the Ent Config tables in UAS.
            // string rval = ReadWriteConfig("TimeOut");
            string rval = "20";
            if (rval != String.Empty)
            {
                int.TryParse(rval, out tout);
            }
            HttpContext.Current.Session["TimeSet"] = tout.ToString();

            //403 Forbidden
            //System.Xml.XmlDocument x = new XmlDocument();
            //x.Load(url + "web.config");
            //System.Xml.XmlNode node = x.SelectSingleNode("/configuration/system.web/authentication/forms");
            //tout = int.Parse(node.Attributes["timeout"].Value, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            return tout;
        }

        // *** Don't use this !!!!   1/7/2015 RRB
        /*
        public string ReadWriteConfig(string inkey)
        {
            string readval = string.Empty;
            // Read Application changes from database (web.config)
            DataDTO.ApplicationInfo appInfo = new DataDTO.ApplicationInfo();
            string t = webclient.GetApplicationByName(Constants.PROGRAM);
            if (!string.IsNullOrEmpty(t))
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(t);
                appInfo.ApplicationID = Convert.ToInt32(xDoc.GetElementsByTagName("applicationid")[0].InnerText);
                appInfo.ApplicationName = xDoc.GetElementsByTagName("applicationname")[0].InnerText;
                // appInfo.EnterpriseID = Convert.ToInt32(xDoc.GetElementsByTagName("enterpriseid")[0].InnerText);
                appInfo.ModifiedDate = Convert.ToDateTime(xDoc.GetElementsByTagName("modifieddate")[0].InnerText);
                appInfo.Params = xDoc.GetElementsByTagName("params")[0].InnerText;
                appInfo.ParamsModified = Convert.ToBoolean(xDoc.GetElementsByTagName("paramsmodified")[0].InnerText);
                appInfo.StatusFlag = Convert.ToChar(xDoc.GetElementsByTagName("statusflag")[0].InnerText.Substring(0, 1));

                foreach (string s in appInfo.Params.Split(';'))
                {
                    string key = s.Substring(0, s.IndexOf("=")).Trim();
                    string val = s.Substring(s.IndexOf("=") + 1).Trim();
                    if (inkey == key) readval = val;
                }

                //Instead of updating web.config, just set values
                //                if ((bool)appInfo.ParamsModified)
                //{
                //    foreach (string s in appInfo.Params.Split(';'))
                //    {
                //        string key = s.Substring(0, s.IndexOf("=")).Trim();
                //        string val = s.Substring(s.IndexOf("=") + 1).Trim();
                //        // ---- Reading a key
                //        //String Version = ConfigurationManager.AppSettings["OVERWRITE_DOMAIN"];

                //        // ---- Writing a key
                //        ExeConfigurationFileMap FileMap = new ExeConfigurationFileMap();
                //        FileMap.ExeConfigFilename = HttpContext.Current.Server.MapPath(@"~\Web.config");


                //        Configuration Config = ConfigurationManager.OpenMappedExeConfiguration(FileMap, ConfigurationUserLevel.None);

                //        Config.AppSettings.Settings.Remove(key);
                //        Config.AppSettings.Settings.Add(key, val);
                //        Config.Save(ConfigurationSaveMode.Modified);
                //    }
                //}
            }
            return readval;
        }
        */

        public static List<int> GetChildGroups(int entId, int groupId)
        {
            AuthenticationClient webclient = new AuthenticationClient();
            List<int> childGroups = webclient.GetChildGroups(entId, groupId);
            webclient.Close();
            return childGroups;
        }


        /*
         * Tests to see if logged in user has access to a form result
         */
        public static bool hasAccess(def_FormResults fr)
        {
            if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Contains(0))
            {
                return true;
            }

            if (fr.assigned == SessionHelper.LoginStatus.UserID)
            {
                return true;
            }

            if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Contains(fr.GroupID.Value))
            {
                return true;
            }

            return false;
        }
        
        
        
        /*
         * Tests to see if logged in user has access and permission to perform a function on a form result
         * 
         */
        public static bool hasAccessAndPermission(def_FormResults fr, int action, string component)
        {
            if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Contains(0))
            {
                return hasPermission(action, component);
            }

            if (fr.assigned == SessionHelper.LoginStatus.UserID)
            {
                return hasPermission(action, component);
            }

            if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Contains(fr.GroupID.Value))
            {
                return hasPermission(action, component);
            }

            return false;
        }

        /*
         * Check if a user has a permission for a permission group at a given index.
         * 
         */

        public static bool hasPermission(int action, int index, string component)
        {
            string permString = SessionHelper.LoginStatus.appGroupPermissions[index].groupPermissionSets[0].PermissionSet;

            return hasPermission(permString, action, component);
        }
        
        /*
         * Check if a user has a permission for their first permission group.
         * 
         */

        public static bool hasPermission(int action, string component)
        {
            string permString = SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet;

            return hasPermission(permString, action, component);
        }
        
        /// <summary>
        /// Check if a user has a permission. Takes a permission string, and action to try to do,
        /// and a component to try to do it with. Ex: string="assmnts=YYYYY;" action = CREATE (0), component = ASSMNTS ("assmnts"), where action
        /// and component are passed constants from the PermissionConstants file.
        /// </summary>
        /// <param name="permString"></param>
        /// <param name="action"></param>
        /// <param name="component"></param>
        /// <returns></returns>

        public static bool hasPermission(string permString, int action, string component)
        {
            try
            {
                // permString will hold each of the permission strings, like "assmnts=YYYYY"
                List<string> permStrings = new List<string>();

                // Break up the permission string into parts separated by ";"
                while (permString != String.Empty)
                {
                    int indexOfSemicolon = -1;

                    indexOfSemicolon = permString.IndexOf(";");

                    string perm = permString.Substring(0, indexOfSemicolon);

                    permStrings.Add(perm);

                    permString = permString.Substring(indexOfSemicolon + 1);
                }

                // Find which of the permission strings we are using
                string permStringToUse = null;

                // Search for the component by the constant string representing it
                // as specified in the PermissionConstants file.
                foreach (string s in permStrings)
                {
                    if (s.IndexOf(component) != -1)
                    {
                        permStringToUse = s;
                        break;
                    }
                }

                // If the component is not found in the string, return null.
                if (permStringToUse == null)
                    return false;

                // Get string of permissions characters (after "=")
                int equalsIndex = permStringToUse.IndexOf("=");

                // This is the permission characters (ie, "YYYYY"), with positions as indicated by the constants
                // in the PermissionConstants file.
                string permChars = permStringToUse.Substring(equalsIndex + 1);

                // Check the permission character string at the corresponding action placement. If it is 'Y', return true.
                if (permChars.Length > action)
                {
                    if (permChars[action] == 'Y')
                    {
                        return true;
                    }
                }
                //else
                //{
                //    Debug.WriteLine("Permissions string for " + component + " is too short.");
                //}
                // Otherwise, return false.
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error getting permissions -- check permission string format.");
            }
            return false;
        }

        public static EntAppConfig GetEntAppConfig(string EnumCode, int EnterpriseID)
        {
            EntAppConfig config = null;
            
            try
            {
                AuthenticationClient webclient = new AuthenticationClient();
                config = webclient.GetEntAppConfigByEntAppAndEnum(EnterpriseID, Constants.APPLICATIONID, EnumCode);
                webclient.Close();
            } 
            catch (Exception ex)
            {
                Debug.WriteLine("Error getting EntAppConfig EnumCode: " + EnumCode + " EnterpriseID " + EnterpriseID);
                Debug.WriteLine("--Exception Message: " + ex.Message);
            }

            return config;

        }

        public static string GetEntAppConfigAdap(string EnumCode)
        {
            string configValue = null;
            int applicationID = 3;
            List<EntAppConfig> configs = null;

            try {
                AuthenticationClient webclient = new AuthenticationClient();
                configs = webclient.GetEntAppConfigsFiltered(SessionHelper.LoginStatus.EnterpriseID, applicationID, EnumCode).ToList();
                webclient.Close();
            }
             catch (Exception ex)
            {
                Debug.WriteLine("Error getting GetEntAppConfigAdap EnumCode: " + EnumCode + " EnterpriseID " + SessionHelper.LoginStatus.EnterpriseID);
                Debug.WriteLine("--Exception Message: " + ex.Message);
            }

            if ((configs != null) && (configs.Count > 0))
                configValue = configs[0].ConfigValue;

            return configValue;

        }

        public static List<EntAppConfig> GetEntAppConfigs(string EnumCode)
        {
            List<EntAppConfig> configs = null;

            try
            {
                AuthenticationClient webclient = new AuthenticationClient();
                configs = webclient.GetEntAppConfigsFiltered(SessionHelper.LoginStatus.EnterpriseID, Constants.APPLICATIONID, EnumCode).ToList();
                webclient.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error getting EntAppConfigs EnumCode: " + EnumCode + " EnterpriseID " + SessionHelper.LoginStatus.EnterpriseID);
                Debug.WriteLine("--Exception Message: " + ex.Message);
            }

            return configs;

        }


        public static List<string> GetForms()
        {
            bool ventureMode = SessionHelper.IsVentureMode;
            if (!ventureMode)
            {
                List<EntAppConfig> configs = GetEntAppConfigs("FORMS");

                if (configs != null)
                {
                    return configs.Select(c => c.ConfigValue).ToList();
                }
            }
            else
            {
                List<EntAppConfig> configs = GetEntAppConfigsVenture("FORMS");

                if (configs != null)
                {
                    return configs.Select(c => c.ConfigValue).ToList();
                }
            }

            return null;
        }

        private static List<EntAppConfig> GetEntAppConfigsVenture(string enumCode)
        {
            List<EntAppConfig> configs = new List<EntAppConfig>();

            using (var context = DataContext.getUasDbContext())
                try
                {

                    var results = (from c in context.uas_EntAppConfig
                                  where c.ApplicationID == Constants.APPLICATIONID 
                                  && c.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID 
                                  && c.EnumCode == enumCode && c.StatusFlag == "A"
                                  select c).ToList();

                    if (results != null)
                    {
                        foreach (var c in results)
                        {
                            EntAppConfig config = new EntAppConfig();
                            config.ApplicationID = c.ApplicationID;
                            config.baseTypeId = c.baseTypeId;
                            config.ConfigName = c.ConfigName;
                            config.ConfigValue = c.ConfigValue;
                            config.CreatedBy = c.CreatedBy;
                            config.CreatedDate = c.CreatedDate;
                            config.EntAppConfigId = c.EntAppConfigId;
                            config.EnterpriseID = c.EnterpriseID;
                            config.EnumCode = c.EnumCode;
                            config.ModifiedBy = c.ModifiedBy;
                            config.ModifiedDate = c.ModifiedDate;
                            config.StatusFlag = c.StatusFlag[0];

                            configs.Add(config);
                        }

                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetEntAppConfigsVenture exception: " + ex.Message);
                }

            return configs;

        }

        public static bool isGroupAuthorized(List<int> authGrps, int groupId)
        {
            foreach (int authGrpId in authGrps)
            {
                if ((authGrpId > -1) && (authGrpId == groupId))
                    return true;
            }
            return false;
        }

        public static bool isUserInEnterprise(int userId, int enterpriseId)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {

                    var result = (from u in context.uas_User
                                  where u.UserID == userId
                                  select u).FirstOrDefault();

                    if (result != null)
                    {
                        return (enterpriseId == result.EnterpriseID);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error checking user's enterprise: " + ex.Message);
                }

            }

            return false;

        }

        public static bool isGroupInEnterprise(int groupId, int enterpriseId)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {

                    var result = (from g in context.uas_Group
                                  where g.GroupID == groupId
                                  select g).FirstOrDefault();

                    if (result != null)
                    {
                        return (enterpriseId == result.EnterpriseID);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error checking group's enterprise: " + ex.Message);
                }

            }

            return false;
        }

        #endregion - END public functions
    }
}