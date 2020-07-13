using AJBoggs.Sis.Domain;
using Assmnts.Models;
using Data.Concrete;
using Kent.Boogaart.KBCsv;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Mvc;


namespace Assmnts.Controllers
{
    public partial class DefwsController : Controller
    {
        private class AssmntRecord
        {
            private readonly DataRecord dr;
            private readonly List<string> headers;

            public AssmntRecord(DataRecord dr)
            {
                this.dr = dr;
                headers = dr.HeaderRecord.ToList();
            }

            public List<string> getHeaders()
            {
                return new List<string>(headers);
            }

            public string this[string header] { get { 
                return dr[header];
            } }

            public int getInt(string header)
            {
                return Int32.Parse(dr[header]);
            }
        }

        [HttpPost]
        public ActionResult UploadCsvGetStubRecords()
        {
            string result = "USE [forms]\nGO\nSET IDENTITY_INSERT [dbo].[def_FormResults] ON\nGO\n";
            using (var csvr = new CsvReader(new StreamReader(Request.InputStream)))
            {
                csvr.ReadHeaderRecord();

                //organize csv headers into a heirarchy similar to the response data schema
                //So for each ItemId, we have a set of ItemVariableIDs, each with one csvHeader.
                Dictionary<int, Dictionary<int, string>> csvHeadersByIvByItem = new Dictionary<int, Dictionary<int, string>>();
                foreach (string csvHeader in csvr.HeaderRecord)
                {
                    string ivIdent = getItemVariableIdentifierFromCsvHeader(csvHeader);
                    def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(ivIdent);
                    if (iv == null)
                        continue;
                    int itmId = iv.itemId;
                    if (!csvHeadersByIvByItem.ContainsKey(itmId))
                        csvHeadersByIvByItem.Add(itmId, new Dictionary<int, string>());
                    csvHeadersByIvByItem[itmId].Add(iv.itemVariableId, csvHeader);
                }

                ////determine which header is present for assigning uploaded formResults to groups 
                //bool useOrgNames;
                //if( csvr.HeaderRecord.Contains( "sub_id" ) )
                //    useOrgNames = false;
                //else if (csvr.HeaderRecord.Contains("org_name"))
                //    useOrgNames = true;
                //else
                //    throw new Exception( "Could not find any headers to link assessments to groups" );

                //iterate through the uploaded csv rows
                while (csvr.HasMoreRecords)
                {
                    AssmntRecord acl = new AssmntRecord(csvr.ReadDataRecord());

                    int formId = 1;
                    int subject = 0;// acl.getInt("ClientId");
                    int entId = acl.getInt("ent_id");
                    //int groupId = 460;//one-off 9/2/15////////////////////////////////
                    //if (useOrgNames)
                    //{
                    //    string orgName = acl["org_name"];
                    //    Group group = auth.GetGroupsByGroupNameAndEnterpriseID(orgName, entId).FirstOrDefault();
                    //    if (group == null)
                    //        throw new Exception("could not find group for org_name \"" + orgName + "\"");
                    //    groupId = group.GroupID;
                    //}
                    //else
                    //{
                    int groupId = acl.getInt("sub_id");
                    //}

                    //add new form result in database
                    //int formRsltId = formsRepo.AddFormResult(formResult);

                    int formRsltId = acl.getInt("sis_id");
                    def_FormResults formResult = formsRepo.GetFormResultById(formRsltId);

                    // if the formResult doesn't already exist, append to the sql script for identity-inserting stub records
                    if (formResult == null)
                    {
                        def_FormResults fr = new def_FormResults();
                        fr.formId = formId;
                        fr.EnterpriseID = entId;
                        fr.GroupID = groupId;
                        fr.subject = subject;
                        fr.dateUpdated = DateTime.Now;
                        fr.formStatus = GetStatus(acl["Status"]);
                        fr.locked = GetLocked(acl["Status"]);
                        fr.sessionStatus = 0;
                        fr.reviewStatus = 255;
                        fr.training = false;

                        string sql = "INSERT INTO [dbo].[def_FormResults]([formResultId],[formId],[formStatus],[sessionStatus],[dateUpdated],[deleted],[locked],"
                        + "[archived],[EnterpriseID],[GroupID],[subject],[interviewer],[assigned],[training],[reviewStatus],[statusChangeDate])\n"
                        + "VALUES( '" + formRsltId + "','" + fr.formId + "','" + fr.formStatus + "','" + fr.sessionStatus + "','" + fr.dateUpdated + "','"
                        + fr.deleted + "','" + fr.locked + "','" + fr.archived + "','" + fr.EnterpriseID + "','" + fr.GroupID + "','" + fr.subject + "','"
                        + fr.interviewer + "','" + fr.assigned + "','" + fr.training + "','" + fr.reviewStatus + "','" + fr.statusChangeDate + "')\n";


                        result += sql;
                    }

                    //    // Null out the locally allocated objects so the garbage collector disposes of them.
                    //    fr = null;
                    //    acl = null;
                    //    formResult = null;
                    //}

                    ////saveAssessmentScript(formResult);
                    //formsRepo.SaveFormResults(formResult);
                    //formsRepo.GetContext().Dispose();
                    //formsRepo = new FormsRepository();

                    //// Null out the locally allocated objects so the garbage collector disposes of them.
                    //fr = null;
                    //acl = null;
                    //formResult = null;
                }
            }

            result += "SET IDENTITY_INSERT [dbo].[def_FormResults] OFF\nGO\n";

            return Content(result);
        }

        /// <summary>
        /// Used for migrating SIS Assessments.
        /// Could potentially be used to import almost any FormResults data.
        /// *** INSERT only *** - not checking for existing data.
        /// </summary>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult UploadAssmntCsv(int rowCount)
        {
            //* * * OT 2-4-16 This will be updated so that when rowCount is large, a smaller batchSize is used.
            int batchSize = rowCount;

            using (var csvr = new CsvReader(new StreamReader(Request.InputStream)))
            {
                csvr.ReadHeaderRecord();

                //organize csv headers into a heirarchy that mirrors the response data schema (itemResults->responseVariables)
                //So for each ItemId, we have a set of ItemVariableIDs, each with one csvHeader.
                //This will be used to organize responses in a similar heirarchy and ultimately perform a batch insert 
                //all without having to read any more meta-data
                Dictionary<int, Dictionary<int, string>> csvHeadersByIvByItem = new Dictionary<int, Dictionary<int, string>>();
                foreach (string csvHeader in csvr.HeaderRecord)
                {
                    string ivIdent = getItemVariableIdentifierFromCsvHeader(csvHeader);
                    def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(ivIdent);
                    if (iv == null)
                        continue;
                    int itmId = iv.itemId;
                    if (!csvHeadersByIvByItem.ContainsKey(itmId))
                        csvHeadersByIvByItem.Add(itmId, new Dictionary<int, string>());
                    csvHeadersByIvByItem[itmId].Add(iv.itemVariableId, csvHeader);
                }

                //NO READS AFTER THIS POINT

                //create a structure matching csvHeadersByIvByItem
                //this one will contain responses
                //for each ItemId, we have a set of ItemVariableIDs, each with an empty array to hold a batch of responses
                Dictionary<int, Dictionary<int, string[]>> rspValsByIvByItem = new Dictionary<int, Dictionary<int, string[]>>();
                foreach (int itemId in csvHeadersByIvByItem.Keys)
                {
                    Dictionary<int, string> csvHeadersByIv = csvHeadersByIvByItem[itemId];
                    Dictionary<int, string[]> rspValsByIv = new Dictionary<int, string[]>();
                    foreach (int ivId in csvHeadersByIv.Keys)
                    {
                        rspValsByIv.Add(ivId, new string[batchSize]);
                    }
                    rspValsByIvByItem.Add(itemId, rspValsByIv);
                }
                int[] formResultIds = new int[batchSize]; //formResultId for each row of the csv


                //* * * OT 2-4-16 The code below (including call to SaveBatchResponses) 
                //* * * will be wrapped in another loop to handle large uploads in multiple batches

                //iterate through the uploaded csv rows, 
                //use the heirarchy of headers to populate the heirarchy of responses with input data
                int rowIndex = 0;
                while (csvr.HasMoreRecords && rowIndex < rowCount)
                {
                    AssmntRecord acl = new AssmntRecord(csvr.ReadDataRecord());

                    int formRsltId = acl.getInt("sis_id");
                    foreach (int itemId in csvHeadersByIvByItem.Keys)
                    {
                        Dictionary<int, string> csvHeadersByIv = csvHeadersByIvByItem[itemId];
                        Dictionary<int, string[]> rspValsByIv = rspValsByIvByItem[itemId];
                        foreach (int ivId in csvHeadersByIv.Keys)
                        {
                            rspValsByIv[ivId][rowIndex] = acl[csvHeadersByIv[ivId]];
                        }
                    }
                    formResultIds[rowIndex] = formRsltId;

                    rowIndex++;
                }

                //push all the data into the database at once. 
                //this does not require any reads, because the response heirarchy matches the DB schema (itemResults->responseVariables)
                try
                {
                    formsRepo.SaveBatchResponses(rspValsByIvByItem, formResultIds);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("* * * UploadAssmntCsv: failed to save formResultIds " + String.Join(", ", formResultIds));
                }
            }

            return Content("Upload Successful!");
        }

        private byte GetStatus(string oldSysStatusText) {
            switch (oldSysStatusText)
            {
                //any other cases here...

                case "Completed":
                case "Completed-Locked":
                    return (byte)FormResults_formStatus.COMPLETED;
                default:
                    return (byte)FormResults_formStatus.IN_PROGRESS;
            }
        }

        private bool GetLocked(string oldSysStatusText) {
            switch (oldSysStatusText)
            {
                //any other cases here...

                case "Completed-Locked":
                    return true;
                default:
                    return false;
            }
        }

        //private void saveAssessmentScript(def_FormResults fr)
        //{
        //    //get safe starting indices for itemresults and response variables
        //    int irId = DataContext.GetDbContext().def_ItemResults.OrderByDescending(ir => ir.itemResultId).First().itemResultId + 1000;
        //    int rvId = DataContext.GetDbContext().def_ResponseVariables.OrderByDescending(rv => rv.responseVariableId).First().responseVariableId + 1000;
            
        //    string outPath = @"C:\\Users\\otessmer\\Downloads\\fr_" + fr.formResultId + ".txt";
        //    System.IO.File.Delete(outPath);
        //    System.IO.File.AppendAllText(outPath, "USE [forms]\nGO\n" );
        //    System.IO.File.AppendAllText(outPath, "SET IDENTITY_INSERT [dbo].[def_ItemResults] ON\nGO\n");
        //    System.IO.File.AppendAllText(outPath, "SET IDENTITY_INSERT [dbo].[def_ResponseVariables] ON\nGO\n");

        //    foreach( def_ItemResults ir in fr.def_ItemResults ){
        //        System.IO.File.AppendAllText( outPath,
        //          "INSERT INTO [dbo].[def_ItemResults]([itemResultId],[formResultId],[itemId],[sessionStatus],[dateUpdated])\n"
        //        + "VALUES('" + irId + "','" + fr.formResultId + "','" + ir.itemId + "','" + ir.sessionStatus + "','" + ir.dateUpdated + "')\nGO\n" );

        //        foreach (def_ResponseVariables rv in ir.def_ResponseVariables)
        //        {
        //            System.IO.File.AppendAllText(outPath,
        //              "INSERT INTO [dbo].[def_ResponseVariables]([responseVariableId],[itemResultId],[itemVariableId],[rspInt],[rspFloat],[rspDate],[rspValue])\n"
        //            + "VALUES('" + rvId + "','" + irId + "','" + rv.itemVariableId + "','" + rv.rspInt + "','" + rv.rspFloat + "','" + rv.rspDate + "','" + ToLiteral(rv.rspValue) + "')\nGO\n");
        //            rvId++;
        //        }

        //        irId++;
        //    }
            
        //    System.IO.File.AppendAllText(outPath, "SET IDENTITY_INSERT [dbo].[def_ItemResults] OFF\nGO\n");
        //    System.IO.File.AppendAllText(outPath, "SET IDENTITY_INSERT [dbo].[def_ResponseVariables] OFF\nGO\n");
        //}

        //http://stackoverflow.com/questions/323640/can-i-convert-a-c-sharp-string-value-to-an-escaped-string-literal
        private static string ToLiteral(string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }

        private static readonly string[] headersToIgnore = { "ent_id", "sub_id", "user_id", "Training" };
        private static readonly string[] headerPartsToIgnore = { "_Score_" };

        private string getItemVariableIdentifierFromCsvHeader(string header)
        {
            foreach (string ignore in headerPartsToIgnore)
                if (header.Contains(ignore))
                    return null;
            foreach (string ignore in headersToIgnore)
                if (header.Equals(ignore))
                    return null;

            string[] parts = header.Split('_');

            //supporter/respondant items from profile page
            if (header.StartsWith("sis_sup") || header.StartsWith("sis_res") )
            {
                string prefix = parts[0] + "_" + parts[1];
                return prefix + "_" + convertOldToNewSuffix(header.Substring(prefix.Length + 1));
            }

            if (header.Equals("s3a_16_other"))
                return "S1aPageOther";
            if (header.Equals("s3b_13_other"))
                return "S1bPageOther";

            //section 1-3
            if (parts[0].StartsWith("s1") || parts[0].StartsWith("s2") || parts[0].StartsWith("s3"))
            {
                if (parts[0].Length < 3) 
                    parts[0] += "a"; 

                int newSystemSectionNum = mapOldSectionNumToNewSystem(Int16.Parse(parts[0].Substring(1, 1)));
                if (parts[1].Equals("PageNotes"))
                    return "txtS" + newSystemSectionNum + parts[0].Substring(2, 1).ToLower() + "PageNotes";

                string prefix;

                if (header.StartsWith("s3a_16_"))
                    prefix = "Q1A19";
                else
                {
                    prefix = "Q" + newSystemSectionNum + parts[0].Substring(2, 1).ToUpper() +
                    mapOldQuestionNumToNewSystem(
                        Int16.Parse(parts[1]), newSystemSectionNum,
                        parts[0].Substring(2, 1).ToUpper()).ToString();
                }

                string suffix;
                if( parts[2].Equals( "support" ) )
                    suffix = parts[0].StartsWith("s3a") ? "ExMedSupport" : "ExBehSupport";
                else
                    suffix = convertOldToNewSuffix(parts[2]);
                return prefix + "_" + suffix;
            }

            //CustomORL (supplemental questions)
            if (header.StartsWith("Question"))
            {
                string shortHeader = header.Replace("Question", "");
                int questionNumber = Convert.ToInt16(shortHeader.Substring(0, 1));

                //"Question1", "Question2", ...
                if (shortHeader.Length == 1)
                {
                    return "Q4A" + questionNumber + "v1";
                }

                //"Question1a", "Question2b", ...
                else if (shortHeader.Length == 2)
                {
                    string questionLetter = shortHeader.Substring(1, 1);
                    return "sis_s4" + questionNumber + questionLetter;
                }

                //"Question1d_hrs", "Question2d_hrs", ...
                else if (shortHeader.EndsWith("d_hrs"))
                {
                    return "sis_s4" + questionNumber + "d2";
                }

                //"Question1Notes", "Question2Notes", ...
                else if (shortHeader.EndsWith("Notes"))
                {
                    return "sis_s4" + questionNumber + "n";
                }
            }
            else if (header.Equals("PageNotes"))
            {
                return "txtS4aPageNotes";
            }

            return convertToNewIdentifier(header);
        }

        private static readonly int oldToNewSuffixMapLength = 20;
        private static readonly string[,] oldToNewSuffixMap = {
            { "to", "ImportantTo" },
            { "for", "ImportantFor" },
            { "tos", "TOS" },
            { "dst", "DST" },
            { "notes", "Notes" },
            { "fqy", "Fqy" },
            { "reln_typ_cd", "reln" },
            { "reln_typ_cd1", "reln" },
            { "name", "name" },
            { "lang_spoken_cd", "lang" },
            { "lang_spoken_cd1", "lang" },
            { "phone_num_ext", "ext" },
            { "phone_num", "phone" },
            { "last_nm", "lastn" },
            { "first_nm", "firstn" },
            { "last_nm1", "lastn" },
            { "first_nm1", "firstn" },
            { "agency_code", "agen" },
            { "agency", "agen" },
            { "email", "email" },
        };

        private string convertOldToNewSuffix(string oldSuffix)
        {
            for (int i = 0; i < oldToNewSuffixMapLength; i++)
            {
                if (oldToNewSuffixMap[i, 0].Equals(oldSuffix))
                    return oldToNewSuffixMap[i, 1];
            }

            throw new Exception( "could not find mapping for old-system suffix \"" + oldSuffix + "\"" );
        }
    }
}
