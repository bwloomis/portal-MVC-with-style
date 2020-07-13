using Assmnts.Infrastructure;

using Data.Concrete;

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web.Mvc;


namespace Assmnts.Controllers
{
    /*
     * The DEF3 Web Services Controller
     * 
     * This can be accessed from a variety of sources via HTTP GET/POST (browser, program, etc.).
     * Workflow requires the client to perform the Login method here or on the login screen before attempting to use other methods.
     * 
     */
    public partial class DefwsController : Controller
    {

        
        // *** RRB 10/29/15 - Don't understand why 2 GetLookup are needed for SIS and ADAP.
        // ***             The only diff is Active ??  This should work for both SIS and ADAP
        // ***     Refactor - put all Lookup methods below into DefwsController.Lookup.cs
        // ***     *** It may be the Group filter that will mess it up
        // ***     *** CO ADAP uses the Group for the Team  *** DESIGN A WAY AROUND IT SO ONE METHOD CAN BE USED
        // ***     ***   Most of this code should go into DEF Domain.
        // ***     ***      Just the Language and JSON transform should be in the Controller

        // *** RRB 2/6/16  Not sure why this controller looks so much different.
        //                Why it isn't using the constructor and formsRepository.
        //           Database code needs to be moved into one of the Domains or even FormsRepository
        //           Session variables remain here and are sent to domains as parameters.

        [HttpGet]
        public string GetLookup(string lkpCd)
        {
            Debug.WriteLine("GetLookup  lkpCd: " + lkpCd );
            if (!SessionHelper.IsUserLoggedIn)
            {
                return "You are NOT logged in.";
            }

            int groupId = SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups[0];

            Debug.WriteLine("GetLookup  lkpCd: " + lkpCd + "     Enterprise: " + SessionHelper.LoginStatus.EnterpriseID + "   GroupID:" + groupId);

            short lang = 1; // Make this use a session variable later!!
            def_LookupMaster master = null;
            try
            {
                using (var context = DataContext.GetDbContext())
                {
                    context.Configuration.LazyLoadingEnabled = false;

                    /*   var query = context.def_LookupMaster
                               .Where(m => m.lookupCode == lkpCd)
                               .Select(m => new { m, det = m.def_LookupDetail.Where(d => ) })
                       */
                    master = (from i in context.def_LookupMaster
                              where lkpCd == i.lookupCode
                              select i).FirstOrDefault();

                    var detailCount = context.Entry(master)
                        .Collection(m => m.def_LookupDetail)
                        .Query()
                        .Where(m => m.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && m.GroupID == groupId)
                        .Count();

                    // if the Enterprise and Group match, load the LookupDetail records
                    if (detailCount > 0)
                    {
                        context.Entry(master)
                            .Collection(m => m.def_LookupDetail)
                            .Query()
                            .Where(m => m.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && m.GroupID == groupId)
                            .OrderBy(m => m.displayOrder)
                            .Load();
                    }
                    else
                    {   // Check for the Enterprise specific LookupDetail
                        detailCount = context.Entry(master)
                            .Collection(m => m.def_LookupDetail)
                            .Query()
                            .Where(m => m.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && m.GroupID == 0)
                            .OrderBy(m => m.displayOrder)
                            .Count();

                        // Load the Enterprise specific LookupDetail
                        if (detailCount > 0)
                        {
                            context.Entry(master)
                                .Collection(m => m.def_LookupDetail)
                                .Query()
                                .Where(m => m.EnterpriseID == SessionHelper.LoginStatus.EnterpriseID && m.GroupID == 0)
                                .OrderBy(m => m.displayOrder)
                                .Load();
                        }
                        else
                        {   // Load the default LookupDetail
                            context.Entry(master)
                                .Collection(m => m.def_LookupDetail)
                                .Query()
                                .Where(m => m.EnterpriseID == 0 && m.GroupID == 0)
                                .OrderBy(m => m.displayOrder)
                                .Load();
                        }

                    }

                    // Load the associated LookupText for the selected LookupDetail records
                    // Get a specific Language
                    // If no records for a specific language, get English
                    foreach (def_LookupDetail det in master.def_LookupDetail)
                    {
                        var textCount = context.Entry(det)
                            .Collection(t => t.def_LookupText)
                            .Query()
                            .Where(t => t.langId == lang)
                            .Count();

                        // If specific Language found, load the records
                        // *** RRB 2/1/16 *** This doesn't look right. 2 different kinds of queries being used Entities and IQueryable
                        // ***            *** Also the last one appears to be pulling in almost anything other than specific language.

                        if (textCount > 0)
                        {
                            context.Entry(det)
                                .Collection(t => t.def_LookupText)
                                .Query()
                                .Where(t => t.langId == lang)
                                .Load();
                        }
                        else
                        {
                            context.Entry(det)
                                .Collection(t => t.def_LookupText)
                                .Load();
                        }
                    }

                }
            }
            catch (SqlException sqlxcptn)
            {
                Debug.WriteLine("GetLookup Using SqlException: " + sqlxcptn.Message);
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("GetLookup Using Exception: " + xcptn.Message);
            }

            string jsonString = String.Empty;
            try
            {
                jsonString = fastJSON.JSON.ToJSON(master);
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("GetLookup fastJSON Exception: " + xcptn.Message);
            }

            Debug.WriteLine("GetLookup jsonString length: " + jsonString.Length);
            return jsonString;
           
        }


        [HttpGet]
        public string GetLookupADAP(string lkpCd)
        {
            Debug.WriteLine("GetLookup  lkpCd: " + lkpCd);
            if (!SessionHelper.IsUserLoggedIn)
            {
                return "You are NOT logged in.";
            }

            Debug.WriteLine("GetLookup  lkpCd: " + lkpCd + "     Enterprise: " + SessionHelper.LoginStatus.EnterpriseID);

            // Get the Language for the User
            CultureInfo ci = Thread.CurrentThread.CurrentUICulture;
            string region = (ci == null) ? "en" : ci.TwoLetterISOLanguageName.ToLower();
            int langId = DataContext.GetDbContext().def_Languages.Where(l => l.languageRegion.ToLower() == region).Select(l=> l.langId).FirstOrDefault();

            def_LookupMaster master = null;
            try
            {
                using (var context = DataContext.GetDbContext())
                {
                    context.Configuration.LazyLoadingEnabled = false;

                    /*   var query = context.def_LookupMaster
                               .Where(m => m.lookupCode == lkpCd)
                               .Select(m => new { m, det = m.def_LookupDetail.Where(d => ) })
                       */
                    master = (from i in context.def_LookupMaster
                              where lkpCd == i.lookupCode
                              select i).FirstOrDefault();

                    if (master != null) {
                        //check if there are enterprise-specific lookups
                        int entID = SessionHelper.LoginStatus.EnterpriseID;
                        int detailCount = context.Entry(master)
                            .Collection(m => m.def_LookupDetail)
                            .Query()
                            .Where(m => m.EnterpriseID == entID && m.StatusFlag == "A")
                            .Count();

                        //if there are no enterprise-specific lookups, use ent 0 to retrieve defaults
                        if (detailCount == 0) {
                            entID = 0;
                            detailCount = context.Entry(master)
                                .Collection(m => m.def_LookupDetail)
                                .Query()
                                .Where(m => m.EnterpriseID == entID && m.StatusFlag == "A")
                                .Count();
                        }

                        if (detailCount > 0) {
                            context.Entry(master)
                                .Collection(m => m.def_LookupDetail)
                                .Query()
                                .Where(m => m.EnterpriseID == entID && m.StatusFlag == "A")
                                .OrderBy(m => m.displayOrder)
                                .Load();
                        }

                        // Get the associated LookupText for each LookupDetail
                        foreach (def_LookupDetail det in master.def_LookupDetail) {
                            var textCount = context.Entry(det)
                                .Collection(t => t.def_LookupText)
                                .Query()
                                .Where(t => t.langId == langId)
                                .Count();

                            // *** RRB 2/1/16 ***
                            // ***    Like the SIS Lookup method above this is using 2 different types of query results
                            // ***  Figure out the correct one during Unit Testing
                            // ***  Since it is the final query, IQueryable isn't necessary
                            if (textCount > 0) {
                                context.Entry(det)
                                    .Collection(t => t.def_LookupText)
                                    .Query()
                                    .Where(t => t.langId == langId)
                                    .Load();
                            }
                            else {
                                context.Entry(det)
                                    .Collection(t => t.def_LookupText)
                                    .Load();
                            }
                        }
                    }
                }
            }
            catch (SqlException sqlxcptn)
            {
                Debug.WriteLine("GetLookup Using SqlException: " + sqlxcptn.Message);
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("GetLookup Using Exception: " + xcptn.Message);
            }

            string jsonString = String.Empty;
            if (master != null) {
                try {
                    jsonString = fastJSON.JSON.ToJSON(master);
                } catch (Exception xcptn) {
                    Debug.WriteLine("GetLookup fastJSON Exception: " + xcptn.Message);
                }
            } else {
                jsonString = "{\"def_BaseTypes\": null,\"def_LookupDetail\": [],\"lookupMasterId\": 0,\"lookupCode\": \"" + lkpCd + "\",\"title\": \"Duration of Power of Attorney (POA)\",\"baseTypeId\": 8}";
            }
            Debug.WriteLine("GetLookup jsonString length: " + jsonString.Length);
            return jsonString;

        }

        /*
         * Returns a list of lookup details given the enterprise ID and Group ID.
         * These must be explicitly passed to ensure they have been intentionally set.
         * 
         */

        public List<def_LookupDetail> GetLookupDetails(string lkpCd, int ent, int grp)
        {
            def_LookupMaster lookupMaster = formsRepo.GetLookupMastersByLookupCode(lkpCd);

            return formsRepo.GetLookupDetails(lookupMaster.lookupMasterId, ent, grp);
        }

        /*
         * Returns a list of lookup Texts given a lookup detail.
         */

        public List<def_LookupText> GetLookupText(def_LookupDetail lkpDtl)
        {
            return formsRepo.GetLookupTextsByLookupDetail(lkpDtl.lookupDetailId);
        }

        /*
         * Returns a list of lookup Texts given a lookup detail and language Id.
         */
        public List<def_LookupText> GetLookupText(def_LookupDetail lkpDtl, int langId)
        {
            return formsRepo.GetLookupTextsByLookupDetailLanguage(lkpDtl.lookupDetailId, langId);
        }

        /// <summary>
        /// Gets the first lookup detail for a lookup master code, ent = enterpriseID, group = 0. Returns the text of the first lookup text
        /// for this detail.
        /// </summary>
        /// <param name="lkpCd"></param>
        /// <returns></returns>
        public string GetLookupTextByCode(string lkpCd)
        {
            try
            {
                List<def_LookupDetail> details = GetLookupDetails(lkpCd, SessionHelper.LoginStatus.EnterpriseID, 0);
                List<def_LookupText> text = null;
                if (details != null && details.Count > 0)
                {
                    text = GetLookupText(details[0], 1);
                }
                if (text != null)
                {
                    return text[0].displayText;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetLookupTextByCode exception: lookup code \"" + lkpCd + "\". " + ex.Message);
            }
            return String.Empty;

        }
    }
}
