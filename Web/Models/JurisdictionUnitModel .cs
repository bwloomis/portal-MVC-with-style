using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Assmnts.Models
{
    public class JurisdictionUnitModel
    {
        public String Jurisdiction { get; set; }

        public List<SelectListItem> Units { get; set; }

        public String Unit { get; set; }

        public int GroupId { get; set; }
    }
}