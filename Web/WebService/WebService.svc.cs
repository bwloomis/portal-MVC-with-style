using AJBoggs.Def.Services;
using AJBoggs.Sis.Domain;
using AJBoggs.Sis.Reports;
using Assmnts.Business;
using Assmnts.Controllers;
using Assmnts.Infrastructure;
using Data.Abstract;
using Data.Concrete;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Schema;
using UAS.Business;
using UAS.DataDTO;
using WebService;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Assmnts.UasServiceRef;
using PdfFileWriter;


namespace Assmnts
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "WebService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select WebService.svc or WebService.svc.cs at the Solution Explorer and start debugging.
    public class WebService : IWebService
    {
        private IFormsRepository mFormsRepository;

        public WebService(IFormsRepository formsRepository) {
            mFormsRepository = formsRepository;
        }

        /// <summary>
        /// This function will get the data for the assessment, by formResult id
        /// </summary>
        /// <param name="userID">String value containing the user name that has access to the SIS Web Service</param>
        /// <param name="passwd">String value containing the encrypted password of the user</param>
        /// <param name="formResultID">formResultid of the assessment we want to return the data</param>
        /// <param name="data">return data in xml format</param>
        /// <returns>SIS web service return code</returns>

        public WSConstants.RETURN_CODE GetDataFormResultIDs(string userID, string passwd, string formResultIDsXML, ref List<KeyValuePair<string, string>> errors, ref string data)
        {
            LoginStatus loginStatus = null;

            List<string> formResultIds = null;

            try
            {
                formResultIds = ParseFormResultIdsXML(formResultIDsXML);
            }
            catch (Exception ex)
            {
                // Something went wrong in the XML processing                                        
                return WSConstants.RETURN_CODE.RC_MalformedXml;
            }

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {
                try
                {

                    // Write XML of the form results
                    StringBuilder stringBuilder = new StringBuilder();

                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "\t";
                    settings.DoNotEscapeUriAttributes = true;
                    settings.NewLineHandling = NewLineHandling.Entitize;

                    using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings))
                    {
                        xmlWriter.WriteStartElement("forms");
                        

                        int countValid = 0;
                        // User status info successfully returned.
                        if (loginStatus != null)
                        {
                            SessionHelper.LoginStatus = loginStatus;

                            foreach (string formResultId in formResultIds)
                            {
                                if (WriteXMLFormResult(xmlWriter, loginStatus, mFormsRepository, formResultId, ref errors) == true)
                                {
                                    countValid++;
                                    WSBusiness.InsertAccessLogRecordWebservice(loginStatus, mFormsRepository, Int32.Parse(formResultId), (int)AccessLogging.accessLogFunctions.EXPORT, "Access assessment data via web service.");
                                }
                            }

                            xmlWriter.WriteEndElement();
                            xmlWriter.Flush();

                            data = stringBuilder.ToString();

                            if (countValid > 0)
                            {
                                return WSConstants.RETURN_CODE.RC_Success;
                            }
                            else
                            {
                                return WSConstants.RETURN_CODE.RC_GeneralError;
                            }

                        }
                        else
                        {
                            // Unable to get LoginStatus
                            return WSConstants.RETURN_CODE.RC_InvalidUser;
                        }

                    }
                }
                catch (Exception ex)
                {
                    // *** Has this been tested ? Does it work?
                    // Does it actually return a code ??  I thought execution stopped / exited on a 'throw' (unless there was a catch in the same method).
                    throw ex;
                    return WSConstants.RETURN_CODE.RC_GeneralError;
                }

            }
            else
            {
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }

        private bool WriteXMLFormResult(XmlWriter xmlWriter, LoginStatus loginStatus, IFormsRepository formsRepo, string formResultIdString, ref List<KeyValuePair<string, string>> errors)
        {
            def_FormResults formResult = null;
            int formResultId;
            if (Int32.TryParse(formResultIdString, out formResultId))
            {
                formResult = formsRepo.GetFormResultById(formResultId);
            }
            // Form result exists
            if (formResult != null)
            {
                // User has access to form result
                if (WSBusiness.hasAccess(loginStatus, formResult))
                {
                    try
                    {
                        xmlWriter.WriteStartElement("form");
                        def_Forms form = formsRepo.GetFormById(formResult.formId);
                        form = formsRepo.GetFormById(formResult.formId);
                        xmlWriter.WriteAttributeString("identifier", form.identifier);
                        xmlWriter.WriteAttributeString("title", form.title);

                        xmlWriter.WriteStartElement("Assessment");

                        List<ValuePair> formResultValues = CommonExport.GetFormResultValues(formResult, form);
                        List<ValuePair> rspValues = CommonExport.GetDataByFormResultId(formResultId);

                        foreach (ValuePair valuePair in formResultValues)
                        {
                            xmlWriter.WriteAttributeString(valuePair.identifier, valuePair.rspValue);
                        }

                        List<def_Parts> parts = formsRepo.GetFormParts(form);

                        foreach (def_Parts part in parts)
                        {
                            xmlWriter.WriteStartElement("Part");
                            xmlWriter.WriteAttributeString("identifier", part.identifier);

                            List<def_Sections> sections = formsRepo.GetSectionsInPartById(part.partId);

                            foreach (def_Sections section in sections)
                            {
                                xmlWriter.WriteStartElement("Section");
                                xmlWriter.WriteAttributeString("identifier", section.identifier);

                                // Recursively gets all the iv identifiers for the section
                                List<string> ivIdsForSection = WSBusiness.GetItemVariableIdentifiersBySectionIncludingSubSections(section.sectionId);

                                foreach (string identifier in ivIdsForSection)
                                {
                                    ValuePair vp = rspValues.Where(vpt => vpt.identifier == identifier).FirstOrDefault();
                                    if (vp != null)
                                        xmlWriter.WriteElementString(vp.identifier, vp.rspValue);
                                }

                                xmlWriter.WriteEndElement();
                            }

                            xmlWriter.WriteEndElement();
                        }



                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndElement();
                    }
                    catch (Exception ex)
                    {
                        HandleError(formResultId.ToString(), "Error writing XML.", errors);
                        return false;
                    }
                }
                else
                {
                    HandleError(formResultId.ToString(), "User does not have access to form result.", errors);
                    return false;
                }
            }
            else
            {
                HandleError(formResultIdString, "Cannot find form result with this ID.", errors);
                return false;
            }

            return true;
        }

        private List<string> ParseFormResultIdsXML(string formResultIDsXML)
        {
            List<string> formResultIds = new List<string>();
            using (XmlReader xmlReader = XmlReader.Create(new StringReader(formResultIDsXML)))
            {
                // Loops through elements of XML
                while (xmlReader.Read())
                {
                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "formResultId")
                    {
                        // Element contains data and is relevant to processing.
                        string identifier = xmlReader.Name;
                        try
                        {
                            xmlReader.Read();
                            if ((xmlReader.NodeType == XmlNodeType.Text) && (xmlReader.HasValue))
                            {

                                string formResultId = xmlReader.Value;
                                formResultIds.Add(formResultId);

                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Invalid input XML for GetDataFormResultIds.");
                            throw ex;
                        }
                    }
                }
            }

            return formResultIds;
        }

        /// <summary>
        /// This function will save the SIS assessment
        /// </summary>
        ///<param name="userID">String value containing the user name that has access to the SIS Web Service</param>
        /// <param name="passwd">String value containing the encrypted password of the user</param>
        /// <param name="data">xml formatted data to save</param>
        /// <param name="errors">returned list of KeyValues (formResultId, error) indicating if errors occurred for specific form results</param>
        /// <param name="keys">returned list of KeyValues of (formResultId, tracking number) for saved assessments</param>
        /// <returns>SIS web service return code</returns>
        public WSConstants.RETURN_CODE SaveDataGetKeys(string userID, string passwd, string data, ref List<KeyValuePair<string, string>> errors, ref List<KeyValuePair<int, string>> keys)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {

                

                // User status info successfully returned.
                if (loginStatus != null)
                {
                    SessionHelper.LoginStatus = loginStatus;

                    // Process the XML file
                    WSConstants.RETURN_CODE returnCode = ProcessXML(loginStatus, data, errors, keys);

                    foreach (KeyValuePair<int, string> pair in keys)
                    {
                        WSBusiness.InsertAccessLogRecordWebservice(loginStatus, mFormsRepository, pair.Key, (int)AccessLogging.accessLogFunctions.EDIT, "Form result updated via webservice.");
                    }

                    return returnCode;
                }
                else
                {
                    // Unable to get LoginStatus
                    return WSConstants.RETURN_CODE.RC_InvalidUser;
                }
            }
            else
            {
                // User did not authenticate
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }

        /// <summary>
        /// XML reading and saving logic for SaveDataGetKeys
        /// </summary>
        /// <param name="formsRepo">Forms repository</param>
        /// <param name="loginStatus">Login status</param>
        /// <param name="data">XML data string</param>
        /// <param name="errors">List of Key Value (string, string) errors</string></param>
        /// <param name="keys">List of Key Value (int, string) keys (formResultId, trackingNumber)</param>
        /// <returns>SIS web service return code</returns>
        private WSConstants.RETURN_CODE ProcessXML(LoginStatus loginStatus, string data, List<KeyValuePair<string, string>> errors, List<KeyValuePair<int, string>> keys)
        {
            def_FormResults formResult = null;

            bool newFormResult = false;
            bool jumpToNextAssessment = false;


            using (XmlReader xmlReader = XmlReader.Create(new StringReader(data)))
            {
                try
                {
                    // Loops through elements of XML
                    while (xmlReader.Read())
                    {
                        // In the event of an error in a form result processing, this will advance the reader to the next assessment
                        if (jumpToNextAssessment == true)
                        {
                            while (true)
                            {
                                if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "Assessment")
                                {
                                    jumpToNextAssessment = false;
                                    xmlReader.Read();
                                    break;
                                }
                                xmlReader.Read();
                            }
                        }

                        // Element is a start element, such as <forms>
                        if (xmlReader.IsStartElement())
                        {

                            // Element is the Assessment element
                            if (xmlReader.Name == "Assessment")
                            {

                                // Try to retrieve the formResultId from the assessment element attributes
                                string formResultId = xmlReader["formResultId"];

                                // No form result ID provided; create new form result.
                                if (String.IsNullOrEmpty(formResultId) || formResultId.Equals("0"))
                                {
                                    try
                                    {

                                        formResult = new def_FormResults();

                                        Exception exception = new Exception();

                                        // Attempt to fill the data for the new form result
                                        FillFormResultData(mFormsRepository, formResult, xmlReader, exception, loginStatus, true);

                                        newFormResult = true;

                                    }
                                    catch (Exception ex)
                                    {
                                        // In the event of an error, get the error message if possible
                                        string errorMsg = ex.Data["message"].ToString();

                                        if (String.IsNullOrEmpty(errorMsg))
                                        {
                                            errorMsg = "Unspecified error.";
                                        }

                                        // Add the error to the errors list
                                        HandleError(formResultId, errorMsg, errors);

                                        // Move on to the next assessment in XML input
                                        jumpToNextAssessment = true;
                                        continue;
                                    }
                                }
                                else // Form result ID is provided in assessments element
                                {

                                    int formResultIdResult;
                                    bool validInteger = false;

                                    // Make sure the form result ID provided is an integer and if so parse it
                                    validInteger = Int32.TryParse(formResultId, out formResultIdResult);

                                    if (validInteger)
                                    {
                                        // Valid integer provided. Attempt to retrieve form result
                                        formResult = mFormsRepository.GetFormResultById(formResultIdResult);
                                    }
                                    else
                                    {
                                        // Valid integer not provided for form result. Add to errors and advance to next assessment in XML input
                                        HandleError(formResultId.ToString(), "Form result ID is not a valid integer.", errors);
                                        jumpToNextAssessment = true;
                                        continue;
                                    }


                                    // Form result could not be retrieved
                                    if (formResult == null)
                                    {
                                        // Add to errors and advance to next assessment in XML input
                                        HandleError(formResultId.ToString(), "No form result found with this ID.", errors);
                                        jumpToNextAssessment = true;
                                        continue;
                                    }

                                    // User can't access this form result
                                    if (!WSBusiness.hasAccess(loginStatus, formResult))
                                    {
                                        // Add to errors and advance to next assessment in XML input
                                        HandleError(formResultId.ToString(), "User does not have access to this record.", errors);
                                        jumpToNextAssessment = true;
                                        continue;
                                    }

                                    try
                                    {
                                        Exception exception = new Exception();

                                        // Attempt to fill in form result data
                                        FillFormResultData(mFormsRepository, formResult, xmlReader, exception, loginStatus, false);
                                    }
                                    catch (Exception ex)
                                    {
                                        // In the event of an error, get the error message if possible
                                        string errorMsg = ex.Data["message"].ToString();

                                        if (String.IsNullOrEmpty(errorMsg))
                                        {
                                            errorMsg = "Unspecified error.";
                                        }

                                        // Add the error to the errors list
                                        HandleError(formResultId, errorMsg, errors);

                                        // Move on to the next assessment in the XML input
                                        jumpToNextAssessment = true;
                                        continue;
                                    }

                                }
                            }
                            else if (xmlReader.IsEmptyElement || xmlReader.Name == "Section" || xmlReader.Name == "Part" || xmlReader.Name == "form" || formResult == null)
                            {
                                // Skip any elements that are irrelevant to processing. 
                                // In the event the form result becomes null somehow skip rest of loop.
                                continue;
                            }
                            else if (xmlReader.NodeType == XmlNodeType.Element)
                            {
                                // Element contains data and is relevant to processing.
                                string identifier = xmlReader.Name;
                                try
                                {

                                    xmlReader.Read();
                                    if ((xmlReader.NodeType == XmlNodeType.Text) && (xmlReader.HasValue))
                                    {

                                        // Find the item variable
                                        def_ItemVariables itemVariable = mFormsRepository.GetItemVariableByIdentifier(identifier);

                                        // Look for an item result
                                        def_ItemResults itemResult = formResult.def_ItemResults.Where(ir => ir.itemId == itemVariable.itemId).Select(ir => ir).FirstOrDefault();

                                        def_ResponseVariables responseVariable = null;

                                        // Need to create new item result for this item
                                        if (itemResult == null)
                                        {
                                            itemResult = new def_ItemResults();

                                            itemResult.itemId = itemVariable.itemId;
                                            itemResult.dateUpdated = DateTime.Now;
                                            itemResult.sessionStatus = 0;

                                            formResult.def_ItemResults.Add(itemResult);
                                        }
                                        else
                                        {
                                            // If item result already exists, see if a corresponding response variable exists for the item variable
                                            responseVariable = mFormsRepository.GetResponseVariablesByItemResultItemVariable(itemResult.itemResultId, itemVariable.itemVariableId);

                                        }

                                        // Create a new response variable if one doesn't already exist
                                        if (responseVariable == null)
                                        {
                                            responseVariable = new def_ResponseVariables();
                                        }

                                        responseVariable.itemVariableId = itemVariable.itemVariableId;

                                        // ensure the interviewer assignments are consistant within the XML. formResult.assigned is assumed to be the correct value unless it's null.
                                        // 6/24 BR Decoupled interviewers with assigned.
                                        //if (identifier.Equals("sis_int_id"))
                                        //{
                                        //    //if (formResult.assigned != null && !xmlReader.Value.Equals(formResult.assigned.ToString()))
                                        //    //{
                                        //    //    responseVariable.rspValue = formResult.assigned.ToString();
                                        //    //}
                                        //    //else
                                        //    //{
                                        //        responseVariable.rspValue = xmlReader.Value;
                                        //        //int result;
                                        //        //bool success = Int32.TryParse(xmlReader.Value, out result);
                                        //        //if (success) {
                                        //        //    formResult.assigned = result;
                                        //        //}
                                        //    //}
                                        //} 
                                        //else if (identifier.Equals("Interviewer_login_id"))
                                        //{
                                        //    if (!String.IsNullOrEmpty(xmlReader.Value))
                                        //    {
                                        //        uas_User u = WSBusiness.GetUserByUserName(xmlReader.Value);
                                        //        if (formResult.assigned != null && u.UserID != formResult.assigned)
                                        //        {
                                        //            uas_User assignedUser = WSBusiness.GetUserByUserId((int) formResult.assigned);
                                        //            responseVariable.rspValue = assignedUser.UserName;
                                        //        }
                                        //        else
                                        //        {
                                        //            responseVariable.rspValue = xmlReader.Value;
                                        //            formResult.assigned = u.UserID;
                                        //        }
                                        //    }
                                        //}
                                        //else
                                        //{
                                            // when not checking interviewers, just update the rspValue.
                                            responseVariable.rspValue = xmlReader.Value;
                                        //}

                                        mFormsRepository.ConvertValueToNativeType(itemVariable, responseVariable);

                                        // See if item result already has the response variable; If it does not, add it
                                        if (itemResult.def_ResponseVariables.Where(rv => rv.itemVariableId == responseVariable.itemVariableId).Select(rv => rv).Count() == 0)
                                        {
                                            itemResult.def_ResponseVariables.Add(responseVariable);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Something went wrong in the XML processing                                        
                                    return WSConstants.RETURN_CODE.RC_MalformedXml;
                                }
                            }
                        }
                        else if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "Assessment")
                        {
                            // We've reached the end (closing tag) of the assessment
                            try
                            {
                                // Attempt to add or save assessment depending on whether it is new
                                if (newFormResult)
                                {
                                    mFormsRepository.AddFormResult(formResult);
                                    newFormResult = false;
                                }
                                else
                                {
                                    mFormsRepository.SaveFormResultsThrowException(formResult);
                                }


                            }
                            catch (Exception ex)
                            {
                                // Add to errors if form result does not save
                                HandleError(formResult.formResultId.ToString(), "Error saving form result", errors);

                                continue;
                            }


                            // Get tracking number of assessment and add key value pair of (formResultId, trackingNumber) to keys list
                            string trackingNum = null;

                            def_ResponseVariables trackingNumberResponseVariable = mFormsRepository.GetResponseVariablesByFormResultIdentifier(formResult.formResultId, "sis_track_num");

                            if (trackingNumberResponseVariable != null)
                            {
                                trackingNum = mFormsRepository.GetResponseVariablesByFormResultIdentifier(formResult.formResultId, "sis_track_num").rspValue;
                            }

                            KeyValuePair<int, string> newKey = new KeyValuePair<int, string>(formResult.formResultId, trackingNum);
                            keys.Add(newKey);

                            formResult = null;
                        }

                    }
                }
                catch (Exception ex)
                {
                    // Something went wrong in the XML processing                                        
                    return WSConstants.RETURN_CODE.RC_MalformedXml;
                }
            }

            // If there are only errors and no keys (form results successfully processed) return general error
            if (errors.Count() > 0 && keys.Count() == 0)
            {
                return WSConstants.RETURN_CODE.RC_GeneralError;
            }

            // If any form results are successfully saved/added (at least one key is returned) return success
            return WSConstants.RETURN_CODE.RC_Success;
        }

        /// <summary>
        /// Handle error
        /// </summary>
        /// <param name="formResultId">String containing form result Id</param>
        /// <param name="errorMsg">The error message to add</param>
        /// <param name="errors">The list of KeyValue(formResultId, errorMessage) to add errors to</param>
        private void HandleError(string formResultId, string errorMsg, List<KeyValuePair<string, string>> errors)
        {
            KeyValuePair<string, string> error = new KeyValuePair<string, string>(formResultId.ToString(), errorMsg);
            errors.Add(error);
        }

        /// <summary>
        /// Fill def_FormResults table data for a form result for SaveDataGetKeys
        /// </summary>
        /// <param name="formsRepo">Forms repository</param>
        /// <param name="formResult">The form result</param>
        /// <param name="xmlReader">The xml reader</param>
        /// <param name="exception">A possible exception to add to and throw</param>
        /// <param name="loginStatus">The user's login status</param>
        /// <param name="newFormResult">Flag for if the form result is new</param>
        private void FillFormResultData(IFormsRepository formsRepo, def_FormResults formResult, XmlReader xmlReader, Exception exception, LoginStatus loginStatus, bool newFormResult)
        {
            try
            {
                exception.Data.Add("message", "Invalid form identifier");

                // Try to get form from form identifier; if fails, throw exception after exception is caught
                formResult.formId = formsRepo.GetFormByIdentifier(xmlReader["identifier"]).formId;

                // Set form result's enterprise to user's enterprise
                formResult.EnterpriseID = loginStatus.EnterpriseID;

                bool parseSuccess = false;


                // Attempt to set the group ID of the form result.
                int groupId;

                // Make sure it is an integer
                parseSuccess = Int32.TryParse(xmlReader["group"], out groupId);

                if (parseSuccess)
                {
                    // Make sure it is one of the user's authorized groups
                    if (loginStatus.appGroupPermissions[0].authorizedGroups.Contains(groupId))
                    {
                        formResult.GroupID = groupId;
                    }
                    else if (loginStatus.appGroupPermissions[0].authorizedGroups.Contains(0)) // User is enterprise wide admin
                    {
                        // As long as the group is in the enterprise it can be used
                        if (UAS_Business_Functions.isGroupInEnterprise(groupId, loginStatus.EnterpriseID))
                        {
                            formResult.GroupID = groupId;
                        }
                    }
                }
                else if (newFormResult)
                {
                    // If form result is new and no group is specified, set it to the user's first authorized group                    
                    formResult.GroupID = loginStatus.appGroupPermissions[0].authorizedGroups[0];
                }

                // Attempt to set the formStatus of the form result
                Byte status = 0;

                // Make sure it is a byte
                parseSuccess = Byte.TryParse(xmlReader["status"], out status);

                if (parseSuccess)
                {
                    if (formResult.formStatus != status)
                    {
                        formResult.statusChangeDate = DateTime.Now;
                    }
                    formResult.formStatus = status;
                }
                else if (newFormResult)
                {
                    // Set form status to new for new form result if none/invalid one is provided
                    formResult.formStatus = (byte)WSConstants.FR_formStatus.NEW;
                }

                // Attempt to set deleted
                bool deleted = false;
                parseSuccess = Boolean.TryParse(xmlReader["deleted"], out deleted);

                if (parseSuccess)
                {
                    // If successful, set it, otherwise, leave it as it is
                    formResult.deleted = deleted;
                    if (deleted == true)
                    {
                        WSBusiness.InsertAccessLogRecordWebservice(loginStatus, mFormsRepository, formResult.formResultId,
                            (int)AccessLogging.accessLogFunctions.DELETE, "Deletion of assessment via webservice.");
                    }
                }

                // Attempt to set locked
                bool locked = false;
                parseSuccess = Boolean.TryParse(xmlReader["locked"], out locked);
                if (parseSuccess)
                {
                    // If successful, set it, otherwise, leave it as it is
                    formResult.locked = locked;
                }

                // Attempt to set archived
                bool archived = false;
                parseSuccess = Boolean.TryParse(xmlReader["archived"], out archived);
                if (parseSuccess)
                {
                    // If successful, set it, otherwise, leave it as it is
                    formResult.archived = archived;
                    if (archived == true)
                    {
                        WSBusiness.InsertAccessLogRecordWebservice(loginStatus, mFormsRepository, formResult.formResultId,
                           (int)AccessLogging.accessLogFunctions.ARCHIVE, "Archiving of assessment via webservice.");
                    }
                }

                // Attempt to set subject
                int subject = 0;
                parseSuccess = Int32.TryParse(xmlReader["recipientId"], out subject);
                if (parseSuccess)
                {
                    // If successful, set it, otherwise, leave it as it is
                    formResult.subject = subject;
                }

                // Attempt to set interviewer
                int interviewer = 0;
                parseSuccess = Int32.TryParse(xmlReader["interviewer"], out interviewer);
                if (parseSuccess)
                {
                    // Make sure the user to change to is in the enterprise
                    if (UAS_Business_Functions.isUserInEnterprise(interviewer, loginStatus.EnterpriseID))
                    {
                        formResult.interviewer = interviewer;
                    }
                }

                // Attempt to set assigned
                string assignStr = xmlReader["assigned"];
                if (!String.IsNullOrEmpty(assignStr))
                {
                    int assigned = 0;
                    parseSuccess = Int32.TryParse(xmlReader["assigned"], out assigned);
                    if (parseSuccess && assigned != 0)
                    {
                        // If successful, set it, otherwise, leave it as it is
                        formResult.assigned = assigned;
                    }
                }
                
                // If the assigned integer isn't set, check the assigned string.
                string assignIdStr = xmlReader["assignedLoginId"];
                if ((formResult.assigned == null || (formResult.assigned.HasValue && formResult.assigned < 1)) && !String.IsNullOrEmpty(assignIdStr))
                {
                    uas_User u = WSBusiness.GetUserByUserName(assignIdStr);
                    if (u != null)
                    {
                        // If successful, set it, otherwese, leave it as is.
                        formResult.assigned = u.UserID;
                    }
                }

                // Attempt to set review status
                Byte reviewStatus;
                parseSuccess = Byte.TryParse(xmlReader["reviewStatus"], out reviewStatus);
                if (parseSuccess)
                {
                    // If successful, set it
                    formResult.reviewStatus = reviewStatus;
                }
                else if (newFormResult)
                {
                    // If form result is new and no review status is specified, set it to blank
                    formResult.reviewStatus = ReviewStatus.BLANK;
                }

                // Set dateUpdated to current date time
                formResult.dateUpdated = DateTime.Now;
            }
            catch (Exception ex)
            {
                // Throw the exception with any message needed to transfer if there is an error
                throw exception;
            }
        }

        /// <summary>
        ///  This function will return the pdf data for an Assessment Report with the given formReultID
        /// </summary>
        /// <param name="userID">String value containing the user name that has access to the SIS Web Service</param>
        /// <param name="passwd">String value containing the encrypted password of the user</param>
        /// <param name="formResultID">formResultID of the assessment we want to return the data</param>
        /// <param name="pdf">returned PDF report for the assessment</param>
        /// <returns>SIS web service return code</returns>       
        public WSConstants.RETURN_CODE GetPdfReport(string userID, string passwd, int formResultID, ref Byte[] pdf)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {



                def_FormResults formResult = mFormsRepository.GetFormResultById(formResultID);

                // User status info successfully returned.
                if (loginStatus != null)
                {
                    // Form result exists
                    if (formResult != null)
                    {
                        // User has access to form result
                        if (WSBusiness.hasAccess(loginStatus, formResult))
                        {
                            // *** 1/22/2016 OT - fixed - options are selected based on the assessment's enterprise
                            try
                            {
                                SisPdfReportOptions options = AJBoggs.Sis.Reports.SisReportOptions.BuildPdfReportOptions(formResult.EnterpriseID);
                                SisPdfRptsController sprc = new SisPdfRptsController(mFormsRepository);
                                SessionHelper.LoginStatus = loginStatus;
                                FileContentResult fcr = sprc.BottomBuildReport(formResultID, options);
                                pdf = fcr.FileContents;

                                // if successfully get pdf
                                return WSConstants.RETURN_CODE.RC_Success;
                            }
                            catch (Exception e)
                            {
                                // if can't successfully get pdf
                                return WSConstants.RETURN_CODE.RC_GeneralError;
                            }
                        }
                        else
                        {
                            // User doesn't have access to form result
                            return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                        }
                    }
                    else
                    {
                        // Form result not found
                        return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                    }
                }
                else
                {
                    // Unable to get LoginStatus
                    return WSConstants.RETURN_CODE.RC_InvalidUser;
                }
            }
            else
            {
                // User did not authenticate
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }


        /// <summary>
        /// This function will return the formResultIDs of the assessments with the given tracking number
        /// </summary>
        /// <param name="userID">String value containing the user name that has access to the SIS web service</param>
        /// <param name="passwd">String value containing the user's password</param>
        /// <param name="trackingNum">Tracking number to find assessments by</param>
        /// <param name="data">string of xml data containing list of tracking numbers</param>
        /// <returns>SIS web service return code</returns>
        public WSConstants.RETURN_CODE GetFormResultIdsFromTrackingNumber(string userID, string passwd, string trackingNum, ref string data)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {

                

                // Get all form results for this tracking number
                Assessments assmnts = new Assessments(mFormsRepository);

                List<def_FormResults> formResults = assmnts.GetAssessmentsByTrackingNumberFilterByAccess(loginStatus, trackingNum);

                List<int> formResultIds = formResults.Select(fr => fr.formResultId).ToList();
                // User status info successfully returned.
                if (loginStatus != null)
                {
                    // Write XML of the form results
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "\t";
                    settings.NewLineHandling = NewLineHandling.Replace;
                    settings.NewLineChars = "\n";

                    StringBuilder stringBuilder = new StringBuilder();
                    using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings))
                    {
                        xmlWriter.WriteStartElement("FormResultIds");
                        foreach (int formResultId in formResultIds)
                        {
                            xmlWriter.WriteElementString("FormResultId", formResultId.ToString());
                        }
                        xmlWriter.WriteEndElement();
                        xmlWriter.Flush();
                    }

                    data = stringBuilder.ToString();
                    return WSConstants.RETURN_CODE.RC_Success;
                }
                else
                {
                    // Unable to get LoginStatus
                    return WSConstants.RETURN_CODE.RC_InvalidUser;
                }
            }
            else
            {
                // User did not authenticate
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }

        /// <summary>
        /// This function will check to see if the assessment is completed.
        /// </summary>
        /// <param name="userID">String value containing the user name that has access to the SIS Web Service</param>
        /// <param name="passwd">String value containing the password of the user</param>
        /// <param name="formResultID">FormResult id of the assessment we are checking for complete</param>
        /// <param name="complete">returns true if the assessment is complete</param>
        /// <returns>SIS Webservice return code</returns>

        public WSConstants.RETURN_CODE IsCompleteFormResultId(string userID, string passwd, int formResultID, ref bool complete)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {



                def_FormResults formResult = mFormsRepository.GetFormResultById(formResultID);

                // User status info successfully returned.
                if (loginStatus != null)
                {
                    // Form result exists
                    if (formResult != null)
                    {
                        // User has access to form result
                        if (WSBusiness.hasAccess(loginStatus, formResult))
                        {
                            // Check if assessment is completed and set value of complete
                            if ((WSConstants.FR_formStatus)formResult.formStatus == WSConstants.FR_formStatus.COMPLETED)
                            {
                                complete = true;
                            }
                            else
                            {
                                complete = false;
                            }
                            return WSConstants.RETURN_CODE.RC_Success;
                        }
                        else
                        {
                            // User doesn't have access to form result
                            return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                        }
                    }
                    else
                    {
                        // Form result not found
                        return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                    }
                }
                else
                {
                    // Unable to get LoginStatus
                    return WSConstants.RETURN_CODE.RC_InvalidUser;
                }
            }
            else
            {
                // User did not authenticate
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }

        }

        /// <summary>
        /// This function will get the status of the assessment by formResultId
        /// </summary>
        /// <param name="userID">String value containing the user name that has access to the SIS Web Service</param>
        /// <param name="passwd">String value containing the password of the user</param>
        /// <param name="formResultID">formResultId of the assessment we are working with</param>
        /// <param name="status">returned value of the assessment</param>
        /// <returns>Success if we were able to get the status. SIS assessment status is in the status field.</returns>
        public WSConstants.RETURN_CODE GetStatusId(string userID, string passwd, int formResultID, ref WSConstants.FORM_STATUS status)
        {

            LoginStatus loginStatus = null;

            // Check for valid user
            if ((VerifyPassword(userID, passwd, ref loginStatus) == true) && (loginStatus != null))
            {



                def_FormResults formResult = mFormsRepository.GetFormResultById(formResultID);

                // Check that form result exists
                if (formResult != null)
                {
                    // See if user hass access to form result
                    if (WSBusiness.hasAccess(loginStatus, formResult))
                    {
                        switch ((WSConstants.FR_formStatus)formResult.formStatus)
                        {
                            case WSConstants.FR_formStatus.NEW:
                                status = WSConstants.FORM_STATUS.SSNew;
                                break;

                            case WSConstants.FR_formStatus.IN_PROGRESS:
                                status = WSConstants.FORM_STATUS.SSInprogress;
                                break;

                            case WSConstants.FR_formStatus.COMPLETED:
                                // Choose whether it is completed or completed locked
                                status = (formResult.locked == true) ? WSConstants.FORM_STATUS.SSCompletedLocked : WSConstants.FORM_STATUS.SSCompleted;
                                break;

                            case WSConstants.FR_formStatus.ABANDONED:
                                status = WSConstants.FORM_STATUS.SSAbandoned;
                                break;

                            default:
                                status = WSConstants.FORM_STATUS.SSUnknown;
                                break;
                        }

                        // Check for achived and deleted assessments
                        if (formResult.archived == true)
                        {
                            status = WSConstants.FORM_STATUS.SSArchived;
                        }
                        else if (formResult.deleted == true)
                        {
                            status = WSConstants.FORM_STATUS.SSDeleted;
                        }

                        return WSConstants.RETURN_CODE.RC_Success;

                    }
                    else
                    {
                        // User does not have access to form result
                        status = WSConstants.FORM_STATUS.SSInvalid;
                        return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                    }
                }
                else
                {
                    // Form result not found
                    status = WSConstants.FORM_STATUS.SSInvalid;
                    return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                }

            }
            else
            {
                // User is not valid
                status = WSConstants.FORM_STATUS.SSInvalid;
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }

        /// <summary>
        /// This function will change the status of the assessment
        /// </summary>
        /// <param name="userID">String value containing the user name that has access to the SIS Web Service</param>
        /// <param name="passwd">String value containing the password of the user</param>
        /// <param name="formResultID">formResultID of the assessment we are working with</param>
        /// <param name="status">status we want to change the assessment to</param>
        /// <returns>SIS web service return code</returns>        
        public WSConstants.RETURN_CODE ChangeStatusId(string userID, string passwd, int formResultID, WSConstants.FORM_STATUS status)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {



                def_FormResults formResult = mFormsRepository.GetFormResultById(formResultID);

                // User status info successfully returned.
                if (loginStatus != null)
                {
                    // Form result exists
                    if (formResult != null)
                    {
                        // User has access to form result
                        if (WSBusiness.hasAccess(loginStatus, formResult))
                        {
                            int oldStatus = formResult.formStatus;

                            // Change the form result status
                            switch (status)
                            {
                                case WSConstants.FORM_STATUS.SSNew:
                                    formResult.formStatus = (int)WSConstants.FR_formStatus.NEW;
                                    break;

                                case WSConstants.FORM_STATUS.SSInprogress:
                                    formResult.formStatus = (int)WSConstants.FR_formStatus.IN_PROGRESS;
                                    break;

                                case WSConstants.FORM_STATUS.SSCompleted:
                                    formResult.formStatus = (int)WSConstants.FR_formStatus.COMPLETED;
                                    break;

                                case WSConstants.FORM_STATUS.SSCompletedLocked:
                                    formResult.formStatus = (int)WSConstants.FR_formStatus.COMPLETED;
                                    formResult.locked = true;
                                    break;

                                case WSConstants.FORM_STATUS.SSAbandoned:
                                    formResult.formStatus = (int)WSConstants.FR_formStatus.ABANDONED;
                                    break;

                                case WSConstants.FORM_STATUS.SSArchived:
                                    formResult.archived = true;
                                    WSBusiness.InsertAccessLogRecordWebservice(loginStatus, mFormsRepository, formResultID, (int)AccessLogging.accessLogFunctions.ARCHIVE,
                                        "Archiving of assessment via webservice.");
                                    break;

                                case WSConstants.FORM_STATUS.SSDeleted:
                                    formResult.deleted = true;
                                    WSBusiness.InsertAccessLogRecordWebservice(loginStatus, mFormsRepository, formResultID, (int)AccessLogging.accessLogFunctions.DELETE,
                                        "Deletion of assessment via webservice");
                                    break;

                                default:
                                    // Can't change status
                                    return WSConstants.RETURN_CODE.RC_GeneralError;
                                    break;
                            }

                            if (oldStatus != formResult.formStatus)
                            {
                                formResult.statusChangeDate = DateTime.Now;
                            }

                            mFormsRepository.SaveFormResults(formResult);
                            return WSConstants.RETURN_CODE.RC_Success;
                        }
                        else
                        {
                            // User doesn't have access to form result
                            return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                        }
                    }
                    else
                    {
                        // Form result not found
                        return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                    }
                }
                else
                {
                    // Unable to get LoginStatus
                    return WSConstants.RETURN_CODE.RC_InvalidUser;
                }
            }
            else
            {
                // User did not authenticate
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }

        /// <summary>
        /// This function will get the tracking number of the assessment by the formResult ID
        /// </summary>
        /// <param name="userID">String value containing the user name that has access to the SIS web service</param>
        /// <param name="passwd">String value containing the user's password</param>
        /// <param name="formResultID">formResult id of the assessment we want to return the tracking number for</param>
        /// <param name="trackingNum">returned tracking number</param>
        /// <returns>SIS web service return code</returns>
        public WSConstants.RETURN_CODE GetTrackingNumberFormResultId(string userID, string passwd, int formResultID, ref string trackingNum)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {



                def_FormResults formResult = mFormsRepository.GetFormResultById(formResultID);

                // User status info successfully returned.
                if (loginStatus != null)
                {
                    // Form result exists
                    if (formResult != null)
                    {
                        // User has access to form result
                        if (WSBusiness.hasAccess(loginStatus, formResult))
                        {
                            def_ResponseVariables rv = mFormsRepository.GetResponseVariablesByFormResultIdentifier(formResult.formResultId, WSConstants.TRACKING_NUMBER);

                            if (rv != null)
                            {
                                // Return found tracking number
                                trackingNum = rv.rspValue;
                                return WSConstants.RETURN_CODE.RC_Success;
                            }
                            else
                            {
                                // Could not find tracking number
                                return WSConstants.RETURN_CODE.RC_GeneralError;
                            }
                        }
                        else
                        {
                            // User doesn't have access to form result
                            return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                        }
                    }
                    else
                    {
                        // Form result not found
                        return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                    }
                }
                else
                {
                    // Unable to get LoginStatus
                    return WSConstants.RETURN_CODE.RC_InvalidUser;
                }
            }
            else
            {
                // User did not authenticate
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }

        /// <summary>
        /// This function will logically delete assessments by the SIS ID
        /// </summary>
        /// <param name="userID">String value containing the user name that has access to the SIS Web Service</param>
        /// <param name="passwd">String value containing the password</param>
        /// <param name="formResultID">SIS ID of the assessment we want to delete</param>
        /// <returns>SIS web service return code</returns>
        public WSConstants.RETURN_CODE DeleteAssessmentFormResultID(string userID, string passwd, int formResultID)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {



                def_FormResults formResult = mFormsRepository.GetFormResultById(formResultID);

                // User status info successfully returned.
                if (loginStatus != null)
                {
                    // FormResult exists
                    if (formResult != null)
                    {
                        // User has access to form result
                        if (WSBusiness.hasAccess(loginStatus, formResult))
                        {
                            // Logically delete assessment
                            formResult.deleted = true;
                            mFormsRepository.SaveFormResults(formResult);
                            WSBusiness.InsertAccessLogRecordWebservice(loginStatus, mFormsRepository, formResultID, (int)AccessLogging.accessLogFunctions.DELETE,
                                "Deletion of assessment via webservice");
                            return WSConstants.RETURN_CODE.RC_Success;
                        }
                        else
                        {
                            // User doesn't have access to form result
                            return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                        }
                    }
                    else
                    {
                        // Form result not found
                        return WSConstants.RETURN_CODE.RC_InvalidFormResultId;
                    }
                }
                else
                {
                    // Unable to get LoginStatus
                    return WSConstants.RETURN_CODE.RC_InvalidUser;
                }
            }
            else
            {
                // User did not authenticate
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }


        /// <summary>
        /// This function will return success. This is a way for the client app to check to see if the web service is available.
        /// </summary>
        /// <returns>SIS web service return code</returns>
        public WSConstants.RETURN_CODE VerifyHost()
        {
            return WSConstants.RETURN_CODE.RC_Success;
        }

        /// <summary>
        /// This function will return xml data schema for a form
        /// </summary>
        /// <param name="userID">String value containing the user name that has access to the SIS Web Service</param>
        /// <param name="passwd">String value containing the user's password</param>
        /// <param name="formIdentifier">Identifier of form to return schema of</param>
        /// <param name="nested">Flag to determine if schema should include nesting by part, section, etc.</param>
        /// <param name="schema">Returned schema</param>
        /// <returns>SIS web service return code</returns>
        public WSConstants.RETURN_CODE GetFormSchema(string userID, string passwd, string formIdentifier, bool nested, ref string schema)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {

                

                XmlSchema xmlSchema = null;

                // User status info successfully returned.
                if (loginStatus != null)
                {
                    SessionHelper.LoginStatus = loginStatus;

                    def_Forms form = mFormsRepository.GetFormByIdentifier(formIdentifier);

                    // Find form
                    if (form != null)
                    {

                        // Write XML of the form results
                        StringBuilder stringBuilder = new StringBuilder();

                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.Indent = true;
                        settings.IndentChars = "\t";
                        settings.DoNotEscapeUriAttributes = true;
                        settings.NewLineHandling = NewLineHandling.Entitize;

                        try
                        {
                            // Write out XML containing every entry possible
                            if (nested) // Nest by part and section
                            {
                                using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings))
                                {

                                    xmlWriter.WriteStartElement("forms");

                                    xmlWriter.WriteStartElement("form");

                                    xmlWriter.WriteAttributeString("identifier", form.identifier);
                                    xmlWriter.WriteAttributeString("title", form.title);

                                    xmlWriter.WriteStartElement("Assessment");

                                    xmlWriter.WriteAttributeString(FormResultExportTagName.formResultId.ToString(), 1234567.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.recipientId.ToString(), 1234.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.identifier.ToString(), "identifier");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.formId.ToString(), 1.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.group.ToString(), 12.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.enterprise.ToString(), 1.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.assigned.ToString(), 1234.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.statusId.ToString(), 1.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.dateUpdated.ToString(), new DateTime().ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.statusChangeDate.ToString(), new DateTime().ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.deleted.ToString(), new Boolean().ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.locked.ToString(), new Boolean().ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.archived.ToString(), new Boolean().ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.reviewStatus.ToString(), 1.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.lastModifiedByUserId.ToString(), 1234.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.reviewStatusText.ToString(), "text");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.statusText.ToString(), "text");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.groupName.ToString(), "text");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.assignedLoginId.ToString(), "text");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.lastModifiedByLoginId.ToString(), "text");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.enterpriseName.ToString(), "text");

                                    List<def_Parts> parts = mFormsRepository.GetFormParts(form);

                                    foreach (def_Parts part in parts)
                                    {
                                        xmlWriter.WriteStartElement("Part");
                                        xmlWriter.WriteAttributeString("identifier", part.identifier);

                                        List<def_Sections> sections = mFormsRepository.GetSectionsInPartById(part.partId);

                                        foreach (def_Sections section in sections)
                                        {
                                            xmlWriter.WriteStartElement("Section");
                                            xmlWriter.WriteAttributeString("identifier", section.identifier);

                                            List<int> subSections = WSBusiness.GetSubSections(section.sectionId);

                                            List<string> ivIdentifiers = WSBusiness.GetItemVariableIdentifiersBySection(section.sectionId);

                                            foreach (string ivIdentifier in ivIdentifiers)
                                            {
                                                xmlWriter.WriteStartElement(ivIdentifier);
                                                xmlWriter.WriteEndElement();
                                            }

                                            foreach (int subSection in subSections)
                                            {
                                                List<string> subIvIdentifiers = WSBusiness.GetItemVariableIdentifiersBySection(subSection);

                                                foreach (string subIvIdentifier in subIvIdentifiers)
                                                {
                                                    xmlWriter.WriteStartElement(subIvIdentifier);
                                                    xmlWriter.WriteEndElement();
                                                }
                                            }

                                            xmlWriter.WriteEndElement();
                                        }

                                        xmlWriter.WriteEndElement();
                                    }

                                    xmlWriter.WriteEndElement();
                                    xmlWriter.WriteEndElement();
                                    xmlWriter.WriteEndElement();

                                    xmlWriter.Flush();

                                }
                            }
                            else // No nesting, flat structure
                            {
                                using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings))
                                {
                                    xmlWriter.WriteStartElement("forms");

                                    xmlWriter.WriteStartElement("form");

                                    xmlWriter.WriteAttributeString("identifier", form.identifier);
                                    xmlWriter.WriteAttributeString("title", form.title);

                                    xmlWriter.WriteStartElement("Assessment");

                                    xmlWriter.WriteAttributeString(FormResultExportTagName.formResultId.ToString(), 1234567.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.recipientId.ToString(), 1234.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.identifier.ToString(), "identifier");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.formId.ToString(), 1.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.group.ToString(), 12.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.enterprise.ToString(), 1.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.assigned.ToString(), 1234.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.statusId.ToString(), 1.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.dateUpdated.ToString(), new DateTime().ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.statusChangeDate.ToString(), new DateTime().ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.deleted.ToString(), new Boolean().ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.locked.ToString(), new Boolean().ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.archived.ToString(), new Boolean().ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.reviewStatus.ToString(), 1.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.lastModifiedByUserId.ToString(), 1234.ToString());
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.reviewStatusText.ToString(), "text");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.statusText.ToString(), "text");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.groupName.ToString(), "text");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.assignedLoginId.ToString(), "text");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.lastModifiedByLoginId.ToString(), "text");
                                    xmlWriter.WriteAttributeString(FormResultExportTagName.enterpriseName.ToString(), "text");

                                    List<def_Parts> parts = mFormsRepository.GetFormParts(form);

                                    foreach (def_Parts part in parts)
                                    {

                                        List<def_Sections> sections = mFormsRepository.GetSectionsInPartById(part.partId);

                                        foreach (def_Sections section in sections)
                                        {
                                            List<int> subSections = WSBusiness.GetSubSections(section.sectionId);

                                            List<string> ivIdentifiers = WSBusiness.GetItemVariableIdentifiersBySection(section.sectionId);

                                            foreach (string ivIdentifier in ivIdentifiers)
                                            {
                                                xmlWriter.WriteStartElement(ivIdentifier);
                                                xmlWriter.WriteEndElement();
                                            }

                                            foreach (int subSection in subSections)
                                            {
                                                List<string> subIvIdentifiers = WSBusiness.GetItemVariableIdentifiersBySection(subSection);

                                                foreach (string subIvIdentifier in subIvIdentifiers)
                                                {
                                                    xmlWriter.WriteStartElement(subIvIdentifier);
                                                    xmlWriter.WriteEndElement();
                                                }
                                            }

                                        }

                                    }

                                    xmlWriter.WriteEndElement();
                                    xmlWriter.WriteEndElement();
                                    xmlWriter.WriteEndElement();

                                    xmlWriter.Flush();

                                }
                            }

                            // Get schema from created XML

                            string schemaString = stringBuilder.ToString();

                            TextReader textReader = new StringReader(schemaString);
                            XmlReader xmlReader = XmlReader.Create(textReader);

                            XmlSchemaSet schemaSet = null;
                            XmlSchemaInference schemaInference = new XmlSchemaInference();

                            schemaSet = schemaInference.InferSchema(xmlReader);

                            StringBuilder schemaStringBuilder = new StringBuilder();
                            XmlWriter xmlSchemaWriter = XmlWriter.Create(schemaStringBuilder, settings);

                            foreach (XmlSchema s in schemaSet.Schemas())
                            {
                                s.Write(xmlSchemaWriter);
                            }

                            schema = schemaStringBuilder.ToString();
                            return WSConstants.RETURN_CODE.RC_Success;
                        }
                        catch (Exception ex)
                        {
                            // Error creating XML output
                            return WSConstants.RETURN_CODE.RC_GeneralError;
                        }
                    }
                    else
                    {
                        // Invalid form
                        return WSConstants.RETURN_CODE.RC_GeneralError;
                    }
                }
                else
                {
                    // Unable to get LoginStatus
                    return WSConstants.RETURN_CODE.RC_InvalidUser;
                }
            }
            else
            {
                // User did not authenticate
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }


        private void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
                Console.Write("WARNING: ");
            else if (args.Severity == XmlSeverityType.Error)
                Console.Write("ERROR: ");

            Debug.WriteLine(args.Message);
        }


        public WSConstants.RETURN_CODE AuthenticateUser(string loginId, string password, ref int subId, ref int entId)
        {
            LoginStatus loginStatus = null;
            return VerifyPasswordLogic(loginId, password, ref subId, ref entId, ref loginStatus);
        }


        private bool VerifyPassword(string loginId, string password, ref LoginStatus loginStatus)
        {
            int groupId = 0;
            int enterpriseId = 0;

            if (VerifyPasswordLogic(loginId, password, ref groupId, ref enterpriseId, ref loginStatus) == WSConstants.RETURN_CODE.RC_Success)
            {
                return true;
            }
            return false;

        }


        /// <summary>
        /// Checks that a user and password combination is valid and for a user with webservice access.
        /// </summary>
        /// <param name="loginId">User name</param>
        /// <param name="password">Unencrypted password</param>
        /// <param name="groupId"></param>
        /// <param name="enterpriseId"></param>
        /// <returns>Success if exists and has permission; InvalidUser otherwise</returns>
        private WSConstants.RETURN_CODE VerifyPasswordLogic(string loginId, string password, ref int groupId, ref int enterpriseId, ref LoginStatus loginStatus)
        {
            WSConstants.RETURN_CODE returnCode = WSConstants.RETURN_CODE.RC_InvalidUser;

            try
            {
                LoginInfo loginInfo = new LoginInfo();
                loginInfo.RememberMe = false;

                CookieData cookieData = HttpFunctions.GetUserCookie();
                if (cookieData.LoginID != null)
                {
                    loginInfo.LoginID = cookieData.LoginID;
                    loginInfo.RememberMe = true;
                }


                loginInfo.LoginID = loginId;
                loginInfo.Password = password;

                loginStatus = UAS_Business_Functions.VerifyUser(loginInfo);


                string userName = String.Empty;
                if (loginStatus != null)
                {
                    if (hasWebServicePermission(loginStatus))
                    {
                        returnCode = WSConstants.RETURN_CODE.RC_Success;
                        enterpriseId = loginStatus.EnterpriseID;
                        groupId = loginStatus.appGroupPermissions[0].authorizedGroups[0];
                    }
                    else
                    {
                        Debug.WriteLine("User does not have webservice access.");
                    }
                }
                else
                {
                    Debug.WriteLine("Unable to verify password.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to verify password: " + ex.Message);
            }
            return returnCode;
        }

        /// <summary>
        /// Checks that a user has permission to use the webservice
        /// </summary>
        /// <param name="loginStatus">The loginStatus for the user</param>
        /// <returns>true if user has permission, false if user does not</returns>
        private bool hasWebServicePermission(LoginStatus loginStatus)
        {
            if (UAS_Business_Functions.hasPermission(loginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, PermissionConstants.WS_ACCESS, PermissionConstants.INTEGRATION))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="passwd"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public WSConstants.RETURN_CODE GetUpdates(string userID, string passwd, ref string data)
        {
            LoginStatus loginStatus = null;
            var errors = new List<KeyValuePair<string, string>>();
            
            List<string> formResultIds = new List<string>();

            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {
                List<def_WebServiceActivity> unprocessed = mFormsRepository.GetUnprocessedWebServiceActivityRequestsByEntId(loginStatus.EnterpriseID);

                if (unprocessed.Count() > 0)
                {

                    foreach (def_WebServiceActivity wsa in unprocessed)
                    {
                        formResultIds.Add(GetActivityParameter("formResultId", wsa.ActivityParameters));
                        wsa.DateTimeServiced = DateTime.Now;
                    }
                    try
                    {
                        // Write XML of the form results
                        StringBuilder stringBuilder = new StringBuilder();

                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.Indent = true;
                        settings.IndentChars = "\t";
                        settings.DoNotEscapeUriAttributes = true;
                        settings.NewLineHandling = NewLineHandling.Entitize;

                        using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings))
                        {
                            xmlWriter.WriteStartElement("forms");

                            int countValid = 0;
                            // User status info successfully returned.
                            if (loginStatus != null)
                            {
                                SessionHelper.LoginStatus = loginStatus;

                                foreach (string formResultId in formResultIds)
                                {
                                    if (WriteXMLFormResult(xmlWriter, loginStatus, mFormsRepository, formResultId, ref errors) == true)
                                    {
                                        countValid++;
                                    }
                                }

                                xmlWriter.WriteEndElement();
                                xmlWriter.Flush();

                                data = stringBuilder.ToString();

                                if (countValid > 0)
                                {
                                    foreach (def_WebServiceActivity wsa in unprocessed)
                                    {
                                        mFormsRepository.SaveWebServiceActivity(wsa);
                                    }

                                    return WSConstants.RETURN_CODE.RC_Success;
                                }
                                else
                                {
                                    return WSConstants.RETURN_CODE.RC_GeneralError;
                                }

                            }
                            else
                            {
                                // Unable to get LoginStatus
                                return WSConstants.RETURN_CODE.RC_InvalidUser;
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        return WSConstants.RETURN_CODE.RC_GeneralError;
                    }
                }
                else
                {
                    return WSConstants.RETURN_CODE.RC_NoResults;
                }
            }
            else
            {
                // Unable to get LoginStatus
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }


        private string GetActivityParameter(string paramName, string activityParams)
        {
            string returnString = null;
            string[] param = activityParams.Split(';');
            foreach (string s in param)
            {
                string pName = string.Format("{0}=", paramName);
                if (s.IndexOf(pName) >= 0)
                {
                    int i = s.IndexOf("=");
                    returnString = s.Substring(i + 1);
                    break;
                }
            }
            return returnString;
        }

        // User related methods

        public WSConstants.RETURN_CODE GetUserInfo(string userID, string passwd, string getUserID, ref string info, ref string error)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {
                AuthenticationClient webclient = new AuthenticationClient();
                if (webclient.GetUserInfo(userID, getUserID, 1, ref info))
                {

                    return WSConstants.RETURN_CODE.RC_Success;
                }
                else
                {
                    error = "Don't have access to user " + getUserID;
                    return WSConstants.RETURN_CODE.RC_GeneralError;
                }
            }
            else
            {
                // Unable to get LoginStatus
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }

        public WSConstants.RETURN_CODE Create_User(string userID, string passwd, string userData, ref string error)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {
                AuthenticationClient webclient = new AuthenticationClient();

                if (webclient.CreateUser(userID, userData, 1))
                {
                    return WSConstants.RETURN_CODE.RC_Success;
                }
                else
                {
                    return WSConstants.RETURN_CODE.RC_GeneralError;
                }
            }
            error = "You don't have permission to add a user.";
            return WSConstants.RETURN_CODE.RC_InvalidUser;
        }

        public WSConstants.RETURN_CODE Update_user(string userID, string passwd, string userData, ref string error)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {
                AuthenticationClient webclient = new AuthenticationClient();

                if (webclient.UpdateUser(userID, userData, 1))
                {
                    return WSConstants.RETURN_CODE.RC_Success;
                }
                else
                {
                    return WSConstants.RETURN_CODE.RC_GeneralError;
                }
            }
            else
            {
                // Unable to get LoginStatus
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }

        }

        public WSConstants.RETURN_CODE Delete_user(string userID, string passwd, string userIDToDelete, ref string error)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {
                AuthenticationClient webclient = new AuthenticationClient();
                if (webclient.DeleteUserByUserName(userID, userIDToDelete, 1))
                {
                    return WSConstants.RETURN_CODE.RC_Success;
                }
                else
                {
                    error = userID + " does not have access to " + userIDToDelete;
                    return WSConstants.RETURN_CODE.RC_GeneralError;
                }
            }
            else
            {
                // Unable to get LoginStatus
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }
        }

        public WSConstants.RETURN_CODE GetAllUsers(string userID, string passwd, ref string users, ref string error)
        {
            LoginStatus loginStatus = null;

            // Check to see that user is valid; store status info
            if (VerifyPassword(userID, passwd, ref loginStatus) == true)
            {
                AuthenticationClient webclient = new AuthenticationClient();
                if (webclient.GetAllUsers(userID, 1, ref users))
                {
                    return WSConstants.RETURN_CODE.RC_Success;
                }

                error = userID + " does not have permission to view users";
                return WSConstants.RETURN_CODE.RC_NoResults;
            }
            else
            {
                // Unable to get LoginStatus
                return WSConstants.RETURN_CODE.RC_InvalidUser;
            }

        }

        private static string Serialize(object objectToSerialize)
        {
            MemoryStream mem = new MemoryStream();
            XmlSerializer ser = new XmlSerializer(objectToSerialize.GetType());
            ser.Serialize(mem, objectToSerialize);
            ASCIIEncoding ascii = new ASCIIEncoding();
            return ascii.GetString(mem.ToArray());
        }

        private static object Deserialize(Type typeToDeserialize, string xmlString)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(xmlString);
            MemoryStream mem = new MemoryStream(bytes);
            XmlSerializer ser = new XmlSerializer(typeToDeserialize);
            return ser.Deserialize(mem);
        }
    }
}
