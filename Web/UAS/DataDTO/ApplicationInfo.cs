using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
namespace UAS.DataDTO
{
    [DataContract]
    [Serializable]
    public class ApplicationInfo
    {
        [DataMember]
        public int ApplicationID { get; set; }
        [DataMember]
        public int EnterpriseID { get; set; }
        [DataMember]
        public int? RangeID { get; set; }
        [DataMember]
        public string ApplicationName { get; set; }
        [DataMember]
        public DateTime CreatedDate { get; set; }
        [DataMember]
        public int CreatedBy { get; set; }
        [DataMember]
        public DateTime ModifiedDate { get; set; }
        [DataMember]
        public int ModifiedBy { get; set; }
        [DataMember]
        public string DefaultSet { get; set; }
        [DataMember]
        public int? SortOrder { get; set; }
        [DataMember]
        public char StatusFlag { get; set; }
        [DataMember]
        public string StatusText { get; set; }
        [DataMember]
        public string Params { get; set; }
        [DataMember]
        public bool? ParamsModified { get; set; }
    }
}
