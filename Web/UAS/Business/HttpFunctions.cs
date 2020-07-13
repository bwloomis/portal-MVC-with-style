using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace UAS.Business
{
    public static class HttpFunctions
    {
        #region - constants
        private const string HEADCOOKIE = "SavedCookie";
        private const string SESSION = "SavedSession";
        // private const int COOKIEEXPIRE = 60;
        // private const string ENCKEY = "d05Xf6dR";
        // private const string TENCKEY = "d05Xf6dR24xvyj8b";
        // private const string TPSSKEY = "xc567hSrjb$^*[{:";
        #endregion - END constants


        public static DataDTO.SessionData GetSession()
        {
            DataDTO.SessionData SD = new DataDTO.SessionData();
            if (HttpContext.Current.Session["SavedSession"] != null)
            {
                SD = (DataDTO.SessionData)HttpContext.Current.Session[SESSION];
            }

            return SD;
        }

        public static bool SaveSession(DataDTO.SessionData sd)
        {
            HttpContext.Current.Session[SESSION] = sd;
            return true;
        }

        public static DataDTO.CookieData GetCookie()
        {
            DataDTO.CookieData CD = new DataDTO.CookieData();
            if (HttpContext.Current != null && HttpContext.Current.Request.Cookies[HEADCOOKIE] != null)
            {
                if (HttpContext.Current.Request.Cookies[HEADCOOKIE]["LoginID"] != null)
                {
                    CD.LoginID = HttpContext.Current.Server.HtmlEncode(HttpContext.Current.Request.Cookies[HEADCOOKIE]["LoginID"].ToString());
                }
            }

            return CD;
        }

        public static DataDTO.CookieData GetUserCookie()
        {
            DataDTO.CookieData CD = GetCookie();
            if (!string.IsNullOrEmpty(CD.LoginID))
            {
                CD.LoginID = UtilityFunction.DataDecrypt(CD.LoginID);
            }
            return CD;
        }

		public static string RemoveUserCookie(string loginid)
        {
            string result = "false";
            if (HttpContext.Current.Request.Cookies[HEADCOOKIE] != null)
            {
                if (HttpContext.Current.Request.Cookies[HEADCOOKIE]["LoginID"] != null)
                {
                    HttpCookie htc = new HttpCookie(HEADCOOKIE);
                    htc.Values["LoginID"] = String.Empty;
                    htc.Expires = DateTime.Now.AddDays(-1);
                    HttpContext.Current.Response.Cookies.Add(htc);
                }
            }
            return result;
        }

        public static string SaveUserCookie(string loginid)
        {
            string result = "false";
            if (!string.IsNullOrEmpty(loginid))
            {
                string x = UtilityFunction.DataEncrypt(loginid);
                result = UtilityFunction.DataDecrypt(x);
                HttpCookie htc = new HttpCookie(HEADCOOKIE);
                htc.Values.Add("LoginID", x);
                htc.Expires = DateTime.MaxValue;    //DateTime.Now.AddDays(COOKIEEXPIRE);
                HttpContext.Current.Response.Cookies.Add(htc);
            }
            return result;
        }

        public static string GetUserIdentity()
        {
            System.Security.Principal.IIdentity pip = HttpContext.Current.User.Identity;
            return pip.Name;
        }

        public static DataDTO.SessionData GetUserSession()
        {
            DataDTO.SessionData sd = GetSession();
            sd.LoginID = UtilityFunction.DataDecrypt(sd.LoginID);
            return sd;
        }

        public static string SaveUserSession(DataDTO.SessionData sessdata)
        {
            string result = "false";
            result = SaveSession(sessdata).ToString();
            return result;
        }

        public static bool UserIsAdmin()
        {
            bool Adm = false;
            string s = string.Empty;
            if (HttpContext.Current.Session["UserIsAdm"] != null)
            {
                s = HttpContext.Current.Session["UserIsAdm"].ToString();
            }
            bool.TryParse(s, out Adm);
            return Adm;
        }


    }
}
