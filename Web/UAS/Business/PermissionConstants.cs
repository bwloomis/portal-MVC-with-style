
namespace UAS.Business
{
    public class PermissionConstants
    {
        public const string ASSMNTS = "assmnts";
        public const string INTEGRATION = "integration";
				public const string REPORTEXP = "RptsExpts";
        
        public const int EDIT = 0;
        public const int CREATE = 1;
        public const int DELETE = 2;
        public const int ARCHIVE = 3;
        public const int MOVE = 4;
        public const int UNLOCK = 5;
        public const int UNDELETE = 7;
        public const int ASSIGNED = 6;
        public const int APPROVE = 9;
        public const int EDIT_LOCKED = 8;
        //$$$ Start - Added for Contractor readonly role
        public const int CNGE_STATUS = 10;
        public const int SHOW_NP = 11;
        public const int ONLY_ADAP = 12;
        //$$$ End - Added for Contractor readonly role
        public const int PATT_CHECK = 9;
        public const int GROUP_WIDE = 10;

        public const int WS_ACCESS = 0;
        public const int VENTURE = 1;

        public const int FAMREP = 0;
        public const int GENREP = 1;
        public const int EXPORT = 2;

				public const int ADAP_STAFF = 0;
    }
}
