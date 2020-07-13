using Data.Abstract;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions; 

namespace Assmnts.Business.Uploads
{
    public class LookupUploads
    {
        public bool UploadLookups(int enterpriseId, int userId, IFormsRepository formsRepo, string excelFileName)
        {
             def_LookupMaster lookupMaster = formsRepo.GetLookupMastersByLookupCode("ADAP_CLINIC");
            
            if (lookupMaster == null) {
                throw new Exception("Cannot find lookup master: code ADAP_CLINIC");
            }

            MarkAllLookupsInactive(lookupMaster.lookupMasterId, enterpriseId, formsRepo);
            
            int dataStartingRow = 7;

            Workbook workBook;
            SharedStringTable sharedStrings;
            IEnumerable<Sheet> workSheets;
            WorksheetPart recSheet;

            try
            {
                //Excel.Application xlApp;
                //Excel.Workbook xlWorkBook;
                //Excel.Worksheet xlWorkSheet;
                //Excel.Range range;

                //xlApp = new Excel.Application();
                //xlWorkBook = xlApp.Workbooks.Open(excelFileName, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                //xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

                //range = xlWorkSheet.UsedRange;

                //for (int rCnt = dataStartingRow; rCnt <= range.Rows.Count; rCnt++)
                //{

                //    // Create string array for row to pass to UpdateLookup function
                //    string[] dataValues = new string[range.Columns.Count];
                //    int index = 0;
                //    for (int cCnt = 1; cCnt <= range.Columns.Count; cCnt++)
                //    {
                //        string str = (range.Cells[rCnt, cCnt] as Excel.Range).Text.ToString();
                //        dataValues[index] = str;
                //        index++;
                //    }

                //    if (arrayNotEmpty(dataValues))
                //    {
                //        UpdateLookups(enterpriseId, dataValues, formsRepo);
                //    }
                //}

                //xlWorkBook.Close(true, null, null);
                //xlApp.Quit();

                //return true;

                //Declare helper variables.
                string recID;

                //Open the Excel workbook.
                using (SpreadsheetDocument document = SpreadsheetDocument.Open(excelFileName, true))
                {
                    //References to the workbook and Shared String Table.
                    workBook = document.WorkbookPart.Workbook;
                    workSheets = workBook.Descendants<Sheet>();
                    sharedStrings = document.WorkbookPart.SharedStringTablePart.SharedStringTable;

                    //Reference to Excel Worksheet with Recipient data.
                    //recID = workSheets.First(s => s.Name == strWorkSheetName).Id;

                    recID = workSheets.First().Id;

                    recSheet = (WorksheetPart)document.WorkbookPart.GetPartById(recID);

                    //LINQ query to skip first row with column names.
                    IEnumerable<Row> dataRows =
                      from row in recSheet.Worksheet.Descendants<Row>()
                      where row.RowIndex >= dataStartingRow
                      select row;

                    // Count the number of columns for headers.
                    IEnumerable<Row> templateHeader = from row in recSheet.Worksheet.Descendants<Row>()
                                                      where row.RowIndex == (dataStartingRow - 1)
                                                      select row;
                    int numCols = templateHeader.First().Count();

                    foreach (Row row in dataRows)
                    {

                        //If a column is missing from row, add an empty value
                        IEnumerable<Cell> cells = from cell in row.Descendants<Cell>()
                                                  select cell;

                        int columnCounter = 0;
                        List<string> dataValues = new List<string>();

                        foreach (var cell in cells)
                        {
                            string colReference = cell.CellReference;
                            int colIndex = GetColumnIndexFromName(GetColumnName(colReference));

                            // if column index is higher than what it should be, add blank space to it until it matches up.
                            if (columnCounter < colIndex)
                            {
                                for (int i = columnCounter; i < colIndex; i++)
                                {
                                    dataValues.Add("");
                                }
                            }

                            string cellValue = string.Empty;
                            if ((cell.DataType != null) && (cell.DataType == CellValues.SharedString))
                            {
                                int id = -1;
                                if (Int32.TryParse(cell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(document.WorkbookPart, id);
                                    if (item.Text != null)
                                        cellValue = item.Text.Text;
                                    else if (item.InnerText != null)
                                        cellValue = item.InnerText;
                                    else if (item.InnerXml != null)
                                        cellValue = item.InnerXml;
                                }
                            }
                            else if (cell.CellValue != null)
                                cellValue = cell.CellValue.InnerText;

                            dataValues.Add(cellValue);
                            columnCounter = colIndex + 1;
                        }

                        if (dataValues.Count() < numCols)
                        {
                            for (int i = dataValues.Count(); i < numCols; i++)
                            {
                                dataValues.Add("");
                            }
                        }
                        else if (dataValues.Count() > numCols)
                        {
                            // if dataValues is more than number of column headers, delete the last data 
                            for (int i = dataValues.Count(); i > numCols; i--)
                            {
                                dataValues.RemoveAt(i - 1);
                            }
                        }

                        if (arrayNotEmpty(dataValues.ToArray()))
                        {
                            UpdateLookups(enterpriseId, dataValues.ToArray(), formsRepo);
                        }
                        else
                        {
                            break;
                        }



                    }

                    
                    document.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
                return false;
            }
        }

        /// <summary>
        /// Given a cell name, parses the specified cell to get the column name.
        /// </summary>
        /// <param name="cellReference">Address of the cell (ie. B2)</param>
        /// <returns>Column Name (ie. B)</returns>
        public static string GetColumnName(string cellReference)
        {
            // Create a regular expression to match the column name portion of the cell name.
            Regex regex = new Regex("[A-Za-z]+");
            Match match = regex.Match(cellReference);

            return match.Value;
        }

        /// <summary>
        /// Given just the column name (no row index), it will return the zero based column index.
        /// Note: This method will only handle columns with a length of up to two (ie. A to Z and AA to ZZ). 
        /// A length of three can be implemented when needed.
        /// </summary>
        /// <param name="columnName">Column Name (ie. A or AB)</param>
        /// <returns>Zero based index if the conversion was successful; otherwise null</returns>
        public static int GetColumnIndexFromName(string columnName)
        {
            int columnIndex = -1;

            if (columnName.Length <= 2)
            {
                int index = 0;
                foreach (char col in columnName)
                {
                    int indexValue = Letters.IndexOf(col);

                    if (indexValue != -1)
                    {
                        // The first letter of a two digit column needs some extra calculations
                        if (index == 0 && columnName.Length == 2)
                        {
                            columnIndex = columnIndex == -1 ? ((indexValue + 1) * 26) : columnIndex + ((indexValue + 1) * 26);
                        }
                        else
                        {
                            columnIndex = columnIndex == -1 ? (indexValue) : columnIndex + indexValue;
                        }
                    }

                    index++;
                }
            }

            return columnIndex;
        }

        private static List<char> Letters = new List<char>() { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', ' ' };

        
        public static SharedStringItem GetSharedStringItemById(WorkbookPart workbookPart, int id)
        {
            return workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(id);
        }
            private void MarkAllLookupsInactive(int lookupMasterId, int enterpriseId, IFormsRepository formsRepo)
        {
            List<def_LookupDetail> lookupDetails = formsRepo.GetLookupDetailsByLookupMasterEnterprise(lookupMasterId, enterpriseId);

            foreach (def_LookupDetail detail in lookupDetails)
            {
                detail.StatusFlag = "I";
                formsRepo.SaveLookupDetail(detail);
            }
        }

        private bool arrayNotEmpty(string[] dataValues)
        {
            for (int i = 0; i < dataValues.Count(); i++)
            {
                if (!String.IsNullOrEmpty(dataValues[i]))
                {
                    return true;
                }
            }

            return false;
        }

        
        /// <summary>
        /// Update lookup detail and lookup text records in the database (add them, update existing not currently supported --LK 11/4/2015)
        /// </summary>
        /// <param name="dataValues"></param>
        /// <param name="formsRepo"></param>
        private void UpdateLookups(int enterpriseId, string[] dataValues, IFormsRepository formsRepo)
        {
            int lookupDetailId;

            bool newDetail = false;

            def_LookupMaster lookupMaster = formsRepo.GetLookupMastersByLookupCode("ADAP_CLINIC");
            
            if (lookupMaster == null) {
                throw new Exception("Cannot find lookup master: code ADAP_CLINIC");
            }

            def_LookupDetail lookupDetail = null;

            // Display Choice (required)  
            if (String.IsNullOrEmpty(dataValues[1]))
            {
                throw new Exception("Display Text is required field.");
            }

            // Display Order (required)
            if (String.IsNullOrEmpty(dataValues[0]))
            {
                throw new Exception("Display Order is required field. Missing for: " + dataValues[1]);
            }
            
            
            
            short langId = 0;

            // Check that language ID is valid (or empty)
            if (!Int16.TryParse(dataValues[2], out langId) || (langId != 1 && langId != 2))
            {
                throw new Exception("Invalid language ID for: " + dataValues[1]);
            }
            
            // Try to find lookup detail by ID
            if (Int32.TryParse(dataValues[6], out lookupDetailId)) {
                lookupDetail = formsRepo.GetLookupDetailById(lookupDetailId);
            }
            
            if (lookupDetail == null && !String.IsNullOrEmpty(dataValues[5])) // find by data value
            {
                lookupDetail = formsRepo.GetLookupDetailByEnterpriseMasterAndDataValue(enterpriseId, lookupMaster.lookupMasterId, dataValues[5]);
            }
            
            if (lookupDetail == null && !String.IsNullOrEmpty(dataValues[1])) // find by display text and language
            {
                def_LookupText tempLookupText = formsRepo.GetLookupTextByDisplayTextEnterpriseIdMasterLang(dataValues[1], enterpriseId, lookupMaster.lookupMasterId, langId);

                if (tempLookupText != null)
                {
                    lookupDetail = formsRepo.GetLookupDetailById(tempLookupText.lookupDetailId);
                }
            }

            if (lookupDetail == null)
            {
                lookupDetail = new def_LookupDetail();
                newDetail = true;
            }

            lookupDetail.lookupMasterId = lookupMaster.lookupMasterId;

            
            // Set detail's display order
            int dispOrder = 0;
            
            if (!Int32.TryParse(dataValues[0], out dispOrder)) {
                throw new Exception("Display Order is not valid for: " + dataValues[1]);
            }
            else
            {
                lookupDetail.displayOrder = dispOrder;
            }

            // Set detail's status flag
            if (String.IsNullOrEmpty(dataValues[4]))
            {
                lookupDetail.StatusFlag = "A";
            }
            else if (dataValues[4].ToUpper() == "A" || dataValues[4].ToLower() == "active")
            {
                lookupDetail.StatusFlag = "A";
            }
            else if (dataValues[4].ToUpper() == "I" || dataValues[4].ToLower() == "inactive")
            {
                lookupDetail.StatusFlag = "I";
            }
            else
            {
                throw new Exception("Invalid value in Status Flag field for " + dataValues[1]);
            }

            
            // Set detail's data value (if data value field in spread sheet is blank, use display choice field instead (which is required))
            string dataValue = dataValues[5];
            
            if (String.IsNullOrEmpty(dataValues[5])) {
                dataValue = dataValues[1];
            }

            lookupDetail.dataValue = dataValue;

            lookupDetail.EnterpriseID = enterpriseId;

            int groupId = 0;
            if (Int32.TryParse(dataValues[3], out groupId))
            {
                lookupDetail.GroupID = groupId;

                UASEntities uasEntities = new UASEntities();

                uas_Group group = uasEntities.uas_Group.Where(g => g.GroupID == groupId && g.EnterpriseID == enterpriseId).FirstOrDefault();

                if (group == null)
                {
                    throw new Exception("Group with ID " + groupId + " does not exist for " + dataValues[1]);
                }

            }
            else
            {
                throw new Exception("Missing group ID for " + dataValues[1]);
            }

            if (newDetail)
            {
                // Add new lookup detail
                formsRepo.AddLookupDetail(lookupDetail);
            }
            else
            {
                // Update existing lookup detail
                formsRepo.SaveLookupDetail(lookupDetail);
            }

            def_LookupText lookupText = null;

            bool newLookupText = false;
            

            lookupText = formsRepo.GetLookupTextsByLookupDetailLanguage(lookupDetail.lookupDetailId, langId).FirstOrDefault();

            if (lookupText == null)
            {
                lookupText = new def_LookupText();
                newLookupText = true;
            }

            // Set Display Choice (required)
            lookupText.displayText = dataValues[1];

            // Set Language
            if (String.IsNullOrEmpty(dataValues[2]))
            {
                throw new Exception("Language ID is a required field.");
            }
            else
            {
                // Language ID is either 1 (English) or 2 (Spanish), so use value
                lookupText.langId = langId;
            }
            // Set lookup detail ID to be for the associated lookup detail (either just saved/added)
            lookupText.lookupDetailId = lookupDetail.lookupDetailId;

            if (newLookupText)
            {
                // Add new lookup text
                formsRepo.AddLookupText(lookupText);
            }
            else
            {
                // Update the lookup text
                formsRepo.SaveLookupText(lookupText);
            }
        }
    }
}