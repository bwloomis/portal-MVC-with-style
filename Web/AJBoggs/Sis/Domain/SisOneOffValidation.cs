
using AJBoggs.Def.Services;

using Assmnts;
using Assmnts.Models;

using Data.Abstract;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;



namespace AJBoggs.Sis.Domain
{

    public class SisOneOffValidation
    {

        /// <summary>
        /// Used by ResultsController.ValidateFormResult()
        /// 
        /// Runs the hard-coded validation rules, which can't be encoded into meta-data, for SIS-A and SIS-C forms.
        /// 
        /// adds to the model's validation messages for any validation errors or warnings.
        /// </summary>
        /// <param name="sv"></param>
        /// <param name="model"></param>
        /// <returns>true if any validation ERRORS where found, false if nothin (or only wanrings) found</returns>
        public static bool RunOneOffValidation( IFormsRepository formsRepo, def_Forms frm, int entId, List<ValuePair> allResponses, TemplateItems model ) {

            bool invalid = false;
            bool isSisCAssessment = frm.identifier.Equals("SIS-C");

            #region if any section 1 "other" text fields are populated, make sure they have a positive numerical response. (and vice-versa)
            string itemsToCheck = isSisCAssessment ?
                "A19,A20,A21,B14,B15,B16" // <- check these section 1 items if SIS-C
                : "A19,B13";              // <- check these if SIS-A
            foreach (string shortIdentifier in itemsToCheck.Split(','))
            {
                string textIdentifier;
                string numIdentifier;

                if (isSisCAssessment)
                {
                    textIdentifier = "SIS-C_Q1" + shortIdentifier + "_Other";
                    numIdentifier = "SIS-C_Q1" + shortIdentifier + "_Ex" + (shortIdentifier.StartsWith("A") ? "Med" : "Beh") + "Support";
                }
                else // Assumes that if it isn't SIS-C,it must be a SIS-A assessment
                {
                    textIdentifier = "Q1" + shortIdentifier + "_Other";
                    numIdentifier = "Q1" + shortIdentifier + "_Ex" + (shortIdentifier.StartsWith("A") ? "Med" : "Beh") + "Support";
                }

                ValuePair textVp = allResponses.SingleOrDefault(vpt => vpt.identifier == textIdentifier);//formsRepo.GetResponseVariablesByFormResultIdentifier(fr.formResultId, textIdentifier);
                ValuePair numVp = allResponses.SingleOrDefault(vpt => vpt.identifier == numIdentifier);//def_ResponseVariables numRv = formsRepo.GetResponseVariablesByFormResultIdentifier(fr.formResultId, numIdentifier);

                bool textBlank = textVp == null || String.IsNullOrWhiteSpace(textVp.rspValue);//isNullEmptyOrBlank(textRv);
                bool numBlank = numVp == null || String.IsNullOrWhiteSpace(numVp.rspValue);//isNullEmptyOrBlank(numRv);

                if (numBlank)
                {
                    invalid = true;
                    model.validationMessages.Add("Section 1: \"other\" item " + shortIdentifier + " has no numerical response");
                }
                else if ((Int32.Parse(numVp.rspValue) > 0) && textBlank)
                {
                    invalid = true;
                    model.validationMessages.Add("Section 1: \"other\" item " + shortIdentifier + " has positive numerical response, but no text explaination");
                }
            }
            #endregion

            #region check interviewee age / interview date
            ValuePair vpBirthDate = allResponses.SingleOrDefault(vpt => vpt.identifier == "sis_cl_dob_dt");
            ValuePair vpIntDate = allResponses.SingleOrDefault(vpt => vpt.identifier == "sis_completed_dt");
            DateTime dtDob;
            DateTime dtInt;
            //def_ResponseVariables rvBirthDate = formsRepo.GetResponseVariablesByFormResultIdentifier(fr.formResultId, "sis_cl_dob_dt");
            //def_ResponseVariables rvIntDate = formsRepo.GetResponseVariablesByFormResultIdentifier(fr.formResultId, "sis_completed_dt");
            if (vpBirthDate != null && DateTime.TryParse(vpBirthDate.rspValue, out dtDob)
                && vpIntDate != null && DateTime.TryParse(vpIntDate.rspValue, out dtInt))
            {
                int age = dtInt.Year - dtDob.Year;
                if (dtInt.Month < dtDob.Month)
                    age--;
                if ((dtInt.Month == dtDob.Month) && (dtInt.Day < dtDob.Day))
                    age--;

                //for SIS-C assessments, age must fall between 5 and 16 inclusive, and interview date must be 2009 at earliest
                if (isSisCAssessment)
                {
                    if (age < 5)
                    {
                        invalid = true;
                        model.validationMessages.Add("Profile: interviewee age is " + age + " years, should be at least 5 years");
                    }
                    if (age > 16)
                    {
                        invalid = true;
                        model.validationMessages.Add("Profile: interviewee age is " + age + " years, should be at most 16 years");
                    }
                    if (dtInt.Year < 2009)
                    {
                        invalid = true;
                        model.validationMessages.Add("Profile: interview date is in the year " + dtInt.Year + ", should be 2009 at the earliest");
                    }
                }

                //for SIS-A assessments, age must be at least 15, and interview date must be 2004 at earliest
                else
                {
                    if (dtInt.Year < 2004)
                    {
                        invalid = true;
                        model.validationMessages.Add("Profile: interview date is in the year " + dtInt.Year + ", should be 2004 at the earliest");
                    }
                    if (age < 15)
                    {
                        invalid = true;
                        model.validationMessages.Add("Profile: interviewee age is " + age + " years, should be at least 15 years");
                    }
                }
            }
            #endregion

            #region if "SEVERE MEDICAL RISK" under supplementl questions is marked "yes"...

            ValuePair vpSup = allResponses.SingleOrDefault(vpt => vpt.identifier == "Q4A1v1");
            //def_ResponseVariables rvSup = formsRepo.GetResponseVariablesByFormResultIdentifier(fr.formResultId, "Q4A1v1");
            if (vpSup != null && vpSup.rspValue != null && (vpSup.rspValue.Equals("1") || vpSup.rspValue.ToLower().Equals("true")))
            {
                int rspInt;

                //make sure at least one of the items under section 1A has a rsponse value "2"
                bool anyTwosInOtherItems = false;
                ValuePair vp;

                for (int i = 1; i <= 21; i++)
                {
                    if (isSisCAssessment) // Accomodating for different identifiers (SIS-A vs SIS-C)
                    {
                        vp = allResponses.SingleOrDefault(vpt => vpt.identifier == "SIS-C_Q1A" + i + "_ExMedSupport");
                    }
                    else
                    {
                        vp = allResponses.SingleOrDefault(vpt => vpt.identifier == "Q1A" + i + "_ExMedSupport");
                    }
                    //def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(fr.formResultId, "Q1A" + i + "_ExMedSupport");
                    if ((vp != null) && Int32.TryParse(vp.rspValue, out rspInt) && (rspInt == 2))
                    {
                        anyTwosInOtherItems = true;
                        break;
                    }
                }

                ValuePair vpExMedSup = allResponses.SingleOrDefault(vpt => vpt.identifier == "Q1A_ed_ExMedSupport");
                //def_ResponseVariables edRv = formsRepo.GetResponseVariablesByFormResultIdentifier(fr.formResultId, "Q1A_ed_ExMedSupport");

                if ((vpExMedSup != null) && Int32.TryParse(vpExMedSup.rspValue, out rspInt) && (rspInt == 2))
                {
                    anyTwosInOtherItems = true;
                }

                if (!anyTwosInOtherItems)
                {
                    invalid = true;
                    model.validationMessages.Add("Marked \"yes\" under SEVERE MEDICAL RISK in supplemental questions, but no scores of 2 in section 1A");
                }
            }
            #endregion

            #region Custom validation for supplemental questions 2-4, depending on section 1B responses
            Dictionary<string, string[]> specs;
            if (isSisCAssessment)
            {
                specs = new Dictionary<string, string[]>
                {
                    { "Q4A2v1", new string[]{
                        //if Q4A2v1 has response "yes", at least one 
                        //of these item variables must have response "2"
                        "SIS-C_Q1B2", //question 2 in SIS-C
                        "SIS-C_Q1B3", //question 3 in SIS-C
                        "SIS-C_Q1B8", //question 8 in SIS-C
                    }},
                    { "Q4A3v1", new string[]{
                        "SIS-C_Q1B2", //question 2 in SIS-C
                        "SIS-C_Q1B3", //question 3 in SIS-C
                        "SIS-C_Q1B8", //question 8 in SIS-C
                    }},
                    { "Q4A4v1", new string[]{
                        "SIS-C_Q1B5", //question 5 in SIS-C
                        "SIS-C_Q1B6", //question 6 in SIS-C
                        "SIS-C_Q1B7", //question 7 in SIS-C
                    }},
                };
            }
            else
            {
                specs = new Dictionary<string, string[]>
                {
                    { "Q4A2v1", new string[]{
                        //if Q4A2v1 has response "yes", at least one 
                        //of these item variables must have response "2"
                        "Q1B2", //question 2 in SIS-C
                        "Q1B3", //question 3 in SIS-C
                        "Q1B9", //question 8 in SIS-C
                    }},
                    { "Q4A3v1", new string[]{
                        "Q1B2", //question 2 in SIS-C
                        "Q1B3", //question 3 in SIS-C
                        "Q1B9", //question 8 in SIS-C
                    }},
                    { "Q4A4v1", new string[]{
                        "Q1B5", //question 5 in SIS-C
                        "Q1B6", //question 6 in SIS-C
                        "Q1B7", //question 7 in SIS-C
                    }},
                };
            }
            foreach (string supIvIdent in specs.Keys)
            {
                def_ItemVariables supIv = formsRepo.GetItemVariableByIdentifier(supIvIdent);
                if (supIv == null)
                    throw new Exception("Could not find item variable with identifier \"" + supIvIdent + "\"");
                def_Items supItm = formsRepo.GetItemById(supIv.itemId);
                string supIndex = supIvIdent.Substring(3, 1);

                //rvSup = formsRepo.GetResponseVariablesByFormResultItemVarId(fr.formResultId, supIv.itemVariableId);
                vpSup = allResponses.SingleOrDefault(vpt => vpt.identifier == supIv.identifier);

                if (vpSup != null && vpSup.rspValue != null && (vpSup.rspValue.Equals("1") || vpSup.rspValue.ToLower().Equals("true")))
                {
                    List<string> checkedItemLabels = new List<string>();
                    bool anyTwosInOtherItems = false;
                    foreach (string ivPrefix in specs[supIvIdent])
                    {
                        def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(ivPrefix + "_ExBehSupport");
                        def_Items itm = formsRepo.GetItemById(iv.itemId);
                        checkedItemLabels.Add("\"" + itm.label + "\"");

                        ValuePair vp = allResponses.SingleOrDefault(vpt => vpt.identifier == iv.identifier);
                        //def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(fr.formResultId, iv.itemVariableId);
                        if ((vp != null) && (vp.rspValue != null) && (vp.rspValue == "2"))
                        {
                            anyTwosInOtherItems = true;
                            break;
                        }
                    }

                    if (!anyTwosInOtherItems)
                    {
                        invalid = true;
                        model.validationMessages.Add("Marked \"yes\" under supplemental question " + supIndex + ", but no scores of 2 in any of these section 1B items: "
                            + String.Join(", ", checkedItemLabels));
                    }
                }
            }
            #endregion

            #region Require notes for Section 1A or 1B where the item is flagged as Important For/To
            foreach (def_FormParts fp in frm.def_FormParts)
            {
                //This is currently hardcodoed for Enterprise 44: Virginia.  This should probably be added to a lookup Table so the feature can be switched on/off for any enterprise.
                //if (fr.EnterpriseID == 44)
                //{
                if (fp.def_Parts.identifier.StartsWith("Section 1"))
                {
                    foreach (def_PartSections ps in fp.def_Parts.def_PartSections)
                    {
                        foreach (def_SectionItems si in ps.def_Sections.def_SectionItems)
                        {
                            // Configured in def_SectionItemsEnt, some Enterprises require validation on these sections.  Currently checks for a 1 for Enterprise 44.
                            // Empty lists should return false.
                            if (si.def_SectionItemsEnt.Any(r => r.validation == 1 && r.EnterpriseID == entId) && si.subSectionId != null)
                            {
                                foreach (def_SectionItems subsi in si.def_SubSections.def_Sections.def_SectionItems)
                                {
                                    List<def_ItemVariables> ivImportant = subsi.def_Items.def_ItemVariables.Where(iv => iv.identifier.EndsWith("_ImportantFor") || iv.identifier.EndsWith("_ImportantTo")).ToList();
                                    if (ivImportant.Count() == 0)
                                    {
                                        Debug.WriteLine("ResultsController:ValidateFormResult method  * * * Could not find Important For/To itemVariable for item \"" + subsi.def_Items.identifier + "\"");
                                    }
                                    else
                                    {
                                        List<bool> rvImpBools = new List<bool>();
                                        foreach (def_ItemVariables iv in ivImportant)
                                        {
                                            ValuePair vp = allResponses.SingleOrDefault(vpt => vpt.identifier == iv.identifier);
                                            //def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(fr.formResultId, iv.itemVariableId);
                                            rvImpBools.Add((vp != null && vp.rspValue != null && vp.rspValue.Equals("1")));
                                        }
                                        // Test true if any value is true.  Empty lists should return false.
                                        if (rvImpBools.Any(r => r))
                                        {
                                            List<def_ItemVariables> ivNote = subsi.def_Items.def_ItemVariables.Where(iv => iv.identifier.EndsWith("_Notes")).ToList();
                                            foreach (def_ItemVariables iv in ivNote)
                                            {
                                                ValuePair vp = allResponses.SingleOrDefault(vpt => vpt.identifier == iv.identifier);
                                                //def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(fr.formResultId, iv.itemVariableId);
                                                if (vp == null || String.IsNullOrEmpty(vp.rspValue))
                                                {
                                                    invalid = true;
                                                    model.validationMessages.Add(subsi.def_Sections.title + ", " + subsi.def_Items.label +
                                                        ": Notes are required for items flagged as Important.");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //}
            }
            #endregion

            #region For all supplemental questions, require sub-questions if top-level response is "Yes" (Bug 13132, #2 in description)

            //iterate through all the sub-sections in the Supplemental Questions section
            def_Sections sqSection = formsRepo.GetSectionByIdentifier("SQ");
            formsRepo.SortSectionItems(sqSection);
            foreach (def_SectionItems si in sqSection.def_SectionItems.Where(si => si.subSectionId != null))
            {
                bool validationRequired = false;
                def_Sections subSct = formsRepo.GetSubSectionById(si.subSectionId);

                //exclude "S4aPageNotes" subsection
                if (subSct.identifier == "S4aPageNotes")
                    continue;

                //iterate through all the sectionitems in this subsection, in order
                formsRepo.SortSectionItems(subSct);
                foreach (def_SectionItems subSi in subSct.def_SectionItems)
                {

                    //if the sectionitem points to an item, that item must represent a top-level yes.no Supplemental Question
                    //and it should have exactly one itemVariable, with basetype 1
                    if (subSi.itemId > 1)
                    {
                        def_ItemVariables ivTopQuestion = subSi.def_Items.def_ItemVariables.FirstOrDefault();

                        ValuePair vpTopQuestion = allResponses.SingleOrDefault(vpt => vpt.identifier == ivTopQuestion.identifier);
                        //def_ResponseVariables rvTopQuestion = formsRepo.GetResponseVariablesByFormResultItemVarId(fr.formResultId, ivTopQuestion.itemVariableId);

                        //check if the response is "yes" for this top-level yes/no
                        if (vpTopQuestion != null && !String.IsNullOrWhiteSpace(vpTopQuestion.rspValue) && vpTopQuestion.rspValue == "1")
                        {
                            validationRequired = true;
                        }
                    }

                    //if the sectionitem points to a sub-subsection, that sub-subsection contains lower-level items 
                    //that should be checked only if the previous top-level question was answered "yes"
                    else if (subSi.subSectionId.HasValue && validationRequired)
                    {
                        def_Sections subSubSct = formsRepo.GetSubSectionById(subSi.subSectionId);

                        //iterate through the item variables in this sub-subsection and require responses for some of them, 
                        //if their identifier ends with a, b, c, d, or d2
                        formsRepo.SortSectionItems(subSubSct);
                        foreach (def_SectionItems subSubSi in subSubSct.def_SectionItems.Where(subSubSi => subSubSi.subSectionId == null))
                        {
                            foreach (def_ItemVariables bottomLevelIv in subSubSi.def_Items.def_ItemVariables)
                            {
                                if ("a,b,c,d,d2".Split(',').Where(suffix => bottomLevelIv.identifier.EndsWith(suffix)).Any())
                                {

                                    ValuePair vp = allResponses.SingleOrDefault(vpt => vpt.identifier == bottomLevelIv.identifier);
                                    //def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(fr.formResultId, bottomLevelIv.itemVariableId);
                                    if (vp == null || String.IsNullOrWhiteSpace(vp.rspValue) ||
                                        (bottomLevelIv.baseTypeId == 1 && vp.rspValue.ToLower() == "unanswered"))
                                    //^ ^ ^ OT 3-24-16 consider "unanswered" the same as blank for yes/nos ("unanswered" responses may exist in DB as a result of a bug: Bug 13132, #1 in description)
                                    {
                                        invalid = true;
                                        model.validationMessages.Add(sqSection.title + ": since you marked \"yes\" under \"" + subSct.title + "\", you must provide a response for \"" + subSubSi.def_Items.label + "\"");
                                    }
                                }
                            }
                        }

                        //reset the flag so the following subsection is not validated unless we hit another top-level "yes"
                        validationRequired = false;
                    }
                }

            }

            #endregion
            
            return invalid;
        }

        public static bool RunSingleSectionOneOffValidation(IFormsRepository formsRepo, FormCollection frmCllctn, int sectionId, out List<string> validationErrorMessages )
        {
            bool invalid = false;
            validationErrorMessages = new List<string>();
            def_Sections sct = formsRepo.GetSectionById(sectionId);

            #region Sections 2,3: if the Frequency is greater than zero, then the Duration and Type of support must be greater than zero.
            if ( "SIS-A 2A,SIS-A 2B,SIS-A 2C,SIS-A 2D,SIS-A 2E,SIS-A 2F,SIS-A 3".Split(',').ToList().Contains( sct.identifier ) )
            {
                formsRepo.SortSectionItems(sct);
                foreach (def_SectionItems si in sct.def_SectionItems)
                {
                    if (si.subSectionId == null)
                    {
                        def_ItemVariables ivFreq = si.def_Items.def_ItemVariables.Where(iv => iv.identifier.EndsWith("_Fqy")).FirstOrDefault();
                        if (ivFreq == null)
                            throw new Exception("could not find fequency itemVariable (with suffix \"_Fqy\") for item \"" + si.def_Items.identifier + "\"");

                        string rspFreq = frmCllctn[ivFreq.identifier];
                        //def_ResponseVariables rvFreq = formsRepo.GetResponseVariablesByFormResultItemVarId(fr.formResultId, ivFreq.itemVariableId);

                        if (rspFreq != null && rspFreq.Trim().Length > 0 && Convert.ToInt16(rspFreq) > 0)
                        {
                            foreach (string suffix in new string[] { "_TOS", "_DST" })
                            {
                                def_ItemVariables ivSuff = si.def_Items.def_ItemVariables.Where(iv => iv.identifier.EndsWith(suffix)).FirstOrDefault();
                                if (ivSuff == null)
                                    throw new Exception("could not find itemVariable with suffix \"" + suffix + "\" for item \"" + si.def_Items.identifier + "\"");

                                string rspSuff = frmCllctn[ivSuff.identifier];
                                //def_ResponseVariables rvSuff = formsRepo.GetResponseVariablesByFormResultItemVarId(fr.formResultId, ivSuff.itemVariableId);
                                if (rspSuff == null || rspSuff.Trim().Length == 0 || Convert.ToInt16(rspSuff) == 0)
                                {
                                    invalid = true;
                                    validationErrorMessages.Add(si.def_Sections.title + ", " + si.def_Items.label +
                                        ": you entered a Frequency greater than zero, so Daily Support Time and Type of Support must also be greater than zero.");
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            return invalid;
        }

    }
}
