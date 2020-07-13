// *** RRB 2/6/16 - needs to be refactored into a generic logging system.
// *** SIS Specific code needs to go into SIS domain.
// *** DEF specifc code (where UAS links might be needed) can go into DEF domain.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Data.Entity;

using Assmnts;

using Data.Abstract;

namespace Data.Concrete
{
    public partial class FormsRepository : IFormsRepository
    {

        // Access Logging
        public def_AccessLogging GetAccessLoggingById(int accessLoggingId)
        {
            return db.def_AccessLogging.Where(a => a.accessLoggingId == accessLoggingId).FirstOrDefault();
        }

        public int AddAccessLogging(def_AccessLogging accessLogging)
        {
            db.def_AccessLogging.Add(accessLogging);
            db.SaveChanges();

            return accessLogging.accessLoggingId;
        }


        public void AddMultipleAccessLoggings(List<def_AccessLogging> accessLoggings)
        {
            db.def_AccessLogging.AddRange(accessLoggings);
            db.SaveChanges();
        }


        public List<def_AccessLogging> GetAllAccessLogsByEnterpriseAndGroups(int entId, List<int> groupIds)
        {
            var query = from log in db.def_AccessLogging
                        join formResult in db.def_FormResults on log.formResultId equals formResult.formResultId
                        where (groupIds.Contains(0) || groupIds.Contains(formResult.GroupID.Value)) &&
                              log.EnterpriseID == entId
                        select log;
            return query.ToList();
        }

        public Dictionary<int, int> GetFormResultIdsFromAccessLogs(List<def_AccessLogging> logs)
        {
            var query = from log in logs
                        join formResult in db.def_FormResults on log.formResultId equals formResult.formResultId
                        select new { logId = log.accessLoggingId, formResId = formResult.formResultId };

            Dictionary<int, int> dict = new Dictionary<int, int>();
            foreach (var q in query)
            {
                dict.Add(q.logId, q.formResId);
            }

            return dict;
        }

        // *** RRB 2/6/16 - refactor to SIS Domain
        // ***            Not really sure what this is doing.
        // ***     Is the 'sis' parameter the formResultID ??  If so, should be passed as an 'int'
        public List<def_AccessLogging> GetFilteredAccessLogs(out int count, int page, int numRecords, int entId, List<int> groupIds, string startDate, string endDate, string userName, string sis)
        {
            DateTime start, end;

            if (!DateTime.TryParse(startDate, out start))
            {
                start = DateTime.MinValue;
            }

            if (!DateTime.TryParse(endDate, out end))
            {
                end = DateTime.MaxValue.AddDays(-1);
            }

            int sisId;
            if (!Int32.TryParse(sis, out sisId))
            {
                sisId = -1;
            }

            end = end.AddDays(1);
            using (var context = DataContext.getSisDbContext())
            {

                using (var context2 = DataContext.getUasDbContext())
                {
                    List<int> userIds = context2.uas_User.Where(u => u.UserName.Contains(userName)).Select(u => u.UserID).ToList();

                    var query = from log in db.def_AccessLogging
                                join formResult in db.def_FormResults on log.formResultId equals formResult.formResultId
                                where (groupIds.Contains(0) || groupIds.Contains(formResult.GroupID.Value)) &&
                                      userIds.Contains(log.UserID) &&
                                      (log.EnterpriseID == entId || entId == 0) &&
                                      start < log.datetimeAccessed &&
                                      (sisId == -1 || sisId == log.formResultId) &&
                                      end > log.datetimeAccessed
                                orderby log.datetimeAccessed descending
                                select log;


                    count = query.Count();

                    var pagedQuery = query.Skip((page - 1) * numRecords).Take(numRecords);

                    List<def_AccessLogging> logs = pagedQuery.ToList();

                    return logs;
                }
            }
        }

        // Access Log Functions

        public List<def_AccessLogFunctions> GetAccessLogFunctions()
        {
            return db.def_AccessLogFunctions.Select(f => f).ToList();
        }

        public def_AccessLogFunctions GetAccessLogFunctionsById(int accessLogFunctionsId)
        {
            return db.def_AccessLogFunctions.Where(a => a.accessLogFunctionId == accessLogFunctionsId).FirstOrDefault();
        }

        public int AddAccessLogFunctions(def_AccessLogFunctions accessLogFunctions)
        {
            db.def_AccessLogFunctions.Add(accessLogFunctions);

            db.SaveChanges();

            return accessLogFunctions.accessLogFunctionId;
        }

        // *** RRB 2/6/16 - refactor to SIS Domain - contains SIS Specific logic
        // ***           this is not generic logging
        // ***           Generic logging should take the FormResult subject and use it as UserId for UAS.
        // ***     If Jim would like to use it this way, it should be a separate SIS Controller
        public Dictionary<int, string> GetRecipientNamesFromFormResultIds(Dictionary<int, int> logFormResult)
        {
            Dictionary<int, string> namesDict = new Dictionary<int, string>();

            Dictionary<int, string> firstNames = (from rv in db.def_ResponseVariables
                                                  join ir in db.def_ItemResults on rv.itemResultId equals ir.itemResultId
                                                  join iv in db.def_ItemVariables on rv.itemVariableId equals iv.itemVariableId
                                                  where (logFormResult.Values.Distinct().Contains(ir.formResultId) && iv.identifier.Equals("sis_cl_first_nm"))
                                                  select new { ir.formResultId, rv.rspValue }).ToDictionary(x => x.formResultId, x => x.rspValue);

            Dictionary<int, string> lastNames = (from rv in db.def_ResponseVariables
                                                 join ir in db.def_ItemResults on rv.itemResultId equals ir.itemResultId
                                                 join iv in db.def_ItemVariables on rv.itemVariableId equals iv.itemVariableId
                                                 where (logFormResult.Values.Distinct().Contains(ir.formResultId) && iv.identifier.Equals("sis_cl_last_nm"))
                                                 select new { ir.formResultId, rv.rspValue }).ToDictionary(x => x.formResultId, x => x.rspValue);

            logFormResult.ToList().ForEach(x => namesDict.Add(x.Key, ((lastNames != null && lastNames.Keys != null && lastNames.Keys.Contains(x.Value)) ? lastNames[x.Value] : "") + ", "
                + ((firstNames != null && firstNames.Keys != null && firstNames.Keys.Contains(x.Value) ? firstNames[x.Value] : ""))));

            return namesDict;
        }

    }

}