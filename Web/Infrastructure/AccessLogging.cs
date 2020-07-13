using Data.Abstract;
using System.Collections.Generic;
using System;


namespace Assmnts.Infrastructure
{
    public static class AccessLogging
    {

        public enum accessLogFunctions
        {
            VIEW = 1,
            EDIT = 2,
            EXPORT = 3,
            IMPORT = 4,
            REPORT = 5,
            REVIEW = 6,
            CHECK_OUT = 7,
            CHECK_IN = 8,
            ARCHIVE = 9,
            DELETE = 10,
            MOVE = 11,
            UNARCHIVE = 12,
            UNDELETE = 13
        };

        public static void InsertAccessLogRecord(IFormsRepository formsRepo, int formResultId, int accessLogFunctionId, string description)
        {
            def_AccessLogging accessLogging = new def_AccessLogging()
            {
                formResultId = formResultId,
                accessLogFunctionId = accessLogFunctionId,
                accessDescription = description,
                datetimeAccessed = DateTime.Now,
                EnterpriseID = SessionHelper.LoginStatus.EnterpriseID,
                UserID = SessionHelper.LoginStatus.UserID
            };

            formsRepo.AddAccessLogging(accessLogging);
        }
        public static void InsertMultipleAccessLogRecords(IFormsRepository formsRepo, int[] formResultIds, int accessLogFunctionId, string description)
        {
            List<def_AccessLogging> records = new List<def_AccessLogging>();
            DateTime dtNow = DateTime.Now;
            foreach (int frId in formResultIds)
            {
                records.Add(new def_AccessLogging()
                {
                    formResultId = frId,
                    accessLogFunctionId = accessLogFunctionId,
                    accessDescription = description,
                    datetimeAccessed = dtNow,
                    EnterpriseID = SessionHelper.LoginStatus.EnterpriseID,
                    UserID = SessionHelper.LoginStatus.UserID
                });
            }
            formsRepo.AddMultipleAccessLoggings(records);
        }

    }
}