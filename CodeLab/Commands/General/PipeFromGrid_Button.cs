using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using CodeLab.Forms;
using CodeLab.Utilities;

namespace CodeLab.Commands.General
{
    /// <summary>
    /// "Pipe from Grid": stacked baseline dimensions for fabrication pipes (CID 2041), from the nearest grid.
    /// Naming rule: button name without spaces + "_Button" = this class, its icons ({class}32.png / {class}16.png)
    /// and its tooltip key.
    /// The user draws a line (locked H/V) and a read window over the active view:
    /// horizontal line = dimensions that measure X (vertical pipes + risers);
    /// vertical line = dimensions that measure Y (horizontal pipes + risers).
    /// Every ladder grows outward and its largest dimension touches the line through the 3rd click,
    /// so all ladders end aligned. Works only in plan views opened directly (not on a sheet / inside a viewport,
    /// not in 3D, sections or elevations). Only MEP Fabrication pipework is read, not regular Revit pipes.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class PipeFromGrid_Button : IExternalCommand
    {
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

                // Inside a viewport: name the view to open. (Opening it from code doesn't work in this state:
                // Revit already reports it as the active view and ignores the request; tested 2026-10-22.)
                return Custom.Cancelled($"This tool works in the view itself, not inside a viewport on a sheet.\n\nOpen '{activeView.Name}' from the Project Browser and run the tool again.");
            }

            // Plan views only: the grids, the risers and the screen directions only make sense seen from above
            if ((activeView is ViewPlan) == false)
            {
                return Custom.Cancelled($"This tool works in plan views (floor, ceiling or structural plans).");
            }

            // Dimension type first: without it there is nothing to do (and no clicks are wasted).
            // It is the type the Linear dimension tool is set to in this project. A type that is not Baseline
            // makes a chain instead of a ladder: the user sees it and changes the type in Revit.
            var dimensionType = Dimension_Utilities.GetDefaultLinearDimensionType(doc);

            if (dimensionType is null)
            {
                return Custom.Cancelled("No linear dimension type is set in this project.\n\nPick one in Annotate > Linear and run the tool again.");
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

            using (var transaction = new Transaction(doc, "C# for Revit: Pipe from Grid"))
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
                Debug.WriteLine($"[PipeFromGrid] {line}");
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
