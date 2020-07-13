using System;
using System.Collections.Generic;
using System.Web;

using UAS.DataDTO;

using Assmnts.Models;
using UAS.Business;

namespace Assmnts.Infrastructure
{
    public static class SessionHelper
    {
		// Session variable constants
        public const string IS_USER_LOGGED_IN = "IsUserLoggedIn";
        public const string USER_ID = "UserId";
        public const string LOGIN_INFO = "LoginInfo";
        public const string LOGIN_STATUS = "LoginStatus";
        public const string IS_VENTURE_MODE = "IsVentureMode";
        public const string IS_UAS_LOGIN = "IsUASLogin";
        public const string SESSION_TOTAL_TIMEOUT_MINUTES = "SessionTotalTimeoutMinutes";
		public const string GROUPS = "Groups";        // comma-delimited string of GroupIDs
        public const string SESSION_FORM = "SessionForm";
        public const string UAS_ADMIN_URL = "UasAdminUrl";
        public const string PORTAL_SESSION = "PortalSession";
        public const string SECURE_EMAIL_URL = "SecureEmailUrl";
        public const string RESPONSE_VALUES = "ResponseValues";
        public const string USER_SECURITY_CONTEXT = "UserSecurityContext";

        // Variable Read/Write classes

        public static T Read<T>(string variable)
		{
			object value = HttpContext.Current.Session[variable];
			return (value == null) ? default(T) : ((T)value);

            /*
             * NOTE: default value for bool = false; int = 0;  strings and structs/classes = null
             */
		}
 
		public static void Write(string variable, object value)
		{
			HttpContext.Current.Session[variable] = value;
		}

        public static void DestroySession()
        {
            HttpContext.Current.Session.Clear();
            HttpContext.Current.Session.Abandon();
        }

        // Getter / Setters for each Session Variable

        public static bool IsUserLoggedIn
        {
            get  { return Read<bool>(IS_USER_LOGGED_IN); }
            set  { Write(IS_USER_LOGGED_IN, value);      }
        }
	 
		public static int UserId {
			get  { return Read<int>(USER_ID); }
			set  { Write(USER_ID, value); }
		}

        public static LoginInfo LoginInfo
        {
            get { return Read<LoginInfo>(LOGIN_INFO); }
            set { Write(LOGIN_INFO, value); }
        }

        public static LoginStatus LoginStatus
        {
            get { return Read<LoginStatus>(LOGIN_STATUS); }
            set { Write(LOGIN_STATUS, value); }
        }

        public static bool IsVentureMode
        {
            get { return Read<bool>(IS_VENTURE_MODE); }
            set { Write(IS_VENTURE_MODE, value); }
        }

        public static bool IsUASLogin
        {
            get { return Read<bool>(IS_UAS_LOGIN); }
            set { Write(IS_UAS_LOGIN, value); }
        }

        public static int SessionTotalTimeoutMinutes
        {
            get { return Read<int>(SESSION_TOTAL_TIMEOUT_MINUTES); }
            set { Write(SESSION_TOTAL_TIMEOUT_MINUTES, value); }
        }

		public static string Groups
		{
			get { return Read<string>(GROUPS); }
			set { Write(GROUPS, value); }
		}
        
        public static string UasAdminUrl
        {
            get { return Read<string>(UAS_ADMIN_URL); }
            set { Write(UAS_ADMIN_URL, value); }
        }

        public static string SecureEmailUrl
        {
            get { return Read<string>(SECURE_EMAIL_URL); }
            set { Write(SECURE_EMAIL_URL, value); }
        }

        public static string PortalSession
        {
            get { return Read<string>(PORTAL_SESSION); }
            set { Write(PORTAL_SESSION, value); }
        }

        public static UserSecurityContext UserSecurityContext {
            get { return Read<UserSecurityContext>(USER_SECURITY_CONTEXT); }
            set { Write(USER_SECURITY_CONTEXT, value); }
        }

        //Data associated with a particular window

        public static Dictionary<string, string> ResponseValues {
            get { return Read<Dictionary<string, string>>(RESPONSE_VALUES); }
            set { Write(RESPONSE_VALUES, value); }
        }
        
        public static SessionForm SessionForm {
            get { return Read<SessionForm>(SESSION_FORM); }
            set { Write(SESSION_FORM, value); }
        }
    }
}
