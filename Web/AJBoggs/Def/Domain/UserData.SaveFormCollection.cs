using Assmnts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security.AntiXss;


namespace AJBoggs.Def.Domain
{
	/*
	 * This parital class is used to Save data from a FormCollection to the FormResults in the Forms Repository
	 * It should be used by Controllers and WebServices to Save User Data
	 * 
	 */
	public partial class UserData
	{

		/// <summary>
		/// Saves an HTML FormCollection to the FormsRepository
		/// </summary>
		/// <param name="frmCllctn"></param>
		/// <param name="sectionId"></param>
		/// <param name="formResultId"></param>
		/// <returns></returns>
		public bool SaveFormCollection(FormCollection frmCllctn, int sectionId, int formResultId)
		{
			//for debugging runtime
			//DateTime startTime = DateTime.Now;

			Debug.WriteLine("***  SaveFormCollection sectionId: " + sectionId.ToString() + "   formResultId: " + formResultId.ToString());

			def_Sections sctn = formsRepo.GetSectionById(sectionId);
			if (sctn != null) {
				SaveSectionsRecursively(sctn, frmCllctn, formResultId);
			}

			// All responses are now in Entity Framework, so Save all the Entities to the Forms Repository
			formsRepo.Save();

			return true;

		}

		/// <summary>
		/// Called from JsonImports which is used by Venture
		/// </summary>
		/// <param name="sctn"></param>
		/// <param name="frmCllctn"></param>
		/// <param name="formResultId"></param>
		public void FormResultsSaveSaveSectionItems(def_Sections sctn, FormCollection frmCllctn, int formResultId)
		{
			SaveSectionItems(sctn, frmCllctn, formResultId);
			formsRepo.Save();
		}

		/// <summary>
		/// Saves User Data associated with a Section by processing Items and SubSections (unlimited nesting).
		/// </summary>
		/// <param name="sctn"></param>
		/// <param name="frmCllctn"></param>
		/// <param name="formResultId"></param>

		private void SaveSectionsRecursively(def_Sections sctn, FormCollection frmCllctn, int formResultId)
		{
			try {
				SaveSectionItems(sctn, frmCllctn, formResultId);
			}
			catch (Exception ex) {
				Debug.WriteLine(ex.Message);
			}

			// Process the subSections (if any)
			foreach (def_SectionItems sctnItm in sctn.def_SectionItems) {
				if (sctnItm.subSectionId.GetValueOrDefault() > 0) {
					SaveSectionsRecursively(formsRepo.GetSubSectionById(sctnItm.subSectionId), frmCllctn, formResultId);
				}
			}

			return;
		}

		/// <summary>
		/// Save the ItemResults and ResponseVariables associated with a SectionItem.
		/// </summary>
		/// <param name="sctn"></param>
		/// <param name="frmCllctn"></param>
		/// <param name="formResultId"></param>
		private void SaveSectionItems(def_Sections sctn, FormCollection frmCllctn, int formResultId)
		{
			Debug.WriteLine("* * *  UserData.SaveFormCollection:SaveSectionItems ==> " + sctn.sectionId.ToString() + ": " + sctn.title);

			formsRepo.GetSectionItems(sctn);
			foreach (def_SectionItems sctnItm in sctn.def_SectionItems) {
				// Skip the subSections (only process Items)
				if (sctnItm.subSectionId.GetValueOrDefault() > 0) {
					continue;
				}
				// Get item variables from cache
				var itemVariables = formsRepo.GetItemVariablesByItemId(sctnItm.itemId);
				foreach (var itemVariable in itemVariables) {
					var deleteResponseVariable = false;
					if (frmCllctn.AllKeys.Contains("DELETE_RESPONSE_FROM_DB_" + itemVariable.identifier)) {
						deleteResponseVariable = true;
					}
					if (frmCllctn.AllKeys.Contains(itemVariable.identifier) || deleteResponseVariable) {
						string value = frmCllctn[itemVariable.identifier];
						value = value == null ? String.Empty : value.Trim();

                        // HTML encode the value
                        value = AntiXssEncoder.HtmlEncode(value, false);

						try {
							#region Comment1
							// OT  03/10/16 - no, so far this isn't used for any screens outside of ADAP. 
							//                  but it could be used for other inputs or special one-off code
							//                  e.g. within cshtml, a "gender=male" dropdown option could be set up to automatically 
							//                  delete any existing responses to a "pregnancy" item in a different section
							// RRB 01/16/16 - this seems to be ADAP specific code for the Income HTML screen.
							//                  Is it meant to be used for other inputs as well??
							// OT 9/16/15 - added possibility to explicitly delete responses (in order to fix Bug 12652, comment 64, part 1)
							#endregion
							if (deleteResponseVariable) {
								def_ResponseVariables rvToDelete = formsRepo.GetResponseVariablesByFormResultItemVarId(formResultId, itemVariable.itemVariableId);
								formsRepo.DeleteResponseVariableNoSave(rvToDelete);
							} else if (itemVariable.baseTypeId == 1 && value.Equals("unanswered", StringComparison.InvariantCultureIgnoreCase)) {
								#region Comment2
								// *** IMPORTANT ***
								// Use the FormCollection from the HTML form to collect the values entered on the screen.
								// This requires that ALL templates must use fields (INPUT, etc.) that have the 'name' attribute match the ItemVariable 'identifier'
								// So using MVC Razor fields will probably not work (at least I couldn't get it to work).

								// RRB 1/30/15 - since text fields aren't being returned if unchanged, need to skip them or the data will get deleted when iv.identifier not found!!
								//          *** The problem this may cause is that there will be ItemResults w/o ResponseVariables ***

								// OT 2/6/15 - for boolean itemVariables that are missing from formCollection, they will be given the default value of false (empty string) rather than being skipped

								// OT 9/2/15 - undid change from 2/6/15, boolean itemvariables missing from formCollection will not be given a response variable
								// RB 9/7/15 - undid change from 9/2/15, checkboxes not being Saved due to 'unchecked' not being sent from HTML Form.
								//if (!frmCllctn.AllKeys.Contains(iv.identifier) && (iv.baseTypeId != 1)) {
								//    continue;
								//}

								//string val = frmCllctn.AllKeys.Contains(iv.identifier) ? frmCllctn[iv.identifier].Trim() : String.Empty;

								// OT 3/24/16 - I thought we were already doing this: "unanswered" response to booleans should be saved as nulls (Bug 13132, #1 in description)
								#endregion

								value = null;
							}
							def_ItemResults itemResult = SaveItemResult(formResultId, itemVariable.itemId);
							if (!deleteResponseVariable) {
								SaveResponseVariable(itemResult, itemVariable, value);
							}
						}
						catch (Exception ex) {
							Debug.WriteLine("* * *  FormResultsSave SaveSectionItems Dictionary item not found. ==> " + itemVariable.identifier + ": " + ex.Message);
							throw new Exception("* * *  FormResultsSave SaveSectionItems Dictionary item not found. ==> " + itemVariable.identifier + ": " + ex.Message);
						}
					}
				}
			}
		}
	}
}
