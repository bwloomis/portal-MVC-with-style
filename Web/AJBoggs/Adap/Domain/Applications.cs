
using Assmnts;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Data.Abstract;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using NLog;
using UAS.Business;
using Assmnts.UasServiceRef;

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

		private IFormsRepository formsRepo;
		private ILogger mLogger;
        private IAuthentication authClient;

        public Applications(IFormsRepository fr)
		{
			formsRepo = fr;
			mLogger = LogManager.GetCurrentClassLogger();
            authClient = new AuthenticationClient();
		}

		public static string GetAdapTemplatesViewDirPath(int entId)
		{
			string viewDirPath = "~/Views/Templates/ADAP/";

			if (entId == 7) {
				viewDirPath = "~/Views/LA_ADAP/";
			} else if (entId == 8) {
				viewDirPath = "~/Views/CAADAP/";
			}

			return viewDirPath;
		}


		/// <summary>
		/// Deterimine which .cshtml file to use for rendering the "default" ADAP report grid
		/// 
		/// Used in AdapController/Report1
		/// </summary>
		/// <param name="entId"></param>
		/// <returns></returns>
		public static string GetDefaultReportViewPathForEnterprise(int entId)
		{
			switch (entId) {
				case 7: //Louisiana
					return "~/Views/LA_ADAP/LA_Report.cshtml";
				case 8:
					return "~/Views/CAADAP/ClientProfile.cshtml";
				default: //Colorado
					return "~/Views/Templates/ADAP/report1.cshtml";
			}
		}

		/// <summary>
		/// Returns a list of the forms that are available to an enterprise.
		/// </summary>
		/// <param name="entId"></param>
		/// <returns>a dictionary where values are form identifiers, keys are formIds</returns>
		public static List<string> GetDefaultFormIdentifiersForEnterprise(int entId)
		{
			switch (entId) {
				case 7: //Louisiana
					return new string[] { "LA-ADAP", "LA-ADAP-Stub", "LA-ADAP-PreIns" }.ToList();

				default: //Colorado
					return new string[] { "ADAP" }.ToList();
			}
		}


		/// <summary>
		/// Primary method for processing database queries posted to DataTables.  Intended to be universal for all ADAP Applicant and Dashboard Reports.
		/// </summary>
		/// <param name="query">The query initialized by the DataTable Web Service calling this method.</param>
		/// <param name="sFName">String of the applicant's first name</param>
		/// <param name="sLName">String of the applicant's last name</param>
		/// <param name="sTeam">String of the Team associated with the application.</param>
		/// <param name="sStat">String of the Status of the application.  Some reports use a specialized key word to add additional parameters to the query.</param>
		/// <param name="sDate">String of the Date of the application.  Some reports use specialized key words to add additional parameters to the query.</param>
		/// <returns>IQueryable<vFormResultUsers> with additional parameters added for the DataTable.</returns>
		public IQueryable<vFormResultUser> SetVfruQueryParams(IQueryable<vFormResultUser> query, String sFName, String sLName, String sTeam, String sStat, String sDate, String sType, int statusMasterId = 1)
		{
			query = query.Where(q => q.StatusFlag.Equals("A"));
			if (!String.IsNullOrEmpty(sFName)) {
				query = query.Where(q => q.FirstName.Contains(sFName));
			}

			if (!String.IsNullOrEmpty(sLName)) {
				query = query.Where(q => q.LastName.Contains(sLName));
			}

			if (!String.IsNullOrEmpty(sTeam) && !sTeam.Equals("All")) {
				query = query.Where(q => q.GroupName.Contains(sTeam));
			}

			if (!String.IsNullOrEmpty(sType) && !sType.Equals("All")) {
				int formId = formsRepo.GetFormByIdentifier(sType).formId;
				query = query.Where(q => q.formId == formId);
			}

			if (!String.IsNullOrEmpty(sStat) && !sStat.Equals("All")) {
				if (sStat.Equals("Pending") || sStat.Equals("All Pending")) {
					// In Process = 0, Needs Review = 1, Needs Information = 2
					int inProcess = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "IN_PROCESS").sortOrder);
					int needsReview = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "NEEDS_REVIEW").sortOrder);
					int needsInformation = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "NEEDS_INFORMATION").sortOrder);
					query = query.Where(q => q.formStatus == inProcess || q.formStatus == needsReview || q.formStatus == needsInformation);
				} else {
					if (sStat.Contains(" Pending")) {
						sStat = sStat.Substring(0, sStat.IndexOf(" Pending"));
					}

					// In Process = 0, Needs Review = 1, Needs Information = 2, Denied = 3, Approved = 4, Cancelled = 5
					int index = Convert.ToInt32(formsRepo.GetStatusDetailByDisplayText(statusMasterId, sStat).sortOrder);
					query = query.Where(q => q.formStatus == index);
				}
			}

			if (!String.IsNullOrEmpty(sDate) && !sDate.Equals("All")) {
				int formId = formsRepo.GetFormByIdentifier("ADAP").formId;
				// "Re-Certs Late" still needs to be added.
				// Current calculation does not account for Late re-certifications.
				if (sDate.Contains("Last Modified within")) {
					try {
						int span = Convert.ToInt32(sDate.Substring(21, 1));
						DateTime compare = DateTime.Now.AddDays(span * -30);
						query = query.Where(q => q.dateUpdated >= compare);
					}
					catch (FormatException ex) {
						Debug.WriteLine("Adap Applications sDate span: " + ex.Message);
					}
				}
				if (sDate.Contains("Re-Certs")) {
					/// Base Rule: END of birth month, END of birth month +6.
					///
					/// Upon first approval, if base rule results in LESS THAN 3 months, advance to the next 6 month date.  E.g. birth month March, 
					/// initially approved July.  Instead of recert due Sept 30, due next March 31.  Exception applies for initial approval.
					/// After that, even if a recert is late, it doesn't delay the next recert due from the Base Rule.

					int approved = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "APPROVED").sortOrder);
					IQueryable<vFormResultUser> subQuery = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, formId)
							.Where(sq => sq.formStatus == approved && sq.StatusFlag.Equals("A")).OrderByDescending(sq => sq.statusChangeDate);

					// Duplicated variable to prevent InvalidOperationException: "A cycle was detected in a LINQ expression."
					IQueryable<vFormResultUser> accepted = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, formId)
							.Where(sq => sq.formStatus == approved && sq.StatusFlag.Equals("A")).OrderByDescending(sq => sq.statusChangeDate);

					// DbFunctions formula:  AddDays(AddMonths(CreateDatetime()))
					// CreateDateTime() to assemble the birthdate of the current year on the first of the month.
					// AddMonths() to either advance the birthdate to 7 months in the future, or a single month in the future.
					// AddDays() to subtract a single day, resulting in either the last day of the month 6 months after the birthdate, or the last day of the birth month.

					// Logic used below should reflect the logic used in the getRecert method.
					// nextRecert pseudocode: create new IQueryable<RecertObject>
					// Test if the current time falls between the birth month and 6 months from the birth month ( DOB < Today < DOB + 6 Months )
					//      if true: use DbFunctions formula to find the last day of the month 6 months after the birthdate
					//      if false: Test if the current time is later than 6 months from teh birth month.  ( DOB + 6 Months < Today )
					//          if true: use DbFunctions formula to find the last day of the birth month in the following year.
					//          if false: use DbFunctions formula to find the last day of the birth month
					IQueryable<RecertObject> nextRecert = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, formId)
							.Select(nr => new RecertObject {
								formResultId = nr.formResultId,
								subject = nr.subject,
								recert = (nr.DOB.Value.Month < DateTime.Now.Month && DateTime.Now.Month <= nr.DOB.Value.Month + 6)
									? DbFunctions.AddDays(DbFunctions.AddMonths(DbFunctions.CreateDateTime(DateTime.Now.Year, nr.DOB.Value.Month, 1, 0, 0, 0), 7), -1)
									: (nr.DOB.Value.Month + 6 < DateTime.Now.Month)
									? DbFunctions.AddDays(DbFunctions.AddMonths(DbFunctions.CreateDateTime(DateTime.Now.Year + 1, nr.DOB.Value.Month, 1, 0, 0, 0), 1), -1)
									: DbFunctions.AddDays(DbFunctions.AddMonths(DbFunctions.CreateDateTime(DateTime.Now.Year, nr.DOB.Value.Month, 1, 0, 0, 0), 1), -1)
							});

					// applyExcep pseudocode: edit nextRecert object to apply the 3 month exception.
					// Ensure there is only one accepted application, then
					// Test if the absolute value of the most recent statusChangeDate for the subject minus the nextRecert.recert is LESS THAN 3 ( statusChangeDate - recert < 3 )
					//      if true: Find the last day of the month 6 months from the recert with recert.AddDays(+1) -> AddMonths(+6) -> AddDays(-1)
					//      if false: Use the established recert date.
					IQueryable<RecertObject> applyExcep = nextRecert.Where(nr => accepted.Where(sq => sq.subject == nr.subject).Count() > 0)
							.Select(nr => new RecertObject {
								formResultId = nr.formResultId,
								subject = nr.subject,
								recert = (accepted.Where(sq => sq.subject == nr.subject).Count() == 1
											&& Math.Abs(accepted.Select(sq => sq.statusChangeDate).FirstOrDefault().Value.Month - nr.recert.Value.Month) < 3)
											? DbFunctions.AddDays(DbFunctions.AddMonths(DbFunctions.AddDays(nr.recert, 1), 6), -1)
											: nr.recert
							});

					//// Data check for validation.
					//List<RecertObject> recertCheck = new List<RecertObject>();
					//foreach (var v in applyExcep)
					//{
					//    recertCheck.Add(v);
					//}

					DateTime deadline = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
					deadline = deadline.AddMonths(1).AddDays(-1);
					if (sDate.Contains("Re-Certs Late")) {
						// Adjust the deadline to 6 months prior to the current deadline.
						deadline = deadline.AddDays(1).AddMonths(-6).AddDays(-1);

						// Take the most recently accepted application and check if the statusChangeDate falls before the deadline.
						subQuery = subQuery.Where(sq => accepted.Where(ac => ac.subject == sq.subject).Select(ac => ac.statusChangeDate).FirstOrDefault() < deadline);
					} else if (sDate.Contains("Re-Certs Due within 7 Days")) {
						// Adjust deadline to the current date so recerts due will only show when the due date is within 7 days.
						deadline = DateTime.Now;
						DateTime range = deadline.AddDays(7);

						subQuery = subQuery.Where(sq => deadline <= applyExcep.Where(nr => nr.subject == sq.subject).Select(nr => nr.recert).FirstOrDefault()
								&& applyExcep.Where(nr => nr.subject == sq.subject).Select(nr => nr.recert).FirstOrDefault() <= range);

						//// Data check for validation
						//List<string[]> dataCheck = new List<string[]>();
						//foreach (var v in subQuery)
						//{
						//    dataCheck.Add(new string[] { v.subject.ToString() });
						//}
					} else if (sDate.Contains("Re-Certs Due within")) {
						try {
							// This block begins by parsing the month multiplier from the selection string.
							int span = Convert.ToInt32(sDate.Substring(20, 1));
							DateTime range = deadline.AddDays(1).AddMonths(span).AddDays(-1);
							subQuery = subQuery.Where(sq => deadline <= applyExcep.Where(nr => nr.subject == sq.subject).Select(nr => nr.recert).FirstOrDefault()
									&& applyExcep.Where(nr => nr.subject == sq.subject).Select(nr => nr.recert).FirstOrDefault() <= range);
						}
						catch (FormatException ex) {
							Debug.WriteLine("Adap Reports Controller sDate span: " + ex.Message);
						}
					}

					query = query.Where(q => subQuery.Where(sq => sq.subject == q.subject).Select(sq => sq.subject).FirstOrDefault() == q.subject);
					query = query.OrderByDescending(q => q.dateUpdated);
				} else if (sDate.Contains("&")) {
					string[] dates = sDate.Split('&');
					DateTime from;
					bool fromBool = true;
					DateTime to;
					bool toBool = true;
					if (!String.IsNullOrEmpty(dates[0])) {
						try {
							from = Convert.ToDateTime(dates[0]);
						}
						catch (FormatException ex) {
							Debug.WriteLine("Adap Reports Controller sDate from date conversation: " + ex.Message);
							from = new DateTime(2000, 1, 1);
						}
					} else {
						from = new DateTime(2000, 1, 1);
						fromBool = false;
					}

					if (!String.IsNullOrEmpty(dates[1])) {
						try {
							to = Convert.ToDateTime(dates[1]);
						}
						catch (FormatException ex) {
							Debug.WriteLine("Adap Reports Controller sDate to date conversation: " + ex.Message);
							to = DateTime.Now.AddDays(1);
						}
					} else {
						to = DateTime.Now.AddDays(1);
						toBool = false;
					}

					IQueryable<vFormResultUser> subQuery = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, formId);
					int needsReview = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "NEEDS_REVIEW").sortOrder);
					subQuery = subQuery.Where(sq => /*sq.formStatus == needsReview &&*/ sq.StatusFlag.Equals("A"));
					if (dates[2].Contains("Overview")) {
						subQuery = subQuery.Where(sq => (sq.formStatus == needsReview) &&
								((fromBool) ? sq.statusChangeDate >= from : true) && ((toBool) ? sq.statusChangeDate <= to : true)).OrderByDescending(sq => sq.statusChangeDate);
					} else if (dates[2].Contains("Summary")) {
						subQuery = subQuery.Where(sq => ((fromBool) ? sq.statusChangeDate >= from : true) && ((toBool) ? sq.statusChangeDate <= to : true))
								.OrderByDescending(sq => sq.statusChangeDate);
					}
					//List<string[]> dataCheck = new List<string[]>();
					//foreach (var v in subQuery)
					//{
					//    dataCheck.Add(new string[] { v.subject.ToString(), v.formStatus.ToString(), v.GroupName, v.statusChangeDate.ToString() });
					//}

					if (dates[2].Contains("Overview")) {
						query = query.Where(q => q.subject == subQuery.Where(sq => sq.subject == q.subject).Select(sq => sq.subject).FirstOrDefault());
					} else if (dates[2].Contains("Summary")) {
						query = query.Where(q => q.formResultId == subQuery.Where(sq => sq.subject == q.subject)
								.OrderByDescending(sq => sq.statusChangeDate).Select(sq => sq.formResultId).FirstOrDefault());
					}
				}

			}

			return query;
		}

		/// <summary>
		/// Primary method for processing database queries posted to DataTables.  Intended to be universal for all ADAP Applicant and Dashboard Reports.
		/// </summary>
		/// <param name="query">The query initialized by the DataTable Web Service calling this method.</param>
		/// <param name="sFName">String of the applicant's first name</param>
		/// <param name="sLName">String of the applicant's last name</param>
		/// <param name="sTeam">String of the Team associated with the application.</param>
		/// <param name="sStat">String of the Status of the application.  Some reports use a specialized key word to add additional parameters to the query.</param>
		/// <param name="sDate">String of the Date of the application.  Some reports use specialized key words to add additional parameters to the query.</param>
		/// <returns>IQueryable<vFormResultUsers> with additional parameters added for the DataTable.</returns>
		public IQueryable<vFormResultUser> SetVfruQueryParams(IQueryable<vFormResultUser> query, String sFName, String sLName, String sTeam, String sStat, String sDate, String sDob, String ssn, string adapId,
				string siteNum, string enrollmentSite, String formType, String elgEndFrom, String elgEndTo, int statusMasterId = 1)
		{
			query = query.Where(q => q.StatusFlag.Equals("A"));
			if (!String.IsNullOrEmpty(sFName)) {
				query = query.Where(q => q.FirstName.Contains(sFName));
			}

			if (!String.IsNullOrEmpty(sLName)) {
				query = query.Where(q => q.LastName.Contains(sLName));
			}

			if (!string.IsNullOrWhiteSpace(siteNum)) {
				query = query.Where(q => q.GroupName.StartsWith(siteNum));
			}

            int formId = 15;

			if (!string.IsNullOrWhiteSpace(formType)) {
                var parts = formType.Split('|');
                var formIdPart = parts[0];
                if (int.TryParse(formIdPart, out formId))
                {
					query = query.Where(q => q.formId == formId);
				}

                if (formId == 15)
                {
                    var formVariant = parts[1];
                    if (formVariant != "Initial Enrollment Application")
                    {
                        query = query.Where(q => q.formVariant == formVariant);
                    }
                    else
                    {
                        query = query.Where(q => q.formVariant == null || q.formVariant == formVariant);
                    }
                }
			}

			if (!string.IsNullOrWhiteSpace(elgEndFrom)) {
				DateTime elgEndFromDate;
				if (DateTime.TryParse(elgEndFrom, out elgEndFromDate)) {
					query = query.Where(q => q.EligibilityEndDate >= elgEndFromDate);
				}
			}

			if (!string.IsNullOrWhiteSpace(elgEndTo)) {
				DateTime elgEndToDate;
				if (DateTime.TryParse(elgEndTo, out elgEndToDate)) {
					query = query.Where(q => q.EligibilityEndDate <= elgEndToDate);
				}
			}

			if (!String.IsNullOrWhiteSpace(enrollmentSite)) {
				var context = DataContext.getUasDbContext();
				var groupIds = (from g in context.uas_Group
												where g.GroupDescription.Contains(enrollmentSite)
												&& g.GroupTypeID == 193
												select g.GroupID).ToList();
				query = query.Where(q => groupIds.Contains(q.GroupID.Value));
			}

			if (!String.IsNullOrEmpty(sTeam) && !sTeam.Equals("All")) {
				// serach by unit
				// get child groups of unit
				var context = DataContext.getUasDbContext();
				int groupId;
				int.TryParse(sTeam, out groupId);
				List<int> groupIds = (from g in context.uas_Group
															where g.ParentGroupId == groupId
															|| g.GroupID == groupId
															select g.GroupID).ToList();

				query = query.Where(q => groupIds.Contains(q.GroupID.Value));
			}

			if (!String.IsNullOrEmpty(sStat) && !sStat.Equals("All")) {
				if (sStat.Equals("Pending") || sStat.Equals("All Pending")) {
					// In Process = 0, Needs Review = 1, Needs Information = 2
					int inProcess = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "IN_PROCESS").sortOrder);
					int needsReview = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "NEEDS_REVIEW").sortOrder);
					int needsInformation = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "NEEDS_INFORMATION").sortOrder);
					query = query.Where(q => q.formStatus == inProcess || q.formStatus == needsReview || q.formStatus == needsInformation);
				} else {
					if (sStat.Contains(" Pending")) {
						sStat = sStat.Substring(0, sStat.IndexOf(" Pending"));
					}

					// In Process = 0, Needs Review = 1, Needs Information = 2, Denied = 3, Approved = 4, Cancelled = 5
                    if (!string.IsNullOrWhiteSpace(formType))
                    {
                        def_StatusMaster statusMaster = formsRepo.GetStatusMasterByFormId(formId);
                        statusMasterId = statusMaster == null ? 1 : statusMaster.statusMasterId;
                        var statusDisplay = formsRepo.GetStatusDetailByDisplayText(statusMasterId, sStat);
                        int index = statusDisplay != null ? statusDisplay.sortOrder.Value : -1;
                        query = query.Where(q => q.formStatus == index);
                    }
                    else
                    {
                        // all form types
                        // form enrollment
                        int formIdStandard = 15;
                        int formIdMoop = 17;
                        int formIdSvf = 19;
                        def_StatusMaster statusMaster = formsRepo.GetStatusMasterByFormId(formIdStandard);
                        statusMasterId = statusMaster == null ? 1 : statusMaster.statusMasterId;
                        var statusDisplay = formsRepo.GetStatusDetailByDisplayText(statusMasterId, sStat);
                        int index = statusDisplay != null ? statusDisplay.sortOrder.Value : -1;
                        
                        // form moop
                        def_StatusMaster statusMasterMoop = formsRepo.GetStatusMasterByFormId(formIdMoop);
                        var statusMasterIdMoop = statusMasterMoop == null ? 1 : statusMasterMoop.statusMasterId;
                        var statusDisplayMoop = formsRepo.GetStatusDetailByDisplayText(statusMasterIdMoop, sStat);
                        int indexMoop = statusDisplayMoop != null ? statusDisplayMoop.sortOrder.Value : -1;

                        // form svf
                        def_StatusMaster statusMasterSvf = formsRepo.GetStatusMasterByFormId(formIdSvf);
                        var statusMasterIdSvf = statusMasterSvf == null ? 1 : statusMasterSvf.statusMasterId;
                        var statusDisplaySvf = formsRepo.GetStatusDetailByDisplayText(statusMasterIdSvf, sStat);
                        int indexSvf = statusDisplaySvf != null ? statusDisplaySvf.sortOrder.Value : -1;

                        query = query.Where(q => (q.formStatus == index && q.formId == formIdStandard)
                            || (q.formStatus == indexSvf && q.formId == formIdSvf) 
                            || (q.formStatus == indexMoop && q.formId == formIdMoop));
                    }
				}
			}

			if (!String.IsNullOrWhiteSpace(sDob)) {
				DateTime dob;
				if (DateTime.TryParse(sDob, out dob)) {
					query = query.Where(q => q.DOB == dob);
				}
			}

			if (!string.IsNullOrWhiteSpace(ssn)) {
				formsEntities context = new formsEntities();
				var formResultIds = (from iv in context.def_ItemVariables
														 join rv in context.def_ResponseVariables on iv.itemVariableId equals rv.itemVariableId
														 join ir in context.def_ItemResults on rv.itemResultId equals ir.itemResultId
														 where iv.identifier == "C1_MemberSocSecNumber" && rv.rspValue == ssn
														 select ir.formResultId);
				query = from q in query
								join fr in formResultIds.ToList() on q.formResultId equals fr
								select q;
			}

			if (!string.IsNullOrWhiteSpace(adapId)) {
                query = query.Where(x => x.adap_id == adapId);
			}

			if (!String.IsNullOrEmpty(sDate) && !sDate.Equals("All")) {
				//int formId = formsRepo.GetFormByIdentifier("ADAP").formId;
				// "Re-Certs Late" still needs to be added.
				// Current calculation does not account for Late re-certifications.
				if (sDate.Contains("Last Modified within")) {
					try {
						int span = Convert.ToInt32(sDate.Substring(21, 1));
						DateTime compare = DateTime.Now.AddDays(span * -30);
						query = query.Where(q => q.dateUpdated >= compare);
					}
					catch (FormatException ex) {
						Debug.WriteLine("Adap Applications sDate span: " + ex.Message);
					}
				}
				if (sDate.Contains("Re-Certs")) {
					/// Base Rule: END of birth month, END of birth month +6.
					///
					/// Upon first approval, if base rule results in LESS THAN 3 months, advance to the next 6 month date.  E.g. birth month March, 
					/// initially approved July.  Instead of recert due Sept 30, due next March 31.  Exception applies for initial approval.
					/// After that, even if a recert is late, it doesn't delay the next recert due from the Base Rule.

					int approved = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "APPROVED").sortOrder);
					IQueryable<vFormResultUser> subQuery = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, formId)
							.Where(sq => sq.formStatus == approved && sq.StatusFlag.Equals("A")).OrderByDescending(sq => sq.statusChangeDate);

					// Duplicated variable to prevent InvalidOperationException: "A cycle was detected in a LINQ expression."
					IQueryable<vFormResultUser> accepted = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, formId)
							.Where(sq => sq.formStatus == approved && sq.StatusFlag.Equals("A")).OrderByDescending(sq => sq.statusChangeDate);

					// DbFunctions formula:  AddDays(AddMonths(CreateDatetime()))
					// CreateDateTime() to assemble the birthdate of the current year on the first of the month.
					// AddMonths() to either advance the birthdate to 7 months in the future, or a single month in the future.
					// AddDays() to subtract a single day, resulting in either the last day of the month 6 months after the birthdate, or the last day of the birth month.

					// Logic used below should reflect the logic used in the getRecert method.
					// nextRecert pseudocode: create new IQueryable<RecertObject>
					// Test if the current time falls between the birth month and 6 months from the birth month ( DOB < Today < DOB + 6 Months )
					//      if true: use DbFunctions formula to find the last day of the month 6 months after the birthdate
					//      if false: Test if the current time is later than 6 months from teh birth month.  ( DOB + 6 Months < Today )
					//          if true: use DbFunctions formula to find the last day of the birth month in the following year.
					//          if false: use DbFunctions formula to find the last day of the birth month
					IQueryable<RecertObject> nextRecert = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, formId)
							.Select(nr => new RecertObject {
								formResultId = nr.formResultId,
								subject = nr.subject,
								recert = (nr.DOB.Value.Month < DateTime.Now.Month && DateTime.Now.Month <= nr.DOB.Value.Month + 6)
									? DbFunctions.AddDays(DbFunctions.AddMonths(DbFunctions.CreateDateTime(DateTime.Now.Year, nr.DOB.Value.Month, 1, 0, 0, 0), 7), -1)
									: (nr.DOB.Value.Month + 6 < DateTime.Now.Month)
									? DbFunctions.AddDays(DbFunctions.AddMonths(DbFunctions.CreateDateTime(DateTime.Now.Year + 1, nr.DOB.Value.Month, 1, 0, 0, 0), 1), -1)
									: DbFunctions.AddDays(DbFunctions.AddMonths(DbFunctions.CreateDateTime(DateTime.Now.Year, nr.DOB.Value.Month, 1, 0, 0, 0), 1), -1)
							});

					// applyExcep pseudocode: edit nextRecert object to apply the 3 month exception.
					// Ensure there is only one accepted application, then
					// Test if the absolute value of the most recent statusChangeDate for the subject minus the nextRecert.recert is LESS THAN 3 ( statusChangeDate - recert < 3 )
					//      if true: Find the last day of the month 6 months from the recert with recert.AddDays(+1) -> AddMonths(+6) -> AddDays(-1)
					//      if false: Use the established recert date.
					IQueryable<RecertObject> applyExcep = nextRecert.Where(nr => accepted.Where(sq => sq.subject == nr.subject).Count() > 0)
							.Select(nr => new RecertObject {
								formResultId = nr.formResultId,
								subject = nr.subject,
								recert = (accepted.Where(sq => sq.subject == nr.subject).Count() == 1
											&& Math.Abs(accepted.Select(sq => sq.statusChangeDate).FirstOrDefault().Value.Month - nr.recert.Value.Month) < 3)
											? DbFunctions.AddDays(DbFunctions.AddMonths(DbFunctions.AddDays(nr.recert, 1), 6), -1)
											: nr.recert
							});

					//// Data check for validation.
					//List<RecertObject> recertCheck = new List<RecertObject>();
					//foreach (var v in applyExcep)
					//{
					//    recertCheck.Add(v);
					//}

					DateTime deadline = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
					deadline = deadline.AddMonths(1).AddDays(-1);
					if (sDate.Contains("Re-Certs Late")) {
						// Adjust the deadline to 6 months prior to the current deadline.
						deadline = deadline.AddDays(1).AddMonths(-6).AddDays(-1);

						// Take the most recently accepted application and check if the statusChangeDate falls before the deadline.
						subQuery = subQuery.Where(sq => accepted.Where(ac => ac.subject == sq.subject).Select(ac => ac.statusChangeDate).FirstOrDefault() < deadline);
					} else if (sDate.Contains("Re-Certs Due within 7 Days")) {
						// Adjust deadline to the current date so recerts due will only show when the due date is within 7 days.
						deadline = DateTime.Now;
						DateTime range = deadline.AddDays(7);

						subQuery = subQuery.Where(sq => deadline <= applyExcep.Where(nr => nr.subject == sq.subject).Select(nr => nr.recert).FirstOrDefault()
								&& applyExcep.Where(nr => nr.subject == sq.subject).Select(nr => nr.recert).FirstOrDefault() <= range);

						//// Data check for validation
						//List<string[]> dataCheck = new List<string[]>();
						//foreach (var v in subQuery)
						//{
						//    dataCheck.Add(new string[] { v.subject.ToString() });
						//}
					} else if (sDate.Contains("Re-Certs Due within")) {
						try {
							// This block begins by parsing the month multiplier from the selection string.
							int span = Convert.ToInt32(sDate.Substring(20, 1));
							DateTime range = deadline.AddDays(1).AddMonths(span).AddDays(-1);
							subQuery = subQuery.Where(sq => deadline <= applyExcep.Where(nr => nr.subject == sq.subject).Select(nr => nr.recert).FirstOrDefault()
									&& applyExcep.Where(nr => nr.subject == sq.subject).Select(nr => nr.recert).FirstOrDefault() <= range);
						}
						catch (FormatException ex) {
							Debug.WriteLine("Adap Reports Controller sDate span: " + ex.Message);
						}
					}

					query = query.Where(q => subQuery.Where(sq => sq.subject == q.subject).Select(sq => sq.subject).FirstOrDefault() == q.subject);
					query = query.OrderByDescending(q => q.dateUpdated);
				} else if (sDate.Contains("&")) {
					string[] dates = sDate.Split('&');
					DateTime from;
					bool fromBool = true;
					DateTime to;
					bool toBool = true;
					if (!String.IsNullOrEmpty(dates[0])) {
						try {
							from = Convert.ToDateTime(dates[0]);
						}
						catch (FormatException ex) {
							Debug.WriteLine("Adap Reports Controller sDate from date conversation: " + ex.Message);
							from = new DateTime(2000, 1, 1);
						}
					} else {
						from = new DateTime(2000, 1, 1);
						fromBool = false;
					}

					if (!String.IsNullOrEmpty(dates[1])) {
						try {
							to = Convert.ToDateTime(dates[1]);
						}
						catch (FormatException ex) {
							Debug.WriteLine("Adap Reports Controller sDate to date conversation: " + ex.Message);
							to = DateTime.Now.AddDays(1);
						}
					} else {
						to = DateTime.Now.AddDays(1);
						toBool = false;
					}

					IQueryable<vFormResultUser> subQuery = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, formId);
					int needsReview = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "NEEDS_REVIEW").sortOrder);
					subQuery = subQuery.Where(sq => /*sq.formStatus == needsReview &&*/ sq.StatusFlag.Equals("A"));
					if (dates[2].Contains("Overview")) {
						subQuery = subQuery.Where(sq => (sq.formStatus == needsReview) &&
								((fromBool) ? sq.statusChangeDate >= from : true) && ((toBool) ? sq.statusChangeDate <= to : true)).OrderByDescending(sq => sq.statusChangeDate);
					} else if (dates[2].Contains("Summary")) {
						subQuery = subQuery.Where(sq => ((fromBool) ? sq.statusChangeDate >= from : true) && ((toBool) ? sq.statusChangeDate <= to : true))
								.OrderByDescending(sq => sq.statusChangeDate);
					}
					//List<string[]> dataCheck = new List<string[]>();
					//foreach (var v in subQuery)
					//{
					//    dataCheck.Add(new string[] { v.subject.ToString(), v.formStatus.ToString(), v.GroupName, v.statusChangeDate.ToString() });
					//}

					if (dates[2].Contains("Overview")) {
						query = query.Where(q => q.subject == subQuery.Where(sq => sq.subject == q.subject).Select(sq => sq.subject).FirstOrDefault());
					} else if (dates[2].Contains("Summary")) {
						query = query.Where(q => q.formResultId == subQuery.Where(sq => sq.subject == q.subject)
								.OrderByDescending(sq => sq.statusChangeDate).Select(sq => sq.formResultId).FirstOrDefault());
					}
				}

			}

			return query;
		}

		/// <summary>
		/// Determines the next schedualed recertification date for a given applicant.
		/// This method shouldn't be called when the count of approved applications is 0, returns 1/1/0001 if it is.
		/// 
		/// Base Rule: END of birth month, END of birth month +6.
		///
		/// Upon first approval, if base rule results in less than 3 months, advance to the next 6 month date.  E.g. birth month March, 
		/// initially approved July.  Instead of recert due Sept 30, due next March 31.  Exception applies for initial approval.
		/// After that, even if a recert is late, it doesn't delay the next recert due from the Base Rule.
		/// 
		/// </summary>
		/// <param name="dob">The subject's listed Date of Birth</param>
		/// <param name="count">The number of approved applications for the subject</param>
		/// <param name="recentRecert">The statusChangeDate of the most recently approved application</param>
		/// <returns>DateTime of the next Recertification date, defaults to 1/1/0001 if calculation does not apply.</returns>
		public DateTime GetRecert(DateTime? dob, int count, DateTime recentRecert)
		{
			DateTime recert = new DateTime();
			try {
				DateTime trueDob = Convert.ToDateTime(dob);
				DateTime thisYearDob = new DateTime(DateTime.Now.Year, trueDob.AddMonths(1).Month, 1).AddDays(-1);
				DateTime sixMonthOff = thisYearDob.AddDays(1).AddMonths(6).AddDays(-1);
				if (count > 0) {
					// Base Rule.
					// Test if the current time falls between the birth month and 6 months from the birth month ( DOB < Today < DOB + 6 Months )
					//      if true: set recert to the last day of the month 6 months after the birthdate
					//      if false: Test if the current time is later than 6 months from the birth month.  ( DOB + 6 Months < Today )
					//          if true: set recert to the last day of the birth month in the following year.
					//          if false: set recert to the last day of the birth month
					if (thisYearDob < DateTime.Now && DateTime.Now < sixMonthOff) {
						recert = sixMonthOff;
					} else if (sixMonthOff < DateTime.Now) {
						recert = thisYearDob.AddYears(1);
					} else {
						recert = thisYearDob;
					}
				} else {
					// This method shouldn't be called when the count of approved applications is 0.
					recert = new DateTime(1, 1, 1);
				}

				// 3 Month exception for first time applicants.
				// Ensure there is only one accepted application, then
				// Test if the absolute value of the most recent statusChangeDate for the subject minus the calculated recert is LESS THAN 3 ( statusChangeDate - recert < 3 )
				//      if true: Find the last day of the month 6 months from the recert with recert.AddDays(+1) -> AddMonths(+6) -> AddDays(-1)
				//      if false: Use the established recert date.
				if (count == 1 && Math.Abs(recentRecert.Month - recert.Month) < 3) {
					recert = recert.AddDays(1).AddMonths(6).AddDays(-1);
				}

			}
			catch (Exception ex) {
				//recert = "Invalid birthdate/Cannot calculate.";
				Debug.WriteLine("getRecert exception: " + ex.Message);
			}

			return recert;
		}

        /// <summary>
        /// To check whether a user is authorized to access the form result id
        /// </summary>
        /// <param name="formresultId"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static bool CACheckUserHasAccesstoFormResult(int formresultId, int userid)
        {
            AuthenticationClient authClient = new AuthenticationClient();

            bool isAuthorizedFrId = false;

            formsEntities dbContext = DataContext.GetDbContext();

            //Will change to constant from constants file after merging the attchments branch
            int enterpriseId = 8;

            List<int> authorizedFormResultIds = new List<int>();

            if (UAS_Business_Functions.hasPermission(PermissionConstants.ASSIGNED, PermissionConstants.ASSMNTS))
            {
                authorizedFormResultIds = dbContext.vFormResultUsers
                                                      .Where(fr => (fr.subject == userid) && (fr.EnterpriseID == enterpriseId))
                                                      .Select(fr => fr.formResultId)
                                                      .ToList();
            }
            else
            {
                //Other
                var groupIds = authClient.GetGroupsInUserPermissions(enterpriseId, SessionHelper.LoginStatus.UserID).GroupBy(x => x.GroupID).Select(x => (int?)x.FirstOrDefault().GroupID);
                authorizedFormResultIds = dbContext.vFormResultUsers
                               .Where(fr => groupIds.Contains(fr.GroupID) && (fr.EnterpriseID == enterpriseId))
                               .Select(fr => fr.formResultId)
                               .ToList();
            }

            if (authorizedFormResultIds.Contains(formresultId))
            {
                isAuthorizedFrId = true;
            }

            return isAuthorizedFrId;
        }
    }
}
