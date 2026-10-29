using Autodesk.Revit.UI.Selection;
using View = Autodesk.Revit.DB.View;

namespace CodeLab.Utilities
{
    // Selection filters (ISelectionFilter) tell Revit WHICH elements the user is allowed to pick
    // during PickObjects. Revit calls AllowElement for every element under the mouse while the user
    // hovers: return true = can be highlighted and picked, false = ignored.
    // Same idea as Availability: one class per rule, nested in one static class.
    public static class Selection_Utilities
    {
        // ByCategory: only elements of one category can be picked (e.g. only rooms)
        public class ISF_ByCategory : ISelectionFilter
        {
            // Field = the filter's "memory". The constructor runs ONCE (new ISF_ByCategory(...)),
            // AllowElement runs MANY times (every hover). The constructor's argument only exists
            // inside the constructor, and AllowElement's signature is fixed by the interface
            // (Revit only passes the element), so the category is kept here to be used later.
            private ElementId _categoryId;

            public ISF_ByCategory(BuiltInCategory category)
            {
                // Converted once here, not on every hover
                _categoryId = new ElementId(category);
            }

            public bool AllowElement(Element elem)
            {
                // Some elements have no category: "is Category category" checks for null and names it
                if (elem.Category is Category category)
                {
                    if (category.Id == _categoryId)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            // Required by the interface (it has 2 methods, both must exist).
            // Only used when picking references (faces, edges, points). We pick ObjectType.Element,
            // so Revit never calls it. If you ever pick faces with this filter, false blocks everything.
            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

        // HiddenInView: only elements hidden in the given view can be picked.
        // To see them, turn on Reveal Hidden Elements (light bulb in the view control bar).
        public class ISF_HiddenInView : ISelectionFilter
        {
            // Same idea: the view comes in the constructor and is kept for AllowElement
            private View _view;

            public ISF_HiddenInView(View view)
            {
                _view = view;
            }

            public bool AllowElement(Element elem)
            {
                if (elem.IsHidden(_view) == true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // Required by the interface, not used when picking elements (see ISF_ByCategory)
            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }
}
