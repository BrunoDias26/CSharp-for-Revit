namespace CSharp_for_Revit.Utilities
{
    /// <summary>
    /// The "record card" of one fabrication part, as seen in one view.
    /// Filled once by FabricationPart_Utilities.GetFabPartInfo, then only read: connectors and
    /// orientation are not recalculated at every step (filter, proximity, nearest grid...).
    /// Positions are in the view's screen coordinates (see View_Utilities.ToViewCoordinates), in feet.
    /// </summary>
    public class FabPartInfo
    {
        /// <summary>The fabrication part itself.</summary>
        public FabricationPart Part { get; set; }

        /// <summary>The CADmep pattern number (e.g. FabricationCid.Pipe = 2041).</summary>
        public FabricationCid Cid { get; set; }

        /// <summary>First end, in screen coordinates (X = right, Y = up, Z = depth towards the viewer).</summary>
        public XYZ StartPointInView { get; set; }

        /// <summary>Second end, in screen coordinates.</summary>
        public XYZ EndPointInView { get; set; }

        /// <summary>
        /// Middle point, in screen coordinates. The position that a dimension measures:
        /// a Vertical part uses its X, a Horizontal part uses its Y, a Riser uses both.
        /// Z is the depth: in a plan view, the higher one is on top.
        /// </summary>
        public XYZ MidPointInView { get; set; }

        /// <summary>How the part appears on the screen. Unknown if it doesn't have exactly 2 ends.</summary>
        public OrientationInView Orientation { get; set; }

        /// <summary>Main diameter, in feet, from FABRICATION_PART_DIAMETER_IN (0 if not round or not readable).</summary>
        public double DiameterFeet { get; set; }

        /// <summary>Centerline length, in feet.</summary>
        public double LengthFeet { get; set; }
    }
}
