//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Assmnts
{
    using System;
    using System.Collections.Generic;
    
    public partial class uas_UserLoginActivity
    {
        public int UserLoginActivityID { get; set; }
        public int UserID { get; set; }
        public int ApplicationID { get; set; }
        public Nullable<int> EnterpriseID { get; set; }
        public Nullable<int> GroupID { get; set; }
        public Nullable<int> RoleID { get; set; }
        public string SessionID { get; set; }
        public System.DateTime LoginDate { get; set; }
        public Nullable<System.DateTime> LogoutDate { get; set; }
        public string ActiveData { get; set; }
        public string StatusFlag { get; set; }
        public Nullable<System.DateTime> SessionExpirationDate { get; set; }
        public string IpAddress { get; set; }
        public Nullable<int> ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public int CreatedBy { get; set; }
        public System.DateTime CreatedDate { get; set; }
    
        public virtual uas_Enterprise uas_Enterprise { get; set; }
        public virtual uas_Group uas_Group { get; set; }
        public virtual uas_Role uas_Role { get; set; }
        public virtual uas_User uas_User { get; set; }
        public virtual uas_User uas_User1 { get; set; }
        public virtual uas_User uas_User2 { get; set; }
    }
}
