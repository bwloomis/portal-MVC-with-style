using AJBoggs.Adap.Domain;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Assmnts.UasServiceRef;
using Data.Abstract;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace Assmnts.Controllers
{
    [ADAPRedirectingAction]
    public partial class AdapController : Controller
    {

        private IFormsRepository formsRepo;

        /// <summary>
        /// Constructor for the AdapController
        /// </summary>
        /// <param name="fr">FormsRepository database Interface</param>
        public AdapController(IFormsRepository fr)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
        }

        public ActionResult TestAdapCa()
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            if (SessionHelper.SessionForm == null)
            {
                SessionHelper.SessionForm = new SessionForm();
            }

            // retrieve and set SessionForm params
            string formIdent = "ADAP-CA", entName = "California";
            def_Forms frm = formsRepo.GetFormByIdentifier(formIdent);
            Enterprise ent = new AuthenticationClient().GetEnterpriseByName(entName);
            if (frm == null)
                return Content("Could not find form with identifier \"" + formIdent + "\"");

            if (ent == null)
                return Content("Could not find enterprise with name \"" + entName + "\"");

            def_FormResults fr = formsRepo.GetFormResultsByFormId(frm.formId).FirstOrDefault();
            if (fr == null)
            {
                fr = new def_FormResults()
                {
                    formId = frm.formId,
                    formStatus = 0,
                    sessionStatus = 0,
                    dateUpdated = DateTime.Now,
                    deleted = false,
                    locked = false,
                    archived = false,
                    EnterpriseID = ent.EnterpriseID,
                    GroupID = 0,
                    subject = 0,
                    interviewer = 0,
                    assigned = 0,
                    training = false,
                    reviewStatus = 0,
                    statusChangeDate = DateTime.Now
                };

                formsRepo.AddFormResult(fr);
            }


            SessionHelper.SessionForm.formId = frm.formId;
            SessionHelper.SessionForm.formResultId = fr.formResultId;
            SessionHelper.SessionForm.formIdentifier = frm.identifier;
            SessionHelper.LoginStatus.EnterpriseID = ent.EnterpriseID;


            def_Parts prt = formsRepo.GetFormParts(frm)[0];
            SessionHelper.SessionForm.partId = prt.partId;
            Session["part"] = prt.partId.ToString();
            def_Sections sct = formsRepo.GetSectionsInPart(prt)[0];
            Session["section"] = sct.sectionId.ToString();

            return RedirectToAction("Template", "Results", new { sectionId = sct.sectionId.ToString(), partId = SessionHelper.SessionForm.partId.ToString() });
        }

        /// <summary>
        /// Default View for Controller, redirects to enterprise-specific index
        /// </summary>
        public ActionResult Index()
        {
            string controllerName = GetEnterpriseSpecificAdapControllerName(SessionHelper.LoginStatus.EnterpriseID);
            return RedirectToAction("Index", controllerName );
        }

        public ActionResult GridPage()
        {
            return View();
        }

        private string GetEnterpriseSpecificAdapControllerName(int entId)
        {
            switch (entId)
            {
                case 7:
                    return "LAADAP";
                case 8:
                    return "AdapCa";
                case 5:
                default:
                    return "COADAP";
            }
        }

        /// <summary>
        /// Connects to Secure Email to send emails.
        /// </summary>
        /// <param name="entId">The current enterprise of the user recieving emails.</param>
        /// <param name="email">String in JSON format to be processed by Secure Email</param>
        /// <returns>Returns true when the email is successfully sent.</returns>
        private bool sendSecureEmail(int entId, string email)
        {
            SecureMessaging.SecureMessagingClient smClient = new SecureMessaging.SecureMessagingClient();
            smClient.AdapAutomation(email);
            return true;
        }

        [HttpGet]
        public JsonResult PovertyGuideline(int persons, int year)
        {
            var context = new formsEntities();
            int numPersons = persons;
            if (persons > 8)
            {
                numPersons = 8;
            }
            var guideline = from p in context.PovertyGuidelines
                            where p.year == year && p.Persons == numPersons
                            select p;
            if (guideline != null)
            {
                var amount = guideline.FirstOrDefault().Guideline;
                if (persons > 8)
                {
                    var extra = (persons - 8) * 4160;
                    amount += extra;
                }

                return Json(new { amount = amount }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { amount = 0 }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// The main page loaded for non-staff users.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult AdapPortal()
        {
            String userId = Request["userId"];
            String err = Request["error"];

            if (!String.IsNullOrEmpty(err) && err.Equals("Not Approved"))
            {
                err = "New Applications cannot be created while an application is being processed.";
            }
            int uId;
            UAS.DataDTO.LoginStatus ls = SessionHelper.LoginStatus;
            if (String.IsNullOrEmpty(userId))
            {
                uId = ls.UserID;
            }
            else
            {
                try
                {
                    uId = Convert.ToInt32(userId);
                }
                catch (Exception ex)
                {
                    uId = 0;
                    Debug.WriteLine("Adap Controller AdapPortal exception:" + ex.Message);
                }
            }
            AuthenticationClient webclient = new AuthenticationClient();
            UserDisplay ud = webclient.GetUserDisplay(uId);

            IQueryable<vFormResultUser> query = formsRepo.GetFormResultsWithSubjects(SessionHelper.LoginStatus.EnterpriseID, 6);
            int? soInt = formsRepo.GetStatusDetailByMasterIdentifier(1, "APPROVED").sortOrder;
            query = query.Where(q => (q.subject == uId) && (q.formStatus == soInt));

            String recert;
            if (query.Count() > 0)
            {
                DateTime dob = Convert.ToDateTime(ud.DOB);
                recert = new Applications(formsRepo).GetRecert(dob, query.Count(), Convert.ToDateTime(query.OrderByDescending(q => q.statusChangeDate).Select(q => q.statusChangeDate).FirstOrDefault())).ToString("MMMM yyyy");
            }
            else
            {
                recert = "None Pending";
            }

            AdapPortal ap = new AdapPortal()
            {
                Name = ud.FirstName + " " + ud.LastName,
                RecertDate = recert,
                UserId = uId.ToString(),
                errorMsg = err,
                EnterpriseID = SessionHelper.LoginStatus.EnterpriseID
            };

            return View("~/Views/Templates/ADAP/AdapPortal.cshtml", ap);
        }
    }


}