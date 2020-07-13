using Data.Abstract;

using System;
using System.Diagnostics;

using SubscaleCatagory = Assmnts.Reports.SharedScoring.SubscaleCatagory;

namespace Assmnts.Reports
{
    public class SisAScoring    
    {
        private static string[] sisASubscaleNumonics = { 
            "Home", "Community", "LifelongLearning", "Employment", "HealthAndSafety", "Social"   
           
            //numonics are matched to the columns in SubscaleTable below (starting with the second column)
            //example to assign two different numonics to the same scoring scale: "Social/Employment"
        };

        private static int inf = 99999; //this is used as a placeholder for unobtainable scores in the table below

        //table copied from Appendix 6.2
        private static int[,] SubscaleTable = {
            
            //standard scores       minimum subscale raw scores (same order as subscaleNumonics) percentiles
            { 19,                   89,    inf,     inf,    inf,    inf,    inf,                100     },
            { 18,                   87,    inf,     inf,    inf,    inf,    inf,                100     },
            { 17,                   85,     91,     inf,    inf,    inf,    inf,                 99     },
            { 16,                   81,     88,      97,    inf,     92,    inf,                 98     },
            { 15,                   77,     84,      92,    inf,     86,     91,                 95     },
            { 14,                   73,     79,      86,     85,     79,     84,                 91     },
            { 13,                   68,     74,      79,     78,     72,     76,                 84     },
            { 12,                   62,     69,      72,     70,     65,     68,                 75     },
            { 11,                   55,     63,      64,     61,     57,     58,                 63     },
            { 10,                   48,     56,      55,     52,     49,     48,                 50     },
            {  9,                   40,     49,      46,     42,     42,     38,                 37     },
            {  8,                   32,     41,      36,     32,     34,     28,                 25     },
            {  7,                   25,     33,      27,     23,     27,     19,                 16     },
            {  6,                   18,     25,      18,     15,     20,     10,                  9     },
            {  5,                   11,     16,       9,      7,     13,      3,                  5     },
            {  4,                    3,      6,       0,      0,      7,      0,                  2     },
            {  3,                    0,      0,       0,      0,      1,      0,                  1     },
            {  2,                    0,      0,       0,      0,      0,      0,                  0     },
        };

        //table copied from Appendix 6.3
        private static int[,] CompositeTable = {
            //standardScoreTotal, supportNeedsIndex, percentile
            {	 97,	143,	100	},
            {	 96,	141,	100	},
            {	 95,	140,	100	},
            {	 94,	139,	100	},
            {	 93,	138,	100	},
            {	 92,	137,	100	},
            {	 91,	136,	100	},
            {	 90,	135,	99	},
            {	 89,	133,	99	},
            {	 88,	132,	99	},
            {	 87,	131,	98	},
            {	 86,	130,	98	},
            {	 85,	129,	97	},
            {	 84,	128,	97	},
            {	 83,	126,	96	},
            {	 82,	125,	95	},
            {	 81,	124,	95	},
            {	 80,	123,	94	},
            {	 79,	122,	93	},
            {	 78,	121,	92	},
            {	 77,	120,	91	},
            {	 76,	118,	89	},
            {	 75,	117,	87	},
            {	 74,	116,	86	},
            {	 73,	115,	84	},
            {	 72,	114,	82	},
            {	 71,	113,	81	},
            {	 70,	111,	77	},
            {	 69,	110,	75	},
            {	 68,	109,	73	},
            {	 67,	108,	70	},
            {	 66,	107,	68	},
            {	 65,	106,	65	},
            {	 64,	105,	63	},
            {	 63,	103,	58	},
            {	 62,	102,	55	},
            {	 61,	101,	53	},
            {	 60,	100,	50	},
            {	 59,	 99,	47	},
            {	 58,	 98,	45	},
            {	 57,	 96,	39	},
            {	 56,	 95,	37	},
            {	 55,	 94,	35	},
            {	 54,	 93,	32	},
            {	 53,	 92,	30	},
            {	 52,	 91,	27	},
            {	 51,	 90,	25	},
            {	 50,	 89,	23	},
            {	 49,	 87,	19	},
            {	 48,	 86,	18	},
            {	 47,	 85,	16	},
            {	 46,	 84,	14	},
            {	 45,	 83,	13	},
            {	 44,	 82,	13	},
            {	 43,	 80,	9	},
            {	 42,	 79,	8	},
            {	 41,	 78,	7	},
            {	 40,	 77,	6	},
            {	 39,	 76,	5	},
            {	 38,	 75,	5	},
            {	 37,	 74,	4	},
            {	 36,	 72,	3	},
            {	 35,	 71,	3	},
            {	 34,	 70,	2	},
            {	 33,	 69,	2	},
            {	 32,	 68,	1	},
            {	 31,	 67,	1	},
            {	 30,	 65,	1	},
            {	 29,	 64,	0	},
            {	 28,	 63,	0	},
            {	 27,	 62,	0	},
            {	 26,	 61,	0	},
            {	 25,	 60,	0	},
            {	 24,	 59,	0	},
            {	 23,	 57,	0	},
            {	 22,	 56,	0	},
            {	 21,	 55,	0	},
            {	 20,	 54,	0	},
            {	 19,	 53,	0	},
            {	 18,	 52,	0	},
            {	 17,	 50,	0	},
            {	 16,	 49,	0	},
            {	 15,	 48,	0	},
            {	 14,	 47,	0	},
            {	 13,	 46,	0	},
            {	 12,	 45,	0	},
            {	 11,	 44,	0	},
            {	 10,	 42,	0	},
            {	  9,	 41,	0	},
            {	  8,	 40,	0	},
            {	  7,	 39,	0	},
            {	  6,	 38,	0	},
        };

        private static SubscaleCatagory[] sisACatagories = null;
        public static SubscaleCatagory[] getSisASubscaleCatagories()
        {
            if (sisACatagories == null)
                sisACatagories = SubscaleCatagory.getCatagoriesFromNumonics(sisASubscaleNumonics);
            return (SubscaleCatagory[])sisACatagories.Clone();
        }

        public static SubscaleCatagory GetSisASubscaleCatagoryForSection( def_Sections sct )
        {
            if( sct == null )
                throw new Exception( "Could not find a subscale catagory for NULL section" );
            string s = sct.title.Replace(" ", "").ToLower();
            foreach (SubscaleCatagory option in getSisASubscaleCatagories())
            {
                foreach (string numonic in option.getNumonics())
                {
                    if( s.Contains( numonic.ToString().ToLower() ) )
                        return option;
                }
            }
            throw new Exception( "Could not find a subscale catagory for section with title \"" + sct.title + "\"" );
        }
        
        public static double GetSisASubscaleStandardScore( int totalRawScore, SubscaleCatagory cat)
        {
            int SubScaleTableRowCount = SubscaleTable.Length/ 8;
            int i = (int)cat.index + 1;
            for (int rowIndex = 0; rowIndex < SubScaleTableRowCount; rowIndex++)
            {
                if (SubscaleTable[rowIndex, i] <= totalRawScore)
                    return SubscaleTable[rowIndex, 0];
            }
            return 0;
        }

        public static double GetSisASubscalePercentile(int totalRawScore, SubscaleCatagory cat)
        {
            int SubScaleTableRowCount = SubscaleTable.Length / 8;
            int i = (int)cat.index + 1;
            for (int rowIndex = 0; rowIndex < SubScaleTableRowCount; rowIndex++)
            {
                if (SubscaleTable[rowIndex, i] <= totalRawScore)
                    return SubscaleTable[rowIndex, sisASubscaleNumonics.Length+1 ];
            }
            return 0;
        }
        
        public static int GetSisASupportNeedsIndex(int standardScoreTotal)
        {
            int CompositeTableRowCount = CompositeTable.Length / 3;
            for (int rowIndex = 0; rowIndex < CompositeTableRowCount; rowIndex++)
            {
                if (CompositeTable[rowIndex, 0] <= standardScoreTotal)
                    return CompositeTable[rowIndex, 1];
            }
            return 0;
        }
        
        public static int GetSisASupportNeedsPercentile(int standardScoreTotal)
        {
            int CompositeTableRowCount = CompositeTable.Length / 3;
            for (int rowIndex = 0; rowIndex < CompositeTableRowCount; rowIndex++)
            {
                if (CompositeTable[rowIndex, 0] <= standardScoreTotal)
                    return CompositeTable[rowIndex, 2];
            }
            return 0;
        }

        public static void UpdateSisAScores(IFormsRepository formsRepo, int formResultId)
        {
            try
            {
                def_Forms frm = formsRepo.GetFormByIdentifier("SIS-A");
                int standardScoreTotal = SharedScoring.UpdateSisScoresNoSave
                (
                    formsRepo, frm, formResultId,
                    getSisASubscaleCatagories(),
                    (int totalRawScore, double avgRawScore, SubscaleCatagory cat) => { return GetSisASubscaleStandardScore(totalRawScore, cat); },
                    (int totalRawScore, double avgRawScore, SubscaleCatagory cat) => { return GetSisASubscalePercentile(totalRawScore, cat); }
                );

                //save standard scores to database
                int compositeIndex = GetSisASupportNeedsIndex(standardScoreTotal);
                int compositePercentile = GetSisASupportNeedsPercentile(standardScoreTotal);
                SharedScoring.UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_support_needs_index", compositeIndex);
                SharedScoring.UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_sni_percentile_rank", compositePercentile);
                formsRepo.Save();
            }
            catch(Exception xcptn)
            {
                Debug.WriteLine("UpdateSisAScores exception: " + xcptn.Message);
            }
        }
    }
}