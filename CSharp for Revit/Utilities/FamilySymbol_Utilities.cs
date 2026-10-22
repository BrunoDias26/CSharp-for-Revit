using CSharp_for_Revit.Forms;

namespace CSharp_for_Revit.Utilities
{
    // FamilySymbol = a family TYPE (e.g. "A1 metric - A1"), not a placed instance.
    public static class FamilySymbol_Utilities
    {
        #region Get

        /// <summary>
        /// Retrieves all title block types from the document.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="sorted">If true, sorts the types by their key (category: family - type).</param>
        /// <returns>A list of title block types.</returns>
        public static List<FamilySymbol> GetTitleBlockTypes(Document doc, bool sorted = true)
        {
            // WhereElementIsElementType = only the types, not the title blocks placed on sheets.
            // Safe here because every Title Blocks type is a FamilySymbol (loadable family).
            // In a category with system types (e.g. OST_Walls -> WallType) the Cast below would crash:
            // there, use .OfClass(typeof(FamilySymbol)) or .OfType<FamilySymbol>() instead.
            var ttbTypes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .ToList();

            if (sorted == true)
            {
                ttbTypes = ttbTypes.OrderBy(f => FamilySymbolKey(f)).ToList();
            }

            return ttbTypes;
        }

        #endregion

        #region Keys

        /// <summary>
        /// Builds the text used to display a family type in forms, as "Category: Family - Type".
        /// </summary>
        /// <param name="symbol">The family type.</param>
        /// <param name="includeId">If true, appends the ElementId at the end.</param>
        /// <returns>The display key of the family type.</returns>
        public static string FamilySymbolKey(FamilySymbol symbol, bool includeId = false)
        {
            if (symbol == null) { return "Invalid family type"; }

            // e.g. "Title Blocks: A1 metric - A1"
            var symbolKey = $"{symbol.Family.FamilyCategory.Name}: {symbol.Family.Name} - {symbol.Name}";

            if (includeId == true)
            {
                return $"{symbolKey} [{symbol.Id}]";
            }
            else
            {
                return symbolKey;
            }
        }

        #endregion

        #region Forms

        /// <summary>
        /// Opens a list form allowing the user to select title block type(s) from the active document.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="title">The window title. If null, a default title is used.</param>
        /// <param name="multiSelect">If true, the user can check several types; otherwise, only one.</param>
        /// <param name="sorted">If true, the types are sorted before being displayed.</param>
        /// <returns>object containing the user's selection status and the chosen type(s).</returns>
        public static FormResult SelectTitleBlockTypes(Document doc, string title = null, bool multiSelect = true, bool sorted = true)
        {
            // Default title: only when none was given (null), plural or singular
            if (title is null)
            {
                if (multiSelect == true)
                {
                    title = "Select title block type(s) from the list:";
                }
                else
                {
                    title = "Select a title block type from the list";
                }
            }

            var ttbTypes = GetTitleBlockTypes(doc, sorted);

            var keys = ttbTypes.Select(f => FamilySymbolKey(f)).ToList();
            var values = ttbTypes.Cast<object>().ToList();

            return Custom.SelectFromList(keys, values, title, multiSelect);
        }

        #endregion
    }
}
