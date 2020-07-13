using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using System.Data;
using System.Data.Sql;
using System.Data.Common;
using System.Data.SqlClient;

using Assmnts;
using Assmnts.Controllers;

using Data.Concrete;

using UAS.DataDTO;
using Assmnts.Infrastructure;

namespace UAS.Business
{
    /*
     * This emulates some of the essential methods of the webclient which connects to the UAS Web Service
     */
    public static class LocalClient
    {

        #region - constants
        // private const string ENCKEY = "d05Xf6dR";
        // private const string TPSSKEY = "xc567hSrjb$^*[{:";
        #endregion - END constants


        /*
         * returns an XML string
         */
        public static string AuthenticateUser( string passkey, string program, string domain,
                                                    string loginID, string pwd, string sessionData)
        {
            string validation = String.Empty;

            return validation;
        }

        /*
        * returns an XML string
        */
        public static string AuthenticateLocalUser(string passkey, string domain,
                                                    string loginID, string pwd)
        {
            // Debug.WriteLine("* * *  AuthenticateLocalUser UasAdo.GetProviderVersion: " + UasAdo.GetProviderVersion());

            // Checks the current number of enterprises in the database
            int rowCount = UasAdo.GetTableRowCount("[dbo].uas_Enterprise");
            Debug.WriteLine("* * *  AuthenticateLocalUser uas_User rowCount: " + rowCount.ToString());
           
            CookieContainer cc = new CookieContainer();

            bool loggedIntoServer = false;

            try
            {
                string lnk = ConfigurationManager.AppSettings["SISOnlineUrl"] + "Defws/Login?UserId=" + loginID + "&pwrd=" + pwd;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(lnk);
                request.Timeout = -1;
                request.CookieContainer = cc;

                using (WebResponse response = request.GetResponse())
                {
                    Debug.WriteLine("LocalClient HttpWebResponse Status: " + ((HttpWebResponse)response).StatusDescription);
              
                    Stream dataStream = ((HttpWebResponse)response).GetResponseStream();

                    using (StreamReader streamReader = new StreamReader(dataStream))
                    {
                        while (streamReader.Peek() >= 0)
                        {
                            string line = streamReader.ReadLine();
                            Debug.WriteLine(line);
                            if (line == "You are now logged in.")
                            {
                                loggedIntoServer = true;
                                break;
                            }

                        }
                    }
                    
                }
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("LocalClient HttpWebRequest: " + xcptn.Message);

                SessionHelper.Write("NoServer", true);
                // Get the local User record
                uas_User usr = null;
                try
                {
                    using (DbConnection connection = UasAdo.GetUasAdoConnection())
                    {
                        connection.Open();
                        usr = UasAdo.GetUserByLogin(connection, loginID);
                        connection.Close();
                        // if the User was not in the local database
                        if (usr == null)
                            throw new ApplicationException("User was not in the local database.");
                    }

                }
                catch (Exception xcp)
                {
                    Debug.WriteLine("LocalClient uas_User.Where exception: " + xcp.Message);
                    throw;
                }

                // Authenticate the User against the local database
                string encryptPwd = UtilityFunction.EncryptPassword(pwd);
                if (string.IsNullOrEmpty(pwd) || !usr.Password.Equals(encryptPwd))
                {
                    Debug.WriteLine("Password NOT correct.  Password: " + usr.Password + "    password: " + pwd);
                    throw new ApplicationException("Password was incorrect for the local database.");
                }

                Debug.WriteLine("LocalClient Authentication OK.  Get Permissions.");

                string xmlResult = String.Empty;
                List<GroupPermissionSet> groupPermissionSet = null;
                groupPermissionSet = RetrieveGroupUserAppPermissions(usr.UserID);
                xmlResult = CreateUserXML(usr, groupPermissionSet);
                Debug.WriteLine("LocalClient xmlResult: " + xmlResult);

                return xmlResult;
            }

                        
            if (loggedIntoServer)
            {
                SessionHelper.Write("NoServer", false);
                bool firstLoginOfEnterprise = false;
                try
                {
                    DataSyncController dsc = new DataSyncController();
                    dsc.DownloadUasTables(cc);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("LocalClient Sync UAS tables: " + ex.Message);
                }


                // Count number of enterprises in enterprise table now
                int newRowCount = UasAdo.GetTableRowCount("[dbo].uas_Enterprise");

                // First Login of user for enterprise
                if (newRowCount > rowCount)
                {
                    firstLoginOfEnterprise = true;
                }
                rowCount = newRowCount;
                Debug.WriteLine("* * *  AuthenticateLocalUser uas_User 2nd check rowCount: " + rowCount.ToString());
                //if (rowCount == 0)
                //{
                //    throw new ApplicationException("User did not authenticate against remote server.");
                //}

            

                // Get the local User record
                uas_User usr = null;
                try
                {
                    using (DbConnection connection = UasAdo.GetUasAdoConnection())
                    {
                        connection.Open();
                        usr = UasAdo.GetUserByLogin(connection, loginID);
                        connection.Close();
                        // if the User was not in the local database
                        if (usr == null)
                            throw new ApplicationException("User was not in the local database.");
                    }

                }
                catch (Exception xcptn)
                {
                    Debug.WriteLine("LocalClient uas_User.Where exception: " + xcptn.Message);
                    throw;
                }

                // Authenticate the User against the local database
                string encryptPwd = UtilityFunction.EncryptPassword(pwd);
                if (string.IsNullOrEmpty(pwd) || !usr.Password.Equals(encryptPwd))
                {
                    Debug.WriteLine("Password NOT correct.  Password: " + usr.Password + "    password: " + pwd);
                    throw new ApplicationException("Password was incorrect for the local database.");
                }

                Debug.WriteLine("LocalClient Authentication OK.  Get Permissions.");

                string xmlResult = String.Empty;
                List<GroupPermissionSet> groupPermissionSet = null;
                groupPermissionSet = RetrieveGroupUserAppPermissions(usr.UserID);
                xmlResult = CreateUserXML(usr, groupPermissionSet);
                Debug.WriteLine("LocalClient xmlResult: " + xmlResult);

                if (firstLoginOfEnterprise)
                {
                    DataSyncController ds = new DataSyncController();
                    ds.DownloadAllMetaTables(cc);
                }

                return xmlResult;
            }
            else
            {
                throw new ApplicationException("Invalid user name or password.");
            }
        }

        
        public static string SignUserOut(string passkey, string loginId)
        {
            string validation = String.Empty;

            return validation;
        }


        private static string CreateUserXML(uas_User usr, List<GroupPermissionSet> app_permissions)
        {
                string x = string.Empty;
                try
                {
                    string email = String.Empty;
                    string status = String.Empty;
                    string userRole = String.Empty;

                    try
                    {
                        using (DbConnection connection = UasAdo.GetUasAdoConnection())
                        {
                            connection.Open();
                            email = UasAdo.GetUserEmail(connection, usr.UserID);
                            connection.Close();
                        }

                    }
                    catch (Exception xcptn)
                    {
                        Debug.WriteLine("LocalClient uas_User.Where exception: " + xcptn.Message);
                        throw;
                    }

                    status = "OK";
                    // string status = context.uas_StatusFlagType.Where(s => s.StatusFlag == usr.StatusFlag).Select(s => s.Description).FirstOrDefault();

                    /*
                    string userRole = (from guap in context.uas_GroupUserAppPermissions
                                       join rap in context.uas_RoleAppPermissions on guap.RoleAppPermissionsID equals rap.RoleAppPermissionsID
                                       join role in context.uas_Role on rap.RoleID equals role.RoleID
                                       where guap.UserID == usr.UserID
                                       select role.RoleName).FirstOrDefault();
                     */
                    userRole = "User";

                    x = "<record>" +
                         "<userid>" + usr.UserID.ToString() + "</userid>" +
                         "<enterprise_id>" + usr.EnterpriseID.ToString() + "</enterprise_id>" +
                         "<useremail>" + email + "</useremail>" +
                         "<userfirstname>" + usr.FirstName + "</userfirstname>" +
                         "<userlastname>" + usr.LastName + "</userlastname>" +
                         "<applicationid>" + UAS.Business.Constants.APPLICATIONID + "</applicationid>" +
                         "<statusflag>" + usr.StatusFlag.ToString() + "</statusflag>" +
                         "<status>" + status + "</status>" +
                         "<role>" + userRole + "</role>" +
                         "<application_permissions>";
                    StringBuilder sbXml = new StringBuilder(x);
                    if (app_permissions != null)
                    {
                        foreach (GroupPermissionSet gps in app_permissions)
                        {
                            string permSet = String.IsNullOrEmpty(gps.PermissionSet) ? string.Empty : gps.PermissionSet;
                            sbXml.Append("<group_permission id=\""  + gps.GroupID.ToString() + "\" >" + permSet + "</group_permission>");
                        }
                    }

                    // Add authorized Groups
                    sbXml.Append("<authorizedGroups>");
                    if (app_permissions != null)
                    {
                        int[] groupIds = GetChildGroups(usr.EnterpriseID, app_permissions[0].GroupID);
                        foreach (int g in groupIds)
                        {
                            if (g == -1)
                            {
                                break;
                            }
                            sbXml.Append("<groupId>" + g.ToString() + "</groupId>");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("LocalClient.cs CreateUserXML: No App Permissions present (app_permissions = null)");
                    } 
                    sbXml.Append("</authorizedGroups>");

                    // Finish XML

                    sbXml.Append("</application_permissions>");
                    sbXml.Append("<userkey>" + usr.UserKey + "</userkey>");
                    sbXml.Append("<securedomain>" + "false" + "</securedomain>");
                    sbXml.Append("<errormessage>" + " " + "</errormessage>");


                    sbXml.Append("</record>");
                    x = sbXml.ToString();
                    Debug.WriteLine("CreateUserXML: " + x);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("LocalClient.cs  CreateUserXML: " + ex.Message);
                }

                return x;

        }

        public static int[] GetChildGroups(int entId, int groupId)
        {
            const int MAX_GROUPS_SIZE = 256;

            // Special case - 0 (zero) is Enterprise wide or ALL groups.
            if (groupId == 0)
            {
                return new int[1] { 0 };
            }

            // Initialize the array of Groups
            int[] grpIds = new int[MAX_GROUPS_SIZE];
            for (int i = 0; i < grpIds.Length; i++)
            {
                grpIds[i] = -1;
            }
            grpIds[0] = groupId;

            int parentIdx = 0;
            int nextChildIdx = 1;

            try
            {
                using (var context = DataContext.getUasDbContext())
                {
                    while (parentIdx < nextChildIdx)
                    {
                        if (nextChildIdx > MAX_GROUPS_SIZE)           // Don't exceed the array size
                        {
                            break;
                        }

                        int pgi = grpIds[parentIdx];

                        int[] childGroupsList = context.uas_Group.Where(g => (g.ParentGroupId == pgi)
                                                                          && (g.EnterpriseID == entId)
                                                                          && (g.ParentGroupId != g.GroupID))
                                                                .Select(g => g.GroupID).ToArray();

                        if ((childGroupsList == null) || (childGroupsList.Length == 0))
                        {
                            break;
                        }
                        else
                        {
                            foreach (int group in childGroupsList)
                            {
                                if (!grpIds.Contains(group))        // Check to make sure it is not already in the array.
                                {
                                    grpIds[nextChildIdx] = group;
                                    nextChildIdx++;
                                }
                            }
                        }

                        parentIdx++;

                    }
                }
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("LocalClient.GetChildGroups exception: " + xcptn.Message);
                if (xcptn.InnerException != null && xcptn.InnerException.Message != null)
                    Debug.WriteLine("LocalClient.GetChildGroups inner exception: " + xcptn.InnerException.Message);
            }

            // Shrink the array to just what is needed.
            int grpCount = 0;
            while ((grpIds[grpCount] != -1) && (grpCount < MAX_GROUPS_SIZE))
            {
                grpCount++;
            }
            int[] childGroupIds = new int[grpCount];

            Array.Copy(grpIds, childGroupIds, grpCount);

            return childGroupIds;
        }


        private static List<GroupPermissionSet> RetrieveGroupUserAppPermissions(int userId)
        {
            Debug.WriteLine("RetrieveGroupUserAppPermissions userId " + userId.ToString());
            List<GroupPermissionSet> gpsList = new List<GroupPermissionSet>();
            try
            {
                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();
                    List<uas_GroupUserAppPermissions> guapList = UasAdo.GetGroupPermissions(connection, userId);
                    connection.Close();

                    foreach (uas_GroupUserAppPermissions guap in guapList)
                    {
                        GroupPermissionSet gps = new GroupPermissionSet();
                        gps.GroupID = (int)guap.GroupID;
                        gps.PermissionSet = guap.SecuritySet;
                        gpsList.Add(gps);
                    }
                    connection.Close();
                }

            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("LocalClient uas_User.Where exception: " + xcptn.Message);
                throw;
            }

            return gpsList;
        }
                           
    }

}
