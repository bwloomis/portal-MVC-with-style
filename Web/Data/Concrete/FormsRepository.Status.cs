using Assmnts;

using Data.Abstract;

using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading;


namespace Data.Concrete
{
    public partial class FormsRepository : IFormsRepository
    {
        // StatusMaster methods

        public List<def_StatusMaster> GetStatusMasters()
        {
            return db.def_StatusMaster.ToList();
        }

        public def_StatusMaster GetStatusMasterById(int statusMasterId)
        {
            return db.def_StatusMaster.Where(m => m.statusMasterId == statusMasterId).FirstOrDefault();
        }

        public def_StatusMaster GetStatusMasterByFormId(int formId)
        {
            return db.def_StatusMaster.Where(m => m.formId == formId).AsNoTracking().FirstOrDefault();
        }

        public void AddStatusMaster(def_StatusMaster statusMaster)
        {
            db.def_StatusMaster.Add(statusMaster);

            db.SaveChanges();
        }

        public void SaveStatusMaster(def_StatusMaster statusMaster)
        {
            db.Entry(statusMaster).State = EntityState.Modified;

            db.SaveChanges();
        }

        public void DeleteStatusMaster(def_StatusMaster statusMaster)
        {
            db.def_StatusMaster.Remove(statusMaster);

            db.SaveChanges();
        }

        // StatusDetails methods

        public List<def_StatusDetail> GetStatusDetails(int statusMasterId)
        {
            return db.def_StatusDetail.Where(d => d.statusMasterId == statusMasterId).OrderBy(d => d.sortOrder).ToList();
        }
        
        public def_StatusDetail GetStatusDetailById(int statusDetailId)
        {
            return db.def_StatusDetail.Where(d => d.statusDetailId == statusDetailId).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the StatusDetail by StatusDetailMaster Id and StatusDetail identifier
        /// </summary>
        /// <param name="statusMasterId"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public def_StatusDetail GetStatusDetailByMasterIdentifier(int statusMasterId, string identifier)
        {
            def_StatusDetail statusDetail = db.def_StatusDetail.Where(d => (d.statusMasterId == statusMasterId) && (d.identifier == identifier) ).FirstOrDefault();
            if (statusDetail == null)
            {
                // Most queries only using sortOrder, populated some fields to prevent exceptions
                statusDetail = new def_StatusDetail()
                {
                    identifier = string.Empty,
                    sortOrder = 0,
                    Active = "I"
                };
            }
            return statusDetail;
        }

        public def_StatusDetail GetStatusDetailBySortOrder(int statusMasterId, int sortOrder)
        {
            return db.def_StatusDetail.Where(d => d.statusMasterId == statusMasterId && d.sortOrder == sortOrder).FirstOrDefault();
        }

        public def_StatusDetail GetStatusDetailByDisplayText(int statusMasterId, string displayText)
        {
            return db.def_StatusDetail.Where(d => d.def_StatusText.Where(s => s.displayText.Equals(displayText)).Select(s => s.statusDetailId).FirstOrDefault() == d.statusDetailId
                && d.statusMasterId == statusMasterId).FirstOrDefault();
        }

        public IQueryable<def_StatusDetail> GetStatusDetailsForPending()
        {
            return db.def_StatusDetail.Where(d => d.identifier.Equals("IN_PROCESS") || d.identifier.Equals("NEEDS REVIEW") || d.identifier.Equals("NEEDS INFORMATION"));
        }

        public void AddStatusDetail(def_StatusDetail statusDetail)
        {
            db.def_StatusDetail.Add(statusDetail);

            db.SaveChanges();
        }

        public void SaveStatusDetail(def_StatusDetail statusDetail)
        {
            db.Entry(statusDetail).State = EntityState.Modified;

            db.SaveChanges();
        }

        public void DeleteStatusDetail(def_StatusDetail statusDetail)
        {
            db.def_StatusDetail.Remove(statusDetail);

            db.SaveChanges();
        }


        // StatusText methods

        public List<def_StatusText> GetStatusTexts(int statusDetailId, int EnterpriseID, int langId)
        {
            return db.def_StatusText.Where(t => t.statusDetailId == statusDetailId && t.EnterpriseID == EnterpriseID && t.langId == langId).ToList();
        }

        public def_StatusText GetStatusText(int statusDetailId, int EnterpriseID, int langId)
        {
            int countEnt = db.def_StatusText.Where(subT => subT.EnterpriseID == EnterpriseID).Count();
            return db.def_StatusText.Where(t => t.statusDetailId == statusDetailId
                && ((countEnt > 0 && t.EnterpriseID == EnterpriseID) || (countEnt == 0 && t.EnterpriseID == 0)) 
                && t.langId == langId).FirstOrDefault();
        }

        public def_StatusText GetStatusTextById(int statusTextId)
        {
            return db.def_StatusText.Where(t => t.statusTextId == statusTextId).FirstOrDefault();
        }
        
        public def_StatusText GetStatusTextByDetailSortOrder(int statusMasterId, int sortOrder)
        {
            CultureInfo ci = Thread.CurrentThread.CurrentUICulture;
            string region = (ci == null) ? "en" : ci.TwoLetterISOLanguageName.ToLower();

            return (from st in db.def_StatusText
                    join sd in db.def_StatusDetail on st.statusDetailId equals sd.statusDetailId
                    join lg in db.def_Languages on st.langId equals lg.langId
                    where sd.statusMasterId == statusMasterId && lg.languageRegion == region && sd.sortOrder == sortOrder
                    select st).FirstOrDefault();

            //return db.def_StatusDetail.Where(d => d.statusMasterId == statusMasterId && d.sortOrder == sortOrder).FirstOrDefault().def_StatusText.FirstOrDefault();
        }

        public Dictionary<int?, string> GetStatusDisplayTextsByStatusMasterId(int statusMasterId)
        {
            // Get all details for status master
            List<def_StatusDetail> details = db.def_StatusDetail.Where(d => d.statusMasterId == statusMasterId).ToList();

            List<int> detailIds = details.Select(d => d.statusDetailId).ToList();

            // Get all texts for these details
            List<def_StatusText> texts = db.def_StatusText.Where(t => (detailIds.Contains(t.statusDetailId.Value))).ToList();

            // Make dictionary of <sort order, display text> to help with displaying statuses
            Dictionary<int?, string> statuses = details.ToDictionary(d => d.sortOrder, d => texts.Where(t => t.statusDetailId == d.statusDetailId).FirstOrDefault().displayText);

            return statuses;
        }
        
        public void AddStatusText(def_StatusText statusText)
        {
            db.def_StatusText.Add(statusText);

            db.SaveChanges();
        }

        public void SaveStatusText(def_StatusText statusText)
        {
            db.Entry(statusText).State = EntityState.Modified;

            db.SaveChanges();
        }

        public void DeleteStatusText(def_StatusText statusText)
        {
            db.def_StatusText.Remove(statusText);

            db.SaveChanges();
        }

        public def_StatusLog GetStatusLogById(int statusLogId)
        {
            return db.def_StatusLog.Where(l => l.statusLogId == statusLogId).FirstOrDefault();
        }

        public IQueryable<def_StatusLog> GetStatusLogsForFormResultId(int formResultId)
        {
            return db.def_StatusLog.Where(l => l.formResultId == formResultId).OrderByDescending(l => l.statusLogDate);
        }

        public def_StatusLog GetMostRecentStatusLogByStatusDetailToFormResultIdAndUserId(int statusDetailId, int formResultId, int userId)
        {
            return db.def_StatusLog.Where(l => l.statusDetailIdTo == statusDetailId && l.formResultId == formResultId && l.UserID == userId).OrderByDescending(l => l.statusLogDate).FirstOrDefault();
        }

        public int AddStatusLog(def_StatusLog statusLog)
        {
            db.def_StatusLog.Add(statusLog);
            db.SaveChanges();
            return statusLog.statusLogId;
        }

        public void SaveStatusLog(def_StatusLog statusLog)
        {
            db.Entry(statusLog).State = EntityState.Modified;

            db.SaveChanges();
        }

        public void DeleteStatusLog(def_StatusLog statusLog)
        {
            db.def_StatusLog.Remove(statusLog);

            db.SaveChanges();
        }

        // Status Flow (Workflow)

        public def_StatusFlow GetStatusFlowById(int statusFlowId)
        {
            return db.def_StatusFlow.Where(f => f.statusFlowId == statusFlowId).FirstOrDefault();
        }

        public void AddStatusFlow(def_StatusFlow statusFlow)
        {
            db.def_StatusFlow.Add(statusFlow);

            db.SaveChanges();
        }

        public void SaveStatusFlow(def_StatusFlow statusFlow)
        {
            db.Entry(statusFlow).State = EntityState.Modified;

            db.SaveChanges();
        }

        public void DeleteStatusFlow(def_StatusFlow statusFlow)
        {
            db.def_StatusFlow.Remove(statusFlow);

            db.SaveChanges();
        }


        public List<def_StatusFlow> GetStatusFlowByDetailEntRole(int statusDetailId, int EnterpriseID, int rolePermission)
        {
            return db.def_StatusFlow.Where(sf => (sf.statusDetailId == statusDetailId) && (sf.EnterpriseID == EnterpriseID) && (sf.rolePermission == rolePermission)).AsNoTracking().ToList();
        }
    }

}
