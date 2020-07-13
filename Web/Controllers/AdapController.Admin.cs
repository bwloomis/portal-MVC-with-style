using Assmnts.Models;
using Assmnts.Infrastructure;

using Data.Abstract;
using Data.Concrete;

using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Assmnts.Controllers
{
    public partial class AdapController : Controller
    {

        /// <summary>
        /// Loads the page to create a new Team to an Enterprise.
        /// </summary>
        /// <param name="lookupDetailId">Identifier for the selected Team.</param>
        /// <param name="enterpriseId">Identifier for the current Enterprise.</param>
        /// <returns>Redirects the user to the Save Detail page.</returns>
        public ActionResult AddDetail(int lookupMasterId, int enterpriseId = 0)
        {
            AdapAdmin model = new AdapAdmin();
            model.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;

            IUasSql uasSql = new UasSql();

            Dictionary<int, string> groups = uasSql.getGroups(enterpriseId);

            //groups.Add(0, "Enterprise Wide");

            model.groups = (new SelectList(groups.OrderBy(x => x.Key), "key", "value")).ToList();

            model.enterpriseId = enterpriseId;
            model.groupId = 0;
            model.lookupMasterId = lookupMasterId;
            model.statusFlag = true;

            return View("~/Views/COADAP/ListItems/SaveDetail.cshtml", model);
        }

        /// <summary>
        /// Loads the page to edit a selected Team.
        /// </summary>
        /// <param name="lookupDetailId">Identifier for the selected Team.</param>
        /// <returns>Redirects the user to the Save Detail page.</returns>
        [HttpGet]
        public ActionResult EditDetail(int lookupDetailId)
        {
            AdapAdmin model = new AdapAdmin();
            model.ActiveUserName = SessionHelper.LoginStatus.FirstName + " " + SessionHelper.LoginStatus.LastName;

            def_LookupDetail detail = formsRepo.GetLookupDetailById(lookupDetailId);


            if (detail != null)
            {
                IUasSql uasSql = new UasSql();

                if (detail.EnterpriseID != null)
                {

                    Dictionary<int, string> groups = uasSql.getGroups(detail.EnterpriseID.Value);
                    groups.Add(0, "Enterprise Wide");
                    model.groups = (new SelectList(groups.OrderBy(x => x.Key), "key", "value")).ToList();
                }
                model.lookupDetailId = lookupDetailId;
                model.lookupMasterId = detail.lookupMasterId;
                model.enterpriseId = detail.EnterpriseID;
                model.groupId = detail.GroupID;
                model.dataValue = detail.dataValue;
                model.displayOrder = detail.displayOrder;
                model.statusFlag = (detail.StatusFlag == "A") ? true : false;

                def_LookupText text = formsRepo.GetLookupTextsByLookupDetail(lookupDetailId).FirstOrDefault();

                if (text != null)
                {
                    model.displayText = text.displayText;
                }
            }

            return View("~/Views/COADAP/ListItems/SaveDetail.cshtml", model);
        }

        /// <summary>
        /// Saves changes made on the SaveDetail page.
        /// </summary>
        /// <param name="adapAdminModel">Model for the SaveDetail cshtml page.</param>
        /// <returns>Redirects the user to the Clinic Admin page.</returns>
        public ActionResult SaveDetail(AdapAdmin adapAdminModel)
        {
            def_LookupDetail detail = formsRepo.GetLookupDetailById(adapAdminModel.lookupDetailId);

            if (detail == null)
            {
                detail = new def_LookupDetail();
                detail.lookupMasterId = adapAdminModel.lookupMasterId;
                detail.EnterpriseID = adapAdminModel.enterpriseId;
                detail.GroupID = adapAdminModel.groupId;
                detail.dataValue = adapAdminModel.dataValue;
                detail.displayOrder = adapAdminModel.displayOrder;
                detail.StatusFlag = adapAdminModel.statusFlag ? "A" : "I";

                formsRepo.AddLookupDetail(detail);

            }
            else
            {
                detail.dataValue = adapAdminModel.dataValue;
                detail.displayOrder = adapAdminModel.displayOrder;
                detail.GroupID = adapAdminModel.groupId;
                detail.StatusFlag = adapAdminModel.statusFlag ? "A" : "I";

                formsRepo.SaveLookupDetail(detail);
            }

            def_LookupText text = formsRepo.GetLookupTextsByLookupDetail(detail.lookupDetailId).FirstOrDefault();
            if (text == null)
            {
                text = new def_LookupText();
                text.displayText = adapAdminModel.displayText;
                text.langId = 1; // English
                text.lookupDetailId = detail.lookupDetailId;

                formsRepo.AddLookupText(text);
            }
            else
            {
                text.displayText = adapAdminModel.displayText;

                formsRepo.SaveLookupText(text);
            }

            return RedirectToAction("Clinic", "ADAP");
        }

        /// <summary>
        /// This method is not in use and may be deprecated.
        /// </summary>
        /// <param name="clinicDataValue"></param>
        /// <param name="formResultId"></param>
        /// <param name="lookupCode"></param>
        public void UpdateTeam(string clinicDataValue, int formResultId, string lookupCode)
        {
            def_FormResults formResult = formsRepo.GetFormResultById(formResultId);

            int? enterpriseId = formResult.EnterpriseID;

            def_LookupMaster lookupMaster = formsRepo.GetLookupMastersByLookupCode(lookupCode);


            if (enterpriseId != null)
            {
                def_LookupDetail lookupDetail = formsRepo.GetLookupDetailByEnterpriseMasterAndDataValue(enterpriseId.Value, lookupMaster.lookupMasterId, clinicDataValue);

                if (lookupDetail != null && lookupDetail.GroupID != null)
                {
                    formResult.GroupID = lookupDetail.GroupID;

                    formsRepo.SaveFormResults(formResult);
                }
            }

        }
    }


}