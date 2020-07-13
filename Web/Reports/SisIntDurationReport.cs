using AJBoggs.Sis.Reports;

using Data.Abstract;

using System;
using System.Collections.Generic;



namespace Assmnts.Reports
{
    class SisIntDurationReport : SisPdfReport
    {
        #region public static List<SubgroupDurationData> getTestData()
        public static List<SubgroupDurationData> GetTestData()
        {
            //test data copied from bugzilla attachment
            int nRows = 49;
            string[,] data = {
                //{ title, nAssmnts, max, min, avg, nInts }
                { "State of Virginia",	"136",	"3:00",	"3:00",	"3:00",	"7" },
                { "Prince William County",	"137",	"3:00",	"1:30",	"2:37",	"4" },
                { "CEV-CSB",	"138",	"5:15",	"1:20",	"2:03",	"21" },
                { "Middle Peninsula-Northern Nec",	"139",	"2:50",	"1:30",	"2:19",	"23" },
                { "Training",	"140",	"3:30",	"2:00",	"2:28",	"27" },
                { "Arlington CSB",	"141",	"3:40",	"2:00",	"2:53",	"41" },
                { "Fairfax CSB",	"142",	"3:00",	"3:00",	"3:00",	"4" },
                { "Valley CSB",	"143",	"3:00",	"3:00",	"3:00",	"1" },
                { "Region Ten CSB",	"144",	"3:15",	"1:50",	"2:20",	"50" },
                { "Cumberland Mountain Community Services",	"145",	"3:00",	"1:15",	"2:02",	"143" },
                { "Virginia Beach",	"146",	"4:00",	"1:45",	"2:45",	"15" },
                { "State of Virginia",	"147",	"2:30",	"2:30",	"2:30",	"7" },
                { "State of Virginia",	"148",	"3:30",	"1:30",	"2:16",	"10" },
                { "State of Virginia",	"149",	"3:00",	"1:40",	"2:13",	"20" },
                { "Alexandria",	"150",	"3:00",	"1:30",	"2:01",	"3" },
                { "Alleghany-Highland",	"151",	"3:00",	"3:00",	"3:00",	"1" },
                { "Chesapeake",	"152",	"3:00",	"3:00",	"3:00",	"6" },
                { "Chesterfield",	"153",	"1:30",	"1:30",	"1:30",	"1" },
                { "Colonial",	"154",	"3:00",	"3:00",	"3:00",	"1" },
                { "Crossroads",	"155",	"2:20",	"2:10",	"2:13",	"3" },
                { "Danville-Pittsylvania",	"156",	"1:30",	"1:30",	"1:30",	"2" },
                { "Dickenson County",	"157",	"3:00",	"1:50",	"2:29",	"38" },
                { "Eastern Shore",	"158",	"4:15",	"1:30",	"2:10",	"37" },
                { "Hampton-Newport News",	"159",	"6:18",	"1:40",	"2:14",	"95" },
                { "Hanover County",	"160",	"7:03",	"1:22",	"2:34",	"125" },
                { "Harrisonburg-Rockingham",	"161",	"2:00",	"1:45",	"1:56",	"8" },
                { "Henrico Area",	"162",	"5:26",	"1:38",	"2:35",	"44" },
                { "Highlands",	"163",	"3:00",	"1:00",	"2:40",	"56" },
                { "Mount Rogers",	"164",	"2:55",	"2:25",	"2:35",	"13" },
                { "New River Valley",	"165",	"3:00",	"1:00",	"2:00",	"2" },
                { "Norfolk",	"166",	"3:00",	"1:30",	"2:26",	"73" },
                { "Northwestern",	"167",	"3:00",	"1:30",	"2:14",	"14" },
                { "Planning District I",	"168",	"3:10",	"1:12",	"2:24",	"88" },
                { "District 19",	"169",	"3:00",	"3:00",	"3:00",	"1" },
                { "Portsmouth",	"170",	"3:00",	"3:00",	"3:00",	"1" },
                { "Rappahannock Area",	"171",	"3:30",	"2:00",	"2:29",	"18" },
                { "Rappahannock-Rapidan",	"172",	"1:24",	"1:24",	"1:24",	"1" },
                { "Richmond",	"173",	"3:00",	"3:00",	"3:00",	"1" },
                { "Blue Ridge",	"174",	"2:30",	"1:30",	"1:54",	"34" },
                { "Rockbridge Area",	"175",	"3:30",	"2:00",	"2:13",	"10" },
                { "Southside",	"176",	"6:00",	"1:15",	"2:11",	"97" },
                { "Western Tidewater",	"177",	"2:00",	"2:00",	"2:00",	"1" },
                { "AAIDD Contract Interviews",	"178",	"3:00",	"2:00",	"2:30",	"2" },
                { "SEVTC",	"179",	"2:59",	"2:53",	"2:56",	"2" },
                { "SVTC",	"180",	"3:00",	"3:00",	"3:00",	"1" },
                { "NVTC",	"181",	"3:00",	"3:00",	"3:00",	"1" },
                { "SWVTC",	"182",	"3:00",	"3:00",	"3:00",	"2" },
                { "707 CVTC",	"183",	"3:00",	"2:00",	"2:42",	"6" },
                { "DD Waiver",	"363",	"3:00",	"3:00",	"3:00",	"1" }
            };

            List<SubgroupDurationData> result = new List<SubgroupDurationData>();
            for (int row = 0; row < nRows; row++)
            {
                SubgroupDurationData sdd = new SubgroupDurationData();
                sdd.subgroupTitle = data[row, 0];
                sdd.nAssessments = Convert.ToInt16(data[row, 1]);
                sdd.max = parseMinutes(data[row, 2]);
                sdd.min = parseMinutes(data[row, 3]);
                sdd.avg = parseMinutes(data[row, 4]);
                sdd.nInterviewers = Convert.ToInt16(data[row, 5]);
                result.Add(sdd);
            }
            return result;
        }

        private static int parseMinutes(string hoursAndMinutes)
        {
            string[] parts = hoursAndMinutes.Split(':');
            return Convert.ToInt16(parts[0]) * 60 + Convert.ToInt16(parts[1]);
        }
        #endregion

        public class SubgroupDurationData
        {
            public string subgroupTitle;

            //duration stats, in minutes
            public int min, max, avg;

            //counts
            public int nAssessments, nInterviewers;
        }

        public void SetData(List<SubgroupDurationData> data)
        {
            this.data = data;
        }

        private List<SubgroupDurationData> data = null;

        public SisIntDurationReport(IFormsRepository formsRepo, int formResultId, SisPdfReportOptions options, string logoPath, string outputPath)
            : base(formsRepo, formResultId, options, logoPath, outputPath ) { }

        override public void BuildReport()
        {
            if (data == null)
                throw new Exception("Interview duration data not set, should call SetData() before BuildReport()");

            output.drawY -= .5;

            PrintSummaryStats();
            output.drawY -= .2;

            PrintDurationHeaders();
            foreach (SubgroupDurationData sdd in data)
                PrintSubgroupDurationRow(sdd);
        }

        private static readonly double[] summaryRowX = { .7, 2.5 };

        private void PrintSummaryStats()
        {
            int longestMins = 0;
            int shortestMins = Int16.MaxValue;

            int durationTotal = 0;
            int durationCount = 0;

            foreach (SubgroupDurationData sdd in data)
            {
                if (sdd.max > longestMins)
                    longestMins = sdd.max;
                if (sdd.min < shortestMins)
                    shortestMins = sdd.min;
                durationTotal += sdd.avg * sdd.nAssessments;
                durationCount += sdd.nAssessments;
            }

            int avgMins = durationTotal / durationCount;

            output.DrawText(output.boldFont, output.fontSize, summaryRowX[0], output.drawY, "Overall Average (hours):");
            output.DrawText(output.contentFont, output.fontSize, summaryRowX[1], output.drawY, toDurationString(avgMins));
            output.drawY -= .2;

            output.DrawText(output.boldFont, output.fontSize, summaryRowX[0], output.drawY, "Longest Interview:");
            output.DrawText(output.contentFont, output.fontSize, summaryRowX[1], output.drawY, toDurationString(longestMins));
            output.drawY -= .2;

            output.DrawText(output.boldFont, output.fontSize, summaryRowX[0], output.drawY, "Shortest Interview:");
            output.DrawText(output.contentFont, output.fontSize, summaryRowX[1], output.drawY, toDurationString(shortestMins));
            output.drawY -= .2;
        }

        private static readonly string[] durationRowHeaders = { "CSB or Subgroup", "Min", "Max", "Average", "# Interviewers" };
        private static readonly double[] durationRowX = { .7, 4, 5, 6, 7 };

        private void PrintDurationHeaders()
        {
            for (int i = 0; i < 5; i++ )
                output.DrawText(output.boldFont, output.fontSize, durationRowX[i], output.drawY,
                    durationRowHeaders[i]);
            output.drawY -= .15;
        }

        private string toDurationString(int minutes)
        {
            int hours = minutes / 60;
            minutes -= hours * 60;
            string sMinutes = minutes.ToString();
            if (sMinutes.Length < 2)
                sMinutes = "0" + sMinutes;
            return hours + ":" + sMinutes;
        }

        private void PrintSubgroupDurationRow( SubgroupDurationData rowData )
        {
            string[] durationRowContent = { 
                rowData.subgroupTitle + " (" + rowData.nAssessments + ")", 
                toDurationString( rowData.min ),
                toDurationString( rowData.max ),
                toDurationString( rowData.avg ),
                rowData.nInterviewers.ToString() 
            };
            for (int i = 0; i < 5; i++)
                output.DrawText(output.contentFont, output.fontSize, durationRowX[i], output.drawY,
                    durationRowContent[i]);
            output.drawY -= .15;
        }
    }
}