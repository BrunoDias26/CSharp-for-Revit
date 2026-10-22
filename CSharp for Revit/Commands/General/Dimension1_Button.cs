using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CSharp_for_Revit.Forms;
using CSharp_for_Revit.Utilities;

namespace CSharp_for_Revit.Commands.General
{
    /// <summary>
    /// Stacked baseline dimensions, first test version: only the dimensions that measure X, for VERTICAL pipes
    /// and RISERS (horizontal pipes are ignored for now); dimension line placed over the pipes (position not refined yet).
    /// Input: only fabrication pipes (CID 2041): the ones in the selection, or, if nothing is selected,
    /// all the pipes of the active view.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Dimension1_Button : IExternalCommand
    {
        // Dimension type used for piping (CTS standard, sheet M-L1-SD-C1)
        private const string DIMENSION_TYPE_NAME = "CTS - Linear - 3/32\" Arial - Line Leader - Inline Text BASELINE";

        // Two pipes closer than this along the measured axis get one witness line only
        private const double MERGE_TOLERANCE_INCHES = 1.0;

        // A pipe closer than this to its grid is not dimensioned (the value would show ~0)
        private const double MIN_DISTANCE_TO_GRID_INCHES = 0.125;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            var activeView = uiDoc.ActiveGraphicalView;

            // Dimension type first: without it there is nothing to do
            var dimensionType = Dimension_Utilities.GetDimensionTypeByName(doc, DIMENSION_TYPE_NAME);

            if (dimensionType is null)
            {
                return Custom.Cancelled($"Dimension type not found in this model:\n{DIMENSION_TYPE_NAME}");
            }

            // Input: which pipes? Something selected = only the pipes in the selection;
            // nothing selected = all the pipes of the active view
            var pipeCids = new List<FabricationCid>() { FabricationCid.Pipe };
            var selectedElements = UIDocument_Utilities.GetSelectedElements(uiDoc);

            List<FabricationPart> parts;
            string source;

            if (selectedElements.Count > 0)
            {
                parts = FabricationPart_Utilities.FilterFabricationParts(selectedElements, pipeCids);
                source = $"selection ({selectedElements.Count} elements selected, {parts.Count} are pipes)";
            }
            else
            {
                parts = FabricationPart_Utilities.GetFabricationParts(doc, activeView, pipeCids);
                source = "all pipes (CID 2041) in the active view";
            }

            // A selection without pipes does not fall back to the whole view: the user chose those elements
            if (parts.Count == 0)
            {
                return Custom.Cancelled($"No fabrication pipes (CID 2041) found. Source: {source}.");
            }

            // Record cards, split by orientation
            var infos = FabricationPart_Utilities.GetFabPartInfos(parts, activeView);
            var verticals = FabricationPart_Utilities.FilterByOrientation(infos, OrientationInView.Vertical);
            var risers = FabricationPart_Utilities.FilterByOrientation(infos, OrientationInView.Riser);
            var horizontalCount = FabricationPart_Utilities.FilterByOrientation(infos, OrientationInView.Horizontal).Count;

            // These dimensions measure X: vertical pipes AND risers (a riser is a point in plan, so it has an X too)
            var measuredByX = new List<FabPartInfo>();
            measuredByX.AddRange(verticals);
            measuredByX.AddRange(risers);

            // One witness line per X position (a riser and a vertical pipe at the same X share one)
            var uniqueByX = FabricationPart_Utilities.RemoveClosePositions(measuredByX, OrientationInView.Vertical, MERGE_TOLERANCE_INCHES / 12.0);

            // Ladders: same nearest vertical grid + same side = one baseline dimension
            var grids = Grid_Utilities.GetGrids(doc, activeView);
            var ladders = Dimension_Utilities.GetLadders(uniqueByX, grids, activeView, OrientationInView.Vertical, MIN_DISTANCE_TO_GRID_INCHES / 12.0);

            if (ladders.Count == 0)
            {
                return Custom.Cancelled($"Nothing to dimension: {verticals.Count} vertical pipe(s) and {risers.Count} riser(s), no vertical grid found for them in this view.");
            }

            // Create the dimensions
            var createdCount = 0;
            var failedCount = 0;
            var detailLines = new List<string>();

            using (var transaction = new Transaction(doc, "C# for Revit: Stacked dimensions (vertical pipes)"))
            {
                transaction.Start();

                foreach (var ladder in ladders)
                {
                    var ladderText = $"grid {ladder.Grid.Name}, {ladder.Parts.Count} pipe(s)";

                    try
                    {
                        var dimension = Dimension_Utilities.CreateBaselineDimension(doc, activeView, ladder, dimensionType);

                        if (dimension is null)
                        {
                            failedCount = failedCount + 1;
                            detailLines.Add($"FAILED (no valid reference): {ladderText}");
                        }
                        else
                        {
                            createdCount = createdCount + 1;
                            detailLines.Add($"OK #{Element_Utilities.IntegerValue(dimension.Id)}: {ladderText}");
                        }
                    }
                    catch (Exception exception)
                    {
                        // Revit refuses some dimensions (e.g. a view type that doesn't accept them): count and go on
                        failedCount = failedCount + 1;
                        detailLines.Add($"FAILED ({exception.Message}): {ladderText}");
                    }
                }

                transaction.Commit();
            }

            // Same lines in Visual Studio's Output window (Debug.R25 + F5)
            foreach (var line in detailLines)
            {
                Debug.WriteLine($"[Dimension1] {line}");
            }

            // Report
            var report = new List<string>();
            report.Add($"Source: {source}");
            report.Add($"Vertical pipes: {verticals.Count}   risers: {risers.Count}   after removing close positions: {uniqueByX.Count}");
            report.Add($"Ignored for now: horizontal {horizontalCount}");
            report.Add($"Dimensions created: {createdCount}   failed: {failedCount}");
            report.Add("");
            report.AddRange(detailLines);

            return Custom.Completed(string.Join("\n", report));
        }
    }
}
