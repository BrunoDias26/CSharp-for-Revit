using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

namespace CodeLab.Commands.General
{
    [Transaction(TransactionMode.Manual)]
    public class Command_Stack1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Code Logic here
            Autodesk.Revit.UI.TaskDialog.Show("Button 1 is working!", doc.Title);

            return Result.Succeeded;
        }
    }

}

