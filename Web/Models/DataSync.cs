using Data.Abstract;
using Data.Concrete;

using System.Collections.Generic;


namespace Assmnts.Models
{
   
    
    public class DataSync
    {
        
        public int ID { get; set; }
        public string tableName { get; set; }
        public int SISOnlineCnt { get; set; }
        public int VentureCnt { get; set; }
        public string SyncStatus { get; set; }
        public List<int> formResultIds { get; set; }
        public bool autoDownload { get; set; }

        public bool formStatusUploaded()
        {
            IFormsSql formsSql = new FormsSql();

            return formsSql.GetFormResultTablesUploadedStatus();
        }

        public bool noFormsToBeUploaded()
        {
            IFormsSql formsSql = new FormsSql();

            return formsSql.GetNoFormResultsToBeUploaded();
        }
    }


}