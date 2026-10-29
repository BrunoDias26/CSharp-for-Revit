using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CodeLab.Forms;
using CodeLab.Utilities;

namespace CodeLab.Commands.General
{
    /// <summary>
    ///     A button to run test code. Now: reads an Excel file (lesson 26)
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Testing_Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Excel test file. The @ makes \ a normal character (no need to write \\ like the author)
            var filePath = @"C:\Users\User\Desktop\Pasta1.xlsx";

            // Check the file first: exists and is not locked (e.g. open in Excel)
            if (Excel_Utilities.VerifyExcelFile(filePath) != Result.Succeeded)
            {
                return Result.Cancelled;
            }

            // using: releases the file when the block ends
            using (var workbook = Excel_Utilities.GetWorkbook(filePath))
            {
                var worksheet = Excel_Utilities.GetWorkSheet(workbook, getFirstIfNotFound: true);
                var matrix = Excel_Utilities.ReadWorksheet(worksheet);

                // Each row -> one line in Visual Studio's Output window (only when debugging with F5)
                foreach (var row in matrix)
                {
                    var readRow = string.Join("\t", row);
                    Debug.WriteLine(readRow);
                }

                // Extra (not in the lesson): the same rows in a message, to check without the debugger
                var allRows = string.Join("\n", matrix.Select(row => string.Join(" | ", row)));
                return Custom.Completed(allRows);
            }
        }
    }
}
