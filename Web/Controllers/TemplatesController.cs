using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

using Assmnts;
using Assmnts.Models;
using Assmnts.Infrastructure;
using Data.Abstract;
using System.Web.Routing;


namespace Assmnts.Controllers
{
    /*
     *  This controller is used to handle forms that maintain the screen meta data.
     *  The forms are all .cshtml (Razor) and are in the Views/Templates directory (i.e., sections.cshtml).
     *  Sub-dirs to Views/Templates such as the SIS directory hold the actual Templates
     */
    [RedirectingAction]
    public class TemplatesController : Controller
    {

        private IFormsRepository formsRepo;

        public TemplatesController(IFormsRepository fr)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
        }

        [HttpGet]
        public ActionResult Index()
        {
            Debug.WriteLine("* * *  TemplatesController:Index method  * * *");

            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account", null);
            }

            if (SessionHelper.SessionForm == null)
            {
                SessionHelper.SessionForm = new SessionForm();
            }

            // Display the forms
            List<def_Forms> frms = formsRepo.GetAllForms();
            Debug.WriteLine("* * *  Index  Forms count: " + frms.Count().ToString());

            Assmnts.Models.TemplateForms tf = new Assmnts.Models.TemplateForms();
            tf.forms = frms;
            // Debug.WriteLine("* * *  FormsRepository  GetAllItems count: " + itemList.Count().ToString());

            return View("forms", tf);

        }

        #region form
        
        /// <summary>
        /// Pulls the data which populates the DataTable in the forms view.
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableForms(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            Debug.WriteLine("Templates Controller DataTableForms draw:" + draw.ToString());
            Debug.WriteLine("Templates Controller DataTableForms start:" + start.ToString());
            Debug.WriteLine("Templates Controller DataTableForms searchValue:" + searchValue);
            Debug.WriteLine("Templates Controller DataTableForms searchRegex:" + searchRegex);
            Debug.WriteLine("Templates Controller DataTableForms order:" + order);

            string result = String.Empty;
            try
            {
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

                    // rename to avoid language/library conflicts
                    int iniIndex = start;
                    int noDsplyRecs = length;

                    List<def_Forms> query = formsRepo.GetAllForms();

                    dtr.recordsTotal = dtr.recordsFiltered = query.Count();
                    int index = 0;
                    foreach (def_Forms q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                    {
                        index++;
                        string[] data = new string[] {
                            index.ToString(),
                            q.formId.ToString(),
                            q.identifier.ToString(),
                            q.title,
                            "<a href=\"/Templates/FormEdit?formId=" + q.formId + "\">Form</a>",
                            "<a href=\"/Templates/FormParts?formId=" + q.formId + "\">Parts</a>"
                        };
                        dtr.data.Add(data);
                    }

                    Debug.WriteLine("Templates Controller DataTableForms data populated.");

                    // Output the JSON in DataTable format
                    fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                    param.EnableAnonymousTypes = true;
                    result = fastJSON.JSON.ToJSON(dtr, param);
                    Debug.WriteLine("Templates Controller DataTableForms result:" + result);
                }
                catch (Exception excptn)
                {
                    Debug.WriteLine("Templates Controller DataTableForms exception:" + excptn.Message);
                    result = result + " - " + excptn.Message;
                }

            }
            catch (FormatException excptn)
            {
                Debug.WriteLine("Templates Controller DataTableForms sectionId format exception:" + excptn.Message);
                result = result + " - " + excptn.Message;
            }

            return result;

        }

        [HttpGet]
        public ActionResult FormEdit()
        {
            string formId = Request["formId"] as string;
            Debug.WriteLine("* * *  TemplatesController FormEdit formId: " + formId);

            Assmnts.Models.TemplateForms fs = new Assmnts.Models.TemplateForms();
            fs.forms = new List<def_Forms>();
            def_Forms frm = new def_Forms();

            if (!String.IsNullOrEmpty(formId))
            {
                Session["formId"] = formId;
                frm = formsRepo.GetFormById(Convert.ToInt32(formId));
                fs.forms.Add(frm);
            }
            else
            {
                Session["formId"] = 0;
                frm.EnterpriseID = SessionHelper.LoginStatus.EnterpriseID;
                fs.forms.Add(frm);
            }

            // Setup session variables
            SessionForm sf = (SessionForm)Session["sessionForm"];
            sf.formId = fs.forms[0].formId;
            Debug.WriteLine("FormEdit sf.formId: " + sf.formId.ToString());

            return View("formEdit", fs);
        }


        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult PostForm(Assmnts.Models.TemplateForms tfIn)
        {
            int frmId = tfIn.forms[0].formId;

            if (tfIn.forms[0].formId == 0)
            {
                frmId = formsRepo.AddForm(tfIn.forms[0]);
            }
            else
            {
                formsRepo.SaveForm(tfIn.forms[0]);
            }

            Assmnts.Models.TemplateForms tf = new Assmnts.Models.TemplateForms();

            return Redirect("Index");
        }
        #endregion

        #region formParts
        [HttpGet]
        public ActionResult FormParts()
        {
            string formId = Request["formId"] as string;
            Debug.WriteLine("* * *  TemplateController FormParts formId: " + formId);
            SessionHelper.SessionForm.formId = Convert.ToInt32(formId);

            def_Forms frm = formsRepo.GetFormById( SessionHelper.SessionForm.formId );
            Assmnts.Models.TemplateFormParts tf = new Assmnts.Models.TemplateFormParts();
            // tf.formParts = new List<def_FormParts>(frm.def_FormParts);
            tf.thisScreenTitle = "(" + frm.formId + ") " + frm.title;
            tf.formId = SessionHelper.SessionForm.formId;

            return View("formParts", tf);

        }

        /// <summary>
        /// Pulls the data which populates the DataTable in the form parts view.
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableFormParts(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            string result = String.Empty;
            Debug.WriteLine("Templates Controller DataTableFormParts draw:" + draw.ToString());
            Debug.WriteLine("Templates Controller DataTableFormParts start:" + start.ToString());
            Debug.WriteLine("Templates Controller DataTableFormParts searchValue:" + searchValue);
            Debug.WriteLine("Templates Controller DataTableFormParts searchRegex:" + searchRegex);
            Debug.WriteLine("Templates Controller DataTableFormParts order:" + order);
            try
            {
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

                    // rename to avoid language/library conflicts
                    int iniIndex = start;
                    int noDsplyRecs = length;

                    List<def_FormParts> query = formsRepo.GetFormPartsByFormId( SessionHelper.SessionForm.formId );

                    dtr.recordsTotal = dtr.recordsFiltered = query.Count();
                    foreach (def_FormParts q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                    {
                        string[] data = new string[] {
                            q.formPartId.ToString(),
                            q.partId.ToString(),
                            q.def_Parts.identifier,
                            q.def_Parts.title,
                            q.order.ToString(),
                            "<a href=\"/Templates/FormPartEdit?formPartId=" + q.formPartId + "\">Part</a>",
                            "<a href=\"/Templates/PartSections?partId=" + q.partId + "\">Sections</a>",
                        };
                        dtr.data.Add(data);
                    }

                    Debug.WriteLine("Templates Controller DataTableFormParts data populated.");

                    // Output the JSON in DataTable format
                    fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                    param.EnableAnonymousTypes = true;
                    result = fastJSON.JSON.ToJSON(dtr, param);
                    Debug.WriteLine("Templates Controller DataTableFormParts result:" + result);
                }
                catch (Exception excptn)
                {
                    Debug.WriteLine("Templates Controller DataTableFormParts exception:" + excptn.Message);
                    result = excptn.Message + " - " + excptn.Message;
                }

            }
            catch (FormatException excptn)
            {
                Debug.WriteLine("Templates Controller DataTableFormParts formId format exception:" + excptn.Message);
            }

            return result;

        }

        [HttpGet]
        public ActionResult FormPartEdit()
        {
            string formPartId = Request["formPartId"] as string;
            Debug.WriteLine("* * *  TemplatesController FormPartEdit formPartId: " + formPartId);

            Assmnts.Models.TemplateFormParts tf = new Assmnts.Models.TemplateFormParts();
            tf.formParts = new List<def_FormParts>();
            SessionForm sf = (SessionForm)Session["sessionForm"];

            if (!String.IsNullOrEmpty(formPartId))
            {
                Session["formPartId"] = formPartId;
                def_FormParts fp = formsRepo.GetFormPartById(Convert.ToInt32(formPartId));
                tf.formParts.Add(fp);
            }
            else
            {
                Session["formPartId"] = 0;
                def_FormParts fp = new def_FormParts();
                fp.def_Forms = formsRepo.GetFormById(sf.formId);
                fp.formId = fp.def_Forms.formId;
                tf.formParts.Add(fp);
                tf.visible = true;
            }
            // Setup session variables
            tf.formId = sf.formId;

            Debug.WriteLine("* * *  TemplatesController FormPartEdit formParts count: " + tf.formParts.Count().ToString());

            return View("formPartEdit", tf);
        }

        [HttpPost]
        public string PartList(string identifier, string title, int iniIndex, int turnPage)
        {
            iniIndex += turnPage;
            int noDsplyRecs = 10;
            IQueryable<def_Parts> parts = formsRepo.GetAllParts().OrderBy(p => p.partId);

            if (!String.IsNullOrEmpty(identifier))
            {
                parts = parts.Where(p => p.identifier.Contains(identifier));
            }

            if (!String.IsNullOrEmpty(title))
            {
                parts = parts.Where(p => p.title.Contains(title));
            }

            int last = iniIndex + noDsplyRecs;
            if (last > parts.Count())
            {
                last = parts.Count();
            }

            List<string> datas = new List<string>();
            datas.Add("page=" + iniIndex + "&endIndex=" + last + "&count=" + parts.Count());
            datas.Add("&nbsp&nbspIdentifier, Title");
            foreach (def_Parts p in parts.Skip(iniIndex).Take(noDsplyRecs).ToList())
            {
                datas.Add("<input type=\"radio\" class=\"control cols-md-1\" name=\"Parts\" value=\"id=" + p.partId 
                    + "&identifier=" + p.identifier + "&title=" + p.title + "\" >" + p.identifier + ", " + p.title + "</input>");
            }

            Debug.WriteLine("* * *  TemplatesController PartList parts count: " + (datas.Count() - 2).ToString());

            // Output the in JSON format
            fastJSON.JSONParameters param = new fastJSON.JSONParameters();
            param.EnableAnonymousTypes = true;
            string result = fastJSON.JSON.ToJSON(datas, param);

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult PostFormPart(Assmnts.Models.TemplateFormParts tsIn)
        {
            // The page updates tsIn.formParts[0].partId, other references to partId must be updated here.
            tsIn.formParts[0].def_Parts.partId = tsIn.formParts[0].partId;
            if (tsIn.partChange)
            {
                if (tsIn.formParts[0].partId == 0)
                {
                    tsIn.formParts[0].partId = formsRepo.AddPart(tsIn.formParts[0].def_Parts);
                }
                else
                {
                    formsRepo.SavePart(tsIn.formParts[0].def_Parts);
                }
                Debug.WriteLine("* * *  TemplatesController PostFormPart def_Part Updated");
            }

            // clearing def_Parts and def_Forms prevents duplicate key exceptions.
            tsIn.formParts[0].def_Parts = null;
            tsIn.formParts[0].def_Forms = null;

            if (tsIn.formPartChange)
            {
                if (tsIn.formParts[0].formPartId == 0)
                {
                    tsIn.formParts[0].formPartId = formsRepo.AddFormPart(tsIn.formParts[0]);
                }
                else
                {
                    formsRepo.SaveFormPart(tsIn.formParts[0]);
                }
                Debug.WriteLine("* * *  TemplatesController PostFormPart def_FormPart Updated");
            }

            Assmnts.Models.TemplateFormParts tf = new Assmnts.Models.TemplateFormParts();
            tf.formParts = new List<def_FormParts>();
            def_FormParts fp = formsRepo.GetFormPartById(tsIn.formParts[0].formPartId);
            tf.formParts.Add(fp);

            Session["formPartId"] = tsIn.formParts[0].formPartId.ToString();
            SessionForm sf = (SessionForm)Session["sessionForm"];
            Session["formId"] = sf.formId;
            tf.thisScreenTitle = "(" + sf.formId + ") " + formsRepo.GetFormById(sf.formId).title;

            Debug.WriteLine("* * *  TemplatesController PostFormPart formPartId: " + tsIn.formParts[0].formPartId.ToString());

            return View("formParts", tf);
        }

        #endregion

        #region part
        [Obsolete("Parts is deprecated, page no longer in use.")]
        [HttpGet]
        public ActionResult Parts()
        {
            if (!SessionHelper.IsUserLoggedIn)
                return RedirectToAction("Index", "Account", null);

            if (SessionHelper.SessionForm == null)
            {
                SessionHelper.SessionForm = new SessionForm();
            }
            SessionForm sessionForm = SessionHelper.SessionForm;

            // Setup the form and its Parts
            string paramFormResultId = Request["formResultId"] as string;
            if (paramFormResultId == null)
            {
                string paramFormId = Request["formId"] as string;
                if (paramFormId == null)
                {
                    throw new Exception("a url parameter is required! \"formResultID\" or \"formId\"");
                }
                sessionForm.formId = Convert.ToInt32(paramFormId);
            }
            else
            {
                sessionForm.formResultId = Convert.ToInt32(paramFormResultId);
                def_FormResults frmRslt = formsRepo.GetFormResultById(sessionForm.formResultId);
                sessionForm.formId = frmRslt.formId;
            }

            Debug.WriteLine("* * *  ResultsController Parts");
            def_Forms frm = formsRepo.GetFormById(sessionForm.formId);
            Assmnts.Models.TemplateParts tp = new Assmnts.Models.TemplateParts();
            tp.parts = formsRepo.GetFormParts(frm);
            tp.formId = sessionForm.formId;
            tp.thisScreenTitle = frm.title;

            // Debug.WriteLine("* * *  FormsRepository  GetAllItems count: " + itemList.Count().ToString());

            return View("parts", tp);
        }

        /// <summary>
        /// Pulls the data which populates the DataTable in the parts view.
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [Obsolete("DataTableParts is deprecated, page no longer in use.")]
        [HttpPost]
        public string DataTableParts(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            int formId = 0;
            string result = String.Empty;
            Debug.WriteLine("Templates Controller DataTableParts draw:" + draw.ToString());
            Debug.WriteLine("Templates Controller DataTableParts start:" + start.ToString());
            Debug.WriteLine("Templates Controller DataTableParts searchValue:" + searchValue);
            Debug.WriteLine("Templates Controller DataTableParts searchRegex:" + searchRegex);
            Debug.WriteLine("Templates Controller DataTableParts order:" + order);
            try
            {
                formId = SessionHelper.SessionForm.formId;

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

                    // rename to avoid language/library conflicts
                    int iniIndex = start;
                    int noDsplyRecs = length;

                    List<def_Parts> query = formsRepo.GetFormParts(formsRepo.GetFormById(formId));

                    dtr.recordsTotal = dtr.recordsFiltered = query.Count();
                    int index = 0;
                    foreach (def_Parts q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                    {
                        index++;
                        string[] data = new string[] {
                            index.ToString(),
                            q.partId.ToString(),
                            q.identifier.ToString(),
                            q.title,
                            "<a href=\"/Templates/PartEdit?partId=" + q.partId + "\">Edit</a>",
                            "<a href=\"/Templates/Sections?partId=" + q.partId + "\">Sections</a>",
                            "<a href=\"/Templates/PartSections?partId=" + q.partId + "\">PartSections</a>"
                        };
                        dtr.data.Add(data);
                    }

                    Debug.WriteLine("Templates Controller DataTableParts data populated.");

                    // Output the JSON in DataTable format
                    fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                    param.EnableAnonymousTypes = true;
                    result = fastJSON.JSON.ToJSON(dtr, param);
                    Debug.WriteLine("Templates Controller DataTableParts result:" + result);
                }
                catch (Exception excptn)
                {
                    Debug.WriteLine("Templates Controller DataTableParts exception:" + excptn.Message);
                    result = excptn.Message + " - " + excptn.Message;
                }

            }
            catch (FormatException excptn)
            {
                Debug.WriteLine("Templates Controller DataTableParts sectionId format exception:" + excptn.Message);
            }
            return result;

        }

        [Obsolete("PartEdit is deprecated, page no longer in use.")]
        [HttpGet]
        public ActionResult PartEdit()
        {
            string partId = Request["partId"] as string;
            string prev = Request["prev"] as string;
            Debug.WriteLine("* * *  TemplatesController:PartEdit method  * * * partId: " + partId);

            Assmnts.Models.TemplateParts tp = new Assmnts.Models.TemplateParts();
            tp.parts = new List<def_Parts>();
            Session["partId"] = partId;
            def_Parts prt = formsRepo.GetPartById(Convert.ToInt32(partId));
            tp.parts.Add(prt);
            SessionForm sf = (SessionForm)Session["sessionForm"];

            if (!String.IsNullOrEmpty(prev))
            {
                tp.prevScreenHref = prev;
            }
            else
            {
                // Default to the formParts page when no redirect is provded.
                tp.prevScreenHref = "FormParts?formId=" + sf.formId;
            }
            // Setup session variables
            tp.formId = sf.formId;
            Debug.WriteLine("PartEdit sf.partId: " + sf.partId.ToString());

            Debug.WriteLine("* * *  Index  Parts count: " + tp.parts.Count().ToString());
            // * * * NOTE: this means that 'lazy loading' is working !!!  Need to turn it off !!!
            Debug.WriteLine("* * *  Index  Parts count: " + tp.parts.Count().ToString());

            return View("partEdit", tp);
        }

        [Obsolete("PostPart is deprecated, page no longer in use.")]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult PostPart(Assmnts.Models.TemplateParts tsIn)
        {
            int partId = tsIn.parts[0].partId;

            if (partId == 0)
            {
                partId = formsRepo.AddPart(tsIn.parts[0]);
                // * * * Temporarily test post method without changing the database - formsRepo lines need to be (de)activated. * * *
                //List<def_Parts> prts = formsRepo.GetAllParts().ToList();
                //partId = prts[prts.Count() - 1].partId;
            }
            else
            {
                formsRepo.SavePart(tsIn.parts[0]);
            }

            //Assmnts.Models.TemplateParts ts = new Assmnts.Models.TemplateParts();
            //ts.parts = new List<def_Parts>();
            //// Session["partId"] = partId;
            def_Parts prt = formsRepo.GetPartById(partId);
            //ts.parts.Add(prt);
            SessionForm sf = (SessionForm)Session["sessionForm"];
            Session["partId"] = prt.partId.ToString();
            Session["formId"] = sf.formId;
            //ts.formId = sf.formId;
            //ts.thisScreenTitle = prt.title;
            //Debug.WriteLine("* * *  Index  Parts count: " + ts.parts.Count().ToString());
            // * * * NOTE: this means that 'lazy loading' is working !!!  Need to turn it off !!!

            //return View("parts", ts);
            // Build the Redirect URL.
            string redirect;
            if (tsIn.prevScreenHref.IndexOf("?") > 0)
            {
                redirect = tsIn.prevScreenHref.Substring(0, tsIn.prevScreenHref.IndexOf("?"));
            }
            else
            {
                redirect = tsIn.prevScreenHref;
            }
            RouteValueDictionary rteVals = findValues(tsIn.prevScreenHref);
            return RedirectToAction(redirect, rteVals);
        }
        #endregion

        #region partSection
        [HttpGet]
        public ActionResult PartSections()
        {

            string paramPartId = Request["partId"] as string;
            Debug.WriteLine("* * *  TemplateController PartSections paramPartId: " + paramPartId);

            SessionHelper.SessionForm.partId = Convert.ToInt32(paramPartId);
            def_Parts prt = formsRepo.GetPartById( SessionHelper.SessionForm.partId );

            Assmnts.Models.TemplatePartSections tp = new Assmnts.Models.TemplatePartSections();
            // tp.partSections = new List<def_PartSections>(prt.def_PartSections);
            tp.thisScreenTitle = "(" + prt.partId + ") " + prt.title;
            tp.partId = SessionHelper.SessionForm.partId;
            tp.formId = SessionHelper.SessionForm.formId;

            return View("partSections", tp);
        }

        /// <summary>
        /// Pulls the data which populates the DataTable in the part sections view.
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTablePartSections(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            int partId = 0;
            string result = String.Empty;
            Debug.WriteLine("Templates Controller DataTablePartSections draw:" + draw.ToString());
            Debug.WriteLine("Templates Controller DataTablePartSections start:" + start.ToString());
            Debug.WriteLine("Templates Controller DataTablePartSections searchValue:" + searchValue);
            Debug.WriteLine("Templates Controller DataTablePartSections searchRegex:" + searchRegex);
            Debug.WriteLine("Templates Controller DataTablePartSections order:" + order);
            try
            {
                partId = SessionHelper.SessionForm.partId;

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

                    // rename to avoid language/library conflicts
                    int iniIndex = start;
                    int noDsplyRecs = length;

                    List<def_PartSections> query = formsRepo.GetAttachedPartById(partId).def_PartSections.OrderBy(ps => ps.order).ToList();

                    dtr.recordsTotal = dtr.recordsFiltered = query.Count();
                    foreach (def_PartSections q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                    {
                        string[] data = new string[] {
                            q.partSectionId.ToString(),
                            q.order.ToString(),
                            q.visible.ToString(),
                            q.sectionId.ToString(),
                            q.def_Sections.identifier,
                            q.def_Sections.title,
                            "<a href=\"/Templates/PartSectionEdit?partSectionId=" + q.partSectionId + "\">Section</a>",
                            "<a href=\"/Templates/SectionItems?sectionId=" + q.sectionId + "\">Items</a>"
                        };
                        dtr.data.Add(data);
                    }

                    Debug.WriteLine("Templates Controller DataTablePartSections data populated.");

                    // Output the JSON in DataTable format
                    fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                    param.EnableAnonymousTypes = true;
                    result = fastJSON.JSON.ToJSON(dtr, param);
                    Debug.WriteLine("Templates Controller DataTablePartSections result:" + result);
                }
                catch (Exception excptn)
                {
                    Debug.WriteLine("Templates Controller DataTablePartSections exception:" + excptn.Message);
                    result = excptn.Message + " - " + excptn.Message;
                }

            }
            catch (FormatException excptn)
            {
                Debug.WriteLine("Templates Controller DataTablePartSections sectionId format exception:" + excptn.Message);
            }
            return result;

        }

        [HttpGet]
        public ActionResult PartSectionEdit()
        {
            string partSectionId = Request["partSectionId"] as string;
            Debug.WriteLine("* * *  TemplatesController PartSectionEdit partSectionId: " + partSectionId);

            Assmnts.Models.TemplatePartSections ts = new Assmnts.Models.TemplatePartSections();
            ts.partSections = new List<def_PartSections>();
            SessionForm sf = (SessionForm)Session["sessionForm"];

            if (!String.IsNullOrEmpty(partSectionId))
            {
                Session["partSectionId"] = partSectionId;
                def_PartSections ps = formsRepo.GetPartSectionById(Convert.ToInt32(partSectionId));
                ts.partSections.Add(ps);
                ts.visible = Convert.ToBoolean(ps.visible);
            }
            else
            {
                Session["partSectionId"] = 0;
                def_PartSections ps = new def_PartSections();
                ps.def_Parts = formsRepo.GetPartById(sf.partId);
                ps.partId = ps.def_Parts.partId;
                ts.partSections.Add(ps);
                ts.visible = true;
            }
            // Setup session variables
            ts.partId = sf.partId;

            Debug.WriteLine("* * *  TemplatesController PartSectionEdit partSections count: " + ts.partSections.Count().ToString());

            return View("partSectionEdit", ts);
        }

        [HttpPost]
        public string SectionList(string identifier, string title, int iniIndex, int turnPage)
        {
            iniIndex += turnPage;
            int noDsplyRecs = 10;
            IQueryable<def_Sections> sections = formsRepo.GetAllSections().OrderBy(s => s.sectionId);

            if (!String.IsNullOrEmpty(identifier))
            {
                sections = sections.Where(i => i.identifier.Contains(identifier));
            }

            if (!String.IsNullOrEmpty(title))
            {
                sections = sections.Where(i => i.title.Contains(title));
            }

            List<string> datas = new List<string>();

            int last = iniIndex + noDsplyRecs;
            if (last > sections.Count())
            {
                last = sections.Count();
            }

            datas.Add("page=" + iniIndex + "&endIndex=" + last + "&count=" + sections.Count());
            datas.Add("&nbsp&nbspIdentifier, Title");
            foreach (def_Sections s in sections.Skip(iniIndex).Take(noDsplyRecs).ToList())
            {
                datas.Add("<input type=\"radio\" class=\"control cols-md-1\" name=\"Items\" value=\"id=" + s.sectionId + "&identifier=" + s.identifier 
                    + "&title=" + s.title + "&visbl=" + s.visible + "&href=" + s.href + "&rubric=" + s.rubricBlock + "\" >" + s.identifier + ", " + s.title + "</input>");
            }

            Debug.WriteLine("* * *  TemplatesController SectionList sections count: " + (datas.Count() - 2).ToString());

            // Output the in JSON format
            fastJSON.JSONParameters param = new fastJSON.JSONParameters();
            param.EnableAnonymousTypes = true;
            string result = fastJSON.JSON.ToJSON(datas, param);

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult PostPartSection(Assmnts.Models.TemplatePartSections tsIn)
        {
            tsIn.partSections[0].visible = tsIn.visible;
            // The page updates tsIn.partSections[0].sectionId, other references to partId must be updated here.
            tsIn.partSections[0].def_Sections.sectionId = tsIn.partSections[0].sectionId;
            if (tsIn.sectionChange)
            {
                if (tsIn.partSections[0].sectionId == 0)
                {
                    tsIn.partSections[0].sectionId = formsRepo.AddSection(tsIn.partSections[0].def_Sections);
                }
                else
                {
                    formsRepo.SectionSave(tsIn.partSections[0].def_Sections);
                }
                Debug.WriteLine("* * *  TemplatesController PostPartSection def_Section Updated");
            }

            // clearing def_Sections and def_Parts prevents duplicate key exceptions.
            tsIn.partSections[0].def_Sections = null;
            tsIn.partSections[0].def_Parts = null;

            if (tsIn.partSectionChange)
            {
                if (tsIn.partSections[0].partSectionId == 0)
                {
                    tsIn.partSections[0].partSectionId = formsRepo.AddPartSection(tsIn.partSections[0]);
                }
                else
                {
                    formsRepo.SavePartSection(tsIn.partSections[0]);
                }
                Debug.WriteLine("* * *  TemplatesController PostPartSection def_PartSection Updated");
            }

            Assmnts.Models.TemplatePartSections ts = new Assmnts.Models.TemplatePartSections();
            ts.partSections = new List<def_PartSections>();
            def_PartSections ps = formsRepo.GetPartSectionById(tsIn.partSections[0].partSectionId);
            ts.partSections.Add(ps);

            Session["partSectionId"] = tsIn.partSections[0].partSectionId.ToString();
            SessionForm sf = (SessionForm)Session["sessionForm"];
            Session["partId"] = sf.partId;
            ts.thisScreenTitle = "(" + sf.partId + ") " + formsRepo.GetPartById(sf.partId).title;
            ts.formId = sf.formId;

            Debug.WriteLine("* * *  TemplatesController PostPartSection sectionId: " + tsIn.partSections[0].partId.ToString());
            Debug.WriteLine("* * *  TemplatesController PostPartSection partSectionId: " + tsIn.partSections[0].partSectionId.ToString());

            return View("partSections", ts);
        }
        #endregion

        #region section
        [Obsolete("Sections is deprecated, page no longer in use.")]
        [HttpGet]
        public ActionResult Sections()
        {
            string partId = Request["partId"] as string;
            if (String.IsNullOrEmpty(partId))
            {
                partId = SessionHelper.SessionForm.partId.ToString();
            }
            Debug.WriteLine("* * *  ResultsController:Sections method  * * * partId: " + partId);

            // Session["part"] = partId;
            def_Parts prt = formsRepo.GetPartById(Convert.ToInt32(partId));
            // Setup session variables
            // SessionForm sf = (SessionForm)Session["sessionForm"];
            // sf.partId = prt.partId;
            if (prt != null)
                SessionHelper.SessionForm.partId = prt.partId;

            Debug.WriteLine("* * *  Sections  PartSections count: " + prt.def_PartSections.Count().ToString());
            // * * * NOTE: this means that 'lazy loading' is working !!!  Need to turn it off !!!

            Assmnts.Models.TemplateSections ts = new Assmnts.Models.TemplateSections();
            ts.formId = SessionHelper.SessionForm.formId;
            ts.thisScreenTitle = prt.title;
            //ts.sections = formsRepo.GetSectionsInPart(prt);
            // Debug.WriteLine("* * *  FormsRepository  GetAllItems count: " + itemList.Count().ToString());

            return View("sections", ts);

            //old code

            //string partId = Request["partId"] as string;
            //Debug.WriteLine("* * *  TemplatesController:Sections method  * * * partId: " + partId);

            //Session["part"] = partId;
            //def_Parts prt = formsRepo.GetPartById(Convert.ToInt32(partId));
            //// Setup session variables
            //SessionForm sf = (SessionForm)Session["sessionForm"];
            //sf.partId = prt.partId;

            //Debug.WriteLine("* * *  Index  PartSections count: " + prt.def_PartSections.Count().ToString());
            //// * * * NOTE: this means that 'lazy loading' is working !!!  Need to turn it off !!!

            //Assmnts.Models.TemplateSections ts = new Assmnts.Models.TemplateSections();
            //ts.sections = formsRepo.GetSectionsInPart(prt);
            //// Debug.WriteLine("* * *  FormsRepository  GetAllItems count: " + itemList.Count().ToString());

            //return View("sections", ts);

        }

        /// <summary>
        /// Pulls the data which populates the DataTable in the sections view.
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [Obsolete("DataTableSections is deprecated, page no longer in use.")]
        [HttpPost]
        public string DataTableSections(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            int partId = 0;
            string result = String.Empty;
            Debug.WriteLine("Templates Controller DataTableSections draw:" + draw.ToString());
            Debug.WriteLine("Templates Controller DataTableSections start:" + start.ToString());
            Debug.WriteLine("Templates Controller DataTableSections searchValue:" + searchValue);
            Debug.WriteLine("Templates Controller DataTableSections searchRegex:" + searchRegex);
            Debug.WriteLine("Templates Controller DataTableSections order:" + order);
            try
            {
                partId = SessionHelper.SessionForm.partId;

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

                    // rename to avoid language/library conflicts
                    int iniIndex = start;
                    int noDsplyRecs = length;

                    List<def_Sections> query = formsRepo.GetSectionsInPartById(partId);

                    dtr.recordsTotal = dtr.recordsFiltered = query.Count();
                    int index = 0;
                    foreach (def_Sections q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                    {
                        index++;
                        string[] data = new string[] {
                            index.ToString(),
                            q.sectionId.ToString(),
                            q.identifier.ToString(),
                            q.title,
                            q.visible.ToString(),
                            q.href,
                            "<a href=\"/Templates/SectionEdit?sectionId=" + q.sectionId + "\">Edit</a>",
                            "<a href=\"/Templates/Items?sectionId=" + q.sectionId + "\">Items</a>",
                            "<a href=\"/Templates/SectionItems?sectionId=" + q.sectionId + "\">SectionItems</a>"
                        };
                        dtr.data.Add(data);
                    }

                    Debug.WriteLine("Templates Controller DataTableSections data populated.");

                    // Output the JSON in DataTable format
                    fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                    param.EnableAnonymousTypes = true;
                    result = fastJSON.JSON.ToJSON(dtr, param);
                    Debug.WriteLine("Templates Controller DataTableSections result:" + result);
                }
                catch (Exception excptn)
                {
                    Debug.WriteLine("Templates Controller DataTableSections exception:" + excptn.Message);
                    result = excptn.Message + " - " + excptn.Message;
                }

            }
            catch (FormatException excptn)
            {
                Debug.WriteLine("Templates Controller DataTableSections sectionId format exception:" + excptn.Message);
            }
            return result;

        }

        [HttpGet]
        [Obsolete("SectionAdd is deprecated, use SectionEdit instead.")]
        public ActionResult SectionAdd()
        {
            Debug.WriteLine("* * *  TemplatesController:SectionAdd method  * * *");

            Assmnts.Models.TemplateSections ts = new Assmnts.Models.TemplateSections();
            ts.sections = new List<def_Sections>();
            Session["sectionId"] = 0;
            def_Sections sct = new def_Sections();
            sct.visible = true;
            sct.multipleItemsPerPage = true;
            ts.sections.Add(sct);

            return View("sectionEdit", ts);
        }

        [HttpGet]
        [Obsolete("SectionEdit is deprecated, page no longer in use.")]
        public ActionResult SectionEdit()
        {
            string sectionId = Request["sectionId"] as string;
            string prev = Request["prev"] as string;
            Debug.WriteLine("* * *  TemplatesController:SectionEdit method  * * * sectionId: " + sectionId);

            Assmnts.Models.TemplateSections ts = new Assmnts.Models.TemplateSections();
            ts.sections = new List<def_Sections>();
            def_Sections sct = new def_Sections();
            SessionForm sf = (SessionForm)Session["sessionForm"];

            if (!String.IsNullOrEmpty(sectionId))
            {
                Session["sectionId"] = sectionId;
                sct = formsRepo.GetSectionById(Convert.ToInt32(sectionId));
                ts.sections.Add(sct);
            }
            else
            {
                Session["sectionId"] = 0;
                sct.visible = true;
                sct.multipleItemsPerPage = true;
                ts.sections.Add(sct);
            }

            if (!String.IsNullOrEmpty(prev))
            {
                ts.prevScreenHref = prev;
            }
            else
            {
                // Default to the partSections page when no redirect is provided.
                ts.prevScreenHref = "PartSections?partId=" + sf.partId;
            }

            // Setup session variables
            sf.sectionId = ts.sections.FirstOrDefault().sectionId;
            Debug.WriteLine("SectionEdit sf.sectionId: " + sf.sectionId.ToString());

            return View("sectionEdit", ts);
        }


        [Obsolete("PostSection is deprecated, page no longer in use.")]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult PostSection(Assmnts.Models.TemplateSections tsIn)
        {
            int sctId = tsIn.sections[0].sectionId;
            int partId = 0;

            if (tsIn.sections[0].sectionId == 0)
            {
                sctId = formsRepo.AddSection(tsIn.sections[0]);
                // Now link it to a PartSection associated with the user selected Section
                def_PartSections ps = new def_PartSections();
                SessionForm sf = (SessionForm)Session["sessionForm"];
                partId = sf.partId;
                Session["partId"] = partId;
                ps.partId = partId;
                ps.sectionId = sctId;
                ps.order = 2;
                formsRepo.AddPartSection(ps);
            }
            else
            {
                formsRepo.SectionSave(tsIn.sections[0]);
                SessionForm sf = (SessionForm)Session["sessionForm"];
                partId = sf.partId;
            }

            //Build the redirect.
            string redirect;
            if (tsIn.prevScreenHref.Contains("?"))
            {
                redirect = tsIn.prevScreenHref.Substring(0,tsIn.prevScreenHref.IndexOf("?"));
            } else {
                redirect = tsIn.prevScreenHref;
            }
            RouteValueDictionary rteVals = findValues(tsIn.prevScreenHref);
            return RedirectToAction(redirect, rteVals);
        }
        #endregion

        #region sectionItem
        [HttpGet]
        public ActionResult SectionItems()
        {
            string sectionId = Request["sectionId"] as string;
            Debug.WriteLine("* * *  TemplatesController SectionItems sectionId: " + sectionId);

            Session["section"] = sectionId;
            def_Sections sct = formsRepo.GetSectionById(Convert.ToInt32(sectionId));
            Assmnts.Models.TemplateSectionItems ts = new Assmnts.Models.TemplateSectionItems();
            SessionForm sf = (SessionForm)Session["sessionForm"];
            ts.sectionItems = formsRepo.GetSectionItemsBySectionIdEnt(sct.sectionId, SessionHelper.LoginStatus.EnterpriseID);
            ts.thisScreenTitle = "(" + sct.sectionId + ") " + sct.title;
            ts.partId = sf.partId;
            ts.sectionId = sct.sectionId;

            // Setup session variables
            sf.sectionId = sct.sectionId;
            Session["sessionForm"] = sf;

            Debug.WriteLine("* * *  SectionItems count: " + ts.sectionItems.Count().ToString());

            return View("SectionItems", ts);
        }

        /// <summary>
        /// Pulls the data which populates the DataTable in the section items view.
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableSectionItems(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            int sectionId = 0;
            string result = String.Empty;
            Debug.WriteLine("Templates Controller DataTableSectionItems draw:" + draw.ToString());
            Debug.WriteLine("Templates Controller DataTableSectionItems start:" + start.ToString());
            Debug.WriteLine("Templates Controller DataTableSectionItems searchValue:" + searchValue);
            Debug.WriteLine("Templates Controller DataTableSectionItems searchRegex:" + searchRegex);
            Debug.WriteLine("Templates Controller DataTableSectionItems order:" + order);
            try
            {
                sectionId = SessionHelper.SessionForm.sectionId;

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

                    // rename to avoid language/library conflicts
                    int iniIndex = start;
                    int noDsplyRecs = length;

                    List<def_SectionItems> query = formsRepo.GetSectionItemsBySectionId(sectionId);

                    dtr.recordsTotal = dtr.recordsFiltered = query.Count();
                    foreach (def_SectionItems q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                    {
                        string[] data = new string[] {
                            q.sectionItemId.ToString(),
                            (q.itemId == 1 && q.subSectionId != null && q.subSectionId != 0)? "<a href=\"/Templates/SubSection?subSectionId=" + q.subSectionId + "\">" + q.subSectionId + "</a>" : q.subSectionId.ToString(),
                            q.order.ToString(),
                            q.display.ToString(),
                            q.itemId.ToString(),
                            q.def_Items.identifier,
                            q.def_Items.title,
                            q.def_Items.label,
                            "<a href=\"/Templates/SectionItemEdit?sectionItemId=" + q.sectionItemId + "\">Item</a>",
                            "<a href=\"/Templates/ItemVariables?itemId=" + q.itemId + "\">Item Variables</a>"
                        };
                        dtr.data.Add(data);
                    }

                    Debug.WriteLine("Templates Controller DataTableSectionItems data populated.");

                    // Output the JSON in DataTable format
                    fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                    param.EnableAnonymousTypes = true;
                    result = fastJSON.JSON.ToJSON(dtr, param);
                    Debug.WriteLine("Templates Controller DataTableSectionItems result:" + result);
                }
                catch (Exception excptn)
                {
                    Debug.WriteLine("Templates Controller DataTableSectionItems exception:" + excptn.Message);
                    result = excptn.Message + " - " + excptn.Message;
                }

            }
            catch (FormatException excptn)
            {
                Debug.WriteLine("Templates Controller DataTableSectionItems sectionId format exception:" + excptn.Message);
            }
            return result;

        }

        /// <summary>
        /// Redirects to the SectionItemEdit page, which will pre-load data with the sectionItemId provided by a session
        /// variable.  If sectionItemId is not provided the page will open with a new sectionItem to create.
        /// </summary>
        /// <returns>Redirects to SectionItemEdit page when appropriate session variables are provided.</returns>
        [HttpGet]
        public ActionResult SectionItemEdit()
        {
            string sectionItemId = Request["sectionItemId"] as string;
            Debug.WriteLine("* * *  TemplatesController SectionItemEdit sectionItemId: " + sectionItemId);

            Assmnts.Models.TemplateSectionItems ts = new Assmnts.Models.TemplateSectionItems();
            ts.sectionItems = new List<def_SectionItems>();
            ts.languages = new List<SelectListItem>();
			SessionForm sf = (SessionForm)Session["sessionForm"];

            if (!String.IsNullOrEmpty(sectionItemId))
            {
                Session["sectionItemId"] = sectionItemId;
                def_SectionItems si = formsRepo.GetSectionItemById(Convert.ToInt32(sectionItemId));
                ts.sectionItems.Add(si);

                foreach (def_Languages l in formsRepo.GetAllLanguages())
                {
                    SelectListItem sli = new SelectListItem();
                    sli.Value = l.langId.ToString();
                    sli.Text = l.title;
                    if (l.langId == si.def_Items.langId)
                    {
                        sli.Selected = true;
                    }
                    ts.languages.Add(sli);
                }
            }
            else
            {
                Session["sectionItemId"] = 0;
                def_SectionItems si = new def_SectionItems();
                si.def_Sections = formsRepo.GetSectionById(sf.sectionId);
                si.sectionId = si.def_Sections.sectionId;
                ts.sectionItems.Add(si);

                foreach (def_Languages l in formsRepo.GetAllLanguages())
                {
                    SelectListItem sli = new SelectListItem();
                    sli.Value = l.langId.ToString();
                    sli.Text = l.title;
                    if (l.langId == 1)
                    {
                        sli.Selected = true;
                    }
                    ts.languages.Add(sli);
                }
            }
            // Setup session variables
			ts.sectionId = sf.sectionId;
			
            Debug.WriteLine("* * *  TemplatesController SectionItemEdit sectionItems count: " + ts.sectionItems.Count().ToString());

            return View("sectionItemEdit", ts);
        }

        [HttpPost]
        public string ItemList(string identifier, string title, int iniIndex, int turnPage)
        {
            iniIndex += turnPage;
            int noDsplyRecs = 10;
            IQueryable<def_Items> items = formsRepo.GetAllItems().OrderBy(i => i.itemId);

            if (!String.IsNullOrEmpty(identifier))
            {
                items = items.Where(i => i.identifier.Contains(identifier));
            }

            if (!String.IsNullOrEmpty(title))
            {
                items = items.Where(i => i.title.Contains(title));
            }

            List<string> datas = new List<string>();

            int last = iniIndex + noDsplyRecs;
            if (last > items.Count())
            {
                last = items.Count();
            }

            datas.Add("page=" + iniIndex + "&endIndex=" + last + "&count=" + items.Count());
            datas.Add("&nbsp&nbspIdentifier, Title");
            foreach (def_Items i in items.Skip(iniIndex).Take(noDsplyRecs))
            {
                datas.Add("<input type=\"radio\" class=\"control cols-md-1\" name=\"Items\" value=\"id=" + i.itemId + 
                    "&identifier=" + i.identifier + "&title=" + i.title + "\" >" + i.identifier + ", " + i.title + "</input>");
            }

            Debug.WriteLine("* * *  TemplatesController ItemList items count: " + (datas.Count() - 2).ToString());

            // Output the in JSON format
            fastJSON.JSONParameters param = new fastJSON.JSONParameters();
            param.EnableAnonymousTypes = true;
            string result = fastJSON.JSON.ToJSON(datas, param);

            return result;
        }

        /// <summary>
        /// Data stored within the Prompt and Body fields of an item can be lengthy and contain html tags which may break the modal on SectionItemEdit.
        /// This method limits the data being transferred to the Items modal by accessing the database a second time after the user has made a selection,
        /// it can also be handled more securely in this fashion.
        /// This data must populate the appropriate fields if it is going to be displayed and saved correctly.
        /// </summary>
        /// <returns>JavaScript object with data for specific fields.</returns>
        [HttpPost]
        public string SectionItemDetails(int itemId)
        {
            List<string> datas = new List<string>();
            def_Items itm = formsRepo.GetItemById(itemId);
            List<def_Languages> lang = formsRepo.GetAllLanguages();
            int selValue = lang.IndexOf(lang.Where(l => l.langId == itm.langId).FirstOrDefault()) + 1;

            datas.Add(itm.label);
            datas.Add(itm.prompt);
            datas.Add(itm.itemBody);
            datas.Add(selValue.ToString());

            // Output the in JSON format
            fastJSON.JSONParameters param = new fastJSON.JSONParameters();
            param.EnableAnonymousTypes = true;
            string result = fastJSON.JSON.ToJSON(datas, param);

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult PostSectionItem(Assmnts.Models.TemplateSectionItems tsIn)
        {
            // The page updates tsIn.sectionItems[0].itemId, other references to itemId must be updated here.
            tsIn.sectionItems[0].def_Items.itemId = tsIn.sectionItems[0].itemId;
            if (tsIn.itemChange)
            {
                if (tsIn.sectionItems[0].itemId == 0)
                {
                    tsIn.sectionItems[0].itemId = formsRepo.ItemAdd(tsIn.sectionItems[0].def_Items);
                }
                else
                {
                    formsRepo.ItemSave(tsIn.sectionItems[0].def_Items);
                }
                Debug.WriteLine("* * *  TemplatesController PostSectionItem def_Item Updated");
            }

            // clearing def_Items and def_Sections prevents duplicate key exceptions.
            tsIn.sectionItems[0].def_Items = null;
            tsIn.sectionItems[0].def_Sections = null;

            if (tsIn.sectionItemChange)
            {
                if (tsIn.sectionItems[0].sectionItemId == 0)
                {
                    tsIn.sectionItems[0].sectionItemId = formsRepo.AddSectionItem(tsIn.sectionItems[0]);
                }
                else
                {
                    formsRepo.SaveSectionItem(tsIn.sectionItems[0]);
                }
                Debug.WriteLine("* * *  TemplatesController PostSectionItem def_SectionItem Updated");
            }

            Assmnts.Models.TemplateSectionItems ts = new Assmnts.Models.TemplateSectionItems();
            ts.sectionItems = new List<def_SectionItems>();
            def_SectionItems si = formsRepo.GetSectionItemById(tsIn.sectionItems[0].sectionItemId);
            ts.sectionItems.Add(si);

            Session["sectionItemId"] = tsIn.sectionItems[0].sectionItemId.ToString();
            SessionForm sf = (SessionForm)Session["sessionForm"];
            Session["sectionId"] = sf.sectionId;
            ts.thisScreenTitle = "(" + sf.sectionId + ") " + formsRepo.GetSectionById(sf.sectionId).title;
            ts.partId = sf.partId;

            Debug.WriteLine("* * *  TemplatesController PostSectionItem sectionItemsId: " + tsIn.sectionItems[0].sectionItemId.ToString());

            return View("sectionItems", ts);
        }

        public ActionResult SubSection()
        {
            string subSectionId = Request["subSectionId"];
            Debug.WriteLine("* * *  Templates Controller SubSection subSectionId: " + subSectionId);

            Assmnts.Models.TemplateSectionItems ts = new Assmnts.Models.TemplateSectionItems();
            SessionForm sf = (SessionForm)Session["sessionForm"];
            Session["section"] = subSectionId;
            def_Sections sub = formsRepo.GetSubSectionById(Convert.ToInt32(subSectionId));
            ts.sectionItems = formsRepo.GetSectionItemsBySectionIdEnt(sub.sectionId, SessionHelper.LoginStatus.EnterpriseID);
            ts.thisScreenTitle = "(" + sub.sectionId + ") " + sub.title;
            ts.partId = sf.partId;
            ts.sectionId = sub.sectionId;

            // Setup session variables
            sf.sectionId = sub.sectionId;

            Debug.WriteLine("* * *  SectionItems count: " + ts.sectionItems.Count().ToString());

            return View("sectionItems", ts);
        }

        [HttpGet]
        [Obsolete("SectionItemAdd is deprecated, use SectionItemEdit instead")]
        public ActionResult SectionItemAdd()
        {
            Debug.WriteLine("* * *  TemplatesController:SectionItemAdd method  * * *");

            Assmnts.Models.TemplateSectionItems ts = new Assmnts.Models.TemplateSectionItems();
            ts.sectionItems = new List<def_SectionItems>();
            def_SectionItems itm = new def_SectionItems();
            ts.sectionItems.Add(itm);
            // Setup session variables
            SessionForm sf = (SessionForm)Session["sessionForm"];

            // * * * NOTE: this means that 'lazy loading' is working !!!  Need to turn it off !!!

            return View("sectionItemEdit", ts);
        }
        #endregion

        #region item
        [Obsolete("Items is deprecated, page no longer in use.")]
        [HttpGet]
        public ActionResult Items()
        {
            string sectionId = Request["sectionId"] as string;
            string searchTerm = Request["search"] as string;

            int searchForm = -1;
            try
            {
                searchForm = Int16.Parse(Request["formId"] as string);
            }
            catch (Exception e) { }

            if( sectionId == null && searchTerm == null )
                throw new Exception( "URL must contain a parameter \"sectionId\", or \"search\"" );

            Assmnts.Models.GeneralForm ts = new Assmnts.Models.GeneralForm();

            if (searchTerm == null)
            {
                Debug.WriteLine("* * *  TemplatesController:Items method  * * * sectionId: " + sectionId);

                Session["section"] = sectionId;
                //def_Sections sct = formsRepo.GetSectionById(Convert.ToInt32(sectionId));
                //ts.items = formsRepo.GetAllItemsForSection(sct);
                // Setup session variables
                SessionForm sf = (SessionForm)Session["sessionForm"];
                sf.sectionId = Convert.ToInt32(sectionId);//sct.sectionId;
                ts.thisScreenTitle = formsRepo.GetSectionById(sf.sectionId).title;
                ts.formId = sf.formId;
                ts.partId = sf.partId;
            }
            else
            {
                Debug.WriteLine("* * *  TemplatesController:Items method  * * * searchTerm: " + searchTerm + ", formId: " + searchForm );

                ts.items = new List<def_Items>();

                //populate the model with zero or one items matching the search query
                //def_Items searchResult = formsRepo.GetItemByIdentifierAndFormId(searchTerm,searchForm);
                //if (searchResult != null)
                //    ts.items.Add(searchResult);
                SessionForm sf = (SessionForm)Session["sessionForm"];
                sf.sectionId = -1;
                // Hijacked nextScreenTitle parameter for searches, rather than it's usual function.
                ts.nextScreenTitle = searchTerm;
                ts.thisScreenTitle = "Item Search";
                ts.formId = searchForm;
                ts.partId = sf.partId;
            }

            //Debug.WriteLine("* * *  Index  Items count: " + ts.items.Count().ToString());
            // * * * NOTE: this means that 'lazy loading' is working !!!  Need to turn it off !!!


            return View("items", ts);
        }

        /// <summary>
        /// Pulls the data which populates the DataTable in the items view.
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [Obsolete("DataTableItems is deprecated, page no longer in use.")]
        [HttpPost]
        public string DataTableItems(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            int sectionId = 0;
            string result = String.Empty;
            Debug.WriteLine("Templates Controller DataTableItems draw:" + draw.ToString());
            Debug.WriteLine("Templates Controller DataTableItems start:" + start.ToString());
            Debug.WriteLine("Templates Controller DataTableItems searchValue:" + searchValue);
            Debug.WriteLine("Templates Controller DataTableItems searchRegex:" + searchRegex);
            Debug.WriteLine("Templates Controller DataTableItems order:" + order);
            try
            {
                sectionId = SessionHelper.SessionForm.sectionId;
                
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

                    string sIden = Request.Params["columns[0][search][value]"];
                    if (String.IsNullOrEmpty(sIden))
                    {
                        sIden = Request["sIden"];
                    }

                    // rename to avoid language/library conflicts
                    int iniIndex = start;
                    int noDsplyRecs = length;

                    //def_Sections sct = formsRepo.GetSectionById(Convert.ToInt32(sectionId));
                    IQueryable<def_Items> query = formsRepo.GetAllItems().OrderBy(q => q.itemId);
                    if (sectionId > 0)
                    {
                        query = query.Where(q => q.def_SectionItems.FirstOrDefault().sectionId == sectionId);
                    }

                    if (!String.IsNullOrEmpty(sIden))
                    {
                        query = query.Where(q => q.identifier.ToUpper().Contains(sIden.ToUpper()));
                    }

                    if (sectionId < 0 && String.IsNullOrEmpty(sIden))
                    {
                        query = query.Where(q => q == null);
                    }

                    dtr.recordsTotal = dtr.recordsFiltered = query.Count();
                    int index = 0;
                    foreach (def_Items q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                    {
                        index++;
                        string[] data = new string[] {
                            index.ToString(),
                            q.itemId.ToString(),
                            q.identifier.ToString(),
                            q.title,
                            q.label,
                            "<a href=\"ItemEdit?itemId=" + q.itemId + "\">Edit</a>",
                            "<a href=\"ItemVariables?itemId=" + q.itemId + "\">IVs</a>",
                        };
                        dtr.data.Add(data);
                    }

                    Debug.WriteLine("Templates Controller DataTableItems data populated.");

                    // Output the JSON in DataTable format
                    fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                    param.EnableAnonymousTypes = true;
                    result = fastJSON.JSON.ToJSON(dtr, param);
                    Debug.WriteLine("Templates Controller DataTableItems result:" + result);
                }
                catch (Exception excptn)
                {
                    Debug.WriteLine("Templates Controller DataTableItems exception:" + excptn.Message);
                    result = excptn.Message + " - " + excptn.Message;
                }

            }
            catch (FormatException excptn)
            {
                Debug.WriteLine("Templates Controller DataTableItems sectionId format exception:" + excptn.Message);
            }
            return result;

        }

        /// <summary>
        /// Searches for a session variable "itemId", then loads a page to allow editing this item.
        /// If the item does not exist, a blank item is loaded allowing new items to be created.
        /// </summary>
        /// <returns>itemEdit.cshtml</returns>
        [Obsolete("ItemEdit is deprecated, page no longer in use.")]
        [HttpGet]
        public ActionResult ItemEdit()
        {
            string itemId = Request["itemId"] as string;
            Debug.WriteLine("* * *  TemplatesController:ItemEdit method  * * * itemId: " + itemId);

            Assmnts.Models.GeneralForm ts = new Assmnts.Models.GeneralForm();
            ts.items = new List<def_Items>();
            ts.languages = new List<SelectListItem>();

            foreach (def_Languages l in formsRepo.GetAllLanguages())
            {
                SelectListItem sli = new SelectListItem();
                sli.Value = l.langId.ToString();
                sli.Text = l.title;
                if (l.langId == 1)
                {
                    sli.Selected = true;
                }
                ts.languages.Add(sli);
            }

            if (!String.IsNullOrEmpty(itemId))
            {
                Session["itemId"] = itemId;
                def_Items itm = formsRepo.GetItemById(Convert.ToInt32(itemId));
                ts.items.Add(itm);
                if (itm.langId != 1)
                {
                    // Pre-select the correct language.
                    for (int i = 0; i < ts.languages.Count(); i++)
                    {
                        if (ts.languages[i].Value.Equals("1"))
                        {
                            ts.languages[i].Selected = false;
                        }
                        else if (ts.languages[i].Value.Equals(itm.langId.ToString()))
                        {
                            ts.languages[i].Selected = true;
                        }
                    }
                }
            }
            else
            {
                Session["itemId"] = 0;
                def_Items itm = new def_Items();
                itm.itemId = 0;
                //itm.langId = 1;         // English
                ts.items.Add(itm);
            }
            // Setup session variables
            SessionForm sf = (SessionForm)Session["sessionForm"];
            string previous = Request["prev"] as string;
            if (!String.IsNullOrEmpty(previous))
            {
                ts.prevScreenHref = previous;
            }
            else
            {
                ts.prevScreenHref = "SectionItems?sectionId=" + sf.sectionId;
            }
            Debug.WriteLine("ItemEdit sf.sectionId: " + sf.sectionId.ToString());

            Debug.WriteLine("* * *  Index  Items count: " + ts.items.Count().ToString());
            // * * * NOTE: this means that 'lazy loading' is working !!!  Need to turn it off !!!
            //Debug.WriteLine("* * *  Index  Items count: " + ts.items.Count().ToString());


            return View("itemEdit", ts);
        }

        [HttpGet]
        [Obsolete("ItemAdd is deprecated, use ItemEdit instead.")]
        public ActionResult ItemAdd()
        {
            Debug.WriteLine("* * *  TemplatesController:ItemAdd method  * * *");

            Assmnts.Models.GeneralForm ts = new Assmnts.Models.GeneralForm();
            ts.items = new List<def_Items>();
            Session["itemId"] = 0;
            def_Items itm = new def_Items();
            itm.itemId = 0;
            itm.langId = 1;         // English
            ts.items.Add(itm);
            // Setup session variables
            SessionForm sf = (SessionForm)Session["sessionForm"];

            // * * * NOTE: this means that 'lazy loading' is working !!!  Need to turn it off !!!

            return View("itemEdit", ts);
        }

        [Obsolete("PostItem is deprecated, page no longer in use.")]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult PostItem(Assmnts.Models.TemplateItems tsIn)
        {

            if (tsIn.items[0].itemId == 0)
            {
                int itmId = formsRepo.ItemAdd(tsIn.items[0]);
                // Now link it to a SectionItem associated with the user selected Section
                //def_SectionItems si = new def_SectionItems();
                //SessionForm sf = (SessionForm)Session["sessionForm"];
                //si.sectionId = sf.sectionId;
                //si.itemId = itmId;
                //si.order = 25;
                //si.validation = 0;
                //si.display = true;
                //si.readOnly = false;
                //si.requiredSection = false;
                //si.requiredForm = false;
                //formsRepo.AddSectionItem(si);
            }
            else
            {
                formsRepo.ItemSave(tsIn.items[0]);
            }

            string itemId = Request["itemId"] as string;
            Debug.WriteLine("* * *  TemplatesController:ItemEdit method  * * * itemId: " + itemId);

            //Assmnts.Models.GeneralForm ts = new Assmnts.Models.GeneralForm();
            //ts.items = new List<def_Items>();
            Session["itemId"] = itemId;
            //def_Items itm = formsRepo.GetItemById(Convert.ToInt32(itemId));
            //ts.items.Add(itm);

            string redirect;
            if (tsIn.prevScreenHref.Contains("?"))
            {
                redirect = tsIn.prevScreenHref.Substring(0, tsIn.prevScreenHref.IndexOf("?"));
            }
            else
            {
                redirect = tsIn.prevScreenHref;
            }
            RouteValueDictionary values = findValues(tsIn.prevScreenHref.Substring(tsIn.prevScreenHref.IndexOf("?") + 1));

            //Debug.WriteLine("* * *  Index  Items count: " + ts.items.Count().ToString());
            // * * * NOTE: this means that 'lazy loading' is working !!!  Need to turn it off !!!

            //return View(tsIn.prevScreenHref, ts);
            return RedirectToAction(redirect, values);
        }
        #endregion

        #region itemVariable
        [HttpGet]
        public ActionResult ItemVariables()
        {
            string itemId = Request["itemId"] as string;
            Debug.WriteLine("* * *  TemplatesController ItemVariables itemId: " + itemId);

            Session["item"] = itemId;
            def_Items itm = formsRepo.GetItemById(Convert.ToInt32(itemId));
            Assmnts.Models.TemplateItemVariables tiv = new Assmnts.Models.TemplateItemVariables();
            SessionForm sf = (SessionForm)Session["sessionForm"];
            tiv.itm = itm;
            tiv.itemVariables = itm.def_ItemVariables.ToList();
            tiv.thisScreenTitle = "(" + itm.itemId + ") " + itm.title;
            tiv.sectionId = sf.sectionId;

            // Setup session variables
            sf.itemId = Convert.ToInt32(itemId);
            Session["sessionForm"] = sf;

            //populate base types list
            tiv.baseTypes = new SelectList(formsRepo.GetBaseTypes(), "baseTypeId", "enumeration" );

            Debug.WriteLine("* * *  ItemVariables count: " + tiv.itemVariables.Count().ToString());

            return View("itemVars", tiv);
        }

        /// <summary>
        /// Pulls the data which populates the DataTable in the item variables view.
        /// </summary>
        /// <param name="draw">Automatically posted by JQuery DataTables: count of the number of times this table has been drawn.</param>
        /// <param name="start">Automatically posted by JQuery DataTables: the starting index of the current result set.</param>
        /// <param name="length">Automatically posted by JQuery DataTables: the number of rows displayed by the table.</param>
        /// <param name="searchValue">Automatically posted by JQuery DataTables: </param>
        /// <param name="searchRegex">Automatically posted by JQuery DataTables: </param>
        /// <param name="order">Automatically posted by JQuery DataTables: </param>
        /// <returns>String formatted for JQuery DataTables.</returns>
        [HttpPost]
        public string DataTableItemVariables(int draw, int start, int length, string searchValue, string searchRegex, string order)
        {
            int itmId = -1;
            string result = String.Empty;
            Debug.WriteLine("Templates Controller DataTableItemVariables draw:" + draw.ToString());
            Debug.WriteLine("Templates Controller DataTableItemVariables start:" + start.ToString());
            Debug.WriteLine("Templates Controller DataTableItemVariables searchValue:" + searchValue);
            Debug.WriteLine("Templates Controller DataTableItemVariables searchRegex:" + searchRegex);
            Debug.WriteLine("Templates Controller DataTableItemVariables order:" + order);
            try
            {
                itmId = SessionHelper.SessionForm.itemId;

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

                    // rename to avoid language/library conflicts
                    int iniIndex = start;
                    int noDsplyRecs = length;

                    List<def_ItemVariables> query = formsRepo.GetAttachedItemById(itmId).def_ItemVariables.ToList();

                    dtr.recordsTotal = dtr.recordsFiltered = query.Count();
                    int index = 0;
                    foreach (def_ItemVariables q in query.Skip(iniIndex).Take(noDsplyRecs).ToList())
                    {
                        index++;
                        string[] data = new string[] {
                            index.ToString(),
                            q.itemVariableId.ToString(),
                            q.itemId.ToString(),
                            q.identifier,
                            "<a href=\"ItemVariableEdit?itemVariableId=" + q.itemVariableId + "\">Edit</a>",
                            q.def_BaseTypes.enumeration + " (" + q.baseTypeId.ToString() + ")",
                            q.defaultValue
                        };
                        dtr.data.Add(data);
                    }

                    Debug.WriteLine("Templates Controller DataTableItemVariables data populated.");

                    // Output the JSON in DataTable format
                    fastJSON.JSONParameters param = new fastJSON.JSONParameters();
                    param.EnableAnonymousTypes = true;
                    result = fastJSON.JSON.ToJSON(dtr, param);
                    Debug.WriteLine("Templates Controller DataTableItemVariables result:" + result);
                }
                catch (Exception excptn)
                {
                    Debug.WriteLine("Templates Controller DataTableItemVariables exception:" + excptn.Message);
                    result = excptn.Message + " - " + excptn.Message;
                }

            }
            catch (FormatException excptn)
            {
                Debug.WriteLine("Templates Controller DataTableItemVariables sectionId format exception:" + excptn.Message);
            }
            return result;

        }

        [HttpGet]
        public ActionResult ItemVariableEdit()
        {
            string itemVariableId = Request["itemVariableId"] as string;
            Debug.WriteLine("* * *  TemplatesController ItemVariableEdit itemVariableId: " + itemVariableId);

            Assmnts.Models.TemplateItemVariables tiv = new Assmnts.Models.TemplateItemVariables();
            tiv.itemVariables = new List<def_ItemVariables>();
            SessionForm sf = (SessionForm)Session["sessionForm"];

            if (!String.IsNullOrEmpty(itemVariableId))
            {
                Session["itemVariableId"] = itemVariableId;
                def_ItemVariables itmVar = formsRepo.GetItemVariableById(Convert.ToInt32(itemVariableId));
                tiv.itemVariables.Add(itmVar);
            }
            else
            {
                Session["itemVariableId"] = 0;
                def_ItemVariables itmVar = new def_ItemVariables();
                itmVar.def_Items = formsRepo.GetItemById(sf.itemId);
                itmVar.itemId = itmVar.def_Items.itemId;
                tiv.itemVariables.Add(itmVar);
            }
            // Setup session variables
            tiv.itemId = sf.itemId;

            Debug.WriteLine("* * * Templates Controller ItemVariableEdit itemVariables count: " + tiv.itemVariables.Count().ToString());

            //populate base types list
            tiv.baseTypes = new SelectList(formsRepo.GetBaseTypes(), "baseTypeId", "enumeration", tiv.itemVariables[0].baseTypeId.ToString());

            return View("itemVariableEdit", tiv);
        }

        [HttpGet]
        [Obsolete("ItemVariableAdd is Deprecated, use ItemVariableEdit instead")]
        public ActionResult ItemVariableAdd()
        {
            string itemId = Request["itemId"] as string;
            Debug.WriteLine("* * *  TemplatesController:ItemVariableAdd method  * * * itemId: " + itemId);

            Assmnts.Models.TemplateItemVariables tiv = new Assmnts.Models.TemplateItemVariables();
            tiv.itemVariables = new List<def_ItemVariables>();
            tiv.itm = formsRepo.GetItemById(Convert.ToInt32(itemId));
            Session["itemVariableId"] = 0;
            Session["itemId"] = itemId;
            def_ItemVariables itmVar = new def_ItemVariables();
            itmVar.itemVariableId = 0;
            itmVar.itemId = Convert.ToInt32(itemId);
            itmVar.baseTypeId = 14;     // default to String
            tiv.itemVariables.Add(itmVar);
            tiv.baseTypes = new SelectList(formsRepo.GetBaseTypes(), "baseTypeId", "enumeration", itmVar.baseTypeId.ToString());

            return View("itemVariableEdit", tiv);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult PostItemVariable(Assmnts.Models.TemplateItemVariables tivIn)
        {
            tivIn.itemVariables[0].def_Items = null;

            if (tivIn.itemVariables[0].itemVariableId == 0)
            {
                formsRepo.AddItemVariable(tivIn.itemVariables[0]);
            }
            else
            {
                formsRepo.ItemVariableSave(tivIn.itemVariables[0]);
            }

            Assmnts.Models.TemplateItemVariables tiv = new Assmnts.Models.TemplateItemVariables();
            tiv.itemVariables = new List<def_ItemVariables>();
            def_ItemVariables itmVar = formsRepo.GetItemVariableById(tivIn.itemVariables[0].itemVariableId);
            tiv.itemVariables.Add(itmVar);

            tiv.baseTypes = new SelectList(formsRepo.GetBaseTypes(), "baseTypeId", "enumeration", itmVar.baseTypeId.ToString());

            Session["itemVariableId"] = tivIn.itemVariables[0].itemVariableId.ToString();
            SessionForm sf = (SessionForm)Session["sessionForm"];
            Session["itemId"] = sf.itemId;
            tiv.thisScreenTitle = "(" + sf.itemId + ") " + formsRepo.GetItemById(sf.itemId).title;
            tiv.sectionId = sf.sectionId;

            Debug.WriteLine("* * *  TemplatesController PostItemVariable itemVariableId: " + tivIn.itemVariables[0].itemVariableId.ToString());

            return View("itemVars", tiv);
        }
        #endregion

        /*
         *  NOTE: the method below is probably NOT being used anywhere.
         *      The code was moved to the ResultsController
         * 
         */
        [HttpGet]
        public ActionResult Template()
        {
            Debug.WriteLine("* * *  TemplatesController:Template method  * * *");

            string sectionId = Request["sectionId"] as string;
            Debug.WriteLine("* * *  Template sectionId: " + sectionId);
            Session["section"] = sectionId;
            def_Sections sctn = formsRepo.GetSectionById(Convert.ToInt32(sectionId));
            // Setup session variables
            SessionForm sf = (SessionForm)Session["sessionForm"];
            sf.sectionId = sctn.sectionId;

            Assmnts.Models.GeneralForm amt = new Assmnts.Models.GeneralForm();
            List<def_Items> itemList = formsRepo.GetAllItemsForSection(sctn);

            switch (sctn.href)
            {
                case "SIS/idprof":
                    {
                        amt.fldLabels = new Dictionary<string, string>();
                        foreach (def_Items itm in itemList)
                        {
                            formsRepo.GetItemVariables(itm);
                            Debug.WriteLine("* * *  itemList.ItemVariables.Count: " + itm.def_ItemVariables.Count.ToString());
                            if (itm.def_ItemVariables.Count > 0)
                            {
                                string ident = itm.def_ItemVariables.First().identifier;
                                amt.fldLabels.Add(ident, itm.label);
                                amt.itmPrompts.Add(ident, itm.prompt);
                            }
                        }

                        //amt.rspValues = new Dictionary<string, string>();
                        //amt.rspValues.Add("ssn", "772-21-1234");
                        //amt.rspValues.Add("nmeLast", "Bogard");
                        //amt.rspValues.Add("nmeFirst", "Richard");
                        //amt.rspValues.Add("nmeMiddle", "X");
                        //amt.rspValues.Add("addrLine", "123 W. Maple Suite 21");
                        //amt.rspValues.Add("addrCity", "Rock City");
                        break;
                    }

                default:            // "SIS/section1a":     Default to the item list
                    {
                        amt.items = itemList;
                        Debug.WriteLine("* * *  FormsRepository  GetAllItems count: " + itemList.Count().ToString());
                        break;
                    }

            }

            return View(sctn.href, amt);
            
        }


        [HttpPost]
        public ActionResult Save(FormCollection frmCllctn, Assmnts.Models.TemplateItems ti)
        {

            Debug.WriteLine("* * *  TemplatesController:Save method  * * *");

            if ( !Session["section"].Equals(null) )
            {
                string strSctn = Session["section"] as string;
                Debug.WriteLine("* * *  TemplatesController: Session :" + strSctn);
            }

            foreach (var key in frmCllctn.AllKeys)
            {
                Debug.WriteLine("* * *  FormCollection  key/value: " + key + ":  " + frmCllctn[key]);
                // this is actually a ValueCollection frmCllctn[key]
                // foreach(var value in ValueCollection)
            }

            // Debug.WriteLine("* * *  Dictionary from model rspValues  sis_cl_last_nm: " + ti.rspValues["sis_cl_last_nm"]);
            // Debug.WriteLine("* * *  Dictionary from model rspValues  sis_cl_first_nm: " + ti.rspValues["sis_cl_first_nm"]);

            /*
            Section sctn = formsRepo.GetSectionById(1);
            List<Item> itemList = formsRepo.getSectionItems(sctn);

            Assmnts.Models.TemplateItems amt = new Assmnts.Models.TemplateItems();
            amt.items = itemList;
            Debug.WriteLine("* * *  FormsRepository  GetAllItems count: " + itemList.Count().ToString());
            */

            // Redisplay the section just Save'd
            return RedirectToAction("Template", "Templates", new { sectionId = Session["section"] });
            // return View(sctn.href, amt);

        }

        /// <summary>
        /// Pulls the parameter values from a formatted url string and places them in a RouteValueDictionary.
        /// 
        /// Example- Redirect?valueId=000&amp;valueText=Hello  - or - valueId=000&amp;valueText=Hello
        /// Return- RouteValueDictionary {
        ///             valueId = &quot;000&quot;,
        ///             valueText = &quot;Hello&quot;
        ///         }
        /// </summary>
        /// <param name="parameters">String containing the url to be parsed.</param>
        /// <returns>RouteValueDictionary containing parsed values.</returns>
        private RouteValueDictionary findValues(string url)
        {
            RouteValueDictionary values = new RouteValueDictionary();
            string[] parameters = (url.Contains("?")) ? url.Substring(url.IndexOf("?") + 1).Split('&') : url.Split('&');
            for (int i = 0; i < parameters.Count(); i++)
            {
                int val = parameters[i].IndexOf("=");
                string route = parameters[i].Substring(0, val);
                string value = parameters[i].Substring(val + 1);
                values.Add(route, value);
            }

            return values;
        }

    }

}
