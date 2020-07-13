using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Assmnts.Business
{
    public static class Updates
    {
        public const string SIS_HIDDEN = "sis_hidden";

        public const string RELATED_FORM_RESULT = "related_form_result_id";
        public const string VERSION = "form_result_version";
        public const string VENTURE_VERSION = "venture_version";
        public const string CHECKED_OUT_TO = "sis_checked_out_to_LoginID";
        
        
        public static void AddField(IFormsRepository formsRepo, string itemIdentifier, def_FormResults formResult, string fieldName, string value)
        {
            int itemId = 0;
            try
            {

                itemId = formsRepo.GetItemByIdentifier(itemIdentifier).itemId;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Add Field to form: " + ex.Message);
                return;
            }
            
            def_ItemResults itemResult = formsRepo.GetItemResultByFormResItem(formResult.formResultId, itemId);

            if (itemResult == null)
            {
                itemResult = new def_ItemResults();

                itemResult.itemId = itemId;
                itemResult.formResultId = formResult.formResultId;
                itemResult.dateUpdated = DateTime.Now;
                itemResult.sessionStatus = 0;

                formsRepo.AddItemResult(itemResult);
            }

            def_ResponseVariables relatedFormResultRV = null;

            try
            {
                relatedFormResultRV = formsRepo.GetResponseVariablesByFormResultIdentifier(formResult.formResultId, fieldName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            def_ItemVariables itemVariableRelated = formsRepo.GetItemVariableByIdentifier(fieldName);


            if (itemVariableRelated != null)
            {

                if (relatedFormResultRV != null)
                {
                    relatedFormResultRV.rspValue = value;

                    formsRepo.ConvertValueToNativeType(itemVariableRelated, relatedFormResultRV);
                }
                else
                {
                    relatedFormResultRV = new def_ResponseVariables();

                    relatedFormResultRV.itemResultId = itemResult.itemResultId;

                    relatedFormResultRV.itemVariableId = itemVariableRelated.itemVariableId;

                    relatedFormResultRV.rspValue = value;

                    formsRepo.ConvertValueToNativeType(itemVariableRelated, relatedFormResultRV);

                    formsRepo.AddResponseVariableNoSave(relatedFormResultRV);

                }

            }

            formsRepo.Save();
        }
    }
}