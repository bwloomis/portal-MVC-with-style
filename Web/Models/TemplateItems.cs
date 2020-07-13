using AJBoggs.Sis.Reports;

using Data.Abstract;

using System.Collections.Generic;
using System.Web.Mvc;
using Assmnts.UasServiceRef;
using System;

namespace Assmnts.Models
{

    //OT 7-7-15
    //Created two identical classes to start updating controller code and views
    //members that are exclusive to one form type should be mvoed from TemplateItems 
    //to either QuestionListForm or GeneralForm
    public class QuestionListForm : TemplateItems { }

    public class GeneralForm : TemplateItems { }

    public class TemplateItems
    {

        public IFormsRepository formsRepo { get; set; }

        public int formResultId { get; set; }

        public bool newAssmnt { get; set; }

        public TemplateNavMenu navMenuModel { get; set; }

        public int formId { get; set; }

        public byte formStatus { get; set; }

        public string formSatusText { get; set; }
        public int partId { get; set; }
        public bool ventureMode { get; set; }
        public string currentUser { get; set; }

        public int thisSectionId { get; set; }
        public int thisPartId { get; set; }

        public string navSectionId { get; set; } //this will be set when the screen is to be changed following a form submission
        public string navPartId { get; set; } //this will be set when the screen is to be changed following a form submission

        public bool isProfile { get; set; }
        public bool inProgress { get; set; }
        
        public string thisScreenCaption         { get; set; }
        public string thisScreenTitle           { get; set; }
        public def_Sections thisSection         { get; set; }

        public string prevScreenHref            { get; set; }
        public string prevScreenTitle           { get; set; }
        public string prevScreenPartId          { get; set; }
        public string prevScreenSectionId       { get; set; }

        public string nextScreenHref            { get; set; }
        public string nextScreenTitle           { get; set; }
        public string nextScreenPartId          { get; set; }
        public string nextScreenSectionId       { get; set; }

        public List<def_Sections> subSections { get; set; }
        public List<def_Items> items { get; set; }
        public def_Items notesItem { get; set; }
        public Dictionary<string, string> fldLabels { get; set; }
        public Dictionary<string, string> itmPrompts { get; set; }
        public Dictionary<string, string> rspValues { get; set; }
        public Dictionary<string, string> sctTitles { get; set; }
        public List<SelectListItem> languages { get; set; }

        public SisPdfReportOptions reportOptions { get; set; }

        //only applicable to certain screens, normally false
        public bool showItemVariableIdentifiersAsTooltips { get; set; }

        //contains a subset of the keys in rspValues
        // public Dictionary<string, string[]> dropDownOptions { get; set; }

        public Dictionary<string, bool> fldRequired { get; set; }
        public Dictionary<string, bool> fldReadOnly { get; set; }

        //validation
        public Dictionary<int, Dictionary<int, List<string>>> missingItemVariablesBySectionByPart { get; set; }
        public List<string> validationMessages { get; set; }

        public UserDisplay formResultUser { get; set; }
        
        //Memberid || clientid || adapid
        public string clientId { get; set; } 

        public DateTime updatedDate { get; set; }

        public string EligibilityEnddate { get; set; }

        public string FormVariantTitle { get; set; }

        public bool CanUserChangeStatus { get; set; }
    }
}
