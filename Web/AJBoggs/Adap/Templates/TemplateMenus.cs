using Assmnts;
using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Abstract;

using System;
using System.Collections.Generic;

namespace AJBoggs.Adap.Templates
{

    public static class TemplateMenus
    {

        public static TemplateAdapNavMenu getAdapNavMenuModel(SessionForm sf, IFormsRepository formsRepo)
        {
            TemplateAdapNavMenu result = new TemplateAdapNavMenu();

            result.sctId = sf.sectionId;
            
            def_Sections currentSection = formsRepo.GetSectionById(result.sctId);
            if (currentSection != null)
                result.currentSectionTitle = currentSection.title;

            result.sectionIds = new Dictionary<string, int>();
            result.sectionTitles = new Dictionary<string, string>();
            foreach ( Assmnts.def_Sections sct in getTopLevelSectionsInForm( sf.formId, formsRepo ) )
            {
                //Assmnts.def_Sections sct = formsRepo.GetSectionByIdentifier(identifier);
                //if (sct == null)
                //{
                //    throw new Exception("could not find section with identifier \"" + identifier + "\"");
                //}

                result.sectionIds.Add(sct.identifier, sct.sectionId);
                result.sectionTitles.Add(sct.identifier, sct.title);
            }

            result.adapFormId = sf.formResultId;
            result.firstName = String.Empty;
            Assmnts.def_ResponseVariables firstNameRV = formsRepo.GetResponseVariablesByFormResultIdentifier(result.adapFormId, "ADAP_D1_FirstName");

            if (firstNameRV != null)
            {
                result.firstName = firstNameRV.rspValue;
            }

            result.lastName = String.Empty;
            Assmnts.def_ResponseVariables lastNameRV = formsRepo.GetResponseVariablesByFormResultIdentifier(result.adapFormId, "ADAP_D1_LastName");

            if (lastNameRV != null)
            {
                result.lastName = lastNameRV.rspValue;
            }

            result.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;

            result.adapPartId = sf.partId;
            result.access = UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts");
            result.readOnly = sf.readOnlyMode;

            return result;
        }

        private static List<def_Sections> getTopLevelSectionsInForm(int formId, IFormsRepository formsRepo)
        {
            List<def_Sections> result = new List<def_Sections>();
            foreach (def_FormParts fp in formsRepo.GetFormPartsByFormId( formId ) )
            {
                result.AddRange(formsRepo.GetSectionsInPartById(fp.partId));
            }
            return result;
        }
    }
}