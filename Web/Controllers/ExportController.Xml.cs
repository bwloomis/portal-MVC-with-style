using Assmnts.Infrastructure;

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Mvc;
using System.Xml;


namespace Assmnts.Controllers
{
    /*
     * This controller is used to download XML data.
     * Parameter: Session formId
     * 
     * Outputs ALL formResults for a Form.
     *      NOTE:  missing the scoring at this time (1/29/15)
     * 
     * Output: results.xml  and /Content/formResults?????.xml
     * 
     */
    public partial class ExportController : Controller
    {

        [HttpGet]
        public FileContentResult GetFormResultsXML()
        {
            string currentIdentifier = String.Empty;
            string outpath = ControllerContext.HttpContext.Server.MapPath("../Content/formResults_" + System.DateTime.Now.Ticks + ".xml");
            Debug.WriteLine("* * *  GetFormResultsXML ConnectionString: " + formsRepo.GetDatabase().Connection.ConnectionString);
            
            string formId = Request["formId"] as string;
            Debug.WriteLine("* * *  GetFormResultsXML formId: " + formId);
            Session["formId"] = formId;
            int iFormId = Convert.ToInt32(formId);

            // *****  HARD CODED FOR TESTING ****
            iFormId = 1;

            try
            {
                XmlWriterSettings xws = new XmlWriterSettings();
                xws.Indent = true;
                xws.OmitXmlDeclaration = true;
                xws.CloseOutput = false;
                // xws.CheckCharacters = false;
               

                XmlWriter xw = XmlWriter.Create(outpath, xws);
                using (xw)
                {
                    xw.WriteStartDocument();
                    xw.WriteStartElement("forms");

                    //int iFormId = 1;
                    def_Forms frm = formsRepo.GetFormById(iFormId);
                    xw.WriteStartElement("form");
                    xw.WriteAttributeString("identifier", frm.identifier);
                    xw.WriteAttributeString("title", frm.title);

                    // Get the identifiers for Part (create an array for each Part)
                    List<def_Parts> formParts = formsRepo.GetFormParts(frm);

                    Debug.WriteLine("* * *  GetFormResultsXML Start formParts.Count: " + formParts.Count.ToString());
                    List<String>[] partIdentifiers = new List<String>[formParts.Count];
                    int iPrt = 0;

                    foreach (def_Parts prt in formParts)
                    {
                        Debug.WriteLine("* * *  GetFormResultsXML Start loading identifiers for: " + prt.identifier);
                        partIdentifiers[iPrt] = new List<String>();
                        foreach (def_Sections sctn in formsRepo.GetSectionsInPart(prt))
                        {
                            formsRepo.CollectItemVariableIdentifiers(sctn, partIdentifiers[iPrt]);
                        }
                        Debug.WriteLine("* * *  GetFormResultsXML Finished loading identifiers for: " + prt.identifier + "   # of idnts: " + partIdentifiers[iPrt].Count);
                        iPrt++;
                    }

                    // Output the XML
                    Debug.WriteLine("* * *  GetFormResultsXML Output the XML");
                    IEnumerable<def_FormResults> formResults = formsRepo.GetFormResultsByFormId(iFormId);
                    foreach (def_FormResults frmRslt in formResults)
                    {
                        if (frmRslt.formResultId == 1000193)       // Just write out 1 Assessments
                            continue;

                        AccessLogging.InsertAccessLogRecord(formsRepo, frmRslt.formResultId, (int)AccessLogging.accessLogFunctions.EXPORT, "Export XML of assessment");
                        
                        xw.WriteStartElement("Assessment");
                        xw.WriteAttributeString("Client_ID", "123456");
                        xw.WriteAttributeString("SIS_Id", frmRslt.formResultId.ToString());
                        xw.WriteAttributeString("identifier", frm.identifier);
                        xw.WriteAttributeString("group", frmRslt.GroupID.HasValue ? frmRslt.GroupID.ToString() : String.Empty);
                        xw.WriteAttributeString("interviewer", frmRslt.interviewer.HasValue ? frmRslt.interviewer.ToString() : String.Empty);
                        xw.WriteAttributeString("status", frmRslt.formStatus.ToString());
                        xw.WriteAttributeString("dateCompleted", frmRslt.dateUpdated.ToShortDateString());
                        xw.WriteAttributeString("deleted", frmRslt.deleted.ToString());
                        xw.WriteAttributeString("locked", frmRslt.locked.ToString());
                        xw.WriteAttributeString("archived", frmRslt.archived.ToString());

                        int iPart = 0;
                        foreach (def_Parts prt in formParts)
                        {
                            /*
                            if (prt.identifier.Equals("Reports"))
                            {
                                continue;
                            }
                             */
                            Debug.WriteLine("* * *  GetFormResultsXML Output Section: " + prt.identifier);
                            xw.WriteStartElement("Section");
                            xw.WriteAttributeString("identifier", prt.identifier);

                            foreach (String idnt in partIdentifiers[iPart])
                            {
                                currentIdentifier = idnt;           // To assist with debugging during an Exception
                                if (prt.identifier.Contains("Scores") && !idnt.StartsWith("scr"))
                                {
                                    continue;
                                }
                                //string xmlName = Char.IsDigit(idnt[0]) ? "Q" + idnt : idnt;
                                if (Char.IsDigit(idnt[0]))
                                {
                                    throw new Exception("for xml exporting, identifiers cannot begin with numbers: \"" + idnt + "\"");
                                }

                                def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(frmRslt.formResultId, idnt);
                                string val = (rv == null) ? String.Empty : rv.rspValue;
                                xw.WriteElementString(idnt, String.IsNullOrEmpty(val) ? String.Empty : val.Trim());
                            }

                            xw.WriteEndElement();       // SIS Section 
                            iPart++;
                        }

                        xw.WriteEndElement();       // Assessment
                    }
                    xw.WriteEndElement();       // form
                    xw.WriteEndElement();       // forms

                    xw.WriteEndDocument();
                }
                xw.Flush();
                xw.Close();

            }
            catch (SqlException sqlXcptn)
            {
                Debug.WriteLine("* * *  GetFormResultsXML SqlException: " + sqlXcptn.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  GetFormResultsXML currentIdentifier: " + currentIdentifier + "    Exception.Message: " + ex.Message);
            }

            return File(System.IO.File.ReadAllBytes(outpath), "text/xml", "results.xml");

        }


    }
}
