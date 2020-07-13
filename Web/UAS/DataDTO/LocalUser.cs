using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace UAS.DataDTO
{
    [Serializable]
    [DataContract]
    public class LocalUser
    {
        [DataMember]
        public int UserID { get; set; }
        [DataMember]
        public string Document { get; set; }
        [DataMember]
        public DateTime CreatedDate { get; set; }
        [DataMember]
        public int CreatedByID { get; set; }
        [DataMember]
        public DateTime ModifiedDate { get; set; }
        [DataMember]
        public int ModifiedByID { get; set; }
        [DataMember]
        public int? SortOrder { get; set; }
        [DataMember]
        public string StatusFlag { get; set; }
    }
}
