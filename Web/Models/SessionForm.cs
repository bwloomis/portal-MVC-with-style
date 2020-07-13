using Assmnts;

namespace Assmnts.Models
{

    public enum TemplateType
    {
        StdSingleItem,              // Display 1 item at a time on the screen
        StdSectionItems,            // Display all the items in a Section (like the SIS Profile screen)
        MultipleCommonItems         // Display a list of common items like the SIS Section Questions
    };

    public class SessionForm
    {
        public TemplateType templateType { get; set; }
        public int formId { get; set; }
        public int partId { get; set; }
        public int sectionId { get; set; }
        public int itemId { get; set; }
        public int langId { get; set; }

        public int formResultId { get; set; }

        public bool readOnlyMode { get; set; }

        public string formIdentifier;

        public SessionForm()
        {
            templateType = TemplateType.StdSectionItems;
            formId = 0;
            partId = 0;
            sectionId = 0;
            itemId = 0;
            langId = 1;

            formResultId = 0;

            readOnlyMode = false;

            formIdentifier = string.Empty;
        }
    }

}
