using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace UAS.DataDTO
{
    [Serializable]
    [DataContract]
    public class SessionData
    {
        [DataMember]
        public string LoginID { get; set; }
        [DataMember]
        public string SessionID { get; set; }
        [DataMember]
        public bool SignedIn { get; set; }
        [DataMember]
        public string PermissionSet { get; set; }
    }
}
