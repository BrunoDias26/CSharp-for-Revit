using View = Autodesk.Revit.DB.View;

namespace CSharp_for_Revit.Utilities
{
    /// <summary>
    /// Methods about how a view is oriented on the user's screen.
    /// Every view has three arrows, in model coordinates:
    /// RightDirection (screen right), UpDirection (screen top) and ViewDirection (out of the screen, towards the viewer).
    /// In a rotated view the arrows rotate with it, so everything here follows the screen, not the project.
    /// </summary>
    public static class View_Utilities
    {
        #region Constants

        /// <summary>
        /// Default angle tolerance, in degrees, to accept a line as horizontal, vertical or riser.
        /// </summary>
        public const double ORIENTATION_TOLERANCE_DEGREES = 1.0;

        #endregion

        #region Screen coordinates

        /// <summary>
        /// Converts a model point (or a direction) to the view's screen coordinates.
        /// </summary>
        /// <param name="point">A point or a direction in model coordinates.</param>
        /// <param name="view">The view that defines the screen.</param>
        /// <returns>An XYZ, in feet, where X = towards the screen's right, Y = towards the screen's top and
        /// Z = depth towards the viewer (in a plan view, higher Z = higher elevation).
        /// null if point or view is null.</returns>
        public static XYZ ToViewCoordinates(XYZ point, View view)
        {
            if (point is null || view is null) { return null; }

            // DotProduct = "how much of the point lies along this arrow": its shadow on a ruler laid on the arrow.
            // The values are measured from the model origin, not from the crop box: fine for comparing
            // positions with each other (pipe x grid), which is all we need.
            var x = point.DotProduct(view.RightDirection);
            var y = point.DotProduct(view.UpDirection);
            var depth = point.DotProduct(view.ViewDirection);

            return new XYZ(x, y, depth);
        }

        /// <summary>
        /// The opposite of ToViewCoordinates: turns screen coordinates back into a model point.
        /// </summary>
        /// <param name="viewPoint">X = screen right, Y = screen top, Z = depth, in feet.</param>
        /// <param name="view">The view that defines the screen.</param>
        /// <returns>The point in model coordinates, or null if viewPoint or view is null.</returns>
        public static XYZ FromViewCoordinates(XYZ viewPoint, View view)
        {
            if (viewPoint is null || view is null) { return null; }

            // The three arrows are perpendicular to each other and have length 1,
            // so walking X along Right, Y along Up and Z along ViewDirection lands on the model point
            return view.RightDirection * viewPoint.X
                + view.UpDirection * viewPoint.Y
                + view.ViewDirection * viewPoint.Z;
        }

        #endregion

        #region Orientation

        /// <summary>
        /// Classifies a direction as it appears on the screen: Riser, Horizontal, Vertical or Diagonal.
        /// </summary>
        /// <param name="vector">A direction in model coordinates (any length; e.g. Connector_Utilities.GetDirection).</param>
        /// <param name="view">The view that defines the screen.</param>
        /// <param name="toleranceDegrees">How many degrees off still count as horizontal, vertical or riser.</param>
        /// <returns>The orientation, or Unknown if vector or view is null, or the vector has no length.</returns>
        public static OrientationInView GetOrientation(XYZ vector, View view, double toleranceDegrees = ORIENTATION_TOLERANCE_DEGREES)
        {
            if (vector is null || view is null) { return OrientationInView.Unknown; }
            if (vector.IsZeroLength() == true) { return OrientationInView.Unknown; }

            // The same direction in screen coordinates, with length 1
            var screen = ToViewCoordinates(vector.Normalize(), view);

            // With length-1 vectors, each component is the cosine of the angle to that arrow:
            // 1 = parallel, 0 = perpendicular. "Within the tolerance" = cosine >= cos(tolerance).
            // Math.Cos works in radians, so degrees * PI / 180.
            var minimumCosine = Math.Cos(toleranceDegrees * Math.PI / 180.0);

            // Math.Abs everywhere: the order of the connectors is random, so the vector may point either way

            // 1. Riser first: almost all of the vector points into / out of the screen
            if (Math.Abs(screen.Z) >= minimumCosine) { return OrientationInView.Riser; }

            // 2. The shadow on the screen: drop the depth and measure the shadow alone.
            //    That's why a sloped pipe, or a 45° offset going down, still counts as horizontal or vertical.
            //    shadowLength is never 0 here: if it were, step 1 would have returned Riser.
            var shadowLength = Math.Sqrt(screen.X * screen.X + screen.Y * screen.Y);
            var shadowX = Math.Abs(screen.X) / shadowLength;
            var shadowY = Math.Abs(screen.Y) / shadowLength;

            if (shadowX >= minimumCosine)
            {
                return OrientationInView.Horizontal;
            }
            else if (shadowY >= minimumCosine)
            {
                return OrientationInView.Vertical;
            }
            else
            {
                return OrientationInView.Diagonal;
            }
        }

        #endregion
    }
}
