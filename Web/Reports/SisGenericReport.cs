using AJBoggs.Sis.Reports;

using Data.Abstract;


namespace Assmnts.Reports
{
    class SisGenericReport : SisPdfReport
    {
        public SisGenericReport(IFormsRepository formsRepo, int formResultId, SisPdfReportOptions options, string logoPath, string outputPath)
            : base(formsRepo, formResultId, options, logoPath, outputPath) { }

        override public void BuildReport()
        {
            int ageInYears = SharedScoring.ComputeClientAgeInYears(formsRepo, formResultId);
            PrintProfilePage(output, ageInYears); 

            foreach (def_FormParts fp in formResults.def_Forms.def_FormParts)
            {
                output.appendPageBreak();
                AppendPartHeader(output, fp.def_Parts.identifier);
                foreach (def_Sections sct in formsRepo.GetSectionsInPart( fp.def_Parts ) )
                {
                    //skip the profile section
                    if (sct.identifier.Equals("Profile"))
                        continue;

                    PrintGenericSection(output, sct, 1);
                }
            }
        }

        private static readonly double labelIndent = 1;
        private static readonly double valueIndent = 2;

        private void PrintGenericSection( PdfOutput output, def_Sections section, int indentLevel )
        {
            output.appendSectionBreak();
            double indent = .5 + labelIndent * (indentLevel - 1);
            output.appendWrappedText(section.identifier, indent, 8-indent, 12);
            output.drawY -= .2;
            formsRepo.GetSectionItems(section);
            foreach (def_SectionItems si in section.def_SectionItems)
            {
                if ( si.subSectionId.HasValue )
                {
                    PrintGenericSection(output, formsRepo.GetSubSectionById(si.subSectionId.Value), indentLevel+1);
                }
                else
                {
                    def_Items itm = formsRepo.GetItemById(si.itemId);//si.def_Items;
                    indent = labelIndent * indentLevel;
                    output.appendWrappedText(itm.label, indent, 8 - indent, output.boldFont);
                    foreach (def_ItemVariables iv in itm.def_ItemVariables)
                    {
                        def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(formResultId, iv.itemVariableId);
                        string s = (rv == null) ? "N/A" : rv.rspValue;
                        indent = valueIndent + labelIndent * (indentLevel - 1);
                        output.appendWrappedText( iv.identifier + ": " + s, indent, 8-indent );
                    }
                    output.drawY -= .1;
                }
            }
        }
    }
}