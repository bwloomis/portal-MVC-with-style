using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Runtime.Serialization.Json;

using Assmnts.Models;

using Data.Abstract;
using Data.Concrete;


namespace Assmnts.Controllers
{
    public partial class DefwsController : Controller
    {

        [HttpPost]
        public string GetInterviewers()
        {
            List<uas_User> interviewers;

            // * * * NOTE * * * Needs to be filtered by at least Ent and possibly Group
            using (var context = DataContext.getUasDbContext())
            {
                interviewers = (from user in context.uas_User
                                select user).ToList();
            }

            if (interviewers == null)
            {
                interviewers = new List<uas_User>();
            }



            string jsonString = "{ \"users\" " + ": [";

            int i = 1;

            foreach (uas_User user in interviewers)
            {
                jsonString += "{ \"value\" : \"" + user.UserID + "\" , \"display\" : \""
                    + user.FirstName + " " + user.LastName + "\" }";
                if (i < interviewers.Count)
                {
                    jsonString += ",";
                }
                i++;

            }
            jsonString += " ]}";

            return jsonString;
        }
    }
}
