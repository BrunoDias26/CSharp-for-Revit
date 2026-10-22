using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CSharp_for_Revit.Forms;
using CSharp_for_Revit.Utilities;

namespace CSharp_for_Revit.Commands.General
{
    /// <summary>
    /// Stacked dimensions, step 1 of the user interaction (TEST: creates nothing in the model):
    /// the user draws a line (locked H/V) and a read window over the active view, and the command
    /// selects the fabrication pipes (CID 2041) that the dimensions would use.
    /// Horizontal line = dimensions that measure X (vertical pipes + risers);
    /// vertical line = dimensions that measure Y (horizontal pipes + risers).
    /// The version that creates the dimensions is in git (commit 9980b8a) and comes back in step 2.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Dimension1_Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            var activeView = uiDoc.ActiveGraphicalView;

            // Ask for the line and the read window (3 clicks, Esc cancels)
            var pick = UIDocument_Utilities.PickLineWithWindow(uiDoc);
            if (pick.Cancelled) { return Result.Cancelled; }

            // Record cards of all the pipes of the view
            var pipeCids = new List<FabricationCid>() { FabricationCid.Pipe };
            var parts = FabricationPart_Utilities.GetFabricationParts(doc, activeView, pipeCids);
            var infos = FabricationPart_Utilities.GetFabPartInfos(parts, activeView);

            // The line decides which pipes count: the ones perpendicular to it, plus the risers
            OrientationInView pipeOrientation;
            string measuredAxis;

            if (pick.LineOrientation == OrientationInView.Horizontal)
            {
                pipeOrientation = OrientationInView.Vertical;
                measuredAxis = "X";
            }
            else
            {
                pipeOrientation = OrientationInView.Horizontal;
                measuredAxis = "Y";
            }

            var candidates = new List<FabPartInfo>();
            candidates.AddRange(FabricationPart_Utilities.FilterByOrientation(infos, pipeOrientation));
            candidates.AddRange(FabricationPart_Utilities.FilterByOrientation(infos, OrientationInView.Riser));

            // The window decides which of them are read
            var touching = FabricationPart_Utilities.FilterByWindow(candidates, pick.WindowMinInView, pick.WindowMaxInView);

            // Select them, so they light up in blue (FabricationPart -> Element: Cast, because of the list types)
            var touchingElements = touching
                .Select(i => i.Part)
                .Cast<Element>()
                .ToList();

            UIDocument_Utilities.SelectElements(uiDoc, touchingElements);

            // Report: the numbers to check the translation from screen to model
            var windowWidth = pick.WindowMaxInView.X - pick.WindowMinInView.X;
            var windowHeight = pick.WindowMaxInView.Y - pick.WindowMinInView.Y;

            var report = new List<string>();
            report.Add($"Line: {pick.LineOrientation}   (dimensions would measure {measuredAxis})");
            report.Add($"Window: {windowWidth:0.00} ft x {windowHeight:0.00} ft");
            report.Add($"Pipes in the view: {infos.Count}   candidates ({pipeOrientation} + risers): {candidates.Count}");
            report.Add($"Touching the window (now selected): {touching.Count}");

            // DEBUG (temporary, delete after the selection is fixed): where the window is x where the pipes are
            var candidateMinX = double.MaxValue;
            var candidateMaxX = double.MinValue;
            var candidateMinY = double.MaxValue;
            var candidateMaxY = double.MinValue;

            foreach (var candidate in candidates)
            {
                if (candidate.MidPointInView is null) { continue; }

                candidateMinX = Math.Min(candidateMinX, candidate.MidPointInView.X);
                candidateMaxX = Math.Max(candidateMaxX, candidate.MidPointInView.X);
                candidateMinY = Math.Min(candidateMinY, candidate.MidPointInView.Y);
                candidateMaxY = Math.Max(candidateMaxY, candidate.MidPointInView.Y);
            }

            // DEBUG (temporary): the same window as a box for Revit's own filter (like a crossing selection).
            // If this also finds 0, the window coordinates are wrong; if it finds pipes, FilterByWindow is wrong.
            var modelCorner1 = View_Utilities.FromViewCoordinates(pick.WindowMinInView, activeView);
            var modelCorner2 = View_Utilities.FromViewCoordinates(pick.WindowMaxInView, activeView);
            var outline = new Outline(
                new XYZ(Math.Min(modelCorner1.X, modelCorner2.X), Math.Min(modelCorner1.Y, modelCorner2.Y), -10000),
                new XYZ(Math.Max(modelCorner1.X, modelCorner2.X), Math.Max(modelCorner1.Y, modelCorner2.Y), 10000));

            var revitCrossingCount = new FilteredElementCollector(doc, activeView.Id)
                .OfCategory(BuiltInCategory.OST_FabricationPipework)
                .WherePasses(new BoundingBoxIntersectsFilter(outline))
                .GetElementCount();

            report.Add("");
            report.Add($"DEBUG window      X: {pick.WindowMinInView.X:0.00} .. {pick.WindowMaxInView.X:0.00}   Y: {pick.WindowMinInView.Y:0.00} .. {pick.WindowMaxInView.Y:0.00}");
            report.Add($"DEBUG candidates  X: {candidateMinX:0.00} .. {candidateMaxX:0.00}   Y: {candidateMinY:0.00} .. {candidateMaxY:0.00}");
            report.Add($"DEBUG Revit crossing filter (all pipework, bounding boxes): {revitCrossingCount}");

            return Custom.Completed(string.Join("\n", report));
        }
    }
}
