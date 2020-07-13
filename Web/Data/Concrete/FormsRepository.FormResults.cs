// *** RRB 2016/2/6  Needs to be refactored - SIS specific methods moved to SIS Domain
// ***               Comment methods
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
        public IList<def_ItemResults> GetItemResults(int formResultId) {
            return db.def_ItemResults
                .Where(x => x.formResultId == formResultId)
                .Include("def_ResponseVariables.def_ItemVariables")
                .ToList();
        }

        // Form Results

        /// <summary>
        /// Add and Save a FormResults records to the SQL table.
        /// </summary>
        /// <param name="frmRslt">FormResults object</param>
        /// <returns>Returns the formResultId of the record INSERTed</returns>
        public int AddFormResult(def_FormResults frmRslt)
        {
            try
            {
                db.def_FormResults.Add(frmRslt);
                db.SaveChanges();
            }
            catch (DbEntityValidationException xptn)
            {
                Debug.WriteLine("* * *  FormsRepository  AddFormResult Exception: " + xptn.Message);
                foreach (var evnt in xptn.EntityValidationErrors)
                {
                    Debug.WriteLine(@"Entity of type '{0}' in state '{1}' has the following validation errors:",
                        evnt.Entry.Entity.GetType().Name, evnt.Entry.State);

                    foreach (var ve in evnt.ValidationErrors)
                    {
                        Debug.WriteLine("- Property: \"{0}\", Error: \"{1}\"", ve.PropertyName, ve.ErrorMessage);
                    }
                }

            }

            return frmRslt.formResultId;
        }

        public void AddFormResultNoSave(def_FormResults frmRslt)
        {
            db.def_FormResults.Add(frmRslt);
        }

        public void SaveFormResults(def_FormResults frmRslt)
        {
            try
            {
                db.def_FormResults.Attach(frmRslt);
                db.Entry(frmRslt).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (DbEntityValidationException dbEx)
            {
                Debug.WriteLine("* * *  FormsRepository  SaveFormResults DbEntityValidationException: " + dbEx.Message);
                foreach (DbEntityValidationResult devr in dbEx.EntityValidationErrors)
                {
                    foreach (DbValidationError dve in devr.ValidationErrors)
                    {
                        Debug.WriteLine("    DbEntityValidationResult: " + dve.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  FormsRepository  SaveFormResults exception: " + ex.Message);
            }

            return;
        }

        public void SaveFormResultsThrowException(def_FormResults frmRslt)
        {
            try
            {
                db.def_FormResults.Attach(frmRslt);
                db.Entry(frmRslt).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (DbEntityValidationException dbEx)
            {
                Debug.WriteLine("* * *  FormsRepository  SaveFormResults DbEntityValidationException: " + dbEx.Message);
                foreach (DbEntityValidationResult devr in dbEx.EntityValidationErrors)
                {
                    foreach (DbValidationError dve in devr.ValidationErrors)
                    {
                        Debug.WriteLine("    DbEntityValidationResult: " + dve.ErrorMessage);
                    }
                }

                throw dbEx;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  FormsRepository  SaveFormResults exception: " + ex.Message);
                db.Entry(frmRslt).State = EntityState.Detached;
                throw ex;
            }

            return;
        }


        public IEnumerable<def_FormResults> GetFormResultsByFormSubject(int formId, int? subject)
        {
            return db.def_FormResults.Where(fr => fr.formId == formId && fr.subject == subject);
        }

        public IQueryable<def_FormResults> GetFormResultsBySubject(int? subject)
        {
            return db.def_FormResults.Where(fr => fr.subject == subject);
        }


        public IQueryable<def_FormResults> GetFormResultsByFormId(int formId)
        {
            return db.def_FormResults.Where(fr => fr.formId == formId);
        }

        public def_FormResults GetFormResultById(int frId)
        {
            return db.def_FormResults.SingleOrDefault(fr => fr.formResultId == frId);
        }

        public void GetFormResultItemResults(def_FormResults frmRslt)
        {
            db.Entry(frmRslt).Collection(fr => fr.def_ItemResults).Load();
            return;
        }

        public void FormResultDelete(def_FormResults fr)
        {
            /*
             * *** NOTE: this code does NOT work correctly. ***
             * *** Needs to be replaced with a Stored Procedure ***
             * *** same for DeleteItemResult  ***
             */
            try
            {
                GetFormResultItemResults(fr);
                Debug.WriteLine("* * *  FormsRepository  FormResultDelete  ItemResults count: " + fr.def_ItemResults.Count.ToString());
                for (int i = 0; i < fr.def_ItemResults.Count; i++)
                {
                    def_ItemResults ir = fr.def_ItemResults.ToList()[i];
                    DeleteItemResult(ir);
                }
                // Read again to eliminate the associated Collections (doesn't seem to work)
                // def_FormResults frmRslt = GetFormResultById(fr.formResultId);
                db.def_FormResults.Remove(fr);
                Save();
            }
            catch (SqlException qex)
            {
                Debug.WriteLine("* * *  FormsRepository  FormResultDelete SqlException: " + qex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  FormsRepository  DeleteFormResult exception: " + ex.Message);
            }

            return;
        }

        public void SaveBatchResponses(Dictionary<int, Dictionary<int, string[]>> rspValsByIvByItem, int[] formResultIds)
        {
            DateTime startTime = DateTime.Now;

            try
            {
                using (formsEntities tempCtx = DataContext.GetDbContext())
                {
                    for (int i = 0; i < formResultIds.Length; i++)
                    {
                        int frId = formResultIds[i];

                        def_FormResults fr = tempCtx.def_FormResults.Where(frt => frt.formResultId == frId).SingleOrDefault();
                        foreach (int itmId in rspValsByIvByItem.Keys)
                        {
                            Dictionary<int, string[]> rspValsByIv = rspValsByIvByItem[itmId];
                            def_ItemResults ir = new def_ItemResults()
                            {
                                itemId = itmId,
                                sessionStatus = 0,
                                dateUpdated = DateTime.Now
                            };

                            foreach (int ivId in rspValsByIv.Keys)
                            {
                                string rspVal = rspValsByIv[ivId][i];
                                ir.def_ResponseVariables.Add(new def_ResponseVariables()
                                {
                                    itemVariableId = ivId,
                                    rspValue = rspVal
                                });
                            }

                            fr.def_ItemResults.Add(ir);
                        }

                        tempCtx.def_FormResults.Attach(fr);
                        tempCtx.Entry(fr).State = EntityState.Modified;
                    }
                    tempCtx.SaveChanges();
                }
            }
            catch (DbEntityValidationException dbEx)
            {
                Debug.WriteLine("* * *  FormsRepository  SaveBatchResponses DbEntityValidationException: " + dbEx.Message);
                foreach (DbEntityValidationResult devr in dbEx.EntityValidationErrors)
                {
                    foreach (DbValidationError dve in devr.ValidationErrors)
                    {
                        Debug.WriteLine("    DbEntityValidationResult: " + dve.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("* * *  FormsRepository  SaveBatchResponses exception: " + ex.Message);
            }

            Debug.WriteLine("* * * FormsRepository  SaveBatchResponses completed in " + (DateTime.Now - startTime).Ticks + " ticks");

            return;
        }

        public void SetFormResultStatus(def_FormResults frmRslt, byte newStatus )
        {
            frmRslt.formStatus = newStatus;
            frmRslt.statusChangeDate = DateTime.Now;
            db.SaveChanges();
            return;
        }


        public void FormResultDeleteLogically(int frId)
        {
            def_FormResults fr = db.def_FormResults.FirstOrDefault(x => x.formResultId == frId);
            fr.deleted = true;
            db.SaveChanges();
        }

        public void FormResultUndelete(int frId)
        {
            def_FormResults fr = db.def_FormResults.FirstOrDefault(x => x.formResultId == frId);
            fr.deleted = false;
            db.SaveChanges();
        }

        public void LockFormResult(int frId)
        {
            def_FormResults fr = db.def_FormResults.FirstOrDefault(x => x.formResultId == frId);
            fr.locked = true;
            db.SaveChanges();
        }

        public void UnlockFormResult(int frId)
        {
            def_FormResults fr = db.def_FormResults.FirstOrDefault(x => x.formResultId == frId);
            fr.locked = false;
            db.SaveChanges();
        }

        public void ArchiveFormResult(int frId)
        {
            def_FormResults fr = db.def_FormResults.FirstOrDefault(x => x.formResultId == frId);
            fr.archived = true;
            db.SaveChanges();
        }
      
        public void UnarchiveFormResult(int frId)
        {
            def_FormResults fr = db.def_FormResults.FirstOrDefault(x => x.formResultId == frId);
            fr.archived = false;
            db.SaveChanges();
        }

        public List<def_FormResults> GetFormResultsByIvIdentifierAndValue(string ivIdentifier, string rvValue)
        {
            var formResults = from fr in db.def_FormResults
                              join ir in db.def_ItemResults on fr.formResultId equals ir.formResultId
                              join rv in db.def_ResponseVariables on ir.itemResultId equals rv.itemResultId
                              join iv in db.def_ItemVariables on rv.itemVariableId equals iv.itemVariableId
                              where iv.identifier == ivIdentifier && rv.rspValue == rvValue
                              select fr;
            return formResults.ToList();
        }

        // *** RRB 2/6/16 - refactor UAS code out
        public List<def_FormResults> GetFormResultsByIvIdentifierAndValueFilterByAccess(UAS.DataDTO.LoginStatus loginStatus, string ivIdentifier, string rvValue)
        {
            List<int> authorizedGroups = loginStatus.appGroupPermissions[0].authorizedGroups;
            
            var formResults = from fr in db.def_FormResults
                              join ir in db.def_ItemResults on fr.formResultId equals ir.formResultId
                              join rv in db.def_ResponseVariables on ir.itemResultId equals rv.itemResultId
                              join iv in db.def_ItemVariables on rv.itemVariableId equals iv.itemVariableId
                              where iv.identifier == ivIdentifier && rv.rspValue == rvValue 
                              && (authorizedGroups.Contains(0) 
                                || authorizedGroups.Contains(fr.GroupID.Value)
                                || loginStatus.UserID == fr.assigned)
                              select fr;
            return formResults.ToList();
        }
    }

}
