using Assmnts.UasServiceRef;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UAS.Business
{
    public class UserSecurityContext
    {
        public UserContextLightweight UserContext { get; set; }

        /// <summary>
        /// Does user have this permission on any group?
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="permissionGroup"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool HasPermission(int applicationId, string permissionGroup, string permission)
        {
            if (applicationId < 0)
            {
                throw new ArgumentOutOfRangeException("Int value was less than 0.", "applicationId");
            }
            if (String.IsNullOrWhiteSpace(permissionGroup))
            {
                throw new ArgumentException("String was null or whitespace.", "permissionGroup");
            }
            if (String.IsNullOrWhiteSpace(permission))
            {
                throw new ArgumentException("String was null or whitespace.", "permission");
            }
            if (UserContext == null)
            {
                throw new InvalidOperationException("UserContext was null.");
            }
            foreach (GroupLightweight group in UserContext.Groups)
            {
                bool hasPermission = HasPermission(applicationId, group.GroupId, permissionGroup, permission);
                if (hasPermission)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Does user have this permission on this group?
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="groupId"></param>
        /// <param name="permissionGroup"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool HasPermission(int applicationId, int groupId, string permissionGroup, string permission)
        {
            if (applicationId < 0)
            {
                throw new ArgumentOutOfRangeException("Int value was less than 0.", "applicationId");
            }
            if (groupId < 1)
            {
                throw new ArgumentOutOfRangeException("Int value was less than 1.", "groupId");
            }
            if (String.IsNullOrWhiteSpace(permissionGroup))
            {
                throw new ArgumentException("String was null or whitespace.", "permissionGroup");
            }
            if (String.IsNullOrWhiteSpace(permission))
            {
                throw new ArgumentException("String was null or whitespace.", "permission");
            }
            if (UserContext == null)
            {
                throw new InvalidOperationException("UserContext was null.");
            }
            var group = UserContext.Groups
                 .Where(x => x.ApplicationId == applicationId
                 && x.GroupId == groupId)
                 .SingleOrDefault();
            if (group == null)
            {
                return false;
            }
            var _permissionGroup = group.PermissionGroups
                .Where(x => x.Name.Equals(permissionGroup, StringComparison.OrdinalIgnoreCase))
                .SingleOrDefault();
            if (_permissionGroup == null)
            {
                return false;
            }
            var _permission = _permissionGroup.Permissions
                .Where(x => x.Name.Equals(permission, StringComparison.OrdinalIgnoreCase))
                .SingleOrDefault();
            if (_permission == null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Does the user have this role on any group?
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public bool HasRole(int applicationId, string roleName)
        {
            if (applicationId < 0)
            {
                throw new ArgumentOutOfRangeException("Int value was less than 0.", "applicationId");
            }
            if (String.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException("String was null or whitespace.", "roleName");
            }
            if (UserContext == null)
            {
                throw new InvalidOperationException("UserContext was null.");
            }
            foreach (GroupLightweight group in UserContext.Groups)
            {
                bool hasPermission = HasRole(applicationId, group.GroupId, roleName);
                if (hasPermission)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Does the user have this role on this group?
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="groupId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public bool HasRole(int applicationId, int groupId, string roleName)
        {
            if (applicationId < 0)
            {
                throw new ArgumentOutOfRangeException("Int value was less than 0.", "applicationId");
            }
            if (groupId < 0)
            {
                throw new ArgumentOutOfRangeException("Int value was less than 0.", "groupId");
            }
            if (String.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException("String was null or whitespace.", "roleName");
            }
            if (UserContext == null)
            {
                throw new InvalidOperationException("UserContext was null.");
            }
            var group = UserContext.Groups
                 .Where(x => x.ApplicationId == applicationId
                 && x.GroupId == groupId)
                 .SingleOrDefault();
            if (group == null)
            {
                return false;
            }
            if (group.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }
    }
}