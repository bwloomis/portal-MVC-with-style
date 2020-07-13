using AJBoggs.Sis.Reports;

using Assmnts.Infrastructure;

using Data.Abstract;

using PdfFileWriter;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

using OptionKey = AJBoggs.Sis.Reports.SisPdfReportOptions.OptionKey;


namespace Assmnts.Reports
{
    public class SisFamilyReport : SisPdfReport
    {

        public SisFamilyReport(IFormsRepository formsRepo, int formResultId, SisPdfReportOptions options, string logoPath, string outputPath)
            : base(formsRepo, formResultId, options, logoPath, outputPath) {}

        override public void BuildReport()
        {
            int age = SharedScoring.ComputeClientAgeInYears( formsRepo, formResultId );

            PrintProfilePage(output, age);
            output.appendPageBreak();

            PrintIntroPage(output);
            output.appendPageBreak();

            PrintSupportNeeds(output, options[OptionKey.includeComments]);
            output.appendPageBreak();

            PrintSupportNeedsGraph(output, age);
            output.appendPageBreak();

            PrintExceptionalSupportNeeds(output);

            if (options[OptionKey.includeToFor])
            {
                Dictionary<string, List<def_Sections>> importantToForSectionsByLabel = getImportantToForSectionsByLabel();

                if (HasImportantResponses("ImportantTo", importantToForSectionsByLabel ) )
                {
                    output.appendPageBreak();
                    PrintImportantToItemsAndNotes(output, importantToForSectionsByLabel );
                }

                if (HasImportantResponses("ImportantFor", importantToForSectionsByLabel ) )
                {
                    output.appendPageBreak();
                    PrintImportantForItemsAndNotes(output, importantToForSectionsByLabel );
                }
            }

            //if (!options.isShort && !options.skipSups )
            if (options[OptionKey.includeSups])
            {
                output.appendPageBreak();
                PrintSupplementalQuestions(output);
            }

            PrintEndOfReport(output);
        }

        //these two vars are used in printing the "important to" and "important for" pages
        private static readonly double[] importantRowX = { .58, 2.33, 6.92, 7.2, 7.54 };
        private static readonly string[] importantIvSuffixes = initImportantIvSuffices();
        private static string[] initImportantIvSuffices()
        {
            List<string> impSufs = SupportNeedsColumnSuffixes.ToList();
            impSufs.Add("ExMedSupport");
            impSufs.Add("ExBehSupport");
            return impSufs.ToArray();
        }

        private static readonly string[,] ratingNumonicsSisA = {
            {"None","Monitoring","Verbal/Gesture Prompting","Partial Physical Assistance","Full Physical Support"},
            {"None or Less Than Monthly","At Least Once a Month, But Not Once a Week",
                "At Least Once a Week, But Not Once a Day",
                "At Least Once a Day, But Not Once an Hour","Hourly or More Frequently"},
            {"None","Less Than 30 Minutes","30 Minutes to Less Than 2 Hours","2 Hours to Less Than 4 Hours","4 Hours or More"},
        };

        private static readonly string[,] ratingNumonicsSisC = {
            {"None","Monitoring","Verbal/Gesture Prompting","Partial Physical Assistance","Full Physical Support"},
            {"Negligible","Infrequently",
                "Frequently",
                "Very Frequently","Always"},
            {"None","Less Than 30 Minutes","30 Minutes to Less Than 2 Hours","2 Hours to Less Than 4 Hours","4 Hours or More"},
        };

        private static readonly string[] exceptNumonics = {
            "No Support Needed", "Some Support Needed", "Extensive Support Needed"
        };

        protected override void AppendPartHeader(PdfOutput output, string title)
        {
            output.drawY -= .25;
            output.SetColor(options[OptionKey.grayscale] ? Color.LightGray : Color.FromArgb(255, 255, 204));
            output.FillRectangle(.35, output.drawY - .05, 7.83, .3);
            output.SetColor(options[OptionKey.grayscale] ? Color.Black : Color.FromArgb(0, 0, 128));
            output.DrawText(output.boldFont, 18, PdfOutput.pageWidth / 2, output.drawY, TextJustify.Center, title);
            output.SetColor(Color.Black);
            output.OutlineRectangle(.35, output.drawY - .05, 7.83, .3);
            output.drawY -= .20;
            output.drawY -= PdfOutput.itemSpacing;
        }

        private static readonly object[,] endOfReportSpec = {
            //x,dy,text
            { .5, .52, "<b>How Information from My Support Profile Can Be Used in Supports Planning Approaches</b>" },
            { .36, .4, "\tEveryone benefits from supports that allow them to take part in everyday life activities and "
                +"maintain a healthy lifestyle. The Supports Intensity Scale [Adult_Child] Version ([SIS-A_SIS-C]) assesses a person's pattern and intensity "
                +"of support needs across life activities and exceptional medical and behavioral support need areas. The "
                +"attached 'My Support Profile' summarizes information from the [SIS-A_SIS-C] that can be used in planning supports "
                +"for individuals based on their support needs and the individuals' goals and interests." },
            { .36, .8, "\tPlanning supports for individuals requires the collective wisdom of a Support Team that is made up "
                +"of the individual receiving the services and supports, his/her parents or family members, a case manager "
                +"or supports coordinator, direct support staff who work with the individual, and one or more professionals "
                +"depending on the individual's support needs. The purpose of this attachment to the 'My Support Profile' is "
                +"to provide answers to six questions asked frequently by the individual and his/her support team members "
                +"as collectively they engage in the development, implementation, and monitoring of the individual's support "
                +"planning." },
            { .68, .9, "<b>1. How do we determine what is important to the individual and what is important for the individual?" },
                { 1.15, .34, "> Identifying support needs that are <b>important to the individual</b> is based on the individual's goals, desires, and preferences." },
                { 1.15, .52, "> Identifying support needs that are <b>important for the individual</b> is based on:" },
                    { 1.4, .2, "- higher support need scores from the 'My Support Profile' in the most relevant life activity areas" },
                    { 1.4, .17, "- needed supports in health and safety" },
                    { 1.4, .17, "- interventions prescribed by a professional." },
            { .68, .31, "<b>2. How do we focus on the whole person and the individual's quality of life?" },
                { 1.15, .34, "> The concept of quality of life reflects a holistic approach to an individual and includes areas that are valued by all persons." },
                { 1.15, .52, "> Eight core quality of life areas reflect this holistic approach:" },
                    { 1.4, .2, "- Personal Development" },{ 3.37, 0.0, "- Self-determination" },{ 5.11, 0.0, "- Interpersonal Relations" },
                    { 1.4, .17, "- Social Inclusion" },    { 3.37, 0.0, "- Rights" },            { 5.11, 0.0, "- Emotional Well-being" },
                    { 1.4, .17, "- Physical Well-being" }, { 3.37, 0.0, "- Material Well-being" },
                { 1.15, .35, "> These eight quality of life areas can be used to develop an ISP." },
            { .68, .36, "<b>3. What are the responsibilities of support team members?" },
                { 1.15, .34, "> Determine <b>what is important to and for</b> the individual" },
                { 1.15, .17, "> Identify specific support strategies to address the individual's personal goals and assessed support needs" },
                { 1.15, .34, "> Specify a specific support objective for each support strategy and indicate who is responsible for implementing each support strategy" },
                { 1.15, .34, "> Implement and monitor the Individual Supports Plan" },

            { .68, .59, "<b>4. What supports can we use to enhance the individual's well-being?" },
                { 1.15, .34, "> Natural sources (e.g. family, friends, and community resources)" },
                { 1.15, .17, "> Technology-based (e.g. assistive technology, information technology, smart technology, and prosthetics)" },
                { 1.15, .34, "> Environment-based (e.g. environmental accommodation)" },
                { 1.15, .17, "> Staff directed (e.g. incentives, skills/knowledge, and positive behavior supports)" },
                { 1.15, .17, "> Professional services (e.g. medical, psychological, therapeutic services)" },

            {0.0,0.0,"\f"},//pagebreak

            { .68, .4, "<b>5. How does information obtained from the [SIS-A_SIS-C] relate to professional recommendations?" },
                { 1.15, .34, "> Professional recommendations such as those from a doctor focus on lessening the impact of the individual's disability-related condition." },
                { 1.15, .34, "> SIS information focuses on the supports an individual needs in order to be more successful in everyday life activities." },
                { 1.15, .34, "> Both types of information need to be a part of planning supports for individuals." },

            { .68, .4, "<b>6. How do we know if the supports provided have an effect on the individual?" },
                { 1.15, .34, "> Informally, people will see an increased involvement of the individual in everyday life activity "
                    + "areas and an improvement in exceptional medical and behavioral support need areas." },
                { 1.15, .34, "> Formally, people will see enhanced personal quality of life-related outcomes on one or more quality of life areas." },

        };

        private static readonly object[,] endOfReportSpecVA = {
            //x,dy,text
            { .5, .52, "<b>How Information from My Support Profile Can Be Used in Supports Planning Approaches</b>" },
            { .36, .4, "\tEveryone benefits from supports that allow them to take part in everyday life activities and "
                +"maintain a healthy lifestyle. The Supports Intensity Scale [Adult_Child] Version ([SIS-A_SIS-C]) assesses a person's pattern and intensity "
                +"of support needs across life activities and exceptional medical and behavioral support need areas. The "
                +"attached 'My Support Profile' summarizes information from the [SIS-A_SIS-C] that can be used in planning "
                +"for individuals based on their support needs and the individuals' life goals and personal interests." 
                + "Thus, the SIS informs the planning process and should be completed prior to the annual planning meeting." },
            { .36, .8, "\tPlanning for individuals requires the collective wisdom of a Support Team that is made up "
                +"of the individual, his/her parents or family members, a case manager "
                +"or supports coordinator, direct support staff who work with the individual, and one or more professionals "
                +"depending on the support needs. The purpose of this attachment to the 'My Support Profile' is "
                +"to provide answers to six questions asked frequently by the individual and his/her support team members "
                +"as collectively they engage in the development, implementation, and monitoring of the individual's support "
                +"planning." },
            { .68, .9, "<b>1. How do we determine what is important to the individual and what is important for the individual?" },
                { 1.15, .34, "> Identifying support needs that are <b>important to the individual</b> is based on the individual's goals, desires, and preferences " 
                             +" or what they may indicate or say in their own words. (or what he/she communicates to the Support Team - JTP)"},
                { 1.15, .52, "> Identifying support needs that are <b>important for the individual</b> is based on:" },
                    { 1.4, .2, "- higher support need scores from the 'My Support Profile' in the most relevant life activity areas" },
                    { 1.4, .17, "- needed supports to be healthy and safe" },
                    { 1.4, .17, "- interventions prescribed by a professional." },
            { .68, .31, "<b>2. How do we focus on the whole person and the individual's quality of life?" },
                { 1.15, .34, "> The concept of quality of life reflects a holistic approach to an individual and includes areas that are valued by all persons." },
                { 1.15, .52, "> Eight core quality of life areas reflect this holistic approach:" },
                    { 1.4, .2, "- Personal Development" },{ 3.37, 0.0, "- Self-determination" },{ 5.11, 0.0, "- Interpersonal Relations" },
                    { 1.4, .17, "- Social Inclusion" },    { 3.37, 0.0, "- Rights" },            { 5.11, 0.0, "- Emotional Well-being" },
                    { 1.4, .17, "- Physical Well-being" }, { 3.37, 0.0, "- Material Well-being" },
                { 1.15, .35, "> These eight quality of life areas can be used to develop an ISP." },
            { .68, .36, "<b>3. What are the responsibilities of support team members?" },
                { 1.15, .34, "> Determine <b>what is important to and for</b> the individual" },
                { 1.15, .17, "> Identify specific support strategies to address the individual's personal goals and assessed support needs" },
                { 1.15, .34, "> Specify a specific support outcome for each support strategy and indicate who is responsible for implementing each support strategy. " 
                             + "Develop specific instructions for the direct support staff." },
                { 1.15, .34, "> Implement and monitor the Individual Supports Plan" },

            { .68, .59, "<b>4. What supports can we use to enhance the individual's well-being?" },
                { 1.15, .34, "> Natural support resources (e.g. family, friends, and community resources)" },
                { 1.15, .17, "> Technology-based (e.g. assistive technology, information technology, smart technology, and prosthetics)" },
                { 1.15, .34, "> Environment-based (e.g. environmental accommodation)" },
                { 1.15, .17, "> Staff directed (e.g. incentives, skills/knowledge, and positive behavior supports)" },
                { 1.15, .17, "> Professional services (e.g. medical, psychological, therapeutic services)" },

            {0.0,0.0,"\f"},//pagebreak

            { .68, .4, "<b>5. How does information obtained from the [SIS-A_SIS-C] relate to professional recommendations?" },
                { 1.15, .34, "> Professional recommendations such as those from a doctor focus on lessening the impact of the individual's disability-related condition." },
                { 1.15, .34, "> SIS information focuses on the supports an individual needs in order to be more successful in everyday life activities and have a life like ours." },
                { 1.15, .34, "> Both types of information need to be a part of planning supports for individuals." },

            { .68, .4, "<b>6. How do we know if the supports provided have an effect on the individual?" },
                { 1.15, .34, "> Informally, people will see an increased involvement of the individual in everyday life activity "
                    + "areas and an improvement in exceptional medical and behavioral support need areas." },
                { 1.15, .34, "> Formally, people will see enhanced personal quality of life-related outcomes on one or more quality of life areas." },

        };

        public void PrintEndOfReport(PdfOutput output)
        {
            if (SessionHelper.LoginStatus.EnterpriseID == 44)
            { // Enterprise is Virginia 
                output.appendPageBreak();
                for (int i = 0; i < endOfReportSpecVA.Length / 3; i++)
                {
                    if ("\f".Equals((string)endOfReportSpecVA[i, 2]))
                    {
                        output.appendPageBreak();
                        continue;
                    }
                    output.drawY -= (double)endOfReportSpecVA[i, 1];

                    //output.appednWrappedText() will incriment output.drawY, but we want to do this manually
                    double temp = output.drawY;

                    output.appendWrappedText(ReplaceSISReservedWords((string)endOfReportSpecVA[i, 2]),
                        (double)endOfReportSpecVA[i, 0],
                        Math.Max(3, 8.1 - (double)endOfReportSpecVA[i, 0] * 2), 9);

                    output.drawY = temp;
                }
            } else {
            
                output.appendPageBreak();
                for (int i = 0; i < endOfReportSpec.Length / 3; i++ )
                {
                    if ("\f".Equals((string)endOfReportSpec[i, 2]))
                    {
                        output.appendPageBreak();
                        continue;
                    }
                    output.drawY -= (double)endOfReportSpec[i,1];

                    //output.appednWrappedText() will incriment output.drawY, but we want to do this manually
                    double temp = output.drawY;

                    output.appendWrappedText(ReplaceSISReservedWords((string)endOfReportSpec[i, 2]), 
                        (double)endOfReportSpec[i, 0], 
                        Math.Max( 3, 8.1 - (double)endOfReportSpec[i, 0]*2 ), 9);

                    output.drawY = temp;
                }
            }
        }

        private void PrintImportantToItemsAndNotes(PdfOutput output, Dictionary<string, List<def_Sections>> sectionsByLabel )
        {
            AppendPartHeader(output, "Most Important To the Individual");
            PrintImportantItemsAndNotes(output, "ImportantTo", sectionsByLabel );
        }

        private void PrintImportantForItemsAndNotes(PdfOutput output, Dictionary<string, List<def_Sections>> sectionsByLabel )
        {
            AppendPartHeader(output, "Most Important For the Individual");
            PrintImportantItemsAndNotes(output, "ImportantFor", sectionsByLabel );
        }

        private Dictionary<string, List<def_Sections>> getImportantToForSectionsByLabel()
        {
            //compile a list of bottom-level sections with potentially important items
            //NOTE: we could compile a list of IDs to make this step more efficient, but overall it doesn't 
            //matter because later in this function all those sections need to be pulled from db
            Dictionary<string, List<def_Sections>> sectionsByLabel = new Dictionary<string, List<def_Sections>>();
            def_Parts prt;
            def_Sections sct;
            string label;

            List<string> partIdentifiers = new List<string>();
            if (form.identifier.Equals("SIS-A"))
                partIdentifiers.Add("Section 3. Supplemental Protection and Advocacy Scale");
            partIdentifiers.Add("Section 2. Supports Needs Index");

            foreach (string partIdent in partIdentifiers)
            {
                prt = formsRepo.GetPartByFormAndIdentifier(form, partIdent);
                formsRepo.GetPartSections(prt);
                foreach (def_PartSections ps in prt.def_PartSections)
                {
                    sct = formsRepo.GetSectionById(ps.sectionId);
                    if (partIdent.StartsWith("Section 3."))
                    {
                        label = "Section 3";
                    }
                    else
                    {
                        string sectionLabel = sct.title;
                        int i = sectionLabel.IndexOf('.');
                        if (i > 0)
                        {
                            sectionLabel = sectionLabel.Substring(0, i);
                        }
                        label = "Section " + sectionLabel;
                    }
                    if (!sectionsByLabel.ContainsKey(label))
                        sectionsByLabel.Add(label, new List<def_Sections>());
                    sectionsByLabel[label].Add(sct);
                }
            }
            prt = formsRepo.GetPartByFormAndIdentifier(form, "Section 1. Exceptional Medical and Behavioral Support Needs");
            formsRepo.GetPartSections(prt);
            foreach (def_PartSections ps in prt.def_PartSections)
            {
                def_Sections parentSection = formsRepo.GetSectionById(ps.sectionId);
                label = parentSection.title;
                int i = label.IndexOf(':');
                if (i > 0)
                {
                    label = label.Substring(0, i);
                }
                foreach (def_SectionItems si in formsRepo.GetSectionItemsBySectionId(ps.sectionId))
                {
                    if (si.subSectionId.HasValue)
                    {
                        sct = formsRepo.GetSubSectionById(si.subSectionId.Value);
                        if (!sectionsByLabel.ContainsKey(label))
                            sectionsByLabel.Add(label, new List<def_Sections>());
                        sectionsByLabel[label].Add(sct);
                    }
                }
            }

            return sectionsByLabel;
        }

        private bool HasImportantResponses(string ivSuffix, Dictionary<string, List<def_Sections>> sectionsByLabel )
        {
            ivSuffix = ivSuffix.ToLower();

            //iterate through all the sections, appending important items to output
            foreach (string sectionLabel in sectionsByLabel.Keys)
            {
                foreach (def_Sections section in sectionsByLabel[sectionLabel])
                {
                    foreach (def_SectionItems si in formsRepo.GetSectionItemsBySectionIdEnt(section.sectionId, SessionHelper.LoginStatus.EnterpriseID))
                    {
                        List<def_ItemVariables> ivList = formsRepo.GetItemVariablesByItemId(si.itemId);

                        def_ItemVariables iv = ivList.Where(ivt => ivt.identifier.ToLower().EndsWith(ivSuffix)).FirstOrDefault();
                        if (iv == null)
                            continue;

                        def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(formResultId, iv.itemVariableId);
                        if ((rv != null) && (rv.rspValue != null) && rv.rspValue.Equals("1"))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void PrintImportantItemsAndNotes(PdfOutput output, string ivSuffix, Dictionary<string, List<def_Sections>> sectionsByLabel)
        {
            ivSuffix = ivSuffix.ToLower();

            //iterate through all the sections, appending important items to output
            foreach (string sectionLabel in sectionsByLabel.Keys)
            {
                foreach (def_Sections section in sectionsByLabel[sectionLabel])
                {
                    foreach (def_SectionItems si in formsRepo.GetSectionItemsBySectionIdEnt(section.sectionId, SessionHelper.LoginStatus.EnterpriseID))
                    {
                        List<def_ItemVariables> ivList = formsRepo.GetItemVariablesByItemId(si.itemId);

                        def_ItemVariables iv = ivList.Where(ivt => ivt.identifier.ToLower().EndsWith(ivSuffix)).FirstOrDefault();
                        if (iv == null)
                            continue;

                        def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(formResultId, iv.itemVariableId);
                        if ((rv != null) && (rv.rspValue != null) && rv.rspValue.Equals("1"))
                        {
                            output.appendSectionBreak();
                            PrintImportantRow(output, sectionLabel, si, ivList);
                        }
                    }
                }
            }
        }

        private void PrintImportantRow( PdfOutput output, string partSectionLabel, def_SectionItems si, List<def_ItemVariables> ivList )
        {

            //print part label, section label, item number
            output.drawY -= .1;
            double sectionLabelWidth = output.DrawText(output.contentFont, output.fontSize, importantRowX[0], output.drawY,
                partSectionLabel + ", Item " + si.order + ": ");

            //print item label
            double itemLabelX = Math.Max(importantRowX[1], importantRowX[0] + sectionLabelWidth);
            string itemLabel = si.def_Items.label;
            if (itemLabel.Length > 60)
            {
                itemLabel = itemLabel.Substring(0, 60) + "...";
            };
            output.DrawText(output.contentFont, output.fontSize, itemLabelX, output.drawY, itemLabel );
            
            //print support needs scale responses (this does nothing for section 1 items)
            //print exceptional medical/behavioral support needs response (this does nothing fro section 2+3 items)
            int i = -1;
            foreach( string suffix in importantIvSuffixes )
            {
                def_ItemVariables iv = ivList.FirstOrDefault(ivt=>ivt.identifier.EndsWith(suffix));
                if (iv == null)
                {
                    continue;
                }else{
                    i++;
                }
                def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(formResultId, iv.itemVariableId);
                String s = "N/A";
                if ((rv != null) && (rv.rspValue != null))
                {
                    s = rv.rspValue;
                }
                output.DrawText(output.contentFont, output.fontSize, importantRowX[i + 2], output.drawY, s);
            }
            
            //print item notes
            output.drawY -= .12;
            def_ResponseVariables notesRv = formsRepo.GetResponseVariablesByFormResultItemVarId(formResultId,
                    ivList.FirstOrDefault(iv => iv.identifier.EndsWith("_Notes")).itemVariableId);
            string notes = "Notes: ";
            if ((notesRv != null) && (notesRv.rspValue != null))
                notes += notesRv.rspValue;
            double height = output.DrawWrappedText(output.contentFont, output.fontSize, importantRowX[0], output.drawY, 8-importantRowX[0], notes);
            output.drawY -= height;
        }

        private void PrintSupportNeeds( PdfOutput output, bool includeComments )
        {
            List<string> partIdentifiers = new List<string>();
            partIdentifiers.Add( "Section 2. Supports Needs Index" );
            if (form.identifier.Equals("SIS-A"))
                partIdentifiers.Add( "Section 3. Supplemental Protection and Advocacy Scale" );

            bool firstPart = true;
            int headerColorIndex = 0;

            foreach (string partIdent in partIdentifiers) 
            {
                def_Parts part = formsRepo.GetPartByFormAndIdentifier(form, partIdent);

                if (firstPart)
                    firstPart = false;
                else
                    output.appendPageBreak();

                AppendPartHeader( output, part.identifier);
                bool firstSection = true;
                //formsRepo.GetPartSections(part);
                List<def_Sections> sections = formsRepo.GetSectionsInPart(part);

                foreach (def_Sections sctn in sections)
                {
                    if (firstSection)
                        firstSection = false;
                    else
                        output.appendPageBreak();
                    PrintSISTablePage(output, sctn, includeComments, headerColorIndex++ );
                }
            }
        }

        private void PrintExceptionalSupportNeeds(PdfOutput output )
        {
            def_Parts part = formsRepo.GetPartByFormAndIdentifier(form, "Section 1. Exceptional Medical and Behavioral Support Needs");
            BuildExceptionalSupportNeedsKey(output);
            foreach (def_Sections sctn in formsRepo.GetSectionsInPart(part))
            {
                output.appendPageBreak();
                PrintExceptionalNeedsTable(output, sctn);
            }
        }

        private static readonly object[,] exceptNeedsSpecSisA = {
            //x,y,wrapwidth,text
            {  .43, 1.06, 2.20, "Type of Support" },
            {  .43, 1.36, 2.20, "<b>0 = No Support Needed</b>" },
            { 2.87, 1.36, 2.20, "<b>1 = Some Support Needed</b>" },
            { 5.29, 1.36, 2.20, "<b>2 = Extensive Support Needed</b>" },
            {  .43, 1.68, 2.20, "No support needed because the medical condition or behavior is not an issue, or no support is needed to manage the medical condition or behavior." },
            { 2.87, 1.68, 2.20, "Support is needed to address the medical condition and/or behavior. People who support must be cognizant continuously of the condition to assure the individual's health and safety." },
            { 2.87, 2.62, 2.20, "For example:" },
            { 2.87, 2.77, 2.20, "Checking in and observing" },
            { 2.87, 2.94, 2.20, "Monitoring and providing occasional assistance" },
            { 2.87, 3.26, 2.20, "Minimal physical/hands on contribution" },
            { 2.87, 3.42, 2.20, "Support is episodic and/or requires minimal devoted support time" },
            { 5.29, 1.68, 2.20, "Extensive support is needed to address the medical condition and/or behavior." },
            { 5.29, 2.16, 2.20, "For example:" },
            { 5.29, 2.32, 2.20, "Significant physical/hands on contribution" },
            { 5.29, 2.63, 2.20, "Support is intense and/or requires significant support time" },
            {  .31, 4.34, 7.38, "Any rating of 2 in this area indicates an exceptional need with Medical conditions and/or Behaviors." },
            {  .31, 4.66, 7.38, "It should be noted that a high total score in section 1 clearly identifies additional support that is required for living safely in the community. The information from section 1 is considered separately from section 2." },
            {  .31, 5.13, 7.38, "Each item under Exceptional Medical and Behavioral is listed and presented from highest to lowest level of support." },
            {  .31, 5.45, 7.38, "Exceptional Medical and Behavioral key items are outlined and may be helpful in the development of the individual's support plan." },
        };

        private static readonly object[,] exceptNeedsSpecSisC = {
            //x,y,wrapwidth,text
            {  .43, 1.06, 2.20, "Type of Support" },
            {  .43, 1.36, 2.20, "<b>0 = No Support Needed</b>" },
            { 2.87, 1.36, 2.20, "<b>1 = Some Support Needed</b>" },
            { 5.29, 1.36, 2.20, "<b>2 = Extensive Support Needed</b>" },
            {  .43, 1.68, 2.20, "No support needed because the medical condition or behavior is not an issue, or no support is needed to manage the medical condition or behavior." },
            { 2.87, 1.68, 2.20, "Support is needed to address the medical condition and/or behavior. People who support must be cognizant continuously of the condition to assure the individual's health and safety." },
            { 2.87, 2.62, 2.20, "For example:" },
            { 2.87, 2.77, 2.20, "Checking in and observing" },
            { 2.87, 2.94, 2.20, "Monitoring and providing occasional assistance" },
            { 2.87, 3.26, 2.20, "Minimal physical/hands on contribution" },
            { 2.87, 3.42, 2.20, "Support is episodic and/or requires minimal devoted support time" },
            { 5.29, 1.68, 2.20, "Extensive support is needed to address the medical condition and/or behavior." },
            { 5.29, 2.16, 2.20, "For example:" },
            { 5.29, 2.32, 2.20, "Significant physical/hands on contribution" },
            { 5.29, 2.63, 2.20, "Support is intense and/or requires significant support time" },
            {  .31, 4.34, 7.38, "Any rating of 2 in this area indicates an exceptional need with Medical conditions and/or Behaviors." },
            {  .31, 4.66, 7.38, "It should be noted that a high total score in section 1 clearly identifies additional support that is required for living safely in the community. The information from section 1 is considered separately from section 2." },
            {  .31, 5.13, 7.38, "Each item under Exceptional Medical and Behavioral is listed and presented from highest to lowest level of support." },
            {  .31, 5.45, 7.38, "Exceptional Medical and Behavioral key items are outlined and may be helpful in the development of the individual's support plan." },
        };

        private void BuildExceptionalSupportNeedsKey(PdfOutput output)
        {
            object[,] exceptNeedsSpec;

            //find which assessment type and use appropriate text
            if (form.identifier == "SIS-A")
                exceptNeedsSpec = exceptNeedsSpecSisA;
            else
                exceptNeedsSpec = exceptNeedsSpecSisC;

            //draw yellow highlight 
            output.SetColor(options[OptionKey.grayscale] ? Color.LightGray : Color.Yellow);
            output.FillRectangle(.3, PdfOutput.pageHeight - 5.7, 7.3, 1.4);
            output.SetColor(Color.Black);

            //draw text
            AppendPartHeader( output, "Rating Key For Section 1");
            for (int i = 0; i < (exceptNeedsSpec.Length / 4); i++)
            {
                output.drawY = 11.0 - (double)exceptNeedsSpec[i, 1];
                output.appendWrappedText((string)exceptNeedsSpec[i, 3],
                    (double)exceptNeedsSpec[i, 0],
                    (double)exceptNeedsSpec[i, 2], 9);
            }

            //draw lines
            output.OutlineRectangle(.35, PdfOutput.pageHeight - 3.9, 7.25, 2.96);
            output.OutlineRectangle( .35, PdfOutput.pageHeight - 1.59, 7.25,  .33);
            output.OutlineRectangle(2.78, PdfOutput.pageHeight - 3.9, 2.43, 2.64);
        }

        /*
         * Prints the Exceptional Support Needs questions.
         */
        private void PrintExceptionalNeedsTable(PdfOutput output, def_Sections section)
        {
            // Populate a list of items corresponding to rows in the output also find the Page Notes item
            List<def_Items> itms = new List<def_Items>();
            def_Items pageNotesItem = null;
            formsRepo.SortSectionItems(section);
            foreach (def_SectionItems si in section.def_SectionItems)
            {
                def_Sections subSct = formsRepo.GetSubSectionById(si.subSectionId);
                formsRepo.SortSectionItems(subSct);
                foreach (def_SectionItems ssi in subSct.def_SectionItems)
                {
                    def_Items itm = formsRepo.GetItemById(ssi.itemId);
                    if (itm.identifier.EndsWith("PageNotes_item"))
                        pageNotesItem = itm;
                    else if (ssi.subSectionId == null) //skip tracking number
                        itms.Add(itm);
                }
            }

            // Get general comment from the page notes item
            string generalComments = String.Empty;
            if (pageNotesItem != null)
            {
                def_ItemResults itemResults = pageNotesItem.def_ItemResults.SingleOrDefault(ir => ir.formResultId == formResultId);
                if (itemResults != null)
                {
                    formsRepo.GetItemResultsResponseVariables(itemResults);
                    //def_ResponseVariables responseVariable = itemResults.def_ResponseVariables.FirstOrDefault();
                    def_ResponseVariables responseVariable = itemResults.def_ResponseVariables.SingleOrDefault(r => r.def_ItemVariables.identifier.EndsWith("PageNotes"));
                    if (responseVariable != null)
                    {
                        string responseValue = responseVariable.rspValue;
                        if (responseValue != null)
                        {
                            generalComments = responseValue;
                        }
                    }
                }
            }

            // Populate a list of table values, skipping items without scores
            List<def_Items> skippedItems = new List<def_Items>();
            List<string[]> valueList = new List<string[]>();
            for (int itemIndex = 0; itemIndex < itms.Count ; itemIndex++)
            {

                //pull item label and all responses
                def_Items itm = itms[itemIndex];
                def_ItemResults itmResults = formsRepo.GetItemResultByFormResItem(formResultId, itm.itemId);
                if (itmResults == null)
                {
                    skippedItems.Add(itm);
                    continue;
                }
                formsRepo.GetItemResultsResponseVariables(itmResults);
                foreach (def_ResponseVariables rspVar in itmResults.def_ResponseVariables)
                {
                    rspVar.def_ItemVariables = formsRepo.GetItemVariableById(rspVar.itemVariableId);
                }
                def_ResponseVariables rv;

                //build string values for each cell in this item's row
                string[] rowVals = new string[3];
                
                //first column (label)
                if (itm.label.StartsWith("Other"))
                {
                    bool isSisCAssessment = form.identifier.Equals("SIS-C");
                    string ivIdent;
                    if (isSisCAssessment)
                    {
                        ivIdent = itm.identifier.Replace("i_", "").Replace("SIS-C_", "SIS-C_Q") + "_Other";
                    }
                    else
                    {
                        ivIdent = "Q" + itm.identifier.Replace("i_", "") + "_Other";
                    } 
                    rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, ivIdent);
                    rowVals[0] = (itemIndex + 1).ToString() + ". " + itm.label + " : " + ( rv == null ? "" : rv.rspValue );
                }
                else
                {
                    rowVals[0] = (itemIndex + 1).ToString() + ". " + itm.label;
                }

                //second column (score)
                rv = itmResults.def_ResponseVariables.SingleOrDefault(r => r.def_ItemVariables.identifier.EndsWith("Support"));
                if (rv == null || String.IsNullOrEmpty(rv.rspValue))
                {
                    skippedItems.Add(itm);
                    continue;
                }
                rowVals[1] = rv.rspValue + leadZeros( 4, (100-itemIndex).ToString() ); //<-trick to make the sorting work properly (score descending, then item number ascending)

                //third column (notes)
                rv = itmResults.def_ResponseVariables.SingleOrDefault(r => r.def_ItemVariables.identifier.EndsWith("_Notes"));
                rowVals[2] = (rv == null) ? String.Empty : rv.rspValue; 

                valueList.Add(rowVals);
            }

            //transfer value list to an array with an extra blank row
            int nRows = valueList.Count() + 1;
            string[][] values = new string[nRows][];
            for (int i = 0; i < nRows-1 ; i++)
                values[i] = valueList[i];

            // Temorarily populate the last row with a negative score so that it stays at the bottom after sorting
            values[nRows - 1] = new string[] { "Total Score", "-1", "" };

            // Sort the values
            Array.Sort(values, delegate(string[] a, string[] b)
            {
                return ForceIntParse(b[1]) - ForceIntParse(a[1]);
            });

            // Insert score numonics, totalScore
            int totalScore = 0;
            for ( int row = 0 ; row < (nRows - 1); row++ )
            {
                string[] rowVals = values[row];
                try
                {
                    int score = (rowVals[1] == null) ? 0 : Int32.Parse(rowVals[1])/10000;
                    if( score > -1 )
                    {
                        rowVals[1] = score + " - " + exceptNumonics[score];
                        totalScore += score;
                    }
                }
                catch (System.FormatException e)
                {
                    Debug.Print("could not parse integer rom table cell with value \"" + rowVals[1] + "\"");
                }

                if (rowVals[1].Equals("-1"))
                {
                    rowVals[1] = "N/A";
                }
            }
            values[nRows - 1] = new string[]{ "Total Score", totalScore.ToString(), "" };

            Color highlightColor = options[OptionKey.grayscale] ? Color.LightGray : Color.FromArgb(0, 255, 255);
            PrintFamilyTable(output, highlightColor, section.title,
                new string[] { "Item", "Support Needed", "Comments" }, values, .3, false, generalComments);

            //special case for assessments migrated from the old system (missing items 16,17,18)
            bool migrated = true;
            for( int itemNumber = 16 ; itemNumber <= 18 ; itemNumber++ )
                if( !skippedItems.Any( i => i.identifier.Equals( "i_1A" + itemNumber ) ) )
                    migrated = false;
            if (migrated)
            {
                output.drawY -= .2;
                output.appendWrappedText("NOTE: Responses not available for items 16, 17, and 18. " +
                    "This is because this assessment was created in the 2004 version of SIS, which did not include those items.", .34, 8);
            }
        }

        private string leadZeros(int len, string s)
        {
            while (s.Length < len)
                s = "0" + s;
            return s;
        }

        private int ForceIntParse(string s)
        {
            int result = 0;
            try
            {
                result = Int16.Parse(s);
            }
            catch (Exception e) { }
            return result;
        }

        private void PrintSISTablePage(PdfOutput output, def_Sections section, bool includeComments, int headerColorIndex)
        {
            // Retrieve the list of section items
            List<def_SectionItems> siList = formsRepo.GetSectionItemsBySectionIdEnt(section.sectionId, SessionHelper.LoginStatus.EnterpriseID);

            // Remove section items that correspond to subsections (would appear as tracking number bug)
            siList.RemoveAll( si => (si.subSectionId != null) );

            // Populate an array of table values
            int row = -1, nRows = siList.Count();
            string[][] values = new string[nRows][];

            //iterate through each item (row)
            foreach (def_SectionItems sctnItm in siList )
            {
                row++;

                //in the end result, each row will have 5 columns,
                //here a sixth column is included to be populated with notes for each item
                //this way the notes will be soted correctly before they are transfered to a separate array
                values[row] = new string[6];

                //retrieve the item results to populate this row with
                def_ItemResults itmResults = formsRepo.GetItemResultByFormResItem(formResultId, sctnItm.itemId);
                if (itmResults == null)
                    continue;
                formsRepo.GetItemResultsResponseVariables(itmResults);
                foreach (def_ResponseVariables rspVar in itmResults.def_ResponseVariables)
                {
                    rspVar.def_ItemVariables = formsRepo.GetItemVariableById(rspVar.itemVariableId);
                }

                //populate first column with the item label
                def_Items itm = formsRepo.GetItemById(sctnItm.itemId);
                values[row][0] = (row + 1).ToString() + ". " + itm.label;

                //populate the second through fifth columns with scores
                int? totalScore = 0;
                for (int i = 0; i < 3; i++)
                {
                    string val = "N/A";
                    string suffix = SupportNeedsColumnSuffixes[i];
                    try
                    {
                        //Debug.WriteLine("\titemvariableId= " + ivEnum.Current.itemVariableId);
                        val = itmResults.def_ResponseVariables.SingleOrDefault(rv => rv.def_ItemVariables.identifier.EndsWith(suffix)).rspValue;
                    }
                    catch (System.NullReferenceException ex)
                    {
                        Debug.WriteLine("for itemId " + itm.itemId + ", could not find responseVariable linked to itemVariableIdentifier with suffix \"" + suffix + "\"");
                        Debug.WriteLine("   NullReferenceException: " + ex.Message);
                    }
                    catch (Exception xcptn)
                    {
                        Debug.WriteLine("for itemId " + itm.itemId + ", could not find responseVariable linked to itemVariableIdentifier with suffix \"" + suffix + "\"");
                        Debug.WriteLine("   Exception: " + xcptn.Message);
                    }

                    if (totalScore != null)
                    {
                        try
                        {
                            int intTest;
                            if (int.TryParse(val, out intTest))
                            {
                                totalScore += UInt16.Parse(val);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Print("failed to parse integer from respnse value: \"" + val + "\"   Msg: " + ex.Message);
                            totalScore = null;
                        }
                    }
                    values[row][i + 1] = val;
                }
                values[row][4] = (totalScore == null) ? "-1" : totalScore.ToString();

                //populate the sixth column with item notes
                //as mentioned above, the sixth column will be eventually transfered to a separate array
                if (includeComments)
                {
                    try
                    {
                        values[row][5] = itmResults.def_ResponseVariables.SingleOrDefault(rv => rv.def_ItemVariables.identifier.EndsWith("Notes")).rspValue;
                    }
                    catch (NullReferenceException e) {}
                }
            }

            //sort the values, then insert numonics
            Array.Sort(values, delegate(string[] a, string[] b)
            {
                int ia = 0, ib = 0;
                try
                {
                    ia = Int16.Parse(a[4]);
                }
                catch (Exception e) { };

                try
                {
                    ib = Int16.Parse(b[4]);
                }
                catch (Exception e) { };

                return ib - ia;
            });

            foreach (string[] rowVals in values)
            {
                if (rowVals == null)
                    continue;

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        int rating = Int16.Parse(rowVals[i + 1]);

                        if (form.identifier.Equals("SIS-A"))
                            rowVals[i + 1] = rating + " - " + ratingNumonicsSisA[i, rating];

                        if (form.identifier.Equals("SIS-C"))
                            rowVals[i + 1] = rating + " - " + ratingNumonicsSisC[i, rating];

                    }
                    catch (System.FormatException e ) {
                        Debug.Print("could not parse integer from table cell with value \"" + rowVals[i + 1] + "\"");
                        Debug.Print("   FormatException: " + e.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("could not parse integer from table cell with value \"" + rowVals[i + 1] + "\"    Exception Msg: " + ex.Message);
                    }
                }

                if ( rowVals[4] == null || rowVals[4].Equals("-1"))
                    rowVals[4] = "N/A";
            }

            //separate the 6-column array into two arrays,
            //one 2D array with 5 columns, and
            //one 1D array with the notes for each row
            string[][] tableValues = new string[nRows][]; // 1 less because of page notes, needs better solution
            string[] rowComments = new string[nRows];
            string generalComments = string.Empty;
            for (row = 0; row < nRows; row++)
            {
                if (values[row] == null)
                    continue;

                tableValues[row] = new string[5];

                for (int col = 0; col < 5; col++)
                {
                    // don't treat page notes like an item row
                    if (values[row][0] != null && values[row][0].Contains("Page Notes"))
                    {
                        generalComments = values[row][5];
                        //tableValues[row][0] = "Page Notes";
                    }
                    else
                    {
                        tableValues[row][col] = values[row][col];
                        rowComments[row] = values[row][5];
                    }
                }
            }

            //pick a header color and append the table to the output
            Color headerCol;
            if (options[OptionKey.grayscale])
            {
                headerCol = Color.LightGray;
            }
            else
            {
                headerColorIndex = headerColorIndex % (section2HeaderColors.Length / 3);
                headerCol = Color.FromArgb(
                    section2HeaderColors[headerColorIndex, 0],
                    section2HeaderColors[headerColorIndex, 1],
                    section2HeaderColors[headerColorIndex, 2]);
            }
            PrintFamilyTable(output, headerCol, section.title,
                new string[] { "Item", "Type of Support", "Frequency", "Daily Support Time", "Total Score" }, 
                tableValues, .45, true, rowComments, generalComments );
        }

        private readonly static double[] dx5 = { .33, 3.23, 4.81, 6.24, 7.54, 8.17 };
        private readonly static double[] dx3 = { .31, 3.24, 4.71, 8.19 };
        private Data.Concrete.FormsRepository formsRepo1;
        private string logoPath;
        private string outpath;
        //private readonly static double[] dx3 = { .31, 3.46, 5.36, 7.68 };

        private void PrintFamilyTable(PdfOutput output, Color headerCol, string title,
                string[] headers, string[][] values, double vSpace, bool fixedSpacing, string generalComments)
        {
            PrintFamilyTable(output, headerCol, title, headers, values, vSpace, fixedSpacing, new string[values.Length], generalComments);
        }


        private void PrintFamilyTable(PdfOutput output, Color headerCol, string title,
                string[] headers, string[][] values, double vSpace, bool fixedSpacing, string[] rowComments, string generalComments)
        {

            //init table object
            int colCount = headers.Count();
            double[] dx = (colCount == 5) ? dx5 : dx3;
            PdfTable table = new PdfTable( headers.Length, dx );

            //add title
            Color titleBackground = colCount == 5 ? headerCol : Color.White;
            table.addMergedRow(title, output.boldFont, 12, titleBackground, .3);

            //add headers
            table.addRow(headers, output.boldFont, 12, Color.LightGray, colCount == 5 ? -1 : .3 );

            //add response rows
            for( int rowIndex = 0 ; rowIndex < values.Length ; rowIndex++ ) {

                // Highlight the first four rows, for exceptional needs tables
                Color background = Color.White;
                if (colCount == 3 && rowIndex < 4)
                {
                    background = headerCol;
                }

                table.addRow(values[rowIndex], output.contentFont, output.fontSize, background);

                //add comments row if applicable
                if (rowComments != null && !String.IsNullOrWhiteSpace( rowComments[rowIndex] ) )
                    table.addMergedRow(rowComments[rowIndex], output.contentFont, output.fontSize, Color.White );
            }

            //add general comments if applicable
            if (!String.IsNullOrWhiteSpace(generalComments))
            {
                table.addMergedRow("General Comments:  " + generalComments, 
                    output.contentFont, output.fontSize, Color.White);
            }
            
            //output table
            table.printTable(output);
        }
        //    if ( (headers.Count() != 5) && (headers.Count() != 3))
        //        throw new Exception("parameter \"headers\" must contain exactly five or three elements, found " + headers.Count() );
        //    int colCount = headers.Count();
        //    foreach (string[] row in values)
        //    {
        //        if ( (row != null) && (row.Count() != headers.Count() ) )
        //            throw new Exception("parameter \"values\" must have exactly" + colCount + " elements per row, found row with " + row.Count());
        //    }

        //    // Compute basic drawing params
        //    double[] dx = (colCount == 5) ? dx5 : dx3;
        //    //double vSpace = colCount == 5 ? .5 : .3;
        //    double w = dx[dx.Length - 1] - dx[0];

        //    //draw title header
        //    output.drawY -= .8;
        //    output.SetColor(headerCol);
        //    output.FillRectangle(dx[0], output.drawY, w, .25 );
        //    output.SetColor(Color.Black);
        //    output.DrawText(output.boldFont, 12, dx[0]+.08 , output.drawY+.07, TextJustify.Left, title);
        //    output.OutlineRectangle(dx[0], output.drawY, w, .25 );

        //    //draw column headers
        //    output.drawY -= vSpace;
        //    output.SetColor(Color.LightGray);
        //    output.FillRectangle(dx[0], output.drawY, w, vSpace);
        //    output.drawY += vSpace;
        //    output.SetColor(Color.Black);
        //    for (int col = 0; col < colCount; col++)
        //    {
        //        output.DrawWrappedText(output.boldFont, 12, dx[col]+.08, output.drawY-.02, dx[col + 1] - dx[col], TextJustify.Center, headers[col] );
        //    }
        //    output.OutlineRectangle(dx[0], output.drawY - vSpace, w, vSpace);
        //    output.SetColor(Color.Black);
        //    for (int i = 0; i < dx.Length; i++)
        //        output.DrawLine(dx[i], output.drawY, dx[i], output.drawY - vSpace, .02);

        //    double startY = output.drawY;
        //    double prevY = startY;

        //    //draw horizontal lines, values
        //    //-1 to skip the page notes at end (needs better solution so page notes don't get treated as items), page notes already have general comments logic below
        //    for (int row = 0; row < values.Count()-1; row++)
        //    {
        //        double y = output.drawY;
        //        output.drawY -= vSpace;
        //        string[] rowVals = values[row];
        //        double maxHeight = 0;
        //        for (int col = 0; col < colCount; col++)
        //        {
        //            double height = output.DrawWrappedText(output.contentFont, output.fontSize, dx[col] + .08, output.drawY - .07, dx[col + 1] - dx[col] - .16, TextJustify.Center, rowVals == null ? "N/A" : rowVals[col]);
        //            if (height > maxHeight)
        //                maxHeight = height;
        //        }
        //        if (!fixedSpacing)
        //        {
        //            output.drawY -= maxHeight;
        //        }

        //        // Highlight the first four rows, for exceptional needs tables
        //        if ((colCount == 3) && (row < 4))
        //        {
        //            output.SetColor(options.grayscale ? Color.LightGray : Color.FromArgb(0, 255, 255));
        //            output.FillRectangle(dx[0], output.drawY - vSpace, dx[dx.Length - 1] - dx[0], y - output.drawY - .01);
        //            output.SetColor(Color.Black);
        //            for (int col = 0; col < colCount; col++)
        //                output.DrawWrappedText(output.contentFont, output.fontSize, dx[col] + .08, y - vSpace - .07, dx[col + 1] - dx[col] - .16, TextJustify.Center, rowVals == null ? "N/A" : rowVals[col]);
        //        }

        //        //draw vertical lines
        //        output.SetColor(Color.Black);
        //        for (int i = 0; i < dx.Length; i++)
        //            output.DrawLine(dx[i], output.drawY - vSpace, dx[i], y - vSpace, .02);

        //        //draw horizontal line
        //        output.DrawLine(dx[0], output.drawY - vSpace, dx[0] + w, output.drawY - vSpace, .02);

        //        //add an aditional row for comments, if applicable
        //        if (rowComments[row] != null && rowComments[row].Trim().Length > 0)
        //        {
        //            output.SetColor(Color.Gray);
        //            output.DrawLine(dx[0], output.drawY - vSpace, dx[0] + w, output.drawY - vSpace, .02);
        //            output.SetColor(Color.Black);

        //            y = output.drawY;
        //            output.drawY -= vSpace;
        //            double height = output.DrawWrappedText(output.contentFont, output.fontSize, dx[0] + .08, output.drawY - .07, dx[dx.Length - 1] - dx[0] - .16, TextJustify.Center, rowComments[row]);
        //            if (!fixedSpacing)
        //            {
        //                output.drawY -= height;
        //            }

        //            output.DrawLine(dx[0], output.drawY - vSpace, dx[0] + w, output.drawY - vSpace, .02);
        //        }
        //        else
        //        {
        //            output.DrawLine(dx[0], output.drawY - vSpace, dx[0] + w, output.drawY - vSpace, .02);
        //        }

        //        prevY = y;
        //    }

        //    double drawYBeforeGeneralComments = output.drawY;

        //    // Add general comments row, if applicable
        //    if ( !String.IsNullOrEmpty(generalComments) )
        //    {
        //        double y = output.drawY;
        //        output.drawY -= vSpace;
        //        double height = output.DrawWrappedText(output.contentFont, output.fontSize, dx[0] + .08, output.drawY - .07, dx[1] - dx[0] - .16, TextJustify.Center, "General Comments");
        //        output.drawY -= height;
        //        double x = dx[0];//fixedSpacing ? dx[1] : dx[0];
        //        height = output.DrawWrappedText(output.contentFont, output.fontSize, x + .08, output.drawY - .07, dx[dx.Length - 1] - x - .16, TextJustify.Center, generalComments, PdfOutput.pageHeight - 1);
        //        output.drawY -= height;
        //        output.DrawLine(dx[0], output.drawY - vSpace, dx[0] + w, output.drawY - vSpace, .02);
        //    }

        //    //draw vertical lines
        //    output.drawY -= vSpace;
        //    output.SetColor(Color.Black);
        //    for (int i = 0; i < dx.Length; i+=(dx.Length-1))
        //    {
        //        double x = dx[i];
        //        double y = output.drawY;
        //        if ((i > 1) && (i < (dx.Length - 1)) && (!String.IsNullOrEmpty(generalComments)))
        //        {
        //            y = drawYBeforeGeneralComments;
        //        }
        //        output.DrawLine(x, y, x, startY, .02);
        //    }
        //}
    }
}