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


namespace Assmnts.Models
{
   
    public class DataSyncProcess
    {

        /// Gets or sets the process status.
        /// <value>The process status.</value>
        //private static int ProcessStatus = 0;
        private static IDictionary<string, int> ProcessStatus { get; set; }

        IFormsSql formsSql = new FormsSql();
        string sisOnlineURL = ConfigurationManager.AppSettings["SISOnlineURL"];
        private static object syncRoot = new object();
        /// <value>The process status.</value>

        private static DataTable RemoveNullString(DataTable dt)
        {
            for (int a = 0; a < dt.Rows.Count; a++)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (dt.Rows[a][i] == "")
                    {
                        dt.Rows[a][i] = DBNull.Value;
                    }
                }
            }

            return dt;
        }

        /// Initializes a new instance of the <see cref="MyLongRunningClass"/> class.
        public DataSyncProcess()
        {
            if (ProcessStatus == null)
            {
                ProcessStatus = new Dictionary<string, int>();
            }
        }

        public void Add(string id)
        {
            lock (syncRoot)
            {
                ProcessStatus.Add(id, 0);
            }
        }

        /// <summary>
        /// Removes the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        public void Remove(string id)
        {
            lock (syncRoot)
            {
                ProcessStatus.Remove(id);
            }
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <param name="id">The id.</param>
        public int GetStatus(string id)
        {
            lock (syncRoot)
            {
                return ProcessStatus[id];
            }
        }
        /// <summary>
        /// Processes the long running action.
        /// </summary>
        /// <param name="id">The id.</param>
        public string ProcessDataSync(string id)
        {
            int rspCnt = 0;
            int resultTableCnt = formsSql.GetNumberOfResultsTables();

            lock (syncRoot)
            {
                ProcessStatus[id] = 1;
            }

            try
            {
                // Delete all result table first
                for (int DeleteOrderId = resultTableCnt - 1; DeleteOrderId >= 0; DeleteOrderId--)
                {
                    rspCnt = formsSql.DeleteTableContent(formsSql.GetResultsTableName(DeleteOrderId));
                    // ProcessStatus[id] = 2, 3, 4
                    lock (syncRoot)
                    {
                        ProcessStatus[id] += 1;
                    }
                }

                for (int fileId = 0; fileId < resultTableCnt; fileId++)
                {
                    //  2. Execute the URL to run the method on the remote server:
                    //       Export/GetResponsesCsv?fileId=1
                    // Construct HTTP request to get the file
                    HttpWebRequest httpRequest = (HttpWebRequest)
                    WebRequest.Create(sisOnlineURL + "Export/GetResponseCsv?fileId=" + fileId.ToString());
                    httpRequest.Method = WebRequestMethods.Http.Get;

                    DataTable csvData = new DataTable();
                    // Get back the HTTP response for web server
                    using (HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                    {
                        Stream httpResponseStream = httpResponse.GetResponseStream();
                        
                        // ProcessStatus[id] = 5, 7, 9  
                        lock (syncRoot)
                        {
                            ProcessStatus[id] += 1;
                        }

                        using (var reader = new CsvReader(httpResponseStream))
                        {
                            // the CSV file has a header record, so we read that first
                            reader.ReadHeaderRecord();
                            csvData.Fill(reader);

                            Debug.WriteLine("Table contains {0} rows.", csvData.Rows.Count);
                        }
                        rspCnt = csvData.Rows.Count;
                        csvData = RemoveNullString(csvData);
                    }
                    //  3. Read the FileStreamResult 
                    //    - Read through the records
                 /*   using (SqlBulkCopy bulkCopy = new SqlBulkCopy(formsSql.GetConnectionString(), SqlBulkCopyOptions.KeepNulls | SqlBulkCopyOptions.KeepIdentity))
                    {
                        bulkCopy.DestinationTableName = formsSql.GetResultsTableName(fileId);
                        bulkCopy.ColumnMappings.Clear();
                        foreach (var column in csvData.Columns)
                            bulkCopy.ColumnMappings.Add(column.ToString(), column.ToString());
                        bulkCopy.WriteToServer(csvData);
                    }
                   */
                    csvData.TableName = formsSql.GetResultsTableName(fileId);

                    DbConnection connection = UasAdo.GetUasAdoConnection();

                    FillDatabaseTable(csvData, connection);
                    // ProcessStatus[id] = 6, 8, 10
                    lock (syncRoot)
                    {
                        ProcessStatus[id] += 1;
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("* * *  DataSync exception: " + ex.Message);
            }
            return id;
        }
        /// <summary>
        /// Processes the long running action.
        /// </summary>
        /// <param name="id">The id.</param>
        public string ProcessMetaDataSync(string id)
        {
            int rspCnt = 0;
            int metaTableCnt = formsSql.GetNumberOfMetaTables();

            lock (syncRoot)
            {
                ProcessStatus[id] = 1;
            }

            try
            {
                // Delete all meta table first
                for (int DeleteOrderId = metaTableCnt - 1; DeleteOrderId >= 0; DeleteOrderId--)
                {
                    rspCnt = formsSql.DeleteTableContent(formsSql.GetMetaDataTableName(DeleteOrderId));
                    // ProcessStatus[id] = 2, 3, 4
                    lock (syncRoot)
                    {
                        ProcessStatus[id] += 1;
                    }
                }

                for (int fileId = 0; fileId < metaTableCnt; fileId++)
                {
                    //  2. Execute the URL to run the method on the remote server:
                    //       Export/GetResponsesCsv?fileId=1
                    // Construct HTTP request to get the file
                    HttpWebRequest httpRequest = (HttpWebRequest)
                    WebRequest.Create(sisOnlineURL + "Export/GetMetaCsv?fileId=" + fileId.ToString());
                    httpRequest.Method = WebRequestMethods.Http.Get;

                    DataTable csvData = new DataTable();
                    // Get back the HTTP response for web server
                    using (HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                    {
                        Stream httpResponseStream = httpResponse.GetResponseStream();
                        
                        // ProcessStatus[id] = 5, 7, 9  
                        lock (syncRoot)
                        {
                            ProcessStatus[id] += 1;
                        }

                        using (var reader = new CsvReader(httpResponseStream))
                        {
                            // the CSV file has a header record, so we read that first
                            reader.ReadHeaderRecord();
                            csvData.Fill(reader);

                            Debug.WriteLine("Table contains {0} rows.", csvData.Rows.Count);
                        }
                        rspCnt = csvData.Rows.Count;
                        csvData = RemoveNullString(csvData);
                    }
                    //  3. Read the FileStreamResult 
                    //    - Read through the records
                   /*using (SqlBulkCopy bulkCopy = new SqlBulkCopy(formsSql.GetConnectionString(), SqlBulkCopyOptions.KeepNulls | SqlBulkCopyOptions.KeepIdentity))
                    {
                        bulkCopy.DestinationTableName = formsSql.GetMetaDataTableName(fileId);
                        bulkCopy.ColumnMappings.Clear();
                        foreach (var column in csvData.Columns)
                            bulkCopy.ColumnMappings.Add(column.ToString(), column.ToString());
                        bulkCopy.WriteToServer(csvData);
                    }*/
                    csvData.TableName = formsSql.GetMetaDataTableName(fileId);

                    DbConnection connection = UasAdo.GetUasAdoConnection();

                    FillDatabaseTable(csvData, connection);
                    // ProcessStatus[id] = 6, 8, 10
                    lock (syncRoot)
                    {
                        ProcessStatus[id] += 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  DataSync exception: " + ex.Message);
            }
            return id;
        }

        public virtual void FillDatabaseTable(DataTable table, DbConnection connection)
        {
            connection.Open();
            SetIdentityInsert(table, connection, true);
            FillDatabaseRecords(table, connection);
            SetIdentityInsert(table, connection, false);
            connection.Close();
        }


        private void FillDatabaseRecords(DataTable table, DbConnection connection)
        {
            var command = connection.CreateCommand();
            command.Connection = connection; //todo combine this sequence
            var rawCommandText = string.Format("insert into {0} ({1}) values ({2})", table.TableName, GetTableColumns(table), "{0}");
            foreach (var row in table.AsEnumerable())
                FillDatabaseRecord(row, command, rawCommandText);
        }

        private string GetTableColumns(DataTable table)
        {
            var values = string.Empty;

            for (int i = 0; i < table.Columns.Count - 1; i++)
            {
                values += table.Columns[i] + ", ";
            }

            values += table.Columns[table.Columns.Count - 1];

            return values.ToString();
        }

        private void FillDatabaseRecord(DataRow row, DbCommand command, string rawCommandText)
        {
            var values = row.ItemArray.Select(i => ItemToSqlFormattedValue(i));
            var valueList = string.Join(", ", values);
            command.CommandText = string.Format(rawCommandText, valueList);
            command.ExecuteNonQuery();
        }

        private string ItemToSqlFormattedValue(object item)
        {
            const string quotedItem = "'{0}'";
            if (item is System.DBNull)
                return "NULL";
            else if (item is bool)
                return (bool)item ? "1" : "0";
            else if (item is Guid)
                return string.Format(quotedItem, item.ToString());
            else if (item is string)
                return string.Format(quotedItem, ((string)item).Replace("'", "''"));
            else if (item is DateTime)
                return string.Format(quotedItem, ((DateTime)item).ToString("yyyy-MM-dd HH:mm:ss"));
            else
                return item.ToString();
        }

        private void SetIdentityInsert(DataTable table, DbConnection connection, bool enable)
        {
            var commandCheck = connection.CreateCommand();
            commandCheck.Connection = connection;
            commandCheck.CommandText = string.Format("DECLARE @intObjectID INT SELECT @intObjectID =OBJECT_ID('{0}') SELECT COALESCE(OBJECTPROPERTY(@intObjectID, 'TableHasIdentity'),0) AS HasIdentity", table.TableName);
            int result = 0;
            result = Int32.Parse(commandCheck.ExecuteScalar().ToString());

            if (result == 1)
            {
                var command = connection.CreateCommand();
                command.Connection = connection;
                command.CommandText = string.Format(
                    "set IDENTITY_INSERT {0} {1}",
                    table.TableName,
                    enable ? "on" : "off");
                command.ExecuteNonQuery();

            }
        }
    }
}