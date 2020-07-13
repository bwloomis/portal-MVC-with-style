using Assmnts.Infrastructure;
using Assmnts.UasServiceRef;
using Data.Abstract;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using UAS.Business;

namespace Assmnts.Business
{
    public static class ReviewStatus
    {
        public const string RELATED_FORM_RESULT_ID = "related_form_result_id";
        
        public const int STATUS_MASTER_ID = 3;

        public const int TO_BE_REVIEWED = 0;
        public const int PRE_QA = 1;
        public const int REVIEWED = 2;
        public const int APPROVED = 3;
        public const int CHECKED_OUT = 4;
        public const int CHECKED_IN = 5;
        public const int BLANK = 255;

        ///// <summary>
        ///// Gets the review status based on the status code provided (the sort order)
        ///// </summary>
        ///// <param name="statusCode">Codes determined by the def_StatusDetail table</param>
        ///// <returns>A string for display with the appropriate status info</returns>
        //public static string GetReviewStatus(int statusCode, IFormsRepository formsRepo)
        //{
        //    def_StatusText statusText = formsRepo.GetStatusTextByDetailSortOrder(STATUS_MASTER_ID, statusCode);

        //    if (statusText != null)
        //    {
        //        return statusText.displayText;
        //    }

        //    return String.Empty;
        //}

        public static Dictionary<int?, string> GetReviewStatuses(IFormsRepository formsRepo)
        {
            Dictionary<int?, string> reviewStatuses = null;
            try
            {

                reviewStatuses = formsRepo.GetStatusDisplayTextsByStatusMasterId(STATUS_MASTER_ID);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
            return reviewStatuses;
        }

        public static void LogStatusChange(IFormsRepository formsRepo, int formResultId, def_StatusDetail from, def_StatusDetail to, string note)
        {
            def_StatusLog statusChangeLog = new def_StatusLog();

            statusChangeLog.formResultId = formResultId;
            if (from != null)
            {
                statusChangeLog.statusDetailIdFrom = from.statusDetailId;
            }
            if (to != null)
            {
                statusChangeLog.statusDetailIdTo = to.statusDetailId;
            }
            statusChangeLog.statusLogDate = DateTime.Now;
            statusChangeLog.UserID = SessionHelper.LoginStatus.UserID;
            statusChangeLog.statusNote = note;
            try
            {
                formsRepo.AddStatusLog(statusChangeLog);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding status log: " + ex.Message);
            }
        }

        internal static void ChangeStatus(IFormsRepository formsRepo, def_FormResults formResult, int newStatus, string note = "")
        {
            def_StatusDetail statusFrom = formsRepo.GetStatusDetailBySortOrder(STATUS_MASTER_ID, formResult.reviewStatus);

            def_StatusDetail statusTo = formsRepo.GetStatusDetailBySortOrder(STATUS_MASTER_ID, newStatus);

            if (statusTo != null)
            {
                formResult.reviewStatus = (byte)statusTo.sortOrder.Value;
            }
            else
            {
                formResult.reviewStatus = (byte)BLANK;
            }
           
            LogStatusChange(formsRepo, formResult.formResultId, statusFrom, statusTo, note);
            
        }

        internal static void AssessmentIsCompleted(IFormsRepository formsRepo, def_FormResults fr)
        {
            //* * * OT 3-15-16 if an assessment has the
            // Reviewed or Approved QA Status, then the QA Review status will not be reset
            // upon Completion. (Bug 13110)
            if (fr.reviewStatus == ReviewStatus.REVIEWED || fr.reviewStatus == ReviewStatus.APPROVED)
                return;

            if (ReviewAll())
            {
                ChangeStatus(formsRepo, fr, ReviewStatus.TO_BE_REVIEWED, "Assessment completed and ready for review.");
            }
            else
            {
                ChangeStatus(formsRepo, fr, ReviewStatus.BLANK, "Assessment completed.");
            }
        }

        internal static bool ReviewAll()
        {
            string reviewAll = SessionHelper.Read<string>("Review_All");

            if (reviewAll != null)
            {
                if (reviewAll.ToLower().Equals("true")) {
                    return true;
                }
                return false;
            }
            
            EntAppConfig reviewAllConfig = UAS_Business_Functions.GetEntAppConfig("REVIEW_ALL", SessionHelper.LoginStatus.EnterpriseID);

            if (reviewAllConfig != null && reviewAllConfig.ConfigValue.ToLower() != "false")
            {
                SessionHelper.Write("Review_All", "true");                
                return true;
            }
            SessionHelper.Write("Review_All", "false");     
            return false;
        }

        internal static def_FormResults GetLatestPreQACopy(IFormsRepository formsRepo, int formResultId)
        {
            def_ResponseVariables responseVariable = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, RELATED_FORM_RESULT_ID);

            if (responseVariable != null)
            {
                int preQAformResultId = Convert.ToInt32(responseVariable.rspValue);

                def_FormResults preQAformResult = formsRepo.GetFormResultById(preQAformResultId);

                if (preQAformResult.reviewStatus == ReviewStatus.PRE_QA)
                {
                    return preQAformResult;
                }
            }

            return null;
        }

        private static void GetReviewStatus(int formResultId)
        {
            throw new NotImplementedException();
        }
    }
}