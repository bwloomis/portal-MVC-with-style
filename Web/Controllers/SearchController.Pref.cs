using System;
using System.Diagnostics;
using System.Web.Mvc;

using Assmnts.Models;
using Assmnts.Infrastructure;


namespace Assmnts.Controllers
{
    public partial class SearchController : Controller
    {

        [HttpGet]
        public ActionResult PrefSearch()
        {
            SearchModel model = new SearchModel();

            model.passPref = false;
            model.profilePref = false;
            model.searchPref = true;

            model.showSearch = false;

            return View("Index", model);
        }

        [HttpGet]
        public ActionResult PrefProfile()
        {
            SearchModel model = new SearchModel();

            model.passPref = false;
            model.profilePref = true;
            model.searchPref = false;

            model.showSearch = false;

            return View("Index", model);
        }
		
        [HttpGet]
        public ActionResult PrefPass()
        {
            SearchModel model = new SearchModel();

            model.passPref = true;
            model.profilePref = false;
            model.searchPref = false;

            model.showSearch = false;

            return View("Index", model);
        }
        
    }
}