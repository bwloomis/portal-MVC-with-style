using System;
using System.Collections.Generic;

namespace UAS.DataDTO
{
    public class AppGroupPermissions
    {
        public int ApplicationID;
        public List<UAS.DataDTO.GroupPermissionSet> groupPermissionSets { get; set; }
        public List<int> authorizedGroups { get; set; }
    }

    public class LoginStatus
    {
        public bool IsLoggedIn { get; set; }
        public int UserID { get; set; }
        public int GroupID { get; set; }
        public int EnterpriseID { get; set; }
        public char Status { get; set; }
        public string UserKey { get; set; }
        public List<AppGroupPermissions> appGroupPermissions { get; set; }
        // public List<GroupPermissionSet> groupPermissionSets {get; set;}
        // public string PermissionSet { get; set; }
        public string EmailAddress { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool SecureDomain { get; set; }
        public bool IsAdmin { get; set; }
        public string ErrorMessage { get; set; }
    }
}