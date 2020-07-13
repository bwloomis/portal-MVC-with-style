using Assmnts.Infrastructure;
using Assmnts.Models;
using Data.Abstract;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Assmnts.Business
{
    public static class AssessmentCopy
    {
        
        public static def_FormResults CopyAssessment(IFormsRepository formsRepo, int formResultId)
        {
            def_FormResults oldFormResult = formsRepo.GetFormResultById(formResultId);

            def_FormResults copyFormResult = new def_FormResults();

            copyFormResult.archived = oldFormResult.archived;
            copyFormResult.assigned = oldFormResult.assigned;
            copyFormResult.dateUpdated = oldFormResult.dateUpdated;
            copyFormResult.deleted = oldFormResult.deleted;
            copyFormResult.EnterpriseID = oldFormResult.EnterpriseID;
            copyFormResult.formId = oldFormResult.formId;
            copyFormResult.formStatus = oldFormResult.formStatus;
            copyFormResult.GroupID = oldFormResult.GroupID;
            copyFormResult.interviewer = oldFormResult.interviewer;
            copyFormResult.locked = oldFormResult.locked;
            copyFormResult.reviewStatus = oldFormResult.reviewStatus;
            copyFormResult.sessionStatus = oldFormResult.sessionStatus;
            copyFormResult.statusChangeDate = oldFormResult.statusChangeDate;
            copyFormResult.subject = oldFormResult.subject;
            copyFormResult.training = oldFormResult.training;

            formsRepo.AddFormResultNoSave(copyFormResult);

            CopyAssessmentData(formsRepo, oldFormResult.formResultId, copyFormResult);

            formsRepo.Save();

            ReviewStatus.ChangeStatus(formsRepo, copyFormResult, ReviewStatus.PRE_QA, "Pre-QA copy created");
            
            AddHiddenFields(formsRepo, oldFormResult, copyFormResult);      

            return copyFormResult;
        }

        private static void AddHiddenFields(IFormsRepository formsRepo, def_FormResults oldFormResult, def_FormResults copyFormResult)
        {

            string version = GetVersion(formsRepo, oldFormResult);

            // Increment version -- may add more complex function here to do this later, right now just increment by 1 each time
            string newVersion = (Int32.Parse(version) + 1).ToString();
            
            
            Updates.AddField(formsRepo, Updates.SIS_HIDDEN, oldFormResult, Updates.RELATED_FORM_RESULT, copyFormResult.formResultId.ToString());
            Updates.AddField(formsRepo, Updates.SIS_HIDDEN, copyFormResult, Updates.RELATED_FORM_RESULT, oldFormResult.formResultId.ToString());

            Updates.AddField(formsRepo, Updates.SIS_HIDDEN, oldFormResult, Updates.VERSION, newVersion);
            Updates.AddField(formsRepo, Updates.SIS_HIDDEN, copyFormResult, Updates.VERSION, version);
            
        }

        private static string GetVersion(IFormsRepository formsRepo, def_FormResults oldFormResult)
        {
            def_ResponseVariables relatedFormResultRV = formsRepo.GetResponseVariablesByFormResultIdentifier(oldFormResult.formResultId, Updates.VERSION);

            if (relatedFormResultRV == null)
            {
                return "0";
            }
            else
            {
                return relatedFormResultRV.rspValue;
            }
        }


        //private static void AddHiddenField(IFormsRepository formsRepo, def_FormResults formResult, string fieldName, string value)
        //{
        //    int itemId = formsRepo.GetItemByIdentifier(SIS_HIDDEN).itemId;

        //    def_ItemResults itemResult = formsRepo.GetItemResultByFormResItem(formResult.formResultId, itemId);

        //    def_ResponseVariables relatedFormResultRV = null;

        //    try
        //    {
        //        relatedFormResultRV = formsRepo.GetResponseVariablesByFormResultIdentifier(formResult.formResultId, fieldName);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
            
        //    def_ItemVariables itemVariableRelated = formsRepo.GetItemVariableByIdentifier(fieldName);


        //    if (itemVariableRelated != null)
        //    {

        //        if (relatedFormResultRV != null)
        //        {
        //            relatedFormResultRV.rspValue = value;

        //            formsRepo.ConvertValueToNativeType(itemVariableRelated, relatedFormResultRV);
        //        }
        //        else
        //        {
        //            relatedFormResultRV = new def_ResponseVariables();

        //            relatedFormResultRV.itemResultId = itemResult.itemResultId;

        //            relatedFormResultRV.itemVariableId = itemVariableRelated.itemVariableId;

        //            relatedFormResultRV.rspValue = value;

        //            formsRepo.ConvertValueToNativeType(itemVariableRelated, relatedFormResultRV);

        //            formsRepo.AddResponseVariableNoSave(relatedFormResultRV);

        //        }

        //    }

        //    formsRepo.Save();
        //}

        


        private static void CopyAssessmentData(IFormsRepository formsRepo, int oldFormResultId, def_FormResults copyFormResult)
        {
            try
            {
                IUasSql uasSql = new UasSql();

                using (DbConnection connection = new SqlConnection(uasSql.GetConnectionString()))
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText
                            = "SELECT IR.itemId, RV.itemVariableId, RV.rspValue from def_ItemResults IR JOIN def_FormResults FR on FR.formResultId = IR.formResultId JOIN def_ResponseVariables RV on RV.itemResultId = IR.itemResultId WHERE FR.formResultId = " + oldFormResultId + " ORDER BY itemId";
                        command.CommandType = CommandType.Text;
                        DataTable dt = new DataTable();
                        dt.Load(command.ExecuteReader());

                        if (dt != null)
                        {
                            SaveAssessmentFromDataTable(formsRepo, copyFormResult, dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  CreateFormResultJSON exception: " + ex.Message);
            }
        }

        private static void SaveAssessmentFromDataTable(IFormsRepository formsRepo, def_FormResults copyFormResult, DataTable dt)
        {
            int prevItemId = -1;

            def_ItemResults itemResult = null;
            foreach (DataRow row in dt.Rows)
            {
                int currItemId = Int32.Parse(row["itemId"].ToString());
                int itemVariableId = Int32.Parse(row["itemVariableId"].ToString());
                string rspValue = row["rspValue"].ToString();

                if (currItemId != prevItemId)
                {
                    itemResult = new def_ItemResults();
                    itemResult.dateUpdated = DateTime.Now;
                    itemResult.itemId = currItemId;
                    itemResult.sessionStatus = 0;

                    copyFormResult.def_ItemResults.Add(itemResult);
                }

                if (itemResult != null)
                {
                    def_ResponseVariables responseVariable = new def_ResponseVariables();
                    responseVariable.itemVariableId = itemVariableId;
                    responseVariable.rspValue = rspValue;

                    def_ItemVariables itemVariable = formsRepo.GetItemVariableById(itemVariableId);

                    if (itemVariable != null)
                    {
                        formsRepo.ConvertValueToNativeType(itemVariable, responseVariable);

                        itemResult.def_ResponseVariables.Add(responseVariable);
                    }

                }
                prevItemId = currItemId;

            }
        }
    }
}