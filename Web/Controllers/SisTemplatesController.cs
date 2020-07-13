using AJBoggs.Sis.Domain;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using UAS.Business;

namespace Assmnts.Controllers
{
    [RedirectingAction]
    public partial class SisTemplatesController : Controller
    {

        private IFormsRepository formsRepo;
        
        public SisTemplatesController(IFormsRepository fr)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
        }

        
        public void SaveFormResults(def_FormResults frmRes)
        {
            formsRepo.SaveFormResults(frmRes);
        }

        [HttpPost]
        public bool ChangeToInProgress()
        {
            if (SessionHelper.SessionForm == null)
            {
                return false;
            }

            def_FormResults frmRslt = formsRepo.GetFormResultById(SessionHelper.SessionForm.formResultId);
            if (frmRslt == null)
            {
                return false;
            }

            frmRslt.formStatus = (byte)FormResults_formStatus.IN_PROGRESS;
            try
            {
                SaveFormResults(frmRslt);
            }
            catch
            {
                return false;
            }

            return true;

        }
        
        [HttpPost]
        public bool saveInterviewer(int? intId)
        {
            if (intId == null)
            {
                return false;
            }

            if (SessionHelper.SessionForm == null)
            {
                SessionHelper.SessionForm = new SessionForm();
            }
            SessionForm sessionForm = SessionHelper.SessionForm;

            def_FormResults frmRslt = formsRepo.GetFormResultById(sessionForm.formResultId);

            if (frmRslt == null)
            {
                string paramFormId = Request["formId"] as string;

                Session["formId"] = paramFormId;
                int formId = Convert.ToInt32(paramFormId);

                frmRslt = new def_FormResults()
                {
                    formId = formId,
                    formStatus = 0,
                    sessionStatus = 0,
                    dateUpdated = DateTime.Today
                };
                int frmRsId = formsRepo.AddFormResult(frmRslt);
                frmRslt = formsRepo.GetFormResultById(frmRsId);
            }

            frmRslt.interviewer = intId;

            bool ventureMode = SessionHelper.IsVentureMode;
            
            if ((frmRslt.formStatus == (byte)FormResults_formStatus.NEW) && !ventureMode)
            {
                frmRslt.assigned = intId;
            }

            try
            {
                SaveFormResults(frmRslt);
            }
            catch
            {
                return false;
            }

            return true;

        }
        

    }
}