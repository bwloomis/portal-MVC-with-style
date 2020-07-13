using Assmnts.Infrastructure;

using Data.Concrete;

using System;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;



namespace Assmnts.Controllers
{
    /*
     * This controller is used to download data in JSON format.
     */
    public partial class ExportController : Controller
    {

        [HttpGet]
        public ContentResult UploadFormResultsJSON()
        {
            //use session variables to pick a FormResults object from the database
            string formId = Request["formId"] as string;
            Debug.WriteLine("* * *  GetFormResultsJSON formId: " + formId);
            Session["formId"] = formId;
            int iFormId = Convert.ToInt32(formId);

            string formResultId = Request["formResultId"] as string;
            Debug.WriteLine("* * *  GetFormResultsJSON formResultId: " + formId);
            Session["formResultId"] = formResultId;
            int frmRsltId = Convert.ToInt32(formResultId);

            formsEntities db = DataContext.GetDbContext();

            db.Configuration.LazyLoadingEnabled = false;
            
            def_FormResults frmRslt = db.def_FormResults.Where(fr => fr.formId == iFormId).Include(fr => fr.def_ItemResults.Select(r => r.def_ResponseVariables)).First(fr => fr.formResultId == frmRsltId);

            string jsonString = fastJSON.JSON.ToJSON(frmRslt);
   
            //write json string to file, then stream it out
            //DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(FormResultJSON));
            string outpath = ControllerContext.HttpContext.Server.MapPath("../Content/formResults_" + System.DateTime.Now.Ticks + ".json");
            FileStream stream = new FileStream( outpath, FileMode.CreateNew );
            StreamWriter streamWriter = new StreamWriter(stream);

            streamWriter.Write(jsonString);
            streamWriter.Close();
            stream.Close();

            //used localhost for testing, should be changed to master server url
            // string url = "http://localhost:50209";
			string url = ConfigurationManager.AppSettings["SISOnlineURL"];

            //upload to master server
            string result;
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                var data = "json=" + System.IO.File.ReadAllText( outpath, Encoding.UTF8 );
                result = client.UploadString(url + "/Defws/ImportFormResultJSON", "POST", data);
            }

            AccessLogging.InsertAccessLogRecord(formsRepo, frmRsltId, (int)AccessLogging.accessLogFunctions.EXPORT, "Export JSON of assessment.");
          
            return Content( "formResultID on master server: " + result );
        }

    }
}
