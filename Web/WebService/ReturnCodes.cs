using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebService
{
    public static class ReturnCodes
    {
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
            RC_InvalidSisId,
            RC_InvalidEntId
        }
    }
}