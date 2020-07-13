using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Assmnts.Models
{
    public class JurisdictionModel
    {
        public List<SelectListItem> Jurisdictions { get; set; }

        public String Jurisdiction { get; set; }

        public List<SelectListItem> Units { get; set; }

        public String Unit { get; set; }

        public int GroupId { get; set; }
    }
}