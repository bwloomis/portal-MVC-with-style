using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Concrete;

using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;


namespace Assmnts.Controllers
{
    public partial class SearchController : Controller
    {

        [HttpPost]
        public ActionResult Venture(SearchModel model)
        {
            int? srchEnterpriseID = null;
            if (SessionHelper.LoginStatus.EnterpriseID != 0)      // Enterprise 0 = System Administrator - view all Assessments
            {
                srchEnterpriseID = SessionHelper.LoginStatus.EnterpriseID;
            }
            
            // Statement below is the Default full Search below
            var context = DataContext.getSisDbContext();
            IQueryable<vSearchGrid> query = context.vSearchGrids.AsNoTracking().OrderByDescending(a => a.Rank);
            //Hide archived, deleted and Pre-QA assessments.
            // query = query.Where(q => (q.deleted == false || q.deleted == null) && (q.archived == false || q.archived == null) && (!q.reviewStatus.Equals("1")));

            // Add filters for Enterprise and Groups.
            model.vSearchResult = filterSearchResultsByEntIdAuthGroups(query);
                //    context.spSearchGrid(true, false, false, string.Empty, srchEnterpriseID, null, string.Empty, string.Empty, string.Empty).ToList()));

            // NOTE: VistaDB does NOT support skip/take
            model.vSearchResultEnumerable = model.vSearchResult.AsEnumerable();
            model.vSearchResultEnumerable = FilterByArchivedDeleted(model, model.vSearchResultEnumerable);

            // model.sisIds = model.SearchResult.Select(r => r.formResultId).ToList();
            // model.formIds = model.SearchResult.Select(r => r.formId).Distinct().ToList();

            var sisIds = model.vSearchResultEnumerable.Select(r => r.formResultId).ToList();
            model.sisIds = new List<int?>();
            foreach (int i in sisIds)
                model.sisIds.Add((int?)i);
            
            var formIds = model.vSearchResultEnumerable.Select(r => r.formId).Distinct().ToList();
            model.formIds = new List<int?>();
            foreach (int i in formIds)
                model.formIds.Add((int?)i);

            addDictionaries(model);

            return View("Index", model);

        }
       
        IEnumerable<vSearchGrid> FilterByArchivedDeleted(SearchModel model, IEnumerable<vSearchGrid> results)
        {
            bool deleted = model.DetailSearchCriteria.deleted;
            bool archived = model.DetailSearchCriteria.archived;

            if ( (deleted == false) && (archived == false))
            {
                return results.Where(r => r.deleted == deleted && r.archived == archived);
            }
            else if (deleted == true && archived == false)
            {
                return results.Where(r => r.deleted == deleted);
            }
            else if (deleted == false && archived == true)
            {
                 return results.Where(r => r.archived == archived);
            }
            else if (deleted == true && archived == true)
            {
                 return results.Where(r => r.deleted == deleted || r.archived == archived);
            }

            return results;
        }

    }
}