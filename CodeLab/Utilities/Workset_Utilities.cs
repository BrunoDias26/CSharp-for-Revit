using CSharp_for_Revit.Forms;

namespace CSharp_for_Revit.Utilities
{
    public static class Workset_Utilities
    {
        #region Get

        /// <summary>
        /// Retrieves all user worksets from the document.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="sorted">If true, sorts the worksets alphabetically by name.</param>
        /// <returns>A list of user worksets.</returns>
        public static List<Workset> GetWorksets(Document doc, bool sorted = true)
        {
            // Worksets are not elements: they have their own collector
            var worksets = new FilteredWorksetCollector(doc)
                .OfKind(WorksetKind.UserWorkset)
                .ToWorksets()
                .ToList();

            if (sorted == true)
            {
                worksets = worksets.OrderBy(w => w.Name).ToList();
            }

            return worksets;
        }

        #endregion

        #region Forms

        /// <summary>
        /// Opens a list form allowing the user to select workset(s) from the active Revit document.
        /// </summary>
        /// <param name="doc">The active Revit document containing the worksets.</param>
        /// <param name="title">The window title of the list form. If null, a default title is used.</param>
        /// <param name="multiSelect">If true, the user can check several worksets; otherwise, only one.</param>
        /// <param name="sorted">If true, the worksets are sorted by name before being displayed.</param>
        /// <returns>object containing the user's selection status and the chosen workset(s).</returns>
        public static FormResult SelectWorksets(Document doc, string title = null, bool multiSelect = true, bool sorted = true)
        {
            // Default title: only when none was given (null), plural or singular
            if (title is null)
            {
                if (multiSelect == true)
                {
                    title = "Select workset(s) from the list:";
                }
                else
                {
                    title = "Select a workset from the list";
                }
            }

            var worksets = GetWorksets(doc, sorted);

            var keys = worksets.Select(w => w.Name).ToList();
            var values = worksets.Cast<object>().ToList();

            return Custom.SelectFromList(keys, values, title, multiSelect);
        }

        #endregion
    }
}
