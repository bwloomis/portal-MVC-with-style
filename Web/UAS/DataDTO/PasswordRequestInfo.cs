using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace UAS.DataDTO
{
    [DataContract]
    [Serializable]
    public class PasswordRequestInfo
    {
        [DataMember]
        public string UserEmail { get; set; }
        [DataMember]
        public List<SecurityQuestionInfo> SecurityQuestionList { get; set; }
    }
}