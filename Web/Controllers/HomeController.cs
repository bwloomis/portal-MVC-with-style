using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Abstract;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;

using UAS.Business;

namespace Assmnts.Controllers
{
    [RedirectingAction]
    public class HomeController : Controller
    {
        private readonly IFormsRepository formsRepo;

        public HomeController(IFormsRepository fr)
        {
            formsRepo = fr;
        }
        
        public ActionResult Index()
        {
            Home model = new Home();

            SessionHelper.SessionTotalTimeoutMinutes = model.timeout;

            model.Forms = Business.Forms.GetFormsDictionary(formsRepo);
           
            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        
    }
}
