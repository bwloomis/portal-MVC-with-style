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
    
    public partial class def_AccessLogFunctions
    {
        public def_AccessLogFunctions()
        {
            this.def_AccessLogging = new HashSet<def_AccessLogging>();
        }
    
        public int accessLogFunctionId { get; set; }
        public string accessLogFunctionName { get; set; }
    
        public virtual ICollection<def_AccessLogging> def_AccessLogging { get; set; }
    }
}
