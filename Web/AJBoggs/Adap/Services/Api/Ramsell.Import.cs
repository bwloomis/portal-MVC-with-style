using AJBoggs.Adap.Services.Xml;

using Assmnts;

using Data.Abstract;

using System;
using System.Diagnostics;


namespace AJBoggs.Adap.Services.Api
{
    public static partial class Ramsell
    {

        #region - constants

        #endregion - END constants

        /// <summary>
        /// Used by the create application method to populate the application with data pulled from Ramsell
        /// this uses the ramsellId from within the formResult's responses, 
        /// so applications that do not have a response to the RamsellId item will not be effected by this function
        /// </summary>
        /// <param name="frmRes">Form result for the application to populate</param>
        public static void PopulateItemsFromRamsellImport(IFormsRepository formsRepo, def_FormResults frmRes)
        {

            //get rammsellId (or terminate)
            string ramsellMemberId = String.Empty;

            def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmRes.formResultId, "ADAP_D9_Ramsell");
            if (rv != null)
            {
                ramsellMemberId = rv.rspValue;
            }

            if ( String.IsNullOrWhiteSpace( ramsellMemberId ) )
            {
                Debug.WriteLine("* * * AdapController.PopulateItemsFromRamsellImport: no RamsellId/MemberId present in formResult " + frmRes.formResultId +", skipping Ramsell import..." );
                return;
            }

            //get oauth token
            // string usrId = "38222218";
            // string password = @"P@ssw0rd";
            string usrId = UAS.Business.UAS_Business_Functions.GetEntAppConfigAdap("USR");
            string password = UAS.Business.UAS_Business_Functions.GetEntAppConfigAdap("PWD");
            Debug.WriteLine("Ramsell UseriD / Password: " + usrId + @" / " + password);

            string oauthToken = Ramsell.GetOauthToken(usrId, password);

            new RamsellImport( formsRepo, frmRes.formResultId ).ImportApplication( oauthToken, ramsellMemberId );
        }
		
		

    }
}
