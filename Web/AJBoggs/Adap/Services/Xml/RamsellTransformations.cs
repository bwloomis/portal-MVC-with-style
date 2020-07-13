using AJBoggs.Def.Domain;
using Assmnts;
using Assmnts.Infrastructure;
using Data.Abstract;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Schema;

namespace AJBoggs.Adap.Services.Xml
{
    /// <summary>
    /// OT 3-14-16
    /// 
    /// This class will contain all the special-case tranformations for Ramsell imports and exports
    /// 
    /// Each one-off transformation will exist as an implimentation of the "Transformation" abstract class (below)
    ///  - each tranformation reads/effects an explicitly-defined set of itemVariables and Ramsell tags
    ///  - each transforamtion has export and import logic is in the same place
    ///  - import/export logic is as concise as possible, no xml/sql/formsRepo
    /// </summary>
    public static class RamsellTransformations
    {
        public abstract class Transformation
        {
            /// <summary>
            /// Implimentations should return the full set of potentially-relevent item variable identifiers
            /// 
            /// On import, will only be allowed to save responses for these item variables
            /// On export, will only be able to "see" responses to these item variables
            /// </summary>
            /// <returns></returns>
            abstract public string[] GetItemVariableIdentifiers();  
                
            /// <summary>
            /// Implimentations should return the full set of potentially-relevent ramsell tagnames
            /// 
            /// On import, will only be able to "see" these tags from the ramsell xml
            /// On export, will only be allowed to add tags with these names to the ramsell xml
            /// </summary>
            /// <returns></returns>
            abstract public string[] GetRamsellTagNames();

            /// <summary>
            /// Export function
            /// 
            /// implimentations should write values to the "out" object without adding or removing keys.
            /// "out" values will be converted into xml tags and appended the ramsell xml export
            /// </summary>
            /// <param name="inRspValsByItemVar">input: keys from GetRelatedItemVariableIdentifiers(), values from response data</param>
            /// <param name="outRamsellValsByTag">output: keys prepopulated from GetRamsellTagNames()</param>
            abstract public void GetExportValuesByRamsellTagName(
                Dictionary<string, string> inRspValsByItemVar,  
                Dictionary<string, string> outRamsellValsByTag);

            /// <summary>
            /// Import function
            /// 
            /// implimentations should write values to the "out" obect without adding or removing keys
            /// "out" values will be saved to DEF responseVariables
            /// </summary>
            /// <param name="inRamsellValsByTag">input: keys based on GetRamsellTagNames(), values from Ramsell xml</param>
            /// <param name="outRspValsByItmVar">output: keys prepopulated from GetRelatedItemVariableIdentifiers(), values may be prepopulated with existing responses</param>
            abstract public void GetImportValuesByItemVarIdentifier(
                Dictionary<string, string> inRamsellValsByTag, 
                Dictionary<string, string> outRspValsByItmVar);
        }

        #region transformation implimentations

        #region Ethnicity

        private class EthnicityTransformation : Transformation
        {
            override public string[] GetItemVariableIdentifiers()
            {
                return new string[] { "ADAP_D4_EthnicDrop" };
            }

            override public string[] GetRamsellTagNames()
            {
                return new string[] { "Ethnicity" };
            }

            override public void GetExportValuesByRamsellTagName(
                Dictionary<string, string> inRspValsByItemVar,   //input: keys from GetRelatedItemVariableIdentifiers(), values from response data
                Dictionary<string, string> outRamsellValsByTag) //output: keys prepopulated from GetRamsellTagNames()
            {
                if (inRspValsByItemVar["ADAP_D4_EthnicDrop"] == "1" )
                    outRamsellValsByTag["Ethnicity"] = "1";
                else
                    outRamsellValsByTag["Ethnicity"] = "2";
            }

            override public void GetImportValuesByItemVarIdentifier(
                Dictionary<string, string> inRamsellValsByTag,
                Dictionary<string, string> outRspValsByItmVar)
            {
                outRspValsByItmVar["ADAP_D4_EthnicDrop"] = inRamsellValsByTag["Ethnicity"];
            }
        }

        #endregion

        #region Gender/transgender
        private class GenderTransformation : Transformation
        {
            override public string[] GetItemVariableIdentifiers()
            {
                return new string[] { "ADAP_D8_CurrGenderDrop" };
            }

            override public string[] GetRamsellTagNames()
            {
                return new string[] { "Gender", "Transgender" };
            }

            override public void GetExportValuesByRamsellTagName(
                Dictionary<string, string> inRspValsByItemVar,   //input: keys from GetRelatedItemVariableIdentifiers(), values from response data
                Dictionary<string, string> outRamsellValsByTag) //output: keys prepopulated from GetRamsellTagNames()
            {
                switch (inRspValsByItemVar["ADAP_D8_CurrGenderDrop"])
                {
                    // just male or just female
                    case "1":
                        outRamsellValsByTag["Gender"] = "1";
                        return;
                    case "2":
                        outRamsellValsByTag["Gender"] = "2";
                        return;

                    // male-to-female
                    case "3":
                        outRamsellValsByTag["Gender"] = "3";
                        outRamsellValsByTag["Transgender"] = "1";
                        return;

                    // female-to-male
                    case "4":
                        outRamsellValsByTag["Gender"] = "3";
                        outRamsellValsByTag["Transgender"] = "2";
                        return;
                }
            }

            override public void GetImportValuesByItemVarIdentifier(
                Dictionary<string, string> inRamsellValsByTag,
                Dictionary<string, string> outRspValsByItmVar)
            {
                switch (inRamsellValsByTag["Gender"])
                {
                    //no gender data from ramsell, do nothing
                    case null:
                    case "":
                        return;

                    // just male or just female
                    case "1":
                        outRspValsByItmVar["ADAP_D8_CurrGenderDrop"] = "1";
                        return;
                    case "2":
                        outRspValsByItmVar["ADAP_D8_CurrGenderDrop"] = "2";
                        return;

                    case "3":
                        switch (inRamsellValsByTag["Transgender"])
                        {
                            // male-to-female
                            case "1":
                                outRspValsByItmVar["ADAP_D8_CurrGenderDrop"] = "2";
                                return;

                            // female-to-male
                            case "2":
                                outRspValsByItmVar["ADAP_D8_CurrGenderDrop"] = "2";
                                return;
                        }
                        return;

                    default: throw UnrecognizedRamsellValueException(inRamsellValsByTag["Gender"], "Gender");
                }
            }
        }
        #endregion

        #region phones
        private class PhoneTransformation : Transformation
        {
            override public string[] GetItemVariableIdentifiers()
            {
                return new string[] { "ADAP_C3_Phone1_Num", "ADAP_C3_Phone2_Num", "ADAP_C3_Phone1_Type", "ADAP_C3_Phone2_Type", "ADAP_C4_Phone" };
            }

            override public string[] GetRamsellTagNames()
            {
                return new string[] { "Phone", "Cell_Phone", "Patient_Access_Phone" };
            }

            private string fixPhoneNumberFormat(string phone) {
                if (phone == null)
                    return null;

                //output format must be 123-123-1234

                //remove any spaces from the entered phone number
                phone = phone.Replace(" ", "");

                //if the phone number is entered as (123)123-1234, convert to correct format
                phone = phone.Replace("(", "").Replace(")", "-");

                //if the phone number is entered as 1231231234, add the dashes automatically
                if (Regex.Match(phone, @"^\d{10}$").Success)
                {
                    phone = phone.Substring(0, 3)
                        + "-" + phone.Substring(3, 3)
                        + "-" + phone.Substring(6, 4);
                }

                //final validation check. If this fails, ommit this phone number from the xml
                if (!Regex.Match(phone, @"^\d{3}-\d{3}-\d{4}$").Success)
                {
                    return null;
                }

                return phone;
            }

            override public void GetExportValuesByRamsellTagName(
                Dictionary<string, string> inRspValsByItemVar,
                Dictionary<string, string> outRamsellValsByTag)
            {
                if (inRspValsByItemVar["ADAP_C3_Phone1_Type"] == "0") //if phone 1 is home phone
                {
                    outRamsellValsByTag["Phone"] = inRspValsByItemVar["ADAP_C3_Phone1_Num"];
                }
                else if (inRspValsByItemVar["ADAP_C3_Phone2_Type"] == "0") //if phone 2 is home phone
                {
                    outRamsellValsByTag["Phone"] = inRspValsByItemVar["ADAP_C3_Phone2_Num"];
                }

                if (inRspValsByItemVar["ADAP_C3_Phone1_Type"] == "1") //if phone 1 is cell phone
                {
                    outRamsellValsByTag["Cell_Phone"] = inRspValsByItemVar["ADAP_C3_Phone1_Num"];
                }
                else if (inRspValsByItemVar["ADAP_C3_Phone2_Type"] == "1") //if phone 2 is cell phone
                {
                    outRamsellValsByTag["Cell_Phone"] = inRspValsByItemVar["ADAP_C3_Phone2_Num"];
                }

                outRamsellValsByTag["Phone"] = fixPhoneNumberFormat(outRamsellValsByTag["Phone"]);
                outRamsellValsByTag["Cell_Phone"] = fixPhoneNumberFormat(outRamsellValsByTag["Cell_Phone"]);
                outRamsellValsByTag["Patient_Access_Phone"] = fixPhoneNumberFormat(inRspValsByItemVar["ADAP_C4_Phone"]);
            }

            override public void GetImportValuesByItemVarIdentifier(
                Dictionary<string, string> inRamsellValsByTag,
                Dictionary<string, string> outRspValsByItmVar)
            {
                string homePhone = inRamsellValsByTag["Phone"];
                string cellPhone = inRamsellValsByTag["Cell_Phone"];

                if (!String.IsNullOrWhiteSpace(homePhone))
                {
                    outRspValsByItmVar["ADAP_C3_Phone1_Num"] = homePhone;
                    outRspValsByItmVar["ADAP_C3_Phone1_Type"] = "0";
                }

                if (!String.IsNullOrWhiteSpace(cellPhone))
                {
                    outRspValsByItmVar["ADAP_C3_Phone2_Num"] = cellPhone;
                    outRspValsByItmVar["ADAP_C3_Phone2_Type"] = "1";
                }

                outRspValsByItmVar["ADAP_C4_Phone"] = inRamsellValsByTag["Patient_Access_Phone"];
            }
        }
        #endregion

        #region Patient_Access names
        private class PatientAccessTransformation : Transformation
        {
            override public string[] GetItemVariableIdentifiers()
            {
                return new string[] { "ADAP_C4_Name" };
            }

            override public string[] GetRamsellTagNames()
            {
                return new string[] { "Patient_Access_First_Name", "Patient_Access_Last_Name" };
            }

            override public void GetExportValuesByRamsellTagName(
                Dictionary<string, string> inRspValsByItemVar,
                Dictionary<string, string> outRamsellValsByTag)
            {
                string result = inRspValsByItemVar["ADAP_C4_Name"];
                if (String.IsNullOrWhiteSpace(result))
                    return;
                result = result.Trim();
                int spaceIndex = result.IndexOf(" ");

                //if there are no spaces in the ADAP_C4_Name response, 
                //assume it is just a first name and do not include Patient_Access_Last_Name in the output
                if (spaceIndex == -1)
                {
                    outRamsellValsByTag["Patient_Access_First_Name"] = result;
                }

                //if there are ANY spaces in the ADAP_C4_Name response, assume the first space separates first and last names
                else if (spaceIndex > 0)
                {
                    outRamsellValsByTag["Patient_Access_First_Name"] = result.Substring(0, spaceIndex).Trim();
                    outRamsellValsByTag["Patient_Access_Last_Name"] = result.Substring(spaceIndex + 1).Trim();
                }

            }

            override public void GetImportValuesByItemVarIdentifier(
                Dictionary<string, string> inRamsellValsByTag,
                Dictionary<string, string> outRspValsByItmVar)
            {
                string firstName = inRamsellValsByTag["Patient_Access_First_Name"];
                string lastName = inRamsellValsByTag["Patient_Access_Last_Name"];

                if (String.IsNullOrWhiteSpace(firstName))
                    firstName = "";
                if (String.IsNullOrWhiteSpace(lastName))
                    lastName = "";

                string result = firstName.Trim() + " " + lastName.Trim();
                outRspValsByItmVar["ADAP_C4_Name"] = result;
            }
        }
        #endregion

        #region Employment Status
        //CO-ADAP 
        //[1]Unemployed for more than 60 days;
        //[2]Recently unemployed (less than 60 days);
        //[3]Retired/Disabled;
        //[4]Self-employed;
        //[5]Employed, working MORE than 32 hours a week;
        //[6]Employed, working LESS than 32 hours a week;
        //[7]Other (please specify below)

        //Ramsell
        //0 = Unknown
        //1 = Full Time
        //2 = Part Time
        //3 = Seasonal/Temporary
        //4 = Self Employed
        //5 = Retired
        //6 = Disabled
        //8 = Unemployed
        private class EmploymentTransformation : Transformation
        {
            override public string[] GetItemVariableIdentifiers()
            {
                return new string[] { "ADAP_F1_EmployOpt" };
            }

            override public string[] GetRamsellTagNames()
            {
                return new string[] { "Employment_Status" };
            }

            override public void GetExportValuesByRamsellTagName(
                Dictionary<string, string> inRspValsByItemVar,
                Dictionary<string, string> outRamsellValsByTag)
            {
                string defEmployOpt = inRspValsByItemVar["ADAP_F1_EmployOpt"];
                string result = null;
                switch (defEmployOpt)
                {
                    case "1":
                    case "2":
                        result = "8"; break;
                    case "3":
                        result = "5"; break;
                    case "4":
                        result = "4"; break;
                    case "5":
                        result = "1"; break;
                    case "6":
                        result = "2"; break;
                    default:
                        result = "0"; break;
                }
                outRamsellValsByTag["Employment_Status"] = result;
            }

            override public void GetImportValuesByItemVarIdentifier(
                Dictionary<string, string> inRamsellValsByTag,
                Dictionary<string, string> outRspValsByItmVar)
            {
                string ramsellEmpStatus = inRamsellValsByTag["Employment_Status"];
                string result = null;
                switch (ramsellEmpStatus)
                {
                    case "1":
                        result = "5"; break;
                    case "2":
                        result = "6"; break;
                    case "4":
                        result = "4"; break;
                    case "5":
                        result = "3"; break;
                    case "8":
                        result = "1"; break;
                    default:
                        result = "7"; break;

                }
                outRspValsByItmVar["ADAP_F1_EmployOpt"] = result;
            }
        }
        #endregion

        #region Housing

        //CO-ADAP 
        //1 = Permanent: I own, rent or share my current residence
        //2 = Temporary: I am in a temporary housing situation (homeless, halfway house, shelter, staying with friends)
        //3 = I am in an institution (hospice, nursing home, etc.)
        //4 = Unknown / refuse to answer

        private class HousingTransformation : Transformation
        {
            override public string[] GetItemVariableIdentifiers()
            {
                return new string[] { "ADAP_H1_StatusDrop" };
            }

            override public string[] GetRamsellTagNames()
            {
                return new string[] { "Homeless", "Permanent_Housing", "Temporary_Housing", "Unknown_Living_Situation" };
            }

            override public void GetExportValuesByRamsellTagName(
                Dictionary<string, string> inRspValsByItemVar,
                Dictionary<string, string> outRamsellValsByTag)
            {
                string coadapHousing = inRspValsByItemVar["ADAP_H1_StatusDrop"];

                switch (coadapHousing)
                {
                    case "1":
                        outRamsellValsByTag["Permanent_Housing"] = "1";
                        break;
                    case "2":
                        outRamsellValsByTag["Homeless"] = "1";
                        break;
                    case "3":
                        outRamsellValsByTag["Temporary_Housing"] = "1";
                        break;
                    case "4":
                        outRamsellValsByTag["Unknown_Living_Situation"] = "1";
                        break;
                }
            }

            override public void GetImportValuesByItemVarIdentifier(
                Dictionary<string, string> inRamsellValsByTag,
                Dictionary<string, string> outRspValsByItmVar)
            {
                string result = null;

                if (!String.IsNullOrWhiteSpace(inRamsellValsByTag["Permanent_Housing"]))
                    result = "1";
                if (!String.IsNullOrWhiteSpace(inRamsellValsByTag["Homeless"]))
                    result = "2";
                if (!String.IsNullOrWhiteSpace(inRamsellValsByTag["Temporary_Housing"]))
                    result = "3";
                if (!String.IsNullOrWhiteSpace(inRamsellValsByTag["Unknown_Living_Situation"]))
                    result = "4";

                if( result != null )
                    outRspValsByItmVar["ADAP_H1_StatusDrop"] = result;
            }
        }
        #endregion

        #region Number of Children

        private class NumberChildrenTransformation : Transformation
        {
            override public string[] GetItemVariableIdentifiers()
            {
                return new string[] { "ADAP_H4_ChildrenIn", "ADAP_H4_ChildrenOut" };
            }

            override public string[] GetRamsellTagNames()
            {
                return new string[] { "Number_Children" };
            }

            private bool trySumStringResponses(string sChildrenIn, string sChildrenOut, out int sum)
            {
                int countIn = 0, countOut = 0;

                bool validIn = Int32.TryParse(sChildrenIn, out countIn);
                bool validOut = Int32.TryParse(sChildrenOut, out countOut);

                if (validIn || validOut){
                    sum = countIn + countOut;
                    return true;
                }
                else
                {
                    sum = -1;
                    return false;
                }
            }

            override public void GetExportValuesByRamsellTagName(
                Dictionary<string, string> inRspValsByItemVar,
                Dictionary<string, string> outRamsellValsByTag)
            {
                string sChildrenIn = inRspValsByItemVar["ADAP_H4_ChildrenIn"];
                string sChildrenOut = inRspValsByItemVar["ADAP_H4_ChildrenOut"];
                int total = -1;

                if (trySumStringResponses(sChildrenIn, sChildrenOut, out total))
                {
                    outRamsellValsByTag["Number_Children"] = total.ToString();
                }
            }

            override public void GetImportValuesByItemVarIdentifier(
                Dictionary<string, string> inRamsellValsByTag,
                Dictionary<string, string> outRspValsByItmVar)
            {
                string sNumChildrenRamsell = inRamsellValsByTag["Number_Children"];
                int numChildrenRamsell = -1;
                if (Int32.TryParse(sNumChildrenRamsell, out numChildrenRamsell) && numChildrenRamsell > -1 )
                {
                    //since we got a numerical value from Ramsell, check if there are already numerical def responses that match
                    string sChildrenIn = outRspValsByItmVar["ADAP_H4_ChildrenIn"];
                    string sChildrenOut = outRspValsByItmVar["ADAP_H4_ChildrenOut"];
                    int numChildrenDef = -1;
                    if (trySumStringResponses(sChildrenIn, sChildrenOut, out numChildrenDef) && numChildrenDef == numChildrenRamsell )
                    {
                        return;
                    }

                    //the current def responses don't match, so update them: assume that all the children are living in the household
                    outRspValsByItmVar["ADAP_H4_ChildrenIn"] = numChildrenRamsell.ToString();
                }
                else
                {
                    //the value from ramsell is not numerical, so there's no way to handle it. Leave def responses unchanged.
                    return;
                }
            }
        }
        #endregion

        #region HIV_Diagnosis_Date

        private class HivDiagnosisDateTransformation : Transformation
        {
            override public string[] GetItemVariableIdentifiers()
            {
                return new string[] { "ADAP_M1_Month", "ADAP_M1_Year" };
            }

            override public string[] GetRamsellTagNames()
            {
                return new string[] { "HIV_Diagnosis_Date" };
            }

            override public void GetExportValuesByRamsellTagName(
                Dictionary<string, string> inRspValsByItemVar,
                Dictionary<string, string> outRamsellValsByTag)
            {

                string inMonth = inRspValsByItemVar["ADAP_M1_Month"];
                string inYear = inRspValsByItemVar["ADAP_M1_Year"];

                int monthNum = 0, yearNum = 0;
                bool validMonth = false, validYear = false;

                validMonth = Int32.TryParse(inMonth, out monthNum) && monthNum >= 1 && monthNum <= 12;
                validYear = Int32.TryParse(inYear, out yearNum) && yearNum > 1000 && yearNum < 2100;

                if (validYear)
                {
                    string result = null;
                    if (validMonth)
                    {
                        string sMonth = monthNum.ToString();
                        if (sMonth.Length == 1)
                            sMonth = "0" + sMonth;
                        result = yearNum + "-" + sMonth + "-01";
                    }
                    else
                    {
                        result = yearNum + "-01-01";
                    }
                    outRamsellValsByTag["HIV_Diagnosis_Date"] = result;
                }
            }

            override public void GetImportValuesByItemVarIdentifier(
                Dictionary<string, string> inRamsellValsByTag,
                Dictionary<string, string> outRspValsByItmVar)
            {
                string sDate = inRamsellValsByTag["HIV_Diagnosis_Date"];
                if (!String.IsNullOrWhiteSpace(sDate))
                {
                    string[] parts = sDate.Split('-');
                    if (parts.Length == 3)
                    {
                        outRspValsByItmVar["ADAP_M1_Month"] = parts[1];
                        outRspValsByItmVar["ADAP_M1_Year"] = parts[0];
                    }
                }
            }
        }
        #endregion

        #region Aids status

        private class AidsStatusTransformation : Transformation
        {
            override public string[] GetItemVariableIdentifiers()
            {
                return new string[] { "ADAP_M2_ToldAIDS" };
            }

            override public string[] GetRamsellTagNames()
            {
                return new string[] { "AIDS_Dx" };
            }

            override public void GetExportValuesByRamsellTagName(
                Dictionary<string, string> inRspValsByItemVar,
                Dictionary<string, string> outRamsellValsByTag)
            {
                string adapResponse = inRspValsByItemVar["ADAP_M2_ToldAIDS"];
                string result = "3";
                if ( !String.IsNullOrWhiteSpace(adapResponse) && adapResponse == "0" )
                {
                    result = "4";
                }
                outRamsellValsByTag["AIDS_Dx"] = result;
            }

            override public void GetImportValuesByItemVarIdentifier(
                Dictionary<string, string> inRamsellValsByTag,
                Dictionary<string, string> outRspValsByItmVar)
            {
                string ramsellVal = inRamsellValsByTag["AIDS_Dx"];
                if ( !String.IsNullOrWhiteSpace( ramsellVal ) && ramsellVal == "4")
                    outRspValsByItmVar["ADAP_M2_ToldAIDS"] = "0";
                else
                    outRspValsByItmVar["ADAP_M2_ToldAIDS"] = "1";
            }
        }
        #endregion

        #endregion

        /// <summary>
        /// This arry should contain one instance of each transformation implimentation
        /// </summary>
        private static readonly Transformation[] allTransformations = new Transformation[]{ 
            new EthnicityTransformation(),
            new GenderTransformation(),
            new PhoneTransformation(),
            new PatientAccessTransformation(),
            new EmploymentTransformation(),
            new HousingTransformation(),
            new NumberChildrenTransformation(),
            new HivDiagnosisDateTransformation(),
            new AidsStatusTransformation(),
        };

        /// <summary>
        /// For testing only,
        /// 
        /// this returns a COPY of allTransformations so that 
        /// the original cannot be modified outside of this file
        /// </summary>
        /// <returns></returns>
        public static Transformation[] GetAllTransformations()
        {
            int n = allTransformations.Length;
            Transformation[] result = new Transformation[n];
            Array.Copy(allTransformations, result, n);
            return result;
        }

        private static Transformation GetTransformationforRamsellTagName(string ramsellTagName)
        {
            Transformation matchingTrans = null;
            foreach (Transformation trans in allTransformations)
            {
                if (Array.IndexOf(trans.GetRamsellTagNames(), ramsellTagName) > -1)
                {
                    matchingTrans = trans;
                    break;
                }
            }
            return matchingTrans;
        }

        /// <summary>
        /// called by RamsellImport
        /// </summary>
        /// <param name="specialCaseValuesByTagname"></param>
        /// <param name="toFormResultId"></param>
        /// <param name="formsRepo"></param>
        public static void ImportSpecialCasesNoSave(Dictionary<string, string> specialCaseValuesByTagname, int toFormResultId, IFormsRepository formsRepo)
        {
            UserData ud = new UserData(formsRepo);
            foreach (string ramsellTagName in specialCaseValuesByTagname.Keys)
            {
                Transformation matchingTrans = GetTransformationforRamsellTagName(ramsellTagName);
                if( matchingTrans == null )
                    continue;

                //prepare in/out vars for the transformation
                Dictionary<string, string> inRamsellValsByTag = new Dictionary<string, string>();
                foreach (string tag in matchingTrans.GetRamsellTagNames())
                {
                    inRamsellValsByTag.Add(tag, specialCaseValuesByTagname.ContainsKey(tag) ? specialCaseValuesByTagname[tag] : null );
                }

                //output is pre-populated with existing responses - though they are typically ignored by transformations
                Dictionary<string, string> outRspValsByItmVar = new Dictionary<string, string>();
                foreach (string ivIdentifier in matchingTrans.GetItemVariableIdentifiers())
                {
                    def_ResponseVariables existingRv = formsRepo.GetResponseVariablesByFormResultIdentifier(toFormResultId, ivIdentifier);
                    outRspValsByItmVar.Add(ivIdentifier, existingRv == null ? null : existingRv.rspValue);
                }

                //run transformation
                matchingTrans.GetImportValuesByItemVarIdentifier(inRamsellValsByTag, outRspValsByItmVar);

                //insert output values into DB
                foreach (string ivIdent in outRspValsByItmVar.Keys)
                {
                    def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(ivIdent);
                    def_ItemResults ir = ud.SaveItemResult(toFormResultId, iv.itemId);
                    ud.SaveResponseVariable(ir, iv, outRspValsByItmVar[ivIdent] );
                }
            }
        }

        /// <summary>
        /// Called by RamsellExport. Returns null, or content to insert into the [ramsellTagName] node in the ramsell export xml.
        /// 
        /// Find and export up to one Transformation matching the ramsellTagName.
        /// 
        /// For transformations that export multiple ramsell tags, only the one matching [ramsellTagName] will be used.
        /// Those transformations will typically be run multiple times for each of their export tag names
        /// This is necessary because the xml structure is dictated by a schema
        /// 
        /// Running the same transformation export multiple times is reasonable because the actual logic is so simple. 
        /// The bottleneck will be in pulling responses from the DB. So if optimization is necessary we should
        /// query all the responses once and pass them to this function
        /// </summary>
        /// <param name="ramsellTagName"></param>
        /// <param name="formResultId"></param>
        /// <param name="formsRepo"></param>
        /// <returns>a string to insert into the ramsell export xml with tagname [ramsellTagName], 
        /// or null to indicate that no special-case transformation is available</returns>
        public static string GetExportValueForRamsellTag(string ramsellTagName, int formResultId, IFormsRepository formsRepo)
        {
            
            //check if any transformations relate to this ramsellTagName
            Transformation matchingTrans = GetTransformationforRamsellTagName(ramsellTagName);

            if (matchingTrans == null)
            {
                //no matching transformation was found, return null to indicate that this ramsellTagName still needs to be handled
                return null;
            }
            else
            {
                //prepare in/out vars for the transformation
                Dictionary<string, string> inRspValsByItemVar = new Dictionary<string, string>();
                foreach (string itemVarIdentifier in matchingTrans.GetItemVariableIdentifiers())
                {
                    def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, itemVarIdentifier);
                    inRspValsByItemVar.Add(itemVarIdentifier, rv == null ? null : rv.rspValue);
                }

                Dictionary<string, string> outRamsellValsByTag = new Dictionary<string, string>();
                foreach (string rmslTagName in matchingTrans.GetRamsellTagNames())
                {
                    outRamsellValsByTag.Add(rmslTagName, null);
                }

                //run transformation
                matchingTrans.GetExportValuesByRamsellTagName(inRspValsByItemVar, outRamsellValsByTag);
                
                //return content to insert into this node in the xml export, 
                //ignoring any other ramsell tagNames exported by this transformation
                return outRamsellValsByTag[ramsellTagName];
            }
        }


        private static Exception UnrecognizedRamsellValueException(string unrecognizedValue, string ramsellTagName)
        {
            return new Exception("Unrecognized value \"" + unrecognizedValue + "\" for Ramsell tag \"" + ramsellTagName + "\"");
        }

        private static Exception UnrecognizedResponseValueException(string unrecognizedValue, string itemVariableIdentifier)
        {
            return new Exception("Unrecognized response value \"" + unrecognizedValue + "\" for item variable \"" + itemVariableIdentifier + "\"");
        }
    }
}