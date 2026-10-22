using View = Autodesk.Revit.DB.View;

namespace CSharp_for_Revit.Utilities
{
    /// <summary>
    /// Methods about grids: collect, orientation on the screen, position, and nearest grid to a part.
    /// Everything follows the screen (View_Utilities), so it works in rotated views.
    /// </summary>
    public static class Grid_Utilities
    {
        #region Get

        /// <summary>
        /// Collects the straight grids of the document.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="view">Optional: if given, only the grids visible in that view. If null, the whole model.</param>
        /// <returns>A list of straight grids (curved grids are left out; empty if doc is null).</returns>
        public static List<Grid> GetGrids(Document doc, View view = null)
        {
            if (doc is null) { return new List<Grid>(); }

            // Whole document, or only what is visible in the view (same idea as Element_Utilities.GetElementsOfCategory)
            FilteredElementCollector collector;

            if (view is null)
            {
                collector = new FilteredElementCollector(doc);
            }
            else
            {
                collector = new FilteredElementCollector(doc, view.Id);
            }

            // Arc grids have no single direction, so they can't be a dimension origin here
            return collector
                .OfClass(typeof(Grid))
                .Cast<Grid>()
                .Where(g => g.IsCurved == false)
                .ToList();
        }

        #endregion

        #region Orientation and position

        /// <summary>
        /// How a grid appears on the screen: Horizontal, Vertical or Diagonal.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="view">The view that defines the screen.</param>
        /// <returns>The orientation, or Unknown if grid or view is null, or the grid is not a straight line.</returns>
        public static OrientationInView GetGridOrientation(Grid grid, View view)
        {
            if (grid is null || view is null) { return OrientationInView.Unknown; }

            // "is Line line": only straight grids have a Direction
            if (grid.Curve is Line line)
            {
                // Same rule used for the pipes: one place decides what "vertical" means
                return View_Utilities.GetOrientation(line.Direction, view);
            }
            else
            {
                return OrientationInView.Unknown;
            }
        }

        /// <summary>
        /// The fixed coordinate of a grid on the screen: X for a vertical grid, Y for a horizontal one.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="view">The view that defines the screen.</param>
        /// <returns>The position in feet (same reference as View_Utilities.ToViewCoordinates),
        /// or double.NaN ("not a number") if the grid is not vertical or horizontal on the screen.</returns>
        public static double GetGridPosition(Grid grid, View view)
        {
            var orientation = GetGridOrientation(grid, view);

            // Any point of the grid works: along a vertical grid X doesn't change, along a horizontal one Y doesn't
            var pointInView = View_Utilities.ToViewCoordinates(grid.Curve.GetEndPoint(0), view);

            if (orientation == OrientationInView.Vertical)
            {
                return pointInView.X;
            }
            else if (orientation == OrientationInView.Horizontal)
            {
                return pointInView.Y;
            }
            else
            {
                // NaN = "not a number": a diagonal grid has no single X or Y.
                // Any comparison with NaN is false, so it can never win "nearest".
                return double.NaN;
            }
        }

        #endregion

        #region Nearest grid

        /// <summary>
        /// Finds the grid nearest to a part, among the grids with the given orientation.
        /// Vertical grids are compared by X (for vertical pipes, and the X of risers);
        /// horizontal grids by Y (for horizontal pipes, and the Y of risers).
        /// A riser calls this twice: once with Vertical, once with Horizontal.
        /// </summary>
        /// <param name="info">The part's record card (needs MidPointInView).</param>
        /// <param name="grids">The candidate grids (e.g. from GetGrids).</param>
        /// <param name="view">The view that defines the screen.</param>
        /// <param name="gridOrientation">Vertical or Horizontal: which grids compete.</param>
        /// <returns>The nearest grid with distance and side, or null if nothing to compare
        /// (no middle point, no grid with that orientation, or gridOrientation is not Vertical/Horizontal).</returns>
        public static GridMatchInfo GetNearestGrid(FabPartInfo info, List<Grid> grids, View view, OrientationInView gridOrientation)
        {
            if (grids is null || view is null) { return null; }

            // 1. Where is the part? X for vertical grids, Y for horizontal grids
            var partPosition = FabricationPart_Utilities.GetPositionInView(info, gridOrientation);

            // NaN = no middle point, or Diagonal / Riser / Unknown asked: nothing to compare
            if (double.IsNaN(partPosition) == true) { return null; }

            // 2. Competition: which parallel grid is closest? Best so far: null = "no candidate yet"
            GridMatchInfo nearest = null;

            foreach (var grid in grids)
            {
                // Only grids parallel to what we measure. "continue" = skip to the next grid.
                if (GetGridOrientation(grid, view) != gridOrientation) { continue; }

                // Signed distance: positive = the part is right of / above the grid
                var signedDistance = partPosition - GetGridPosition(grid, view);
                var distance = Math.Abs(signedDistance);

                // First candidate, or closer than the best so far
                if (nearest is null || distance < nearest.DistanceFeet)
                {
                    nearest = new GridMatchInfo();
                    nearest.Grid = grid;
                    nearest.DistanceFeet = distance;
                    nearest.IsOnPositiveSide = signedDistance > 0;
                }
            }

            return nearest;
        }

        #endregion

        #region Keys

        /// <summary>
        /// Text that describes a GridMatchInfo, for debug messages. Example: "grid 11, 8' - 6", right".
        /// The distance uses the project's length units, to compare with a manual measure in Revit.
        /// </summary>
        /// <param name="match">The nearest grid result (from GetNearestGrid).</param>
        /// <param name="gridOrientation">The orientation used in GetNearestGrid: it names the side (right/left or above/below).</param>
        /// <returns>The description, or "no grid" if match is null.</returns>
        public static string GridMatchKey(GridMatchInfo match, OrientationInView gridOrientation)
        {
            if (match is null || match.Grid is null) { return "no grid"; }

            // Distance as the project shows it (e.g. 8' - 6"), instead of decimal feet
            var doc = match.Grid.Document;
            var distanceText = UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, match.DistanceFeet, false);

            // The side's name depends on the grid: a vertical grid has right/left, a horizontal one above/below
            string sideText;

            if (gridOrientation == OrientationInView.Vertical)
            {
                if (match.IsOnPositiveSide == true)
                {
                    sideText = "right";
                }
                else
                {
                    sideText = "left";
                }
            }
            else
            {
                if (match.IsOnPositiveSide == true)
                {
                    sideText = "above";
                }
                else
                {
                    sideText = "below";
                }
            }

            return $"grid {match.Grid.Name}, {distanceText}, {sideText}";
        }

        #endregion
    }
}
