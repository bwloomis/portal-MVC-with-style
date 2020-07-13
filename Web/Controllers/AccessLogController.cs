using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Abstract;
using Data.Concrete;


namespace Assmnts.Controllers
{
    [RedirectingAction]
    public class AccessLogController : Controller
    {

        private readonly IFormsRepository formsRepo;

        public AccessLogController(IFormsRepository fr)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
        }

        // GET: AccessLog
        public ActionResult Index()
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account");
            }

            Debug.WriteLine("AccessLogController Enterprise:" + SessionHelper.LoginStatus.EnterpriseID.ToString());
            AccessLog accessLogModel = new AccessLog();

            //try
            //{
            //    accessLogModel.accessLogs = formsRepo.GetAllAccessLogsByEnterpriseAndGroups(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.ToList());

            //    using (var context = DataContext.getUasDbContext())
            //    {
            //        accessLogModel.enterpriseDict.Add(SessionHelper.LoginStatus.EnterpriseID, context.uas_Enterprise.Where(e => e.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID).Select(e => e.EnterpriseName).FirstOrDefault().ToString());

            //        foreach (uas_User user in context.uas_User.Where(u => u.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID).Select(u => u).ToList())
            //        {
            //            accessLogModel.usersDict.Add(user.UserID, user.UserName);
            //        }
            //    }

            //}
            //catch (Exception xcptn)
            //{
            //    Debug.WriteLine("AccessLogController exception:" + xcptn.Message);
            //}

            //foreach(def_AccessLogFunctions function in formsRepo.GetAccessLogFunctions()) {
            //    accessLogModel.functionDict.Add(function.accessLogFunctionId, function.accessLogFunctionName);
            //}
            
            //Dictionary<int, int> logFormResult = formsRepo.GetFormResultIdsFromAccessLogs(accessLogModel.accessLogs);

            //foreach(KeyValuePair<int, int> k in logFormResult) {
            //    string first = String.Empty;
            //    string last = String.Empty;
            //    def_ResponseVariables rvFirst = formsRepo.GetResponseVariablesByFormResultIdentifier(k.Value, "sis_cl_first_nm");
            //    if (rvFirst != null)
            //        first = rvFirst.rspValue;
            //    def_ResponseVariables rvLast = formsRepo.GetResponseVariablesByFormResultIdentifier(k.Value, "sis_cl_last_nm");
            //    if (rvLast != null)
            //        last = rvLast.rspValue;

            //    accessLogModel.recipNameDict.Add(k.Key, last + ", " + first);
            //}
            
            return View("Index", accessLogModel);
        }

        [HttpPost]
        public ActionResult Filter(int page, int numRecords, string startDate, string endDate, string sUser, string sis)
        {
            AccessLog accessLogModel = new AccessLog();
            
            int count = 0;

            if (SessionHelper.LoginStatus.EnterpriseID == 0 && SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.ToList().Contains(0))
            {
                accessLogModel.accessLogs = formsRepo.GetFilteredAccessLogs(out count, page, numRecords, 0, SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.ToList(), startDate, endDate, sUser, sis);

                List<int> entIds = accessLogModel.accessLogs.Select(a => a.EnterpriseID).ToList();
                List<int> userIds = accessLogModel.accessLogs.Select(a => a.UserID).ToList();
                
                using (var context = DataContext.getUasDbContext())
                {
                    accessLogModel.enterpriseDict = (context.uas_Enterprise.Where(e => entIds.Contains(e.EnterpriseID)).Select(e => new { e.EnterpriseID, e.EnterpriseName })).ToDictionary(e => e.EnterpriseID, e => e.EnterpriseName);

                    foreach (uas_User user in context.uas_User.Where(u => userIds.Contains(u.UserID)).Select(u => u))
                    {
                        accessLogModel.usersDict.Add(user.UserID, user.UserName);
                    }
                }
            }
            else
            {
                accessLogModel.accessLogs = formsRepo.GetFilteredAccessLogs(out count, page, numRecords, SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.ToList(), startDate, endDate, sUser, sis);

                List<int> userIds = accessLogModel.accessLogs.Select(a => a.UserID).ToList();

                using (var context = DataContext.getUasDbContext())
                {
                    accessLogModel.enterpriseDict.Add(SessionHelper.LoginStatus.EnterpriseID, context.uas_Enterprise.Where(e => e.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID).Select(e => e.EnterpriseName).FirstOrDefault().ToString());

                    foreach (uas_User user in context.uas_User.Where(u => u.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && userIds.Contains(u.UserID)).Select(u => u))
                    {
                        accessLogModel.usersDict.Add(user.UserID, user.UserName);
                    }
                }
            }

            foreach (def_AccessLogFunctions function in formsRepo.GetAccessLogFunctions())
            {
                accessLogModel.functionDict.Add(function.accessLogFunctionId, function.accessLogFunctionName);
            }

            Dictionary<int, int> logFormResult = formsRepo.GetFormResultIdsFromAccessLogs(accessLogModel.accessLogs);

            accessLogModel.recipNameDict = formsRepo.GetRecipientNamesFromFormResultIds(logFormResult);

            accessLogModel.count = count;
            
            return PartialView("_Results", accessLogModel);
        }
    
    }
}