using AJBoggs.Def.Domain;
using Assmnts;
using Assmnts.Infrastructure;

using Data.Abstract;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace AJBoggs.Adap.Services.Xml
{
    public class RamsellImport
    {
        private readonly IFormsRepository formsRepo;
        private readonly int toFormResultId;
        private readonly UserData userData;

        public RamsellImport(IFormsRepository formsRepo, int toFormResultId)
        {
            this.formsRepo = formsRepo;
            this.userData = new UserData(formsRepo);
            this.toFormResultId = toFormResultId;
        }

        /// <summary>
        /// Get the application data from Ramsell in their XML format.
        /// Nearly identical to the XML format that is sent to Ramsell
        /// </summary>
        /// <param name="oauthToken">Oauth 2 security token</param>
        /// <param name="fromMemberId">Ramsell MemberId</param>
        /// <returns></returns>
        public int ImportApplication(string oauthToken, string fromMemberId)
        {
            string decodedXml = Services.Api.Ramsell.GetApplicationXml(oauthToken, fromMemberId);
            if( String.IsNullOrWhiteSpace( decodedXml ) )
                throw new Exception( "received empty response from Ramsell for memberId \"" + fromMemberId + "\"" );

            // Process the XML returned from Ramsell
            List<string> checkedBoxes = new List<string>();
            Dictionary<string,string> specialCaseValuesByTagname = new Dictionary<string,string>();
            using (System.Xml.XmlReader reader = XmlReader.Create(new StringReader(decodedXml)))
            {
                //iterate through each element node in the file
                while (reader.Read())
                {
                    //tags "string" and "Patient" are boilerplate, they can be ignored (this doesn't skip their children)
                    if ( (reader.NodeType != XmlNodeType.Element) || (reader.Name == "string") || (reader.Name == "Patient") ) 
                        continue;

                    //update DEF responses based on tagname + contents
                    ImportXmlNodeFromRamsell(reader, checkedBoxes, specialCaseValuesByTagname);
                }
            }

            //for any checkboxes that are included in the "ramsellIdentifierMap" in RamsellExport.cs,
            //uncheck the ones that weren't included in the xml
            List<string> checkboxIvIdentifiers = GetGroupedCheckboxItemVarIdentifiers();
            foreach (string checkboxIvIdent in checkboxIvIdentifiers)
            {
                if (checkedBoxes.Contains(checkboxIvIdent))
                    continue;
                def_ItemVariables checkBoxIv = formsRepo.GetItemVariableByIdentifier(checkboxIvIdent);
                def_ItemResults ir = userData.SaveItemResult(toFormResultId, checkBoxIv.itemId);
                userData.SaveResponseVariable(ir, checkBoxIv, "0");
            }

            //handle any special-case ramsell tags that require on-off transformations
            RamsellTransformations.ImportSpecialCasesNoSave(specialCaseValuesByTagname, toFormResultId, formsRepo);

            formsRepo.Save();

            return 0;
        }

        /// <summary>
        /// Get a list of itemvariable identifiers corresponding with grouped checkboxes
        /// These checkboxes will need to be unchecked at the end of import if they were not present in the Ramsell xml
        /// </summary>
        /// <returns></returns>
        private List<string> GetGroupedCheckboxItemVarIdentifiers()
        {
            int nRows = RamsellExport.ramsellIdentifierMap.Length / 2;

            Dictionary<string, int> ramsellTagOccurenceCount = new Dictionary<string, int>();
            for (int row = 0; row < nRows; row++)
            {
                string ramsellTag = RamsellExport.ramsellIdentifierMap[row, 0];
                if (!ramsellTagOccurenceCount.ContainsKey(ramsellTag))
                    ramsellTagOccurenceCount.Add(ramsellTag, 0);
                ramsellTagOccurenceCount[ramsellTag]++;
            }

            List<string> result = new List<string>();
            for (int row = 0; row < nRows; row++)
            {
                string ramsellTag = RamsellExport.ramsellIdentifierMap[row, 0];
                string ivIdent = RamsellExport.ramsellIdentifierMap[row, 1];
                if (ramsellTagOccurenceCount[ramsellTag] > 1)
                    result.Add(ivIdent);
            }
            
            return result;
        }

        /// <summary>
        /// Process a single node from an XML improted from Ramsell, modifying the specified formResult as applicable.
        /// Noramlly this will only be called from within a loop to iterate over xml nodes, however this function will recurse to handle Ramsell's "income" structures
        /// </summary>
        /// <param name="fromReader">XmlReader where Read() has already been called</param>
        /// <param name="checkedBoxes">this should ba appended to with itemVariable identifiers for each checked checkbox. Used to uncheck excluded boxes at the end of the import</param>
        /// <param name="specialCaseValuesByTagname">this will be used to run special-case transformations that may involve multiple ramsell tags</param>
        private void ImportXmlNodeFromRamsell(
            XmlReader fromReader, 
            List< string > checkedBoxes,
            Dictionary<string, string> specialCaseValuesByTagname)
        {
            if (fromReader.NodeType != XmlNodeType.Element)
                throw new Exception("Expecting NodeType \"" + XmlNodeType.Element + "\", found \"" + fromReader.NodeType + "\"" );
            string ramellTagName = fromReader.Name;

            //the "Income_Item" tag is a one-off structure with multiple occurances
            if (ramellTagName == "Income_Item")
            {
                ImportIncomeStructureFromRamsell(fromReader);
                return;
            }

            //get the nodes contents
            string ramsellVal = String.Empty;
            if (!fromReader.IsEmptyElement)
            {
                fromReader.Read();
                if (fromReader.NodeType == XmlNodeType.EndElement)
                    return;
                if (fromReader.NodeType != XmlNodeType.Text)
                    throw new Exception("Inside of node \"" + ramellTagName + "\", found NodeType \"" + fromReader.NodeType
                        + "\", expecting NodeType \"" + XmlNodeType.Text + "\", or \"" + XmlNodeType.EndElement + "\"");
                ramsellVal = fromReader.Value;
            }


            //based on tagName, check if this a simple case (no transformation necessary)
            List<string> ivIdentifiers = RamsellExport.GetItemVariableIdentifiersForRamsellTagName(ramellTagName);
            if (ivIdentifiers.Count == 1)
            {
                //one-to-one case: this ramsell tagName corresponds with exactly one itemVariable
                //so just save the text contents as a response to that itemVariable

                // RRB 4/18/16 Ramsell seems to be sending a default date of 1900-01-01
                //              This should be tested more.  If doesn't fix, maybe our date converter is causing an empty DOB to be this date.
                if ( ramellTagName.Equals("DOB")  &&  (string.IsNullOrEmpty(ramsellVal) || ramsellVal.StartsWith("1900") ) )
                    ;
                else
                {
                    UpdateResponse(ivIdentifiers[0], ramsellVal);
                }

            }
            else if (ivIdentifiers.Count > 1)
            {
                //checkbox case: this ramsell tagName corresponds to a set of itemVariables (representing checkboxes in the application)
                //so pick the checkbox based on this node's text contents, and save the response "1" for that checkbox
                #region checkbox case

                //based on lookups, pick the inidividual itemvariable (matchIv) that matches the node contents (ramsellVal)
                def_ItemVariables matchIv = null;
                foreach (string ivIdent in ivIdentifiers)
                {
                    def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(ivIdent);
                    if (iv.baseTypeId == 1)
                    {
                        def_LookupMaster lm = formsRepo.GetLookupMastersByLookupCode(iv.identifier);
                        if (lm != null)
                        {
                            List<def_LookupDetail> ld = formsRepo.GetLookupDetailsByLookupMasterEnterprise(lm.lookupMasterId, SessionHelper.LoginStatus.EnterpriseID);
                            if (ld.Where(ldt => ramsellVal == ldt.dataValue).Any())
                            {
                                matchIv = iv;
                                break;
                            }
                        }
                    }
                }

                //save the respones "1" to the single matched itemVariable, and add it to the "checkedBoxes" list
                //at the end of the import, any grouped checkboxes that haven't been added to that list will be unchecked
                if (matchIv == null)
                    Debug.WriteLine("* * * RamsellImport: Could not find matching itemVariable for ramsell tag/value \"" + ramellTagName + "/" + ramsellVal + "\", skipping...");
                else
                {
                    def_ItemResults ir = userData.SaveItemResult(toFormResultId, matchIv.itemId);
                    userData.SaveResponseVariable(ir, matchIv, "1");
                    checkedBoxes.Add(matchIv.identifier);
                }
                #endregion

            }
            else
            {
                //this tagname must be either ignorable or handled by a special one-off transformation, 
                //so just record the tagname/value pair to be handled later on.
                //the special-cases can involve multiple ramsell tags so there is no way to handle them one tag at a time.
                specialCaseValuesByTagname.Add(ramellTagName, ramsellVal);
            }
        }

        private int importedIncomeCount = 0; //count is used to determine which income subsection to update on each import

        private void ImportIncomeStructureFromRamsell(XmlReader fromReader)
        {

            //ADAP_F3_[A-D]_IncomeTypeDrop options

            //0 = Employment
            //1 = Unemployment benefits
            //2 = Short-/Long-term disability
            //3 = SSI (supplemental Security Income)
            //4 = Worker's compensation
            //5 = SSDI (Supplemental Security Disability Insurance)
            //6 = AND (Aid to the Needy Disabled)
            //7 = TANF (Temporary Aid to Needy Families)
            //8 = Interest/Investment Income
            //9 = Veterans benefits
            //10 = Retirement/Pension
            //11 = Taxable trust income
            //12 = Alimony paid to you
            //13 = Other (please describe below)


            string subsectionLetter = "ABCD".Substring(importedIncomeCount, 1);
            string subsectionName = "ADAP_F3_" + subsectionLetter;

            Debug.WriteLine("started ImportIncomeStructureFromRamsell");
            
            //iterate through each element node within this Income_Item structure
            string currentChildName = null;
            while (fromReader.Read())
            {
                if ( fromReader.NodeType == XmlNodeType.Element)
                {
                    if (currentChildName != null)
                        throw new Exception("ImportIncomeStructureFromRamsell: found unexpected child node of income child \"" + currentChildName + "\"");
                    currentChildName = fromReader.Name;
                }
                    
                if ( fromReader.NodeType == XmlNodeType.EndElement )
                {
                    if( currentChildName == null )
                        break;
                    currentChildName = null;
                }
                    
                if ( fromReader.NodeType == XmlNodeType.Text )
                {
                    if( currentChildName == null )
                        throw new Exception("ImportIncomeStructureFromRamsell: found unexpected text node \"" + fromReader.Value + "\" at the root of an Income_Item");
                    string currentChildValue = fromReader.Value;

                    switch (currentChildName)
                    {
                        case "Income_Earner_Name":
                            UpdateResponse(subsectionName + "_Recipient", currentChildValue);
                            break;

                        case "Salary_Wages":
                            UpdateResponse(subsectionName + "_IncomeTypeDrop", "0");
                            break;

                        case "Investments_Trust":
                            UpdateResponse(subsectionName + "_IncomeTypeDrop", "8");
                            break;

                        case "SSDI":
                            UpdateResponse(subsectionName + "_IncomeTypeDrop", "5");
                            break;

                        case "SSI":
                            UpdateResponse(subsectionName + "_IncomeTypeDrop", "3");
                            break;

                        case "AND":
                            UpdateResponse(subsectionName + "_IncomeTypeDrop", "6");
                            break;

                        case "Workers_Comp":
                            UpdateResponse(subsectionName + "_IncomeTypeDrop", "4");
                            break;

                        case "Child_Support_Alimony":
                            UpdateResponse(subsectionName + "_IncomeTypeDrop", "12");
                            break;

                        case "Retirement":
                            UpdateResponse(subsectionName + "_IncomeTypeDrop", "10");
                            break;

                        case "VA_Income":
                            UpdateResponse(subsectionName + "_IncomeTypeDrop", "9");
                            break;

                        case "Unemployment":
                            UpdateResponse(subsectionName + "_IncomeTypeDrop", "1");
                            break;

                        case "Other":
                            UpdateResponse(subsectionName + "_IncomeTypeDrop", "13");
                            break;

                        case "Other_Description":
                            UpdateResponse(subsectionName + "_IncomeTypeOther", currentChildValue);
                            break;
                    }
                }
            }


            Debug.WriteLine("finished ImportIncomeStructureFromRamsell");
            importedIncomeCount++;
        }

        private void UpdateResponse(string itemVariableIdentifier, string response)
        {
            def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(itemVariableIdentifier);
            if (iv == null)
                throw new Exception("Cannot find itemVariable with identifier \"" + itemVariableIdentifier + "\"");
            def_ItemResults ir = userData.SaveItemResult(toFormResultId, iv.itemId);
            userData.SaveResponseVariable(ir, iv, response);
        }
    }
}