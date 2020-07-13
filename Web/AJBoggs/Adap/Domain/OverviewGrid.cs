using Assmnts;
using Data.Abstract;
using System.Collections.Generic;
using System.Linq;


namespace AJBoggs.Adap.Domain
{
    public class OverviewGrid : ReportGrid<OverviewRow>
    {
        private readonly bool overview;

        public OverviewGrid(IFormsRepository formsRepo, Dictionary<int, string> applicableFormIdentifiers, bool overview)
            : base(4, formsRepo, applicableFormIdentifiers) {
                this.overview = overview;
        }

        override public IEnumerable<OverviewRow> BuildRowsFromVFormResultUsers(IEnumerable<vFormResultUser> vfruRecords, string countURL = "")
        {

            //* * *OT 4-14-16 Not sure what the intention was here. 
            //the following two GroupBy+Select statements were originally in AdapController.Reports DataTableApplicationOverview()
            //very similar code was moved from AdapController.Reports DataTableApplicationStatus() to ajboggs\adap\domain\StatusGrid.cs
            //both blocks seem to do the same thing, but I'm keeping them separate because it seems they were intended to behave differently
            var subgroup = vfruRecords.GroupBy(q => new { q.formId, q.subject, q.GroupID, q.GroupName, q.formStatus })
                .Select(g =>
                new
                {
                    FormId = g.Key.formId,
                    Subject = g.Key.subject,
                    GroupId = g.Key.GroupID,
                    GroupName = g.Key.GroupName,
                    StatusSortOrder = g.Key.formStatus,
                    MaxDate = g.Select(q => (overview) ? q.dateUpdated : q.statusChangeDate).OrderByDescending(d => d).FirstOrDefault()
                });

            int totalCount = subgroup.Count();
            //List<string[]> dataCheck2 = new List<string[]>();
            //foreach (var v in subgroup)
            //{
            //    dataCheck2.Add(new string[] { v.Subject.ToString(), v.Group, v.Status.ToString(), v.MaxDate.ToString() });
            //}

            var group = subgroup.GroupBy(g => new { g.FormId, g.GroupId, g.GroupName, g.StatusSortOrder })
                .Select(g =>
                new
                {
                    FormId = g.Key.FormId,
                    GroupId = g.Key.GroupId,
                    GroupName = g.Key.GroupName,
                    StatusSortOrder = g.Key.StatusSortOrder,
                    Count = g.Count()
                });
            
            List<OverviewRow> result = new List<OverviewRow>();
            foreach (var g in group )
            {
                string formIdentifier = applicableFormIdentifiers[g.FormId];

                def_StatusText defstatusText = formsRepo.GetStatusTextByDetailSortOrder(1, g.StatusSortOrder);

                string statusDisplayText =  string.Empty;

                if(defstatusText != null)
                {
                    statusDisplayText = defstatusText.displayText;

                    string newCountURL = countURL;
                    if (newCountURL.Contains("sTeam"))
                    {
                        newCountURL = newCountURL.Replace("sTeam", g.GroupName);
                    }

                    if (newCountURL.Contains("sStatus"))
                    {
                        newCountURL = newCountURL.Replace("sStatus", statusDisplayText);
                    }

                    result.Add(new OverviewRow(
                            g.FormId, formIdentifier,
                            g.GroupId, g.GroupName,
                            g.StatusSortOrder, statusDisplayText,
                            g.Count, newCountURL));
                }

            }
            return result;
        }
    }
}
