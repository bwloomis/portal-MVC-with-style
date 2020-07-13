using AJBoggs.Sis.Reports;

using Data.Abstract;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Assmnts.Reports
{
    class SisShortLongReport : SisPdfReport
    {

        public SisShortLongReport(IFormsRepository formsRepo, int formResultId, SisPdfReportOptions options, string logoPath, string outputPath)
            :base(formsRepo, formResultId, options, logoPath, outputPath){}

        override public void BuildReport()
        {
            int ageInYears = SharedScoring.ComputeClientAgeInYears(formsRepo, formResultId);

            PrintProfilePage( output, ageInYears );
            output.appendPageBreak();

            PrintSupportNeedsGraph( output, ageInYears );
            output.appendPageBreak();

            PrintIntroPage( output );
            output.appendPageBreak();

            output.drawY -= PdfOutput.itemSpacing;
            PrintSupplementalProtectionAndAdvocacyScale(output);
            output.appendPageBreak();

            PrintExceptionalMedicalNeeds(output);
            output.appendPageBreak();

            PrintSupplementalQuestions(output);
        }

        private void PrintExceptionalMedicalNeeds(PdfOutput output)
        {
            def_Parts part = formsRepo.GetPartByFormAndIdentifier(form, "Section 1. Exceptional Medical and Behavioral Support Needs");
            AppendPartHeader(output, part.identifier);
            foreach (def_Sections sct in formsRepo.GetSectionsInPart(part))
            {
                PrintExceptMedNeedsTable(part, output, sct);
            }
        }

        private void PrintSupplementalProtectionAndAdvocacyScale(PdfOutput output)
        {
            def_Parts part = formsRepo.GetPartByFormAndIdentifier(form, "Section 3. Supplemental Protection and Advocacy Scale");
            AppendPartHeader(output, part.identifier);
            foreach (def_Sections sct in formsRepo.GetSectionsInPart(part))
            {
                BuildDetailedResponseTable(part, output, sct);
            }
        }

        //this is for the sections in the "Exceptional medical... needs" part
        private void PrintExceptMedNeedsTable( def_Parts part, PdfOutput output, def_Sections sct)
        {
            int nCols = 4;
            string[] headers = { sct.title, "", "Score", "Important \"To\" or \"For\"" };
            double[] indents = { .5, .75, 5.6, 6.75 };

            //headers
            output.drawY -= .1;
            for (int i = 0; i < nCols; i++)
            {
                output.DrawText(output.boldFont, output.fontSize, indents[i], output.drawY, headers[i]);
            }
            output.drawY += .13; output.appendSectionBreak();
            output.drawY -= PdfOutput.itemSpacing;

            indents = new double[] { .5, .75, 5.73, 6.75 };
            List<def_Items> itms = new List<def_Items>();
            formsRepo.SortSectionItems(sct);
            foreach (def_SectionItems si in sct.def_SectionItems)
            {
                if (si.subSectionId == null)    // NOTE: this case probably never occurs for this Part
                {
                    itms.Add(formsRepo.GetItemById(si.itemId));
                }
                else
                {
                    def_Sections sctn = formsRepo.GetSubSectionById(si.subSectionId);
                    formsRepo.SortSectionItems(sctn);
                    foreach (def_SectionItems ssi in sctn.def_SectionItems)
                    {
                        if (ssi.subSectionId == null)
                        {
                            itms.Add(formsRepo.GetItemById(ssi.itemId));
                        }
                    }
                }
            }
            int nRows = itms.Count() + 1;

            // Load the Values
            for (int row = 0; row < itms.Count; row++)
            {
                def_Items itm = itms[row];
                formsRepo.GetItemResultByFormResItem(formResultId, itm.itemId);
                def_ItemResults itmResults = itm.def_ItemResults.SingleOrDefault(ir => (ir.formResultId == formResultId));
                formsRepo.GetItemResultsResponseVariables(itmResults);
                foreach (def_ResponseVariables rv in itmResults.def_ResponseVariables)
                {
                    rv.def_ItemVariables = formsRepo.GetItemVariableById(rv.itemVariableId);
                }

                output.drawY -= .1;
                for (int col = 0; col < nCols; col++)
                {
                    string s = "ERROR";
                    switch (col)
                    {
                        case 0: 
                            s = (row + 1).ToString(); 
                            break;

                        case 1: 
                            s = itm.label; 
                            break;

                        case 2:
                            def_ResponseVariables rv = itmResults.def_ResponseVariables.SingleOrDefault(r => r.def_ItemVariables.identifier.EndsWith("_ExMedSupport"));
                            s = ( (rv == null) ? "" : rv.rspValue);
                            break;

                        case 3: 
                            s = String.Empty; 
                            break;
                    }
                    output.DrawText(output.contentFont, output.fontSize, indents[col], output.drawY, s);
                }
                output.drawY -= PdfOutput.itemSpacing * 2.5;
                if (output.drawY < 1)
                    output.appendPageBreak();
            }

            output.drawY -= .1;
            output.DrawText(output.boldFont, output.fontSize, indents[1], output.drawY, "Page Notes:");
            output.drawY -= PdfOutput.itemSpacing * 4;
            if (output.drawY < 1)
                output.appendPageBreak();

        }

        // This is for the sets of items that have 3 numerical response values
        private void BuildDetailedResponseTable( def_Parts part, PdfOutput output, def_Sections sct)
        {
            int nCols = 6;
            string[] headers = { sct.title, "", "Freq", "Time", "Type", "Important \"To\" or \"For\"" };
            double[] indents = { .5, .75, 5.3, 5.6, 5.95, 6.75 };

            //headers
            output.drawY -= .1;
            for (int i = 0; i < nCols; i++)
            {
                output.DrawText(output.boldFont, output.fontSize, indents[i], output.drawY, headers[i]);
            }
            output.drawY += .13; output.appendSectionBreak();
            output.drawY -= PdfOutput.itemSpacing;

            indents = new double[] { .5, .75, 5.38, 5.73, 6.05, 6.75 };
            List<def_Items> itms = formsRepo.GetSectionItems(sct);
            // Values
            for (int row = 0; row < itms.Count; row++)
            {
                def_Items itm = itms[row];
                def_ItemResults itmResults = formsRepo.GetItemResultByFormResItem(formResultId, itm.itemId);
                formsRepo.GetItemResultsResponseVariables(itmResults);
                foreach (def_ResponseVariables rv in itmResults.def_ResponseVariables)
                {
                    rv.def_ItemVariables = formsRepo.GetItemVariableById(rv.itemVariableId);
                }

                output.drawY -= .1;
                for (int col = 0; col < nCols; col++)
                {
                    string s = "ERROR";
                    switch (col)
                    {
                        case 0: 
                            s = (row + 1).ToString(); 
                            break;

                        case 1: 
                            s = itm.label; 
                            break;

                        case 2:
                        case 3:
                        case 4:
                            s = null;
                            string suffix = SupportNeedsColumnSuffixes[col-2];
                            try
                            {
                                //Debug.WriteLine("\titemvariableId= " + ivEnum.Current.itemVariableId);
                                s = itmResults.def_ResponseVariables.SingleOrDefault(rv => rv.def_ItemVariables.identifier.EndsWith(suffix)).rspValue;
                            }
                            catch (System.NullReferenceException ex)
                            {
                                Debug.Print("for itemId " + itm.itemId + ", could not find responseVariable linked to itemVariableIdentifier with suffix \"" + suffix + "\"");
                                Debug.Print("   NullReferenceException: " + ex.Message);
                            }
                            s = String.IsNullOrEmpty(s) ? "N/A" : s;
                            break;

                        case 5: 
                            s = String.Empty; 
                            break;
                    }
                    output.DrawText(output.contentFont, output.fontSize, indents[col], output.drawY, s);
                }
                output.drawY -= PdfOutput.itemSpacing * 2.5;
                if (output.drawY < 1)
                    output.appendPageBreak();
            }

            output.drawY -= .1;
            output.DrawText(output.boldFont, output.fontSize, indents[1], output.drawY, "Page Notes:");
            output.drawY -= PdfOutput.itemSpacing * 4;
            if (output.drawY < 1)
                output.appendPageBreak();
        }
    }
}