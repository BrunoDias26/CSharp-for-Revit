using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CodeLab.Forms;
using CodeLab.Forms.Bases;
using CodeLab.Utilities;

namespace CodeLab.Commands.General
{
    /// <summary>
    ///     Creates sheets from an Excel file (A = Number, B = Name), or renames the ones that already exist
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Import1_Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Select the Excel file
            var formResult = Custom.SelectFilePaths(filter: Custom.FILTER_EXCEL, multiSelect: false);

            if (formResult.Cancelled) { return Result.Cancelled; }
            var filePath = formResult.Object as string;

            // Is the file accessible (exists, not open in Excel)
            if (Excel_Utilities.VerifyExcelFile(filePath) != Result.Succeeded)
            {
                return Result.Cancelled;
            }

            // Read the Excel data (using: releases the file at the end of the block)
            List<List<string>> matrix;

            using (var workbook = Excel_Utilities.GetWorkbook(filePath))
            {
                var worksheet = Excel_Utilities.GetWorkSheet(workbook, getFirstIfNotFound: true);
                matrix = Excel_Utilities.ReadWorksheet(worksheet);
            }

            // Verify the structure: header "Number | Name" and at least one row below it
            if (matrix.Count < 2 || matrix[0].Count < 2 || matrix[0][0] != "Number" || matrix[0][1] != "Name")
            {
                return Custom.Cancelled("Excel file structure is not valid.\n\nExpected: A1 = Number, B1 = Name.");
            }

            // Remove the header
            matrix.RemoveAt(0);

            // Select the title block type for the new sheets
            var formResultTtb = FamilySymbol_Utilities.SelectTitleBlockTypes(doc, multiSelect: false);

            if (formResultTtb.Cancelled) { return Result.Cancelled; }
            var ttbType = formResultTtb.Object as FamilySymbol;

            // Dictionary: sheet number (key) -> sheet (value).
            // Finds a sheet by its number at once, instead of looping over all sheets for every Excel row
            var sheets = Sheet_Utilities.GetProjectSheets(doc, includePlaceholders: true, sorted: false);
            var sheetDictionary = new Dictionary<string, ViewSheet>();

            foreach (var sheet in sheets)
            {
                // Key in lowercase, so "a-001" in Excel finds "A-001" in Revit
                sheetDictionary[sheet.SheetNumber.ToLower()] = sheet;
            }

            // Properties related to task
            int progressTotal = matrix.Count;
            int progressStep = Form_Utilities.ProgressDelay(progressTotal);
            int sheetsCreated = 0;
            int sheetsUpdated = 0;
            int sheetsSkipped = 0;

            // Using a progress bar...
            using (var pb = new BaseProgressBar(taskName: "Creating/updating sheet(s)", pbTotal: progressTotal))
            {
                // Using a transaction...
                using (var t = new Transaction(doc, "C# for Revit: Import Sheets"))
                {
                    t.Start();

                    foreach (var row in matrix)
                    {
                        // Cancel command if progress bar cancelled (rolls back the transaction)
                        if (pb.CancelCheck(t)) { return Result.Cancelled; }

                        // Column A = number, column B = name
                        var sheetNumber = row[0];
                        var sheetName = row[1];

                        // Empty number or name: skip (not in the lesson: Revit would refuse them)
                        if (sheetNumber == "" || sheetName == "")
                        {
                            sheetsSkipped++;
                        }
                        // Does the sheet exist? TryGetValue = true and the sheet in exSheet, or false
                        else if (sheetDictionary.TryGetValue(sheetNumber.ToLower(), out var exSheet))
                        {
                            // Rename only if it can be edited and the name is different
                            if (Element_Utilities.IsEditable(exSheet, doc) == true && exSheet.Name != sheetName)
                            {
                                exSheet.Name = sheetName;
                                sheetsUpdated++;
                            }
                            else
                            {
                                sheetsSkipped++;
                            }
                        }
                        // If not, create it
                        else
                        {
                            var newSheet = ViewSheet.Create(doc, ttbType.Id);
                            newSheet.Name = sheetName;
                            newSheet.SheetNumber = sheetNumber;
                            sheetsCreated++;

                            // Not in the lesson: add it to the dictionary too, so a number repeated
                            // in Excel renames this sheet instead of failing on a duplicate number
                            sheetDictionary[sheetNumber.ToLower()] = newSheet;
                        }

                        // Increment progress
                        Thread.Sleep(progressStep);
                        pb.Increment();
                    }

                    // Cancelled during the last step (not in the lesson): avoids "task finished" after a rollback
                    if (pb.CancelCheck(t)) { return Result.Cancelled; }

                    // Commit the transaction
                    pb.Commit(t);
                }
            }

            // Tell the user what happened
            return Custom.Completed("The task has finished.\n\n" +
                $"{sheetsCreated} sheets created.\n" +
                $"{sheetsUpdated} sheets updated.\n" +
                $"{sheetsSkipped} sheets skipped.\n\n" +
                "Skipped sheets were either not editable, had the same name as in Excel, or had an empty number/name.");
        }
    }
}
