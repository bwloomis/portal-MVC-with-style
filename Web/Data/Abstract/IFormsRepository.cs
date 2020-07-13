using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using Assmnts;

namespace Data.Abstract
{
	public interface IFormsRepository
	{
		IQueryable<T> GetEntities<T>() where T : class;
		IQueryable<T> GetEntities<T>(Expression<Func<T, bool>> predicate) where T : class;

		IList<def_ItemResults> GetItemResults(int itemResultId);
		def_LookupMaster GetLookup(string lookupCode);

		//functions moved from ResultsController        

		// *** The 3 below need to be moved to Def.Domain - TemplateItems need to be kept of of the Repository.
		void GetItemListIncludingSubSections(def_Sections sctn, Assmnts.Models.TemplateItems amt);
		void GetItemListForNotes(def_Sections sctn, Assmnts.Models.TemplateItems amt);
		void GetItemLabelsResponses(int formResultId, IList<def_Items> items);
		void GetItemLabelsResponses(int formId, Assmnts.Models.TemplateItems amt, List<def_Items> items, bool allowNullResponseValues = false);


		void GetEnterpriseItems(List<def_Items> items, int ent);
		void ConvertValueToNativeType(def_ItemVariables iv, def_ResponseVariables rv);

		// Common Routines
		DbContext GetContext();
		Database GetDatabase();
		void SetLazyLoadingEnabled(bool tfSwitch);
		void Save();

		// Forms
		def_Forms GetFormById(int formId);
		def_Forms GetFormByIdentifier(string identifier);
		int GetFormIdByIdentifier(string identifier);
		List<def_Forms> GetFormsByIdentifiers(List<string> identifiers);
		List<def_Forms> GetAllForms();
		int AddForm(def_Forms frm);
		void SaveForm(def_Forms frm);
		void Update(def_Forms frm);
		void Delete(def_Forms frm);

		int GetLastFormId();

		// Parts
		int AddPart(def_Parts prt);
		void SavePart(def_Parts prt);
		def_Parts GetPartById(int prtId);
		def_Parts GetAttachedPartById(int prtId);
		def_Parts GetPartByFormAndIdentifier(def_Forms form, string identifier);
		IQueryable<def_Parts> GetAllParts();

		// Form Parts
		void SaveFormPart(def_FormParts frmPrt);
		int AddFormPart(def_FormParts frmPrt);
		List<def_Parts> GetFormParts(def_Forms frm);
		List<def_FormParts> GetFormPartsByFormId(int formId);
		def_FormParts GetFormPartById(int formPartId);

		// Sections
		int AddSection(def_Sections sct);
		def_Sections GetSectionById(int sctnId);
		IQueryable<def_Sections> GetAllSections();
		List<def_Sections> GetSectionsInPart(def_Parts prt);
		List<def_Sections> GetSectionsInPartById(int partId);
		List<def_Items> GetAllItemsInSection(def_Sections sct, bool onlyNotes = false);
		void SectionSave(def_Sections sct);
		def_Sections GetSectionByIdentifier(string ident);
		void SectionDelete(def_Sections sct);

		//SubSections
		int AddSubSection(def_SubSections sub);
		def_Sections GetSubSectionById(int? subSctnId);
		def_SubSections GetSubSectionBySectionId(int sectionId);

		// PartSections
		void SavePartSection(def_PartSections prtSct);
		int AddPartSection(def_PartSections prtSct);
		def_PartSections GetPartSectionById(int partSectionId);
		void GetPartSections(def_Parts prt);

		List<def_PartSectionsEnt> GetPartSectionsEnt(def_PartSections partSection);

		// Items
		int ItemAdd(def_Items itm);
		void ItemDelete(def_Items itm);
		void ItemSave(def_Items itm);
		def_Items GetItemById(int itmId);
		def_Items GetAttachedItemById(int itmId);
		def_Items GetItemByIdentifier(string itemIdent);
		def_Items GetItemByIdentifierAndFormId(string itemIdent, int formId);
		int GetLastItemId();
		IQueryable<def_Items> GetAllItems();
		List<def_Items> GetAllItemsForSection(def_Sections sctn);

		// Item Variables
		void ItemVariableDelete(def_ItemVariables iv);
		int AddItemVariable(def_ItemVariables itmVar);
		void GetItemVariables(def_Items itm);
		List<def_ItemVariables> GetItemVariablesByItemId(int itemId);
		def_ItemVariables GetItemVariableById(int itmVarId);
		def_ItemVariables GetItemVariableByIdentifier(string ivIdentifier);
		void ItemVariableSave(def_ItemVariables itmVar);
		void CollectItemVariableIdentifiersById(def_Sections section, Dictionary<int, string> output);
		void CollectItemVariableIdentifiers(def_Sections section, List<string> output);

		// SectionItems
		List<def_SectionItems> getSectionItemsForItem(def_Items itm);
		void SectionItemDelete(def_SectionItems si);
		def_SectionItems GetSectionItemById(int sctItmId);
		int AddSectionItem(def_SectionItems sctItm);
		void SaveSectionItem(def_SectionItems sctItm);
		void SortSectionItems(def_Sections sctn);
		List<def_Items> GetSectionItems(def_Sections sctn);
		List<def_SectionItems> GetSectionItemsBySectionId(int sectionId);
		void ReplaceSectionItemsWithSectionEnt(ICollection<def_SectionItems> sectionItems, int enterpriseID);
		List<def_SectionItems> GetSectionItemsBySectionIdEnt(int sectionId, int EnterpriseID);

		List<def_SectionItemsEnt> GetSectionItemsEnt(def_SectionItems sectionItem);
		IEnumerable<def_SectionItems> GetSectionItemsBySection(def_Sections section);

		// Form Results
		int AddFormResult(def_FormResults frmRslt);
		void AddFormResultNoSave(def_FormResults frmRslt);
		void SaveFormResults(def_FormResults frmRslt);
		void SaveFormResultsThrowException(def_FormResults frmRslt);
		IQueryable<def_FormResults> GetFormResultsByFormId(int formId);
		IEnumerable<def_FormResults> GetFormResultsByFormSubject(int formId, int? subject);
		IQueryable<def_FormResults> GetFormResultsBySubject(int? subject);
		List<def_FormResults> GetFormResultsByIvIdentifierAndValue(string ivIdentifier, string rvValue);
		List<def_FormResults> GetFormResultsByIvIdentifierAndValueFilterByAccess(UAS.DataDTO.LoginStatus loginStatus, string ivIdentifier, string rvValue);
		def_FormResults GetFormResultById(int frId);
		void GetFormResultItemResults(def_FormResults frmResult);
		void FormResultDelete(def_FormResults frmRslt);
		void SetFormResultStatus(def_FormResults frmRslt, byte newStatus);
		void FormResultDeleteLogically(int frId);
		void FormResultUndelete(int frId);
		void LockFormResult(int formResultId);
		void UnlockFormResult(int frId);
		void ArchiveFormResult(int formResultId);
		void UnarchiveFormResult(int frId);
		void SaveBatchResponses(Dictionary<int, Dictionary<int, string[]>> rspValsByIvByItem, int[] formResultIds);

		// Item Results
		void AddItemResultNoSave(def_ItemResults itmRslt);
		int AddItemResult(def_ItemResults itmRslt);
		int DeleteItemResult(def_ItemResults itmRslt);
		def_ItemResults GetItemResultById(int irId);
		// List<def_ItemResults> getItemResultsByItemId(int itemId);
		def_ItemResults GetItemResultByFormResItem(int frmRsltId, int itmId);
		void GetItemResultsResponseVariables(def_ItemResults itemResult);

		// Response Variables
		int AddResponseVariable(def_ResponseVariables rspVar);
		void AddResponseVariableNoSave(def_ResponseVariables rspVar);
		void DeleteResponseVariableNoSave(def_ResponseVariables rv);
		def_ResponseVariables GetResponseVariablesById(int rvId);
		def_ResponseVariables GetResponseVariablesByItemResultItemVariable(int itmRsltId, int itmVrbleId);
		def_ResponseVariables GetResponseVariablesByFormResultIdentifier(int frmRsltId, string identifier);  // Get ResponseVariables by formResultId and ItemVariable identifier
		def_ResponseVariables GetResponseVariablesByFormResultItemVarId(int frmRsltId, int itemVariableId); //Get ResponseVariables by formResultId and ItemVariableId
		def_ResponseVariables GetResponseVariablesBySubjectForm(int subject, int formId, string identifier);
		string CreateNewResponseValues(string frmRsltId, Dictionary<string, string> responsesByIdentifier);

		// Languages
		def_Languages GetLanguageById(int langId);
		def_Languages GetLanguageByTwoLetterISOName(string isoName);
		List<def_Languages> GetAllLanguages();

		// Base Types
		def_BaseTypes GetBaseTypeById(int btId);
		List<def_BaseTypes> GetBaseTypes();

		// File Upload
		int AddFileAttachment(def_FileAttachment fa);
		bool UpdateFileAttachment(def_FileAttachment fa);
		def_FileAttachment GetFileAttachment(int RelatedId, int RelatedEnumId, string fileName);
		def_FileAttachment GetFileAttachment(int RelatedId, int RelatedEnumId);
		def_FileAttachment GetFileAttachment(int fileId);
		List<def_FileAttachment> GetFileAttachmentList(int RelatedId, int RelatedEnumId, int? AttachTypeId = null);
		Dictionary<int, string> GetFileAttachmentDisplayText(int RelatedId, int RelatedEnumId, string StatusFlag = null, int? AttachTypeId = null);
		Dictionary<int, string> GetDistinctFileAttachmentDisplayText(int RelatedId, int RelatedEnumId, string StatusFlag = null, int? AttachTypeId = null);
		int GetRelatedEnumIdByEnumDescription(string enumDescription);
		int GetAttachTypeIdByAttachDescription(string attachDescription);

		// Lookup Master
		List<def_LookupMaster> GetLookupMasters();
		def_LookupMaster GetLookupMasterById(int masterId);
		def_LookupMaster GetLookupMastersByLookupCode(string lkpCd);
		def_LookupMaster SetLookupMaster(string lkpCd, string title, int baseType, def_LookupMaster lkpMstr);
		void DeleteLookupMaster(def_LookupMaster lkpMstr);

		// Lookup Detail
		// List<def_LookupDetail> GetLookupDetailsByLookupMaster(int lkpMstrId);
		List<def_LookupDetail> GetLookupDetailsByLookupMasterEnterprise(int lkpMstrId, int entId);
		List<def_LookupDetail> GetLookupDetails(int lkpMstrId, int entId, int grpId);
		def_LookupDetail GetLookupDetailByEnterpriseMasterAndDataValue(int enterpriseId, int lookupMasterId, string dataValue);
		// List<def_LookupDetail> GetLookupDetails(int lkpMstrId);
		def_LookupDetail GetLookupDetailById(int detailId);
		def_LookupDetail SetLookupDetail(string dtVl, int mstrId, int? entId, int? grpId, int dsplyOrdr, def_LookupDetail lkpDtl);
		void DeleteLookupDetail(def_LookupDetail lkpDtl);
		void AddLookupDetail(def_LookupDetail lkpDtl);
		void SaveLookupDetail(def_LookupDetail lkpDtl);

		// Lookup Text
		List<def_LookupText> GetLookupTextsByLookupDetail(int lkpDtlId);
		List<def_LookupText> GetLookupTextsByLookupDetailLanguage(int lkpDtlId, int langId);
		def_LookupText GetLookupTextById(int textId);
		def_LookupText GetLookupTextByDisplayTextEnterpriseIdMasterLang(string displayText, int enterpriseId, int lookupMasterId, int langId);
		def_LookupText SetLookupText(int detailId, int lang, string displayText, def_LookupText lkpTxt);
		void DeleteLookupText(def_LookupText lkpTxt);
		void AddLookupText(def_LookupText lkpTxt);
		void SaveLookupText(def_LookupText lkpTxt);
        string GetLookupTextByIdentifierDisplyOrderLang(string identifier, int lang, int displayOrder);


		// Access Logging
		def_AccessLogging GetAccessLoggingById(int accessLoggingId);
		int AddAccessLogging(def_AccessLogging accessLogging);
		void AddMultipleAccessLoggings(List<def_AccessLogging> accessLoggings);
		List<def_AccessLogging> GetAllAccessLogsByEnterpriseAndGroups(int entId, List<int> groupIds);
		List<def_AccessLogging> GetFilteredAccessLogs(out int count, int page, int numRecords, int entId, List<int> groupIds, string startDate, string endDate, string userName, string sis);
		Dictionary<int, int> GetFormResultIdsFromAccessLogs(List<def_AccessLogging> logs);
		Dictionary<int, string> GetRecipientNamesFromFormResultIds(Dictionary<int, int> logFormResult);

		// Access Log Functions
		List<def_AccessLogFunctions> GetAccessLogFunctions();
		def_AccessLogFunctions GetAccessLogFunctionsById(int accessLogFunctionsId);
		int AddAccessLogFunctions(def_AccessLogFunctions accessLogFunctions);

		// ADAP Report Methods
		IQueryable<vFormResultUser> GetAssessmentsAndUserNames(int EnterpriseId, int FormId);
		IQueryable<vFormResultUser> GetFormResultsWithSubjects(int entId, int formId/*, int pageStart = 0, int pageSize = 10*/);
		IQueryable<vFormResultUser> GetFormResultsWithSubjects(int entId, IEnumerable<int> formIds);
		IQueryable<vFormResultUser> GetFormResultsWithSubjects(int entId, int subject, IEnumerable<int> formIds);
		IQueryable<vFormResultUser> GetFormResultsWithSubjects(int entId, List<int?> groupIds, IEnumerable<int> formIds);
		//IQueryable<string[]> GetUserContactInfo(int formsResult);

		// Status Master
		List<def_StatusMaster> GetStatusMasters();
		def_StatusMaster GetStatusMasterById(int statusMasterId);
		def_StatusMaster GetStatusMasterByFormId(int formId);
		void AddStatusMaster(def_StatusMaster statusMaster);
		void SaveStatusMaster(def_StatusMaster statusMaster);
		void DeleteStatusMaster(def_StatusMaster statusMaster);

		// Status Detail
		List<def_StatusDetail> GetStatusDetails(int statusMasterId);
		def_StatusDetail GetStatusDetailById(int statusDetailId);
		def_StatusDetail GetStatusDetailByMasterIdentifier(int statusMasterId, string identifier);
		def_StatusDetail GetStatusDetailBySortOrder(int statusMasterId, int sortOrder);
		def_StatusDetail GetStatusDetailByDisplayText(int statusMasterId, string displayText);
		void AddStatusDetail(def_StatusDetail statusDetail);
		void SaveStatusDetail(def_StatusDetail statusDetail);
		void DeleteStatusDetail(def_StatusDetail statusDetail);

		// Status Text
		List<def_StatusText> GetStatusTexts(int statusDetailId, int EnterpriseID, int langId);
		def_StatusText GetStatusText(int statusDetailId, int EnterpriseID, int langId);
		def_StatusText GetStatusTextById(int statusTextId);
		def_StatusText GetStatusTextByDetailSortOrder(int statusMasterId, int sortOrder);
		Dictionary<int?, string> GetStatusDisplayTextsByStatusMasterId(int statusMasterId);
		IQueryable<def_StatusDetail> GetStatusDetailsForPending();
		void AddStatusText(def_StatusText statusText);
		void SaveStatusText(def_StatusText statusText);
		void DeleteStatusText(def_StatusText statusText);

		// Status Log
		def_StatusLog GetStatusLogById(int statusLogId);
		IQueryable<def_StatusLog> GetStatusLogsForFormResultId(int formResultId);
		def_StatusLog GetMostRecentStatusLogByStatusDetailToFormResultIdAndUserId(int statusDetailId, int formResultId, int userId);
		int AddStatusLog(def_StatusLog statusLog);
		void SaveStatusLog(def_StatusLog statusLog);
		void DeleteStatusLog(def_StatusLog statusLog);


		// Status Flow
		def_StatusFlow GetStatusFlowById(int statusFlowId);
		void AddStatusFlow(def_StatusFlow statusFlow);
		void SaveStatusFlow(def_StatusFlow statusFlow);
		void DeleteStatusFlow(def_StatusFlow statusFlow);
		List<def_StatusFlow> GetStatusFlowByDetailEntRole(int statusDetailId, int EnterpriseID, int rolePermission);


		// Web Service Activity
		def_WebServiceActivity GetWebServiceActivityById(int webServiceActivityId);
		int AddWebServiceActivity(def_WebServiceActivity webServiceActivity);
		List<def_WebServiceActivity> GetUnprocessedWebServiceActivityRequestsByEntId(int entId);
		void SaveWebServiceActivity(def_WebServiceActivity wsa);
	}

}
