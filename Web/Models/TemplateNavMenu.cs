
using Assmnts.UasServiceRef;
using System;

namespace Assmnts.Models
{
    public abstract class TemplateNavMenu
    {
        public bool readOnly { get; set; }

        //Added for Client info Bar.. To be removed when client info component is done
        public string formSatusText { get; set; }
        public UserDisplay formResultUser { get; set; }

        public string clientId { get; set; }

        public DateTime updatedDate { get; set; }

        public string EligibilityEnddate { get; set; }

        public string FormVariantTitle { get; set; }

        public bool CanUserChangeStatus { get; set; }

        public int formResultId { get; set; }
    }
}
