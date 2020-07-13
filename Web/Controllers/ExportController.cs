using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

using Assmnts.Models;

using Data.Abstract;
using Data.Concrete;
using Assmnts.Infrastructure;


namespace Assmnts.Controllers
{
    /*
     * This controller is used to download data.
     * 
     *   ************ Many of the methods towards the end of the code need to be removed once the Exports are done.
     */
    [RedirectingAction]
    public partial class ExportController : Controller
    {

        private IFormsRepository formsRepo;

        public ExportController(IFormsRepository fr)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
        }

        [HttpGet]
        public ActionResult Index()
        {
            Session["userId"] = "0";
            Debug.WriteLine("* * *  ExportController:Index method  * * *");
            // Initialize the session variables
            SessionForm sf = (SessionForm)Session["sessionForm"];
            if (sf == null)
            {
                sf = new SessionForm()
                {
                    formId = 0,
                    partId = 0,
                    sectionId = 0,
                    formResultId = 0
                };
                Session["sessionForm"] = sf;
            }

            // Display the formResults (Assessments)
            Assmnts.Models.FormResults frmRsltsMdl = new Assmnts.Models.FormResults();
            frmRsltsMdl.formResults = new List<def_FormResults>();
            foreach (def_Forms frm in formsRepo.GetAllForms())
            {
                frmRsltsMdl.formResults.AddRange(formsRepo.GetFormResultsByFormId(frm.formId));
            }
            //frmRsltsMdl.formResults = formsRepo.GetAllFormResults();

            bool noFormResults = false;
            if (frmRsltsMdl.formResults.Count() == 0)
            {
                Debug.WriteLine("* * *  Index  FormResults Count was 0. ");
                noFormResults = true;
                def_FormResults frmRslts = new def_FormResults()
                {
                    formResultId = 0,
                    formId = 0,
                    dateUpdated = System.DateTime.Now
                };
                frmRsltsMdl.formResults.Add(frmRslts);
            }
            else
            {
                Debug.WriteLine("* * *  Index  FormResults count: " + frmRsltsMdl.formResults.Count().ToString());
            }

            frmRsltsMdl.formTitles = new List<String>();
            if (noFormResults)
            {
                frmRsltsMdl.formTitles.Add("There are no Assessments in the system.");
            }
            else
            {
                foreach (def_FormResults frmRslt in frmRsltsMdl.formResults)
                {
                    def_Forms frm = formsRepo.GetFormById(frmRslt.formId);
                    if (frm != null)
                    {
                        frmRsltsMdl.formTitles.Add(frm.title);
                    }
                }
            }

            return View("formResults", frmRsltsMdl);

        }


        /*
         * WebService to return Meta Data table row count
        */
        [HttpGet]
        public string GetMetaDataTableCount()
        {
            string paramFileId = Request["fileId"] as string;
            Debug.WriteLine("* * *  ExportController GetMetaDataTableCount fileId: " + paramFileId);
            int iFileId = Convert.ToInt32(paramFileId);
            IFormsSql formsSql = new FormsSql();
            int rowCount = formsSql.GetTableRecordCountVenture(formsSql.GetMetaDataTableName(iFileId));

            return rowCount.ToString();
        }



        [HttpGet]
        public ActionResult SaveViewTemplateToTestDir()
        {
            // Create a request for the URL. 
            WebRequest request = WebRequest.Create(
              "http://localhost:50209/Export/getViewTemplate");

            // If required by the server, set the credentials.
            //request.Credentials = CredentialCache.DefaultCredentials;

            // Get the response.
            WebResponse response = request.GetResponse();

            // Display the status.
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();

            //save the stream contents to a local file
            string localPath = ControllerContext.HttpContext.Server.MapPath("../Views/Test/DownloadedTemplate.cshtml");
            FileStream output = new FileStream(localPath, FileMode.Create);
            dataStream.CopyTo(output);
            output.Close();
            dataStream.Close();
            response.Close();

            return new RedirectResult("~");
        }

        [HttpGet]
        public ActionResult UpdateFormTemplates( int formId )
        {

            //create a list of all the templates used by the form
            List<string> formTemplates = new List<string>();
            foreach (def_FormParts fp in formsRepo.GetFormById(formId).def_FormParts)
            {
                foreach (def_PartSections ps in fp.def_Parts.def_PartSections)
                {
                    string href = ps.def_Sections.href;
                    if (!formTemplates.Contains(href))
                        formTemplates.Add(href);
                }
            }                    

            //remove from the list any templates which are up-to-date
            for (int i = 0; i < formTemplates.Count(); i++)
            {
                string href = formTemplates[i];

                //get remote datetime
                DateTime remoteDateTime = DateTime.Parse(((ContentResult)GetFileDatetime(href)).Content);

                //get local datetime
                string local = ControllerContext.HttpContext.Server.MapPath(href);
                DateTime localDateTime = new FileInfo(local).LastWriteTime;

                ////determine if up-to-date
                //if (localDateTime > remoteDateTime)
                //{
                //    formTemplates.Remove(href);
                //    i--;
                //}
            }

            //special case if all templates where up-to-date
            if( formTemplates.Count() == 0 )
                return Content( "All templates where up-to-date" );
            string resultMessage = "the following local files where updated:";

            //iterate through teamplates that are not up-to-date, copy remote file to local file
            foreach (string href in formTemplates)
            {
                WebRequest request = WebRequest.Create(
                  "http://localhost:50209/Export/getViewTemplate?templatePath=" + href );
                WebResponse response = request.GetResponse();
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                Stream dataStream = response.GetResponseStream();
                string localPath = ControllerContext.HttpContext.Server.MapPath(href);
                resultMessage += "</br>" + localPath;
                FileStream output = new FileStream(localPath, FileMode.Create);
                dataStream.CopyTo(output);
                output.Close();
                dataStream.Close();
                response.Close();
            }

            return Content(resultMessage);
        }

        [HttpGet]
        public ActionResult GetFileDatetime(string templatePath)
        {
            if( templatePath == null )
                templatePath = "/Views/Templates/SIS/spprtNeedsScale.cshtml";
            string viewPath = ControllerContext.HttpContext.Server.MapPath( templatePath );
            return Content(System.IO.File.GetLastWriteTime(viewPath).ToString());
        }

        [HttpGet]
        public ActionResult getViewTemplate(string templatePath)
        {
            if( templatePath == null )
                templatePath = "/Views/Templates/SIS/spprtNeedsScale.cshtml";
            string viewPath = ControllerContext.HttpContext.Server.MapPath( templatePath );
            Debug.WriteLine("* * *  getViewTemplate: " + viewPath);
            FileContentResult fcr = File(System.IO.File.ReadAllBytes(viewPath), "text");
            return fcr;
        }

		
    }
}
