// *** RRB 2/6/16 Needs refactoring
// *** 1. Remove Session variables (in case it's used in a WPF program Venture launcher)
// *** 2. Make status codes parameters so as to be used across multiple applications 
// *** 3. Separate out VistaDB code so it can be a separate build and VistaDB DLL doesn't need to be part of the normal web app. 
using AJBoggs.Sis.Domain;

using Assmnts;
using Assmnts.Infrastructure;

using Data.Abstract;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

using UAS.Business;

using VistaDB.DDA;


namespace Data.Concrete
{
    /// <summary>
    /// Used to synchronize local/distributed Venture databases to master SQL Server database
    /// </summary>
    public class FormsSql : IFormsSql
    {

        // This should include all result data tables
        string[] formResultsTables = { 
                    "def_FormResults",
                    "def_ItemResults",
                    "def_ResponseVariables",
                    "Contact",
                    "Recipient"
         };

        // This should include all the screen meta data tables
        string[] metaDataTables = { 
                    "def_BaseTypes",
                    "def_Languages",
                    "def_LookupMaster",
                    "def_LookupDetail",
                    "def_LookupText",
                    "def_BranchRules",
                    "def_Forms",
                    "def_Parts",
                    "def_FormParts",
                    "def_Sections",
                    "def_SubSections",
                    "def_PartSections",
                    "def_PartSectionsEnt",
                    "def_Items",
                    "def_ItemsEnt",
                    "def_SectionItems",
                    "def_SectionItemsEnt",
                    "def_OutcomeDeclaration",
                    "def_ItemVariables"
        };


        string[] oldMetaDataTables = {
                    "def_BaseTypes",
                    "def_Languages",
                    "def_LookupMaster",
                    "def_LookupDetail",
                    "def_LookupText",
                    "def_BranchRules",
                    "def_Forms",
                    "def_Parts",
                    "def_FormParts",
                    "def_Sections",
                    "def_SubSections",
                    "def_PartSections",
                    "def_Items",
                    "def_ItemsEnt",
                    "def_SectionItems",
                    "def_SectionItemsEnt",
                    "def_OutcomeDeclaration",
                    "def_ItemVariables"
                                      
        };


        formsEntities db = DataContext.GetDbContext();

        public string GetMetaDataTableName(int fileId)
        {
            string tableName = String.Empty;
            if ((fileId >= 0) && (fileId < metaDataTables.Count()))
            {
                tableName = metaDataTables[fileId];
            }
            return tableName;
        }

        public string GetOldMetaDataTableName(int fileId)
        {
            string tableName = String.Empty;
            if ((fileId >= 0) && (fileId < oldMetaDataTables.Count()))
            {
                tableName = oldMetaDataTables[fileId];
            }
            return tableName;
        }

        public string GetResultsTableName(int fileId)
        {
            string tableName = String.Empty;
            if ((fileId >= 0) && (fileId < formResultsTables.Count()))
            {
                tableName = formResultsTables[fileId];
            }
            return tableName;
        }


        public string GetConnectionString()
        {
            string connectionString = String.Empty;
            try
            {
                connectionString = db.Database.Connection.ConnectionString;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("   Connection: " + db.Database.Connection.State);
                Debug.WriteLine("* * *  AssessmentRepository  Save exception: " + ex.Message);
            }

            return connectionString;
        }

        public int GetNumberOfResultsTables()
        {
            return formResultsTables.Count();
        }

        public int GetNumberOfMetaTables()
        {
            return metaDataTables.Count();
        }

        public string GenerateQuery(string formTable)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (formTable == "def_ResponseVariables")
            {
                stringBuilder.Append(" JOIN def_ItemResults on def_ItemResults.itemResultId = def_ResponseVariables.itemResultId");
            }

            if (formTable == "def_ItemResults" || formTable == "def_ResponseVariables")
            {
                stringBuilder.Append(" JOIN def_FormResults on def_FormResults.formResultId = def_ItemResults.formResultId");
            }

            if (formTable == "Recipient")
            {
                stringBuilder.Append(" INNER JOIN def_FormResults on def_FormResults.subject = Recipient.Recipient_ContactID");
            }

            if (formTable == "Contact")
            {
                stringBuilder.Append(" INNER JOIN def_FormResults on def_FormResults.subject = Contact.ContactID");
            }

            if (formTable == "def_FormResults" || formTable == "def_ItemResults" || formTable == "def_ResponseVariables" || formTable == "Recipient" || formTable == "Contact")
            {
                stringBuilder.Append(" WHERE EnterpriseID = " + SessionHelper.LoginStatus.EnterpriseID);

                if (!SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Contains(0))
                {
                    stringBuilder.Append(" AND (");

                    for (int i = 0; i < SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Count; i++)
                    {
                        stringBuilder.Append("GroupID = " + SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[i]);

                        if (i < SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Count - 1)
                        {
                            stringBuilder.Append(" OR ");
                        }

                    }
                    stringBuilder.Append(")");
                }

                if (!UAS_Business_Functions.hasPermission(PermissionConstants.GROUP_WIDE, PermissionConstants.ASSMNTS))
                {
                    stringBuilder.Append(" AND " + " assigned = " + SessionHelper.LoginStatus.UserID);
                }

               // stringBuilder.Append(" AND " + " deleted = 0");
            }

            return stringBuilder.ToString();
        }

        public int GetTableRecordCount(string formTable)
        {
            int rowCount = 0;
            try
            {
                // SELECT COUNT(*)

                StringBuilder stringBuilder = new StringBuilder("SELECT COUNT(*) FROM (SELECT DISTINCT [dbo]." + formTable + ".* FROM [dbo]." + formTable);
                stringBuilder.Append(GenerateQuery(formTable));

                // RRB - 08/22/15 - tables other than def_FormResults don't have formStatus ??????????
                // if (formTable == "def_FormResults" || formTable == "def_ItemResults" || formTable == "def_ResponseVariables" || formTable == "Recipient" || formTable == "Contact")
                if (formTable == "def_FormResults")
                {
                    stringBuilder.Append(" AND formStatus < " + (int)FormResults_formStatus.COMPLETED);
                    stringBuilder.Append(" AND deleted = 0");
                    stringBuilder.Append(" AND assigned = " + SessionHelper.LoginStatus.UserID);
                }
                stringBuilder.Append(") r");
                string qry = stringBuilder.ToString();

                SqlConnection sqlConn = new SqlConnection(GetConnectionString());
                sqlConn.Open();
                using (SqlCommand command
                    = new SqlCommand(qry, sqlConn))
                {
                    command.CommandType = System.Data.CommandType.Text;
                    rowCount = (Int32)command.ExecuteScalar();
                }
                Debug.WriteLine("* * *  " + "GetTableRecordCount method rowCount: " + formTable + ": "  + rowCount.ToString());

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetTableRecordCount error. " + formTable + ":  " + ex.Message);
            }

            return rowCount;

        }

        public int GetTableRecordCountVenture(string formTable)
        {
            int rowCount = 0;
            try
            {
                // SELECT COUNT(*)


                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM (SELECT DISTINCT [dbo]." + formTable + ".* FROM [dbo]." + formTable;
                        command.CommandText += GenerateQuery(formTable);
                        command.CommandText += ")";
                        command.CommandType = CommandType.Text;
                        rowCount = (Int32)command.ExecuteScalar();

                    }
                    connection.Close();
                }
                Debug.WriteLine("* * *  GetTableRecordCount  " + formTable + "  rowCount: " + rowCount.ToString());

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetTableRecordCount error  " + formTable + ": " + ex.Message);
            }

            return rowCount;

        }


        public int DeleteTableContent(string formTable)
        {
            int rowsDeleted = 0;
            try
            {

                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE [dbo]." + formTable + " FROM [dbo]." + formTable;
                        command.CommandText += GenerateQuery(formTable);
                        command.CommandType = CommandType.Text;
                        rowsDeleted = command.ExecuteNonQuery();

                    }
                    connection.Close();
                }

                Debug.WriteLine("* * *  GetTableRecordCount  " + formTable + "  rowsDeleted: " + rowsDeleted.ToString());

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  DeleteResultsTableContent error.  " + formTable + ": " + ex.Message);
            }

            return rowsDeleted;

        }


        public bool GetFormResultTablesUploadedStatus()
        {
            int rowCount = 0;
            try
            {

                FormResults_formStatus status = FormResults_formStatus.UPLOADED;

                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM [dbo]." + "def_FormResults";
                        command.CommandText += GenerateQuery("def_FormResults");
                        command.CommandText += " AND formStatus <> " + (int)status;

                        command.CommandType = CommandType.Text;
                        rowCount = (Int32)command.ExecuteScalar();

                    }
                    connection.Close();
                }
                Debug.WriteLine("* * *  GetFormResultTablesUploadedStatus rowCount: " + rowCount.ToString());

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetFormResultTablesUploadedStatus error: " + ex.Message);
            }

            return (rowCount == 0);

        }

        public System.Collections.Generic.List<int> GetCompletedFormResultIds()
        {

            List<int> results = null;
            {
                FormResults_formStatus status = FormResults_formStatus.COMPLETED;

                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT formResultId FROM [dbo]." + "def_FormResults";
                        command.CommandText += GenerateQuery("def_FormResults");
                        command.CommandText += " AND formStatus = " + (int)status;
                        command.CommandType = CommandType.Text;
                        DataTable dt = new DataTable();
                        dt.Load(command.ExecuteReader());

                        results = dt.Rows.OfType<DataRow>().Select(dr => dr.Field<int>("formResultId")).ToList();

                    }
                    connection.Close();
                }
            }
            return results;
        }

        /// <summary>
        ///     Checks if there are any FormResults with Completed status.
        /// </summary>
        /// <returns>true / false</returns>
        public bool GetNoFormResultsToBeUploaded()
        {
            int rowCount = 0;
            try
            {
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM [dbo]." + "def_FormResults";
                        command.CommandText += GenerateQuery("def_FormResults");
                        command.CommandText += " AND formStatus = " + (int)FormResults_formStatus.COMPLETED;
                        command.CommandType = CommandType.Text;
                        rowCount = (Int32)command.ExecuteScalar();

                    }
                    connection.Close();
                }

                Debug.WriteLine("* * *  GetNoFormResultsToBeUploaded rowCount: " + rowCount.ToString());

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetNoFormResultsToBeUploaded error: " + ex.Message);
            }

            return (rowCount == 0);
        }

        // LK 4/22/2015 #12522 Merged this function with CreateFormResultsJSON
        //public int[] GetCompletedFormResults()
        //{
        //    int[] formResultIds;
        //    try
        //    {
        //        FormResults_formStatus status = FormResults_formStatus.COMPLETED;

        //        using (DbConnection connection = UasAdo.GetUasAdoConnection())
        //        {
        //            connection.Open();

        //            using (DbCommand command = connection.CreateCommand())
        //            {
        //                command.CommandText = "SELECT formResultId FROM [dbo]." + "def_FormResults WHERE formStatus = " + (int)status;
        //                command.CommandType = CommandType.Text;

        //                DbDataAdapter adapter = UasAdo.CreateDataAdapter(connection);

        //                adapter.SelectCommand = command;

        //                DataSet dataSet = new DataSet();

        //                adapter.Fill(dataSet);


        //                // here you have your dataset filled

        //                //To access your data

        //               formResultIds = new int[dataSet.Tables[0].Rows.Count];

        //                for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
        //                {
        //                    formResultIds[i] = Int32.Parse(dataSet.Tables[0].Rows[i]["formResultId"].ToString());
        //                }


        //            }
        //            connection.Close();
        //            return formResultIds;
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("* * *  GetCompletedFormResults error: " + ex.Message);
        //        return null;
        //    }
        //}


        public class FormResultJsonContainer
        {
            public Dictionary<string, object> FormResult {get; set;}
            public List<Dictionary<string, object>> Data {get; set;}
        }
        
         
         public string CreateFormResultJSON(int formResultId, int newFormResultId = -1)
        {
            FormResultJsonContainer container = new FormResultJsonContainer();

            db.Configuration.LazyLoadingEnabled = false;

            // Not a new form result; don't change the id
            if (newFormResultId == -1)
            {
                newFormResultId = formResultId;
            }

            try
            {
                DataTable dt1 = null;
                DataTable dt2 = null;
                
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
 
                        command.CommandText = "SELECT " + newFormResultId + " as newFormResultId, fr.* from def_FormResults fr where formResultId = " + formResultId;

                        
                        command.CommandType = CommandType.Text;
                        dt1 = new DataTable();
                        dt1.Load(command.ExecuteReader());

                        container.FormResult = new Dictionary<string, object>();

                        foreach(DataColumn dc in dt1.Columns) {
                           container.FormResult.Add(dc.ColumnName, dt1.Rows[0][dc]);
                        }
                    }


                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "select rv.responseVariableId, iv.itemId, rv.itemVariableId, rv.rspValue from [dbo].def_ResponseVariables rv join def_ItemVariables iv on rv.itemVariableId = iv.itemVariableId join def_ItemResults ir on rv.itemResultId = ir.itemResultId join def_FormResults fr on ir.formResultId = fr.formResultId where fr.formResultId = " + formResultId + " order by iv.itemId";          
                        command.CommandType = CommandType.Text;
                        dt2 = new DataTable();
                        dt2.Load(command.ExecuteReader());

                        container.Data = new List<Dictionary<string, object>>();

                        foreach (DataRow dr in dt2.Rows)
                        {
                            Dictionary<string, object> dataDict = new Dictionary<string, object>();
                            foreach (DataColumn dc in dt2.Columns)
                            {
                                dataDict.Add(dc.ColumnName, dr.Field<object>(dc.ColumnName));
                            }

                            container.Data.Add(dataDict);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  CreateFormResultJSON exception: " + ex.Message);
                return null;
            }

            string json = fastJSON.JSON.ToJSON(container);

            return json;
        }


        //public string CreateFormResultJSON(int formResultId, int newFormResultId = -1)
        //{
        //    string json = String.Empty;

        //    db.Configuration.LazyLoadingEnabled = false;

        //    // Not a new form result; don't change the id
        //    if (newFormResultId == -1)
        //    {
        //        newFormResultId = formResultId;
        //    }

        //    try
        //    {

        //        using (DbConnection connection = UasAdo.GetUasAdoConnection())
        //        {
        //            connection.Open();

        //            using (DbCommand command = connection.CreateCommand())
        //            {
        //                command.CommandText = "SELECT RV.sectionId, RV.rspValue, RV.identifier, " + newFormResultId + " AS formResultId, FR.formStatus, FR.dateUpdated FROM RspVarsWithSection RV JOIN def_FormResults FR ON FR.formResultId = RV.formResultId WHERE RV.formResultId = " + formResultId + " ORDER BY sectionId";
        //                command.CommandType = CommandType.Text;
        //                DataTable dt = new DataTable();
        //                dt.Load(command.ExecuteReader());

        //                json = fastJSON.JSON.ToJSON(dt);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("* * *  CreateFormResultJSON exception: " + ex.Message);
        //        return null;
        //    }

        //    return json;
        //}

        public string[] CreateFormResultsJSON()
        {
            int[] formResultIds;
            try
            {
                FormResults_formStatus status = FormResults_formStatus.COMPLETED;

                int EnterpriseId = SessionHelper.LoginStatus.EnterpriseID;

                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT formResultId FROM [dbo]." + "def_FormResults WHERE formStatus = " + (int)status + " AND";

                        command.CommandText += GenerateQuery("def_FormResults");

                        command.CommandType = CommandType.Text;

                        DataSet dataSet = new DataSet();

                        DbDataAdapter adapter = UasAdo.CreateDataAdapter(connection);
                        adapter.SelectCommand = command;
                        adapter.Fill(dataSet);

                        formResultIds = new int[dataSet.Tables[0].Rows.Count];

                        for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                        {
                            formResultIds[i] = Int32.Parse(dataSet.Tables[0].Rows[i]["formResultId"].ToString());
                        }

                    }

                    string[] jsonStrings = new string[formResultIds.Count()];
                    string json = String.Empty;

                    string formStatus = String.Empty;
                    string dateUpdated = String.Empty;
                    int j = 0;
                    foreach (int formResultId in formResultIds)
                    {

                        using (DbCommand command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT formStatus, dateUpdated FROM " + "def_FormResults WHERE formResultId = " + formResultId;
                            command.CommandType = CommandType.Text;
                            {
                                using (DbDataReader reader = command.ExecuteReader())
                                {
                                    if (reader != null)
                                    {
                                        if (reader.Read())
                                        {
                                            formStatus = reader["formStatus"].ToString();
                                            dateUpdated = reader["dateUpdated"].ToString();
                                        }
                                    }
                                }
                            }
                        }


                        using (DbCommand command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT * FROM " + "RspVarsWithSection WHERE formResultId = " + formResultId + " ORDER BY sectionId";
                            command.CommandType = CommandType.Text;

                            using (DbDataReader reader = command.ExecuteReader())
                            {
                                if (reader != null)
                                {
                                    if (reader.Read())
                                    {
                                        int section = -1;
                                        bool sectionChange = true; ;
                                        while (true)
                                        {
                                            if (String.IsNullOrEmpty(json))
                                            {
                                                json += "{ \"formResults\": { \"id\": \"" + reader["formResultId"]
                                                    + "\",\"formStatus\":\"" + formStatus + "\",\"dateUpdated\":\"" + dateUpdated + "\",\"sections\": [";


                                            }
                                            if (sectionChange == true)
                                            {
                                                json += "{ \"id\": \"" + reader["sectionId"] + "\", \"responses\": [{";
                                                section = Int32.Parse(reader["sectionId"].ToString());
                                            }
                                            json += "\"" + reader["identifier"] + "\": \"" + reader["rspValue"] + "\"";

                                            if (!reader.Read())
                                                break;

                                            if (section != Int32.Parse(reader["sectionId"].ToString()))
                                            {
                                                json += "}] },";
                                                sectionChange = true;
                                            }
                                            else
                                            {
                                                json += ", ";
                                                sectionChange = false;
                                            }

                                        }
                                        json += "}] }] } }";
                                    }

                                }
                            }


                            jsonStrings[j++] = json;
                            json = String.Empty;

                            Debug.WriteLine("*** FormsSql - CreateFormResultsJSON: Creating JSON for form result id = " + formResultId);

                        }
                    }
                    return jsonStrings;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  CreateFormResultsJSON error: " + ex.Message);
                return null;
            }


        }

        public List<int> GetNonNewFormResultIds()
        {
            int[] formResultIds;
            try
            {
                FormResults_formStatus status = FormResults_formStatus.NEW;

                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM def_FormResults" + GenerateQuery("def_FormResults");
                        command.CommandText += " AND formStatus <> " + (int)status;
                        command.CommandType = CommandType.Text;

                        DbDataAdapter adapter = UasAdo.CreateDataAdapter(connection);
                        adapter.SelectCommand = command;

                        DataSet dataSet = new DataSet();
                        adapter.Fill(dataSet);

                        formResultIds = new int[dataSet.Tables[0].Rows.Count];

                        for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                        {
                            formResultIds[i] = Int32.Parse(dataSet.Tables[0].Rows[i]["formResultId"].ToString());
                        }

                    }
                    connection.Close();
                    return formResultIds.ToList();
                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetCompletedFormResults error: " + ex.Message);
                return null;
            }
        }

        // Deprecated, use MarkSingleUploaded
        public void MarkUploaded()
        {
            try
            {
                // SELECT COUNT(*)

                FormResults_formStatus upldStatus = FormResults_formStatus.UPLOADED;
                FormResults_formStatus cmpltdStatus = FormResults_formStatus.COMPLETED;

                int EnterpriseId = SessionHelper.LoginStatus.EnterpriseID;

                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE [dbo].def_FormResults SET formStatus = " + (int)upldStatus + " FROM " + "[dbo].def_FormResults WHERE formStatus = " + (int)cmpltdStatus;
                        command.ExecuteReader();


                    }
                    connection.Close();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetCompletedFormResults error: " + ex.Message);
            }

        }

        public void MarkSingleUploaded(int formResultId)
        {
            try
            {

                FormResults_formStatus upldStatus = FormResults_formStatus.UPLOADED;

                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE [dbo].def_FormResults SET formStatus = " + (int)upldStatus + ", archived = 1, statusChangeDate = '" + DateTime.Now + "' FROM " + "[dbo].def_FormResults WHERE formResultId = " + formResultId;
                        command.ExecuteReader();


                    }
                    connection.Close();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetCompletedFormResults error: " + ex.Message);
            }

        }

        /// <summary>
        /// Turns off auto record identity generation so the record id from the master database can be used.
        /// </summary>
        /// <param name="tableName">SQL table name</param>
        /// <param name="columnName">the identity column</param>
        /// <param name="enable"></param>
        public void SetIdentityInsert(string tableName, string columnName, bool enable)
        {

            // VistaDB DDA code 
            IVistaDBTable tbl = null;
            try
            {
                IVistaDBDDA DDAObj = VistaDBEngine.Connections.OpenDDA();
                string vistaDbPath = (string)AppDomain.CurrentDomain.GetData("DataDirectory");
                Debug.WriteLine("FormsSql.SetIdentityInsert VistaDbPath: " + vistaDbPath);
                // IVistaDBDatabase db = DDAObj.OpenDatabase(System.AppDomain.CurrentDomain.BaseDirectory + "App_Data\\forms.vdb5", VistaDB.VistaDBDatabaseOpenMode.NonexclusiveReadWrite, "aj80995");
                IVistaDBDatabase db = DDAObj.OpenDatabase(vistaDbPath + @"\forms.vdb5", VistaDB.VistaDBDatabaseOpenMode.NonexclusiveReadWrite, "P@ssword1");

                tbl = db.OpenTable(tableName, false, false);
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("FormsSql.SetIdentityInsert Exception: " + xcptn.Message);
                throw xcptn;
            }

            // table1s.AddColumn("ID", VistaDBType.Int); 
            // table1s.DefineColumnAttributes("ID", false, false, false, false, null, null); 
            // table1s.AddColumn("COLINT", VistaDBType.Int); 
            // table1s.DefineColumnAttributes("COLINT", false, false, false, false, null, null); 

            if (tbl.EnforceIdentities)
            {
                if (enable)
                {
                    tbl.DropIdentity(columnName);
                }
                else
                {
                    if (tableName == "def_FormResults")
                    {
                        int min = getMin(columnName, tableName);
                        int next = min - 1;
                        tbl.CreateIdentity(columnName, next.ToString(), "-1");
                    }
                    else
                    {
                        int max = getMax(columnName, tableName);
                        int next = max + 1;
                        tbl.CreateIdentity(columnName, next.ToString(), "1");
                    }
                }
            }
            else
            {
                if (!enable)
                {
                    if (tableName == "def_FormResults")
                    {
                        int min = getMin(columnName, tableName);
                        int next = min - 1;
                        tbl.CreateIdentity(columnName, next.ToString(), "-1");
                    }
                    else
                    {
                        int max = getMax(columnName, tableName);
                        int next = max + 1;
                        tbl.CreateIdentity(columnName, next.ToString(), "1");
                    }
                }
            }
            // tbl.CreateIndex("Primary", "ID", true, true); 
            // tbl.CreateIndex("idxDate", "COLDATETIME", false, false); 

            tbl.Close();
            tbl.Dispose();
            tbl = null;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private int getMax(string columnName, string tableName)
        {
            int max = 0;
            try
            {
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT MAX(" + columnName + ") FROM [dbo]." + tableName;
                        command.CommandType = CommandType.Text;
                        max = (Int32)command.ExecuteScalar();

                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FormsSql getMax exception: " + ex.Message);
            }
            return max;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private int getMin(string columnName, string tableName)
        {
            int min = 0;
            try
            {
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT MIN(" + columnName + ") FROM [dbo]." + tableName;
                        command.CommandType = CommandType.Text;
                        min = (Int32)command.ExecuteScalar();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FormsSql getMax exception: " + ex.Message);
            }

            if (min > 0)
            {
                min = 0;
            }

            return min;
        }

        public void UpdateFormResultID(int formResultId, int newFormResultId)
        {
            // Turn off identity insert on form result
            SetIdentityInsert("def_FormResults", "formResultId", true);

            // Copy form result to new id
            try
            {
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO [dbo].def_FormResults (formResultId, formId, formStatus, sessionStatus, dateUpdated, deleted, locked, archived, EnterpriseID, GroupID, [subject], interviewer, assigned, training, reviewStatus, statusChangeDate) select " + newFormResultId + ", formId, formStatus, sessionStatus, dateUpdated, deleted, locked, archived, EnterpriseID, GroupID, [subject], interviewer, assigned, training, reviewStatus, statusChangeDate from def_FormResults where formResultId = " + formResultId;
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FormsSql update form result Id exception: " + ex.Message);
                SetIdentityInsert("def_FormResults", "formResultId", false);
                return;
            }


            // change references in item results to form result
            try
            {
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE def_ItemResults SET formResultId = " + newFormResultId + " WHERE formResultId = " + formResultId;
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FormsSql update form result Id exception: " + ex.Message);
                SetIdentityInsert("def_FormResults", "formResultId", false);
                return;
            }


            // delete form result with old id
            try
            {
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM def_FormResults WHERE formResultId = " + formResultId;
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FormsSql update form result Id exception: " + ex.Message);
                SetIdentityInsert("def_FormResults", "formResultId", false);
                return;
            }

            // turn on identity insert for form result
            SetIdentityInsert("def_FormResults", "formResultId", false);
        }

    }
}
