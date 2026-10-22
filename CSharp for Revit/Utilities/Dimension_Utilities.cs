using System.Diagnostics;
using View = Autodesk.Revit.DB.View;

namespace CSharp_for_Revit.Utilities
{
    /// <summary>
    /// Methods about dimensions: dimension types, centerline references, ladders (grouping) and creation.
    /// </summary>
    public static class Dimension_Utilities
    {
        #region Get

        /// <summary>
        /// Finds a dimension type by its exact name.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="typeName">The exact type name, as in the Type Selector.</param>
        /// <returns>The dimension type, or null if no type has that name.</returns>
        public static DimensionType GetDimensionTypeByName(Document doc, string typeName)
        {
            if (doc is null || typeName is null) { return null; }

            var dimensionTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .ToList();

            foreach (var dimensionType in dimensionTypes)
            {
                if (dimensionType.Name == typeName)
                {
                    return dimensionType;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the reference of an element's centerline (a LINE), to be used by a dimension.
        /// Good for pipes that lie on the screen (vertical / horizontal in plan).
        /// Recipe validated with pyRevit on MEP Fabrication Pipework (2026-08-20, view G1).
        /// </summary>
        /// <param name="element">A pipe or fabrication part.</param>
        /// <param name="view">The view where the dimension will be created.</param>
        /// <returns>The centerline reference, or null if not found or if it points to another element.</returns>
        public static Reference GetCenterlineReference(Element element, View view)
        {
            var centerline = FindCenterlineCurve(element, view, out var cameFromInstance);
            if (centerline is null) { return null; }

            return QualifyReference(centerline.Reference, element, cameFromInstance);
        }

        /// <summary>
        /// Gets the reference of the first END POINT of an element's centerline, to be used by a dimension.
        /// Made for risers: in plan their centerline points out of the screen and is seen as a point,
        /// so a point reference is the natural thing to dimension to.
        /// </summary>
        /// <param name="element">A pipe or fabrication part.</param>
        /// <param name="view">The view where the dimension will be created.</param>
        /// <returns>The end point reference, or null if not found or if it points to another element.</returns>
        public static Reference GetCenterlineEndPointReference(Element element, View view)
        {
            var centerline = FindCenterlineCurve(element, view, out var cameFromInstance);
            if (centerline is null) { return null; }

            // 0 = start point of the centerline. In plan a riser's two ends are on top of each other, so either works.
            var endPointReference = centerline.GetEndPointReference(0);
            if (endPointReference is null) { return null; }

            return QualifyReference(endPointReference, element, cameFromInstance);
        }

        /// <summary>
        /// Finds the centerline of an element: the longest curve with a reference in its geometry.
        /// The centerline is invisible geometry, so it needs IncludeNonVisibleObjects.
        /// </summary>
        /// <param name="element">A pipe or fabrication part.</param>
        /// <param name="view">The view where the dimension will be created.</param>
        /// <param name="cameFromInstance">Returns true if the curve was inside a GeometryInstance (see QualifyReference).</param>
        /// <returns>The centerline curve, or null if none was found.</returns>
        private static Curve FindCenterlineCurve(Element element, View view, out bool cameFromInstance)
        {
            // "out": the method must give this a value before leaving, on every path
            cameFromInstance = false;

            if (element is null || view is null) { return null; }

            var options = new Options();
            options.ComputeReferences = true;          // without this, the geometry has no references
            options.IncludeNonVisibleObjects = true;   // the centerline is invisible: without this, zero references
            options.View = view;

            var geometry = element.get_Geometry(options);
            if (geometry is null) { return null; }

            // The longest curve with a reference is the centerline.
            // Geometry can be nested (a GeometryInstance holds more geometry), so a "to do" stack is used.
            Curve bestCurve = null;

            var toVisit = new Stack<GeometryElement>();
            var fromInstance = new Stack<bool>();
            toVisit.Push(geometry);
            fromInstance.Push(false);

            while (toVisit.Count > 0)
            {
                var current = toVisit.Pop();
                var currentFromInstance = fromInstance.Pop();

                foreach (var geometryObject in current)
                {
                    if (geometryObject is GeometryInstance instance)
                    {
                        // Look inside it later
                        toVisit.Push(instance.GetInstanceGeometry());
                        fromInstance.Push(true);
                    }
                    else if (geometryObject is Curve curve)
                    {
                        if (curve.Reference is not null)
                        {
                            if (bestCurve is null || curve.Length > bestCurve.Length)
                            {
                                bestCurve = curve;
                                cameFromInstance = currentFromInstance;
                            }
                        }
                    }
                }
            }

            return bestCurve;
        }

        /// <summary>
        /// Makes a reference safe to use: fixes the ":INSTANCE:" trap and checks that it points to the element.
        /// One place for this rule, used by the line and the point references.
        /// </summary>
        /// <param name="reference">A reference taken from the element's geometry.</param>
        /// <param name="element">The element the reference must point to.</param>
        /// <param name="cameFromInstance">True if the reference came from inside a GeometryInstance.</param>
        /// <returns>The safe reference, or null if it can't be fixed or points to another element.</returns>
        private static Reference QualifyReference(Reference reference, Element element, bool cameFromInstance)
        {
            if (reference is null || element is null) { return null; }

            var doc = element.Document;
            var qualified = reference;

            // TRAP (found 2026-08-20): GetInstanceGeometry gives a reference of the SYMBOL (the family geometry
            // shared by many instances), without the ":INSTANCE:" token. Used as is, Revit attaches the dimension
            // to ANOTHER instance with the same geometry, with no error. Fix: prefix it with this element's UniqueId.
            if (cameFromInstance == true)
            {
                var stable = reference.ConvertToStableRepresentation(doc);

                if (stable.Contains(":INSTANCE:") == false)
                {
                    try
                    {
                        qualified = Reference.ParseFromStableRepresentation(doc, $"{element.UniqueId}:0:INSTANCE:{stable}");
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            // Mandatory check: the reference must point to THIS element (the only defense against the trap above)
            var referencedElement = doc.GetElement(qualified);

            if (referencedElement is null || referencedElement.Id != element.Id)
            {
                Debug.WriteLine($"[Dimension] Reference of #{Element_Utilities.IntegerValue(element.Id)} points to another element: skipped.");
                return null;
            }

            return qualified;
        }

        #endregion

        #region Ladders

        /// <summary>
        /// Groups the parts into dimension ladders: same nearest grid AND same side = one baseline dimension.
        /// Parts closer to the grid than minDistanceFeet are left out (the dimension would show ~0).
        /// </summary>
        /// <param name="infos">The record cards (e.g. vertical pipes, already without close positions).</param>
        /// <param name="grids">The grids of the view.</param>
        /// <param name="view">The view that defines the screen.</param>
        /// <param name="gridOrientation">Vertical (dimensions measure X) or Horizontal (dimensions measure Y).</param>
        /// <param name="minDistanceFeet">Minimum distance to the grid, in feet.</param>
        /// <returns>The ladders, each with its parts sorted from the nearest to the farthest.</returns>
        public static List<DimensionLadderInfo> GetLadders(List<FabPartInfo> infos, List<Grid> grids, View view, OrientationInView gridOrientation, double minDistanceFeet)
        {
            // Key = "grid Id|side". Dictionary + TryGetValue: finds the ladder of a key without going through a list
            var laddersByKey = new Dictionary<string, DimensionLadderInfo>();

            if (infos is null) { return new List<DimensionLadderInfo>(); }

            foreach (var info in infos)
            {
                var match = Grid_Utilities.GetNearestGrid(info, grids, view, gridOrientation);

                if (match is null) { continue; }

                // Practically on the grid: the dimension would show 0 and only take space
                if (match.DistanceFeet < minDistanceFeet) { continue; }

                var key = $"{Element_Utilities.IntegerValue(match.Grid.Id)}|{match.IsOnPositiveSide}";

                // "out var ladder": if the key exists, TryGetValue fills ladder and returns true
                if (laddersByKey.TryGetValue(key, out var ladder) == false)
                {
                    ladder = new DimensionLadderInfo();
                    ladder.Grid = match.Grid;
                    ladder.GridOrientation = gridOrientation;
                    ladder.IsOnPositiveSide = match.IsOnPositiveSide;

                    laddersByKey.Add(key, ladder);
                }

                ladder.Parts.Add(info);
            }

            // Sort each ladder from the nearest to the farthest part (the baseline order)
            var ladders = laddersByKey.Values.ToList();

            foreach (var ladder in ladders)
            {
                var gridPosition = Grid_Utilities.GetGridPosition(ladder.Grid, view);

                ladder.Parts = ladder.Parts
                    .OrderBy(i => Math.Abs(FabricationPart_Utilities.GetPositionInView(i, gridOrientation) - gridPosition))
                    .ToList();
            }

            return ladders;
        }

        #endregion

        #region Create

        /// <summary>
        /// Creates the baseline dimension of one ladder: the grid first, then each part's centerline.
        /// Must run inside an open Transaction. The dimension line is placed over the parts (position not refined yet).
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="view">The view where the dimension is created.</param>
        /// <param name="ladder">The ladder (from GetLadders).</param>
        /// <param name="dimensionType">The dimension type (Baseline style).</param>
        /// <returns>The new dimension, or null if no part had a valid centerline reference.</returns>
        public static Dimension CreateBaselineDimension(Document doc, View view, DimensionLadderInfo ladder, DimensionType dimensionType)
        {
            if (doc is null || view is null || ladder is null || dimensionType is null) { return null; }
            if (ladder.Parts.Count == 0) { return null; }

            // References: the grid is the origin (first), then the parts from the nearest to the farthest
            var references = new ReferenceArray();
            references.Append(new Reference(ladder.Grid));

            var partReferenceCount = 0;

            foreach (var info in ladder.Parts)
            {
                // Pipes on the screen: the centerline (a line). Risers: a point of the centerline,
                // because in plan the line points out of the screen; if the point fails, try the line.
                Reference reference;

                if (info.Orientation == OrientationInView.Riser)
                {
                    reference = GetCenterlineEndPointReference(info.Part, view);

                    if (reference is null)
                    {
                        reference = GetCenterlineReference(info.Part, view);
                    }
                }
                else
                {
                    reference = GetCenterlineReference(info.Part, view);
                }

                if (reference is null)
                {
                    Debug.WriteLine($"[Dimension] No centerline reference: {FabricationPart_Utilities.FabPartKey(info)}");
                    continue;
                }

                references.Append(reference);
                partReferenceCount = partReferenceCount + 1;
            }

            if (partReferenceCount == 0) { return null; }

            // Dimension line, in screen coordinates: from the grid to the farthest part
            var gridPosition = Grid_Utilities.GetGridPosition(ladder.Grid, view);
            var farthestPosition = FabricationPart_Utilities.GetPositionInView(ladder.Parts.Last(), ladder.GridOrientation);
            var depth = View_Utilities.ToViewCoordinates(view.Origin, view).Z;

            XYZ startInView;
            XYZ endInView;

            if (ladder.GridOrientation == OrientationInView.Vertical)
            {
                // Measures X: a horizontal line, at the height of the highest part (over the pipes, for now)
                var lineY = ladder.Parts.Max(i => i.MidPointInView.Y);
                startInView = new XYZ(gridPosition, lineY, depth);
                endInView = new XYZ(farthestPosition, lineY, depth);
            }
            else
            {
                // Measures Y: a vertical line, at the right of the rightmost part
                var lineX = ladder.Parts.Max(i => i.MidPointInView.X);
                startInView = new XYZ(lineX, gridPosition, depth);
                endInView = new XYZ(lineX, farthestPosition, depth);
            }

            var line = Line.CreateBound(
                View_Utilities.FromViewCoordinates(startInView, view),
                View_Utilities.FromViewCoordinates(endInView, view));

            return doc.Create.NewDimension(view, line, references, dimensionType);
        }

        #endregion
    }
}
