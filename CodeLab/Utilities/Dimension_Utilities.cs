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
        /// The dimension type the Linear dimension tool is set to in this project
        /// (the type shown in the Type Selector of Annotate > Linear). Each project keeps its own.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <returns>The dimension type, or null if the project has none set.</returns>
        public static DimensionType GetDefaultLinearDimensionType(Document doc)
        {
            if (doc is null) { return null; }

            var typeId = doc.GetDefaultElementTypeId(ElementTypeGroup.LinearDimensionType);

            if (typeId == ElementId.InvalidElementId) { return null; }

            // "as": null if the element is not a DimensionType (instead of an error)
            return doc.GetElement(typeId) as DimensionType;
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
        /// Gets the reference of the centerline end NEAREST to the dimension line, to be used by a dimension.
        /// Made for pipes that are almost parallel (up to 1°): Revit refuses their line in a dimension,
        /// but accepts a point. The two ends give different values when the pipe is off, so the end on the side
        /// of the dimension line is the one the reader expects.
        /// </summary>
        /// <param name="element">A pipe or fabrication part.</param>
        /// <param name="view">The view where the dimension will be created.</param>
        /// <param name="gridOrientation">Vertical (the dimension measures X) or Horizontal (it measures Y).</param>
        /// <param name="linePositionInView">Where the dimension line is, on the screen axis across it
        /// (Y for dimensions that measure X, X for dimensions that measure Y), in feet.</param>
        /// <returns>The end point reference, or null if not found or if it points to another element.</returns>
        public static Reference GetCenterlineNearestEndPointReference(Element element, View view, OrientationInView gridOrientation, double linePositionInView)
        {
            var centerline = FindCenterlineCurve(element, view, out var cameFromInstance);
            if (centerline is null) { return null; }

            var start = View_Utilities.ToViewCoordinates(centerline.GetEndPoint(0), view);
            var end = View_Utilities.ToViewCoordinates(centerline.GetEndPoint(1), view);

            // Distance to the dimension line, measured across it:
            // Y for dimensions that measure X, X for dimensions that measure Y
            double startDistance;
            double endDistance;

            if (gridOrientation == OrientationInView.Vertical)
            {
                startDistance = Math.Abs(start.Y - linePositionInView);
                endDistance = Math.Abs(end.Y - linePositionInView);
            }
            else
            {
                startDistance = Math.Abs(start.X - linePositionInView);
                endDistance = Math.Abs(end.X - linePositionInView);
            }

            // 0 = start point of the centerline, 1 = end point
            int nearestIndex;

            if (startDistance <= endDistance)
            {
                nearestIndex = 0;
            }
            else
            {
                nearestIndex = 1;
            }

            var endPointReference = centerline.GetEndPointReference(nearestIndex);
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
        /// Must run inside an open Transaction. The first row is placed at linePositionInView;
        /// use AlignOuterRow afterwards to stack it outward and align its outermost row.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="view">The view where the dimension is created.</param>
        /// <param name="ladder">The ladder (from GetLadders).</param>
        /// <param name="dimensionType">The dimension type (Baseline style).</param>
        /// <param name="linePositionInView">Where the dimension line goes, on the screen axis across it:
        /// Y for dimensions that measure X, X for dimensions that measure Y (feet, see View_Utilities.ToViewCoordinates).</param>
        /// <returns>The new dimension, or null if no part had a valid centerline reference.</returns>
        public static Dimension CreateBaselineDimension(Document doc, View view, DimensionLadderInfo ladder, DimensionType dimensionType, double linePositionInView)
        {
            if (doc is null || view is null || ladder is null || dimensionType is null) { return null; }
            if (ladder.Parts.Count == 0) { return null; }

            // References: the grid is the origin (first), then the parts from the nearest to the farthest
            var references = new ReferenceArray();
            references.Append(new Reference(ladder.Grid));

            var partReferenceCount = 0;

            foreach (var info in ladder.Parts)
            {
                // Three cases:
                // - Risers: a point of the centerline, because in plan the line points out of the screen;
                //   if the point fails, try the line.
                // - Pipes EXACTLY parallel to the grids: the centerline (a line).
                // - Pipes almost parallel (up to 1°): the centerline end nearest to the dimension line. Revit refuses
                //   a line that is not exactly parallel, and that breaks the WHOLE dimension (seen on 2026-10-28).
                Reference reference;

                if (info.Orientation == OrientationInView.Riser)
                {
                    reference = GetCenterlineEndPointReference(info.Part, view);

                    if (reference is null)
                    {
                        reference = GetCenterlineReference(info.Part, view);
                    }
                }
                else if (FabricationPart_Utilities.IsExactlyParallel(info, ladder.GridOrientation) == true)
                {
                    reference = GetCenterlineReference(info.Part, view);
                }
                else
                {
                    // No fallback to the line here: it would break the whole dimension again
                    reference = GetCenterlineNearestEndPointReference(info.Part, view, ladder.GridOrientation, linePositionInView);
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
                // Measures X: a horizontal line, at the requested height
                startInView = new XYZ(gridPosition, linePositionInView, depth);
                endInView = new XYZ(farthestPosition, linePositionInView, depth);
            }
            else
            {
                // Measures Y: a vertical line, at the requested X
                startInView = new XYZ(linePositionInView, gridPosition, depth);
                endInView = new XYZ(linePositionInView, farthestPosition, depth);
            }

            // The line starts at the grid: that keeps the grid as the origin of the baseline
            var line = Line.CreateBound(
                View_Utilities.FromViewCoordinates(startInView, view),
                View_Utilities.FromViewCoordinates(endInView, view));

            return doc.Create.NewDimension(view, line, references, dimensionType);
        }

        /// <summary>
        /// Makes the stack grow outward (largest value farthest from the pipes) and moves the dimension
        /// so that its outermost row lies on the target line. Must run inside an open Transaction.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="view">The view of the dimension.</param>
        /// <param name="dimension">A baseline dimension just created by CreateBaselineDimension.</param>
        /// <param name="gridOrientation">Vertical (the dimension measures X) or Horizontal (it measures Y).</param>
        /// <param name="firstRowInView">Where the dimension line was created (the linePositionInView used).</param>
        /// <param name="targetInView">Where the outermost row must go, on the same screen axis (feet).</param>
        /// <param name="outwardSign">+1 if "outward" is towards bigger screen Y (or X), -1 if towards smaller.</param>
        public static void AlignOuterRow(Document doc, View view, Dimension dimension, OrientationInView gridOrientation, double firstRowInView, double targetInView, double outwardSign)
        {
            if (doc is null || view is null || dimension is null) { return; }

            // Text positions are only up to date after a regeneration
            doc.Regenerate();

            // A dimension with a single value reports 0 segments: that is 1 row
            var rowCount = dimension.NumberOfSegments;
            if (rowCount < 1) { rowCount = 1; }

            // Baseline Offset = the step between rows, in paper feet (e.g. 1/4"). It can be negative.
            var offsetParameter = dimension.get_Parameter(BuiltInParameter.BASELINE_DIM_OFFSET);
            var stepPaperFeet = 0.0;

            if (offsetParameter is not null)
            {
                stepPaperFeet = Math.Abs(offsetParameter.AsDouble());
            }

            // 1. Stack direction: MEASURED, never deduced from the sign. The same sign stacks up or down
            //    depending on the direction of the dimension line (found in August, and seen again on 2026-10-20).
            if (rowCount > 1 && offsetParameter is not null && offsetParameter.IsReadOnly == false)
            {
                var growth = GetStackGrowth(view, dimension, gridOrientation);

                // growth and outwardSign with opposite signs = the stack grew towards the pipes: flip it
                if (growth * outwardSign < 0)
                {
                    offsetParameter.Set(-offsetParameter.AsDouble());
                    doc.Regenerate();
                }
            }

            // 2. The outermost row: the first row is the dimension line, each next row one step further out.
            //    The step on the model = step on paper x view scale (1/4" at 1:96 = 2 ft).
            var stepModelFeet = stepPaperFeet * view.Scale;
            var outerRowInView = firstRowInView + (rowCount - 1) * stepModelFeet * outwardSign;
            var distanceToMove = targetInView - outerRowInView;

            if (Math.Abs(distanceToMove) < 1e-9) { return; }

            // Move across the dimension line: up/down for dimensions that measure X, right/left for Y
            XYZ moveDirection;

            if (gridOrientation == OrientationInView.Vertical)
            {
                moveDirection = view.UpDirection;
            }
            else
            {
                moveDirection = view.RightDirection;
            }

            ElementTransformUtils.MoveElement(doc, dimension.Id, moveDirection * distanceToMove);
        }

        /// <summary>
        /// Which way the stack grows, measured on the texts: position of the largest value minus position of the
        /// smallest, on the screen axis across the dimension line. Positive = it grows towards bigger Y (or X).
        /// </summary>
        /// <returns>The difference in feet, or 0 if it can't be measured.</returns>
        private static double GetStackGrowth(View view, Dimension dimension, OrientationInView gridOrientation)
        {
            DimensionSegment smallest = null;
            DimensionSegment largest = null;

            // Segments is an old collection type (without <T>): the explicit type in the foreach does the cast
            foreach (DimensionSegment segment in dimension.Segments)
            {
                // Value is "double?" (it can be empty): HasValue checks it, .Value reads it
                if (segment.Value.HasValue == false || segment.TextPosition is null) { continue; }

                if (smallest is null || segment.Value.Value < smallest.Value.Value)
                {
                    smallest = segment;
                }

                if (largest is null || segment.Value.Value > largest.Value.Value)
                {
                    largest = segment;
                }
            }

            if (smallest is null || largest is null) { return 0.0; }

            var largestPosition = View_Utilities.ToViewCoordinates(largest.TextPosition, view);
            var smallestPosition = View_Utilities.ToViewCoordinates(smallest.TextPosition, view);

            if (gridOrientation == OrientationInView.Vertical)
            {
                return largestPosition.Y - smallestPosition.Y;
            }
            else
            {
                return largestPosition.X - smallestPosition.X;
            }
        }

        #endregion
    }
}
