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
    
    public partial class def_LookupText
    {
        public virtual int lookupTextId { get; set; }
        public virtual int lookupDetailId { get; set; }
        public virtual short langId { get; set; }
        public virtual string displayText { get; set; }
        public bool Cache { get; set; }
    
        public virtual def_Languages def_Languages { get; set; }
        public virtual def_LookupDetail def_LookupDetail { get; set; }
    }
}
