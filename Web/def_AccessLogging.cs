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
    
    public partial class def_AccessLogging
    {
        public int accessLoggingId { get; set; }
        public int EnterpriseID { get; set; }
        public int formResultId { get; set; }
        public int UserID { get; set; }
        public System.DateTime datetimeAccessed { get; set; }
        public int accessLogFunctionId { get; set; }
        public string accessDescription { get; set; }
    
        public virtual def_AccessLogFunctions def_AccessLogFunctions { get; set; }
        public virtual def_FormResults def_FormResults { get; set; }
    }
}
