using Assmnts;
using Data.Abstract;
using System.Collections.Generic;
using System.Linq;

namespace Data.Concrete
{
    public partial class FormsRepository : IFormsRepository
    {

        // ==========================================================
        // Read the vFormResultUser (links 'subject' to the uas_User)
        // ==========================================================


        public IQueryable<vFormResultUser> GetFormResultsWithSubjects(int entId, int formId/*, int pageStart = 0, int pageSize = 10*/)
        {
            return GetFormResultsWithSubjects(entId, new int[] { formId });
        }

        public IQueryable<vFormResultUser> GetFormResultsWithSubjects(int entId, IEnumerable<int> formIds/*, int pageStart = 0, int pageSize = 10*/)
        {
            string sortBy = "LastName";
            IQueryable<vFormResultUser> results = (from v in db.vFormResultUsers
                                                   where (v.EnterpriseID == entId) && (formIds.Contains(v.formId))
                                                   orderby (sortBy)
                                                   select v);
            //.Skip(pageStart)
            //.Take(pageSize);

            return results;

        }

        public IQueryable<vFormResultUser> GetFormResultsWithSubjects(int entId, int subject, IEnumerable<int> formIds/*, int pageStart = 0, int pageSize = 10*/)
        {
            string sortBy = "LastName";
            IQueryable<vFormResultUser> results = (from v in db.vFormResultUsers
                                                   where (v.EnterpriseID == entId) && (v.subject == subject) && (formIds.Contains(v.formId))
                                                   orderby (sortBy)
                                                   select v);
            //.Skip(pageStart)
            //.Take(pageSize);

            return results;

        }

        public IQueryable<vFormResultUser> GetFormResultsWithSubjects(int entId, List<int?> groupIds, IEnumerable<int> formIds/*, int pageStart = 0, int pageSize = 10*/)
        {
            string sortBy = "LastName";
            IQueryable<vFormResultUser> results = (from v in db.vFormResultUsers
                                                   where (v.EnterpriseID == entId) && (groupIds.Contains(v.GroupID)) && (formIds.Contains(v.formId))
                                                   orderby (sortBy)
                                                   select v);
            //.Skip(pageStart)
            //.Take(pageSize);

            return results;

        }

        public IQueryable<string[]> GetUserContactInfo(int formResultID)
        {
            IQueryable<string[]> results = null;
            //IQueryable<String[]> results = (from vf in db.vFormResultUsers
            //                                join a in db.uas_UserAddress on vf.subject equals a.UserID into ag
            //                                from alj in
            //                                    (from g in ag
            //                                     select new
            //                                     {
            //                                         g.UserID,
            //                                         g.Address1,
            //                                         g.Address2,
            //                                         g.City,
            //                                         g.PostalCode,
            //                                         g.StateProvince,
            //                                         g.SortOrder,
            //                                         g.UserAddressID
            //                                     }
            //                                    ).DefaultIfEmpty()
            //                                join e in db.uas_UserEMail on vf.subject equals e.UserID into eg
            //                                from elj in
            //                                    (from g in eg
            //                                     select new
            //                                     {
            //                                         g.UserID,
            //                                         g.EmailAddress,
            //                                         g.SortOrder,
            //                                         g.UserEmailID
            //                                     }
            //                                    ).DefaultIfEmpty()
            //                                join p in db.uas_UserPhone on vf.subject equals p.UserID into pg
            //                                from plj in
            //                                    (from g in pg
            //                                     select new
            //                                     {
            //                                         g.UserID,
            //                                         g.PhoneNumber,
            //                                         g.SortOrder,
            //                                         g.UserPhoneID
            //                                     }
            //                                    ).DefaultIfEmpty()
            //                                join c in db.uas_UserPhone on vf.subject equals c.UserID into cg
            //                                from clj in
            //                                    (from g in cg
            //                                     select new
            //                                     {
            //                                         g.UserID,
            //                                         g.PhoneNumber,
            //                                         g.SortOrder,
            //                                         g.UserPhoneID
            //                                     }
            //                                    ).DefaultIfEmpty()
            //                                where (vf.formResultId == formResultID
            //                                && (alj.SortOrder == null || alj.SortOrder == ag.Min(x => x.SortOrder))
            //                                && (alj.UserAddressID == null || alj.UserAddressID == ag.Min(x => x.UserAddressID)) // to pull only 1 row
            //                                && (elj.SortOrder == null || elj.SortOrder == eg.Min(x => x.SortOrder))
            //                                && (elj.UserEmailID == null || elj.UserEmailID == eg.Min(x => x.UserEmailID)) // to pull only 1 row
            //                                && (plj.SortOrder == null || plj.SortOrder == 1)
            //                                && (plj.UserPhoneID == null || plj.UserPhoneID == pg.Min(x => x.UserPhoneID)) // to pull only 1 row
            //                                && (clj.SortOrder == null || clj.SortOrder == 2)
            //                                && (clj.UserPhoneID == null || clj.UserPhoneID == cg.Min(x => x.UserPhoneID))) // to pull only 1 row
            //                                select new string[]
            //                                    {
            //                                        vf.FirstName,
            //                                        vf.LastName,
            //                                        plj.PhoneNumber,
            //                                        clj.PhoneNumber,
            //                                        alj.Address1,
            //                                        alj.Address2,
            //                                        alj.City,
            //                                        alj.StateProvince,
            //                                        alj.PostalCode,
            //                                        elj.EmailAddress,
            //                                        vf.GroupName
            //                                    });
            return results;

        }


    }

}
