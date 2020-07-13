using Data.Abstract;
using System;


namespace AJBoggs.Adap.Domain
{
    public class OverviewRow : IReportRow
    {
        public readonly int formId;
        public readonly string formIdentifier;

        public readonly int? groupId;
        public readonly string groupName;

        public readonly byte statusSortOrder;
        public readonly string statusDisplayText;

        public readonly int count;
        public readonly string countURL;

        #region OverviewRow Constructor, with all members above as params
        public OverviewRow(
            int formId,
            string formIdentifier,
            int? groupId,
            string groupName,
            byte statusSortOrder,
            string statusDisplayText,
            int count,
            string countURL = "")
        {
            this.formId = formId;
            this.formIdentifier = formIdentifier;
            this.groupId = groupId;
            this.groupName = groupName;
            this.statusSortOrder = statusSortOrder;
            this.statusDisplayText = statusDisplayText;
            this.count = count;
            this.countURL = countURL;
        }
        #endregion

        public string GetHtmlForColumn(int columnIndex)
        {
            switch ( columnIndex ) 
            {
                case 0: //type (form identifier)
                    return "<div class=\"form-type-text\" data-formIdentifier=\"" + formIdentifier + "\">" + formIdentifier + "</div>";

                case 1://team (group)
                    return groupName;

                case 2://status
                    return statusDisplayText;

                case 3: //count
                    string returnVal;
                    if (!String.IsNullOrWhiteSpace(countURL))
                    {
                        if (countURL.Contains(":"))
                        {
                            string[] temp = countURL.Split(':');
                            returnVal = temp[0] + count.ToString() + temp[1];
                        }
                        else
                        {
                            returnVal = countURL + count.ToString();
                        }
                    }
                    else
                    {
                        returnVal = count.ToString();
                    }
                    return returnVal;

                default:
                    throw new Exception( "invalid column index: " + columnIndex );
            }
        }

        public IComparable GetSortingValueForColumn(int columnIndex)
        {
            switch (columnIndex)
            {
                case 0: //type (form identifier)
                    return "<div class=\"form-type-text\" data-formIdentifier=\"" + formIdentifier + "\">" + formIdentifier + "</div>";

                case 1://team (group)
                    return groupId;

                case 2: //status
                    return statusSortOrder;

                case 3: //count
                    return count;

                default: 
                    throw new Exception("unsupported sorting column index: " + columnIndex);
            }
        }
    }

}
