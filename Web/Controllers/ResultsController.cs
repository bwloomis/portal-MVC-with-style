using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using AJBoggs;
using AJBoggs.Sis.Domain;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Data.Abstract;
using NLog;
using Assmnts.UasServiceRef;
using System.Web;
using AJBoggs.Adap.Domain;
using UAS.Business;
using System.Web.Configuration;

namespace Assmnts.Controllers
{
	/*
	 * This controller is used to display and save formResult data from the template screens.
	 * The templates are in sub-dirs to Views/Templates such as Views/Templates/SIS
	 * 
	 */
	[HandleError]
	[RedirectingAction]
	public partial class ResultsController : Controller
	{

     
        private IAuthentication authClient;
		private IFormsRepository formsRepo;
		private ILogger mLogger;

		public ResultsController(IFormsRepository fr)
		{
			formsRepo = fr;
			mLogger = LogManager.GetCurrentClassLogger();
            authClient = new AuthenticationClient();
		}

		public ResultsController(ILogger logger, IFormsRepository fr, IAuthentication auth)
		{
			// Initiialized by Infrastructure.Ninject
			formsRepo = fr;
			mLogger = logger;
            authClient = auth;
		}

		[HttpGet]
		public ActionResult Index()
		{
			if (!SessionHelper.IsUserLoggedIn) {
				return RedirectToAction("Index", "Account", null);
			}

			// RRB 9/18/15 - user somehow got this URL.  Just log them out for now.
			// return RedirectToAction("Index", "Account", null);

			mLogger.Debug("* * *  ResultsController:Index method  * * *");
			// Initialize the session variables
			// SessionForm sf = SessionHelper.SessionForm;

			// Display the formResults (Assessments)
			Assmnts.Models.FormResults frmRsltsMdl = new Assmnts.Models.FormResults();
			frmRsltsMdl.formResults = new List<def_FormResults>();
			List<def_Forms> formsList = formsRepo.GetAllForms();
			if (formsList != null) {
				// foreach (def_Forms frm in formsList)
				// {
				// frmRsltsMdl.formResults.AddRange(formsRepo.GetFormResultsByFormId(frm.formId));
				frmRsltsMdl.formResults.AddRange(formsRepo.GetFormResultsByFormId(5));
				// }
			}
			//frmRsltsMdl.formResults = formsRepo.GetAllFormResults();

			bool noFormResults = false;
			if (frmRsltsMdl.formResults.Count() == 0) {
				mLogger.Debug("* * *  Index  FormResults Count was 0. ");
				noFormResults = true;
				def_FormResults frmRslts = new def_FormResults() {
					formResultId = 0,
					formId = 0,
					dateUpdated = System.DateTime.Now
				};
				frmRsltsMdl.formResults.Add(frmRslts);
			} else {
				mLogger.Debug("* * *  Index  FormResults count: {0}", frmRsltsMdl.formResults.Count());
			}

			frmRsltsMdl.formTitles = new List<String>();
			if (noFormResults) {
				frmRsltsMdl.formTitles.Add("There are no Assessments in the system.");
			} else {
				foreach (def_FormResults frmRslt in frmRsltsMdl.formResults) {
					def_Forms frm = formsRepo.GetFormById(frmRslt.formId);
					if (frm != null) {
						frmRsltsMdl.formTitles.Add(frm.title);
					}
				}
			}

			return View("formResults", frmRsltsMdl);

		}

		[HttpGet]
		public ActionResult AddFormResults()
		{
			string paramFormId = Request["formId"] as string;
			mLogger.Debug("* * *  ResultsController AddFormResults formId: {0}", paramFormId);

			Session["formId"] = paramFormId;
			int formId = Convert.ToInt32(paramFormId);
			def_FormResults frmRes = new def_FormResults() {
				formId = formId,
				formStatus = 0,
				sessionStatus = 0,
				dateUpdated = DateTime.Today
			};
			int frmRslt = formsRepo.AddFormResult(frmRes);
			frmRes = formsRepo.GetFormResultById(frmRslt);
			def_Forms frm = formsRepo.GetFormById(formId);
			List<def_Parts> parts = formsRepo.GetFormParts(frm);

			// Setup session variables
			SessionForm sf = SessionHelper.SessionForm;
			sf.formId = frm.formId;
			sf.formResultId = frmRslt;

			return RedirectToAction("Parts", "Results", null);
		}

		[HttpGet]
		public ActionResult StartAssmnt()
		{
			if (!SessionHelper.IsUserLoggedIn) {
				return RedirectToAction("Index", "Account", null);
			}

			if (SessionHelper.SessionForm == null) {
				SessionHelper.SessionForm = new SessionForm();
			}

			// retrieve and set SessionForm params
			string paramFormId = Request["formId"] as string;
			SessionHelper.SessionForm.formId = Convert.ToInt32(paramFormId);

			string paramFormResultId = Request["formResultId"] as string;
			SessionHelper.SessionForm.formResultId = Convert.ToInt32(paramFormResultId);

			// get the sectionId of the first section of the first part based on the formId
			def_Forms frm = formsRepo.GetFormById(SessionHelper.SessionForm.formId);
			SessionHelper.SessionForm.formIdentifier = frm.identifier;

			def_Parts prt = formsRepo.GetFormParts(frm)[0];
			SessionHelper.SessionForm.partId = prt.partId;
			Session["part"] = prt.partId;

			def_Sections sct = formsRepo.GetSectionsInPart(prt)[0];

			return RedirectToAction("Template", "Results", new { sectionId = sct.sectionId.ToString(), partId = SessionHelper.SessionForm.partId.ToString() });
		}

		private TemplateNavMenu getNavMenuModel(SessionForm sf, TemplateItems parent)
		{
			switch (sf.formIdentifier) {
				case "ADAP":
				case "CA-ADAP":
				case "CA-ADAP-DASHBOARD":
				case "LA-ADAP":
				case "LA-ADAP-Stub":
				case "LA-ADAP-PreIns":
                case "LA-ADAP-MAGI":
					return AJBoggs.Adap.Templates.TemplateMenus.getAdapNavMenuModel(sf, formsRepo);
				default:
					return AJBoggs.Sis.Templates.TemplateMenus.getSisNavMenuModel(sf, parent, formsRepo);
			}
		}

		[HttpGet]
		public ActionResult Help()
		{
			mLogger.Debug("* * *  ResultsController:Help method  * * *");

			if (!SessionHelper.IsUserLoggedIn) {
				return RedirectToAction("Index", "Account", null);
			}

			if (SessionHelper.SessionForm == null) {
				SessionHelper.SessionForm = new SessionForm();
			}

			GeneralForm amt = new GeneralForm();
			amt.formId = SessionHelper.SessionForm.formId;

			return View("~/Views/Home/Help.cshtml", amt);
		}

		[HttpGet]
		public ActionResult Template(int? sectionIdOverride = null, List<string> validationMessages = null)
		{
			mLogger.Debug("* * *  ResultsController:Template method  * * *");

			if (!SessionHelper.IsUserLoggedIn) {
				return RedirectToAction("Index", "Account", null);
			}

			if (SessionHelper.SessionForm == null) {
				SessionHelper.SessionForm = new SessionForm();
			}

			if (Session["form"] != null && !string.IsNullOrWhiteSpace(Session["form"].ToString())) {
				int formId = SessionHelper.SessionForm.formId;
				formId = int.Parse(Session["form"].ToString());
				def_Forms frm = formsRepo.GetFormById(formId);
				SessionHelper.SessionForm.formId = formId;
				SessionHelper.SessionForm.formIdentifier = frm.identifier;
				var oldFrmResult = formsRepo.GetFormResultById(SessionHelper.SessionForm.formResultId);
				var newFrmResult = formsRepo.GetFormResultsByFormSubject(formId, oldFrmResult.subject).OrderByDescending(f => f.dateUpdated);
				if (newFrmResult.Any()) {
					// use most recent form result
					SessionHelper.SessionForm.formResultId = newFrmResult.FirstOrDefault().formResultId;
				}

				Session["form"] = null;
				int part = int.Parse(Session["part"].ToString());
				SessionHelper.SessionForm.partId = part;
			}

			SessionForm sessionForm = SessionHelper.SessionForm;

			// set language ID in session based on meta-data (default to English) + other session vars
			CultureInfo ci = Thread.CurrentThread.CurrentUICulture;
			string isoName = (ci == null) ? "en" : ci.TwoLetterISOLanguageName.ToLower();
			def_Languages lang = formsRepo.GetLanguageByTwoLetterISOName(isoName);
			if (lang == null)
				throw new Exception("could not find def_Language entry for iso code \"" + isoName + "\"");
			else
				sessionForm.langId = lang.langId;



			// This is the master or top level sectionId, there can be mulitple subSections below this section.
			// The subSections will be in the SectionItems
			string sectionId = (sectionIdOverride == null) ? Request["sectionId"] as string : sectionIdOverride.ToString();
			mLogger.Debug("* * *  Results Template sectionId: {0}", sectionId);
			Session["section"] = sectionId;
			def_Sections sctn = formsRepo.GetSectionById(Convert.ToInt32(sectionId));
			sessionForm.sectionId = sctn.sectionId;

			// Create a new Assessments Model Template (AMT) that is used by the .cshtml template (Razor code)
			TemplateItems amt;

			if (sctn.href != null && (sctn.href.EndsWith("spprtNeedsScale.cshtml") || sctn.href.EndsWith("excptnlMedSpprtNeed.cshtml")))
				amt = new QuestionListForm();
			else
				amt = new GeneralForm();


			amt.thisSection = sctn;
			amt.thisSectionId = sctn.sectionId;
			amt.formsRepo = formsRepo;
			amt.formResultId = sessionForm.formResultId;
			amt.currentUser = SessionHelper.LoginInfo.LoginID;

			//amt.items = new List<def_Items>();
			amt.subSections = new List<def_Sections>();
			amt.fldLabels = new Dictionary<string, string>();
			amt.itmPrompts = new Dictionary<string, string>();
			amt.rspValues = new Dictionary<string, string>();
			mLogger.Debug("* * *  ResultsController:Template sessionForm.formResultId: {0}", sessionForm.formResultId);
			def_FormResults fr = formsRepo.GetFormResultById(sessionForm.formResultId);
			mLogger.Debug("* * *  ResultsController:Template fr.formResultId: {0}", fr.formResultId);

			if (fr != null) {
				if (fr.formStatus == (byte)FormResults_formStatus.IN_PROGRESS) {
					amt.inProgress = true;
				}
				if (fr.formStatus == (byte)FormResults_formStatus.NEW) {
					amt.newAssmnt = true;
				}
			}



            //Start: added for enhancement Bug 13663 to be refactored

            amt.updatedDate = fr.dateUpdated;

            amt.formStatus = fr.formStatus;

            int statusMasterId = 0;

            def_StatusMaster statusMaster = formsRepo.GetStatusMasterByFormId(fr.formId);

            if (statusMaster != null)
            {

                statusMasterId = statusMaster.statusMasterId;

                def_StatusDetail statusdetails = formsRepo.GetStatusDetailBySortOrder(statusMasterId, fr.formStatus);

                amt.formSatusText = statusdetails.def_StatusText
                                                 .Where(sd => sd.EnterpriseID == 8 && sd.langId == 1)
                                                 .Select(z => z.displayText)
                                                 .FirstOrDefault();
            }
            else
            {
                //This is used as currently we are showing In Progress for all items which do not have status
                amt.formSatusText = "In Progress";
            }

            amt.CanUserChangeStatus = true;

            if (UAS_Business_Functions.hasPermission(PermissionConstants.ASSIGNED, PermissionConstants.ASSMNTS))
            {
                amt.CanUserChangeStatus = false;
            }
            else if (amt.formSatusText.ToLower() == "needs review" || amt.formSatusText.ToLower().Contains("approved"))
            {
                if (!UAS_Business_Functions.hasPermission(PermissionConstants.APPROVE, PermissionConstants.ASSMNTS))
                {
                    amt.CanUserChangeStatus = false;
                }
            }
            else if (amt.formId == 18)
            {
                amt.CanUserChangeStatus = false;
            }

            

            var formType = formsRepo.GetResponseVariablesByFormResultIdentifier(fr.formResultId, "C1_FormType");
            string formVariant = string.Empty;

            if (formType != null && formType.rspValue != null && formType.rspValue != String.Empty)
            {
                formVariant = formType.rspValue;
                amt.FormVariantTitle = formVariant;
            }
            else
            {
                formsEntities context = new Assmnts.formsEntities();

                amt.FormVariantTitle = context.def_FormVariants
                                              .Where(fv => fv.formID == fr.formId)
                                              .Select(fv => fv.title)
                                              .FirstOrDefault();

            }

            //get the subject id from form result id
            if (fr.subject != null)
            {
                int subject = (int)fr.subject;

                var elgEndDate = formsRepo.GetResponseVariablesBySubjectForm(subject, 18, "C1_ProgramEligibleEndDate");

                string endDate = elgEndDate != null ? elgEndDate.rspValue : string.Empty;

                amt.EligibilityEnddate = endDate;

                amt.formResultUser = authClient.GetUserDisplay(subject);

                amt.clientId = authClient.GetExistingAdapIdentifier(amt.formResultUser.UserID, 8);
            }




            //End: added for enhancement Bug 13663 to be refactored


            amt.formId = sessionForm.formId;

			formsRepo.SortSectionItems(sctn);       // This is both loading and sorting the SectionItems.

			// Get the Items in the SubSections *** NOTE: this only goes ONE level deep ***  Should be made recursive for unlimited levels !!!
			// amt.sections = new List<def_Sections>();
			foreach (def_SectionItems sctnItm in sctn.def_SectionItems.OrderBy(si => si.order)) {
				if (sctnItm.subSectionId.GetValueOrDefault(0) == 0 || sctnItm.display == false)
					continue;
				// def_Sections subSctns = sctnItm.def_SubSections.def_Sections;
				def_Sections subSctns = formsRepo.GetSubSectionById(sctnItm.subSectionId);

				//do not add notes section to the section list, (if there is a notes item, it will later be set as amt.notesItem)
				if (subSctns.multipleItemsPerPage == false)
					continue;

				formsRepo.SortSectionItems(subSctns);       // Load and sort the SectionItems
				amt.subSections.Add(subSctns);
				// GetItemLabelsResponses(fr.formResultId, amt, formsRepo.GetSectionItems(sctnItm.def_SubSections.def_Sections));
			}

			// RRB 5/28/15 *** This is only necessary for the Profile screens
			//             *** Getting and sorting the SectionItems above is only necessary for Question forms
			//             ***   In fact, it may duplicate what is being done in the templates
			formsRepo.GetItemListIncludingSubSections(sctn, amt);
			//GetItemListIncludingSubSections(sctn, amt);

			// Get the Notes items
			formsRepo.GetItemListForNotes(sctn, amt);

			formsRepo.GetItemLabelsResponses(fr.formResultId, amt, amt.items);

			//determine whether or not this user will be shown itemvariable identifiers as tooltips
			//TODO this is a DEV mode only thing
			amt.showItemVariableIdentifiersAsTooltips = Convert.ToBoolean(ConfigurationManager.AppSettings["showItemVariableIdentifiersAsTooltips"]);// true;//new UAS.Business.UAS_Business_Functions().hasPermission(7,"assmnts");

			//save response values in session variable to later determine which ones were modified
			////List<string> keysToClear = new List<string>();
			////foreach (string key in Session.Keys)
			////    if (key.StartsWith("frmOriginal___"))
			////        keysToClear.Add(key);
			////foreach (string key in keysToClear)
			////    Session.Remove(key);
			//DateTime oneHourFromNow = DateTime.Now.AddHours(1);
			//foreach (string key in amt.rspValues.Keys)
			//{
			//Response.Cookies["frmOriginal___" + key].Value = amt.rspValues[key];
			//Response.Cookies["frmOriginal___" + key].Expires = oneHourFromNow;
			//}

			mLogger.Debug("* * *  AFTER GetItemLabelsResponses amt.items.Count: {0}", amt.items.Count);

			mLogger.Debug("* * *  amt.rspValues.Count (Total): {0}", amt.rspValues.Count);

			//populate required and readonly dictionaries
			amt.fldRequired = new Dictionary<string, bool>();
			amt.fldReadOnly = new Dictionary<string, bool>();
			List<def_SectionItems> siList = formsRepo.GetSectionItemsBySectionIdEnt(sctn.sectionId, SessionHelper.LoginStatus.EnterpriseID);
			if (amt.items != null) {
				foreach (def_Items itm in amt.items) {
					if (amt.fldRequired.ContainsKey(itm.identifier)) {
						throw new Exception("Found duplicate item identifier \"" + itm.identifier + "\"");
					}
					def_SectionItems si = siList.FirstOrDefault(s => (!s.subSectionId.HasValue && s.itemId == itm.itemId));

					amt.fldRequired.Add(itm.identifier, (si == null) ? false : si.requiredForm);
					amt.fldReadOnly.Add(itm.identifier, (si == null) ? false : si.readOnly);
				}
			}

			amt.isProfile = false;

			string[] uriSegments = sctn.href.Split('/');
			string templateFileName = uriSegments[uriSegments.Count() - 1];
			switch (templateFileName) {
				case "idprof1.cshtml":
				case "idprof2.cshtml":
				case "idprof2_Child.cshtml":
					if (mLogger.IsDebugEnabled) {
						mLogger.Debug("showing section item identifiers:");
						foreach (def_SectionItems si in siList) {
							mLogger.Debug("\t{0}", si.def_Items.identifier);
						}
					}
					amt.notesItem = siList.Single(si => si.def_Items.identifier.EndsWith("PageNotes_item")).def_Items;
					sessionForm.templateType = TemplateType.StdSectionItems;
					amt.isProfile = true;
					break;

				default:            // "SIS/section1a":     Default to the item list
					sessionForm.templateType = TemplateType.MultipleCommonItems;
					break;

			}

			// Setup the Previous / Next screens
			amt.prevScreenTitle = amt.prevScreenHref = String.Empty;
			amt.nextScreenTitle = amt.nextScreenHref = String.Empty;

			def_Parts prt = formsRepo.GetPartById(Convert.ToInt32(Session["part"]));



			List<def_Sections> partSections = formsRepo.GetSectionsInPart(prt);
			mLogger.Debug("* * *  ResultsController:Template method  * * * partSections.Count: {0}", partSections.Count);

			//get a list of parts in the current form

			def_Forms form = formsRepo.GetFormById(amt.formId);
			List<def_Parts> partsInForm = formsRepo.GetFormParts(form);
			List<int> partIdsInForm = partsInForm.Select(p => p.partId).ToList();
			// List<def_Parts> partsInForm = formsRepo.getFormParts(fr.def_Forms);



			//these variables will be assigned int he loop below
			int currentSectionIndex = 0;
			int prevSectionIndex = 0;
			int nextSectionIndex = 0;

			//start by assuming the previous and next section will be in the same part
			int currentPartId = (prt == null) ? sessionForm.partId : prt.partId;

			amt.thisPartId = currentPartId;

			amt.navPartId = currentPartId.ToString();
			int prevPartId = currentPartId;
			int nextPartId = currentPartId;

			//in a special cases one of these will be set to true
			bool veryFirst = false;
			bool veryLast = false;

			bool foundSectionId = false;

			//iterate through possible section ids
			for (int idx = 0; idx < partSections.Count; idx++) {
				if (partSections[idx].sectionId == sessionForm.sectionId) {
					//found the current section id, start with some assumptions about previous and next section ids
					foundSectionId = true;
					currentSectionIndex = idx;
					prevSectionIndex = idx - 1;
					nextSectionIndex = idx + 1;

					//we may have to link to the last section of the previous part
					if (prevSectionIndex < 0) {
						//special case where we're on the first section of the first part
						if (partIdsInForm.IndexOf(prt.partId) == 0)//currentPartId == 1)
						{
							//veryFirst = true;
							int partIndexInForm = partsInForm.Count();
							List<def_Sections> prevPartSections = formsRepo.GetSectionsInPart(partsInForm[partIndexInForm - 1]);
							prevPartId = partsInForm[partIndexInForm - 1].partId;
							prevSectionIndex = prevPartSections.Count - 1;
						} else {
							int partIndexInForm = partIdsInForm.IndexOf(prt.partId);
							// occasionally a part with no sections may need to be skipped, which can happen with PartSectionsEnt records.
							int listIndex = 0;
							for (int i = 1; i < partIndexInForm; i++) {
								if (formsRepo.GetSectionsInPart(partsInForm[partIndexInForm - i]).Count() > 0) {
									listIndex = partIndexInForm - i;
									break;
								}
							}

							List<def_Sections> prevPartSections = formsRepo.GetSectionsInPart(partsInForm[listIndex]);
							prevPartId = partsInForm[listIndex].partId;
							prevSectionIndex = prevPartSections.Count - 1;
						}

					}

					// Oliver - same here the PartSections are ordered - just use the first one in the List.
					// you can press F12 in Visual Studio and it will take you to the method.
					// Data/Concrete/FormsRepository line 200
					// The defined Interfaces are in Data/Abstract/IFormsRepository.cs

					//we may have to link to he first section of the next part
					if (nextSectionIndex > (partSections.Count - 1)) {
						int partIndexInForm = partIdsInForm.IndexOf(prt.partId);
						if (partIndexInForm == (partsInForm.Count() - 1))  //formsRepo.GetPartById(nextPartId) == null)
						{
							//veryLast = true;
							nextPartId = partsInForm[0].partId;
							nextSectionIndex = 0;
						} else {
							// occasionally a part with no sections may need to be skipped, which can happen with PartSectionsEnt records.
							int listIndex = 0;
							for (int i = 1; i < partsInForm.Count() - 1 - partIndexInForm; i++) {
								if (formsRepo.GetSectionsInPart(partsInForm[partIndexInForm + i]).Count() > 0) {
									listIndex = partIndexInForm + i;
									break;
								}
							}

							nextPartId = partsInForm[listIndex].partId;
							nextSectionIndex = 0;
						}
					}

					break;
				}
			}

			if (!foundSectionId) {
				string msg = "current section id (" + sessionForm.sectionId + ") could not be found.\r\nListing candidates: (identifier / sectionId)";
				foreach (def_Sections sct in partSections) {
					msg += (sct == null) ? "null" : ("\r\n" + sct.identifier + " / " + sct.sectionId);
				}
				throw new Exception(msg);
			}

			//print some debugging info
			if (mLogger.IsDebugEnabled) {
				mLogger.Debug("* * *  ResultsController:Template method  * * * " +
						(veryFirst ? "this is the very first part/section" : "prevPartId: " + prevPartId.ToString() +
						", prevSectionIndex: " + prevSectionIndex.ToString()));
				mLogger.Debug("* * *  ResultsController:Template method  * * * " +
						(veryLast ? "this is the very last part/section" : "nextPartId: " + nextPartId.ToString() +
						", nextSectionIndex: " + nextSectionIndex.ToString()));
			}
			if (partSections.Count > 0) {
				int idxPeriod = prt.identifier.IndexOf('.');
				string partShortTitle = (idxPeriod < 0) ? prt.identifier : prt.identifier.Substring(0, idxPeriod);
				// amt.thisScreenCaption = partShortTitle + " - " + partSections[thisIdx].identifier;
				amt.thisScreenCaption = partShortTitle + " - " + partSections[currentSectionIndex].identifier;
				amt.thisScreenTitle = partSections[currentSectionIndex].title;

				if (!veryFirst) {
					List<def_Sections> prevPartSections = formsRepo.GetSectionsInPart(formsRepo.GetPartById(prevPartId));
					amt.prevScreenTitle = prevPartSections[prevSectionIndex].title;
					amt.prevScreenPartId = prevPartId.ToString();
					amt.prevScreenSectionId = prevPartSections[prevSectionIndex].sectionId.ToString();
					amt.prevScreenHref = "Template?partId=" + nextPartId + "&sectionId=" + amt.prevScreenSectionId;
					//if (prevPartId != currentPartId)
					//    amt.previousScreenHref = "Parts?formResultId=" + formsRepo.GetPartById(prevPartId).partId.ToString() + amt.previousScreenHref;
				}

				if (!veryLast) {
					def_Parts tempPart = formsRepo.GetPartById(nextPartId);
					if (tempPart != null) {
						List<def_Sections> tempPartSections = formsRepo.GetSectionsInPart(tempPart);
						amt.nextScreenTitle = tempPartSections[nextSectionIndex].title;
						amt.nextScreenPartId = nextPartId.ToString();
						amt.nextScreenSectionId = tempPartSections[nextSectionIndex].sectionId.ToString();
						amt.nextScreenHref = "Template?partId=" + nextPartId + "&sectionId=" + amt.nextScreenSectionId;
					}
				}
			}

			//pass a NavMenu model as a field in this items Model
			sessionForm.formIdentifier = form.identifier;
			amt.navMenuModel = getNavMenuModel(sessionForm, amt);


            //Start: added for enhancement Bug 13663 to be refactored
            amt.navMenuModel.formResultUser = amt.formResultUser;
            amt.navMenuModel.clientId = amt.clientId;
            amt.navMenuModel.formSatusText = amt.formSatusText;
            amt.navMenuModel.EligibilityEnddate = amt.EligibilityEnddate;
            amt.navMenuModel.FormVariantTitle = amt.FormVariantTitle;
            amt.navMenuModel.updatedDate = amt.updatedDate;
            amt.navMenuModel.CanUserChangeStatus = amt.CanUserChangeStatus;
            amt.navMenuModel.formResultId = amt.formResultId;
            //End: added for enhancement Bug 13663 to be refactored



            amt.ventureMode = SessionHelper.IsVentureMode;

			//special case for reports sections
			if ((sctn.identifier.EndsWith("Report") /*&& (Request["ignoreValidation"] as string) == null*/ )
					|| sctn.href.Equals("~/Views/Templates/SIS/reportErrors.cshtml")
					) {
				if (!ValidateFormResult(fr, amt)) {
					return View("~/Views/Templates/SIS/reportErrors.cshtml", amt);
				}
			}

			//add messages to the mdoel if necessary
			amt.validationMessages = validationMessages; //validationMessages from method params, normally null

			//add a message to the model if necessary
			string message = Request["message"] as string;
			if (message != null) {
				if (amt.validationMessages == null) {
					amt.validationMessages = new List<string>();
				}
				amt.validationMessages.Add(message);
			}

			// *** RRB 10/27/15 - added to fix problems with Venture and to be more efficient.
			if (!SessionHelper.IsVentureMode) {
				if (templateFileName.Equals("reportOptions.cshtml")) {
					amt.reportOptions = AJBoggs.Sis.Reports.SisReportOptions.BuildPdfReportOptions(fr.EnterpriseID);
				}
			}

			// * * * OT 3/24/16 - for SIS supplemental questions, pre-populate sis_s41f with some item prompts from section 1A 
			//  Don't do anything if there is already a response for sis_s41f
			//  (Bug 13132, #4 in description)
			if (sctn.identifier == "SQ" && amt.rspValues.ContainsKey("sis_s41f") && String.IsNullOrWhiteSpace(amt.rspValues["sis_s41f"])) {
				amt.rspValues["sis_s41f"] = String.Empty;

				//iterate through all section 1A items
				int itmCount = 0;
				bool isSisCAssessment = fr.def_Forms.identifier.Equals("SIS-C");
				def_Sections sct1A = formsRepo.GetSectionByIdentifier(isSisCAssessment ? "SIS-C 1A" : "SIS-A 1A");
				formsRepo.SortSectionItems(sct1A);
				foreach (def_SectionItems si in sct1A.def_SectionItems) {
					def_Sections subSct = formsRepo.GetSubSectionById(si.subSectionId);
					formsRepo.SortSectionItems(subSct);
					foreach (def_SectionItems subSi in subSct.def_SectionItems) {
						//for items that have an itemVariable with suffix "_ExMedSupport", check if that itemVariable have a response of "2"
						def_Items itm = subSi.def_Items;
						itmCount++;
						def_ItemVariables exMedSupIv = itm.def_ItemVariables.Where(iv => iv.identifier.EndsWith("_ExMedSupport")).FirstOrDefault();
						if (exMedSupIv != null) {
							def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(fr.formResultId, exMedSupIv.itemVariableId);
							if (rv != null && !String.IsNullOrWhiteSpace(rv.rspValue) && rv.rspValue == "2") {
								//append the item prompt to the pre-populated response for sis_s41f
								amt.rspValues["sis_s41f"] += itmCount + ". " + itm.prompt + "\n";
							}
						}

					}
				}
			}

			// *** OT 11/23/15 - added as test for Bug 12910 - Comparison: what changed from last approved application?
			//if we're on the ADAP form...
			if (form.identifier.Contains("ADAP")) {

				//get the previous formresult
				int? sortOrder = formsRepo.GetStatusDetailByMasterIdentifier(1, "CANCELLED").sortOrder;
				int userId = fr.subject.HasValue ? fr.subject.Value : -1;
                if (userId < 1)
                {
                    mLogger.Warn("subject < 1 for form result having formResultId = {0}.", fr.formResultId);
                }
				// * * * OT 1-19-16 added stipulation that "prevRes" is not the current formResult "fr"
				def_FormResults prevRes = null;
				if (sortOrder.HasValue && userId > 0) {
					prevRes = formsRepo.GetEntities<def_FormResults>(
							x => x.formId == fr.formId
							&& x.formResultId != fr.formResultId
							&& x.subject == userId
							&& x.formStatus != sortOrder)
						.OrderByDescending(x => x.dateUpdated)
						.FirstOrDefault();
				}
				// * * * OT 1-19-16 in order to duplicate existing behavior and avoid crashes,
				// default to using the current formResult as the previous if no others are applicable
				if (prevRes == null)
					prevRes = fr;

				List<string> remainingIdentifiers = amt.rspValues.Keys.ToList();

				IList<def_ItemResults> previousItemResults = formsRepo.GetItemResults(prevRes.formResultId);
				foreach (def_ItemResults ir in previousItemResults) {
					foreach (def_ResponseVariables rv in ir.def_ResponseVariables) {

						if (rv.def_ItemVariables == null)
							continue;

						remainingIdentifiers.Remove(rv.def_ItemVariables.identifier);
                        string rspValue = HttpUtility.HtmlDecode(rv.rspValue);

                        amt.rspValues.Add("PREVIOUS_" + rv.def_ItemVariables.identifier, rspValue);
					}
				}

				//populate amt with empty responses for fields Bmissing from previous formResult
				foreach (string ident in remainingIdentifiers) {
					amt.rspValues.Add("PREVIOUS_" + ident, "");
				}
			}

			SessionHelper.ResponseValues = amt.rspValues;

			//Displays save message
			ViewBag.Notify = "";
			ViewBag.NotifyMessage = "";
			if (Session["IsPageLoad"] != null) {
				if (!(bool)Session["IsPageLoad"]) {
					if (TempData["Savemsg"] != null && TempData["SavemsgHeader"] != null) {
						if (TempData["Savemsg"].ToString() != "" && TempData["SavemsgHeader"].ToString() != "") {
							ViewBag.Notify = TempData["SavemsgHeader"].ToString();
							ViewBag.NotifyMessage = TempData["Savemsg"].ToString();
						}
					}
				}
			}


			TempData["SavemsgHeader"] = "";
			TempData["Savemsg"] = "";
			return View(sctn.href, amt);

		}
		public ActionResult PageLoadEvents()
		{
			Session["IsPageLoad"] = true;
			return Json("");

		}

		public ActionResult PageLoadClickEvents()
		{
			Session["IsPageLoad"] = false;
			return Json("");

		}


        /// <summary>
        /// Funtion for Client Handout information
        /// </summary>
        /// <param name="formResultId">form result id </param>
        /// <returns>ClientHandout.cshtml</returns>
        [HttpPost]
        public ActionResult ClientHandOut(int formResultId)
        {
            bool hasAutorizationToFormResult = Applications.CACheckUserHasAccesstoFormResult(formResultId, SessionHelper.LoginStatus.UserID);

            if (!hasAutorizationToFormResult)
            {
                string basePortalUrl = WebConfigurationManager.AppSettings["UASAdminURL"];
                return Redirect(new Uri(new Uri(basePortalUrl), "Portal").ToString());
            }

            AuthenticationClient webclient = new AuthenticationClient();
            var result = webclient.ListHandout(formResultId);
            ClientHandout1 c1 = new ClientHandout1();
            foreach (ClientHandout c in result)
            {
                c1.Enrollment_Worker = c.Enrollment_Worker;
                c1.Phone_Number = c.Phone_Number;
                c1.MemberID = c.MemberID;
                c1.LoginId = c.LoginId;
                c1.SVF_no_laterthan = c.SVF_no_laterthan;
                c1.Re_Enrollment_Date = c.Re_Enrollment_Date;
            }
            //ViewBag.Website = ConfigurationManager.AppSettings["ClientHandoutWebsite"].ToString();
            string viewDirPath = Applications.GetAdapTemplatesViewDirPath(SessionHelper.LoginStatus.EnterpriseID);
            return View(viewDirPath + "ClientHandout.cshtml", c1);
        }

        /*
		 * Recursive function to get List of Items from sections and subsections
		 */
        //private void GetItemListIncludingSubSections(def_Sections sctn, Assmnts.Models.TemplateItems amt)
        //{
        //    //amt.notesItem = formsRepo.GetAllItemsInSection(sctn, true).FirstOrDefault();

        //    //amt.items = formsRepo.GetAllItemsInSection(sctn);
        //    formsRepo.SortSectionItems(sctn);
        //    //special case for subsection containing notes item
        //    if (sctn.multipleItemsPerPage == false)
        //    {
        //        // formsRepo.SortSectionItems(sctn);
        //        // amt.notesItem = sctn.def_SectionItems.First().def_Items;
        //        amt.notesItem = formsRepo.GetItemById(sctn.def_SectionItems.First().itemId);
        //        return;
        //    }


        //    foreach (def_SectionItems sctnItm in sctn.def_SectionItems)
        //    {
        //        if (sctnItm.subSectionId.GetValueOrDefault(0) == 0)     // No subsection, must be an Item
        //        {
        //            amt.items.Add( formsRepo.GetItemById(sctnItm.itemId) );
        //            // amt.items.Add(sctnItm.def_Items);
        //        }
        //        else
        //        {
        //            // amt.sections.Add(sctnItm.def_SubSections.def_Sections);
        //            // GetItemListFromAllSections(sctnItm.def_SubSections.def_Sections, amt);
        //            GetItemListIncludingSubSections(formsRepo.GetSubSectionById(sctnItm.subSectionId), amt);
        //        }
        //    }

        //    return;
        //}





    }
}
