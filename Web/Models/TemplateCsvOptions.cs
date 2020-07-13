using System.Collections.Generic;


namespace Assmnts.Models
{

    public class TemplateCsvOptions
    {
        public List<def_Parts> parts { get; set; }
        public int formId { get; set; }
        public List<def_Forms> forms { get; set; }
        public Dictionary<string, int> formPartMap { get; set; }
    }

}
