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
    
    public partial class formsEntities : DbContext
    {
        public formsEntities()
            : base("name=formsEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<def_BaseTypes> def_BaseTypes { get; set; }
        public virtual DbSet<def_BranchRules> def_BranchRules { get; set; }
        public virtual DbSet<def_FormParts> def_FormParts { get; set; }
        public virtual DbSet<def_FormResults> def_FormResults { get; set; }
        public virtual DbSet<def_Forms> def_Forms { get; set; }
        public virtual DbSet<def_ItemResults> def_ItemResults { get; set; }
        public virtual DbSet<def_Items> def_Items { get; set; }
        public virtual DbSet<def_ItemsEnt> def_ItemsEnt { get; set; }
        public virtual DbSet<def_ItemVariables> def_ItemVariables { get; set; }
        public virtual DbSet<def_Languages> def_Languages { get; set; }
        public virtual DbSet<def_OutcomeDeclaration> def_OutcomeDeclaration { get; set; }
        public virtual DbSet<def_Parts> def_Parts { get; set; }
        public virtual DbSet<def_PartSections> def_PartSections { get; set; }
        public virtual DbSet<def_ResponseVariables> def_ResponseVariables { get; set; }
        public virtual DbSet<def_SectionItems> def_SectionItems { get; set; }
        public virtual DbSet<def_SectionItemsEnt> def_SectionItemsEnt { get; set; }
        public virtual DbSet<def_Sections> def_Sections { get; set; }
        public virtual DbSet<def_SubSections> def_SubSections { get; set; }
        public virtual DbSet<def_LookupDetail> def_LookupDetail { get; set; }
        public virtual DbSet<def_LookupMaster> def_LookupMaster { get; set; }
        public virtual DbSet<def_LookupText> def_LookupText { get; set; }
        public virtual DbSet<def_AccessLogFunctions> def_AccessLogFunctions { get; set; }
        public virtual DbSet<def_AccessLogging> def_AccessLogging { get; set; }
        public virtual DbSet<vFormResultUser> vFormResultUsers { get; set; }
        public virtual DbSet<def_StatusDetail> def_StatusDetail { get; set; }
        public virtual DbSet<def_StatusFlow> def_StatusFlow { get; set; }
        public virtual DbSet<def_StatusLog> def_StatusLog { get; set; }
        public virtual DbSet<def_StatusMaster> def_StatusMaster { get; set; }
        public virtual DbSet<def_StatusText> def_StatusText { get; set; }
        public virtual DbSet<def_PartSectionsEnt> def_PartSectionsEnt { get; set; }
        public virtual DbSet<def_WebServiceActivity> def_WebServiceActivity { get; set; }
        public virtual DbSet<def_WebServiceActivityFunctions> def_WebServiceActivityFunctions { get; set; }
        public virtual DbSet<def_FormText> def_FormText { get; set; }
        public virtual DbSet<def_ItemText> def_ItemText { get; set; }
        public virtual DbSet<def_PartText> def_PartText { get; set; }
        public virtual DbSet<def_SectionText> def_SectionText { get; set; }
        public virtual DbSet<def_AttachType> def_AttachType { get; set; }
        public virtual DbSet<def_FileAttachment> def_FileAttachment { get; set; }
        public virtual DbSet<def_RelatedEnum> def_RelatedEnum { get; set; }
        public virtual DbSet<PovertyGuideline> PovertyGuidelines { get; set; }
        public virtual DbSet<def_FormVariants> def_FormVariants { get; set; }
    
        public virtual ObjectResult<def_Items> def_sp_GetItemListFromSection(Nullable<int> sectionId, Nullable<bool> onlyNotes)
        {
            var sectionIdParameter = sectionId.HasValue ?
                new ObjectParameter("sectionId", sectionId) :
                new ObjectParameter("sectionId", typeof(int));
    
            var onlyNotesParameter = onlyNotes.HasValue ?
                new ObjectParameter("onlyNotes", onlyNotes) :
                new ObjectParameter("onlyNotes", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<def_Items>("def_sp_GetItemListFromSection", sectionIdParameter, onlyNotesParameter);
        }
    
        public virtual ObjectResult<def_Items> def_sp_GetItemListFromSection(Nullable<int> sectionId, Nullable<bool> onlyNotes, MergeOption mergeOption)
        {
            var sectionIdParameter = sectionId.HasValue ?
                new ObjectParameter("sectionId", sectionId) :
                new ObjectParameter("sectionId", typeof(int));
    
            var onlyNotesParameter = onlyNotes.HasValue ?
                new ObjectParameter("onlyNotes", onlyNotes) :
                new ObjectParameter("onlyNotes", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<def_Items>("def_sp_GetItemListFromSection", mergeOption, sectionIdParameter, onlyNotesParameter);
        }
    
        public virtual ObjectResult<ItemLabelResponse> sp_GetItemLabelsResponses(Nullable<int> formResultId, string itemIdsCsv)
        {
            var formResultIdParameter = formResultId.HasValue ?
                new ObjectParameter("formResultId", formResultId) :
                new ObjectParameter("formResultId", typeof(int));
    
            var itemIdsCsvParameter = itemIdsCsv != null ?
                new ObjectParameter("itemIdsCsv", itemIdsCsv) :
                new ObjectParameter("itemIdsCsv", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<ItemLabelResponse>("sp_GetItemLabelsResponses", formResultIdParameter, itemIdsCsvParameter);
        }
    }
}
