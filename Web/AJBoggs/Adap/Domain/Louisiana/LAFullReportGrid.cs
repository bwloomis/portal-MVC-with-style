using Assmnts;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AJBoggs.Adap.Domain
{

    public class LAFullReportGrid : ReportGrid<LAFullReportRow>
    {
        private readonly bool showFullHistory;

        public LAFullReportGrid(IFormsRepository formsRepo, Dictionary<int, string> applicableFormIdentifiers, bool showFullHistory)
            : base(15, formsRepo, applicableFormIdentifiers)
        {
            this.showFullHistory = showFullHistory;
        }

        override public IEnumerable<LAFullReportRow> BuildRowsFromVFormResultUsers(IEnumerable<vFormResultUser> vfruRecords, string countURL = null)
        {
            List<LAFullReportRow> result = new List<LAFullReportRow>();
            foreach (vFormResultUser vfru in vfruRecords)
            {
                LAFullReportRow baseRow = BuildRowFromVFormResultUser(vfru, vfruRecords);
                result.Add(baseRow);

                if (showFullHistory)
                    result.AddRange(BuildHistoryRowsForBaseRow(baseRow, vfru, vfruRecords));
            }
            return result;
        }

        private List<LAFullReportRow> BuildHistoryRowsForBaseRow(LAFullReportRow baseRow, vFormResultUser thisRecord, IEnumerable<vFormResultUser> allRecords)
        {
            IQueryable<def_StatusLog> statusLogs = formsRepo
                .GetStatusLogsForFormResultId(thisRecord.formResultId)
                .Where(sl => sl.statusDetailIdTo.HasValue);
            List<LAFullReportRow> result = new List<LAFullReportRow>();
            foreach (def_StatusLog sl in statusLogs)
            {
                def_FileAttachment snapshot = formsRepo.GetFileAttachment(sl.statusLogId, 2);
                result.Add(new LAFullReportRow(formsRepo, allRecords, baseRow.formIdentifier, thisRecord, sl.statusLogDate, true, snapshot));
            }
            return result;
        }

        private LAFullReportRow BuildRowFromVFormResultUser(vFormResultUser thisRecord, IEnumerable<vFormResultUser> allRecords)
        {
            string formIdentifier = applicableFormIdentifiers[thisRecord.formId];
            return new LAFullReportRow(formsRepo, allRecords, formIdentifier, thisRecord, thisRecord.statusChangeDate);
        }

    }
}
