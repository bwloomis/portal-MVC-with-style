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
    
    public partial class UASEntities : DbContext
    {
        public UASEntities()
            : base("name=UASEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<uas_Config> uas_Config { get; set; }
        public virtual DbSet<uas_Enterprise> uas_Enterprise { get; set; }
        public virtual DbSet<uas_Group> uas_Group { get; set; }
        public virtual DbSet<uas_GroupUserAppPermissions> uas_GroupUserAppPermissions { get; set; }
        public virtual DbSet<uas_ProfileConfig> uas_ProfileConfig { get; set; }
        public virtual DbSet<uas_Role> uas_Role { get; set; }
        public virtual DbSet<uas_RoleAppPermissions> uas_RoleAppPermissions { get; set; }
        public virtual DbSet<uas_SecretQuestion> uas_SecretQuestion { get; set; }
        public virtual DbSet<uas_SecretQuestionAnswer> uas_SecretQuestionAnswer { get; set; }
        public virtual DbSet<uas_StatusFlagType> uas_StatusFlagType { get; set; }
        public virtual DbSet<uas_User> uas_User { get; set; }
        public virtual DbSet<uas_UserAddress> uas_UserAddress { get; set; }
        public virtual DbSet<uas_UserEmail> uas_UserEmail { get; set; }
        public virtual DbSet<uas_UserPhone> uas_UserPhone { get; set; }
        public virtual DbSet<uas_UserProfile> uas_UserProfile { get; set; }
        public virtual DbSet<uas_EntAppConfig> uas_EntAppConfig { get; set; }
        public virtual DbSet<uas_ConfirmationQueue> uas_ConfirmationQueue { get; set; }
        public virtual DbSet<uas_EmailQueue> uas_EmailQueue { get; set; }
        public virtual DbSet<uas_EntConfig> uas_EntConfig { get; set; }
        public virtual DbSet<uas_EntGrpAddress> uas_EntGrpAddress { get; set; }
        public virtual DbSet<uas_EntGrpEmail> uas_EntGrpEmail { get; set; }
        public virtual DbSet<uas_EntGrpPhone> uas_EntGrpPhone { get; set; }
        public virtual DbSet<uas_GroupType> uas_GroupType { get; set; }
        public virtual DbSet<uas_License> uas_License { get; set; }
        public virtual DbSet<uas_PasswordRestriction> uas_PasswordRestriction { get; set; }
        public virtual DbSet<uas_UserLoginActivity> uas_UserLoginActivity { get; set; }
        public virtual DbSet<vEnrollmentSite> vEnrollmentSites { get; set; }
    }
}