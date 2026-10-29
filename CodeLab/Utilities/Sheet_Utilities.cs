using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using CodeLab.Forms;

namespace CodeLab.Utilities
{
    internal class Sheet_Utilities
    {
        /// <summary>
        /// Retrieves a processed list of ViewSheets elements from the active document, with an option to exclude placeholder sheets.
        /// </summary>
        /// <param name="doc">The active Revit database document.</param>
        /// <param name="includePlaceholders"> includes or not unplaced/placeholder sheets; otherwise, returns only physical sheets.</param>
        /// <returns>A list of ViewSheets elements matching the filter criteria</returns>
        public static List<ViewSheet> GetProjectSheets(Document doc, bool includePlaceholders = false)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .ToList();


            if (includePlaceholders == false)
            {
                return collector.Where(s => s.IsPlaceholder == false).ToList();
            }

            return collector;
        }

        /// <summary>
        /// Retrieves a processed list of ViewSheets elements from the active document, 
        /// with options to exclude placeholder sheets and sort by sheet number.
        /// </summary>
        /// <param name="doc">The active Revit database document.</param>
        /// <param name="includePlaceholders">If set to true, includes unplaced/placeholder sheets; otherwise, returns only physical sheets.</param>
        /// <param name="sorted">If set to true, sorts the final collection alphabetically by Sheet Number.</param>
        /// <returns>A list of ViewSheets elements matching the filter criteria.</returns>
        public static List<ViewSheet> GetProjectSheets(Document doc, bool includePlaceholders = false, bool sorted = true)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .ToList();

            if (includePlaceholders == false)
            {
                collector = collector.Where(s => s.IsPlaceholder == false).ToList();
            }

            if (sorted == true)
            {
                collector = collector.OrderBy(s => s.SheetNumber).ToList();
            }

            return collector;
        }

        /// <summary>
        /// Builds the text used to display a sheet in forms, as "Number: Name".
        /// </summary>
        /// <param name="sheet">The sheet to describe.</param>
        /// <param name="includeId">If true, appends the ElementId at the end.</param>
        /// <returns>The display key of the sheet.</returns>
        public static string SheetKey(ViewSheet sheet, bool includeId = false)
        {
            if (sheet == null)
            {
                return "Invalid Sheet";
            }

            if (includeId)
            {
                return $"{sheet.SheetNumber}: {sheet.Name} [{sheet.Id.ToString()}]";
            }
            else
            {
                return $"{sheet.SheetNumber}: {sheet.Name}";
            }
        }

        /// <summary>
        /// Builds the file name used when exporting a sheet: "ProjectNumber-SheetNumber [Revision] - SheetName".
        /// </summary>
        /// <param name="sheet">The sheet to name.</param>
        /// <param name="doc">The related document. If null, the sheet's own document is used.</param>
        /// <param name="valueIfInvalid">Text used when the sheet has no current revision.</param>
        /// <returns>The export file name (without extension).</returns>
        public static string SheetExportKey(ViewSheet sheet, Document doc = null, string valueIfInvalid = "-")
        {
            if (doc is null)
            {
                doc = sheet.Document;
            }

            var projectNumber = doc.ProjectInformation.Number;
            // BuiltInParameter instead of the name "Current Revision": works in any Revit language
            var sheetRevision = Element_Utilities.GetStringParameterValue(sheet, BuiltInParameter.SHEET_CURRENT_REVISION,
                valueIfInvalid: valueIfInvalid, emptyIsInvalid: true);

            // e.g. "1234-A-001 [P02] - Ground Floor"
            return $"{projectNumber}-{sheet.SheetNumber} [{sheetRevision}] - {sheet.Name}";
        }

        /// <summary>
        /// Opens a list form allowing the user to select sheet(s) from the active Revit document.
        /// </summary>
        /// <param name="doc">The active Revit document containing the sheets.</param>
        /// <param name="title">The window title of the list form. If null, a default title is used.</param>
        /// <param name="multiSelect">If true, the user can check several sheets; otherwise, only one.</param>
        /// <param name="includePlaceholders">If true, placeholder sheets are listed too.</param>
        /// <param name="sorted">If true, the sheets are sorted by number before being displayed.</param>
        /// <returns>object containing the user's selection status and the chosen sheet(s).</returns>
        public static FormResult SelectSheets(Document doc, string title = null, bool multiSelect = true, bool includePlaceholders = false, bool sorted = true)
        {
            // Default title: only when none was given (null), plural or singular
            if (title is null)
            {
                if (multiSelect == true)
                {
                    title = "Select sheet(s) from the list:";
                }
                else
                {
                    title = "Select a sheet from the list";
                }
            }

            var sheets = GetProjectSheets(doc, includePlaceholders, sorted);

            var keys = sheets.Select(s => SheetKey(s)).ToList();
            var values = sheets.Cast<object>().ToList();

            return Custom.SelectFromList(keys, values, title, multiSelect);
        }

        /// <summary>
        /// Adds a revision to a sheet, keeping the revisions the sheet already has.
        /// Must be called inside a Transaction.
        /// </summary>
        /// <param name="sheet">The sheet that receives the revision.</param>
        /// <param name="revision">The revision to add.</param>
        /// <returns>Result.Succeeded if the revision was added; Result.Failed if the sheet already had it.</returns>
        public static Result AddRevision(ViewSheet sheet, Revision revision)
        {
            // SetAdditionalRevisionIds REPLACES the whole list of revisions on the sheet, it does not add one.

            // Get: revisions already on the sheet, e.g. [R1, R2]
            var revisionIds = sheet.GetAdditionalRevisionIds();

            // Nothing to do if the sheet already has it
            if (revisionIds.Contains(revision.Id))
            {
                return Result.Failed;
            }
            else
            {
                // Add: [R1, R2] -> [R1, R2, R3]
                revisionIds.Add(revision.Id);

                // Set: the full list goes back, so the sheet keeps R1, R2 and gets R3
                sheet.SetAdditionalRevisionIds(revisionIds);
                return Result.Succeeded;
            }
        }
    }

}
