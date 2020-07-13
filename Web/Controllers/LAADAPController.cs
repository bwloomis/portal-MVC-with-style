using Assmnts.Business;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Assmnts.Reports;
using Assmnts.UasServiceRef;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Mvc;
using System.Linq;
using System.Web.Configuration;
using AJBoggs.Adap.Services.Xml;
using AJBoggs.Adap.Services.Api;
using AJBoggs.Def.Services;
using AJBoggs.Adap.Domain;
using System.Net.Http;
using Assmnts.Business.Uploads;

namespace Assmnts.Controllers
{
    [ADAPRedirectingAction]
    public class LAADAPController : Controller
    {

        private IFormsRepository formsRepo;

        public LAADAPController(IFormsRepository fr)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
        }

        public ActionResult Index()
        {
            if (UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts"))
            {
                return RedirectToAction("ApplicationsPending", "ADAP");
            }
            else
            {
                int mainAppFormId = formsRepo.GetFormByIdentifier("LA-ADAP").formId;
                int stubAppFormId = formsRepo.GetFormByIdentifier("LA-ADAP-Stub").formId;
                bool isCaseMgr = UAS.Business.UAS_Business_Functions.hasPermission(2, "RptsExpts"); // Case Manager permission

                IQueryable<vFormResultUser> query = formsRepo.GetFormResultsWithSubjects(
                    SessionHelper.LoginStatus.EnterpriseID,
                    new int[] { mainAppFormId, stubAppFormId });

                int userId = SessionHelper.LoginStatus.UserID;
                query = query.Where(fr => fr.subject == userId);

                string actionName = (query.Count() > 0 || isCaseMgr) ? "AdapPortal" : "CreateAdapApplication";
                return RedirectToAction(actionName, "LAADAP", new { userId = userId } );
            }
        }


        /// <summary>
        /// The main page loaded for non-staff users.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult AdapPortal()
        {

            //* * * OT 3-18-16
            //create instance of the "base" Adap controller to reference its shared functions
            //these functions should be moved to adap domain so AdapController won't need to be referenced here
            AdapController adapCtrl = new AdapController(formsRepo);
            adapCtrl.ControllerContext = ControllerContext;

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
            bool isCaseMgr = UAS.Business.UAS_Business_Functions.hasPermission(2, "RptsExpts"); // Case Manager permission

            if (isCaseMgr)
            {
                // Case Managers will no longer be creating apps for clients.
                //string clientUserId = Session["clientUserId"] as string;
                //if (clientUserId != null)
                //{
                //    try
                //    {
                //        int cuId = Int32.Parse(clientUserId);
                //        if (cuId > 0)
                //        {
                //            Session["clientUserId"] = null;
                //            return RedirectToAction("CreateAdapApplication", new { userId = cuId } );
                //        }
                //    }
                //    catch (Exception excptn)
                //    {
                //        Debug.WriteLine("* * * AdapPortal clientUserId exception: " + excptn.Message);
                //    }
                //}

                // Check existing formResults to ensure each user is still assigned to this CM.
                CheckAndRemoveCaseManager();                
            }

            IQueryable<vFormResultUser> query = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, 6);
            int? soInt = formsRepo.GetStatusDetailByMasterIdentifier(1, "APPROVED").sortOrder;
            if (isCaseMgr)
            {
                query = query.Where(q => q.interviewer == uId);
            }
            else
            {
                query = query.Where(q => (q.subject == uId) && (q.formStatus == soInt));
            }

            String recert = String.Empty;
            if (!isCaseMgr)
            {
                if (query.Count() > 0)
                {
                    DateTime dob = Convert.ToDateTime(ud.DOB);
                    recert = new Applications(formsRepo).GetRecert(dob, query.Count(), Convert.ToDateTime(query.OrderByDescending(q => q.statusChangeDate).Select(q => q.statusChangeDate).FirstOrDefault())).ToString("MMMM yyyy");
                }
                else
                {
                    recert = "None Pending";
                }
            }

            AdapPortal ap = new AdapPortal()
            {
                Name = ud.FirstName + " " + ud.LastName,
                RecertDate = recert,
                UserId = uId.ToString(),
                errorMsg = err,
                EnterpriseID = SessionHelper.LoginStatus.EnterpriseID
            };

            return View("~/Views/LA_ADAP/LA_AdapPortal.cshtml", ap);
        }

        [HttpGet]
        public ActionResult AppsHistory()
        {
            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ReportName = "History";
            aar1.ShowFullHistory = true;


            AdapController ac = new AdapController(formsRepo);
            ac.ControllerContext = ControllerContext;
            return ac.Report1(aar1);
        }

        /// <summary>
        /// Sends user to SecureEmail (through UAS)
        /// 
        /// If recipients is non-null, the user will be redirected to a "compose" screen with those recipients in the "to:" line.
        /// 
        /// Without any parameters, this will redirect to the logged-in user's inbox- same result as (in CO-ADAP) "exit to portal menu" -> "Secure Email"
        /// 
        /// Used for the "Secure Email" link in the LA-ADAP nav menu
        /// </summary>
        /// <param name="recipients">comm-separated list of recipient userIds</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult RedirectToSecureEmail( string recipients = null )
        {
            string basePortalUrl = WebConfigurationManager.AppSettings["UASAdminURL"];
            string redirectUrl = basePortalUrl + @"/Portal/RedirectToSecureEmail";
            if (!String.IsNullOrWhiteSpace(recipients))
            {
                redirectUrl += (redirectUrl.Contains("?") ? "&" : "?") + "recipients=" + recipients;
            }
            return Redirect( redirectUrl );
        }


        /// <summary>
        /// Sends user to UAS user administration
        /// 
        /// Same result as (in CO-ADAP) "exit to portal menu" -> "User Administration"
        /// 
        /// Used for the "Administration" link in the LA-ADAP nav menu
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult RedirectToUserAdmin()
        {
            string basePortalUrl = WebConfigurationManager.AppSettings["UASAdminURL"];
            return Redirect(basePortalUrl + @"/Site/Limited/limited_Users");
        }


        /// <summary>
        /// Redirects the logged-in user to their UAS user profile
        /// 
        /// Same result as (in CO-ADAP) "exit to portal menu" -> "Profile"
        /// 
        /// Used for the "Logout" link in the LA-ADAP nav menu
        /// </summary>
        /// <returns></returns>
        public ActionResult RedirectToUserProfile()
        {
            string basePortalUrl = WebConfigurationManager.AppSettings["UASAdminURL"];
            return Redirect(basePortalUrl + @"/UserProfile/UserProfile");
        }


        /// <summary>
        /// Redirects the logged-in user to their UAS user profile
        /// 
        /// Same result as (in CO-ADAP) "exit to portal menu" -> "Profile"
        /// 
        /// Used for the "Logout" link in the LA-ADAP nav menu
        /// </summary>
        /// <returns></returns>
        public ActionResult RedirectToCaseManagerProfile()
        {
            string basePortalUrl = WebConfigurationManager.AppSettings["UASAdminURL"];
            return Redirect(basePortalUrl + @"/UserProfile/CaseManagerProfile");
        }

        /// <summary>
        /// Logs user out of UAS
        /// 
        /// Same result as (in CO-ADAP) "exit to portal menu" -> "Logout"
        /// 
        /// Used for the "Logout" link in the LA-ADAP nav menu
        /// </summary>
        /// <returns></returns>
        public ActionResult RedirectToUasLogout()
        {
            string basePortalUrl = WebConfigurationManager.AppSettings["UASAdminURL"];
            return Redirect(basePortalUrl + @"/Account/ssoLogout");
        }

        /// <summary>
        /// Creates a new ADAP application, attempting to pull as much data from the previous application and from UAS as possible.
        /// New applications are only permitted when there is no previous applicatoin, or the previous application was approved.
        /// </summary>
        /// <returns>Redirects the user to the Application pages.</returns>
        [HttpGet]
        public ActionResult CreateAdapApplication()
        {
            def_Forms frm = formsRepo.GetFormByIdentifier("LA-ADAP");
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

            bool isCaseMgr = UAS.Business.UAS_Business_Functions.hasPermission(2, "RptsExpts"); // Case Manager permission

            int? intSO = formsRepo.GetStatusDetailByMasterIdentifier(1, "CANCELLED").sortOrder;

            def_FormResults prevRes = formsRepo.GetEntities<def_FormResults>(f => f.subject == userId && intSO != null && f.formStatus != intSO).OrderByDescending(f => f.dateUpdated).FirstOrDefault();
            //def_FormResults prevRes = frm.def_FormResults.Where(f => f.subject == userId && intSO != null && f.formStatus != intSO).OrderByDescending(f => f.dateUpdated).FirstOrDefault();

            if ((prevRes == null) || (prevRes.formStatus == formsRepo.GetStatusDetailByMasterIdentifier(1, "APPROVED").sortOrder))
            {
                AuthenticationClient webclient = new AuthenticationClient();
                UserDisplay ud = webclient.GetUserDisplay(userId);

                //Dictionary<string, string> ItemsToPopulateFromUAS = new Dictionary<string, string>();
                //ItemsToPopulateFromUAS.Add(
                //    "ADAP_D1_FirstName_item", String.IsNullOrEmpty(ud.FirstName) ? String.Empty : ud.FirstName);
                //ItemsToPopulateFromUAS.Add(
                //    "ADAP_D1_LastName_item", String.IsNullOrEmpty(ud.LastName) ? String.Empty : ud.LastName);

                //ItemsToPopulateFromUAS.Add(
                //    "ADAP_D2_DOB_item", ud.DOB.HasValue == false ? String.Empty : ud.DOB.Value.ToShortDateString());

                //if (ud.Addresses.Any())
                //{
                //    ItemsToPopulateFromUAS.Add(
                //                        "ADAP_C1_Address_item", String.IsNullOrEmpty(ud.Addresses[0].Address1) ? String.Empty 
                //                        : ud.Addresses[0].Address1);
                //    ItemsToPopulateFromUAS.Add(
                //                       "LA_ADAP_AddrAptNum_item", String.IsNullOrEmpty(ud.Addresses[0].Address2) ? String.Empty
                //                       : ud.Addresses[0].Address2);
                //    ItemsToPopulateFromUAS.Add(
                //                       "ADAP_C1_City_item", String.IsNullOrEmpty(ud.Addresses[0].City) ? String.Empty
                //                       : ud.Addresses[0].City);
                //    ItemsToPopulateFromUAS.Add(
                //                       "ADAP_C1_State_item", String.IsNullOrEmpty(ud.Addresses[0].State) ? String.Empty
                //                       : ud.Addresses[0].State);
                //    ItemsToPopulateFromUAS.Add(
                //                       "ADAP_C1_Zip_item", String.IsNullOrEmpty(ud.Addresses[0].ZIP) ? String.Empty
                //                       : ud.Addresses[0].ZIP);
                //}

                //if (ud.Emails.Any())
                //{
                //    ItemsToPopulateFromUAS.Add(
                //                        "LA_ADAP_Email_Adr", String.IsNullOrEmpty(ud.Emails[0].Email) ? String.Empty
                //                        : ud.Emails[0].Email);
                //}
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

              

                Applications appl = new Applications(formsRepo);
                def_FormResults frmRes = appl.CreateFormResultPopulatedFromUAS(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.GroupID, userId, frm.formId, ItemsToPopulateFromUAS);

                if (isCaseMgr)
                {
                    frmRes.interviewer = SessionHelper.LoginStatus.UserID;
                }
                else if (ud.ManagerID != null)
                {
                    frmRes.interviewer = ud.ManagerID;
                }

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

                if (SessionHelper.SessionForm == null)
                {
                    SessionHelper.SessionForm = new SessionForm();
                }

                SessionForm sf = SessionHelper.SessionForm;
                sf.formId = frm.formId;
                sf.formIdentifier = frm.identifier;
                sf.sectionId = formsRepo.GetSectionByIdentifier("LA_ADAP_PreScreen").sectionId;
                sf.partId = formsRepo.GetPartByFormAndIdentifier(frm, "LA-ADAP").partId;

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
                if (isCaseMgr)
                {
                    userId = SessionHelper.LoginStatus.UserID;
                }
                return RedirectToAction("AdapPortal", "LAADAP", new { userId = userId, error = "Not Approved" });
            }
        }

        public def_FormResults CreateMagiForm(int? subject)
        {
            def_Forms f = formsRepo.GetFormByIdentifier("LA-ADAP-MAGI");
            def_FormResults magi = new def_FormResults()
            {
                formId = f.formId,
                formStatus = 0,
                dateUpdated = DateTime.Now,
                EnterpriseID = SessionHelper.LoginStatus.EnterpriseID,
                GroupID = SessionHelper.LoginStatus.GroupID,
                subject = subject,
                interviewer = SessionHelper.LoginStatus.UserID,
                statusChangeDate = DateTime.Now,
                LastModifiedByUserId = SessionHelper.LoginStatus.UserID
            };

            magi.formResultId = formsRepo.AddFormResult(magi);

            return magi;
        }

        public ActionResult SaveStubApplication(FormCollection frmCllctn, TemplateItems itm)
        {
            //run normal save (update/add response variables based on form collection)
            ResultsController rc = new ResultsController(formsRepo);
            rc.ControllerContext = ControllerContext;
            ActionResult ar = rc.Save(frmCllctn, itm);

            //update or add UAS_User based on responses
            int formResultId = SessionHelper.SessionForm.formResultId;
            new AdapLAStubApp(formsRepo).UpdateUASFromStubApp(formResultId);

            return ar;
        }

        /// <summary>
        /// Creates a new LA-ADAP Stub application for the given ramsellId
        /// 
        /// This may involve inserting a new UAS_User.
        /// </summary>
        /// <returns>Redirects the user to the first section view for the newly-created stub application</returns>
        [HttpGet]
        public ActionResult CreateStubApplication( string ramsellId )
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            if (SessionHelper.SessionForm == null)
            {
                SessionHelper.SessionForm = new SessionForm();
            }

            //createa new def_FormResult for the stub application
            AdapLAStubApp stubHelper = new AdapLAStubApp( formsRepo );
            int newFrId = stubHelper.CreateAndPrepopulateNewStubApp( ramsellId );
            def_Forms stubForm = stubHelper.GetStubForm();

            //setup session vars for viewing stub application screen(s)
            SessionHelper.SessionForm.formId = stubForm.formId;
            SessionHelper.SessionForm.formResultId = newFrId;
            SessionHelper.SessionForm.formIdentifier = AdapLAStubApp.stubFormIdentifier;
            SessionHelper.LoginStatus.EnterpriseID = SessionHelper.LoginStatus.EnterpriseID;

            def_Parts prt = formsRepo.GetFormParts(stubForm)[0];
            SessionHelper.SessionForm.partId = prt.partId;
            Session["part"] = prt.partId;

            def_Sections sct = formsRepo.GetSectionsInPart(prt)[0];
            SessionHelper.SessionForm.sectionId = sct.sectionId;
            Session["section"] = sct.sectionId;

            //redirect to first section in newly-created stub app
            return RedirectToAction("Template", "Results", new { 
                sectionId = SessionHelper.SessionForm.sectionId.ToString(),
                partId = SessionHelper.SessionForm.partId.ToString()
            });
        }

        public ActionResult TestStubApp()
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            if (SessionHelper.SessionForm == null)
            {
                SessionHelper.SessionForm = new SessionForm();
            }

            // retrieve and set SessionForm params
            string formIdent = "LA-ADAP-Stub", entName = "Louisiana";
            def_Forms frm = formsRepo.GetFormByIdentifier(formIdent);
            Enterprise ent = new AuthenticationClient().GetEnterpriseByName(entName);
            if (frm == null)
                return Content("Could not find form with identifier \"" + formIdent + "\"");
            if (ent == null)
                return Content("Could not find enterprise with name \"" + entName + "\"");
            def_FormResults fr = formsRepo.GetFormResultsByFormId(frm.formId).FirstOrDefault();
            if (fr == null)
            {
                fr = new def_FormResults()
                {
                    formId = frm.formId,
                    formStatus = 0,
                    sessionStatus = 0,
                    dateUpdated = DateTime.Now,
                    deleted = false,
                    locked = false,
                    archived = false,
                    EnterpriseID = ent.EnterpriseID,
                    GroupID = 0,
                    subject = 0,
                    interviewer = 0,
                    assigned = 0,
                    training = false,
                    reviewStatus = 0,
                    statusChangeDate = DateTime.Now
                };
                formsRepo.AddFormResult(fr);
            }


            SessionHelper.SessionForm.formId = frm.formId;
            SessionHelper.SessionForm.formResultId = fr.formResultId;
            SessionHelper.SessionForm.formIdentifier = frm.identifier;
            SessionHelper.LoginStatus.EnterpriseID = ent.EnterpriseID;

            def_Parts prt = formsRepo.GetFormParts(frm)[0];
            SessionHelper.SessionForm.partId = prt.partId;
            Session["part"] = prt.partId;

            def_Sections sct = formsRepo.GetSectionsInPart(prt)[0];
            SessionHelper.SessionForm.sectionId = sct.sectionId;
            Session["section"] = sct.sectionId;

            return RedirectToAction("Template", "Results", new
            {
                sectionId = SessionHelper.SessionForm.sectionId.ToString(),
                partId = SessionHelper.SessionForm.partId.ToString()
            });
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
            SharedValidation sv = new SharedValidation(allResponses );
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
                        ssSectionIds.Add(formsRepo.GetSubSectionById(si.subSectionId.Value).sectionId);

                    model.titlesOfMissingSubsectionsBySectionByPart[prtId].Add(sctId, new List<string>());
                    foreach (string itemVariableIdent in model.missingItemVariablesBySectionByPart[prtId][sctId])
                    {
                        //for each item variable identifier returned by generic validation,
                        //lookup the corresponding subsection's title for display on the ADAP "validation errors" screen
                        def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(itemVariableIdent);
                        def_Items itm = iv == null ? null : iv.def_Items;
                        def_SectionItems si = itm == null ? null : formsRepo.getSectionItemsForItem(itm).Where(sit => ssSectionIds.Contains(sit.sectionId)).FirstOrDefault();
                        def_Sections sub = si == null ? null : si.def_Sections;
                        if (sub != null && !model.titlesOfMissingSubsectionsBySectionByPart[prtId][sctId].Contains(sub.title))
                            model.titlesOfMissingSubsectionsBySectionByPart[prtId][sctId].Add(sub.title);
                    }
                }
            }

            //run LA-ADAP one-off validation
            if (AdapLAOneOffValidation.RunOneOffValidation(formsRepo, sv, model))
                invalid = true;

            if (invalid)
            {
                return View("~/Views/LA_ADAP/ValidationErrors.cshtml", model);
            }

            //formsRepo.FormResultComplete(formsRepo.GetFormResultById(formResultId));

            if (model.validationMessages.Count == 0)
                model.validationMessages.Add("No errors were found");

            int sectionId = Convert.ToInt16(frmCllctn["navSectionId"]);
            return rc.Template(sectionId, model.validationMessages);

            //return Redirect("~/Results/Template" 
            //    + "?partId=" + frmCllctn["navPartId"] 
            //    + "&sectionId=" + frmCllctn["navSectionId"]
            //    + "&message=" + "No errors were found" );
        }

        /// <summary>
        /// Builds a condensed report of all ADAP applicationd data.
        /// *** NOTE: output file is put in the /Content directory - need permissions from SysAdmin.
        ///             In addition, Product or Project Manager must accept the security risk (HIPAA data on web accessible directory).
        /// </summary>
        /// <returns>PDF file</returns>
        [HttpGet]
        public FileContentResult BuildPDFReport(int? formResultId = null)
        {
            Debug.WriteLine("* * *  LAADAPController:BuildPDFReport method  * * *");
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

            if (formResultId == null)
                formResultId = SessionHelper.SessionForm.formResultId;

            try
            {
                AccessLogging.InsertAccessLogRecord(formsRepo, formResultId.Value, (int)AccessLogging.accessLogFunctions.REPORT, "Generated Report");
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("Access Logging error: " + xcptn.Message);
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
                result = new FileContentResult(Encoding.ASCII.GetBytes(msg), "text/html");
            }

            return result;
        }

        [HttpPost]
        public ActionResult RetrieveMagi(int index)
        {
            //Use the index and the current formResultId to pull the fileAttachment record to find the formResultId of the MAGI form.
            int fileId = -1;
            int RelatedEnumId = formsRepo.GetRelatedEnumIdByEnumDescription("formResultId");
            int AttachTypeId = formsRepo.GetAttachTypeIdByAttachDescription("Def Form");

            string attachedForm = findMagi(index, RelatedEnumId, AttachTypeId);

            if (String.IsNullOrEmpty(attachedForm))
            {
                def_FormResults fr = formsRepo.GetFormResultById(SessionHelper.SessionForm.formResultId);

                def_FormResults magi = CreateMagiForm(fr.subject);
                attachedForm = magi.formResultId.ToString();

                def_FileAttachment fa = new def_FileAttachment() { 
                    EnterpriseId = SessionHelper.LoginStatus.EnterpriseID,
                    GroupId = SessionHelper.LoginStatus.GroupID,
                    UserId = fr.subject,
                    AttachTypeId = AttachTypeId,
                    RelatedId = fr.formResultId,
                    RelatedEnumId = RelatedEnumId,
                    displayText = index + "/" + attachedForm,
                    FilePath = index + "/" + attachedForm,
                    FileName = attachedForm,
                    StatusFlag = "A",
                    CreatedDate = DateTime.Now,
                    CreatedBy = SessionHelper.LoginStatus.UserID
                };
                FileUploads.CreateDataAttachment(formsRepo, SessionHelper.SessionForm.formResultId, fa);
            }

            // Save the current form data as a session variable
            SessionForm sf = new SessionForm();
            sf.formId = SessionHelper.SessionForm.formId;
            sf.formResultId = SessionHelper.SessionForm.formResultId;
            sf.partId = SessionHelper.SessionForm.partId;
            sf.sectionId = SessionHelper.SessionForm.sectionId;
            sf.itemId = SessionHelper.SessionForm.itemId;
            Session.Add("ParentFormData", sf);

            return RedirectToAction("ToTemplate", "Adap", new { formResultId = attachedForm });
        }

        [HttpPost]
        public bool validMagi(int index)
        {
            int RelatedEnumId = formsRepo.GetRelatedEnumIdByEnumDescription("formResultId");
            int AttachTypeId = formsRepo.GetAttachTypeIdByAttachDescription("Def Form");

            string attachedForm = findMagi(index, RelatedEnumId, AttachTypeId);

            // In theory there would be actual validation here.

            return !String.IsNullOrEmpty(attachedForm);
        }

        private string findMagi(int index, int RelatedEnumId, int AttachTypeId)
        {
            string attachedForm = "";
            Dictionary<int, string> magiAttached = FileUploads.RetrieveFileDisplayTextsByRelatedId(formsRepo, SessionHelper.SessionForm.formResultId, RelatedEnumId, "A", AttachTypeId);
            foreach (var k in magiAttached.Keys)
            {
                string[] magi = magiAttached[k].Split('/');
                if (Convert.ToInt32(magi[0]) == index)
                {
                    attachedForm = magi[1];
                }
            }

            return attachedForm;
        }

        [HttpPost]
        public string DataTableCSSPeopleList(int draw, int start, int length, string searchValue, string searchRegex,
                                        string order)
        {
            string result = String.Empty;
            Debug.WriteLine("DataTableController UserList draw:" + draw.ToString());
            Debug.WriteLine("DataTableController UserList start:" + start.ToString());
            Debug.WriteLine("DataTableController UserList searchValue:" + searchValue);
            Debug.WriteLine("DataTableController UserList searchRegex:" + searchRegex);
            Debug.WriteLine("DataTableController UserList order:" + order);

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

                var sFirstN = Request.Params["columns[1][search][value]"];
                var sLastN = Request.Params["columns[2][search][value]"];
                var sloginId = Request.Params["columns[3][search][value]"];
                var sEmail = Request.Params["columns[4][search][value]"];

                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;


                // Parse the parameters from DataTable request
                /*
                if (!String.IsNullOrEmpty(json))
                {
                    var dResults = fastJSON.JSON.Parse(json);
                }
                */

                AuthenticationClient auth = new AuthenticationClient();

                int entId = 8;
                int appId = 3;

                List<List<string>> usrs = auth.List_CSS_people_Users(SessionHelper.LoginStatus.UserID, entId, iniIndex, noDsplyRecs,
                    sFirstN, sLastN, sloginId, sEmail);

                //* * * OT 2-23-16 explaination for Bug 13019, comment 21, first bullet
                //for users that do not have the "View users" permission, they can only see themselves in the limited users list
                //we do this by searching for the current user's loginId
                //this normally works, but in some cases there are multiple loginIds that contain the current user's loginId 
                //Example: Login as nctest and you can also see NCTest1.


                //Final record stores total result set count.
                if (usrs.Count > 0)
                {
                    dtr.recordsTotal = dtr.recordsFiltered = Convert.ToInt32(usrs[usrs.Count - 1][0]);//usrCount;
                    usrs.RemoveAt(usrs.Count - 1);
                    Debug.WriteLine("DataTableController usrs.Count:" + usrs.Count.ToString());
                }
                else
                {
                    dtr.recordsTotal = dtr.recordsFiltered = 0;
                    Debug.WriteLine("DataTableController usrs.Count:" + 0);
                }
                dtr.data = (from u in usrs
                            select u.ToArray()).ToList();

                Debug.WriteLine("DataTableController UserList data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                Debug.WriteLine("DataTableController UserList result:" + result);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("DataTableController UserList exception:" + excptn.Message);
                result = excptn.Message + " - " + excptn.InnerException.Message;
            }

            return result;

        }

        [HttpPost]
        public ActionResult UpdateAssigned(int formResultId, int userId)
        {
            var formResult = formsRepo.GetFormResultById(formResultId);
            formResult.assigned = userId;
            formsRepo.Save();
            return Json(new { Success = true });
        }
        
        private void CheckAndRemoveCaseManager()
        {
            IQueryable<def_FormResults> frs = formsRepo.GetFormResultsByFormId(11).Where(fr => fr.interviewer == SessionHelper.LoginStatus.UserID);

            List<int> users = new List<int>();
            using (var uasContext = Data.Concrete.DataContext.getUasDbContext())
            {
                users = (from u in uasContext.uas_User
                         where u.ManagerID == SessionHelper.LoginStatus.UserID
                         select u.UserID).ToList();
            }

            List<int> invalidFormResults = (from fr in frs
                                            where !users.Any(u => u == fr.subject)
                                            select fr.formResultId).ToList();

            foreach (int formResultId in invalidFormResults)
            {
                def_FormResults fr = formsRepo.GetFormResultById(formResultId);
                fr.interviewer = fr.subject;
                formsRepo.SaveFormResults(fr);
            } 
        }
    }
}