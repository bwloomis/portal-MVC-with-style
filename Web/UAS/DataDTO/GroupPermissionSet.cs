using System;
using System.Runtime.Serialization;

namespace UAS.DataDTO
{
    [Serializable]
    [DataContract]
    public class GroupPermissionSet
    {
        [DataMember]
        public int GroupID { get; set; }
		[DataMember]
        public string PermissionSet { get; set; }
    }
}
