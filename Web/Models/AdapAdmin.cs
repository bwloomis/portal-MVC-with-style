using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Assmnts.Models
{
    public class AdapAdmin
    {
  
        [DataType(DataType.Text)]
        [Display(Name = "Data Value")]
        public string dataValue { get; set; }
        
        [DataType(DataType.Text)]
        [Display(Name = "Display Order")]
        public int displayOrder { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Display Text")]
        public string displayText { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Status")]
        public bool statusFlag { get; set; }

        public int lookupDetailId { get; set; }

        public int lookupMasterId { get; set; }

        public int? enterpriseId { get; set; }

        public string ActiveUserName { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Team Colors")]
        public int? groupId { get; set; }

        public List<SelectListItem> groups { get; set; }

        public string fileName { get; set; }

        public string Message { get; set; }
        
    }
}