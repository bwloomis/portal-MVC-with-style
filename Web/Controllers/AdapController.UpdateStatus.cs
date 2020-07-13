using AJBoggs.Adap.Domain;
using AJBoggs.Adap.Services.Api;
using Assmnts.Business.Uploads;
using Assmnts.Business.Workflow;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Assmnts.Reports;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Assmnts.Controllers
{
    public partial class AdapController : Controller
    {
        /// <summary>
        /// Loads the page to update the Status for a given application.
        /// </summary>
        /// <returns>View to update the status for the selected application.</returns>
        [HttpGet]
        public ActionResult UpdateStatus()
        {
            string formResultId = Request["formResultId"];

            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;
            int accessLevel = (UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts")) ? 1 : 0;
            // check here for managerial status.
            if ((accessLevel == 1) && UAS.Business.UAS_Business_Functions.hasPermission(1, "RptsExpts"))
            {
                accessLevel = 2;
            }

            try
            {
                int frmResId = Convert.ToInt32(formResultId);
                def_FormResults result = formsRepo.GetFormResultById(frmResId);
                def_StatusMaster statusMaster = formsRepo.GetStatusMasterByFormId(result.formId);
                int statusMasterId = statusMaster.statusMasterId;

                //populate the model with the current formResultId, and a list of possible "new statuses"
                aar1.FormId = result.formId;
                aar1.formResult = frmResId;
                aar1.StatusDDL = AdapApprovals.PossibleWorkflow(formsRepo, statusMasterId, result.formStatus, accessLevel);

                //add the current status as an option to the model
                def_StatusText currentStatusText = formsRepo.GetStatusTextByDetailSortOrder(statusMasterId, result.formStatus);
                aar1.StatusDDL.Add(result.formStatus, currentStatusText.displayText + " (Current Status)");

                def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "C1_MemberIdentifier") ?? formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D9_Ramsell");
                aar1.MemberId = (rv == null || String.IsNullOrEmpty(rv.rspValue)) ? "0" : rv.rspValue;

                rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D1_FirstName");
                aar1.FirstName = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;

                rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D1_LastName");
                aar1.LastName = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;

                aar1.setStatus = currentStatusText.displayText;

                // Repurposing the Team field for the previous status note.
                aar1.setTeam = String.Empty;
                def_StatusDetail statusDetail = formsRepo.GetStatusDetailBySortOrder(statusMasterId, result.formStatus);
                if (statusDetail != null)
                {
                    def_StatusLog statusLog = formsRepo.GetMostRecentStatusLogByStatusDetailToFormResultIdAndUserId(
                        statusDetail.statusDetailId, frmResId, SessionHelper.LoginStatus.UserID);
                    if (statusLog != null)
                    {
                        aar1.setTeam = statusLog.statusNote;
                    }
                }

                //*** Start Adding Eligibility End Date for Lousiana
                if (result.formId == 11)
                {
                    // aar1.EigibilityEndDate 
                    rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "C1_ProgramEligibleEndDate");
                    aar1.EigibilityEndDate = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;


                }
                //*** End Adding Eligibility End Date for Lousiana
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Reports Controller UpdateStatus exception:" + excptn.Message);

                aar1.MemberId = "0";
                aar1.FirstName = "Exception";
                aar1.LastName = "Message";
                aar1.setStatus = "0";
                aar1.setTeam = excptn.Message;
                aar1.StatusDDL = new Dictionary<int,string>();
            }

            string viewDirPath = Applications.GetAdapTemplatesViewDirPath(SessionHelper.LoginStatus.EnterpriseID);
            return View(viewDirPath + "UpdateStatus.cshtml", aar1);
        }

        /// <summary>
        /// Processes changes made on the UpdateStatus page to save to the database.
        /// </summary>
        /// <param name="message">String input by the user to save to the database or send as an email.</param>
        /// <param name="formResultId">formResultId for the given application.</param>
        /// <param name="status">Status set by the user on the Update Status page.</param>
        /// <param name="email">boolean to determine if an email should be sent.</param>
        /// <returns>Redirects the user to the Applicant Report</returns>
        [HttpPost]
        public ActionResult StatusUpdated(string message, int formResultId, int status, bool email, string eligibilityEndDate = null)
        {
            //set this to a non-null value to show an error message 
            //on the report1 screen after redirecting at the end of this function
            string usrMsg = string.Empty; 

            try
            {
                def_FormResults result = formsRepo.GetFormResultById(formResultId);
                def_StatusMaster statusMaster = formsRepo.GetStatusMasterByFormId(result.formId);
                int statusMasterId = statusMaster.statusMasterId;

                int oldStatus = result.formStatus;
                if ( (status != oldStatus) || !String.IsNullOrWhiteSpace(message) )
                {

                    int accessLevel = (UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts")) ? 1 : 0;
                    // check here for managerial status.
                    if ( (accessLevel == 1) && UAS.Business.UAS_Business_Functions.hasPermission(1, "RptsExpts") )
                    {
                        accessLevel = 2;
                    }

                    result.formStatus = Convert.ToByte(status);
                    result.statusChangeDate = DateTime.Now;
                    formsRepo.SaveFormResults(result);

                    def_StatusLog statusLog = new def_StatusLog();

                    // status and oldStatus represent the sortOrder, not the statusDetailId
                    def_StatusDetail sdFrom = formsRepo.GetStatusDetailBySortOrder(statusMasterId, oldStatus);
                    def_StatusDetail sdTo   = formsRepo.GetStatusDetailBySortOrder(statusMasterId, status);
                    statusLog.statusDetailIdFrom = sdFrom.statusDetailId;
                    statusLog.statusDetailIdTo = sdTo.statusDetailId;
                    statusLog.formResultId = result.formResultId;
                    statusLog.UserID = SessionHelper.LoginStatus.UserID;
                    statusLog.statusLogDate = DateTime.Now;

                    string oldStatusIdent = sdFrom.identifier;
                    string statusIdent = sdTo.identifier;


                    //Start : Bug 14091 - HC3 - populate termination date and PBM GC on NEEDS INFO (ADAP ONLY)

                    if (result.formId == 15 && oldStatusIdent == "NEEDS_REVIEW" && statusIdent == "NEEDS_INFORMATION")
                    {

                        CheckADAPOnlyCoverageAndSetterminationDate(result.formResultId);


                    }
                    //End: Bug 14091 - HC3 - populate termination date and PBM GC on NEEDS INFO (ADAP ONLY)



                    if (!String.IsNullOrWhiteSpace(message))
                    {
                        if (oldStatusIdent == "NEEDS_REVIEW" || email)
                        {
                            if (statusIdent == "APPROVED" || statusIdent ==  "DENIED" || statusIdent ==  "NEEDS_INFORMATION" || email )
                            {
                                // Flag comments which are sent to the applicant.
                                message = "EMAIL SENT: " + message;
                            }
                        }
                        // Save message to status log
                        statusLog.statusNote = message;
                    }

                    int statusLogId = -1;
                    try
                    {
                        statusLogId = formsRepo.AddStatusLog(statusLog);
                    }
                    catch (Exception xcptn)
                    {
                        usrMsg = "AddStatusLog failed. : " + xcptn.Message;
                        if (xcptn.InnerException != null)
                            usrMsg = usrMsg + " " + xcptn.InnerException.Message;
                    }


                    if (oldStatusIdent == "NEEDS_REVIEW" || email )
                    {
                        if (statusIdent == "APPROVED" || statusIdent == "DENIED" || statusIdent == "NEEDS_INFORMATION" || email)
                        {
                            // initialize message vars
                            string useremail = result.subject.ToString();
                            StringBuilder body = new StringBuilder();

                            if (result.EnterpriseID == 8 && result.formId == 15)
                            {
                                var formType = formsRepo.GetResponseVariablesByFormResultIdentifier(result.formResultId, "C1_FormType");
                                string formVariant = formType.rspValue;
                                var firstName = formsRepo.GetResponseVariablesByFormResultIdentifier(result.formResultId, "C1_MemberFirstName").rspValue;
                                var lastName = formsRepo.GetResponseVariablesByFormResultIdentifier(result.formResultId, "C1_MemberLastName").rspValue;

                                body.Append("The " + formVariant + " for " + firstName + " " + lastName);

                                if (statusIdent == "NEEDS_REVIEW")
                                {
                                    var clientId = formsRepo.GetResponseVariablesByFormResultIdentifier(result.formResultId, "C1_MemberIdentifier");
                                    string client = clientId != null ? clientId.rspValue : string.Empty;
                                    var elgEndDate = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_ProgramEligibleEndDate");
                                    string endDate = elgEndDate != null ? elgEndDate.rspValue : string.Empty;

                                    body.Append(" has been successfully submitted for secondary review. You will be notified by e-mail after secondary review is complete and a determination is made. Temporary medication assistance is now available to the applicant; please see below for the current eligibility information:");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>Client ID: " + client + "</p>");
                                    body.Append("<p>Program Eligibility End Date: " + endDate + " </p>");
                                }
                                else if (statusIdent == "NEEDS_INFORMATION")
                                {
                                    var elgEndDate = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_ProgramEligibleEndDate");
                                    string endDate = elgEndDate != null ? elgEndDate.rspValue : string.Empty;
                                    body.Append(" has undergone secondary review and was determined to be incomplete.");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>Please submit the following documentation by " + endDate + " to continue receiving medication assistance. </p>");
                                    body.Append("<br/><br/>");
                                    body.Append(@"<p>&lt;proof of photo identification with date of birth listed&gt;</p>
                                                  <p>&lt;proof of California residency&gt;</p>
                                                  <p>&lt;proof of initial diagnosis&gt;</p>
                                                  <p>&lt;proof of labs for Viral Load&gt;</p>
                                                  <p>&lt;proof of labs for CD4 count&gt;</p>
                                                  <p>&lt;proof of household income&gt;</p>
                                                  <p>&lt;signed ADAP Consent Form&gt;</p>
                                                  <p>&lt;signed Temporary Access Period Request Form&gt;</p>
                                                  <p>&lt;copy of third party payer health insurance card&gt;</p>");
                                    body.Append("<br/><br/>");
                                    body.Append(@"<p>Below is a list of documentation that can be submitted to complete the application:</p>
                                                <ul>
	                                                <li>
	                                                Provide proof of photo identification with date of birth listed. Acceptable proofs are:
		                                                <ul>
			                                                <li>Driver’s License</li>
			                                                <li>United States passport</li>
			                                                <li>Permanent residence card</li>
			                                                <li>Work Permit</li>
			                                                <li>State identification card</li>
			                                                <li>Other photo identification issued by foreign government (e.g. voter registration card, passport, country of origin consulate identification card</li>
		                                                </ul>
	                                                </li>
	                                                <li>
	                                                Provide proof of California residency. Acceptable proof are:
		                                                <ul>
			                                                <li>Rental/Lease Agreement (within a year)</li>
			                                                <li>California rent or mortgage receipt (within 30 days)</li>
			                                                <li>Utility bill (within 30 days)</li>
			                                                <li>Voter registration card (within a year)</li>
			                                                <li>Vehicle registration card (within a year, not expired)</li>
			                                                <li>Social Security/Disability Award Letter (within a year)</li>
			                                                <li>W2 or 1099 (within a year)</li>
			                                                <li>Paystubs (within 3 months)</li>
			                                                <li>Residency Verification Affidavit Form</li>
		                                                </ul>
	                                                </li>
	                                                <li>
	                                                Provide proof of initial diagnosis. Acceptable proof are:
		                                                <ul>
			                                                <li>A copy of lab results indicating positive HIV diagnosis</li>
			                                                <li>A diagnosis form completed by a licensed healthcare provider</li>
			                                                <li>A diagnosis letter from the prescribing physician with the prescriber ID and the physician’s signature on the physician’s letterhead</li>
		                                                </ul>
	                                                </li>
	                                                <li>
	                                                Provide proof of labs for Viral Load (within a year)
	                                                </li>
	                                                <li>Provide proof of labs for CD4 count</li>
	                                                <li>
	                                                Provide proof of household income. Some acceptable proofs are:
		                                                <ul>
			                                                <li>Federal Tax Return</li>
			                                                <li>State Tax Return</li>
			                                                <li>3 current consecutive months of paystubs or 1 paystub showing year-to-date earnings for at least 3 months.</li>
			                                                <li>Disability Award letter or the most recent bank statement clearly identifying the for Supplemental Security Income and/or Social Security Disability Insurance (SSDI) deposit/income source.</li>
			                                                <li>Self-Employment Affidavit Form</li>
			                                                <li>Income Verification Affidavit Form</li>
		                                                </ul>
	                                                </li>
                                                </ul>");
                                }
                                else if (statusIdent == "DENIED")
                                {
                                    var currDate = DateTime.Now.ToShortDateString();
                                    body.Append(" has been denied effective " + currDate + ".");
                                    body.Append("The APPLICATION/FORM has been denied for the following reason(s):");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>&lt;Applicant currently has coverage through Medi-Cal without a Share of Cost&gt;</p>");
                                    body.Append("<p>&lt;Applicant does not reside in California&gt;</p>");
                                    body.Append("<p>&lt;Applicant is under the age of eighteen&gt;</p>");
                                    body.Append("<p>&lt;Applicant is not HIV positive&gt;</p>");
                                    body.Append("<p>&lt;Applicant’s Modified Adjusted Gross Income (MAGI) exceeds 500% Federal Poverty Level based on household size and household income&gt;</p>");
                                }
                                else if (statusIdent == "Approved - Medication Assistance Only")
                                {
                                    var clientId = formsRepo.GetResponseVariablesByFormResultIdentifier(result.formResultId, "C1_MemberIdentifier");
                                    string client = clientId != null ? clientId.rspValue : string.Empty;
                                    var elgEndDate = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_ProgramEligibleEndDate");
                                    string endDate = elgEndDate != null ? elgEndDate.rspValue : string.Empty;
                                    var annualReEnrollment = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_NextAnnualRecertDate");
                                    var nextSvf = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_Next6MonthRecertDate");
                                    var annualDate = annualReEnrollment != null ? annualReEnrollment.rspValue : string.Empty;
                                    var svfDate = nextSvf != null ? nextSvf.rspValue : string.Empty;
                                    body.Append(" has been approved; please see below for the current eligibility information:");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>Client ID: " + client + "</p>");
                                    body.Append("<p>Program Eligibility End Date: " + endDate + "</p>");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>Next annual re-enrollment: " + annualDate + "</p>");
                                    body.Append("<p>Next annual re-certification via Self-Verification Form (SVF): " + svfDate + "</p>");
                                }
                                else if (statusIdent == "Approved - Medication Assistance with OA-HIPP")
                                {
                                    var clientId = formsRepo.GetResponseVariablesByFormResultIdentifier(result.formResultId, "C1_MemberIdentifier");
                                    string client = clientId != null ? clientId.rspValue : string.Empty;
                                    var elgEndDate = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_ProgramEligibleEndDate");
                                    string endDate = elgEndDate != null ? elgEndDate.rspValue : string.Empty;
                                    var annualReEnrollment = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_NextAnnualRecertDate");
                                    var nextSvf = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_Next6MonthRecertDate");
                                    var annualDate = annualReEnrollment != null ? annualReEnrollment.rspValue : string.Empty;
                                    var svfDate = nextSvf != null ? nextSvf.rspValue : string.Empty;
                                    var healthProvider = formsRepo.GetResponseVariablesByFormResultIdentifier(result.formResultId, "C1_OtherInsuranceProvider");
                                    string helathInsuranceProvider = healthProvider != null ? healthProvider.rspValue : "your health insurance provider";
                                    body.Append(" has been approved; please see below for the current eligibility information:");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>Client ID: " + client + "</p>");
                                    body.Append("<p>Program Eligibility End Date: " + endDate + "</p>");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>Next annual re-enrollment: " + annualDate + "</p>");
                                    body.Append("<p>Next annual re-certification via Self-Verification Form (SVF): " + svfDate + "</p>");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>A request has been sent to generate a payment on behalf of " + firstName + " " + lastName);
                                    body.Append(" to be sent to " + helathInsuranceProvider + ".");
                                    body.Append(" You will receive another e-mail after the payment is sent, until then clients must continue to make payments to the health insurance provider to ensure the health insurance policy remains active.</p>");
                                }
                                else if (statusIdent == "Approved - Medication Assistance but OA-HIPP needs more information")
                                {
                                    var elgEndDate = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_ProgramEligibleEndDate");
                                    string endDate = elgEndDate != null ? elgEndDate.rspValue : string.Empty;
                                    body.Append(" has been approved for medication assistance but the Health Insurance Premium Payment (HIPP) program documentation is incomplete.");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>Please have the HIPP program documentation completed.");
                                    body.Append(" If the HIPP program portion is not completed it may delay the applicant receiving health insurance premium payment assistance because the program will begin payment on the month a complete application is submitted, if approved. In the interim, please continue to make payments to the health insurance provider to ensure the health insurance policy remains active.</p>");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>Please submit the following documentation.</p>");
                                    body.Append(@"<p>&lt;The most updated health insurance billing statement for medical, dental, and vision as applicable&gt;</p>
                                                     <p>&lt;Proof of the maximum Advance Premium Tax Credit (APTC) such as a Covered CA welcome letter or plan enrollment summary page (required for Covered CA plans only)&gt;</p>
                                                     <p>&lt;Proof of dependents (required for family plans only). Acceptable proofs are:</p>
                                                    <ul>
                                                        <li>Current tax return listing the spouse, registered domestic partner, and/or dependent(s)</li>
                                                    	<li>Marriage certificate</li> 
                                                    	<li>Registered Domestic Partnership</li>  
                                                    	<li>Birth certificate for dependent(s) &gt;</li>
                                                    </ul>
                                                    <p>&lt;Current tax return along with IRS Form 7862 if re-enrolling or re-certifying into the program (for a Covered CA plan only)&gt;</p>");
                                }
                                else if (statusIdent == "Approved - Medication Assistance but OA-HIPP denied")
                                {
                                    var elgEndDate = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_ProgramEligibleEndDate");
                                    string endDate = elgEndDate != null ? elgEndDate.rspValue : string.Empty;
                                    body.Append(" has been approved for medication assistance but the Health Insurance Premium Payment (HIPP) program portion is denied.");
                                    body.Append(@"<p>Please see below for the denial reason:</p>
                                                            <p>&lt;The health insurance plan was inactivated&gt;</p>" +
                                                            @"<p>&lt;The client is eligible for Full-scope Medi-Cal&gt</p>
                                                            <p>&lt;The client is Medicare eligible&gt;</p>");
                                }
                                else if (statusIdent == "Approved - Medication Assistance with Medicare Part D")
                                {
                                    var clientId = formsRepo.GetResponseVariablesByFormResultIdentifier(result.formResultId, "C1_MemberIdentifier");
                                    string client = clientId != null ? clientId.rspValue : string.Empty;
                                    var elgEndDate = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_ProgramEligibleEndDate");
                                    string endDate = elgEndDate != null ? elgEndDate.rspValue : string.Empty;
                                    var annualReEnrollment = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_NextAnnualRecertDate");
                                    var nextSvf = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_Next6MonthRecertDate");
                                    var annualDate = annualReEnrollment != null ? annualReEnrollment.rspValue : string.Empty;
                                    var svfDate = nextSvf != null ? nextSvf.rspValue : string.Empty;
                                    var healthProvider = formsRepo.GetResponseVariablesByFormResultIdentifier(result.formResultId, "C1_OtherInsuranceProvider");
                                    string helathInsuranceProvider = healthProvider != null ? healthProvider.rspValue : "your health insurance provider";
                                    body.Append(" has been approved; please see below for the current eligibility information:");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>Client ID: " + client + "</p>");
                                    body.Append("<p>Program Eligibility End Date: " + endDate + "</p>");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>Next annual re-enrollment: " + annualDate + "</p>");
                                    body.Append("<p>Next annual re-certification via Self-Verification Form (SVF): " + svfDate + "</p>");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>A request has been sent to generate a payment on behalf of " + firstName + " " + lastName);
                                    body.Append(" to be sent to the Medicare Part D health insurance provider. You will receive another e-mail after the payment is sent to the Medicare Part D provider. Clients must continue to make payments to ensure the Medicare Part D plan remains active. </p>");
                                }
                                else if (statusIdent == "Approved - Medication Assistance but Medicare Part D needs more information")
                                {
                                    var clientId = formsRepo.GetResponseVariablesByFormResultIdentifier(result.formResultId, "C1_MemberIdentifier");
                                    string client = clientId != null ? clientId.rspValue : string.Empty;
                                    var elgEndDate = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_ProgramEligibleEndDate");
                                    string endDate = elgEndDate != null ? elgEndDate.rspValue : string.Empty;
                                    body.Append(", " + client + ", has been approved for medication assistance but the Medicare Part D Premium Payment Program portion is incomplete.");
                                    body.Append("<br/><br/>");
                                    body.Append("<p>Please have the Medicare Part D Premium Payment Program portion completed.");
                                    body.Append("If the Medicare Part D Premium Payment Program portion is not completed it may delay the applicant in receiving Medicare Part D premium payment assistance because the program will begin payment on the month a complete application is submitted, if approved. In the interim, please continue to make payments to the Medicare Part D provider to ensure the Medicare Part D plan remains active.</p>");
                                    body.Append(@"<p>Please submit the following documentation</p>
	                                                    <p>&lt;Copy of your Medicare Part D health insurance card&gt;</p>");

                                }
                                else if (statusIdent == "Approved - Medication Assistance but Medicare Part D denied")
                                {
                                    var clientId = formsRepo.GetResponseVariablesByFormResultIdentifier(result.formResultId, "C1_MemberIdentifier");
                                    string client = clientId != null ? clientId.rspValue : string.Empty;
                                    var elgEndDate = formsRepo.GetResponseVariablesBySubjectForm(result.subject.Value, 18, "C1_ProgramEligibleEndDate");
                                    string endDate = elgEndDate != null ? elgEndDate.rspValue : string.Empty;
                                    body.Append(", " + client + ", has been approved for medication assistance but the Medicare Part D Premium Payment Program portion is denied. Please see below for the denial reason:");
                                    body.Append(@"<p>   &lt;The health insurance plan was inactivated&gt;</p>
                                                        <p>&lt;Applicant currently has coverage through Medi-Cal without a Share of Cost or is 100% Extra Help/Full Low Income Subsidy (LIS). Therefore is eligible to enroll in a Benchmark plan with no Medicare Part D premiums.&gt; </p>");

                                }
                                else
                                {
                                    body.Append("<p>Your ADAP application, " + result.formId.ToString() + ", has had its status changed from "
                                + formsRepo.GetStatusDetailBySortOrder(statusMasterId, oldStatus).def_StatusText.FirstOrDefault().displayText
                                + " to " + formsRepo.GetStatusDetailBySortOrder(statusMasterId, status).def_StatusText.FirstOrDefault().displayText + ".</p>");

                                    // append status change comment to message body
                                    if ((oldStatus == status) && !String.IsNullOrWhiteSpace(message))
                                    {
                                        body.Clear();
                                        body.Append("<p>Your ADAP application, " + result.formResultId.ToString() + ", has had additional comments added as described below:\n\n</p><p>" + message + "\n\n</p>");
                                    }
                                    else if (!String.IsNullOrWhiteSpace(message))
                                    {
                                        body.Append("<p>\n\nAn explanation is provided below:\n\n</p><p>" + message + "\n\n</p>");
                                    }

                                    // Append other comments from application to message body
                                    Applications applications = new Applications(formsRepo);
                                    string appComments = applications.GetFullCommentsReport(formResultId, true);
                                    if (!String.IsNullOrWhiteSpace(appComments))
                                    {
                                        body.Append(appComments);
                                    }
                                }
                            }
                            else
                            {
                                body.Append("<p>Your ADAP application, " + result.formId.ToString() + ", has had its status changed from "
                                + formsRepo.GetStatusDetailBySortOrder(statusMasterId, oldStatus).def_StatusText.FirstOrDefault().displayText
                                + " to " + formsRepo.GetStatusDetailBySortOrder(statusMasterId, status).def_StatusText.FirstOrDefault().displayText + ".</p>");

                                // append status change comment to message body
                                if ((oldStatus == status) && !String.IsNullOrWhiteSpace(message))
                                {
                                    body.Clear();
                                    body.Append("<p>Your ADAP application, " + result.formResultId.ToString() + ", has had additional comments added as described below:\n\n</p><p>" + message + "\n\n</p>");
                                }
                                else if (!String.IsNullOrWhiteSpace(message))
                                {
                                    body.Append("<p>\n\nAn explanation is provided below:\n\n</p><p>" + message + "\n\n</p>");
                                }

                                // Append other comments from application to message body
                                Applications applications = new Applications(formsRepo);
                                string appComments = applications.GetFullCommentsReport(formResultId, true);
                                if (!String.IsNullOrWhiteSpace(appComments))
                                {
                                    body.Append(appComments);
                                }
                            }

                            string jsonEmail = "{\"msgSenderId\":\"" + SessionHelper.LoginStatus.UserID.ToString() + "\"," +
                                "\"msgRecipientList\":\"" + useremail + "\"," +
                                "\"msgCcList\":\"" + String.Empty + "\"," +
                                "\"msgSubject\":\"" + "Status Change Notification" + "\"," +
                                "\"msgBody\":\"" + body.ToString().Replace("\"", "\\\"") + "\"," +
                                "\"sessionId\":\"" + SessionHelper.PortalSession +"\"}";

                            try
                            {
                                sendSecureEmail(SessionHelper.LoginStatus.EnterpriseID, jsonEmail);
                            }
                            catch (Exception xcptn)
                            {
                                usrMsg = usrMsg + " Failed to send status update notification email: " + xcptn.Message;
                                if (xcptn.InnerException != null)
                                    usrMsg = usrMsg +  " " + xcptn.InnerException.Message;
                            }
                        }
                    }
                    
                    // Add or Update application data in Ramsell if necessary
                    if (statusIdent == "APPROVED" || statusIdent == "NEEDS_INFORMATION")
                    {
                        if (result.EnterpriseID != 8)
                        {
                            usrMsg = usrMsg + Ramsell.SendApplicationData(formsRepo, result.formResultId);
                        }
                    }

                    //on submission and approval, for historic purposes, generate a report pdf
                    //and add it as a def_attachment related to the status log entry that was just added
                    if (statusLogId > -1 && statusIdent == "APPROVED" || statusIdent ==  "NEEDS_REVIEW")
                    {
                        string outpath = ControllerContext.HttpContext.Server.MapPath("../Content/report_" + System.DateTime.Now.Ticks + ".pdf");
                        FormResultPdfReport report = new AdapPdfReport(formsRepo, formResultId, outpath, false);
                        report.BuildReport();
                        report.outputToFile();
                        string superDir = SessionHelper.LoginStatus.EnterpriseID.ToString();
                        string attachmentName =  formResultId 
                            + (statusIdent == "APPROVED" ? ".approved" : ".submit" ) 
                            + DateTime.Now.ToString("yyyy-MM-dd-HH-mm", CultureInfo.InvariantCulture) + ".pdf";
                        FileUploads.CreateAttachment(formsRepo,
                            System.IO.File.OpenRead(outpath), attachmentName, superDir, null, statusLogId, 2, 1);
                    }

                    if (result.formId == 15 && statusIdent == "NEEDS_REVIEW")
                    {
                        SetMagellanGroupCode(formResultId);
                    }
                    else if (result.formId == 19 && statusIdent == "APPROVED")
                    {
                        // update dashboard dates
                        var subject = result.subject.Value;
                        var dob = formsRepo.GetResponseVariablesBySubjectForm(subject, 15, "C1_MemberDateOfBirth");
                        if (dob != null)
                        {
                            var currDate = DateTime.Now;
                            DateTime dDob;
                            DateTime.TryParse(dob.rspValue, out dDob);
                            dDob = new DateTime(currDate.Year, dDob.Month, dDob.Day);
                            DateTime myDob = dDob;
                            
                            // date of birth already happened
                            if ((currDate.Month > dDob.Month) || (currDate.Month == dDob.Month && currDate.Day >= dDob.Day))
                            {
                                dDob = dDob.AddYears(1);
                            }
                            DateTime svfDate = dDob.AddDays(180);
                            svfDate = new DateTime(svfDate.Year, svfDate.Month, 1).AddMonths(1).AddDays(-1);
                            DateTime elgEndDate = dDob;
                            elgEndDate = new DateTime(elgEndDate.Year, elgEndDate.Month, 1).AddMonths(1).AddDays(-1);

                            var fElgEndDate = formsRepo.GetResponseVariablesBySubjectForm(subject, 18, "C1_ProgramEligibleEndDate");
                            if (fElgEndDate != null)
                            {
                                fElgEndDate.rspValue = String.Format("{0:MM/dd/yy}", elgEndDate);
                            }

                            var nextSvf = formsRepo.GetResponseVariablesBySubjectForm(subject, 18, "C1_Next6MonthRecertDate");
                            if (nextSvf != null)
                            {
                                nextSvf.rspValue = String.Format("{0:MM/dd/yy}", svfDate);
                            }

                            def_ResponseVariables rvAnnual = formsRepo.GetResponseVariablesBySubjectForm(subject, 18, "C1_NextAnnualRecertDate");
                            if (rvAnnual != null)
                            {
                                if (myDob < currDate.AddMonths(6))
                                {
                                    myDob = myDob.AddYears(1);
                                }
                                rvAnnual.rspValue = String.Format("{0:MM/dd/yy}", myDob);
                            }

                            var hippEndDate = formsRepo.GetResponseVariablesBySubjectForm(subject, 18, "C1_HIPPEndDate");

                            if (hippEndDate != null && !string.IsNullOrWhiteSpace(hippEndDate.rspValue))
                            {
                                hippEndDate.rspValue = String.Format("{0:MM/dd/yy}", elgEndDate);
                            }

                            var partDEndDate = formsRepo.GetResponseVariablesBySubjectForm(subject, 18, "C1_MDPPEndDate");
                            if (partDEndDate != null && !string.IsNullOrWhiteSpace(partDEndDate.rspValue))
                            {
                                partDEndDate.rspValue = String.Format("{0:MM/dd/yy}", elgEndDate);
                            }

                            formsRepo.Save();
                        }
                    }

                    //*** Start adding eligibility end date to lousiana
                    if (result.formId == 11 && statusIdent == "APPROVED")
                    {
                        CreateEligibilityField(formResultId, "C1_ProgramEligibleEndDate", Convert.ToDateTime(eligibilityEndDate).ToShortDateString());
                    }
                    //*** End adding eligibility end date to lousiana
                }
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("AdapController.UpdateStatus StatusUpdated exception:" + excptn.Message);
                usrMsg = usrMsg + excptn.Message;
            }

            if (usrMsg.Length > 500)
                usrMsg = usrMsg.Substring(0, 500);

            return RedirectToAction("Report1", "ADAP", new { errorMessage = usrMsg } );
        }

        private void SetMagellanGroupCode(int formResultId)
        {
            AdapCaController adapCa = new AdapCaController(formsRepo);
            adapCa.AutoGroupAssignment(formResultId);
        }


        private void CreateHealthCoverageField(int formResultId, string identifier, string responseValue)
        {
            def_FormResults frmRes = formsRepo.GetFormResultById(formResultId);

            def_Items item = formsRepo.GetItemByIdentifier(identifier + "_item");

            def_ItemResults ir = new def_ItemResults()
            {
                itemId = item.itemId,
                sessionStatus = 0,
                dateUpdated = DateTime.Now
            };

            var itemResult = formsRepo.GetItemResultByFormResItem(formResultId, item.itemId);

            if (itemResult == null)
            {
                frmRes.def_ItemResults.Add(ir);
            }
            else
            {
                ir = itemResult;
            }


            foreach (var iv in formsRepo.GetItemVariablesByItemId(item.itemId))
            {
                // Note for General forms like ADAP there should only be 1 ItemVariable per Item
                def_ResponseVariables rv = new def_ResponseVariables();
                rv.itemVariableId = iv.itemVariableId;
                // rv.rspDate = DateTime.Now;    // RRB 11/11/15 The date, fp, and int fields are for the native data conversion.
                rv.rspValue = responseValue;

                formsRepo.ConvertValueToNativeType(iv, rv);

                ir.def_ResponseVariables.Add(rv);
            }

            formsRepo.Save();

        }


        //for LA eligibility field creation
        private void CreateEligibilityField(int formResultId, string identifier, string responseValue)
        {
            def_FormResults frmRes = formsRepo.GetFormResultById(formResultId);

            def_Items item = formsRepo.GetItemByIdentifier(identifier + "_item");

            def_ItemResults ir = new def_ItemResults()
            {
                itemId = item.itemId,
                sessionStatus = 0,
                dateUpdated = DateTime.Now
            };

            var itemResult = formsRepo.GetItemResultByFormResItem(formResultId, item.itemId);

            if (itemResult == null)
            {
                frmRes.def_ItemResults.Add(ir);
            }
            else
            {
                ir = itemResult;
            }


            foreach (var iv in formsRepo.GetItemVariablesByItemId(item.itemId))
            {
                // Note for General forms like ADAP there should only be 1 ItemVariable per Item
                def_ResponseVariables rv = new def_ResponseVariables();
                rv.itemVariableId = iv.itemVariableId;
                // rv.rspDate = DateTime.Now;    // RRB 11/11/15 The date, fp, and int fields are for the native data conversion.
                rv.rspValue = responseValue;

                formsRepo.ConvertValueToNativeType(iv, rv);

                ir.def_ResponseVariables.Add(rv);
            }

            formsRepo.Save();

        }

        private void CheckADAPOnlyCoverageAndSetterminationDate(int formResultId)
        {

            try
            {
                bool isHc1Available = false;

                def_ResponseVariables hc1 = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_StateMedicaidEnrolled");

                if (hc1 != null && hc1.rspValue == 0.ToString())
                {
                    isHc1Available = true;

                }



                bool isHc2Available = false;

                def_ResponseVariables hc2 = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_MedicareEligible");

                if (hc2 != null && hc2.rspValue == 1.ToString())
                {
                    isHc2Available = true;

                }


                bool isHc3Available = false;

                def_ResponseVariables hc3 = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_ACAStateExchgEnroll");

                if (hc3 != null && hc3.rspValue == 1.ToString())
                {
                    isHc3Available = true;

                }


                bool isHc3OtherAvailable = false;

                def_ResponseVariables hc3Other = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_OtherInsuranceEnroll");

                if (hc3Other != null && hc3Other.rspValue == 1.ToString())
                {
                    isHc3OtherAvailable = true;

                }


                if (!isHc1Available && !isHc2Available && !isHc3Available && !isHc3OtherAvailable)
                {
                    //if all types of health coverage is not available checking if our system has previous data

                    //C1_OtherInsuranceType //C1_OtherInsuranceProvider //C1_OtherInsuranceEffDate //C1_OtherInsuranceEndDate

                    bool isPreviousInsuranceAvailable = false;

                    //C1_OtherInsuranceMemberID2

                    def_ResponseVariables PreviousOtherInsuranceMemberID = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_OtherInsuranceMemberID2");

                    if (PreviousOtherInsuranceMemberID != null && !String.IsNullOrEmpty(PreviousOtherInsuranceMemberID.rspValue))
                    {
                        isPreviousInsuranceAvailable = true;

                    }

                    def_ResponseVariables PreviousEffectiveStart = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_OtherInsuranceEffDate");

                    if (PreviousEffectiveStart != null && !String.IsNullOrEmpty(PreviousEffectiveStart.rspValue))
                    {
                        isPreviousInsuranceAvailable = true;

                    }

                    if (isPreviousInsuranceAvailable)
                    {
                        //update the effective end date

                        DateTime newTerminationDate = DateTime.Today.AddDays(-1);

                        var EffectiveEndDate = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_OtherInsuranceEndDate");

                        if (EffectiveEndDate != null)
                        {
                            DateTime dateValue;

                            if ((!String.IsNullOrEmpty(EffectiveEndDate.rspValue)) && DateTime.TryParse(EffectiveEndDate.rspValue, CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None, out dateValue))
                            {
                                if (dateValue > newTerminationDate)
                                {
                                    EffectiveEndDate.rspValue = newTerminationDate.ToShortDateString();
                                }
                            }
                            else
                            {
                                EffectiveEndDate.rspValue = newTerminationDate.ToShortDateString();
                            }

                            formsRepo.Save();
                        }
                        else
                        {
                            CreateHealthCoverageField(formResultId, "C1_OtherInsuranceEndDate", newTerminationDate.ToShortDateString());
                        }

                    }
                    else
                    {
                        //previous insurance not available

                    }


                }
            }
            catch (Exception ex)
            {
                //log the message
                Debug.WriteLine("AdapController.UpdateStatus CheckADAPOnlyCoverageAndSetterminationDate exception:" + ex.Message);
            }
        }

    }
}