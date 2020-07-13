using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace UAS.DataDTO
{
    [DataContract]
    [Serializable]
    public class GroupInfo
    {
        [DataMember]
        public int EnterpriseID { get; set; }
        [DataMember]
        public int GroupID { get; set; }
        [DataMember]
        public int GroupTypeID { get; set; }
        [DataMember]
        public int ParentGroupID { get; set; }
        [DataMember]
        public string GroupName { get; set; }
        [DataMember]
        public string GroupDescription { get; set; }
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