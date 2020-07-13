using AJBoggs.Sis.Domain;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web.Mvc;



namespace Assmnts.Controllers
{
    /*
     * This controller is used to display and save formResult data from the template screens.
     * The templates are in sub-dirs to Views/Templates such as Views/Templates/SIS
     * 
     */
    [RedirectingAction]
    public partial class TestController : Controller
    {

        //private AuthenticationClient auth = new AuthenticationClient();
        private IFormsRepository formsRepo;

        public TestController(IFormsRepository fr)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
        }

        [HttpGet]
        public ActionResult Index()
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
            string formIdent = "View_Test_Form";
            def_Forms frm = formsRepo.GetFormByIdentifier(formIdent);
            if (frm == null)
                return Content("Could not find form with identifier \"" + formIdent + "\"" );
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
                    EnterpriseID = 0,
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
            SessionHelper.LoginStatus.EnterpriseID = 0;

            def_Parts prt = formsRepo.GetFormParts(frm)[0];
            SessionHelper.SessionForm.partId = prt.partId;
            Session["part"] = prt.partId;

            def_Sections sct = formsRepo.GetSectionsInPart(prt)[0];

            return RedirectToAction("Template", "Results", new { sectionId = sct.sectionId.ToString(), partId = SessionHelper.SessionForm.partId.ToString() });
        }

    }
}
