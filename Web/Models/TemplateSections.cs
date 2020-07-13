using System.Collections.Generic;


namespace Assmnts.Models
{

    public class TemplateSections
    {
        public int formId { get; set; }
        public List<def_Sections> sections { get; set; }
        public string thisScreenTitle { get; set; }
        public string prevScreenHref { get; set; }
    }

}
