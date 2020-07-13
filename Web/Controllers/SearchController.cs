using System;
using System.Configuration;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Abstract;
using Data.Concrete;

using UAS.DataDTO;
using UAS.Business;


namespace Assmnts.Controllers
{
    [RedirectingAction]
    public partial class SearchController : Controller
    {
        private readonly IFormsRepository formsRepo;

        private bool ventureMode;
        
        public SearchController(IFormsRepository fr)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;

            ventureMode = SessionHelper.IsVentureMode;
        }

        [HttpGet]
        public ActionResult Index()
        {

            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account");
            }

            LoginStatus loginStatus = SessionHelper.LoginStatus;
            Debug.WriteLine("* * *  SearchController:Index  LoginStatus  EnterpriseID: " + loginStatus.EnterpriseID.ToString() +
                            "   Name: " + loginStatus.FirstName + " " + loginStatus.LastName
            );

            Debug.WriteLine("* * *  SearchController:Index  LoginStatus.group" + loginStatus.appGroupPermissions.Count.ToString());
            foreach (AppGroupPermissions agp in loginStatus.appGroupPermissions)
            {
                Debug.WriteLine("   LoginStatus GroupPermissionSets  GroupID: " +
                    ((agp.groupPermissionSets.Count > 0) ? agp.groupPermissionSets[0].GroupID.ToString() + "   " + agp.groupPermissionSets[0].PermissionSet : "N/A")
                );
            }

            // Limit Search to Enterprise
            int? srchEnterpriseID = null;
            if (loginStatus.EnterpriseID != 0)      // Enterprise 0 = System Administrator - view all Assessments
            {
                srchEnterpriseID = loginStatus.EnterpriseID;
            }
            Debug.WriteLine("* * *  SearchController:Index  srchEnterpriseID: " + srchEnterpriseID.ToString());

            SearchModel model = new SearchModel();

            // var watch = Stopwatch.StartNew();

            /*
            if (ventureMode == true)
            {
                return Venture(model);
            }
            */

            try
            {
                string errMsg = string.Empty;
                try
                {
                    model.vSearchResult = createSISSearchQuery();
                    if (ventureMode == true)
                    {
                        model.vSearchResultEnumerable = model.vSearchResult.AsEnumerable();
                    }

                    // watch.Stop();
                    // var elapsedMs = watch.ElapsedMilliseconds;
                    // Debug.WriteLine("Search idx elapsed ms: " + elapsedMs);
                    /*
                    if (model.vSearchResult != null)
                    {
                        Debug.WriteLine("Search Index SearchResult count: " + model.SearchResult.Count().ToString());
                    }
                    */
                }
                catch (TimeoutException te)
                {
                    errMsg = te.Message;
                    if (te.InnerException != null && te.InnerException.Message != null)
                    {
                        errMsg = errMsg + " * Inner Exception: " + te.InnerException.Message;
                        Debug.WriteLine("Search Index TimeoutException: " + errMsg);
                        return DisplayError("Search Index TimeoutException: " + errMsg, "Index");
                    }
                }
                catch (DuplicateWaitObjectException doe)
                {
                    errMsg = doe.Message;
                    if (doe.InnerException != null && doe.InnerException.Message != null)
                    {
                        errMsg = errMsg + " * Inner Exception: " + doe.InnerException.Message;
                        Debug.WriteLine("Search Index DuplicateWaitObjectException: " + errMsg);
                        return DisplayError("Search Index DuplicateWaitObjectException: " + errMsg, "Index");
                    }

                }
                catch (InsufficientMemoryException ime)
                {
                    errMsg = ime.Message;
                    if (ime.InnerException != null && ime.InnerException.Message != null)
                    {
                        errMsg = errMsg + " * Inner Exception: " + ime.InnerException.Message;
                        Debug.WriteLine("Search Index InsufficientMemoryException: " + errMsg);
                        return DisplayError("Search Index InsufficientMemoryException: " + errMsg, "Index");
                    }

                }
                catch (Exception ex)
                {
                    errMsg = ex.Message;
                    if (ex.InnerException != null && ex.InnerException.Message != null)
                    {
                        errMsg = errMsg + " * Inner Exception: " + ex.InnerException.Message;
                        Debug.WriteLine("Search Index Exception: " + errMsg);
                        return DisplayError("Search Index Exception: " + errMsg, "Index");
                    }
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                if (ex.InnerException != null && ex.InnerException.Message != null)
                    errMsg = errMsg + " * Inner Exception: " + ex.InnerException.Message;
                Debug.WriteLine("Search Index exception: " + errMsg);
                return DisplayError("Search Index exception: " + errMsg, "Index");
            }

            //BR 3/24 This should be investigated to determine if it's necessary, or if there's another way.  Calling ToList is slowing down this query.
            // model.sisIds = model.SearchResult.Select(r => r.formResultId).ToList();
            // model.formIds = model.SearchResult.Select(r => r.formId).Distinct().ToList();

            // addDictionaries(model);
            //addUsedForms(model);
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

        /// <summary>
        /// Creates the initial IQueryable object.  Contains required, default filters for an initial search.
        /// </summary>
        /// <returns></returns>
        private IQueryable<vSearchGrid> createSISSearchQuery()
        {
            var context = DataContext.getSisDbContext();
            IQueryable<vSearchGrid> query = context.vSearchGrids.AsNoTracking().OrderByDescending(a => a.Rank);
            //Hide archived, deleted and Pre-QA assessments.
            query = query.Where(q => (q.deleted == false || q.deleted == null) && (q.archived == false || q.archived == null) && (!q.reviewStatus.Equals("1")));

            // Add filters for Enterprise and Groups.
            query = filterSearchResultsByEntIdAuthGroups(query);

            return query;
        }

        private IQueryable<vSearchGrid> filterSearchResultsByEntIdAuthGroups(IQueryable<vSearchGrid> query)
        {
            // Filter by Enterprise, unless Enterprise == 0.
            query = query.Where(srch => srch.ent_id == SessionHelper.LoginStatus.EnterpriseID || SessionHelper.LoginStatus.EnterpriseID == 0);

            if (!UAS_Business_Functions.hasPermission(UAS.Business.PermissionConstants.GROUP_WIDE, UAS.Business.PermissionConstants.ASSMNTS))
            {
                // Filter by Assigned Only
                query = query.Where(srch => srch.assigned == SessionHelper.LoginStatus.UserID);
            }

            if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0] == 0)
            {
                return query;
            }
            List<int> authGroups = SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups;

            // Remove any Groups which are not authorized.
            // Check that the sub_id has a value, then compare it to the authGroups, where the authGroup is > -1.
            query = query.Where(srch => srch.sub_id.HasValue && authGroups.Any(ag => ag > -1 && ag == srch.sub_id));

            return query;
        }

        private void addUsedForms(SearchModel model)
        {
            ExportModel exportModel = new ExportModel();

            exportModel.formIds = model.formIds;
            exportModel.formResultIds = model.sisIds;

            exportModel.formNames = new Dictionary<int, string>();

            foreach (int? nFormId in exportModel.formIds)
            {
                if (nFormId != null)
                {
                    int formId = (int)nFormId;
                    def_Forms form = formsRepo.GetFormById(formId);
                    if (form != null)
                    {
                        string name = form.identifier;
                        exportModel.formNames.Add(formId, name);
                    }
                }
            }

            Session["ExportModel"] = exportModel;
            
            model.usedForms = exportModel.formNames;
            if (model.usedForms.Count() > 1)
            {
                model.usedForms.Add(-1, "All");
            }
        }

        //public void buildExportCSVCaller(SearchModel model)
        //{
        //    //// Determine if general or detail search was done
        //    //if (Session["GeneralSearchString"] != null )
        //    //{
        //    //    // Redo the search
        //    //    IEnumerable<spSearchGrid_Result> results = GeneralSearch(Session["GeneralSearchString"].ToString());

        //    //    // Extract SIS ids (formResultIds)
        //    //     sisIds = results.Select(r => r.formResultId).ToList();
        //    //     formIds = results.Select(r => r.formId).Distinct().ToList();                                
        //    //}
        //    //else if (Session["DetailSearchCriteria"] != null)
        //    //{
        //    //   // Redo the search
        //    //    IEnumerable<spSearchGrid_Result> results = DetailSearch((SearchCriteria)Session["DetailSearchCriteria"]);

        //    //    // Extract SIS ids (formResulsIds)
        //    //    sisIds = results.Select(r => r.formResultId).ToList();
        //    //    formIds = results.Select(r => r.formId).Distinct().ToList();
        //    //}
        //    //else
        //    //{
        //    //    using (var context = DataContext.getSisDbContext())
        //    //    {
        //    //        int? srchEnterpriseID = null;
        //    //        if (SessionHelper.LoginStatus.EnterpriseID != 0)      // Enterprise 0 = System Administrator - view all Assessments
        //    //        {
        //    //            srchEnterpriseID = SessionHelper.LoginStatus.EnterpriseID;
        //    //        }

        //    //        IEnumerable<spSearchGrid_Result> results = filterSearchResultsByAuthGroups(
        //    //            context.spSearchGrid(true, false, false, string.Empty, srchEnterpriseID, null, string.Empty, string.Empty, string.Empty).ToList());

        //    //        sisIds = results.Select(r => r.formResultId).ToList();
        //    //        formIds = results.Select(r => r.formId).Distinct().ToList();
        //    //    }
        //    //}


        //    ExportModel exportModel = new ExportModel();
            
        //    exportModel.formIds = model.formIds;
        //    exportModel.formResultIds = model.sisIds;

        //    exportModel.formNames = new Dictionary<int, string>();
            
        //    foreach (int? nFormId in exportModel.formIds)
        //    {
        //        if (nFormId != null)
        //        {
        //            int formId = (int)nFormId;
        //            def_Forms form = formsRepo.GetFormById(formId);
        //            if (form != null)
        //            {
        //                string name = form.identifier;
        //                exportModel.formNames.Add(formId, name);
        //            }
        //        }
        //    }
            
        //    Session["ExportModel"] = exportModel;
            
        //}

        public ActionResult exportCSVCaller()
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account");
            }
            
     //       buildExportCSVCaller();
            // Call batchCsvOptions in export controller with list of SIS ids
            return RedirectToAction("BatchCSVOptions", "Export");
        }

        // *** RRB 2/15/16 - I don't understand why this is necessary.
        //         3/7/16  - Updated
        //                   The queries should filter by EnterpriseID if User is not in 0 (Admin).
        //                   If the User is in 0, there is no need to filter.
        //                   Maybe if the Search allows User with 0 to select a specific Enterprise
        private List<spSearchGrid_Result> filterSearchResultsByEnterprise(List<spSearchGrid_Result> results)
        {
            int resultsCount = results.Count();
            Debug.WriteLine("* * *  SearchController:filterSearchResultsByEnterprise  resultsCount: " + resultsCount.ToString());

            int?[] idsToRemove = new int?[resultsCount];

            int i = 0;
            foreach (spSearchGrid_Result r in results)
            {
                if (r.ent_id.HasValue && (r.ent_id == SessionHelper.LoginStatus.EnterpriseID || SessionHelper.LoginStatus.EnterpriseID == 0))
                {
                    continue;
                }
                else
                {
                    idsToRemove[i] = r.formResultId;
                    i++;
                }
             
            }

            foreach (int? id in idsToRemove)
            {
                if (id != null)
                {
                    results.Remove(results.Single(r => r.formResultId == id));
                }
                else
                {
                    break;
                }
            }

            return results;
            
        }
        
        private List<spSearchGrid_Result> filterSearchResultsByAuthGroups(List<spSearchGrid_Result> results)
        {

            results = filterSearchResultsByEnterprise(results);

            if (!UAS_Business_Functions.hasPermission(UAS.Business.PermissionConstants.GROUP_WIDE, UAS.Business.PermissionConstants.ASSMNTS))
            {
                results = filterResultsByAssignOnly(results);
            }
            
            if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0] == 0)
            {
                return results;
            }
            List<int> authGroups = SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups;
                   
            int?[] idsToRemove = new int?[results.Count()];

            int i = 0;
            foreach (spSearchGrid_Result r in results)
            {
                if ((r != null) && r.formResultId.HasValue)
                {
                    //def_FormResults fr = formsRepo.GetFormResultById(r.formResultId.Value);
                    if ((r != null) && r.sub_id.HasValue && UAS_Business_Functions.isGroupAuthorized(authGroups, r.sub_id.Value))
                    {
                        continue;
                    }
                    else
                    {
                        idsToRemove[i] = r.formResultId;
                        i++;
                    }
                                  
                }
            }

            foreach (int? id in idsToRemove)
            {
                if (id != null)
                {
                    results.Remove(results.Single(r => r.formResultId == id));
                }
                else
                {
                    break;
                }
            }

            return results;
        }

        private IQueryable<spSearchGrid_Result> filterSearchResultsByAuthGroups(IQueryable<spSearchGrid_Result> query)
        {
            // Filter by Enterprise, unless Enterprise == 0.
            query = query.Where(srch => srch.ent_id == SessionHelper.LoginStatus.EnterpriseID || SessionHelper.LoginStatus.EnterpriseID == 0);

            if (!UAS_Business_Functions.hasPermission(UAS.Business.PermissionConstants.GROUP_WIDE, UAS.Business.PermissionConstants.ASSMNTS))
            {
                // Filter by Assigned Only
                query = query.Where(srch => srch.assigned == SessionHelper.LoginStatus.UserID);
            }

            if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0] == 0)
            {
                return query;
            }
            List<int> authGroups = SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups;

            // Remove any Groups which are not authorized.
            // Check that the sub_id has a value, then compare it to the authGroups, where the authGroup is > -1.
            query = query.Where(srch => srch.sub_id.HasValue && authGroups.Any(ag => ag > -1 && ag == srch.sub_id));

            return query;
        }

        private List<spSearchGrid_Result> filterResultsByAssignOnly(List<spSearchGrid_Result> results)
        {
            int?[] idsToRemove = new int?[results.Count()];

            int i = 0;
            foreach (spSearchGrid_Result r in results)
            {
                if ((r != null) && r.formResultId.HasValue)
                {
                    if (r.assigned == SessionHelper.LoginStatus.UserID)
                    {
                        continue;
                    }
                    else
                    {
                        idsToRemove[i] = r.formResultId;
                        i++;
                    }

                }
            }

            foreach (int? id in idsToRemove)
            {
                if (id != null)
                {
                    results.Remove(results.Single(r => r.formResultId == id));
                }
                else
                {
                    break;
                }
            }
            return results;
        }

        [HttpGet]
        public ActionResult ShowAllForRec()
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account");
            }

            SearchModel model = new SearchModel();
            model.ShowAllAssessmentsForRecipient = true;
            using (var context = DataContext.getSisDbContext())
            {
                int? srchEnterpriseID = null;
                
                // Comment out below to remove filter by enterprise
                if (SessionHelper.LoginStatus.EnterpriseID != 0)      // Enterprise 0 = System Administrator - view all Assessments
                {
                    srchEnterpriseID = SessionHelper.LoginStatus.EnterpriseID;
                }
                
                // model.SearchResult = filterSearchResultsByAuthGroups(
                   // context.spSearchGrid(true, true, false, string.Empty, srchEnterpriseID, null, string.Empty, string.Empty, string.Empty).ToList());
            }

            addDictionaries(model);

            return View("Index", model);
        }

        [HttpGet]
        public ActionResult ViewReport(int formResultId, int formid)
        {
            if (SessionHelper.SessionForm == null)
            {
                SessionHelper.SessionForm = new SessionForm();
            }
            SessionForm sf = SessionHelper.SessionForm;
            sf.formId = formid;
            sf.formResultId = formResultId;

            def_Forms form = formsRepo.GetFormById(formid);
            sf.formIdentifier = form.identifier;

            Session["part"] = (int)6;
            return RedirectToAction("Template", "Results", new { sectionId = 38 });
        }


        private void addDictionaries(SearchModel model) {
            model.Groups = GetGroupsDictionary();
            model.Enterprises = GetEnterprisesDictionary();

            model.userNames = GetUserNamesDictionary();

            model.Forms = Business.Forms.GetFormsDictionary(formsRepo);

            model.AllForms = GetAllFormsDictionary();

            model.reviewStatuses = Business.ReviewStatus.GetReviewStatuses(formsRepo);

            addUsedForms(model);
        }

        private Dictionary<int, string> GetUserNamesDictionary()
        {
            using (var context = DataContext.getUasDbContext())
            {
                Dictionary<int, string> userNames;
                List<uas_User> users = null;
                if (SessionHelper.LoginStatus.EnterpriseID == 0) // Site wide admin
                {
                    users = context.uas_User.Select(u => u).ToList();
                }
                else if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Contains(0)) // Enterprise admin
                {
                    users = context.uas_User.Where(u => u.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID).Select(u => u).ToList();
                }
                else // Normal user
                {
                    var authorizedGroups = SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups;

                    List<int> userIdsForUserGroup = context.uas_GroupUserAppPermissions.Where(g =>
                        authorizedGroups.Contains(g.uas_Group.GroupID)).Select(g => g.UserID).ToList();
                   
                    users = context.uas_User.Where(u => u.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID
                        && userIdsForUserGroup.Contains(u.UserID)).Select(u => u).ToList();
                }
                userNames = users.ToDictionary(u => u.UserID, u => u.UserName);

                return userNames;
            }
    }

        private Dictionary<int, string> GetGroupsDictionary()
        {
            using (var context = DataContext.getUasDbContext())
            {
                Dictionary<int, string> groups;
                if (SessionHelper.LoginStatus.EnterpriseID != 0)
                {
                    groups = (from i in context.uas_Group
                              where i.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID
                              select i).ToDictionary(i => i.GroupID, i => i.GroupName);
                }
                else
                {
                    groups = (from i in context.uas_Group
                              select i).ToDictionary(i => i.GroupID, i => i.GroupName);
                }
                return groups;
            }
        }

        private Dictionary<int, string> GetEnterprisesDictionary()
        {
            using (var context = DataContext.getUasDbContext())
            {
                Dictionary<int, string> enterprises;
                if (SessionHelper.LoginStatus.EnterpriseID != 0)
                {
                    enterprises = (from i in context.uas_Enterprise
                                   where i.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID
                                   select i).ToDictionary(i => i.EnterpriseID, i => i.EnterpriseName);
                }
                else
                {
                    enterprises = (from i in context.uas_Enterprise
                                   select i).ToDictionary(i => i.EnterpriseID, i => i.EnterpriseName);
                }
                return enterprises;
            }
        }

        private Dictionary<int, string> GetAllFormsDictionary()
        {
            Dictionary<int, string> forms = (from i in formsRepo.GetAllForms()
                                             select i).ToDictionary(i => i.formId, i => i.identifier);
            return forms;
        }

        public ActionResult DisplayError( string message, string actionName )
        {
            ViewBag.ErrorMessage = message;
            HandleErrorInfo model = new HandleErrorInfo(new Exception(message), "Search", actionName);
            return View("Error", model );
        }
        
        [HttpGet]
        public void SetNewFormId(int formId)
        {
            SessionHelper.Write("newFormId", formId);
        }
        
        // This is used by the timeout script as an ajax callback; doesn't do anything but prevents js error. -LK 5/12/15 #12554
        [HttpPost]
        public void keepAlive()
        {

        }
    }
}