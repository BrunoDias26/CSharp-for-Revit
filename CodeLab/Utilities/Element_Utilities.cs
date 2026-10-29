using View = Autodesk.Revit.DB.View;

namespace CodeLab.Utilities
{
    public static class Element_Utilities
    {
        /// <summary>
        /// Collects all elements (not types) of the provided category.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="builtInCategory">A Revit BuiltInCategory, e.g. BuiltInCategory.OST_TitleBlocks.</param>
        /// <param name="view">An optional view: if given, only elements visible in that view.</param>
        /// <returns>A list of elements.</returns>
        public static List<Element> GetElementsOfCategory(Document doc, BuiltInCategory builtInCategory, View view = null)
        {
            if (doc is null) { return new List<Element>(); }

            // Whole document, or only what is visible in the view
            FilteredElementCollector collector;

            if (view is null)
            {
                collector = new FilteredElementCollector(doc);
            }
            else
            {
                collector = new FilteredElementCollector(doc, view.Id);
            }

            return collector
                .OfCategory(builtInCategory)
                .WhereElementIsNotElementType()
                .ToList();
        }

        /// <summary>
        /// Returns if an ElementId is null or invalid.
        /// </summary>
        /// <param name="elementId">The ElementId to check.</param>
        /// <returns>True if the ElementId is null or invalid; otherwise, false.</returns>
        public static bool IsNullOrInvalid(ElementId elementId)
        {
            // || = "or": true if any of the two sides is true
            return elementId is null || elementId == ElementId.InvalidElementId;
        }

        /// <summary>
        /// Returns if an ElementId is valid: the opposite of IsNullOrInvalid.
        /// </summary>
        /// <param name="elementId">The ElementId to check.</param>
        /// <returns>True if the ElementId is not null and not invalid; otherwise, false.</returns>
        public static bool IsValid(ElementId elementId)
        {
            return IsNullOrInvalid(elementId) == false;
        }

        /// <summary>
        /// Returns the integer value of an ElementId, in any Revit version.
        /// </summary>
        /// <param name="elementId">The ElementId.</param>
        /// <returns>The Id as an integer, or -1 if null or invalid.</returns>
        public static int IntegerValue(ElementId elementId)
        {
            if (IsNullOrInvalid(elementId) == true) { return -1; }

            // #if is decided at COMPILE time, not when the code runs: only one branch goes into the DLL.
            // REVIT2024_OR_GREATER is created by the Nice3point SDK from the configuration (Debug.R25 -> 2025).
            // A normal "if" wouldn't work: .Value doesn't exist in the Revit 2023 API, so it wouldn't even compile.
#if REVIT2024_OR_GREATER
            // Revit 2024+: ElementId.Value is a long (64-bit) and IntegerValue is obsolete
            return (int)elementId.Value;
#else
            // Revit 2023 and older: only IntegerValue exists
            return elementId.IntegerValue;
#endif
        }

        /// <summary>
        /// Reads the value of a text parameter by its BuiltInParameter (e.g. BuiltInParameter.SHEET_CURRENT_REVISION).
        /// </summary>
        /// <param name="element">The element to read from.</param>
        /// <param name="builtInParameter">Revit's fixed code for the parameter (same in any language).</param>
        /// <param name="valueIfInvalid">Value returned if the parameter is missing or not text.</param>
        /// <param name="emptyIsInvalid">If true, an empty value also returns valueIfInvalid.</param>
        /// <returns>The parameter's text, or valueIfInvalid.</returns>
        public static string GetStringParameterValue(Element element, BuiltInParameter builtInParameter, string valueIfInvalid = "", bool emptyIsInvalid = false)
        {
            if (element is null) { return valueIfInvalid; }

            // BuiltInParameter = Revit's fixed code for the parameter: works in any Revit language
            var parameter = element.get_Parameter(builtInParameter);
            if (parameter is null) { return valueIfInvalid; }

            // Only text parameters
            if (parameter.StorageType == StorageType.String)
            {
                var value = parameter.AsString();

                if (string.IsNullOrEmpty(value) == true && emptyIsInvalid == true)
                {
                    return valueIfInvalid;
                }
                else
                {
                    return value;
                }
            }

            return valueIfInvalid;
        }

        /// <summary>
        /// Reads the value of a number parameter by its BuiltInParameter (e.g. BuiltInParameter.FABRICATION_PART_DIAMETER_IN).
        /// Lengths come in Revit's internal unit: feet.
        /// </summary>
        /// <param name="element">The element to read from.</param>
        /// <param name="builtInParameter">Revit's fixed code for the parameter (same in any language).</param>
        /// <param name="valueIfInvalid">Value returned if the parameter is missing or not a number.</param>
        /// <returns>The parameter's value, or valueIfInvalid.</returns>
        public static double GetDoubleParameterValue(Element element, BuiltInParameter builtInParameter, double valueIfInvalid = 0.0)
        {
            if (element is null) { return valueIfInvalid; }

            // Same pattern as GetStringParameterValue, for numbers
            var parameter = element.get_Parameter(builtInParameter);
            if (parameter is null) { return valueIfInvalid; }

            // Only number parameters (lengths, areas, angles...)
            if (parameter.StorageType == StorageType.Double)
            {
                return parameter.AsDouble();
            }
            else
            {
                return valueIfInvalid;
            }
        }

        /// <summary>
        /// Checks if an element can be edited by the current user, considering worksharing.
        /// </summary>
        /// <param name="element">The element to check.</param>
        /// <param name="doc">The related document. If null, the element's own document is used.</param>
        /// <returns>True if the element is editable; otherwise, false.</returns>
        public static bool IsEditable(Element element, Document doc = null)
        {
            doc ??= element.Document;

            // Not workshared: everything is editable
            if (!doc.IsWorkshared) { return true; }

            // Get worksharing status
            var checkoutStatus = WorksharingUtils.GetCheckoutStatus(doc, element.Id);
            var updateStatus = WorksharingUtils.GetModelUpdatesStatus(doc, element.Id);

            // Owned by someone else, by us, or by nobody
            if (checkoutStatus == CheckoutStatus.OwnedByOtherUser) { return false; }
            else if (checkoutStatus == CheckoutStatus.OwnedByCurrentUser) { return true; }
            else { return updateStatus == ModelUpdatesStatus.CurrentWithCentral; }
        }
    }
}
