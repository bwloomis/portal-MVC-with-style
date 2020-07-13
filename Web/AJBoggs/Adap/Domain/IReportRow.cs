using Assmnts;
using Data.Abstract;
using System;
using System.Collections.Generic;


namespace AJBoggs.Adap.Domain
{
    public interface IReportRow
    {
        string GetHtmlForColumn(int columnIndex);

        IComparable GetSortingValueForColumn(int columnIndex);
    }

}
