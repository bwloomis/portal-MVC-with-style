using Assmnts.Infrastructure;

using Data.Concrete;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;

using UAS.Business;
using UAS.DataDTO;

using Assmnts.Business;
using Assmnts.UasServiceRef;


namespace Assmnts.Models
{
    public class SearchModel
    {

        public IEnumerable<spSearchGrid_Result> SearchResult { get; set; }
        public IEnumerable<vSearchGrid> vSearchResultEnumerable { get; set; }
        public IQueryable<vSearchGrid> vSearchResult { get; set; }
        public Dictionary<int, string> Groups { get; set; }
        public Dictionary<int, string> Enterprises { get; set; }
        public Dictionary<int, string> Forms { get; set; }
        public Dictionary<int, string> AllForms { get; set; }
        public Dictionary<int, string> usedForms { get; set; }

        public Dictionary<int?, string> reviewStatuses { get; set; }

        public Dictionary<int, string> userNames { get; set; }
        
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string SearchType { get; set; }
        public bool VentureMode { get; set; }
        public bool ShowAllAssessmentsForRecipient { get; set; }
        public string GeneralSearchString { get; set; }
        public bool ShowPatternCheck { get; set; }

        public int numAssmnts { get; set; }
        public SearchCriteria DetailSearchCriteria { get; set; }
        public List<uas_User> Interviewers { get; set; }

        public bool searchPref { get; set; }
        public bool profilePref { get; set; }
        public bool passPref { get; set; }

        public bool batchCSVExportOptions { get; set; }
        public bool CSVExportOptions { get; set; }

        public bool showSearch { get; set; }

        public bool edit { get; set; }
        public bool create { get; set; }
        public bool unlock { get; set; }
        public bool delete { get; set; }
        public bool archive { get; set; }
        public bool undelete { get; set; }
        public bool editLocked { get; set; }
        public bool move { get; set; }

        public bool reviewAll { get; set; }

        public UAS.Business.PermissionConstants permConst { get; set; }

        public List<int?> formIds { get; set; }
        public List<int?> sisIds { get; set; }

        public int timeout { get; set; } 


        public SearchModel()
        {
            reviewAll = ReviewStatus.ReviewAll();

            permConst = new UAS.Business.PermissionConstants();

            VentureMode = SessionHelper.IsVentureMode;
           
            timeout = SessionHelper.SessionTotalTimeoutMinutes;

            try
            {
                if (SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets.Count() > 0)
                {
                    string permSet = SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet;
                    edit        = UAS_Business_Functions.hasPermission(permSet, UAS.Business.PermissionConstants.EDIT       , UAS.Business.PermissionConstants.ASSMNTS);
                    create      = UAS_Business_Functions.hasPermission(permSet, UAS.Business.PermissionConstants.CREATE     , UAS.Business.PermissionConstants.ASSMNTS);
                    unlock      = UAS_Business_Functions.hasPermission(permSet, UAS.Business.PermissionConstants.UNLOCK     , UAS.Business.PermissionConstants.ASSMNTS);
                    delete      = UAS_Business_Functions.hasPermission(permSet, UAS.Business.PermissionConstants.DELETE     , UAS.Business.PermissionConstants.ASSMNTS);
                    archive     = UAS_Business_Functions.hasPermission(permSet, UAS.Business.PermissionConstants.ARCHIVE    , UAS.Business.PermissionConstants.ASSMNTS);
                    undelete    = UAS_Business_Functions.hasPermission(permSet, UAS.Business.PermissionConstants.UNDELETE   , UAS.Business.PermissionConstants.ASSMNTS);
                    editLocked  = UAS_Business_Functions.hasPermission(permSet, UAS.Business.PermissionConstants.EDIT_LOCKED, UAS.Business.PermissionConstants.ASSMNTS);
                    move        = UAS_Business_Functions.hasPermission(permSet, UAS.Business.PermissionConstants.MOVE       , UAS.Business.PermissionConstants.ASSMNTS);
                  
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Missing permissoin: " +  ex.Message);
            }
            searchPref = false;
            profilePref = false;
            passPref = false;

            showSearch = true;

            UASEntities UASContext = DataContext.getUasDbContext();
            numAssmnts = getNumAssmnts(UASContext);

            EntAppConfig patternsForEnterprise = null;
            if (!SessionHelper.IsVentureMode)
            {
                AuthenticationClient webclient = new AuthenticationClient();
                var entConfig = webclient.GetEntAppConfigByEnumAndEnt("PATTERN_CHECK", SessionHelper.LoginStatus.EnterpriseID);
                if (entConfig != null && entConfig.ConfigValue == bool.TrueString)
                {
                    if (
                        UAS_Business_Functions.hasPermission(
                            SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet,
                            PermissionConstants.PATT_CHECK, PermissionConstants.ASSMNTS))
                    {
                        ShowPatternCheck = true;
                    }
                }

                patternsForEnterprise = webclient.GetEntAppConfigByEnumAndEnt("PATTERNS_FOR_ENTERPRISE",
                    SessionHelper.LoginStatus.EnterpriseID);

                if (patternsForEnterprise == null)
                {
                    patternsForEnterprise = webclient.GetEntAppConfigByEnumAndEnt("PATTERNS_FOR_ENTERPRISE", 0);
                }
            }

            //int grpPerm0Id = Assmnts.Infrastructure.SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].GroupID;

            //int grpPerm1Id;
            //if (Assmnts.Infrastructure.SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets.Count() > 1)
            //{
            //    grpPerm1Id = Assmnts.Infrastructure.SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[1].GroupID;
            //}
            //else
            //{
            //    grpPerm1Id = grpPerm0Id;
            //}

            /* **************************************************************
             * The statements below need to be replaced with pulling a variable 
             * from the web.config file.
             * 'VentureMode'.
             * 
             * Done 2/13/15 LK
             * Move up in function so can use to get timeout value if neccessary
             * 5/15/15 LK #12554
             * 
             * **************************************************************
             */
            //if (!String.IsNullOrEmpty(strVentureMode))
            //{
            //    OfflineMode = Convert.ToBoolean(strVentureMode);
            //}
           /* string currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            OfflineMode = (currentPath.ToLower().Contains("venture")) ? true : false;
*/
            ShowAllAssessmentsForRecipient = false;  // Do not show all assessments for Recipient by default.

            DetailSearchCriteria = new SearchCriteria();

            DetailSearchCriteria.InterviewerList = new List<SelectListItem>();
            DetailSearchCriteria.EnterpriseList = new List<SelectListItem>();
            DetailSearchCriteria.GroupList = new List<SelectListItem>();
            DetailSearchCriteria.PatternList = new List<SelectListItem>();

            DetailSearchCriteria.SelectedInterviewers = new List<string>();
            DetailSearchCriteria.SelectedEnts = new List<int>();
            DetailSearchCriteria.SelectedGroups = new List<int>();

            List<string> interviewers = new List<string>();
            List<uas_Group> groups = new List<uas_Group>();
            List<uas_Enterprise> ents = new List<uas_Enterprise>();

            int entId = Assmnts.Infrastructure.SessionHelper.LoginStatus.EnterpriseID;
            if (entId == 0)
            {
                interviewers = (from i in UASContext.uas_User
                                select i.LoginID).ToList();

                groups = (from g in UASContext.uas_Group
                            select g).ToList();

                ents = (from e in UASContext.uas_Enterprise
                        select e).ToList();

            }
            else
            {
                interviewers = (from i in UASContext.uas_User
                                where i.EnterpriseID == entId
                                select i.LoginID).ToList();

                ents = (from e in UASContext.uas_Enterprise
                        where e.EnterpriseID == entId
                        select e).ToList();
                

                //if (grpPerm0Id == 0)
                //{
                //    using (var UASContext = new SISEntities())
                //    {
                //        groups = (from g in UASContext.uas_Group
                //                  where g.EnterpriseID == entId
                //                  select g).ToList();
                //    }
                //}
                //else
                //{
                
                try
                {
                    if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0] == 0)
                    {
                        groups = UASContext.uas_Group.Where(g => g.EnterpriseID == entId).ToList();

                        //auth.GetGroupsByEnterprise(auth.GetEnterprise(entId)).Select(h => UASContext.uas_Group.FirstOrDefault(g => g.GroupID == h.GroupID)).ToList();

                    }
                    else
                    {

                        AppGroupPermissions agp = SessionHelper.LoginStatus.appGroupPermissions[0];
                        groups = UASContext.uas_Group.Where(g => agp.authorizedGroups.Contains(g.GroupID)).ToList();


                        //auth.GetChildGroups(entId, SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0])
                        //.Where(id => id > -1)
                        //.Select(groupId => UASContext.uas_Group.FirstOrDefault(g => g.GroupID == groupId)).ToList();
                        //groups = (from g in UASContext.uas_Group
                        //          where g.EnterpriseID == entId
                        //          && (g.GroupID == grpPerm0Id
                        //            || g.GroupID == grpPerm1Id)
                        //          select g).ToList();

                    }
                }
                //}
                catch (Exception ex) {
                    Debug.WriteLine(ex.Message);
                }
                
            }

            foreach (string interviewer in interviewers)
            {
                SelectListItem interviewerListItem = new SelectListItem()
                {
                    Text = interviewer,
                    Value = interviewer
                };
                DetailSearchCriteria.InterviewerList.Add(interviewerListItem);
            }

            foreach (uas_Group group in groups)
            {
                SelectListItem groupListItem = new SelectListItem()
                {
                    Text = group.GroupName,
                    Value = group.GroupID.ToString()
                };
                DetailSearchCriteria.GroupList.Add(groupListItem);
            }

            bool admin = false;
            foreach (int grp in SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups)
            {
                if (grp == 0)
                {
                    admin = true;
                    break;
                }
            }
            
            foreach (uas_Enterprise ent in ents)
            {
                SelectListItem entListItem = new SelectListItem()
                {
                    Text = ent.EnterpriseName,
                    Value = ent.EnterpriseID.ToString()
                };
                
                // if user is enterprise admin, show enterprise
                if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0] == 0 || admin)
                    DetailSearchCriteria.EnterpriseList.Add(entListItem);

            }

            // add patterns
            if(!SessionHelper.IsVentureMode && patternsForEnterprise.ConfigValue.Contains("1"))
            {
                DetailSearchCriteria.PatternList.Add(new SelectListItem()
                {
                    Value = "1",
                    Text = "1. Important To, Important For Utilization Check"
                });
            }
            if (!SessionHelper.IsVentureMode && patternsForEnterprise.ConfigValue.Contains("2"))
            {
                DetailSearchCriteria.PatternList.Add(new SelectListItem()
                {
                    Value = "2",
                    Text = "2. Learning Job Skills Typical Person Standard Check"
                });
            }

            if (!SessionHelper.IsVentureMode && patternsForEnterprise.ConfigValue.Contains("3"))
            {
                DetailSearchCriteria.PatternList.Add(new SelectListItem()
                {
                    Value = "3",
                    Text = "3. Transportation Consistency Check"
                });
            }

            if (!SessionHelper.IsVentureMode && patternsForEnterprise.ConfigValue.Contains("4"))
            {
                DetailSearchCriteria.PatternList.Add(new SelectListItem()
                {
                    Value = "4",
                    Text = "4. Relationship \"Typical Person Standard\" Check"
                });
            }

            if (!SessionHelper.IsVentureMode && patternsForEnterprise.ConfigValue.Contains("5"))
            {
                DetailSearchCriteria.PatternList.Add(new SelectListItem()
                {
                    Value = "5",
                    Text = "5. Sexual Aggression Community Safety Consistency Check"
                });
            }

            if (!SessionHelper.IsVentureMode && patternsForEnterprise.ConfigValue.Contains("6"))
            {
                DetailSearchCriteria.PatternList.Add(new SelectListItem()
                {
                    Value = "6",
                    Text = "6. Ambulation Consistency Check"
                });
            }
        }

        private int getNumAssmnts(UASEntities uasCntxt)
        {
            try
            {
                uas_Config config = uasCntxt.uas_Config.Where(c => c.ConfigName == "NoGridAssmnts").FirstOrDefault();

                if (config != null)
                {
                    uas_ProfileConfig profileConfig = uasCntxt.uas_ProfileConfig.Where(p => p.ConfigID == config.ConfigID).FirstOrDefault();

                    if (profileConfig != null)
                    {
                        uas_UserProfile userProfile = uasCntxt.uas_UserProfile.Where(u => u.ProfileConfigID == profileConfig.ProfileConfigID
                                                                                        && u.UserID == SessionHelper.LoginStatus.UserID).FirstOrDefault();

                        if (userProfile != null)
                        {
                            return Int32.Parse(userProfile.OptionSet);
                        }
                    }

                }
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("SearchModel.getNumAssmnts exception: " + xcptn.Message);
            }

            return 15;
        }

    }
}