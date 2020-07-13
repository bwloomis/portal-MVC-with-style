using Assmnts;
using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AJBoggs.Adap.Domain
{

    public class CaFullReportGrid : ReportGrid<CaFullReportRow>
    {
        private readonly bool showFullHistory;

        public CaFullReportGrid(IFormsRepository formsRepo, Dictionary<int, string> applicableFormIdentifiers, bool showFullHistory)
            : base(14, formsRepo, applicableFormIdentifiers) 
        {
            this.showFullHistory = showFullHistory;
        }

        override public IEnumerable<CaFullReportRow> BuildRowsFromVFormResultUsers(IEnumerable<vFormResultUser> vfruRecords, string countURL = null)
        {
            List<CaFullReportRow> result = new List<CaFullReportRow>();
            foreach (vFormResultUser vfru in vfruRecords)
            {
                CaFullReportRow baseRow = BuildRowFromVFormResultUser(vfru, vfruRecords);
                result.Add(baseRow);

                if( showFullHistory )
                    result.AddRange(BuildHistoryRowsForBaseRow(baseRow, vfru, vfruRecords));
            }
            return result;
        }

        private List<CaFullReportRow> BuildHistoryRowsForBaseRow(CaFullReportRow baseRow, vFormResultUser thisRecord, IEnumerable<vFormResultUser> allRecords)
        {
            IQueryable<def_StatusLog> statusLogs = formsRepo
                .GetStatusLogsForFormResultId(thisRecord.formResultId)
                .Where(sl => sl.statusDetailIdTo.HasValue);
            List<CaFullReportRow> result = new List<CaFullReportRow>();
            foreach (def_StatusLog sl in statusLogs)
            {
                def_FileAttachment snapshot = formsRepo.GetFileAttachment(sl.statusLogId, 2);
                result.Add(new CaFullReportRow(formsRepo, allRecords, baseRow.formIdentifier, thisRecord, sl.statusLogDate, true, snapshot));
            }
            return result;
        }

        private CaFullReportRow BuildRowFromVFormResultUser(vFormResultUser thisRecord, IEnumerable<vFormResultUser> allRecords)
        {
            string formIdentifier = applicableFormIdentifiers[thisRecord.formId];
            return new CaFullReportRow(formsRepo, allRecords, formIdentifier, thisRecord, thisRecord.statusChangeDate);
        }

    }
}
