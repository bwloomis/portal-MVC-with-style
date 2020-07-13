using System;
using System.Collections.Generic;

namespace Assmnts.Models
{

    [Serializable()]
    public class DataTableResult
    {
        public int draw { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }
        public List<string[]> data { get; set; }
        public string error { get; set; }
    }
}