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
    
    public partial class Recipient
    {
        public int ent_id { get; set; }
        public long Recipient_ContactID { get; set; }
        public Nullable<int> ClientID { get; set; }
        public string MedExtNum { get; set; }
        public string ID { get; set; }
        public string Lst4 { get; set; }
        public Nullable<System.DateTime> DOB { get; set; }
        public string Gender { get; set; }
        public Nullable<bool> HasGuardian { get; set; }
        public string OtherName { get; set; }
        public string NativeLang { get; set; }
        public Nullable<bool> InterpreterNeeded { get; set; }
        public Nullable<int> sub_id { get; set; }
        public string RecordType { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> Updated { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<int> RecipientStatusId { get; set; }
    
        public virtual Contact Contact { get; set; }
    }
}
