using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace UAS.DataDTO
{
    [DataContract]
    [Serializable]
    public class SecurityQuestionInfo
    {
        [DataMember]
        public int QuestionID { get; set; }
        [DataMember]
        public string Question { get; set; }
        [DataMember]
        public string Answer { get; set; }
    }
}