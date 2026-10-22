using View = Autodesk.Revit.DB.View;

namespace CSharp_for_Revit.Utilities
{
    /// <summary>
    /// Methods about MEP Fabrication pipework parts (pipes, fittings, valves...): collect, describe and filter.
    /// </summary>
    public static class FabricationPart_Utilities
    {
        #region Get

        /// <summary>
        /// Collects the fabrication pipework parts of the document.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="view">Optional: if given, only the parts visible in that view. If null, the whole model.</param>
        /// <param name="cids">Optional: if given, only the parts with one of these CIDs (e.g. FabricationCid.Pipe).
        /// If null or empty, no CID filter: all parts are returned.</param>
        /// <returns>A list of fabrication parts (empty if none, or if doc is null).</returns>
        public static List<FabricationPart> GetFabricationParts(Document doc, View view = null, List<FabricationCid> cids = null)
        {
            if (doc is null) { return new List<FabricationPart>(); }

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

            // Category first: hangers and ductwork are also FabricationPart, so OfClass alone would bring them too.
            // After both filters every element is a FabricationPart, so Cast is safe.
            var parts = collector
                .OfCategory(BuiltInCategory.OST_FabricationPipework)
                .OfClass(typeof(FabricationPart))
                .Cast<FabricationPart>()
                .ToList();

            // The CID rule lives in FilterByCid (the same rule is used for the user's selection)
            return FilterByCid(parts, cids);
        }

        #endregion

        #region Filter

        /// <summary>
        /// Keeps, from any list of elements (e.g. the user's selection), only the fabrication pipework parts
        /// with the given CIDs. Same rules as GetFabricationParts, for elements that didn't come from a collector.
        /// </summary>
        /// <param name="elements">Any elements: walls, hangers, pipes... mixed.</param>
        /// <param name="cids">Optional: only these CIDs. If null or empty, no CID filter.</param>
        /// <returns>The fabrication pipework parts that pass (empty if elements is null).</returns>
        public static List<FabricationPart> FilterFabricationParts(List<Element> elements, List<FabricationCid> cids = null)
        {
            if (elements is null) { return new List<FabricationPart>(); }

            // Same category as GetFabricationParts: hangers and ductwork are also FabricationPart
            var pipeworkCategoryId = new ElementId(BuiltInCategory.OST_FabricationPipework);

            // OfType (not Cast): the list is mixed, OfType skips what is not a FabricationPart without an error
            var parts = elements
                .OfType<FabricationPart>()
                .Where(p => p.Category is not null && p.Category.Id == pipeworkCategoryId)
                .ToList();

            return FilterByCid(parts, cids);
        }

        /// <summary>
        /// Keeps only the parts with one of the given CIDs. The single place of the CID rule.
        /// </summary>
        /// <param name="parts">The fabrication parts.</param>
        /// <param name="cids">The CIDs to keep. If null or empty, no filter: all parts are returned.</param>
        /// <returns>The parts that pass (empty if parts is null).</returns>
        public static List<FabricationPart> FilterByCid(List<FabricationPart> parts, List<FabricationCid> cids)
        {
            if (parts is null) { return new List<FabricationPart>(); }

            // No CID filter: return everything
            if (cids is null || cids.Count == 0) { return parts; }

            // ItemCustomId = the CID that comes from the CADmep database, as an int.
            // (FabricationCid) puts the int under the enum's label, so it can be compared with the list.
            return parts
                .Where(p => cids.Contains((FabricationCid)p.ItemCustomId))
                .ToList();
        }

        #endregion

        #region Record cards (FabPartInfo)

        /// <summary>
        /// Fills the record card of one part, as seen in one view.
        /// </summary>
        /// <param name="part">The fabrication part.</param>
        /// <param name="view">The view that defines the screen (Right / Up / depth).</param>
        /// <returns>The filled FabPartInfo, or null if part or view is null.
        /// A part without exactly 2 ends (tee, cap...) comes with Orientation = Unknown and no points.</returns>
        public static FabPartInfo GetFabPartInfo(FabricationPart part, View view)
        {
            if (part is null || view is null) { return null; }

            // A new, empty card for this part only
            var info = new FabPartInfo();

            // Data that comes straight from the part
            info.Part = part;
            info.Cid = (FabricationCid)part.ItemCustomId;
            info.LengthFeet = part.CenterlineLength;
            info.DiameterFeet = Element_Utilities.GetDoubleParameterValue(part, BuiltInParameter.FABRICATION_PART_DIAMETER_IN);

            // Ends: only parts with exactly 2 get points and an orientation
            var endPoints = Connector_Utilities.GetEndPoints(part);
            if (endPoints.Count != 2) { return info; }

            info.StartPointInView = View_Utilities.ToViewCoordinates(endPoints[0], view);
            info.EndPointInView = View_Utilities.ToViewCoordinates(endPoints[1], view);

            // Middle point = average of the two ends
            info.MidPointInView = (info.StartPointInView + info.EndPointInView) / 2.0;

            // Orientation on the screen
            var direction = Connector_Utilities.GetDirection(part);
            info.Orientation = View_Utilities.GetOrientation(direction, view);

            return info;
        }

        /// <summary>
        /// Fills the record cards of many parts: one card per part.
        /// </summary>
        /// <param name="parts">The fabrication parts (e.g. from GetFabricationParts).</param>
        /// <param name="view">The view that defines the screen.</param>
        /// <returns>One FabPartInfo per part, in the same order (empty if parts is null).</returns>
        public static List<FabPartInfo> GetFabPartInfos(List<FabricationPart> parts, View view)
        {
            var infos = new List<FabPartInfo>();

            if (parts is null) { return infos; }

            foreach (var part in parts)
            {
                var info = GetFabPartInfo(part, view);

                if (info is not null)
                {
                    infos.Add(info);
                }
            }

            return infos;
        }

        /// <summary>
        /// The coordinate of a part that a dimension measures, on the screen:
        /// Vertical = its X (measured from vertical grids), Horizontal = its Y (measured from horizontal grids).
        /// Uses the middle point: with the angle tolerance the two ends may differ a little, the middle is their average.
        /// </summary>
        /// <param name="info">The part's record card (needs MidPointInView).</param>
        /// <param name="axis">Vertical (give me X) or Horizontal (give me Y).</param>
        /// <returns>The position in feet, or double.NaN ("not a number") if info has no middle point
        /// or axis is not Vertical/Horizontal.</returns>
        public static double GetPositionInView(FabPartInfo info, OrientationInView axis)
        {
            if (info is null || info.MidPointInView is null) { return double.NaN; }

            if (axis == OrientationInView.Vertical)
            {
                return info.MidPointInView.X;
            }
            else if (axis == OrientationInView.Horizontal)
            {
                return info.MidPointInView.Y;
            }
            else
            {
                return double.NaN;
            }
        }

        /// <summary>
        /// Removes parts that are closer than the tolerance to another part, along one axis:
        /// one dimension witness line is enough for both. Who stays: the bigger diameter;
        /// same diameter, the longer one (a pipe wins over a short piece of the same run).
        /// </summary>
        /// <param name="infos">The record cards.</param>
        /// <param name="axis">Vertical = compare X (horizontal dimensions); Horizontal = compare Y (vertical dimensions).</param>
        /// <param name="toleranceFeet">Positions closer than this are "the same", in feet.</param>
        /// <returns>The parts that stay (empty if infos is null). Parts without a position are left out.</returns>
        public static List<FabPartInfo> RemoveClosePositions(List<FabPartInfo> infos, OrientationInView axis, double toleranceFeet)
        {
            var kept = new List<FabPartInfo>();

            if (infos is null) { return kept; }

            // Priority order: bigger diameter first; same diameter, longer first.
            // Going through the list in this order, the first part at each position is the one that stays.
            var ordered = infos
                .OrderByDescending(i => i.DiameterFeet)
                .ThenByDescending(i => i.LengthFeet)
                .ToList();

            foreach (var candidate in ordered)
            {
                var position = GetPositionInView(candidate, axis);

                // No middle point: can't be compared, so it can't be dimensioned either
                if (double.IsNaN(position) == true) { continue; }

                // Is there already a kept part at (almost) the same position?
                var isTooClose = false;

                foreach (var keptInfo in kept)
                {
                    var keptPosition = GetPositionInView(keptInfo, axis);

                    if (Math.Abs(position - keptPosition) < toleranceFeet)
                    {
                        isTooClose = true;
                        break;
                    }
                }

                if (isTooClose == false)
                {
                    kept.Add(candidate);
                }
            }

            return kept;
        }

        /// <summary>
        /// Keeps only the cards with one orientation (e.g. only the vertical pipes).
        /// </summary>
        /// <param name="infos">The record cards.</param>
        /// <param name="orientation">The orientation to keep.</param>
        /// <returns>The cards with that orientation (empty if infos is null).</returns>
        public static List<FabPartInfo> FilterByOrientation(List<FabPartInfo> infos, OrientationInView orientation)
        {
            if (infos is null) { return new List<FabPartInfo>(); }

            return infos
                .Where(i => i.Orientation == orientation)
                .ToList();
        }

        #endregion

        #region Keys

        /// <summary>
        /// Text that describes a card, for debug messages and Debug.WriteLine.
        /// Example: "#123456 Pipe Vertical Ø0.333 ft X=12.50 Y=40.10".
        /// </summary>
        /// <param name="info">The record card.</param>
        /// <returns>The description, or "(null)" if info is null.</returns>
        public static string FabPartKey(FabPartInfo info)
        {
            if (info is null) { return "(null)"; }

            var key = $"#{Element_Utilities.IntegerValue(info.Part.Id)} {info.Cid} {info.Orientation} Ø{info.DiameterFeet:0.###} ft";

            // Parts without 2 ends have no middle point
            if (info.MidPointInView is not null)
            {
                key = key + $" X={info.MidPointInView.X:0.00} Y={info.MidPointInView.Y:0.00}";
            }

            return key;
        }

        #endregion
    }
}
