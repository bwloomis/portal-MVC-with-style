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
    
    public partial class def_ItemText
    {
        public virtual int itemTextId { get; set; }
        public virtual int itemId { get; set; }
        public virtual int EnterpriseID { get; set; }
        public virtual short langId { get; set; }
        public virtual string title { get; set; }
        public virtual string label { get; set; }
        public virtual string prompt { get; set; }
        public virtual string itemBody { get; set; }
        public bool Cache { get; set; }
    
        public virtual def_Items def_Items { get; set; }
        public virtual def_Languages def_Languages { get; set; }
    }
}