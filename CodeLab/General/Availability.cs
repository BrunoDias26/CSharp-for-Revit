using Autodesk.Revit.UI;

namespace CodeLab.General
{
    // Availability classes tell Revit WHEN a push button is enabled or grayed out.
    // Revit calls IsCommandAvailable very often (every time the UI refreshes, e.g. a new selection),
    // Without an availability class, Revit's default is: enabled only when a document is open.
    public static class Availability
    {
        /// <summary>
        /// Public shortcuts to the availability class names, to pass as the 'availability' argument.
        /// The rule classes below are private, so no other C# code can use them directly.
        /// Revit can, because it finds them by this NAME at runtime (reflection ignores private).
        /// </summary>
        public static class AvailabilityNames
        {
            // typeof(...).FullName builds the name for us, e.g. "CodeLab.General.Availability+Project".
            public static string Disabled { get { return typeof(Availability.Disabled).FullName; } }
            public static string ZeroDoc { get { return typeof(Availability.ZeroDoc).FullName; } }
            public static string Project { get { return typeof(Availability.Project).FullName; } }
            public static string Family { get { return typeof(Availability.Family).FullName; } }
            public static string Workshared { get { return typeof(Availability.Workshared).FullName; } }
            public static string Selection { get { return typeof(Availability.Selection).FullName; } }
            public static string Selection_SheetsOnly { get { return typeof(Availability.Selection_SheetsOnly).FullName; } }
        }


        // Disabled: always grayed out (e.g. a tool still in progress)
        private class Disabled : IExternalCommandAvailability
        {
            public bool IsCommandAvailable(UIApplication uiApp, CategorySet selectedCategories)
            {
                return false;
            }
        }

        // ZeroDoc: always enabled, even with no document open.
        // The command must then NOT assume a document exists (ActiveUIDocument can be null).
        private class ZeroDoc : IExternalCommandAvailability
        {
            public bool IsCommandAvailable(UIApplication uiApp, CategorySet selectedCategories)
            {
                return true;
            }
        }

        // Project: enabled only when the active document is a project (not a family)
        private class Project : IExternalCommandAvailability
        {
            public bool IsCommandAvailable(UIApplication uiApp, CategorySet selectedCategories)
            {
                // "is UIDocument uiDoc" = there is an active document (not null), and call it uiDoc
                if (uiApp.ActiveUIDocument is UIDocument uiDoc)
                {
                    if (uiDoc.Document.IsFamilyDocument == false)
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
                    // No document open
                    return false;
                }
            }
        }

        // Family: enabled only when the active document is a family (Family Editor)
        private class Family : IExternalCommandAvailability
        {
            public bool IsCommandAvailable(UIApplication uiApp, CategorySet selectedCategories)
            {
                if (uiApp.ActiveUIDocument is UIDocument uiDoc)
                {
                    if (uiDoc.Document.IsFamilyDocument == true)
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
        }

        // Workshared: enabled only when the active document is workshared
        private class Workshared : IExternalCommandAvailability
        {
            public bool IsCommandAvailable(UIApplication uiApp, CategorySet selectedCategories)
            {
                if (uiApp.ActiveUIDocument is UIDocument uiDoc)
                {
                    if (uiDoc.Document.IsWorkshared == true)
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
        }

        // Selection: enabled only when something is selected.
        // selectedCategories = categories of the selected elements (empty when nothing is selected)
        private class Selection : IExternalCommandAvailability
        {
            public bool IsCommandAvailable(UIApplication uiApp, CategorySet selectedCategories)
            {
                if (uiApp.ActiveUIDocument is UIDocument uiDoc)
                {
                    if (selectedCategories.Size > 0)
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
        }

        // Selection_SheetsOnly: enabled only when the selection has sheets and nothing else
        private class Selection_SheetsOnly : IExternalCommandAvailability
        {
            public bool IsCommandAvailable(UIApplication uiApp, CategorySet selectedCategories)
            {
                if (uiApp.ActiveUIDocument is UIDocument uiDoc)
                {
                    // Exactly one category selected (0 = nothing selected, 2+ = mixed selection)
                    if (selectedCategories.Size != 1)
                    {
                        return false;
                    }

                    // ...and that one category is Sheets
                    var sheetsCategory = Category.GetCategory(uiDoc.Document, BuiltInCategory.OST_Sheets);

                    if (selectedCategories.Contains(sheetsCategory) == true)
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
        }
    }
}
