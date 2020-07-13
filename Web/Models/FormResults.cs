using System;
using System.Diagnostics;
using System.Collections.Generic;

using Assmnts;

namespace Assmnts.Models
{

    public class FormResults
    {
        public List<def_FormResults> formResults { get; set; }
        public List<String> formTitles { get; set; }

        /*
         * Creates a new def_FormResults model
         * 
         */
        public static def_FormResults CreateNewFormResultModel(string formId)
        {
            def_FormResults form_result = new def_FormResults()
            {
                formId = Convert.ToInt32(formId),
                formStatus = 0,
                sessionStatus = 0,
                dateUpdated = DateTime.Today,
                deleted = false,
                locked = false,
                archived = false,
                reviewStatus = 255 // Corresponds to blank review status
            };

            return form_result;
        }

        public static def_FormResults CreateNewFormResultFull(string formId, string enterprise_id, string group_id, string subject, string interviewer)
        {
            def_FormResults frmRes = null;

            try
            {
                frmRes = FormResults.CreateNewFormResultModel(formId);

                // Add the FKs
                if (!String.IsNullOrEmpty(enterprise_id))
                {
                    frmRes.EnterpriseID = Convert.ToInt32(enterprise_id);
                }

                if (!String.IsNullOrEmpty(group_id))
                {
                    frmRes.GroupID = Convert.ToInt32(group_id);
                }

                if (!String.IsNullOrEmpty(subject))
                {
                    frmRes.subject = Convert.ToInt32(subject);
                }

                if (!String.IsNullOrEmpty(interviewer))
                {
                    frmRes.interviewer = Convert.ToInt32(interviewer);
                }

            }
            catch(Exception xcptn)
            {
                Debug.WriteLine("CreateNewFormResultFull  formId: " + formId + "   " + xcptn.Message);
            }

            return frmRes;
        }

    }

}
