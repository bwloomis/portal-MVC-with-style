using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Assmnts.Models
{
    public class ActiveEnrollmentSites1
    {
        
       // public int GroupID { get; set; }
        public string GroupNumber { get; set; }// we use properties "get and set" to access through the model //
        public string GroupDescription { get; set; }
        public string Address1 { get; set; }
        
        public string Address2 { get; set; }
        
        public string City { get; set; }
        
        public string ZipCode { get; set; }
        
        public string State { get; set; }
       
        public string County { get; set; }
        
        public string EmailAddress { get; set; }
        
        public string PhoneNumber { get; set; }
    }
}