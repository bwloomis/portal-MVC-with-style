using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Diagnostics;
using System.Data.Entity;
using System.Data.Entity.Validation;

using Data.Concrete;

namespace Assmnts
{
    /// <summary>
    /// Summary description for AddFormResult
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class FormResultBuilder : System.Web.Services.WebService
    {
        [WebMethod]
        public int AddFormResult( int formId, int entId, int subId, int userId )
        {
            formsEntities db = DataContext.GetDbContext();

            //pick a new formResultId that is greater than any existing formResultId
            int formResultId = 0;
            foreach (def_FormResults fr in db.def_FormResults)
                if (fr.formResultId > formResultId)
                    formResultId = fr.formResultId + 1;

            //create the new formREsult object
            def_FormResults formResult = new def_FormResults();
            formResult.formResultId = formResultId;
            formResult.formId = formId;

            //add the new object to the database and save changes
            db.def_FormResults.Add( formResult );
            db.SaveChanges();

            return formResultId;
        }
    }
}
