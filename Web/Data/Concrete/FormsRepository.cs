using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using AJBoggs;
using Assmnts;
using Assmnts.Infrastructure;
using Data.Abstract;
using NLog;
using System.Web;

namespace Data.Concrete
{
	public partial class FormsRepository : IFormsRepository
	{
		private const string PREVIOUS_PREFIX = "PREVIOUS_";
		private formsEntities db = DataContext.GetDbContext();
		private ICachedFormsEntities mCachedFormsEntities;
		private ILogger mLogger;

		// *** RRB 2/8/16 - The constructors and members below are being tested.
		//                  At this point, when Ninject is populating the Controller with FormsRepository
		//                    the Session data does not appear to be initialized yet.

		private int frEntId;
		private int frLangId;

		// Constructors
		public FormsRepository(ILogger logger, ICachedFormsEntities cachedFormsEntities)
		{
			mLogger = logger;
			db.Database.CommandTimeout = 180;
			mCachedFormsEntities = cachedFormsEntities;
		}

		public FormsRepository(int langId)
		{
			mLogger = LogManager.GetCurrentClassLogger();
			db.Database.CommandTimeout = 180;
			mCachedFormsEntities = System.Web.Mvc.DependencyResolver.Current.GetService(typeof(ICachedFormsEntities)) as ICachedFormsEntities;
			frLangId = langId;
		}

		public FormsRepository(int langId, int entId)
		{
			mLogger = LogManager.GetCurrentClassLogger();
			db.Database.CommandTimeout = 180;
			mCachedFormsEntities = System.Web.Mvc.DependencyResolver.Current.GetService(typeof(ICachedFormsEntities)) as ICachedFormsEntities;
			frEntId = entId;
		}

		/// <summary>
		/// Not cloning
		/// </summary>
		/// <param name="lookupCode"></param>
		/// <returns></returns>
		public def_LookupMaster GetLookup(string lookupCode)
		{

			//I copied this method from the WebAPI controller DefwsController and made some minor changes

			mLogger.Debug("GetLookup  lookupCode: " + lookupCode);
			if (!SessionHelper.IsUserLoggedIn) {
				return null;
			}
			if (lookupCode.StartsWith(PREVIOUS_PREFIX)) {
				lookupCode = lookupCode.Substring(PREVIOUS_PREFIX.Length);
			}
			int groupId = SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0];
			mLogger.Debug("GetLookup  lookupCode: " + lookupCode + "     Enterprise: " + SessionHelper.LoginStatus.EnterpriseID + "   GroupID:" + groupId);

			// TODO Make this use a session variable later!!
			short lang = 1;
			def_LookupMaster master = mCachedFormsEntities.def_LookupMaster
					.Where(x => x.lookupCode == lookupCode)
					.Select(x => new def_LookupMaster(x))
					.SingleOrDefault();
			if (master == null) {
				return null;
			}
			var details = mCachedFormsEntities.def_LookupDetail
							.Where(x => x.lookupMasterId == master.lookupMasterId && x.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && x.GroupID == groupId)
							.OrderBy(m => m.displayOrder)
							.Select(x => new def_LookupDetail(x))
							.ToList<def_LookupDetail>();
			if (details.Count == 0) {
				details = mCachedFormsEntities.def_LookupDetail
						.Where(x => x.lookupMasterId == master.lookupMasterId && x.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && x.GroupID == 0)
						.OrderBy(m => m.displayOrder)
						.Select(x => new def_LookupDetail(x))
						.ToList<def_LookupDetail>();
			}
			master.def_LookupDetail = details;

			foreach (def_LookupDetail lookupDetail in master.def_LookupDetail) {
				lookupDetail.def_LookupText = mCachedFormsEntities.def_LookupText
						.Where(x => x.lookupDetailId == lookupDetail.lookupDetailId && x.langId == lang)
						.ToList<def_LookupText>();
			}

			return master;
		}

		/*
		 * Converts the string values to native types for the database storage.
		 */
		public void ConvertValueToNativeType(def_ItemVariables iv, def_ResponseVariables rv)
		{
			if (String.IsNullOrEmpty(rv.rspValue)) {
				// Process BOOLEANs that are blank
				if (iv.baseTypeId > 1) {
					return;
				}
				if (rv.rspValue == null) {
					return;
				}
			}

			switch (iv.baseTypeId) {
				case 1:             // boolean
														// *** This logic and the logic above are not working !!!!
														// *** The HTML screen appears to be returning NULL for unchecked.
														// *** Unable to detrmine if it was ever checked
					if (rv.rspValue.Equals(String.Empty)) {
						rv.rspValue = "0";             // Set False to '0' (zero)
					}
					break;
				case 3:
					// date - RRB - 10/5/2015 - added CultureInfo to fix problem with Alberta, Canada
					// rv.rspDate = Convert.ToDateTime(rv.rspValue);
					DateTime dateValue;
					if (DateTime.TryParse(rv.rspValue, CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out dateValue)) {
						rv.rspDate = dateValue;
					}
					break;
				case 8:
					// integer
					int intValue;
					//TryParse does not throw any exceptions on the basis of data provided in argument.
					if (Int32.TryParse(rv.rspValue, out intValue)) {
						rv.rspInt = intValue;
					}
					break;
				case 6:
					// Single Floating point
					float floatValue;
					if (Single.TryParse(rv.rspValue, out floatValue)) {
						rv.rspFloat = floatValue;
					}
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Calls the sp_GetGetItemLabelsResponses stored procedure and
		/// for each item for the particular form result, populates the def_ItemResults and def_ItemVariables. Populates the
		/// def_ResponseVariables on the elements of the def_ItemResults and def_ItemVariables lists.
		/// </summary>
		/// <param name="formResultId"></param>
		/// <param name="items"></param>
		public void GetItemLabelsResponses(int formResultId, IList<def_Items> items)
		{
			if (items == null || items.Count == 0) {
				return;
			}
			string itemIdsCsv = string.Join(",", items.Select(x => x.itemId).ToList());
			if (itemIdsCsv.Length > Constants.Data.FormsRepository.SP_GETITEMLABELSRESPONSES_ITEMIDSCSV_MAX_LENGTH) {
				throw new InvalidOperationException(String.Format("The length({0}) of the string itemIdsCsv is greater than the max length({1}) of the stored procedure parameter.",
						itemIdsCsv.Length, Constants.Data.FormsRepository.SP_GETITEMLABELSRESPONSES_ITEMIDSCSV_MAX_LENGTH));
			}
			List<ItemLabelResponse> itemLabelResponses = db.sp_GetItemLabelsResponses(formResultId, itemIdsCsv).ToList();
			foreach (var item in items) {
				var _itemLabelResponses = itemLabelResponses.Where(x => x.Item_identifier == item.identifier).ToList();
				foreach (var itemLabelResponse in _itemLabelResponses) {
					if (itemLabelResponse.ItemResult_itemResultId.HasValue) {
						var itemResult = new def_ItemResults {
							itemResultId = itemLabelResponse.ItemResult_itemResultId ?? 0,
							formResultId = itemLabelResponse.ItemResult_formResultId ?? 0,
							itemId = itemLabelResponse.ItemResult_itemId ?? 0,
							sessionStatus = itemLabelResponse.ItemResult_sessionStatus ?? 0,
							dateUpdated = itemLabelResponse.ItemResult_dateUpdated ?? DateTime.MinValue
						};
						//If I select new (from linq projection) i need to initialize the navigation properties
						if (!item.def_ItemResults.Where(x => x.itemResultId == itemLabelResponse.ItemResult_itemResultId).Any()) {
							item.def_ItemResults.Add(itemResult);
						}
					}
					if (itemLabelResponse.ItemVariable_itemVariableId.HasValue) {
						var itemVariable = new def_ItemVariables {
							itemVariableId = itemLabelResponse.ItemVariable_itemVariableId ?? 0,
							itemId = itemLabelResponse.ItemVariable_itemId ?? 0,
							identifier = itemLabelResponse.ItemVariable_identifier,
							baseTypeId = itemLabelResponse.ItemVariable_baseTypeId ?? 0,
							defaultValue = itemLabelResponse.ItemVariable_defaultValue,
							outcomeDeclarationId = itemLabelResponse.ItemVariable_outcomeDeclarationId ?? 0
						};
						if (!item.def_ItemVariables.Where(x => x.itemVariableId == itemLabelResponse.ItemVariable_itemVariableId).Any()) {
							item.def_ItemVariables.Add(itemVariable);
						}
					}
					if (itemLabelResponse.ResponseVariable_responseVariableId.HasValue) {
						var responseVariable = new def_ResponseVariables {
							responseVariableId = itemLabelResponse.ResponseVariable_responseVariableId ?? 0,
							itemResultId = itemLabelResponse.ResponseVariable_itemResultId ?? 0,
							itemVariableId = itemLabelResponse.ResponseVariable_itemVariableId ?? 0,
							rspInt = itemLabelResponse.ResponseVariable_rspInt ?? 0,
							rspFloat = itemLabelResponse.ResponseVariable_rspFloat ?? 0d,
							rspDate = itemLabelResponse.ResponseVariable_rspDate ?? DateTime.MinValue,
							rspValue = itemLabelResponse.ResponseVariable_rspValue
						};
						var itemResult = item.def_ItemResults.Where(x => x.itemResultId == (itemLabelResponse.ItemResult_itemResultId ?? 0)).FirstOrDefault();
						if (itemResult != null) {
							//TODO make sure def_ResponseVariables doesn't contain this already
							itemResult.def_ResponseVariables.Add(responseVariable);
						}
						var itemVariable = item.def_ItemVariables.Where(x => x.itemVariableId == (itemLabelResponse.ItemVariable_itemVariableId ?? 0)).FirstOrDefault();
						if (itemVariable != null) {
							//TODO make sure def_ResponseVariables doesn't contain this already
							itemVariable.def_ResponseVariables.Add(responseVariable);
						}
					}
				}
			}
		}

		public void GetItemLabelsResponses(int formResultId, Assmnts.Models.TemplateItems amt, List<def_Items> items, bool allowNullResponseValues = false)
		{
			if (amt.fldLabels == null)
				amt.fldLabels = new Dictionary<string, string>();
			if (amt.itmPrompts == null)
				amt.itmPrompts = new Dictionary<string, string>();
			if (amt.rspValues == null)
				amt.rspValues = new Dictionary<string, string>();

			//instead of using the parameter list directly, reference a new list that also includes the notesItem from the model (if avaliable)
			mLogger.Debug("* * *  GetItemLabelsResponses items.Count:" + items.Count.ToString() + "   amt.items.Count:" + amt.items.Count.ToString());
			items = new List<def_Items>(items);
			mLogger.Debug("* * *  GetItemLabelsResponses after Lists copy items.Count:" + items.Count.ToString() + "   amt.items.Count:" + amt.items.Count.ToString());
			if (amt.notesItem != null) {
				items.Add(amt.notesItem);
			}

			mLogger.Debug("* * *  GetItemLabelsResponses after notesItem add items.Count:" + items.Count.ToString() + "   amt.items.Count:" + amt.items.Count.ToString());
			if (amt.fldLabels == null)
				mLogger.Debug("* * *  GetItemLabelsResponses amt.fldLabels is null !!!");
			mLogger.Debug("* * *  GetItemLabelsResponses amt.fldLabels.Count:" + amt.fldLabels.Count.ToString());

			string itemIdsCsv = string.Join(",", items.Select(x => x.itemId).ToList());
			if (itemIdsCsv.Length > Constants.Data.FormsRepository.SP_GETITEMLABELSRESPONSES_ITEMIDSCSV_MAX_LENGTH) {
				throw new InvalidOperationException(String.Format("The length({0}) of the string itemIdsCsv is greater than the max length({1}) of the stored procedure parameter.",
						itemIdsCsv.Length, Constants.Data.FormsRepository.SP_GETITEMLABELSRESPONSES_ITEMIDSCSV_MAX_LENGTH));
			}
			List<ItemLabelResponse> itemLabelResponses = db.sp_GetItemLabelsResponses(formResultId, itemIdsCsv).ToList();

			foreach (var item in items) {

				if (!amt.fldLabels.ContainsKey(item.identifier)) {
					amt.fldLabels.Add(item.identifier, item.label);
					amt.itmPrompts.Add(item.identifier, item.prompt);
					mLogger.Debug("* * *  fldLabels  add -> " + item.identifier + " : " + item.label);
				} else
					mLogger.Debug("* * *  WARNING! skipping duplicate item identifier: " + item.identifier);

				// Get the ItemResult (Existing code assumed that there would be only one)
				def_ItemResults itemResult = itemLabelResponses
						.Where(x => x.ItemResult_itemId.HasValue && x.ItemResult_itemId.Value == item.itemId)
						.Select(x => new def_ItemResults {
							itemResultId = x.ItemResult_itemResultId ?? 0,
							formResultId = x.ItemResult_formResultId ?? 0,
							itemId = x.ItemResult_itemId ?? 0,
							sessionStatus = x.ItemResult_sessionStatus ?? 0,
							dateUpdated = x.ItemResult_dateUpdated ?? DateTime.MinValue
						})
						.SingleOrDefault();

				// Get the ItemVariables
				List<def_ItemVariables> itemVariables = itemLabelResponses
						.Where(x => x.ItemVariable_itemId.HasValue && x.ItemVariable_itemId.Value == item.itemId)
						.Select(x => new def_ItemVariables {
							itemVariableId = x.ItemVariable_itemVariableId ?? 0,
							itemId = x.ItemVariable_itemId ?? 0,
							identifier = x.ItemVariable_identifier,
							baseTypeId = x.ItemVariable_baseTypeId ?? 0,
							defaultValue = x.ItemVariable_defaultValue,
							outcomeDeclarationId = x.ItemVariable_outcomeDeclarationId ?? 0
						})
						.ToList();

				mLogger.Debug("* * *  itemList.ItemVariables.Count: " + itemVariables.Count.ToString());
				foreach (def_ItemVariables iv in itemVariables) {
					string rspValue = allowNullResponseValues ? null : String.Empty;
					if (itemResult != null) {
						def_ResponseVariables rv = itemLabelResponses
								.Where(x => x.ResponseVariable_itemResultId.HasValue && x.ResponseVariable_itemResultId.Value == itemResult.itemResultId
										&& x.ResponseVariable_itemVariableId.HasValue && x.ResponseVariable_itemVariableId.Value == iv.itemVariableId)
								.Select(x => new def_ResponseVariables {
									responseVariableId = x.ResponseVariable_responseVariableId ?? 0,
									itemResultId = x.ResponseVariable_itemResultId ?? 0,
									itemVariableId = x.ResponseVariable_itemVariableId ?? 0,
									rspInt = x.ResponseVariable_rspInt ?? 0,
									rspFloat = x.ResponseVariable_rspFloat ?? 0d,
									rspDate = x.ResponseVariable_rspDate ?? DateTime.MinValue,
									rspValue = x.ResponseVariable_rspValue
								})
								.SingleOrDefault();
						if (rv != null) {
							if (iv.baseTypeId == 1 && !String.IsNullOrWhiteSpace(rv.rspValue)) {
								if (rv.rspValue.Equals("0"))
									rspValue = "false";
								else if (rv.rspValue.Equals("1"))
									rspValue = "true";
								else
									rspValue = rv.rspValue;
							} else {
                                rspValue = HttpUtility.HtmlDecode(rv.rspValue);
							}
						}
					}
					if (!amt.rspValues.ContainsKey(iv.identifier)) {
						amt.rspValues.Add(iv.identifier, rspValue);
						mLogger.Debug("* * *  rspValue add -> " + iv.identifier + " : " + rspValue);
					} else {
						mLogger.Debug("* * *  WARNING! skipping duplicate itemvariable identifier: " + iv.identifier);
					}
				}
			}
			mLogger.Debug("* * *  amt.rspValues.Count: " + amt.rspValues.Count());

		}

		public void GetItemListIncludingSubSections(def_Sections sctn, Assmnts.Models.TemplateItems amt)
		{
			if (amt.items == null)
				amt.items = new List<def_Items>();
			if (amt.sctTitles == null)
				amt.sctTitles = new Dictionary<string, string>();

			amt.sctTitles.Add(sctn.identifier, sctn.title);
			//amt.notesItem = formsRepo.GetAllItemsInSection(sctn, true).FirstOrDefault();

			//amt.items = formsRepo.GetAllItemsInSection(sctn);
			// SortSectionItems(sctn);
			//special case for subsection containing notes item
			if (!sctn.multipleItemsPerPage) {
				// formsRepo.SortSectionItems(sctn);
				// amt.notesItem = sctn.def_SectionItems.First().def_Items;
				// amt.notesItem = GetItemById(sctn.def_SectionItems.First().itemId);
				return;
			}

			//foreach (def_Items itm in db.def_sp_GetItemListFromSection(sctn.sectionId, false))
			//    amt.items.Add(itm);


			SortSectionItems(sctn);


			foreach (def_SectionItems sctnItm in sctn.def_SectionItems) {
				if (sctnItm.subSectionId.GetValueOrDefault(0) == 0)     // No subsection, must be an Item
				{
					def_Items itm = GetItemById(sctnItm.itemId);
					if (itm == null)
						throw new Exception("could not find item with id " + sctnItm.itemId + ", from sectionItem with id " + sctnItm.sectionItemId);
					amt.items.Add(itm);
					// amt.items.Add(sctnItm.def_Items);
				} else {
					// amt.sections.Add(sctnItm.def_SubSections.def_Sections);
					// GetItemListFromAllSections(sctnItm.def_SubSections.def_Sections, amt);
					GetItemListIncludingSubSections(GetSubSectionById(sctnItm.subSectionId), amt);
				}
			}

			return;
		}

		public void GetItemListForNotes(def_Sections sctn, Assmnts.Models.TemplateItems amt)
		{
			GetItemListForNotesRecursive(sctn, amt);
			GetEnterpriseItems(amt.items, SessionHelper.LoginStatus.EnterpriseID);
		}

		/*
		 * Recursive function to get List of Items from sections and subsections
		*/
		private void GetItemListForNotesRecursive(def_Sections sctn, Assmnts.Models.TemplateItems amt)
		{
			// amt.notesItem = GetAllItemsInSection(sctn, true).FirstOrDefault();

			// SortSectionItems(sctn);
			//special case for subsection containing notes item

			this.SortSectionItems(sctn);
			if (sctn.multipleItemsPerPage == false) {
				//  SortSectionItems(sctn);


				// amt.notesItem = sctn.def_SectionItems.First().def_Items;
				amt.notesItem = GetItemById(sctn.def_SectionItems.First().itemId);
				return;
			}

			foreach (def_SectionItems sctnItm in sctn.def_SectionItems) {
				if (sctnItm.subSectionId.GetValueOrDefault(0) == 0)     // No subsection, must be an Item
				{
					continue;
				} else {
					// amt.sections.Add(sctnItm.def_SubSections.def_Sections);
					// GetItemListFromAllSections(sctnItm.def_SubSections.def_Sections, amt);
					GetItemListForNotes(GetSubSectionById(sctnItm.subSectionId), amt);
				}
			}


			return;
		}

		public void GetEnterpriseItems(List<def_Items> items, int entId)
		{
			List<def_ItemsEnt> itemsEnt = mCachedFormsEntities.def_ItemsEnt
				.Where(ie => ie.ent_id == entId)
				.ToList<def_ItemsEnt>();

			itemsEnt.ForEach(ie => {
				def_Items item = mCachedFormsEntities.def_Items
				.Where(i => ie.itemId == i.itemId)
				.FirstOrDefault();

				if (item != null) {
					item.label = ie.label;
					item.prompt = ie.prompt;
				}
			});

		}

		// Common Routines
		public DbContext GetContext()
		{
			return db;
		}

		public Database GetDatabase()
		{
			return db.Database;
		}

		/// <summary>
		/// Set Entity Framework Lazy Loading feature
		/// Default os On or True
		/// </summary>
		/// <param name="tfSwitch">True/False</param>
		public void SetLazyLoadingEnabled(bool tfSwitch)
		{
			db.Configuration.LazyLoadingEnabled = tfSwitch;
		}

		public void Save()
		{
			try {
				db.SaveChanges();
			}
			catch (DbEntityValidationException dbEx) {
				mLogger.Error(dbEx, "Save DbEntityValidationException: ");
				foreach (DbEntityValidationResult devr in dbEx.EntityValidationErrors) {
					foreach (DbValidationError dve in devr.ValidationErrors) {
						mLogger.Error("\tDbEntityValidationResult: {0}", dve.ErrorMessage);
					}
				}
			}
			catch (SqlException qex) {
				mLogger.Error(qex, "Save SqlException: ");
			}
			catch (Exception ex) {
				mLogger.Error(ex, "Save exception: ");
				throw ex;
			}
		}

		// Form methods

		public def_Forms GetFormById(int formId)
		{
			// Data.Concrete.ClearCache(db);
			return mCachedFormsEntities.def_Forms
				.SingleOrDefault(a => a.formId == formId);
		}

		public def_Forms GetFormByIdentifier(string identifier)
		{
			return mCachedFormsEntities.def_Forms
				.Where(f => f.identifier.Equals(identifier))
				.FirstOrDefault();
		}

		/// <summary>
		/// Get the formId of a Form using the identifier
		/// </summary>
		/// <param name="formIdentifier">def_Forms.identifier - identifier string.</param>
		/// <returns>def_Forms.formId</returns>
		public int GetFormIdByIdentifier(string identifier)
		{
			def_Forms frm = GetFormByIdentifier(identifier);
			return frm.formId;
		}


		public List<def_Forms> GetFormsByIdentifiers(List<string> identifiers)
		{
			return mCachedFormsEntities.def_Forms
				.Where(f => identifiers.Contains(f.identifier))
				.ToList<def_Forms>();
		}

		public List<def_Forms> GetAllForms()
		{
			List<def_Forms> formsList = null;
			try {
				formsList = mCachedFormsEntities.def_Forms
					.ToList<def_Forms>();
			}
			catch (Exception ex) {
				mLogger.Fatal(ex);
				def_Forms frm = new def_Forms {
					formId = 0,
					identifier = "Error",
					title = ex.Message
				};
				formsList = new List<def_Forms>();
				formsList.Add(frm);
			}

			return formsList;
		}


		public int AddForm(def_Forms frm)
		{
			db.def_Forms.Add(frm);
			db.SaveChanges();
			return frm.formId;
		}

		public void SaveForm(def_Forms frm)
		{
			try {
				db.def_Forms.Attach(frm);

				db.Entry(frm).State = EntityState.Modified;
				Save();
			}
			catch (Exception ex) {
				mLogger.Debug(ex);
			}

			return;
		}

		public void Delete(def_Forms frm)
		{
			db.def_Forms.Remove(frm);
		}


		public void Update(def_Forms frm)
		{
			// Assessment assmnt = db.Forms.FirstOrDefault(a => a.assessmentId == assmnt.assessmentId);
			// assmnt.assessmentId = assmnt.assessmentId;
			Save();

		}

		public int GetLastFormId()
		{
			return (from f in mCachedFormsEntities.def_Forms orderby f.formId descending select f.formId).FirstOrDefault();
		}

		// =====================================================
		// Parts
		// =====================================================

		public int AddPart(def_Parts prt)
		{
			db.def_Parts.Add(prt);
			db.SaveChanges();
			return prt.partId;
		}

		public def_Parts GetPartById(int prtId)
		{
			def_Parts result = mCachedFormsEntities.def_Parts
				.Where(p => p.partId == prtId)
				.Select(x => new def_Parts(x))
				.SingleOrDefault();

			int langId = (SessionHelper.SessionForm == null) ? 1 : SessionHelper.SessionForm.langId;
			def_PartText partText = mCachedFormsEntities.def_PartText
				.Where(pt => pt.langId == langId && pt.partId == prtId)
				.FirstOrDefault();

			if (partText != null) {
				result.title = partText.prtTitle;
			}

			return result;
		}

		public def_Parts GetAttachedPartById(int prtId)
		{
			// Data.Concrete.ClearCache(db);
			def_Parts result = mCachedFormsEntities.def_Parts
				.SingleOrDefault(p => p.partId == prtId);

			return result;
		}

		public def_Parts GetPartByFormAndIdentifier(def_Forms form, string identifier)
		{
			def_Parts result = (from p in mCachedFormsEntities.def_Parts
													join fp in mCachedFormsEntities.def_FormParts on p.partId equals fp.partId
													where (fp.formId == form.formId && p.identifier == identifier)
													select p).FirstOrDefault();
			if (result == null) {
				string errorMessage = String.Format("Could not find part with identifier {0} in form {1}.", identifier, form.formId);
				mLogger.Error(errorMessage);
				throw new Exception(errorMessage);
			}
			return result;
		}

		public IQueryable<def_Parts> GetAllParts()
		{
			return mCachedFormsEntities.def_Parts.AsQueryable();
		}

		public void SavePart(def_Parts prt)
		{
			try {
				db.def_Parts.Attach(prt);

				db.Entry(prt).State = EntityState.Modified;
				Save();
			}
			catch (Exception ex) {
				mLogger.Debug(ex);
			}

			return;
		}


		// =====================================================
		// FormParts
		// =====================================================

		public void SaveFormPart(def_FormParts frmPrt)
		{
			try {
				db.def_FormParts.Attach(frmPrt);

				db.Entry(frmPrt).State = EntityState.Modified;
				Save();
			}
			catch (Exception ex) {
				mLogger.Debug(ex);
			}

			return;
		}

		public int AddFormPart(def_FormParts frmPrt)
		{
			db.def_FormParts.Add(frmPrt);
			db.SaveChanges();
			return frmPrt.formPartId;
		}

		public List<def_Parts> GetFormParts(def_Forms frm)
		{
			List<def_FormParts> formParts = mCachedFormsEntities.def_FormParts
					.Where(x => x.formId == frm.formId)
					.OrderBy(x => x.order)
					.ToList<def_FormParts>();

			List<def_Parts> partsList = new List<def_Parts>();
			foreach (def_FormParts fp in formParts) {
				partsList.Add(this.GetPartById(fp.partId));
			}

			return partsList;
		}

		/// <summary>
		/// Returns a sorted list of FormParts.
		/// NoTracking - will not allow updates
		/// </summary>
		/// <param name="formId">def_Forms</param>
		/// <returns>List<def_FormParts></returns>
		public List<def_FormParts> GetFormPartsByFormId(int formId)
		{
			return mCachedFormsEntities.def_FormParts
				.Where(fp => fp.formId == formId)
				.OrderBy(fp => fp.order)
				.ToList<def_FormParts>();
		}

		public def_FormParts GetFormPartById(int formPartId)
		{
			def_FormParts frmPrt = mCachedFormsEntities.def_FormParts
				.Where(fp => fp.formPartId == formPartId)
				.FirstOrDefault();
			return frmPrt;
		}


		// =====================================================
		// Sections
		// =====================================================

		public int AddSection(def_Sections sctn)
		{
			db.def_Sections.Add(sctn);
			db.SaveChanges();
			return sctn.sectionId;
		}

		public def_Sections GetSectionById(int sctnId)
		{
			def_Sections result = mCachedFormsEntities.def_Sections
				.Where(s => (s.sectionId == sctnId))
				.Select(x => new def_Sections(x))
				.SingleOrDefault();

			//modify result so it contains language-dependant labels
			if (result != null) {
				int langId = (SessionHelper.SessionForm == null) ? 1 : SessionHelper.SessionForm.langId;
				def_SectionText sectionText = mCachedFormsEntities.def_SectionText
					.Where(st => st.langId == langId && st.sectionId == sctnId)
					.FirstOrDefault();

				if (sectionText != null) {
					result.title = sectionText.sctnTitle;
				}
			}

			return result;
		}

		public def_Sections GetSectionByIdentifier(string ident)
		{
			return mCachedFormsEntities.def_Sections
				.SingleOrDefault(s => s.identifier == ident);
		}

		public IQueryable<def_Sections> GetAllSections()
		{
			return mCachedFormsEntities.def_Sections.AsQueryable();
		}

		public List<def_Sections> GetSectionsInPart(def_Parts prt)
		{
			List<def_PartSections> partSections = mCachedFormsEntities.def_PartSections
				.Where(p => p.partId == prt.partId)
				.OrderBy(p => p.order)
				.Select(x => new def_PartSections(x))
				.ToList<def_PartSections>();

			List<def_Sections> sectionList = new List<def_Sections>();
			OverlayPartSectionsEnt(partSections);
			partSections = partSections.OrderBy(ps => ps.order).ToList();

			foreach (def_PartSections ps in partSections) {
				if (ps.visible == null || (bool)ps.visible) {
					sectionList.Add(this.GetSectionById(ps.sectionId));
				}
			}
			return sectionList;
		}


		private void OverlayPartSectionsEnt(ICollection<def_PartSections> partSections)
		{
			List<def_PartSectionsEnt> partSectionsEnt = mCachedFormsEntities.def_PartSectionsEnt
				.Where(pse => pse.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID)
				.ToList<def_PartSectionsEnt>();


			foreach (def_PartSectionsEnt pse in partSectionsEnt) {
				def_PartSections partSection = partSections
					.Where(ps => ps.partSectionId == pse.partSectionId)
					.FirstOrDefault();

				if (partSection != null) {
					def_PartSections foundPartSection = mCachedFormsEntities.def_PartSections
						.Where(ps => ps.partSectionId == pse.entPartSectionId)
						.FirstOrDefault();
					// BR 5/26 - Detaching the entity state prevents the PartSectionEnt from being saved into the PartSection, but VistaDB doesn't view detached states.
					// It shouldn't matter if Venture over writes form data, because that is never saved back to the main database.

					partSection.partId = foundPartSection.partId;
					partSection.sectionId = foundPartSection.sectionId;
					partSection.visible = pse.visible;
					partSection.order = foundPartSection.order;
				}
			}

		}

		public List<def_Sections> GetSectionsInPartById(int partId)
		{
			def_Parts part = GetPartById(partId);
			List<def_Sections> sectionList = GetSectionsInPart(part);
			return sectionList;
		}

		public void SectionSave(def_Sections sct)
		{
			try {
				db.def_Sections.Attach(sct);

				db.Entry(sct).State = EntityState.Modified;
				Save();
			}
			catch (Exception ex) {
				mLogger.Error(ex);
			}

			return;
		}

		/*
		 * Get the Section referenced by the SubSeciton in the SectionItems
		 */
		public def_Sections GetSubSectionById(int? subSctnId)
		{
			def_SubSections ss = mCachedFormsEntities.def_SubSections
				.SingleOrDefault(s => (s.subSectionId == subSctnId));
			return GetSectionById(ss.sectionId);
		}

		public int AddSubSection(def_SubSections sub)
		{
			db.def_SubSections.Add(sub);
			db.SaveChanges();
			return sub.subSectionId;
		}

		public def_SubSections GetSubSectionBySectionId(int sectionId)
		{
			return mCachedFormsEntities.def_SubSections
				.SingleOrDefault(s => s.sectionId == sectionId);
		}

		public void SectionDelete(def_Sections sct)
		{
			db.def_Sections.Remove(sct);
		}

		// =====================================================
		// PartSections
		// =====================================================

		public void SavePartSection(def_PartSections prtSctn)
		{
			try {
				db.def_PartSections.Attach(prtSctn);

				db.Entry(prtSctn).State = EntityState.Modified;
				Save();
			}
			catch (Exception ex) {
				mLogger.Error(ex);
			}

			return;
		}

		public int AddPartSection(def_PartSections prtSctn)
		{
			db.def_PartSections.Add(prtSctn);
			db.SaveChanges();
			return prtSctn.partSectionId;
		}

		public def_PartSections GetPartSectionById(int partSectionId)
		{
			return mCachedFormsEntities.def_PartSections
				.SingleOrDefault(s => s.partSectionId == partSectionId);
		}

		public void GetPartSections(def_Parts prt)
		{
			prt.def_PartSections = mCachedFormsEntities.def_PartSections
				.Where(ps => ps.partId == prt.partId && ps.visible == true)
				.OrderBy(ps => ps.order)
				.Select(x => new def_PartSections(x))
				.ToList();
			OverlayPartSectionsEnt(prt.def_PartSections);

			return;
		}

		public List<def_PartSectionsEnt> GetPartSectionsEnt(def_PartSections partSection)
		{
			return mCachedFormsEntities.def_PartSectionsEnt
				.Where(x => x.partSectionId == partSection.partSectionId)
				.ToList<def_PartSectionsEnt>();
		}


		// =====================================================
		// Items
		// =====================================================

		public int ItemAdd(def_Items itm)
		{
			try {
				db.def_Items.Add(itm);
				db.SaveChanges();
			}
			catch (DbEntityValidationException dbEx) {
				mLogger.Error(dbEx);
				foreach (DbEntityValidationResult devr in dbEx.EntityValidationErrors) {
					foreach (DbValidationError dve in devr.ValidationErrors) {
						mLogger.Error("DbEntityValidationResult: {0}", dve.ErrorMessage);
					}
				}
			}
			catch (Exception ex) {
				if (ex.IsCritical()) {
					throw ex;
				}
				mLogger.Error(ex);
			}
			return itm.itemId;
		}

		public void ItemDelete(def_Items itm)
		{
			db.def_Items.Remove(itm);
		}

		public void ItemSave(def_Items itm)
		{
			try {
				db.def_Items.Attach(itm);
				db.Entry(itm).State = EntityState.Modified;
				db.SaveChanges();
			}
			catch (DbEntityValidationException dbEx) {
				mLogger.Error(dbEx);
				foreach (DbEntityValidationResult devr in dbEx.EntityValidationErrors) {
					foreach (DbValidationError dve in devr.ValidationErrors) {
						mLogger.Error("DbEntityValidationResult: {0}", dve.ErrorMessage);
					}
				}
			}
			catch (Exception ex) {
				if (ex.IsCritical()) {
					throw ex;
				}
				mLogger.Error(ex);
			}
			return;
		}

		public def_Items GetItemById(int itmId)
		{
			def_Items result = mCachedFormsEntities.def_Items
				.Where(i => i.itemId == itmId)
				.Select(x => new def_Items(x))
				.SingleOrDefault();

			// modify result so it contains language-dependant labels ( default is langId 1 [English] )
			if (result != null) {
				int langId = (SessionHelper.SessionForm == null) ? 1 : SessionHelper.SessionForm.langId;
				int entId = SessionHelper.LoginStatus.EnterpriseID;

				def_ItemText itemText = (from it in mCachedFormsEntities.def_ItemText
																 where it.langId == langId && it.itemId == itmId && it.EnterpriseID == entId
																 select it).FirstOrDefault();

				if (itemText != null) {
					result.label = itemText.label;
					result.title = itemText.title;
					result.prompt = itemText.prompt;
					result.itemBody = itemText.itemBody;
				}
			}
			return result;
		}

		/// <summary>
		/// Does this really need to be attached?
		/// </summary>
		/// <param name="itmId"></param>
		/// <returns></returns>
		public def_Items GetAttachedItemById(int itmId)
		{
			def_Items result = mCachedFormsEntities.def_Items
				.SingleOrDefault(i => i.itemId == itmId);
			return result;
		}

		public int GetLastItemId()
		{
			return (from i in mCachedFormsEntities.def_Items orderby i.itemId descending select i.itemId).FirstOrDefault();
		}

		public IQueryable<def_Items> GetAllItems()
		{
			IQueryable<def_Items> iqi = mCachedFormsEntities.def_Items.Select(x => new def_Items(x)).AsQueryable();
			mLogger.Trace("FormsRepository GetAllItems count: {0}", iqi.Count());
			return iqi;
		}

		// =====================================================
		// ItemVariables
		// =====================================================

		public int AddItemVariable(def_ItemVariables itmVar)
		{
			db.def_ItemVariables.Add(itmVar);
			db.SaveChanges();
			return itmVar.itemVariableId;
		}

		public void ItemVariableDelete(def_ItemVariables iv)
		{
			db.def_ItemVariables.Remove(iv);
		}

		public void GetItemVariables(def_Items itm)
		{
			itm.def_ItemVariables = mCachedFormsEntities.def_ItemVariables
				.Where(iv => iv.itemId == itm.itemId)
				.ToList<def_ItemVariables>();
		}

		public List<def_ItemVariables> GetItemVariablesByItemId(int itemId)
		{
			return mCachedFormsEntities.def_ItemVariables
				.Where(iv => (iv.itemId == itemId))
				.ToList<def_ItemVariables>();
		}

		public def_ItemVariables GetItemVariableById(int itmVarId)
		{
			return mCachedFormsEntities.def_ItemVariables
				.Where(i => (i.itemVariableId == itmVarId))
				.SingleOrDefault();
		}

		public def_ItemVariables GetItemVariableByIdentifier(string ivIdentifier)
		{
			return mCachedFormsEntities.def_ItemVariables
					.Where(iv => iv.identifier.ToLower().Equals(ivIdentifier.ToLower()))
					.Select(x => new def_ItemVariables(x))
					.SingleOrDefault();
		}

		public void ItemVariableSave(def_ItemVariables itmVar)
		{
			try {
				db.def_ItemVariables.Attach(itmVar);
				db.Entry(itmVar).State = EntityState.Modified;
				db.SaveChanges();
			}
			catch (DbEntityValidationException dbEx) {
				mLogger.Error(dbEx);
				foreach (DbEntityValidationResult devr in dbEx.EntityValidationErrors) {
					foreach (DbValidationError dve in devr.ValidationErrors) {
						mLogger.Debug("DbEntityValidationResult: {0}", dve.ErrorMessage);
					}
				}
			}
			catch (Exception ex) {
				if (ex.IsCritical()) {
					throw ex;
				}
				mLogger.Error(ex);
			}
			return;
		}

		private Dictionary<int, string> GetItemVariableIdIdentByItemId(int itemId)
		{
			return mCachedFormsEntities.def_ItemVariables
					.Where(iv => iv.itemId == itemId)
					.ToDictionary<CachedItemVariable, int, string>(iv => iv.itemVariableId, iv => iv.identifier);
		}

		/*
		 * This method recursively gets all the ItemVariables within a Section
		* To implement:
		*    Create an empty 'output' dictionary.
		*    Loop through all the Sections in the PartSections table.
		*    
		*/
		public void CollectItemVariableIdentifiersById(def_Sections section, Dictionary<int, string> output)
		{
			this.SortSectionItems(section);


			foreach (def_SectionItems sectionItem in section.def_SectionItems) {
				if (sectionItem.subSectionId != null) {
					def_Sections subSctn = this.GetSubSectionById(sectionItem.subSectionId);
					CollectItemVariableIdentifiersById(subSctn, output);
				} else {
					Dictionary<int, string> IvIdIdents = GetItemVariableIdIdentByItemId(sectionItem.itemId);
					foreach (int ivId in IvIdIdents.Keys) {
						if (!output.ContainsKey(ivId))
							output.Add(ivId, IvIdIdents[ivId]);
					}
				}
			}
		}


		/*
		 * This method recursively gets all the ItemVariables within a Section
		* To implement:
		*    Create an empty 'output' List collection.
		*    Loop through all the Sections in the PartSections table.
		*    
		*/
		public void CollectItemVariableIdentifiers(def_Sections section, List<string> output)
		{
			SortSectionItems(section);
			foreach (def_SectionItems sectionItem in section.def_SectionItems) {
				if (sectionItem.subSectionId != null) {
					def_Sections subSctn = this.GetSubSectionById(sectionItem.subSectionId);
					CollectItemVariableIdentifiers(subSctn, output);
				} else {
					def_Items itm = this.GetItemById(sectionItem.itemId);
					this.GetItemVariables(itm);
					foreach (def_ItemVariables itemVariable in itm.def_ItemVariables) {
						if (!output.Contains(itemVariable.identifier))
							output.Add(itemVariable.identifier);
					}
				}
			}
		}

		// =====================================================
		// SectionItems
		// =====================================================

		public def_SectionItems GetSectionItemById(int sctItmId)
		{
			return mCachedFormsEntities.def_SectionItems
					.Where(si => si.sectionItemId == sctItmId)
					.SingleOrDefault();
		}

		public void SectionItemDelete(def_SectionItems si)
		{
			db.def_SectionItems.Remove(si);
		}

		public List<def_SectionItems> getSectionItemsForItem(def_Items itm)
		{
			return mCachedFormsEntities.def_SectionItems
				.Where(si => si.itemId == itm.itemId)
				.ToList<def_SectionItems>();
		}

		public int AddSectionItem(def_SectionItems sctnItm)
		{
			db.def_SectionItems.Add(sctnItm);
			db.SaveChanges();
			return sctnItm.sectionItemId;
		}

		public void SaveSectionItem(def_SectionItems sctnItm)
		{
			try {
				db.def_SectionItems.Attach(sctnItm);
				db.Entry(sctnItm).State = EntityState.Modified;
				db.SaveChanges();
			}
			catch (DbEntityValidationException dbEx) {
				mLogger.Error(dbEx);
				foreach (DbEntityValidationResult devr in dbEx.EntityValidationErrors) {
					foreach (DbValidationError dve in devr.ValidationErrors) {
						mLogger.Error("DbEntityValidationResult: {0}", dve.ErrorMessage);
					}
				}
			}
			catch (Exception ex) {
				if (ex.IsCritical()) {
					throw ex;
				}
				mLogger.Error(ex);
			}
			return;
		}

		/// <summary>
		/// Loads and sorts section items, and overlays any SectionItemEnts
		/// </summary>
		/// <param name="sctn">The section to load SectionItems for</param>

		public void SortSectionItems(def_Sections sctn)
		{
			sctn.def_SectionItems = mCachedFormsEntities.def_SectionItems
					.Where(si => si.sectionId == sctn.sectionId)
					.OrderBy(si => si.order)
					.Select(x => new def_SectionItems(x))
					.ToList();
			ReplaceSectionItemsWithSectionEnt(sctn.def_SectionItems, SessionHelper.LoginStatus.EnterpriseID);
		}

		public List<def_Items> GetSectionItems(def_Sections sctn)
		{
			SortSectionItems(sctn);
			List<def_Items> itemList = new List<def_Items>();
			foreach (def_SectionItems si in sctn.def_SectionItems) {
				itemList.Add(this.GetItemById(si.itemId));
			}
			return itemList;
		}

		public List<def_SectionItemsEnt> GetSectionItemsEnt(def_SectionItems sectionItem)
		{
			return mCachedFormsEntities.def_SectionItemsEnt
					.Where(x => x.sectionItemId == sectionItem.sectionItemId)
					.ToList<def_SectionItemsEnt>();
		}

		public List<def_Items> GetAllItemsForSection(def_Sections sctn)
		{
			SortSectionItems(sctn);
			List<def_Items> itemList = new List<def_Items>();
			List<def_SectionItems> allSectionItems = new List<def_SectionItems>();
			GetSectionItemsRecursive(sctn.sectionId, allSectionItems);
			foreach (def_SectionItems si in allSectionItems) {
				if (si.subSectionId == null) {
					itemList.Add(this.GetItemById(si.itemId));
				}
			}
			return itemList;
		}

		private void GetSectionItemsRecursive(int? sectionId, List<def_SectionItems> sectionItems)
		{
			if (sectionId != null) {

				List<def_SectionItems> newSectionItems = mCachedFormsEntities.def_SectionItems
						.Where(si => si.sectionId == sectionId)
						.ToList<def_SectionItems>();

				foreach (def_SectionItems si in newSectionItems) {
					sectionItems.Add(si);
					if (si.subSectionId != null) {
						def_SubSections subSection = mCachedFormsEntities.def_SubSections
								.Where(ss => ss.subSectionId == si.subSectionId)
								.FirstOrDefault();

						GetSectionItemsRecursive(subSection.sectionId, sectionItems);
					}
				}
			}
		}

		public List<def_SectionItems> GetSectionItemsBySectionId(int sectionId)
		{
			return mCachedFormsEntities.def_SectionItems
					.Where(si => si.sectionId == sectionId)
					.OrderBy(si => si.order)
					.ToList<def_SectionItems>();
		}

		public void ReplaceSectionItemsWithSectionEnt(ICollection<def_SectionItems> sectionItems, int enterpriseID)
		{

			List<def_SectionItems> sectionItemsList = sectionItems.ToList();

			// *** this is selecting all the SectionItemsEnt for an Enterprise it shouldn't it also be filtered by the Section id?
			// *** this can be optimized by eliminating the 'sectionItemsList' and using sectionItems in the 2nd qry.
			// *** then making 'sectionItemsEnt' IQueryable - this will delay the actual SQL query until the 2nd query ToList() method.
			List<def_SectionItemsEnt> sectionItemsEnt = mCachedFormsEntities.def_SectionItemsEnt
					 .Where(s => s.EnterpriseID == enterpriseID)
					 .ToList<def_SectionItemsEnt>();

			// *** This probably doesn't have any value after changing the above to filter by the Section
			List<def_SectionItemsEnt> sectionItemsEntForSection = sectionItemsEnt
					.Join(sectionItemsList, (sie => sie.sectionItemId), (si => si.sectionItemId), ((sie, si) => sie))
					.ToList();

			sectionItemsEntForSection.ForEach(s => {
				def_SectionItems sectionItem = sectionItems
					.Where(i => i.sectionItemId == s.sectionItemId)
					.FirstOrDefault();

				if (sectionItem != null) {
					sectionItem.display = s.display;
					sectionItem.readOnly = s.readOnly;
					sectionItem.requiredForm = s.requiredForm;
					sectionItem.requiredSection = s.requiredSection;
					sectionItem.validation = s.validation;
					sectionItem.def_Items = GetItemById(sectionItem.itemId);
				}
			});

		}

		public List<def_SectionItems> GetSectionItemsBySectionIdEnt(int sectionId, int EnterpriseID)
		{
			List<def_SectionItems> sectionItems = GetSectionItemsBySectionId(sectionId);
			// *** RRB 11/10/15 this can be optimized by making sectionItemsEnt IQueryable
			List<def_SectionItemsEnt> sectionItemsEnt = mCachedFormsEntities.def_SectionItemsEnt
						 .Where(s => s.EnterpriseID == EnterpriseID)
						 .ToList<def_SectionItemsEnt>();

			List<def_SectionItemsEnt> sectionItemsEntForSection = sectionItemsEnt
					.Join(sectionItems, (sie => sie.sectionItemId), (si => si.sectionItemId), ((sie, si) => sie))
					.ToList();

			sectionItemsEntForSection.ForEach(s => {
				def_SectionItems sectionItem = sectionItems
					.Where(i => i.sectionItemId == s.sectionItemId)
					.FirstOrDefault();

				if (sectionItem != null) {
					sectionItem.display = s.display;
					sectionItem.readOnly = s.readOnly;
					sectionItem.requiredForm = s.requiredForm;
					sectionItem.requiredSection = s.requiredSection;
					sectionItem.validation = s.validation;
					sectionItem.def_Items = GetItemById(sectionItem.itemId);
				}
			});

			foreach (def_SectionItems si in new List<def_SectionItems>(sectionItems)) {
				if (si.subSectionId.HasValue) {
					sectionItems.Remove(si);
					def_Sections sub = GetSubSectionById(si.subSectionId);
					sectionItems.AddRange(GetSectionItemsBySectionIdEnt(sub.sectionId, EnterpriseID));
				}
			}

			return sectionItems;
		}

		// Item Results
		public int AddItemResult(def_ItemResults itmRslt)
		{
			db.def_ItemResults.Add(itmRslt);
			db.SaveChanges();
			return itmRslt.itemResultId;
		}

		public void AddItemResultNoSave(def_ItemResults itmRslt)
		{
			db.def_ItemResults.Add(itmRslt);
		}

		public int DeleteItemResult(def_ItemResults itmRslt)
		{
			try {
				GetItemResultsResponseVariables(itmRslt);
				for (int i = 0; i < itmRslt.def_ResponseVariables.Count; i++) {
					def_ResponseVariables rv = itmRslt.def_ResponseVariables.ToList()[i];
					db.def_ResponseVariables.Remove(rv);
					Save();
				}
				mLogger.Trace("FormsRepository DeleteItemResult RVs deleted for itemId: {0}", itmRslt.itemId);
				db.def_ItemResults.Remove(itmRslt);
				Save();
				mLogger.Trace("FormsRepository DeleteItemResult IR deleted for itemId: {0}", itmRslt.itemId);
			}
			catch (SqlException qex) {
				mLogger.Error(qex);
			}
			catch (Exception ex) {
				if (ex.IsCritical()) {
					throw ex;
				}
				mLogger.Error(ex);
			}
			return 0;
		}

		public def_ItemResults GetItemResultById(int irId)
		{
			return db.def_ItemResults.SingleOrDefault(ir => ir.itemResultId == irId);
		}

		public def_ItemResults GetItemResultByFormResItem(int frmRsltId, int itmId)
		{
			return db.def_ItemResults
				.SingleOrDefault(ir => (ir.formResultId == frmRsltId) && (ir.itemId == itmId));
		}

		public void GetItemResultsResponseVariables(def_ItemResults itemResult)
		{
			db.Entry(itemResult).Collection(ir => ir.def_ResponseVariables).Load();
			return;
		}

		// Response Variables
		public int AddResponseVariable(def_ResponseVariables rspVar)
		{
			db.def_ResponseVariables.Add(rspVar);
			db.SaveChanges();
			return rspVar.responseVariableId;
		}

		public void AddResponseVariableNoSave(def_ResponseVariables rspVar)
		{
			db.def_ResponseVariables.Add(rspVar);
		}

		public void DeleteResponseVariableNoSave(def_ResponseVariables rv)
		{
			db.def_ResponseVariables.Remove(rv);
		}

		public def_ResponseVariables GetResponseVariablesById(int rvId)
		{
			return db.def_ResponseVariables.SingleOrDefault(rv => (rv.responseVariableId == rvId));
		}

		public def_ResponseVariables GetResponseVariablesByItemResultItemVariable(int itmRsltId, int itmVrbleId)
		{
			return db.def_ResponseVariables.SingleOrDefault(rv => (rv.itemResultId == itmRsltId) && (rv.itemVariableId == itmVrbleId));
		}

		/// <summary>
		/// Get ResponseVariables by formResultId and ItemVariable identifier
		/// </summary>
		/// <param name="frmRsltId">def_FormResults.formResultId</param>
		/// <param name="identifier">def_ItemVariables.identifier</param>
		/// <returns></returns>
		public def_ResponseVariables GetResponseVariablesByFormResultIdentifier(int frmRsltId, string identifier)
		{
			def_ItemVariables iv = GetItemVariableByIdentifier(identifier);

			if (iv == null) {
				return null;
			}

			var qry = (from rv in db.def_ResponseVariables
								 join ir in db.def_ItemResults on rv.itemResultId equals ir.itemResultId
								 where ((ir.itemId == iv.itemId) && (ir.formResultId == frmRsltId) && (rv.itemVariableId == iv.itemVariableId))
								 select rv).FirstOrDefault();

			def_ResponseVariables rspVar = null;
			try {
				rspVar = qry;
			}
			catch (InvalidOperationException ex) {
				mLogger.Error(ex, "GetResponseVariablesByFormResultIdentifier Exception frmRsltId: {0}, identifier: {2}", frmRsltId, identifier);

				rspVar = new def_ResponseVariables {
					rspValue = String.Empty,
				};
			}

			return rspVar;
		}

		/*
		 *  Get ResponseVariables by formResultId and ItemVariableId
		 */
		public def_ResponseVariables GetResponseVariablesByFormResultItemVarId(int frmRsltId, int itemVariableId)
		{
			// *** RRB - does this need a join with ItemResults and ResponseVariables?
			//                seems like it would to be efficient
			//              plus have the itemId so it could find the unique ItemResult and then get the ResponseVariable

			//  EF is using an INNER JOIN here which could result in multiple records being returned.
			//  Really need to look at using the itemId as part of the query.

			// db.Database.Log = s => System.Diagnostics.mLogger.Debug("GetResponseVariablesByFormResultItemVarId: " + s);

			return db.def_ResponseVariables.SingleOrDefault(rv =>
					(rv.def_ItemResults.formResultId == frmRsltId) &&
					(rv.itemVariableId == itemVariableId)
			);
		}


		public def_Items GetItemByIdentifier(string itemIdent)
		{
			return mCachedFormsEntities.def_Items
					.Where(itm => itm.identifier.Equals(itemIdent))
					.SingleOrDefault();
		}

		public int GetItemIdByIdentifier(string itemIdent)
		{
			var qry = (from itm in mCachedFormsEntities.def_Items
								 where (itm.identifier == itemIdent)
								 select itm.itemId);

			return qry.First();
		}

		// RRB - 10/2/2015 - This method should never be used.  All Items should have unique identifiers.
		public def_Items GetItemByIdentifierAndFormId(string itemIdent, int formId)
		{
			itemIdent = itemIdent.ToLower();
			def_Forms form = GetFormById(formId);
			if (form == null)
				return null;
			foreach (def_Parts part in GetFormParts(form)) {
				GetPartSections(part);
				foreach (def_PartSections ps in part.def_PartSections) {
					def_Sections section = GetSectionById(ps.sectionId);
					if (section == null)
						continue;
					foreach (def_Items item in GetSectionItems(ps.def_Sections)) {
						if (item.identifier.ToLower().Equals(itemIdent))
							return item;
					}
				}
			}
			return null;
		}

		public string CreateNewResponseValues(string frmRsltId, Dictionary<string, string> responsesByIdentifier)
		{
			const string cnrvMethodName = "FormsRepository.CreateNewResponseValues";
			int paramFormResultId = 0;
			if (String.IsNullOrEmpty(frmRsltId)) {
				mLogger.Error("{0} frmRsltId is null or empty: {1}", cnrvMethodName, frmRsltId);
				return "No FormResultId (Assessment) was provided for the Response Values.";
			}
			if (!Int32.TryParse(frmRsltId, out paramFormResultId)) {
				mLogger.Debug("{0} failed converting frmRsltId to int: {1}.", cnrvMethodName, frmRsltId);
			}

			int rspCnt = 0;
			string returnMsg = String.Empty;
			foreach (string ivIdentifier in responsesByIdentifier.Keys) {
				// Find the item variable by identifier
				def_ItemVariables iv = this.GetItemVariableByIdentifier(ivIdentifier);
				def_Items parentItem = iv.def_Items;

				// Make sure there exists an ItemResult for this formId, parentItem
				def_ItemResults itemResult = this.GetItemResultByFormResItem(paramFormResultId, parentItem.itemId);
				int itmRsltId;
				if (itemResult == null) {
					itmRsltId = this.AddItemResult(new def_ItemResults() {
						formResultId = paramFormResultId,
						itemId = parentItem.itemId,
						sessionStatus = 0,
						dateUpdated = DateTime.Today
					});
				} else {
					itmRsltId = itemResult.itemResultId;
				}

				// Add the new response variable to the database
				def_ResponseVariables rv = new def_ResponseVariables() {
					itemResultId = itmRsltId,
					itemVariableId = iv.itemVariableId,
					rspValue = responsesByIdentifier[ivIdentifier]
				};

				ConvertValueToNativeType(iv, rv);
				this.AddResponseVariable(rv);
				rspCnt++;

			}

			returnMsg = rspCnt.ToString() + " Response Variables written to formResultId: " + frmRsltId;

			return returnMsg;
		}



		// =====================================================
		// Language
		// =====================================================

		/// <summary>
		/// Not cloning
		/// </summary>
		/// <param name="langId"></param>
		/// <returns></returns>
		public def_Languages GetLanguageById(int langId)
		{
			// Data.Concrete.ClearCache(db);
			return mCachedFormsEntities.def_Languages.SingleOrDefault(a => a.langId == langId);
		}

		public def_Languages GetLanguageByTwoLetterISOName(string isoName)
		{
			isoName = isoName.ToLower();
			return mCachedFormsEntities.def_Languages.SingleOrDefault(l => l.languageRegion.ToLower() == isoName);
		}

		public List<def_Languages> GetAllLanguages()
		{
			return mCachedFormsEntities.def_Languages.ToList<def_Languages>();
		}

		// =====================================================
		// Base Types
		// =====================================================

		public def_BaseTypes GetBaseTypeById(int btId)
		{
			return mCachedFormsEntities.def_BaseTypes.SingleOrDefault(a => a.baseTypeId == btId);
		}

		public List<def_BaseTypes> GetBaseTypes()
		{
			return mCachedFormsEntities.def_BaseTypes.ToList<def_BaseTypes>();
		}

		public List<def_Items> GetAllItemsInSection(def_Sections sct, bool onlyNotes = false)
		{
			throw new NotImplementedException();
		}


		public IQueryable<vFormResultUser> GetAssessmentsAndUserNames(int EnterpriseId, int formId)
		{
			return db.vFormResultUsers.Where(fr => (fr.EnterpriseID == EnterpriseId) && (fr.formId == formId));
		}

		public IEnumerable<def_SectionItems> GetSectionItemsBySection(def_Sections section)
		{
			throw new NotImplementedException();
		}


		public def_ResponseVariables GetResponseVariablesBySubjectForm(int subject, int formId, string identifier)
		{
			def_ItemVariables iv = GetItemVariableByIdentifier(identifier);

			if (iv == null)
				throw new Exception("Could not find itemVariable with identifier \"" + identifier + "\"");

			var qry = (from rv in db.def_ResponseVariables
								 join ir in db.def_ItemResults on rv.itemResultId equals ir.itemResultId
								 join fr in db.def_FormResults on ir.formResultId equals fr.formResultId
                                 orderby fr.dateUpdated descending
								 where ((ir.itemId == iv.itemId) && (fr.formId == formId && fr.subject == subject) && (rv.itemVariableId == iv.itemVariableId))
								 select rv).FirstOrDefault();

			def_ResponseVariables rspVar = null;
			try {
				rspVar = qry;
			}
			catch (InvalidOperationException ex) {
				mLogger.Debug(ex, "GetResponseVariablesByFormResultIdentifier Exception formId: {0}, identifier: {1}", formId, identifier);
				rspVar = new def_ResponseVariables {
					rspValue = String.Empty,
				};
			}

			return rspVar;
		}

		public IQueryable<T> GetEntities<T>() where T : class
		{
			return db.Set<T>().AsQueryable<T>();
		}

		public IQueryable<T> GetEntities<T>(Expression<Func<T, bool>> predicate) where T : class
		{
			return db.Set<T>().Where(predicate).AsQueryable<T>();
		}
	}

}
