
using System;
using System.Xml;
using System.Xml.Schema;

namespace AJBoggs.Adap.Services.Xml
{
    public static class Utilities
    {
        public static readonly string xmlNamespaceURI = "http://www.w3.org/2001/XMLSchema";

        public static readonly XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\r\n",
            NewLineHandling = NewLineHandling.Replace
        };


        public static XmlSchemaSet GetXsdSchemaSet(string xsdFilePath)
        {
            // load single schema from xsd into a schemaset
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.Add(Utilities.xmlNamespaceURI, xsdFilePath);
            schemaSet.Compile();

            return schemaSet;
        }


        public static bool ContainsNamespace(XmlNode node)
        {
            if (node.NamespaceURI.Equals(xmlNamespaceURI))
                return true;

            foreach (XmlNode child in node.ChildNodes)
            {
                if (ContainsNamespace(child))
                    return true;
            }

            return false;
        }



        public static string GetDefaultValue( XmlSchemaSimpleType type )
        {
            switch (type.Datatype.TypeCode)
            {
                case XmlTypeCode.Date:
                    return DateTime.Now.ToString("yyyy-MM-dd");

                case XmlTypeCode.DateTime:
                    return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");

                case XmlTypeCode.String:
                    return string.Empty;

                case XmlTypeCode.Integer:
                case XmlTypeCode.UnsignedByte:
                case XmlTypeCode.Decimal:
                    return "0";

                case XmlTypeCode.Boolean:
                    return false.ToString().ToLower();

                default:
                    throw new Exception("unrecognized typecode \"" + type.Datatype.TypeCode + "\"");
            }
        }

        public static string FormatDataByXmlDataType(string rspVal, XmlSchemaSimpleType type)
        {
            switch (type.Datatype.TypeCode)
            {
                case XmlTypeCode.String:

                    //for strings, apply maxLength restriction if applicable
                    XmlSchemaSimpleTypeRestriction restriction = type.Content as XmlSchemaSimpleTypeRestriction;
                    if (restriction != null)
                    {
                        foreach (XmlSchemaObject facet in restriction.Facets)
                        {
                            if (facet is XmlSchemaMaxLengthFacet)
                            {
                                int len = Convert.ToInt16(((XmlSchemaMaxLengthFacet)facet).Value);
                                if (rspVal.Length > len)
                                    rspVal = rspVal.Substring(0, len);
                            }
                        }
                    }
                    return rspVal;

                case XmlTypeCode.Date:
                    return Convert.ToDateTime(rspVal).ToString("yyyy-MM-dd");

                case XmlTypeCode.Integer:
                    return Convert.ToInt64(rspVal).ToString();

                case XmlTypeCode.UnsignedByte:
                    return Convert.ToByte(rspVal).ToString();

                case XmlTypeCode.Boolean:
                    return Convert.ToBoolean(rspVal).ToString().ToLower();

                case XmlTypeCode.Decimal:
                    return Convert.ToDouble(rspVal).ToString();

                case XmlTypeCode.DateTime:
                    return Convert.ToDateTime(rspVal).ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");

                default:
                    throw new Exception("unrecognized typecode \"" + type.Datatype.TypeCode + "\"");
            }
        }


    }
}