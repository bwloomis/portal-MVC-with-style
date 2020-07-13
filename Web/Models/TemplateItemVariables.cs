using System.Collections.Generic;

using System.Web.Mvc;

namespace Assmnts.Models
{

    public class TemplateItemVariables
    {
        public int sectionId { get; set; }
        public int itemId { get; set; }
        public def_Items itm { get; set; }
        public List<def_ItemVariables> itemVariables { get; set; }
        public SelectList baseTypes { get; set; }
        public string thisScreenTitle { get; set; }
    }

}
