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
    
    public partial class def_SectionText
    {
        public virtual int sctnTextId { get; set; }
        public virtual int sectionId { get; set; }
        public virtual int EnterpriseID { get; set; }
        public virtual short langId { get; set; }
        public virtual string sctnTitle { get; set; }
        public bool Cache { get; set; }
    
        public virtual def_Languages def_Languages { get; set; }
        public virtual def_Sections def_Sections { get; set; }
    }
}
