using Assmnts;
using System.Diagnostics;
using System.IO;
using System.Web;


namespace AJBoggs.Def.Domain
{
    /*
     * This parital class is used to Save file uploads associated with a formResultId and a ResponseVariables ItemVariable
     * 
     * It should be used by Controllers and WebServices to Save User Data
     * 
     */
    public partial class UserData
    {

        public bool SaveFileUploads(int formResultId, HttpRequestBase httpRequest)
        {
            //for debugging runtime
            //DateTime startTime = DateTime.Now;
            Debug.WriteLine("***  SaveFileUploads formResultId: " + formResultId.ToString());

            // This section handles files uploaded as part of an HTML file upload
            if ((httpRequest != null) && (httpRequest.Files != null))
            {
                foreach (string key in httpRequest.Files.AllKeys)
                {
                    def_ItemVariables iv = formsRepo.GetItemVariableByIdentifier(key);
                    if (iv != null)
                    {
                        HttpPostedFileWrapper upload = (HttpPostedFileWrapper)httpRequest.Files[iv.identifier];
                        if (upload.ContentLength > 0)
                        {
                            // RRB 11/10/15 moved to Business layer
                            // val = formsRepo.SaveUpload(upload, iv.identifier, SessionHelper.SessionForm.formResultId);
                            string rspVal = Assmnts.Business.Uploads.FileUploads.SaveUpload(
                                upload.InputStream, Path.GetFileName(upload.FileName), iv.identifier, formResultId);

                            def_ItemResults ir = SaveItemResult(formResultId, iv.itemId);
                            SaveResponseVariable(ir, iv, rspVal);
                        }
                    }
                }

                // All responses are now in Entity Framework, so Save all the Entities to the Forms Repository
                formsRepo.Save();
            }

            return true;

        }

    }
}
