using Assmnts;
using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Abstract;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using UAS.Business;

namespace AJBoggs.Sis.Templates
{

    public static class TemplateMenus
    {

        public static TemplateSisNavMenu getSisNavMenuModel(SessionForm sf, TemplateItems parent, IFormsRepository formsRepo)
        {
            TemplateSisNavMenu result = new TemplateSisNavMenu(formsRepo);

            if (SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets.Count() > 0)
            {
                result.create = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.CREATE, UAS.Business.PermissionConstants.ASSMNTS);
                result.unlock = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.UNLOCK, UAS.Business.PermissionConstants.ASSMNTS);
                result.delete = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.DELETE, UAS.Business.PermissionConstants.ASSMNTS);
                result.archive = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.ARCHIVE, UAS.Business.PermissionConstants.ASSMNTS);
                result.undelete = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.UNDELETE, UAS.Business.PermissionConstants.ASSMNTS);

            }

            result.parent = parent;
            result.ventureMode = SessionHelper.IsVentureMode;

            result.forms = Assmnts.Business.Forms.GetFormsDictionary(formsRepo);

            try
            {
                def_Forms frm = formsRepo.GetFormById(sf.formId);
                result.assmntTitle = frm.title;
                result.currentUser = SessionHelper.LoginInfo.LoginID;
                result.sectionsByPart = new Dictionary<def_Parts, List<def_Sections>>();
                foreach (def_Parts prt in formsRepo.GetFormParts(frm))
                {
                    result.sectionsByPart.Add(prt, formsRepo.GetSectionsInPart(prt));
                }
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("TemplateMenus getSisNavMenuModel get form, parts, sections exception: " + xcptn.Message);
                throw;
            }

            // LK -- 3/26/15 - #12497 Changed header tracking ID to display tracking ID instead of SIS ID
            // RRB - 9/8/2015 - refactored to handle empty or null sis_track_num
            //          This is all custom SIS code and needs to be moved somewhere else.
            try
            {
                result.trackingNumber = String.Empty;     // Default to blank
                def_ItemVariables trackIV = formsRepo.GetItemVariableByIdentifier("sis_track_num");
                if (trackIV != null)
                {
                    def_ResponseVariables trackRV = formsRepo.GetResponseVariablesByFormResultItemVarId(sf.formResultId, trackIV.itemVariableId);
                    if (trackRV != null)
                    {
                        if (!String.IsNullOrEmpty(trackRV.rspValue))
                            result.trackingNumber = trackRV.rspValue;
                    }
                }
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("SisTemplatesController getSisNavMenuModel trackingNumber exception: " + xcptn.Message);
                throw;
            }

            // BR - added to include the recipient's name on the form when applicable.
            try
            {
                result.recipientName = String.Empty;     // Default to blank
                def_ItemVariables fNameIV = formsRepo.GetItemVariableByIdentifier("sis_cl_first_nm");
                def_ItemVariables lNameIV = formsRepo.GetItemVariableByIdentifier("sis_cl_last_nm");
                if (fNameIV != null)
                {
                    def_ResponseVariables fNameRV = formsRepo.GetResponseVariablesByFormResultItemVarId(sf.formResultId, fNameIV.itemVariableId);
                    if (fNameRV != null)
                    {
                        if (!String.IsNullOrEmpty(fNameRV.rspValue))
                            result.recipientName = fNameRV.rspValue;
                    }
                }

                if (lNameIV != null)
                {
                    def_ResponseVariables lNameRV = formsRepo.GetResponseVariablesByFormResultItemVarId(sf.formResultId, lNameIV.itemVariableId);
                    if (lNameRV != null)
                    {
                        if (!String.IsNullOrEmpty(lNameRV.rspValue))
                        {
                            if (!String.IsNullOrEmpty(result.recipientName))
                            {
                                result.recipientName += " ";
                            }
                            result.recipientName += lNameRV.rspValue;
                        }
                    }
                }
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("TemplateMenus getSisNavMenuModel trackingNumber exception: " + xcptn.Message);
                throw;
            }

            return result;
        }
        
    }
}