//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class def_Items
    {
        public def_Items()
        {
            this.def_ItemResults = new HashSet<def_ItemResults>();
            this.def_ItemVariables = new HashSet<def_ItemVariables>();
            this.def_SectionItems = new HashSet<def_SectionItems>();
            this.def_ItemsEnt = new HashSet<def_ItemsEnt>();
            this.def_ItemText = new HashSet<def_ItemText>();
        }
    
        public virtual int itemId { get; set; }
        public virtual string identifier { get; set; }
        public virtual string title { get; set; }
        public virtual string label { get; set; }
        public virtual string prompt { get; set; }
        public virtual string itemBody { get; set; }
        public virtual short langId { get; set; }
        public bool Cache { get; set; }
    
        public virtual ICollection<def_ItemResults> def_ItemResults { get; set; }
        public virtual ICollection<def_ItemVariables> def_ItemVariables { get; set; }
        public virtual ICollection<def_SectionItems> def_SectionItems { get; set; }
        public virtual def_Languages def_Languages { get; set; }
        public virtual ICollection<def_ItemsEnt> def_ItemsEnt { get; set; }
        public virtual ICollection<def_ItemText> def_ItemText { get; set; }
    }
}
