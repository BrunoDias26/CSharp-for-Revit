using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CSharp_for_Revit.Forms;
using CSharp_for_Revit.Utilities;

namespace CSharp_for_Revit.Commands.General
{
    /// <summary>
    ///     Lets the user pick rooms only (filtered selection), then selects them
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Select2_Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;

            // Make the selection filter: only rooms can be picked
            var selectionFilter = new Selection_Utilities.ISF_ByCategory(BuiltInCategory.OST_Rooms);

            // Pick the rooms (showMessage: false = only the message below, not two in a row)
            var selectedElements = UIDocument_Utilities.SelectWithFilter(uiDoc, selectionFilter,
                prompt: "Select room(s), then press 'Finish'",
                showMessage: false);

            // Make sure we got some rooms
            if (selectedElements.Count == 0)
            {
                return Custom.Cancelled("No elements selected.");
            }

            // Select the elements
            return UIDocument_Utilities.SelectElements(uiDoc, selectedElements);
        }
    }
}
