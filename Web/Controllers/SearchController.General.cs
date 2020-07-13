using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;

using Assmnts.Infrastructure;
using Assmnts.Models;

using Data.Concrete;


namespace Assmnts.Controllers
{
    public partial class SearchController : Controller
    {

        [HttpGet]
        public ActionResult GeneralSearch()
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account");
            }

            SearchModel model = new SearchModel();
            model.GeneralSearchString = Session["GeneralSearchString"].ToString();
            model.SearchResult = GeneralSearch(model.GeneralSearchString);

            model.sisIds = model.SearchResult.Select(r => r.formResultId).ToList();
            model.formIds = model.SearchResult.Select(r => r.formId).Distinct().ToList();


            addDictionaries(model);
            
            return View("Index", model);
        }

        [HttpPost]
        public ActionResult GeneralSearch(SearchModel model, string command)
        {
            if (!SessionHelper.IsUserLoggedIn)
            {
                return RedirectToAction("Index", "Account");
            }

            if (command.Equals("Reset") || string.IsNullOrWhiteSpace(model.GeneralSearchString))
            {
                Session["GeneralSearchString"] = null;
                Session["DetailSearchCriteria"] = null;

                return RedirectToAction("Index", "Search");
            }
            else
            {
                Session["DetailSearchCriteria"] = null;
                Session["GeneralSearchString"] = model.GeneralSearchString;
                model.SearchResult = GeneralSearch(model.GeneralSearchString);

                model.sisIds = model.SearchResult.Select(r => r.formResultId).ToList();
                model.formIds = model.SearchResult.Select(r => r.formId).Distinct().ToList();
                                
                addDictionaries(model);

                return View("Index", model);
            }
        }

        private IEnumerable<spSearchGrid_Result> GeneralSearch(string searchString)
        {
            List<string> searchWords = searchString.Trim().Replace('\t', ' ').Split(' ').ToList();
            string searchWordsForFullIndex = string.Empty;
            List<long> searchNumbers = new List<long>();
            List<DateTime> searchDates = new List<DateTime>();
            List<string> searchStatuses = new List<string>();

            int counter = 0;
            foreach (var word in searchWords)
            {
                DateTime tmpDate = DateTime.Now;
                if (DateTime.TryParse(word, out tmpDate))
                {
                    searchDates.Add(tmpDate);
                }
                else
                {
                    int tempNumber = 0;
                    if (int.TryParse(word, out tempNumber))
                    {
                        searchNumbers.Add(tempNumber);
                    }

                    string lowerWord = word.ToLower();
                    if (lowerWord.Contains("complete"))
                    {
                        searchStatuses.Add("completed");
                    }
                    else if ( lowerWord.Equals("new") )
                    {
                        searchStatuses.Add("new");
                    }
                    else if ( lowerWord.Equals("progress") )
                    {
                        searchStatuses.Add("inprogress");
                    }
                    else if (lowerWord.Contains("abandon"))
                    {
                        searchStatuses.Add("abandoned");
                    }

                    searchWordsForFullIndex = (counter == 0) ? word : searchWordsForFullIndex + " OR " + word;

                    counter++;
                }
            }

            XElement xmlNumbers = new XElement("Numbers", searchNumbers.Select(i => new XElement("Number", i)));
            XElement xmlDates = new XElement("Dates", searchDates.Select(i => new XElement("Date", i.ToString("MM/dd/yyyy"))));

            IEnumerable<spSearchGrid_Result> tmpResult;
            using (var context = DataContext.getSisDbContext())
            {
                tmpResult = filterSearchResultsByAuthGroups(
                    context.spSearchGrid(false, false, false, string.Empty, null, null, searchWordsForFullIndex, xmlNumbers.ToString(), xmlDates.ToString()).ToList());
                //tmpResult = context.spSearchGrid(false, false, false, string.Empty, null, null, searchWordsForFullIndex, xmlNumbers.ToString(), xmlDates.ToString()).ToList();
            }


            // Search status after getting the result. 
            if (searchStatuses.Count > 0)
            {
                if (tmpResult.Count() == 0)
                {
                    using (var context = DataContext.getSisDbContext())
                    {
                        // if tmpResult is empty, get all
                        tmpResult = filterSearchResultsByAuthGroups( 
                            context.spSearchGrid(true, false, false, string.Empty, null, null, string.Empty, string.Empty, string.Empty).ToList());
                        //tmpResult = context.spSearchGrid(true, false, false, string.Empty, null, null, string.Empty, string.Empty, string.Empty).ToList();
                    }
                }

                tmpResult = (from a in tmpResult
                             where (a.Status != null && searchStatuses.Contains(a.Status.ToLower()))
                             select a).ToList();

            }

            return tmpResult;
        }

    }
}