using AJBoggs.Adap.Domain;
using Assmnts.Business.Workflow;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Assmnts.UasServiceRef;
using Data.Abstract;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using UAS.Business;

namespace Assmnts.Controllers
{
    public partial class AdapController : Controller
    {
        public static readonly List<String> dateSelectList =
            new List<String> { 
                 "All",
                "Last Modified within 3 Months",
                "Last Modified within 1 Month",
                "Re-Certs Late",
                "Re-Certs Due within 7 Days",
                "Re-Certs Due within 1 Month",
                "Re-Certs Due within 2 Months",
                "Re-Certs Due within 3 Months"
        };

        /// <summary>
        /// Primary method for the Application List report
        /// Displays individual a list of applications with links to access each application or update associated data.
        /// Used by other methods which use similar report styles.
        /// </summary>
        /// <param name="aar1">Model object for cshtml page</param>
        /// <param name="errorMessage">Message to display to the user.</param>
        /// <returns>View for Application Report</returns>
        [HttpGet]
        public ActionResult Report1(AdapApplicantRpt1 aar1 = null, string errorMessage = null)
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            bool accessLevel = UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts");
            if (!accessLevel && SessionHelper.LoginStatus.EnterpriseID != 8)
            {
                return RedirectToAction("AdapPortal", "ADAP", new { userId = SessionHelper.LoginStatus.UserID });
            }

            AuthenticationClient webclient = new AuthenticationClient();
            // Populate the Report Model
            // Will be very similar to /Model/TemplateForms.cs for now.
            // replace 'null' in the return with with Model instance.
            // replace view with the Report model
            if (aar1 == null)
            {
                aar1 = new AdapApplicantRpt1();
            }
            aar1.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;
            if (String.IsNullOrEmpty(aar1.ReportName))
            {
                aar1.ReportName = "Applicant Report";
            }
            if (String.IsNullOrEmpty(aar1.setTeam))
            {
                string rTeam = Request["Team"];
                if (!String.IsNullOrEmpty(rTeam))
                {
                    aar1.setTeam = rTeam;
                }
            }
            if (String.IsNullOrEmpty(aar1.setStatus))
            {
                string rStatus = Request["Status"];
                if (!String.IsNullOrEmpty(rStatus))
                {
                    aar1.setStatus = rStatus;
                }
            }

            aar1.TeamDDL = webclient.GetGroupsByEnterpriseID(SessionHelper.LoginStatus.EnterpriseID);
            aar1.TeamDDL.Insert(0, new Group()
            {
                GroupName = "All"
            });

            aar1.TypeDDL = Applications.GetDefaultFormIdentifiersForEnterprise(SessionHelper.LoginStatus.EnterpriseID);
            aar1.TypeDDL.Insert(0, "All");

            if (SessionHelper.LoginStatus.EnterpriseID == 8)
            {
                aar1.StatusDDL = AdapApprovals.GetAdapStatusList(formsRepo, SessionHelper.LoginStatus.EnterpriseID, 4);

                 //for moop, Key 51 given to diiferentiate from others in the items with Status Master ID 4, On screen using text as the search criteria
                aar1.StatusDDL.Add(101, "Submitted");
            }
            else
            {
                aar1.StatusDDL = AdapApprovals.GetAdapStatusList(formsRepo, SessionHelper.LoginStatus.EnterpriseID);
            }
            aar1.StatusDDL.Add(-1, "All");

            // This probably should not be hardcoded.
            // Search parameters in DataTableApplicationsList parse these strings for the numerical data
            // (1 month, 3 months, etc), a safer means should be implemented.
            aar1.DateDDL = dateSelectList;

            aar1.errorMessage = errorMessage;


            aar1.Forms = new List<SelectListItem>();
            formsEntities context = new Assmnts.formsEntities();
            aar1.Forms = (from f in context.def_FormVariants
                          where f.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID
                          select new SelectListItem()
                          {
                              Text = f.title,
                              Value = f.formID.ToString()
                          }).ToList();

            aar1.SearchForms = (from f in context.def_FormVariants
                                where f.EnterpriseID == 8
                                select new SelectListItem()
                                {
                                    Text = f.title,
                                    Value = f.formID.ToString() + "|" + f.title
                                }).ToList();
            aar1.SearchForms.Insert(0, new SelectListItem()
            {
                Text = "All",
                Value = string.Empty
            });

            aar1.Units = new List<SelectListItem>();
            var groups = webclient.GetGroupsByEnterpriseID(SessionHelper.LoginStatus.EnterpriseID);
            aar1.Units = (from g in groups
                          where g.GroupTypeID == 195
                          select new SelectListItem()
                          {
                              Text = g.GroupDescription,
                              Value = g.GroupID.ToString()
                          }).ToList();
            aar1.Units.Insert(0, new SelectListItem()
            {
                Text = "All",
                Value = string.Empty
            });

            webclient.Close();

            string viewPath = Applications.GetDefaultReportViewPathForEnterprise(SessionHelper.LoginStatus.EnterpriseID);

            return View(viewPath, aar1);
        }
        /// <summary>
        ///  controller for the ActiveEnrollment Site. It returns the list of active enrollment sites 
        ///  using web service. 
        /// </summary>
        /// <returns></returns>
        public ActionResult ApplicationsActiveEnrollmentSite() 
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            bool accessLevel = UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts");
            if (!accessLevel && SessionHelper.LoginStatus.EnterpriseID != 8) // checking the Enterprise id and checking the access to the reports(Rpts)//
            {
                return RedirectToAction("AdapPortal", "ADAP", new { userId = SessionHelper.LoginStatus.UserID }); // so 'if' condition fails, it redirects to Action() method of AdapController.Reports.cs
            }

            AuthenticationClient webclient = new AuthenticationClient(); // webclient is the object of the web service
            // Populate the Report Model
            // Will be very similar to /Model/TemplateForms.cs for now.
            // replace 'null' in the return with with Model instance.
            // replace view with the Report model
            //List <ActiveEnrollmentSites> aar1=webclient.ListEnrollment();
            var result = webclient.ListEnrollment(); // Calling the ListEnrollment which executes our store procedure sp_ActiveEnrollmentSites
            List<ActiveEnrollmentSites1> aar1 = new List<ActiveEnrollmentSites1>(); 
            foreach (var rec in result)
            {
                ActiveEnrollmentSites1 AE = new ActiveEnrollmentSites1();
                //AE.GroupID = rec.GroupID;
                AE.GroupDescription = rec.GroupDescription;
                AE.GroupNumber = rec.GroupNumber;
                AE.Address1 = rec.Address1;
                AE.Address2 = rec.Address2;
                AE.City = rec.City;
                AE.ZipCode = rec.ZipCode;
                AE.State = rec.State;
                AE.County = rec.County;
                AE.EmailAddress = rec.EmailAddress;
                AE.PhoneNumber = rec.PhoneNumber;
                aar1.Add(AE); ////adding each aar1site in to list // 
            }
         
            webclient.Close(); // web client is the object of the web service//

            string viewPath = "~/Views/CAADAP/ActiveEnrollmentSites.cshtml";// Applications.GetDefaultReportViewPathForEnterprise(SessionHelper.LoginStatus.EnterpriseID);
            return View(viewPath, aar1);

           
        }
        /// <summary>
        /// Creates paramters for Applications Pending for ADAP Staff
        /// </summary>
        /// <returns>Leads to Report 1, which returns the Application Report</returns>
        [HttpGet]
        public ActionResult ApplicationsPending()
        {
            string rTeam = Request["Team"];
            string rStatus = Request["Status"];
            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ReportName = "Applications Pending";

            if (!String.IsNullOrEmpty(rTeam))
            {
                aar1.setTeam = rTeam;
            }

            if (!String.IsNullOrEmpty(rStatus))
            {
                aar1.setStatus = rStatus;
            }

            if (String.IsNullOrEmpty(rTeam) && String.IsNullOrEmpty(rStatus))
            {
                aar1.setStatus = "Pending";
            }

            return Report1(aar1);
        }

        /// <summary>
        /// Creates paramters for Applicants Re-certifications Due.
        /// </summary>
        /// <returns>Leads to Report 1, which returns the Application Report</returns>
        [HttpGet]
        public ActionResult ReCertsDue()
        {
            string rTeam = Request["Team"];
            string rStatus = Request["Status"];

            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ReportName = "Re-Certifications Due";
            aar1.setDate = "Re-Certs Due";

            if (!String.IsNullOrEmpty(rTeam))
            {
                aar1.setTeam = rTeam;
            }

            if (!String.IsNullOrEmpty(rStatus))
            {
                aar1.setStatus = rStatus;
            }

            return Report1(aar1);
        }

        /// <summary>
        /// Primary method for displaying the Application Overview report.
        /// Shows Application Status Summary Counts by Group / Status
        /// Used by other reports which use a similar report style.
        /// </summary>
        /// <param name="aar1">Model object for cshtml page</param>
        /// <returns>View for Overview report</returns>
        [HttpGet]
        public ActionResult AppOverview(AdapApplicantRpt1 aar1 = null)
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            bool accessLevel = UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts");
            if (!accessLevel)
            {
                return RedirectToAction("AdapPortal", "ADAP", new { userId = SessionHelper.LoginStatus.UserID });
            }

            AuthenticationClient webclient = new AuthenticationClient();
            // Populate the Report Model
            // Will be very similar to /Model/TemplateForms.cs for now.
            // replace 'null' in the return with with Model instance.
            // replace view with the Report model
            if (aar1 == null)
            {
                aar1 = new AdapApplicantRpt1();
            }
            aar1.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;
            if (String.IsNullOrEmpty(aar1.ReportName))
            {
                aar1.ReportName = "Applicant Overview";
            }
            aar1.TeamDDL = webclient.GetGroupsByEnterpriseID(SessionHelper.LoginStatus.EnterpriseID);
            aar1.TeamDDL.Insert(0, new Group()
            {
                GroupName = "All"
            });

            aar1.StatusDDL = AdapApprovals.GetAdapStatusList(formsRepo, SessionHelper.LoginStatus.EnterpriseID);
            aar1.StatusDDL.Add(-1, "All");

            aar1.TypeDDL = Applications.GetDefaultFormIdentifiersForEnterprise(SessionHelper.LoginStatus.EnterpriseID);
            aar1.TypeDDL.Insert(0, "All");

            // This probably should not be hardcoded.
            // Search parameters in DataTableApplicationsList parse these strings for the numerical data
            // (1 month, 3 months, etc), a safer means should be implemented.
            aar1.DateDDL = dateSelectList;

            string rType = Request["Type"];
            if (!String.IsNullOrEmpty(rType))
            {
                aar1.setType = rType;
            }

            string rTeam = Request["Team"];
            if (!String.IsNullOrEmpty(rTeam))
            {
                aar1.setTeam = rTeam;
            }

            string rStatus = Request["Status"];
            if (!String.IsNullOrEmpty(rStatus))
            {
                aar1.setStatus = rStatus;
            }

            string rDate = Request["Date"];
            if (!String.IsNullOrEmpty(rDate))
            {
                aar1.setDate = rDate;
            }

            webclient.Close();

            string viewDirPath = Applications.GetAdapTemplatesViewDirPath(SessionHelper.LoginStatus.EnterpriseID);
            return View(viewDirPath + "AppOverview.cshtml", aar1);
        }

        /// <summary>
        /// Creates parameters for the Process Summary report.
        /// </summary>
        /// <returns>Leads to AppOverview, which returns the Overview Report</returns>
        [HttpGet]
        public ActionResult ProcessSummary()
        {
            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ReportName = "Process Summary";
            aar1.setDate = "Summary";

            return AppOverview(aar1);
        }

        /// <summary>
        /// Primary method for dislaying the Apps Pending report
        /// Displays data by Group / Status, and allows drill down links to Application reports.
        /// Used by other reports which use a similar report style.
        /// </summary>
        /// <param name="aar1">Model object for the cshtml page.</param>
        /// <returns>View for Applications Pending</returns>
        [HttpGet]
        public ActionResult AppsPending(AdapApplicantRpt1 aar1 = null)
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            bool accessLevel = UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts");
            if (!accessLevel)
            {
                return RedirectToAction("AdapPortal", "ADAP", new { userId = SessionHelper.LoginStatus.UserID });
            }

            AuthenticationClient webclient = new AuthenticationClient();

            // Populate the Report Model
            if (aar1 == null)
            {
                aar1 = new AdapApplicantRpt1();
            }
            aar1.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;
            if (String.IsNullOrEmpty(aar1.ReportName))
            {
                aar1.ReportName = "Applications Pending";
                aar1.setStatus = "Pending";
            }

            aar1.TeamDDL = webclient.GetGroupsByEnterpriseID(SessionHelper.LoginStatus.EnterpriseID);
            aar1.TeamDDL.Insert(0, new Group()
            {
                GroupName = "All"
            });
            webclient.Close();

            aar1.StatusDDL = AdapApprovals.GetAdapStatusList(formsRepo, SessionHelper.LoginStatus.EnterpriseID);
            aar1.StatusDDL.Add(-1, "All");

            aar1.TypeDDL = Applications.GetDefaultFormIdentifiersForEnterprise(SessionHelper.LoginStatus.EnterpriseID);
            aar1.TypeDDL.Insert(0, "All");

            // This probably should not be hardcoded.
            // Search parameters in DataTableApplicationsList parse these strings for the numerical data
            // (1 month, 3 months, etc), a safer means should be implemented.
            aar1.DateDDL = dateSelectList;

            string viewDirPath = Applications.GetAdapTemplatesViewDirPath(SessionHelper.LoginStatus.EnterpriseID);
            return View(viewDirPath + "AppDashboards.cshtml", aar1);
        }

        /// <summary>
        /// Set the parameters for the Current New Apps report.
        /// </summary>
        /// <returns>Leads to AppsPending, which returns the Applications Pending report.</returns>
        [HttpGet]
        public ActionResult CurrentNewApps()
        {
            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ReportName = "Current New Applicants";
            aar1.setStatus = "Needs Review";

            return AppsPending(aar1);
        }

        /// <summary>
        /// Set the parameters for the Dashboard Re-Certifications report
        /// </summary>
        /// <returns>Leads to AppsPending, which returns the Applications Pending report.</returns>
        [HttpGet]
        public ActionResult DashboardReCerts()
        {
            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ReportName = "Re-Certification";
            aar1.setDate = "Re-Certs Due";

            return AppsPending(aar1);
        }

        /// <summary>
        /// Creates the Status History page. 
        /// </summary>
        /// <param name="formResultId">The formResultId of the application in question.</param>
        /// <returns>View of the Status History page.</returns>
        [HttpGet]
        public ActionResult StatusHistory(int formResultId)
        {
            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;
            aar1.ReportName = "Status History";
            aar1.formResultId = formResultId;

            def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "C1_MemberIdentifier") ?? formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "ADAP_D9_Ramsell");

            aar1.MemberId = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? "0" : rv.rspValue;
            rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "ADAP_D1_FirstName");
            aar1.FirstName = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;
            rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "ADAP_D1_LastName");
            aar1.LastName = ((rv == null) || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;


            string viewDirPath = Applications.GetAdapTemplatesViewDirPath(SessionHelper.LoginStatus.EnterpriseID);
            return View(viewDirPath + "StatusHistory.cshtml", aar1);
        }


        /// <summary>
        /// Transfers control to the Assessment Template controller to display a form.
        /// </summary>
        /// <returns>The view associated with viewing a form.</returns>
        [HttpGet]
        public ActionResult ToTemplate()
        {
            string sFormResultId = Request["formResultId"];

            // If you can't find the FormResult, error out.  Anything else is a security risk.
            int formResultId = 0;
            try
            {
                formResultId = Convert.ToInt32(sFormResultId);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("AdapController.Reports converting formResultId exception:" + sFormResultId);
            }
            def_FormResults fr = formsRepo.GetFormResultById(formResultId);

            // Get the first Part and Section in the Form
            // NOTE: this could be made a little more efficient
            def_Forms form = formsRepo.GetFormById(fr.formId);
            List<def_Parts> partsList = formsRepo.GetFormParts(form);

            List<def_Sections> sectionsInPart = formsRepo.GetSectionsInPart(partsList[0]);
            int sectionId = sectionsInPart[0].sectionId;

            if (Request["sectionIdOverride"] != null && !String.IsNullOrWhiteSpace(Request["sectionIdOverride"].ToString()))
            {
                string val = Request["sectionIdOverride"];
                sectionId = Convert.ToInt32(val);
            }

            // Eligibility and can approve
            if (fr.formId == 18 && UAS.Business.UAS_Business_Functions.hasPermission(PermissionConstants.APPROVE, PermissionConstants.ASSMNTS))
            {
                // go to write version of Eligbility
                sectionId = sectionsInPart[1].sectionId;
            }

            bool isReadOnly = false;
            if (!UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts"))
            {
                try
                {
                    int status = fr.formStatus;
                    isReadOnly = (status != formsRepo.GetStatusDetailByMasterIdentifier(1, "IN_PROCESS").sortOrder
                        && status != formsRepo.GetStatusDetailByMasterIdentifier(1, "NEEDS_INFORMATION").sortOrder);
                }
                catch (Exception exptn)
                {
                    Debug.WriteLine("AdapController.Reports finding form Status exception: " + formResultId);
                }
            }

            // Readonly when app in approved status
            if (fr.formStatus >= 6 && fr.formId == 15)
            {
                isReadOnly = true;
            }

            // Check access for Case Managers.
            if (!isReadOnly && UAS.Business.UAS_Business_Functions.hasPermission(2, "RptsExpts"))
            {
                isReadOnly = true;
            }

            SessionHelper.SessionForm = new SessionForm()
            {
                formId = fr.formId,
                formIdentifier = form.identifier,
                partId = partsList[0].partId,
                sectionId = sectionId,
                formResultId = formResultId,
                readOnlyMode = isReadOnly
            };

            string update = Request["Update"];
            if (!String.IsNullOrEmpty(update))
            {
                //def_FormResults result = formsRepo.GetFormResultById(frmResId);
                //if (result.formStatus == 1)
                //{
                //    result.formStatus = 2;
                //    formsRepo.SaveFormResults(result);
                //}
            }

            // This is being deprecated.
            Session["part"] = partsList[0].partId;
            Session["form"] = null;

            return RedirectToAction("Template", "Results", new { sectionId = sectionId.ToString() });
        }

        /// <summary>
        /// Loads the page to update the Team assignment for a given application.
        /// </summary>
        /// <returns>View of the UpdateTeam page.</returns>
        [HttpGet]
        public ActionResult UpdateTeam()
        {
            string formId = Request["formId"];
            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            int? entepriseId = 0;

            try
            {
                int frmResId = Convert.ToInt32(formId);
                def_FormResults result = formsRepo.GetFormResultById(frmResId);
                entepriseId = result.EnterpriseID;

                aar1.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;
                aar1.formResultId = frmResId;

                if (result.GroupID != null)
                {
                    aar1.teamId = result.GroupID.Value;
                }

                def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "C1_MemberIdentifier") ?? formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D9_Ramsell");
                aar1.MemberId = (rv == null || String.IsNullOrEmpty(rv.rspValue)) ? "0" : rv.rspValue; ;
                rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D1_FirstName");
                aar1.FirstName = (rv == null || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;
                rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmResId, "ADAP_D1_LastName");
                aar1.LastName = (rv == null || String.IsNullOrEmpty(rv.rspValue)) ? String.Empty : rv.rspValue;

                IUasSql uasSql = new Data.Concrete.UasSql();

                Dictionary<int, string> groups = uasSql.getGroups(result.EnterpriseID.Value);

                if (!groups.ContainsKey(0))
                {
                    groups.Add(0, "Enterprise Wide");
                }

                aar1.Teams = (new SelectList(groups.OrderBy(x => x.Key), "key", "value")).ToList();

            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Reports Controller UpdateTeam exception:" + excptn.Message);

            }

            if (entepriseId == 8)
            {
                return View("~/Views/CAADAP/UpdateTeam.cshtml", aar1);
            }
            else
            {
                return View("~/Views/Templates/ADAP/UpdateTeam.cshtml", aar1);
            }

        }

        /// <summary>
        /// Processes changes made on the TeamUpdate page to save the changes to the database.
        /// </summary>
        /// <param name="aar1">Model associated with the cshtml page.</param>
        /// <returns>Redirects the user to the Application Report.</returns>
        [HttpPost]
        public ActionResult TeamUpdated(AdapApplicantRpt1 aar1)
        {

            def_FormResults fr = formsRepo.GetFormResultById(aar1.formResultId);

            if (fr != null)
            {
                fr.GroupID = aar1.teamId;

                formsRepo.SaveFormResults(fr);
            }

            return RedirectToAction("Report1", "ADAP");
        }

        /// <summary>
        /// Used to get data for the Contact Info modal on the Application Reports.
        /// </summary>
        /// <returns>A JSON formatted string for the Contact Info modal</returns>
        [HttpGet]
        public string ContactInfo()
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                Report1();
                return String.Empty;
            }


            ContactInfoModel cim = null;
            try
            {
                string strFormResultId = Request["formResultId"] as string;
                int formResultId = Convert.ToInt32(strFormResultId);
                Debug.WriteLine("ContactInfo formResultId: " + formResultId.ToString());
                def_FormResults frm = formsRepo.GetFormResultById(formResultId);

                AuthenticationClient webclient = new AuthenticationClient();
                UserDisplay usr = (frm.subject != null) ? webclient.GetUserDisplay(Convert.ToInt32(frm.subject)) : new UserDisplay();

                cim = new ContactInfoModel()
                {
                    UserId = usr.UserID,
                    Name = usr.FirstName + " " + usr.LastName,
                    HomePhone = String.Empty,
                    CellPhone = String.Empty,
                    ResidAddress1 = String.Empty,
                    ResidCity = String.Empty,
                    ResidState = String.Empty,
                    ResidZip = string.Empty,

                    MailAddress1 = String.Empty,
                    MailAddress2 = String.Empty,
                    MailCity = String.Empty,
                    MailState = String.Empty,
                    MailZip = String.Empty,
                    MailMayContact = false,

                    Email = String.Empty,
                    CaseManager = "data source undefined",
                    Clinic = "data source undefined",
                    Team = usr.GroupName
                    //Plan = "data source undefined",
                    //Group = "data source undefined"
                };

                //itemVariableId    identifier
                //3577	ADAP_D1_LastName
                //3578	ADAP_D1_FirstName
                //3464	ADAP_C1_Address
                //3465	ADAP_C1_City
                //3466	ADAP_C1_State
                //3467	ADAP_C1_Zip
                //3472	ADAP_C2_Address
                //3473	ADAP_C2_City
                //3474	ADAP_C2_State
                //3475	ADAP_C2_Zip
                //3478	ADAP_C3_Phone1_Num
                //3479	ADAP_C3_Phone1_Type
                //3480	ADAP_C3_Phone1_MayMsgYN
                //3481	ADAP_C3_Phone2_Num
                //3482	ADAP_C3_Phone2_Type
                //3483	ADAP_C3_Phone2_MayMsgYN
                //3484	ADAP_C4_MayCallYN
                //3485	ADAP_C4_Name
                //3486	ADAP_C4_Phone
                //3487	ADAP_C4_KnowHivYN
                //3488	ADAP_C5_HasCaseMngrYN
                //3489	ADAP_C5_Mngr1_Name
                //3490	ADAP_C5_Mngr1_Clinic
                //3491	ADAP_C5_Mngr2_Name
                //3492	ADAP_C5_Mngr2_Clinic
                //3493	ADAP_C5_CanReferYN

                // Pull data from the Application.
                //string[] identifiers = { "ADAP_C3_Phone1_Type", "ADAP_C3_Phone1_Num", "ADAP_C3_Phone2_Type", "ADAP_C3_Phone2_Num" };
                //for (int i = 0; i < identifiers.Length; i = i + 2)
                //{
                //    string typeValue = pullFormResponseByIdentifier(formResultId, identifiers[i]);
                //    if (typeValue.Equals("0"))
                //    {
                //        cim.HomePhone = pullFormResponseByIdentifier(formResultId, identifiers[i + 1]);
                //    }
                //    else if (!String.IsNullOrEmpty(typeValue))
                //    {
                //        cim.CellPhone = pullFormResponseByIdentifier(formResultId, identifiers[i + 1]);
                //    }
                //}
                //cim.Address1 = pullFormResponseByIdentifier(formResultId, "ADAP_C1_Address");
                //cim.City = pullFormResponseByIdentifier(formResultId, "ADAP_C1_City");
                //cim.State = pullFormResponseByIdentifier(formResultId, "ADAP_C1_State");
                //cim.Zip = pullFormResponseByIdentifier(formResultId, "ADAP_C1_Zip");
                cim.CaseManager = pullFormResponseByIdentifier(formResultId, "ADAP_C5_Mngr1_Name");

                // get user language, for pulling language-dependant lookupText entries
                CultureInfo ci = Thread.CurrentThread.CurrentUICulture;
                string region = (ci == null) ? "en" : ci.TwoLetterISOLanguageName.ToLower();
                int langId = formsRepo.GetLanguageByTwoLetterISOName(region).langId;

                // get the numerical "clinic" response and get text value from lookups
                cim.Clinic = String.Empty;
                string clinicDataValue = pullFormResponseByIdentifier(formResultId, "ADAP_M4_Clinic");
                def_LookupMaster lm = formsRepo.GetLookupMastersByLookupCode("ADAP_CLINIC");
                def_LookupDetail ld = formsRepo.GetLookupDetailByEnterpriseMasterAndDataValue(SessionHelper.LoginStatus.EnterpriseID, lm.lookupMasterId, clinicDataValue);
                if (ld != null)
                {
                    def_LookupText lt = formsRepo.GetLookupTextsByLookupDetailLanguage(ld.lookupDetailId, langId).FirstOrDefault();
                    if (lt != null)
                        cim.Clinic = lt.displayText;
                }

                // Check UAS for missing information.
                if (String.IsNullOrEmpty(cim.HomePhone) && (usr.Phones != null) && (usr.Phones.Count > 2))
                {
                    cim.HomePhone = String.IsNullOrEmpty(usr.Phones[2].Phone) ? String.Empty : usr.Phones[2].Phone;
                }

                if (String.IsNullOrEmpty(cim.CellPhone) && (usr.Phones != null) && (usr.Phones.Count > 0))
                {
                    cim.CellPhone = String.IsNullOrEmpty(usr.Phones[0].Phone) ? String.Empty : usr.Phones[0].Phone;
                }

                if (String.IsNullOrEmpty(cim.ResidAddress1) && usr.Addresses != null && usr.Addresses.Count > 0)
                {
                    UserDisplayAddress uda = usr.Addresses[0];
                    cim.ResidAddress1 = String.IsNullOrEmpty(uda.Address1) ? String.Empty : uda.Address1;
                    cim.ResidAddress2 = String.IsNullOrEmpty(uda.Address2) ? String.Empty : uda.Address2;
                    cim.ResidCity = String.IsNullOrEmpty(uda.City) ? String.Empty : uda.City;
                    cim.ResidState = String.IsNullOrEmpty(uda.State) ? String.Empty : uda.State;
                    cim.ResidZip = String.IsNullOrEmpty(uda.ZIP) ? String.Empty : uda.ZIP;
                    cim.ResidMayContact = uda.MayContactAddress;
                }

                if (String.IsNullOrEmpty(cim.MailAddress1) && usr.Addresses != null && usr.Addresses.Count > 1)
                {
                    UserDisplayAddress uda = usr.Addresses[1];
                    cim.MailAddress1 = String.IsNullOrEmpty(uda.Address1) ? String.Empty : uda.Address1;
                    cim.MailAddress2 = String.IsNullOrEmpty(uda.Address2) ? String.Empty : uda.Address2;
                    cim.MailCity = String.IsNullOrEmpty(uda.City) ? String.Empty : uda.City;
                    cim.MailState = String.IsNullOrEmpty(uda.State) ? String.Empty : uda.State;
                    cim.MailZip = String.IsNullOrEmpty(uda.ZIP) ? String.Empty : uda.ZIP;
                    cim.MailMayContact = uda.MayContactAddress;
                }

                if (String.IsNullOrEmpty(cim.Email) && (usr.Emails != null) && (usr.Emails.Count > 0))
                {
                    UserDisplayEmail ude = usr.Emails.Where(e => e.MayContactEmail).FirstOrDefault();
                    if (ude != null)
                        cim.Email = ude.Email;
                }

            }
            catch (Exception ex)
            {

                Debug.WriteLine("ContactInfo Exception: " + ex.Message);

                cim = new ContactInfoModel()
                {
                    Name = "Contact Info not found.",
                    HomePhone = string.Empty,
                    CellPhone = string.Empty,
                    ResidAddress1 = ex.Message,
                    ResidCity = string.Empty,
                    ResidState = string.Empty,
                    ResidZip = string.Empty,
                    Email = string.Empty,
                    CaseManager = string.Empty,
                    Clinic = string.Empty,
                    Team = "Unassigned",
                    //Plan = "Aetna Private Catistrophic Health",
                    //Group = "QWE123XCV"
                };

            }

            // Output the in JSON format
            fastJSON.JSONParameters param = new fastJSON.JSONParameters();
            param.EnableAnonymousTypes = true;
            string result = fastJSON.JSON.ToJSON(cim, param);

            return result;

        }

        // *** RRB This needs to be eliminated - the formsRepo method is returning 2 diff types of results.
        // *** The formsRepo methods needs to be refactored to return a consistent result.
        private string pullFormResponseByIdentifier(int formResultId, string identifier)
        {
            def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, identifier);
            if (rv != null)
            {
                return rv.rspValue;
            }

            return String.Empty;
        }

        /// <summary>
        /// Used to post data to the Missing Info modal
        /// </summary>
        /// <returns>Returns a string formatted for the Missing Info modal.</returns>
        [HttpGet]
        public string MissingInfo()
        {
            // This method needs to be updated with validation for additional information as required.
            if (!SessionHelper.IsUserLoggedIn)
            {
                Report1();
                return String.Empty;
            }

            int formResult = 0;
            List<string> missingFields = new List<string>();
            try
            {
                string strUserId = Request["userId"] as string;
                int userId = Convert.ToInt32(strUserId);

                // *** RRB - this is really the formResultId
                string strFormResult = Request["formResultId"] as string;
                formResult = Convert.ToInt32(strFormResult);

                Debug.WriteLine("MissingInfo userId: " + userId.ToString());

                AuthenticationClient webclient = new AuthenticationClient();
                UserDisplay usr = webclient.GetUserDisplay(userId);

                missingFields.Add(String.IsNullOrEmpty(usr.FirstName + usr.LastName) ? "Missing Name" : usr.FirstName + " " + usr.LastName);

                if ((usr.Phones == null) || (usr.Phones.Count == 0))
                {
                    missingFields.Add("Home Phone");
                }

                if ((usr.Addresses == null) || (usr.Addresses.Count == 0))
                {
                    missingFields.Add("Street Address");
                    missingFields.Add("City");
                    missingFields.Add("State");
                    missingFields.Add("Zip Code");
                }

                if ((usr.Emails == null) || (usr.Emails.Count == 0))
                {
                    missingFields.Add("Email Address");
                }

                missingFields.Add("Case Manager");
                missingFields.Add("Clinic");

                if (String.IsNullOrEmpty(usr.GroupName))
                {
                    missingFields.Add("Team Color");
                }
                missingFields.Add("Plan");
                missingFields.Add("Group");

                def_FormResults currFormResult = formsRepo.GetFormResultById(formResult);
                def_StatusLog statusLog = formsRepo.GetMostRecentStatusLogByStatusDetailToFormResultIdAndUserId(formsRepo.GetStatusDetailBySortOrder(1, currFormResult.formStatus).statusDetailId, formResult, SessionHelper.LoginStatus.UserID);

                if (statusLog != null)
                {
                    missingFields.Add("Status Note: " + statusLog.statusNote);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("MissingInfo Exception: " + ex.Message);
                missingFields.Add("Missing Name");
                missingFields.Add("Home Phone");
                missingFields.Add("Street Address");
                missingFields.Add("City");
                missingFields.Add("State");
                missingFields.Add("Zip Code");
                missingFields.Add("Email Address");
                missingFields.Add("Case Manager");
                missingFields.Add("Clinic");
                missingFields.Add("Team Color");
                missingFields.Add("Plan");
                missingFields.Add("Group");
            }


            // Output the in JSON format
            fastJSON.JSONParameters param = new fastJSON.JSONParameters();
            param.EnableAnonymousTypes = true;
            string result = fastJSON.JSON.ToJSON(missingFields, param);

            return result;
        }

        /// <summary>
        /// Used to post data to the Application Comment modal
        /// </summary>
        /// <returns>Returns a string formatted for the Application Comment Info modal.</returns>
        [HttpGet]
        public string AppCmmt()
        {
            // This method needs to be updated with validation for additional information as required.
            /*
            if (!SessionHelper.IsUserLoggedIn)
            {
                Report1();
                return String.Empty;
            }
            */

            int formResultId = 0;

            string result = string.Empty;
            try
            {
                string strFormResult = Request["formResultId"] as string;
                formResultId = Convert.ToInt32(strFormResult);
                Debug.WriteLine("AppCmmt formResultId: " + formResultId.ToString());

                Applications applications = new Applications(formsRepo);
                result = applications.GetFullCommentsReport(formResultId, true, @"<br/>", @"&nbsp;&nbsp;&nbsp;");

                if (String.IsNullOrEmpty(result))
                {
                    result = "There are no comments for this application.";
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("AppCmmt Exception: " + ex.Message);
                result = "Exception: " + ex.Message;
            }

            return result;
        }


        /// <summary>
        /// Web Service to process requests from the ADAP Report 1 List DataTable
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableApplicationsList(int draw, int start, int length, string searchValue, string searchRegex)
        {
            string result = String.Empty;
            Debug.WriteLine("Adap Reports Controller ApplicationsList draw:" + draw.ToString());
            Debug.WriteLine("Adap Reports Controller ApplicationsList start:" + start.ToString());
            Debug.WriteLine("Adap Reports Controller ApplicationsList searchValue:" + searchValue);
            Debug.WriteLine("Adap Reports Controller ApplicationsList searchRegex:" + searchRegex);

            //  Lists all the Request parameters
            foreach (string s in Request.Params.Keys)
            {
                Debug.WriteLine(s.ToString() + ":" + Request.Params[s]);
            }

            try
            {
                // Initialize the DataTable response (later transformed to JSON)
                DataTableResult dtr = new DataTableResult()
                {
                    draw = draw,
                    data = new List<string[]>(),
                    error = String.Empty
                };

                var sFName = Request.Params["columns[1][search][value]"];
                var sLName = Request.Params["columns[2][search][value]"];
                var sTeam = Request.Params["columns[3][search][value]"];
                var sStat = Request.Params["columns[4][search][value]"];
                var sDate = Request.Params["columns[5][search][value]"];
                var sType = Request.Params["columns[6][search][value]"];

                if (String.IsNullOrEmpty(sType))
                {
                    string s = Request["Type"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sType = s;
                    }
                }

                if (String.IsNullOrEmpty(sTeam))
                {
                    string s = Request["Team"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sTeam = s;
                    }
                }

                if (String.IsNullOrEmpty(sStat))
                {
                    string s = Request["Status"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sStat = s;
                    }
                }

                if (String.IsNullOrEmpty(sDate))
                {
                    string s = Request["Date"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sDate = s;
                    }
                }

                string sFullHistory = Request["showFullHistory"];
                bool showFullHistory = sFullHistory == null ? false : Convert.ToBoolean(sFullHistory);

                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;

                //get a queryable set of all applicable vFormResultUsers for enterprise, form identifiers, search criteria
                int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;
                Dictionary<int, string> formIdentifiersById = new Dictionary<int, string>();
                foreach (string formIdentifier in Request["FormIdentifiers"].Split(';'))
                {
                    int formId = formsRepo.GetFormByIdentifier(formIdentifier).formId;
                    formIdentifiersById.Add(formId, formIdentifier);
                }
                FullReportGrid reportGridHelper = new FullReportGrid(formsRepo, formIdentifiersById, showFullHistory);
                IQueryable<vFormResultUser> vfruQuery = formsRepo.GetFormResultsWithSubjects(enterpriseId, formIdentifiersById.Keys);//.OrderByDescending(fr => fr.statusChangeDate);
                vfruQuery = new Applications(formsRepo).SetVfruQueryParams(vfruQuery, sFName, sLName, sTeam, sStat, sDate, sType);

                Debug.WriteLine("Adap Reports Controller ApplicationsList SelectedEnterprise:" + SessionHelper.LoginStatus.EnterpriseID.ToString());

                //* * * OT 3/31/16 toList() here is necessary to allow OrderBy using Applications.GridReports.cs -> GetOrderingValueByColumnIndex()
                IEnumerable<vFormResultUser> vfruSearchResults = vfruQuery.ToList();
                IEnumerable<FullReportRow> adapGridRecords = reportGridHelper.BuildRowsFromVFormResultUsers(vfruSearchResults);
                dtr.recordsTotal = dtr.recordsFiltered = adapGridRecords.Count();

                //set ordering, if specified in request parameters
                if (Request.Params["order[0][column]"] != null)
                {
                    int orderColumnIndex = Convert.ToInt32(Request.Params["order[0][column]"]);
                    bool descending = Request.Params["order[0][dir]"] == "desc";
                    adapGridRecords = adapGridRecords.OrderBy(row => row.GetSortingValueForColumn(orderColumnIndex));
                    if (descending)
                        adapGridRecords = adapGridRecords.Reverse();
                }

                //generate html for the rows that will be visible on the screen
                foreach (FullReportRow thisRowRecord in adapGridRecords.Skip(iniIndex).Take(noDsplyRecs).ToList())
                {
                    //build html content for a single row
                    string[] data = reportGridHelper.GetHtmlForReportRow(thisRowRecord);
                    dtr.data.Add(data);
                }

                Debug.WriteLine("Adap Reports Controller ApplicationsList data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                Debug.WriteLine("Adap Reports Controller ApplicationsList result:" + result);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Reports Controller ApplicationsList exception:" + excptn.Message);
                string innerExcptnMsg = string.Empty;
                if ((excptn.InnerException != null) && (excptn.InnerException.Message != null))
                {
                    innerExcptnMsg = " * Inner Exception: " + excptn.InnerException.Message;
                    Debug.WriteLine("  InnerException: " + innerExcptnMsg);
                }

                result = excptn.Message + " - " + innerExcptnMsg;
            }

            return result;

        }

        /// <summary>
        /// Web Service to process requests from the ADAP Report 1 List DataTable for LA
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableApplicationsListLA(int draw, int start, int length, string searchValue, string searchRegex)
        {
            string result = String.Empty;
            Debug.WriteLine("Adap Reports Controller ApplicationsList draw:" + draw.ToString());
            Debug.WriteLine("Adap Reports Controller ApplicationsList start:" + start.ToString());
            Debug.WriteLine("Adap Reports Controller ApplicationsList searchValue:" + searchValue);
            Debug.WriteLine("Adap Reports Controller ApplicationsList searchRegex:" + searchRegex);

            //  Lists all the Request parameters
            foreach (string s in Request.Params.Keys)
            {
                Debug.WriteLine(s.ToString() + ":" + Request.Params[s]);
            }

            try
            {
                // Initialize the DataTable response (later transformed to JSON)
                DataTableResult dtr = new DataTableResult()
                {
                    draw = draw,
                    data = new List<string[]>(),
                    error = String.Empty
                };

                var sFName = Request.Params["columns[1][search][value]"];
                var sLName = Request.Params["columns[2][search][value]"];
                var sTeam = Request.Params["columns[3][search][value]"];
                var sStat = Request.Params["columns[4][search][value]"];
                var sDate = Request.Params["columns[5][search][value]"];
                var sType = Request.Params["columns[6][search][value]"];

                if (String.IsNullOrEmpty(sType))
                {
                    string s = Request["Type"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sType = s;
                    }
                }

                if (String.IsNullOrEmpty(sTeam))
                {
                    string s = Request["Team"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sTeam = s;
                    }
                }

                if (String.IsNullOrEmpty(sStat))
                {
                    string s = Request["Status"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sStat = s;
                    }
                }

                if (String.IsNullOrEmpty(sDate))
                {
                    string s = Request["Date"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sDate = s;
                    }
                }

                string sFullHistory = Request["showFullHistory"];
                bool showFullHistory = sFullHistory == null ? false : Convert.ToBoolean(sFullHistory);

                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;

                //get a queryable set of all applicable vFormResultUsers for enterprise, form identifiers, search criteria
                int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;
                Dictionary<int, string> formIdentifiersById = new Dictionary<int, string>();
                foreach (string formIdentifier in Request["FormIdentifiers"].Split(';'))
                {
                    int formId = formsRepo.GetFormByIdentifier(formIdentifier).formId;
                    formIdentifiersById.Add(formId, formIdentifier);
                }
                LAFullReportGrid reportGridHelper = new LAFullReportGrid(formsRepo, formIdentifiersById, showFullHistory);
                IQueryable<vFormResultUser> vfruQuery = formsRepo.GetFormResultsWithSubjects(enterpriseId, formIdentifiersById.Keys);//.OrderByDescending(fr => fr.statusChangeDate);
                vfruQuery = new Applications(formsRepo).SetVfruQueryParams(vfruQuery, sFName, sLName, sTeam, sStat, sDate, sType);

                Debug.WriteLine("Adap Reports Controller ApplicationsList SelectedEnterprise:" + SessionHelper.LoginStatus.EnterpriseID.ToString());

                //* * * OT 3/31/16 toList() here is necessary to allow OrderBy using Applications.GridReports.cs -> GetOrderingValueByColumnIndex()
                IEnumerable<vFormResultUser> vfruSearchResults = vfruQuery.ToList();
                IEnumerable<LAFullReportRow> adapGridRecords = reportGridHelper.BuildRowsFromVFormResultUsers(vfruSearchResults);
                dtr.recordsTotal = dtr.recordsFiltered = adapGridRecords.Count();

                //set ordering, if specified in request parameters
                if (Request.Params["order[0][column]"] != null)
                {
                    int orderColumnIndex = Convert.ToInt32(Request.Params["order[0][column]"]);
                    bool descending = Request.Params["order[0][dir]"] == "desc";
                    adapGridRecords = adapGridRecords.OrderBy(row => row.GetSortingValueForColumn(orderColumnIndex));
                    if (descending)
                        adapGridRecords = adapGridRecords.Reverse();
                }

                //generate html for the rows that will be visible on the screen
                foreach (LAFullReportRow thisRowRecord in adapGridRecords.Skip(iniIndex).Take(noDsplyRecs).ToList())
                {
                    //build html content for a single row
                    string[] data = reportGridHelper.GetHtmlForReportRow(thisRowRecord);
                    dtr.data.Add(data);
                }

                Debug.WriteLine("Adap Reports Controller ApplicationsList data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                Debug.WriteLine("Adap Reports Controller ApplicationsList result:" + result);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Reports Controller ApplicationsList exception:" + excptn.Message);
                string innerExcptnMsg = string.Empty;
                if ((excptn.InnerException != null) && (excptn.InnerException.Message != null))
                {
                    innerExcptnMsg = " * Inner Exception: " + excptn.InnerException.Message;
                    Debug.WriteLine("  InnerException: " + innerExcptnMsg);
                }

                result = excptn.Message + " - " + innerExcptnMsg;
            }

            return result;

        }

        /// <summary>
        /// Web Service to process requests from the ADAP AppOverview DataTable
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableApplicationOverview(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            string result = String.Empty;
            Debug.WriteLine("Adap Reports Controller ApplicationOverview draw:" + draw.ToString());
            Debug.WriteLine("Adap Reports Controller ApplicationOverview start:" + start.ToString());
            Debug.WriteLine("Adap Reports Controller ApplicationOverview searchValue:" + searchValue);
            Debug.WriteLine("Adap Reports Controller ApplicationOverview searchRegex:" + searchRegex);
            Debug.WriteLine("Adap Reports Controller ApplicationOverview order:" + order);

            //  Lists all the Request parameters
            foreach (string s in Request.Params.Keys)
            {
                Debug.WriteLine(s.ToString() + ":" + Request.Params[s]);
            }

            try
            {
                // Initialize the DataTable response (later transformed to JSON)
                DataTableResult dtr = new DataTableResult()
                {
                    draw = draw,
                    recordsTotal = 1,
                    recordsFiltered = 1,
                    data = new List<string[]>(),
                    error = String.Empty
                };

                int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;
                Dictionary<int, string> formIdentifiersById = new Dictionary<int, string>();
                foreach (string formIdentifier in Request["FormIdentifiers"].Split(';'))
                {
                    int formId = formsRepo.GetFormByIdentifier(formIdentifier).formId;
                    formIdentifiersById.Add(formId, formIdentifier);
                }
                IQueryable<vFormResultUser> query = formsRepo.GetFormResultsWithSubjects(enterpriseId, formIdentifiersById.Keys);

                var sType = Request.Params["columns[0][search][value]"];
                var sTeam = Request.Params["columns[1][search][value]"];
                var sStat = Request.Params["columns[2][search][value]"];
                var sDate = Request.Params["columns[3][search][value]"];

                // boolean to flag parameters which apply to the overview report, as opposed to process summary
                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;

                bool overview = true;
                if (String.IsNullOrEmpty(sDate))
                {
                    sDate = Request["Date"];
                    if (!String.IsNullOrEmpty(sDate) && sDate.Equals("Summary"))
                    {
                        overview = false;
                    }
                }

                string countURL = "<a href=\"/Adap/Report1?Team=sTeam&Status=sStatus\">:</a>";

                OverviewGrid reportHelper = new OverviewGrid(formsRepo, formIdentifiersById, overview);
                query = new Applications(formsRepo).SetVfruQueryParams(query, string.Empty, string.Empty, sTeam, sStat, sDate, sType);
                IEnumerable<OverviewRow> gridRecords = reportHelper.BuildRowsFromVFormResultUsers(query, countURL);

                Debug.WriteLine("Adap Reports Controller ApplicationOverview SelectedEnterprise:" + SessionHelper.LoginStatus.EnterpriseID.ToString());

                dtr.recordsTotal = dtr.recordsFiltered = gridRecords.Count();

                //set ordering, if specified in request parameters
                if (Request.Params["order[0][column]"] != null)
                {
                    int orderColumnIndex = Convert.ToInt32(Request.Params["order[0][column]"]);
                    bool descending = Request.Params["order[0][dir]"] == "desc";
                    gridRecords = gridRecords.OrderBy(row => row.GetSortingValueForColumn(orderColumnIndex));
                    if (descending)
                        gridRecords = gridRecords.Reverse();
                }

                //generate html for the rows that will be visible on the screen
                foreach (OverviewRow thisRowRecord in gridRecords.Skip(iniIndex).Take(noDsplyRecs))
                {
                    //build html content for a single row
                    string[] data = reportHelper.GetHtmlForReportRow(thisRowRecord);
                    dtr.data.Add(data);
                }

                //build html for the last display row, which just shows the total "count" for all rows, including rows on other pages
                int totalCount = gridRecords.Select(or => or.count).Sum();
                dtr.data.Add(new string[] { string.Empty, string.Empty, "Total", "<a href=\"/Adap/Report1 \">" + totalCount.ToString() + "</a>" });

                Debug.WriteLine("Adap Reports Controller ApplicationOverview data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                Debug.WriteLine("DataTableController ApplicationOverview result:" + result);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Reports Controller ApplicationOverview exception:" + excptn.Message);
                result = excptn.Message + " - " + excptn.Message;
            }

            return result;

        }

        /// <summary>
        /// Web Service to process requests from the Application Status DataTable
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableApplicationStatus(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            string result = String.Empty;
            Debug.WriteLine("Adap Reports Controller ApplicationsStatus draw:" + draw.ToString());
            Debug.WriteLine("Adap Reports Controller ApplicationsStatus start:" + start.ToString());
            Debug.WriteLine("Adap Reports Controller ApplicationsStatus searchValue:" + searchValue);
            Debug.WriteLine("Adap Reports Controller ApplicationsStatus searchRegex:" + searchRegex);
            Debug.WriteLine("Adap Reports Controller ApplicationsStatus order:" + order);

            //  Lists all the Request parameters
            foreach (string s in Request.Params.Keys)
            {
                Debug.WriteLine(s.ToString() + ":" + Request.Params[s]);
            }

            try
            {
                // Initialize the DataTable response (later transformed to JSON)
                DataTableResult dtr = new DataTableResult()
                {
                    draw = draw,
                    recordsTotal = 1,
                    recordsFiltered = 1,
                    data = new List<string[]>(),
                    error = String.Empty
                };

                int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;
                Dictionary<int, string> formIdentifiersById = new Dictionary<int, string>();
                foreach (string formIdentifier in Request["FormIdentifiers"].Split(';'))
                {
                    int formId = formsRepo.GetFormByIdentifier(formIdentifier).formId;
                    formIdentifiersById.Add(formId, formIdentifier);
                }
                IQueryable<vFormResultUser> query = formsRepo.GetFormResultsWithSubjects(enterpriseId, formIdentifiersById.Keys);

                var sType = Request.Params["columns[0][search][value]"];
                var sTeam = Request.Params["columns[1][search][value]"];
                var sStat = Request.Params["columns[2][search][value]"];
                var sDate = Request.Params["columns[3][search][value]"];
                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;

                if (String.IsNullOrEmpty(sType))
                {
                    sType = Request["Type"];
                }
                if (String.IsNullOrEmpty(sTeam))
                {
                    sTeam = Request["Team"];
                }
                if (String.IsNullOrEmpty(sStat))
                {
                    sStat = Request["Status"];
                }

                if (String.IsNullOrEmpty(sDate))
                {
                    sDate = Request["Date"];
                }

                string countURL = "";
                if (!String.IsNullOrEmpty(sDate) && sDate.Equals("Re-Certs Due"))
                {
                    countURL = "<a href=\"/Adap/ReCertsDue?Team=sTeam&Status=sStatus\">:</a>";
                }
                else
                {
                    countURL = "<a href=\"/Adap/ApplicationsPending?Team=sTeam&Status=sStatus\">:</a>";
                }

                StatusGrid reportHelper = new StatusGrid(formsRepo, formIdentifiersById);
                query = new Applications(formsRepo).SetVfruQueryParams(query, string.Empty, string.Empty, sTeam, sStat, sDate, sType);
                IEnumerable<OverviewRow> gridRecords = reportHelper.BuildRowsFromVFormResultUsers(query, countURL);
                dtr.recordsTotal = dtr.recordsFiltered = gridRecords.Count();

                //set ordering, if specified in request parameters
                if (Request.Params["order[0][column]"] != null)
                {
                    int orderColumnIndex = Convert.ToInt32(Request.Params["order[0][column]"]);
                    bool descending = Request.Params["order[0][dir]"] == "desc";
                    gridRecords = gridRecords.OrderBy(row => row.GetSortingValueForColumn(orderColumnIndex));
                    if (descending)
                        gridRecords = gridRecords.Reverse();
                }

                //generate html for the rows that will be visible on the screen
                foreach (OverviewRow thisRowRecord in gridRecords.Skip(iniIndex).Take(noDsplyRecs))
                {
                    //build html content for a single row
                    string[] data = reportHelper.GetHtmlForReportRow(thisRowRecord);
                    dtr.data.Add(data);
                }

                //build html for the last display row, which just shows the total "count" for all rows, including rows on other pages
                int totalCount = gridRecords.Select(or => or.count).Sum();
                if (!String.IsNullOrEmpty(sDate) && sDate.Equals("Re-Certs Due"))
                {
                    dtr.data.Add(new string[] { String.Empty, String.Empty, "Total", "<a href=\"/Adap/RecertsDue\">" + totalCount.ToString() + "</a>" });
                }
                else
                {
                    dtr.data.Add(new string[] { String.Empty, String.Empty, "Total", "<a href=\"/Adap/ApplicationsPending?Status=" + sStat + " \">" + totalCount.ToString() + "</a>" });
                }

                Debug.WriteLine("Adap Reports Controller ApplicationsStatus data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                Debug.WriteLine("DataTableController ApplicationsStatus result:" + result);

            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Reports Controller ApplicationsStatus exception:" + excptn.Message);
                result = excptn.Message + " - " + excptn.Message;
            }

            return result;
        }

        /// <summary>
        /// Web Service to process requests from the Comments DataTable
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        public string DataTableCommentsList(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            string result = String.Empty;
            Debug.WriteLine("Adap Reports Controller CommentsList draw:" + draw.ToString());
            Debug.WriteLine("Adap Reports Controller CommentsList start:" + start.ToString());
            Debug.WriteLine("Adap Reports Controller CommentsList searchValue:" + searchValue);
            Debug.WriteLine("Adap Reports Controller CommentsList searchRegex:" + searchRegex);
            Debug.WriteLine("Adap Reports Controller CommentsList order:" + order);

            //  Lists all the Request parameters
            foreach (string s in Request.Params.Keys)
            {
                Debug.WriteLine(s.ToString() + ":" + Request.Params[s]);
            }

            try
            {
                // Initialize the DataTable response (later transformed to JSON)
                DataTableResult dtr = new DataTableResult()
                {
                    draw = draw,
                    recordsTotal = 1,
                    recordsFiltered = 1,
                    data = new List<string[]>(),
                    error = String.Empty
                };


                var sFormResultId = Request.Params["columns[1][search][value]"];

                if (String.IsNullOrEmpty(sFormResultId))
                {
                    string s = Request["formResultId"];
                    if (!String.IsNullOrEmpty(s))
                    {
                        sFormResultId = s;
                    }
                }

                IQueryable<def_StatusLog> query = formsRepo.GetStatusLogsForFormResultId(Convert.ToInt32(sFormResultId));

                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;

                //query = setParams(query, sFName, sLName, sTeam, sStat, sDate);

                AuthenticationClient webclient = new AuthenticationClient();

                int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;
                Debug.WriteLine("Adap Reports Controller CommentsList SelectedEnterprise:" + enterpriseId.ToString());

                dtr.recordsTotal = dtr.recordsFiltered = query.Count();

                foreach (def_StatusLog q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                {
                    // First line should display the Ramsell ID, which would not exist on default.  
                    // Code to check the database should be added at a later date.
                    UserDisplay usr = webclient.GetUserDisplay(Convert.ToInt32(q.UserID));
                    string from = formsRepo.GetStatusText(Convert.ToInt32(q.statusDetailIdFrom), enterpriseId, 1).displayText;
                    string to = formsRepo.GetStatusText(Convert.ToInt32(q.statusDetailIdTo), enterpriseId, 1).displayText;
                    def_FileAttachment snapshot = formsRepo.GetFileAttachment(q.statusLogId, 2);
                    string[] data = new string[] {
                        (q.statusLogDate != null)               ? q.statusLogDate.ToString()    : String.Empty,
                        (usr != null)                           ? usr.UserName                  : q.UserID.ToString(),
                        (!String.IsNullOrEmpty(from))           ? from                          : q.statusDetailIdFrom.ToString(),
                        (!String.IsNullOrEmpty(to))             ? to                            : q.statusDetailIdTo.ToString(),
                        (!String.IsNullOrEmpty(q.statusNote))   ? q.statusNote                  : String.Empty,
                        (snapshot != null)                      ? "<a href=\"/Search/DownloadFile?fileId=" + snapshot.FileId + "&fileDownloadName=snapshot.pdf\">Download Snapshot</a>" : "N/A"
                    };
                    dtr.data.Add(data);
                }

                Debug.WriteLine("Adap Reports Controller CommentsList data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                Debug.WriteLine("Adap Reports Controller CommentsList result:" + result);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Reports Controller CommentsList exception:" + excptn.Message);
                result = excptn.Message + " - " + excptn.Message;
            }

            return result;

        }


        /// <summary>
        /// Web Service to process requests from the ADAP Portal DataTable
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTablePortal(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            string result = String.Empty;
            Debug.WriteLine("Adap Reports Controller DateTablePortal draw:" + draw.ToString());
            Debug.WriteLine("Adap Reports Controller DateTablePortal start:" + start.ToString());
            Debug.WriteLine("Adap Reports Controller DateTablePortal searchValue:" + searchValue);
            Debug.WriteLine("Adap Reports Controller DateTablePortal searchRegex:" + searchRegex);
            Debug.WriteLine("Adap Reports Controller DateTablePortal order:" + order);

            //  Lists all the Request parameters
            foreach (string s in Request.Params.Keys)
            {
                Debug.WriteLine(s.ToString() + ":" + Request.Params[s]);
            }

            try
            {
                // Initialize the DataTable response (later transformed to JSON)
                DataTableResult dtr = new DataTableResult()
                {
                    draw = draw,
                    recordsTotal = 1,
                    recordsFiltered = 1,
                    data = new List<string[]>(),
                    error = String.Empty
                };

                int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;
                List<int> formIds = new List<int>();
                foreach (string formIdentifier in Request["FormIdentifiers"].Split(';'))
                {
                    int formId = formsRepo.GetFormByIdentifier(formIdentifier).formId;
                    formIds.Add(formId);
                }
                IQueryable<vFormResultUser> query = formsRepo.GetFormResultsWithSubjects(enterpriseId, formIds);

                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;

                int userId = Convert.ToInt32(Request["UserId"]);
                int cancel = Convert.ToInt32(formsRepo.GetStatusDetailByMasterIdentifier(1, "CANCELLED").sortOrder);

                if (UAS.Business.UAS_Business_Functions.hasPermission(2, "RptsExpts"))
                {
                    query = query.Where(q => q.interviewer == SessionHelper.LoginStatus.UserID);
                }
                else
                {
                    query = query.Where(q => q.subject == userId && (q.formStatus != cancel));
                }

                dtr.recordsTotal = dtr.recordsFiltered = query.Count();

                foreach (var q in query.Skip(iniIndex).Take(noDsplyRecs))
                {
                    string ramsellIdDisplayText = pullFormResponseByIdentifier(q.formResultId, "ADAP_D9_Ramsell");
                    if (String.IsNullOrWhiteSpace(ramsellIdDisplayText))
                        ramsellIdDisplayText = @Resources.AdapPortal.NoRamsellId;

                    // First line should display the Ramsell ID, which would not exist on default.  Code to check the database should be added at a later date.
                    string[] data = new string[] {
                        "<a href=\"/ADAP/ToTemplate?formResultId=" + q.formResultId.ToString() + "\">" + ramsellIdDisplayText + "</a>",
                        q.statusChangeDate.ToString(),
                        //(AdapApprovals.GetAdapStatusList(formsRepo).Count() > q.formStatus) ? AdapApprovals.GetAdapStatusList(formsRepo)[q.formStatus] : q.formStatus.ToString()
                        formsRepo.GetStatusTextByDetailSortOrder(1, q.formStatus).displayText
                    };
                    dtr.data.Add(data);
                }

                Debug.WriteLine("Adap Reports Controller DateTablePortal data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                Debug.WriteLine("Adap Reports Controller DateTablePortal result:" + result);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Reports Controller DateTablePortal exception:" + excptn.Message);
                result = excptn.Message + " - " + excptn.Message;
            }

            return result;
        }

        [HttpPost]
        public string DataTablePeopleList(int draw, int start, int length, string searchValue, string searchRegex,
                                        string order)
        {
            string result = String.Empty;
            Debug.WriteLine("DataTableController UserList draw:" + draw.ToString());
            Debug.WriteLine("DataTableController UserList start:" + start.ToString());
            Debug.WriteLine("DataTableController UserList searchValue:" + searchValue);
            Debug.WriteLine("DataTableController UserList searchRegex:" + searchRegex);
            Debug.WriteLine("DataTableController UserList order:" + order);

            //  Lists all the Request parameters
            foreach (string s in Request.Params.Keys)
            {
                Debug.WriteLine(s.ToString() + ":" + Request.Params[s]);
            }

            try
            {
                // Initialize the DataTable response (later transformed to JSON)
                DataTableResult dtr = new DataTableResult()
                {
                    draw = draw,
                    recordsTotal = 1,
                    recordsFiltered = 1,
                    data = new List<string[]>(),
                    error = String.Empty
                };

                var sFirstN = Request.Params["columns[1][search][value]"];
                var sLastN = Request.Params["columns[2][search][value]"];
                var sAdapId = Request.Params["columns[3][search][value]"];
                var sDob = Request.Params["columns[4][search][value]"];
                var sSsn = Request.Params["columns[5][search][value]"];
                var sEsNumber = Request.Params["columns[6][search][value]"];

                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;


                // Parse the parameters from DataTable request
                /*
                if (!String.IsNullOrEmpty(json))
                {
                    var dResults = fastJSON.JSON.Parse(json);
                }
                */

                AuthenticationClient auth = new AuthenticationClient();

                int entId = SessionHelper.LoginStatus.EnterpriseID;
                int appId = 3;

                List<List<string>> usrs = auth.List_people_Users(SessionHelper.LoginStatus.UserID, entId, iniIndex, noDsplyRecs,
                    sFirstN, sLastN, sAdapId, sSsn, sEsNumber, sDob);

                //* * * OT 2-23-16 explaination for Bug 13019, comment 21, first bullet
                //for users that do not have the "View users" permission, they can only see themselves in the limited users list
                //we do this by searching for the current user's loginId
                //this normally works, but in some cases there are multiple loginIds that contain the current user's loginId 
                //Example: Login as nctest and you can also see NCTest1.


                //Final record stores total result set count.
                if (usrs.Count > 0)
                {
                    dtr.recordsTotal = dtr.recordsFiltered = Convert.ToInt32(usrs[usrs.Count - 1][0]);//usrCount;
                    usrs.RemoveAt(usrs.Count - 1);
                    Debug.WriteLine("DataTableController usrs.Count:" + usrs.Count.ToString());
                }
                else
                {
                    dtr.recordsTotal = dtr.recordsFiltered = 0;
                    Debug.WriteLine("DataTableController usrs.Count:" + 0);
                }
                dtr.data = (from u in usrs
                            select u.ToArray()).ToList();

                Debug.WriteLine("DataTableController UserList data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                Debug.WriteLine("DataTableController UserList result:" + result);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("DataTableController UserList exception:" + excptn.Message);
                result = excptn.Message + " - " + excptn.InnerException.Message;
            }

            return result;

        }
    }
}