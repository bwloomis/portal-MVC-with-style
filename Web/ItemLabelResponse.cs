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
    
    public partial class ItemLabelResponse
    {
        public int Item_itemId { get; set; }
        public string Item_identifier { get; set; }
        public string Item_title { get; set; }
        public string Item_label { get; set; }
        public string Item_prompt { get; set; }
        public string Item_itemBody { get; set; }
        public short Item_langId { get; set; }
        public Nullable<int> ItemResult_itemResultId { get; set; }
        public Nullable<int> ItemResult_formResultId { get; set; }
        public Nullable<int> ItemResult_itemId { get; set; }
        public Nullable<int> ItemResult_sessionStatus { get; set; }
        public Nullable<System.DateTime> ItemResult_dateUpdated { get; set; }
        public Nullable<int> ItemVariable_itemVariableId { get; set; }
        public Nullable<int> ItemVariable_itemId { get; set; }
        public string ItemVariable_identifier { get; set; }
        public Nullable<short> ItemVariable_baseTypeId { get; set; }
        public string ItemVariable_defaultValue { get; set; }
        public Nullable<int> ItemVariable_outcomeDeclarationId { get; set; }
        public Nullable<int> ResponseVariable_responseVariableId { get; set; }
        public Nullable<int> ResponseVariable_itemResultId { get; set; }
        public Nullable<int> ResponseVariable_itemVariableId { get; set; }
        public Nullable<int> ResponseVariable_rspInt { get; set; }
        public Nullable<double> ResponseVariable_rspFloat { get; set; }
        public Nullable<System.DateTime> ResponseVariable_rspDate { get; set; }
        public string ResponseVariable_rspValue { get; set; }
    }
}
