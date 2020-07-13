using Assmnts.Infrastructure;

using Data.Abstract;

using System.Collections.Generic;
using System.Linq;

using UAS.Business;

namespace Assmnts.Models
{

    public class TemplateAssmntNavMenu
    {
        public TemplateItems parent { get; set; }

        public string assmntTitle { get; set; }
        public string currentUser { get; set; }
        public string trackingNumber { get; set; }

        public Dictionary<int, string> forms { get; set; }

        public Dictionary<def_Parts,List<def_Sections>> sectionsByPart { get; set; }

        public bool create { get; set; }
        public bool unlock { get; set; }
        public bool delete { get; set; }
        public bool archive { get; set; }
        public bool undelete { get; set; }
        public bool ventureMode { get; set; }

        public TemplateAssmntNavMenu(IFormsRepository formsRepo)
        {

            if (SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets.Count() > 0)
            {
                create = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.CREATE, UAS.Business.PermissionConstants.ASSMNTS);
                unlock = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.UNLOCK, UAS.Business.PermissionConstants.ASSMNTS);
                delete = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.DELETE, UAS.Business.PermissionConstants.ASSMNTS);
                archive = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.ARCHIVE, UAS.Business.PermissionConstants.ASSMNTS);
                undelete = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.UNDELETE, UAS.Business.PermissionConstants.ASSMNTS);

            }

            ventureMode = SessionHelper.IsVentureMode;

            forms = Business.Forms.GetFormsDictionary(formsRepo);
            
        }

    }

}
