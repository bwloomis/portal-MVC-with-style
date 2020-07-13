using System.Collections.Generic;


namespace Assmnts.Models
{

    public class TemplatePartSections
    {
        public int formId { get; set; }
        public int partId { get; set; }
        public bool sectionChange { get; set; }
        public bool partSectionChange { get; set; }
        public List<def_PartSections> partSections { get; set; }
        public string thisScreenTitle { get; set; }
        public bool visible { get; set; }
    }

}
