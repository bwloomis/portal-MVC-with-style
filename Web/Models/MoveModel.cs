using Assmnts.Infrastructure;

using Data.Abstract;
using Data.Concrete;

using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Assmnts.Models
{
    public class MoveModel
    {
        IFormsRepository formsRepo { get; set; }

        public string firstName { get; set; }

        public string lastName { get; set; }

        public int formResultId { get; set; }

        public int? recipientID { get; set; }
        
        public string assigned { get; set; }

        public int newEnterpriseID { get; set; }

        public int newGroupID { get; set; }

        public int newAssigned { get; set; }

        public string enterprise { get; set; }
        
        public string group { get; set; }

        public int selectedGroup { get; set; }

        public int selectedEnterprise { get; set; }

        public int selectedUser { get; set; }

        public List<SelectListItem> LoginIDs {get; set;}
        public List<SelectListItem> Groups { get; set; }
        public List<SelectListItem> Enterprises { get; set; }
        
        public MoveModel(int frID, IFormsRepository fr)
        {
            LoginIDs = new List<SelectListItem>();
            
            formResultId = frID;

            formsRepo = fr;

            def_FormResults formResult = formsRepo.GetFormResultById(formResultId);

            def_ResponseVariables rvFirst = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "sis_cl_first_nm");
            if (rvFirst != null) {
                firstName = rvFirst.rspValue;
            }

            def_ResponseVariables rvLast = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "sis_cl_last_nm");
            if (rvLast != null)
            {
                lastName = rvLast.rspValue;
            } 
            
            recipientID = formResult.subject;

            using (var context = DataContext.getUasDbContext())
            {
                if (formResult.EnterpriseID != null)
                {

                    enterprise = context.uas_Enterprise.Where(e => e.EnterpriseID == formResult.EnterpriseID).Select(e => e.EnterpriseName).FirstOrDefault();
                    selectedEnterprise = (int)formResult.EnterpriseID;
                }
                if (formResult.GroupID != null)
                {
                    group = context.uas_Group.Where(g => g.GroupID == formResult.GroupID).Select(g => g.GroupName).FirstOrDefault();
                    selectedGroup = (int)formResult.GroupID;
                }
                if (formResult.assigned != null)
                {
                    uas_User assignedUser = context.uas_User.Where(u => u.UserID == formResult.assigned).Select(u => u).FirstOrDefault();

                    selectedUser = (int)formResult.assigned;
                    if (assignedUser != null)
                    {
                        assigned = assignedUser.UserName;

                        //if (assignedUser.StatusFlag != "A")
                        //{
                        //    assigned += " (inactive)";
                        //}
                    }
                }
                LoginIDs.Add(new SelectListItem { Value = "", Text = "" });

                if (SessionHelper.LoginStatus.EnterpriseID == 0) // User has site wide (all enterprise) access
                {
                    Enterprises = new List<SelectListItem>();

                    Groups = new List<SelectListItem>();

                    List<uas_Enterprise> enterprises = context.uas_Enterprise.Where(e => e.StatusFlag == "A").Select(e => e).ToList();

                    foreach (uas_Enterprise ent in enterprises)
                    {
                        Enterprises.Add(new SelectListItem { Value = ent.EnterpriseID.ToString(), Text = ent.EnterpriseName });
                    }

                    List<uas_Group> groups = context.uas_Group.Where(g => g.StatusFlag == "A" && g.EnterpriseID == selectedEnterprise).Select(g => g).ToList();

                    Groups.Add(new SelectListItem { Value = "", Text = "" });

                    foreach (uas_Group grp in groups)
                    {
                        Groups.Add(new SelectListItem { Value = grp.GroupID.ToString(), Text = grp.GroupName });
                    }
                    List<uas_User> users = null;
                    if (selectedGroup > 0) 
                    {
                        List<int> userIdsForUserGroup = context.uas_GroupUserAppPermissions.Where(g =>
                            selectedGroup == g.uas_Group.GroupID && g.StatusFlag == "A").Select(g => g.UserID).ToList();
                         users = context.uas_User.Where(u => u.EnterpriseID == selectedEnterprise
                            && userIdsForUserGroup.Contains(u.UserID) && u.StatusFlag == "A").Select(u => u).ToList();
                
                    } else {
                        users = context.uas_User.Where(u => u.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID).Select(u => u).ToList();
                    }
                    foreach (uas_User user in users)
                    {
                        LoginIDs.Add(new SelectListItem { Value = user.UserID.ToString(), Text = user.UserName });
                    }
                }
                else if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Contains(0)) // User has enterprise wide access (all groups/users in enterprise)
                {
                    Groups = new List<SelectListItem>();
                    
                    List<uas_User> users = context.uas_User.Where(u => u.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && u.StatusFlag == "A").Select(u => u).ToList();

                    foreach (uas_User user in users)
                    {
                        LoginIDs.Add(new SelectListItem { Value = user.UserID.ToString(), Text = user.UserName });
                    }

                    List<uas_Group> groups = context.uas_Group.Where(g => g.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && g.StatusFlag == "A").Select(g => g).ToList();

                    Groups.Add(new SelectListItem { Value = "", Text = "" });
                    
                    foreach (uas_Group grp in groups)
                    {
                        Groups.Add(new SelectListItem { Value = grp.GroupID.ToString(), Text = grp.GroupName });
                    }
                }
                else // Regular user. No selection for enterprise or group.
                {
                    var authorizedGroups = SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups;
                    
                    List<int> userIdsForUserGroup = context.uas_GroupUserAppPermissions.Where(g =>
                        authorizedGroups.Contains(g.uas_Group.GroupID) && g.StatusFlag == "A").Select(g => g.UserID).ToList();
                    List<uas_User> users = context.uas_User.Where(u => u.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID 
                        && userIdsForUserGroup.Contains(u.UserID) && u.StatusFlag == "A").Select(u => u).ToList();
                   
                    foreach (uas_User user in users)
                    {
                        LoginIDs.Add(new SelectListItem { Value = user.UserID.ToString(), Text = user.UserName });
                    }
                }
            }
        }
        
        public void GetGroups(int enterpriseId)
        {
            Groups.Clear();

            using (var context = DataContext.getUasDbContext())
            {

                List<uas_Group> groups = context.uas_Group.Where(g => g.EnterpriseID == enterpriseId && g.StatusFlag == "A").Select(g => g).ToList();

                Groups.Add(new SelectListItem { Value = "", Text = "" });

                foreach (uas_Group grp in groups)
                {
                    Groups.Add(new SelectListItem { Value = grp.GroupID.ToString(), Text = grp.GroupName });
                }
            }
        }

        public void GetUsers(int enterpriseId)
        {
            LoginIDs.Clear();
            using (var context = DataContext.getUasDbContext())
            {
                List<uas_User> users = context.uas_User.Where(u => u.StatusFlag == "A" && u.EnterpriseID == enterpriseId).Select(u => u).ToList();

                LoginIDs.Add(new SelectListItem { Value = "", Text = "" });
                
                foreach (uas_User user in users)
                {
                    LoginIDs.Add(new SelectListItem { Value = user.UserID.ToString(), Text = user.UserName });
                }
            }
        }

        public void GetUsersByGroup(int groupId, int entId = -1)
        {
            LoginIDs.Clear();
            LoginIDs.Add(new SelectListItem { Value = "", Text = "" });
            List<uas_User> users = null;
            using (var context = DataContext.getUasDbContext())
            {
                if (groupId != -1)
                {

                    List<int> userIdsForUserGroup = context.uas_GroupUserAppPermissions.Where(g => g.uas_Group.GroupID == groupId && g.StatusFlag == "A")
                        .Select(g => g.UserID).ToList();
                    users = context.uas_User.Where(u => userIdsForUserGroup.Contains(u.UserID) && u.StatusFlag == "A").Select(u => u).ToList();
                }
                else
                {
                    if (entId == -1)
                    {
                        users = context.uas_User.Where(u => u.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && u.StatusFlag == "A").Select(u => u).ToList();
                    } 
                    else 
                    {
                        users = context.uas_User.Where(u => u.EnterpriseID == entId && u.StatusFlag == "A").Select(u => u).ToList();
                    }
                }
                foreach (uas_User user in users)
                {
                    LoginIDs.Add(new SelectListItem { Value = user.UserID.ToString(), Text = user.UserName });
                }
            }
        }

        public void GetGroupByUser(int groupId)
        {

        }
    }
}