using System.Collections.Generic;


namespace Assmnts.Models
{

    public class TemplateFormParts
    {
        public int formId { get; set; }
        public int partId { get; set; }
        public bool partChange { get; set; }
        public bool formPartChange { get; set; }
        public List<def_FormParts> formParts { get; set; }
        public string thisScreenTitle { get; set; }
        public bool visible { get; set; }
    }

}
