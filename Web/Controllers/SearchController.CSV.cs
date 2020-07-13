using System;
using System.Diagnostics;
using System.Web.Mvc;

using Assmnts.Models;
using System.Linq;


namespace Assmnts.Controllers
{
    public partial class SearchController : Controller
    {

        [HttpGet]
        public ActionResult BatchCSVOptions()
        {
            SearchModel model = new SearchModel();

            IQueryable<vSearchGrid> query = createSISSearchQuery();
            model.DetailSearchCriteria = (SearchCriteria)Session["DetailSearchCriteria"];
            if (model.DetailSearchCriteria != null)
            {
                query = DetailSearch(model.DetailSearchCriteria);
            }
            model.formIds = query.GroupBy(q => q.formId).Select(grp => (int?)grp.Key).ToList();
            model.sisIds = query.GroupBy(q => q.formResultId).Select(grp => (int?)grp.Key).ToList();
            addUsedForms(model);

            model.passPref = false;
            model.profilePref = false;
            model.searchPref = false;
            model.batchCSVExportOptions = true;

            model.showSearch = false;

            return View("Index", model);
        }
      
    }
}