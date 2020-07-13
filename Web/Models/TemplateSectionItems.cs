using System.Collections.Generic;
using System.Web.Mvc;


namespace Assmnts.Models
{

    public class TemplateSectionItems
    {
        public int partId { get; set; }
        public int sectionId { get; set; }
        public bool itemChange { get; set; }
        public bool sectionItemChange { get; set; }
        public List<def_SectionItems> sectionItems { get; set; }
        public string thisScreenTitle { get; set; }

        public List<SelectListItem> languages { get; set; }
    }

}
