using Assmnts.Infrastructure;
using Data.Abstract;
using PdfFileWriter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;


namespace Assmnts.Reports
{

    public class AdapPdfReport : FormResultPdfReport
    {
        //handlers for printing special-case sections, by section identifier
        //initialized in constructor below
        private readonly Dictionary<string, Action<def_Sections, int, double, double, double>> specialCaseSections;

        private readonly List<string> sectionsToSkip;

        public AdapPdfReport(
            IFormsRepository formsRepo, 
            int formResultId, 
            string outputPath, 
            bool grayscale ) : base( formsRepo, formResultId, outputPath, grayscale )
        {
            setFontSize(8);
            setPartHeaderFontSize(12);
            setSectionHeaderFontSize(10);

            if (form.title.Equals("ADAP Application"))
            {
                setCustomSectionOrder("ADAP_demographic", "ADAP_contact", "ADAP_household", "ADAP_medical",
                        "ADAP_health", "ADAP_income", "ADAP_cert", "ADAP_finalCert");
            }
            else
            {
                defaultOrderSections();
            }
            setSectionIdentifierPrefixToRemove("ADAP_");

            //handlers for printing special-case sections, by section identifier
            //handlers should print section contents
            specialCaseSections = new Dictionary<string, Action<def_Sections, int, double, double, double>>{
                { "ADAP_C1", printC1 },
                { "ADAP_C3", printC3 },
                { "ADAP_D5", printD5AndD6 },
                { "ADAP_D6", printD5AndD6 },
                { "ADAP_D7", printD7 },
                { "ADAP_D8", printD8 },
                { "ADAP_M1", printM1 },
                { "ADAP_H5", printH5 },
                { "ADAP_F3_A", printF3 },
                { "ADAP_F3_B", printF3 },
                { "ADAP_F3_C", printF3 },
                { "ADAP_F3_D", printF3 },
                { "ADAP_I2", printI2 },
                { "ADAP_I3", printI3 },
            };

            sectionsToSkip = new string[] { "ADAP_F3", "ADAP_reminder" }.ToList();

            //many sections can potentially be printed on one line using the same function
            foreach (string sctName in "D2 D4 D9 H1 H2 M3 M4".Split(' '))
                specialCaseSections.Add("ADAP_" + sctName, tryPrintSingleLineResponse);
            foreach (string sctName in "D3".Split(' '))
                specialCaseSections.Add("ADAP_" + sctName, tryPrintSingleLineLabel);
        }

        private void tryPrintSingleLineResponse(def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {
            Dictionary<string, string> responses = GetResponsesByItemVariableIdentifierThroughLookupTables(sct);

            //create a filtered set of responses, ignoring all emtpy or "no" responses
            Dictionary<string, string> filteredResponses = new Dictionary<string, string>(responses);
            foreach (string key in filteredResponses.Keys.ToList())
            {
                if (filteredResponses[key].Trim().Length == 0)
                    filteredResponses.Remove(key);
            }
            if (filteredResponses.Count > 1)
            {
                foreach (string key in filteredResponses.Keys.ToList())
                {
                    if (filteredResponses[key].Equals("No"))
                        filteredResponses.Remove(key);
                }
            }

            //if there is exactly one filtered response for this section, print it on the same line as the section header
            if (filteredResponses.Count == 1)
            {
                //certain sections with long titles need the responses printed farther to the right in the report
                if (sct.identifier.EndsWith("M2") || sct.identifier.EndsWith("I2"))
                    responseIndent += 1.5;

                string rsp = filteredResponses.Values.First();
                double drawY = output.drawY;
                printGenericSectionHeader(sct, sectionLabelIndent);
                output.drawY = drawY;
                output.appendWrappedText(rsp, responseIndent, 8 - responseIndent);
                output.drawY -= .1;
            }

            //otherewise, delegate to the standard section printing
            else
            {
                base.PrintGenericSection(output, sct, indentLevel, responses);
            }
        }

        #region Special-case section printing methods
        
        private void printH5( def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {
            Dictionary<string, string> responses = GetResponsesByItemVariableIdentifier(sct);

            //if the client is female...
            if (responses["ADAP_D8_CurrGenderDrop"].Equals("Female"))
            {
                string rsp = responses["ADAP_H5_PregnantOpt"];
                double drawY = output.drawY;
                printGenericSectionHeader(sct, sectionLabelIndent);
                output.drawY = drawY;
                output.appendWrappedText(rsp, responseIndent, 8 - responseIndent);
                output.drawY -= .1;

                //if client is female and pregnant
                if (responses["ADAP_H5_PregnantOpt"].Equals("Yes"))
                {
                    string label = formsRepo.GetItemByIdentifier("ADAP_H5_PregnantDue_item").label;
                    rsp = responses["ADAP_H5_PregnantDue"];
                    double itemLabelHeight = output.appendWrappedText( label, itemLabelIndent, responseIndent - itemLabelIndent, output.boldFont);
                    double drawYBelowItemLabel = output.drawY;
                    output.drawY += itemLabelHeight;
                    output.appendWrappedText(rsp, responseIndent, 8 - responseIndent);
                    output.drawY = Math.Min(drawYBelowItemLabel, output.drawY - .05);
                }
            }

            //if the client isn't female, no code runs here (meaning this section is ommitted from the report)
        }

        private void tryPrintSingleLineLabel(def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {

            Dictionary<string, string> responses = GetResponsesByItemVariableIdentifier(sct);
            List<string> positiveResponsesItemVariables = responses.Where(pair => pair.Value.Equals("Yes")).Select(pair => pair.Key).ToList();

            //if there is exactly one response for this section, print it on the same line as the section header
            if (positiveResponsesItemVariables.Count == 1)
            {
                string ivIdent = positiveResponsesItemVariables.First();
                def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(ivIdent);
                def_Items itm = formsRepo.GetItemById(iv.itemId);
                string label = itm.label;
                double drawY = output.drawY;
                printGenericSectionHeader(sct, sectionLabelIndent);
                output.drawY = drawY;
                output.appendWrappedText(label, responseIndent, 8 - responseIndent);
                output.drawY -= .1;
            }

            //otherewise, delegate to the standard section printing
            else
            {
                base.PrintGenericSection(output, sct, indentLevel, responses);
            }
        }

        private void printD5AndD6(def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {
            Dictionary<string, string> responses = GetResponsesByItemVariableIdentifier(sct);

            List<string> yesLabels = new List<string>();
            foreach( string ivIdent in responses.Keys )
            {
                if (responses[ivIdent].Equals("Yes"))
                {
                    def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(ivIdent);
                    def_Items itm = formsRepo.GetItemById(iv.itemId);
                    yesLabels.Add(itm.label);
                }
            }
            if (yesLabels.Count > 0)
            {
                string rsp = yesLabels[0];
                for (int i = 1; i < yesLabels.Count; i++)
                    rsp += ", " + yesLabels[i];

                printGenericSectionHeader(sct, sectionLabelIndent);
                output.appendWrappedText( rsp, responseIndent, 8 - responseIndent);
                output.drawY -= .1;
            }
        }

        //ADAP_D7 (preferred language)
        private void printD7(def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {
            //append the one response in the same line as the section header
            //if the user responded with "other", show the text they entered into the "other" textbox
            double drawY = output.drawY;
            printGenericSectionHeader(sct, sectionLabelIndent);
            def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier("ADAP_D7_LangDrop");
            string rsp = GetResponseThroughLookupTables(formResultId, iv.def_Items, iv );
            if (rsp.StartsWith("Other"))
                rsp = GetSingleResponse("ADAP_D7_LangOther");

            if( !String.IsNullOrWhiteSpace( rsp ) )
            {
                output.drawY = drawY;
                output.appendWrappedText(rsp, responseIndent, 8 - responseIndent);
                output.drawY -= .1;
            }
        }

        private void printD8(def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {
            //append the "current gender" response in the same line as the section header
            double drawY = output.drawY;
            printGenericSectionHeader(sct, sectionLabelIndent);
            def_ItemVariables ivCurrGender = formsRepo.GetItemVariableByIdentifier("ADAP_D8_CurrGenderDrop");
            string rspCurrGender = GetResponseThroughLookupTables(formResultId, ivCurrGender.def_Items, ivCurrGender);
            if (!String.IsNullOrWhiteSpace(rspCurrGender))
            {
                output.drawY = drawY;
                output.appendWrappedText(rspCurrGender, responseIndent, 8 - responseIndent);
                output.drawY -= .1;
            }

            //if applicable, append the "gender at birth" label and response on one line
            def_Items itmBirthGenderLabel = formsRepo.GetItemByIdentifier("ADAP_D8_BirthGenderLabel_item");
            def_ItemVariables ivBirthGender = formsRepo.GetItemVariableByIdentifier("ADAP_D8_BirthGenderDrop");
            string rspBirthGender = GetResponseThroughLookupTables(formResultId, ivBirthGender.def_Items, ivBirthGender);
            if (!String.IsNullOrWhiteSpace(rspBirthGender))
            {
                double itemLabelHeight = itmBirthGenderLabel == null ? 0 :
                    output.appendWrappedText(itmBirthGenderLabel.label, itemLabelIndent, responseIndent - itemLabelIndent, output.boldFont);
                double drawYBelowItemLabel = output.drawY;
                output.drawY += itemLabelHeight;
                output.appendWrappedText(rspBirthGender, responseIndent, 8 - responseIndent);
                output.drawY = Math.Min(drawYBelowItemLabel, output.drawY - .05);
            }

            //Dictionary<string, string> responses = GetResponsesByItemVariableIdentifierThroughLookupTables(sct);

            //foreach (string prefix in new string[] { "ADAP_D8_CurrGender", "ADAP_D8_BirthGender" })
            //{

            //    string ivIdent = prefix + "Drop";
            //    string labelItemIdent = prefix + "Label_item";

            //    def_Items labelItm = formsRepo.GetItemByIdentifier(labelItemIdent);
            //    string rsp = responses.ContainsKey( ivIdent ) ? responses[ivIdent] : "";

            //    //append item label (if applicable), without actually moving drawY down the page
            //    double itemLabelHeight =  labelItm == null ? 0 : 
            //        output.appendWrappedText(labelItm.label, itemLabelIndent, responseIndent - itemLabelIndent, output.boldFont);
            //    double drawYBelowItemLabel = output.drawY;
            //    output.drawY += itemLabelHeight;
            //    output.appendWrappedText(rsp, responseIndent, 8 - responseIndent);
            //    output.drawY = Math.Min(drawYBelowItemLabel, output.drawY - .05);

            //}
        }

        private void printC1(def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {
            printGenericSectionHeader(sct, sectionLabelIndent);

            string prefix = "ADAP_C1_";
            
            //append some responses generically
            foreach (string shortIdent in new string[] { "Address", "MayContactYN" })
                appendItemLabelAndResponses(formsRepo.GetItemByIdentifier(prefix + shortIdent + "_item"), itemLabelIndent, responseIndent, 
                    GetResponsesByItemVariableIdentifierThroughLookupTables( sct ) );

            //append city,state,zip,county all in one line
            string combinedResponse = "";
            foreach (string shortIdent in new string[] { "City", "State", "Zip", "County" })
            {
                string response = GetSingleResponse(prefix + shortIdent);
                if (combinedResponse.Length > 0)
                    combinedResponse += ", ";
                combinedResponse += String.IsNullOrWhiteSpace(response) ? "[No " + shortIdent + " entered]" : response;
            }
            double itemLabelHeight = output.appendWrappedText( "City, State, Zip, County", itemLabelIndent, responseIndent - itemLabelIndent, output.boldFont);
            double drawYBelowItemLabel = output.drawY;
            output.drawY += itemLabelHeight;
            output.appendWrappedText( combinedResponse, responseIndent, 8 - responseIndent);
            output.drawY = Math.Min(drawYBelowItemLabel, output.drawY - .05);

            // append address proof with hyperlink to the attachment (if attachment exists)
            if ( !String.IsNullOrWhiteSpace( GetSingleResponse("ADAP_C1_AddressProof") ) )
            {
                appendAdapAttachmentLink(formsRepo.GetItemByIdentifier("ADAP_C1_AddressProof_item").label, "ADAP_C1_AddressProof", itemLabelIndent, responseIndent);
            }
        }

        //ADAP_C3
        private void printC3(def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {
            printGenericSectionHeader(sct, sectionLabelIndent);

            Dictionary<string, string> responses = GetResponsesByItemVariableIdentifierThroughLookupTables(sct);
            for (int i = 1; i <= 2; i++)
            {
                string prefix = "ADAP_C3_Phone" + i + "_";
                if (responses.ContainsKey(prefix + "Num") && !String.IsNullOrWhiteSpace(responses[prefix + "Num"]))
                {
                    foreach (string shortIdent in new string[] { "Num", "Type", "MayMsgYN" })
                    {
                        def_Items itm = formsRepo.GetItemByIdentifier(prefix + shortIdent + "_item");
                        itm = formsRepo.GetItemById(itm.itemId);    //Get the version of the item as it appears on the form.                   
                        appendItemLabelAndResponses(itm, itemLabelIndent, responseIndent, responses);
                    }
                }
            }
        }

        ////ADAP_D3
        //private void printD3(def_Sections sct, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        //{
        //    //append the section header generically, only keep output.drawY constant
        //    double drawY = output.drawY;
        //    printGenericSectionHeader(sct, sectionLabelIndent);
        //    output.drawY = drawY;

        //    //print the item label for each positive response, on the same line as the section header
            
        //}

        protected void appendAdapAttachmentLink( string itemLabel, string ivIdent, double itemLabelIndent, double responseIndent)
        {
            //append item label, without actually moving drawY down the page
            double itemLabelHeight = output.appendWrappedText(itemLabel, itemLabelIndent, responseIndent - itemLabelIndent, output.boldFont);
            double drawYBelowItemLabel = output.drawY;
            output.drawY += itemLabelHeight;

            //append hyperlink to the right of the item label
            string displayText = "Download Attachment";
            string href = ConfigurationManager.AppSettings["SISOnlineUrl"] + "COADAP/DownloadAttachment?ivIdent=" + ivIdent + "&formResultId=" + formResultId;
            output.appendWebLink(displayText, href, responseIndent);
            output.drawY = Math.Min(drawYBelowItemLabel, output.drawY - .05);
        }

        //ADAP_F3
        protected void printF3(def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {
            printGenericSectionHeader(sct, sectionLabelIndent);
            Dictionary<string, string> responses = GetResponsesByItemVariableIdentifierThroughLookupTables(sct);
            foreach (string ivIdent in responses.Keys)
            {
                if (String.IsNullOrWhiteSpace(responses[ivIdent]))
                    continue;
                def_Items itm = formsRepo.GetItemByIdentifier(ivIdent + "_item");

                itm = formsRepo.GetItemById(itm.itemId);    //Get the version of the item as it appears on the form. 

                if (ivIdent.EndsWith("IncomeProof") || ivIdent.EndsWith("EmployerForm"))
                {
                    appendAdapAttachmentLink(itm.label, ivIdent, itemLabelIndent, responseIndent);
                }
                else
                {
                    appendItemLabelAndResponses(itm, itemLabelIndent, responseIndent, responses);
                }
            }
        }

        //ADAP_I2
        protected void printI2(def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {
            printGenericSectionHeader(sct, sectionLabelIndent);
            Dictionary<string, string> responses = GetResponsesByItemVariableIdentifierThroughLookupTables(sct);
            foreach (string ivIdent in responses.Keys)
            {
                if (String.IsNullOrWhiteSpace(responses[ivIdent]))
                    continue;
                def_Items itm = formsRepo.GetItemByIdentifier(ivIdent + "_item");

                if (ivIdent == "ADAP_I2_Invoice" )
                {
                    appendAdapAttachmentLink(itm.label, ivIdent, itemLabelIndent, responseIndent);
                }
                else
                {
                    appendItemLabelAndResponses(itm, itemLabelIndent, responseIndent, responses);
                }
            }
        }

        //ADAP_I3
        protected void printI3(def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {
            printGenericSectionHeader(sct, sectionLabelIndent);
            Dictionary<string, string> responses = GetResponsesByItemVariableIdentifier(sct);
            foreach (string ivIdent in responses.Keys)
            {
                if (String.IsNullOrWhiteSpace(responses[ivIdent]))
                    continue;
                def_Items itm = formsRepo.GetItemByIdentifier(ivIdent + "_item");

                if (ivIdent == "ADAP_I3_Invoice")
                {
                    appendAdapAttachmentLink(itm.label, ivIdent, itemLabelIndent, responseIndent);
                }
                else
                {
                    appendItemLabelAndResponses(itm, itemLabelIndent, responseIndent, responses);
                }
            }
        }

        //ADAP_M1
        private void printM1(def_Sections sct, int indentLevel, double sectionLabelIndent, double itemLabelIndent, double responseIndent)
        {
            printGenericSectionHeader(sct, sectionLabelIndent);

            string month = GetSingleResponse("ADAP_M1_Month");
            string year = GetSingleResponse("ADAP_M1_Year");
            string city = GetSingleResponse("ADAP_M1_DiagnosisLoc");

            //try and convert the month response from number to month name,
            //and just leave it as the raw response if unsuccessful
            try
            {
                month = new System.Globalization.DateTimeFormatInfo()
                    .GetMonthName(Convert.ToInt32(month)).ToString();
            }
            catch (Exception e) { }

            string text =
                (String.IsNullOrWhiteSpace(month) ? "[no month entered]" : month) + ", " +
                (String.IsNullOrWhiteSpace(year) ? "[no year entered]" : year) + ", " +
                (String.IsNullOrWhiteSpace(city) ? "[no city entered]" : city);

            output.appendWrappedText(text, itemLabelIndent, 8 - itemLabelIndent);
            output.drawY -= .1;
        }
        #endregion

        private void printGenericSectionHeader(def_Sections section, double sectionLabelIndent)
        {
            output.appendWrappedText(buildSectionHeaderText(section),
                sectionLabelIndent, 8 - sectionLabelIndent, output.sectionHeaderFontSize);
            output.drawY -= .1;
        }

        /// <summary>
        /// Override of FormResultPdfReport.PrintGenericSection
        /// 
        /// This will delegate to the special-case section printing methods above, or use the base PrintGenericSection implimentation
        /// </summary>
        /// <param name="output"></param>
        /// <param name="section"></param>
        /// <param name="indentLevel"></param>
        override protected void PrintGenericSection(PdfOutput output, def_Sections section, int indentLevel)
        {
            if (sectionsToSkip.Contains(section.identifier))
            {
                Debug.WriteLine("AdapPdfReport.PrintGenericSection  Skip it: " + section.identifier);
                return;
            }
            else if (specialCaseSections.ContainsKey(section.identifier))
            {

                if (output.drawY < 1.5)
                    output.appendPageBreak();

                if (indentLevel < 2)
                    output.appendSectionBreak();

                double sectionLabelIndent = .5 + labelIndent * (indentLevel - 1);
                double itemLabelIndent = labelIndent * indentLevel;
                double responseIndent = valueIndent + labelIndent * (indentLevel - 1);

                //print section content using special case handler
                specialCaseSections[section.identifier].Invoke(section, indentLevel, sectionLabelIndent, itemLabelIndent, responseIndent);
            }
            else
            {

                //print section header and content generically, excluding application comments except in the "finalCert" section
                Dictionary<string, string> responsesByItemVariable = GetResponsesByItemVariableIdentifierThroughLookupTables(section);
                if (section.identifier != "ADAP_finalCert")
                    responsesByItemVariable.Remove("ADAP_Application_Comments_txt");

                Debug.WriteLine("AdapPdfReport.base.PrintGenericSection: " + section.identifier);
                base.PrintGenericSection(output, section, indentLevel, responsesByItemVariable);
            }
        }

        /// <summary>
        /// Similar to FormResultPdf.getResponsesByItemVariableIdentifier
        /// 
        /// the difference is that this implimentation references def_Lookup tables to get display text for 
        /// dropdown or radio button responses that are saved as numbers in the DB
        /// </summary>
        /// <param name="sct"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetResponsesByItemVariableIdentifierThroughLookupTables(def_Sections sct)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (def_Items itm in formsRepo.GetSectionItems(sct))
            {
                formsRepo.GetItemVariables(itm);
                foreach (def_ItemVariables iv in itm.def_ItemVariables)
                {
                    if (result.ContainsKey(iv.identifier))
                        continue;

                    string responseText = iv.baseTypeId == 8 ?
                        GetResponseThroughLookupTables(formResultId, itm, iv)
                        : GetResponse(formResultId, itm, iv);

                    Debug.WriteLine("iv.identifier: " + iv.identifier + "    responseText: " + responseText);
                    result.Add(iv.identifier, responseText);
                }
            }
            return result;
        }

        /// <summary>
        /// Used to display responses to radiobuttons or dropdowns.
        /// 
        /// Retrieve a numerical response for the given def_FormResult and deF_ItemVariable from the ResponseVariables table,
        /// Then lookup the display text associated with the numerical response, for the current UI language.
        /// </summary>
        /// <param name="formResultId"></param>
        /// <param name="itm"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        private string GetResponseThroughLookupTables(int formResultId, def_Items itm, def_ItemVariables iv)
        {
            string rsp = GetResponse(formResultId, itm, iv);
            def_LookupMaster lm = formsRepo.GetLookupMastersByLookupCode(iv.identifier);

            if (lm == null)
                return rsp;

            def_LookupDetail ld = formsRepo.GetLookupDetailByEnterpriseMasterAndDataValue(SessionHelper.LoginStatus.EnterpriseID, lm.lookupMasterId, rsp);
            if (ld != null)
            {
                CultureInfo ci = Thread.CurrentThread.CurrentUICulture;
                string region = (ci == null) ? "en" : ci.TwoLetterISOLanguageName.ToLower();
                int langId = formsRepo.GetLanguageByTwoLetterISOName(region).langId;
                def_LookupText lt = formsRepo.GetLookupTextsByLookupDetailLanguage(ld.lookupDetailId, langId).FirstOrDefault();
                if (lt != null)
                    return lt.displayText;
            }
            return rsp;
        }
    }
}
