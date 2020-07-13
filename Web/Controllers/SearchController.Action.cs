using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Concrete;
using Assmnts.Business;
using AJBoggs.Sis.Domain;


namespace Assmnts.Controllers
{
	/* This partial Controller handles the Action functions for an Assessment
	 * It includes the New Blank Assessment button.
	 *
	 */
	
    public partial class SearchController : Controller
    {

        [HttpPost]
        public ActionResult GoAction(FormCollection formCollection)
        {
            string recId = formCollection["recId"];
            string paramFormResultId = formCollection["sisId"];
            string paramFormId = formCollection["formId"];
            string category = formCollection["CategoryID"];
            SearchModel model = new SearchModel();

            if (SessionHelper.SessionForm == null)
            {
                // return RedirectToAction("Index", "Search", null);
                SessionHelper.SessionForm = new SessionForm();
            }
            SessionForm sf = SessionHelper.SessionForm;

            if (String.IsNullOrWhiteSpace(category))
            {
                return RedirectToAction("Index", "Search");
            } 
            else if (category.ToLower().Equals("edit") && model.edit)
            {

                // retrieve and set SessionForm params
                sf.formId = Convert.ToInt32(paramFormId);
                sf.formResultId = Convert.ToInt32(paramFormResultId);

                def_FormResults formResult = formsRepo.GetFormResultById(sf.formResultId);

                if (!formResult.locked || (formResult.locked && model.editLocked))
                {

                    // get the sectionId of the first section of the first part based on the formId
                    def_Forms frm = formsRepo.GetFormById(sf.formId);
                    def_Parts prt = formsRepo.GetFormParts(frm)[0];
                    sf.partId = prt.partId;
                    Session["part"] = prt.partId;
                    def_Sections sct = formsRepo.GetSectionsInPart(prt)[0];

                    if (ventureMode == false)
                    {
                        AccessLogging.InsertAccessLogRecord(formsRepo, sf.formResultId, (int)AccessLogging.accessLogFunctions.EDIT, "Initiate editing of assessment.");
                    }
                    return RedirectToAction("Template", "Results", new { sectionId = sct.sectionId.ToString(), partId = sf.partId.ToString() });
                }
            }
            else if (category.ToLower().Equals("review"))
            {
                 // retrieve and set SessionForm params
                sf.formId = Convert.ToInt32(paramFormId);
                sf.formResultId = Convert.ToInt32(paramFormResultId);

                def_FormResults formResult = formsRepo.GetFormResultById(sf.formResultId);

                if (model.editLocked)
                {

                    if (ventureMode == false)
                    {
                        AccessLogging.InsertAccessLogRecord(formsRepo, sf.formResultId, (int)AccessLogging.accessLogFunctions.REVIEW, "Assessment accessed for review.");
                    }                    
                    
                    if (model.unlock == true)
                    {
                        formsRepo.LockFormResult(formResult.formResultId);
                    }

                    if (formResult.reviewStatus != (int)ReviewStatus.REVIEWED && formResult.reviewStatus != (int)ReviewStatus.APPROVED && formResult.reviewStatus != (int)ReviewStatus.PRE_QA)
                    {
                        def_FormResults preQAcopy = AssessmentCopy.CopyAssessment(formsRepo, formResult.formResultId);

                        if (WebServiceActivity.IsWebServiceEnabled())
                        {
                            WebServiceActivity.CallWebService(formsRepo, (int)WebServiceActivity.webServiceActivityFunctions.REVIEW, "formResultId=" + preQAcopy.formResultId.ToString());
                        }
                    }

                    ReviewStatus.ChangeStatus(formsRepo, formResult, ReviewStatus.REVIEWED, "Initiate review of assessment");
                    
                    // get the sectionId of the first section of the first part based on the formId
                    def_Forms frm = formsRepo.GetFormById(sf.formId);
                    def_Parts prt = formsRepo.GetFormParts(frm)[0];
                    sf.partId = prt.partId;
                    Session["part"] = prt.partId;
                    def_Sections sct = formsRepo.GetSectionsInPart(prt)[0];


                    return RedirectToAction("Template", "Results", new { sectionId = sct.sectionId.ToString(), partId = sf.partId.ToString() });
                }
            }
            else if (category.ToLower().Equals("approve"))
            {
                // retrieve and set SessionForm params
                sf.formId = Convert.ToInt32(paramFormId);
                sf.formResultId = Convert.ToInt32(paramFormResultId);

                def_FormResults formResult = formsRepo.GetFormResultById(sf.formResultId);

                if (model.editLocked)
                {

                    ReviewStatus.ChangeStatus(formsRepo, formResult, ReviewStatus.APPROVED, "Approve review of assessment");

                    if (WebServiceActivity.IsWebServiceEnabled())
                    {
                        WebServiceActivity.CallWebService(formsRepo, (int)WebServiceActivity.webServiceActivityFunctions.APPROVE, "formResultId=" + formResult.formResultId.ToString());
                    }

                }
            }
            else if (category.ToLower().Equals("create"))
            {
                if (model.create == true)
                {
                    string formId = String.IsNullOrEmpty(paramFormId) ? "1" : paramFormId;
                    def_FormResults frmRes = FormResults.CreateNewFormResultFull(formId,
                                                                                SessionHelper.LoginStatus.EnterpriseID.ToString(),
                                                                                SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].GroupID.ToString(),
                                                                                recId,
                                                                                SessionHelper.LoginStatus.UserID.ToString()
                                                                            );
                    string strFormResultId = String.Empty;
                    try
                    {
                        int formRsltId = formsRepo.AddFormResult(frmRes);
                        strFormResultId = formRsltId.ToString();
                        Debug.WriteLine("GoAction create FormResultId strFormResultId:" + strFormResultId);
                    }
                    catch (Exception excptn)
                    {
                        Debug.WriteLine("GoAction Defws.CreateNewFormResultFull formsRepo.AddFormResult  exception:" + excptn.Message);
                        Debug.WriteLine("   GoAction formId:" + formId
                            + "   entId: " + SessionHelper.LoginStatus.EnterpriseID.ToString()
                            + "   GroupId: " + SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].GroupID.ToString()
                            + "   recId: " + recId.ToString()
                            + "   UserId: " + SessionHelper.LoginStatus.UserID.ToString()
                            );
                    }

                    if ((!String.IsNullOrEmpty(strFormResultId)) && !String.IsNullOrEmpty(recId))
                    {

                        int intRecId;

                        if (int.TryParse(recId, out intRecId))
                        {
                            using (var context = DataContext.getSisDbContext())
                            {
                                // Contact tempContact = new Contact();
                                Contact tempContact = (from c in context.Contacts
                                                       where c.ContactID == intRecId
                                                       select c).FirstOrDefault();

                                // Address tempAddress = new Address();
                                Address tempAddress = (from a in context.Addresses
                                                       where (a.ContactID == tempContact.ContactID) &&
                                                               (a.AddressType == "R")
                                                       select a).FirstOrDefault();

                                Dictionary<string, string> responsesByIdentifier = new Dictionary<string, string>();
                                responsesByIdentifier.Add("sis_cl_first_nm", tempContact.FirstName);
                                responsesByIdentifier.Add("sis_cl_last_nm", tempContact.LastName);

                                if (tempAddress != null)
                                {
                                    responsesByIdentifier.Add("sis_cl_addr_line1", tempAddress.Address1);
                                    responsesByIdentifier.Add("sis_cl_city", tempAddress.City);
                                    responsesByIdentifier.Add("sis_cl_st", tempAddress.StateCode);
                                    responsesByIdentifier.Add("sis_cl_zip", tempAddress.Zip);
                                }

                                string msg = formsRepo.CreateNewResponseValues(strFormResultId, responsesByIdentifier);
                                Debug.WriteLine("GoAction CreateNewResponseValues strFormResultId: " + strFormResultId + "    msg: " + msg);
                            }
                        }

                        // Retrieve and set SessionForm params
                        sf.formId = 1; // Temporary
                        sf.formResultId = Convert.ToInt32(strFormResultId);

                        // Get the sectionId of the first section of the first part based on the formId
                        def_Forms frm = formsRepo.GetFormById(sf.formId);
                        def_Parts prt = formsRepo.GetFormParts(frm)[0];
                        sf.partId = prt.partId;
                        Session["part"] = prt.partId;
                        def_Sections sct = formsRepo.GetSectionsInPart(prt)[0];

                        return RedirectToAction("Template", "Results", new { sectionId = sct.sectionId.ToString(), partId = sf.partId.ToString() });
                    }
                }
            }
            else if (category.ToLower().Equals("delete"))
            {
                if (model.delete == true)
                {
                    formsRepo.FormResultDeleteLogically(Convert.ToInt32(paramFormResultId));

                    if (ventureMode == false)
                    {
                        AccessLogging.InsertAccessLogRecord(formsRepo, Convert.ToInt32(paramFormResultId), (int)AccessLogging.accessLogFunctions.DELETE, "Delete assessment.");
                    }
                    
                    
                    if (ventureMode == false && WebServiceActivity.IsWebServiceEnabled())
                    {
                        WebServiceActivity.CallWebService(formsRepo, (int)WebServiceActivity.webServiceActivityFunctions.DELETE, "formResultId=" + paramFormResultId);
                        def_FormResults preQAcopy = ReviewStatus.GetLatestPreQACopy(formsRepo, Convert.ToInt32(paramFormResultId));

                        if (preQAcopy != null)
                        {
                            formsRepo.FormResultDeleteLogically(preQAcopy.formResultId);
                            WebServiceActivity.CallWebService(formsRepo, (int)WebServiceActivity.webServiceActivityFunctions.DELETE, "formResultId=" + preQAcopy.formResultId.ToString());
                        }
                    }
                }
            }
            else if (category.ToLower().Equals("lock"))
            {
                if (model.unlock == true) 
                {
                   formsRepo.LockFormResult(Convert.ToInt32(paramFormResultId));
                }
            }
            else if (category.ToLower().Equals("unlock"))
            {
                if (model.unlock == true)
                {
                    formsRepo.UnlockFormResult(Convert.ToInt32(paramFormResultId));
                }
            }
            else if (category.ToLower().Equals("archive"))
            {
                if (model.archive == true) 
                {
                    formsRepo.ArchiveFormResult(Convert.ToInt32(paramFormResultId));

                    if (ventureMode == false)
                    {
                        AccessLogging.InsertAccessLogRecord(formsRepo, Convert.ToInt32(paramFormResultId), (int)AccessLogging.accessLogFunctions.ARCHIVE, "Archive assessment.");
                    }
                    
                }
            }
            else if (category.ToLower().Equals("unarchive"))
            {
                if (model.archive == true)
                {
                    formsRepo.UnarchiveFormResult(Convert.ToInt32(paramFormResultId));

                    if (ventureMode == false)
                    {
                        AccessLogging.InsertAccessLogRecord(formsRepo, Convert.ToInt32(paramFormResultId), (int)AccessLogging.accessLogFunctions.UNARCHIVE, "Unarchive assessment.");
                    }
                    
                }
            }
            else if (category.ToLower().Equals("upload"))
            {
                def_FormResults fr = formsRepo.GetFormResultById(Convert.ToInt32(paramFormResultId));

                if (fr.formStatus == (byte)FormResults_formStatus.COMPLETED)
                {
                    SessionHelper.Write("uploadFormResultId", Convert.ToInt32(paramFormResultId));
                    return RedirectToAction("UploadSingle", "DataSync");
                }
            }
            else if (category.ToLower().Equals("undelete"))
            {
                if (model.undelete == true)
                {
                    formsRepo.FormResultUndelete(Convert.ToInt32(paramFormResultId));

                    if (ventureMode == false)
                    {
                        AccessLogging.InsertAccessLogRecord(formsRepo, Convert.ToInt32(paramFormResultId), (int)AccessLogging.accessLogFunctions.UNDELETE, "Undelete assessment.");
                    }
                    
                }
            }
            else if (category.ToLower().Equals("planning") && model.edit)
            {
                // retrieve and set SessionForm params
                sf.formId = Convert.ToInt32(paramFormId);
                sf.formResultId = Convert.ToInt32(paramFormResultId);

                def_FormResults formResult = formsRepo.GetFormResultById(sf.formResultId);

                if (!formResult.locked || (formResult.locked && model.editLocked))
                {

                    // get the sectionId of the first section of the first part based on the formId
                    def_Forms frm = formsRepo.GetFormById(sf.formId);
                    def_Parts prt = formsRepo.GetPartByFormAndIdentifier(frm, "Other");
                    sf.partId = prt.partId;
                    Session["part"] = prt.partId;
                    def_Sections sct = formsRepo.GetSectionsInPart(prt)[0];

                    if (ventureMode == false)
                    {
                        AccessLogging.InsertAccessLogRecord(formsRepo, sf.formResultId, (int)AccessLogging.accessLogFunctions.EDIT, "Initiate editing of interview planning.");
                    }
                    return RedirectToAction("Template", "Results", new { sectionId = sct.sectionId.ToString(), partId = sf.partId.ToString() });
                }
            }

            return RedirectToAction("Index", "Search");
        }

        [HttpGet]
        public ActionResult NewBlankAssessment()
        {
            string formId = Request["formId"];
            
            SearchModel model = new SearchModel();

            if (model.create && model.edit)
            {
                /*
                 * **** Check if User has authorization to create an Assessment for this Group.
                 */
                // *** RRB 10/27/15 - the Create Assessment should be a method used here and from the 'Action' above.
                def_FormResults frmRes = FormResults.CreateNewFormResultFull(formId,
                                                                        SessionHelper.LoginStatus.EnterpriseID.ToString(),
                                                                        SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].GroupID.ToString(),
                                                                        "0",
                                                                        SessionHelper.LoginStatus.UserID.ToString());
                if (ventureMode)
                {
                    frmRes.assigned = SessionHelper.LoginStatus.UserID;
                }
                
                formsRepo.AddFormResult(frmRes);

                if (SessionHelper.SessionForm == null)
                {
                    SessionHelper.SessionForm = new SessionForm();
                }
                SessionForm sf = SessionHelper.SessionForm;
                /*
                if (sf == null)
                {
                    return RedirectToAction("Index", "Search", null);
                }
                */
                // set SessionForm params
                sf.formId = frmRes.formId;                          // TODO: How to determine which formid?
                sf.formResultId = frmRes.formResultId;

                // get the sectionId of the first section of the first part based on the formId
                def_Forms frm = formsRepo.GetFormById(sf.formId);
                def_Parts prt = formsRepo.GetFormParts(frm)[0];
                sf.partId = prt.partId;
                Session["part"] = prt.partId;
                def_Sections sct = formsRepo.GetSectionsInPart(prt)[0];

                return RedirectToAction("Template", "Results", new { sectionId = sct.sectionId.ToString(), partId = sf.partId.ToString() });
            }
            else
            {
                return RedirectToAction("Index", "Search");
            }
        }

    }
}