using AJBoggs.Sis.Reports;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Assmnts.Reports;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Web.Hosting;
using System.Web.Mvc;
using UAS.Business;
using System.IO.Compression;
using AJBoggs.Sis.Domain;


namespace Assmnts.Controllers
{

	/*
     * This controller is used to print SIS reports to PDFs.
     * 
     */
    [RedirectingAction]
    public class SisPdfRptsController : Controller
    {
        private IFormsRepository formsRepo;

        public SisPdfRptsController(IFormsRepository fr)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
        }

        [HttpGet]
        public FileContentResult TestIntDurationReport()
        {
            string outpath = ControllerContext.HttpContext.Server.MapPath("../Content/report_" + System.DateTime.Now.Ticks + ".pdf");

            SisIntDurationReport report = new SisIntDurationReport(

                null, -1,  //formsRepo, formResultId (used in other reports)

                new SisPdfReportOptions(){ type = SisPdfReportOptions.ReportType.IntDuration },

                ControllerContext.HttpContext.Server.MapPath("../Content/images/aaidd_logo_full.jpg"), //path to logo for report header

                outpath
            );

            report.SetData(SisIntDurationReport.GetTestData());

            //build report, output to file
            report.BuildReport();
            report.outputToFile();

            //build the result object containing the file contents
            FileContentResult result = File(System.IO.File.ReadAllBytes(outpath), "application/pdf", "report.pdf");

            //delete the original file
            System.IO.File.Delete(outpath);

            return result;
        }

        [HttpGet]
        public FileContentResult GetBatchPdfReport()
        {
            //debug
            //Debug.WriteLine( "* * * GetBatchPdfReport start " + DateTime.Now.Ticks );

            foreach (var v in Session.Keys)
            {
                Debug.WriteLine("Search GetBatchPdfReport Session Keys: " + v);
            }

            // Retrieve a list of formresults to export
            ExportModel exportModel = (ExportModel)Session["ExportModel"];
            SearchModel search = (SearchModel)Session["SearchModel"];
            if (exportModel == null)
            {
                exportModel = new ExportModel();
            }
            
            if (exportModel.formResultIds == null)
            {
                if (search != null)
                {
                    exportModel.formResultIds = search.vSearchResult.GroupBy(sr => sr.formResultId).Select(grp => (int?)grp.Key).ToList();
                }
                else
                {
                    exportModel.formResultIds = new List<int?>();
                }
            }
            List<int?> formResultIds = exportModel.formResultIds;

            // Build a zip archive containing one PDF per assessment
            MemoryStream zipStream = new MemoryStream();
            using(zipStream)
            {
                using (ZipArchive zip = new ZipArchive( zipStream, ZipArchiveMode.Create, true))
                {
                    foreach ( int frId in formResultIds )
                    {
                        def_FormResults fr = formsRepo.GetFormResultById(frId);

                        //skip form results that are not complete
                        if (fr.formStatus != (byte)FormResults_formStatus.COMPLETED)
                            continue;

                        //generate PDF report with default options based on the formResult's enterprise
                        SisPdfReportOptions options = AJBoggs.Sis.Reports.SisReportOptions.BuildPdfReportOptions(fr.EnterpriseID);
                        SisPdfRptsController sprc = new SisPdfRptsController(formsRepo);
                        FileContentResult fcr = sprc.BottomBuildReport(frId, options);

                        //add the new pdf to the result (zip file)
                        var zipEntry = zip.CreateEntry( fcr.FileDownloadName );
                        using (Stream entryStream = zipEntry.Open() )
                        {
                            entryStream.Write(fcr.FileContents, 0, fcr.FileContents.Length);
                        }


                        //debug
                        //Debug.WriteLine("* * * GetBatchPdfReport finished formResult " + frId + " - " + DateTime.Now.Ticks);
                    }
                }
            }
            return File( zipStream.ToArray(), "multipart/x-zip", "reports.zip");
        }

        [HttpGet]
        public FileContentResult BuildFamilyReport( int formResultId ) //"formResultId" is synonymous with "sisId"
        {
            SisPdfReportOptions options = new SisPdfReportOptions();

            DateTime startTime = DateTime.Now;
            FileContentResult r = BottomBuildReport(formResultId, options);
            Debug.WriteLine( "SisPdfRptsController.BuildFamilyReport(): BottomBuildReport duration: " 
                + DateTime.Now.Subtract(startTime).TotalSeconds + " seconds" );
            return r;
        }

        [HttpGet]
        public FileContentResult BuildReport()
        {
            Debug.WriteLine("* * *  SisPdfRptsController:BuildReport method  * * *");
            //fetch options
            //bool b = Request.Form["grayscale"] == "on";
            //string sGrayscale = Request["grayscale"] as string;
            //bool grayscale = sGrayscale != null && sGrayscale.ToLower().Equals("true");

            if (!SessionHelper.IsUserLoggedIn)
            {
                string msg = "User not logged into application.";
                // byte[] emptyFile = new byte[] {0x20, 0x00};
                return new FileContentResult(Encoding.ASCII.GetBytes(msg), "text/html");
            }

            //setup session vars if necessary, add access log record
            if (SessionHelper.SessionForm == null)
            {
                SessionHelper.SessionForm = new SessionForm();
            }
            string routedFormResultId = Request["formResultId"];
            if ( routedFormResultId != null )
            {
                SessionHelper.SessionForm.formResultId = Convert.ToInt32(routedFormResultId);
            }
            AccessLogging.InsertAccessLogRecord(formsRepo, SessionHelper.SessionForm.formResultId, (int)AccessLogging.accessLogFunctions.REPORT, "Generated Report");


            SisPdfReportOptions options;
            //if report options are present in the request, use those.
            if (Request["hasOptions"].ToLower().Equals("true"))
            {
                options = new SisPdfReportOptions(Request);
            }

            //...otherwise use default options for this enterpise
            else {
                int? entId = formsRepo.GetFormResultById(SessionHelper.SessionForm.formResultId).EnterpriseID;
                options = AJBoggs.Sis.Reports.SisReportOptions.BuildPdfReportOptions(entId);
            }

            DateTime startTime = DateTime.Now;
            FileContentResult r = BottomBuildReport(SessionHelper.SessionForm.formResultId, options);
            Debug.WriteLine("SisPdfRptsController.BuildReport(): BottomBuildReport duration: "
                + DateTime.Now.Subtract(startTime).TotalSeconds + " seconds");
            return r;
        }


        //called by all report-building methods
        public FileContentResult BottomBuildReport(int formResultId, SisPdfReportOptions options )
        {
            //construct a report object, containing instructions for building the pdf

            string logoPath = HostingEnvironment.MapPath("/Content/images/aaidd_logo_full.jpg");
            string outpath = HostingEnvironment.MapPath("/Content/report_" + System.DateTime.Now.Ticks + ".pdf");
            SisPdfReport report;

            def_FormResults fr = formsRepo.GetFormResultById(formResultId);

            string entLogoPath = String.Empty;
            try
            {
                // custom enterprise logo is located at /websiteroot/Enterprise/EnterpriseID/logo-left.png
                entLogoPath = Path.Combine(ConfigurationManager.AppSettings["EnterpriseDir"], fr.EnterpriseID.ToString()) + "\\logo-left.png";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SisPdfRpts: BottomBuildReport: Unable to load custom logo for enterprise. Message: " + ex.Message);
            }
            if (System.IO.File.Exists(entLogoPath))
                logoPath = entLogoPath;
            else
                logoPath = HostingEnvironment.MapPath("/Content/images/aaidd_logo_full.jpg");

            try
            {
                // LK 3/26/2015 #12436 Added check on authorization for group to a central method (hasAccess) and used here

                if (!UAS_Business_Functions.hasAccess(fr))
                {
                    throw new Exception("You do not have access to this assessment record.");
                }

                //build report or reject, based on options and securityset
                if (options.type == SisPdfReportOptions.ReportType.Family)
                {
                    if (!UAS_Business_Functions.hasPermission(PermissionConstants.FAMREP, "reportexp"))//String.IsNullOrEmpty(reportexpSet) || reportexpSet[0] != 'Y')
                        throw new Exception("You do not have permission to build a family-friendly report");
                    report = new SisFamilyReport(formsRepo, formResultId, options, logoPath, outpath);
                }
                else if (options.type == SisPdfReportOptions.ReportType.Generic )
                {
                    if (!UAS_Business_Functions.hasPermission(PermissionConstants.GENREP, "reportexp"))//String.IsNullOrEmpty(reportexpSet) || (reportexpSet[1] != 'Y') )
                        throw new Exception("You do not have permission to build a generic report");
                    report = new SisGenericReport(formsRepo, formResultId, options, logoPath, outpath);
                }
                else
                {
                    if (!UAS_Business_Functions.hasPermission(PermissionConstants.EXPORT, "reportexp"))//String.IsNullOrEmpty(reportexpSet) || reportexpSet[2] != 'Y')
                        throw new Exception("You do not have permission to build a default report");
                    report = new SisShortLongReport(formsRepo, formResultId, options, logoPath, outpath);
                }
            }
            catch (Exception xcptn)
            {
                string msg = "Build Report constructor failed.  Exception: " + xcptn.Message;
                return new FileContentResult(Encoding.ASCII.GetBytes(msg), "text/html");
            }

            //build and save the pdf
            //try
            //{
                report.BuildReport();
            //}
            //catch (Exception xcptn)
            //{
            //    string msg = "BuildReport process failed.  Exception: " + xcptn.Message;
            //    return new FileContentResult(Encoding.ASCII.GetBytes(msg), "text/html");
            //}

            FileContentResult result;
            try
            {
                Debug.WriteLine("   SisPdfRptsController outpath:" + outpath);
                report.outputToFile();

                //build a descriptive filename
                string sDate = DateTime.Now.ToString( "MM-dd-yyyy" );
                string lastName = getResponseStringOrEmpty(formResultId, "sis_cl_last_nm");
                string firstName = getResponseStringOrEmpty( formResultId, "sis_cl_first_nm" );
                string filename = String.Format("{0}_{1}_{2}_{3}.pdf", lastName, firstName, formResultId, sDate);

                //build the result object containing the file contents
                result = File(System.IO.File.ReadAllBytes(outpath), "application/pdf", filename);

                //delete the original file
                System.IO.File.Delete(outpath);
            }
            catch (Exception xcptn)
            {
                string msg = "Build Report output failed.  Exception: " + xcptn.Message;
                result = new FileContentResult(Encoding.ASCII.GetBytes(msg), "text/html");
            }

            return result;
        }

        private string getResponseStringOrEmpty( int formResultId, string ivIdent ){
            def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, ivIdent);
            if( rv == null || String.IsNullOrWhiteSpace( rv.rspValue ) )
                return "";
            return rv.rspValue.Trim();
        }

    }
}