namespace CSharp_for_Revit.Utilities
{
    /// <summary>
    /// How a line (a pipe, a grid) appears on the user's screen, in a given view.
    /// Relative to the view, not to the project: in a rotated view the answer rotates with it.
    /// </summary>
    public enum OrientationInView
    {
        /// <summary>Not classified yet, or no direction (e.g. a tee). First on purpose: it is the default value.</summary>
        Unknown,

        /// <summary>Runs left-right on the screen (along view.RightDirection, parallel to the top and bottom edges).</summary>
        Horizontal,

        /// <summary>Runs bottom-top on the screen (along view.UpDirection, parallel to the left and right edges).</summary>
        Vertical,

        /// <summary>Lies on the screen, but neither horizontal nor vertical.</summary>
        Diagonal,

        /// <summary>Points into / out of the screen (view.ViewDirection): a riser or drop, seen as a point in plan.</summary>
        Riser
    }
}
