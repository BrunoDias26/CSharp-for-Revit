namespace CSharp_for_Revit.Utilities
{
    /// <summary>
    /// The answer to "which grid is nearest to this part?": the grid, the distance and the side, together.
    /// Created by Grid_Utilities.GetNearestGrid.
    /// </summary>
    public class GridMatchInfo
    {
        /// <summary>The nearest grid: the origin of the baseline dimension.</summary>
        public Grid Grid { get; set; }

        /// <summary>Distance from the grid to the part, in feet, always positive: the value the dimension will show.</summary>
        public double DistanceFeet { get; set; }

        /// <summary>
        /// Which side of the grid the part is on, on the screen:
        /// true = right of a vertical grid / above a horizontal grid; false = left / below.
        /// Parts with the same Grid and the same side form one dimension ladder.
        /// </summary>
        public bool IsOnPositiveSide { get; set; }
    }
}
