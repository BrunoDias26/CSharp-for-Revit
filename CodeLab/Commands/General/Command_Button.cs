using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CSharp_for_Revit.Utilities;

namespace CSharp_for_Revit.Commands.General
{
    /// <summary>
    ///     Example command
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Command_Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;

            // ZeroDoc availability: this button is enabled even with no document open,
            // so ActiveUIDocument can be null and uiDoc.Document below would crash
            if (uiApp.ActiveUIDocument is null)
            {
                return CSharp_for_Revit.Forms.Custom.Completed("This is working! (no document open)");
            }

            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;


            // Collect Elements
            var walls = new FilteredElementCollector(doc).OfCategories(BuiltInCategory.OST_Walls).WhereElementIsNotElementType().ToElements();
            var sheets = Sheet_Utilities.GetProjectSheets(doc, false);
            var sheets1 = Sheet_Utilities.GetProjectSheets(doc, false, true);

            // Code Logic here
            var revisionResult = CSharp_for_Revit.Utilities.Revision_Utilities.SelectRevision(doc);

            if (revisionResult.Cancelled) { return Result.Cancelled; }
            var selectedRevision = revisionResult.Object as Revision;
            CSharp_for_Revit.Forms.Custom.Message(message: selectedRevision.RevisionKey());

            // Select sheets
            var sheetResult = Sheet_Utilities.SelectSheets(doc);

            if (sheetResult.Cancelled) { return Result.Cancelled; }
            var selectedSheets = sheetResult.Objects.Cast<ViewSheet>().ToList();

            CSharp_for_Revit.Forms.Custom.Message(message: $"{selectedSheets.Count} sheets selected.");

            // Editability check
            if (doc.IsWorkshared)
            {
                var worksharingResult = Workshare_Utilities.ProcessElements(selectedSheets.Cast<Element>().ToList(), doc);

                if (worksharingResult.Cancelled) { return Result.Cancelled; }
                selectedSheets = worksharingResult.EditableElements.Cast<ViewSheet>().ToList();
            }


            return Result.Succeeded;

        }


    }

}