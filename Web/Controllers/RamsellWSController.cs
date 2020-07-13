using AJBoggs.Adap.Services.Xml;

using Assmnts.Infrastructure;

using Data.Abstract;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Schema;

namespace Assmnts.Controllers
{
    [ADAPRedirectingAction]
    public class RamsellWSController : Controller
    {
        private IFormsRepository formsRepo;

        public RamsellWSController(IFormsRepository fr)
        {
            // Initiialized by Infrastructure.Ninject
            formsRepo = fr;
        }


        [HttpPost]
        public ActionResult TestRamsellXMLImport()
        {
            //load single schema from xsd into a schemaset
            string exampleInputPath = ControllerContext.HttpContext.Server.MapPath(RamsellExport.xsdPath);
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.Add(Utilities.xmlNamespaceURI, exampleInputPath);
            schemaSet.Compile();

            //load xml document from request stream
            XmlDocument doc = new XmlDocument();
            doc.Schemas = schemaSet;
            using (var sr = new StreamReader(Request.InputStream))
            {
                string docInput = sr.ReadToEnd();
                doc.LoadXml(docInput);
            }
            
            // check that the xml contains the namespace we're using
            if (!Utilities.ContainsNamespace(doc.DocumentElement))
                return Content(String.Format("xml must contain the namespace uri \"{0}\"", Utilities.xmlNamespaceURI));

            // validate xml based on schema
            List<string> validationErrors = new List<string>();
            doc.Validate((object sender, ValidationEventArgs args) => { validationErrors.Add(args.Message); });

            if (validationErrors.Count > 0)
            {
                //return a page with error messages if validation failed
                StringBuilder sb = new StringBuilder();
                sb.Append("<html><body><table>");
                foreach (string msg in validationErrors)
                    sb.AppendFormat("<tr><td>{0}</td></tr>", msg);
                sb.Append("</table></body></html>");

                return Content( sb.ToString() );
            }
            else
            {
                //process uploaded xml if validation passed
                ProcessUploadedXML(doc.DocumentElement);
                return Content("Validation passed!");
            }
        }

        private void ProcessUploadedXML(XmlElement node)
        {
            bool rspNode = true;
            foreach (XmlNode child in node.ChildNodes) 
            { 
                if( child is XmlElement )
                {
                    rspNode = false;
                    ProcessUploadedXML((XmlElement)child);
                }
            }
            
            if ( rspNode )
            {
                string tagName = node.Name;
                List<string> itemVariableIdentifiers = RamsellExport.GetItemVariableIdentifiersForRamsellTagName(tagName);
                string allIdentifiers = String.Join(", ", itemVariableIdentifiers.ToArray());
                string rspVal = node.InnerText;
                Debug.WriteLine("ProcessUploadedXML:\n\ttag \"{0}\" -> itemVariable(s) \"{1}\"\n\tvalue \"{2}\"",
                    tagName, allIdentifiers, rspVal);
            }
        }

        /// <summary>
        /// Export FormResults / Responsevariables to XML based on an XSD.
        /// Tag names will be transformed vai a lookup table.
        /// Data Type transformations will be done based on the XSD and ItemVariable data types.
        /// Transformation of data codes will be done by ?????
        /// </summary>
        /// <param name="formResultId"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult TestRamsellXMLExport( int? formResultId = null )
        {
            // find an ADAP assessment to export
            if (formResultId == null)
            {
                int formId = formsRepo.GetFormByIdentifier("ADAP").formId;
                formResultId = formsRepo.GetFormResultsByFormId(formId).First().formResultId;
            }
            Debug.WriteLine("* * *  TestRamsellXmlExport formResultId: " + formResultId.ToString());

            XmlDocument outputXmlDoc = RamsellExport.BuildRamsellXmlDocForFormResult(formResultId.Value, formsRepo);

            // stream result xml with line breaks and indents
            StringBuilder sbOut = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sbOut, Utilities.xmlWriterSettings))
            {
                outputXmlDoc.Save(writer);
            }

            FileContentResult fcr = File(Encoding.ASCII.GetBytes(sbOut.ToString()), "text/plain", "result.xml");

            return fcr;
        }
    }
}