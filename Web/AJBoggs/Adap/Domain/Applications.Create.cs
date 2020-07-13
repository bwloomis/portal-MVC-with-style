using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AJBoggs.Adap.Domain;
using Assmnts;
using Assmnts.Controllers;
using Data.Abstract;

namespace AJBoggs.Adap.Domain
{
	/*
	 * This class is used to process ADAP Applications with DEF.
	 * All Applications (Forms), Part, Sections, Items 
	 * 
	 * It should be used by Controllers and WebServices for special ADAP Application processing.
	 * 
	 */
	public partial class Applications
	{
		//Public instance methods

		/// <summary>
		/// Used by the create application method to populate the application with data from UAS.
		/// Creates a new forms result object for the application.
		/// </summary>
		/// <param name="subjectUserId">Numerical Identifier of the user creating a new application.</param>
		/// <param name="formId">Numerical Identifier of the form to use for the new application</param>
		/// <param name="DataToPopulate">Dictionary object listing the items to be pulled from UAS.</param>
		/// <returns>Returns the forms result object created by this method.</returns>
		public def_FormResults CreateFormResultPopulatedFromUAS(int entId, int groupId, int subjectUserId, int formId, Dictionary<string, string> DataToPopulate)
		{
			Debug.WriteLine("*** populateFromUAS ***");
			def_FormResults frmRes = Assmnts.Models.FormResults.CreateNewFormResultModel(formId.ToString());
			frmRes.EnterpriseID = entId;
			frmRes.GroupID = groupId;
			frmRes.subject = subjectUserId;
			frmRes.interviewer = subjectUserId;
			frmRes.assigned = subjectUserId;
			frmRes.reviewStatus = 0;

			foreach (String s in DataToPopulate.Keys) {
				def_Items item = formsRepo.GetItemByIdentifier(s);
				def_ItemResults ir = new def_ItemResults() {
					itemId = item.itemId,
					sessionStatus = 0,
					dateUpdated = DateTime.Now
				};

				frmRes.def_ItemResults.Add(ir);

				foreach (var iv in formsRepo.GetItemVariablesByItemId(item.itemId)) {
					// Note for General forms like ADAP there should only be 1 ItemVariable per Item
					def_ResponseVariables rv = new def_ResponseVariables();
					rv.itemVariableId = iv.itemVariableId;
					// rv.rspDate = DateTime.Now;    // RRB 11/11/15 The date, fp, and int fields are for the native data conversion.
					rv.rspValue = DataToPopulate[s];

					formsRepo.ConvertValueToNativeType(iv, rv);
					ir.def_ResponseVariables.Add(rv);
				}
			}

			Debug.WriteLine("PopulateFromUAS FormResults populated.");
			return frmRes;
		}

		public void PopulateItemsFromEligibilityDashboard(def_FormResults formResult, List<ItemToPrepopulate> itemsToPrepopulate)
		{
			if (formResult == null) {
				return;
			}
			if (itemsToPrepopulate == null || itemsToPrepopulate.Count == 0) {
				return;
			}
			//Get Eligibility Dashboard form response
			def_FormResults eligibilityDashboardFormResult = formsRepo.GetFormResultsByFormId(Assmnts.Constants.CAADAP.CA_ADAP_DASHBOARD_FORM_ID)
					.Where(x => x.subject == formResult.subject)
					.OrderByDescending(x => x.dateUpdated)
					.FirstOrDefault();
			if (eligibilityDashboardFormResult == null) {
				return;
			}

			PopulateItems(formResult, eligibilityDashboardFormResult, itemsToPrepopulate);
		}

		public void PopulateItemsFromPreviousApplication(def_FormResults formResult, List<ItemToPrepopulate> itemsToPrepopulate)
		{
			if (formResult == null) {
				return;
			}
			if (itemsToPrepopulate == null || itemsToPrepopulate.Count == 0) {
				return;
			}
			def_FormResults previousFormResult = formsRepo.GetFormResultsByFormId(formResult.formId)
				.Where(q => q.subject == formResult.subject && q.formResultId != formResult.formResultId)
				.OrderByDescending(q => q.dateUpdated)
				.FirstOrDefault();

			if (previousFormResult == null) {
				return;
			}

			PopulateItems(formResult, previousFormResult, itemsToPrepopulate);
		}

		/// <summary>
		/// Used by the create application method to populate the application with data from the previous application.
		/// </summary>
		/// <param name="frmRes">Form results for the new application.</param>
		/// <param name="previousApplicationItemIdentifiers">String Array listing ADAP identifers in the database</param>
		[Obsolete("Prefer using PopulateItemsFromPreviousApplication(def_FormResults, List<ItemToPrepopulate>).", false)]
		public void PopulateItemsFromPrevApplication(def_FormResults frmRes, string[] previousApplicationItemIdentifiers)
		{
			Debug.WriteLine("*** PopulateItemsFromPrevApplication ***");
			//Convert.ToInt32(frmRes.subject);
			IQueryable<def_FormResults> query = formsRepo.GetFormResultsByFormId(frmRes.formId);
			query = query.Where(q => q.subject == frmRes.subject);
			query = query.OrderByDescending(q => q.dateUpdated);
			def_FormResults prevApp = query.FirstOrDefault();

			if (prevApp != null) {
				foreach (string s in previousApplicationItemIdentifiers) {
					def_Items item = formsRepo.GetItemByIdentifier(s);
					if (item != null) {
						def_ItemResults noExist =
								(from check in frmRes.def_ItemResults.OfType<def_ItemResults>()
								 where check.itemId == item.itemId
								 select check).FirstOrDefault();

						// *** RRB 11/12/15 - test if the statements above/below are equivalent
						// bool itmRsltExists = frmRes.def_ItemResults.Any(ir => ir.itemId == item.itemId);

						// ItemResult may have been populated by UAS.
						if (noExist == null) {
							def_ItemResults ir = new def_ItemResults() {
								itemId = item.itemId,
								sessionStatus = 0,
								dateUpdated = DateTime.Now
							};
							frmRes.def_ItemResults.Add(ir);

							def_ItemResults prevItmRslt =
									(from prev in prevApp.def_ItemResults.OfType<def_ItemResults>()
									 where prev.itemId == item.itemId
									 select prev).FirstOrDefault();

							// *** RRB 11/12/15 - test if the statements above/below are equivalent
							// bool PrevItmRsltExists = prevApp.def_ItemResults.Any(pir => pir.itemId == item.itemId);

							if (prevItmRslt != null) {
								def_ResponseVariables prevRV =
										(from prev in prevItmRslt.def_ResponseVariables.OfType<def_ResponseVariables>()
										 where prev.itemResultId == prevItmRslt.itemResultId
										 select prev).FirstOrDefault();

								// *** RRB 11/12/15 - test if the statements above/below are equivalent
								// bool prevRspVarExists = prevItmRslt.def_ResponseVariables.Any(prv => prv.itemResultId == prevItmRslt.itemResultId);

								foreach (var iv in formsRepo.GetItemVariablesByItemId(item.itemId)) {
									// Note for General forms like ADAP there should only be 1 ItemVariable per Item
									def_ResponseVariables rv = new def_ResponseVariables();
									rv.itemVariableId = iv.itemVariableId;
									// rv.rspDate = DateTime.Now;  RRB - 11/12/15 - populated by ConvertValueToNative below if necessary.
									rv.rspValue = (prevRV != null) ? prevRV.rspValue : String.Empty;

									try {
										formsRepo.ConvertValueToNativeType(iv, rv);
									}
									catch (Exception e) {
										Debug.WriteLine("error converting response to native type for itemvariable \""
												+ iv.identifier + "\": " + e.Message);
									}

									ir.def_ResponseVariables.Add(rv);
								}
							}
						}
					}
				}
			}
		}

		//Private instance methods

		private void PopulateItems(def_FormResults formResult, def_FormResults previousFormResult, List<ItemToPrepopulate> itemsToPrepopulate)
		{
			List<Tuple<def_Items, ItemToPrepopulate>> itemTuples = new List<Tuple<def_Items, ItemToPrepopulate>>();
			foreach (ItemToPrepopulate itemToPrepopulate in itemsToPrepopulate) {
				def_Items item = formsRepo.GetItemByIdentifier(itemToPrepopulate.Title + Constants.CAADAP.IDENTIFIER_SUFFIX); //This is coming from cache
				if (item != null) {
					item = new def_Items(item); //We can't modify an item from the cache
					itemTuples.Add(new Tuple<def_Items, ItemToPrepopulate>(item, itemToPrepopulate));
				}
			}
			if (itemTuples.Count == 0) {
				return;
			}

			formsRepo.GetItemLabelsResponses(previousFormResult.formResultId, itemTuples.Select(x => x.Item1).ToList());

			foreach (var itemTuple in itemTuples) {
				def_Items item = itemTuple.Item1;
				ItemToPrepopulate itemToPrepopulate = itemTuple.Item2;
				def_ItemResults itemResult = formResult.def_ItemResults.Where(x => x.itemId == item.itemId).FirstOrDefault();
				def_ItemResults previousItemResult = item.def_ItemResults.FirstOrDefault();
				def_ItemVariables itemVariable = item.def_ItemVariables.FirstOrDefault();
				if (previousItemResult == null || itemVariable == null) {
					continue;
				}
				def_ResponseVariables previousResponseVariable = itemVariable.def_ResponseVariables.FirstOrDefault();
				if (previousResponseVariable != null && !String.IsNullOrWhiteSpace(previousResponseVariable.rspValue)) {
					bool addItemResult = false;
					bool addResponseVariable = false;
					if (itemResult == null) {
						itemResult = new def_ItemResults() {
							itemId = item.itemId,
							sessionStatus = 0,
							dateUpdated = DateTime.Now
						};
						addItemResult = true;
					}
					def_ResponseVariables responseVariable = itemResult.def_ResponseVariables.FirstOrDefault();
					if (responseVariable == null) {
						responseVariable = new def_ResponseVariables {
							itemVariableId = itemVariable.itemVariableId
						};
						addResponseVariable = true;
					}
					if (String.IsNullOrWhiteSpace(responseVariable.rspValue)) {
						bool documentCopyFailed = false;
						if (itemToPrepopulate.Document.HasValue && itemToPrepopulate.Document.Value) {
							string newSaveDirectoryPath = Assmnts.Business.Uploads.FileUploads.GetSaveDirectoryPath(formResult.formResultId, itemToPrepopulate.Title);
							string newDocumentPath;
							if (TryCopyDocument(previousResponseVariable.rspValue, newSaveDirectoryPath, out newDocumentPath)) {
								responseVariable.rspValue = newDocumentPath;
							} else {
								documentCopyFailed = true;
							}
						} else {
							//The item is not a document, just copy the value
							responseVariable.rspValue = previousResponseVariable.rspValue;
							try {
								formsRepo.ConvertValueToNativeType(itemVariable, responseVariable);
							}
							catch (Exception ex) {
								Debug.WriteLine("error converting response to native type for itemvariable \""
										+ itemVariable.identifier + "\": " + ex.Message);
							}
						}
						if (!documentCopyFailed) {
							if (addResponseVariable) {
								itemResult.def_ResponseVariables.Add(responseVariable);
							}
							if (addItemResult) {
								itemResult.dateUpdated = DateTime.Now;
								formResult.def_ItemResults.Add(itemResult);
							}
						}
					}

				}
			}
		}

		private bool TryCopyDocument(string previousDocumentPath, string newSaveDirectoryPath, out string newDocumentPath)
		{
			newDocumentPath = null;
			if (String.IsNullOrWhiteSpace(previousDocumentPath)) {
				mLogger.Error("previousDocumentPath was null or whitespace.");
				return false;
			}
			if (String.IsNullOrWhiteSpace(newSaveDirectoryPath)) {
				mLogger.Error("newSaveDirectoryPath was null or whitespace.");
				return false;
			}
			try {
				if (!File.Exists(previousDocumentPath)) {
					mLogger.Error("Previous document didn't exist at {0}.", previousDocumentPath);
					return false;
				}
				//Parse old path for file name
				FileInfo fileInfo = new FileInfo(previousDocumentPath);
				string fileName = fileInfo.Name;
				if (String.IsNullOrWhiteSpace(fileName)) {
					mLogger.Error("Unable to parse previous document path for file name. Previous path was {0}.", previousDocumentPath);
					return false;
				}
				//Create new save directory
				DirectoryInfo newSaveDirectoryInfo = null;
				if (Directory.Exists(newSaveDirectoryPath)) {
					newSaveDirectoryInfo = new DirectoryInfo(newSaveDirectoryPath);
				} else {
					newSaveDirectoryInfo = Directory.CreateDirectory(newSaveDirectoryPath);
				}
				if (newSaveDirectoryInfo == null) {
					mLogger.Error("Unable to create new save directory at {0}.", newSaveDirectoryPath);
					return false;
				}
				//Copy the file
				newDocumentPath = Path.Combine(newSaveDirectoryInfo.FullName, fileName);
				File.Copy(previousDocumentPath, newDocumentPath, true);
			}
			catch (Exception ex) {
				if (ex.IsCritical()) {
					throw ex;
				}
				mLogger.Error(ex, "Caught exception copying previous document.");
				return false;
			}
			return true;
		}
	}
}
