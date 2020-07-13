using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Assmnts.Models
{
    public class AdapPortal
    {
        public string Name { get; set; }
        public string RecertDate { get; set; }
        public string MemberId { get; set; }
        public string AppDate { get; set; }
        public string AppStatus { get; set; }
        public string UserId { get; set; }
        public int EnterpriseID { get; set; }

        public string errorMsg { get; set; }
    }
}