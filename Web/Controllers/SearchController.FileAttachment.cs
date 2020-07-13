using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using Assmnts.Business.Uploads;
using Assmnts.Infrastructure;
using Assmnts.Models;
using UAS.Business;

using Data.Concrete;
using Assmnts.Business;
using AJBoggs.Sis.Domain;
using System.IO;

namespace Assmnts.Controllers
{
    /// <summary>
    /// This class handles File Attachments handled through the SIS Search Screen.
    /// </summary>
    public partial class SearchController : Controller
    {
        [HttpGet]
        public string getFileDisplayText(int formResultId)
        {
            int RelatedEnumId = formsRepo.GetRelatedEnumIdByEnumDescription("formResultId");
            int AttachTypeId = formsRepo.GetAttachTypeIdByAttachDescription("Generic Upload");
            Dictionary<int, string> displayTexts = FileUploads.RetrieveDistinctFileDisplayTextsByRelatedId(formsRepo, formResultId, RelatedEnumId, "A", AttachTypeId);
            string optionList = String.Empty;
            if (displayTexts.Count > 0)
            {
                optionList = "<select class=\"filesDDL" + formResultId + "\" style=\"min-width:100px\">";
                foreach (int key in displayTexts.Keys)
                {
                    optionList += "<option value=\"" + key + "\">" + displayTexts[key] + "</option>";
                }
                optionList += "</select>";
            }
            else
            {
                optionList = "<label style=\"min-width:100px\">No records found.</label>";
            }

            return optionList;
        }

        /// <summary>
        /// Originally intended to intercept the form submit for a file Upload and check if the file name exists so the user
        /// can be prompted to confirm overwriting the old file.
        /// 3/21 BR I could not figure out how to pull the formResultId and fileName to get this to work.  Fix that and this will work fine.
        /// </summary>
        /// <param name="formCollection"></param>
        /// <returns></returns>
        [HttpPost]
        public bool UploadCheck(FormCollection formCollection)
        {
            bool newFile = true;
            int RelatedEnumId = formsRepo.GetRelatedEnumIdByEnumDescription("formResultId");
            int AttachTypeId = formsRepo.GetAttachTypeIdByAttachDescription("Generic Upload");

            string data = Request["arrayData"];
            int formResultId = Convert.ToInt32(formCollection["formResultId"]);
            System.Web.HttpPostedFileWrapper file = (System.Web.HttpPostedFileWrapper)Request.Files["file" + formResultId.ToString()];
            def_FormResults fr = formsRepo.GetFormResultById(formResultId);

            Dictionary<int, string> texts = FileUploads.RetrieveFileDisplayTextsByRelatedId(formsRepo, formResultId, RelatedEnumId, "A", AttachTypeId);
            foreach (int k in texts.Keys)
            {
                if (file.FileName.Equals(texts[k]))
                {
                    newFile = false;
                }
            }

            return newFile;
        }

        [HttpGet]
        public bool hasFiles()
        {
            int formResultId = Convert.ToInt32(Request["formResultId"]);
            int RelatedEnumId = formsRepo.GetRelatedEnumIdByEnumDescription("formResultId");
            int AttachTypeId = formsRepo.GetAttachTypeIdByAttachDescription("Generic Upload");
            bool hasFiles = formsRepo.GetFileAttachmentDisplayText(formResultId, RelatedEnumId, "A", AttachTypeId).Count() > 0;

            return hasFiles;
        }

        [HttpPost]
        public bool UploadFile(FormCollection formCollection)
        {
            bool edit = UAS_Business_Functions.hasPermission(UAS.Business.PermissionConstants.EDIT, UAS.Business.PermissionConstants.ASSMNTS);
            bool editlocked = UAS_Business_Functions.hasPermission(UAS.Business.PermissionConstants.EDIT_LOCKED, UAS.Business.PermissionConstants.ASSMNTS);
            int fileId = -1;

            if (edit || editlocked)
            {
                int RelatedEnumId = formsRepo.GetRelatedEnumIdByEnumDescription("formResultId");
                int AttachTypeId = formsRepo.GetAttachTypeIdByAttachDescription("Generic Upload");

                int formResultId = Convert.ToInt32(formCollection["formResultId"]);
                System.Web.HttpPostedFileWrapper file = (System.Web.HttpPostedFileWrapper)Request.Files["file" + formResultId.ToString()];
                def_FormResults fr = formsRepo.GetFormResultById(formResultId);

                DateTime now = DateTime.Now;
                string date = now.Year.ToString() + ((now.Month < 10) ? "0" : "") + now.Month.ToString() + ((now.Day < 10) ? "0" : "") + now.Day.ToString();
                string time = ((now.Hour < 10) ? "0" : "") + now.Hour.ToString() + ((now.Minute < 10) ? "0" : "") + now.Minute.ToString() + ((now.Second < 10) ? "0" : "") + now.Second.ToString();
                string subDir = date + Path.DirectorySeparatorChar + time;

                fileId = FileUploads.CreateAttachment(formsRepo, file, null, subDir, formResultId, RelatedEnumId, AttachTypeId);

                // check if the file uploaded has a duplicated displayText for this assessment.  i.e., a logical overwrite.
                if (fileId > -1)
                {
                    def_FileAttachment fa = formsRepo.GetFileAttachment(fileId);
                    Dictionary<int, string> texts = FileUploads.RetrieveFileDisplayTextsByRelatedId(formsRepo, formResultId, RelatedEnumId, "A", AttachTypeId);

                    foreach (int k in texts.Keys)
                    {
                        if (texts[k].Equals(fa.displayText) && k != fa.FileId)
                        {
                            FileUploads.DeleteFile(formsRepo, k);
                        }
                    }
                }
            }

            if (fileId > -1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [HttpGet]
        public FileContentResult DownloadFile()
        {
            int fileId = Convert.ToInt32(Request["fileId"]);
            string fileDownloadName = Request["fileDownloadName"];

            FileContentResult result = FileUploads.RetrieveFile(formsRepo, fileId);
            if (fileDownloadName != null)
                result.FileDownloadName = fileDownloadName;

            return result;
        }

        [HttpGet]
        public void DeleteFile()
        {
            bool edit = UAS_Business_Functions.hasPermission(UAS.Business.PermissionConstants.EDIT, UAS.Business.PermissionConstants.ASSMNTS);
            bool editlocked = UAS_Business_Functions.hasPermission(UAS.Business.PermissionConstants.EDIT_LOCKED, UAS.Business.PermissionConstants.ASSMNTS);

            if (edit || editlocked)
            {
                int RelatedEnumId = formsRepo.GetRelatedEnumIdByEnumDescription("formResultId");

                int formResultId = Convert.ToInt32(Request["formResultId"]);
                int fileId = Convert.ToInt32(Request["fileId"]);
                def_FormResults fr = formsRepo.GetFormResultById(formResultId);

                if (!fr.locked || (fr.locked && editlocked))
                {
                    bool result = FileUploads.DeleteFile(formsRepo, fileId);
                }
            }
        }
    }
}