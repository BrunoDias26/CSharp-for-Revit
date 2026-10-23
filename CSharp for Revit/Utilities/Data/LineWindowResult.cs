namespace CSharp_for_Revit.Utilities
{
    /// <summary>
    /// The answer of UIDocument_Utilities.PickLineWithWindow: the line and the read window the user drew,
    /// in the view's screen coordinates (see View_Utilities.ToViewCoordinates), in feet.
    /// "Result" because it carries a status: check Cancelled before using the rest.
    /// </summary>
    public class LineWindowResult
    {
        /// <summary>True if the user pressed Esc or the overlay could not open. The other fields are then empty.</summary>
        public bool Cancelled { get; set; }

        /// <summary>1st point of the line: where the largest dimension goes.</summary>
        public XYZ LineStartInView { get; set; }

        /// <summary>2nd point of the line (already locked to horizontal or vertical).</summary>
        public XYZ LineEndInView { get; set; }

        /// <summary>The 3rd click: the far edge of the read window, where the largest dimension of each ladder goes.</summary>
        public XYZ ExtentInView { get; set; }

        /// <summary>Horizontal (the dimensions measure X) or Vertical (they measure Y).</summary>
        public OrientationInView LineOrientation { get; set; }

        /// <summary>Corner of the read window with the smallest X and Y (bottom-left on the screen).</summary>
        public XYZ WindowMinInView { get; set; }

        /// <summary>Corner of the read window with the largest X and Y (top-right on the screen).</summary>
        public XYZ WindowMaxInView { get; set; }
    }
}
