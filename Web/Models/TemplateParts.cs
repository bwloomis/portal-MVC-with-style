using System.Collections.Generic;


namespace Assmnts.Models
{

    public class TemplateParts
    {
        public int formId { get; set; }
        public List<def_Parts> parts { get; set; }
        public string thisScreenTitle { get; set; }
        public string prevScreenHref { get; set; }
    }

}
