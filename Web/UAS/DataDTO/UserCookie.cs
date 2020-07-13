using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace UAS.DataDTO
{
    [Serializable]
    [DataContract]
    public class CookieData
    {
        [DataMember]
        public string LoginID { get; set; }
    }
}
