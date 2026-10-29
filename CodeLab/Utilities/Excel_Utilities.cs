using System.IO;
using Autodesk.Revit.UI;
using ClosedXML.Excel;
using CSharp_for_Revit.Forms;

namespace CSharp_for_Revit.Utilities
{
    // ClosedXML (NuGet package) reads .xlsx files without Excel installed.
    // Hierarchy: XLWorkbook (the .xlsx file) -> Worksheets (the tabs) -> rows -> cells.
    public static class Excel_Utilities
    {
        /// <summary>
        /// Attempts to open a workbook (.xlsx file) at a file path.
        /// </summary>
        /// <param name="filePath">The file path to open.</param>
        /// <returns>An XLWorkbook, or null if it could not be opened.</returns>
        public static XLWorkbook GetWorkbook(string filePath)
        {
            try
            {
                return new XLWorkbook(filePath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to get a worksheet (tab) by name, with the option to default to the first one.
        /// </summary>
        /// <param name="workbook">The workbook to check.</param>
        /// <param name="worksheetName">The name of the worksheet to get.</param>
        /// <param name="getFirstIfNotFound">Get the first worksheet if the name is not found.</param>
        /// <returns>An IXLWorksheet, or null if not found.</returns>
        public static IXLWorksheet GetWorkSheet(XLWorkbook workbook, string worksheetName = "Sheet1", bool getFirstIfNotFound = false)
        {
            if (workbook is null) { return null; }

            foreach (var worksheet in workbook.Worksheets)
            {
                if (worksheet.Name == worksheetName)
                {
                    return worksheet;
                }
            }

            if (getFirstIfNotFound == true)
            {
                return workbook.Worksheets.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Verifies if an Excel file can be read, showing a message if not.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <param name="worksheetName">An optional worksheet name that must exist.</param>
        /// <returns>Result.Succeeded if the file can be read; otherwise, Result.Cancelled.</returns>
        public static Result VerifyExcelFile(string filePath, string worksheetName = null)
        {
            if (File.Exists(filePath) == false)
            {
                return Custom.Cancelled("The file could not be found.");
            }

            if (File_Utilities.IsFileReadable(filePath) == false)
            {
                return Custom.Cancelled("The file could not be read.\n\nIs it open in Excel?");
            }

            // null = no worksheet name given, so this check is skipped
            if (worksheetName is not null)
            {
                using (var workbook = GetWorkbook(filePath))
                {
                    if (GetWorkSheet(workbook, worksheetName, false) is null)
                    {
                        return Custom.Cancelled($"{worksheetName} was not found in the Excel file.");
                    }
                }
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Reads a worksheet into a matrix: a list of rows, each row a list of cell texts.
        /// </summary>
        /// <param name="worksheet">The worksheet to read.</param>
        /// <param name="readRows">Number of rows to read (0 = all).</param>
        /// <param name="readColumns">Number of columns to read (0 = all).</param>
        /// <returns>A list of rows; each row is a list of strings.</returns>
        public static List<List<string>> ReadWorksheet(IXLWorksheet worksheet, int readRows = 0, int readColumns = 0)
        {
            var matrix = new List<List<string>>();
            if (worksheet is null) { return matrix; }

            // RangeUsed = the rectangle that has data (null if the worksheet is empty)
            var rangeUsed = worksheet.RangeUsed();
            if (rangeUsed is null) { return matrix; }

            // Asking for more than exists = read all
            if (rangeUsed.RowCount() <= readRows) { readRows = 0; }
            if (rangeUsed.ColumnCount() <= readColumns) { readColumns = 0; }

            int rowsRead = 0;

            foreach (var row in rangeUsed.RowsUsed())
            {
                // If we can still read rows... 
                if (rowsRead < readRows || readRows == 0)
                {
                    var dataList = new List<string>();
                    int columnsRead = 0;

                    foreach (var cell in row.Cells())
                    {
                        // If we can still read columns...
                        if (columnsRead < readColumns || readColumns == 0)
                        {
                            // The cell's value as text, without spaces at the ends
                            var value = cell.GetString().Trim();
                            dataList.Add(value);
                        }
                        columnsRead++;
                    }

                    // Inside the if (fixed from the lesson): rows past the limit no longer add empty lists
                    matrix.Add(dataList);
                }

                rowsRead++;
            }

            return matrix;
        }
    }
}
