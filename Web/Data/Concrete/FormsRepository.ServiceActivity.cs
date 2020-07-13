using Assmnts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Diagnostics;
using Data.Abstract;

using System.Linq;

namespace Data.Concrete
{
    public partial class FormsRepository : IFormsRepository
    {
        public def_WebServiceActivity GetWebServiceActivityById(int webServiceActivityId)
        {
            return db.def_WebServiceActivity.Where(wsa => wsa.RequestID == webServiceActivityId).FirstOrDefault();
        }

        public int AddWebServiceActivity(def_WebServiceActivity webServiceActivity)
        {
            db.def_WebServiceActivity.Add(webServiceActivity);
            db.SaveChanges();

            return webServiceActivity.RequestID;
        }

        public List<def_WebServiceActivity> GetUnprocessedWebServiceActivityRequestsByEntId(int entId)
        {
            return db.def_WebServiceActivity.Where(wsa => wsa.EnterpriseID == entId && wsa.DateTimeServiced == null && wsa.SentBy.ToLower() == "webservice").ToList();
        }


        public void SaveWebServiceActivity(def_WebServiceActivity wsa)
        {
            try
            {
                db.def_WebServiceActivity.Attach(wsa);
                db.Entry(wsa).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (DbEntityValidationException dbEx)
            {
                Debug.WriteLine("* * *  FormsRepository  SaveWebServiceActivity DbEntityValidationException: " + dbEx.Message);
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
                Debug.WriteLine("* * *  FormsRepository  Save WebServiceActivity exception: " + ex.Message);
            }

            return;
        }
    }
}