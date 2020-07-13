using System.Collections.Generic;


namespace Assmnts.Models
{
    public class AccessLog
    {

        public List<def_AccessLogging> accessLogs  { get; set; }

        public Dictionary<int, string> enterpriseDict { get; set; }
        public Dictionary<int, string> usersDict { get; set; }
        public Dictionary<int, string> functionDict { get; set; }
        public Dictionary<int, string> recipNameDict { get; set; }
        public int count { get; set; }

        public AccessLog() {
            enterpriseDict = new Dictionary<int, string>();
            usersDict = new Dictionary<int, string>();
            functionDict = new Dictionary<int, string>();
            recipNameDict = new Dictionary<int, string>();
        }
    }
}