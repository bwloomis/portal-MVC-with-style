using Data.Abstract;

using System.Collections.Generic;


namespace Assmnts.Models
{

    public class TemplateAdapNavMenu : TemplateNavMenu
    {

        public int                      adapFormId              { get; set; }
        public int                      adapPartId              { get; set; }
        public int                      sctId                   { get; set; }
        public Dictionary<string, int>  sectionIds              { get; set; }
        public Dictionary<string, string>  sectionTitles        { get; set; }

        public string                   ActiveUserName          { get; set; }
        public string                   firstName               { get; set; }
        public string                   lastName                { get; set; }
        public string                   currentSectionTitle     { get; set; }

        public bool                     access                  { get; set; }

        public TemplateAdapNavMenu()
        {

        }
    }
}
