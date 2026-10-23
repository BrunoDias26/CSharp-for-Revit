using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CSharp_for_Revit.Forms;
using CSharp_for_Revit.Utilities;

namespace CSharp_for_Revit.Commands.General
{
    /// <summary>
    /// Stacked baseline dimensions for fabrication pipes (CID 2041), from the nearest grid.
    /// The user draws a line (locked H/V) and a read window over the active view:
    /// horizontal line = dimensions that measure X (vertical pipes + risers);
    /// vertical line = dimensions that measure Y (horizontal pipes + risers).
    /// Every ladder grows outward and its largest dimension touches the line through the 3rd click,
    /// so all ladders end aligned. Works only in plan views opened directly (not on a sheet / inside a viewport,
    /// not in 3D, sections or elevations). Only MEP Fabrication pipework is read, not regular Revit pipes.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Dimension1_Button : IExternalCommand
    {
        // Dimension type used for piping (CTS standard, sheet M-L1-SD-C1)
        private const string DIMENSION_TYPE_NAME = "CTS - Linear - 3/32\" Arial - Line Leader - Inline Text BASELINE";

        // Two pipes closer than this (1") along the measured axis get one witness line only.
        // static readonly, not const: the value comes from a method, calculated once and never changed.
        private static readonly double MERGE_TOLERANCE_FEET = Units_Utilities.InchesToFeet(1.0);

        // A pipe closer than this (1/8") to its grid is not dimensioned (the value would show ~0)
        private static readonly double MIN_DISTANCE_TO_GRID_FEET = Units_Utilities.InchesToFeet(0.125);

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            var activeView = uiDoc.ActiveGraphicalView;

            // Only in the view itself: on a sheet (or inside a viewport) the screen translation lands in the wrong place
            if (UIDocument_Utilities.IsWorkingOnSheet(uiDoc) == true)
            {
                // The sheet itself is open: it may hold several views, so there is no single one to offer
                if (activeView is ViewSheet)
                {
                    return Custom.Cancelled("This tool works in a plan view, not on a sheet.\n\nOpen the floor plan from the Project Browser and run the tool again.");
                }

                // Inside a viewport: offer to open that view. The command stops after switching, because
                // screen values read right after a view change, in the same command, can be out of date.
                var answer = Custom.Message(
                    title: "Open the view?",
                    message: $"This tool works in the view itself, not inside a viewport on a sheet.\n\nOpen '{activeView.Name}' now?\n(Then run the tool again.)",
                    yesNo: true);

                if (answer.Affirmative == true)
                {
                    if (UIDocument_Utilities.ActivateView(uiDoc, activeView) == false)
                    {
                        return Custom.Cancelled($"Revit did not open '{activeView.Name}'.\n\nOpen it from the Project Browser and run the tool again.");
                    }
                }

                return Result.Cancelled;
            }

            // Plan views only: the grids, the risers and the screen directions only make sense seen from above
            if ((activeView is ViewPlan) == false)
            {
                return Custom.Cancelled($"This tool works in plan views (floor, ceiling or structural plans).\n\nThe active view '{activeView.Name}' is a {activeView.ViewType}.");
            }

            // Dimension type first: without it there is nothing to do (and no clicks are wasted)
            var dimensionType = Dimension_Utilities.GetDimensionTypeByName(doc, DIMENSION_TYPE_NAME);

            if (dimensionType is null)
            {
                return Custom.Cancelled($"Dimension type not found in this model:\n{DIMENSION_TYPE_NAME}");
            }

            // Ask for the line and the read window (3 clicks, Esc cancels)
            var pick = UIDocument_Utilities.PickLineWithWindow(uiDoc);
            if (pick.Cancelled) { return Result.Cancelled; }

            // The line decides which pipes count and where the largest dimensions go
            OrientationInView pipeOrientation;
            string measuredAxis;
            double lineInView;
            double targetInView;

            if (pick.LineOrientation == OrientationInView.Horizontal)
            {
                // Horizontal line: vertical pipes, dimensions measure X, rows are stacked in Y
                pipeOrientation = OrientationInView.Vertical;
                measuredAxis = "X";
                lineInView = pick.LineStartInView.Y;
                targetInView = pick.ExtentInView.Y;
            }
            else
            {
                // Vertical line: horizontal pipes, dimensions measure Y, rows are stacked in X
                pipeOrientation = OrientationInView.Horizontal;
                measuredAxis = "Y";
                lineInView = pick.LineStartInView.X;
                targetInView = pick.ExtentInView.X;
            }

            // Outward = from the line towards the 3rd click (the pipes are in between)
            double outwardSign;

            if (targetInView > lineInView)
            {
                outwardSign = 1.0;
            }
            else
            {
                outwardSign = -1.0;
            }

            // Record cards of the pipes of the view; the ones perpendicular to the line, plus the risers
            var pipeCids = new List<FabricationCid>() { FabricationCid.Pipe };
            var parts = FabricationPart_Utilities.GetFabricationParts(doc, activeView, pipeCids);
            var infos = FabricationPart_Utilities.GetFabPartInfos(parts, activeView);

            var candidates = new List<FabPartInfo>();
            candidates.AddRange(FabricationPart_Utilities.FilterByOrientation(infos, pipeOrientation));
            candidates.AddRange(FabricationPart_Utilities.FilterByOrientation(infos, OrientationInView.Riser));

            // The window decides which of them are read
            var touching = FabricationPart_Utilities.FilterByWindow(candidates, pick.WindowMinInView, pick.WindowMaxInView);

            if (touching.Count == 0)
            {
                return Custom.Cancelled(
                    $"No {pipeOrientation.ToString().ToLower()} fabrication pipes touch the window.\n\n" +
                    "A horizontal line reads vertical pipes; a vertical line reads horizontal pipes.");
            }

            // One witness line per position (a riser and a pipe at the same position share one)
            var unique = FabricationPart_Utilities.RemoveClosePositions(touching, pipeOrientation, MERGE_TOLERANCE_FEET);

            // Ladders: same nearest grid (parallel to the pipes) + same side = one baseline dimension
            var grids = Grid_Utilities.GetGrids(doc, activeView);
            var ladders = Dimension_Utilities.GetLadders(unique, grids, activeView, pipeOrientation, MIN_DISTANCE_TO_GRID_FEET);

            if (ladders.Count == 0)
            {
                return Custom.Cancelled($"Nothing to dimension: no {pipeOrientation.ToString().ToLower()} grid visible in this view for the {unique.Count} fabrication pipe position(s).\n\nThe dimensions start at the nearest grid parallel to the pipes.");
            }

            // Create the dimensions: created at the target line, then stacked outward and pulled in until
            // the largest dimension touches the target line
            var createdCount = 0;
            var failedCount = 0;
            var detailLines = new List<string>();

            using (var transaction = new Transaction(doc, "C# for Revit: Stacked dimensions"))
            {
                transaction.Start();

                foreach (var ladder in ladders)
                {
                    var ladderText = $"grid {ladder.Grid.Name}, {ladder.Parts.Count} pipe(s)";

                    try
                    {
                        var dimension = Dimension_Utilities.CreateBaselineDimension(doc, activeView, ladder, dimensionType, targetInView);

                        if (dimension is null)
                        {
                            failedCount = failedCount + 1;
                            detailLines.Add($"FAILED (no valid reference): {ladderText}");
                        }
                        else
                        {
                            Dimension_Utilities.AlignOuterRow(doc, activeView, dimension, pipeOrientation, targetInView, targetInView, outwardSign);

                            createdCount = createdCount + 1;
                            detailLines.Add($"OK #{Element_Utilities.IntegerValue(dimension.Id)}: {ladderText}");
                        }
                    }
                    catch (Exception exception)
                    {
                        // Revit refuses some dimensions: count and go on with the others
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

            // Everything worked: no message, the dimensions on the screen are the answer
            if (failedCount == 0) { return Result.Succeeded; }

            // Something failed: say what, so a missing dimension is never silent
            var report = new List<string>();
            report.Add($"Line: {pick.LineOrientation}   (dimensions measure {measuredAxis})");
            report.Add($"Fabrication pipes touching the window: {touching.Count}   after removing close positions: {unique.Count}");
            report.Add($"Dimensions created: {createdCount}   failed: {failedCount}");
            report.Add("");
            report.AddRange(detailLines);

            Custom.Message(title: "Some dimensions failed", message: string.Join("\n", report), noCancel: true, icon: MessageBoxIcon.Warning);
            return Result.Succeeded;
        }
    }
}
