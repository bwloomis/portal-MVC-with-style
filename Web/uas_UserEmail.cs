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
    
    public partial class uas_UserEmail
    {
        public int UserEmailID { get; set; }
        public string EmailType { get; set; }
        public string EmailAddress { get; set; }
        public string TempEmail { get; set; }
        public int UserID { get; set; }
        public Nullable<int> SortOrder { get; set; }
        public string StatusFlag { get; set; }
        public Nullable<int> ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public int CreatedBy { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public bool MayContact { get; set; }
    
        public virtual uas_User uas_User { get; set; }
        public virtual uas_User uas_User1 { get; set; }
        public virtual uas_User uas_User2 { get; set; }
    }
}
