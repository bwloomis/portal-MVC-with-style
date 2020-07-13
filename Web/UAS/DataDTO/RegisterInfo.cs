using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace UAS.DataDTO
{
    [DataContract]
    [Serializable]
    public class RegisterInfo
    {
        [DataMember]
        public List<EnterpriseInfo> Enterprises { get; set; }
        [DataMember]
        public string GroupLabel { get; set; }
        [DataMember]
        public List<GroupInfo> Groups { get; set; }
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
        [DataMember]
        public List<string> Prefix { get; set; }
        [DataMember]
        public string MiddleName { get; set; }
        [DataMember]
        public string LoginID { get; set; }
        [DataMember]
        public string DomainID { get; set; }
        [DataMember]
        public string Pwd { get; set; }
        [DataMember]
        public string CnfPwd { get; set; }
        [DataMember]
        public string UserEmail { get; set; }
        [DataMember]
        public string UserPhone { get; set; }
        [DataMember]
        public List<SecurityQuestionInfo> SecurityQuestionList { get; set; }
    }
}