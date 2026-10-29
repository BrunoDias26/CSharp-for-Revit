namespace CodeLab.Utilities
{
    /// <summary>
    /// Methods about units. Revit's API always works in feet for lengths, so these methods convert at the edges:
    /// values written in inches or millimeters go in as feet, and lengths shown to the user come out in the
    /// project's own units. Tolerances from a standard (e.g. "1/8 inch") are NOT read from the project units:
    /// they keep their meaning in any project.
    /// </summary>
    public static class Units_Utilities
    {
        #region Convert

        /// <summary>
        /// Converts inches into feet (Revit's internal length unit). Example: 1" -> 0.0833 ft.
        /// </summary>
        /// <param name="inches">A length in inches.</param>
        /// <returns>The same length in feet.</returns>
        public static double InchesToFeet(double inches)
        {
            return UnitUtils.ConvertToInternalUnits(inches, UnitTypeId.Inches);
        }

        /// <summary>
        /// Converts millimeters into feet (Revit's internal length unit). Example: 25 mm -> 0.0820 ft.
        /// </summary>
        /// <param name="millimeters">A length in millimeters.</param>
        /// <returns>The same length in feet.</returns>
        public static double MillimetersToFeet(double millimeters)
        {
            return UnitUtils.ConvertToInternalUnits(millimeters, UnitTypeId.Millimeters);
        }

        /// <summary>
        /// Converts feet (Revit's internal length unit) into inches. Example: 0.5 ft -> 6".
        /// </summary>
        /// <param name="feet">A length in feet.</param>
        /// <returns>The same length in inches.</returns>
        public static double FeetToInches(double feet)
        {
            return UnitUtils.ConvertFromInternalUnits(feet, UnitTypeId.Inches);
        }

        #endregion

        #region Format

        /// <summary>
        /// A length as text, the way the project shows it (e.g. 13' - 11 7/8" or 4267 mm),
        /// using the project's length units (Manage > Project Units).
        /// </summary>
        /// <param name="doc">The document whose units are used.</param>
        /// <param name="feet">The length in feet (Revit's internal unit).</param>
        /// <returns>The formatted text; in decimal feet if there is no document.</returns>
        public static string FormatLength(Document doc, double feet)
        {
            if (doc is null) { return $"{feet:0.000} ft"; }

            // false = for display (true would be the format used while editing a value in a text box)
            return UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, feet, false);
        }

        #endregion
    }
}
