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
    
    public partial class def_ResponseVariables
    {
        public int responseVariableId { get; set; }
        public int itemResultId { get; set; }
        public int itemVariableId { get; set; }
        public Nullable<int> rspInt { get; set; }
        public Nullable<double> rspFloat { get; set; }
        public Nullable<System.DateTime> rspDate { get; set; }
        public string rspValue { get; set; }
    
        public virtual def_ItemResults def_ItemResults { get; set; }
        public virtual def_ItemVariables def_ItemVariables { get; set; }
    }
}
