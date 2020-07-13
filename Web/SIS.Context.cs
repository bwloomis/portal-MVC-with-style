﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Assmnts
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class SISEntities : DbContext
    {
        public SISEntities()
            : base("name=SISEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Address> Addresses { get; set; }
        public virtual DbSet<Contact> Contacts { get; set; }
        public virtual DbSet<Recipient> Recipients { get; set; }
        public virtual DbSet<vSisDefRaw> vSisDefRaws { get; set; }
        public virtual DbSet<vSISAssessmentsForSearch> vSISAssessmentsForSearches { get; set; }
        public virtual DbSet<vSearchGrid> vSearchGrids { get; set; }
        public virtual DbSet<PatternCheck> PatternChecks { get; set; }
        public virtual DbSet<PatternCheckItem> PatternCheckItems { get; set; }
        public virtual DbSet<def_FormResults> def_FormResults { get; set; }
    
        public virtual ObjectResult<spSearchGrid_Result> spSearchGrid(Nullable<bool> getAllRecords, Nullable<bool> getAllAssessments, Nullable<bool> detailSearch, string ids, Nullable<int> ent_id, Nullable<int> sub_id, string searchWords, string searchNumbers, string searchDates)
        {
            var getAllRecordsParameter = getAllRecords.HasValue ?
                new ObjectParameter("getAllRecords", getAllRecords) :
                new ObjectParameter("getAllRecords", typeof(bool));
    
            var getAllAssessmentsParameter = getAllAssessments.HasValue ?
                new ObjectParameter("getAllAssessments", getAllAssessments) :
                new ObjectParameter("getAllAssessments", typeof(bool));
    
            var detailSearchParameter = detailSearch.HasValue ?
                new ObjectParameter("detailSearch", detailSearch) :
                new ObjectParameter("detailSearch", typeof(bool));
    
            var idsParameter = ids != null ?
                new ObjectParameter("ids", ids) :
                new ObjectParameter("ids", typeof(string));
    
            var ent_idParameter = ent_id.HasValue ?
                new ObjectParameter("ent_id", ent_id) :
                new ObjectParameter("ent_id", typeof(int));
    
            var sub_idParameter = sub_id.HasValue ?
                new ObjectParameter("sub_id", sub_id) :
                new ObjectParameter("sub_id", typeof(int));
    
            var searchWordsParameter = searchWords != null ?
                new ObjectParameter("searchWords", searchWords) :
                new ObjectParameter("searchWords", typeof(string));
    
            var searchNumbersParameter = searchNumbers != null ?
                new ObjectParameter("searchNumbers", searchNumbers) :
                new ObjectParameter("searchNumbers", typeof(string));
    
            var searchDatesParameter = searchDates != null ?
                new ObjectParameter("searchDates", searchDates) :
                new ObjectParameter("searchDates", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<spSearchGrid_Result>("spSearchGrid", getAllRecordsParameter, getAllAssessmentsParameter, detailSearchParameter, idsParameter, ent_idParameter, sub_idParameter, searchWordsParameter, searchNumbersParameter, searchDatesParameter);
        }
    }
}
