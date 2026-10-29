namespace CSharp_for_Revit.Utilities
{
    /// <summary>
    /// The record card of one dimension "ladder": one baseline dimension that starts at a grid
    /// and measures the parts on one side of it. Created by Dimension_Utilities.GetLadders.
    /// </summary>
    public class DimensionLadderInfo
    {
        /// <summary>The origin grid (the first reference of the baseline dimension).</summary>
        public Grid Grid { get; set; }

        /// <summary>The grid's orientation on the screen: Vertical = the dimension measures X; Horizontal = Y.</summary>
        public OrientationInView GridOrientation { get; set; }

        /// <summary>Side of the grid: true = right / above; false = left / below.</summary>
        public bool IsOnPositiveSide { get; set; }

        /// <summary>
        /// The parts measured by this dimension, from the nearest to the farthest from the grid.
        /// "= new ..." starts it as an empty list, so Parts.Add never finds a null.
        /// </summary>
        public List<FabPartInfo> Parts { get; set; } = new List<FabPartInfo>();
    }
}
