using Assmnts.Infrastructure;
using Assmnts.Models;
using Assmnts.UasServiceRef;

using Data.Abstract;
using Data.Concrete;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;

namespace Assmnts.Controllers
{
    public partial class AdapController : Controller
    {

        /// <summary>
        /// Loads the page for clinic administration.
        /// </summary>
        /// <returns>Redirects the user to the clinic admin page.</returns>
        [HttpGet]
        public ActionResult Clinic()
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            bool accessLevel = UAS.Business.UAS_Business_Functions.hasPermission(0, "RptsExpts");
            if (!accessLevel)
            {
                return RedirectToAction("AdapPortal", "ADAP", new { userId = SessionHelper.LoginStatus.UserID });
            }

            AuthenticationClient webclient = new AuthenticationClient();
            AdapApplicantRpt1 aar1 = new AdapApplicantRpt1();
            aar1.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;
            aar1.ReportName = "Clinic Administration";

            // AdapApplicatnRpt1 was originally written for Report1, but the various fields make it versatile
            // formResultId stores the lookupMasterId, and teamId stores EnterpriseId for the Clinic Report.
            aar1.formResultId = formsRepo.GetLookupMastersByLookupCode("ADAP_CLINIC").lookupMasterId;
            aar1.teamId = SessionHelper.LoginStatus.EnterpriseID;

            aar1.TeamDDL = new List<Group>();//webclient.GetGroupsByEnterpriseID(SessionHelper.LoginStatus.EnterpriseID);
            foreach (var v in formsRepo.GetLookupMastersByLookupCode("ADAP_CLINIC").def_LookupDetail
                .Where(f => (aar1.teamId > 0) ? aar1.teamId == f.EnterpriseID : true)
                .GroupBy(f => f.GroupID)
                .Select(grp => grp.First())
                .ToList()) 
            {
                aar1.TeamDDL.Add(webclient.getGroup(Convert.ToInt32(v.GroupID)));
            }

            aar1.TeamDDL.Insert(0, new Group()
            {
                GroupID = -1,
                GroupName = "All"
            });

            webclient.Close();

            return View("~/Views/COADAP/ClinicAdmin.cshtml", aar1);
        }

        /// <summary>
        ///  Web Service to process requests from the ADAP Clinic Admin DataTable
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        public string DataTableClinicList(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            string result = String.Empty;
            Debug.WriteLine("Adap Controller ClinicList draw:" + draw.ToString());
            Debug.WriteLine("Adap Controller ClinicList start:" + start.ToString());
            Debug.WriteLine("Adap Controller ClinicList searchValue:" + searchValue);
            Debug.WriteLine("Adap Controller ClinicList searchRegex:" + searchRegex);
            Debug.WriteLine("Adap Controller ClinicList order:" + order);

            //  Lists all the Request parameters
            foreach (string s in Request.Params.Keys)
            {
                Debug.WriteLine(s.ToString() + ":" + Request.Params[s]);
            }

            try
            {
                // Initialize the DataTable response (later transformed to JSON)
                DataTableResult dtr = new DataTableResult()
                {
                    draw = draw,
                    recordsTotal = 1,
                    recordsFiltered = 1,
                    data = new List<string[]>(),
                    error = String.Empty
                };

                // How to find Enterprise ID and Form ID?  Values currently hardcoded to populate Data Table.
                //IQueryable<def_LookupMaster> query = formsRepo.GetLookupMaster(74);

                var sSort = Request.Params["columns[1][search][value]"];
                var sTeam = Request.Params["columns[2][search][value]"];
                int teamId = -1;
                if (!String.IsNullOrEmpty(sTeam) && !sTeam.Equals("-1"))
                {
                    try
                    {
                        teamId = Convert.ToInt32(sTeam);
                    }
                    catch (Exception excptn)
                    {
                        Debug.WriteLine("Adap Controller ClinicList Integer Conversion:" + excptn.Message);
                    }
                }
                // rename to avoid language/library conflicts
                int iniIndex = start;
                int noDsplyRecs = length;

                int entId = SessionHelper.LoginStatus.EnterpriseID;
                Debug.WriteLine("Adap Controller ClinicList SelectedEnterprise:" + entId.ToString());

                IEnumerable<def_LookupDetail> detail = formsRepo.GetLookupMastersByLookupCode("ADAP_CLINIC").def_LookupDetail
                        .Where( f => (teamId > -1) ? f.GroupID == teamId : true )
                        .Where( f => (entId > 0) ? f.EnterpriseID == entId : true );
                if (String.IsNullOrEmpty(sSort) || sSort.Equals("false"))
                {
                    detail = detail.OrderBy( f => f.displayOrder );
                }
                else
                {
                    detail = detail.OrderBy(f => (f.def_LookupText.FirstOrDefault() != null) ? f.def_LookupText.FirstOrDefault().displayText : "zzzzz");
                }

                IUasSql uasSql = new UasSql();

                dtr.recordsTotal = dtr.recordsFiltered = detail.Count();
                
                foreach (var d in detail.Skip(iniIndex).Take(noDsplyRecs) )
                {
                    string[] data = new string[] {
                        "<button type=\"button\" class=\"btn btn-primary btn-xs text-center\" onclick=\"window.location='/Adap/EditDetail?lookupDetailId=" + d.lookupDetailId.ToString() + "'\">Edit</button>",
                        (formsRepo.GetLookupTextsByLookupDetail(d.lookupDetailId).FirstOrDefault() != null) ? formsRepo.GetLookupTextsByLookupDetail(d.lookupDetailId).FirstOrDefault().displayText : String.Empty,
                        d.dataValue,
                        (d.GroupID == null) ? "" : uasSql.getGroupName(d.GroupID.Value),
                        d.displayOrder.ToString(),
                        d.StatusFlag
                    };
                    dtr.data.Add(data);
                }

                Debug.WriteLine("Adap Controller ClinicList data populated.");

                // Output the JSON in DataTable format
                fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                param.EnableAnonymousTypes = true;
                result = fastJSON.JSON.ToJSON(dtr, param);
                Debug.WriteLine("DataTable Clinic result:" + result);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("Adap Controller ClinicList exception:" + excptn.Message);
                result = excptn.Message + " - " + excptn.Message;
            }

            return result;
        }

    }

}