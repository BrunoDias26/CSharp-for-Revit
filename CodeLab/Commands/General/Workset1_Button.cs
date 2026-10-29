using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CodeLab.Forms;
using CodeLab.Forms.Bases;
using CodeLab.Utilities;

namespace CodeLab.Commands.General
{
    /// <summary>
    ///     Creates the standard worksets that are missing from the document
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Workset1_Button : IExternalCommand
    {
        // Standard workset names offered to the user
        private static readonly List<string> WorksetNames = new List<string>()
        {
            "AR Interior",
            "AR Facade",
            "AR Structure",
            "AR Fitout",
            "AR Site",
            "Z_Link DWG XXX",
            "Z_Link RVT XXX"
        };

        // New name for Revit's default "Shared Levels and Grids" workset
        private static readonly string GridsWorksetName = "AR Levels and Grids";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // No "is workshared?" check here anymore: the Workshared availability
            // grays out the button when the document is not workshared

            // Get worksets and names
            var worksets = Workset_Utilities.GetWorksets(doc);
            var worksetNames = worksets.Select(w => w.Name).ToList();

            // Get standard names that are not in the document yet
            var unusedNames = WorksetNames.Where(n => !worksetNames.Contains(n)).ToList();

            if (unusedNames.Count == 0)
            {
                return Custom.Cancelled("All standard worksets are available.");
            }

            // Choose the missing names from a list (keys and values are the same strings)
            var formResult = Custom.SelectFromList(keys: unusedNames,
                values: unusedNames.Cast<object>().ToList(),
                title: "Select worksets to add to document");

            if (formResult.Cancelled) { return Result.Cancelled; }
            var chosenWorksetNames = formResult.Objects.Cast<string>().ToList();

            // Offer to rename grids workset if present
            if (worksetNames.Contains("Shared Levels and Grids"))
            {
                var confirmRenameResult = Custom.Message(title: "Rename workset",
                    message: "The default grids and levels workset has been found\n\n" +
                    "Would you like to rename it?",
                    yesNo: true);

                if (confirmRenameResult.Affirmative)
                {
                    // Same index in both lists, since names came from worksets
                    int index = worksetNames.IndexOf("Shared Levels and Grids");
                    var gridsWorkset = worksets[index];

                    if (gridsWorkset.IsEditable)
                    {
                        using (var t = new Transaction(doc, "C# for Revit: Rename grids workset"))
                        {
                            t.Start();

                            WorksetTable.RenameWorkset(doc, gridsWorkset.Id, GridsWorksetName);

                            t.Commit();
                        }
                    }
                    else
                    {
                        Custom.Error("The workset was not editable.\n\n" +
                            "Remaining worksets will now be created anyway.");
                    }
                }
            }

            // Properties related to task
            int progressTotal = chosenWorksetNames.Count;
            int progressStep = Form_Utilities.ProgressDelay(progressTotal);

            // Using a progress bar...
            using (var pb = new BaseProgressBar(taskName: "Creating worksets", pbTotal: progressTotal))
            {
                // Using a transaction...
                using (var t = new Transaction(doc, "C# for Revit: Create worksets"))
                {
                    t.Start();

                    foreach (var name in chosenWorksetNames)
                    {
                        // Cancel command if progress bar cancelled (rolls back the transaction)
                        if (pb.CancelCheck(t)) { return Result.Cancelled; }

                        Workset.Create(doc, name);

                        // Increment progress
                        Thread.Sleep(progressStep);
                        pb.Increment();
                    }

                    // Cancelled during the last step (not in the lesson): avoids "created" after a rollback
                    if (pb.CancelCheck(t)) { return Result.Cancelled; }

                    // Commit the transaction
                    pb.Commit(t);
                }
            }

            // Show the outcome and return Result.Succeeded
            return Custom.Completed($"{progressTotal} workset(s) created");
        }
    }
}
