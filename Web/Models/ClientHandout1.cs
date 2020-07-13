using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using System.Web;

namespace Assmnts.Models
{
    public class ClientHandout1
    {
        
        public string MemberID { get; set; }
        
        public string Re_Enrollment_Date { get; set; }
                                            // we use properties "get and set" to access through the model //
        public string SVF_no_laterthan { get; set; }
        
        public string LoginId { get; set; }
     
        public DateTime DOB { get; set; }
       
        public string Enrollment_Worker { get; set; }
        
        public string Phone_Number { get; set; }
    }
}
