using AJBoggs.Def.Services;
using AJBoggs.Sis.Domain;

using Assmnts.Business;
using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Abstract;
using Data.Concrete;

using Kent.Boogaart.KBCsv;
using Kent.Boogaart.KBCsv.Extensions.Data;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;

using UAS.Business;


namespace Assmnts.Controllers
{
    /*
     * This controller is used to download data.
     * 
     *   ************ Many of the methods towards the end of the code need to be removed once the Exports are done.
     */
    public partial class ExportController : Controller
    {

        private string getCSVSecondRowLabelForFormResultTagName(FormResultExportTagName tagName)
        {
            switch (tagName)
            {
                case FormResultExportTagName.formResultId:
                    return "SIS_ID";
                case FormResultExportTagName.identifier:
                    return "Form Name";
                default:
                    return tagName.ToString();
            }
        }

        /*
         *
         *
         */
        [HttpGet]
        public FileContentResult GetCsv()
        {
            string paramFormResultId = Request["formResultId"] as string;
            Debug.WriteLine("* * *  ExportController GetCsv formResultId: " + paramFormResultId);

            // Get all the itemVariable's in Assessment order
            // Write out the initial field name (identifier) record
            // Get the Item responses
            // Get the associated ResponseVariables
            // Write it out to CSV
            int formResultId = Convert.ToInt32(paramFormResultId);
            def_FormResults frmRes = formsRepo.GetFormResultById(formResultId);
            def_Forms frm = formsRepo.GetFormById(frmRes.formId);
            List<def_Parts> parts = formsRepo.GetFormParts(frm);

            // return new FileStreamResult(fileStream, "text/csv") { FileDownloadName = fileDownloadName };
            StringWriter sw = new StringWriter();
            sw.WriteLine("FirstName, LastName, NickName");
            sw.WriteLine("Charlie, Chaplin, Chuckles");
            return File(new System.Text.UTF8Encoding().GetBytes(sw.ToString()), "text/csv", "SisAssmnt.csv");
        }

        [HttpGet]
        public ActionResult BatchCSVOptions()
        {
            ExportModel exportModel = (ExportModel)Session["ExportModel"];

            return View("~/Views/Templates/SIS/batchCsvOptions.cshtml", exportModel);
        }

        [HttpGet]
        public ActionResult CSVOptions(string formId)
        {
            Debug.WriteLine("* * *  ExportController Parts formId: " + formId);
            Session["formId"] = formId;
            int iFormId = Convert.ToInt32(formId);
            def_Forms frm = formsRepo.GetFormById(iFormId);

            TemplateCsvOptions model = new TemplateCsvOptions();
            model.formPartMap = new Dictionary<string, int>();
            if (iFormId == 0)
            {
                if (model.parts == null)
                {
                    model.parts = new List<def_Parts>();
                }

                ExportModel exportModel = (ExportModel)Session["ExportModel"];
                List<int?> formIds = exportModel.formIds;
                model.forms = new List<def_Forms>();

                foreach (int? id in formIds)
                {
                    // Use the dictionary to indicate the starting index for each form in the parts list.
                    model.formPartMap.Add(id + " start", model.parts.Count());
                    frm = formsRepo.GetFormById(Convert.ToInt32(id));
                    List<def_Parts> prts = formsRepo.GetFormParts(frm);
                    model.parts.AddRange(prts);
                    model.forms.Add(frm);

                    // Mark the final index.
                    model.formPartMap.Add(id + " end", model.parts.Count());
                }
            }
            else if (frm == null)
            {
                throw new Exception("ExportController.CSVOptions: could not find form with id " + formId);
            }
            else
            {
                model.parts = formsRepo.GetFormParts(frm);
            }
            model.formId = iFormId;
            Session["formPartMap"] = model.formPartMap;
            return View("~/Views/Templates/SIS/csvOptions.cshtml", model);
        }

        /*
         * This methods exports FormResults by Part.
         * Parameters:  formId, includeParts (a comma-delimited list of Parts)
         * 
         */
        [HttpGet]
        public FileContentResult GetFormResultsCSV()
        {
            return BuildFormResultsCSV(new string[]{
                "FORMRESULT_sisId"
            }.ToList());
        }

        [HttpGet]
        public FileContentResult GetSISExportCSV()
        {
            return BuildFormResultsCSV(new string[] { 
                "FORMRESULT_formResultId",     //1)  SIS_ID
                "sis_cl_last_nm",       //2)  Name 
                "sis_cl_first_nm",      //3)  Name
                "sis_cl_middle_nm",     //4)  Name
                "sis_cl_dob_dt",        //5)  Date of Birth
                "sis_cl_sex_cd",        //6)  Gender
                "sis_cl_medicaidNum",   //7)  Identifier: Med _id
                "sis_cl_ssn",           //8)  Identifier: SSN
                "sis_cl_clientId",      //9)  Identifier: Client _id
                "sis_track_num",        //10) Identifier: Tracking #
                "sis_why",           //11) Reason for IW
                "FORMRESULT_statusText",    //12) Status
                "FORMRESULT_statusChangeDate", //13) When completed
                "sis_completed_dt",
                "FORMRESULT_dateUpdated",  //14) Last Modified
                "FORMRESULT_lastModifiedByLoginId",

                //15) Contact Information and then the remainder of the columns
                "sis_cl_addr_line1",
                "sis_cl_cou",
                "sis_cl_city",
                "sis_cl_st",
                "sis_cl_zip",
                "sis_cl_phone",
                "sis_cl_ext"

            }.ToList());
        }

        private FileContentResult BuildFormResultsCSV(List<string> leadingHeaders)
        {
            //debug
            Debug.WriteLine(" * * * BuildFormResultsCSV - " + DateTime.Now.Ticks + " started");

            string includeParts = Request["includeParts"] as string;
            Dictionary<string, int> formPartMap = (Dictionary<string, int>) Session["formPartMap"];
            string formId = Request["formId"] as string;

            ExportModel exportModel = (ExportModel)Session["ExportModel"];
            int iFormId = Convert.ToInt32(formId);

            List<def_Forms> forms = new List<def_Forms>();
            if (iFormId == 0)
            {
                foreach (int? id in exportModel.formIds)
                {
                    forms.Add(formsRepo.GetFormById(Convert.ToInt32(id)));
                }
            } else {
                forms.Add(formsRepo.GetFormById(iFormId));
            }
            

            string outpath = ControllerContext.HttpContext.Server.MapPath("../Content/formResults_" + System.DateTime.Now.Ticks + ".csv");
            Stream stream = new FileStream(outpath, FileMode.Create);
            CsvWriter writer = new CsvWriter(stream);

            // Build a map of with all relevent itemVariable identifiers
            Dictionary<int, string> ivIdentifiersById = new Dictionary<int, string>();
            foreach (string leadingIdent in leadingHeaders)
            {
                int id = leadingIdent.StartsWith("FORMRESULT_") ? -1-ivIdentifiersById.Count :
                    formsRepo.GetItemVariableByIdentifier(leadingIdent).itemVariableId;
                ivIdentifiersById.Add(id, leadingIdent);
            }
            int i = 0;
            Dictionary<int, List<string>> formIdentMap = new Dictionary<int, List<string>>();
            foreach (def_Forms form in forms)
            {
                List<string> map = new List<string>();
                // Use the formPartMap when its not empty.
                i = (formPartMap.Count() > 0) ? formPartMap[form.formId + " start"] : 0;
                foreach (def_Parts prt in formsRepo.GetFormParts(form))
                {
                    if (prt.identifier.Contains("Scores"))
                    {
                        continue;
                    }
                    if (includeParts[i++] == '1')
                    {
                        //formPartMap.Add(form.formId + " " + prt.partId + " ident start", ivIdentifiersById.Count());
                        foreach (def_Sections sctn in formsRepo.GetSectionsInPart(prt))
                        {
                            formsRepo.CollectItemVariableIdentifiersById(sctn, ivIdentifiersById);
                            foreach (def_Items item in formsRepo.GetAllItemsForSection(sctn))
                            {
                                map.AddRange(formsRepo.GetItemVariablesByItemId(item.itemId).Select(iv => iv.identifier).ToList());
                            }
                        }
                            
                        Debug.WriteLine("Added more sections to ivIdentifiersById, form: " + form.formId + " part: " + prt.partId + " new Count: " + ivIdentifiersById.Count());
                        //formPartMap.Add(form.formId + " " + prt.partId + " ident end", ivIdentifiersById.Count());
                    }
                }
                formIdentMap.Add(form.formId, map);
            }

            //add additional columns for any unhandled data the comes back from CommonExport.GetFormResultValues
            foreach (FormResultExportTagName tagName in Enum.GetValues(typeof(FormResultExportTagName)))
            {
                string header = "FORMRESULT_" + tagName.ToString();
                if (!ivIdentifiersById.Values.Contains( header ) )
                {
                    int id = -1 - ivIdentifiersById.Count; //not used, but must be unique to act as dictionary key
                    ivIdentifiersById.Add(id, header);
                }
            }

            //debug
            Debug.WriteLine(" * * * BuildFormResultsCSV - " + DateTime.Now.Ticks + " finished Build a map of with all column identifiers");

            // Build a list of formresults to export
            List<int?> formResultIds = exportModel.formResultIds;
            List<def_FormResults> formResultsToExport;
            if (iFormId == 0)
            {
                formResultsToExport = new List<def_FormResults>();
                foreach (def_Forms form in forms)
                {
                    formResultsToExport.AddRange(formsRepo.GetFormResultsByFormId(form.formId).Where(r => formResultIds.Contains(r.formResultId)).ToList());
                }
            }
            else
            {
                if (formResultIds == null)
                    formResultsToExport = formsRepo.GetFormResultsByFormId(iFormId).ToList();
                else
                    formResultsToExport = formsRepo.GetFormResultsByFormId(iFormId).Where(r => formResultIds.Contains(r.formResultId)).ToList();
            }
            Debug.WriteLine(" * * * BuildFormResultsCSV - " + DateTime.Now.Ticks + " finished building a list of formresults to export");

            //build a header record with identifiers
            int n = ivIdentifiersById.Count;
            List<int> ivIds = new List<int>();
            List<string> identifiers = new List<string>();
            List<string> headerText = new List<string>();
            int j = 0;
            foreach (KeyValuePair<int, string> de in ivIdentifiersById)
            {
                ivIds.Add(de.Key);
                identifiers.Add(de.Value);
                headerText.Add(de.Value.Replace("FORMRESULT_", ""));
                j++;
            }
            HeaderRecord hr = new HeaderRecord(true, headerText);
            writer.WriteRecord(hr);
            Debug.WriteLine(" * * * BuildFormResultsCSV - " + DateTime.Now.Ticks + " finished build a header record with identifiers");

            //Build a DataRecord with item labels (second row in the output file)
            string[] values = new string[n]; //used to temporarily store the values for each row in the export
            for (int k = 0; k < n; k++)
            {
                if (identifiers[k].StartsWith("FORMRESULT_"))
                {
                    FormResultExportTagName tag = (FormResultExportTagName)Enum.Parse(typeof(FormResultExportTagName), headerText[k]);
                    values[k] = getCSVSecondRowLabelForFormResultTagName(tag);
                }
                else
                    values[k] = formsRepo.GetItemVariableById(ivIds[k]).def_Items.label;
            }
            writer.WriteRecord(new DataRecord(hr, values));
            Debug.WriteLine(" * * * BuildFormResultsCSV - " + DateTime.Now.Ticks + " finished Build a DataRecord with item labels");
            
            //insert access logs (one for each assessment being exported)
            int[] arrFrIds = formResultIds.Where(id => id.HasValue).Select(id => id.Value).ToArray();
            AccessLogging.InsertMultipleAccessLogRecords(formsRepo, arrFrIds, (int)AccessLogging.accessLogFunctions.EXPORT, "Export CSV of assessment");

            // Build a DataRecord for each form result
            int count = 0, total = formResultsToExport.Count();
            foreach (def_FormResults fr in formResultsToExport)
            {
                //pull all necessary data for this formResult
                List<ValuePair> rspValues = CommonExport.GetDataByFormResultId(fr.formResultId);
                List<ValuePair> frValues  = CommonExport.GetFormResultValues(fr, forms.Where(f => f.formId == fr.formId).FirstOrDefault());

                //fill in values from formResult
                for (int k = 0; k < n; k++)
                {
                    if (identifiers[k].StartsWith("FORMRESULT_"))
                    {
                        //values[k] = GetFormResultValue(fr, headerText[k]);
                        ValuePair vp = frValues.Where(vpt => vpt.identifier == headerText[k]).FirstOrDefault();
                        if (vp != null)
                            values[k] = vp.rspValue;
                        else
                            values[k] = "";
                    }
                }

                //replace reponse values (numbers) with meaningful text for certain cells
                //FormResults_formStatus status;
                //Enum.TryParse(values[colIndex_status], out status);
                //values[colIndex_status] = status.ToString();
                //values[colIndex_sis_why] = GetDropdownText("assmtReason", values[colIndex_sis_why]);

                //fill in values from responseVariables
                for (i = 0; i < n; i++) {
                    if (identifiers[i].StartsWith("FORMRESULT_"))
                        continue;
                    ValuePair vp = null;
                    if (identifiers[i].StartsWith("FORMRESULT_") || formIdentMap[fr.formId].Contains(identifiers[i]))
                    {
                        vp = rspValues.Where(vpt => vpt.identifier == identifiers[i]).FirstOrDefault(); 
                    }
                    values[i] = vp == null ? "" : vp.rspValue;
                }
                writer.WriteRecord(new DataRecord(hr, values));

                //debug
                Debug.WriteLine(" * * * BuildFormResultsCSV - " + DateTime.Now.Ticks + " finished " + (++count) + "/" + total + " records");
            }
            writer.Close(); //<- calls stream.Close()


            //debug
            Debug.WriteLine(" * * * BuildFormResultsCSV - " + DateTime.Now.Ticks + " finished everything");

            return File(System.IO.File.ReadAllBytes(outpath), "text/csv", "results.csv");
        }

        //private string GetFormResultValue(def_FormResults fr, string placeholder)
        //{
        //    switch (placeholder)
        //    {
        //        case "sisId":
        //            return fr.formResultId.ToString();
        //        case "status":
        //            return fr.formStatus.ToString();
        //        case "statusChange":
        //            return fr.statusChangeDate.ToString();
        //        case "modified":
        //            return fr.dateUpdated.ToString();
        //        case "lastModifiedByUserId":
        //            return fr.LastModifiedByUserId.ToString();
        //        default:
        //            throw new Exception("unrecognized placeholder identifier \"" + placeholder + "\"");
        //    }
        //}

        //cache for the function GetDropdownText() below
        //used so that function will only pull from the db one time per lookupCode
        private readonly Dictionary<string, Dictionary<int, string>>
            lookupTextByOrderByLookupCode = new Dictionary<string, Dictionary<int, string>>();

        private string GetDropdownText(string lookupCode, string rspVal)
        {
            try
            {
                int rspInt = Convert.ToInt32(rspVal);

                //add entry to cache if necessary
                if (!lookupTextByOrderByLookupCode.ContainsKey(lookupCode))
                {
                    Dictionary<int, string> textByOrder = new Dictionary<int, string>();

                    int lookupMasterId = formsRepo.GetLookupMastersByLookupCode(lookupCode).lookupMasterId;
                    List<def_LookupDetail> details = formsRepo.GetLookupDetailsByLookupMasterEnterprise(lookupMasterId, 0);
                    foreach (def_LookupDetail ld in details)
                    {
                        int order = ld.displayOrder;
                        string text = formsRepo.GetLookupTextsByLookupDetail(ld.lookupDetailId).Find(lt => lt.langId == 1).displayText;
                        textByOrder.Add(order, text);
                    }
                    lookupTextByOrderByLookupCode.Add(lookupCode, textByOrder);
                }

                //pull result from cache
                return lookupTextByOrderByLookupCode[lookupCode][rspInt];

            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception getting dropdown text for lookupCode \"{0}\", rspVal \"{1}\"", lookupCode, rspVal);
                return rspVal;
            }
        }

        [HttpGet]
        public ActionResult UpdateMetaData()
        {
            // Create a request for the URL. 
            WebRequest request = WebRequest.Create("http://sisdev2.ad.ixn.net//Export/GetMetaCsv?fileId=0");

            // If required by the server, set the credentials.
            //request.Credentials = CredentialCache.DefaultCredentials;

            // Get the response.
            WebResponse response = request.GetResponse();

            // Display the status.
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();

            //save the stream contents to a local file
            string localPath = ControllerContext.HttpContext.Server.MapPath("../Views/Test/testFile.csv");
            FileStream output = new FileStream(localPath, FileMode.Create);
            dataStream.CopyTo(output);
            output.Close();
            dataStream.Close();
            response.Close();

            return new RedirectResult("~");
        }


        /// <summary>
        /// Called by local Venture DataSync Controller
        /// </summary>
        /// <returns>Returns CSV format of a DEF meta data table (Forms, Parts, Sections, Items, etc.)</returns>
        [HttpGet]
        public FileStreamResult GetMetaCsv()
        {
            string paramFileId = Request["fileId"] as string;
            string paramVentureVersion = Request["ventureVersion"] as string;

            string msgVentureVersion = String.IsNullOrEmpty(paramVentureVersion) ? "* No ventureVersion. *" : paramVentureVersion;
            Debug.WriteLine("* * *  ExportController GetMetaCsv fileId: " + paramFileId + "     ventureVersion: " + msgVentureVersion);

            int fileId = 0;
            try
            {
                fileId = Convert.ToInt32(paramFileId);
            }
            catch (Exception exptn)
            {
                Debug.WriteLine("* * *  ExportController GetMetaCsv Exception: " + exptn.Message);
            }
            Debug.WriteLine("* * *  ExportController GetMetaCsv fileId: " + fileId.ToString());

            IFormsSql formsSql = new FormsSql();

            string tableName;

            if (!String.IsNullOrEmpty(paramVentureVersion))
            {
                tableName = formsSql.GetMetaDataTableName(fileId);
            }
            else
            {
                tableName = formsSql.GetOldMetaDataTableName(fileId);
            }

            if (String.IsNullOrEmpty(tableName))
            {
                string errMsg = "Error: Invalid fileId: " + paramFileId;
                return ProcessError(errMsg);
            }

            string sqlQuery = getMetaQuery(tableName);

            return CreateCsvStream(formsSql.GetConnectionString(), sqlQuery);

        }

        /// <summary>
        /// Builds SQL query for the DEF Meta Data table download (Forms, Parts, etc.)
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private string getMetaQuery(string tableName)
        {
            string qry = "SELECT " + tableName + ".* FROM " + tableName;

            if (tableName == "def_LookupText")
            {
                qry += " JOIN def_LookupDetail ON def_LookupDetail.lookupDetailId = " + tableName + ".lookupDetailId";
            }

            if (tableName == "def_LookupText" || tableName == "def_LookupDetail")
            {
                qry += " WHERE (EnterpriseID = " + SessionHelper.LoginStatus.EnterpriseID + " OR EnterpriseID = 0)";

            }

            if (tableName == "def_SectionItemsEnt" || tableName == "def_PartSectionsEnt")
            {
                qry += " WHERE EnterpriseID = " + SessionHelper.LoginStatus.EnterpriseID;
            }

            if (tableName == "def_ItemsEnt")
            {
                qry += " WHERE ent_id = " + SessionHelper.LoginStatus.EnterpriseID;
            }

            return qry;
        }


        /// <summary>
        /// This method returns 1 of the 3 tables used for FormResults (Assessments)
        /// DEF User Data + SIS Recipients and Contacts
        /// 1st Parameter is the fileId as listed in Data/Concrete/FormsSql
        /// 2nd Parameter is the Venture version 
        /// Used for Venture downloading
        /// fileId is in Data.FormsSql formResultsTables
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public FileStreamResult GetResponseCsv()
        {

            string paramFileId = Request["fileId"] as string;
            Debug.WriteLine("* * *  ExportController GetResponseCsv fileId: " + paramFileId);

            // Handle older versions of Venture
            string paramVentureVersion = Request["ventureVersion"] as string;
            string msgVentureVersion = String.IsNullOrEmpty(paramVentureVersion) ? "* No ventureVersion. *" : paramVentureVersion;

            int fileId = 0;
            try
            {
                fileId = Convert.ToInt32(paramFileId);
            }
            catch (Exception exptn)
            {
                Debug.WriteLine("* * *  ExportController GetResponseCsv Exception: " + exptn.Message);
            }
            Debug.WriteLine("* * *  ExportController GetResponseCsv fileId: " + fileId.ToString());


            IFormsSql formsSql = new FormsSql();
            string tableName = formsSql.GetResultsTableName(fileId);
            if (String.IsNullOrEmpty(tableName))
            {
                string errMsg = "Error: Invalid fileId: " + paramFileId;
                return ProcessError(errMsg);
            }

            string sqlQuery = GetSqlQryForCsv(fileId);

            return CreateCsvStream(formsSql.GetConnectionString(), sqlQuery);

        }

        public void ChangeStatusToCheckedOut()
        {
            List<int> formResultIds = SessionHelper.Read<List<int>>("formResultIds");

            if (formResultIds != null)
            {
                foreach (int formResultId in formResultIds)
                {
                    def_FormResults formResult = formsRepo.GetFormResultById(formResultId);

                    ReviewStatus.ChangeStatus(formsRepo, formResult, ReviewStatus.CHECKED_OUT, "Checked out to Venture by " + SessionHelper.LoginInfo.LoginID);

                    Updates.AddField(formsRepo, Updates.SIS_HIDDEN, formResult, Updates.CHECKED_OUT_TO, SessionHelper.LoginInfo.LoginID);

                    AccessLogging.InsertAccessLogRecord(formsRepo, formResultId, (int)AccessLogging.accessLogFunctions.CHECK_OUT, "Checked out to Venture by " + SessionHelper.LoginInfo.LoginID);

                }
            }
        }

        private FileStreamResult ProcessError(String errMsg)
        {
            MemoryStream ms = new MemoryStream();
            FileStreamResult fsr = null;

            ms.Write(Encoding.ASCII.GetBytes(errMsg), 0, errMsg.Length);
            ms.Position = 0;
            fsr = new FileStreamResult(ms, "text/plain");
            return fsr;
        }

        private FileStreamResult CreateCsvStream(string connectionString, string sqlQuery)
        {
            DataTable dtResults = new DataTable();
            MemoryStream ms = new MemoryStream();
            FileStreamResult fsr = null;

            try
            {
                SqlConnection sqlConn = new SqlConnection(connectionString);
                sqlConn.Open();
                SqlCommand cmd = new SqlCommand(sqlQuery, sqlConn);

                dtResults.Load(cmd.ExecuteReader());
                sqlConn.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CreateCsvStream: " + ex.Message);
                string errMsg = "SQL Error: " + ex.Message;
                return ProcessError(errMsg);
            }

            try
            {
                CsvWriter cw = new CsvWriter(ms);

                dtResults.WriteCsv(cw);
                cw.Flush();
                ms.Position = 0;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("CreateCsvStream Csv Write Exception: " + ex.Message);
                string errMsg = "Csv Write Exception: " + ex.Message;
                return ProcessError(errMsg);
            }

            try
            {
                fsr = new FileStreamResult(ms, "text/csv");
            }
            catch (Exception exp)
            {
                Debug.WriteLine("File write exception: " + exp.Message);
                string errMsg = "File write exception: " + exp.Message;
                return ProcessError(errMsg);
            }

            return fsr;
        }

        private string GetSqlQryForCsv(int fileId)
        {
            IFormsSql formsSql = new FormsSql();
            string tableName = formsSql.GetResultsTableName(fileId);

            StringBuilder stringBuilder = null;

            List<int> formResultIds = SessionHelper.Read<List<int>>("formResultIds");
            List<int?> contactIds = SessionHelper.Read<List<int?>>("contactIds");


            if (formResultIds == null || contactIds == null)
            {
                GetAuthorizedFormResultIds(formsSql, out formResultIds, out contactIds);
                SessionHelper.Write("formResultIds", formResultIds);
                SessionHelper.Write("contactIds", contactIds);
            }

            if (formResultIds != null && formResultIds.Count > 0)
            {

                if (tableName == "Contact" || tableName == "Recipient")
                {
                    stringBuilder = getSqlQryForRecCon(tableName, contactIds);

                    return stringBuilder.ToString();
                }

                if (tableName == "def_FormResults")
                {

                    // if 'old' Venture
                    string frFields = "*";
                    if (1 == 3)
                    {
                        frFields = "formResultId, formId, formStatus, sessionStatus, dateUpdated, deleted, locked, archived, EnterpriseID, GroupID, subject, interviewer, assigned, training, reviewStatus, statusChangeDate ";
                    }

                    stringBuilder = new StringBuilder("SELECT " + frFields + " FROM def_FormResults");

                }
                else if (tableName == "def_ItemResults")
                {
                    stringBuilder = new StringBuilder("SELECT * FROM def_ItemResults");
                }
                else if (tableName == "def_ResponseVariables")
                {
                    stringBuilder = new StringBuilder("SELECT def_ResponseVariables.* FROM def_ResponseVariables");
                    stringBuilder.Append(" JOIN def_ItemResults on def_ItemResults.itemResultId = def_ResponseVariables.itemResultId");
                }

                stringBuilder.Append(" WHERE (");
                for (int i = 0; i < formResultIds.Count; i++)
                {
                    stringBuilder.Append("formResultId = " + formResultIds[i]);

                    if (i < formResultIds.Count - 1)
                    {
                        stringBuilder.Append(" OR ");
                    }
                }

                stringBuilder.Append(")");

                return stringBuilder.ToString();
            }

            return null;
        }

        private StringBuilder getSqlQryForRecCon(string tableName, List<int?> contactIds)
        {
            StringBuilder stringBuilder = null;
            string contactIdFieldName = String.Empty;
            if (tableName == "Contact")
            {
                stringBuilder = new StringBuilder("SELECT * FROM Contact");
                contactIdFieldName = "ContactID";
            }
            else if (tableName == "Recipient")
            {
                stringBuilder = new StringBuilder("SELECT * FROM Recipient");
                contactIdFieldName = "Recipient_ContactID";
            }

            stringBuilder.Append(" WHERE (");
            for (int i = 0; i < contactIds.Count; i++)
            {
                if (contactIds[i] != null)
                {
                    stringBuilder.Append(contactIdFieldName + " = " + contactIds[i]);

                    if (i < contactIds.Count - 1)
                    {
                        stringBuilder.Append(" OR ");
                    }
                }
            }

            stringBuilder.Append(")");

            return stringBuilder;
        }

        private void GetAuthorizedFormResultIds(IFormsSql formsSql, out List<int> formResultIds, out List<int?> contactIds)
        {
            DataTable dtFormResults = new DataTable();

            for (int j = 0; j < SessionHelper.LoginStatus.appGroupPermissions.Count; j++)
            {
                StringBuilder stringBuilder = new StringBuilder("SELECT formResultId, subject FROM def_FormResults");

                stringBuilder.Append(" WHERE EnterpriseID = " + SessionHelper.LoginStatus.EnterpriseID);
                if (!SessionHelper.LoginStatus.appGroupPermissions[j].authorizedGroups.Contains(0))
                {
                    stringBuilder.Append(" AND (");

                    for (int i = 0; i < SessionHelper.LoginStatus.appGroupPermissions[j].authorizedGroups.Count; i++)
                    {
                        stringBuilder.Append("GroupID = " + SessionHelper.LoginStatus.appGroupPermissions[j].authorizedGroups[i]);

                        if (i < SessionHelper.LoginStatus.appGroupPermissions[j].authorizedGroups.Count - 1)
                        {
                            stringBuilder.Append(" OR ");
                        }

                    }
                    stringBuilder.Append(")");
                }

                stringBuilder.Append(" AND assigned = " + SessionHelper.LoginStatus.UserID);
                stringBuilder.Append(" AND formStatus = " + (int)FormResults_formStatus.NEW);
                stringBuilder.Append(" AND deleted = 0");

                String qry = stringBuilder.ToString();

                try
                {
                    SqlConnection sqlConn = new SqlConnection(formsSql.GetConnectionString());
                    sqlConn.Open();
                    SqlCommand cmd = new SqlCommand(qry, sqlConn);

                    if (j == 0)
                    {
                        dtFormResults.Load(cmd.ExecuteReader());
                    }
                    else
                    {
                        DataTable dt = new DataTable();
                        dt.Load(cmd.ExecuteReader());
                        dtFormResults.Merge(dt);
                    }
                    sqlConn.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetAuthorizedFormResultIds: " + ex.Message);
                    formResultIds = null;
                    contactIds = null;
                    return;
                }
            }

            formResultIds = (from fr in dtFormResults.Select()
                             select fr.Field<int>(0)).ToList();

            contactIds = (from fr in dtFormResults.Select()
                          select fr.Field<int?>(1)).ToList();
        }

        /*
         * This method returns 1 of the 10 tables used for UAS (Assessments)
         * Parameter is the fileId as listed in Data/Concrete/UasSql
         * 
         */
        [HttpGet]
        public FileStreamResult GetUasCsv()
        {

            string paramFileId = Request["fileId"] as string;
            Debug.WriteLine("* * *  ExportController GetUasCsv fileId: " + paramFileId);
            int fileId = 0;
            try
            {
                fileId = Convert.ToInt32(paramFileId);
            }
            catch (Exception exptn)
            {
                Debug.WriteLine("* * *  ExportController GetUasCsv Exception: " + exptn.Message);
            }
            Debug.WriteLine("* * *  ExportController GetUasCsv fileId: " + fileId.ToString());

            IUasSql uasSql = new UasSql();
            string tableName = uasSql.GetUasTableName(fileId);
            if (String.IsNullOrEmpty(tableName))
            {
                string errMsg = "Error: Invalid fileId: " + paramFileId;
                return ProcessError(errMsg);
            }

            Debug.WriteLine("   Connection: " + uasSql.GetConnectionString());

            string sqlQuery = getUASQuery(tableName);

            return CreateCsvStream(uasSql.GetConnectionString(), sqlQuery);

        }

        private string getUASQuery(string tableName)
        {
            string qry = string.Empty;
            if (tableName == "uas_Enterprise")
            {
                qry = "SELECT * FROM " + tableName + " WHERE EnterpriseID = " + SessionHelper.LoginStatus.EnterpriseID;
            }
            else if (tableName == "uas_Group")
            {
                qry = "SELECT * FROM " + tableName + " WHERE EnterpriseID = " + SessionHelper.LoginStatus.EnterpriseID;
            }
            else if (tableName == "uas_User")
            {
                qry = "SELECT * FROM " + tableName + " WHERE UserID = " + SessionHelper.LoginStatus.UserID;
            }
            else if (tableName == "uas_GroupUserAppPermissions")
            {
                qry = "SELECT * FROM " + tableName + " WHERE UserID = " + SessionHelper.LoginStatus.UserID + " AND ApplicationID = " + UAS.Business.Constants.APPLICATIONID + " AND (";

                for (int i = 0; i < SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Count; i++)
                {
                    qry += "GroupID = " + SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[i];

                    if (i < SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Count - 1)
                    {
                        qry += " OR ";
                    }
                }
                qry += ")";
            }
            else if (tableName == "uas_Config")
            {
                qry = "SELECT * FROM " + tableName;
            }
            else if (tableName == "uas_GroupType")
            {
                qry = "SELECT * FROM " + tableName + " WHERE EnterpriseID = " + SessionHelper.LoginStatus.EnterpriseID;
            }
            else if (tableName == "uas_RoleType")
            {
                qry = "SELECT * FROM " + tableName;
            }
            else if (tableName == "uas_Role")
            {
                qry = "SELECT * FROM " + tableName + " WHERE EnterpriseID = " + SessionHelper.LoginStatus.EnterpriseID;
            }
            else if (tableName == "uas_RoleAppPermissions")
            {
                qry = "SELECT " + tableName + ".[RoleAppPermissionsID], " + tableName + ".[RoleID], " + tableName + ".[CreatedDate], "
                    + tableName + ".[CreatedBy], " + tableName + ".[ModifiedDate], " + tableName + ".[ModifiedBy], " + tableName
                    + ".[StatusFlag], " + tableName + ".[SecuritySet]," + tableName + ".[ApplicationID] FROM " + tableName
                    + " JOIN uas_Role ON " + tableName + ".RoleID = uas_Role.RoleID WHERE uas_Role.EnterpriseID = " + SessionHelper.LoginStatus.EnterpriseID
                    + " AND " + tableName + ".ApplicationID = " + UAS.Business.Constants.APPLICATIONID;
            }
            else if (tableName == "uas_UserEmail" || tableName == "uas_UserPhone" || tableName == "uas_UserAddress")
            {
                qry = "SELECT * FROM " + tableName + " WHERE UserID = " + SessionHelper.LoginStatus.UserID;
            }
            else if (tableName == "uas_EntAppConfig")
            {
                qry = "SELECT * FROM " + tableName + " WHERE EnterpriseID = " + SessionHelper.LoginStatus.EnterpriseID + " AND ApplicationID = " + UAS.Business.Constants.APPLICATIONID;
            }

            Debug.WriteLine("UAS query: " + qry);
            return qry;
        }

        private List<int> GetAuthorizedFormResultIds(IFormsSql formsSql)
        {
            DataTable dtFormResults = new DataTable();

            for (int j = 0; j < SessionHelper.LoginStatus.appGroupPermissions.Count; j++)
            {
                StringBuilder stringBuilder = new StringBuilder("SELECT formResultId FROM def_FormResults");

                stringBuilder.Append(" WHERE EnterpriseID = " + SessionHelper.LoginStatus.EnterpriseID);
                if (!SessionHelper.LoginStatus.appGroupPermissions[j].authorizedGroups.Contains(0))
                {
                    stringBuilder.Append(" AND (");

                    for (int i = 0; i < SessionHelper.LoginStatus.appGroupPermissions[j].authorizedGroups.Count; i++)
                    {
                        stringBuilder.Append("GroupID = " + SessionHelper.LoginStatus.appGroupPermissions[j].authorizedGroups[i]);

                        if (i < SessionHelper.LoginStatus.appGroupPermissions[j].authorizedGroups.Count - 1)
                        {
                            stringBuilder.Append(" OR ");
                        }

                    }
                    stringBuilder.Append(")");
                }

                if (!UAS_Business_Functions.hasPermission(PermissionConstants.GROUP_WIDE, j, PermissionConstants.ASSMNTS))
                {
                    stringBuilder.Append(" AND " + " assigned = " + SessionHelper.LoginStatus.UserID);
                }

                stringBuilder.Append(" AND formStatus < " + (int)FormResults_formStatus.COMPLETED);

                String qry = stringBuilder.ToString();

                try
                {
                    SqlConnection sqlConn = new SqlConnection(formsSql.GetConnectionString());
                    sqlConn.Open();
                    SqlCommand cmd = new SqlCommand(qry, sqlConn);

                    if (j == 0)
                    {
                        dtFormResults.Load(cmd.ExecuteReader());
                    }
                    else
                    {
                        DataTable dt = new DataTable();
                        dt.Load(cmd.ExecuteReader());
                        dtFormResults.Merge(dt);
                    }
                    sqlConn.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetAuthorizedFormResultIds: " + ex.Message);
                    return null;
                }
            }

            List<int> formResultIds = (from fr in dtFormResults.Select()
                                       select fr.Field<int>(0)).ToList();

            return formResultIds;
        }

    }
}
