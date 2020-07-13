using System;
using System.Collections.Generic;

namespace Data.Abstract
{
    public interface IUasSql
    {
        string GetConnectionString();
        Dictionary<int, string> getEnterprises();
        Dictionary<int, string> getGroups();
        Dictionary<int, string> getGroups(int entId);

        string getEnterpriseName(int entId);
        string getGroupName(int groupId);

        string GetUasTableName(int fileId);
        int GetNumberTables();

        bool SaveDOB(int userId, string date);
    }
}
