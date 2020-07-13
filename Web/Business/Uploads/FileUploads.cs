using Assmnts.Infrastructure;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Mvc;
using NLog;
using AJBoggs;

namespace Assmnts.Business.Uploads
{
	public static class FileUploads
	{
		private static ILogger mLogger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Saves a file to the file system, replacing whitespaces with underscores, according to the following structure: 
		/// [Root Directory] / superDir / formResultId / ivIdent
		/// </summary>
		/// <param name="file">File being saved.</param>
		/// <param name="ivIdent">Item Variable identifier in ADAP, or optional sub directory for multiple attachment system.</param>
		/// <param name="formResultId">formResultId for ADAP, or RelatedId for multiple attachment system.</param>
		/// <param name="superDir">Optional super directory for multiple attachment system.</param>
		/// <returns>String of the complete file path.</returns>
		public static string SaveUpload(Stream file, string originalFileName, string ivIdent, int formResultId, string superDir = null)
		{
			try {
				//string originalFileName = Path.GetFileName(file.Name);
				string fileName = originalFileName.Replace(' ', '_');

				// *** BR 3/2 method structure altered to allow more customizable file structures based on application.
				// add root directory.
				string saveDir = GetSaveDirectoryPath(formResultId, ivIdent, superDir);

				Directory.CreateDirectory(saveDir);
				string path = Path.Combine(saveDir, fileName);
				//file.SaveAs(path);
				using (var fileStream = File.Create(path)) {
					file.Seek(0, SeekOrigin.Begin);
					file.CopyTo(fileStream);
				}

				return path;
			}
			catch (Exception ex) {
				if (ex.IsCritical()) {
					throw ex;
				}
				mLogger.Error(ex, "Caught exception saving file upload.");
				return null;
			}
		}

		public static string GetSaveDirectoryPath(int formResultId, string ivIdent = null, string superDir = null)
		{
			if (formResultId < 1) {
				throw new ArgumentOutOfRangeException("Int value was less than 1.", "formResultId");
			}
			string saveDir = System.Configuration.ConfigurationManager.AppSettings[Constants.ATTACHMENT_DIRECTORY_SETTING];
			if (String.IsNullOrWhiteSpace(saveDir)) {
				mLogger.Fatal("Unable to read valid save directory from AppSettings {0}.", Constants.ATTACHMENT_DIRECTORY_SETTING);
				throw new InvalidOperationException(String.Format("Unable to read valid save directory from AppSettings {0}.", Constants.ATTACHMENT_DIRECTORY_SETTING));
			}
			// add optional super directory
			if (!String.IsNullOrEmpty(superDir)) {
				saveDir = Path.Combine(saveDir, superDir);
			}
			// add identifier specific directory
			saveDir = Path.Combine(saveDir, formResultId.ToString());

			// add optional sub directory.
			if (!String.IsNullOrEmpty(ivIdent)) {
				saveDir = Path.Combine(saveDir, ivIdent);
			}

			return saveDir;
		}

		/// <summary>
		/// Adds a new record, or Updates an existing record, to the def_FileAttachment table, then saves the file in the file system.  Files in the same directory with the same name
		/// will be overwritten without warning.  Use of sub-directories can prevent this.
		/// </summary>
		/// <param name="formsRepo"> IFormsRepository necessary for database access.</param>
		/// <param name="file">File being uploaded.</param>
		/// <param name="subDir">Optional superDirectory name, set to null to disable.</param>
		/// <param name="subDir">Optional subDirectory name, set to null to disable.</param>
		/// <param name="RelatedId">Identifier used to associate a file with a record type.</param>
		/// <param name="RelatedEnumId">Type of identifier used in RelatedId.</param>
		/// <param name="AttachTypeId">Indicates how the file was attached, which may represent the file itself or just a generic type.</param>
		/// <returns>Int value of the FileId</returns>
		public static int CreateAttachment(IFormsRepository formsRepo, System.Web.HttpPostedFileWrapper file, string superDir, string subDir, int RelatedId, int RelatedEnumId, int AttachTypeId)
		{
			int fileId = -1;
			if (file != null && !String.IsNullOrEmpty(file.FileName.Trim())) {
				fileId = CreateAttachment(formsRepo, file.InputStream, file.FileName, superDir, subDir, RelatedId, RelatedEnumId, AttachTypeId);
			} else {
				Debug.WriteLine("FileUploads CreateAttachment Error: File not found.");
			}

			return fileId;
		}

		/// <summary>
		/// Adds a new record, or Updates an existing record, to the def_FileAttachment table, then saves the file in the file system.  Files in the same directory with the same name
		/// will be overwritten without warning.  Use of sub-directories can prevent this.
		/// </summary>
		/// <param name="formsRepo"> IFormsRepository necessary for database access.</param>
		/// <param name="fileStream">input stream containing the contents of the attachment file</param>
		/// <param name="originalFileName">the original filename of the attachment file</param>
		/// <param name="subDir">Optional superDirectory name, set to null to disable.</param>
		/// <param name="subDir">Optional subDirectory name, set to null to disable.</param>
		/// <param name="RelatedId">Identifier used to associate a file with a record type.</param>
		/// <param name="RelatedEnumId">Type of identifier used in RelatedId.</param>
		/// <param name="AttachTypeId">Indicates how the file was attached, which may represent the file itself or just a generic type.</param>
		/// <returns>Int value of the FileId</returns>
		public static int CreateAttachment(IFormsRepository formsRepo, Stream fileStream, string originalFileName, string superDir, string subDir, int RelatedId, int RelatedEnumId, int AttachTypeId)
		{
			int fileId = -1;
			if (fileStream != null && !String.IsNullOrEmpty(originalFileName.Trim())) {
				Debug.WriteLine("FileUploads CreateAttachment FileName: " + originalFileName);
				// Save the file to the file system.
				string fileName = SaveUpload(fileStream, Path.GetFileName(originalFileName), subDir, RelatedId, superDir);

				if (!String.IsNullOrEmpty(fileName)) {
					// Append the sub directory and the file name if sub directory has a value.
					String subFileName = (!String.IsNullOrEmpty(subDir)) ? subDir + Path.DirectorySeparatorChar + fileName.Substring(fileName.LastIndexOf(Path.DirectorySeparatorChar) + 1)
																	: fileName.Substring(fileName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
					def_FileAttachment fa = formsRepo.GetFileAttachment(RelatedId, RelatedEnumId, subFileName);
					if (fa == null) {
						fa = new def_FileAttachment();
						fa.EnterpriseId = SessionHelper.LoginStatus.EnterpriseID;
						//fa.GroupId = SessionHelper.LoginStatus.GroupID;  //LoginStatus was returning an invalid number for GroupId
						fa.UserId = SessionHelper.LoginStatus.UserID;
						fa.AttachTypeId = AttachTypeId;
						fa.RelatedId = RelatedId;
						fa.RelatedEnumId = RelatedEnumId;
						fa.displayText = fileName.Substring(fileName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
						fa.FilePath = fileName;
						fa.FileName = subFileName;
						fa.StatusFlag = "A";
						fa.CreatedDate = DateTime.Now;
						fa.CreatedBy = SessionHelper.LoginStatus.UserID;

						fileId = formsRepo.AddFileAttachment(fa);
					} else {
						fa.FilePath = fileName;
						fa.FileName = subFileName;
						fa.StatusFlag = "A";
						formsRepo.UpdateFileAttachment(fa);

						fileId = fa.FileId;
					}
				} else {
					Debug.WriteLine("FileUploads CreateAttachment Error: File not saved.");
				}
			} else {
				Debug.WriteLine("FileUploads CreateAttachment Error: File not found.");
			}

			return fileId;
		}

		/// <summary>
		/// Overloaded method allowing the creation of the File Attachment record in a higher level method.  The FilePath is the only field updated within this method.
		/// </summary>
		/// <param name="formsRepo"> IFormsRepository necessary for database access.</param>
		/// <param name="file">File being uploaded.</param>
		/// <param name="subDir">Optional superDirectory name, set to null to disable.</param>
		/// <param name="subDir">Optional subDirectory name, set to null to disable.</param>
		/// <param name="RelatedId">Identifier used to associate a file with a record type.</param>
		/// <param name="fa">FileAttachment record to be recorded.</param>
		/// <returns>Int value of the FileId</returns>
		public static int CreateAttachment(IFormsRepository formsRepo, System.Web.HttpPostedFileWrapper file, string superDir, string subDir, int RelatedId, def_FileAttachment fa)
		{
			int fileId = -1;
			if (file != null && !String.IsNullOrEmpty(file.FileName.Trim())) {
				Debug.WriteLine("FileUploads CreateAttachment FileName: " + file.FileName);
				// Save the file to the file system.
				string fileName = SaveUpload(file.InputStream, Path.GetFileName(file.FileName), subDir, RelatedId, superDir);

				if (!string.IsNullOrEmpty(fileName)) {
					def_FileAttachment faQuery = formsRepo.GetFileAttachment(RelatedId, fa.RelatedEnumId, fileName);
					if (faQuery == null) {
						fa.FilePath = fileName;
						fileId = formsRepo.AddFileAttachment(fa);
					} else {
						fa.FilePath = fileName;
						formsRepo.UpdateFileAttachment(fa);

						fileId = fa.FileId;
					}
				} else {
					Debug.WriteLine("FileUploads CreateAttachment Error: File not saved.");
				}
			} else {
				Debug.WriteLine("FileUploads CreateAttachment Error: File not found.");
			}

			return fileId;
		}

        /// <summary>
        /// Allows the attachment of database records or other types of information which DO NOT require saving to the file system.
        /// Be aware the distinct displayText class expects the RelatedId and RelatedEnumId to form a unique key.
        /// </summary>
        /// <param name="formsRepo"> IFormsRepository necessary for database access.</param>
        /// <param name="RelatedId">Identifier used to associate a file with a record type.</param>
        /// <param name="fa">FileAttachment record to be recorded.</param>
        /// <returns></returns>
        public static int CreateDataAttachment(IFormsRepository formsRepo, int RelatedId, def_FileAttachment fa)
        {
            int fileId = -1;
            Debug.WriteLine("FileUploads CreateDataAttachment FileName: " + fa.FileName);

            if (!string.IsNullOrEmpty(fa.FilePath))
            {
                def_FileAttachment faQuery = formsRepo.GetFileAttachment(RelatedId, fa.RelatedEnumId, fa.FilePath);
                if (faQuery == null)
                {
                    fileId = formsRepo.AddFileAttachment(fa);
                }
                else
                {
                    formsRepo.UpdateFileAttachment(fa);

                    fileId = fa.FileId;
                }
            }
            else
            {
                Debug.WriteLine("FileUploads CreateAttachment Error: File not saved.");
            }

            return fileId;
        }

		public static Dictionary<int, string> RetrieveFileDisplayTextsByRelatedId(IFormsRepository formsRepo, int RelatedId, int RelatedEnumId, string StatusFlag = null, int? AttachTypeId = null)
		{
			Dictionary<int, string> displayText = new Dictionary<int, string>();
			try {
				Debug.WriteLine("FileUploads RetrieveFileDisplayTextsByRelatedId RelatedId: " + RelatedId);
				displayText = formsRepo.GetFileAttachmentDisplayText(RelatedId, RelatedEnumId, StatusFlag, AttachTypeId);
			}
			catch (Exception excptn) {
				Debug.WriteLine("FileUploads RetrieveFileDisplayTextsByRelatedId error: " + excptn.Message);
			}

			return displayText;
		}

		public static Dictionary<int, string> RetrieveDistinctFileDisplayTextsByRelatedId(IFormsRepository formsRepo, int RelatedId, int RelatedEnumId, string StatusFlag = null, int? AttachTypeId = null)
		{
			Dictionary<int, string> displayText = new Dictionary<int, string>();
			try {
				Debug.WriteLine("FileUploads RetrieveFileDisplayTextsByRelatedId RelatedId: " + RelatedId);
				displayText = formsRepo.GetDistinctFileAttachmentDisplayText(RelatedId, RelatedEnumId, StatusFlag, AttachTypeId);
			}
			catch (Exception excptn) {
				Debug.WriteLine("FileUploads RetrieveFileDisplayTextsByRelatedId error: " + excptn.Message);
			}

			return displayText;
		}

		public static FileContentResult RetrieveFile(IFormsRepository formsRepo, int FileId)
		{
			try {
				def_FileAttachment fa = formsRepo.GetFileAttachment(FileId);

				FileContentResult result = new FileContentResult(System.IO.File.ReadAllBytes(fa.FilePath),
								System.Net.Mime.MediaTypeNames.Application.Octet);
				result.FileDownloadName = System.IO.Path.GetFileName(fa.FilePath);

				return result;
			}
			catch (Exception excptn) {
				Debug.WriteLine("FileUploads RetrieveFile Error: " + excptn.Message);

				return new FileContentResult(Encoding.ASCII.GetBytes(excptn.Message), "text/html");
			}
		}

		public static bool DeleteFile(IFormsRepository formsRepo, int FileId)
		{
			// Pull the database record and set the Status Flag to 'D'.
			bool success = false;
			try {
				def_FileAttachment fa = formsRepo.GetFileAttachment(FileId);
				fa.StatusFlag = "D";
				success = formsRepo.UpdateFileAttachment(fa);
			}
			catch (Exception excptn) {
				Debug.WriteLine("FileUploads RetrieveFile Error: " + excptn.Message);
			}

			return success;
		}


	}
}