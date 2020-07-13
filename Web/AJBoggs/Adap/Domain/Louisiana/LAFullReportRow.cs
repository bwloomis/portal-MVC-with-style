using Assmnts;
using Assmnts.UasServiceRef;
using Data.Abstract;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using UAS.Business;


namespace AJBoggs.Adap.Domain
{
    public class LAFullReportRow : IReportRow
    {

        private readonly IFormsRepository formsRepo;
        private readonly IEnumerable<vFormResultUser> allVfrus;

        public readonly string formIdentifier;
        public readonly vFormResultUser vfru;
        public readonly DateTime? statusChangeDate;
        public readonly bool isHistorical;
        public readonly def_FileAttachment historicalSnapshot;

        #region FullReportRow Constructor, with all members above as params
        public LAFullReportRow(
            IFormsRepository formsRepo,
            IEnumerable<vFormResultUser> allVfrus,
            string formIdentifier,
            vFormResultUser vfru,
            DateTime? statusChangeDate,
            bool isHistorical = false,
            def_FileAttachment historicalSnapshot = null)
        {
            this.formsRepo = formsRepo;
            this.allVfrus = allVfrus;
            this.formIdentifier = formIdentifier;
            this.vfru = vfru;
            this.statusChangeDate = statusChangeDate;
            this.isHistorical = isHistorical;
            this.historicalSnapshot = historicalSnapshot;
        }
        #endregion

        public string GetHtmlForColumn(int columnIndex)
        {
            switch (columnIndex)
            {
                case 0: //ramsellId / MemberId
                    def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(vfru.formResultId, "C1_MemberIdentifier") ?? formsRepo.GetResponseVariablesByFormResultIdentifier(vfru.formResultId, "ADAP_D9_Ramsell");
                    string ramsellId = (rv == null) ? null : rv.rspValue;
                    string ramsellIdDisplayText;
                    if ((ramsellId == null) || String.IsNullOrWhiteSpace(ramsellId))
                    {
                        if (vfru.EnterpriseID == 8)
                        {
                            ramsellIdDisplayText = @Resources.AdapPortal.NoId;
                        }
                        else
                        {
                            ramsellIdDisplayText = @Resources.AdapPortal.NoRamsellId;
                        }
                    }
                    else
                        ramsellIdDisplayText = ramsellId;
                    if (UAS_Business_Functions.hasPermission(PermissionConstants.EDIT, PermissionConstants.ASSMNTS))
                    {
                        return isHistorical ? ramsellIdDisplayText : "<a href=\"/ADAP/ToTemplate?formResultId=" + Convert.ToString(vfru.formResultId) + "&Update=Y\">" + ramsellIdDisplayText + "</a>";
                    }
                    else
                    {
                        return ramsellIdDisplayText;
                    }

                case 1: //FirstName
                    return vfru.FirstName;

                case 2: //Last name
                    return vfru.LastName;

                case 3: //DOB
                    return vfru.DOB.HasValue ? vfru.DOB.Value.ToString("MM/dd/yyyy") : String.Empty;

                case 4: //Status
                    def_StatusMaster statusMaster = formsRepo.GetStatusMasterByFormId(vfru.formId);
                    int statusMasterId = statusMaster == null ? 1 : statusMaster.statusMasterId;

                    string statusDisplayText = formsRepo.GetStatusTextByDetailSortOrder(statusMasterId, vfru.formStatus).displayText;
                    return isHistorical ? statusDisplayText : "<a href=\"/ADAP/UpdateStatus?formResultId=" + Convert.ToString(vfru.formResultId) + "\">" + statusDisplayText + "</a>";
                
                case 5: //Assigned
                    AuthenticationClient auth = new AuthenticationClient();
                    var user = auth.GetUser(vfru.assigned.Value);
                    string linkText = "CSS People";
                    if (vfru.subject.Value != vfru.assigned.Value)
                    {
                        linkText = user.FirstName + " " + user.LastName;
                    }

                    if (UAS.Business.UAS_Business_Functions.hasPermission(1, "RptsExpts"))
                    {
                        return
                             "<div class=\"row\">" +
                                     "<div class=\"col-md-12 text-center\">" +
                                         "<a href=\"#\" data-toggle=\"modal\" data-target=\"#CssPeoplepickerModal\" data-formresultid=\"" + Convert.ToString(vfru.formResultId) + "\">" +
                                            linkText +
                                     "</div>" +
                             "</div>";
                    }
                    else
                    {
                        if (vfru.subject.Value == vfru.assigned.Value) 
                        {
                            return "CSS People";
                        }
                        else
                        {
                            return user.FirstName + " " + user.LastName;
                        }
                    };

                case 6: //Status changed
                    return statusChangeDate == null ? String.Empty : statusChangeDate.ToString();

case 7: //Eligibility end date

                    
                    DateTime? eligibilityEndDate = vfru.EligibilityEndDate;
                    string endDate = "None Pending";
                    if (eligibilityEndDate.HasValue)
                    {
                        endDate = eligibilityEndDate.Value.ToShortDateString();
                    }
                    return endDate;

                case 8: //Next Recert
                   DateTime? elgEndDate = vfru.EligibilityEndDate;
                    string recertDatestr = "None Pending";
                    if (elgEndDate!=null && elgEndDate.HasValue)
                    {
                        recertDatestr = elgEndDate.Value.AddDays(-42).ToShortDateString();
                    }
                    return recertDatestr;


                case 9: //type
                    var form = formsRepo.GetFormByIdentifier(formIdentifier).title;
                    return "<div class=\"form-type-text\" data-formIdentifier=\"" + formIdentifier + "\">" + form + "</div>";

                case 10: //group
                    if (vfru.EnterpriseID == 8)
                    {
                        string groupDesc = string.Empty;
                        var context = DataContext.getUasDbContext();
                        var group = (from g in context.uas_Group
                                     where g.GroupID == vfru.GroupID
                                     select g).FirstOrDefault();
                        groupDesc = group.GroupDescription;
                        return isHistorical ? vfru.GroupName + " - " + groupDesc : "<a href=\"/ADAP/UpdateTeam?formId=" + Convert.ToString(vfru.formResultId) + "\">" + vfru.GroupName + " - " + groupDesc + "</a>";
                    }
                    else
                    {
                        return isHistorical ? vfru.GroupName : "<a href=\"/ADAP/UpdateTeam?formId=" + Convert.ToString(vfru.formResultId) + "\">" + vfru.GroupName + "</a>";
                    }
                case 11: //contact info
                    return
                        "<div class=\"row\">" +
                                "<div class=\"col-md-12 text-center\">" +
                                    "<a href=\"#\" data-toggle=\"modal\" data-target=\"#contactsModal\" data-formresultid=\"" + Convert.ToString(vfru.formResultId) + "\">" +
                                        "Contact" +
                                    "</a>" +
                                    "<span class=\"text-divider\">|</span>" +
                                    "<a href=\"#\" data-toggle=\"modal\" data-target=\"#cmmtModal\" data-userid=\"" + vfru.subject.ToString() + "\" data-formresultid=\"" + Convert.ToString(vfru.formResultId) + "\">" +
                                        "Comments" +
                                    "</a>" +
                                    "<span class=\"text-divider\">|</span>" +
                                    "<a href=\"/ADAP/StatusHistory?formResultId=" + Convert.ToString(vfru.formResultId) + "\">" +
                                        "History" +
                                    "</a>" +
                                "</div>" +
                        "</div>";

                case 12: //print button
                    if (isHistorical)
                    {
                        if (historicalSnapshot != null)
                        {
                            return "<a class=\"glyphicon glyphicon-print text-primary\" href=\"/Search/DownloadFile?fileId=" + historicalSnapshot.FileId + "&fileDownloadName=snapshot.pdf\"></a>";
                        }
                        else
                        {
                            return "N/A";
                        }
                    }
                    else
                    {
                        return "<a class=\"glyphicon glyphicon-print text-primary\" href=\"../COADAP/BuildPDFReport?formResultId=" + Convert.ToString(vfru.formResultId) + "\"></a>";
                    }

                case 13: //subject (user id number for applicant)
                    return Convert.ToString(vfru.subject);

                case 14: //upload and download attachments
                    if (isHistorical)
                    {
                        return String.Empty;
                    }
                    else
                    {
                        int frId = vfru.formResultId;
                        return "<div>"
                            + "<div id=\"attachment1" + frId + "\">"
                                + "<form class=\"uploadForm\" action=\"/Search/UploadFile\" method=\"post\" enctype=\"multipart/form-data\">"
                                + "<input type=\"hidden\" name=\"formResultId\" value=\"" + frId + "\" />"
                                + "<input type=\"file\" id=\"file" + frId + "\" name=\"file" + frId + "\">"
                                + "<a href=\"#\" onclick=\"$('#Upload" + frId + "').click()\">Upload</a>"
                                + "&nbsp;&nbsp;&nbsp;"
                                + "<a href=\"#\" class=\"viewFiles\" id=\"view" + frId + "\" onclick=\"downloadAttach(" + frId + ")\" hidden=\"hidden\">View Files</a>"
                                + "&nbsp;&nbsp;&nbsp;"
                                + "<input type=\"submit\" id=\"Upload" + frId + "\" value=\"Upload\" hidden=\"hidden\" />"
                                + "</form>"
                            + "</div>"
                            + "<div id=\"attachment2" + frId + "\" hidden=\"hidden\">"
                                + "<span id=\"dwnldDDL" + frId + "\" class=\"AttachDDL" + frId + "\" style=\"min-width:10px\"></span> "
                                + "<a href=\"#\" onclick=\"downloadFile(" + frId + ")\">Download</a>"
                                + "&nbsp;&nbsp;&nbsp;"
                                + "<a href=\"#\" onclick=\"deleteAttach(" + frId + ")\">Delete Files</a>"
                                + "&nbsp;&nbsp;&nbsp;"
                                + "<a href=\"#\" onclick=\"cancelAttach(" + frId + ")\">Cancel</a>"
                            + "</div>"
                            + "<div id=\"attachment3" + frId + "\" hidden=\"hidden\">"
                                + "<span id=\"dltDDL" + frId + "\" class=\"AttachDDL" + frId + "\"></span> "
                                + "<a href=\"#\" onclick=\"deleteFile(" + frId + ")\">Delete</a>"
                                + "&nbsp;&nbsp;&nbsp;"
                                + "<a href=\"#\" onclick=\"downloadAttach(" + frId + ")\">Cancel</a>"
                            + "</div>"
                        + "</div>";
                    }
   

                default:
                    throw new Exception("invalid column index: " + columnIndex);
            }
        }

        /// <summary>
        /// Used in GetRecert(), to determine which applications are approved
        /// 
        /// Will be set to a non-null value the first time it is accessed.
        /// </summary>
        private int? sortOrderForApprovedStatusDetail = null;

        /// <summary>
        /// Determines the next schedualed recertification date for a given applicant.
        /// This method shouldn't be called when the count of approved applications is 0, returns 1/1/0001 if it is.
        /// 
        /// Base Rule: END of birth month, END of birth month +6.
        ///
        /// Upon first approval, if base rule results in less than 3 months, advance to the next 6 month date.  E.g. birth month March, 
        /// initially approved July.  Instead of recert due Sept 30, due next March 31.  Exception applies for initial approval.
        /// After that, even if a recert is late, it doesn't delay the next recert due from the Base Rule.
        /// </summary>
        /// 
        /// <param name="formResultUser">a formResultUser view record for the fiven applicant</param>
        /// 
        /// <param name="query">the full set of formResultUsers to consider in computing the recertification date. 
        /// Any records that aren't approved or that have a "subject" that does not match the given applicant will be ignored.</param>
        /// 
        /// <returns>DateTime of the next Recertification date, defaults to null if calculation does not apply.</returns>
        private DateTime? GetRecert(IFormsRepository formsRepo, vFormResultUser formResultUser, IEnumerable<vFormResultUser> query)
        {
            if (!sortOrderForApprovedStatusDetail.HasValue)
            {
                def_StatusDetail sd = formsRepo.GetStatusDetailByMasterIdentifier(1, "APPROVED");
                sortOrderForApprovedStatusDetail = Convert.ToInt32(sd.sortOrder);
            }
            // Get applications for the applicant so Recertification can be determined.
            IEnumerable<vFormResultUser> apprRcrd = query.Where(fr => fr.formStatus == sortOrderForApprovedStatusDetail && fr.subject == formResultUser.subject).OrderByDescending(fr => fr.statusChangeDate);
            int acceptCount = apprRcrd.Count();
            if (acceptCount == 0)
                return null;

            DateTime recentRecert = Convert.ToDateTime(apprRcrd.Select(fr => fr.statusChangeDate).FirstOrDefault());

            return new Applications(formsRepo).GetRecert(formResultUser.DOB, acceptCount, recentRecert);
        }

        public IComparable GetSortingValueForColumn(int columnIndex)
        {
            switch (columnIndex)
            {
                case 0: //RamsellId / MemberId (no longer supported - bad performance)
                    return 0;// ramsellIdDisplayText;

                case 1: //firstName
                    return vfru.FirstName;

                case 2: //lastName
                    return vfru.LastName;

                case 3: //DOB
                    return vfru.DOB;

                case 4: //status
                    return vfru.formStatus;

                case 5://assigned
                    return vfru.assigned;

                case 6: //status change date
                    return vfru.statusChangeDate;

                case 7: //next recert (no longer supported - bad performance)
                    return 0; //nextRecertDate;

                case 8: //formId
                    return vfru.formId;

                case 9: //group
                    return vfru.GroupName;

                default: //10 and 11 (contact info and print button) not supported
                    throw new Exception("unsupported sorting column index: " + columnIndex);
            }
        }
    }

}
