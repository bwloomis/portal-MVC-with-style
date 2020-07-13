using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Xml;
using System.IO;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
using System.Net;

using Assmnts.Models;
using Assmnts.UasServiceRef;

using Data.Abstract;
using Data.Concrete;


namespace Assmnts.Controllers
{
    public partial class DefwsController : Controller
    {
        private static readonly AuthenticationClient auth = new AuthenticationClient();

        private int getIntAttribute(XmlNode assmntNode, string attr)
        {
            int result = 0;
            var nodeAttr = assmntNode.Attributes == null ? null : assmntNode.Attributes[attr];
            if (nodeAttr == null)
            {
                Debug.WriteLine("could not find assessment node attribute \"" + attr + "\"");
            }
            else
            {
                try
                {
                    result = Convert.ToInt32(nodeAttr.Value);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("error parsing " + attr + " (integer) from string \"" + nodeAttr.Value + "\"");
                }
            }
            return result;
        }

        [HttpGet]
        public ActionResult TestUploadMultipleAssmntXmls()
        {
            //any xml descending from this directory will be uploaded
            string xmlDir = "C:\\Users\\otessmer\\Documents\\SIS_xmls";

            int count = uploadAssmntXmlDir(xmlDir);

            return Content("Success! Uploaded " + count + " assessments from xml" );
        }

        private int uploadAssmntXmlDir(string dirPath)
        {
            int count = 0;
            foreach (string dir in System.IO.Directory.EnumerateDirectories(dirPath))
                count += uploadAssmntXmlDir(dir);
            foreach (string xml in System.IO.Directory.EnumerateFiles(dirPath))
            {
                try
                {
                    uploadAssmntXml(xml);
                    count++;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }
            return count;
        }

        private void uploadAssmntXml(string xmlPath)
        {
            Debug.WriteLine( "uploading xml \"" + xmlPath + "\"" );

            var _url = "http://localhost:50209/Defws/Upload";

            string xmlName = Path.GetFileNameWithoutExtension(xmlPath);
            int sisID = Convert.ToInt32(xmlName.Replace("sisId_", ""));

            XmlDocument soapEnvelopeXml = CreateSoapEnvelope(sisID,xmlPath);
            HttpWebRequest webRequest = CreateWebRequest(_url);
            InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);

            // begin async call to web request.
            IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);

            // suspend this thread until call is complete. You might want to
            // do something usefull here like update your UI.
            asyncResult.AsyncWaitHandle.WaitOne();

            // get the response from the completed web request.
            try
            {
                string soapResult;
                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                {
                    using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                }
                Debug.WriteLine(soapResult);
            }
            catch (Exception e) { }
        }

        private void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }

        private HttpWebRequest CreateWebRequest(string url)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private XmlDocument CreateSoapEnvelope(int sisID, string xmlPath)
        {
            XmlDocument soapEnvelop = new XmlDocument();

            //working soap for action VerifyUser
            //soapEnvelop.LoadXml("<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">  <soap12:Body>    <VerifyUser xmlns=\"http://SisServiceDev.ajboggs.com\">      <userID>Oliver</userID>      <passwd>p@ssword</passwd>      <validUser>true</validUser>      <entId>7</entId>      <subId>31</subId>    </VerifyUser>  </soap12:Body></soap12:Envelope>");

            //working soap for action GetDataSisID
            //soapEnvelop.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">  <soap12:Body>    <GetDataSisID xmlns=\"http://SisServiceDev.ajboggs.com\">      <userID>Oliver</userID>      <passwd>p@ssword</passwd>      <sisID>" + sisID + "</sisID>      <section>SS_FullAssessment</section>      <data>string</data>    </GetDataSisID>  </soap12:Body></soap12:Envelope>");

            soapEnvelop.LoadXml(System.IO.File.ReadAllText(xmlPath));

            return soapEnvelop;
        }

        [HttpPost]
        public ActionResult Upload()
        {
            XmlDocument doc = new XmlDocument();
            using (var sr = new StreamReader(Request.InputStream))
            {
                string docInput = sr.ReadToEnd();
                doc.LoadXml(docInput);
            }

            // Since there is a CDATA in SOAP, may need to get the
            //   sis:data node and stream or string that into XML Document
            // or it appears that SOAP Formatter may do this for you.
            //XmlNode nodeSisData = doc.GetElementsByTagName("sis:data")[0];

            //XmlDocument assmntsDoc = new XmlDocument();
            //assmntsDoc.LoadXml(nodeSisData.InnerText);

            //Find the node from XML document with name Assessments
            XmlNode nodeAssmnts = doc.GetElementsByTagName("data")[0].ChildNodes[0];
            //XmlNode nodeAssmnts = assmntsDoc.GetElementsByTagName("Assessments")[0];

            //Loop through the child nodes (Assessment tags - there can be multiple)
            foreach (XmlNode assmntNode in nodeAssmnts.ChildNodes)
            {
                if ((assmntNode).NodeType == XmlNodeType.Element)
                {
                    int subject = getIntAttribute(assmntNode, "ClientId");//getIntAttribute(assmntNode, "subject");
                    int groupId = 93;// getIntAttribute(assmntNode, "GroupID");
                    int entId = 20;// getIntAttribute(assmntNode, "EnterpriseID");
                    int formId = 1;// getIntAttribute(assmntNode, "formId");

                    //add new form result in database
                    def_FormResults formResult = new def_FormResults();
                    formResult.formId = formId;
                    formResult.EnterpriseID = entId;
                    formResult.GroupID = groupId;
                    formResult.subject = subject;
                    formResult.dateUpdated = DateTime.Now;
                    formResult.formStatus = 0;
                    formResult.sessionStatus = 0;
                    formResult.reviewStatus = 0;
                    formResult.training = false;

                    int formRsltId = formsRepo.AddFormResult(formResult);
                    formResult = formsRepo.GetFormResultById(formRsltId);

                    foreach (XmlNode assmntChildNode in assmntNode.ChildNodes)
                    {
                        switch (assmntChildNode.Name)
                        {
                            case "Profile":
                                Upload_ProcessProfileNode(assmntChildNode, formResult);
                                break;

                            case "Sections":
                                Upload_ProcessSectionsNode(assmntChildNode, formResult);
                                break;

                            default:
                                Debug.WriteLine("Unrecognized assessment child node \"" + assmntChildNode.Name + "\"");
                                break;
                        }
                    }

                    //form results are typically changed in Upload_ProcessProfileNode above
                    formsRepo.SaveFormResults(formResult);
                }
            }

            return Content("Upload Successful!" );
        }

        private void Upload_ProcessSectionsNode(XmlNode sectionsNode, def_FormResults formResult)
        {
            foreach (XmlNode sectionsChildNode in sectionsNode)
            {
                switch (sectionsChildNode.Name)
                {
                    case "Section":
                        int sectionNum = getIntAttribute(sectionsChildNode, "SectionNum");
                        XmlAttribute partAttr = sectionsChildNode.Attributes["Part"];
                        if (partAttr == null)
                        {
                            Debug.WriteLine("missing attribite \"Part\" in section node (skipping): " + getNodePath(sectionsChildNode) );
                            continue;
                        }
                        string sectionPartLetter = partAttr.Value;
                        XmlAttribute descAttr = sectionsChildNode.Attributes["Description"];
                        bool oldFormat = false;

                        //data exported from the old system has a wierd description attribute for each section
                        if (descAttr != null && descAttr.Value.StartsWith("AAMR"))
                        {
                            oldFormat = true;
                            sectionNum = mapOldSectionNumToNewSystem(sectionNum);
                            if (sectionNum == 3)
                                sectionPartLetter = "A";
                        }

                        Upload_ProcessSingleSectionNode(sectionsChildNode, sectionNum, sectionPartLetter, formResult, oldFormat);
                        break;

                    default:
                        Debug.WriteLine("Unrecognized sections child node \"" + sectionsChildNode.Name + "\"");
                        break;
                }
            }
        }

        private string getNodePath(XmlNode node)
        {
            if (node.ParentNode == null)
                return node.Name;
            return getNodePath(node.ParentNode) + " -> " + node.Name + "[" + getNodeIndexInParent(node) + "]";
        }

        private int getNodeIndexInParent(XmlNode node)
        {
            XmlNodeList xnl = node.ParentNode.ChildNodes;
            for (int i = 0; i < xnl.Count; i++)
                if (xnl[i].Equals(node))
                    return i;
            return -1;
        }

        private void Upload_ProcessSingleSectionNode(XmlNode sectionNode, int sectionNum, string sectionPartLetter, def_FormResults formResult, bool oldFormat)
        {
            foreach (XmlNode sectionChildNode in sectionNode)
            {
                switch (sectionChildNode.Name)
                {
                    case "Question":
                        Upload_ProcessQuestionNode(sectionChildNode, sectionNum, sectionPartLetter, formResult, oldFormat);
                        break;
                }
            }
        }

        private void Upload_ProcessQuestionNode(XmlNode questionNode, int sectionNum, string sectionPartLetter, def_FormResults formResult, bool oldFormat)
        {
            int questionNum = getIntAttribute(questionNode, "num");
            if( oldFormat )
                questionNum = mapOldQuestionNumToNewSystem( questionNum, sectionNum, sectionPartLetter );

            foreach (XmlNode questionChildNode in questionNode)
            {
                string suffix;
                switch (questionChildNode.Name)
                {
                    case "Frequency":
                        suffix = "Fqy";
                        break;
                    case "DailySupport":
                        suffix = "DST";
                        break;
                    case "TypeOfSupport":
                        suffix = "TOS";
                        break;
                    case "ImportantTo":
                        suffix = "ImportantTo";
                        break;
                    case "ImportantFor":
                        suffix = "ImportantFor";
                        break;
                    case "Notes":
                        suffix = "Notes";
                        break;
                    case "SupportNeeds":
                        suffix = sectionPartLetter.Equals( "A" ) ? "ExMedSupport" : "ExBehSupport";
                        break;
                    case "Other":
                        suffix = "Other";
                        break;
                    default:
                        Debug.WriteLine("Unrecognized question child node name \"" + questionChildNode.Name + "\"");
                        suffix = questionChildNode.Name;
                        break;
                }

                string identifier;
                if( suffix.Equals("Other") )
                    identifier = sectionPartLetter.Equals( "A" ) ? "S1aPageOther" : "S1bPageOther";
                else
                    identifier = "Q" + sectionNum + sectionPartLetter + questionNum + "_" + suffix;
                Upload_SaveResponseVariable(identifier, questionChildNode.InnerText, formResult);
            }
        }

        private void Upload_ProcessProfileNode(XmlNode profileNode, def_FormResults formResult)
        {
            // Get the data tags in the Profile node
            foreach (XmlNode profileChildNode in profileNode)
            {
                switch (profileChildNode.Name)
                {
                    case "SupportProviders":
                        Upload_ProcessSupportProvidersNode(profileChildNode, formResult);
                        break;

                    case "Respondants":
                        Upload_ProcessRespondantsNode(profileChildNode, formResult);
                        break;

                    default:
                        Upload_ProcessItemNode(profileChildNode, formResult);
                        break;
                }
            }
        }

        private void Upload_ProcessSupportProvidersNode(XmlNode supNode, def_FormResults formResult)
        {
            foreach (XmlNode supChildNode in supNode)
            {
                int num = getIntAttribute(supChildNode, "num");
                foreach (XmlNode supItemNode in supChildNode)
                {
                    if (supItemNode.InnerText == null || supItemNode.InnerText.Length == 0)
                        continue;

                    string suffix;
                    switch (supItemNode.Name)
                    {
                        case "Name": suffix = "name";
                            break;
                        case "RealationType": suffix = "reln";
                            break;
                        case "PhoneNum": suffix = "phone";
                            break;
                        case "PhoneExt": suffix = "ext";
                            break;
                        default:
                            throw new Exception("Unrecognized support provider item node \"" + supItemNode.Name + "\"");
                    }
                    string identifier = "sis_sup" + num + "_" + suffix;
                    Upload_SaveResponseVariable(identifier, supItemNode.InnerText, formResult);
                }
            }
        }

        private void Upload_ProcessRespondantsNode(XmlNode respNode, def_FormResults formResult)
        {
            foreach (XmlNode rspChildNode in respNode)
            {
                int num = getIntAttribute(rspChildNode, "num");
                foreach (XmlNode rspItemNode in rspChildNode)
                {
                    if (rspItemNode.InnerText == null || rspItemNode.InnerText.Length == 0)
                        continue;

                    string suffix;
                    switch (rspItemNode.Name)
                    {
                        case "FirstName": suffix = "firstn";
                            break;
                        case "LastName": suffix = "lastn";
                            break;
                        case "RealtionType": suffix = "reln";
                            break;
                        case "LanguageSpoken": suffix = "lang";
                            break;
                        default:
                            throw new Exception("Unrecognized respondant item node \"" + rspItemNode.Name + "\"");
                    }
                    string identifier = "sis_res" + num + "_" + suffix;
                    Upload_SaveResponseVariable(identifier, rspItemNode.InnerText, formResult);
                }
            }
        }

        private void Upload_ProcessItemNode(XmlNode itemNode, def_FormResults formResult)
        {
            switch (itemNode.Name)
            {

                case "SitChanged": case "Upload_Info": 
                    break;

                case "SubscriptionID":
                    //groupId is hard-coded now
                    //alternatively we could look it up using the commented code below

                    //int oldSystemSubId = -1;
                    //try
                    //{
                    //    oldSystemSubId = Convert.ToInt32(itemNode.InnerText);
                    //}
                    //catch (Exception e){}
                    //if (oldSystemSubId < 0)
                    //{
                    //    Debug.WriteLine("SubscriptionID node: could not parse valid old-system subscription id from string \"" + itemNode.InnerText + "\", setting GroupID to 0");
                    //    formResult.GroupID = 0;
                    //    break;
                    //}

                    //try
                    //{
                    //    formResult.GroupID = MapOldToNewGroupId(oldSystemSubId, formResult.EnterpriseID.Value );
                    //}
                    //catch (Exception e)
                    //{
                    //    Debug.WriteLine("SubscriptionID node: could not map old-system susbscription id " + oldSystemSubId + " to a new-system GroupID, setting GroupID to 0");
                    //    formResult.GroupID = 0;
                    //}
                    break;

                case "InterviewerUserId":
                    UasServiceRef.User interviewer = auth.GetUserByLoginID(itemNode.InnerText);
                    if (interviewer == null)
                    {
                        Debug.WriteLine("InterviewerUserId node: Could not find user with loginID \"" + itemNode.InnerText + "\", setting interviewer to 0");
                        formResult.interviewer = 0;
                    }
                    else
                        formResult.interviewer = interviewer.UserID;
                    break;

                case "UserID":
                    UasServiceRef.User assigned = auth.GetUserByLoginID(itemNode.InnerText);
                    if (assigned == null)
                    {
                        Debug.WriteLine("UserID node: Could not find user with loginID \"" + itemNode.InnerText + "\", setting assigned to 0");
                        formResult.assigned = 0;
                    }
                    else
                        formResult.assigned = assigned.UserID;
                    break;

                case "Training":
                    formResult.training = Convert.ToBoolean(itemNode.InnerText);
                    break;

                case "ReviewStatus":
                    formResult.reviewStatus = Convert.ToByte(itemNode.InnerText);
                    break;

                case "SIS_StatusChangeDate":
                    formResult.statusChangeDate = Convert.ToDateTime(itemNode.InnerText);
                    break;

                case "SIS_STATUS":
                    formResult.formStatus = Convert.ToByte(itemNode.InnerText);
                    break;

                case "sis_archived":
                    formResult.archived = Convert.ToBoolean(itemNode.InnerText);
                    break;

                //normal case (for items that corespond to itemVariables i the new system)
                default:
                    string itmVarIdentifier = convertToNewIdentifier(itemNode.Name);
                    string rspVal = itemNode.InnerText;
                    Upload_SaveResponseVariable(itmVarIdentifier, rspVal, formResult );
                    break;
            }
        }

        //private int MapOldToNewGroupId(int oldGroupId, int entId)
        //{
        //    string inputPath = "C:\\users\\otessmer\\downloads\\oldsis_subs.txt";

        //    using (System.IO.StreamReader file = new System.IO.StreamReader(inputPath))
        //    {
        //        string line;
        //        while ((line = file.ReadLine()) != null)
        //        {
        //            string[] parts = line.Split('\t');

        //            int thisGroupId = Convert.ToInt32(parts[0]);


        //            if (thisGroupId == oldGroupId)
        //            {
        //                //find enterprise in the new system that matches this name
        //                foreach (Group grp in auth.GetGroupsByEnterpriseID(entId))
        //                {
        //                    if (grp.GroupName.Equals(parts[36]))
        //                        return grp.GroupID;
        //                }

        //                throw new Exception("could not find new-system group with groupName \"" + parts[36] + "\" and enterpriseId \"" + entId + "\"");
        //            }
        //        }
        //    }

        //    throw new Exception("could not get new-system group id for old-system group id " + oldGroupId);
        //}

        private readonly Dictionary<string, def_ItemVariables> itemVariablesByIdentifier = new Dictionary<string, def_ItemVariables>();
        private def_ItemVariables GetItemVariableByIdentifier(string ident)
        {
            if (!itemVariablesByIdentifier.ContainsKey(ident))
                itemVariablesByIdentifier.Add(ident, formsRepo.GetItemVariableByIdentifier(ident));
            return itemVariablesByIdentifier[ident];
        }

        private void Upload_SaveResponseVariable(string itmVarIdentifier, string rspVal, def_FormResults fr)
        {
            if (String.IsNullOrWhiteSpace( rspVal ) )
                return;

            Debug.WriteLine( "sving identifier/value: " + itmVarIdentifier + " -> " + rspVal);

            def_ItemVariables iv = GetItemVariableByIdentifier(itmVarIdentifier);
            if (iv == null)
            {
                Debug.WriteLine("could not find item variable with identifier \"" + itmVarIdentifier + "\" (not case-sensitive)");
                return;
            }

            //delete any existing response for this formResult+itemVariable
            def_ResponseVariables existingRv = formsRepo.GetResponseVariablesByFormResultItemVarId(fr.formResultId, iv.itemVariableId);
            if (existingRv != null)
            {
                formsRepo.DeleteResponseVariableNoSave(existingRv);
            }

            //for response values that reresent dates, convert them to the new format
            try
            {
                if (iv.baseTypeId == 3)
                    rspVal = convertOldToNewDate(rspVal);
            }
            catch (DateNotSupportedException dnse)
            {
                Debug.WriteLine("found date prior to 1900 in response for item variable \"" + itmVarIdentifier + "\", skipping...");
                return;
            }

            //
            if (itmVarIdentifier.Equals("sis_cl_attend"))
            {
                switch (rspVal)
                {
                    case "All Of":  rspVal = "1"; break;
                    case "Part Of": rspVal = "2"; break;
                    case "Did Not": rspVal = "3"; break;
                }
            }

            def_ItemResults ir = fr.def_ItemResults.Where(r => r.itemId == iv.itemId).FirstOrDefault();//formsRepo.GetItemResultByFormResItem(formRsltId, iv.itemId);
            //int itemResultId;
            if (ir == null)
            {
                ir = new def_ItemResults();
                //ir.formResultId = formRsltId;
                ir.itemId = iv.itemId;
                ir.sessionStatus = 0;
                ir.dateUpdated = DateTime.Now;

                fr.def_ItemResults.Add(ir);
                //try{
                //    itemResultId = formsRepo.AddItemResult(ir);
                //}
                //catch (Exception e)
                //{
                //    return;
                //    Debug.WriteLine("error while adding item result! ItemVariable Identifier: \"{0}\", response value: \"{1}\", formResultId: \"{2}\"", itmVarIdentifier, rspVal, formRsltId);
                //}
            }
            //else
            //{
            //    itemResultId = ir.itemResultId;
            //}

            //if there is already a response variable for this itemvariable, return
            //if (ir.def_ResponseVariables.Where(v => v.itemVariableId == iv.itemVariableId).Any())
            //    return;

            def_ResponseVariables rv = new def_ResponseVariables
            {
                //itemResultId = itemResultId,
                itemVariableId = iv.itemVariableId,
                rspValue = rspVal
            };

            try
            {
                formsRepo.ConvertValueToNativeType(iv, rv);
            }
            catch (Exception e)
            {
                Debug.WriteLine("For item variable \"{0}\", Unable to convert value \"{1}\" to native type (baseTypeId {2})", iv.identifier, rspVal, iv.baseTypeId);
                return;
            }

            ir.def_ResponseVariables.Add(rv);
            //try
            //{
            //    formsRepo.AddResponseVariable(rv);
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine("error while adding response variable! ItemVariable Identifier: \"{0}\", response value: \"{1}\", formResultId: \"{2}\"", itmVarIdentifier, rspVal, formRsltId);
            //}

            //formsRepo.Save();
        }

        private int mapOldQuestionNumToNewSystem(int oldQuestionNum, int sectionNum, string sectionPartLetter)
        {
            return mapOldQuestionNumToNewSystem(oldQuestionNum, sectionNum + sectionPartLetter);
        }

        private int mapOldQuestionNumToNewSystem(int oldQuestionNum, string sectionId )
        {
            for (int row = 0; row*3 < oldToNewQuestionNums.Length; row++)
                if (oldToNewQuestionNums[row, 0].Equals(sectionId) && (int)oldToNewQuestionNums[row, 1] == oldQuestionNum)
                    return (int)oldToNewQuestionNums[row, 2];
            return oldQuestionNum;
        }

        private int mapOldSectionNumToNewSystem(int oldSectionNum)
        {
            switch (oldSectionNum)
            {
                case 1:
                    return 2;
                case 2:
                    return 3;
                case 3:
                    return 1;
                default:
                    Debug.WriteLine( "Could not find section number mapping for old section number \"" + oldSectionNum + "\"" );
                    return oldSectionNum;
            }
        }

        private static object[,] oldToNewQuestionNums = {
            //{section,	old, new}
            { "1A",  16,    19},
            { "1B",  1,     2},
            { "1B",  2,     3},
            { "1B",  3,     4},
            { "1B",  4,     5},
            { "1B",  5,     7},
            { "1B",  7,     9},
            { "1B",  9,     1},
            { "1B",  10,    11},
            { "1B",  11,    10},
            { "2A",  1,     3},
            { "2A",  2,     7},
            { "2A",  3,     5},
            { "2A",  4,     6},   
            { "2A",  5,     8},
            { "2A",  6,     4},
            { "2A",  7,     2},   
            { "2A",  8,     1},
            { "2B",  3,     5},
            { "2B",  4,     8},
            { "2B",  5,     3},
            { "2B",  8,     4},
            { "2C",  1,     8},
            { "2C",  2,     6},
            { "2C",  3,     1},
            { "2C",  4,     9},
            { "2C",  5,     7},
            { "2C",  6,     2},
            { "2C",  7,     3},
            { "2C",  8,     4},
            { "2C",  9,     5},
            { "2D",  1,     2},
            { "2D",  2,     1},
            { "2E",  2,     3},
            { "2E",  3,     4},
            { "2E",  4,     2},
            { "2F",  1,     6},
            { "2F",  5,     7},
            { "2F",  6,     1},
            { "2F",  7,     5},
            { "3A",  2,     7},
            { "3A",  7,     2},
        };

        //think of this as a dictionary that maps old identifiers to new identifiers
        //if an old identifier is the same as the new one, it should not be included
        //nothing here is case-sensitive
        private static string[,] oldToNewIdentifiers = {

            //{ "old_dentifier", "new_identifier" }
                                    
            { "Sis_Completed_Date",         "sis_completed_dt"      },
            { "Sis_Modified_Date",          "sis_modified_dt"       },
            { "sis_medicaidNum",            "sis_cl_medicaidNum"    },
            { "Sis_SSN",                    "sis_cl_ssn"            },
            { "Sis_cl_State",               "sis_cl_st"             },
            { "Sis_cl_DOB",                 "sis_cl_dob_dt"         },
            { "Sis_cl_Phone_Num",           "sis_cl_phone"          },
            { "Sis_cl_Phone_Ext",           "sis_cl_ext"            },
            { "Sis_cl_Lang_Spoken",         "sis_cl_lang"           },
            { "Sis_cl_Sex",                 "sis_cl_sex_cd"         },
            { "sis_cl_county",              "sis_cl_cou"            },
            { "Sis_Int_Full_Name",          "sis_int_full_nm"       },
            { "Sis_Int_Position",           "sis_int_position_cd"   },
            { "Sis_Int_Agency_Name",        "sis_int_agency_nm"     },
            { "Sis_Int_State",              "sis_int_st"            },
            { "Sis_DataEntry_First_Name",   "sis_entry_firstn"      },
            { "Sis_DataEntry_Last_Name",    "sis_entry_lastn"       },
            { "InterviewStartTime",         "sis_startTime"         },
            { "InterviewEndTime",           "sis_endTime"           },
            { "InterviewSetting",           "sis_cl_set"            },
            { "IndividualParticipation",    "sis_cl_attend"         },
            { "ReasonCompleted",            "sis_cl_why"            },
            { "Sis_Other_Info",             "Prof1_Notes"           },
            { "LivingSituation",            "sis_cl_living"         },
            { "sis_cl_phone_num_ext",       "sis_cl_ext"            },
            { "sis_cl_lang_spoken_cd",      "sis_cl_lang"           },
            { "sis_dataentry_first_nm",     "sis_entry_firstn"      },
            { "sis_dataentry_last_nm",      "sis_entry_lastn"       },
        };

        //returns the new identifier if the oldIdentifier is present on the array above
        //returns the oldIdentifier otherwise
        private string convertToNewIdentifier(string oldIdentifier)
        {
            for (int row = 0; row * 2 < oldToNewIdentifiers.Length ; row++)
            {
                if (oldToNewIdentifiers[row, 0].ToLower().Equals(oldIdentifier.ToLower()))
                {
                    string result = oldToNewIdentifiers[row, 1];
                    //Debug.WriteLine( "converted old->new identifier \"{0}\" -> \"{1}\"", oldIdentifier, result );
                    return result;
                }
            }
            return oldIdentifier;
        }

        private string convertOldToNewDate(string oldDate)
        {
            //first, confirm that the input string is in the old format YYYY-MM-DD
            if (oldDate == null)
                return null;
            string[] parts = oldDate.Split('-');
            if (parts.Length != 3)
                return oldDate;
            if (!(parts[0].Length == 4 && parts[1].Length == 2 && parts[2].Length == 2))
                return oldDate;

            try
            {
                int year = Convert.ToInt16(parts[0]);
                if (year < 1900)
                    throw new DateNotSupportedException();
            }
            catch (FormatException fe){}

            //do conversion to new format MM/DD/YYYY
            return String.Join("/", parts[1], parts[2], parts[0]);
        }

        private class DateNotSupportedException : Exception {}


        //OT 2/6/15 below is the code where I was playing with SoapFormatter, if anyone's interested
        //now I'm testing from UnitTestSoap.cs, I will delete the comments below when I have the soap import working

        //[HttpPost]
        //public ActionResult Upload()
        //{
        //    //XmlDocument doc = new XmlDocument();
        //    //using (var sr = new StreamReader(Request.InputStream))
        //    //{
        //    //    string docInput  = sr.ReadToEnd();
        //    //    doc.LoadXml(docInput);
        //    //}

        //    //XmlElement docElem = doc.DocumentElement;

        //    //de-serialize the uploaded object
        //    try
        //    {
        //        SoapFormatter formatter = new SoapFormatter();
        //        AssmntRecordSet ht = (AssmntRecordSet)formatter.Deserialize(Request.InputStream);
        //        Debug.WriteLine("successfully deserialized soap object!");
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.WriteLine("Failed to deserialize. Reason: " + e.Message);
        //    }
        //    finally
        //    {
        //        Request.InputStream.Close();
        //    }

        //    //create a serializable test object
        //    AssmntRecordSet testResult = new AssmntRecordSet();
        //    AssmntRecord testRecord = new AssmntRecord();
        //    testRecord.fields = new AssmntField[]{ new AssmntField("sis_Track_Num", "8888") };
        //    testResult.records = new AssmntRecord[] { testRecord };

        //    //return the serialized test object
        //    Stream stream = new MemoryStream();
        //    SoapFormatter sf = new SoapFormatter();
        //    sf.Serialize(stream, testResult);

        //    StringBuilder s = new StringBuilder();

        //    stream.Seek(0, SeekOrigin.Begin);
        //    while (true)
        //    {
        //        int b = stream.ReadByte();
        //        if (b == -1)
        //            break;

        //        s.Append(Convert.ToChar(b));
        //    }
        //    stream.Close();

        //    return Content(s.ToString());
        //}


        ////AssmntRecordSet is the top-level object that defines the structure of the soap objects
        //[Serializable]
        //public class AssmntRecordSet
        //{
        //    //add any fields here that should be included FOR EACH UPLOAD

        //    public AssmntRecord[] records;
        //}

        //[Serializable]
        //public class AssmntRecord
        //{
        //    //add any fields here that should be included FOR EACH ASSESSMENT

        //    public AssmntField[] fields;
        //}

        //[Serializable]
        //public class AssmntField
        //{
        //    public string ivIdentifier, value;

        //    public AssmntField(string ivIdentifier, string value)
        //    {
        //        this.ivIdentifier = ivIdentifier;
        //        this.value = value;
        //    }
        //}

    }
}
