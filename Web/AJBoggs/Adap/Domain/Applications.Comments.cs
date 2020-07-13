using Assmnts;

using Data.Abstract;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


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

        public string GetFullCommentsReport(int formResultId, bool includeStyling, string newLine = "\n", string tab = "\t")
        {
            string result;

            if (includeStyling)
            {
                result = "<div class='bs-callout bs-callout-primary' style='background-color:lightyellow'><h4 style='color:black;'>Comments on application</h4><br/><div>";
                result += GetApplicationComments(formResultId, newLine, tab);
                result += "</div></div>";

                result += "<div class='bs-callout bs-callout-primary' style='background-color:lightyellow'><h4 style='color:black;'>Status change comments</h4><br/><div>";
                result += GetAllStatusComments(formResultId, newLine, tab);
                result += "</div></div>";
            }
            else
            {
                result = "Comments on application:" + newLine;
                result += GetApplicationComments(formResultId, newLine, tab);

                result += newLine + newLine + "Status change comments:" + newLine;
                result += GetAllStatusComments(formResultId, newLine, tab);
            }

            return result;
        }

        private string GetAllStatusComments(int formResultId, string newLine, string tab)
        {
            string result = "";

            def_FormResults fr = formsRepo.GetFormResultById(formResultId);
            def_StatusMaster statusMaster = formsRepo.GetStatusMasterByFormId(fr.formId);
            List<def_StatusDetail> statusDetails = formsRepo.GetStatusDetails(statusMaster.statusMasterId);
            List<def_StatusText> statusTexts = statusDetails.Select( sd => formsRepo.GetStatusTextByDetailSortOrder( statusMaster.statusMasterId, sd.sortOrder.Value ) ).ToList();
            IQueryable<def_StatusLog> statusLogs = formsRepo.GetStatusLogsForFormResultId(fr.formResultId).OrderByDescending(sl => sl.statusLogDate);

            foreach (def_StatusLog sl in statusLogs)
            {
                def_StatusDetail sdFrom = statusDetails.Where(sd => sd.statusDetailId == sl.statusDetailIdFrom).FirstOrDefault();
                def_StatusDetail sdTo   = statusDetails.Where(sd => sd.statusDetailId == sl.statusDetailIdTo  ).FirstOrDefault();
                if (sdFrom == null)
                    throw new Exception("could not find def_StatusDetail for statusDetailIdFrom " + sl.statusDetailIdFrom + " in def_StatusLog " + sl.statusLogId);
                if (sdTo == null)
                    throw new Exception("could not find def_StatusDetail for statusDetailIdTo " + sl.statusDetailIdTo + " in def_StatusLog " + sl.statusLogId);
                
                def_StatusText stFrom = statusTexts.Where( st => st.statusDetailId == sdFrom.statusDetailId ).FirstOrDefault();
                def_StatusText stTo = statusTexts.Where( st => st.statusDetailId == sdTo.statusDetailId ).FirstOrDefault();
                if( stFrom == null )
                    throw new Exception("could not find def_StatusText for statusDetailId " + sdFrom.statusDetailId );
                if( stTo == null )
                    throw new Exception("could not find def_StatusText for statusDetailId " + sdTo.statusDetailId );

                result += "<span style='color:Red'>" + sl.statusLogDate + "</span> - " + stFrom.displayText + " -> " + stTo.displayText + newLine;
                result += tab + sl.statusNote + newLine + "<hr>";
            }
            //statusLog.statusDetailIdFrom = formsRepo.GetStatusDetailBySortOrder(statusMasterId, oldStatus).statusDetailId;
            //statusLog.statusDetailIdTo = formsRepo.GetStatusDetailBySortOrder(statusMasterId, status).statusDetailId;
            //statusLog.formResultId = result.formResultId;
            //statusLog.UserID = SessionHelper.LoginStatus.UserID;
            //statusLog.statusLogDate = DateTime.Now;

            return result;
        }

        /// <summary>
        /// Gets all the Comments in a an application.
        /// There is a Comment ResponseVariable for each screen.
        /// </summary>
        /// <param name="formResultId"></param>
        /// <param name="lineFeed">string used to create a break between comments.</param>
        /// <returns></returns>
        private string GetApplicationComments(int formResultId, string newLine, string tab )
        {
            string result = String.Empty;
            try
            {
                Debug.WriteLine("GetApplicationComments formResultId: " + formResultId.ToString());

                string ivIdentifier = "ADAP_Application_Comments_txt";

                def_ResponseVariables rvCmmt = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, ivIdentifier);
                if (rvCmmt != null)
                {
                    string cmmt = rvCmmt.rspValue;
                    if (!String.IsNullOrEmpty(cmmt))
                        result = cmmt;
                }

                result = result.Replace("\n", newLine);
                result = result.Replace("\t", tab);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Applications.GetApplicationComments Exception: " + ex.Message);
                result = "Exception: " + ex.Message;
            }

            return result;
        }
    }
}
