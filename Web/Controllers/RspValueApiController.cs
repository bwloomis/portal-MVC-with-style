using System;
using System.Diagnostics;
using System.Collections.Generic;
// using System.Linq;
// using System.Net;
// using System.Threading.Tasks;
// using System.Net.Http;
using System.Web.Http;

using Assmnts.Models;

using Data.Abstract;
using Assmnts.Infrastructure;

namespace Assmnts.Controllers
{
    public class ResponseValue
    {
        public string id { get; set; }
        public string val { get; set; }
        public string msg { get; set; }
    }
   
    [RedirectingAction]
    public class RspValueApiController : ApiController
    {

        // private IFormsRepository formsRepo;

        private Dictionary<string, string> tstValues { get; set; }


        // public DefApiController(IFormsRepository fr)
        public RspValueApiController()
        {
            // Initiialized by Infrastructure.Ninject
            // formsRepo = fr;

            // Setup mock until the repository can be injected
            tstValues = new Dictionary<string, string>();
            tstValues.Add("p1s1i1", "2");
            tstValues.Add("p1s1i3", "3");
            tstValues.Add("p1s1i5", "2");
            tstValues.Add("p1s2i1", "1");
            tstValues.Add("p1s2i2", "2");
        }

        // GET: api/Books
        /*
        public IQueryable<BookDTO> GetBooks()
        {
            var books = from b in db.Books
                        select new BookDTO()
                        {
                            Id = b.Id,
                            Title = b.Title,
                            AuthorName = b.Author.Name
                        };

            return books;
        }
        */

        // GET: api/Books

        // public def_Parts GetParts()
        /*
        [HttpGet]
        [ActionName("parts")]
        public String GetParts()
        {

            def_Parts prt = new def_Parts()
                        {
                            partId = 1,
                            identifier = "Part 1"
                        };


            return "Part 1";
        }
        */

        public ResponseValue GetResponseValue(string id)
        {
            Debug.WriteLine("* * *  RspValueApiController:GetRspValue method  * * *  identifier: " + id);
            // string ret = String.Empty;
            ResponseValue rv = new ResponseValue() {id = String.Empty, val = String.Empty, msg = String.Empty };
            if (String.IsNullOrEmpty(id))
            {
                rv.msg = "The 'id' was missing.";
            }
            else
            {
                try
                {
                    rv.id = id;
                    rv.val = tstValues[id];
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("* * *  RspValueApiController:GetRspValue method  * * *  Exception: " + ex.Message);
                    rv.msg = ex.Message;
                }
            }

            return rv;
        }



        // GET: api/ResponseVariable/5
        /*
        [ResponseType(typeof(String))]
        public async Task<IHttpActionResult> GetResponseVariable(string rspVarId)
        {
            Debug.WriteLine("* * *  DefApiController:GetResponseVariable method  * * *  rspVarId: " + rspVarId.ToString());
            return Ok("2");
        }
        */


        // GET: api/RspValueApi/p1q2itm3
        /*
        [ResponseType(typeof(BookDetailDTO))]
        public async Task<IHttpActionResult> GetBook(int id)
        {
            var book = await db.Books.Include(b => b.Author).Select(b =>
                new BookDetailDTO()
                {
                    Id = b.Id,
                    Title = b.Title,
                    Year = b.Year,
                    Price = b.Price,
                    AuthorName = b.Author.Name,
                    Genre = b.Genre
                }).SingleOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return Ok(book);
        }
        */

    }
}
