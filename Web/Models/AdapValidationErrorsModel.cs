using System.Collections.Generic;

namespace Assmnts.Models
{
    public class AdapValidationErrorsModel : TemplateItems
    {
        public Dictionary<int, Dictionary<int, List<string>>> titlesOfMissingSubsectionsBySectionByPart { get; set; }
    }
}
