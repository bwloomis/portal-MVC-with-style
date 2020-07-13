using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebService
{
    public static class WSConstants
    {
        public const string TRACKING_NUMBER = "sis_track_num";
        
        public enum FR_formStatus
        {
            // Most of these were taken from the SIS Status table.

            // Actual statuses were coming from vSISAssesmentForSearch view. Updated to use these. 
            //--LK 2/24/2015
            NEW = 0,
            IN_PROGRESS = 1,
            COMPLETED = 2,
            ABANDONED = 3,
            UPLOADED = 4
        }

        
        
        public enum FORM_STATUS
        {
            SSUnknown = 0,
            SSInprogress,
            SSCompleted,
            SSDeleted,
            SSArchived,
            SSAbandoned,
            SSNew,
            SSCompletedLocked,
            SSInvalid
        }

        public enum RETURN_CODE
        {
            RC_Success = 0,
            RC_GeneralError,
            RC_InvalidUser,
            RC_MalformedXml,
            RC_InvalidXML,
            RC_XmlError,
            RC_ImportError,
            RC_NotSupported,
            RC_NotComplete,
            RC_Obsolete,
            RC_InvalidIP,
            RC_InvalidFormResultId,
            RC_InvalidEntId,
            RC_NoResults
        }

        public const string USERS = "users";

        public enum USER_PERMISSIONS
        {
            Add = 0,
            Edit,
            View,
            CustomizePermissions
        }
    }
}