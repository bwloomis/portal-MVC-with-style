using AJBoggs.Sis.Domain;

using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Abstract;
using Data.Concrete;

using Kent.Boogaart.KBCsv;
using Kent.Boogaart.KBCsv.Extensions.Data;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Mvc;



namespace Assmnts.Controllers
{
    /*
     *  The methods in this controller are used to exchange SQL tables with Venture (local DEF3).
     *      The index methods displays a screen to manage the synching (updload / download process).
     * 
     */
    [RedirectingAction]
    public partial class DataSyncController : Controller
    {

        IFormsSql formsSql = new FormsSql();
        string sisOnlineURL = ConfigurationManager.AppSettings["SISOnlineURL"];
        IFormsRepository formsRepo;

        public DataSyncController()
        {
            formsRepo = null;
        }

        public DataSyncController(IFormsRepository fr)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
        }

        private static DataTable RemoveNullString(DataTable dt)
        {
            if (dt == null)
            {
                return dt;
            }

            for (int a = 0; a < dt.Rows.Count; a++)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if ( String.IsNullOrEmpty((string)dt.Rows[a][i]) )
                    {
                        dt.Rows[a][i] = DBNull.Value;
                    }
                }
            }

            return dt;
        }


        // GET: DataSync
        public ActionResult Index()
        {
            List<DataSync> items = new List<DataSync>();

            for (int resultTableIndex = 0; resultTableIndex < formsSql.GetNumberOfResultsTables(); resultTableIndex++)
            {
                DataSync ds = new DataSync
                {
                    ID = resultTableIndex,
                    tableName = formsSql.GetResultsTableName(resultTableIndex),
                    SISOnlineCnt = 0,
                    VentureCnt = formsSql.GetTableRecordCountVenture(formsSql.GetResultsTableName(resultTableIndex)),
                    SyncStatus = String.Empty
                };

                CookieContainer cc = new CookieContainer();
                try
                {
                    string lnk = ConfigurationManager.AppSettings["SISOnlineUrl"] + "Defws/Login?UserId=" + SessionHelper.LoginInfo.LoginID + "&pwrd=" + SessionHelper.LoginInfo.Password;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(lnk);
                    request.Timeout = -1;
                    request.CookieContainer = cc;

                    using (WebResponse response = request.GetResponse())
                    {
                        Debug.WriteLine("DataSync HttpWebResponse Status: " + ((HttpWebResponse)response).StatusDescription);
                        using (Stream dataStream = ((HttpWebResponse)response).GetResponseStream())
                        {

                        }
                    }
                }
                catch (Exception xcptn)
                {
                    Debug.WriteLine("DataSync HttpWebRequest: " + xcptn.Message);
                }

                try
                {
                    HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(sisOnlineURL + "DataSync/GetResultsDataTableCount?fileId=" + resultTableIndex.ToString());
                    httpRequest.Method = WebRequestMethods.Http.Get;
                    httpRequest.CookieContainer = cc;
                    // Get back the HTTP response for web server
                    using (HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                    {
                        using (Stream httpResponseStream = httpResponse.GetResponseStream())
                        {
                            byte[] rsp = new byte[httpResponse.ContentLength];
                            httpResponseStream.Read(rsp, 0, (int)httpResponse.ContentLength);

                            Debug.WriteLine("DataSync.Index HttpWebResponse Status: " + httpResponse.StatusDescription);
                            Debug.WriteLine(System.Text.Encoding.UTF8.GetString(rsp));

                            int onlineCnt = Convert.ToInt32(System.Text.Encoding.UTF8.GetString(rsp));
                            ds.SISOnlineCnt = onlineCnt;

                            ds.VentureCnt = formsSql.GetTableRecordCountVenture(formsSql.GetResultsTableName(resultTableIndex));
                        }
                    }
                }
                catch (System.Net.WebException wex)
                {
                    ds.SyncStatus = "SIS Server not found.";
                    Debug.WriteLine("* * *  DataSync index exception " + wex.Message);
                }
                catch (Exception excptn)
                {
                    ds.SyncStatus = excptn.Message;
                    Debug.WriteLine("* * *  DataSync index exception: " + excptn.Message);
                }
                items.Add(ds);

            }

            //Session["timeout"] = Business.Timeout.GetTotalTimeoutMinutes(SessionHelper.LoginStatus.EnterpriseID);
            //// Added by RRB 8/25/15 as an example of how to use it.
            //SessionHelper.SessionTotalTimeoutMinutes = Business.Timeout.GetTotalTimeoutMinutes(SessionHelper.LoginStatus.EnterpriseID);

            bool justLoggedIn = SessionHelper.Read<bool>("justLoggedIn");

            DataSync dsc = new DataSync();

            if (justLoggedIn)
            {
                dsc.autoDownload = true;
                SessionHelper.Write("justLoggedIn", false);
            }
            ViewBag.TableCnt = items.Count;
            ViewBag.ResultTable = items;


            return View("indexSync", dsc);
        }


        [HttpGet]
        public ActionResult MetaSync()
        {
            List<DataSync> metaItems = new List<DataSync>();

            for (int metaTableIndex = 0; metaTableIndex < formsSql.GetNumberOfMetaTables(); metaTableIndex++)
            {
                DataSync ds = new DataSync
                {
                    ID = metaTableIndex,
                    tableName = formsSql.GetMetaDataTableName(metaTableIndex),
                    SISOnlineCnt = 0,
                    VentureCnt = formsSql.GetTableRecordCountVenture(formsSql.GetMetaDataTableName(metaTableIndex)),
                    SyncStatus = String.Empty
                };

                CookieContainer cc = new CookieContainer();
                try
                {
                    string lnk = ConfigurationManager.AppSettings["SISOnlineUrl"] + "Defws/Login?UserId=" + SessionHelper.LoginInfo.LoginID + "&pwrd=" + SessionHelper.LoginInfo.Password;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(lnk);
                    request.Timeout = -1;
                    request.CookieContainer = cc;

                    Session["CookieContainer"] = cc;

                    using (WebResponse response = request.GetResponse())
                    {
                        Debug.WriteLine("DataSync HttpWebResponse Status: " + ((HttpWebResponse)response).StatusDescription);
                        using (Stream dataStream = ((HttpWebResponse)response).GetResponseStream())
                        {

                        }
                    }
                }
                catch (Exception xcptn)
                {
                    Debug.WriteLine("DataSync HttpWebRequest: " + xcptn.Message);
                }

                try
                {
                    HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(sisOnlineURL + "DataSync/GetMetaDataTableCount?fileId=" + metaTableIndex.ToString());
                    httpRequest.Method = WebRequestMethods.Http.Get;
                    httpRequest.CookieContainer = cc;
                    // Get back the HTTP response for web server
                    using (HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                    {
                        using (Stream httpResponseStream = httpResponse.GetResponseStream())
                        {
                            Debug.WriteLine("DataSync.MetaSync HttpWebResponse Status: " + httpResponse.StatusDescription);

                            byte[] rsp = new byte[httpResponse.ContentLength];
                            httpResponseStream.Read(rsp, 0, (int)httpResponse.ContentLength);


                            int onlineCnt = Convert.ToInt32(System.Text.Encoding.UTF8.GetString(rsp));
                            ds.SISOnlineCnt = onlineCnt;

                            ds.VentureCnt = formsSql.GetTableRecordCountVenture(formsSql.GetMetaDataTableName(metaTableIndex));
                        }
                    }
                }
                catch (System.Net.WebException wex)
                {
                    ds.SyncStatus = "SIS Server not found.";
                    Debug.WriteLine("* * *  DataSync index exception " + wex.Message);
                }
                catch (Exception excptn)
                {
                    ds.SyncStatus = excptn.Message;
                    Debug.WriteLine("* * *  DataSync index exception: " + excptn.Message);
                }

                metaItems.Add(ds);

            }

            DataSync dsc = new DataSync();

            ViewBag.MetaCnt = metaItems.Count;
            ViewBag.MetaTable = metaItems;

            return View("metaSync", dsc);
        }

        [HttpGet]
        public string GetResultsDataTableCount()
        {
            string paramFileId = Request["fileId"] as string;
            Debug.WriteLine("* * *  ExportController GetResultsDataTableCount fileId: " + paramFileId);
            int iFileId = Convert.ToInt32(paramFileId);
            // IFormsSql formsSql = new FormsSql();
            int rowCount = formsSql.GetTableRecordCount(formsSql.GetResultsTableName(iFileId));

            return rowCount.ToString();
        }

        [HttpGet]
        public string GetMetaDataTableCount()
        {
            string paramFileId = Request["fileId"] as string;
            Debug.WriteLine("* * *  ExportController GetMetaDataTableCount fileId: " + paramFileId);
            int iFileId = Convert.ToInt32(paramFileId);
            // IFormsSql formsSql = new FormsSql();
            int rowCount = formsSql.GetTableRecordCount(formsSql.GetMetaDataTableName(iFileId));

            return rowCount.ToString();
        }

        public string GetResultTblCnt(string index)
        {
            return formsSql.GetTableRecordCountVenture(formsSql.GetResultsTableName(Convert.ToInt32(index))).ToString();
        }

        public string GetMetaTblCnt(string index)
        {
            return formsSql.GetTableRecordCountVenture(formsSql.GetMetaDataTableName(Convert.ToInt32(index))).ToString();
        }

        [HttpPost]
        public string DeleteTableContent()
        {
            DataSync ds = new DataSync();
            int overrideDelete = Int32.Parse(Request["override"] as string);

            string msg = null;
            if (ds.formStatusUploaded() || overrideDelete == 1)
            {
                string paramTableName = Request["tableName"] as string;
                Debug.WriteLine("* * *  DataSync DeleteTableContent tableName; " + paramTableName);

                // IFormsSql formsSql = new FormsSql();
                // *** this needs to be uncommented for Production
                int deletedRecordCount = formsSql.DeleteTableContent(paramTableName);
                //int deletedRecordCount = 23;
                msg = deletedRecordCount.ToString() + " records deleted from table " + paramTableName;
            }
            else
            {
                msg = "Cannot delete records because non-uploaded form results present.";
            }
            return msg;
        }



        /// <summary>
        /// Downloads only FormResults, ItemResults, and ResponseVariables DEF tables.
        /// Runs in Venture mode only.
        /// </summary>
        /// <param name="fileId">The file or table id in the  Data.FormsSql formResultsTables.</param>
        /// <param name="tableName">The name of the DEF table </param>
        [HttpGet]
        public string DownloadTable()
        {
            string paramFileId = Request["fileId"] as string;
            int iFileId = Convert.ToInt32(paramFileId);
            string paramTableName = Request["tableName"] as string;
            Debug.WriteLine("* * *  DataSync DownloadTable fileId: " + iFileId.ToString());
            Debug.WriteLine("* * *  DataSync DownloadTable tableName: " + paramTableName);

            string msg = String.Empty;
            
            // If the first table to download, login to the remote/master server.
            if (paramTableName == "def_FormResults")
            {
                try
                {
                    CookieContainer cc = new CookieContainer();

                    string lnk = ConfigurationManager.AppSettings["SISOnlineUrl"] + "Defws/Login?UserId=" + SessionHelper.LoginInfo.LoginID + "&pwrd=" + SessionHelper.LoginInfo.Password;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(lnk);
                    request.Timeout = -1;
                    request.CookieContainer = cc;

                    Session["CookieContainer"] = cc;

                    using (WebResponse response = request.GetResponse())
                    {
                        Debug.WriteLine("DataSync HttpWebResponse Status: " + ((HttpWebResponse)response).StatusDescription);
                        using (Stream dataStream = ((HttpWebResponse)response).GetResponseStream())
                        {

                        }
                    }

                }
                catch (Exception xcptn)
                {
                    Debug.WriteLine("DataSync.DownloadTable Defws Login HttpWebRequest: " + xcptn.Message);
                }

            }

            try
            {
                // Construct HTTP request to get the file
                string requestUrl = sisOnlineURL + "Export/GetResponseCsv?fileId=" + iFileId.ToString() + "&ventureVersion=2";
                Debug.WriteLine("DataSyncController - DownloadTable requestUrl: " + requestUrl);

                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(requestUrl);

                httpRequest.Method = WebRequestMethods.Http.Get;
                httpRequest.CookieContainer = (CookieContainer)Session["CookieContainer"];

                // Get back the HTTP response for web server
                using (HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                {
                    using (Stream httpResponseStream = httpResponse.GetResponseStream())
                    {

                        DataTable csvData = new DataTable();

                        using (var reader = new CsvReader(httpResponseStream))
                        {
                            // the CSV file has a header record, so we read that first
                            reader.ReadHeaderRecord();
                            csvData.Fill(reader);
                        }

                        int rspCnt = csvData.Rows.Count;
                        msg = csvData.Rows.Count.ToString();
                        csvData = RemoveNullString(csvData);
                        csvData.TableName = formsSql.GetResultsTableName(iFileId);
                        Debug.WriteLine("Table {0} contains {1} rows.", csvData.TableName, csvData.Rows.Count);

                        if (rspCnt > 0)
                        {
                            using (DbConnection connection = UasAdo.GetUasAdoConnection())
                            {
                                FilterPreExistingOnVenture(ref csvData);
                                
                                MarkDuplicateResults(ref csvData);

                                if (csvData.TableName == "def_FormResults")
                                {
                                    SetLastModifiedByUserIdToOne(ref csvData);
                                }

                                FillDatabaseTable(csvData, connection);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  DataSync.DownloadTable() exception: " + ex.Message);
                msg = ex.Message;
                if (ex.InnerException != null && ex.InnerException.Message != null)
                    Debug.WriteLine("* * *  DataSync.DownloadTable() InnerException: " + ex.InnerException.Message);
                return msg;
            }

            if (paramTableName == "def_ResponseVariables")
            {
                try
                { // Construct HTTP request to get the file
                    HttpWebRequest httpRequest = (HttpWebRequest)
                    WebRequest.Create(sisOnlineURL + "Export/ChangeStatusToCheckedOut");
                    httpRequest.Method = WebRequestMethods.Http.Get;
                    httpRequest.CookieContainer = (CookieContainer)Session["CookieContainer"];

                    // Get back the HTTP response for web server
                    using (WebResponse response = httpRequest.GetResponse())
                    {
                        Debug.WriteLine("DataSync HttpWebResponse Status: " + ((HttpWebResponse)response).StatusDescription);
                        using (Stream dataStream = ((HttpWebResponse)response).GetResponseStream())
                        {

                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("* * *  DataSync.DownloadTable() exception: " + ex.Message);
                    //msg = ex.Message;
                    //if (ex.InnerException != null && ex.InnerException.Message != null)
                    //    Debug.WriteLine("* * *  DataSync.DownloadTable() InnerException: " + ex.InnerException.Message);

                }
            }
            return msg;
        }

       
        private void MarkDuplicateResults(ref DataTable csvData)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM " + csvData.TableName;
                        command.CommandType = CommandType.Text;

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  Mark Duplicates: " + ex.Message);
            }

            int index = 0;

            if (csvData.TableName == "Contact" || csvData.TableName == "Recipient")
            {
                index = 1;
            }
            if (csvData.TableName == "def_FormResults" || csvData.TableName == "Contact" || csvData.TableName == "Recipient")
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string expression = dataTable.Columns[index].ColumnName + " = " + "'" + row[index].ToString() + "'";
                    DataRow[] rowsToUpdate = csvData.Select(expression);

                    foreach (DataRow r in rowsToUpdate)
                    {
                        r.AcceptChanges();
                        r.SetModified();

                    }

                }
            }
            else if (csvData.TableName == "def_ItemResults")
            {
                foreach (DataRow row in csvData.Rows)
                {
                    string expression = dataTable.Columns[1].ColumnName + " = " + "'" + row[1].ToString() + "' AND " + dataTable.Columns[2].ColumnName + " = " + "'" + row[2].ToString() + "'";
                    DataRow[] rowsToUpdate = dataTable.Select(expression);

                    foreach (DataRow r in rowsToUpdate)
                    {
                        r.AcceptChanges();
                        r.SetModified();
                    }
                }

                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();
                    DeleteDatabaseRecords(dataTable, connection);
                }
               
            } 
            else if (csvData.TableName == "def_ResponseVariables")
            {
                foreach (DataRow row in csvData.Rows)
                {
                    string expression = dataTable.Columns[1].ColumnName + " = " + "'" + row[1].ToString() + "' AND " + dataTable.Columns[2].ColumnName + " = " + "'" + row[2].ToString() + "'";
                    DataRow[] rowsToUpdate = dataTable.Select(expression);

                    foreach (DataRow r in rowsToUpdate)
                    {
                        r.AcceptChanges();
                        r.SetModified();
                    }

                }

                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();
                    DeleteDatabaseRecords(dataTable, connection);
                }
            }

        }
        
        private void FilterPreExistingOnVenture(ref DataTable csvData)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT " + csvData.TableName + ".* FROM " + csvData.TableName;
                        command.CommandText += formsSql.GenerateQuery(csvData.TableName);
                        command.CommandType = CommandType.Text;

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * * Take out assessments with non-deleted status on Venture: " + ex.Message);

                throw ex;
            }

            int index = 0;
            if ((csvData.TableName == "Contact") || (csvData.TableName == "Recipient"))
            {
                index = 1;
            }

            foreach (DataRow row in dataTable.Rows)
            {
                string expression = dataTable.Columns[index].ColumnName + " = " + "'" + row[index].ToString() + "'";
                DataRow[] rowsToUpdate = csvData.Select(expression);

                foreach (DataRow r in rowsToUpdate)
                {
                    r.AcceptChanges();
                    r.Delete();
                }

            }

            csvData.AcceptChanges();
        }

        //private void FilterNonNewOnVenture(ref DataTable csvData)
        //{
        //    DataTable dataTable = new DataTable();

        //    try
        //    {
        //        using (DbConnection connection = UasAdo.GetUasAdoConnection())
        //        {
        //            connection.Open();

        //            using (DbCommand command = connection.CreateCommand())
        //            {
        //                command.CommandText = "SELECT " + csvData.TableName + ".* FROM " + csvData.TableName;
        //                command.CommandText += formsSql.GenerateQuery(csvData.TableName);
        //                command.CommandText += " AND formStatus <> " + (byte)FormResults_formStatus.NEW;
        //                command.CommandType = CommandType.Text;

        //                using (DbDataReader reader = command.ExecuteReader())
        //                {
        //                    dataTable.Load(reader);
        //                }
        //            }

        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("* * * Take out assessments with non-new or non-deleted status on Venture: " + ex.Message);

        //        throw ex;
        //    }

        //    int index = 0;
        //    if ( (csvData.TableName == "Contact") || (csvData.TableName == "Recipient") )
        //    {
        //        index = 1;
        //    }

        //    foreach (DataRow row in dataTable.Rows)
        //    {
        //        string expression = dataTable.Columns[index].ColumnName + " = " + "'" + row[index].ToString() + "'";
        //        DataRow[] rowsToUpdate = csvData.Select(expression);

        //        foreach (DataRow r in rowsToUpdate)
        //        {
        //            r.AcceptChanges();
        //            r.Delete();
        //        }

        //    }

        //    csvData.AcceptChanges();
        //}

        public void DeleteAllMetaTables()
        {
            for (int i = formsSql.GetNumberOfMetaTables() - 1; i >= 0; i--)
            {
                string tableName = formsSql.GetMetaDataTableName(i);
                int recordCount = formsSql.DeleteTableContent(tableName);

                Debug.WriteLine("DataSync: Deleted " + recordCount + " records from table " + tableName);
            }
        }

        public void DownloadAllMetaTables(CookieContainer cc)
        {
            for (int i = 0; i < formsSql.GetNumberOfMetaTables(); i++)
            {
                string tableName = formsSql.GetMetaDataTableName(i);
                Debug.WriteLine("DataSync: Downloading table " + tableName);
                ProcessMetaSync(cc, i);
            }
        }

        /// <summary>
        /// Downloads DEF meta data tables (Forms, Parts, Sections, Items, etc.)
        /// Runs only in Venture mode.
        /// </summary>
        /// <param name="fileId">The meta data table array number in Data.Concrete.FormsSql.cs</param>
        [HttpGet]
        public string DownloadMetaTable()
        {

            string paramFileId = Request["fileId"] as string;
            int iFileId = Convert.ToInt32(paramFileId);
            string paramTableName = Request["tableName"] as string;
            Debug.WriteLine("* * *  DataSync DownloadTable fileId: " + iFileId.ToString());
            Debug.WriteLine("* * *  DataSync DownloadTable tableName: " + paramTableName);

            string msg = String.Empty;

            CookieContainer cc = new CookieContainer();

            // Login to the remote master server
            try
            {
                string lnk = ConfigurationManager.AppSettings["SISOnlineUrl"] + "Defws/Login?UserId=" + SessionHelper.LoginInfo.LoginID + "&pwrd=" + SessionHelper.LoginInfo.Password;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(lnk);
                request.Timeout = -1;
                request.CookieContainer = cc;

                using (WebResponse response = request.GetResponse())
                {
                    Debug.WriteLine("DataSync HttpWebResponse Status: " + ((HttpWebResponse)response).StatusDescription);
                    using (Stream dataStream = ((HttpWebResponse)response).GetResponseStream())
                    {

                    }
                }
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("DataSync HttpWebRequest: " + xcptn.Message);
            }

            return ProcessMetaSync(cc, iFileId);
        }

        /// <summary>
        /// Downloads the DEF Meta Data file in CSV format and loads into the local Venture table.
        /// </summary>
        /// <param name="cc"></param>
        /// <param name="iFileId"></param>
        /// <returns></returns>
        public string ProcessMetaSync(CookieContainer cc, int iFileId)
        {
            string msg = String.Empty;

            try
            {
                // Construct HTTP request to get the file
                string requestUrl = sisOnlineURL + "Export/GetMetaCsv?fileId=" + iFileId.ToString() + "&ventureVersion=2";
                Debug.WriteLine("DataSyncController - ProcessMetaSync requestUrl: " + requestUrl);

                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create( Uri.EscapeUriString(requestUrl) );
                httpRequest.Method = WebRequestMethods.Http.Get;
                httpRequest.CookieContainer = cc;

                // Get back the HTTP response for web server
                using (HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                {
                    using (Stream httpResponseStream = httpResponse.GetResponseStream())
                    {
                        DataTable csvData = new DataTable();

                        using (var reader = new CsvReader(httpResponseStream))
                        {
                            // the CSV file has a header record, so we read that first
                            reader.ReadHeaderRecord();
                            csvData.Fill(reader);
                        }

                        msg = csvData.Rows.Count.ToString();
                        csvData = RemoveNullString(csvData);
                        csvData.TableName = formsSql.GetMetaDataTableName(iFileId);
                        Debug.WriteLine("Table {0} contains {1} rows.", csvData.TableName, msg);
                        if (csvData.TableName == "def_Sections")
                        {
                            MakeReportsInvisible(ref csvData);
                        }

                        MarkDuplicates(ref csvData);

                        using (DbConnection connection = UasAdo.GetUasAdoConnection())
                        {
                            FillDatabaseTable(csvData, connection);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  DataSync.ProcessMetaSync exception: " + ex.Message);
                msg = ex.Message;
            }
            
            return msg;
        }

        private void MakeReportsInvisible(ref DataTable csvData)
        {
            int[] reportIds = new int[3];

            reportIds[0] = 38;
            reportIds[1] = 94;
            reportIds[2] = 89;

            foreach (int rId in reportIds)
            {
                string expression = csvData.Columns[0].ColumnName + " = " + "'" + rId + "'";
                DataRow[] rowsToUpdate = csvData.Select(expression);

                foreach (DataRow r in rowsToUpdate)
                {
                    r["visible"] = false;
                    r.AcceptChanges();
                }
            }
        }

        /// <summary>
        /// Downloads the UAS tables.
        /// 
        /// Loops through all of the UAS tables from the UasSql class string array variable UasTables
        /// and requests an export, using the index of each entry in the array, from the version of the 
        /// app running at the sisOnlineUrl.    
        /// 
        /// </summary>
        /// <param name="cc">Cookie container from request used to log in to SisOnline at sisOnlineUrl</param>
        /// <returns></returns>
        [HttpGet]
        public string DownloadUasTables(CookieContainer cc)
        {
            // fileIds and filters
            // 0 - Config - All recs
            // 1 - Enterprise - a single record - the Enterprise of the User (from Session)
            // 2 and 3 - GroupTypes / Groups for the Enterprise
            // 4 - User - just the user logged in (from Session)

            IUasSql uas = new UasSql();
            string msg = String.Empty;

            for (int i = 0; i < uas.GetNumberTables(); i++)
            {
                try
                {
                    HttpWebRequest httpRequest = null;

                    httpRequest = (HttpWebRequest)WebRequest.Create(sisOnlineURL + "Export/GetUasCsv?fileId=" + i.ToString());
                    httpRequest.CookieContainer = cc;
                    httpRequest.Method = WebRequestMethods.Http.Get;

                    // Get back the HTTP response for web server
                    using (HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                    {
                        using (Stream httpResponseStream = httpResponse.GetResponseStream())
                        {
                            DataTable csvData = new DataTable();

                            csvData.TableName = uas.GetUasTableName(i);
                            Debug.WriteLine("DataSync DownloadUasTables CvsReader on table: " + csvData.TableName);
                            // Debug.WriteLine("                          httpResponseStream.Length: " + httpResponseStream.Length.ToString());

                            using (var reader = new CsvReader(httpResponseStream))
                            {
                                // the CSV file has a header record, so we read that first
                                reader.ReadHeaderRecord();
                                csvData.Fill(reader);
                            }

                            Debug.WriteLine("        cvsData Table {0} contains {1} rows.", csvData.TableName, csvData.Rows.Count);
                            msg = csvData.Rows.Count.ToString();
                            csvData = RemoveNullString(csvData);

                            Debug.WriteLine("       Modify csvData: " + csvData.TableName);
                            if (csvData == null)
                                Debug.WriteLine("       uh-oh csvData is Null!!!");

                            using (DbConnection connection = UasAdo.GetUasAdoConnection())
                            {

                                if (csvData.TableName == "uas_Group")
                                {
                                    SetParentGroupToSelf(ref csvData);
                                }
                                
                                SetCreatedByAndModifiedByToOne(ref csvData);
                                
                                MarkDuplicates(ref csvData);

                                FillDatabaseTable(csvData, connection);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("* * *  DataSync exception: " + ex.Message);
                    if (ex.InnerException != null && ex.InnerException.Message != null)
                    {
                        Debug.WriteLine("Inner exception: " + ex.InnerException.Message);
                    }
                    msg = ex.Message;
                }
            }
            return msg;
        }

        private void SetParentGroupToSelf(ref DataTable csvData)
        {
            DataRow[] uasRow = csvData.Select("ParentGroupId <> GroupID AND ParentGroupId <> 0");

            foreach (DataRow row in uasRow)
            {
                row["ParentGroupId"] = row["GroupID"];
                row.AcceptChanges();
            }
        }

        private void SetLastModifiedByUserIdToOne(ref DataTable csvData)
        {
            DataRow[] uasRow = csvData.Select("LastModifiedByUserId <> 1");

            foreach (DataRow row in uasRow)
            {
                row["LastModifiedByUserId"] = 1;
                row.AcceptChanges();
           }
        }

        private void SetCreatedByAndModifiedByToOne(ref DataTable csvData)
        {
            DataRow[] uasRow = csvData.Select("CreatedBy <> 1 OR ModifiedBy <> 1");

            foreach (DataRow row in uasRow)
            {
                row["ModifiedBy"] = 1;
                row["CreatedBy"] = 1;
                row.AcceptChanges();
            }
        }

        /// <summary>
        /// Marks any incoming rows that are already in the local database.
        /// </summary>
        /// <param name="csvData"></param>

        private void MarkDuplicates(ref DataTable csvData)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM " + csvData.TableName;
                        command.CommandType = CommandType.Text;

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  Mark Duplicates: " + ex.Message);
            }

            int index = 0;
            if (csvData.TableName == "Contact" || csvData.TableName == "Recipient")
            {
                index = 1;
            }

            foreach (DataRow row in dataTable.Rows)
            {
                string expression = dataTable.Columns[index].ColumnName + " = " + "'" + row[index].ToString() + "'";
                DataRow[] rowsToUpdate = csvData.Select(expression);

                foreach (DataRow r in rowsToUpdate)
                {
                    r.AcceptChanges();
                    r.SetModified();

                }

            }
        }


          public virtual void FillDatabaseTable(DataTable table, DbConnection connection)
        {
            connection.Open();
            int column = 0;

            if (table.TableName == "Contact" || table.TableName == "Recipient")
            {
                column = 1;
            }

            Debug.WriteLine("* * *  FillDatabaseTable: " + table.TableName);
            formsSql.SetIdentityInsert(table.TableName, table.Columns[column].ColumnName, true);
            try
            {
                FillDatabaseRecords(table, connection);
            }
            catch (Exception ex)
            {
                if (table.TableName == "def_ItemResults" || table.TableName == "def_ResponseVariables" || table.TableName == "def_FormResults")
                {
                    formsSql.SetIdentityInsert(table.TableName, table.Columns[column].ColumnName, false);
                }

                throw ex;

            }

            if (table.TableName == "def_ItemResults" || table.TableName == "def_ResponseVariables" || table.TableName == "def_FormResults")
            {
                formsSql.SetIdentityInsert(table.TableName, table.Columns[column].ColumnName, false);
            }

            connection.Close();
        }


        private void DeleteDatabaseRecords(DataTable table, DbConnection connection) 
        {
            var command = connection.CreateCommand();
            command.Connection = connection; //todo combine this sequence
            var rawCommandTextDelete = string.Format("delete from {0} where {1} = {2}", table.TableName, "{0}", "{1}");
            
            //Debug.WriteLine(rawCommandText);
            foreach (var row in table.AsEnumerable())
            {

                //if (GetFormStatus(row, connection) != FormResults_formStatus.NEW)
                //{
                //    row.AcceptChanges();
                //    continue;
                //}
                
                if (row.RowState == DataRowState.Modified)
                {
                    DeleteRow(row, command, rawCommandTextDelete);
                }

            }
        }

        private FormResults_formStatus GetFormStatus(DataRow row, DbConnection connection)
        {
            if (row.Table.TableName == "def_ItemResults")
            {
 
                try {
                    connection.Open();
                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT formStatus FROM def_FormResults WHERE formResultId = " + row[1];
                        command.CommandType = CommandType.Text;

                        int formStatus = Int32.Parse(command.ExecuteScalar().ToString());

                        return (FormResults_formStatus)formStatus;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("* * *  Get Form Status: " + ex.Message);
                }
            }

            if (row.Table.TableName == "def_ResponseVariables")
            {
                try
                {
                    connection.Open();
                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT formStatus FROM def_FormResults fr JOIN def_ItemResults ir ON ir.formResultId = fr.formResultId  WHERE itemResultId = " + row[1];
                        command.CommandType = CommandType.Text;

                        int formStatus = Int32.Parse(command.ExecuteScalar().ToString());

                        return (FormResults_formStatus)formStatus;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("* * *  Get Form Status: " + ex.Message);
                }
            }

            return FormResults_formStatus.IN_PROGRESS;
        }



        private void DeleteRow(DataRow row, DbCommand command, string rawCommandTextDelete)
        {
            try
            {
                command.CommandText = string.Format(rawCommandTextDelete, row.Table.Columns[0], row[0]);
                Debug.WriteLine(command.CommandText);
                command.ExecuteNonQuery();
            }
            catch (Exception xcptn)
            {

                Debug.WriteLine("* * *  DataSync UpdateDeleteRecord exception: " + xcptn.Message);

                if (xcptn.InnerException != null && xcptn.InnerException.Message != null)
                {
                    Debug.WriteLine("Inner Exception: " + xcptn.InnerException.Message);
                }

                throw xcptn;
            }
        }
        
        
        private void FillDatabaseRecords(DataTable table, DbConnection connection)
        {
            var command = connection.CreateCommand();
            command.Connection = connection; //todo combine this sequence
            var rawCommandTextInsert = string.Format("insert into {0} ({1}) values ({2})", table.TableName, GetTableColumns(table), "{0}");
            var rawCommandTextUpdate = string.Format("update {0} set {1} where {2} = {3}", table.TableName, "{0}", "{1}", "{2}");
            foreach (var row in table.AsEnumerable())
            {
                if (row.RowState == DataRowState.Modified)
                {
                   // Debug.WriteLine(rawCommandTextUpdate);
                    UpdateDatabaseRecord(row, command, rawCommandTextUpdate);
                }
                else if (row.RowState != DataRowState.Deleted)
                {
                  //  Debug.WriteLine(rawCommandTextInsert);
                    FillDatabaseRecord(row, command, rawCommandTextInsert);
                }

            }
        }

        private void UpdateDatabaseRecord(DataRow row, DbCommand command, string rawCommandTextUpdate)
        {
            string valueList = String.Empty;

            try
            {
                List<string> columns = new List<string>();
                foreach (DataColumn dc in row.Table.Columns)
                {
                    columns.Add(dc.ColumnName);
                }
                List<string> values = row.ItemArray.Select(i => ItemToSqlFormattedValue(i)).ToList();

                int keyIndex = 0;

                if (row.Table.TableName == "Contact" || row.Table.TableName == "Recipient")
                {
                    keyIndex = 1;
                }

                for (int i = 0; i < columns.Count - 1; i++)
                {
                    if (i != keyIndex)
                    {
                        valueList += "[" + columns[i] + "] = " + values[i] + ", ";
                    }
                }

                valueList += "[" + columns[columns.Count - 1] + "] = " + values[columns.Count - 1];
                command.CommandText = string.Format(rawCommandTextUpdate, valueList, row.Table.Columns[keyIndex], row[keyIndex]);
                //  Debug.WriteLine(command.CommandText);
                command.ExecuteNonQuery();
            }
            catch (Exception xcptn)
            {
                
                Debug.WriteLine("* * *  DataSync UpdateDatabaseRecord exception: " + xcptn.Message); 

                if (xcptn.InnerException != null && xcptn.InnerException.Message != null) {
                    Debug.WriteLine("Inner Exception: "+ xcptn.InnerException.Message);
                }
                Debug.WriteLine("* * *  DataSync UpdateDatabaseRecord valueList: " + valueList);

                throw xcptn;
            }
        }


        private string GetTableUpdateColumnSet(DataTable table)
        {
            var setValues = "[";

            for (int i = 0; i < table.Columns.Count - 1; i++)
            {
                setValues += table.Columns[i] + "] = " + table + ", [";
            }

            setValues += table.Columns[table.Columns.Count - 1] + "]";

            // Debug.WriteLine(values);
            return setValues.ToString();
        }

        private string GetTableColumns(DataTable table)
        {
            var values = "[";

            for (int i = 0; i < table.Columns.Count - 1; i++)
            {
                values += table.Columns[i] + "], [";
            }

            values += table.Columns[table.Columns.Count - 1] + "]";

            // Debug.WriteLine(values);
            return values.ToString();
        }

        private void FillDatabaseRecord(DataRow row, DbCommand command, string rawCommandText)
        {
            string valueList = String.Empty;
            try
            {
                var values = row.ItemArray.Select(i => ItemToSqlFormattedValue(i));
                valueList = string.Join(", ", values);
                command.CommandText = string.Format(rawCommandText, valueList);
                //  Debug.WriteLine(command.CommandText);
                command.ExecuteNonQuery();
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("* * *  DataSync FillDatabaseRecord exception: " + xcptn.Message);
                Debug.WriteLine("                Inner exception: " + ( (xcptn.InnerException == null || xcptn.InnerException.Message == null) ? String.Empty : xcptn.InnerException.Message) );
                Debug.WriteLine("                rawCommandText:  " + rawCommandText);
                Debug.WriteLine("* * *  DataSync FillDatabaseRecord valueList: " + valueList);
                throw xcptn;
            }
        }

        private string ItemToSqlFormattedValue(object item)
        {
            const string quotedItem = "'{0}'";
            if (item.ToString().Contains("False"))
            {
                return "0";
            }
            else if (item.ToString().Contains("True"))
            {
                return "1";
            }

            if (item is System.DBNull)
                return "NULL";
            else if (item is bool)
                return ((bool)item ? "1" : "0");
            else if (item is Guid)
                return string.Format(quotedItem, item.ToString());
            else if (item is string)
                return string.Format(quotedItem, ((string)item).Replace("'", "''"));
            else if (item is DateTime)
                return string.Format(quotedItem, ((DateTime)item).ToString("yyyy-MM-dd HH:mm:ss"));
            else
                return item.ToString();
        }


    }
}
