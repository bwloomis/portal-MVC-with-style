using Data.Abstract;

using System;
using System.Diagnostics;
using System.Linq;

namespace Assmnts.Reports
{
    public class SharedScoring
    {
        public class SubscaleCatagory
        {
            private readonly string[] numonics;
            public readonly int index;
            public SubscaleCatagory(string numonics, int index)
            {
                this.numonics = numonics.Split('/');
                this.index = index;
            }


            public static SubscaleCatagory[] getCatagoriesFromNumonics( string[] subscaleNumonics )
            {
                int n = subscaleNumonics.Length;
                SubscaleCatagory[] result = new SubscaleCatagory[n];
                for (int i = 0; i < n; i++)
                    result[i] = new SubscaleCatagory(subscaleNumonics[i], i);
                return result;
            }

            public string[] getNumonics()
            {
                return (string[])numonics.Clone();
            }
        };

        public static def_Sections GetSectionForSubscaleCatagory(def_Forms frm, SubscaleCatagory cat)
        {
            if (cat == null)
                throw new Exception("Could not find a section for NULL SubscaleCatagory");

            //def_Forms frm = formsRepo.GetFormByIdentifier("SIS-A");
            try
            {
                foreach (def_FormParts fp in frm.def_FormParts)
                {
                    foreach (def_PartSections ps in fp.def_Parts.def_PartSections)
                    {
                        string s = ps.def_Sections.title.Replace(" ", "").ToLower();
                        foreach (string numonic in cat.getNumonics())
                        {
                            if (s.Contains(numonic.ToString().ToLower()))
                                return ps.def_Sections;
                        }
                    }
                }
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("GetSectionForSubscaleCatagory: " + xcptn.Message);
            }

            throw new Exception("Could not find a section for SubscaleCatagory \"" + cat.getNumonics()[0] + "\"");
        }

        public static int ComputeClientAgeInYears( IFormsRepository formsRepo, int formResultId )
        {

            def_ResponseVariables dobRV = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "sis_cl_dob_dt");
            if (dobRV == null)
                throw new Exception("could not find date-of-birth response variable with identifier \"sis_cl_dob_dt\" under formResultId " + formResultId);

            def_ResponseVariables dintRV = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "sis_completed_dt");
            if (dintRV == null)
                throw new Exception("could not find interview-date response variable with identifier \"sis_completed_dt\" under formResultId " + formResultId);

            AssertRVHasDate(dobRV);
            AssertRVHasDate(dintRV);

            //http://stackoverflow.com/a/4127396
            DateTime dob = dobRV.rspDate.Value;
            DateTime intDate = dintRV.rspDate.Value;
            DateTime zeroTime = new DateTime(1, 1, 1);
            TimeSpan span = intDate - dob;
            return (zeroTime + span).AddDays(-1).Year - 1;
        }

        private static void AssertRVHasDate(def_ResponseVariables dateRv)
        {
            if (!dateRv.rspDate.HasValue)
            {
                try
                {
                    dateRv.rspDate = Convert.ToDateTime(dateRv.rspValue, new System.Globalization.CultureInfo("en-US"));
                    if (dateRv == null || !dateRv.rspDate.HasValue)
                        throw new Exception(); //this is intentionally thrown and caught below, so a null result is handled the same way as an exception during conversion
                }
                catch (Exception e)
                {
                    throw new Exception("could not find or compute interview-date rspDate in response variable " + dateRv.responseVariableId);
                }
            }
        }

        protected static readonly string[] SupportNeedsColumnSuffixes = { "_TOS", "_Fqy", "_DST" };


        public static void updateSection3ScoresNoSave(
            IFormsRepository formsRepo,
            def_Forms frm,
            int formResultId)
        {
            if (frm.identifier == "SIS-C")
                return;

            string sectionIdentifier = "SIS-A 3";
            def_Sections sct = formsRepo.GetSectionByIdentifier(sectionIdentifier);
            if( sct == null )
                throw new Exception( "Could not find section with identifier \"" + sectionIdentifier + "\"" );
            int rawTotal = 0;

            foreach (def_SectionItems si in sct.def_SectionItems)
            {
                if (si.subSectionId != null)        // skip the Subsections
                {
                    continue;
                }
                def_Items itm = formsRepo.GetItemById(si.itemId);
                // def_ItemResults itmResults = itm.def_ItemResults.SingleOrDefault(ir => ir.formResultId == formResultId);
                def_ItemResults itmResults = formsRepo.GetItemResultByFormResItem(formResultId, itm.itemId);
                if (itmResults == null)
                    continue;
                formsRepo.GetItemResultsResponseVariables(itmResults);
                foreach (def_ResponseVariables rv in itmResults.def_ResponseVariables)
                {
                    rv.def_ItemVariables = formsRepo.GetItemVariableById(rv.itemVariableId);
                }
                for (int i = 0; i < 3; i++)
                {
                    string suffix = SupportNeedsColumnSuffixes[i];
                    string rspValue = null;

                    def_ResponseVariables rv = itmResults.def_ResponseVariables.SingleOrDefault(trv => trv.def_ItemVariables.identifier.EndsWith(suffix));
                    if (rv != null)
                        rspValue = rv.rspValue;

                    if (!String.IsNullOrWhiteSpace(rspValue))
                    {
                        int rawScore;
                        if (Int32.TryParse(rv.rspValue, out rawScore))
                        {
                            rawTotal += rawScore;
                        }
                        else
                        {
                            Debug.WriteLine("* * * Skipping item on updating section 1 scores: " +
                                "Could not parse integer from response value \"{0}\" for itemVariable \"{1}\". (formResultID {2})",
                                rv.rspValue, rv.def_ItemVariables.identifier, formResultId);
                        }
                    }
                }
            }

            UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_3_raw_total", rawTotal);
        }

        public static void updateSection1ScoresNoSave(
            IFormsRepository formsRepo,
            def_Forms frm,
            int formResultId)
        {
            def_FormResults fr = formsRepo.GetFormResultById(formResultId);
            int entId = fr.EnterpriseID.HasValue ? fr.EnterpriseID.Value : 0;

            string sectionIdentifierPrefix = frm.identifier; //"SIS-A" or "SIS-C"

            foreach (string shortSectionName in new string[] { "1A", "1B" })
            {
                string sectionIdentifier = sectionIdentifierPrefix + " " + shortSectionName; // e.g. "SIS-A 1A"
                def_Sections sct = formsRepo.GetSectionByIdentifier( sectionIdentifier );
                if (sct == null)
                    throw new Exception("Could not find section with identifier \"" + sectionIdentifier + "\"");

                int rawTotal = 0;

                foreach (def_SectionItems si in formsRepo.GetSectionItemsBySectionIdEnt(sct.sectionId, entId)) 
                {
                    def_ItemVariables rawScoreIv = formsRepo.GetItemVariablesByItemId(si.itemId)
                        .Where(iv => iv.identifier.EndsWith("Support")).SingleOrDefault(); //e.g. "Q1A1_ExMedSupport"

                    if (rawScoreIv != null)
                    {
                        def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultItemVarId(formResultId, rawScoreIv.itemVariableId);
                        if (rv != null && !String.IsNullOrWhiteSpace( rv.rspValue ) )
                        {
                            int rawScore;
                            if (Int32.TryParse(rv.rspValue, out rawScore))
                            {
                                rawTotal += rawScore;
                            }
                            else
                            {
                                Debug.WriteLine("* * * Skipping item on updating section 1 scores: " + 
                                    "Could not parse integer from response value \"{0}\" for itemVariable \"{1}\". (formResultID {2})", 
                                    rv.rspValue, rawScoreIv.identifier, formResultId );
                            }
                        }
                    }
                }

                UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_" + shortSectionName + "_raw_total", rawTotal);
            }
        }

        public static int UpdateSisScoresNoSave(
            IFormsRepository formsRepo, 
            def_Forms frm, 
            int formResultId, 
            SubscaleCatagory[] subscales,
            Func<int, double, SubscaleCatagory, double> GetSubscaleStandardScore,
            Func<int, double, SubscaleCatagory, double> GetSubscalePercentile)
        {
            DateTime startTime = DateTime.Now;

            updateSection1ScoresNoSave(formsRepo, frm, formResultId);
            if (frm.identifier == "SIS-A")
                updateSection3ScoresNoSave(formsRepo, frm, formResultId);

            double standardScoreTotal = 0, totalRating = 0;
            int totalRawScoreTotal = 0;
            int standardScoreCount = 0;

            foreach (SubscaleCatagory cat in subscales)
            {
                def_Sections sct = GetSectionForSubscaleCatagory(frm, cat);
                formsRepo.SortSectionItems(sct);

                int totalRawScore = 0, countRawScore = 0;


                foreach (def_SectionItems si in sct.def_SectionItems)
                {
                    if (si.subSectionId != null)        // skip the Subsections
                    {
                        continue;
                    }
                    def_Items itm = formsRepo.GetItemById(si.itemId);
                    // def_ItemResults itmResults = itm.def_ItemResults.SingleOrDefault(ir => ir.formResultId == formResultId);
                    def_ItemResults itmResults = formsRepo.GetItemResultByFormResItem(formResultId, itm.itemId);
                    if (itmResults == null)
                        continue;
                    formsRepo.GetItemResultsResponseVariables(itmResults);
                    foreach (def_ResponseVariables rv in itmResults.def_ResponseVariables)
                    {
                        rv.def_ItemVariables = formsRepo.GetItemVariableById(rv.itemVariableId);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        string suffix = SupportNeedsColumnSuffixes[i];
                        string rspValue = null;

                        def_ResponseVariables rv = itmResults.def_ResponseVariables.SingleOrDefault(trv => trv.def_ItemVariables.identifier.EndsWith(suffix));
                        if( rv != null )
                            rspValue = rv.rspValue;

                        if (!String.IsNullOrWhiteSpace(rspValue))
                        {
                            int rspInt;
                            if (Int32.TryParse(rspValue, out rspInt))
                            {
                                totalRawScore += rspInt;
                                countRawScore++;
                            }
                            else
                            {
                                Debug.WriteLine("* * * Skipping item on updating scores: " +
                                    "Could not parse integer from response value \"{0}\" for itemVariable \"{1}\". (formResultID {2})",
                                    rv.rspValue, rv.def_ItemVariables.identifier, formResultId);
                                Debug.Print("could not parse integer from response value \"" + rspValue + "\"");
                            }
                        }
                    }
                }

                double rawScoreAvg = Math.Round((double)totalRawScore / countRawScore, 2);
                double standardScore = GetSubscaleStandardScore(totalRawScore, rawScoreAvg, cat);//totalRawScore / 5;
                double percentile = GetSubscalePercentile(totalRawScore, rawScoreAvg, cat);//totalRawScore;

                //save subscale scores to database
                string sctNumberLetter = sct.title.Substring(0, sct.title.IndexOf('.'));
                UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_" + sctNumberLetter + "_raw", totalRawScore);
                UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_" + sctNumberLetter + "_std", standardScore);
                UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_" + sctNumberLetter + "_avg", rawScoreAvg);
                UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_" + sctNumberLetter + "_pct", percentile);

                standardScoreTotal += standardScore;
                totalRawScoreTotal += totalRawScore;
                totalRating += rawScoreAvg;
                standardScoreCount++;
            }

            //save overall scores to database
            //int compositeIndex = GetSupportNeedsIndex((int)standardScoreTotal);//19;
            //int compositePercentile = GetSupportNeedsPercentile((int)standardScoreTotal);
            UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_total_rawscores_all_SIS_sections", totalRawScoreTotal);
            UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_standard_score_total", standardScoreTotal);
            //saveResponse(formsRepo, formResultId, "scr_support_needs_index", compositeIndex);
            //saveResponse(formsRepo, formResultId, "scr_sni_percentile_rank", compositePercentile);
            UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_total_rating", totalRating);

            TimeSpan duration = DateTime.Now - startTime;
            Debug.WriteLine("* * *  SharedScoring:UpdateSisScores COMPLETED IN " + duration.ToString() );
            return (int)standardScoreTotal;
        }

        public static void UpdateScoreResponseNoSave(IFormsRepository formsRepo, int formResultId, string itemVariableIdent, double response)
        {
            //if response exists already...
            def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, itemVariableIdent);
            if (rv != null)
            {
                rv.rspValue = response.ToString();
            }

            //if not...
            def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(itemVariableIdent);
            if (iv == null)
                throw new Exception("could not find itemVariable with identifier \"" + itemVariableIdent + "\"");
            def_ItemResults ir = formsRepo.GetItemResultByFormResItem(formResultId, iv.itemId);
            if (ir == null)
            {
                ir = new def_ItemResults()
                {
                    itemId = iv.itemId,
                    formResultId = formResultId,
                    dateUpdated = DateTime.Now,
                    sessionStatus = 0
                };
                formsRepo.AddItemResult(ir);
            }
            rv = ir.def_ResponseVariables.FirstOrDefault(trv => trv.itemVariableId == iv.itemVariableId);
            if (rv == null)
            {
                rv = new def_ResponseVariables()
                {
                    itemResultId = ir.itemResultId,
                    itemVariableId = iv.itemVariableId,
                    rspValue = response.ToString()
                };
                formsRepo.AddResponseVariable(rv);
            }
            else
            {
                rv.rspValue = response.ToString();
            }
        }
    }
}