using Data.Abstract;

using System.Collections.Generic;


namespace Assmnts.Models
{

    public class TemplateSisNavMenu : TemplateNavMenu
    {
        public TemplateItems parent { get; set; }

        public string assmntTitle { get; set; }
        public string currentUser { get; set; }
        public string recipientName { get; set; }
        public string trackingNumber { get; set; }

        public Dictionary<int, string> forms { get; set; }

        private readonly IFormsRepository formsRepo;

        public Dictionary<def_Parts,List<def_Sections>> sectionsByPart { get; set; }

        public bool create { get; set; }
        public bool unlock { get; set; }
        public bool delete { get; set; }
        public bool archive { get; set; }
        public bool undelete { get; set; }
        public bool ventureMode { get; set; }

        public TemplateSisNavMenu(IFormsRepository fr)
        {
            formsRepo = fr;            
        }
    }
}
