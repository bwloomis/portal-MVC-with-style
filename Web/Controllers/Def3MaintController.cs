using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web.Mvc;

using Assmnts.Infrastructure;

using Data.Abstract;
using Data.Concrete;
using Assmnts.Models;
using System.Diagnostics;

namespace Assmnts.Controllers
{
    /*
     * This Controller handles the screens for maintenance of the DEF3 Lookup tables.
     * Includes: Master, Detail, and Text tables.
     * 
     */
    [RedirectingAction]
    public class Def3MaintController : Controller
    {
        private IFormsRepository formsRepo;
        
        private dynamic lookupModel;

        // The classes below are for the display model data structures needed by the screens.
        public class ExpLookupMaster
        {
            public int lookupMasterId { get; set; }
            public string lookupCode { get; set; }
            public string title { get; set; }
            public string baseType { get; set; }
            public int baseTypeId { get; set; }
        }
        
        public class ExpLookupDetail
        {
            public int lookupDetailId { get; set; }
            public int lookupMasterId { get; set; }
            public string enterprise { get; set; }
            public string group { get; set; }
            public int displayOrder { get; set; }
            public string dataValue { get; set; }

        }
        
        public class ExpLookupText
        {
            public int lookupTextId { get; set; }
            public int lookupDetailId { get; set; }
            public string lang { get; set; }
            public string displayText { get; set; }
        }
        
        /*
         *   Constructor
         */
        public Def3MaintController(IFormsRepository fr)
        {
            formsRepo = fr;

            lookupModel = new ExpandoObject(); 
            lookupModel.baseTypes = fr.GetBaseTypes();
            lookupModel.languages = formsRepo.GetAllLanguages();

        }
            
        
        // Default screen
        public ActionResult Index()
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            return View(lookupModel);
        }


        public ActionResult Select()
        {
            IUasSql uasSql = new UasSql();
            if (SessionHelper.LoginStatus.EnterpriseID == 0)
            {
                lookupModel.enterprises = uasSql.getEnterprises();
            }
            else
            {
                lookupModel.enterprises = new Dictionary<int, string>();
                lookupModel.enterprises.Add(SessionHelper.LoginStatus.EnterpriseID, uasSql.getEnterpriseName(SessionHelper.LoginStatus.EnterpriseID));
            }
           
            return PartialView("_Select", lookupModel);
        }
        
        [HttpPost]
        public ActionResult Texts(int detailId)
        {
            //lookupModel.LookupTexts = getTexts(detailId);
            lookupModel.detailId = detailId;

            return PartialView("_Texts", lookupModel);
        }

        /// <summary>
        /// Pulls the data which populates the DataTable in the _Texts partial view.
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableTexts(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            int detailId = 0;
            string result = String.Empty;
            Debug.WriteLine("Def3Maint Controller DataTableTexts draw:" + draw.ToString());
            Debug.WriteLine("Def3Maint Controller DataTableTexts start:" + start.ToString());
            Debug.WriteLine("Def3Maint Controller DataTableTexts searchValue:" + searchValue);
            Debug.WriteLine("Def3Maint Controller DataTableTexts searchRegex:" + searchRegex);
            Debug.WriteLine("Def3Maint Controller DataTableTexts order:" + order);
            try
            {
                string r = Request["detailId"];
                if (!String.IsNullOrEmpty(r))
                {
                    detailId = Convert.ToInt32(r);
                }
                //  Lists all the Request parameters
                foreach (string s in Request.Params.Keys)
                {
                    Debug.WriteLine(s.ToString() + ":" + Request.Params[s]);
                }

                try
                {
                    // Initialize the DataTable response (later transformed to JSON)
                    DataTableResult dtr = new DataTableResult()
                    {
                        draw = draw,
                        recordsTotal = 1,
                        recordsFiltered = 1,
                        data = new List<string[]>(),
                        error = String.Empty
                    };

                    // rename to avoid language/library conflicts
                    int iniIndex = start;
                    int noDsplyRecs = length;

                    List<def_LookupText> query = formsRepo.GetLookupTextsByLookupDetail(detailId);

                    List<def_BaseTypes> types = formsRepo.GetBaseTypes();
                    string basedropdown = String.Empty;
                    foreach (def_Languages l in formsRepo.GetAllLanguages())
                    {
                        basedropdown += "<option value=\"" + l.langId + "\">" + l.title + "</option>";
                    }

                    dtr.recordsTotal = dtr.recordsFiltered = query.Count();
                    foreach (def_LookupText q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                    {
                        string dropdown = basedropdown;

                        // Flag the appropriate html option as selected.
                        string findText = "value=\"" + q.langId;
                        int index = dropdown.IndexOf(findText);
                        dropdown = dropdown.Insert(index - 1, " selected=\"selected\"");

                        string[] data = new string[] {
                            "<span id=\"language[" + q.lookupTextId + "]\">" + formsRepo.GetLanguageById(q.langId).title + "</span>"
                            + "<span hidden=\"hidden\" id=\"languageSelectTd[" + q.lookupTextId + "]\">"
                                + "<select name=\"selectdLanguage\" class=\"form-control\" id=\"selectLanguage[" + q.lookupTextId + "]\">"
                                    + dropdown
                                + "</select>"
                            + "</span>",
                            "<span id=\"displayText[" + q.lookupTextId + "]\">" + q.displayText + "</span>",
                            "<span id=\"editText[" + q.lookupTextId + "]\"><a href=\"#\" onclick=\"editText(" + q.lookupTextId + ")\">Edit</a></span>"
                            + "<span hidden=\"hidden\" id=\"saveText[" + q.lookupTextId + "]\"><a href=\"#\" onclick=\"saveText(" + q.lookupTextId + ")\">Save</a></span>",
                            "<span id=\"deleteText[" + q.lookupTextId + "]\"><a href=\"#\" onclick=\"deleteText(" + q.lookupTextId + ",'" + q.displayText + "')\">Delete</a></span>"
                            + "<span hidden=\"hidden\" id=\"cancelText[" + q.lookupTextId + "]\"><a href=\"#\" onclick=\"cancelText(" + q.lookupTextId + ")\">Cancel</a></span>"
                        };
                        dtr.data.Add(data);
                    }

                    Debug.WriteLine("Def3Maint Controller DataTableTexts data populated.");

                    // Output the JSON in DataTable format
                    fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                    param.EnableAnonymousTypes = true;
                    result = fastJSON.JSON.ToJSON(dtr, param);
                    Debug.WriteLine("Def3Maint Controller DataTableTexts result:" + result);
                }
                catch (Exception excptn)
                {
                    Debug.WriteLine("Def3Maint Controller DataTableTexts exception:" + excptn.Message);
                    result = excptn.Message + " - " + excptn.Message;
                }

            }
            catch (FormatException excptn)
            {
                Debug.WriteLine("Def3Maint Controller DataTableTexts detailId format exception:" + excptn.Message);
            }
            return result;

        }
        
        [HttpPost]
        public ActionResult Details(int masterId, int entId = 0, int grpId = 0)
        {
            lookupModel.masterId = masterId;

            return PartialView("_Details", lookupModel);
        }

        /// <summary>
        /// Pulls the data which populates the DataTable in the _Details partial view.
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableDetails(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            int masterId = 0;
            int ent = 0;
            int grp = 0;
            string result = String.Empty;
            Debug.WriteLine("Def3Maint Controller DataTableDetails draw:" + draw.ToString());
            Debug.WriteLine("Def3Maint Controller DataTableDetails start:" + start.ToString());
            Debug.WriteLine("Def3Maint Controller DataTableDetails searchValue:" + searchValue);
            Debug.WriteLine("Def3Maint Controller DataTableDetails searchRegex:" + searchRegex);
            Debug.WriteLine("Def3Maint Controller DataTableDetails order:" + order);
            try
            {
                string r = Request["masterId"];
                if (!String.IsNullOrEmpty(r))
                {
                    masterId = Convert.ToInt32(r);
                }
                r = Request["entId"];
                if (!String.IsNullOrEmpty(r))
                {
                    try
                    {
                        ent = Convert.ToInt32(r);
                    }
                    catch (FormatException excptn)
                    {

                    }
                }
                r = Request["grpId"];
                if (!String.IsNullOrEmpty(r))
                {
                    try
                    {
                        grp = Convert.ToInt32(r);
                    }
                    catch (FormatException excptn)
                    {

                    }
                }
                //  Lists all the Request parameters
                foreach (string s in Request.Params.Keys)
                {
                    Debug.WriteLine(s.ToString() + ":" + Request.Params[s]);
                }

                try
                {
                    // Initialize the DataTable response (later transformed to JSON)
                    DataTableResult dtr = new DataTableResult()
                    {
                        draw = draw,
                        recordsTotal = 1,
                        recordsFiltered = 1,
                        data = new List<string[]>(),
                        error = String.Empty
                    };

                    // rename to avoid language/library conflicts
                    int iniIndex = start;
                    int noDsplyRecs = length;

                    int englishId = 0;
                    foreach (def_Languages l in formsRepo.GetAllLanguages()) {
                        if (l.title.Equals("English")) {
                            englishId = l.langId;
                            break;
                        }
                    }
                    List<def_LookupDetail> query = formsRepo.GetLookupDetails(masterId, ent, grp);

                    List<def_BaseTypes> types = formsRepo.GetBaseTypes();
                    dtr.recordsTotal = dtr.recordsFiltered = query.Count();
                    foreach (def_LookupDetail q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                    {
                        def_LookupText dl = formsRepo.GetLookupTextsByLookupDetailLanguage(q.lookupDetailId, englishId).FirstOrDefault();
                        string engText = (dl != null) ? dl.displayText : String.Empty;
                        string[] data = new string[] {
                            "<span id=\"dataValue[" + q.lookupDetailId + "]\">" + q.dataValue + "</span>",
                            "<span id=\"order[" + q.lookupDetailId + "]\">" + q.displayOrder + "</span>",
                            "<span id=\"english[" + q.lookupDetailId + "]\">" + engText + "</span>",
                            "<span id=\"editDetail[" + q.lookupDetailId + "]\"><a href=\"#\" onclick=\"editDetail(" + q.lookupDetailId + ")\">Edit</a></span>"
                            + "<span id=\"saveDetail[" + q.lookupDetailId + "]\" hidden=\"hidden\"><a href=\"#\" onclick=\"saveDetail(" + q.lookupDetailId + ")\">Save </a></span>",
                            "<span id=\"textDetail[" + q.lookupDetailId + "]\"><a href=\"#\" onclick=\"textDetail(" + q.lookupDetailId + ", '" + q.dataValue + "')\">View Text </a></span>"
                            + "<span id=\"cancelDetail[" + q.lookupDetailId + "]\" hidden=\"hidden\"><a href=\"#\" onclick=\"cancelDetail(" + q.lookupDetailId + ")\">Cancel </a></span>",
                            "<span id=\"deleteDetail[" + q.lookupDetailId + "]\"><a href=\"#\" onclick=\"deleteDetail(" + q.lookupDetailId + ", '" + q.dataValue + "')\">Delete</a></span>"
                        };
                        dtr.data.Add(data);
                    }

                    Debug.WriteLine("Def3Maint Controller DataTableDetails data populated.");

                    // Output the JSON in DataTable format
                    fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                    param.EnableAnonymousTypes = true;
                    result = fastJSON.JSON.ToJSON(dtr, param);
                    Debug.WriteLine("Def3Maint Controller DataTableDetails result:" + result);
                }
                catch (Exception excptn)
                {
                    Debug.WriteLine("Def3Maint Controller DataTableDetails exception:" + excptn.Message);
                    result = excptn.Message + " - " + excptn.Message;
                }

            }
            catch (FormatException excptn)
            {
                Debug.WriteLine("Def3Maint Controller DataTableDetails masterId format exception:" + excptn.Message);
            }
            return result;

        }

        [HttpPost]
        public ActionResult Masters()
        {
            //lookupModel.LookupMasters = getMasters();
            return PartialView("_Masters", lookupModel);
        }

        /// <summary>
        /// Pulls the data which populates the DataTable in the _Masters partial view.
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableMasters(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            string result = String.Empty;
            Debug.WriteLine("Def3Maint Controller DataTableMasters draw:" + draw.ToString());
            Debug.WriteLine("Def3Maint Controller DataTableMasters start:" + start.ToString());
            Debug.WriteLine("Def3Maint Controller DataTableMasters searchValue:" + searchValue);
            Debug.WriteLine("Def3Maint Controller DataTableMasters searchRegex:" + searchRegex);
            Debug.WriteLine("Def3Maint Controller DataTableMasters order:" + order);

            //  Lists all the Request parameters
            foreach (string s in Request.Params.Keys)
            {
                Debug.WriteLine(s.ToString() + ":" + Request.Params[s]);
            }

            try
            {
                // Initialize the DataTable response (later transformed to JSON)
                DataTableResult dtr = new DataTableResult()
                {
                    draw = draw,
                    recordsTotal = 1,
                    recordsFiltered = 1,
                    data = new List<string[]>(),
                    error = String.Empty
                };

                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;

                List<def_LookupMaster> query = formsRepo.GetLookupMasters();

                List<def_BaseTypes> types = formsRepo.GetBaseTypes();
                string basedropdown = String.Empty;
                foreach (def_BaseTypes bt in types)
                {
                    basedropdown += "<option value=\"" + bt.baseTypeId + "\">" + bt.enumeration + "</option>";
                }

                dtr.recordsTotal = dtr.recordsFiltered = query.Count();
                foreach (def_LookupMaster q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                {
                    string dropdown = basedropdown;

                    // Flag the appropriate html option as selected.
                    string findText = "value=\"" + q.baseTypeId;
                    int index = dropdown.IndexOf(findText);
                    dropdown = dropdown.Insert(index - 1, " selected=\"selected\"");

                    string[] data = new string[] {
                        "<span id=\"lkpCd[" + q.lookupMasterId + "]\">" + q.lookupCode + "</span>",
                        "<span id=\"ttl[" + q.lookupMasterId + "]\">" + q.title + "</span>",
                        "<span id=\"bsTyp[" + q.lookupMasterId + "]\">" + formsRepo.GetBaseTypeById(q.baseTypeId).enumeration + "</span>"
                        + "<span id=\"bsTypSelDat[" + q.lookupMasterId + "]\" hidden=\"hidden\">"
                            + "<select class=\"masterBase form-control\" id=\"bsTypSel[" + q.lookupMasterId + "]\" name=\"addMasterBase\">"
                                + dropdown 
                            + "</select>" 
                        + "</span>",
                        "<span id=\"editMaster[" + q.lookupMasterId + "]\"><a href=\"#\" onclick=\"editMaster(" + q.lookupMasterId + ")\">Edit</a></span>"
                        + "<span id=\"saveMaster[" + q.lookupMasterId + "]\" hidden=\"hidden\"><a href=\"#\" onclick=\"saveMaster(" + q.lookupMasterId + ")\">Save</a></span>",
                        "<span id=\"detailsInquiry[" + q.lookupMasterId + "]\"><a href=\"#\" onclick=\"DetailsInquiry(" + q.lookupMasterId + ", '" + q.title + "')\">View Details</a></span>"
                        + "<span id=\"cancelMaster[" + q.lookupMasterId + "]\" hidden=\"hidden\"><a href=\"#\" onclick=\"cancelMaster(" + q.lookupMasterId + ")\">Cancel</a></span>",
                        "<span id=\"deleteMaster[" + q.lookupMasterId + "]\"><a href=\"#\" onclick=\"deleteMaster(" + q.lookupMasterId + ", '" + q.lookupCode + "')\">Delete</a></span>"
                    };
                    dtr.data.Add(data);
                }

                Debug.WriteLine("Def3Maint Controller DataTableMasters data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                Debug.WriteLine("Def3Maint Controller DataTableMasters result:" + result);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Def3Maint Controller DataTableMasters exception:" + excptn.Message);
                result = excptn.Message + " - " + excptn.Message;
            }

            return result;

        }
        
        [HttpPost]
        public ActionResult addMaster(string lookupCode, string title, int baseType)
        {
            formsRepo.SetLookupMaster(lookupCode, title, baseType, null);

            lookupModel.LookupMasters = getMasters();
            return PartialView("_Masters", lookupModel);
        }
        
        [HttpPost]
        public ActionResult saveMaster(int lookupMasterId, string lookupCode, string title, int baseType)
        {
            def_LookupMaster master = formsRepo.GetLookupMasterById(lookupMasterId);

            formsRepo.SetLookupMaster(lookupCode, title, baseType, master);

            lookupModel.LookupMasters = getMasters();

            return PartialView("_Masters", lookupModel);
        }
      
        [HttpPost]
        public ActionResult deleteMaster(int masterId)
        {
            List<def_LookupDetail> lookupDetails = new List<def_LookupDetail>();
            lookupDetails.AddRange(formsRepo.GetLookupMasterById(masterId).def_LookupDetail);

            List<def_LookupText> lookupTexts = new List<def_LookupText>();

            foreach (def_LookupDetail detail in lookupDetails)
            {
                lookupTexts.AddRange(formsRepo.GetLookupTextsByLookupDetail(detail.lookupDetailId));
            }

            foreach (def_LookupText text in lookupTexts)
            {
                formsRepo.DeleteLookupText(text);
            }

            foreach (def_LookupDetail detail in lookupDetails)
            {
                formsRepo.DeleteLookupDetail(detail);
            }

            def_LookupMaster master = formsRepo.GetLookupMasterById(masterId);
            formsRepo.DeleteLookupMaster(master);

            lookupModel.LookupMasters = getMasters();
            return PartialView("_Masters", lookupModel);
        }
        
        [HttpPost]
        public ActionResult addDetail(int lookupMasterId, string dataValue, int enterprise, int group, int displayOrder)
        {
            formsRepo.SetLookupDetail(dataValue, lookupMasterId, enterprise, group, displayOrder, null);

            //lookupModel.LookupDetails = getDetails(lookupMasterId, enterprise, group);
            lookupModel.masterId = lookupMasterId;

            return PartialView("_Details", lookupModel);
        }

        [HttpPost]
        public ActionResult saveDetail(int lookupDetailId, int lookupMasterId, string dataValue, int enterprise, int group, int displayOrder)
        {
            def_LookupDetail detail = formsRepo.GetLookupDetailById(lookupDetailId);

            formsRepo.SetLookupDetail(dataValue, lookupMasterId, enterprise, group, displayOrder, detail);

            //lookupModel.LookupDetails = getDetails(lookupMasterId, enterprise, group);
            lookupModel.masterId = lookupMasterId;

            return PartialView("_Details", lookupModel);
        }
        
        [HttpPost]
        public ActionResult deleteDetail(int lookupDetailId, int lookupMasterId, int enterprise = 0, int group = 0)
        {
            List<def_LookupText> texts = formsRepo.GetLookupTextsByLookupDetail(lookupDetailId);

            foreach (def_LookupText text in texts)
            {
                formsRepo.DeleteLookupText(text);
            }

            def_LookupDetail detail = formsRepo.GetLookupDetailById(lookupDetailId);
            formsRepo.DeleteLookupDetail(detail);

            //lookupModel.LookupDetails = getDetails(lookupMasterId, enterprise, group);
            lookupModel.masterId = lookupMasterId;

            return PartialView("_Details", lookupModel);

        }


        [HttpPost]
        public ActionResult addText(int lookupDetailId, int langId, string displayText)
        {
            formsRepo.SetLookupText(lookupDetailId, langId, displayText, null);

            //lookupModel.LookupTexts = getTexts(lookupDetailId);
            lookupModel.detailId = lookupDetailId;

            return PartialView("_Texts", lookupModel);

        }
        
        [HttpPost]
        public ActionResult saveText(int lookupTextId, int lookupDetailId, int langId, string displayText)
        {
            def_LookupText text = formsRepo.GetLookupTextById(lookupTextId);

            formsRepo.SetLookupText(lookupDetailId, langId, displayText, text);

            //lookupModel.LookupTexts = getTexts(lookupDetailId);
            lookupModel.detailId = lookupDetailId;

            return PartialView("_Texts", lookupModel);
        }

        [HttpPost]
        public ActionResult deleteText(int lookupTextId, int lookupDetailId)
        {
            def_LookupText text = formsRepo.GetLookupTextById(lookupTextId);

            formsRepo.DeleteLookupText(text);

            //lookupModel.LookupTexts = getTexts(lookupDetailId);
            lookupModel.detailId = lookupDetailId;

            return PartialView("_Texts", lookupModel);
        }

        [HttpPost]
        public string GetGroups(int ent)
        {
            List<int> authGroups = SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.ToList();
            
            using (var context = DataContext.getUasDbContext())
            {
                context.Configuration.LazyLoadingEnabled = false;

                var groups = context.uas_Group.Where(i => i.EnterpriseID == ent && (authGroups.Contains(i.GroupID) || authGroups.Contains(0))).ToList();
                //var groups = UAS.Business.LocalClient.GetChildGroups(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0])
                //            .Where(id => id > -1)
                //            .Select(groupId => context.uas_Group.FirstOrDefault(g => g.GroupID == groupId)).ToList();

                string jsonString = fastJSON.JSON.ToJSON(groups);
                return jsonString;
            }
        }

        private List<ExpLookupMaster> getMasters()
        {
            List<ExpLookupMaster> expLookupMasters = new List<ExpLookupMaster>();

            List<def_LookupMaster> lookupMasters = formsRepo.GetLookupMasters();
            foreach (def_LookupMaster master in lookupMasters)
            {
                ExpLookupMaster expMaster = new ExpLookupMaster();

                expMaster.lookupMasterId = master.lookupMasterId;
                expMaster.lookupCode = master.lookupCode;
                expMaster.title = master.title;
                expMaster.baseType = formsRepo.GetBaseTypeById(master.baseTypeId).enumeration;
                expMaster.baseTypeId = master.baseTypeId;

                expLookupMasters.Add(expMaster);
            }
            return expLookupMasters;
        }

        private List<def_LookupDetail> getDetails(int masterId, int ent = 0, int grp = 0)
        {

            IUasSql uasSql = new UasSql();

            string enterpriseName = uasSql.getEnterpriseName(ent);
            string groupName = uasSql.getGroupName(grp);
            
            List<def_LookupDetail> lookupDetails = new List<def_LookupDetail>(); 
            
            lookupDetails.AddRange(formsRepo.GetLookupDetails(masterId, ent, grp));


            return lookupDetails;
        }

        private List<ExpLookupText> getTexts(int detailId)
        {
            List<def_LookupText> texts = formsRepo.GetLookupTextsByLookupDetail(detailId);

            List<ExpLookupText> expLookupTexts = new List<ExpLookupText>();

            foreach (def_LookupText text in texts) 
            {
                ExpLookupText expText = new ExpLookupText();

                expText.lookupTextId = text.lookupTextId;
                expText.lookupDetailId = text.lookupDetailId;
                expText.lang = formsRepo.GetLanguageById(text.langId).title;
                expText.displayText = text.displayText;

                expLookupTexts.Add(expText);
            }
            return expLookupTexts;
        }
        
    }


}
