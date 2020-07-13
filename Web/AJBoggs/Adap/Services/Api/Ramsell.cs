using AJBoggs.Adap.Services.Xml;
using AJBoggs.Def.Domain;
using Assmnts;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Xml;


namespace AJBoggs.Adap.Services.Api
{
    public static partial class Ramsell
    {

        #region - constants

        // private const string urlRamsell = "https://demoapi.ramsellcorp.com/";
        private const string urlRamsell = "https://pbmapi.ramsellcorp.com/";

        #endregion - END constants


        public static string GetOauthToken(string userId, string password)
        {

            // Setup the variables necessary to make the Request 
            string grantType = "password";
            string applicationID = "{applicationId}";
            string clientString = "{clientString}";

            HttpWebResponse response = null;
            string accessToken = String.Empty;


            // Create the data to send
            StringBuilder data = new StringBuilder();
            data.Append("grant_type=" + Uri.EscapeDataString(grantType));
            // data.Append("&client_id=" + Uri.EscapeDataString(applicationID));
            // data.Append("&username=" + Uri.EscapeDataString(clientString + "\\" + username));
            data.Append("&username=" + Uri.EscapeDataString(userId));
            data.Append("&password=" + Uri.EscapeDataString(password));

            // Create a byte array of the data to be sent
            byte[] postBytes = Encoding.UTF8.GetBytes(data.ToString());

            string url = urlRamsell + @"oauth/token";
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "POST";

            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = postBytes.Length;

            // Write the data stream to Ramsell
            using (Stream strmReq = webRequest.GetRequestStream())
            {
                strmReq.Write(postBytes, 0, postBytes.Length);
            }

            /*
            POST https://api.oauth2server.com/token
    grant_type=password&
    username=USERNAME&
    password=PASSWORD&
    client_id=CLIENT_ID
             */

            /*
            string authInfo = "AfKNLhCngYfGb-Eyv5gn0MnzCDBHD7T9OD7PATaJWQzP3I1xDRV1mMK1i3WO:ECSAgxAiBE00pq-SY9YB5tHw0fd2UlayHGfMr5fjAaULMD2NFP1syLY7GCzt";
            WebClient client = new WebClient();
            NameValueCollection values;

            values = new NameValueCollection();
            values.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo)));
            values.Add("Accept", "application/json");
            values.Add("Accept-Language", "en_US");

            client.UploadValues("https://api.sandbox.paypal.com/v1/oauth2/token", values);
            */ 

            try
            {
                response = (HttpWebResponse)webRequest.GetResponse();

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    // Get the Response Stream
                    string json = reader.ReadLine();
                    Debug.WriteLine("response JSON: " + json);
 
                    // Retrieve and Return the Access Token
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    Dictionary<string, object> x = (Dictionary<string, object>)ser.DeserializeObject(json);
                    accessToken =  x["access_token"].ToString();
                    Debug.WriteLine("accessToken: " + accessToken);
                }

            }
            catch (WebException ex)
            {
                Debug.WriteLine("WebException: " + ex.Message);
                throw new ApplicationException("Could not get Ramsell OAuth token.", ex);
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
                throw new ApplicationException("Could not get Ramsell OAuth token.", ex);
            }

            return accessToken;
        }

        public static HttpWebRequest CreateWebRequestWithToken(string method, string urlCommand, string token)
        {

            // Setup the Ramsell WebRequest
            string url = urlRamsell + urlCommand;

            Debug.WriteLine("CreateWebRequestWithToken url: " + url);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = method;

            webRequest.Headers.Add("Authorization", "Bearer " + token);
            Debug.WriteLine("Headers: " + webRequest.Headers.ToString());

            return webRequest;
        }

        /// <summary>
        /// Gets User attributes from Ramsell
        /// </summary>
        /// <param name="oauthToken"></param>
        /// <returns></returns>
        public static int GetUserInfo(string oauthToken)
        {

            // Setup the variables necessary to make the Request 
            HttpWebRequest webRequest = CreateWebRequestWithToken("GET", @"oauth/me", oauthToken);
            webRequest.ContentType = "text/xml";

            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)webRequest.GetResponse();

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    // Get the Response Stream
                    string soapXml = reader.ReadLine();
                    Debug.WriteLine("GetUserInfo response SOAP(XML): " + soapXml);
                }

            }
            catch (WebException ex)
            {
                Debug.WriteLine("WebException: " + ex.Message);
                // throw new ApplicationException("Could not make Api call.", ex);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
                // throw new ApplicationException("Could not make Api call.", ex);

            }

            // Returned SOAP XML- NOT JSON !!!!
// GetUserInfo response JSON: [{"Type":"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name","Value":"38222218"},{"Type":"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress","Value":""},{"Type":"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier","Value":"7067"},{"Type":"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/stateorprovince","Value":"8"},{"Type":"http://schemas.xmlsoap.org/ws/2009/09/identity/claims/actor","Value":"TREVOR PETERSON"}]

            return 0;
        }

        /// <summary>
        /// Get the application data from Ramsell in their XML format.
        /// Nearly identical to the XML format that is sent to Ramsell
        /// </summary>
        /// <param name="oauthToken">Oauth 2 security token</param>
        /// <param name="memberId">Ramsell MemberId</param>
        /// <returns></returns>

        public static string GetApplicationXml(string oauthToken, string memberId)
        {
            HttpWebRequest webRequest = null;
            try
            {
                webRequest = CreateWebRequestWithToken("GET", @"api/Enrollment/" + memberId, oauthToken);
                webRequest.ContentType = "text/xml";
            }
            catch (WebException ex)
            {
                Debug.WriteLine("1st WebException: " + ex.Message);
                // throw new ApplicationException("Could not make Api call.", ex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("1st Exception: " + ex.Message);
                // throw new ApplicationException("Could not make Api call.", ex);
            }

            string decodedXml = string.Empty;
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)webRequest.GetResponse();
                // string returnString = response.StatusCode.ToString();
                // Debug.WriteLine("AddApplication StatusCode: " + returnString);

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    // Get the Response Stream
                    string xml = reader.ReadLine();
                    
                    Debug.WriteLine("GetApplicationXml response xml: " + xml);

                    decodedXml = System.Net.WebUtility.HtmlDecode(xml);
                    Debug.WriteLine("GetApplicationXml Decoded(XML): " + decodedXml);
                }

            }
            catch (WebException ex)
            {
                Debug.WriteLine("GetApplicationXml WebException: " + ex.Message);

                if ((ex.InnerException != null) && (ex.InnerException.Message != null))
                {
                    string errMsg = " * Inner Exception: " + ex.InnerException.Message;
                    Debug.WriteLine("GetApplicationXml WebException" + errMsg);
                }
                // throw new ApplicationException("Could not make Api call.", ex);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
                // throw new ApplicationException("Could not make Api call.", ex);
            }

            return decodedXml;
        }

        public static string SendApplicationData(Data.Abstract.IFormsRepository formsRepo, int formResultId)
        {
            string usrMsg = string.Empty; 


            // attempt to get the Ramsell MemberId from application (FormResults)
            def_ResponseVariables rvRamsellId = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "ADAP_D9_Ramsell");
            string ramsellId = ( (rvRamsellId == null) || (String.IsNullOrWhiteSpace(rvRamsellId.rspValue)) ) ? null : rvRamsellId.rspValue;

            // Create an XML Document from the current application FormResults
            string xmlString = string.Empty;
            try
            {
                System.Xml.XmlDocument xmlDoc = RamsellExport.BuildRamsellXmlDocForFormResult(formResultId, formsRepo);
                StringBuilder xmlStringBuilder = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(xmlStringBuilder, Utilities.xmlWriterSettings))
                {
                    xmlDoc.Save(writer);
                }
                xmlString = xmlStringBuilder.ToString();
            }
            catch (Exception xcptn)
            {
                usrMsg = usrMsg + " Error: " + xcptn.Message;
                return usrMsg;
            }

            if ( String.IsNullOrEmpty(xmlString) )
            {
                usrMsg = usrMsg + " Error: No XML document was created for Ramsell.";
                return usrMsg;
            }

            //attempt to add/update application to Ramsell system
            // string usrId = "38222218";
            // string password = @"P@ssw0rd";

            string usrId = UAS.Business.UAS_Business_Functions.GetEntAppConfigAdap("USR");
            string password = UAS.Business.UAS_Business_Functions.GetEntAppConfigAdap("PWD");
            Debug.WriteLine("Ramsell UseriD / Password: " + usrId + @" / " + password);

            // Get OAuth token from Ramsell. If problem, exit immediately.
            string token = string.Empty;
            try
            {
                token = Ramsell.GetOauthToken(usrId, password);
            }
            catch (Exception xcptn)
            {
                usrMsg = usrMsg + " Error: " + xcptn.Message;
                return usrMsg;
            }

            if (String.IsNullOrEmpty(token))
            {
                usrMsg = usrMsg + " Error: no OAuth token returned from Ramsell.";
                return usrMsg;
            }

            // If Application already has a Ramsell MemberId, update Ramsell with the current application data
            if (ramsellId != null)
            {
                try
                {
                    Ramsell.AddOrUpdateApplication(token, xmlString, ramsellId);
                    usrMsg = usrMsg + " Application successfully updated in Ramsell.";
                }
                catch (Exception e)
                {
                    string decodedMsg = e.Message.Replace("&lt;", " ").Replace("&amp;", "&").Replace("&gt;", " ").Replace("&quot;", "\"").Replace("&apos;", "'");
                    usrMsg = usrMsg + " Error updating application in Ramsell with Member ID: " + ramsellId + " - " + decodedMsg;
                }
            }

            // no Ramsell MemberId, add application to Ramsell
            else
            {
                try
                {
                    long newRamsellId = Ramsell.AddOrUpdateApplication(token, xmlString);
                    Ramsell.UpdateRamsellId(formsRepo, formResultId, newRamsellId);
                    usrMsg = usrMsg + " Application successfully added in Ramsell.";
                }
                catch (Exception e)
                {
                    string decodedMsg = e.Message.Replace("&lt;", " ").Replace("&amp;", "&").Replace("&gt;", " ").Replace("&quot;", "\"").Replace("&apos;", "'");
                    usrMsg = usrMsg + " Error adding application in Ramsell: " + decodedMsg;
                }
            }

            return usrMsg;
        }

        /// <summary>
        /// Adds the MemberId returned from Ramsell to the Application (FormResults)
        /// *** this should be refactored to use a generic method(s) in AJBoggs.Def.Domain.UserData
        /// </summary>
        /// <param name="formsRepo">FormsRepository</param>
        /// <param name="frId">FormResults.formResultId</param>
        /// <param name="ramsellId">Ramsell MemberId</param>
        private static void UpdateRamsellId(IFormsRepository formsRepo, int frId, long ramsellId)
        {
            string itmVarIdentifier = "ADAP_D9_Ramsell";
            def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(itmVarIdentifier);
            if (iv == null)
            {
                Debug.WriteLine("could not find item variable with identifier \"" + itmVarIdentifier + "\" (not case-sensitive)");
                return;
            }

            UserData userData = new UserData(formsRepo);
            def_ItemResults ir = userData.SaveItemResult(frId, iv.itemId);
            userData.SaveResponseVariable(ir, iv, ramsellId.ToString());
            formsRepo.Save();
        }


        /// <summary>
        /// Adds or updates an ADAP Application to the Ramsell system using OAuth authenticaion token
        /// </summary>
        /// <param name="oauthToken"></param>
        /// <param name="xmlApplication"></param>
        /// <param name="memberId">Id of application to update. Omit or use null to add an application</param>
        /// <returns>new memberId if adding an application</returns>
        public static long AddOrUpdateApplication(string oauthToken, string xmlApplication, string memberId = null)
        {
            xmlApplication = xmlApplication.Replace("<Patient xmlns=\"http://www.w3.org/2001/XMLSchema\">", "<Patient>");

            //xmlApplication = File.ReadAllText(@"C:\Users\otessmer\Documents\resultMM.xml");

            HttpWebRequest webRequest = null;
            try
            {
                string urlCommand = @"api/Enrollment";
                if (memberId != null)
                    urlCommand += "/" + memberId;
                string action = memberId == null ? "PUT" : "POST";

                webRequest = CreateWebRequestWithToken( action, urlCommand, oauthToken);
                webRequest.ContentType = "text/xml";
                if (!String.IsNullOrEmpty(xmlApplication))
                {
                    // Create a byte array of the data to be sent
                    byte[] putBytes = Encoding.UTF8.GetBytes(xmlApplication);
                    webRequest.ContentLength = putBytes.Length;
                    // webRequest.AllowWriteStreamBuffering = false;
                    // Write the data stream (XML Application) to Ramsell
                    using (Stream strmReq = webRequest.GetRequestStream())
                    {
                        strmReq.Write(putBytes, 0, putBytes.Length);
                    }
                    Debug.WriteLine("AddOrUpdateApplication bytes sent: " + putBytes.Length.ToString());
                }
            }
            catch (WebException ex)
            {
                Debug.WriteLine("1st WebException: " + ex.Message);
                // throw new ApplicationException("Could not make Api call.", ex);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("1st Exception: " + ex.Message);
                // throw new ApplicationException("Could not make Api call.", ex);

            }

            if (webRequest.HaveResponse)
                Debug.WriteLine("webRequest.HaveResponse is true.");
            else
            {
                Debug.WriteLine("webRequest.HaveResponse is false.");
            }

            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)webRequest.GetResponse();
                // string returnString = response.StatusCode.ToString();
                // Debug.WriteLine("AddApplication StatusCode: " + returnString);

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    // Get the Response Stream (probably XML is being returned).
                    string json = reader.ReadLine();
                    Debug.WriteLine("AddOrUpdateApplication response JSON: " + json);

                    if (memberId == null)
                    {
                        long result = Int64.Parse(Regex.Replace(json, "<[^>]+>", String.Empty));
                        return result;
                    }
                    else
                    {
                        return 0;
                    }
                }

                // Debug.WriteLine("response: " + response.);
            }
            catch (WebException webXcptn)
            {
                Debug.WriteLine("AddOrUpdateApplication WebException: " + webXcptn.Message);
                if ((webXcptn.InnerException != null) && (webXcptn.InnerException.Message != null))
                {
                    Debug.WriteLine(" * Inner Exception: " + webXcptn.InnerException.Message);
                }

                // Read the Error returned - should be XML
                string errMsg = null;
                Debug.WriteLine(">>> WebException - Error Message returned from Ramsell.");
                if (webXcptn.Response != null)
                {
                    using (var errResp = (HttpWebResponse)webXcptn.Response)
                    {
                        using (StreamReader reader = new StreamReader(errResp.GetResponseStream()))
                        {
                            // Get the Response Stream
                            errMsg = reader.ReadToEnd();
                            Debug.WriteLine(errMsg);
                        }
                    }
                }
                Debug.WriteLine(">>> Error Message End <<<");
                if( errMsg.Contains("The current custom error settings for this application prevent the details of the application error from being viewed remotely") )
                    throw new Exception("No details available.");
                errMsg = Regex.Replace(errMsg, "<[^>]+>", " "); //remove xml tags
                throw new Exception( errMsg );
            }

            return 0;
        }
    }
}
