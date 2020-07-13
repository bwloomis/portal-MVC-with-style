using System;
using System.Runtime.Serialization;

namespace UAS.DataDTO
{
    [DataContract]
    [Serializable]
    public class LoginInfo
    {
        [DataMember]
        public bool IsLoggedIn { get; set; }
        [DataMember]
        public string LoginID { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public string CnfPassword { get; set; }
        [DataMember]
        public bool RememberMe { get; set; }
        [DataMember]
        public string CookieData { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public string SessionData { get; set; }
        [DataMember]
        public string Domain { get; set; }
        [DataMember]
        public bool SecureDomain { get; set; }
        [DataMember]
        public bool ISCRS { get; set; }
        [DataMember]
        public string TimeStamp { get; set; }
        [DataMember]
        public string Signature { get; set; }
    }
}