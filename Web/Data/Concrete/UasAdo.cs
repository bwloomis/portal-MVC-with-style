using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.Common;

using Assmnts;
using Assmnts.Infrastructure;

namespace Data.Concrete
{
    public static class UasAdo
    {
        /*
         * Method to get a database connection from the server or local embedded database.
         * built on generic ADO.Net
         */
        public static DbConnection GetUasAdoConnection()
        {
            DbConnection dbConn = null;
            try
            {
                System.Configuration.ConnectionStringSettings connectionString = new System.Configuration.ConnectionStringSettings();
                if (SessionHelper.IsVentureMode)
                {
                    connectionString = DataContext.getUASEntitiesAdoVenture();
                }
                else
                {
                    connectionString = System.Configuration.ConfigurationManager.ConnectionStrings[DataContext.GetUasConnectionStringName()];
                }

                // Loads the factory
                System.Data.Common.DbProviderFactory factory = System.Data.Common.DbProviderFactories.GetFactory(connectionString.ProviderName);

                dbConn = factory.CreateConnection();
                dbConn.ConnectionString = connectionString.ConnectionString;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetUasAdoConnection: " + ex.Message);
              //  GetProviderFactoryClasses();
            }

            return dbConn;

        }

        /*
         * Method to get the number of rows in a table.
         * mainly used to see if a table is empty and needs to be downloaded from the master/remote server
         */
        public static int GetTableRowCount(string tableName)
        {
            int rowCount = 0;
            try
            {
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM " + tableName;
                        command.CommandType = CommandType.Text;

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    rowCount = reader.GetInt32(0);
                                    Debug.WriteLine("* * *  GetTableRowCount: " + tableName + " - " +  rowCount.ToString());
                                }
                            }
                        }
                    }
                    connection.Close();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetTableRowCount: " + ex.Message);
            }

            return rowCount;

        }


        public static bool AddEntAppConfig(DbConnection dbConn, uas_EntAppConfig eac)
        {
            DbDataReader reader = null;

            try
            {
                int maxEntAppConfigId = -1;

                using (DbCommand command = dbConn.CreateCommand())
                {
                    command.CommandText = "SELECT MAX(EntAppConfigId) FROM [dbo].[uas_EntAppConfig] ";
                    command.CommandType = CommandType.Text;

                    reader = command.ExecuteReader();
                    reader.Read();
                    if (!reader.IsDBNull(0))
                    {
                        maxEntAppConfigId = reader.GetInt32(0);
                    }
                    reader.Close();
                }

                if (maxEntAppConfigId == -1)
                    throw new Exception("* * * AddEntAppConfig: Failed to retrieve an unused EntAppConfigId");

                using (DbCommand command = dbConn.CreateCommand())
                {
                    command.CommandText = "INSERT INTO [dbo].[uas_EntAppConfig] "
                        + "([EntAppConfigId],[EnterpriseID],[ApplicationID],[EnumCode],[baseTypeId],[ConfigName],[ConfigValue],[CreatedDate],[CreatedBy],[StatusFlag]) "
                        + "VALUES("
                        + (maxEntAppConfigId+1) + ","
                        + eac.EnterpriseID + ","
                        + eac.ApplicationID + ","
                        + "'" + eac.EnumCode + "',"
                        + eac.baseTypeId + ","
                        + "'" + eac.ConfigName + "',"
                        + "'" + eac.ConfigValue + "',"
                        + "'" + eac.CreatedDate + "',"
                        + eac.CreatedBy + ","
                        + "'" + eac.StatusFlag + "')";
                    command.CommandType = CommandType.Text;
                    reader = command.ExecuteReader();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  AddEntAppConfig  Exception: " + ex.Message);
                throw;
            }

            return true;
        }

        /// <summary>
        /// Retrieve a uas_EntAppConfig Record
        /// 
        /// Used in venture mode SharedValidation, to retrieve a pre-computed list of required def_ItemVariable identifiers
        /// </summary>
        /// <param name="dbConn"></param>
        /// <param name="enumCode"></param>
        /// <param name="enterpriseID"></param>
        /// <returns></returns>
        public static uas_EntAppConfig GetEntAppConfig(DbConnection dbConn, string enumCode, int enterpriseID)
        {

            uas_EntAppConfig result = null;
            DbDataReader reader = null;

            try
            {
                using (DbCommand command = dbConn.CreateCommand())
                {
                    command.CommandText = "SELECT EntAppConfigId,EnterpriseID,ApplicationID,EnumCode," 
                        + "baseTypeId,ConfigName,ConfigValue,CreatedDate,CreatedBy,StatusFlag FROM [dbo].uas_EntAppConfig WHERE EnumCode = '" + enumCode + "' AND EnterpriseID = " + enterpriseID;
                    command.CommandType = CommandType.Text;

                    reader = command.ExecuteReader();
                    reader.Read();
                    if (!reader.IsDBNull(0))
                    {
                        Debug.WriteLine("* * *  GetEntAppConfig uas_EntAppConfig FieldCount: " + reader.FieldCount.ToString());
                        result = new uas_EntAppConfig();
                        result.EntAppConfigId = reader.GetInt32(0);
                        result.EnterpriseID = reader.GetInt32(1);
                        result.ApplicationID = reader.GetInt32(2);
                        result.EnumCode = reader["EnumCode"].ToString();
                        result.baseTypeId = reader.GetInt16(4);
                        result.ConfigName = reader["ConfigName"].ToString();
                        result.ConfigValue = reader["ConfigValue"].ToString();
                        result.CreatedDate = Convert.ToDateTime(reader["CreatedDate"].ToString());
                        result.CreatedBy = reader.GetInt32(8);
                        result.StatusFlag = reader["StatusFlag"].ToString();
                    }
                    reader.Close();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetEntAppConfig  Exception: " + ex.Message);
                throw; 
            }

            return result;
        }

        /*
         * Method to get the uas_User 
         * *** Only needed fields are being populated. ***
         */
        public static uas_User GetUserByLogin(DbConnection dbConn, string loginID)
        {
            uas_User usr = null;
            DbDataReader reader = null;

            try
            {
                    using (DbCommand command = dbConn.CreateCommand())
                    {
                        command.CommandText = "SELECT UserID,EnterpriseID,Password,FirstName,LastName,StatusFlag FROM [dbo].uas_User WHERE LoginID = '" + loginID + "'";
                        command.CommandType = CommandType.Text;

                        reader = command.ExecuteReader();
                        reader.Read();
                        if (!reader.IsDBNull(0))
                        {
                            Debug.WriteLine("* * *  GetUserByLogin uas_User FieldCount: " + reader.FieldCount.ToString());
                            Debug.WriteLine("* * *  GetUserByLogin uas_User : " + reader["FirstName"] + " " + reader["LastName"]);
                            var fname = reader["FirstName"];
                            var lname = reader["LastName"];
                            usr = new uas_User();
                            usr.UserID = reader.GetInt32(0);
                            usr.EnterpriseID = reader.GetInt32(1);
                            usr.Password = reader.GetString(2);
                            usr.FirstName = (fname == null)? String.Empty : fname.ToString();
                            usr.LastName = (lname == null) ? String.Empty : lname.ToString();
                            usr.StatusFlag = reader.GetString(5);
                        }
                        reader.Close();
                    }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetUserByLogin  Exception: " + loginID + " - " + ex.Message);
                throw;  // throw the same exception
            }

            return usr;

        }

        /*
        * Method to get the User email address
        */
        public static string GetUserEmail(DbConnection dbConn, int userId)
        {
            string usrEmail = String.Empty;
            DbDataReader reader = null;

            try
            {
                using (DbCommand command = dbConn.CreateCommand())
                {
                    command.CommandText = "SELECT EmailAddress FROM [dbo].uas_UserEmail WHERE UserID = " + userId.ToString() + ";";
                    command.CommandType = CommandType.Text;

                    reader = command.ExecuteReader();
                    reader.Read();
                    if (!reader.IsDBNull(0))
                    {
                        // Debug.WriteLine("* * *  GetUserByLogin uas_User FieldCount: " + reader.FieldCount.ToString());
                        usrEmail = reader.GetString(0);
                    }
                    reader.Close();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetUserEmail  Exception  UserId: " + userId.ToString() + " - " + ex.Message);
                throw;  // throw the same exception
            }

            return usrEmail;

        }

        /*
         * Method to get the Group Permissions for a User
         */
        public static List<uas_GroupUserAppPermissions> GetGroupPermissions(DbConnection dbConn, int userId)
        {
            List<uas_GroupUserAppPermissions> guapList = new List<uas_GroupUserAppPermissions>();
            try
            {
                using (DbCommand command = dbConn.CreateCommand())
                {
                    
                    command.CommandText = "SELECT GroupID, SecuritySet FROM [dbo].uas_GroupUserAppPermissions WHERE"
                    + " UserID = " + userId.ToString() 
                    + " AND StatusFlag = 'A'"
                    + " AND ApplicationId = " + UAS.Business.Constants.APPLICATIONID.ToString();
                    command.CommandType = CommandType.Text;

                    Debug.WriteLine("* * *  GetGroupPermissions SELECT: " + command.CommandText);

                    DbDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                        Debug.WriteLine("* * *  GetGroupPermissions reader.HasRows.");
                    while ( reader.Read() )
                    {
                        Debug.WriteLine("* * *  GetGroupPermissions Group/Perms: " + reader.GetInt32(0).ToString() + " / " +  reader.GetString(1));
                        uas_GroupUserAppPermissions guap = new uas_GroupUserAppPermissions();
                        guap.GroupID = reader.GetInt32(0);
                        guap.SecuritySet = reader.GetString(1);
                        guapList.Add(guap);
                    }
                    reader.Close();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetGroupPermissions Exception  userId: " + userId.ToString() + " - " + ex.Message);
                throw;  // throw the same exception
            }

            return guapList;

        }


        /*
         * Method to test the database provider version (only use for testing)
         */
        public static string GetProviderVersion()
        {
            string versionInfo = String.Empty;
            try
            {
                // SELECT COUNT(*)
                // VistaDBConnection sqlConn = new VistaDBConnection("data source='|DataDirectory|\forms.vdb5';Password=aj80995");
                System.Configuration.ConnectionStringSettings connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["UASEntitiesVenture"];
                // Loads the factory
                System.Data.Common.DbProviderFactory factory = System.Data.Common.DbProviderFactories.GetFactory(connectionString.ProviderName);
                using (DbConnection connection = factory.CreateConnection())
                {
                    connection.ConnectionString = connectionString.ConnectionString;
                    connection.Open();
                    Debug.WriteLine("* * *  GetProviderVersion ServerVersion: " + connection.ServerVersion);
                    Debug.WriteLine("* * *  GetProviderVersion DataSource: " + connection.DataSource);

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT @@VERSION;";
                        command.CommandType = CommandType.Text;

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string result = reader.GetString(0);
                                if (!reader.IsDBNull(0))
                                {
                                    versionInfo = result;
                                    Debug.WriteLine("* * *  GetProviderVersion versionInfo: " + versionInfo);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                versionInfo = "Exception: " + ex.Message;
                Debug.WriteLine("* * *  AuthenticateLocalUser uas_User row count error: " + ex.Message);
            }

            return versionInfo;

        }

        // This example assumes a reference to System.Data.Common. 
        public static DataTable GetProviderFactoryClasses()
        {
            // Retrieve the installed providers and factories.
            DataTable table = DbProviderFactories.GetFactoryClasses();

            // Display each row and column value. 
            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn column in table.Columns)
                {
                    Debug.WriteLine(row[column]);
                }
            }
            return table;
        }
        
        public static DbDataAdapter CreateDataAdapter(DbConnection connection)
        {
            return DbProviderFactories.GetFactory(connection).CreateDataAdapter();
        }
    
    }
}