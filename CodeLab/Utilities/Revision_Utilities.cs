using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using CSharp_for_Revit.Forms;

namespace CSharp_for_Revit.Utilities
{
    public static class Revision_Utilities
    {
        public static string RevisionKey(this Revision revision, bool includeId = false)
        {
            if (revision == null)
            {
                return "Invalid Revision";
            }

            if (includeId)
            {
                return $"{revision.SequenceNumber}: {revision.RevisionDate} - {revision.Description} [{revision.Id.ToString()}";
            }
            else
            {
                return $"{revision.SequenceNumber}: {revision.RevisionDate} - {revision.Description}";
            }
        }

        public static List<Revision> GetRevisions(Document doc, bool sorted = true)
        {
            var revisions = new FilteredElementCollector(doc).OfClass(typeof(Revision)).Cast<Revision>().ToList();

            if (sorted == true)
            {
                revisions = revisions.OrderBy(r => r.SequenceNumber).ToList();
            }
            return revisions;
        }
        /// <summary>
        /// Opens a dropdown form allowing the user to select a Revision from the active Revit document.
        /// </summary>
        /// <param name="doc">The active Revit document containing the revisions.</param>
        /// <param name="title">The window title of the dropdown form. If null, a default title is used.</param>
        /// <param name="message">The description message displayed inside the form. If null, a default message is used.</param>
        /// <param name="sorted">if sorted the revisions will be sorted before being displayed.</param>
        /// <returns>object containing the user's selection status and the chosen revision.</returns>
        public static FormResult SelectRevision(Document doc, string title = null, string message = null, bool sorted = true)
        {
            title ??= "Select a Revision";
            message ??= "Select a revision from the list below.";

            var revisions = GetRevisions(doc, sorted);

            var keys = revisions.Select(r => RevisionKey(r)).ToList();
            var values = revisions.Cast<object>().ToList();

            return CSharp_for_Revit.Forms.Custom.SelectFromDropdown(keys, values, title, message);
        }
    }

}
