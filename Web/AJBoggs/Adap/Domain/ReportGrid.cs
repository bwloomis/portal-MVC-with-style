using Assmnts;
using Data.Abstract;
using System.Collections.Generic;


namespace AJBoggs.Adap.Domain
{
    public abstract class ReportGrid<T> where T : IReportRow
    {
        protected readonly int nColumns;
        protected readonly IFormsRepository formsRepo;
        protected readonly Dictionary<int, string> applicableFormIdentifiers;

        public ReportGrid(int nColumns, IFormsRepository formsRepo, Dictionary<int, string> applicableFormIdentifiers)
        {
            this.nColumns = nColumns;
            this.formsRepo = formsRepo;
            this.applicableFormIdentifiers = applicableFormIdentifiers;
        }

        public string[] GetHtmlForReportRow(T row)
        {
            string[] result = new string[nColumns];
            for (int i = 0; i < nColumns; i++)
                result[i] = row.GetHtmlForColumn(i);
            return result;
        }

        public abstract IEnumerable<T> BuildRowsFromVFormResultUsers(IEnumerable<vFormResultUser> vfruRecords, string countURL = "");
    }
}
