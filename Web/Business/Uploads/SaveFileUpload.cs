using System;
using System.Diagnostics;
using System.IO;

namespace Assmnts.Business.Uploads
{
    public static class FileUploads
    {
        
        public static string SaveUpload(System.Web.HttpPostedFileWrapper file, string ivIdent, int formResultId)
        {
            try
            {
                string originalFileName = Path.GetFileName(file.FileName);
                string fileName = originalFileName.Replace(' ', '_');

                string saveDir = System.Configuration.ConfigurationManager.AppSettings["AttachmentDir"] + Path.DirectorySeparatorChar + formResultId + Path.DirectorySeparatorChar + ivIdent;
                Directory.CreateDirectory(saveDir);
                string path = Path.Combine(saveDir, fileName);
                file.SaveAs(path);

                return path;
            }
            catch (Exception e)
            {
                Debug.WriteLine("* * *  ERROR FileUploads.SaveUpload: " + e.Message);
                return null;
            }
        }

        
    }
}