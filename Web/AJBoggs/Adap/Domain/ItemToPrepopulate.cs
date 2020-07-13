using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AJBoggs.Adap.Domain
{
	public class ItemToPrepopulate
	{
		public string Title { get; set; }
		public string Source { get; set; }
		public string Type { get; set; }
		public bool? Document { get; set; }
	}
}