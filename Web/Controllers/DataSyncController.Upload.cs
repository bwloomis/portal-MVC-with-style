using Assmnts.Infrastructure;
using Assmnts.Models;

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;

using UAS.Business;



namespace Assmnts.Controllers
{
    /*
     *  The methods in this controller are used to exchange SQL tables with Venture (local DEF3).
     *      The index methods displays a screen to manage the synching (updload / download process).
     * 
     */
    public partial class DataSyncController : Controller
    {
        [HttpGet]
        public string TestCreateFormResults()
        {
            string formResult = Request["formResultId"] as string;
            int formResultId = Int32.Parse(formResult);
            def_FormResults fr = formsRepo.GetFormResultById(formResultId);

            int result = CreateFormResult(fr.formId, (int)fr.formStatus, (int)fr.sessionStatus, 
                fr.dateUpdated, fr.deleted, fr.locked, fr.archived, fr.EnterpriseID, fr.GroupID, 
                fr.subject, fr.interviewer, fr.assigned, fr.training, (int)fr.reviewStatus, fr.statusChangeDate);

            return "New form result ID of copy: " + result;
        }
        
        
        /// <summary>
        /// Web service method to create a form result (called from Venture)
        /// </summary>
        /// <param name="formId"></param>
        /// <param name="formStatus"></param>
        /// <param name="EnterpriseID"></param>
        /// <param name="GroupID"></param>
        /// <param name="interviewer"></param>
        /// <returns></returns>
        [HttpPost]
        public int CreateFormResult(int formId, int formStatus, int sessionStatus, DateTime dateUpdated, 
            bool deleted, bool locked, bool archived, int? EnterpriseID, int? GroupID, int? subject, int? interviewer, int? assigned, bool training, int reviewStatus, DateTime? statusChangeDate)
        {
            int grpId = (GroupID == null) ? -1 : (int)GroupID;
            try {
                if (SessionHelper.LoginStatus.EnterpriseID == 0 || SessionHelper.LoginStatus.EnterpriseID == EnterpriseID)
                {
                    if (SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Contains(0) || SessionHelper.LoginStatus.appGroupPermissions[0].authorizedGroups.Contains(grpId))
                    {
                        bool create = UAS_Business_Functions.hasPermission(SessionHelper.LoginStatus.appGroupPermissions[0].groupPermissionSets[0].PermissionSet, UAS.Business.PermissionConstants.CREATE, UAS.Business.PermissionConstants.ASSMNTS);

                        if (create)
                        {
                            def_FormResults formResult = new def_FormResults();

                            formResult.formId = formId;
                            formResult.formStatus = (byte)formStatus;
                            formResult.sessionStatus = (byte)sessionStatus;
                            formResult.dateUpdated = dateUpdated;
                            formResult.deleted = deleted;
                            formResult.locked = locked;
                            formResult.archived = archived;
                            formResult.EnterpriseID = EnterpriseID;
                            formResult.GroupID = GroupID;
                            formResult.subject = subject;
                            formResult.interviewer = interviewer;
                            formResult.assigned = assigned;
                            formResult.training = training;
                            formResult.reviewStatus = (byte)reviewStatus;
                            formResult.statusChangeDate = statusChangeDate;

                            formsRepo.AddFormResult(formResult);

                            return formResult.formResultId;
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("CreatFormResult exception: " + ex.Message);
                return -2;
            }
            return -1;
        }
        
        
        [HttpGet]
        public ActionResult Upload()
        {
            DataSync model = new DataSync();
            model.formResultIds = formsSql.GetCompletedFormResultIds();
            return View("uploadSync", model);
        }

        [HttpGet]
        public ActionResult UploadSingle()
        {
            DataSync model = new DataSync();
            model.ID = SessionHelper.Read<Int32>("uploadFormResultId");
            return View("uploadSingle", model);
        }

        public void MarkUploaded()
        {
            formsSql.MarkUploaded();
        }

        public void MarkSingleUploaded(int formResultId)
        {
            formsSql.MarkSingleUploaded(formResultId);

            if (SessionHelper.Read<int>("newFormResultId") != 0)
            {
                try
                {
                    formsSql.UpdateFormResultID(formResultId, SessionHelper.Read<int>("newFormResultId"));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error updating form result id for new assessment.");
                }
            }
        }
           
        //public string UploadCompletedJson(int i)
        //{
        //    string[] jsonStrings = (string[])Session["jsonStrings"];
        //    string json = jsonStrings[i];
        //    bool success = UploadJSON(json);
        //    return (success) ? "true" : "false";
        //}

        /// <summary>
        /// Run from uploadSync.cshtml and uploadSingle.cshtml
        /// </summary>
        /// <param name="formResultId">FormResults.formResultId</param>
        /// <returns>Complete FormResult structure in JSON format.</returns>
        [HttpPost]
        public bool CreateFormResultJSON(int formResultId)
        {
            int newFormResultId = -1;
            // New Assessments created in the Venture database must have negative formResultIds.
            if (formResultId < 0)
            {
                newFormResultId = GetNewFormResultID(formResultId);
            }
            Debug.WriteLine("DataSync CreateFormResultJSON formResultId: " + formResultId.ToString() + "   " + newFormResultId.ToString());

            string json = formsSql.CreateFormResultJSON(formResultId, newFormResultId);

            SessionHelper.Write("jsonData", json);

            // RRB - 10/14/2015 - removed test files.
            // string path = Server.MapPath("~/App_Data/testjson.txt");
            // System.IO.File.WriteAllText(path, json);
            // System.IO.File.WriteAllText(@"C:\Users\lkelly\Documents\testJSON\testjson.txt", json);
            return true;
        }

        private int GetNewFormResultID(int formResultId)
        {
            Debug.WriteLine("DataSync.GetNewFormResultID formResultId: " + formResultId.ToString());

            def_FormResults fr = formsRepo.GetFormResultById(formResultId);

            try
            {
                HttpWebRequest httpRequest = null;
                httpRequest = (HttpWebRequest)WebRequest.Create(sisOnlineURL + @"Defws/Login?UserId=" + SessionHelper.LoginInfo.LoginID + "&pwrd=" + SessionHelper.LoginInfo.Password);

                CookieContainer cc = new CookieContainer();
                httpRequest.CookieContainer = cc;

                httpRequest.Method = WebRequestMethods.Http.Get;

                // Get back the HTTP response for web server
                using (HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                {
                    using (Stream httpResponseStream = httpResponse.GetResponseStream())
                    {
                        string response = String.Empty;
                        using (StreamReader reader = new StreamReader(httpResponseStream))
                        {
                            response = reader.ReadToEnd();
                        }

                        Debug.WriteLine("GetNewFormResultID response: " + response);
                    }
                }

                using (var client = new CookieWebClient(cc))
                {
                    var data = new NameValueCollection()
                        {
                            //int formId, int formStatus, int EnterpriseID, int GroupID, int interviewer)
                            
//                            int formId, int formStatus, int sessionStatus, DateTime dateUpdated, 
//            bool deleted, bool locked, bool archived, int EnterpriseID, int GroupID, int subject, int interviewer, int assigned, bool training, int reviewStatus, DateTime statusChangeDate)
                            {"formId", fr.formId.ToString()},
                            {"formStatus", fr.formStatus.ToString()},
                            {"sessionStatus", fr.sessionStatus.ToString()},
                            {"dateUpdated", fr.dateUpdated.ToString()},
                            {"deleted", fr.deleted.ToString()},
                            {"locked", fr.locked.ToString()},
                            {"archived", fr.archived.ToString()},
                            {"EnterpriseID", fr.EnterpriseID.ToString()},
                            {"GroupID", fr.GroupID.ToString()},
                            {"subject", fr.subject.ToString()},
                            {"interviewer", fr.interviewer.ToString()},
                            {"assigned", fr.assigned.ToString()},
                            {"training", fr.training.ToString()},
                            {"reviewStatus", fr.reviewStatus.ToString()},
                            {"statusChangeDate", fr.statusChangeDate.ToString()}
                        };
                    byte[] result = client.UploadValues(sisOnlineURL + "DataSync/" + "CreateFormResult", "POST", data);

                    string newId = Encoding.ASCII.GetString(result);
                    int newFormResultId = Int32.Parse(newId);
                    SessionHelper.Write("newFormResultId", newFormResultId);
       
                    return newFormResultId;
                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetNewFormResultID - exception:" + ex.Message);
                return formResultId;
            }

        }


        /// <summary>
        /// This is used by uploadSingle.cshtml
        /// </summary>
        /// <param name="jsonData"></param>
        /// <returns>true if successful, false if transmission of Assessment fails.</returns>
        [HttpPost]
        public bool UploadSingleJson()
        {
            string jsonData = SessionHelper.Read<string>("jsonData");
            
            try
            {
                // Login to the remote server
                HttpWebRequest httpRequest = null;
                string uriString = sisOnlineURL + @"Defws/Login?UserId=" + SessionHelper.LoginInfo.LoginID + "&pwrd=" + SessionHelper.LoginInfo.Password;
                Debug.WriteLine("UploadSingleJson uriString: " + uriString);
                // string encodedUrl = WebUtility.UrlEncode(uriString);

                Uri sisServerUri = new Uri(uriString);
                // Debug.WriteLine("UploadSingleJson uriString: " + sisServerUri.ToString());
                Debug.WriteLine("UploadSingleJson  sisServerUri: " + sisServerUri.ToString());

                httpRequest = (HttpWebRequest)WebRequest.Create(sisServerUri);
                CookieContainer cc = new CookieContainer();
                httpRequest.CookieContainer = cc;
                httpRequest.Method = WebRequestMethods.Http.Get;

                // Get back the HTTP response from remote server for the login
                using (HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                {
                    using (Stream httpResponseStream = httpResponse.GetResponseStream())
                    {
                        string response = String.Empty;
                        using (StreamReader reader = new StreamReader(httpResponseStream))
                        {
                            response = reader.ReadToEnd();
                        }

                        Debug.WriteLine("Response: " + response);
                    }
                }

                // *** RRB 10/28/15 - There should be a check here for a valid login response.
                // The login logic above should be in its own method and used by all the logins.

                // Transmit the Assessment to the remote server.
                using (var client = new CookieWebClient(cc))
                {
                    var data = new NameValueCollection()
                    {
                        {"json", jsonData}
                    };
                    byte[] result = client.UploadValues(sisOnlineURL + "Defws/" + "UpdateAssessmentJSONVenture", "POST", data);

                    Debug.WriteLine("Status code: " + client.StatusCode());
                    if (result.Length == 0)
                    {
                        return true;
                    }
                    if (result == null)
                        Debug.WriteLine("Upload JSON error  result is null.");
                    if (result.Length > 0)
                        Debug.WriteLine("Upload JSON error  result.length: " + result.Length);
                    
                    Debug.WriteLine("Upload JSON error: " + Encoding.ASCII.GetString(result));

                    return false;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("UploadSingleJSON:" + ex.Message);
                if (ex.InnerException != null && ex.InnerException.Message != null)
                {
                    Debug.WriteLine("UploadSingleJSON InnerException: " + ex.InnerException.Message);
                }

                return false;
            }
        }
        
        
        public int ConvertCompletedToJSON()
        {
          //  int[] completed = formsSql.GetCompletedFormResults();
            string[] jsonStrings = formsSql.CreateFormResultsJSON();
            Session["jsonStrings"] = jsonStrings;
            return jsonStrings.Length;
        }

        //public bool UploadJSON(string json)
        //{
        //    try
        //    {

        //        HttpWebRequest httpRequest = null;

        //        httpRequest = (HttpWebRequest)WebRequest.Create(sisOnlineURL + "Defws/" + "Login?UserId=" + SessionHelper.LoginInfo.LoginID + "&pwrd=" + SessionHelper.LoginInfo.Password);

        //        CookieContainer cc = new CookieContainer();

        //        httpRequest.CookieContainer = cc;

        //        httpRequest.Method = WebRequestMethods.Http.Get;

        //        // Get back the HTTP response for web server
        //        HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
        //        Stream httpResponseStream = httpResponse.GetResponseStream();

        //        string response = String.Empty;
        //        using (StreamReader reader = new StreamReader(httpResponseStream))
        //        {
        //            response = reader.ReadToEnd();
        //        }

        //        Debug.WriteLine("Response: " + response);
                
        //        httpResponseStream.Close();
        //        httpResponse.Close();


        //        using (var client = new CookieWebClient(cc))
        //        {
        //            var data = new NameValueCollection()
        //            {
        //                {"json", json}
        //            };
        //            var result = client.UploadValues(sisOnlineURL + "Defws/" + "UpdateFormResultJSON", "POST", data);


        //            if (result.Length == 0) {
        //                return true;
        //            }
        //            Console.WriteLine("Upload JSON error: " + result);

        //            return false;
        //        }
                
                
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("UploadJSON:" + ex.Message);
        //        return false;
        //    }
        //}


        public class CookieWebClient : WebClient
        {
            public CookieContainer CookieContainer { get; private set; }

            private WebRequest _Request = null;
            
            public CookieWebClient()
            {
                this.CookieContainer = new CookieContainer();
            }

            public CookieWebClient(CookieContainer cookieContainer)
            {
                this.CookieContainer = cookieContainer;
            }

            public HttpStatusCode StatusCode()
            {
                HttpStatusCode result;

                if (this._Request == null)
                {
                    throw (new InvalidOperationException("Unable to retrieve the status code, maybe you haven't made a request yet."));
                }

                HttpWebResponse response = base.GetWebResponse(this._Request) 
                                           as HttpWebResponse;

                if (response != null)
                {
                    result = response.StatusCode;
                }
                else
                {
                    throw (new InvalidOperationException("Unable to retrieve the status code, maybe you haven't made a request yet."));
                }

                return result;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address) as HttpWebRequest;
                if (request == null) return base.GetWebRequest(address);
                request.ContinueTimeout = 5 * 60 * 1000;
                request.CookieContainer = CookieContainer;
                request.Timeout = 5 * 60 * 1000; // 2 minutes to time out
                this._Request = request;
                return request;
            }
        }
    
    }
}
