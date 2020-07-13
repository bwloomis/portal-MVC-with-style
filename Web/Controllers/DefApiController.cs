using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Assmnts.Infrastructure;


namespace Assmnts.Controllers
{
    [RedirectingAction]
    public class DefApiController : ApiController
    {
        /*  calls made via the api URL.
         *  Tutorial: http://www.codeproject.com/Articles/659131/Understanding-and-Implementing-ASPNET-WebAPI
         * 
         *  Must map to HTTP commands GET, PUT, POST, DELETE (basic CRUD)
         *  To Test:
         *          http://localhost:50209/api/DefApi/1
         */

        public DefApiController()
        {
        }

        // GET: api/DefApi
        public String GetVersion()
        {
            return "1.0";
        }


    }
}
