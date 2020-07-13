using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Assmnts.Infrastructure;
using System.IO;

namespace Assmnts
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AuthConfig.RegisterAuth();

            DependencyResolver.SetResolver(new NinjectDependencyResolver());
        }

        protected void Application_AcquireRequestState()
        {
            // support for on demand language change stored via cookie
            var langCookie = HttpContext.Current.Request.Cookies["lang"];
            if (langCookie != null)
            {
                var ci = new CultureInfo(langCookie.Value);

                // check for existing culture
                if (ci == null)
                {
                    // set english as default
                    string langName = "en";

                    // try to get values from Accept lang HTTP header
                    if (HttpContext.Current.Request.UserLanguages != null && HttpContext.Current.Request.UserLanguages.Length != 0)
                    {
                        langName = HttpContext.Current.Request.UserLanguages[0].Substring(0, 2);
                    }

                    langCookie = new HttpCookie("lang", langName)
                    {
                        HttpOnly = true
                    };

                    HttpContext.Current.Response.AppendCookie(langCookie);
                }

                // set culture for each request
                Thread.CurrentThread.CurrentUICulture = ci;
                Thread.CurrentThread.CurrentCulture = ci;
            }
        }

        protected void Session_End(object sender, EventArgs e)
        {
            Debug.WriteLine(" * * * SESSION_END");
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            Debug.WriteLine(" * * * SESSION_START");
        }
    }
}