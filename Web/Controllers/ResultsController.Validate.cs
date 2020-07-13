using AJBoggs.Adap.Domain;
using AJBoggs.Def.Services;
using AJBoggs.Sis.Domain;

using Assmnts.Business;
using Assmnts.Infrastructure;
using Assmnts.Models;

using System.Collections.Generic;
using System.Web.Mvc;



namespace Assmnts.Controllers
{
    /*
     * This partial controller is used to validate FormResults.
     */
    public partial class ResultsController : Controller
    {

        /*  This method validates the SIS templates in Views/Templates/SIS
         * 
         * 
         */
        private bool ValidateFormResult(def_FormResults fr, TemplateItems amt = null)
        {
            // * * * OT 3-15-16 completed form results need to be validated on every save (Bug 13110)
            //// * * * OT 1-4-16   form results that are already marked as completed get to skip validation
            //if (fr.formStatus == (byte)FormResults_formStatus.COMPLETED)
            //    return true;

            if (amt == null)
                amt = new GeneralForm();

            //retrieve all responses for this formResult, by def_ItemVariable identifier
            List<ValuePair> allResponses = CommonExport.GetDataByFormResultId(fr.formResultId);

            //pass the response data through generic validation (meta-data driven validation)
            def_Forms frm = formsRepo.GetFormById( fr.formId );
            string[] ItemVariableSuffixesToSkip = { "Notes", "portantTo", "portantFor", "cl_age", "int_id" };
            SharedValidation sv = new SharedValidation(allResponses);
            bool invalid = sv.DoGenericValidation(formsRepo, amt, frm, ItemVariableSuffixesToSkip);

            //pass the response data through one-off validation (rules that can't be encoded into meta-data)
            //populate amt.valdiationMessages in the process
            amt.validationMessages = new List<string>();
            if (SisOneOffValidation.RunOneOffValidation(formsRepo, frm, fr.EnterpriseID.Value, allResponses, amt))
                invalid = true;

            if (invalid)
            {
                return false;
            }

            //  Mark the FormResult as Complete
            new Assessments(formsRepo).AssessmentComplete(fr);// formsRepo.FormResultComplete(fr);

            // Insert status log indicating assessment has been marked complete
            if (SessionHelper.IsVentureMode == false)
            {
                ReviewStatus.AssessmentIsCompleted(formsRepo, fr);

                if (WebServiceActivity.IsWebServiceEnabled())
                {
                    WebServiceActivity.CallWebService(formsRepo, (int)WebServiceActivity.webServiceActivityFunctions.COMPLETE, "formResultId=" + fr.formResultId.ToString());
                }
            
            }

            // Populate the hidden Venture Version field upon validation
            
            if (SessionHelper.IsVentureMode == true)
            {
                string ventureVersion = SessionHelper.Read<string>("venture_version");

                if (ventureVersion != null)
                {
                    Updates.AddField(formsRepo, Updates.SIS_HIDDEN, fr, Updates.VENTURE_VERSION, ventureVersion);
                }
            }

            return true;
        }

        private bool isNullEmptyOrBlank(def_ResponseVariables rv)
        {
            if (rv == null)
                return true;

            if (rv.rspValue == null)
                return true;

            if (rv.rspValue.Length == 0)
                return true;

            return false;
        }
        
    }
}
