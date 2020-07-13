using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Assmnts.Models
{
    public class EnrollmentSiteModel
    {
        public List<SelectListItem> Units { get; set; }

        public int GroupId { get; set; }

        [Required]
        public String Unit { get; set; }

        [Display(Name = "Enrollment Site")]
        [Required]
        public String EnrollmentSite { get; set; }

        [Display(Name = "Address 1")]
        public String Address1 { get; set; }

        [Display(Name = "Address 2")]
        public String Address2 { get; set; }

        public String City { get; set; }
        public String State { get; set; }
        
        [Display(Name = "Zip Code")]
        public String ZipCode { get; set; }

        [Display(Name = "Site Number")]
        public String SiteNumber { get; set; }
        public String Restrictions { get; set; }

        [Display(Name = "Contact Phone")]
        public String ContactPhone { get; set; }

        [Display(Name = "Contact Email")]
        public String ContactEmail { get; set; }

        [Display(Name = "Contact Person")]
        public String ContactPerson { get; set; }

        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }
    }
}