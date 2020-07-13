using System.Collections.Generic;

namespace Assmnts.Models
{
    public class ExportModel
    {
        public List<int?> formIds { get; set; }

        public Dictionary<int, string> formNames { get; set; }

        public List<int?> formResultIds { get; set; }
    }
}