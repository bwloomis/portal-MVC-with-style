using Assmnts;
using Assmnts.Infrastructure;

using Data.Abstract;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace AJBoggs.Adap.Services.Xml
{
    public static class RamsellExport
    {
        public const string xsdPath = @"..\Content\adap\docs\Colorado Enrollment Schema.xsd";

        public static XmlDocument BuildRamsellXmlDocForFormResult( int formResultId, IFormsRepository formsRepo )
        {
            string xsdFilePath = HttpContext.Current.Server.MapPath(xsdPath);
            XmlSchemaSet schemaSet = Utilities.GetXsdSchemaSet(xsdFilePath);

            // Extract the schema sets from the XSD (microsoft actually does it this way)
            // https://msdn.microsoft.com/en-us/library/ms255932(v=vs.110).aspx
            XmlSchema schema = null;
            foreach (XmlSchema xs in schemaSet.Schemas())
            {
                schema = xs;
            }

            // convert schema heirarchy into custom types. See "SchemaElement" and subclasses below.
            // (the ResponseElement subclass will be used to link "leaf" nodes with item variables)
            List<RamsellExport.DefSchemaElement> defElements = RamsellExport.ParseSchemaElements(schema.Elements.Values);

            // build response xml document based on schema
            XmlDocument outputXmlDoc = new XmlDocument();

            // Removed schemaSet from top level tag per Ramsell
            // outputXmlDoc.Schemas.Add(schemaSet);
            //outputXmlDoc.AppendChild(outputXmlDoc.CreateComment("Responses taken from formResult " + formResultId));
            foreach (RamsellExport.DefSchemaElement elem in defElements)
            {
                RamsellExport.BuildResponseXml(null, outputXmlDoc, elem, formResultId, formsRepo);
            }

            // Validate response xml based on schema
            // resultDoc.Validate((object sender, ValidationEventArgs args) => { throw new Exception(args.Message); } );
            try
            {
                Debug.WriteLine("XSD resultDoc.Schemas.Count: " + outputXmlDoc.Schemas.Count.ToString());
                /*
                foreach (System.Xml.Schema.XmlSchemaSet xss in resultDoc.Schemas.Schemas())
                {
                    Debug.WriteLine("XSD Schemas NameTable: " + xss.NameTable.ToString());
                }
                */
                ValidationEventHandler eventHandler = new ValidationEventHandler(ValidationEventHandler);
                outputXmlDoc.Validate(eventHandler);
                Debug.WriteLine("*** Validation is complete.");
            }
            catch (Exception xcptn)
            {
                Debug.WriteLine("Validate exception: " + xcptn.Message);
            }

            return outputXmlDoc;
        }

        /// <summary>
        /// This is a recursive method that processes the XSD
        /// Nested / complex XML types recurse until the layers are complete
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public static List<DefSchemaElement> ParseSchemaElements( ICollection elements )
        {
            List<DefSchemaElement> schemaElementList = new List<DefSchemaElement>();

            foreach (var any in elements)
            {
                if (!(any is XmlSchemaElement))
                    continue;

                XmlSchemaElement element = (XmlSchemaElement)any;

                XmlSchemaSimpleType simpleType = element.ElementSchemaType as XmlSchemaSimpleType;
                XmlSchemaComplexType complexType = element.ElementSchemaType as XmlSchemaComplexType;

                //if this is a simpletype element, build a corresponding DefResponseElement
                if ( simpleType != null )
                {
                    schemaElementList.Add( BuildDefResponseElement(element.Name, element.MinOccurs, simpleType) );
                }

                //if this is a complextype element, build a DefStructureElement and recurse over childern
                else if (complexType != null)
                {
                    DefStructureElement dse = new DefStructureElement(element.Name);
                    XmlSchemaSequence sequence = complexType.ContentTypeParticle as XmlSchemaSequence;
                    dse.children.AddRange( ParseSchemaElements(sequence.Items) );
                    schemaElementList.Add(dse);
                }
            }

            return schemaElementList;
        }

        public abstract class DefSchemaElement
        {
            public readonly string tagName;

            protected DefSchemaElement(string tagName)
            {
                this.tagName = tagName;
            }
        }

        public class DefStructureElement : DefSchemaElement
        {
            public readonly List<DefSchemaElement> children = new List<DefSchemaElement>();

            public DefStructureElement(string tagName) : base(tagName) { }
        };

        public class DefResponseElement : DefSchemaElement
        {
            public decimal minOccurs;
            public XmlSchemaSimpleType xmlType;

            public DefResponseElement(string tagName, decimal minOccurs, XmlSchemaSimpleType type) : base(tagName)
            {
                this.minOccurs = minOccurs;
                this.xmlType = type;
            }
        }


        /// <summary>
        /// Build the XML tag element (transform if necessary, itemVariable, OR default value)
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="minOccurs"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static DefResponseElement BuildDefResponseElement(string tagName, decimal minOccurs, XmlSchemaSimpleType type)
        {
            return new DefResponseElement(tagName, minOccurs, type);

        }


        /// <summary>
        /// Recursive method that builds the XML output structure
        /// </summary>
        /// <param name="appendTo"></param>
        /// <param name="doc"></param>
        /// <param name="schemaElem"></param>
        /// <param name="formResultId"></param>
        /// <param name="formsRepo"></param>
        public static void BuildResponseXml(
            XmlNode appendTo, 
            XmlDocument doc, 
            RamsellExport.DefSchemaElement schemaElem, 
            int formResultId, 
            IFormsRepository formsRepo)
        {
            RamsellExport.DefStructureElement structElem = schemaElem as RamsellExport.DefStructureElement;
            RamsellExport.DefResponseElement rspElem = schemaElem as RamsellExport.DefResponseElement;

            //if this is a structural element...
            if (structElem != null)
            {
                if (structElem.tagName == "Income_Item")
                {
                    //special case for "Income_Item" structure, with multiple occurences
                    AppendIncomeStructs(appendTo, doc, structElem, formResultId, formsRepo );
                }
                else
                {
                    //normal logic for structural elements: insert a single structural node and recurse for each child element
                    XmlElement outputElem = doc.CreateElement(schemaElem.tagName, Utilities.xmlNamespaceURI);
                    foreach (RamsellExport.DefSchemaElement child in structElem.children)
                        BuildResponseXml(outputElem, doc, child, formResultId, formsRepo);

                    if (appendTo == null)
                        doc.AppendChild(outputElem);
                    else
                        appendTo.AppendChild(outputElem);
                }
            }

            //if this is a response element...
            else if (rspElem != null)
            {
                TransformAndAppendResponseNodesToXML(appendTo, doc, rspElem, formResultId, formsRepo);
            }
            else
            {
                throw new Exception("Unrecognized SchemaElement subclass \"" + schemaElem.GetType().Name + "\"");
            }
        }
        
        public static void AppendIncomeStructs(
            XmlNode appendTo, 
            XmlDocument doc, 
            RamsellExport.DefStructureElement incomeStructElem, 
            int formResultId, 
            IFormsRepository formsRepo)
        {

            int totalMonthlyAmtNum = 0;

            //iterate though each of the subsections "ADAP_F3_[A-D]"
            foreach (string subsectionLetter in new string[] { "A", "B", "C", "D" })
            {
                string subsectionName = "ADAP_F3_" + subsectionLetter;

                def_ResponseVariables incomeEarnerNameRV = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, subsectionName + "_Recipient");

                def_ResponseVariables monthlyAmtRV = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, subsectionName + "_IncomeAmt");
                string monthlyAmtVal = null;
                if (monthlyAmtRV != null && !String.IsNullOrWhiteSpace(monthlyAmtRV.rspValue))
                    monthlyAmtVal = monthlyAmtRV.rspValue;

                int monthlyAmtNum = 0;
                Int32.TryParse(monthlyAmtVal, out monthlyAmtNum);
                totalMonthlyAmtNum += monthlyAmtNum;

                //ADAP_F3_[A-D]_IncomeTypeDrop options

                //0 = Employment
                //1 = Unemployment benefits
                //2 = Short-/Long-term disability
                //3 = SSI (supplemental Security Income)
                //4 = Worker's compensation
                //5 = SSDI (Supplemental Security Disability Insurance)
                //6 = AND (Aid to the Needy Disabled)
                //7 = TANF (Temporary Aid to Needy Families)
                //8 = Interest/Investment Income
                //9 = Veterans benefits
                //10 = Retirement/Pension
                //11 = Taxable trust income
                //12 = Alimony paid to you
                //13 = Other (please describe below)

                def_ResponseVariables incomeTypeRV = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, subsectionName + "_IncomeTypeDrop");
                string incomeTypeVal = "";
                if (incomeTypeRV != null && !String.IsNullOrWhiteSpace(incomeTypeRV.rspValue))
                    incomeTypeVal = incomeTypeRV.rspValue;

                //create income_item structure and add children
                XmlElement incomeStruct = doc.CreateElement(incomeStructElem.tagName, Utilities.xmlNamespaceURI);
                foreach (RamsellExport.DefSchemaElement child in incomeStructElem.children)
                {
                    if (child.tagName == "Income_Earner_Name")
                    {
                        if (incomeEarnerNameRV != null && !String.IsNullOrWhiteSpace(incomeEarnerNameRV.rspValue))
                            AppendContentNodeToXML(incomeStruct, doc, child.tagName, incomeEarnerNameRV.rspValue);
                    }

                    else if (child.tagName == "Salary_Wages"            && incomeTypeVal == "0"  && monthlyAmtVal != null)
                    {
                        AppendContentNodeToXML(incomeStruct, doc, child.tagName, monthlyAmtVal);
                    }

                    else if (child.tagName == "Investments_Trust"       && incomeTypeVal == "8" && monthlyAmtVal != null)
                    {
                        AppendContentNodeToXML(incomeStruct, doc, child.tagName, monthlyAmtVal);
                    }

                    else if (child.tagName == "SSDI"                    && incomeTypeVal == "5" && monthlyAmtVal != null)
                    {
                        AppendContentNodeToXML(incomeStruct, doc, child.tagName, monthlyAmtVal);
                    }

                    else if (child.tagName == "SSI"                     && incomeTypeVal == "3" && monthlyAmtVal != null)
                    {
                        AppendContentNodeToXML(incomeStruct, doc, child.tagName, monthlyAmtVal);
                    }

                    else if (child.tagName == "AND"                     && incomeTypeVal == "6" && monthlyAmtVal != null)
                    {
                        AppendContentNodeToXML(incomeStruct, doc, child.tagName, monthlyAmtVal);
                    }

                    else if (child.tagName == "Workers_Comp"            && incomeTypeVal == "4" && monthlyAmtVal != null)
                    {
                        AppendContentNodeToXML(incomeStruct, doc, child.tagName, monthlyAmtVal);
                    }

                    else if (child.tagName == "Child_Support_Alimony"   && incomeTypeVal == "12" && monthlyAmtVal != null)
                    {
                        AppendContentNodeToXML(incomeStruct, doc, child.tagName, monthlyAmtVal);
                    }

                    else if (child.tagName == "Retirement"              && incomeTypeVal == "10" && monthlyAmtVal != null)
                    {
                        AppendContentNodeToXML(incomeStruct, doc, child.tagName, monthlyAmtVal);
                    }

                    else if (child.tagName == "VA_Income"               && incomeTypeVal == "9" && monthlyAmtVal != null)
                    {
                        AppendContentNodeToXML(incomeStruct, doc, child.tagName, monthlyAmtVal);
                    }

                    else if (child.tagName == "Unemployment"            && incomeTypeVal == "1" && monthlyAmtVal != null)
                    {
                        AppendContentNodeToXML(incomeStruct, doc, child.tagName, monthlyAmtVal);
                    }

                    else if (child.tagName == "Other"                   && incomeTypeVal == "13" && monthlyAmtVal != null)
                    {
                        AppendContentNodeToXML(incomeStruct, doc, child.tagName, monthlyAmtVal);
                    }

                    else if (child.tagName == "Other_Description"       && incomeTypeVal == "13" )
                    {
                        def_ResponseVariables descRV = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, subsectionName + "_IncomeTypeOther");
                        if (descRV != null && !String.IsNullOrWhiteSpace(descRV.rspValue))
                            AppendContentNodeToXML(incomeStruct, doc, child.tagName, descRV.rspValue);
                    }

                    else
                    {
                        Debug.WriteLine("* * * ramsellExport.AppendIncomeStructs() - found unrecognized Income_Item child tag \"" + child.tagName + "\", skipping...");
                    }
                }


                //if the income_item node contains any info, add it to xml
                if (incomeStruct.ChildNodes.Count > 0)
                {
                    if (appendTo == null)
                        doc.AppendChild(incomeStruct);
                    else
                        appendTo.AppendChild(incomeStruct);
                }
            }

            AppendContentNodeToXML(appendTo, doc, "Total_Income", (totalMonthlyAmtNum * 12).ToString() );
        }

        public static void TransformAndAppendResponseNodesToXML(
            XmlNode appendTo, XmlDocument doc, DefResponseElement rspElement, int formResultId, IFormsRepository formsRepo)
        {
            string elementContent = null;

            //super-special case for Enrollment_Type, which is based on the formStatus, rather than responses
            if (rspElement.tagName == "Enrollment_Type")
            {
                #region check the formResult.formStatus field to determin enrollment type

                def_FormResults fr = formsRepo.GetFormResultById(formResultId);
                def_StatusMaster statusMaster = formsRepo.GetStatusMasterByFormId(fr.formId);
                def_StatusDetail statusDetail = formsRepo.GetStatusDetailBySortOrder(statusMaster.statusMasterId, fr.formStatus);
                switch (statusDetail.identifier)
                {
                    case "NEEDS_INFORMATION":
                        elementContent = "2";
                        break;
                    case "APPROVED":
                        elementContent = "2";
                        break;
                    default:
                        elementContent = "1";
                        break;
                }

                #endregion
            } 
            
            if (rspElement.tagName == "Email")
            {
                #region check uas tables for email
                using (UASEntities context = Data.Concrete.DataContext.getUasDbContext())
                {
                    def_FormResults fr = formsRepo.GetFormResultById(formResultId);
                    var data = from ue in context.uas_UserEmail
                                where ue.UserID == fr.subject
                                    && ue.EmailAddress != null
                                    && ue.MayContact
                                orderby ue.SortOrder
                                select ue.EmailAddress;
                    elementContent = data.FirstOrDefault();
                }
                #endregion
            }

            if (rspElement.tagName == "Mailing_Address" || rspElement.tagName == "Mailing_City"
                || rspElement.tagName == "Mailing_State" || rspElement.tagName == "Mailing_Zip")
            {
                def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "ADAP_C2_SameAsMailing");
                if (rv.rspInt != null && rv.rspInt == 1)
                {
                    return;
                }
            }

            //assign a special-case transformation value if applicable
            if( elementContent == null )
                elementContent = RamsellTransformations.GetExportValueForRamsellTag(rspElement.tagName, formResultId, formsRepo);

            //if elementContent has been assigned a non-null value, 
            //it must have been assigned to handle a one-off special case (above), 
            //so append one node with elementContent and terminate this function
            if (elementContent != null)
            {
                AppendContentNodeToXML(appendTo, doc, rspElement.tagName, elementContent);
                return;
            }

            #region normal case: append an xml node for each associated itemvariable identifier in ramsellIdentifierMap

            List<string> ivIdentList = GetItemVariableIdentifiersForRamsellTagName(rspElement.tagName);
            foreach (string ivIdent in ivIdentList)
            {
                def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, ivIdent);
                if ( (rv != null) && !String.IsNullOrWhiteSpace(rv.rspValue) )
                {
                    elementContent = GetFormattedResponse(rv, rspElement.xmlType, formsRepo);
                }
                else
                {
                    GetDefaultValue(rspElement.xmlType);  // to pass validation
                }

                //if there are multiple itemVariables, assume this is one out of a set of checkboxes
                //in which case zeroes represent unchecked checkboxes which should be ignored
                if ( (elementContent == "0") && (ivIdentList.Count > 1) )
                    continue;

                // if no output and the tag is optional, don't write it out
                //   *** For some reason Ramsell system doesn't seem process empty tags or recognize as valid
                //   *** Even though they pass XML / XSD validation
                if ( String.IsNullOrWhiteSpace(elementContent) && rspElement.minOccurs.Equals(0.0m) )
                    continue;

                AppendContentNodeToXML(appendTo, doc, rspElement.tagName, elementContent);
            }

            #endregion
        }

        private static void AppendContentNodeToXML(XmlNode appendTo, XmlDocument doc, string tagName, string content )
        {
            XmlElement outputElem = doc.CreateElement(tagName, Utilities.xmlNamespaceURI);
            outputElem.InnerText = content;
            appendTo.AppendChild(outputElem);
        }

        private static string GetFormattedResponse(def_ResponseVariables rv, XmlSchemaSimpleType type, IFormsRepository formsRepo)
        {
            string response = rv.rspValue;

            //for boolean items (typically representing checked checkboxes), check if there is a specific output value in lookup tables
            //negative responses to boolean items (rspValue "0") can be ignored in this step because they will be excluded from the output anyways

            if (rv.rspValue == "1") //this if-statement reduces the number of DB reads, doesn't effect output
            {
                def_ItemVariables iv = formsRepo.GetItemVariableById(rv.itemVariableId);
                if( iv.baseTypeId == 1 )
                {
                    def_LookupMaster lm = formsRepo.GetLookupMastersByLookupCode( iv.identifier );
                    if (lm != null)
                    {
                        List<def_LookupDetail> ld = formsRepo.GetLookupDetailsByLookupMasterEnterprise(lm.lookupMasterId, SessionHelper.LoginStatus.EnterpriseID);
                        if (ld.Count > 0)
                        {
                            response = ld[0].dataValue;
                        }
                    }
                }
            }

            try
            {
                return Utilities.FormatDataByXmlDataType(response, type);
            }
            catch (Exception excptn)
            {
                Debug.WriteLine("GetFormattedResponse Message: " + excptn.Message);

                return GetDefaultValue(type);  // to pass validation
            }
        }
        
        /// <summary>
        /// Get the default value - currently based on XML / XSD data type.
        /// *** Should probably use the DEF ItemVariable DefaultValue ??  Need to research that option.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string GetDefaultValue(XmlSchemaSimpleType type)
        {
            if (type == null)
                return string.Empty;

            return Utilities.GetDefaultValue(type);
        }


        public static List<string> GetItemVariableIdentifiersForRamsellTagName(string ramsellIdentifier)
        {

            List<string> result = new List<string>();
            int nRows = ramsellIdentifierMap.Length / 2;
            for (int row = 0; row < nRows; row++)
            {
                if (ramsellIdentifierMap[row, 0].ToLower().Equals(ramsellIdentifier.ToLower()))
                    result.Add(ramsellIdentifierMap[row, 1]);
            }

            return result;
        }

        public static readonly string[,] ramsellIdentifierMap = 
        {
            //{ [ramsell tagname],      [itemvariable identifier] },

            {"Last_Name"                ,"ADAP_D1_LastName"         },
            {"First_Name"               ,"ADAP_D1_FirstName"        },
            {"MI"                       ,"ADAP_D1_MiddleIntl"       },
            {"DOB"                      ,"ADAP_D2_DOB"              },
            {"Language"                 ,"ADAP_D7_LangDrop"         },

            {"Hispanic_Subgroup"        ,"ADAP_D4_Mexican"          },
            {"Hispanic_Subgroup"        ,"ADAP_D4_Puerto"           },
            {"Hispanic_Subgroup"        ,"ADAP_D4_Cuban"            },
            {"Hispanic_Subgroup"        ,"ADAP_D4_Other"            },

            {"NHPI_Subgroup"            ,"ADAP_D6_Native"           },
            {"NHPI_Subgroup"            ,"ADAP_D6_Guam"             },
            {"NHPI_Subgroup"            ,"ADAP_D6_Samoan"           },
            {"NHPI_Subgroup"            ,"ADAP_D6_Other"            },

            { "Race"                    , "ADAP_D3_White"           },
            { "Race"                    , "ADAP_D3_Black"           },
            { "Race"                    , "ADAP_D3_Asian"           },
            { "Race"                    , "ADAP_D3_Native"          },
            { "Race"                    , "ADAP_D3_Indian"          },

            {"Asian_Subgroup"           ,"ADAP_D5_Indian"           },
            {"Asian_Subgroup"           ,"ADAP_D5_Chinese"          },
            {"Asian_Subgroup"           ,"ADAP_D5_Filipino"         },
            {"Asian_Subgroup"           ,"ADAP_D5_Japanese"         },
            {"Asian_Subgroup"           ,"ADAP_D5_Korean"           },
            {"Asian_Subgroup"           ,"ADAP_D5_Vietnamese"       },
            {"Asian_Subgroup"           ,"ADAP_D5_Other"            },

            {"Sex_at_Birth"             ,"ADAP_D8_BirthGenderDrop"  },

            {"Contact"                  ,"ADAP_C1_MayContactYN"},
            {"Address"                  ,"ADAP_C1_Address"          },
            {"City"                     ,"ADAP_C1_City"             },
            {"State"                    ,"ADAP_C1_State"            },
            {"Zip"                      ,"ADAP_C1_Zip"              },
            
            {"Receives_Mail_At_Residential_Address","ADAP_C2_SameAsMailing"},
            {"Mailing_Address"          ,"ADAP_C2_Address"          },
            {"Mailing_City"             ,"ADAP_C2_City"             },
            {"Mailing_State"            ,"ADAP_C2_State"            },
            {"Mailing_Zip"              ,"ADAP_C2_Zip"              },
            
            {"Aware_of_HIV_Status"      ,"ADAP_C4_KnowHivYN"        },
            {"Marital_Status"           ,"ADAP_H2_RelnDrop"         },
            {"SSN"                      ,"ADAP_D10_SSN"             },
            {"Number_Children"          ,"ADAP_H4_ChildrenIn"       },
            {"Hep_C_Test_Status"        ,"ADAP_M3_ToldHepC"         },
            {"Medicare_Part_A"          ,"ADAP_I3_PartAYN"          },
            {"Medicare_Part_B"          ,"ADAP_I3_PartBYN"          },

            {"Employer_Plan"            ,"ADAP_F1_EmployerInsOpt"   },
            { "County_of_Residence"     ,"ADAP_C1_County"           },

            { "Household_Members_No_Adults", "ADAP_H4_ChildrenIn" },
        };

        static void ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            switch (e.Severity)
            {
                case XmlSeverityType.Error:
                    Debug.WriteLine("Validation Error: {0}", e.Message);
                    break;

                case XmlSeverityType.Warning:
                    Debug.WriteLine("Validation Warning {0}", e.Message);
                    break;
            }

        }
    }
}