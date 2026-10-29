using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CSharp_for_Revit.Forms;
using CSharp_for_Revit.Forms.Bases;
using CSharp_for_Revit.Utilities;

namespace CSharp_for_Revit.Commands.General
{
    /// <summary>
    ///     Adds a selected revision to selected sheets 
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Revision1_Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Select a revision
            var revisionResult = Revision_Utilities.SelectRevision(doc);

            if (revisionResult.Cancelled) { return Result.Cancelled; }
            var selectedRevision = revisionResult.Object as Revision;

            // Select sheets
            var sheetResult = Sheet_Utilities.SelectSheets(doc);

            if (sheetResult.Cancelled) { return Result.Cancelled; }
            var selectedSheets = sheetResult.Objects.Cast<ViewSheet>().ToList();

            // Editability check
            if (doc.IsWorkshared)
            {
                var worksharingResult = Workshare_Utilities.ProcessElements(selectedSheets.Cast<Element>().ToList(), doc);

                if (worksharingResult.Cancelled) { return Result.Cancelled; }
                selectedSheets = worksharingResult.EditableElements.Cast<ViewSheet>().ToList();
            }

            // Properties related to task
            int progressTotal = selectedSheets.Count;
            int progressStep = Form_Utilities.ProgressDelay(progressTotal);
            int sheetsRevved = 0;

            // Using a progress bar...
            using (var pb = new BaseProgressBar(taskName: "Adding revision to sheet(s)", pbTotal: progressTotal))
            {
                // Using a transaction...
                using (var t = new Transaction(doc, "C# for Revit: Revision1"))
                {
                    t.Start();

                    foreach (var sheet in selectedSheets)
                    {
                        // Cancel command if progress bar cancelled (rolls back the transaction)
                        if (pb.CancelCheck(t)) { return Result.Cancelled; }

                        var addResult = Sheet_Utilities.AddRevision(sheet, selectedRevision);

                        if (addResult == Result.Succeeded)
                        {
                            sheetsRevved++;
                        }

                        // Increment progress
                        Thread.Sleep(progressStep);
                        pb.Increment();
                    }

                    // Cancelled during the last step (not in the lesson): avoids "sheets revised" after a rollback
                    if (pb.CancelCheck(t)) { return Result.Cancelled; }

                    // Commit the transaction
                    pb.Commit(t);
                }
            }

            // Show the outcome and return Result.Succeeded
            return Custom.Completed($"{sheetsRevved}/{selectedSheets.Count} sheets revised.\n\n" +
                "If sheets were skipped, they already had that revision applied.");
        }
    }
}
