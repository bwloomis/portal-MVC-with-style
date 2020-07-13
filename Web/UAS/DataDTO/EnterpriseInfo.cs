using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace UAS.DataDTO
{
    [DataContract]
    [Serializable]
    public class EnterpriseInfo
    {
        [DataMember]
        public int EnterpriseID { get; set; }
        [DataMember]
        public string EnterpriseName { get; set; }
        [DataMember]
        public string EnterpriseDescription { get; set; }
        [DataMember]
        public string ConfigParams { get; set; }
        [DataMember]
        public string Notes { get; set; }
        [DataMember]
        public DateTime CreatedDate { get; set; }
        [DataMember]
        public int CreatedByID { get; set; }
        [DataMember]
        public DateTime ModifiedDate { get; set; }
        [DataMember]
        public int ModifiedByID { get; set; }
        [DataMember]
        public int SortOrder { get; set; }
        [DataMember]
        public string StatusFlag { get; set; }
    }
}