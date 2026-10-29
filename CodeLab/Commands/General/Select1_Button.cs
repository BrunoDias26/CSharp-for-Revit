using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CodeLab.Forms;
using CodeLab.Utilities;

namespace CodeLab.Commands.General
{
    /// <summary>
    ///     Selects the title blocks placed on the selected sheets
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Select1_Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Get selected sheet Ids (OfType keeps only the ViewSheets of the selection)
            var selectedSheetIds = UIDocument_Utilities.GetSelectedElements(uiDoc)
                .OfType<ViewSheet>()
                .Select(s => s.Id)
                .ToList();

            // Get all title blocks that are on any of those sheets (OwnerViewId = the sheet they are placed on)
            var titleBlocks = Element_Utilities.GetElementsOfCategory(doc, BuiltInCategory.OST_TitleBlocks)
                .Where(ttb => selectedSheetIds.Contains(ttb.OwnerViewId))
                .ToList();

            if (titleBlocks.Count == 0)
            {
                return Custom.Cancelled("No titleblocks were found.");
            }

            // Select the title blocks (SelectElements already returns a Result)
            return UIDocument_Utilities.SelectElements(uiDoc, titleBlocks);
        }
    }
}
