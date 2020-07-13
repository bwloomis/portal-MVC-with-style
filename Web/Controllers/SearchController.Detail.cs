using Assmnts.Business;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;
using UAS.Business;


namespace Assmnts.Controllers
{
    public partial class SearchController : Controller
    {
        [HttpGet]
        public ActionResult DetailSearch()
        {
            Debug.WriteLine("DetailSearch()");


            SearchModel model = new SearchModel();
            model.DetailSearchCriteria = (SearchCriteria)Session["DetailSearchCriteria"];
            model.vSearchResult = DetailSearch(model.DetailSearchCriteria);

            //model.sisIds = model.SearchResult.Select(r => r.formResultId).ToList();
            //model.formIds = model.SearchResult.Select(r => r.formId).Distinct().ToList();

            //addDictionaries(model);
            // the addDictionaries code below except for the Export 
            model.Groups = GetGroupsDictionary();
            model.Enterprises = GetEnterprisesDictionary();

            model.userNames = GetUserNamesDictionary();

            model.Forms = Business.Forms.GetFormsDictionary(formsRepo);

            model.AllForms = GetAllFormsDictionary();

            model.reviewStatuses = Business.ReviewStatus.GetReviewStatuses(formsRepo);

            model.usedForms = GetAllFormsDictionary();

            Session["SearchModel"] = model;

            return View("Index", model);
        }


        [HttpPost]
        public ActionResult DetailSearch(SearchModel model, string command)
        {
            Debug.WriteLine("DetailSearch(model, command)");

            if (command.StartsWith("Clear"))
            {
                Session["GeneralSearchString"] = null;
                Session["DetailSearchCriteria"] = null;

                return RedirectToAction("Index", "Search");
            }
            else
            {
                Session["GeneralSearchString"] = null;
                Session["DetailSearchCriteria"] = model.DetailSearchCriteria;
                model.vSearchResult = DetailSearch(model.DetailSearchCriteria);

                //model.sisIds = model.SearchResult.Select(r => r.formResultId).ToList();
                //model.formIds = model.SearchResult.Select(r => r.formId).Distinct().ToList();

                //addDictionaries(model);
                // the addDictionaries code below except for the Export 
                model.Groups = GetGroupsDictionary();
                model.Enterprises = GetEnterprisesDictionary();

                model.userNames = GetUserNamesDictionary();

                model.Forms = Business.Forms.GetFormsDictionary(formsRepo);

                model.AllForms = GetAllFormsDictionary();

                model.reviewStatuses = Business.ReviewStatus.GetReviewStatuses(formsRepo);

                model.usedForms = GetAllFormsDictionary();

                Session["SearchModel"] = model;

                return View("Index", model);
            }
        }


        private IQueryable<vSearchGrid> DetailSearch(SearchCriteria searchCriteria)
        {
            IQueryable<vSearchGrid> tmpResult = null;
            var context = DataContext.getSisDbContext();
            bool previousSearchResult = false;
            bool previousRecSearchResult = false;
            bool debug = false; // set to true to display record counts after each parameter is added.

            List<long> RecipientIDs = new List<long>();
            List<int> SISIds = new List<int>();
            List<string> TrackList = null;

            //
            if (!string.IsNullOrWhiteSpace(searchCriteria.sisIds))
            {
                List<string> tempSisIdlist = new List<string>(searchCriteria.sisIds.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
                foreach (var tempSis in tempSisIdlist)
                {
                    int tempSisInt = 0;
                    if (Int32.TryParse(tempSis, out tempSisInt))
                        SISIds.Add(tempSisInt);
                }

                if (SISIds.Count > 0)
                {
                    previousSearchResult = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(searchCriteria.recIds))
            {
                List<string> tempRecIdList = new List<string>(searchCriteria.recIds.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
                foreach (var tempRecId in tempRecIdList)
                {
                    int tempRecIDInt = 0;
                    if (Int32.TryParse(tempRecId, out tempRecIDInt))
                        RecipientIDs.Add(tempRecIDInt);
                }

                if (RecipientIDs.Count > 0)
                {
                    previousRecSearchResult = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(searchCriteria.trackingNumber))
            {
                TrackList = new List<string>(searchCriteria.trackingNumber.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
                //// List<int> tempList = new List<int>();

                //List<int> tempList = (from a in context.vSisDefRaws
                //                      where
                //                      (
                //                        (a.rspValue != null && a.identifier == "sis_track_num")
                //                        &&
                //                        (tempTrackList.Contains(a.rspValue) && a.identifier == "sis_track_num")
                //                      )
                //                      select a.formResultId).ToList();

                //// if previous search result returned a result, only get the result that also exist in this result.
                //// this will simulate "AND" search for all search criteria
                //if (previousSearchResult)
                //{
                //    SISIds = (from a in SISIds
                //              join b in tempList on a equals b
                //              select a).ToList();
                //}
                //else
                //{
                //    SISIds.AddRange(tempList);
                //}

                //if (tempList.Count > 0)
                //    previousSearchResult = true;
            }

            bool isFirstNameNull = string.IsNullOrWhiteSpace(searchCriteria.FirstName);
            bool isLastNameNull = string.IsNullOrWhiteSpace(searchCriteria.LastName);

            if (!isFirstNameNull || !isLastNameNull)
            {
                //Recipient Search
                // List<long> tempRecList = new List<long>();
                List<long> tempRecList = (from r in context.Recipients
                                          where (
                                                   (isFirstNameNull || (r.Contact.FirstName.Contains(searchCriteria.FirstName)))
                                                   && (isLastNameNull || (r.Contact.LastName.Contains(searchCriteria.LastName)))
                                                )
                                          select r.Recipient_ContactID).ToList();

                if (previousRecSearchResult)
                {
                    RecipientIDs = (from a in RecipientIDs
                                    join b in tempRecList on a equals b
                                    select a).ToList();
                }
                else
                {
                    RecipientIDs.AddRange(tempRecList);
                }

                if (tempRecList.Count > 0)
                {
                    previousRecSearchResult = true;
                }

                // SIS Search
                List<int> tempList = new List<int>();
                if (!isLastNameNull)
                {
                    tempList.AddRange((from a in context.vSisDefRaws
                                       where
                                       (
                                         (a.rspValue != null && a.identifier == "sis_cl_last_nm")
                                         &&
                                         (a.rspValue.Contains(searchCriteria.LastName) && a.identifier == "sis_cl_last_nm")
                                       )
                                       select a.formResultId).ToList());
                }

                if (!isFirstNameNull)
                {
                    tempList.AddRange((from a in context.vSisDefRaws
                                       where
                                       (
                                         (a.rspValue != null && a.identifier == "sis_cl_first_nm")
                                         &&
                                         (a.rspValue.Contains(searchCriteria.FirstName) && a.identifier == "sis_cl_first_nm")
                                       )
                                       select a.formResultId).ToList());
                }

                if (!isFirstNameNull && !isLastNameNull)
                {
                    tempList = (tempList.GroupBy(x => x).Where(group => group.Count() > 1).Select(group => group.Key)).ToList();
                }

                // if previous search result returned a result, only get the result that also exist in this result.
                // this will simulate "AND" search for all search criteria
                if (previousSearchResult)
                {
                    SISIds = (from a in SISIds
                              join b in tempList on a equals b
                              select a).ToList();
                }
                // Next line was causing search for first and last name not to work.
                else
                    SISIds.AddRange(tempList);

                if (tempList.Count > 0)
                    previousSearchResult = true;
            }

            bool getAllAssessmentsForEachRecipient = false;
            if (searchCriteria.allForEachRecipient)
            {
                getAllAssessmentsForEachRecipient = true;
            }

            // *** Required filters ***
            // Set up the query.
            if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0] != 0)
            {
                // Remove any Groups which are not authorized.
                List<int> authGroups = SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups;

                // Check that the sub_id has a value, then compare it to the authGroups, where the authGroup is > -1.
                tmpResult = context.vSearchGrids.AsNoTracking().Where(srch => srch.sub_id.HasValue && authGroups.Any(ag => ag > -1 && ag == srch.sub_id));
                if (debug) 
                    Debug.WriteLine("DetailSearch Initial Count after authgroups: " + tmpResult.Count());
            }
            else
            {
                tmpResult = context.vSearchGrids.AsNoTracking();
                if (debug)
                    Debug.WriteLine("DetailSearch Initial record Count: " + tmpResult.Count());
            }

            // Filter by Enterprise, unless Enterprise == 0.
            tmpResult = tmpResult.Where(srch => srch.ent_id == SessionHelper.LoginStatus.EnterpriseID || SessionHelper.LoginStatus.EnterpriseID == 0);
            if (debug)
                Debug.WriteLine("DetailSearch limit by Enterprise record Count: " + tmpResult.Count());

            if (!UAS_Business_Functions.hasPermission(UAS.Business.PermissionConstants.GROUP_WIDE, UAS.Business.PermissionConstants.ASSMNTS))
            {
                // Filter by Assigned Only
                tmpResult = tmpResult.Where(srch => srch.assigned == SessionHelper.LoginStatus.UserID);
            }
            if (debug)
                Debug.WriteLine("DetailSearch Assign Only record Count: " + tmpResult.Count());
            //*** End Required Filters

            // If statement ensures 0 results will be returned when no records are found.
            if (!String.IsNullOrWhiteSpace(searchCriteria.sisIds) || !String.IsNullOrWhiteSpace(searchCriteria.recIds)
                || !String.IsNullOrWhiteSpace(searchCriteria.FirstName) || !String.IsNullOrWhiteSpace(searchCriteria.LastName))
            {
                tmpResult = tmpResult.Where(srch => SISIds.Any(sis => sis == srch.formResultId) || RecipientIDs.Any(recip => recip == srch.Recipient_ContactID));
            }
            if (debug)
                Debug.WriteLine("DetailSearch SISIds and RecipientIds record Count: " + tmpResult.Count());

            // Filter by Tracking number
            if (TrackList != null && TrackList.Count() > 0)
            {
                tmpResult = tmpResult.Where(srch => TrackList.Any(trk => trk.Equals(srch.TrackingNumber)));
            }
            if (debug)
                Debug.WriteLine("DetailSearch Tracking Number record Count: " + tmpResult.Count());

            // Filter by Interviewers
            if (searchCriteria.SelectedInterviewers != null && searchCriteria.SelectedInterviewers.Count() > 0) 
            {
                tmpResult = tmpResult.Where(srch => searchCriteria.SelectedInterviewers.Contains(srch.InterviewerLoginID));
            }
            if (debug)
                Debug.WriteLine("DetailSearch Selected Interviewers record Count: " + tmpResult.Count());

            // Filter by Group
            if (searchCriteria.SelectedGroups != null && searchCriteria.SelectedGroups.Count() > 0)
            {
                tmpResult = tmpResult.Where(srch => searchCriteria.SelectedGroups.Any(sg => srch.sub_id == sg));
            }
            if (debug)
                Debug.WriteLine("DetailSearch Selected Group record Count: " + tmpResult.Count());

            // Filter by Selected Enterprises, unless the SelectedEnts list is empty or null.
            if (searchCriteria.SelectedEnts != null && searchCriteria.SelectedEnts.Count() > 0)
            {
                tmpResult = tmpResult.Where(srch => searchCriteria.SelectedEnts.Any(se => srch.ent_id == se));
            }
            if (debug)
                Debug.WriteLine("DetailSearch Selected Enterprise record Count: " + tmpResult.Count());

            // Filter by patterns
            if (searchCriteria.SelectedPatterns != null && searchCriteria.SelectedPatterns.Any())
            {
                tmpResult = from t in tmpResult
                    join p in context.PatternChecks on t.formResultId equals p.formResultId
                    where searchCriteria.SelectedPatterns.Contains(p.PatternId)
                    select t;
            } else if (searchCriteria.PatternCheck)
            {
                tmpResult = from t in tmpResult
                            join p in context.PatternChecks on t.formResultId equals p.formResultId
                            select t;
            }

            if (debug)
                Debug.WriteLine("DetailSearch Selected Pattern record Count: " + tmpResult.Count());

            // Search by Date Updated
            if (searchCriteria.updatedDateFrom != null || searchCriteria.updatedDateTo != null)
            {
                if (searchCriteria.updatedDateFrom != null)
                {
                    DateTime dt = Convert.ToDateTime(searchCriteria.updatedDateFrom);
                    tmpResult = from t in tmpResult
                                join fr in context.def_FormResults on t.formResultId equals fr.formResultId
                                where fr.dateUpdated >= dt
                                select t;
                }

                if (searchCriteria.updatedDateTo != null)
                {
                    DateTime dt = Convert.ToDateTime(searchCriteria.updatedDateTo);
                    tmpResult = from t in tmpResult
                                join fr in context.def_FormResults on t.formResultId equals fr.formResultId
                                where fr.dateUpdated <= dt
                                select t;
                }

                if (debug)
                    Debug.WriteLine("DetailSearch Updated Date From/To record Count: " + tmpResult.Count());
            }

            // Search by Interview Date Range
            if (searchCriteria.interviewDateFrom != null) 
            {
                DateTime dt = Convert.ToDateTime(searchCriteria.interviewDateFrom);
                tmpResult = tmpResult.Where(srch => srch.InterviewDate >= dt);
            } 
            if (searchCriteria.interviewDateTo != null)
            {
                DateTime dt = Convert.ToDateTime(searchCriteria.interviewDateTo);
                tmpResult = tmpResult.Where(srch => srch.InterviewDate <= dt);
            }
            if (debug)
                Debug.WriteLine("DetailSearch Interview Date Range record Count: " + tmpResult.Count());

            // Check Level of Completion checkboxes, include records where appropriate.  Text is hardcoded into a database view, inaccessible through Assessments.
            tmpResult = tmpResult.Where(srch => (searchCriteria.statusNew && srch.Status.Equals("New"))
                    || (searchCriteria.statusInProgress && srch.Status.Equals("In Progress"))
                    || (searchCriteria.statusCompleted && srch.Status.Equals("Completed"))
                    || (searchCriteria.statusAbandoned && srch.Status.Equals("Abandoned")));

            if (debug)
                Debug.WriteLine("DetailSearch Count after Level of Completion: " + tmpResult.Count());

            // Check QA Review Status.  Include every result when nothing is checked, except Pre-QA.
            tmpResult = tmpResult.Where(srch => (!(searchCriteria.reviewStatusToBeReviewed || searchCriteria.reviewStatusPreQA || searchCriteria.reviewStatusReviewed || searchCriteria.reviewStatusApproved) && !srch.reviewStatus.Equals(ReviewStatus.PRE_QA.ToString()))
                                || (searchCriteria.reviewStatusToBeReviewed && srch.reviewStatus.Equals(ReviewStatus.TO_BE_REVIEWED.ToString()))
                                || (searchCriteria.reviewStatusPreQA && srch.reviewStatus.Equals(ReviewStatus.PRE_QA.ToString()) && searchCriteria.statusCompleted)
                                || (searchCriteria.reviewStatusReviewed && srch.reviewStatus.Equals(ReviewStatus.REVIEWED.ToString()))
                                || (searchCriteria.reviewStatusApproved && srch.reviewStatus.Equals(ReviewStatus.APPROVED.ToString())));
            if (debug)
                Debug.WriteLine("DetailSearch Count after QA Review: " + tmpResult.Count());

            // Check typically hidden records.  Hide deleted or archived unless those boxes are checked.
            tmpResult = tmpResult.Where(srch => (!searchCriteria.deleted && !searchCriteria.archived && ((!srch.deleted || srch.deleted == null) && (!srch.archived || srch.archived == null)))
                || (searchCriteria.deleted && srch.deleted)
                || (searchCriteria.archived && srch.archived)
                || (searchCriteria.deleted && searchCriteria.archived && (srch.deleted || srch.archived)));
            if (debug)
                Debug.WriteLine("DetailSearch Count after Hidden Records: " + tmpResult.Count());

            tmpResult = tmpResult.GroupBy(x => x.formResultId).Select(g => g.FirstOrDefault());

            if (debug)
                Debug.WriteLine("DetailSearch Count after Group By: " + tmpResult.Count());

            if (!searchCriteria.adultSis || !searchCriteria.childSis)
            {
                // 3-22-16 MR fixed logic to accurately search for adults or child assessments
                if (!searchCriteria.adultSis)
                {
                    tmpResult = tmpResult.Where(x => (x.formId != 1) && (x.formId != null));
                }

                if (!searchCriteria.childSis)
                {
                    tmpResult = tmpResult.Where(x => (x.formId != 2) && (x.formId != null));
                }
            }
            if (debug)
                Debug.WriteLine("DetailSearch Count after SIS-A/SIS-C filter: " + tmpResult.Count());


            return tmpResult.OrderByDescending(a => a.Rank);
        }
    }
}