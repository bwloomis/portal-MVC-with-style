using Assmnts;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AJBoggs.Adap.Domain
{

    public class FullReportGrid : ReportGrid<FullReportRow>
    {
        private readonly bool showFullHistory;

        public FullReportGrid(IFormsRepository formsRepo, Dictionary<int, string> applicableFormIdentifiers, bool showFullHistory)
            : base(13, formsRepo, applicableFormIdentifiers) 
        {
            this.showFullHistory = showFullHistory;
        }

        override public IEnumerable<FullReportRow> BuildRowsFromVFormResultUsers(IEnumerable<vFormResultUser> vfruRecords, string countURL = null)
        {
            List<FullReportRow> result = new List<FullReportRow>();
            foreach (vFormResultUser vfru in vfruRecords)
            {
                FullReportRow baseRow = BuildRowFromVFormResultUser(vfru, vfruRecords);
                result.Add(baseRow);

                if( showFullHistory )
                    result.AddRange(BuildHistoryRowsForBaseRow(baseRow, vfru, vfruRecords));
            }
            return result;
        }

        private List<FullReportRow> BuildHistoryRowsForBaseRow( FullReportRow baseRow, vFormResultUser thisRecord, IEnumerable<vFormResultUser> allRecords)
        {
            IQueryable<def_StatusLog> statusLogs = formsRepo
                .GetStatusLogsForFormResultId(thisRecord.formResultId)
                .Where(sl => sl.statusDetailIdTo.HasValue);
            List<FullReportRow> result = new List<FullReportRow>();
            foreach (def_StatusLog sl in statusLogs)
            {
                def_FileAttachment snapshot = formsRepo.GetFileAttachment(sl.statusLogId, 2);
                result.Add(new FullReportRow(formsRepo, allRecords, baseRow.formIdentifier, thisRecord, sl.statusLogDate, true, snapshot));
            }
            return result;
        }

        private FullReportRow BuildRowFromVFormResultUser(vFormResultUser thisRecord, IEnumerable<vFormResultUser> allRecords)
        {
            string formIdentifier = applicableFormIdentifiers[thisRecord.formId];
            return new FullReportRow(formsRepo, allRecords, formIdentifier, thisRecord, thisRecord.statusChangeDate);
        }

    }
}
