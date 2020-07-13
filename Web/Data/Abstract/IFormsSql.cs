

using Assmnts;

namespace Data.Abstract
{
    public interface IFormsSql
    {

        // Common Routines
        string GetConnectionString();
        string GetMetaDataTableName(int fileId);
        string GetOldMetaDataTableName(int fileId);
        string GetResultsTableName(int fileId);

        int GetNumberOfResultsTables();
        int GetNumberOfMetaTables();
        int GetTableRecordCount(string tableName);
        int GetTableRecordCountVenture(string tableName);
        bool GetNoFormResultsToBeUploaded();
        bool GetFormResultTablesUploadedStatus();
     //   int[] GetCompletedFormResults();
        string CreateFormResultJSON(int formResultId, int newFormResultId);
        string[] CreateFormResultsJSON();
        int DeleteTableContent(string tableName);
        string GenerateQuery(string formTable);

        void MarkSingleUploaded(int formResultId);
        void MarkUploaded();

        void SetIdentityInsert(string tableName, string columnName, bool enable);


        System.Collections.Generic.List<int> GetCompletedFormResultIds();

        System.Collections.Generic.List<int> GetNonNewFormResultIds();

        void UpdateFormResultID(int formResultId, int p);


    }
}
