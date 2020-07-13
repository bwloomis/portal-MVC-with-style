using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using AJBoggs;
using Assmnts.Business.Uploads;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Data.Concrete;

namespace Assmnts.Controllers
{
    public partial class AdapCaController : Controller
    {

        public ActionResult EnrollmentSite()
        {
            return View();
        }

        public ActionResult Jurisdiction()
        {
            var context = DataContext.getUasDbContext();
            var model = new JurisdictionModel();
            var jurisdictions = from g in context.uas_Group
                                where g.GroupTypeID == 194
                                select g;
            model.Jurisdictions = (from j in jurisdictions
                                   select new SelectListItem()
                                   {
                                       Text = j.GroupName,
                                       Value = j.GroupID.ToString()
                                   }).ToList();
            var units = from g in context.uas_Group
                        where g.GroupTypeID == 195
                        select g;
            model.Jurisdictions.Insert(0, new SelectListItem()
            {
                Text = "All",
                Value = string.Empty
            });
            model.Units = (from u in units
                           select new SelectListItem()
                           {
                               Text = u.GroupName,
                               Value = u.GroupID.ToString()
                           }).ToList();
            model.Units.Insert(0, new SelectListItem()
            {
                Text = "All",
                Value = string.Empty
            });
            return View(model);
        }

        [HttpPost]
        public ActionResult EditEnrollmentSite(EnrollmentSiteModel model)
        {
            var context = DataContext.getUasDbContext();

            if (ModelState.IsValid)
            {
                // save the enrollment site
                try
                {
                    var group = context.uas_Group.SingleOrDefault(x => x.GroupID == model.GroupId);
                    group.GroupName = model.SiteNumber;
                    group.GroupDescription = model.EnrollmentSite;
                    group.Notes = model.Restrictions;
                    group.ParentGroupId = int.Parse(model.Unit);
                    var address = context.uas_EntGrpAddress.FirstOrDefault(x => x.GroupID == model.GroupId);
                    if (address == null)
                    {
                        address = new uas_EntGrpAddress();
                        address.GroupID = model.GroupId;
                        address.EnterpriseID = group.EnterpriseID;
                        address.CreatedBy = SessionHelper.LoginStatus.UserID;
                        address.CreatedDate = DateTime.Now;
                        address.AddressType = "Site";
                        address.StatusFlag = "A";
                        // add the new address
                        context.uas_EntGrpAddress.Add(address);
                    }
                    else
                    {
                        address.ModifiedBy = SessionHelper.LoginStatus.UserID;
                        address.ModifiedDate = DateTime.Now;
                    }
                    address.Address1 = model.Address1;
                    address.Address2 = model.Address2;
                    address.City = model.City;
                    address.StateProvince = model.State;
                    address.PostalCode = model.ZipCode;

                    if (model.StartDate.HasValue && model.EndDate.HasValue)
                    {
                        string dates = string.Format("{0:yyyy-MM-dd}", model.StartDate) + ";" +
                            string.Format("{0:yyyy-MM-dd}", model.EndDate);
                        address.URL = dates;
                    }

                    var phone = context.uas_EntGrpPhone.FirstOrDefault(x => x.GroupID == model.GroupId);
                    if (phone == null)
                    {
                        phone = new uas_EntGrpPhone();
                        phone.GroupID = model.GroupId;
                        phone.EnterpriseID = group.EnterpriseID;
                        phone.PhoneType = "Site";
                        phone.CreatedBy = SessionHelper.LoginStatus.UserID;
                        phone.CreatedDate = DateTime.Now;
                        phone.StatusFlag = "A";
                        context.uas_EntGrpPhone.Add(phone);
                    }
                    else
                    {
                        phone.ModifiedBy = SessionHelper.LoginStatus.UserID;
                        phone.ModifiedDate = DateTime.Now;
                    }
                    phone.PhoneNumber = model.ContactPhone ?? " ";
                    phone.CityAreaCode = model.ContactPerson;



                    var email = context.uas_EntGrpEmail.FirstOrDefault(x => x.GroupID == model.GroupId);
                    if (email == null)
                    {
                        email = new uas_EntGrpEmail();
                        email.GroupID = group.GroupID;
                        email.EnterpriseID = group.EnterpriseID;
                        email.CreatedBy = SessionHelper.LoginStatus.UserID;
                        email.CreatedDate = DateTime.Now;
                        email.StatusFlag = "A";
                        context.uas_EntGrpEmail.Add(email);
                    }
                    else
                    {
                        email.ModifiedBy = SessionHelper.LoginStatus.UserID;
                        email.ModifiedDate = DateTime.Now;
                    }
                    email.EmailAddress = model.ContactEmail;

                    context.SaveChanges();
                    return RedirectToAction("EnrollmentSite");
                }
                catch (Exception ex)
                {
                    if (ex.IsCritical()) {
                        throw ex;
                    }
                    mLogger.Error(ex, "Caught exception saving enrollment site.");
                }
            }
            LoadJurisdictions(model, context);
            return View(model);
        }

        private static void LoadJurisdictions(EnrollmentSiteModel model, UASEntities context)
        {
            var jurisdictions = from g in context.uas_Group
                                where g.GroupTypeID == 195
                                select g;
            model.Units = (from j in jurisdictions
                                   select new SelectListItem()
                                   {
                                       Text = j.GroupName,
                                       Value = j.GroupID.ToString()
                                   }).ToList();
        }

        [HttpGet]
        public ActionResult EditEnrollmentSite(int id)
        {
            var model = new EnrollmentSiteModel();
            var context = DataContext.getUasDbContext();
            var jurisdictions = from g in context.uas_Group
                                where g.GroupTypeID == 195
                                select g;
            model.GroupId = id;
            model.Units = (from j in jurisdictions
                                   select new SelectListItem()
                                   {
                                       Text = j.GroupName,
                                       Value = j.GroupID.ToString()
                                   }).ToList();
            var group = context.uas_Group.SingleOrDefault(x => x.GroupID == id);
            model.Unit = group.ParentGroupId.ToString();
            model.EnrollmentSite = group.GroupDescription;
            model.SiteNumber = group.GroupName;
            var address = context.uas_EntGrpAddress.FirstOrDefault(x => x.GroupID == id);
            if (address != null)
            {
                model.Address1 = address.Address1;
                model.Address2 = address.Address2;
                model.City = address.City;
                model.State = address.StateProvince;
                model.ZipCode = address.PostalCode;
                if (!string.IsNullOrWhiteSpace(address.URL))
                {
                    if (address.URL.Contains(";"))
                    {
                        var dates = address.URL.Split(';');
                        model.StartDate = DateTime.Parse(dates[0]);
                        model.EndDate = DateTime.Parse(dates[1]);
                    }
                }
            }
            model.Restrictions = group.Notes;
            var phone = context.uas_EntGrpPhone.FirstOrDefault(x => x.GroupID == id);

            if (phone != null)
            {
                model.ContactPhone = phone.PhoneNumber;
                model.ContactPerson = phone.CityAreaCode;
            }

            var email = context.uas_EntGrpEmail.FirstOrDefault(x => x.GroupID == id);

            if (email != null)
            {
                model.ContactEmail = email.EmailAddress;
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult EditJurisdiction(JurisdictionUnitModel model)
        {
            var context = DataContext.getUasDbContext();

            if (ModelState.IsValid)
            {
                var group = context.uas_Group.SingleOrDefault(x => x.GroupID == model.GroupId);
                int unitId = 0;
                int.TryParse(model.Unit, out unitId);
                group.ParentGroupId = unitId;
                context.SaveChanges();
                return RedirectToAction("Jurisdiction");
            }

            LoadJurisdictionDropdowns(context, model);
            return View(model);
        }

        [HttpGet]
        public ActionResult EditJurisdiction(int id)
        {
            var context = DataContext.getUasDbContext();
            var group = context.uas_Group.SingleOrDefault(x => x.GroupID == id);
            var model = new JurisdictionUnitModel();
            LoadJurisdictionDropdowns(context, model);
            model.Jurisdiction = group.GroupName;
            model.Unit = group.ParentGroupId.ToString();
            model.GroupId = id;
            return View(model);
        }


        private static void LoadJurisdictionDropdowns(UASEntities context, JurisdictionUnitModel model)
        {
            var units = from g in context.uas_Group
                        where g.GroupTypeID == 195
                        select g;
            model.Units = (from u in units
                           select new SelectListItem()
                           {
                               Text = u.GroupName,
                               Value = u.GroupID.ToString()
                           }).ToList();
        }

        /// <summary>
        ///  Web Service to process requests from the Enrollment Site DataTable
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        public string DataTableEnrollmentSite(int draw, int start, int length, string searchValue, string searchRegex, string order)
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

            var sUnit = Request.Params["columns[1][search][value]"];
            var sSiteName = Request.Params["columns[2][search][value]"];
            int iniIndex = start;
            int noDsplyRecs = length;

            var context = DataContext.getUasDbContext();
            IEnumerable<vEnrollmentSite> query = (from v in context.vEnrollmentSites
                                                  where (string.IsNullOrEmpty(sUnit) || v.Jurisdiction.Contains(sUnit))
                                                  && (string.IsNullOrEmpty(sSiteName) || v.Enrollment_Site.Contains(sSiteName))
                                                  select v).ToList();


            if (Request.Params["order[0][column]"] != null)
            {
                int orderColumnIndex = Convert.ToInt32(Request.Params["order[0][column]"]);
                bool descending = Request.Params["order[0][dir]"] == "desc";
                query = query.OrderBy(row => GetEnrollmentSiteSort(row, orderColumnIndex));
                if (descending)
                    query = query.Reverse();
            }
            else
            {
                query = query.OrderBy(x => x.Site__).ToList();
            }

            var sites = query.Skip(iniIndex).Take(noDsplyRecs).ToList();

            dtr.recordsTotal = dtr.recordsFiltered = query.Count();

            var data = from v in sites
                       select new List<string>
                                         {
                                            "<a href=\"/AdapCa/EditEnrollmentSite?id=" + v.GroupId + "\"><i class=\"glyphicon glyphicon-edit\"></i></a>",
                                             v.Jurisdiction,
                                             v.Site__,
                                             v.Restrictions,
                                             v.Enrollment_Site,
                                             v.Address,
                                             v.Telephone,
                                             v.Contact
                                         };

            dtr.data = (from s in data
                        select s.ToArray()).ToList();
            fastJSON.JSONParameters param = new fastJSON.JSONParameters();
            param.EnableAnonymousTypes = true;
            var result = fastJSON.JSON.ToJSON(dtr, param);
            return result;
        }

        private IComparable GetEnrollmentSiteSort(vEnrollmentSite site, int index)
        {
            switch (index)
            {
                case 0:
                    return 0;
                case 1:
                    return site.Jurisdiction;
                case 2:
                    return site.Site__;
                case 3:
                    return site.Restrictions;
                case 4:
                    return site.Enrollment_Site;
                case 5:
                    return site.Address;
                case 6:
                    return site.Telephone;
                case 7:
                    return site.Contact;
                default:
                    throw new Exception("unsupported sorting column index: " + index);
            }
        }

        /// <summary>
        ///  Web Service to process requests from the Enrollment Site DataTable
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        public string DataTableJurisdiction(int draw, int start, int length, string searchValue, string searchRegex, string order)
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

            var sJurisdiction = Request.Params["columns[1][search][value]"];
            var sUnit = Request.Params["columns[2][search][value]"];
            int iniIndex = start;
            int noDsplyRecs = length;
            int jurisdiction = -1;
            bool haveUnit = false;
            bool haveJurisdiction = false;

            if (int.TryParse(sJurisdiction, out jurisdiction))
            {
                haveJurisdiction = true;
            }

            if (!string.IsNullOrEmpty(sUnit))
            {
                haveUnit = true;
            }

            var context = DataContext.getUasDbContext();
            var query = (from gr in context.uas_Group
                         join e in context.uas_Group on gr.ParentGroupId equals e.GroupID into eg // left join uas_Group
                         from elj in
                             (from g in eg
                              select new
                              {
                                  g.GroupID,
                                  g.GroupName
                              }
                             ).DefaultIfEmpty()
                         where gr.GroupTypeID == 194
                         && (haveJurisdiction == false || gr.GroupID == jurisdiction)
                         && (haveUnit == false || elj.GroupName.StartsWith(sUnit))
                         orderby gr.GroupName
                         let Unit = elj.GroupName
                         select new
                         {
                             gr.GroupID,
                             gr.GroupName,
                             Unit
                         }).ToList();


            if (Request.Params["order[0][column]"] != null)
            {
                int orderColumnIndex = Convert.ToInt32(Request.Params["order[0][column]"]);
                bool descending = Request.Params["order[0][dir]"] == "desc";
                query = query.OrderBy(row => GetJurisdictionSort(row, orderColumnIndex)).ToList();
                if (descending)
                    query.Reverse();
            }

            var jurisdicions = query.Skip(iniIndex).Take(noDsplyRecs);

            dtr.recordsTotal = dtr.recordsFiltered = query.Count();

            var data = from j in jurisdicions
                       select new List<String>
                        {
                            "<a href=\"/AdapCa/EditJurisdiction?id=" + j.GroupID + "\"><i class=\"glyphicon glyphicon-edit\"></i></a>",
                            j.GroupName,
                            j.Unit
                        };

            dtr.data = (from s in data
                        select s.ToArray()).ToList();
            fastJSON.JSONParameters param = new fastJSON.JSONParameters();
            param.EnableAnonymousTypes = true;
            var result = fastJSON.JSON.ToJSON(dtr, param);
            return result;
        }

        IComparable GetJurisdictionSort(dynamic row, int index)
        {
            switch (index)
            {
                case 0:
                    return 0;
                case 1:
                    return row.GroupName;
                case 2:
                    return row.Unit;
                default:
                    throw new Exception("unsupported sorting column index: " + index);
            }
        }

       
    }
}