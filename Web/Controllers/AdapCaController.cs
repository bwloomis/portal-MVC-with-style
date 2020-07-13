using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using AJBoggs.Adap.Domain;
using AJBoggs.Adap.Domain;
using AJBoggs.Def.Services;
using Assmnts.Business;
using Assmnts.Business.Uploads;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Assmnts.Reports;
using Assmnts.UasServiceRef;
using Data.Abstract;
using Newtonsoft.Json;
using NLog;
using UAS.Business;
using Assmnts.Business.Workflow;
using System.Web.Configuration;

namespace Assmnts.Controllers
{
    [ADAPRedirectingAction]
    public partial class AdapCaController : Controller
    {
        public static readonly List<String> dateSelectList =
                new List<String> {
								 "All",
								"Last Modified within 3 Months",
								"Last Modified within 1 Month",
								"Re-Certs Late",
								"Re-Certs Due within 7 Days",
								"Re-Certs Due within 1 Month",
								"Re-Certs Due within 2 Months",
								"Re-Certs Due within 3 Months"
		};

        private IFormsRepository formsRepo;
        private ILogger mLogger;
        private IAuthentication authClient;

        public AdapCaController(IFormsRepository fr)
        {
            formsRepo = fr;
            mLogger = LogManager.GetCurrentClassLogger();
            authClient = new AuthenticationClient();
        }

        public AdapCaController(ILogger logger, IFormsRepository fr, IAuthentication auth)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
            mLogger = logger;
            authClient = auth;
        }

        // GET: AdapCa
        public ActionResult Index()
        {
            return RedirectToAction("Report1", "Adap");
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
                    mLogger.Error(ex, "Adap Controller AdapPortal exception.");
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

            return View("~/Views/CAADAP/AdapPortal.cshtml", ap);
        }

        [HttpGet]
        public ActionResult CreateAdapApplication()
        {
            def_Forms frm = formsRepo.GetFormByIdentifier("CA-ADAP");
            //string userId = Request["userId"];
            int userId = 0;
            try
            {
                userId = Convert.ToInt32(Request["userId"]);
            }
            catch (Exception excptn)
            {
                mLogger.Error(excptn, "Adap Controller CreateAdapApplication exception:");
            }

            int? intSO = formsRepo.GetStatusDetailByMasterIdentifier(1, "CANCELLED").sortOrder;
            def_FormResults prevRes = frm.def_FormResults.Where(f => f.subject == userId && intSO != null && f.formStatus != intSO).OrderByDescending(f => f.dateUpdated).FirstOrDefault();

            if ((prevRes == null) || (prevRes.formStatus == formsRepo.GetStatusDetailByMasterIdentifier(1, "APPROVED").sortOrder))
            {
                AuthenticationClient webclient = new AuthenticationClient();
                UserDisplay ud = webclient.GetUserDisplay(userId);

                Dictionary<string, string> ItemsToPopulateFromUAS = new Dictionary<string, string>();
                ItemsToPopulateFromUAS.Add(
                        "C1_MemberFirstName_item", String.IsNullOrEmpty(ud.FirstName) ? String.Empty : ud.FirstName);
                ItemsToPopulateFromUAS.Add(
                        "C1_MemberLastName_item", String.IsNullOrEmpty(ud.LastName) ? String.Empty : ud.LastName);
                ItemsToPopulateFromUAS.Add(
                        "C1_MemberMiddleInitial_item", String.IsNullOrEmpty(ud.MiddleName) ? String.Empty : ud.MiddleName.Substring(0, 1));
                ItemsToPopulateFromUAS.Add(
                        "C1_MemberDateOfBirth_item", (ud.DOB == null) ? String.Empty : Convert.ToDateTime(ud.DOB).ToString("MM/dd/yyyy"));
                ItemsToPopulateFromUAS.Add(
                        "C1_ResidentialAddressLine1_item", (ud.Addresses != null && ud.Addresses.Count() > 0) ? ud.Addresses[0].Address1 : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_ResidentialAddressMayContact_item", (ud.Addresses != null && ud.Addresses.Count() > 0) ? ((ud.Addresses[0].MayContactAddress) ? "1" : "0") : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_ResidentialAddressCity_item", (ud.Addresses != null && ud.Addresses.Count() > 0) ? ud.Addresses[0].City : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_ResidentialAddressState_item", (ud.Addresses != null && ud.Addresses.Count() > 0) ? ud.Addresses[0].State : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_ResidentialAddressZIP_item", (ud.Addresses != null && ud.Addresses.Count() > 0) ? ud.Addresses[0].ZIP : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_MailingAddressLine1_item", (ud.Addresses != null && ud.Addresses.Count() > 1) ? ud.Addresses[1].Address1 : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_MailingAddressMayContact_item", (ud.Addresses != null && ud.Addresses.Count() > 1) ? ((ud.Addresses[1].MayContactAddress) ? "1" : "0") : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_MailingAddressCity_item", (ud.Addresses != null && ud.Addresses.Count() > 1) ? ud.Addresses[1].City : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_MailingAddressState_item", (ud.Addresses != null && ud.Addresses.Count() > 1) ? ud.Addresses[1].State : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_MailingAddressZIP_item", (ud.Addresses != null && ud.Addresses.Count() > 1) ? ud.Addresses[1].ZIP : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_DaytimePhoneNumber_item", (ud.Phones != null && ud.Phones.Count() > 0) ? ud.Phones[0].Phone : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_DaytimePhoneMayMessage_item", (ud.Phones != null && ud.Phones.Count() > 0) ? ((ud.Phones[0].MayContactPhone) ? "1" : "0") : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_AltPhoneNumber_item", (ud.Phones != null && ud.Phones.Count() > 1) ? ud.Phones[1].Phone : String.Empty);
                ItemsToPopulateFromUAS.Add(
                        "C1_AltPhoneMayMessage_item", (ud.Phones != null && ud.Phones.Count() > 1) ? ((ud.Phones[1].MayContactPhone) ? "1" : "0") : String.Empty);

                // * * * OT 1-20-16 updated this list based on Docs/ADAP/ADAP_item_list.xslx (revision 80734)
                string[] ItemIdentifiersToPopulateFromPrevApplication = new string[]
				{
                    #region long list of item identifiers
                    "C1_MemberLastName_item",
										"C1_MemberFirstName_item",
										"C1_MemberMiddleInitial_item",
										"C1_MemberAlsoKnownAs_item",
										"C1_MemberDateOfBirth_item",
// BWL: revisit these after re-importing correct schema (BUG)
//                    "ADAP_D3_White_item",
//                    "ADAP_D3_Black_item",
//                    "ADAP_D3_Asian_item",
//                    "ADAP_D3_Native_item",
//                    "ADAP_D3_Indian_item",
//                    "ADAP_D4_EthnicDrop_item",
//                    "ADAP_D4_Mexican_item",
//                    "ADAP_D4_Puerto_item",
//                    "ADAP_D4_Cuban_item",
//                    "ADAP_D4_Other_item",
//                    "ADAP_D5_Indian_item",
//                    "ADAP_D5_Filipino_item",
//                    "ADAP_D5_Korean_item",
//                    "ADAP_D5_Other_item",
//                    "ADAP_D5_Chinese_item",
//                    "ADAP_D5_Japanese_item",
//                    "ADAP_D5_Vietnamese_item",
//                    "ADAP_D5_NA_item",
//                    "ADAP_D6_Native_item",
//                    "ADAP_D6_Guam_item",
//                    "ADAP_D6_Samoan_item",
//                    "ADAP_D6_Other_item",
//                    "ADAP_D6_NA_item",
//                    "ADAP_D7_LangDrop_item",
//                    "ADAP_D7_LangOther_item",
//                    "C1_MemberCurrentGender",
//                    "C1_MemberBirthGender",
// 					"ADAP_D9_Ramsell_item",
                    "C1_MemberSocSecNumber_item",
										"C1_ResidentialAddressLine1_item",
										"C1_ResidentialAddressCity_item",
										"C1_ResidentialAddressState_item",
										"C1_ResidentialAddressZIP_item",
										"C1_ResidentialAddressCounty_item",
										"C1_ResidentialAddressMayContact_item",
										"C1_MailingAddressSameAsResidential_item",
										"C1_MailingAddressLine1_item",
										"C1_MailingAddressCity_item",
										"C1_MailingAddressState_item",
										"C1_MailingAddressZIP_item",
										"C1_MailingAddressMayContact_item",
										"C1_DaytimePhoneNumber_item",
										"C1_DaytimePhoneType_item",
										"C1_DaytimePhoneMayMessage_item",
										"C1_AltPhoneNumber_item",
										"C1_AltPhoneType_item",
										"C1_AltPhoneMayMessage_item",
//                    "ADAP_C4_MayCallYN_item",
//                    "ADAP_C4_Name_item",
//                    "ADAP_C4_Phone_item",
//                    "ADAP_C4_KnowHivYN_item",
                    "C1_MemberCaseManager_item",
//                    "ADAP_C5_Mngr1_Name_item",
                    "C1_MemberSelectedEnrollmentSite_item",
					//                    "ADAP_C5_CanReferYN_item",
					//                    "ADAP_M1_Month_item",
					//                    "ADAP_M1_Year_item",
					//                    "ADAP_M1_DiagnosisLoc_item",
					//                    "ADAP_M2_ToldAIDS_item",
					//                    "ADAP_M3_ToldHepC_item",
					//                    "ADAP_M4_Clinic_item",
					//                    "ADAP_I1_Med_Yes_item",
					//                    "ADAP_I1_Med_Denied_item",
					//                    "ADAP_I1_Med_No_item",
					//                    "ADAP_I1_Med_Waiting_item",
					//                    "ADAP_I1_Med_DontKnow_item",
					//                    "ADAP_I1_DeniedReason_item",
					//                    "ADAP_I1_NotAppliedReason_item",
					//                    "ADAP_I2_AffCareOpt_item",
					//                    "ADAP_I2_AffCareOther_item",
					//                    "ADAP_I3_MedicareYN_item",
					//                    "ADAP_I3_MedNumber_item",
					//                    "ADAP_I3_PartAYN_item",
					//                    "ADAP_I3_PartBYN_item",
					//                    "ADAP_I3_PartADate_item",
					//                    "ADAP_I3_PartBDate_item",
					//                    "ADAP_H1_StatusDrop_item",
					//                    "ADAP_H2_RelnDrop_item",
					//                    "ADAP_H2_RelnOther_item",
					//                    "ADAP_H3_FileTaxYN_item",
					//                    "ADAP_H3_TaxStatusOpt_item",
					//                    "ADAP_H3_TaxDependants_item",
					//                    "ADAP_H3_TaxNotFileOpt_item",
					//                    "ADAP_H3_TaxNotFileOther_item",
					//                    "ADAP_H3_Relatives_item",
					//                    "ADAP_H4_ChildrenIn_item",
					//                    "ADAP_H4_ChildrenOut_item",
					//                    "ADAP_F1_EmployOpt_item",
					//                    "ADAP_F1_EmployOther_item",
					//                    "ADAP_F1_EmployerInsOpt_item",
					//                    "ADAP_F1_EmployNotEnrolled_item",
					//                    "ADAP_F2_EmployLast90YN_item",
					//                    "ADAP_F3_A_Recipient_item",
					//                    "ADAP_F3_A_IncomeTypeDrop_item",
					//                    "ADAP_F3_A_Employer_item",
					//                    "ADAP_F3_A_EmployStart_item",
					//                    "ADAP_F3_A_TempYN_item",
					//                    "ADAP_F3_A_IncomeTypeOther_item",
					//                    "ADAP_F3_A_IncomeAmt_item",
					//                    "ADAP_F3_A_EmployerForm_item",
					//                    "ADAP_F3_B_Recipient_item",
					//                    "ADAP_F3_B_IncomeTypeDrop_item",
					//                    "ADAP_F3_B_Employer_item",
					//                    "ADAP_F3_B_EmployStart_item",
					//                    "ADAP_F3_B_TempYN_item",
					//                    "ADAP_F3_B_IncomeTypeOther_item",
					//                    "ADAP_F3_B_IncomeAmt_item",
					//                    "ADAP_F3_B_EmployerForm_item",
					//                    "ADAP_F3_C_Recipient_item",
					//                    "ADAP_F3_C_IncomeTypeDrop_item",
					//                    "ADAP_F3_C_Employer_item",
					//                    "ADAP_F3_C_EmployStart_item",
					//                    "ADAP_F3_C_TempYN_item",
					//                    "ADAP_F3_C_IncomeTypeOther_item",
					//                    "ADAP_F3_C_IncomeAmt_item",
					//                    "ADAP_F3_C_EmployerForm_item",
					//                    "ADAP_F3_D_Recipient_item",
					//                    "ADAP_F3_D_IncomeTypeDrop_item",
					//                    "ADAP_F3_D_Employer_item",
					//                    "ADAP_F3_D_EmployStart_item",
					//                    "ADAP_F3_D_TempYN_item",
					//                    "ADAP_F3_D_IncomeTypeOther_item",
					//                    "ADAP_F3_D_IncomeAmt_item",
					//                    "ADAP_F3_D_EmployerForm_item",
					//                    "ADAP_cert_NoSharing_item",
					//                    "ADAP_cert_Reminders_item"
					#endregion
				};


                Applications appl = new Applications(formsRepo);

                AuthenticationClient authClient = new AuthenticationClient();
                var groupIds = authClient.GetGroupsInUserPermissions(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.UserID).Select(x => x.GroupID);
                var groupId = groupIds.FirstOrDefault();

                def_FormResults frmRes = appl.CreateFormResultPopulatedFromUAS(SessionHelper.LoginStatus.EnterpriseID, groupId, userId, frm.formId, ItemsToPopulateFromUAS);
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
                    mLogger.Error(ex, "AddFormResult exception:");
                }

                mLogger.Debug("AddFormResult newFormResultId: {0}", newFormResultId);

                // *** RRB 5/30/16 - Ramsell not being used by CA.
                // query the ramsell system and update the formResult responses based on Ramsell response
                /*
                def_ResponseVariables rvMemberId = formsRepo.GetResponseVariablesByFormResultIdentifier(newFormResultId, "ADAP_D9_Ramsell");
                if (rvMemberId != null && !String.IsNullOrWhiteSpace(rvMemberId.rspValue))
                {
                        string memberId = rvMemberId.rspValue;
                        string usrId = UAS.Business.UAS_Business_Functions.GetEntAppConfigAdap("USR");
                        string password = UAS.Business.UAS_Business_Functions.GetEntAppConfigAdap("PWD");
                        mLogger.Debug("Ramsell UseriD / Password: " + usrId + @" / " + password);

                        string token = Ramsell.GetOauthToken(usrId, password);
                        new RamsellImport(formsRepo, frmRes.formResultId).ImportApplication(token, memberId);
                }
                AJBoggs.Adap.Services.Api.Ramsell.PopulateItemsFromRamsellImport(formsRepo, frmRes);
                */
                if (SessionHelper.SessionForm == null)
                {
                    SessionHelper.SessionForm = new SessionForm();
                }

                SessionForm sf = SessionHelper.SessionForm;
                sf.formId = frm.formId;
                sf.formIdentifier = frm.identifier;
                sf.sectionId = formsRepo.GetSectionByIdentifier("CA-ADAP-Contact").sectionId;
                sf.partId = formsRepo.GetPartByFormAndIdentifier(frm, "CA-ADAP").partId;

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
                return RedirectToAction("AdapPortal", "AdapCa", new { userId = userId, error = "Not Approved" });
            }
        }

        /// <summary>
        /// Creates a new ADAP application, attempting to pull as much data from the previous application and from UAS as possible.
        /// New applications are only permitted when there is no previous applicatoin, or the previous application was approved.
        /// </summary>
        /// <returns>Redirects the user to the Application pages.</returns>
        [HttpPost]
        public ActionResult CreateForm(int formId, int userId, string formVariant)
        {
            mLogger.Info("Create form formId={0} userId={1}", formId, userId);

            def_Forms frm = formsRepo.GetFormById(formId);

            int? intSO = formsRepo.GetStatusDetailByMasterIdentifier(4, "CANCELLED").sortOrder;
            int? intDenied = formsRepo.GetStatusDetailByMasterIdentifier(4, "DENIED").sortOrder;

            var frmResults = formsRepo.GetFormResultsByFormId(formId);
            def_FormResults prevRes = frmResults.Where(f => f.subject == userId && intSO != null && f.formStatus != intSO && intDenied != null && f.formStatus != intDenied).OrderByDescending(f => f.dateUpdated).FirstOrDefault();

            var enrollmentForms = formsRepo.GetFormResultsByFormId(15);
            def_FormResults prevEnrollment = enrollmentForms.Where(f => f.subject == userId).OrderByDescending(f => f.dateUpdated).FirstOrDefault();

            if (((prevRes == null) || (prevRes.formStatus >= 6 || prevRes.formStatus == 4)) && formId == 15)
            {
                //Get items to prepopulate from json file
                string itemsFilePath = Server.MapPath(Constants.CAADAP.ITEMS_TO_PREPOPULATE_FILEPATH);
                if (!System.IO.File.Exists(itemsFilePath))
                {
                    mLogger.Error("Items to prepopulate file was not found at {0}.", itemsFilePath);
                    throw new InvalidOperationException(String.Format("Items to prepopulate file was not found at {0}.", itemsFilePath));
                }
                string itemsJson = System.IO.File.ReadAllText(itemsFilePath);
                List<ItemToPrepopulate> itemsToPrepopulate = JsonConvert.DeserializeObject<List<ItemToPrepopulate>>(itemsJson);
                if (itemsToPrepopulate.Count == 0)
                {
                    mLogger.Warn("No items to prepopulate were found in {0}.", itemsFilePath);
                }

                AuthenticationClient webclient = new AuthenticationClient();
                UserDisplay uasData = webclient.GetUserDisplay(userId);

                //TODO get these values from the JSON file
                Dictionary<string, string> uasItemDictionary = new Dictionary<string, string>();

                var adapId = webclient.GetExistingAdapIdentifier(uasData.UserID, uasData.EnterpriseID);

                // ADAP ID
                if (!String.IsNullOrWhiteSpace(adapId))
                {
                    uasItemDictionary.Add("C1_MemberIdentifier_item", adapId);
                }

                uasItemDictionary.Add("C1_FormType_item", formVariant);

                //Name
                if (!String.IsNullOrWhiteSpace(uasData.LastName))
                {
                    uasItemDictionary.Add("C1_MemberLastName_item", uasData.LastName);
                }
                if (!String.IsNullOrWhiteSpace(uasData.FirstName))
                {
                    uasItemDictionary.Add("C1_MemberFirstName_item", uasData.FirstName);
                }
                if (!String.IsNullOrWhiteSpace(uasData.MiddleName) && uasData.MiddleName.Length > 0)
                {
                    uasItemDictionary.Add("C1_MemberMiddleInitial_item", uasData.MiddleName.Substring(0, 1));
                }
                //Email
                if (uasData.Emails != null && uasData.Emails.Count > 0 && !String.IsNullOrWhiteSpace(uasData.Emails[0].Email))
                {
                    uasItemDictionary.Add("C1_MemberEmail_item", uasData.Emails[0].Email);
                }
                //Residential address
                if (uasData.Addresses != null)
                {
                    if (uasData.Addresses.Count > 0)
                    {
                        if (!String.IsNullOrWhiteSpace(uasData.Addresses[0].Address1))
                        {
                            uasItemDictionary.Add("C1_ResidentialAddressLine1_item", uasData.Addresses[0].Address1);
                        }
                        if (!String.IsNullOrWhiteSpace(uasData.Addresses[0].Address2))
                        {
                            uasItemDictionary.Add("C1_ResidentialAddressLine2_item", uasData.Addresses[0].Address2);
                        }
                        if (!String.IsNullOrWhiteSpace(uasData.Addresses[0].City))
                        {
                            uasItemDictionary.Add("C1_ResidentialAddressCity_item", uasData.Addresses[0].City);
                        }
                        if (!String.IsNullOrWhiteSpace(uasData.Addresses[0].State))
                        {
                            uasItemDictionary.Add("C1_ResidentialAddressState_item", uasData.Addresses[0].State);
                        }
                        if (!String.IsNullOrWhiteSpace(uasData.Addresses[0].ZIP))
                        {
                            uasItemDictionary.Add("C1_ResidentialAddressZIP_item", uasData.Addresses[0].ZIP);
                        }
                        //Mailing address ... Is this correct? The residential and mailing addresses are ordered in the UAS result? 
                        if (uasData.Addresses.Count > 1)
                        {
                            if (!String.IsNullOrWhiteSpace(uasData.Addresses[1].Address1))
                            {
                                uasItemDictionary.Add("C1_MailingAddressLine1_item", uasData.Addresses[1].Address1);
                            }
                            if (!String.IsNullOrWhiteSpace(uasData.Addresses[1].Address2))
                            {
                                uasItemDictionary.Add("C1_MailingAddressLine2_item", uasData.Addresses[1].Address2);
                            }
                            if (!String.IsNullOrWhiteSpace(uasData.Addresses[1].City))
                            {
                                uasItemDictionary.Add("C1_MailingAddressCity_item", uasData.Addresses[1].City);
                            }
                            if (!String.IsNullOrWhiteSpace(uasData.Addresses[1].State))
                            {
                                uasItemDictionary.Add("C1_MailingAddressState_item", uasData.Addresses[1].State);
                            }
                            if (!String.IsNullOrWhiteSpace(uasData.Addresses[1].ZIP))
                            {
                                uasItemDictionary.Add("C1_MailingAddressZIP_item", uasData.Addresses[1].ZIP);
                            }
                        }
                    }
                }
                if (uasData.Phones != null)
                {
                    if (uasData.Phones.Count > 0)
                    {
                        //Daytime phone
                        if (!String.IsNullOrWhiteSpace(uasData.Phones[0].Phone))
                        {
                            uasItemDictionary.Add("C1_DaytimePhoneNumber_item", uasData.Phones[0].Phone);
                            uasItemDictionary.Add("C1_DaytimePhoneMayMessage_item", uasData.Phones[0].MayContactPhone ? "1" : "0");
                            //C1_DaytimePhoneType ... the requirements say to pull this from UAS but it doesn't appear to be there
                        }
                        //Alt phone
                        if (uasData.Phones.Count > 1)
                        {
                            if (!String.IsNullOrWhiteSpace(uasData.Phones[1].Phone))
                            {
                                uasItemDictionary.Add("C1_AltPhoneNumber_item", uasData.Phones[1].Phone);
                                uasItemDictionary.Add("C1_AltPhoneMayMessage_item", uasData.Phones[1].MayContactPhone ? "1" : "0");
                                //C1_AltPhoneType ... the requirements say to pull this from UAS but it doesn't appear to be there
                            }
                        }
                    }
                }

                Applications appl = new Applications(formsRepo);

                AuthenticationClient authClient = new AuthenticationClient();
                var groupIds = authClient.GetGroupsInUserPermissions(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.UserID).Select(x => x.GroupID);
                var groupId = groupIds.FirstOrDefault();

                //Populate from UAS
                //Populate items where Source = UAS in json file
                def_FormResults frmRes = appl.CreateFormResultPopulatedFromUAS(SessionHelper.LoginStatus.EnterpriseID, groupId, userId, frm.formId, uasItemDictionary);

                //Populate from Eligibility Dashboard (unless conflicts with UAS)
                //Populate items where Source = DASHBOARD or Source = UAS
                appl.PopulateItemsFromEligibilityDashboard(frmRes, itemsToPrepopulate
                    .Where(x => x.Source == Constants.CAADAP.PREPOPULATE_SOURCE_DASHBOARD || x.Source == Constants.CAADAP.PREPOPULATE_SOURCE_UAS)
                    .ToList());

                //We need the formResultId to copy the documents over (which we do in PopulateItemsFromPreviousApplication)
                //so we need to save the formResult here
                int newFormResultId = formsRepo.AddFormResult(frmRes);

                //Populate from Previous Application (unless conflicts with UAS or Eligibility Dashboard)
                //Populate items where Source = PREVIOUS_APPLICATION or Source = DASHBOARD or Source = UAS ... this is all the items in the json file
                appl.PopulateItemsFromPreviousApplication(frmRes, itemsToPrepopulate);

                frmRes.statusChangeDate = DateTime.Now;

                // Save the FormResult, ItemResults, and ResponseVariables
                formsRepo.SaveFormResults(frmRes);
                mLogger.Debug("AddFormResult newFormResultId: {0}", newFormResultId);

                // *** RRB 5/30/16 - Ramsell not being used by CA.
                // query the ramsell system and update the formResult responses based on Ramsell response
                /*
                def_ResponseVariables rvMemberId = formsRepo.GetResponseVariablesByFormResultIdentifier(newFormResultId, "ADAP_D9_Ramsell");
                if (rvMemberId != null && !String.IsNullOrWhiteSpace(rvMemberId.rspValue))
                {
                        string memberId = rvMemberId.rspValue;
                        string usrId = UAS.Business.UAS_Business_Functions.GetEntAppConfigAdap("USR");
                        string password = UAS.Business.UAS_Business_Functions.GetEntAppConfigAdap("PWD");
                        mLogger.Debug("Ramsell UseriD / Password: " + usrId + @" / " + password);

                        string token = Ramsell.GetOauthToken(usrId, password);
                        new RamsellImport(formsRepo, frmRes.formResultId).ImportApplication(token, memberId);
                }
                AJBoggs.Adap.Services.Api.Ramsell.PopulateItemsFromRamsellImport(formsRepo, frmRes);
                */
                if (SessionHelper.SessionForm == null)
                {
                    SessionHelper.SessionForm = new SessionForm();
                }

                SessionForm sf = SessionHelper.SessionForm;
                sf.formId = frm.formId;
                sf.formIdentifier = frm.identifier;
                sf.sectionId = formsRepo.GetSectionByIdentifier("CA-ADAP-Contact").sectionId;
                sf.partId = formsRepo.GetPartByFormAndIdentifier(frm, "CA-ADAP").partId;

                // *** RRB - should be deprecated - use SessionForm
                // *** BR - line 359 of the ResultsController calls this session variable, so it must be set to prevent an exception.
                //  Other parts of the application may still use that variable, so changing it in the ResultsController may break something else.
                Session["part"] = sf.partId;
                // Should have the partId also - may not be required.

                sf.formResultId = newFormResultId;

                return RedirectToAction("Template", "Results", new { sectionId = sf.sectionId.ToString() });
            }
            // SVF w/ no changes and previous enrollment form was approved or cancelled
            else if (formId == 19 && prevEnrollment != null && (prevEnrollment.formStatus >= 6 || prevEnrollment.formStatus == 5)
                && (prevRes == null || prevRes.formStatus != 0))
            {
                // SVF with no changes and an approved application exist
                AuthenticationClient webclient = new AuthenticationClient();
                UserDisplay ud = webclient.GetUserDisplay(userId);

                Dictionary<string, string> itemsToPopulateFromSef = new Dictionary<string, string>();
                var formResultId = prevEnrollment.formResultId;
                AddFromSef(itemsToPopulateFromSef, formResultId, "C1_MemberIdentifier");
                AddFromSef(itemsToPopulateFromSef, formResultId, "C1_ResidentialAddressLine1");
                AddFromSef(itemsToPopulateFromSef, formResultId, "C1_ResidentialAddressLine2");
                AddFromSef(itemsToPopulateFromSef, formResultId, "C1_ResidentialAddressCity");
                AddFromSef(itemsToPopulateFromSef, formResultId, "C1_ResidentialAddressState");
                AddFromSef(itemsToPopulateFromSef, formResultId, "C1_ResidentialAddressZIP");
                AddFromSef(itemsToPopulateFromSef, formResultId, "C1_MemberCalcHouseholdIncome");

                // insurance provider name
                var privateInsurance = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_OtherInsuranceProvider");

                if (privateInsurance != null && !string.IsNullOrWhiteSpace(privateInsurance.rspValue))
                {
                    itemsToPopulateFromSef.Add("C1_OtherInsuranceProvider_item", privateInsurance.rspValue);
                }

                var medicarePartD = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_MedicarePartDPlanName");

                if (medicarePartD != null && !string.IsNullOrWhiteSpace(medicarePartD.rspValue))
                {
                    itemsToPopulateFromSef.Add("C1_MedicarePartDPlanName_item", medicarePartD.rspValue);
                }

                var mediCal = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_StateMedicaidMemberId");
                if (mediCal != null && !string.IsNullOrWhiteSpace(mediCal.rspValue))
                {
                    itemsToPopulateFromSef.Add("C1_SVFMediCalPlanName_item", "Medi-Cal");
                }


                AuthenticationClient authClient = new AuthenticationClient();
                var groupIds = authClient.GetGroupsInUserPermissions(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.UserID).Select(x => x.GroupID);
                var groupId = groupIds.FirstOrDefault();

                Applications appl = new Applications(formsRepo);
                def_FormResults frmRes = appl.CreateFormResultPopulatedFromUAS(SessionHelper.LoginStatus.EnterpriseID, groupId, userId, frm.formId, itemsToPopulateFromSef);

                frmRes.statusChangeDate = DateTime.Now;

                // Save the FormResult, ItemResults, and ResponseVariables
                int newFormResultId = 0;
                try
                {
                    newFormResultId = formsRepo.AddFormResult(frmRes);
                }
                catch (Exception ex)
                {
                    mLogger.Error(ex, "AddFormResult exception:");
                }

                mLogger.Debug("AddFormResult newFormResultId: {0}" + newFormResultId);

                if (SessionHelper.SessionForm == null)
                {
                    SessionHelper.SessionForm = new SessionForm();
                }

                SessionForm sf = SessionHelper.SessionForm;
                sf.formId = frm.formId;
                sf.formIdentifier = frm.identifier;
                var firstPart = formsRepo.GetFormParts(frm).FirstOrDefault();
                formsRepo.GetPartSections(firstPart);
                var section = firstPart.def_PartSections.FirstOrDefault();
                sf.sectionId = section.sectionId;
                sf.partId = firstPart.partId;

                // *** RRB - should be deprecated - use SessionForm
                // *** BR - line 359 of the ResultsController calls this session variable, so it must be set to prevent an exception.
                //  Other parts of the application may still use that variable, so changing it in the ResultsController may break something else.
                Session["part"] = sf.partId;
                // Should have the partId also - may not be required.

                sf.formResultId = newFormResultId;

                return RedirectToAction("Template", "Results", new { sectionId = sf.sectionId.ToString() });
            }
            else if (formId != 15 && formId != 19)
            {
                AuthenticationClient webclient = new AuthenticationClient();
                UserDisplay ud = webclient.GetUserDisplay(userId);

                Dictionary<string, string> ItemsToPopulateFromUAS = new Dictionary<string, string>();

                // * * * OT 1-20-16 updated this list based on Docs/ADAP/ADAP_item_list.xslx (revision 80734)
                string[] ItemIdentifiersToPopulateFromPrevApplication = new string[]
				{
				};

                AuthenticationClient authClient = new AuthenticationClient();
                var groupIds = authClient.GetGroupsInUserPermissions(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.UserID).Select(x => x.GroupID);
                var groupId = groupIds.FirstOrDefault();

                Applications appl = new Applications(formsRepo);
                def_FormResults frmRes = appl.CreateFormResultPopulatedFromUAS(SessionHelper.LoginStatus.EnterpriseID, groupId, userId, frm.formId, ItemsToPopulateFromUAS);
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
                    mLogger.Error(ex, "AddFormResult exception:");
                }

                mLogger.Debug("AddFormResult newFormResultId: {0}" + newFormResultId);

                if (SessionHelper.SessionForm == null)
                {
                    SessionHelper.SessionForm = new SessionForm();
                }

                SessionForm sf = SessionHelper.SessionForm;
                sf.formId = frm.formId;
                sf.formIdentifier = frm.identifier;
                var firstPart = formsRepo.GetFormParts(frm).FirstOrDefault();
                formsRepo.GetPartSections(firstPart);
                var section = firstPart.def_PartSections.FirstOrDefault();
                sf.sectionId = section.sectionId;
                sf.partId = firstPart.partId;

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
                return RedirectToAction("AdapPortal", "AdapCa", new { userId = userId, error = "Not Approved" });
            }
        }

        private void AddFromSef(Dictionary<string, string> ItemsToPopulateFromUAS, int formResultId, string identifier)
        {
            var response = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, identifier);

            if (response != null)
            {
                ItemsToPopulateFromUAS.Add(identifier + "_item", response.rspValue);
            }
        }

        public ActionResult Validate(FormCollection frmCllctn, TemplateItems ti)
        {
            mLogger.Info("Validate");

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
                        ssSectionIds.Add(formsRepo.GetSubSectionById(si.subSectionId.Value).sectionId);

                    model.titlesOfMissingSubsectionsBySectionByPart[prtId].Add(sctId, new List<string>());
                    foreach (string itemVariableIdent in model.missingItemVariablesBySectionByPart[prtId][sctId])
                    {
                        //for each item variable identifier returned by generic validation,
                        //lookup the corresponding subsection's title for display on the ADAP "validation errors" screen
                        def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(itemVariableIdent);
                        def_Items itm = iv == null ? null : formsRepo.GetItemById(iv.itemId);
                        def_SectionItems si = itm == null ? null : formsRepo.getSectionItemsForItem(itm).Where(sit => ssSectionIds.Any() == false || ssSectionIds.Contains(sit.sectionId)).FirstOrDefault();
                        def_Sections sub = si == null ? null : formsRepo.GetSectionById(si.sectionId);
                        if (sub != null && !model.titlesOfMissingSubsectionsBySectionByPart[prtId][sctId].Contains(itm.label))
                            model.titlesOfMissingSubsectionsBySectionByPart[prtId][sctId].Add(itm.label);

                    }
                }
            }

            //run CO-ADAP one-off validation
            if (AdapCAOneOffValidation.RunOneOffValidation(formsRepo,sv, model))
                invalid = true;

            //return the validation errors screen if any errors were found
            //model.validationMessages could have some warnings even if no errors were found, in which case "invalid" would be false here
            if (invalid)
            {
                return View("~/Views/CAADAP/ValidationErrors.cshtml", model);
            }

            //if no problems were found, or if warnings were found but no errors, return the next section in the application
            if (model.validationMessages.Count == 0)
            {
                model.validationMessages.Add("No errors found.");
            }
            int sectionId = Convert.ToInt16(frmCllctn["navSectionId"]);
            return rc.Template(sectionId, model.validationMessages);
        }

        public ActionResult ValidateAndCertify(FormCollection frmCllctn, TemplateItems ti)
        {
            mLogger.Info("Validate and Certify");

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
                        ssSectionIds.Add(formsRepo.GetSubSectionById(si.subSectionId.Value).sectionId);

                    model.titlesOfMissingSubsectionsBySectionByPart[prtId].Add(sctId, new List<string>());
                    foreach (string itemVariableIdent in model.missingItemVariablesBySectionByPart[prtId][sctId])
                    {
                        //for each item variable identifier returned by generic validation,
                        //lookup the corresponding subsection's title for display on the ADAP "validation errors" screen
                        def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(itemVariableIdent);
                        def_Items itm = iv == null ? null : formsRepo.GetItemById(iv.itemId);
                        def_SectionItems si = itm == null ? null : formsRepo.getSectionItemsForItem(itm).Where(sit => ssSectionIds.Any() == false || ssSectionIds.Contains(sit.sectionId)).FirstOrDefault();
                        def_Sections sub = si == null ? null : formsRepo.GetSectionById(si.sectionId);
                        if (sub != null && !model.titlesOfMissingSubsectionsBySectionByPart[prtId][sctId].Contains(itm.label))
                            model.titlesOfMissingSubsectionsBySectionByPart[prtId][sctId].Add(itm.label);

                    }
                }
            }

            //run CO-ADAP one-off validation
            if (AdapCAOneOffValidation.RunOneOffValidation(formsRepo,sv, model))
                invalid = true;

            //return the validation errors screen if any errors were found
            //model.validationMessages could have some warnings even if no errors were found, in which case "invalid" would be false here
            if (invalid)
            {
                return View("~/Views/CAADAP/ValidationErrors.cshtml", model);
            }

            //if no problems were found, or if warnings were found but no errors, return the next section in the application
            if (model.validationMessages.Count == 0)
            {
                if (AutoGroupAssignment(formResultId))
                {
                    model.validationMessages.Add("Successfully Submitted with TAP");
                }
                else
                {
                    model.validationMessages.Add("Successfully Submitted");
                }

                var frmResult = formsRepo.GetFormResultById(formResultId);

                // not approved status
                if (frmResult.formStatus < 6)
                {
                    // update the status to needs review
                    int status = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(4, "NEEDS_REVIEW").sortOrder);
                    AdapController ac = new AdapController(formsRepo);
                    ac.ControllerContext = ControllerContext;
                    ac.StatusUpdated("", formResultId, status, true);
                }
            }
            int sectionId = Convert.ToInt16(frmCllctn["navSectionId"]);
            return rc.Template(sectionId, model.validationMessages);
        }

        public bool AutoGroupAssignment(int formResultId)
        {
            mLogger.Info("AutoGroupAssignment");
            bool result = false;

            var tap = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_TAPDoc");

            // have TAP attached document
            if (tap != null && string.IsNullOrWhiteSpace(tap.rspValue) == false)
            {
                result = true;

                bool insuranceSet = false;

                var medicaidEnrolled = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_StateMedicaidEnrolled");
                if (medicaidEnrolled != null)
                {
                    if (medicaidEnrolled.rspValue == "0")
                    {
                        insuranceSet = true;
                        // medical coverage
                        MediCalSet(formResultId);
                    }
                }

                var medicareEligable = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_MedicareEligible");
                if (medicareEligable != null && medicareEligable.rspValue == "1")
                {
                    var partDenrolled = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_MedicarePartDEnroll");
                    if (partDenrolled != null && partDenrolled.rspValue == "1")
                    {
                        insuranceSet = true;
                        PartDInsuranceSet(formResultId);
                    }
                }

                var stateExchngEnroll = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_ACAStateExchgEnroll");
                if (stateExchngEnroll != null)
                {
                    if (stateExchngEnroll.rspValue == "1")
                    {
                        // set Eligibility Dashboard fields
                        insuranceSet = true;
                        PrivateInsruanceSet(formResultId);
                    }
                    else if (stateExchngEnroll.rspValue == "0")
                    {
                        var otherInsEnroll = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_OtherInsuranceEnroll");
                        var otherInsType = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_OtherInsuranceType");

                        if (otherInsEnroll != null && otherInsEnroll.rspValue == "1" && otherInsType != null)
                        {
                            if (otherInsType.rspValue == "1" || otherInsType.rspValue == "2" || otherInsType.rspValue == "3")
                            {
                                // set Eligibility dashboard fields
                                insuranceSet = true;
                                PrivateInsruanceSet(formResultId);
                            }
                        }
                    }
                }

                if (!insuranceSet)
                {
                    // ADAP Only Insurnace
                    AdapInsuranceSet(formResultId);
                }
            }
            else
            {
                bool insuranceSet = false;

                var medicaidEnrolled = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_StateMedicaidEnrolled");

                if (medicaidEnrolled != null)
                {
                    if (medicaidEnrolled.rspValue == "0")
                    {
                        insuranceSet = true;
                        // medical coverage
                        SetGroupCode(formResultId, "222313");
                    }
                }

                        var medicareEligable = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_MedicareEligible");
                        if (medicareEligable != null && medicareEligable.rspValue == "1")
                        {
                            var partDenrolled = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_MedicarePartDEnroll");
                            if (partDenrolled != null && partDenrolled.rspValue == "1")
                            {
                                insuranceSet = true;
                        SetGroupCode(formResultId, "222312");
                    }
                }

                var stateExchngEnroll = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_ACAStateExchgEnroll");
                if (stateExchngEnroll != null)
                {
                    if (stateExchngEnroll.rspValue == "1")
                    {
                        // set Eligibility Dashboard fields
                        insuranceSet = true;
                        SetGroupCode(formResultId, "222314");
                    }
                    else if (stateExchngEnroll.rspValue == "0")
                    {
                        var otherInsEnroll = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_OtherInsuranceEnroll");
                        var otherInsType = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_OtherInsuranceType");

                        if (otherInsEnroll != null && otherInsEnroll.rspValue == "1" && otherInsType != null)
                        {
                            if (otherInsType.rspValue == "1" || otherInsType.rspValue == "2" || otherInsType.rspValue == "3")
                            {
                                // set Eligibility dashboard fields
                                insuranceSet = true;
                                SetGroupCode(formResultId, "222314");
                            }
                        }
                    }
                }

                if (!insuranceSet)
                {
                    // ADAP Only Insurnace
                    SetGroupCode(formResultId, "222311");
                }

                formsRepo.Save();
            }

            return result;
        }


        private int SetGroupCode(int formResultId, string groupCode)
        {
            var formResult = formsRepo.GetFormResultById(formResultId);
            var magGroupCode = formsRepo.GetResponseVariablesBySubjectForm(formResult.subject.Value, 18, "C1_MagellanGroupCode");

            if (magGroupCode == null)
            {
                // create Eligibility if it doesn't exist
                CreateElgibility(formResultId);
            }

            magGroupCode = formsRepo.GetResponseVariablesBySubjectForm(formResult.subject.Value, 18, "C1_MagellanGroupCode");

            magGroupCode.rspValue = groupCode;
            int subject = formResult.subject.Value;
            return subject;
        }

        public void CopyToEligibility(string sefIdentifier, string elgIdentifier, int formResultId, int subject)
        {
            var sef = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, sefIdentifier);
            var elg = formsRepo.GetResponseVariablesBySubjectForm(subject, 18, elgIdentifier);

            if (elg == null)
            {
                CreateEligibilityField(elgIdentifier);
                elg = formsRepo.GetResponseVariablesBySubjectForm(subject, 18, elgIdentifier);
            }

            if (sef != null && elg != null)
            {
                elg.rspValue = sef.rspValue;
            }
        }

        private void PrivateInsruanceSet(int formResultId)
        {
            int subject = SetGroupCode(formResultId, "222314");

            CommonInsuranceSet(formResultId, subject);

            CopyToEligibility("C1_OtherInsuranceProvider", "C1_OtherInsuranceProvider", formResultId, subject);
            CopyToEligibility("C1_OtherInsurancePlanID", "C1_OtherInsurancePlanID", formResultId, subject);
            CopyToEligibility("C1_OtherInsuranceMemberID2", "C1_OtherInsuranceMemberID2", formResultId, subject);
            CopyToEligibility("C1_OtherInsuranceStartDate", "C1_OtherInsuranceStartDate", formResultId, subject);
            CopyToEligibility("C1_OtherInsuranceEffDate", "C1_OtherInsuranceEffDate", formResultId, subject);

            // Save changes
            formsRepo.Save();
        }

        private void CommonInsuranceSet(int formResultId, int subject)
        {
            // copy over Ernollment Site Name
            CopyToEligibility("C1_FormSubmitEnrollmentSiteName", "C1_MemberSelectedEnrollmentSite", formResultId, subject);

            // copy over enrollment worker name
            CopyToEligibility("C1_FormSubmitEnrollmentWorkerName", "C1_FormSubmitEnrollmentWorkerName", formResultId, subject);

            // set Elgibility End Date
            var endDate = DateTime.Now.AddDays(30).ToShortDateString();
            var elgEndDate = formsRepo.GetResponseVariablesBySubjectForm(subject, 18, "C1_ProgramEligibleEndDate");
            elgEndDate.rspValue = endDate;

            // set eligibility start date
            var currDate = DateTime.Now.ToShortDateString();
            var elgStartDate = formsRepo.GetResponseVariablesBySubjectForm(subject, 18, "C1_ProgramEligibleStartDate");
            if (elgStartDate == null)
            {
                CreateEligibilityField("C1_ProgramEligibleStartDate");
            }

            if (String.IsNullOrWhiteSpace(elgStartDate.rspValue))
            {
                elgStartDate.rspValue = currDate;
            }
        }

        private void PartDInsuranceSet(int formResultId)
        {
            int subject = SetGroupCode(formResultId, "222312");

            CommonInsuranceSet(formResultId, subject);

            CopyToEligibility("C1_MedicarePartDEffDate", "C1_MedicarePartDEffDate", formResultId, subject);
            CopyToEligibility("C1_MedicarePartDEnrollDate", "C1_MedicarePartDEnrollDate", formResultId, subject);
            CopyToEligibility("C1_MedicarePartDIdNumber", "C1_MedicarePartDIdNumber", formResultId, subject);
            CopyToEligibility("C1_MedicarePartDPlanName", "C1_MedicarePartDPlanName", formResultId, subject);

            formsRepo.Save();
        }

        private void MediCalSet(int formResultId)
        {
            int subject = SetGroupCode(formResultId, "222313");

            CommonInsuranceSet(formResultId, subject);

            CopyToEligibility("C1_StateMedicaidMemberId", "C1_StateMedicaidMemberId", formResultId, subject);
            CopyToEligibility("C1_StateMAidEffDate", "C1_StateMAidEffDate", formResultId, subject);

            formsRepo.Save();
        }

        private void AdapInsuranceSet(int formResultId)
        {
            int subject = SetGroupCode(formResultId, "222311");

            CommonInsuranceSet(formResultId, subject);

            formsRepo.Save();
        }

        public ActionResult ClientProfile()
        {
            mLogger.Info("ClientProfile");

            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            string rTeam = Request["Team"];
            string rStatus = Request["Status"];
            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ReportName = "Applications Pending";

            aar1.Forms = new List<SelectListItem>();
            formsEntities context = new Assmnts.formsEntities();
            aar1.Forms = (from f in context.def_FormVariants
                          where f.EnterpriseID == 8
                          select new SelectListItem()
                          {
                              Text = f.title,
                              Value = f.formID.ToString()
                          }).ToList();

            aar1.SearchForms = (from f in context.def_FormVariants
                                where f.EnterpriseID == 8
                                select new SelectListItem()
                                {
                                    Text = f.title,
                                    Value = f.formID.ToString() + "|" + f.title
                                }).ToList();
            aar1.SearchForms.Insert(0, new SelectListItem()
            {
                Text = "All",
                Value = string.Empty
            });

            aar1.Units = new List<SelectListItem>();
            AuthenticationClient webclient = new AuthenticationClient();
            var groups = webclient.GetGroupsByEnterpriseID(SessionHelper.LoginStatus.EnterpriseID);
            aar1.Units = (from g in groups
                          where g.GroupTypeID == 195
                          select new SelectListItem()
                          {
                              Text = g.GroupDescription,
                              Value = g.GroupID.ToString()
                          }).ToList();
            aar1.Units.Insert(0, new SelectListItem()
            {
                Text = "All",
                Value = string.Empty
            });


            if (!String.IsNullOrEmpty(rTeam))
            {
                aar1.setTeam = rTeam;
            }

            if (!String.IsNullOrEmpty(rStatus))
            {
                aar1.setStatus = rStatus;
            }

            if (String.IsNullOrEmpty(rTeam) && String.IsNullOrEmpty(rStatus))
            {
                aar1.setStatus = "Pending";
            }

            //bool accessLevel = UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts");
            //if (!accessLevel)
            //{
            //    return RedirectToAction("AdapPortal", "AdapCa", new { userId = SessionHelper.LoginStatus.UserID });
            //}

            // Populate the Report Model
            // Will be very similar to /Model/TemplateForms.cs for now.
            // replace 'null' in the return with with Model instance.
            // replace view with the Report model
            if (aar1 == null)
            {
                aar1 = new AdapApplicantRpt1();
            }
            aar1.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;
            if (String.IsNullOrEmpty(aar1.ReportName))
            {
                aar1.ReportName = "Applicant Report";
            }
            aar1.TeamDDL = webclient.GetGroupsByEnterpriseID(SessionHelper.LoginStatus.EnterpriseID);
            aar1.TeamDDL.Insert(0, new Group()
            {
                GroupName = "All"
            });

            aar1.TypeDDL = Applications.GetDefaultFormIdentifiersForEnterprise(SessionHelper.LoginStatus.EnterpriseID);
            aar1.TypeDDL.Insert(0, "All");

            aar1.StatusDDL = GetAdapStatusList(formsRepo, SessionHelper.LoginStatus.EnterpriseID);
            aar1.StatusDDL.Add(-1, "All");

            // This probably should not be hardcoded.
            // Search parameters in DataTableApplicationsList parse these strings for the numerical data
            // (1 month, 3 months, etc), a safer means should be implemented.
            aar1.DateDDL = dateSelectList;
            webclient.Close();

            string viewPath = Applications.GetDefaultReportViewPathForEnterprise(SessionHelper.LoginStatus.EnterpriseID);

            return View(viewPath, aar1);
        }

        /// <summary>
        /// Get List of all Application Status
        /// </summary>
        /// <param name="formsRepo">FormsRepository interface</param>
        /// <returns>Dic</returns>
        public static Dictionary<int, string> GetAdapStatusList(IFormsRepository formsRepo, int enterpriseId)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();

            // RRB 3/24/16
            // Need to add GetStatusMaster(ApplicationId, FormId)
            List<def_StatusDetail> allSd = formsRepo.GetStatusDetails(4);

            foreach (def_StatusDetail sd in allSd)
            {
                def_StatusText st = formsRepo.GetStatusText(sd.statusDetailId, enterpriseId, 1);
                int sortOrder = sd.sortOrder.Value;
                string text = st.displayText;
                result.Add(sortOrder, text);
            }

            return result;

        }

        [HttpGet]
        public ActionResult CreateDocument()
        {
            def_Forms frm = formsRepo.GetFormByIdentifier("CA-ADAP-PROFILEATTACHMT");
            //string userId = Request["userId"];
            int userId = 0;
            try
            {
                userId = Convert.ToInt32(Request["userId"]);
            }
            catch (Exception excptn)
            {
                mLogger.Error(excptn, "Adap Controller CreateAdapApplication exception:");
            }

            int? intSO = formsRepo.GetStatusDetailByMasterIdentifier(1, "CANCELLED").sortOrder;
            def_FormResults prevRes = frm.def_FormResults.Where(f => f.subject == userId && intSO != null && f.formStatus != intSO).OrderByDescending(f => f.dateUpdated).FirstOrDefault();


            AuthenticationClient webclient = new AuthenticationClient();
            UserDisplay ud = webclient.GetUserDisplay(userId);

            Dictionary<string, string> ItemsToPopulateFromUAS = new Dictionary<string, string>();

            // * * * OT 1-20-16 updated this list based on Docs/ADAP/ADAP_item_list.xslx (revision 80734)
            string[] ItemIdentifiersToPopulateFromPrevApplication = new string[]
					{
					};

            AuthenticationClient authClient = new AuthenticationClient();
            var groupIds = authClient.GetGroupsInUserPermissions(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.UserID).Select(x => x.GroupID);
            var groupId = groupIds.FirstOrDefault();

            Applications appl = new Applications(formsRepo);
            def_FormResults frmRes = appl.CreateFormResultPopulatedFromUAS(SessionHelper.LoginStatus.EnterpriseID, groupId, userId, frm.formId, ItemsToPopulateFromUAS);
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
                mLogger.Error(ex, "AddFormResult exception:");
            }

            mLogger.Debug("AddFormResult newFormResultId: {0}" + newFormResultId);

            if (SessionHelper.SessionForm == null)
            {
                SessionHelper.SessionForm = new SessionForm();
            }

            SessionForm sf = SessionHelper.SessionForm;
            sf.formId = frm.formId;
            sf.formIdentifier = frm.identifier;
            sf.sectionId = formsRepo.GetSectionByIdentifier("CA-ADAP-Document").sectionId;
            sf.partId = formsRepo.GetPartByFormAndIdentifier(frm, "CA-ADAP-PA").partId;

            // *** RRB - should be deprecated - use SessionForm
            // *** BR - line 359 of the ResultsController calls this session variable, so it must be set to prevent an exception.
            //  Other parts of the application may still use that variable, so changing it in the ResultsController may break something else.
            Session["part"] = sf.partId;
            // Should have the partId also - may not be required.

            sf.formResultId = newFormResultId;

            return RedirectToAction("Template", "Results", new { sectionId = sf.sectionId.ToString() });
        }

        public ActionResult FinalSubmit(FormCollection frmCllctn, TemplateItems itm)
        {
            int formResultId = SessionHelper.SessionForm.formResultId;

            //run save (update/add response variables based on form collection)
            ResultsController rc = new ResultsController(formsRepo);
            rc.ControllerContext = ControllerContext;
            rc.Save(frmCllctn, itm);

            //run status update
            int status = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(4, "NEEDS_REVIEW").sortOrder);

            AdapController ac = new AdapController(formsRepo);
            ac.ControllerContext = ControllerContext;
            ac.StatusUpdated("", formResultId, status, true);

            return Redirect("~/ADAP");
        }

        /// <summary>
        /// Web Service to process requests from the ADAP Report 1 List DataTable
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableApplicationsList(int draw, int start, int length, string searchValue, string searchRegex)
        {
            string result = String.Empty;
            mLogger.Debug("Adap Reports Controller ApplicationsList draw: {0}" + draw);
            mLogger.Debug("Adap Reports Controller ApplicationsList start: {0}" + start);
            mLogger.Debug("Adap Reports Controller ApplicationsList searchValue: {0}" + searchValue);
            mLogger.Debug("Adap Reports Controller ApplicationsList searchRegex: {0}" + searchRegex);
            var mine = bool.Parse(Request["Mine"]);

            //  Lists all the Request parameters
            if (mLogger.IsTraceEnabled)
            {
                foreach (string s in Request.Params.Keys)
                {
                    mLogger.Trace("{0}:{1}", s, Request.Params[s]);
                }
            }

            try
            {
                // Initialize the DataTable response (later transformed to JSON)
                DataTableResult dtr = new DataTableResult()
                {
                    draw = draw,
                    data = new List<string[]>(),
                    error = String.Empty
                };

                var sFName = Request.Params["columns[1][search][value]"];
                var sLName = Request.Params["columns[2][search][value]"];
                var sTeam = Request.Params["columns[3][search][value]"];
                var sStat = Request.Params["columns[4][search][value]"];
                var sDate = Request.Params["columns[5][search][value]"];
                var sDob = Request.Params["columns[6][search][value]"];
                var sSsn = Request.Params["columns[7][search][value]"];
                var sAdapId = Request.Params["columns[8][search][value]"];
                var siteNum = Request.Params["columns[9][search][value]"];
                var enrollmentSite = Request.Params["columns[10][search][value]"];
                var formType = Request.Params["columns[11][search][value]"];
                var elgEndFrom = Request.Params["columns[12][search][value]"];
                var elgEndTo = Request.Params["columns[13][search][value]"];

                if (String.IsNullOrEmpty(sTeam))
                {
                    string s = Request["Team"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sTeam = s;
                    }
                }

                if (String.IsNullOrEmpty(sStat))
                {
                    string s = Request["Status"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sStat = s;
                    }
                }

                if (String.IsNullOrEmpty(sDate))
                {
                    string s = Request["Date"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sDate = s;
                    }
                }

                string sFullHistory = Request["showFullHistory"];
                bool showFullHistory = sFullHistory == null ? false : Convert.ToBoolean(sFullHistory);

                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;

                //get a queryable set of all applicable vFormResultUsers for enterprise, form identifiers, search criteria
                int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;
                Dictionary<int, string> formIdentifiersById = new Dictionary<int, string>();
                formsEntities context = new Assmnts.formsEntities();
                var forms = from f in context.def_Forms
                            where f.EnterpriseID == 8
                            && f.formId != 18
                            select f;
                foreach (var item in forms)
                {
                    formIdentifiersById.Add(item.formId, item.identifier);
                }

                CaFullReportGrid reportGridHelper = new CaFullReportGrid(formsRepo, formIdentifiersById, showFullHistory);
                AuthenticationClient authClient = new AuthenticationClient();
                var groupIds = authClient.GetGroupsInUserPermissions(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.UserID).GroupBy(x => x.GroupID).Select(x => (int?)x.FirstOrDefault().GroupID);
                //List<int?> gIds = new List<int?>();
                //foreach (var item in groupIds)
                //{
                //    gIds.Add(item);
                //}

                IQueryable<vFormResultUser> vfruQuery;
                if (UAS_Business_Functions.hasPermission(PermissionConstants.ASSIGNED, PermissionConstants.ASSMNTS) || mine)
                {
                    vfruQuery = formsRepo.GetFormResultsWithSubjects(enterpriseId, SessionHelper.LoginStatus.UserID, formIdentifiersById.Keys);
                }
                else
                {
                    vfruQuery = formsRepo.GetFormResultsWithSubjects(enterpriseId, groupIds.ToList(), formIdentifiersById.Keys);//.OrderByDescending(fr => fr.statusChangeDate);
                }
                if (!mine)
                {
                    // get forms not for this user
                    vfruQuery = vfruQuery.Where(x => x.subject != SessionHelper.LoginStatus.UserID);
                }

                vfruQuery = new Applications(formsRepo).SetVfruQueryParams(vfruQuery, sFName, sLName, sTeam, sStat, sDate, sDob, sSsn, sAdapId, siteNum, enrollmentSite, formType, elgEndFrom, elgEndTo, 4);

                mLogger.Debug("Adap Reports Controller ApplicationsList SelectedEnterprise: {0}", SessionHelper.LoginStatus.EnterpriseID);

                //* * * OT 3/31/16 toList() here is necessary to allow OrderBy using Applications.GridReports.cs -> GetOrderingValueByColumnIndex()
                dtr.recordsTotal = dtr.recordsFiltered = vfruQuery.Count();
                bool hasSort = Request.Params["order[0][column]"] != null;

                //set ordering, if specified in request parameters
                if (hasSort)
                {
                    int orderColumnIndex = Convert.ToInt32(Request.Params["order[0][column]"]);
                    bool descending = Request.Params["order[0][dir]"] == "desc";
                    //adapGridRecords = adapGridRecords.OrderBy(row => row.GetSortingValueForColumn(orderColumnIndex));
                    if (!descending)
                    {
                        switch (orderColumnIndex)
                        {
                            case 0:
                                vfruQuery = vfruQuery.OrderBy(x => x.adap_id);
                                break;
                            case 1:
                                vfruQuery = vfruQuery.OrderBy(x => x.FirstName);
                                break;
                            case 2:
                                vfruQuery = vfruQuery.OrderBy(x => x.LastName);
                                break;
                            case 3:
                                vfruQuery = vfruQuery.OrderBy(x => x.formStatus);
                                break;
                            case 4:
                                vfruQuery = vfruQuery.OrderBy(x => x.statusChangeDate);
                                break;
                            case 5:
                                vfruQuery = vfruQuery.OrderBy(x => x.EligibilityEndDate);
                                break;
                            case 8:
                                vfruQuery = vfruQuery.OrderBy(x => x.formId);
                                break;
                            case 9:
                                vfruQuery = vfruQuery.OrderBy(x => x.GroupName);
                                break;
                            case 11:
                                vfruQuery = vfruQuery.OrderBy(x => x.DOB);
                                break;
                        }
                    }
                    else
                    {
                        switch (orderColumnIndex)
                        {
                            case 0:
                                vfruQuery = vfruQuery.OrderByDescending(x => x.adap_id);
                                break;
                            case 1:
                                vfruQuery = vfruQuery.OrderByDescending(x => x.FirstName);
                                break;
                            case 2:
                                vfruQuery = vfruQuery.OrderByDescending(x => x.LastName);
                                break;
                            case 3:
                                vfruQuery = vfruQuery.OrderByDescending(x => x.formStatus);
                                break;
                            case 4:
                                vfruQuery = vfruQuery.OrderByDescending(x => x.statusChangeDate);
                                break;
                            case 5:
                                vfruQuery = vfruQuery.OrderByDescending(x => x.EligibilityEndDate);
                                break;
                            case 8:
                                vfruQuery = vfruQuery.OrderByDescending(x => x.formId);
                                break;
                            case 9:
                                vfruQuery = vfruQuery.OrderByDescending(x => x.GroupName);
                                break;
                            case 11:
                                vfruQuery = vfruQuery.OrderByDescending(x => x.DOB);
                                break;
                        }
                    }

                    //adapGridRecords = adapGridRecords.Skip(iniIndex).Take(noDsplyRecs).ToList();
                }

                IEnumerable<vFormResultUser> vfruSearchResults = vfruQuery.Skip(iniIndex).Take(noDsplyRecs); // hasSort ? vfruQuery.ToList() : vfruQuery.Skip(iniIndex).Take(noDsplyRecs).ToList();
                IEnumerable<CaFullReportRow> adapGridRecords = reportGridHelper.BuildRowsFromVFormResultUsers(vfruSearchResults);


                //generate html for the rows that will be visible on the screen
                foreach (CaFullReportRow thisRowRecord in adapGridRecords)
                {
                    //build html content for a single row
                    string[] data = reportGridHelper.GetHtmlForReportRow(thisRowRecord);
                    def_StatusMaster statusMaster = formsRepo.GetStatusMasterByFormId(thisRowRecord.vfru.formId);
                    int statusMasterId = statusMaster == null ? 1 : statusMaster.statusMasterId;
                    var status = formsRepo.GetStatusTextByDetailSortOrder(statusMasterId, thisRowRecord.vfru.formStatus);
                    var statusDisplayText = status != null ? status.displayText : "In Progress";

                    if (UAS_Business_Functions.hasPermission(PermissionConstants.ASSIGNED, PermissionConstants.ASSMNTS))
                    {
                        data[3] = statusDisplayText;
                    }
                    else if (statusDisplayText.ToLower() == "needs review" || statusDisplayText.Contains("approved"))
                    {
                        if (!UAS_Business_Functions.hasPermission(PermissionConstants.APPROVE, PermissionConstants.ASSMNTS))
                        {
                            data[3] = statusDisplayText;
                        }
                    }
                    else if (thisRowRecord.vfru.formId == 18)
                    {
                        data[3] = statusDisplayText;
                    }

                    dtr.data.Add(data);
                }

                mLogger.Debug("Adap Reports Controller ApplicationsList data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                mLogger.Trace("Adap Reports Controller ApplicationsList result: {0}", result);
            }
            catch (Exception excptn)
            {
                mLogger.Error(excptn, "Adap Reports Controller ApplicationsList exception:");
                result = excptn.Message;
            }

            return result;

        }


        [HttpPost]
        public string RetrieveEnWorkersByGroup(string site)
        {
            PeoplePickerHelper objUtility = new PeoplePickerHelper(authClient, formsRepo);

            return objUtility.RetrieveEnWorkersByGroup(site);
        }



        /// <summary>
        /// Web Service to process requests from the ADAP Report 1 List DataTable
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableDocumentList(int draw, int start, int length, string searchValue, string searchRegex)
        {
            string result = String.Empty;
            mLogger.Debug("Adap Reports Controller ApplicationsList draw: {0}" + draw);
            mLogger.Debug("Adap Reports Controller ApplicationsList start: {0}" + start);
            mLogger.Debug("Adap Reports Controller ApplicationsList searchValue: {0}" + searchValue);
            mLogger.Debug("Adap Reports Controller ApplicationsList searchRegex: {0}" + searchRegex);

            //  Lists all the Request parameters
            if (mLogger.IsDebugEnabled)
            {
                foreach (string s in Request.Params.Keys)
                {
                    mLogger.Debug("{0}:{1}", s, Request.Params[s]);
                }
            }
            try
            {
                // Initialize the DataTable response (later transformed to JSON)
                DataTableResult dtr = new DataTableResult()
                {
                    draw = draw,
                    data = new List<string[]>(),
                    error = String.Empty
                };

                var sFName = Request.Params["columns[1][search][value]"];
                var sLName = Request.Params["columns[2][search][value]"];
                var sTeam = Request.Params["columns[3][search][value]"];
                var sStat = Request.Params["columns[4][search][value]"];
                var sDate = Request.Params["columns[5][search][value]"];
                var sType = Request.Params["columns[6][search][value]"];

                if (String.IsNullOrEmpty(sType))
                {
                    string s = Request["Type"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sType = s;
                    }
                }

                if (String.IsNullOrEmpty(sTeam))
                {
                    string s = Request["Team"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sTeam = s;
                    }
                }

                if (String.IsNullOrEmpty(sStat))
                {
                    string s = Request["Status"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sStat = s;
                    }
                }

                if (String.IsNullOrEmpty(sDate))
                {
                    string s = Request["Date"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sDate = s;
                    }
                }

                string sFullHistory = Request["showFullHistory"];
                bool showFullHistory = sFullHistory == null ? false : Convert.ToBoolean(sFullHistory);

                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;

                //get a queryable set of all applicable vFormResultUsers for enterprise, form identifiers, search criteria
                int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;
                Dictionary<int, string> formIdentifiersById = new Dictionary<int, string>();
                foreach (string formIdentifier in Request["FormIdentifiers"].Split(';'))
                {
                    int formId = formsRepo.GetFormByIdentifier(formIdentifier).formId;
                    formIdentifiersById.Add(formId, formIdentifier);
                }
                FullReportGrid reportGridHelper = new FullReportGrid(formsRepo, formIdentifiersById, showFullHistory);
                AuthenticationClient authClient = new AuthenticationClient();
                var groupIds = authClient.GetGroupsInUserPermissions(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.UserID).Select(x => x.GroupID);
                List<int?> gIds = new List<int?>();
                foreach (var item in groupIds)
                {
                    gIds.Add(item);
                }

                IQueryable<vFormResultUser> vfruQuery;
                if (UAS_Business_Functions.hasPermission(PermissionConstants.ASSIGNED, PermissionConstants.ASSMNTS))
                {
                    vfruQuery = formsRepo.GetFormResultsWithSubjects(enterpriseId, SessionHelper.LoginStatus.UserID, formIdentifiersById.Keys);
                }
                else
                {
                    vfruQuery = formsRepo.GetFormResultsWithSubjects(enterpriseId, gIds, formIdentifiersById.Keys);//.OrderByDescending(fr => fr.statusChangeDate);
                }
                //vfruQuery = new Applications(formsRepo).SetVfruQueryParams(vfruQuery, sFName, sLName, sTeam, sStat, sDate, sType);

                vfruQuery = vfruQuery.Where(q => q.StatusFlag.Equals("A"));

                mLogger.Debug("Adap Reports Controller ApplicationsList SelectedEnterprise: {0}", SessionHelper.LoginStatus.EnterpriseID);

                //* * * OT 3/31/16 toList() here is necessary to allow OrderBy using Applications.GridReports.cs -> GetOrderingValueByColumnIndex()
                IEnumerable<vFormResultUser> vfruSearchResults = vfruQuery.ToList();
                IEnumerable<FullReportRow> adapGridRecords = reportGridHelper.BuildRowsFromVFormResultUsers(vfruSearchResults);
                dtr.recordsTotal = dtr.recordsFiltered = adapGridRecords.Count();

                //set ordering, if specified in request parameters
                if (Request.Params["order[0][column]"] != null)
                {
                    int orderColumnIndex = Convert.ToInt32(Request.Params["order[0][column]"]);
                    bool descending = Request.Params["order[0][dir]"] == "desc";
                    adapGridRecords = adapGridRecords.OrderBy(row => row.GetSortingValueForColumn(orderColumnIndex));
                    if (descending)
                        adapGridRecords = adapGridRecords.Reverse();
                }

                //generate html for the rows that will be visible on the screen
                foreach (FullReportRow thisRowRecord in adapGridRecords.Skip(iniIndex).Take(noDsplyRecs).ToList())
                {
                    //build html content for a single row
                    string statusDisplayText = formsRepo.GetStatusTextByDetailSortOrder(1, thisRowRecord.vfru.formStatus).displayText;
                    def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(thisRowRecord.vfru.formResultId, "C1_ProfileDocumentTitle");
                    string title = "No title";

                    if (rv != null)
                    {
                        title = rv.rspValue;
                    }

                    string[] data = new string[]
					{
												"<a href=\"/ADAP/ToTemplate?formResultId=" + Convert.ToString(thisRowRecord.vfru.formResultId) + "&Update=Y\">" + title + "</a>",
												thisRowRecord.vfru.FirstName,
												thisRowRecord.vfru.LastName,
												statusDisplayText
					};//reportGridHelper.GetHtmlForReportRow(thisRowRecord);
                    dtr.data.Add(data);
                }

                mLogger.Debug("Adap Reports Controller ApplicationsList data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                mLogger.Debug("Adap Reports Controller ApplicationsList result: {0}", result);
            }
            catch (Exception excptn)
            {
                mLogger.Error(excptn, "Adap Reports Controller ApplicationsList exception:");
                result = excptn.Message;
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
        public FileContentResult BuildPDFReport(int? formResultId = null)
        {
            mLogger.Debug("* * *  AdapCaController:BuildPDFReport method  * * *");
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

            AccessLogging.InsertAccessLogRecord(formsRepo, formResultId.Value, (int)AccessLogging.accessLogFunctions.REPORT, "Generated Report");

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
            int sectionId = formsRepo.GetSectionByIdentifier("CA-ADAP-Consent").sectionId;

            //run status update
            int status = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(4, "NEEDS_REVIEW").sortOrder);

            int formResultId = SessionHelper.SessionForm.formResultId;
            AdapController ac = new AdapController(formsRepo);
            ac.ControllerContext = ControllerContext;
            ac.StatusUpdated("", formResultId, status, true);

            ViewBag.Notify = "Saved";
            ViewBag.NotifyMessage = "Successfully Submitted";

            return rc.Template(sectionId);
        }

        [HttpPost]
        public bool UploadFile(FormCollection formCollection)
        {
            int fileId = -1;
            int RelatedEnumId = formsRepo.GetRelatedEnumIdByEnumDescription("formResultId");
            int AttachTypeId = formsRepo.GetAttachTypeIdByAttachDescription("Generic Upload");

            int formResultId = Convert.ToInt32(formCollection["formResultId"]);
            System.Web.HttpPostedFileWrapper file = (System.Web.HttpPostedFileWrapper)Request.Files["file" + formResultId.ToString()];
            def_FormResults fr = formsRepo.GetFormResultById(formResultId);

            DateTime now = DateTime.Now;
            string date = now.Year.ToString() + ((now.Month < 10) ? "0" : "") + now.Month.ToString() + ((now.Day < 10) ? "0" : "") + now.Day.ToString();
            string time = ((now.Hour < 10) ? "0" : "") + now.Hour.ToString() + ((now.Minute < 10) ? "0" : "") + now.Minute.ToString() + ((now.Second < 10) ? "0" : "") + now.Second.ToString();
            string subDir = date + Path.DirectorySeparatorChar + time;

            fileId = FileUploads.CreateAttachment(formsRepo, file, null, subDir, formResultId, RelatedEnumId, AttachTypeId);

            // check if the file uploaded has a duplicated displayText for this assessment.  i.e., a logical overwrite.
            if (fileId > -1)
            {
                def_FileAttachment fa = formsRepo.GetFileAttachment(fileId);
                Dictionary<int, string> texts = FileUploads.RetrieveFileDisplayTextsByRelatedId(formsRepo, formResultId, RelatedEnumId, "A", AttachTypeId);

                foreach (int k in texts.Keys)
                {
                    if (texts[k].Equals(fa.displayText) && k != fa.FileId)
                    {
                        FileUploads.DeleteFile(formsRepo, k);
                    }
                }
            }


            if (fileId > -1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [HttpGet]
        public void DeleteFile()
        {
            int RelatedEnumId = formsRepo.GetRelatedEnumIdByEnumDescription("formResultId");
            int formResultId = Convert.ToInt32(Request["formResultId"]);
            int fileId = Convert.ToInt32(Request["fileId"]);
            def_FormResults fr = formsRepo.GetFormResultById(formResultId);
            bool result = FileUploads.DeleteFile(formsRepo, fileId);
        }
        [HttpPost]
        public string DataActiveEnrollementList1(int draw, int start, int length)
        {
            string result = String.Empty;
            Debug.WriteLine("DataTableController UserList draw:" + draw.ToString());
            Debug.WriteLine("DataTableController UserList start:" + start.ToString());
           
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

                //var sFirstN = Request.Params["columns[1][search][value]"];
                //var sLastN = Request.Params["columns[2][search][value]"];
                //var sAdapId = Request.Params["columns[3][search][value]"];
                //var sDob = Request.Params["columns[4][search][value]"];
                //var sSsn = Request.Params["columns[5][search][value]"];
                //var sEsNumber = Request.Params["columns[6][search][value]"];

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
                var result1 = auth.ListEnrollment();
                dtr.recordsTotal = result1.Count();
                dtr.recordsFiltered = result1.Count();

                result1 = result1.Skip(iniIndex).Take(noDsplyRecs).ToList();

                foreach (var rec in result1)
                {
                    dtr.data.Add(new string[] {rec.GroupNumber,rec.GroupDescription,rec.Address1
                                    ,rec.Address2,rec.County,rec.City,rec.State,rec.ZipCode,rec.PhoneNumber,rec.EmailAddress
                    });

                //    dtr.data.Add(new string[] {rec.GroupNumber,rec.GroupDescription,rec.Address1
                //                    ,rec.Address2,rec.County,rec.City,rec.State,rec.ZipCode,rec.PhoneNumber,rec.EmailAddress
                //    });
                //    //aar1.Add(AE); ////adding each aar1site in to list // 
                //
                }
         
                int entId = 8;
                int appId = 3;

                

                //* * * OT 2-23-16 explaination for Bug 13019, comment 21, first bullet
                //for users that do not have the "View users" permission, they can only see themselves in the limited users list
                //we do this by searching for the current user's loginId
                //this normally works, but in some cases there are multiple loginIds that contain the current user's loginId 
                //Example: Login as nctest and you can also see NCTest1.


                //Final record stores total result set count.
                //if (usrs.Count > 0)
                //{
                //    dtr.recordsTotal = dtr.recordsFiltered = Convert.ToInt32(usrs[usrs.Count - 1][0]);//usrCount;
                //    usrs.RemoveAt(usrs.Count - 1);
                //    Debug.WriteLine("DataTableController usrs.Count:" + usrs.Count.ToString());
                //}
                //else
                //{
                //    dtr.recordsTotal = dtr.recordsFiltered = 0;
                //    Debug.WriteLine("DataTableController usrs.Count:" + 0);
                //}
                //dtr.data = (from u in usrs
                //            select u.ToArray()).ToList();
                //dtr.data = (from u in auth.ListEnrollment()
                //            select u.ToString());

                //string[] ar = new string[] { "Venu",
                //            "Venudhar"};
                //string[] ar1 = new string[] { "Venu1",
                //            "Venudhar1"};
                //dtr.data.Add(ar);
                //dtr.data.Add(ar1);
                //dtr.recordsTotal = dtr.recordsFiltered = 10;
                //Debug.WriteLine("DataTableController UserList data populated.");

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
        public string DataTablePeopleList(int draw, int start, int length, string searchValue, string searchRegex,
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
                var sAdapId = Request.Params["columns[3][search][value]"];
                var sDob = Request.Params["columns[4][search][value]"];
                var sSsn = Request.Params["columns[5][search][value]"];
                var sEsNumber = Request.Params["columns[6][search][value]"];

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

                List<List<string>> usrs = auth.List_people_Users(SessionHelper.LoginStatus.UserID, entId, iniIndex, noDsplyRecs,
                    sFirstN, sLastN, sAdapId, sSsn, sEsNumber, sDob);

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
        [HttpGet]
        public ActionResult CancelSVF(int formResultId)
        {
            int status = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(6, "CANCELLED").sortOrder);
            AdapController ac = new AdapController(formsRepo);
            ac.ControllerContext = ControllerContext;
            ac.StatusUpdated("", formResultId, status, true);

            return RedirectToAction("Index", "AdapCa");
        }

        [HttpPost]
        public string DataTablePortal(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            string result = String.Empty;
            Debug.WriteLine("Adap Reports Controller DateTablePortal draw:" + draw.ToString());
            Debug.WriteLine("Adap Reports Controller DateTablePortal start:" + start.ToString());
            Debug.WriteLine("Adap Reports Controller DateTablePortal searchValue:" + searchValue);
            Debug.WriteLine("Adap Reports Controller DateTablePortal searchRegex:" + searchRegex);
            Debug.WriteLine("Adap Reports Controller DateTablePortal order:" + order);

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

                int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;
                List<int> formIds = new List<int>();
                foreach (string formIdentifier in Request["FormIdentifiers"].Split(';'))
                {
                    int formId = formsRepo.GetFormByIdentifier(formIdentifier).formId;
                    formIds.Add(formId);
                }
                IQueryable<vFormResultUser> query = formsRepo.GetFormResultsWithSubjects(enterpriseId, formIds);

                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;

                int userId = Convert.ToInt32(Request["UserId"]);
                int cancel = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(4, "CANCELLED").sortOrder);

                query = query.Where(q => q.subject == userId && (q.formStatus != cancel));

                dtr.recordsTotal = dtr.recordsFiltered = query.Count();

                foreach (var q in query.Skip(iniIndex).Take(noDsplyRecs))
                {
                    var ramsellIdDisplay = formsRepo.GetResponseVariablesByFormResultIdentifier(q.formResultId, "C1_MemberIdentifier");
                    var ramsellIdDisplayText = ramsellIdDisplay != null ? ramsellIdDisplay.rspValue : string.Empty;
                    if (String.IsNullOrWhiteSpace(ramsellIdDisplayText))
                        ramsellIdDisplayText = @Resources.AdapPortal.NoId;

                    // First line should display the Ramsell ID, which would not exist on default.  Code to check the database should be added at a later date.
                    string[] data = new string[] {
                        ramsellIdDisplayText,
                        q.statusChangeDate.ToString(),
                        //(AdapApprovals.GetAdapStatusList(formsRepo).Count() > q.formStatus) ? AdapApprovals.GetAdapStatusList(formsRepo)[q.formStatus] : q.formStatus.ToString()
                        formsRepo.GetStatusTextByDetailSortOrder(1, q.formStatus).displayText
                    };
                    dtr.data.Add(data);
                }

                Debug.WriteLine("Adap Reports Controller DateTablePortal data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                Debug.WriteLine("Adap Reports Controller DateTablePortal result:" + result);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Reports Controller DateTablePortal exception:" + excptn.Message);
                result = excptn.Message + " - " + excptn.Message;
            }

            return result;
        }

        #region medical claim Action

        /// <summary>
        /// Method to (validate) and submit the Medical Claim
        /// </summary>
        /// <param name="frmCllctn"></param>
        /// <param name="ti"></param>
        /// <returns></returns>
        public ActionResult SubmitMedicalClaim(FormCollection frmCllctn, TemplateItems ti)
        {

            //save response from formCollection into the DB
            ResultsController resultsController = new ResultsController(mLogger, formsRepo, authClient);
            resultsController.ControllerContext = ControllerContext;
            resultsController.Save(frmCllctn, ti);

            int formResultId = SessionHelper.SessionForm.formResultId;

            //Need to check validations before saving to Database


            int status = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(5, "SUBMITTED").sortOrder);

            // update the status to needs review
            AdapController adapController = new AdapController(formsRepo);
            adapController.ControllerContext = ControllerContext;
            adapController.StatusUpdated("", formResultId, status, false);

            int sectionId = Convert.ToInt16(frmCllctn["navSectionId"]);
            return resultsController.Template(sectionId, null);

        }

        /// <summary>
        /// Method to cancel the medical claim
        /// </summary>
        /// <param name="formResultId"></param>
        /// <returns></returns>
        public ActionResult CancelMedicalClaim(int formResultId)
        {
            int status = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(5, "CANCELLED").sortOrder);
            AdapController ac = new AdapController(formsRepo);
            ac.ControllerContext = ControllerContext;
            ac.StatusUpdated("", formResultId, status, false);

            return RedirectToAction("Index", "AdapCa");

        }


        #endregion

        #region status

        /// <summary>
        /// Loads the page to update the Status for a given application.
        /// </summary>
        /// <returns>View to update the status for the selected application.</returns>
        [HttpPost]
        public ActionResult UpdateStatus(string formResultId)
        {
           

             AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;
            int accessLevel = (UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts")) ? 1 : 0;
            // check here for managerial status.
            if ((accessLevel == 1) && UAS.Business.UAS_Business_Functions.hasPermission(1, "RptsExpts"))
            {
                accessLevel = 2;
            }

            try
            {
                int frmResId = Convert.ToInt32(formResultId);

                bool hasAutorizationToFormResult = Applications.CACheckUserHasAccesstoFormResult(frmResId, SessionHelper.LoginStatus.UserID);

                if (!hasAutorizationToFormResult)
                {
                    string basePortalUrl = WebConfigurationManager.AppSettings["UASAdminURL"];
                    return Redirect(new Uri(new Uri(basePortalUrl), "Portal").ToString());
                }

                def_FormResults result = formsRepo.GetFormResultById(frmResId);
                def_StatusMaster statusMaster = formsRepo.GetStatusMasterByFormId(result.formId);
                int statusMasterId = statusMaster.statusMasterId;

                //populate the model with the current formResultId, and a list of possible "new statuses"
                aar1.FormId = result.formId;
                aar1.formResult = frmResId;
                aar1.StatusDDL = AdapApprovals.PossibleWorkflow(formsRepo, statusMasterId, result.formStatus, accessLevel);

                //add the current status as an option to the model
                def_StatusText currentStatusText = formsRepo.GetStatusTextByDetailSortOrder(statusMasterId, result.formStatus);
                aar1.StatusDDL.Add(result.formStatus, currentStatusText.displayText + " (Current Status)");

                def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "C1_MemberIdentifier") ?? formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D9_Ramsell");
                aar1.MemberId = (rv == null || String.IsNullOrEmpty(rv.rspValue)) ? "0" : rv.rspValue;

                rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D1_FirstName");
                aar1.FirstName = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;

                rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D1_LastName");
                aar1.LastName = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;

                aar1.setStatus = currentStatusText.displayText;

                // Repurposing the Team field for the previous status note.
                aar1.setTeam = String.Empty;
                def_StatusDetail statusDetail = formsRepo.GetStatusDetailBySortOrder(statusMasterId, result.formStatus);
                if (statusDetail != null)
                {
                    def_StatusLog statusLog = formsRepo.GetMostRecentStatusLogByStatusDetailToFormResultIdAndUserId(
                        statusDetail.statusDetailId, frmResId, SessionHelper.LoginStatus.UserID);
                    if (statusLog != null)
                    {
                        aar1.setTeam = statusLog.statusNote;
                    }
                }

                //*** Start Adding Eligibility End Date for Lousiana
                if (result.formId == 11)
                {
                    // aar1.EigibilityEndDate 
                    rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "C1_ProgramEligibleEndDate");
                    aar1.EigibilityEndDate = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;


                }
                //*** End Adding Eligibility End Date for Lousiana
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Reports Controller UpdateStatus exception:" + excptn.Message);

                aar1.MemberId = "0";
                aar1.FirstName = "Exception";
                aar1.LastName = "Message";
                aar1.setStatus = "0";
                aar1.setTeam = excptn.Message;
                aar1.StatusDDL = new Dictionary<int, string>();
            }

            string viewDirPath = Applications.GetAdapTemplatesViewDirPath(SessionHelper.LoginStatus.EnterpriseID);
            return View(viewDirPath + "UpdateStatus.cshtml", aar1);
        }

        /// <summary>
        /// Creates the Status History page. 
        /// </summary>
        /// <param name="formResultId">The formResultId of the application in question.</param>
        /// <returns>View of the Status History page.</returns>
        [HttpPost]
        public ActionResult StatusHistory(string formResultId)
        {
            int frmResId = Convert.ToInt32(formResultId);

            bool hasAutorizationToFormResult = Applications.CACheckUserHasAccesstoFormResult(frmResId, SessionHelper.LoginStatus.UserID);

            if (!hasAutorizationToFormResult)
            {
                string basePortalUrl = WebConfigurationManager.AppSettings["UASAdminURL"];
                return Redirect(new Uri(new Uri(basePortalUrl), "Portal").ToString());
            }

            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;
            aar1.ReportName = "Status History";
            aar1.formResultId = frmResId;

            def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "C1_MemberIdentifier") ?? formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D9_Ramsell");

            aar1.MemberId = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? "0" : rv.rspValue;
            rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D1_FirstName");
            aar1.FirstName = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;
            rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D1_LastName");
            aar1.LastName = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;


            string viewDirPath = Applications.GetAdapTemplatesViewDirPath(SessionHelper.LoginStatus.EnterpriseID);
            return View(viewDirPath + "StatusHistory.cshtml", aar1);
        }

        #endregion
    }
}