using System;

namespace Assmnts.Models
{
    public class ContactInfoModel
    {
        public int UserId { get; set; }
        public String Name { get; set; }
        public String HomePhone { get; set; }
        public String CellPhone { get; set; }

        public String ResidAddress1 { get; set; }
        public String ResidAddress2 { get; set; }
        public String ResidCity { get; set; }
        public String ResidState { get; set; }
        public String ResidZip { get; set; }
        public bool ResidMayContact { get; set; }

        public String MailAddress1 { get; set; }
        public String MailAddress2 { get; set; }
        public String MailCity { get; set; }
        public String MailState { get; set; }
        public String MailZip { get; set; }
        public bool MailMayContact { get; set; }

        public String Email { get; set; }
        public String CaseManager { get; set; }
        public String Clinic { get; set; }
        public String Team { get; set; }
        //public String Plan { get; set; }
        //public String Group { get; set; }
    }
}