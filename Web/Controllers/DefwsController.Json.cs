using Assmnts.Business;
using Assmnts.Infrastructure;
using Assmnts.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Mvc;
using AJBoggs.Def.Services.Json;


namespace Assmnts.Controllers
{
    public partial class DefwsController : Controller
    {
		
        /// <summary>
        /// Import a JSON version of an assessment. Uses different format than the JSON Venture import. 
        /// 
        /// For Venture import, see UpdateAssessmentJSON
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ImportFormResultJSON(string json)
        {
            ContentResult result = new ContentResult();

            result.Content = JsonImports.ImportFormResultJSON(formsRepo, json);

            return result;
        }

        /// <summary>
        /// New function to handle a uploading an assessment in the JSON format created in the FormsSql CreateFormResultJSON.
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns>ContentResult - exception message if any.</returns>
        [HttpPost]
        public ActionResult UpdateAssessmentJSONVenture(string json)
        {
            ContentResult result = new ContentResult();

            result.Content = JsonImports.UpdateAssessmentJSONVenture(formsRepo, json);

            return result;

        }

        /// <summary>
        /// Updating Assessment by Venture for old version (4.2) of Venture (new versions should use new function).
        /// </summary>
        /// <param name="json"></param>
        /// <returns>ContentResult - exception message if any.</returns>
        [HttpPost]
        public ActionResult UpdateAssessmentJSON(string json)
        {
            ContentResult result = new ContentResult();

            result.Content = JsonImports.UpdateAssessmentJSON(formsRepo, json);

            return result;

        }

       
    }
}
