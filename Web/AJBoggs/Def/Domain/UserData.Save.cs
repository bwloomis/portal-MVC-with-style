using Assmnts;

using Data.Abstract;

using System;
using System.Diagnostics;
using System.Linq;


namespace AJBoggs.Def.Domain
{
    /*
     * This partial class is used to Save ItemResults and ResponseVariables.
     * 
     * It should be used by other DEF methods, Controllers and WebServices for saving data to the Forms Repository.
     * 
     */
    public partial class UserData
    {
 
        public def_ItemResults SaveItemResult(int formResultId, int itmId)
        {
            // Add/Update Item Result
            def_ItemResults itemResult = formsRepo.GetItemResultByFormResItem(formResultId, itmId);
            if (itemResult == null)
            {
                // Add the ItemResult
                itemResult = new def_ItemResults()
                {
                    formResultId = formResultId,
                    itemId = itmId,
                    sessionStatus = 0,
                    dateUpdated = DateTime.Now
                };

                formsRepo.AddItemResultNoSave(itemResult);
            }
            else
            {
                // Update the ItemResult
                itemResult.dateUpdated = DateTime.Now;
            }

            return itemResult;
        }


        public def_ResponseVariables SaveResponseVariable(def_ItemResults ir, def_ItemVariables iv, string val)
        {

            // Add / Update the associated Response Variable in the database
            def_ResponseVariables rv = ir.def_ResponseVariables.Where(rrv => rrv.itemVariableId == iv.itemVariableId).FirstOrDefault();
            if (rv == null)
            {
                rv = new def_ResponseVariables()
                {
                    //itemResultId = ir.itemResultId,
                    itemVariableId = iv.itemVariableId,
                    rspValue = val
                };

                try
                {
                    formsRepo.ConvertValueToNativeType(iv, rv);
                }
                catch (Exception xcptn)
                {
                    Debug.WriteLine(iv.identifier + " Exception: " + xcptn.Message);
                }

                ir.def_ResponseVariables.Add(rv);
            }
            else
            {
                rv.rspValue = val;
                try
                {
                    formsRepo.ConvertValueToNativeType(iv, rv);
                }
                catch (Exception xcptn)
                {
                    Debug.WriteLine(iv.identifier + " Exception: " + xcptn.Message);
                }
            }

            return rv;
        }


    }
}
