using AJBoggs.Def.Services;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Assmnts.UasServiceRef;
using Data.Abstract;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using UAS.Business;

namespace Assmnts.Business
{
    /*
     * This class is used to perform "generic" validation (based on meta-data), and also provides helpers 
     * for use in implimenting one-off validation for specific applications.
     * 
     * Usage:
     * 
     * //create an instance of SharedValidation for the current formResult and DB context
     * SharedValidation sv = new SharedValidation( frId, formsRepo );
     * 
     * //perform generic validation
     * sv.doGenericValidation( ... );
     * 
     * //EXAMPLE impliment one-off validation using other sv methods
     * if( !sv.anyPositiveResponses( "checkbox_identifier_1", "checkbox_identifier_2", "checkbox_identifier_3" )
     * {
     *     errorMessage = "you must check at least one of the three checkboxes";
     * }
     */
    public class SharedValidation
    {
        private readonly List<ValuePair> allResponses;

        public SharedValidation( List< ValuePair > allResponses )
        {
            this.allResponses = allResponses;
        }

        /// <summary>
        /// Perform generic validation:
        /// 
        /// For the given formResultId (assigned in SharedValidation constructor), 
        /// Make sure there is some non-null, non-whitespace response value for each of the 
        /// def_ItemVariables corresponding to a def_SectionItems entry (or def_SectionItemsEnt entry) 
        /// where the "display" "validation" and "requiredForm" flags are all marked 1/true.
        /// 
        /// Only def_SectionItems for the current def_Forms (parameter "frm") are considered.
        /// 
        /// Only def_SectionItemsEnt for the current def_Forms and form enterprise
        /// </summary>
        /// <param name="amt"></param>
        /// <param name="frm"></param>
        /// <param name="ItemVariableSuffixesToSkip"></param>
        /// <returns></returns>
        public bool DoGenericValidation(
                IFormsRepository formsRepo,
                TemplateItems amt,
                def_Forms frm,
                string[] ItemVariableSuffixesToSkip = null)
        {

            //pick an enterprise ID to use for validation purposes
            int entId = SessionHelper.LoginStatus.EnterpriseID;
            bool invalid = false;
            amt.missingItemVariablesBySectionByPart = new Dictionary<int, Dictionary<int, List<string>>>();

            //get a list of identifiers for all the required item variables
            //typically this list will be pulled from a single EntAppConfig record for this enterprise/form
            //if such a record is not available, the meta-data structure will be traversed to find the required itemvariables
            //and an EntAppDonfig record will be added to speed the next validation for this enterprise/form
            List<ItemVariableIdentifierWithPartSection> requiredIvs = GetRequiredItemVarsWithPartsSections(formsRepo, frm, entId);

            //iterate through all the required item variable identifiers
            foreach (ItemVariableIdentifierWithPartSection ivps in requiredIvs)
            {
                //if this identifier ends with one of the "skippable" suffixes, skip it
                if (ItemVariableSuffixesToSkip != null)
                {
                    bool skip = false;
                    foreach (string suffixtoSkip in ItemVariableSuffixesToSkip)
                    {
                        if (ivps.itemVariableIdentifier.EndsWith(suffixtoSkip))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip)
                        continue;
                }

                //check if we have a valid response for this required item variable
                ValuePair vp = allResponses.FirstOrDefault(vpt => vpt.identifier == ivps.itemVariableIdentifier);
                if (vp == null || vp.rspValue == null || vp.rspValue.Trim().Length == 0 )
                {
                    invalid = true;
                    AddMissingItemtoModel(ivps, amt);
                }
            }

            return invalid;
        }

        private void AddMissingItemtoModel(ItemVariableIdentifierWithPartSection ivps, TemplateItems amt)
        {
            if (!amt.missingItemVariablesBySectionByPart.ContainsKey(ivps.partId))
                amt.missingItemVariablesBySectionByPart.Add(ivps.partId, new Dictionary<int, List<string>>());
            if (!amt.missingItemVariablesBySectionByPart[ivps.partId].ContainsKey(ivps.sectionId))
                amt.missingItemVariablesBySectionByPart[ivps.partId].Add(ivps.sectionId, new List<string>());
            if (!amt.missingItemVariablesBySectionByPart[ivps.partId][ivps.sectionId].Contains(ivps.itemVariableIdentifier))
                amt.missingItemVariablesBySectionByPart[ivps.partId][ivps.sectionId].Add(ivps.itemVariableIdentifier);
        }

        public class ItemVariableIdentifierWithPartSection
        {
            public string itemVariableIdentifier;
            public int partId, sectionId;
        }

        public static List<ItemVariableIdentifierWithPartSection> GetRequiredItemVarsWithPartsSections(IFormsRepository formsRepo, def_Forms frm, int enterpriseId)
        {
            //if not in venture mode, get the required item vars as normal (without touching uas_EntApConfig)
            if( !SessionHelper.IsVentureMode )
                return ComputeRequiredItemVarsWithPartsSections(formsRepo, frm, enterpriseId);

            string configEnumCode = "Required_ItemVariables_FormId_" + frm.formId;

            //attempt to query a pre-computed list of item variabel identifiers from the local Venture DB
            uas_EntAppConfig eac = null;
            using (DbConnection connection = UasAdo.GetUasAdoConnection())
            {
                connection.Open();
                eac = UasAdo.GetEntAppConfig(connection, configEnumCode, enterpriseId);
            }

            //if no pre-computed list was found, create it now and insert it into the local Venture DB so it can be quickly retrieved next time
            if (eac == null || String.IsNullOrWhiteSpace(eac.ConfigValue))
            {
                List<ItemVariableIdentifierWithPartSection> requiredIvs = ComputeRequiredItemVarsWithPartsSections(formsRepo, frm, enterpriseId);
                string configValueString = String.Empty;
                foreach (ItemVariableIdentifierWithPartSection ivps in requiredIvs)
                {
                    configValueString += ivps.partId + "," + ivps.sectionId + "," + ivps.itemVariableIdentifier + ";";
                }

                using (DbConnection connection = UasAdo.GetUasAdoConnection())
                {
                    connection.Open();
                    UasAdo.AddEntAppConfig(connection, new uas_EntAppConfig()
                    {
                        EnterpriseID = enterpriseId,
                        ApplicationID = UAS.Business.Constants.APPLICATIONID,
                        EnumCode = configEnumCode,
                        baseTypeId = 14,
                        ConfigName = "Required ItemVariable Identifiers for Form " + frm.identifier,
                        ConfigValue = configValueString,
                        CreatedDate = DateTime.Now,
                        CreatedBy = 1,
                        StatusFlag = "A"
                    });
                }
                return requiredIvs;
            }

            //if a pre-computed list was found in the local Venture DB, parse identifiers, partIds, sectionIds from the configValue
            List<ItemVariableIdentifierWithPartSection> result = new List<ItemVariableIdentifierWithPartSection>();
            string[] chunks = eac.ConfigValue.Split(';');
            foreach (string chunk in chunks)
            {
                if (String.IsNullOrWhiteSpace(chunk))
                    continue;
                try
                {
                    string[] subChunks = chunk.Split(',');
                    result.Add(new ItemVariableIdentifierWithPartSection
                    {
                        partId = Convert.ToInt32(subChunks[0]),
                        sectionId = Convert.ToInt32(subChunks[1]),
                        itemVariableIdentifier = subChunks[2]
                    });
                }
                catch (Exception e)
                {
                    throw new Exception( "Error converting string \"" + chunk + "\" into ItemVariableIdentifierWithPartSection", e );
                }
            }

            return result;
        }

        /// <summary>
        /// Step through meta-data heirarchy and retrieve a list of identifiers for all required def_ItemVariables
        /// 
        /// Also include the partId and sectionId for each def_ItemVariable identifier returned.
        /// </summary>
        /// <param name="frm"></param>
        /// <returns></returns>
        private static List<ItemVariableIdentifierWithPartSection> ComputeRequiredItemVarsWithPartsSections(IFormsRepository formsRepo, def_Forms frm, int enterpriseId)
        {
            List<ItemVariableIdentifierWithPartSection> result = new List<ItemVariableIdentifierWithPartSection>();
            List<def_FormParts> formParts = formsRepo.GetFormPartsByFormId(frm.formId);
            foreach (def_FormParts fp in formParts)
            {
                var part = formsRepo.GetPartById(fp.partId);
                formsRepo.GetPartSections(part); //Loads part sections into part def_PartSections property
                def_Sections firstSection = null;

                foreach (def_PartSections ps in part.def_PartSections)
                {
                    var partSectionsEnt = formsRepo.GetPartSectionsEnt(ps);
                    bool skip = false;
                    foreach (def_PartSectionsEnt pse in partSectionsEnt)
                    {
                        if (pse.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && !pse.visible)
                        {
                            skip = true;
                            continue;
                        }
                    }

                    if (skip)
                        continue;
                    List<def_SectionItems> allSectionItems = new List<def_SectionItems>();

                    var section = formsRepo.GetSectionById(ps.sectionId);
                    firstSection = section;
                    formsRepo.SortSectionItems(section); //This loads section items into section.def_SectionItems

                    foreach (def_SectionItems si in section.def_SectionItems)
                    {
                        var sectionItemsEnt = formsRepo.GetSectionItemsEnt(si);
                        // if no items are found, ICollection.Any() will return false, .All() will return true.
                        if (si.display == false && !sectionItemsEnt.Any(sie => sie.EnterpriseID == enterpriseId && sie.validation == 1))
                            continue;
                        if (si.subSectionId.HasValue)
                        {
                            def_Sections sub = formsRepo.GetSubSectionById(si.subSectionId.Value);
                            formsRepo.SortSectionItems(sub); //This loads section items into sub.def_SectionItems
                            foreach (def_SectionItems ssi in sub.def_SectionItems)
                                allSectionItems.Add(ssi);
                        }
                        else
                            allSectionItems.Add(si);
                    }

                    foreach (def_SectionItems si in allSectionItems)
                    {
                        si.def_SectionItemsEnt = formsRepo.GetSectionItemsEnt(si);
                        si.def_Items = formsRepo.GetItemById(si.itemId);
                        if (si.display == false && !si.def_SectionItemsEnt.Any(sie => sie.EnterpriseID == enterpriseId && sie.validation == 1))
                        {
                            Debug.WriteLine("* * *  SharedValidation:DoGenericValidation method  * * * Validation: skipping section item with display=false (itm label = " + si.def_Items.label + ")");
                            continue;
                        }

                        if (!si.requiredForm)
                        {
                            // if no items are found, ICollection.Any() will return false, .All() will return true.
                            if (!si.def_SectionItemsEnt.Any(r => r.EnterpriseID == enterpriseId && r.requiredForm))
                            {
                                Debug.WriteLine("* * *  SharedValidation:DoGenericValidation method  * * * Validation: skipping section item with requiredform=false (itm label = " + si.def_Items.label + ")");
                                continue;
                            }
                        }

                        // if no items are found, ICollection.Any() will return false, .All() will return true.
                        if (si.def_SectionItemsEnt.Any(sie => sie.EnterpriseID == enterpriseId && sie.validation == 0))
                        {
                            Debug.WriteLine("* * *  SharedValidation:DoGenericValidation method  * * * Validation: skipping section item where Enterprise specific validation is 0 (itm label = " + si.def_Items.label + ")");
                            continue;
                        }

                        var item = formsRepo.GetItemById(si.itemId);
                        formsRepo.GetItemVariables(item);
                        foreach (def_ItemVariables iv in item.def_ItemVariables)
                        {
                            result.Add(
                                new ItemVariableIdentifierWithPartSection
                                {
                                    partId = ps.partId,
                                    sectionId = ps.sectionId,
                                    itemVariableIdentifier = iv.identifier
                                }
                            );
                        }
                    }
                }
            }

            return result;
        }

        #region validation helpers for implimenting one-off validation rules

        /// <summary>
        /// Returns the response for a def_ItemVariable in the form of an integer
        /// 
        /// Defaults to -1 if no response is available, or if the response is empty/whitespace.
        /// 
        /// Throws exception if there is an existing response in the wrong format
        /// </summary>
        /// <param name="itemVariableIdentifier"></param>
        /// <returns>an integer, or -1 if no response</returns>
        public int GetResponseInt(string itemVariableIdentifier)
        {

            //def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, itemVariableIdentifier);
            ValuePair vp = allResponses.FirstOrDefault(vpt => vpt.identifier == itemVariableIdentifier);

            if ( vp == null || String.IsNullOrWhiteSpace( vp.rspValue ) )
            {
                return -1;
            }

            try
            {
                return Convert.ToInt32(vp.rspValue);
            }
            catch (Exception e)
            {
                throw new Exception("Expected integer response to item variable \"" + itemVariableIdentifier + "\", but found \"" + vp.rspValue + "\"");
            }
        }

        /// <summary>
        /// Check that hasAnyResponse(identifier) returns true for all the given identifiers
        /// </summary>
        /// <param name="itemVariableIdentifiers">def_ItemVariable identifiers to check</param>
        /// <returns></returns>
        public bool AllComplete(params string[] itemVariableIdentifiers)
        {
            foreach (string ident in itemVariableIdentifiers)
            {
                if (!HasAnyResponse(ident))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Check if any of the given identifiers returns true for hasPositiveResponse(identifier)
        /// </summary>
        /// <param name="itemVariableIdentifiers">def_ItemVariable identifiers to check</param>
        /// <returns></returns>
        public bool AnyPositiveResponses(params string[] itemVariableIdentifiers)
        {
            foreach (string ident in itemVariableIdentifiers)
            {
                if (HasPositiveResponse(ident))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// returns true if there is an existing, nuumerical, >0 response for the given def_ItemVariable
        /// </summary>
        /// <param name="itemVariableIdentifier"></param>
        /// <returns></returns>
        public bool HasPositiveResponse(string itemVariableIdentifier)
        {
            //def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, itemVariableIdentifier);
            ValuePair vp = allResponses.FirstOrDefault(vpt => vpt.identifier == itemVariableIdentifier);

            return vp != null && !String.IsNullOrWhiteSpace(vp.rspValue) && (vp.rspValue.ToLower() == "true" || ForceParseInt(vp.rspValue) > 0);
        }

        /// <summary>
        /// Attempt to parse an integer from a string, and default to 0 if there are any exceptions
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private long ForceParseInt(string s)
        {
            try
            {
                return Int64.Parse(s);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// Check if the response to a given def_ItemVariable matches the targetResponse
        /// </summary>
        /// <param name="itemVariableIdentifier">identifier of the def_ItemVariable in question</param>
        /// <param name="targetResponse">expected response</param>
        /// <returns>true if there is a response and it matches</returns>
        public bool HasExactResponse(string itemVariableIdentifier, string targetResponse)
        {
            //def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, itemVariableIdentifier);
            ValuePair vp = allResponses.FirstOrDefault(vpt => vpt.identifier == itemVariableIdentifier);
            return vp != null && vp.rspValue != null && vp.rspValue.Equals(targetResponse);
        }

        /// <summary>
        /// Check if the response to a given def_ItemVariable matches the targetPattern
        /// </summary>
        /// <param name="itemVariableIdentifier">identifier of the def_ItemVariable in question</param>
        /// <param name="targetPattern">expected response regex pattern</param>
        /// <returns>true if there is a response and it matches</returns>
        public bool HasPattern(string itemVariableIdentifier, string targetPattern)
        {
            //def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, itemVariableIdentifier);
            ValuePair vp = allResponses.FirstOrDefault(vpt => vpt.identifier == itemVariableIdentifier);
            return vp != null && vp.rspValue != null && System.Text.RegularExpressions.Regex.IsMatch(vp.rspValue, targetPattern);
        }

        /// <summary>
        /// Check if there is some non-null, non-empty, non-whitespace response for the given def_ItemVariable
        /// </summary>
        /// <param name="itemVariableIdentifier">identifier of the def_ItemVariable in question</param>
        /// <returns></returns>
        public bool HasAnyResponse(string itemVariableIdentifier)
        {
            //def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, itemVariableIdentifier);
            ValuePair vp = allResponses.FirstOrDefault(vpt => vpt.identifier == itemVariableIdentifier);
            return vp != null && !String.IsNullOrWhiteSpace(vp.rspValue) && (vp.rspValue.Trim().Length > 0);
        }

        #endregion
    }
}