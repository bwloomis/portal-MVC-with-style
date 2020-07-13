using AJBoggs.Def.Services;
using Assmnts;
using Data.Abstract;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UAS.Business;
using UAS.DataDTO;

namespace WebService
{

    public static class WSBusiness
    {
        /*
         * Tests to see if logged in user has access to a form result
         */

        public static bool hasAccess(LoginStatus loginStatus, def_FormResults formResult)
        {
            if (loginStatus.appGroupPermissions[0].authorizedGroups.Contains(0))
            {
                return true;
            }

            if (formResult.assigned == loginStatus.UserID)
            {
                return true;
            }

            if (formResult.GroupID != null &&
                loginStatus.appGroupPermissions[0].authorizedGroups.Contains(formResult.GroupID.Value))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Test if a user of the web service has permissions and access to another user
        /// </summary>
        /// <param name="loginStatus"></param>
        /// <param name="userName">The user that is logged in to the web service</param>
        /// <param name="alterUserName">The user the logged in user is trying to access</param>
        /// <returns>True if the user has access to the other user</returns>        
        public static bool hasAccessToUser(LoginStatus loginStatus, string userName, string alterUserName)
        {
            if (!hasPermission(loginStatus, PermissionConstants.GROUP_WIDE, PermissionConstants.ASSMNTS))
                // The user is assign only
            {
                if (userName.ToLower() == alterUserName.ToLower()) // Only user that assign only user can edit is itself
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                List<string> users = null;

                if (loginStatus.appGroupPermissions[0].authorizedGroups.Contains(0)) // Enterprise wide admin
                {
                    users = GetUsersByEnterpriseGroup(loginStatus.EnterpriseID);
                }
                else // Regular user; limit user list to those in same group or sub group
                {

                    users = GetUsersByEnterpriseGroup(loginStatus.EnterpriseID,
                        loginStatus.appGroupPermissions[0].authorizedGroups[0]);
                }

                List<string> lowerCaseUsers = users.Select(u => u.ToLower()).ToList();

                if (lowerCaseUsers.Contains(alterUserName.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<string> GetUsersByEnterpriseGroup(int enterpriseId, int groupId = -1)
        {
            List<string> userNames = new List<string>();

            List<int> groups = new List<int>();

            if (groupId != -1)
            {
                groups.AddRange(UAS_Business_Functions.GetChildGroups(enterpriseId, groupId));
            }
            else
            {
                groups.Add(groupId);
            }

            foreach (int childGroup in groups)
            {

                using (var context = DataContext.getUasDbContext())
                {
                    try
                    {

                        List<string> addUserNames = (from u in context.uas_User
                            join guap in context.uas_GroupUserAppPermissions on u.UserID equals guap.UserID
                            where u.EnterpriseID == enterpriseId && (groupId == -1 || childGroup == guap.GroupID)
                            select u.UserName).Distinct().ToList();

                        userNames.AddRange(addUserNames);

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error checking user's enterprise: " + ex.Message);
                    }

                }
            }
            return userNames;

        }

        internal static uas_User GetUserByUserName(string userName)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {

                    uas_User user = (from u in context.uas_User
                        where u.UserName.ToLower() == userName.ToLower()
                        select u).FirstOrDefault();

                    return user;

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error checking user's enterprise: " + ex.Message);
                }
            }

            return null;
        }

        internal static uas_User GetUserByUserId(int userId)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {

                    uas_User user = (from u in context.uas_User
                                     where u.UserID == userId
                                     select u).FirstOrDefault();

                    return user;

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error checking user's enterprise: " + ex.Message);
                }
            }

            return null;
        }

        /*
         * Check if a user has a permission for their first permission group.
         * 
         */

        public static bool hasPermission(LoginStatus loginStatus, int action, string component)
        {
            string permString = loginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet;

            return UAS_Business_Functions.hasPermission(permString, action, component);
        }

        public static void InsertAccessLogRecordWebservice(LoginStatus loginStatus, IFormsRepository formsRepo,
            int formResultId, int accessLogFunctionId, string description)
        {
            def_AccessLogging accessLogging = new def_AccessLogging()
            {
                formResultId = formResultId,
                accessLogFunctionId = accessLogFunctionId,
                accessDescription = description,
                datetimeAccessed = DateTime.Now,
                EnterpriseID = loginStatus.EnterpriseID,
                UserID = loginStatus.UserID
            };

            formsRepo.AddAccessLogging(accessLogging);
        }

        public static List<string> GetItemVariableIdentifiersBySection(int sectionId)
        {
            List<string> data = null;
            using (formsEntities context = new formsEntities())
            {
                try
                {
                    StringBuilder qry = new StringBuilder("SELECT iv.identifier, si.[order]");
                    qry.Append(" FROM def_ItemVariables iv");
                    qry.Append(" JOIN def_SectionItems si on si.itemId = iv.itemId");
                    qry.Append(" where si.sectionId = " + sectionId + " and si.subSectionId IS NULL order by si.[order]");

                    using (SqlConnection sqlConn = new SqlConnection(context.Database.Connection.ConnectionString))
                    {
                        sqlConn.Open();
                        using (SqlCommand command = new SqlCommand(qry.ToString(), sqlConn))
                        {
                            command.CommandType = System.Data.CommandType.Text;
                            DataTable dt = new DataTable();
                            dt.Load(command.ExecuteReader());

                            data = dt.Rows.OfType<DataRow>().Select(dr => dr.Field<string>("identifier")).ToList();
                        }

                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }


            return data;

        }


        public static List<string> GetItemVariableIdentifiersBySectionIncludingSubSections(int sectionId)
        {
            List<int> subSections = new List<int>();

            subSections.Add(sectionId);

            List<int> subSectionsToAdd = GetSubSections(sectionId);

            subSections.AddRange(subSectionsToAdd);

            List<string> itemVariableIdentifiers = new List<string>();



            foreach (int subSection in subSections)
            {
                List<string> itemVariableIdentifiersToAdd = GetItemVariableIdentifiersBySection(subSection);

                if (itemVariableIdentifiersToAdd != null)
                {
                    itemVariableIdentifiers.AddRange(itemVariableIdentifiersToAdd);
                }
            }

            return itemVariableIdentifiers;
        }

        public static List<int> GetSubSections(int sectionId)
        {
            return CommonExport.GetSubSections(sectionId);
        }



        internal static bool hasUserPermission(LoginStatus loginStatus, WSConstants.USER_PERMISSIONS userPermission)
        {
            string uasPermissionString = GetUASPermissions(loginStatus.UserID);

            if (uasPermissionString != null)
            {
                if (UAS_Business_Functions.hasPermission(uasPermissionString, (int) userPermission, WSConstants.USERS))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetUASPermissions(int userId)
        {
            string permissions = null;

            using (var context = DataContext.getUasDbContext())
            {
                try
                {

                    uas_GroupUserAppPermissions guap = (from g in context.uas_GroupUserAppPermissions
                        where g.UserID == userId && g.ApplicationID == 0
                        select g).FirstOrDefault();

                    if (guap != null)
                    {
                        permissions = guap.SecuritySet;
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error:" + ex.Message);
                }
            }

            return permissions;
        }

        internal static uas_UserAddress GetUserAddressByUserName(string userName)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {

                    uas_UserAddress userAddress = (from u in context.uas_User
                        join a in context.uas_UserAddress on u.UserID equals a.UserID
                        where u.UserName.ToLower() == userName.ToLower()
                        select a).FirstOrDefault();

                    return userAddress;

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error checking user's enterprise: " + ex.Message);
                }
            }

            return null;
        }

        internal static uas_UserPhone GetUserPhoneByUserName(string userName)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {

                    uas_UserPhone userPhone = (from u in context.uas_User
                        join p in context.uas_UserPhone on u.UserID equals p.UserID
                        where u.UserName.ToLower() == userName.ToLower()
                        select p).FirstOrDefault();

                    return userPhone;

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error checking user's enterprise: " + ex.Message);
                }
            }

            return null;
        }

        internal static uas_UserEmail GetUserEmailByUserName(string userName)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {

                    uas_UserEmail userEmail = (from u in context.uas_User
                        join e in context.uas_UserEmail on u.UserID equals e.UserID
                        where u.UserName.ToLower() == userName.ToLower()
                        select e).FirstOrDefault();

                    return userEmail;

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error checking user's enterprise: " + ex.Message);
                }
            }

            return null;
        }

        internal static uas_GroupUserAppPermissions GetUserPermissionsByUserName(string userName, int applicationId)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {

                    uas_GroupUserAppPermissions userPermissions = (from u in context.uas_User
                        join g in context.uas_GroupUserAppPermissions on u.UserID equals g.UserID
                        where u.UserName.ToLower() == userName.ToLower()
                              && g.ApplicationID == applicationId
                        select g).FirstOrDefault();

                    return userPermissions;

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error checking user's enterprise: " + ex.Message);
                }
            }

            return null;
        }

        internal static uas_RoleAppPermissions GetRoleAppPermissionByRoleNameAndAppId(string roleName, int applicationId)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {
                    var role = (from r in context.uas_Role
                        where r.RoleName.Equals(roleName, StringComparison.CurrentCultureIgnoreCase)
                        select r).FirstOrDefault();

                    var rap = (from r in context.uas_RoleAppPermissions
                        where r.RoleID == role.RoleID &&
                              r.ApplicationID == applicationId
                        select r).FirstOrDefault();
                    return rap;

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error getting RoleAppPermission: " + ex.Message);
                }
            }

            return null;
        }

        internal static uas_Role GetRoleByRoleAppPermissionId(int roleAppPermissionId)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {
                    var rap = (from r in context.uas_RoleAppPermissions
                              where r.RoleAppPermissionsID == roleAppPermissionId
                        select r).FirstOrDefault();
                    var role = (from r in context.uas_Role
                                where r.RoleID == rap.RoleID
                                select r).FirstOrDefault();

                    return role;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error getting Role: " + ex.Message);
                }
            }

            return null;
        }

        internal static uas_Group GetGroupByGroupName(string groupName)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {
                    var group = (from g in context.uas_Group
                        where g.GroupName.Equals(groupName, StringComparison.InvariantCultureIgnoreCase)
                        select g).FirstOrDefault();
                    return group;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error getting Role: " + ex.Message);
                }
            }
            return null;
        }

        public static void UpdateSentLockoutEmail(int userId, bool locked)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {
                    var user = context.uas_User.SingleOrDefault(x => x.UserID == userId);
                    user.SentLockoutEmail = locked;
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error setting lockout email: " + ex.Message);
                }
            }
        }

        public static DateTime? GetLastLoginTime(string userName)
        {
            var user = GetUserByUserName(userName);

            using (var context = DataContext.getUasDbContext())
            {
                try
                {
                    var result = (from u in context.uas_UserLoginActivity
                        where u.UserID == user.UserID
                        orderby u.LoginDate descending
                        select u.LoginDate).FirstOrDefault();
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error getting last login time: " + ex.Message);
                }
            }

            return null;
        }

        public static void ResetLoginAttempts(int userId)
        {
            using (var context = DataContext.getUasDbContext())
            {
                try
                {
                    var user = (from u in context.uas_User
                        where u.UserID == userId
                        select u).FirstOrDefault();
                    user.LoginAttempts = 0;
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error reseting login attempts: " + ex.Message);
                }
            }
        }
    }
}