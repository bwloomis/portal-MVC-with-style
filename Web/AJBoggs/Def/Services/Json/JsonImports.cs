using AJBoggs.Def.Domain;
using AJBoggs.Sis.Domain;
using Assmnts;
using Assmnts.Business;
using Assmnts.Controllers;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Mvc;

namespace AJBoggs.Def.Services.Json
{
    public static class JsonImports
    {
        public static string ImportFormResultJSON(IFormsRepository formsRepo, string json)
        {
            string result = String.Empty;
            try
            {
                var dFormResults = fastJSON.JSON.Parse(json);
                Dictionary<string, Object> resDict = (Dictionary<string, Object>)dFormResults;

                ////add the def_FormResults object to the database, which will assign and return a formResultId
                //int formRsltId = formsRepo.AddFormResult((def_FormResults) dFormResults);

                def_FormResults formResult = new def_FormResults()
                {
                    formId = Convert.ToInt32(resDict["formId"]),
                    dateUpdated = DateTime.Now,
                    formStatus = Convert.ToByte(resDict["formStatus"]),
                    sessionStatus = Convert.ToByte(resDict["sessionStatus"])
                };

                int formRsltId = formsRepo.AddFormResult(formResult);

                List<Object> itemObjectList = (List<Object>)resDict["def_ItemResults"];
                foreach (Object o in itemObjectList)
                {
                    Dictionary<string, object> dict = (Dictionary<string, object>)o;
                    def_ItemResults itemResults = new def_ItemResults
                    {
                        formResultId = formRsltId,
                        itemId = Convert.ToInt32(dict["itemId"]),
                        sessionStatus = Convert.ToInt32(dict["sessionStatus"]),
                        dateUpdated = Convert.ToDateTime(dict["dateUpdated"])
                    };

                    int itemResultId = formsRepo.AddItemResult(itemResults);

                    List<Object> respVarObjectList = (List<Object>)dict["def_ResponseVariables"];

                    foreach (Object r in respVarObjectList)
                    {
                        Dictionary<string, object> rDict = (Dictionary<string, object>)r;

                        DateTime? date;
                        if (rDict["rspDate"] == null)
                        {
                            date = null;
                        }
                        else
                        {
                            date = Convert.ToDateTime(rDict["rspDate"]);
                        }

                        def_ResponseVariables dResponseVariables = new def_ResponseVariables
                        {
                            itemResultId = itemResultId,
                            itemVariableId = Convert.ToInt32(rDict["itemVariableId"]),
                            rspInt = Convert.ToInt32(rDict["rspInt"]),
                            rspFloat = Convert.ToDouble(rDict["rspFloat"]),
                            rspDate = date,
                            rspValue = Convert.ToString(rDict["rspValue"])
                        };

                        formsRepo.AddResponseVariableNoSave(dResponseVariables);
                    }

                }

                formsRepo.Save();

                //pass the formResultId to the return value container
                result = formRsltId.ToString();

                AccessLogging.InsertAccessLogRecord(formsRepo, formRsltId, (int)AccessLogging.accessLogFunctions.IMPORT, "Imported assessment from JSON.");
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Def3WebServices.LoadFormResultJSON exception:" + excptn.Message);
            }

            return result;
        }

        /// <summary>
        /// New function to handle a uploading an assessment in the JSON format created in the FormsSql CreateFormResultJSON.
        /// 
        /// Reads in a form result record first and updates it.
        /// 
        /// Adds/modifies item results and response variables for this form result.
        /// 
        /// Item result and response variable data is in the following order:
        /// 
        /// responseVariableId, itemId, itemVariableId, rspValue
        /// 
        /// The data is sorted by itemId
        /// 
        /// responseVariableId is only included so that the data table on the client side has a primary key 
        /// (we may find a better way to do this later)
        /// </summary>
        /// <param name="json"></param>
        /// <returns>string - Exception message if any.</returns>

        public static string UpdateAssessmentJSONVenture(IFormsRepository formsRepo, string json)
        {
            string result = String.Empty;
            try
            {
                var dResults = fastJSON.JSON.Parse(json);
                Dictionary<string, Object> resDict = (Dictionary<string, Object>)dResults;

                Dictionary<string, object> formResultDict = (Dictionary<string, object>)resDict["FormResult"];

                // Find the form result we are updating.
                def_FormResults formResult = formsRepo.GetFormResultById(Int32.Parse(formResultDict["newFormResultId"].ToString()));

                // Update the form result's data (More values can be added here as needed to fill in other def_FormResults table fields)
                if (formResult != null)
                {
                    // 4/4/2016 Bug #13143 LK All uploaded assessments should have completed status; some were being set to uploaded somehow. Sets to completed.
                    formResult.formStatus = 2; // Hard code status "2" for "Complete" (all uploaded assessments must be Complete). To see other formStatus, visit: AJBoggs.Sis.Domain.FormResults_formStatus. OLD code: //Byte.Parse(formResultDict["formStatus"].ToString());
                    formResult.dateUpdated = DateTime.Parse(formResultDict["dateUpdated"].ToString());
                    formResult.interviewer = Int32.Parse(formResultDict["interviewer"].ToString());
                }

                // Get a list of all the data

                List<object> data = (List<object>)resDict["Data"];

                def_ItemResults itemResult = null;

                bool newItemResult = false;


                // Loop over the data items and process them, adding/modifying item results and response variables as needed
                foreach (object dataValue in data)
                {
                    // Order of data in this list is: responseVariableId, itemId, itemVariableId, rspValue
                    Dictionary<string, object> dataValueDict = (Dictionary<string, object>)dataValue;

                    int itemId = Int32.Parse(dataValueDict["itemId"].ToString());


                    // Find the item result corresponding to the form result and item
                    if (itemResult == null)
                    {
                        itemResult = formsRepo.GetItemResultByFormResItem(formResult.formResultId, itemId);
                    }
                    else if (itemResult.itemId != itemId) // we have moved on to the next item
                    {
                        // if the item result was new, and it has response variables, it is added to the item result list of the form result
                        // (this will be saved later)
                        if (newItemResult == true && itemResult.def_ResponseVariables.Count > 0)
                        {
                            formResult.def_ItemResults.Add(itemResult);
                            newItemResult = false;

                        }

                        // Attempt to get the next item result
                        itemResult = formsRepo.GetItemResultByFormResItem(formResult.formResultId, itemId);
                    }

                    // Create a new item result if it doesn't already exist
                    if (itemResult == null)
                    {
                        itemResult = new def_ItemResults();
                        itemResult.itemId = itemId;
                        itemResult.formResultId = formResult.formResultId;
                        itemResult.sessionStatus = 0;
                        newItemResult = true;
                    }

                    // Set the date updated for the item result (new or old) to now
                    itemResult.dateUpdated = DateTime.Now;


                    int itemVariableId = Int32.Parse(dataValueDict["itemVariableId"].ToString());

                    string rspValue = String.Empty;
                    if (dataValueDict["rspValue"] != null)
                    {
                        rspValue = dataValueDict["rspValue"].ToString();
                    }

                    // Attempt to find a response variable corresponding to the item variable id and form result
                    def_ResponseVariables responseVariable = formsRepo.GetResponseVariablesByFormResultItemVarId(formResult.formResultId, itemVariableId);

                    def_ItemVariables itemVariable = formsRepo.GetItemVariableById(itemVariableId);

                    // If no response variable is found, create one, convert it, and add it to the item result's response variable collection
                    if (responseVariable == null)
                    {
                        responseVariable = new def_ResponseVariables();
                        responseVariable.itemVariableId = itemVariableId;
                        responseVariable.rspValue = rspValue;

                        formsRepo.ConvertValueToNativeType(itemVariable, responseVariable);

                        itemResult.def_ResponseVariables.Add(responseVariable);

                    }
                    else // A response variable was found. Change the value and convert it.
                    {
                        responseVariable.rspValue = rspValue;

                        formsRepo.ConvertValueToNativeType(itemVariable, responseVariable);
                    }

                }

                // After all changes are made, save everything.
                formsRepo.Save();

                // check is this assessment has been scored.  If not, update the scores.
                def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier("scr_total_rawscores_all_SIS_sections");
                def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(formResult.formResultId, iv.itemVariableId);
                if (rv == null || String.IsNullOrEmpty(rv.rspValue))
                {
                    new Assessments(formsRepo).UpdateAssessmentScores(formResult);
                }

                try
                {

                    // Add access log record for check in.
                    AccessLogging.InsertAccessLogRecord(formsRepo, formResult.formResultId, (int)AccessLogging.accessLogFunctions.CHECK_IN, "Check in of assessment from Venture by " + SessionHelper.LoginInfo.LoginID);

                    // Add status log record for check in.
                    ReviewStatus.ChangeStatus(formsRepo, formResult, ReviewStatus.CHECKED_IN, "Checked in from Venture by " + SessionHelper.LoginInfo.LoginID);

                    // Change assessment status to completed (multiple workflows determined by enterprise; has separate method from ChangeStatus)
                    ReviewStatus.AssessmentIsCompleted(formsRepo, formResult);

                    // If webservice is enabled, add record to webservice activity table
                    if (WebServiceActivity.IsWebServiceEnabled())
                    {
                        WebServiceActivity.CallWebService(formsRepo, (int)WebServiceActivity.webServiceActivityFunctions.UPLOAD, "formResultId=" + formResult.formResultId.ToString());
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.StackTrace);
                }
            }
            catch (Exception excptn)
            {
                // If any exception is thrown, put the messages from it into the result returned to the client for troubleshooting purposes.

                Debug.WriteLine("JsonImports UpdateAssessmentJSON exception:" + excptn.Message);
                Debug.WriteLine(excptn.StackTrace);
                result = excptn.Message + "\n" + excptn.StackTrace;
                if ((excptn.InnerException != null) && (excptn.InnerException.Message != null))
                    result = result + "\n " + excptn.InnerException.Message;
                Debug.WriteLine("JsonImports UpdateAssessmentJSONVenture exception json:" + json);
            }


            return result;

        }


        /// <summary>
        /// Updating Assessment by Venture for old versions of Venture (new versions should use new function).
        /// </summary>
        /// <param name="json"></param>
        /// <returns>string - exception message if any.</returns>
        public static string UpdateAssessmentJSON(IFormsRepository formsRepo, string json)
        {
            string result = String.Empty;
            try
            {
                var dResults = fastJSON.JSON.Parse(json);
                Dictionary<string, Object> resDict = (Dictionary<string, Object>)dResults;

                List<Object> resList = (List<Object>)resDict[""];

                List<Object> firstList = (List<Object>)resList[0];

                int formStatus = Int32.Parse(firstList[4].ToString());
                int formResultId = Int32.Parse(firstList[3].ToString());
                Debug.WriteLine("Defws UpdateAssessmentJSON formResultId:" + firstList[3].ToString());

                def_FormResults fr = formsRepo.GetFormResultById(formResultId);
                fr.formStatus = (byte)formStatus;
                fr.dateUpdated = DateTime.Parse(firstList[5].ToString());

                SessionHelper.SessionForm = new SessionForm()
                {
                    formId = fr.formId,
                    formResultId = formResultId
                };

                AccessLogging.InsertAccessLogRecord(formsRepo, formResultId, (int)AccessLogging.accessLogFunctions.IMPORT, "Imported assessment from Venture.");

                int sectionId = Int32.Parse(firstList[0].ToString());
                def_Sections section = formsRepo.GetSectionById(sectionId);
                SessionHelper.SessionForm.sectionId = sectionId;

                FormCollection fc = new FormCollection();
                List<Object> rspData = null;

                foreach (Object res in resList)
                {
                    rspData = (List<Object>)res;

                    int nextSectionId = Int32.Parse(rspData[0].ToString());

                    if (nextSectionId != sectionId)
                    {
                        // New section; save last section's form collection
                        if (section == null)
                        {
                            Debug.WriteLine("Missing section " + sectionId + " on server.");
                            sectionId = nextSectionId;
                            continue; // If a section is missing, go to the next one but log error to console
                            //throw new Exception("Missing section " + sectionId + " on server.");
                        }

                        //* * * OT 03/10/16 Switched to using AJBoggs\Def\Domain\UserData.SaveFormCollection.cs
                        new UserData(formsRepo).FormResultsSaveSaveSectionItems(section, fc, formResultId);
                        sectionId = nextSectionId;
                        section = formsRepo.GetSectionById(sectionId);

                        SessionHelper.SessionForm.sectionId = sectionId;
                        fc.Clear();
                    }

                    string identifier = rspData[2].ToString();

                    string value = String.Empty;
                    if (rspData[1] != null)
                        value = rspData[1].ToString();

                    fc.Add(identifier, value);

                }

                if (section != null)
                {
                    //ResultsController r = new ResultsController(formsRepo);

                    //* * * OT 03/10/16 Switched to using AJBoggs\Def\Domain\UserData.SaveFormCollection.cs
                    new UserData(formsRepo).FormResultsSaveSaveSectionItems(section, fc, formResultId);
                }
                else
                {
                    Debug.WriteLine("Missing section " + sectionId + " on server.");
                }
                formsRepo.SaveFormResults(fr);

                AccessLogging.InsertAccessLogRecord(formsRepo, fr.formResultId, (int)AccessLogging.accessLogFunctions.CHECK_IN, "Check in of assessment from Venture by " + SessionHelper.LoginInfo.LoginID);
                ReviewStatus.ChangeStatus(formsRepo, fr, ReviewStatus.CHECKED_IN, "Checked in from Venture by " + SessionHelper.LoginInfo.LoginID);
                ReviewStatus.AssessmentIsCompleted(formsRepo, fr);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Defws UpdateAssessmentJSON exception:" + excptn.Message);
                result = excptn.Message;
                if ((excptn.InnerException != null) && (excptn.InnerException.Message != null))
                    result = result + " " + excptn.InnerException.Message;
            }

            return result;

        }
    }
}