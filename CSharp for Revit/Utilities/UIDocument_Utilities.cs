using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CSharp_for_Revit.Forms;

namespace CSharp_for_Revit.Utilities
{
    public static class UIDocument_Utilities
    {
        #region Get selection

        /// <summary>
        /// Gets the currently selected elements.
        /// To keep only one type, add .OfType&lt;ViewSheet&gt;() (or any other type) after the call.
        /// </summary>
        /// <param name="uiDoc">The active UIDocument.</param>
        /// <returns>A list of elements.</returns>
        public static List<Element> GetSelectedElements(UIDocument uiDoc)
        {
            if (uiDoc is null) { return new List<Element>(); }

            var doc = uiDoc.Document;

            // Revit gives the selection as Ids: turn each Id into its Element
            return uiDoc.Selection.GetElementIds()
                .Select(id => doc.GetElement(id))
                .Where(e => e is not null)
                .ToList();
        }

        /// <summary>
        /// Asks the user to pick elements in the model, allowing only what the selection filter accepts.
        /// The user clicks the elements, then presses Finish (or Esc to cancel).
        /// </summary>
        /// <param name="uiDoc">The active UIDocument.</param>
        /// <param name="selectionFilter">The filter (e.g. Selection_Utilities.ISF_ByCategory) deciding what can be picked.</param>
        /// <param name="prompt">Text shown in the status bar while picking.</param>
        /// <param name="showMessage">Show a message if the pick is cancelled or fails.</param>
        /// <returns>A list of elements (empty if cancelled).</returns>
        public static List<Element> SelectWithFilter(UIDocument uiDoc, ISelectionFilter selectionFilter, string prompt = null, bool showMessage = true)
        {
            prompt ??= "Select element(s)";

            // PickObjects throws an exception when the user presses Esc, so it needs try/catch
            try
            {
                // PickObjects returns References (what was clicked): turn each one into its Element
                return uiDoc.Selection.PickObjects(ObjectType.Element, selectionFilter, prompt)
                    .Select(r => uiDoc.Document.GetElement(r))
                    .Where(e => e is not null)
                    .ToList();
            }
            catch
            {
                if (showMessage == true)
                {
                    Custom.Cancelled("Could not select elements.");
                }

                return new List<Element>();
            }
        }

        #endregion

        #region Set selection

        // The 4 Select methods are independent on purpose (easier to read while learning):
        // each one cleans its own input and calls Revit's Selection.SetElementIds by itself.
        // Trade-off: the rules (skip null/invalid, remove duplicates) are repeated,
        // so if a rule changes, update all 4 methods.

        /// <summary>
        /// Sets the current selection to the given ElementIds.
        /// </summary>
        /// <param name="uiDoc">The active UIDocument.</param>
        /// <param name="elementIds">The ElementIds to select.</param>
        /// <returns>Result.Succeeded, or Result.Failed if there is no UIDocument.</returns>
        public static Result SelectElementIds(UIDocument uiDoc, List<ElementId> elementIds)
        {
            if (uiDoc is null) { return Result.Failed; }

            // Remove null/invalid Ids and duplicates
            elementIds = elementIds
                .Where(id => Element_Utilities.IsValid(id))
                .Distinct()
                .ToList();

            uiDoc.Selection.SetElementIds(elementIds);
            return Result.Succeeded;
        }

        /// <summary>
        /// Sets the current selection to the given Elements.
        /// </summary>
        /// <param name="uiDoc">The active UIDocument.</param>
        /// <param name="elements">The elements to select.</param>
        /// <returns>Result.Succeeded, or Result.Failed if there is no UIDocument.</returns>
        public static Result SelectElements(UIDocument uiDoc, List<Element> elements)
        {
            if (uiDoc is null) { return Result.Failed; }

            // Elements -> Ids, removing null elements, invalid Ids and duplicates
            var elementIds = elements
                .Where(e => e is not null)
                .Select(e => e.Id)
                .Where(id => Element_Utilities.IsValid(id))
                .Distinct()
                .ToList();

            uiDoc.Selection.SetElementIds(elementIds);
            return Result.Succeeded;
        }

        /// <summary>
        /// Sets the current selection to the given ElementId.
        /// </summary>
        /// <param name="uiDoc">The active UIDocument.</param>
        /// <param name="elementId">The ElementId to select.</param>
        /// <returns>Result.Succeeded, or Result.Failed if there is no UIDocument or the Id is invalid.</returns>
        public static Result SelectElementId(UIDocument uiDoc, ElementId elementId)
        {
            if (uiDoc is null || Element_Utilities.IsNullOrInvalid(elementId)) { return Result.Failed; }

            // SetElementIds always takes a list: a list with just this one Id
            var elementIds = new List<ElementId>() { elementId };

            uiDoc.Selection.SetElementIds(elementIds);
            return Result.Succeeded;
        }

        /// <summary>
        /// Sets the current selection to the given element.
        /// </summary>
        /// <param name="uiDoc">The active UIDocument.</param>
        /// <param name="element">The element to select.</param>
        /// <returns>Result.Succeeded, or Result.Failed if there is no UIDocument or element.</returns>
        public static Result SelectElement(UIDocument uiDoc, Element element)
        {
            if (uiDoc is null || element is null) { return Result.Failed; }

            if (Element_Utilities.IsNullOrInvalid(element.Id)) { return Result.Failed; }

            // SetElementIds always takes a list: a list with just this element's Id
            var elementIds = new List<ElementId>() { element.Id };

            uiDoc.Selection.SetElementIds(elementIds);
            return Result.Succeeded;
        }

        #endregion
    }
}
