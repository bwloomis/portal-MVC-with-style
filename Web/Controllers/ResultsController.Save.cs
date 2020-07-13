using AJBoggs.Def.Domain;
using AJBoggs.Sis.Domain;
using Assmnts.Business;
using Assmnts.Infrastructure;
using Assmnts.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using Assmnts.UasServiceRef;

namespace Assmnts.Controllers
{
	/*
	 * This controller is used to Save formResult data from the template screens.
	 * The templates are in sub-dirs to Views/Templates such as Views/Templates/SIS
	 * 
	 */
	public partial class ResultsController : Controller
	{
		[HttpGet]
		public ActionResult jsonUpdate()
		{
			return View("jsonUpdate");
		}

		[HttpPost]
		public void SaveFormResults(def_FormResults frmRes)
		{
			formsRepo.SaveFormResults(frmRes);
		}

		[HttpPost]
		/*  This method Saves data from the screen when the Submit button is clicked.
		 *  The concept is that we know which Section was last displayed from the session variables.
		 *  So we cycle through the ItemVariables and update the associated ResponseValues from the FormCollection of screen values.
		 */
		public ActionResult Save(FormCollection frmCllctn, Assmnts.Models.TemplateItems ti)
		{
			mLogger.Debug("  ResultController:Save");

			Dictionary<string, string> previousResponseValues = SessionHelper.ResponseValues;
			if (previousResponseValues != null) {
				//Throw out responses that haven't changed
				foreach (var previousResponseKvp in previousResponseValues) {
					if (!string.IsNullOrWhiteSpace(previousResponseKvp.Key)) {
						var newResponseValue = frmCllctn.GetValue(previousResponseKvp.Key);
						//If the value is bool and it is now false (checkbox for example) it won't be sent back in the form collection
						//we need to test for bool and set these values to false (if they are true)
						var itemVariable = formsRepo.GetItemVariableByIdentifier(previousResponseKvp.Key);
						//itemVariable will often be null because all the PREVIOUS_ identifiers get sent through also
						var impliedBooleanFalseResult = false;
						if (newResponseValue == null && itemVariable != null && itemVariable.baseTypeId == Assmnts.Constants.CAADAP.BASE_TYPE_BOOLEAN) {
							newResponseValue = new ValueProviderResult("0", "0", CultureInfo.CurrentCulture);
							impliedBooleanFalseResult = true;
						}
						if (newResponseValue != null) {
							//Compare previous with posted values
							//Note if they are both null or both empty we want to throw it out
							if (newResponseValue.AttemptedValue == null && previousResponseKvp.Value == null) {
								frmCllctn.Remove(previousResponseKvp.Key);
							} else if (newResponseValue.AttemptedValue != null && previousResponseKvp.Value != null) {
								var previousResponseString = previousResponseKvp.Value;
								var newResponseString = newResponseValue.AttemptedValue;
								bool previousResponseTrue = false;
								bool previousResponseFalse = false;
								//If it's a bool, try to convert 'false' to '0' and so on
								if (previousResponseTrue = previousResponseString.Equals("true", StringComparison.InvariantCultureIgnoreCase)
										|| (previousResponseFalse = previousResponseString.Equals("false", StringComparison.InvariantCultureIgnoreCase))) {

									if (itemVariable != null && itemVariable.baseTypeId == Assmnts.Constants.CAADAP.BASE_TYPE_BOOLEAN) {
										if (previousResponseFalse) {
											previousResponseString = "0";
										} else if (previousResponseTrue) {
											previousResponseString = "1";
										}
									}
								}
								if (newResponseString.Equals(previousResponseString)) {
									frmCllctn.Remove(previousResponseKvp.Key);
								} else if (impliedBooleanFalseResult) {
									//The values are different, but the form collection doesn't contain the result
									frmCllctn.Add(previousResponseKvp.Key, newResponseValue.AttemptedValue);
								}
							}
						}
					}
				}
			}

			// If User is not logged in, there are no session variables.
			// This prevents saving the formResultId
			if (!SessionHelper.IsUserLoggedIn) {
				return RedirectToAction("Index", "Account", null);
			}

			//check if the form is unchanged
			//bool unchanged = true;
			//foreach (string key in frmCllctn.Keys)
			//{
			//    HttpCookie oldVal = Request.Cookies["frmOriginal___" + key];
			//    if (oldVal != null && !frmCllctn[key].Trim().Equals(oldVal.Value.Replace("%0d%0a","").Trim()))
			//    {
			//        //mLogger.Debug("* * *  ResultsController:SaveSectionItems changed key: \"" + key + "\", original value: \"" + Session["frmOriginal___" + key] + "\", new value: \"" + frmCllctn[key] + "\"" );
			//        unchanged = false;
			//        break;
			//    }
			//}

			bool unchanged = false;
			SessionForm sf = SessionHelper.SessionForm;

			if (unchanged || sf.readOnlyMode) {
				mLogger.Debug("* * *  ResultsController:SaveSectionItems form is unchanged, skipping update");
			} else {
				mLogger.Debug("* * *  ResultsController:Save method  * * *    sectionId: {0}", sf.sectionId);

				// save responses to database

				//* * * OT 03/10/16 Switched to using AJBoggs\Def\Domain\UserData.SaveFormCollection.cs
				UserData ud = new UserData(formsRepo);
				ud.SaveFormCollection(frmCllctn, sf.sectionId, sf.formResultId);
				ud.SaveFileUploads(sf.formResultId, Request);

				def_FormResults formResult = formsRepo.GetFormResultById(sf.formResultId);
				formResult.LastModifiedByUserId = SessionHelper.LoginStatus.UserID;
				formResult.dateUpdated = DateTime.Now;

				// Update the assigned field in the formResults based on the data in the Interviewer field on idprof1.
				if (sf.sectionId == 1) {
					def_ItemVariables interviewerVar = formsRepo.GetItemVariableByIdentifier("sis_int_id");
					if (interviewerVar != null) {
						def_ItemResults ir = formsRepo.GetItemResultByFormResItem(formResult.formResultId, interviewerVar.itemId);
						if (ir != null) {
							def_ResponseVariables rv = formsRepo.GetResponseVariablesByItemResultItemVariable(ir.itemResultId, interviewerVar.itemVariableId);
							if (rv != null && !String.IsNullOrEmpty(rv.rspValue)) {
								int rspInt;
								if (Int32.TryParse(rv.rspValue, out rspInt)) {
									formResult.assigned = rspInt;
								} else {
									mLogger.Error("Error converting response value {0} to int.", rv.rspValue);
								}
							}
						}
					}
				}

				formsRepo.Save();

				//set status to "in progress" for SIS forms
				//if (sf.formIdentifier.Equals("SIS-A") || sf.formIdentifier.Equals("SIS-C"))
				//{

				//if ( (fr.reviewStatus != ReviewStatus.APPROVED) && (fr.reviewStatus != ReviewStatus.REVIEWED) && (fr.reviewStatus != ReviewStatus.PRE_QA) )
				//{
				//    fr.formStatus = (byte)FormResults_formStatus.IN_PROGRESS;
				//    // this will be saved to the database in one of the scoring updates below    
				//}

				//}

				bool isSisForm = sf.formIdentifier.Equals("SIS-A") || sf.formIdentifier.Equals("SIS-C");

				//run single-section validation for sis forms
				if (isSisForm) {
					List<string> validationErrorMessages;
					bool ssValidationFailed = SisOneOffValidation.RunSingleSectionOneOffValidation(
							formsRepo, frmCllctn, sf.sectionId, out validationErrorMessages);
					if (ssValidationFailed) {
						return Template(sf.sectionId, validationErrorMessages);
					}

					//run full-assessment validation and update scores for completed SIS forms
					if (formResult.formStatus == (byte)FormResults_formStatus.COMPLETED || formResult.locked) {
						//if validation passes, it will trigger scoring automatically
						if (sf.sectionId != 504 && !ValidateFormResult(formResult)) // Section 504 is interview planning                        
						{
							//if validation fails set the form status to in-progress
							formsRepo.SetFormResultStatus(formResult, (byte)FormResults_formStatus.IN_PROGRESS);
						}
					}
				}
			}

			// string ctrl = "Results";
			// if (frmCllctn.AllKeys.Contains("UseThisControllerForTemplate"))
			//    ctrl = frmCllctn["UseThisControllerForTemplate"];

			//debug runtime
			//TimeSpan duration = DateTime.Now - startTime;
			//using (StreamWriter sw = System.IO.File.AppendText(@"C:\Users\otessmer\Desktop\log.txt"))
			//{
			//    sw.WriteLine(duration.Milliseconds);
			//}

			// if necessary, redirect to a different section now that we've saved the results
			string navSectionId = String.Empty;
			if (frmCllctn.AllKeys.Contains("navSectionId")) {
				navSectionId = frmCllctn["navSectionId"];
				if (!String.IsNullOrEmpty(navSectionId)) {
					if (navSectionId == "search")
						return RedirectToAction("Index", "Search", new { });

					if (navSectionId == "logout") {
						return RedirectToAction("LogoutUAS", "Account");
					}

					if (navSectionId == "new") {
						return RedirectToAction("NewBlankAssessment", "Search", new { formId = SessionHelper.Read<int>("newFormId") });
					}

                    // Redirect subforms to the original parent page.
                    if (navSectionId == "SubForm")
                    {
                        SessionForm psf = (SessionForm)Session["ParentFormData"];
                        return RedirectToAction("ToTemplate", "Adap", new { 
                            formResultId = psf.formResultId, 
                            formId = psf.formId, 
                            partId = psf.partId, 
                            sectionIdOverride = psf.sectionId 
                        });
                    }

					// Must be a Form Part Section
					//Displays successful save message
					Session["part"] = frmCllctn["navPartId"];
					Session["form"] = frmCllctn["navFormId"];
				}
			} else {
				navSectionId = sf.sectionId.ToString();
			}

			if (Session["IsPageLoad"] != null) {
			}
			if (navSectionId == "703") {
				TempData["Savemsg"] = "Contact info save successful";
				TempData["SavemsgHeader"] = "Saved";
			} else if (navSectionId == "704") {
				TempData["Savemsg"] = "Demographics info save successful";
				TempData["SavemsgHeader"] = "Saved";
			} else if (navSectionId == "705") {
				TempData["Savemsg"] = "Clinical info save successful";
				TempData["SavemsgHeader"] = "Saved";
			} else if (navSectionId == "706") {
				TempData["Savemsg"] = "Health Coverage info save successful";
				TempData["SavemsgHeader"] = "Saved";
			} else if (navSectionId == "726") {
				TempData["Savemsg"] = "Income info save successful";
				TempData["SavemsgHeader"] = "Saved";
			} else if (navSectionId == "732") {
				TempData["Savemsg"] = "Insurance Assistance info save successful";
				TempData["SavemsgHeader"] = "Saved";
			} else if (navSectionId == "734") {
				TempData["Savemsg"] = "Attachment info save successful";
				TempData["SavemsgHeader"] = "Saved";
			} else if (navSectionId == "753") {
				TempData["Savemsg"] = "Medical Out of Pocket info save successful";
				TempData["SavemsgHeader"] = "Saved";
			} else if (navSectionId == "708") {
				TempData["Savemsg"] = "Consent&Submit info save successful";
				TempData["SavemsgHeader"] = "Saved";
			} else if (navSectionId == "756") {
				TempData["Savemsg"] = "Eligibility info save successful";
				TempData["SavemsgHeader"] = "Saved";
			}
            else if (navSectionId == "759")
            {
                TempData["Savemsg"] = "SVF info save successful";
                TempData["SavemsgHeader"] = "Saved";
            }
            else
            {
                TempData["Savemsg"] = "";
                TempData["SavemsgHeader"] = "";
            }

			if (sf.sectionId == 708) {
				def_FormResults formResult = formsRepo.GetFormResultById(sf.formResultId);
				IEnumerable<def_FormResults> frElgList = formsRepo.GetFormResultsByFormSubject(18, formResult.subject);

				var adapCaController = new AdapCaController(formsRepo);

				if (!frElgList.Any()) {
                    adapCaController.CreateElgibility(sf.formResultId);
				}

				adapCaController.CopyToEligibility("C1_FormSubmitEnrollmentSiteName", "C1_MemberSelectedEnrollmentSite", formResult.formResultId, formResult.subject.Value);
				formsRepo.Save();

			}

            //$$ Start of people Picker V2 Functionality

            PeoplePickerHelper objUtility = new PeoplePickerHelper(authClient, formsRepo);

            objUtility.SaveresultsForEnWrkerPicker(sf.sectionId, sf.formResultId);

            //$$ End of people Picker V2 Functionality

            var redirect = frmCllctn["navRedirect"];
            if (!string.IsNullOrWhiteSpace(redirect))
            {
                return Redirect(redirect);
            }
            else
            {
                return RedirectToAction("Template", "Results", new { sectionId = navSectionId });
            }
		}

		[HttpPost]
		public ActionResult SaveThenRedirect(FormCollection frmCllctn = null, TemplateItems itm = null)
		{
			Save(frmCllctn, itm);

			if (!frmCllctn.AllKeys.Contains("redirectUrl")) {
				throw new Exception("Expected key \"redirectUrl\" to appear in form collection");
			}

			return Redirect(frmCllctn["redirectUrl"]);
		}



	}
}
