using Assmnts.Infrastructure;
using Assmnts.Models;
using Data.Abstract;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Assmnts.Business.Uploads;
using System.IO;

namespace Assmnts.Controllers
{
    public class UploadLookupDataController : Controller
    {
        private IFormsRepository mFormsRepository;

        public UploadLookupDataController(IFormsRepository formsRepository) {
            mFormsRepository = formsRepository;
        }

        // GET: UploadLookupData
        public ActionResult Index()
        {
            AdapAdmin model = new AdapAdmin();
            model.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;

            int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;

            def_LookupMaster lookupMaster = mFormsRepository.GetLookupMastersByLookupCode("ADAP_CLINIC");

            int lookupMasterId = 0;

            if (lookupMaster != null)
            {
                lookupMasterId = lookupMaster.lookupMasterId;
            }
            
            model.enterpriseId = enterpriseId;
            model.groupId = 0;
            model.lookupMasterId = lookupMasterId;
            model.statusFlag = true;
            
            return View("~/Views/COADAP/UploadLookupData.cshtml", model);
        }

        public ActionResult Upload(HttpPostedFileBase file)
        {
            AdapAdmin model = new AdapAdmin();
            model.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;

            int enterpriseId = SessionHelper.LoginStatus.EnterpriseID;

            def_LookupMaster lookupMaster = mFormsRepository.GetLookupMastersByLookupCode("ADAP_CLINIC");

            int lookupMasterId = 0;

            if (lookupMaster != null)
            {
                lookupMasterId = lookupMaster.lookupMasterId;
            }

            model.enterpriseId = enterpriseId;
            model.groupId = 0;
            model.lookupMasterId = lookupMasterId;
            model.statusFlag = true;
            
            if (file.ContentLength > 0)
            {
                var fileName = Path.GetFileName(file.FileName);
                var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName);
                file.SaveAs(path);
   
                LookupUploads lookupUploads = new LookupUploads();

                model.Message = "Upload successful";

                try
                {
                    lookupUploads.UploadLookups(enterpriseId, SessionHelper.LoginStatus.UserID, mFormsRepository, path);
                }
                catch (Exception ex)
                {
                    model.Message = "Upload error: " + ex.Message;
                    return View("~/Views/COADAP/UploadLookupData.cshtml", model);

                }
            }
            else
            {
                model.Message = "Upload error: No file selected.";
                return View("~/Views/COADAP/UploadLookupData.cshtml", model);
            }

            return RedirectToAction("Clinic", "ADAP");
        }
    }
}