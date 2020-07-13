using Assmnts.Infrastructure;
using Assmnts.UasServiceRef;

using System;
using System.Web.Configuration;

using UAS.Business;

namespace Assmnts.Business
{
    public static class Timeout
    {
        /*
         * Returns the total Session timeout in minutes.
         * Default: sessionState from the web.config
         * If available, from the Enterprise Application Configuration.
         * 
         */
        public static int GetTotalTimeoutMinutes(int EnterpriseId)
        {
            SessionStateSection sessionSection = (SessionStateSection)WebConfigurationManager.GetSection("system.web/sessionState");
            int timeout = (int)sessionSection.Timeout.TotalMinutes;


            bool VentureMode = SessionHelper.IsVentureMode;
            if (VentureMode)
                return timeout;
            

            EntAppConfig timeoutConfig = UAS_Business_Functions.GetEntAppConfig(UAS.Business.ConfigEnumCodes.TIMEOUT, EnterpriseId);
            if (timeoutConfig != null)
            {
                int time;
                bool convertTimeoutString = Int32.TryParse(timeoutConfig.ConfigValue, out time);
                timeout = (convertTimeoutString) ? time : (int)sessionSection.Timeout.TotalMinutes;
            }

            return timeout;
        }
    }
}