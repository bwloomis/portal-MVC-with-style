using AJBoggs.Adap.Domain;
using AJBoggs.Adap.Services.Api;
using AJBoggs.Adap.Services.Xml;
using AJBoggs.Def.Services;

using Assmnts.Business;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Assmnts.Reports;
using Assmnts.UasServiceRef;

using Data.Abstract;

using NLog;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Assmnts.Controllers
{
    [ADAPRedirectingAction]
    public class COADAPController : Controller
    {

        private IFormsRepository formsRepo;
        private ILogger mLogger;

        public COADAPController(IFormsRepository fr, ILogger logger)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
            mLogger = logger;
        }

        public ActionResult Index()
        {
            if (UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts"))
            {
                return RedirectToAction("ApplicationsPending", "ADAP");
            }
            else
            {
                int formId = formsRepo.GetFormByIdentifier("ADAP").formId;
                IQueryable<vFormResultUser> query = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, formId);

                int userId = SessionHelper.LoginStatus.UserID;
                query = query.Where(fr => fr.subject == userId);

                if (query.Count() > 0)
                {
                    return RedirectToAction("AdapPortal", "COADAP", new { userId = userId });
                }
                else
                {
                    return RedirectToAction("CreateAdapApplication", "COADAP", new { userId = userId });
                }
            }
        }

        /// <summary>
        /// The main page loaded for non-staff users.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult AdapPortal()
        {

            String userId = Request["userId"];
            String err = Request["error"];

            if (!String.IsNullOrEmpty(err) && err.Equals("Not Approved"))
            {
                err = "New Applications cannot be created while an application is being processed.";
            }
            int uId;
            UAS.DataDTO.LoginStatus ls = SessionHelper.LoginStatus;
            if (String.IsNullOrEmpty(userId))
            {
                uId = ls.UserID;
            }
            else
            {
                try
                {
                    uId = Convert.ToInt32(userId);
                }
                catch (Exception ex)
                {
                    uId = 0;
                    Debug.WriteLine("Adap Controller AdapPortal exception:" + ex.Message);
                }
            }
            AuthenticationClient webclient = new AuthenticationClient();
            UserDisplay ud = webclient.GetUserDisplay(uId);

            IQueryable<vFormResultUser> query = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, 6);
            int? soInt = formsRepo.GetStatusDetailByMasterIdentifier(1, "APPROVED").sortOrder;
            query = query.Where(q => (q.subject == uId) && (q.formStatus == soInt));

            String recert;
            if (query.Count() > 0)
            {
                DateTime dob = Convert.ToDateTime(ud.DOB);
                recert = new Applications(formsRepo).GetRecert(dob, query.Count(), Convert.ToDateTime(query.OrderByDescending(q => q.statusChangeDate).Select(q => q.statusChangeDate).FirstOrDefault())).ToString("MMMM yyyy");
            }
            else
            {
                recert = "None Pending";
            }

            AdapPortal ap = new AdapPortal()
            {
                Name = ud.FirstName + " " + ud.LastName,
                RecertDate = recert,
                UserId = uId.ToString(),
                errorMsg = err,
                EnterpriseID = SessionHelper.LoginStatus.EnterpriseID
            };

            return View("~/Views/Templates/ADAP/AdapPortal.cshtml", ap);
        }

        /// <summary>
        /// Creates a new ADAP application, attempting to pull as much data from the previous application and from UAS as possible.
        /// New applications are only permitted when there is no previous applicatoin, or the previous application was approved.
        /// </summary>
        /// <returns>Redirects the user to the Application pages.</returns>
        [HttpGet]
        public ActionResult CreateAdapApplication()
        {
            def_Forms frm = formsRepo.GetFormByIdentifier("ADAP");
            //string userId = Request["userId"];
            int userId = 0;
            try
            {
                userId = Convert.ToInt32(Request["userId"]);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Controller CreateAdapApplication exception:" + excptn.Message);
            }

            int? intSO = formsRepo.GetStatusDetailByMasterIdentifier(1, "CANCELLED").sortOrder;
            def_FormResults prevRes = formsRepo.GetEntities<def_FormResults>(x => x.formId == frm.formId
                && x.subject == userId
                && x.formStatus != intSO)
                .OrderByDescending(x => x.dateUpdated)
                .FirstOrDefault();
            //def_FormResults prevRes = frm.def_FormResults.Where(f => f.subject == userId && intSO != null && f.formStatus != intSO).OrderByDescending(f => f.dateUpdated).FirstOrDefault();


            if ((prevRes == null) || (prevRes.formStatus == formsRepo.GetStatusDetailByMasterIdentifier(1, "APPROVED").sortOrder))
            {
                AuthenticationClient webclient = new AuthenticationClient();
                UserDisplay ud = webclient.GetUserDisplay(userId);

                Dictionary<string, string> ItemsToPopulateFromUAS = new Dictionary<string, string>();
                ItemsToPopulateFromUAS.Add(
                    "ADAP_D1_FirstName_item", String.IsNullOrEmpty(ud.FirstName) ? String.Empty : ud.FirstName);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_D1_LastName_item", String.IsNullOrEmpty(ud.LastName) ? String.Empty : ud.LastName);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_D1_MiddleIntl_item", String.IsNullOrEmpty(ud.MiddleName) ? String.Empty : ud.MiddleName.Substring(0, 1));
                ItemsToPopulateFromUAS.Add(
                    "ADAP_D2_DOB_item", (ud.DOB == null) ? String.Empty : Convert.ToDateTime(ud.DOB).ToString("MM/dd/yyyy"));
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C1_Address_item", (ud.Addresses != null && ud.Addresses.Count() > 0) ? ud.Addresses[0].Address1 : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C1_MayContactYN_item", (ud.Addresses != null && ud.Addresses.Count() > 0) ? ((ud.Addresses[0].MayContactAddress) ? "1" : "0") : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C1_City_item", (ud.Addresses != null && ud.Addresses.Count() > 0) ? ud.Addresses[0].City : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C1_State_item", (ud.Addresses != null && ud.Addresses.Count() > 0) ? ud.Addresses[0].State : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C1_Zip_item", (ud.Addresses != null && ud.Addresses.Count() > 0) ? ud.Addresses[0].ZIP : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C2_Address_item", (ud.Addresses != null && ud.Addresses.Count() > 1) ? ud.Addresses[1].Address1 : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C2_MayContactYN_item", (ud.Addresses != null && ud.Addresses.Count() > 1) ? ((ud.Addresses[1].MayContactAddress) ? "1" : "0") : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C2_City_item", (ud.Addresses != null && ud.Addresses.Count() > 1) ? ud.Addresses[1].City : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C2_State_item", (ud.Addresses != null && ud.Addresses.Count() > 1) ? ud.Addresses[1].State : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C2_Zip_item", (ud.Addresses != null && ud.Addresses.Count() > 1) ? ud.Addresses[1].ZIP : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C3_Phone1_Num_item", (ud.Phones != null && ud.Phones.Count() > 0) ? ud.Phones[0].Phone : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C3_Phone1_MayMsgYN_item", (ud.Phones != null && ud.Phones.Count() > 0) ? ((ud.Phones[0].MayContactPhone) ? "1" : "0") : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C3_Phone2_Num_item", (ud.Phones != null && ud.Phones.Count() > 1) ? ud.Phones[1].Phone : String.Empty);
                ItemsToPopulateFromUAS.Add(
                    "ADAP_C3_Phone2_MayMsgYN_item", (ud.Phones != null && ud.Phones.Count() > 1) ? ((ud.Phones[1].MayContactPhone) ? "1" : "0") : String.Empty);

                // * * * OT 1-20-16 updated this list based on Docs/ADAP/ADAP_item_list.xslx (revision 80734)
                string[] ItemIdentifiersToPopulateFromPrevApplication = new string[]
                {
                    #region long list of item identifiers
                    "ADAP_D1_LastName_item",
                    "ADAP_D1_FirstName_item",
                    "ADAP_D1_MiddleIntl_item",
                    "ADAP_D1_AltName_item",
                    "ADAP_D2_DOB_item",
                    "ADAP_D3_White_item",
                    "ADAP_D3_Black_item",
                    "ADAP_D3_Asian_item",
                    "ADAP_D3_Native_item",
                    "ADAP_D3_Indian_item",
                    "ADAP_D4_EthnicDrop_item",
                    "ADAP_D4_Mexican_item",
                    "ADAP_D4_Puerto_item",
                    "ADAP_D4_Cuban_item",
                    "ADAP_D4_Other_item",
                    "ADAP_D5_Indian_item",
                    "ADAP_D5_Filipino_item",
                    "ADAP_D5_Korean_item",
                    "ADAP_D5_Other_item",
                    "ADAP_D5_Chinese_item",
                    "ADAP_D5_Japanese_item",
                    "ADAP_D5_Vietnamese_item",
                    "ADAP_D5_NA_item",
                    "ADAP_D6_Native_item",
                    "ADAP_D6_Guam_item",
                    "ADAP_D6_Samoan_item",
                    "ADAP_D6_Other_item",
                    "ADAP_D6_NA_item",
                    "ADAP_D7_LangDrop_item",
                    "ADAP_D7_LangOther_item",
                    "ADAP_D8_CurrGenderDrop_item",
                    "ADAP_D8_BirthGenderDrop_item",
                    "ADAP_D9_Ramsell_item",
                    "ADAP_D10_SSN_item",
                    "ADAP_C1_Address_item",
                    "ADAP_C1_City_item",
                    "ADAP_C1_State_item",
                    "ADAP_C1_Zip_item",
                    "ADAP_C1_County_item",
                    "ADAP_C1_MayContactYN_item",
                    "ADAP_C2_SameAsMailing_item",
                    "ADAP_C2_Address_item",
                    "ADAP_C2_City_item",
                    "ADAP_C2_State_item",
                    "ADAP_C2_Zip_item",
                    "ADAP_C2_County_item",
                    "ADAP_C2_MayContactYN_item",
                    "ADAP_C3_Phone1_Num_item",
                    "ADAP_C3_Phone1_Type_item",
                    "ADAP_C3_Phone1_MayMsgYN_item",
                    "ADAP_C3_Phone2_Num_item",
                    "ADAP_C3_Phone2_Type_item",
                    "ADAP_C3_Phone2_MayMsgYN_item",
                    "ADAP_C4_MayCallYN_item",
                    "ADAP_C4_Name_item",
                    "ADAP_C4_Phone_item",
                    "ADAP_C4_KnowHivYN_item",
                    "ADAP_C5_HasCaseMngrYN_item",
                    "ADAP_C5_Mngr1_Name_item",
                    "ADAP_C5_Mngr1_Clinic_item",
                    "ADAP_C5_Mngr2_Name_item",
                    "ADAP_C5_Mngr2_Clinic_item",
                    "ADAP_C5_CanReferYN_item",
                    "ADAP_M1_Month_item",
                    "ADAP_M1_Year_item",
                    "ADAP_M1_DiagnosisLoc_item",
                    "ADAP_M2_ToldAIDS_item",
                    "ADAP_M3_ToldHepC_item",
                    "ADAP_M4_Clinic_item",
                    "ADAP_I1_Med_Yes_item",
                    "ADAP_I1_Med_Denied_item",
                    "ADAP_I1_Med_No_item",
                    "ADAP_I1_Med_Waiting_item",
                    "ADAP_I1_Med_DontKnow_item",
                    "ADAP_I1_DeniedReason_item",
                    "ADAP_I1_NotAppliedReason_item",
                    "ADAP_I2_AffCareOpt_item",
                    "ADAP_I2_AffCareOther_item",
                    "ADAP_I3_MedicareYN_item",
                    "ADAP_I3_MedNumber_item",
                    "ADAP_I3_PartAYN_item",
                    "ADAP_I3_PartBYN_item",
                    "ADAP_I3_PartADate_item",
                    "ADAP_I3_PartBDate_item",
                    "ADAP_H1_StatusDrop_item",
                    "ADAP_H2_RelnDrop_item",
                    "ADAP_H2_RelnOther_item",
                    "ADAP_H3_FileTaxYN_item",
                    "ADAP_H3_TaxStatusOpt_item",
                    "ADAP_H3_TaxDependants_item",
                    "ADAP_H3_TaxNotFileOpt_item",
                    "ADAP_H3_TaxNotFileOther_item",
                    "ADAP_H3_Relatives_item",
                    "ADAP_H4_ChildrenIn_item",
                    "ADAP_H4_ChildrenOut_item",
                    "ADAP_F1_EmployOpt_item",
                    "ADAP_F1_EmployOther_item",
                    "ADAP_F1_EmployerInsOpt_item",
                    "ADAP_F1_EmployNotEnrolled_item",
                    "ADAP_F2_EmployLast90YN_item",
                    "ADAP_F3_A_Recipient_item",
                    "ADAP_F3_A_IncomeTypeDrop_item",
                    "ADAP_F3_A_Employer_item",
                    "ADAP_F3_A_EmployStart_item",
                    "ADAP_F3_A_TempYN_item",
                    "ADAP_F3_A_IncomeTypeOther_item",
                    "ADAP_F3_A_IncomeAmt_item",
                    "ADAP_F3_A_EmployerForm_item",
                    "ADAP_F3_B_Recipient_item",
                    "ADAP_F3_B_IncomeTypeDrop_item",
                    "ADAP_F3_B_Employer_item",
                    "ADAP_F3_B_EmployStart_item",
                    "ADAP_F3_B_TempYN_item",
                    "ADAP_F3_B_IncomeTypeOther_item",
                    "ADAP_F3_B_IncomeAmt_item",
                    "ADAP_F3_B_EmployerForm_item",
                    "ADAP_F3_C_Recipient_item",
                    "ADAP_F3_C_IncomeTypeDrop_item",
                    "ADAP_F3_C_Employer_item",
                    "ADAP_F3_C_EmployStart_item",
                    "ADAP_F3_C_TempYN_item",
                    "ADAP_F3_C_IncomeTypeOther_item",
                    "ADAP_F3_C_IncomeAmt_item",
                    "ADAP_F3_C_EmployerForm_item",
                    "ADAP_F3_D_Recipient_item",
                    "ADAP_F3_D_IncomeTypeDrop_item",
                    "ADAP_F3_D_Employer_item",
                    "ADAP_F3_D_EmployStart_item",
                    "ADAP_F3_D_TempYN_item",
                    "ADAP_F3_D_IncomeTypeOther_item",
                    "ADAP_F3_D_IncomeAmt_item",
                    "ADAP_F3_D_EmployerForm_item",
                    "ADAP_cert_NoSharing_item",
                    "ADAP_cert_Reminders_item"
                    #endregion
                };

                Applications appl = new Applications(formsRepo);
                def_FormResults frmRes = appl.CreateFormResultPopulatedFromUAS(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.GroupID, userId, frm.formId, ItemsToPopulateFromUAS);
                appl.PopulateItemsFromPrevApplication(frmRes, ItemIdentifiersToPopulateFromPrevApplication);

                frmRes.statusChangeDate = DateTime.Now;

                // Save the FormResult, ItemResults, and ResponseVariables
                int newFormResultId = 0;
                try
                {
                    newFormResultId = formsRepo.AddFormResult(frmRes);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("AddFormResult exception:" + ex.Message);
                }

                Debug.WriteLine("AddFormResult newFormResultId:" + newFormResultId.ToString());

                // query the ramsell system and update the formResult responses based on Ramsell response
                def_ResponseVariables rvMemberId = formsRepo.GetResponseVariablesByFormResultIdentifier(newFormResultId, "ADAP_D9_Ramsell");
                if (rvMemberId != null && !String.IsNullOrWhiteSpace(rvMemberId.rspValue))
                {
                    string memberId = rvMemberId.rspValue;
                    string usrId = UAS.Business.UAS_Business_Functions.GetEntAppConfigAdap("USR");
                    string password = UAS.Business.UAS_Business_Functions.GetEntAppConfigAdap("PWD");
                    Debug.WriteLine("Ramsell UseriD / Password: " + usrId + @" / " + password);

                    string token = Ramsell.GetOauthToken(usrId, password);
                    new RamsellImport(formsRepo, frmRes.formResultId).ImportApplication(token, memberId);
                }
                //AJBoggs.Adap.Services.Api.Ramsell.PopulateItemsFromRamsellImport(formsRepo, frmRes);

                if (SessionHelper.SessionForm == null)
                {
                    SessionHelper.SessionForm = new SessionForm();
                }

                SessionForm sf = SessionHelper.SessionForm;
                sf.formId = frm.formId;
                sf.formIdentifier = frm.identifier;
                sf.sectionId = formsRepo.GetSectionByIdentifier("ADAP_demographic").sectionId;
                sf.partId = formsRepo.GetPartByFormAndIdentifier(frm, "ADAP").partId;

                // *** RRB - should be deprecated - use SessionForm
                // *** BR - line 359 of the ResultsController calls this session variable, so it must be set to prevent an exception.
                //  Other parts of the application may still use that variable, so changing it in the ResultsController may break something else.
                Session["part"] = sf.partId;
                // Should have the partId also - may not be required.

                sf.formResultId = newFormResultId;

                return RedirectToAction("Template", "Results", new { sectionId = sf.sectionId.ToString() });
            }
            else
            {
                return RedirectToAction("AdapPortal", "COADAP", new { userId = userId, error = "Not Approved" });
            }
        }

        /// <summary>
        /// Used to skip to final submission screens without validation, 
        /// only after the user has been warned and yet they selected to "submit anyways" on the ValidationErrors.cshtml screen
        /// </summary>
        /// <returns></returns>
        public ActionResult SkipToFinalCert()
        {
            // *** RRB 4/7/16 
            // Is there a reason why 'RedirectToAction' can't be used here ??
            // return RedirectToAction("Template", "Results", new { sectionId = sf.sectionId.ToString() });

            ResultsController rc = new ResultsController(formsRepo);
            rc.ControllerContext = ControllerContext;
            int sectionId = formsRepo.GetSectionByIdentifier("ADAP_cert").sectionId;
            return rc.Template(sectionId);
        }

        public bool FinalSubmit(FormCollection frmCllctn, TemplateItems itm)
        {
            int formResultId = SessionHelper.SessionForm.formResultId;

            //run save (update/add response variables based on form collection)
            ResultsController rc = new ResultsController(formsRepo);
            rc.ControllerContext = ControllerContext;
            rc.Save(frmCllctn, itm);

            //run status update
            int status = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "NEEDS_REVIEW").sortOrder);
            
            AdapController ac = new AdapController(formsRepo);
            ac.ControllerContext = ControllerContext;
            ac.StatusUpdated("", formResultId, status, false );

            //return Redirect("~/ADAP");
            return true;
        }

        public ActionResult CloseApplication(FormCollection frmCllctn, TemplateItems itm)
        {
            return Redirect("~/ADAP");
        }

        public ActionResult Validate( FormCollection frmCllctn, TemplateItems ti )
        {
            //save response from formCollection into the DB
            ResultsController rc = new ResultsController(formsRepo);
            rc.ControllerContext = ControllerContext;
            rc.Save(frmCllctn, ti);

            int formResultId = SessionHelper.SessionForm.formResultId;
            SessionHelper.SessionForm.sectionId = -1;

            //Run generic validation
            AdapValidationErrorsModel model = new AdapValidationErrorsModel();
            model.navMenuModel = AJBoggs.Adap.Templates.TemplateMenus.getAdapNavMenuModel(SessionHelper.SessionForm, formsRepo);
            model.validationMessages = new List<string>();
            model.missingItemVariablesBySectionByPart = new Dictionary<int, Dictionary<int, List<string>>>();
            def_Forms frm = formsRepo.GetFormById(SessionHelper.SessionForm.formId);
            List<ValuePair> allResponses = CommonExport.GetDataByFormResultId(formResultId);
            SharedValidation sv = new SharedValidation(allResponses);
            bool invalid = sv.DoGenericValidation(formsRepo, model, frm);

            //transfer generic validation results to an ADAP-specific model 
            model.titlesOfMissingSubsectionsBySectionByPart = new Dictionary<int, Dictionary<int, List<string>>>();
            foreach (int prtId in model.missingItemVariablesBySectionByPart.Keys)
            {
                model.titlesOfMissingSubsectionsBySectionByPart.Add(prtId, new Dictionary<int, List<string>>());
                foreach (int sctId in model.missingItemVariablesBySectionByPart[prtId].Keys)
                {
                    def_Sections sct = formsRepo.GetSectionById(sctId);
                    formsRepo.SortSectionItems(sct);
                    List<int> ssSectionIds = new List<int>();
                    foreach (def_SectionItems si in sct.def_SectionItems.Where(si => si.subSectionId.HasValue))
                        ssSectionIds.Add( formsRepo.GetSubSectionById(si.subSectionId.Value).sectionId );

                    model.titlesOfMissingSubsectionsBySectionByPart[prtId].Add(sctId, new List<string>());
                    foreach (string itemVariableIdent in model.missingItemVariablesBySectionByPart[prtId][sctId])
                    {
                        //for each item variable identifier returned by generic validation,
                        //lookup the corresponding subsection's title for display on the ADAP "validation errors" screen
                        def_ItemVariables   iv  = formsRepo.GetItemVariableByIdentifier( itemVariableIdent );
                        def_Items           itm = iv  == null ? null : iv.def_Items;
                        def_SectionItems    si  = itm == null ? null : formsRepo.getSectionItemsForItem(itm).Where(sit => ssSectionIds.Contains(sit.sectionId)).FirstOrDefault();
                        def_Sections        sub = si  == null ? null : si.def_Sections;
                        if (sub != null && !model.titlesOfMissingSubsectionsBySectionByPart[prtId][sctId].Contains(sub.title))
                            model.titlesOfMissingSubsectionsBySectionByPart[prtId][sctId].Add(sub.title);
                    }
                }
            }

            //run CO-ADAP one-off validation
            if (AdapCOOneOffValidation.RunOneOffValidation(sv, model))
                invalid = true;

            //return the validation errors screen if any errors were found
            //model.validationMessages could have some warnings even if no errors were found, in which case "invalid" would be false here
            if (invalid)
            {
                return View("~/Views/COADAP/ValidationErrors.cshtml", model);
            }

            //if no problems were found, or if warnings were found but no errors, return the next section in the application
            if (model.validationMessages.Count == 0)
                model.validationMessages.Add("No errors were found");
            int sectionId = Convert.ToInt16(frmCllctn["navSectionId"]);
            return rc.Template(sectionId, model.validationMessages);
        }

        [HttpGet]
        public FileContentResult DownloadAttachment(string ivIdent, int formResultId)
        {
            // * * * OT 3-10-16 removed authentication so attachments can be downloaded from links in PDFs (Bug 13051)

            //if (!SessionHelper.IsUserLoggedIn)
            //{
            //    string msg = "User not logged into application.";
            //    // byte[] emptyFile = new byte[] {0x20, 0x00};
            //    return new FileContentResult(Encoding.ASCII.GetBytes(msg), "text/html");
            //}

            FileContentResult result;
            try
            {
                string serverPath = formsRepo.GetResponseVariablesByFormResultIdentifier(
                    formResultId, ivIdent).rspValue;

                result = File(System.IO.File.ReadAllBytes(serverPath), 
                    System.Net.Mime.MediaTypeNames.Application.Octet,
                    Path.GetFileName( serverPath ) );
            }
            catch (Exception xcptn)
            {
                string msg = "COADAPController.DownloadAttachment failed.  Exception: " + xcptn.Message;
                result = new FileContentResult(Encoding.ASCII.GetBytes(msg), "text/html");
            }

            return result;
        }
    
        /// <summary>
        /// Builds a condensed report of all ADAP applicationd data.
        /// *** NOTE: output file is put in the /Content directory - need permissions from SysAdmin.
        ///             In addition, Product or Project Manager must accept the security risk (HIPAA data on web accessible directory).
        /// </summary>
        /// <returns>PDF file</returns>
        [HttpGet]
        public FileContentResult BuildPDFReport( int? formResultId = null )
        {
            Debug.WriteLine("* * *  COADAPController:BuildPDFReport method  * * *");
            if (!SessionHelper.IsUserLoggedIn)
            {
                string msg = "User not logged into application.";
                // byte[] emptyFile = new byte[] {0x20, 0x00};
                return new FileContentResult(Encoding.ASCII.GetBytes(msg), "text/html");
            }

            if (SessionHelper.SessionForm == null)
            {
                SessionHelper.SessionForm = new SessionForm();
            }

            if( formResultId == null )
                formResultId = SessionHelper.SessionForm.formResultId;

            try
            {
                AccessLogging.InsertAccessLogRecord(formsRepo, formResultId.Value, (int)AccessLogging.accessLogFunctions.REPORT, "Generated Report");
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("Access Logging error: " + xcptn.Message);
                mLogger.Error("Access Logging error: " + xcptn.Message);
            }

            string outpath = ControllerContext.HttpContext.Server.MapPath("../Content/report_" + System.DateTime.Now.Ticks + ".pdf");
            FormResultPdfReport report = new AdapPdfReport(formsRepo, formResultId.Value, outpath, false);
            report.BuildReport();

            //output to file locally, and stream to end-user
            FileContentResult result;
            try
            {
                report.outputToFile();
                result = File(System.IO.File.ReadAllBytes(outpath), "application/pdf", "report.pdf");
                System.IO.File.Delete(outpath);
            }
            catch (Exception xcptn)
            {
                string msg = "Build Report output failed.  Exception: " + xcptn.Message;
                mLogger.Error(msg);
                result = new FileContentResult(Encoding.ASCII.GetBytes(msg), "text/html");
            }

            return result;
        }

        public ActionResult GridPage()
        {
            return View();
        }

    }
}