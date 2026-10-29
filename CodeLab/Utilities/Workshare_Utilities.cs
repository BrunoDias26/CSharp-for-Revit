using CSharp_for_Revit.Forms;

namespace CSharp_for_Revit.Utilities
{
    public static class Workshare_Utilities
    {
        /// <summary>
        /// Splits elements into editable and non-editable, asking the user how to proceed when needed.
        /// </summary>
        /// <param name="elements">Elements to process.</param>
        /// <param name="doc">The related document.</param>
        /// <returns>A WorksharingResult with both lists and whether the task was cancelled.</returns>
        public static WorksharingResult ProcessElements(List<Element> elements, Document doc = null)
        {
            var worksharingResult = new WorksharingResult() { Cancelled = false };
            var editable = new List<Element>();
            var notEditable = new List<Element>();

            // Sort each element into a list
            foreach (var element in elements)
            {
                if (Element_Utilities.IsEditable(element, doc))
                {
                    editable.Add(element);
                }
                else
                {
                    notEditable.Add(element);
                }
            }

            worksharingResult.EditableElements = editable;
            worksharingResult.NoneditableElements = notEditable;

            // No element is editable: cancel
            if (editable.Count == 0)
            {
                if (elements.Count > 0)
                {
                    Custom.Cancelled("No required elements are editable.\n\n" +
                        "The task can not proceed, and has been cancelled.");
                }
                worksharingResult.Cancelled = true;
            }
            // Some elements are not editable: ask the user
            else if (notEditable.Count > 0)
            {
                var formResult = Custom.Message(title: "Choose how to proceed",
                    message: "Not all elements are editable, would you like to proceed with those that are?",
                    yesNo: true);

                if (!formResult.Affirmative)
                {
                    worksharingResult.Cancelled = true;
                }
            }

            return worksharingResult;
        }
    }
}
