using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Assmnts.Infrastructure
{
    public class RedirectingActionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (SessionHelper.IsUserLoggedIn == false)
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "Account",
                    action = "Index"
                }));
            }
        }
    }

    public class ADAPRedirectingActionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // * * * OT 3-10-16 removed authentication on DownloadAttachment so attachments can be downloaded from links in PDFs (Bug 13051)
            if (filterContext.ActionDescriptor.ActionName == "DownloadAttachment" &&
                filterContext.ActionDescriptor.ControllerDescriptor.ControllerName == "COADAP")
                return;

            if (SessionHelper.IsUserLoggedIn == false)
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "Account",
                    action = "SsoLogout"
                }));
            }
        }
    }
}