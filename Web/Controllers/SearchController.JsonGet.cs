using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using Assmnts.Infrastructure;
using Assmnts.UasServiceRef;

using Data.Concrete;

namespace Assmnts.Controllers
{
    public partial class SearchController : Controller
    {
     
        [HttpPost]
        public string GetInterviewers()
        {

            using (var context = DataContext.getUasDbContext())
            {
                context.Configuration.LazyLoadingEnabled = false;

                // RRB - 03/12/15 - added filter for Enterprise
                var users = context.uas_User.Where(usr => usr.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID).ToList();

                string jsonString = fastJSON.JSON.ToJSON(users);
                return jsonString;
            }
            
        }

        /*
         * Gets Group and subGroups - reads local database to get Group Descriptions
         */
        [HttpPost]
        public string GetGroups()
        {
            AuthenticationClient auth = new AuthenticationClient();
            using (var context = DataContext.getUasDbContext())
            {
                context.Configuration.LazyLoadingEnabled = false;

                //var groups = context.uas_Group.Where(i => i.EnterpriseID == enterpriseId).ToList();
                var groups = UAS.Business.LocalClient.GetChildGroups(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0])
                            .Where(id => id > -1)
                            .Select(groupId => context.uas_Group.FirstOrDefault(g => g.GroupID == groupId)).ToList();

                string jsonString = fastJSON.JSON.ToJSON(groups);
                return jsonString;

            }
        }

        [HttpPost]
        public string GetInterviewerInfo(string userId)
        {
            int uid = Int32.Parse(userId);

            using (var context = DataContext.getUasDbContext())
            {
                context.Configuration.LazyLoadingEnabled = false;

                uas_User user = (from i in context.uas_User
                                 where i.UserID == uid
                                 select i).FirstOrDefault();

                context.Entry(user).Collection(u => u.uas_UserAddress).Load();
                context.Entry(user).Collection(u => u.uas_UserPhone).Load();
                context.Entry(user).Collection(u => u.uas_UserEmail).Load();

                string jsonInfo = fastJSON.JSON.ToJSON(user);
                return jsonInfo;

            }
          
        }

    }
}