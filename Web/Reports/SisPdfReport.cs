using AJBoggs.Sis.Domain;
using AJBoggs.Sis.Reports;
using Assmnts.Infrastructure;
using Data.Abstract;
using dotnetCHARTING;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using OptionKey = AJBoggs.Sis.Reports.SisPdfReportOptions.OptionKey;

//contains properties and helpers shared by all reports
namespace Assmnts.Reports
{
    public abstract class SisPdfReport : FormResultPdfReport
    {
        public static readonly int[,] section2HeaderColors = {
            { 211,  92,  86 },
            { 179, 209,   8 },
            {  92, 138, 251 },
            { 211, 106, 184 },
            { 209, 122,  51 },
            {  46, 161,  84 },
        };
        protected readonly SisPdfReportOptions options;

        public SisPdfReport(IFormsRepository formsRepo, int formResultId, SisPdfReportOptions options, string logoPath, string outputPath)
            : base(formsRepo, formResultId, outputPath, options[OptionKey.grayscale])
        {
            Debug.WriteLine("SisReport formResultId: " + formResultId.ToString());
            this.options = options;
            output.setBottomMargin(0);

            output.pageHeaderXPos = BuildPageHeaderXPos();
            output.pageHeaderText = BuildPageHeaderText();

            //pick a title based on report type, and adjust the pageHeaderText if necessary

            string title = string.Empty;
            if (options[OptionKey.includeHeaderTitle])
            {
                switch (options.type)
                {
                    case SisPdfReportOptions.ReportType.Family:
                        title = "Family-Friendly Report" +
                            " (" + form.identifier + ")";
                        break;

                    case SisPdfReportOptions.ReportType.IntDuration:
                        title = "SIS Interviewer Duration Report";
                        output.pageHeaderText = new string[]{
                        "(Note: times are in hours.)",
                        "Date created:",
                        DateTime.Now.ToString()
                    };
                        break;

                    default:
                        title = "Supports Intensity Scale Report" +
                            " (" + form.identifier + ")";
                        break;
                }
            }

            //add report header
            output.pageHeaderXPos = BuildPageHeaderXPos();
            output.appendTopHeader(title, logoPath, options.type != SisPdfReportOptions.ReportType.IntDuration, form.identifier);

            //add page number 1 as footer on first page
            //output.appendPageFooter();
        }

        public virtual string[] BuildPageHeaderText()
        {
            return new string[] { 
                GetSingleResponse("sis_cl_last_nm") + ", " + GetSingleResponse("sis_cl_first_nm"), 
                "SIS ID: " + formResults.formResultId,
                "Date SIS Completed: " + GetSingleResponse("sis_completed_dt") 
            };
        }

        public virtual double[] BuildPageHeaderXPos()
        {
            return new double[] { .34, 3.7, 6.5 };
        }

        protected void PrintProfilePage(PdfOutput output, int ageInYears)
        {
            List<def_Parts> prts = formsRepo.GetFormParts(form);
            def_Parts part = prts[0];

            PdfOutput secondCol = output.getSecondColumnBranch();
            output.drawY -= .3;
            if (SessionHelper.LoginStatus.EnterpriseID == 44)
            {
                buildSubheaderWithResults(output, part, "Person Being Assessed",
                "itmNmeLst", "nmeFrst", "nmeMdl", "sis_cl_lang_item", "itmGender", "adrlne", "adrcty",
                "sis_cl_st1", "sis_cl_zip1", "sis_cl_phone_item",
                "sis_cl_dob_dt1", new LabelValuePair("Age", ageInYears.ToString()), "trkNum", new LabelValuePair("Medicaid Number", MaskMedicaidNumber(GetSingleResponse(formsRepo.GetItemByIdentifier("sis_cl_medicaidNum1")))),
                new LabelValuePair("SSN", MaskSSN(GetSingleResponse(formsRepo.GetItemByIdentifier("itmSsn")))));
            }
            else
            {
                buildSubheaderWithResults(output, part, "Person Being Assessed",
                "itmNmeLst", "nmeFrst", "nmeMdl", "sis_cl_lang_item", "itmGender", "adrlne", "adrcty",
                "sis_cl_st1", "sis_cl_zip1", "sis_cl_phone_item",
                "sis_cl_dob_dt1", new LabelValuePair("Age", ageInYears.ToString()), "trkNum", "sis_cl_medicaidNum1",
                new LabelValuePair("SSN", MaskSSN(GetSingleResponse(formsRepo.GetItemByIdentifier("itmSsn")))));
            }
            
            buildSubheaderWithResults(output, part, "Assessment Data", "intvwDate", "isp_begin_date_item", new LabelValuePair("SIS ID", formResults.formResultId.ToString()));
            secondCol.drawY -= .6;
            buildSubheaderWithResults(secondCol, part, "Interviewer Data",
                "sis_int_full_nm1", "sis_int_agency_nm1", "sis_int_addr_line11", "sis_int_city1",
                "sis_int_st1", "sis_int_zip", "sis_int_position_cd1", "sis_int_phone_num1",
                "sis_int_phone_num_ext1", "sis_int_email1");
            output.drawY -= .3;
            output.appendSectionBreak();
            output.appendSubHeader("Support Providers", "Essential supports for this individual are being provided by the following");
            buildTableWithItems(output, part, 4,
                "sis_sup1_name_item", "sis_sup1_reln_item", "sis_sup1_phone_item", "sis_sup1_ext_item",
                "sis_sup2_name_item", "sis_sup2_reln_item", "sis_sup2_phone_item", "sis_sup2_ext_item",
                "sis_sup3_name_item", "sis_sup3_reln_item", "sis_sup3_phone_item", "sis_sup3_ext_item",
                "sis_sup4_name_item", "sis_sup4_reln_item", "sis_sup4_phone_item", "sis_sup4_ext_item",
                "sis_sup5_name_item", "sis_sup5_reln_item", "sis_sup5_phone_item", "sis_sup5_ext_item",
                "sis_sup6_name_item", "sis_sup6_reln_item", "sis_sup6_phone_item", "sis_sup6_ext_item");
            output.drawY -= .3;
            output.appendSectionBreak();
            output.appendSubHeader("Respondent Data", "Information for the SIS ratings was provided by the following respondents:");
            //buildTableWithItems(output, part, 5,
            //    new string[] { "First Name", "Last Name", "Agency", "Email", "Language", },
            //    "sis_res1_firstn_item", "sis_res1_lastn_item", "sis_res1_agen_item", "sis_res1_email_item", "sis_res1_lang_item",
            //    "sis_res2_firstn_item", "sis_res2_lastn_item", "sis_res2_agen_item", "sis_res2_email_item", "sis_res2_lang_item",
            //    "sis_res3_firstn_item", "sis_res3_lastn_item", "sis_res3_agen_item", "sis_res3_email_item", "sis_res3_lang_item",
            //    "sis_res4_firstn_item", "sis_res4_lastn_item", "sis_res4_agen_item", "sis_res4_email_item", "sis_res4_lang_item",
            //    "sis_res5_firstn_item", "sis_res5_lastn_item", "sis_res5_agen_item", "sis_res5_email_item", "sis_res5_lang_item",
            //    "sis_res6_firstn_item", "sis_res6_lastn_item", "sis_res6_agen_item", "sis_res6_email_item", "sis_res6_lang_item",
            //    "sis_res7_firstn_item", "sis_res7_lastn_item", "sis_res7_agen_item", "sis_res7_email_item", "sis_res7_lang_item",
            //    "sis_res8_firstn_item", "sis_res8_lastn_item", "sis_res8_agen_item", "sis_res8_email_item", "sis_res8_lang_item",
            //    "sis_res9_firstn_item", "sis_res9_lastn_item", "sis_res9_agen_item", "sis_res9_email_item", "sis_res9_lang_item",
            //    "sis_res10_firstn_item", "sis_res10_lastn_item", "sis_res10_agen_item", "sis_res10_email_item", "sis_res10_lang_item");

            buildTableWithItems(output, part, 6,
                new string[] { "First Name", "Last Name", "Relationship", "Agency", "Email", "Language", },
                "sis_res1_firstn_item", "sis_res1_lastn_item", "sis_res1_reln_item", "sis_res1_agen_item", "sis_res1_email_item", "sis_res1_lang_item",
                "sis_res2_firstn_item", "sis_res2_lastn_item", "sis_res2_reln_item", "sis_res2_agen_item", "sis_res2_email_item", "sis_res2_lang_item",
                "sis_res3_firstn_item", "sis_res3_lastn_item", "sis_res3_reln_item", "sis_res3_agen_item", "sis_res3_email_item", "sis_res3_lang_item",
                "sis_res4_firstn_item", "sis_res4_lastn_item", "sis_res4_reln_item", "sis_res4_agen_item", "sis_res4_email_item", "sis_res4_lang_item",
                "sis_res5_firstn_item", "sis_res5_lastn_item", "sis_res5_reln_item", "sis_res5_agen_item", "sis_res5_email_item", "sis_res5_lang_item",
                "sis_res6_firstn_item", "sis_res6_lastn_item", "sis_res6_reln_item", "sis_res6_agen_item", "sis_res6_email_item", "sis_res6_lang_item",
                "sis_res7_firstn_item", "sis_res7_lastn_item", "sis_res7_reln_item", "sis_res7_agen_item", "sis_res7_email_item", "sis_res7_lang_item",
                "sis_res8_firstn_item", "sis_res8_lastn_item", "sis_res8_reln_item", "sis_res8_agen_item", "sis_res8_email_item", "sis_res8_lang_item",
                "sis_res9_firstn_item", "sis_res9_lastn_item", "sis_res9_reln_item", "sis_res9_agen_item", "sis_res9_email_item", "sis_res9_lang_item",
                "sis_res10_firstn_item", "sis_res10_lastn_item", "sis_res10_reln_item", "sis_res10_agen_item", "sis_res10_email_item", "sis_res10_lang_item");


            output.drawY -= .3;

            output.appendSectionBreak();
            buildSubheaderWithResults(output, part, "Person who entered this information",
                "sis_entry_firstn_item", "sis_entry_lastn_item");

            if (options[OptionKey.includeComments])
            {
                output.appendSectionBreak();

                def_Items itm = formsRepo.GetItemByIdentifier("SIS-Prof1_PageNotes_item");
                if (itm == null)
                    throw new Exception("could not find item with identifier " + "SIS-Prof1_PageNotes_item");
                string rv = GetSingleResponse(itm);

                output.appendSubHeaderOnNewPageIfNecessary("Other Pertinent Information", rv);

                //BuildItemResults(output, part, "SIS-Prof1_PageNotes_item");
                output.drawY -= .3;
            }

            //output.appendWrappedText("Introduction to the SIS Report:", .36, 7.9, output.boldFont);
        }

        protected void PrintIntroPage(PdfOutput output)
        {
            output.drawY = PdfOutput.pageHeight - .52;
            output.appendWrappedText("Introduction to the SIS Report:", .36, 7.9, output.boldFont);

            output.drawY = PdfOutput.pageHeight - .73;
            output.appendWrappedText(ReplaceSISReservedWords(
                "The Supports Intensity Scale [Adult_Child] Version ([SIS-A_SIS-C]) profile information is " +
                "designed to assist in the service planning process for the individual, " +
                "their parents, family members, and service providers. The profile " +
                "information outlines the type and intensity of support the individual " +
                "would benefit from to participate and be successful in his or her " +
                "community. The [SIS-A_SIS-C] profile report is best applied in combination with " +
                "person-centered planning to achieve the desired outcome in creating individual goals."),
                .36, 7.9);

            output.drawY = PdfOutput.pageHeight - 1.25;

            if (form.identifier == "SIS-A")
                AppendPartHeader(output, "Rating Key for Sections 2 and 3");
            else
                AppendPartHeader(output, "Rating Key for Section 2");

            output.drawY = PdfOutput.pageHeight - 1.56;
            output.appendWrappedText(ReplaceSISReservedWords(
                "This describes the rating for Type of Support, Frequency " +
                "and Daily Support time for each of the six areas discussed in your [SIS-A_SIS-C] profile"),
                .36, 7.9);

            //table headers
            output.drawY = PdfOutput.pageHeight - 1.99;
            output.appendWrappedText("Type of Support", .5, 7.9);
            output.drawY = PdfOutput.pageHeight - 1.99;
            output.appendWrappedText("Frequency", 3.5, 7.9);
            output.drawY = PdfOutput.pageHeight - 1.99;
            output.appendWrappedText("Daily Support Time", 6.08, 7.9);

            //first column of text
            double[] firstColumnDy = { 2.4, 2.87, 3.56, 4.17, 4.34, 4.68, 4.85, 5.02, 5.19, 5.36, 5.53, 5.87, 6.04, 6.21, 6.38, 6.55, 6.72, 7.06, 7.23, 7.40, 7.57, 7.74, 8.08, 8.25, 8.42 };
            string[] firstColumnText = { 
                    "What help do you need to do the (item) on your own or by yourself",
                    "If engaged in the activity over the next several months, what would the nature of the support look like?",
                    "Which support type dominates the support provided?",

                    "0 = None", 
		            "No support needed at any time",

                    "1 = Monitoring (reminders). For example", 
 		            "* Encouragement, general supervision", 
                    "* Checking in, observing, telling, &/or giving",
		            "  reminders to complete the activity",
                    "* Asking questions to trigger the individual to",
		            "  complete steps within the activity",

                    "2 = Verbal/Gesture Prompting (demonstration). For",
		            "example:", 
                    "* Step by step instruction",
                    "  Walking a person through required steps", 
	                "* Providing visual prompts, showing", 
		            "* Modeling, teaching, role play, social stories",

                    "3 = Partial Physical Assistance (help through)", 
		            "doing). For example:",
		            "* Individual participates in some parts of the activity",
                    "* Some, essential steps are required to be", 
		            " completed for the person",

                    "4 = Full Physical Support (doing for). For example:",
                    "* All essential steps need to be completed for the",
                    "  person" };

            for (int i = 0; (i < firstColumnDy.Length) && (i < firstColumnText.Length); i++)
            {
                output.drawY = PdfOutput.pageHeight - firstColumnDy[i];
                output.appendWrappedText(firstColumnText[i], .5, 2.7, firstColumnText[i].Contains("=") ? output.boldFont : output.contentFont);
            }

            //Build SIS-A frequency second column
            double[] secondColumnDySisA = { 2.4, 4.17, 4.83, 5.5, 6.17, 7 };
            string[] secondColumnTextSisA = { 
                    "How frequently is supported needed for this activity?",
                    "0 = None or less than monthly",
                    "1 = At least once a month, but not once a week",
                    "2 = At least once a week, but not once a day",
                    "3 = At least once a day, But not once an hour",
                    "4 = Hourly or more frequently" };

            //Build SIS-C frequency second column
            double[] secondColumnDySisC = { 2.4, 4.17, 4.83, 5.5, 6.17, 6.84 };
            string[] secondColumnTextSisC = { 
                    "Frequency of Support",
                    "0 = Negligible; the child's support needs are rarely if ever different from those of same-aged peers in regard to frequency.",
                    "1 = Infrequently; the child will occasionally need someone to provide extraordinary support that same-aged peers will not need.",
                    "2 = Frequently; in order for the child to participate in the activity, extra support will need to be provided for about half of the occurrences of the activity.",
                    "3 = Very Frequently; in most occurrences of the activity, the child will need extra support that same-aged peers will not need.",
                    "4 = Always; on every occasion that the child participates in the activity, the child will need extra support that same aged peers will not need. " };

            // Write out SIS-A frequency column
            if (form.identifier.Equals("SIS-A"))
            {
                for (int i = 0; (i < secondColumnDySisA.Length) && (i < secondColumnTextSisA.Length); i++)
                {
                    output.drawY = PdfOutput.pageHeight - secondColumnDySisA[i];
                    output.appendWrappedText(secondColumnTextSisA[i], 3.5, 2.2, secondColumnTextSisA[i].Contains("=") ? output.boldFont : output.contentFont);
                }
            }

            // Write out SIS-C frequency column
            if (form.identifier.Equals("SIS-C"))
            {
                for (int i = 0; (i < secondColumnDySisC.Length) && (i < secondColumnTextSisC.Length); i++)
                {
                    output.drawY = PdfOutput.pageHeight - secondColumnDySisC[i];
                    output.appendWrappedText(secondColumnTextSisC[i], 3.5, 2.2, secondColumnTextSisC[i].Contains("=") ? output.boldFont : output.contentFont);
                }
            }

            //third column of text
            double[] thirdColumnDy = { 2.4, 4.17, 4.67, 5.17, 5.83, 6.33 };
            string[] thirdColumnText = { 
                    "If engaged in the activity over the next several months, in a typical 24-hour day, "
                    + "how much total, cumulative time would be needed to provide support?",
                    "0 = None",
                    "1 = Less Than 30 Minutes",
                    "2 = 30 Minutes to Less Than 2 Hours",
                    "3 = 2 Hours to Less Than 4 Hours",
                    "4 = 4 Hours or More" };

            for (int i = 0; (i < thirdColumnDy.Length) && (i < thirdColumnText.Length); i++)
            {
                output.drawY = PdfOutput.pageHeight - thirdColumnDy[i];
                output.appendWrappedText(thirdColumnText[i], 6.08, 1.92, thirdColumnText[i].Contains("=") ? output.boldFont : output.contentFont);
            }


            //horizontal lines
            foreach (double dy in new double[] { 1.82, 2.25, 3.99, 8.93 })
            {
                output.DrawLine(.36, PdfOutput.pageHeight - dy, 8.12, PdfOutput.pageHeight - dy, .02);
            }

            //vertical lines
            foreach (double x in new double[] { .36, 3.35, 5.93, 8.12 })
            {
                output.DrawLine(x, PdfOutput.pageHeight - 1.82, x, PdfOutput.pageHeight - 8.93, .02);
            }
        }

        protected string ReplaceSISReservedWords(string original)
        {
            bool isChild = form.identifier.Equals("SIS-C");
            return original
                .Replace("[Adult_Child]", isChild ? "Child" : "Adult")
                .Replace("[SIS-A_SIS-C]", isChild ? "SIS-C" : "SIS-A");

        }

        private double getScoreResponse(string itemVariableIdent)
        {
            def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, itemVariableIdent);
            return Convert.ToDouble(rv.rspValue);
        }

        protected static readonly string[] SupportNeedsColumnSuffixes = { "_TOS", "_Fqy", "_DST" };
        protected void PrintSupportNeedsGraph(PdfOutput output, int ageInYears)
        {
            Debug.WriteLine("PrintSupportNeedsGraph() method.");

            new Assessments(formsRepo).UpdateAssessmentScores(formResults);

            def_Parts prt = formsRepo.GetPartByFormAndIdentifier(form, "Section 2. Supports Needs Index");
            formsRepo.GetPartSections(prt);
            int row = 0, nRows = prt.def_PartSections.Count();
            double[] percentiles = new double[nRows];
            string[][] supNeedsTableVals = new string[nRows][];
            List<string> subscaleLabels = new List<string>();

            Debug.WriteLine("     Print Part 3");
            foreach (def_PartSections ps in prt.def_PartSections)
            {
                // Debug.WriteLine("     ps.sectionId: " + ps.sectionId.ToString());
                def_Sections sct = formsRepo.GetSectionById(ps.sectionId);
                subscaleLabels.Add(sct.title);
                Debug.WriteLine("     sct.sectionId: " + sct.sectionId.ToString());


                //pull subscale scores from database
                string sctNumberLetter = sct.title.Substring(0, sct.title.IndexOf('.'));
                double totalRawScore = getScoreResponse("scr_" + sctNumberLetter + "_raw");
                double standardScore = getScoreResponse("scr_" + sctNumberLetter + "_std");
                double rawScoreAvg = getScoreResponse("scr_" + sctNumberLetter + "_avg");
                double percentile = getScoreResponse("scr_" + sctNumberLetter + "_pct");
                double confidenceLow = Math.Max(0, standardScore - 1);
                double confidenceHigh = standardScore + 1;

                percentiles[row] = percentile;
                string rowLabel = sct.title.Replace("ActivitiesHI", "");
                rowLabel = rowLabel.Replace("and Neighborhood", "...");
                supNeedsTableVals[row] = new string[]{ 
                    rowLabel, 
                    (form.formId == 1) ? totalRawScore.ToString() : String.Format("{0:0.##}", rawScoreAvg ), 
                    standardScore.ToString(), 
                    percentile.ToString(), 
                    confidenceLow+"-"+confidenceHigh };

                row++;
            }

            //pull overall scores from database
            double totalRawScoreTotal = getScoreResponse("scr_total_rawscores_all_SIS_sections");
            double standardScoreTotal = getScoreResponse("scr_standard_score_total");
            double compositeIndex = getScoreResponse("scr_support_needs_index");
            double compositePercentile = getScoreResponse("scr_sni_percentile_rank");
            double totalRating = getScoreResponse("scr_total_rating");
            double meanRating = Math.Round((double)totalRating / 7, 2); //usd only for SIS-C reports

            AppendPartHeader(output, "Support Needs Profile - Graph");

            output.appendWrappedText("The graph provides a visual presentation of the six life activity areas from section 2. ", .5, 7.5);

            output.drawY -= .1;
            output.appendWrappedText("The graph reflects the pattern and intensity of the individual’s level of support. "
             + "The intent of the graph is to provide an easy means to prioritize the life activity areas in consideration "
             + "of setting goals and developing the Individual Support Plan. ", .5, 7.5);

            output.drawY -= .15;

            if (options[OptionKey.includeScores])
            {
                output.appendSimpleTable(
                    new string[] { "Activities Subscale", (form.formId == 1) ? "Total Raw Score" : "Average Raw Score", 
                    "Standard Score", "Percentile", "Confidence Interval (95%)" },
                    supNeedsTableVals,
                    new double[] { 0, .35, .35, .3, .35 });
                output.drawY -= PdfOutput.itemSpacing;

                if (form.identifier.Equals("SIS-A"))
                {
                    //for SIS-A, add a row to the bottom of the table, with totals
                    output.DrawLine(.75, output.drawY+.05, 7.75, output.drawY+.05, .01 );
                    output.appendSimpleTable(
                        new string[] { "Total:", 
                        totalRawScoreTotal.ToString(), 
                        standardScoreTotal.ToString(), "", "" },
                        new string[0][],
                        new double[] { 0, .35, .35, .3, .35 },
                        new double[] { .08, .35, .35, .3, .35 }
                    );

                    output.drawY -= .2;
                }
                else
                {
                    //for SIS-C
                    output.appendItem("Overall Mean Rating", String.Format("{0:0.##}", meanRating), 2);
                }

                output.appendItem("SIS Support Needs Index", compositeIndex.ToString(), 2);
                output.appendItem("Percentile", compositePercentile.ToString(), 2);
                output.drawY -= PdfOutput.itemSpacing;
            }

            //AppendPartHeader(output, "Support Needs Profile");
            AppendReportChart(output, subscaleLabels.ToArray(), percentiles, compositePercentile);

            if (!options[OptionKey.includeScores])
            {
                return;
            }

            if (form.identifier.Equals("SIS-A"))
            {
                Debug.WriteLine("     Print Part 4");
                prt = formsRepo.GetPartByFormAndIdentifier(form, "Section 3. Supplemental Protection and Advocacy Scale");
                formsRepo.GetPartSections(prt);
                List<def_Sections> scts = formsRepo.GetSectionsInPart(prt);
                def_Sections proSct = scts[0];
                if (proSct == null)
                {
                    throw new Exception("could not find any sections in part \"Section 3. Supplemental Protection and Advocacy Scale\"");
                }

                List<def_Items> proItms = formsRepo.GetSectionItems(proSct);  // getItemsInSection(proSct);
                // RRB 3/17/15 - this is hokey, but not sure how else to do it at this point w/o the SectionItems.
                //                  should be checking SectionItems.subSectionId != null
                // Prune out the subSections (this is 
                for (int i = 0; i < proItms.Count; i++)
                {
                    if (proItms[i].itemId == 1)
                        proItms.Remove(proItms[i]);
                }

                int nProItms = proItms.Count();
                string[][] supProTableVals = new string[nProItms][];
                for (int i = 0; i < nProItms; i++)
                {
                    def_Items itm = proItms[i];
                    int rawScore = 0;
                    // def_ItemResults itmResults = itm.def_ItemResults.SingleOrDefault(ir => ir.formResultId == formResultId);
                    def_ItemResults itmResults = formsRepo.GetItemResultByFormResItem(formResultId, itm.itemId);
                    for (int j = 0; j < 3; j++)
                    {
                        string suffix = SupportNeedsColumnSuffixes[j];
                        string rspValue = null;

                        try
                        {
                            rspValue = itmResults.def_ResponseVariables.SingleOrDefault(rv => rv.def_ItemVariables.identifier.EndsWith(suffix)).rspValue;
                        }
                        catch (System.NullReferenceException ex)
                        {
                            Debug.Print("for itemId " + itm.itemId + ", could not find responseVariable linked to itemVariableIdentifier with suffix \"" + suffix + "\"");
                        }

                        try
                        {
                            rawScore += Int16.Parse(rspValue);
                        }
                        catch (Exception ex)
                        {
                            Debug.Print("could not parse integer from response value \"" + rspValue + "\"");
                        }
                    }

                    supProTableVals[i] = new string[2];
                    supProTableVals[i][0] = itm.label;
                    supProTableVals[i][1] = rawScore.ToString();
                }

                output.drawY -= PdfOutput.itemSpacing;
                AppendPartHeader(output, "Section 3: Supplemental Protection and Advocacy Scale");
                output.appendSimpleTable(new string[] { "Protection and Advocacy Activities", "Raw Score" }, supProTableVals, new double[] { 0, .2 });

                output.drawY -= .2;
                output.appendWrappedText("The support needs profile reflects the pattern and intensity of the "
                 + "individual’s support. The information provided in sections 1, 2, and 3, can be beneficial "
                 + "in the development of the individual’s support plan.", .5, 7.5);
            }
        }

        protected void PrintSupplementalQuestions(PdfOutput output)
        {
            def_Parts part = formsRepo.GetPartByFormAndIdentifier(form, "Supplemental Questions");
            AppendPartHeader(output, part.identifier);

            def_Sections topSct = formsRepo.GetSectionsInPart(part).FirstOrDefault();
            if (topSct == null)
            {
                throw new Exception("not sections found in part with partId " + part.partId);
            }

            List<def_Sections> sectionList = new List<def_Sections>();
            formsRepo.SortSectionItems(topSct);
            foreach (def_SectionItems sctnItm in topSct.def_SectionItems)
            {
                if (sctnItm.display == false)
                    continue;
                def_Sections subSctns = formsRepo.GetSubSectionById(sctnItm.subSectionId);// sctnItm.def_SubSections.def_Sections;
                sectionList.Add(subSctns);
            }

            int grayCount = 0;
            bool showWhiteQuestions = true;
            foreach (def_Sections sct in sectionList)
            {
                formsRepo.SortSectionItems(sct);
                foreach (def_SectionItems si in sct.def_SectionItems)
                {
                    if (si.display == false)
                        continue;

                    //if this is a "gray" question... (high-level questions pertaining to a set of "white" questions)
                    if (si.subSectionId == null)
                    {
                        grayCount++;
                        if (grayCount > 1)
                            output.drawY -= .5;
                        def_Items itm = si.def_Items;
                        def_ItemVariables iv = itm.def_ItemVariables.FirstOrDefault();
                        if (iv == null)
                        {
                            throw new Exception("no item variable for question with itemId " + si.itemId);
                        }
                        def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(formResultId, iv.itemVariableId);//iv.def_ResponseVariables.FirstOrDefault();
                        if (rv == null)
                            continue;

                        //don't show the white questions if the response value is false
                        if (iv.baseTypeId == 1 && (rv.rspValue.ToLower().Equals("false") || rv.rspValue.Equals("0")))
                            showWhiteQuestions = false;
                        else
                            showWhiteQuestions = true;

                        if (output.drawY < .5)
                            output.appendPageBreak();

                        if (!itm.label.Equals("Page Notes"))
                        {
                            double y = output.drawY;
                            output.appendWrappedText(grayCount + ".", .8, 6, output.boldFont);
                            output.drawY = y;
                        }
                        output.appendWrappedText(itm.label, 1, 7, output.boldFont);
                        output.appendWrappedText(GetSupplementalResponseText(iv, rv), 1.5, 6, output.boldFont);
                        output.drawY -= PdfOutput.itemSpacing;
                    }

                    // if this is a "white" question-set
                    else
                    {
                        if (!showWhiteQuestions)
                            continue;
                        foreach (def_SectionItems ssi in si.def_SubSections.def_Sections.def_SectionItems)
                        {
                            def_Items itm = ssi.def_Items;
                            if (itm == null)
                            {
                                throw new Exception("no item for setionItems with sectionItemId " + ssi.sectionItemId);
                            }
                            def_ItemVariables iv = itm.def_ItemVariables.FirstOrDefault();
                            if (iv == null)
                            {
                                throw new Exception("no item variable for item with itemId " + itm.itemId);
                            }
                            def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(formResultId, iv.itemVariableId);

                            try
                            {
                                output.appendWrappedText(itm.label, 1.1, 6);
                            }
                            catch (ApplicationException e)
                            {
                                string msg = "Exception appending label \"" +
                                    itm.label + "\" for item with itemId " + itm.itemId + "\nchars:";
                                foreach (char c in itm.label.ToCharArray())
                                    msg += " " + c + "(" + ((int)c) + ")";
                                throw new Exception(msg, e);
                            }

                            output.appendWrappedText(GetSupplementalResponseText(iv, rv), 1.5, 6);
                            output.drawY -= PdfOutput.itemSpacing;
                        }
                    }
                }
            }
        }

        private string GetSupplementalResponseText(def_ItemVariables iv, def_ResponseVariables rv)
        {
            string responseText;
            if (rv == null)
            {
                responseText = "";
            }
            else
            {
                responseText = rv.rspValue;
                if (iv.baseTypeId == 1)
                {
                    if (responseText.Equals("1") || responseText.ToLower().Equals("true"))
                        return "yes";
                    if (responseText.Equals("0") || responseText.ToLower().Equals("false"))
                        return "no";
                }
            }
            return responseText;
        }

        private void AppendReportChart(PdfOutput output, string[] xLabels, double[] percentiles, double supportIndex)
        {
            //string chartFilename = "C:\\Code\\site\\Assmnts\\Content\\chart.bmp";
            string chartFilename = "chart.bmp";
            string outpath = CreateReportChart(chartFilename, xLabels, percentiles, supportIndex);
            if (outpath != null)
            {
                output.appendImage(outpath);
            }
            else
            {
                throw new Exception("Error creating report chart");
            }
        }

        /// <summary>
        /// Creates the the Report Chart
        /// </summary>
        /// <param name="chartFileName">File name to save chart to</param>
        /// <returns>output path if chart was successfully created, null otherwise</returns>
        private string CreateReportChart(string chartFileName, string[] xLabels, double[] percentiles, double supportIndex, bool relSIS = false)
        {
            string outpath = null;
            Chart crtSection1 = new Chart();

            Bitmap img;

            def_Items itm = formsRepo.GetItemByIdentifier("trkNum");
            string trackingNumber = GetSingleResponse(itm);

            try
            {
                // set overlap footer on to make chart the same size
                // if there is a valid license or not
                crtSection1.OverlapFooter = true;
                crtSection1.ChartArea.XAxis.Label.Text = "Life Activity Subscale";             // set the xaxis title
                crtSection1.ChartArea.YAxis.Label.Text = "Percentile";                         // set the yaxis title
                crtSection1.TempDirectory = System.IO.Path.GetTempPath();//System.Configuration.ConfigurationManager.AppSettings["ChartPath"];         // set the temp directory
                crtSection1.Debug = true;                                                      // do not show debug info on chart
                crtSection1.FileName = chartFileName;                                              // set the file name

                if (relSIS)
                {
                    crtSection1.Title = "Activity Subscale and Composite Score Profile";
                    crtSection1.Size = "600x400";                                                  // set the chart size
                }
                else
                {
                    crtSection1.Title = "Tracking Number: " + trackingNumber; //+
                    //ds.Tables["ReportData"].Rows[0]["sis_track_num"].ToString();
                    crtSection1.Size = "575x442";                                                  // set the chart size
                }

                // set the chart title  
                crtSection1.DefaultSeries.DefaultElement.ShowValue = true;
                crtSection1.Mentor = false;
                crtSection1.Use3D = false;
                crtSection1.DefaultSeries.DefaultElement.Transparency = 15;
                crtSection1.TitleBox.Visible = true;                                          // show the title box
                crtSection1.LegendBox.Visible = true;                                          // show the legend
                crtSection1.LegendBox.Orientation = dotnetCHARTING.Orientation.Top;            // show the legend at the top
                crtSection1.YAxis.Interval = 10;                                               // set the chart inverval
                crtSection1.YAxis.Minimum = 0;                                                 // set the min value the chart shows
                crtSection1.YAxis.Maximum = 100;                                               // set the max value the chart shows
                crtSection1.LegendBox.Background.Color = Color.FromArgb(240, 234, 202);        // set the legend box color
                crtSection1.LegendBox.Background.GlassEffect = true;
                crtSection1.ChartArea.Background = new Background(Color.Ivory);                // set the chart background color
                crtSection1.ImageFormat = ImageFormat.Png;
                string bgImagePath = String.Format("/Content/Images/SuppNeedOverlay.jpg", crtSection1.TempDirectory);
                crtSection1.ChartArea.Background = new Background(bgImagePath, BackgroundMode.ImageTile);
                // add the values to the chart
                Series sr = new Series();
                sr = new Series();
                sr.Name = "Activity Subscale Percentile";
                sr.Type = SeriesType.Bar;

                for (int i = 0; i < xLabels.Length; i++)
                {
                    int headerColorIndex = i % (section2HeaderColors.Length / 3);
                    sr.Elements.Add(new Element(xLabels[i], percentiles[i])
                    {
                        Color = Color.FromArgb(
                            section2HeaderColors[headerColorIndex, 0],
                            section2HeaderColors[headerColorIndex, 1],
                            section2HeaderColors[headerColorIndex, 2])
                    });
                }
                sr.DefaultElement.Color = Color.FromArgb(190, 65, 30);
                sr.LegendEntry.Value = "";
                crtSection1.SeriesCollection.Add(sr);

                sr = new Series();
                sr.Name = "SIS Support Index Percentile";
                sr.Type = SeriesType.Bar;
                sr.Elements.Add(new Element("SIS Support Index", supportIndex));
                sr.DefaultElement.Color = Color.FromArgb(215, 195, 93);

                // bind the data to the chart
                crtSection1.SeriesCollection.Add(sr);

                // save the chart image
                img = crtSection1.GetChartBitmap();
                crtSection1.FileManager.SaveImage(img);
            }
            finally
            {
                outpath = crtSection1.TempDirectory + crtSection1.FileName + ".png";
                // clean up
                crtSection1.Dispose();
                crtSection1 = null;
            }

            return outpath;
        }

        private string MaskSSN(string ssn)
        {
            if (ssn != null && ssn.Length > 4)
            {
                int l = ssn.Length;
                ssn = ssn.Substring(l - 4);
                while (ssn.Length < l)
                    ssn = "*" + ssn;
            }
            return ssn;
        }

        private string MaskMedicaidNumber(string medicaidNumber)
        {
            if (medicaidNumber != null && medicaidNumber.Length > 4)
            {
                int l = medicaidNumber.Length;
                medicaidNumber = medicaidNumber.Substring(l - 4);
                while (medicaidNumber.Length < l)
                    medicaidNumber = "*" + medicaidNumber;
            }
            return medicaidNumber;
        }
    }
}
