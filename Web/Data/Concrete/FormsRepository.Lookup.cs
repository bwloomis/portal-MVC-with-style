using Assmnts;

using Data.Abstract;

using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Data.Concrete
{
    public partial class FormsRepository : IFormsRepository
    {

        // =====================================================
        // Lookup Master
        // =====================================================

        public List<def_LookupMaster> GetLookupMasters()
        {
            return db.def_LookupMaster.ToList();
  
        }

        public def_LookupMaster GetLookupMasterById(int masterId)
        {
            return db.def_LookupMaster.Where(lm => lm.lookupMasterId == masterId).FirstOrDefault();
        }
             
        public def_LookupMaster GetLookupMastersByLookupCode(string lkpCd)
        {
            lkpCd = lkpCd.ToUpper();
            return db.def_LookupMaster.Where(lm => lm.lookupCode.ToUpper().Equals(lkpCd) ).FirstOrDefault();
        }

        public def_LookupMaster SetLookupMaster(string lkpCd, string title, int baseType, def_LookupMaster lkpMstr = null)
        {
            if(lkpMstr == null)
            {
                lkpMstr = new def_LookupMaster();

                lkpMstr.lookupCode = lkpCd;
                lkpMstr.title = title;
                lkpMstr.baseTypeId = (short)baseType;


                db.def_LookupMaster.Add(lkpMstr);
            }
            else
            {
                lkpMstr.lookupCode = lkpCd;
                lkpMstr.title = title;
                lkpMstr.baseTypeId = (short)baseType;

                db.Entry(lkpMstr).State = EntityState.Modified;

            }

            db.SaveChanges();
            return lkpMstr;

        }

        public void DeleteLookupMaster(def_LookupMaster lkpMstr)
        {
            db.def_LookupMaster.Remove(lkpMstr);
            db.SaveChanges();
        }
        
        // =====================================================
        // Lookup Detail
        // =====================================================

        public List<def_LookupDetail> GetLookupDetails(int lkpMstrId, int entId, int grpId)
        {
            return db.def_LookupDetail.Where(ld => (ld.lookupMasterId == lkpMstrId) &&
                                                   (ld.EnterpriseID == entId) &&
                                                   ld.GroupID == grpId).OrderBy(ld => ld.displayOrder).ToList();
        }

        public List<def_LookupDetail> GetLookupDetailsByLookupMasterEnterprise(int lkpMstrId, int entId)
        {
            return db.def_LookupDetail.Where(ld => (ld.lookupMasterId == lkpMstrId) &&
                                       (ld.EnterpriseID == entId)).OrderBy(ld => ld.displayOrder).ToList();
        }

        /*
        public List<def_LookupDetail> GetLookupDetails(int lkpMstrId)
        {
            return db.def_LookupDetail.Where(ld => (ld.lookupMasterId == lkpMstrId)).OrderBy(ld => ld.displayOrder).ToList();
        }
		*/

        public def_LookupDetail GetLookupDetailById(int detailId)
        {
            return db.def_LookupDetail.Where(ld => ld.lookupDetailId == detailId).FirstOrDefault();
        }

        public def_LookupDetail GetLookupDetailByEnterpriseMasterAndDataValue(int enterpriseId, int lookupMasterId, string dataValue)
        {
            return db.def_LookupDetail.Where(d => d.EnterpriseID == enterpriseId && d.lookupMasterId == lookupMasterId && d.dataValue == dataValue).FirstOrDefault();
        }
        
        public def_LookupDetail SetLookupDetail(string dataValue, int mstrId, int? entId, int? grpId, int dsplyOrdr, def_LookupDetail lkpDtl = null)
        {
            if (lkpDtl == null)
            {
                lkpDtl = new def_LookupDetail();

                lkpDtl.lookupMasterId = mstrId;
                lkpDtl.dataValue = dataValue;
                lkpDtl.EnterpriseID = entId;
                lkpDtl.GroupID = grpId;
                lkpDtl.displayOrder = dsplyOrdr;
                lkpDtl.StatusFlag = "A";

                db.def_LookupDetail.Add(lkpDtl);
            }
            else
            {
                lkpDtl.lookupMasterId = mstrId;
                lkpDtl.dataValue = dataValue;
                lkpDtl.EnterpriseID = entId;
                lkpDtl.GroupID = grpId;
                lkpDtl.displayOrder = dsplyOrdr;
                lkpDtl.StatusFlag = "A";

                db.Entry(lkpDtl).State = EntityState.Modified;
            }

            db.SaveChanges();

            return lkpDtl;
        }

        public void DeleteLookupDetail(def_LookupDetail lkpDtl)
        {
            db.def_LookupDetail.Remove(lkpDtl);
            db.SaveChanges();
        }


        public void AddLookupDetail(def_LookupDetail lkpDtl)
        {
            db.def_LookupDetail.Add(lkpDtl);

            db.SaveChanges();
        }

        public void SaveLookupDetail(def_LookupDetail lkpDtl)
        {
            db.Entry(lkpDtl).State = EntityState.Modified;

            db.SaveChanges();
        }


        // =====================================================
        // Lookup Text
        // =====================================================

        public List<def_LookupText> GetLookupTextsByLookupDetail(int lkpDtlId)
        {
            return db.def_LookupText.Where(lt => lt.lookupDetailId == lkpDtlId).ToList();
        }

        public List<def_LookupText> GetLookupTextsByLookupDetailLanguage(int lkpDtlId, int langId)
        {
            return db.def_LookupText.Where(lt => (lt.lookupDetailId == lkpDtlId) && 
                                                 (lt.langId == langId)).ToList();
        }

        public def_LookupText GetLookupTextById(int textId) 
        {
            return db.def_LookupText.Where(lt => lt.lookupTextId == textId).FirstOrDefault();
        }

        public def_LookupText GetLookupTextByDisplayTextEnterpriseIdMasterLang(string displayText, int enterpriseId, int lookupMasterId, int langId)
        {
            def_LookupDetail lookupDetail = null;

            List<def_LookupDetail> lookupDetails = db.def_LookupDetail.Where(ld => (ld.EnterpriseID == enterpriseId) &&
                                                                                   (ld.lookupMasterId == lookupMasterId)).ToList();

            def_LookupText lookupText = null;

            List<def_LookupText> lookupTexts = db.def_LookupText.Where(lt => lt.displayText == displayText && lt.langId == langId).ToList();

            List<int> lookupDetailIds = new List<int>();

            lookupDetailIds = lookupDetails.Select(ld => ld.lookupDetailId).ToList();

            lookupText = lookupTexts.Where(lt => lookupDetailIds.Contains(lt.lookupDetailId)).FirstOrDefault();

            return lookupText;
        }
    
        public def_LookupText SetLookupText(int detailId, int lang, string displayText, def_LookupText lkpTxt = null)
        {
            if (lkpTxt == null)
            {
                lkpTxt = new def_LookupText();

                lkpTxt.lookupDetailId = detailId;
                lkpTxt.langId = (short)lang;
                lkpTxt.displayText = displayText;

                db.def_LookupText.Add(lkpTxt);
            }
            else
            {
                lkpTxt.lookupDetailId = detailId;
                lkpTxt.langId = (short)lang;
                lkpTxt.displayText = displayText;

                db.Entry(lkpTxt).State = EntityState.Modified;

            }

            db.SaveChanges();

            return lkpTxt;
        }

        public string GetLookupTextByIdentifierDisplyOrderLang(string identifier, int lang, int displayOrder)
        {
            string text = null;

            var queryResult = (from d in db.def_LookupDetail
                     join m in db.def_LookupMaster on d.lookupMasterId equals m.lookupMasterId
                     join t in db.def_LookupText on d.lookupDetailId equals t.lookupDetailId
                     where m.lookupCode == identifier
                       && t.langId == lang
                       && d.displayOrder == displayOrder
                     select t.displayText).FirstOrDefault();

            text = queryResult != null ? queryResult.ToString() : null;

            return text;

        }
		
        public void DeleteLookupText(def_LookupText lkpTxt)
        {
            db.def_LookupText.Remove(lkpTxt);
            db.SaveChanges();
        }

        public void AddLookupText(def_LookupText lkpTxt)
        {
            db.def_LookupText.Add(lkpTxt);

            db.SaveChanges();
        }

        public void SaveLookupText(def_LookupText lkpTxt)
        {
            db.Entry(lkpTxt).State = EntityState.Modified;

            db.SaveChanges();
        }
        
    }

}
