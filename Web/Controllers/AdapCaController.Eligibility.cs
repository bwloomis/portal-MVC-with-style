using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using AJBoggs.Adap.Domain;
using AJBoggs.Def.Services;
using Assmnts.Business;
using Assmnts.Business.Uploads;
using Assmnts.Infrastructure;
using Assmnts.Models;
using Assmnts.Reports;
using Assmnts.UasServiceRef;
using Data.Abstract;
using UAS.Business;

namespace Assmnts.Controllers
{
    public partial class AdapCaController : Controller
    {
        public ActionResult ShowEligibility()
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            if (SessionHelper.SessionForm == null)
            {
                SessionHelper.SessionForm = new SessionForm();
            }

            // retrieve and set SessionForm params
            string formIdent = "CA-ADAP-DASHBOARD", entName = "California";
            def_Forms frm = formsRepo.GetFormByIdentifier(formIdent);
            Enterprise ent = new AuthenticationClient().GetEnterpriseByName(entName);
            if (frm == null)
                return Content("Could not find form with identifier \"" + formIdent + "\"");

            if (ent == null)
                return Content("Could not find enterprise with name \"" + entName + "\"");

            //  Get the FormResult to get the subject of the current Application
            def_FormResults fr = formsRepo.GetFormResultById(SessionHelper.SessionForm.formResultId);
            if (fr == null)
                return Content("Could not find the current Application  \"" + SessionHelper.SessionForm.formResultId.ToString() + "\"");

            // Get the Eligibility Form for the subject
            IEnumerable<def_FormResults> frElgList = formsRepo.GetFormResultsByFormSubject(frm.formId, fr.subject);
            def_FormResults frElg = null;
            if ((frElgList == null) || frElgList.Count<def_FormResults>() == 0)
            {
                mLogger.Debug("Couldn't find a current FormResult for Eligibility.  Create one.");
                frElg = new def_FormResults()
                {
                    formId = frm.formId,
                    formStatus = 0,
                    sessionStatus = 0,
                    dateUpdated = DateTime.Now,
                    deleted = false,
                    locked = false,
                    archived = false,
                    EnterpriseID = ent.EnterpriseID,
                    GroupID = fr.GroupID,        // User the same Enrollment Center
                    subject = fr.subject,
                    interviewer = fr.interviewer,
                    assigned = fr.assigned,
                    training = false,
                    reviewStatus = 0,
                    statusChangeDate = DateTime.Now,
                    LastModifiedByUserId = fr.LastModifiedByUserId
                };

                AuthenticationClient authClient = new AuthenticationClient();
                var groupIds = authClient.GetGroupsInUserPermissions(SessionHelper.LoginStatus.EnterpriseID, SessionHelper.LoginStatus.UserID).Select(x => x.GroupID);
                var groupId = groupIds.FirstOrDefault();

                Dictionary<string, string> uasItemDictionary = new Dictionary<string, string>();
                var uasData = authClient.GetUser(fr.subject.Value);

                var adapId = authClient.GetExistingAdapIdentifier(uasData.UserID, uasData.EnterpriseID);

                // ADAP ID
                if (!String.IsNullOrWhiteSpace(adapId))
                {
                    uasItemDictionary.Add("C1_MemberIdentifier_item", adapId);
                }

                Applications appl = new Applications(formsRepo);

                //Populate from UAS
                //Populate items where Source = UAS in json file
                frElg = appl.CreateFormResultPopulatedFromUAS(SessionHelper.LoginStatus.EnterpriseID, groupId, uasData.UserID, frm.formId, uasItemDictionary);

                int newFrmRsltId = formsRepo.AddFormResult(frElg);
                mLogger.Debug("New Eligibility FormResult created: {0}", newFrmRsltId);
            }
            else
            {
                frElg = frElgList.First<def_FormResults>();
            }


            SessionHelper.SessionForm.formId = frElg.formId;
            SessionHelper.SessionForm.formResultId = frElg.formResultId;
            SessionHelper.SessionForm.formIdentifier = formIdent;
            // SessionHelper.LoginStatus.EnterpriseID = ent.EnterpriseID;


            def_Parts prt = formsRepo.GetFormParts(frm)[0];
            SessionHelper.SessionForm.partId = prt.partId;
            Session["part"] = prt.partId.ToString();
            def_Sections sct = formsRepo.GetSectionsInPart(prt)[1];
            Session["section"] = sct.sectionId.ToString();

            return RedirectToAction("Template", "Results", new { sectionId = sct.sectionId.ToString(), partId = SessionHelper.SessionForm.partId.ToString() });
        }

        public void CreateElgibility(int formResultId)
        {
            string formIdent = "CA-ADAP-DASHBOARD", entName = "California";
            def_Forms frm = formsRepo.GetFormByIdentifier(formIdent);
            Enterprise ent = new AuthenticationClient().GetEnterpriseByName(entName);

            //  Get the FormResult to get the subject of the current Application
            def_FormResults fr = formsRepo.GetFormResultById(formResultId);

            // Get the Eligibility Form for the subject
            IEnumerable<def_FormResults> frElgList = formsRepo.GetFormResultsByFormSubject(frm.formId, fr.subject);
            def_FormResults frmRes = null;
            if ((frElgList == null) || frElgList.Count<def_FormResults>() == 0)
            {
                mLogger.Debug("Couldn't find a current FormResult for Eligibility.  Create one.");
                frmRes = new def_FormResults()
                {
                    formId = frm.formId,
                    formStatus = 0,
                    sessionStatus = 0,
                    dateUpdated = DateTime.Now,
                    deleted = false,
                    locked = false,
                    archived = false,
                    EnterpriseID = ent.EnterpriseID,
                    GroupID = fr.GroupID,        // User the same Enrollment Center
                    subject = fr.subject,
                    interviewer = fr.interviewer,
                    assigned = fr.assigned,
                    training = false,
                    reviewStatus = 0,
                    statusChangeDate = DateTime.Now,
                    LastModifiedByUserId = fr.LastModifiedByUserId
                };

                int newFrmRsltId = formsRepo.AddFormResult(frmRes);
                mLogger.Debug("New Eligibility FormResult created: {0}", newFrmRsltId);
            }
            else
            {
                frmRes = frElgList.First<def_FormResults>();
            }

            // make sure item responses exist for the form
            Dictionary<string, string> DataToPopulate = new Dictionary<string, string>();
            var sectionItems = formsRepo.GetSectionItemsBySectionId(756);

            AuthenticationClient authClient = new AuthenticationClient();
            var uasData = authClient.GetUser(fr.subject.Value);
            var adapId = authClient.GetADAPIdentifier(uasData.UserID, uasData.EnterpriseID);

            foreach (var item in sectionItems)
            {
                var defItem = formsRepo.GetItemById(item.itemId);

                if (defItem.identifier == "C1_MemberIdentifier_item")
                {
                    DataToPopulate.Add(defItem.identifier, adapId);
                }
                else if (defItem.identifier == "C1_MemberFirstName_item")
                {
                    def_ResponseVariables firstName = formsRepo.GetResponseVariablesBySubjectForm(fr.subject.Value, 15, "C1_MemberFirstName");
                    DataToPopulate.Add(defItem.identifier, firstName != null ? firstName.rspValue : string.Empty);
                }
                else if (defItem.identifier == "C1_MemberLastName_item")
                {
                    def_ResponseVariables lastName = formsRepo.GetResponseVariablesBySubjectForm(fr.subject.Value, 15, "C1_MemberLastName");
                    DataToPopulate.Add(defItem.identifier, lastName != null ? lastName.rspValue : string.Empty);
                }
                else if (defItem.identifier == "C1_MemberDateOfBirth_item")
                {
                    def_ResponseVariables dob = formsRepo.GetResponseVariablesBySubjectForm(fr.subject.Value, 15, "C1_MemberDateOfBirth");
                    DataToPopulate.Add(defItem.identifier, dob != null ? dob.rspValue : string.Empty);
                }
                else
                {
                    DataToPopulate.Add(defItem.identifier, string.Empty);
                }
            }

            foreach (String s in DataToPopulate.Keys)
            {
                def_Items item = formsRepo.GetItemByIdentifier(s);
                def_ItemResults ir = new def_ItemResults()
                {
                    itemId = item.itemId,
                    sessionStatus = 0,
                    dateUpdated = DateTime.Now
                };

                // check if the key already exist
                var itemResult = formsRepo.GetItemResultByFormResItem(frmRes.formResultId, item.itemId);
                if (itemResult == null)
                {
                    frmRes.def_ItemResults.Add(ir);

                    foreach (var iv in formsRepo.GetItemVariablesByItemId(item.itemId))
                    {
                        // Note for General forms like ADAP there should only be 1 ItemVariable per Item
                        def_ResponseVariables rv = new def_ResponseVariables();
                        rv.itemVariableId = iv.itemVariableId;
                        // rv.rspDate = DateTime.Now;    // RRB 11/11/15 The date, fp, and int fields are for the native data conversion.
                        rv.rspValue = DataToPopulate[s];

                        formsRepo.ConvertValueToNativeType(iv, rv);
                        ir.def_ResponseVariables.Add(rv);
                    }
                }
                else
                {
                    ir = itemResult;
                }
            }

            formsRepo.Save();
        }

        public void CreateEligibilityField(string identifier)
        {
            //  Get the FormResult to get the subject of the current Application
            def_FormResults fr = formsRepo.GetFormResultById(SessionHelper.SessionForm.formResultId);

            // Get the Eligibility Form for the subject
            IEnumerable<def_FormResults> frElgList = formsRepo.GetFormResultsByFormSubject(18, fr.subject);
            var frmRes = frElgList.First<def_FormResults>();
            Dictionary<string, string> DataToPopulate = new Dictionary<string, string>();
            DataToPopulate.Add(identifier, string.Empty);

            foreach (String s in DataToPopulate.Keys)
            {
                def_Items item = formsRepo.GetItemByIdentifier(s + "_item");
                def_ItemResults ir = new def_ItemResults()
                {
                    itemId = item.itemId,
                    sessionStatus = 0,
                    dateUpdated = DateTime.Now
                };

                var itemResult = formsRepo.GetItemResultByFormResItem(frmRes.formResultId, item.itemId);
                if (itemResult == null)
                {
                    frmRes.def_ItemResults.Add(ir);
                }
                else
                {
                    ir = itemResult;
                }

                foreach (var iv in formsRepo.GetItemVariablesByItemId(item.itemId))
                {
                    // Note for General forms like ADAP there should only be 1 ItemVariable per Item
                    def_ResponseVariables rv = new def_ResponseVariables();
                    rv.itemVariableId = iv.itemVariableId;
                    // rv.rspDate = DateTime.Now;    // RRB 11/11/15 The date, fp, and int fields are for the native data conversion.
                    rv.rspValue = DataToPopulate[s];

                    formsRepo.ConvertValueToNativeType(iv, rv);
                    ir.def_ResponseVariables.Add(rv);
                }
            }

            formsRepo.Save();
        }
    }
}