using Assmnts;
using Data.Abstract;
using System.Collections.Generic;
using System.Linq;


namespace AJBoggs.Adap.Domain
{

    public class StatusGrid : OverviewGrid
    {
        public StatusGrid(IFormsRepository formsRepo, Dictionary<int, string> applicableFormIdentifiers)
            : base( formsRepo, applicableFormIdentifiers, false ) {}

        override public IEnumerable<OverviewRow> BuildRowsFromVFormResultUsers(IEnumerable<vFormResultUser> query, string countURL = null)
        {
            //* * *OT 4-14-16 
            //the following two GroupBy+Select statements were originally in AdapController.Reports DataTableApplicationStatus()
            //very similar code was moved from AdapController.Reports DataTableApplicationOverview() to ajboggs\adap\domain\overviewgrid.cs
            //both blocks seem to do the same thing, but I'm keeping them separate because it seems they were intended to behave differently
            //if it turns out they are supposed to be the same, the most efficient method should be kept in OverviewReport and this class should be eliminated
            var group = query
                .GroupBy(q => new { q.formId, q.GroupID, q.GroupName, q.formStatus })
                .Select(g => new {
                        FormId = g.Key.formId,
                        GroupId = g.Key.GroupID,
                        GroupName = g.Key.GroupName,
                        StatusSortOrder = g.Key.formStatus,
                        Count = g.Count()
                    });

            List<OverviewRow> result = new List<OverviewRow>();
            foreach (var g in group )
            {
                string formIdentifier = applicableFormIdentifiers[g.FormId];
                string statusDisplayText = formsRepo.GetStatusTextByDetailSortOrder(1, g.StatusSortOrder).displayText;

                string newCountURL = countURL;
                if (newCountURL.Contains("sTeam"))
                {
                    newCountURL = newCountURL.Replace("sTeam", g.GroupName);
                }

                if (newCountURL.Contains("sStatus"))
                {
                    newCountURL = newCountURL.Replace("sStatus", statusDisplayText);
                }
                result.Add( new OverviewRow( 
                        g.FormId, formIdentifier, 
                        g.GroupId, g.GroupName,
                        g.StatusSortOrder, statusDisplayText, 
                        g.Count, newCountURL ));
            }

            return result;
        }
    }
}
