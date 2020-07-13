using Assmnts.UasServiceRef;

using System;
using System.Collections.Generic;
using System.Web.Mvc;


namespace Assmnts.Models
{

    public class AdapApplicantRpt1
    {
        public string MemberId { get; set; }
        public int formResult { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ActiveUserName { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public List<string> TypeDDL { get; set; }
        public List<Group> TeamDDL { get; set; }
        public Dictionary<int,string> StatusDDL { get; set; }
        public List<string> DateDDL { get; set; }

        public List<SelectListItem> Teams { get; set; }
        public int teamId { get; set; }
        public int formResultId { get; set; }

        public string ReportName { get; set; }
        public string setType { get; set; }
        public string setTeam { get; set; }
        public string setStatus { get; set; }
        public string setDate { get; set; }
        public bool ShowFullHistory { get; set; }

        public string errorMessage { get; set; }

        public List<SelectListItem> Forms { get; set; }
        public int FormId { get; set; }

        public List<SelectListItem> SearchForms { get; set; }
        public int SearchFormId { get; set; }

        public List<SelectListItem> Units { get; set; }
        public int UnitId { get; set; }

        public string EigibilityEndDate { get; set; }
    }
}