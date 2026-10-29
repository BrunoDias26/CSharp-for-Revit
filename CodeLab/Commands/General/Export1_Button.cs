using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CodeLab.Forms;
using CodeLab.Utilities;

namespace CodeLab.Commands.General
{
    /// <summary>
    ///     Exports the selected sheets to PDF (one file per sheet), then opens the folder
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Export1_Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Select sheets to export
            var sheetResult = Sheet_Utilities.SelectSheets(doc, title: "Select sheets to export");
            if (sheetResult.Cancelled) { return Result.Cancelled; }

            var sheets = sheetResult.Objects.Cast<ViewSheet>().ToList();

            // Select a folder to export to
            var folderResult = Custom.SelectFolder(title: "Select folder to export to");
            if (folderResult.Cancelled) { return Result.Cancelled; }

            var directoryPath = folderResult.Object as string;

            // Default PDF export settings
            var options = Export_Utilities.DefaultPdfExportOptions();

            int sheetsExported = 0;

            // No progress bar here (removed from the lesson, as requested).
            // The transaction is kept as in the lesson: export doesn't change the model.
            using (var t = new Transaction(doc, "Export sheets to PDF"))
            {
                t.Start();

                foreach (var sheet in sheets)
                {
                    var fileName = Sheet_Utilities.SheetExportKey(sheet, doc);

                    if (Export_Utilities.ExportSheetToPdf(sheet, fileName, directoryPath, doc, options) == Result.Succeeded)
                    {
                        sheetsExported++;
                    }
                }

                t.Commit();
            }

            // Not in the lesson: tell the user if nothing was exported, instead of opening an empty folder
            if (sheetsExported == 0)
            {
                return Custom.Error("No sheets were exported.");
            }

            // Open the folder (OpenDirectory already returns a Result)
            return File_Utilities.OpenDirectory(directoryPath);
        }
    }
}
