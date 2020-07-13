using Assmnts.Infrastructure;
using Assmnts.UasServiceRef;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Configuration;

using UAS.Business;

namespace Assmnts.Models
{
    public class Home
    {
        public string LoginID { get; set; }
        public bool isAdmin { get; set; }
        public bool create { get; set; }
        public Dictionary<int, string> Forms { get; set; }
        public int timeout { get; set; } // in minutes
        public bool VentureMode { get; set; }
      
        public Home()
        {
            LoginID = SessionHelper.LoginInfo.LoginID;
            isAdmin = SessionHelper.LoginStatus.IsAdmin;

            timeout = SessionHelper.SessionTotalTimeoutMinutes;
            try
            {
                if (SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets.Count() > 0)
                {
                    create = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.CREATE, UAS.Business.PermissionConstants.ASSMNTS);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Missing permission: " + ex.Message);
            } 
        }

    }
}