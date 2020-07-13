using AJBoggs.Sis.Domain;

using Assmnts;
using Assmnts.Infrastructure;

using Data.Abstract;

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

using UAS.Business;

namespace Data.Concrete
{
    public partial class FormsRepository : IFormsRepository
    {
        public int AddFileAttachment(def_FileAttachment fa)
        {
            db.def_FileAttachment.Add(fa);
            db.SaveChanges();
            return fa.FileId;
        }

        public bool UpdateFileAttachment(def_FileAttachment fa)
        {
            bool success = false;
            try
            {
                db.def_FileAttachment.Attach(fa);

                db.Entry(fa).State = EntityState.Modified;
                Save();
                success = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  FormsRepository  UpdateFileAttachment exception: " + ex.Message);
            }

            return success;
        }

        public def_FileAttachment GetFileAttachment(int RelatedId, int RelatedEnumId, string fileName)
        {
            return db.def_FileAttachment.Where(f => f.RelatedId == RelatedId && f.RelatedEnumId == RelatedEnumId && f.FileName.Equals(fileName)).FirstOrDefault();
        }

        public def_FileAttachment GetFileAttachment(int RelatedId, int RelatedEnumId)
        {
            return db.def_FileAttachment.Where(f => f.RelatedId == RelatedId && f.RelatedEnumId == RelatedEnumId).FirstOrDefault();
        }

        public def_FileAttachment GetFileAttachment(int fileId)
        {
            return db.def_FileAttachment.Where(f => f.FileId == fileId).FirstOrDefault();
        }

        /// <summary>
        /// Returns a list of File Attachments by RelatedId and RelatedEnumId.  
        /// Optional AttachTypeId, disregard to return all Attachment Types. 
        /// </summary>
        /// <param name="RelatedId">Id related to the given set of attachments, i.e. formResultId</param>
        /// <param name="RelatedEnumId">Enumeration indicating RelatedId datasource.</param>
        /// <param name="AttachTypeId">Enumeration indicating type of upload.</param>
        /// <returns></returns>
        public List<def_FileAttachment> GetFileAttachmentList(int RelatedId, int RelatedEnumId, int? AttachTypeId = null)
        {
            return db.def_FileAttachment.Where(f => f.RelatedId == RelatedId && f.RelatedEnumId == RelatedEnumId && (AttachTypeId == null || f.AttachTypeId == AttachTypeId)).ToList();
        }

        /// <summary>
        /// Returns a list of DisplayTexts for a given RealtedId and RelatedEnumId.
        /// Optional StatusFlag and AttachTypeId, disregard to return all Status or Attachment types.
        /// </summary>
        /// <param name="RelatedId">Id related to the given set of attachments, i.e. formResultId</param>
        /// <param name="RelatedEnumId">Enumeration indicating RelatedId datasource.</param>
        /// <param name="StatusFlag">Single character string indicating the status of the given record.</param>
        /// <param name="AttachTypeID">Enumeration indicating type of upload.</param>
        /// <returns></returns>
        public Dictionary<int, string> GetFileAttachmentDisplayText(int RelatedId, int RelatedEnumId, string StatusFlag = null, int? AttachTypeId = null)
        {
            return db.def_FileAttachment.Where(f => f.RelatedId == RelatedId && f.RelatedEnumId == RelatedEnumId 
                && (String.IsNullOrEmpty(StatusFlag) || f.StatusFlag.Equals(StatusFlag)) && (AttachTypeId == null || f.AttachTypeId == AttachTypeId))
                .ToDictionary(f => f.FileId, f => f.displayText);
        }

        /// <summary>
        /// Returns an unduplicated list of DisplayTexts for a given RealtedId and RelatedEnumId.
        /// Optional StatusFlag and AttachTypeId, disregard to return all Status or Attachment types.
        /// </summary>
        /// <param name="RelatedId">Id related to the given set of attachments, i.e. formResultId</param>
        /// <param name="RelatedEnumId">Enumeration indicating RelatedId datasource.</param>
        /// <param name="StatusFlag">Single character string indicating the status of the given record.</param>
        /// <param name="AttachTypeID">Enumeration indicating type of upload.</param>
        /// <returns></returns>
        public Dictionary<int, string> GetDistinctFileAttachmentDisplayText(int RelatedId, int RelatedEnumId, string StatusFlag = null, int? AttachTypeId = null)
        {

            Dictionary<int, string> results = db.def_FileAttachment.Where(f => f.RelatedId == RelatedId && f.RelatedEnumId == RelatedEnumId
                && (String.IsNullOrEmpty(StatusFlag) || f.StatusFlag.Equals(StatusFlag)) && (AttachTypeId == null || f.AttachTypeId == AttachTypeId))
                .GroupBy(f => f.displayText)                                                    //Group by displayText
                .Select(g => g.OrderByDescending(f => f.CreatedDate).FirstOrDefault())          //Order by newest to oldest, then pull the first of each distinct displayText
                .ToDictionary(g => g.FileId, g => g.displayText);                               //Build the dictionary

            return results;
        }

        public int GetRelatedEnumIdByEnumDescription(string enumDescription)
        {
            return db.def_RelatedEnum.Where(r => r.EnumDescription.Equals(enumDescription)).Select(r => r.RelatedEnumId).FirstOrDefault();
        }

        public int GetAttachTypeIdByAttachDescription(string attachDescription)
        {
            return db.def_AttachType.Where(a => a.AttachDescription.Equals(attachDescription)).Select(a => a.AttachTypeId).FirstOrDefault();
        }
    }
}