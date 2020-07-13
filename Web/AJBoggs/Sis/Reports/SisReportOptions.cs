using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;

using ReportOptionKey = AJBoggs.Sis.Reports.SisPdfReportOptions.OptionKey;

namespace AJBoggs.Sis.Reports
{


    public static class SisReportOptions
    {

        /// <summary>
        /// Builds the custom SIS Report Options for reportOptions.cshtml screen.
        /// 
        /// </summary>
        /// <param name="enterpriseId">FormResults.EnterpriseID</param>
        /// <returns>SisPdfReportOptions instance</returns>
        public static SisPdfReportOptions BuildPdfReportOptions(int? enterpriseId)
        {
           SisPdfReportOptions reportOptions = new SisPdfReportOptions();

            string reportConfigPath = String.Empty;
            try
            {
                // check for and use custom enterprise report options if they exist (based off the assessment enterpriseID, not the user enterpriseID)
                reportConfigPath = Path.Combine(ConfigurationManager.AppSettings["EnterpriseDir"], enterpriseId.ToString()) + "\\ReportOptions.cfg";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SisReportOptions, find custom enterprise report options failed. Message: " + ex.Message);
            }


            // set report default options for enterprise is they exist
            if (System.IO.File.Exists(reportConfigPath))
            {
                Dictionary<String, String> reportConfig = Assmnts.Infrastructure.Configuration.LoadConfig(reportConfigPath);

                bool configOut;
                string value;

                foreach (ReportOptionKey key in Enum.GetValues(typeof(ReportOptionKey)))
                {
                    string sKey = key.ToString();
                    if (reportConfig.ContainsKey(sKey))
                    {
                        if (reportConfig.TryGetValue(sKey, out value))
                        {
                            if (Boolean.TryParse(value, out configOut))
                                reportOptions[key] = configOut;
                        }
                    }
                }
            }

            return reportOptions;

        }

    }
}
