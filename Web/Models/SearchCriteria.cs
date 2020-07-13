using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;


namespace Assmnts.Models
{

    public class SearchCriteria
    {
        public List<SelectListItem> InterviewerList { get; set; }
        public List<SelectListItem> GroupList { get; set; }
        public List<SelectListItem> EnterpriseList { get; set; }
        public List<SelectListItem> PatternList { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string sisIds { get; set; }
        public string recIds { get; set; }
        public string trackingNumber { get; set; }
        public string primeIds { get; set; }
        public string cliendIds { get; set; }
        public DateTime? updatedDateFrom { get; set; }
        public DateTime? updatedDateTo { get; set; }
        public DateTime? interviewDateFrom { get; set; }
        public DateTime? interviewDateTo { get; set; }
        public List<string> SelectedInterviewers { get; set; }
        public List<int> SelectedGroups { get; set; }
        public List<int> SelectedEnts { get; set; }
        public List<int> SelectedPatterns { get; set; }

        public bool statusCompleted { get; set; }
        public bool statusNew { get; set; }
        public bool statusInProgress { get; set; }
        public bool statusAbandoned { get; set; }
        public bool statusLocked { get; set; }

        public bool reviewStatusToBeReviewed { get; set; }
        public bool reviewStatusReviewed { get; set; }
        public bool reviewStatusApproved { get; set; }
        public bool reviewStatusPreQA { get; set; }

        public bool adultSis { get; set; }
        public bool childSis { get; set; }

        public bool locked { get; set; }
        public bool archived { get; set; }
        public bool deleted { get; set; }
        public bool inactiveRecipient { get; set; }
        public bool training { get; set; }
        public bool allForEachRecipient { get; set; }

        [Display(Name = "Pattern Check")]
        public bool PatternCheck { get; set; }

        public SearchCriteria()
        {
            adultSis = true;
            childSis = true;
            statusCompleted = true;
            statusNew = true;
            statusInProgress = true;
            statusAbandoned = true;
            statusLocked = true;
            PatternCheck = false;
        }
    }
}