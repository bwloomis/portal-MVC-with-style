using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Diagnostics;

using Assmnts;

using Data.Abstract;
using Assmnts.Infrastructure;

namespace Data.Concrete
{
    public class UasSql : IUasSql
    {
        string[] UasTables = { 
                    "uas_Config",
                    "uas_Enterprise",
                    "uas_GroupType",
                    "uas_Group",
                    "uas_User",
                    "uas_RoleType",
                    "uas_Role",
                    "uas_RoleAppPermissions",
                    "uas_GroupUserAppPermissions",
                    "uas_UserEmail",
                    "uas_UserPhone",
                    "uas_UserAddress",
                    "uas_EntAppConfig",
        };

        SISEntities db = DataContext.getSisDbContext();

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
                Debug.WriteLine("* * *  UasSql  Save exception: " + ex.Message);
            }

            return connectionString;
        }

        public Dictionary<int, String> getEnterprises()
        {
            Dictionary<int, String> entDict = new Dictionary<int,string>();

            try
            {
                SqlConnection sqlConnection1 = new SqlConnection(GetConnectionString());
                SqlCommand cmd = new SqlCommand();
                SqlDataReader reader;

                cmd.CommandText = "SELECT EnterpriseID, EnterpriseName FROM uas_Enterprise ORDER BY EnterpriseName";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection1;

                sqlConnection1.Open();

                reader = cmd.ExecuteReader();
                // Data is accessible through the DataReader object here.

                while (reader.Read())
                {
                   IDataRecord record = (IDataRecord)reader;
                   entDict.Add(Convert.ToInt32(record[0]), record[1].ToString());
                }

                reader.Close();
                sqlConnection1.Close();
                return entDict;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error getting enterprises:" + ex.Message);
            }
            return null;

        }

        public string getEnterpriseName(int entId)
        {
            string name = "";
            try
            {
                SqlConnection sqlConnection1 = new SqlConnection(GetConnectionString());
                SqlCommand cmd = new SqlCommand();
                SqlDataReader reader;

                cmd.CommandText = "SELECT EnterpriseName FROM uas_Enterprise WHERE EnterpriseID = " + entId;
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection1;

                sqlConnection1.Open();

                reader = cmd.ExecuteReader();
                // Data is accessible through the DataReader object here.

                if (reader.Read())
                {
                    name = reader.GetString(0);
                }

                reader.Close();
                sqlConnection1.Close();
               
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error getting enterprises:" + ex.Message);
            }
            return name;
        }
        
        public string getGroupName(int groupId)
        {
            string name = "";
            try
            {
                SqlConnection sqlConnection1 = new SqlConnection(GetConnectionString());
                SqlCommand cmd = new SqlCommand();
                SqlDataReader reader;

                cmd.CommandText = "SELECT GroupName FROM uas_Group WHERE GroupID = " + groupId;
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection1;

                sqlConnection1.Open();

                reader = cmd.ExecuteReader();
                // Data is accessible through the DataReader object here.
                while (reader.Read())
                {
                    IDataRecord record = (IDataRecord)reader;
                    name = record[0].ToString();
                }

                reader.Close();
                sqlConnection1.Close();

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error getting enterprises:" + ex.Message);
            }
            return name;
        }

        public Dictionary<int, string> getGroups()
        {
            Dictionary<int, String> entDict = new Dictionary<int, string>();

            try
            {
                SqlConnection sqlConnection1 = new SqlConnection(GetConnectionString());
                SqlCommand cmd = new SqlCommand();
                SqlDataReader reader;

                cmd.CommandText = "SELECT GroupID, GroupName FROM uas_Group ORDER BY GroupName";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection1;

                sqlConnection1.Open();

                reader = cmd.ExecuteReader();
                // Data is accessible through the DataReader object here.

                while (reader.Read())
                {
                    IDataRecord record = (IDataRecord)reader;

                    entDict.Add(Convert.ToInt32(record[0]), record[1].ToString());
                }

                reader.Close();
                sqlConnection1.Close();
                return entDict;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error getting groups:" + ex.Message);
            }
            return null;
        }

        public Dictionary<int, string> getGroups(int entId)
        {
            Dictionary<int, String> entDict = new Dictionary<int, string>();

            try
            {
                SqlConnection sqlConnection1 = new SqlConnection(GetConnectionString());
                SqlCommand cmd = new SqlCommand();
                SqlDataReader reader;

                cmd.CommandText = "SELECT GroupID, GroupName FROM uas_Group WHERE EnterpriseID = " + entId + " ORDER BY GroupName";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection1;

                sqlConnection1.Open();

                reader = cmd.ExecuteReader();
                // Data is accessible through the DataReader object here.

                while (reader.Read())
                {
                    IDataRecord record = (IDataRecord)reader;
                    entDict.Add(Convert.ToInt32(record[0]), record[1].ToString());
                }

                reader.Close();
                sqlConnection1.Close();
                return entDict;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error getting groups:" + ex.Message);
            }
            return null;
        }

        public string GetUasTableName(int fileId)
        {
            string tableName = String.Empty;
            if ((fileId >= 0) && (fileId < UasTables.Length))
            {
                tableName = UasTables[fileId];
            }
            return tableName;
        }

        public int GetNumberTables()
        {
            return UasTables.Length;
        }

        public bool SaveDOB(int userId, string date)
        {
            
            try
            {
                SqlConnection sqlConnection1 = new SqlConnection(GetConnectionString());
                SqlCommand cmd = new SqlCommand();


                cmd.CommandText = "UPDATE uas_User SET DOB = CAST('" + date + "' AS DATETIME) WHERE UserID = " + userId;
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection1;

                sqlConnection1.Open();

                cmd.ExecuteNonQuery();

                sqlConnection1.Close();
               
                return true;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving DOB: " + ex.Message);
            }

            return false;
        }

    }
}