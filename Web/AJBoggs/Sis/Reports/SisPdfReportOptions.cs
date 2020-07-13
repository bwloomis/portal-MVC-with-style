using System;
using System.Collections.Generic;
using System.Web;

namespace AJBoggs.Sis.Reports
{
    public class SisPdfReportOptions
    {
        public enum ReportType
        {
            Family, Generic, Default, IntDuration
        }

        public enum OptionKey {  
            grayscale, includeSups, includeScores, includeToFor, includeComments, includeHeaderTitle 
        }

        private readonly Dictionary<OptionKey, bool> optionDict = new Dictionary<OptionKey, bool>();

        public ReportType type;

        public SisPdfReportOptions()
        {
            //set all options to true
            foreach (OptionKey key in Enum.GetValues(typeof(OptionKey)))
            {
                optionDict[key] = true;
            }

            //..except includeSups
            optionDict[OptionKey.includeSups] = false;

            type = ReportType.Family;
        }

        public SisPdfReportOptions(HttpRequestBase Request)
        {
            foreach (OptionKey key in Enum.GetValues(typeof(OptionKey)))
            {
                optionDict[key] = requestBool(Request,key.ToString());
            }

            type = ReportType.Family;
            //if (requestBool(Request, "family"))
            //    type = ReportType.Family;
            //else if (requestBool(Request, "generic"))
            //    type = ReportType.Generic;
            //else
            //    type = ReportType.Default;
        }

        private bool requestBool(HttpRequestBase Request,string key)
        {
            string resp = Request[key] as string;
            return (resp != null) && resp.ToLower().Equals("true");
        }

        public bool this[OptionKey key]
        {
            get { return optionDict[key]; }
            set { optionDict[key] = value; }
        }
    }
}