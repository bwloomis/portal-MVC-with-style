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
    
    public partial class def_SectionItems
    {
        public def_SectionItems()
        {
            this.def_SectionItemsEnt = new HashSet<def_SectionItemsEnt>();
        }
    
        public virtual int sectionItemId { get; set; }
        public virtual int sectionId { get; set; }
        public virtual int itemId { get; set; }
        public virtual Nullable<int> subSectionId { get; set; }
        public virtual Nullable<short> order { get; set; }
        public virtual int validation { get; set; }
        public virtual bool display { get; set; }
        public virtual bool readOnly { get; set; }
        public virtual bool requiredSection { get; set; }
        public virtual bool requiredForm { get; set; }
        public bool Cache { get; set; }
    
        public virtual def_Items def_Items { get; set; }
        public virtual ICollection<def_SectionItemsEnt> def_SectionItemsEnt { get; set; }
        public virtual def_Sections def_Sections { get; set; }
        public virtual def_SubSections def_SubSections { get; set; }
    }
}
