using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

namespace CSharp_for_Revit.Commands.General
{
    [Transaction(TransactionMode.Manual)]
    public class Command_3Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Code Logic here
            Autodesk.Revit.UI.TaskDialog.Show("Button 3 is working!", doc.Title);

            return Result.Succeeded;
        }
    }
}
