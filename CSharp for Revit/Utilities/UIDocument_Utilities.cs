using System.Runtime.InteropServices;
using System.Text;
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

        #region Pick with overlay

        // DllImport = calling a function of Windows itself (user32.dll), outside .NET.
        // GetWindowText reads the text of a window's title bar.
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr windowHandle, StringBuilder text, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr windowHandle);

        /// <summary>
        /// Is the user working on a sheet: the sheet itself, or a view edited inside a viewport (double-clicked)?
        /// The API can't tell the viewport case: ActiveView returns the plan in both cases (tested 2026-10-22).
        /// Revit's title bar can: it shows "Sheet: number - name" when the sheet is in front.
        /// </summary>
        /// <param name="uiDoc">The active UIDocument.</param>
        /// <returns>True if the active window is a sheet; false if it is the view itself.</returns>
        public static bool IsWorkingOnSheet(UIDocument uiDoc)
        {
            if (uiDoc is null) { return false; }

            var doc = uiDoc.Document;
            var view = uiDoc.ActiveGraphicalView;

            if (view is null) { return false; }

            // The sheet itself is open (no viewport activated)
            if (view is ViewSheet) { return true; }

            // Is this view placed on a sheet at all?
            var viewport = View_Utilities.GetViewportOfView(doc, view);
            if (viewport is null) { return false; }

            var sheet = doc.GetElement(viewport.SheetId) as ViewSheet;
            if (sheet is null) { return false; }

            // Number + name, not only the name: in the CTS sheets the sheet name is equal to the view name.
            // And not the word "Sheet:", because it changes with Revit's language.
            var caption = GetRevitCaption(uiDoc.Application.MainWindowHandle);
            var sheetText = $"{sheet.SheetNumber} - {sheet.Name}";

            // IndexOf with a comparison works in .NET Framework 4.8 too (Contains with a comparison doesn't)
            if (caption.IndexOf(sheetText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Reads the text of Revit's title bar (e.g. "Autodesk Revit 2025.4 - [Model.rvt - Floor Plan: Level 1]").
        /// </summary>
        /// <param name="windowHandle">Revit's main window (uiApp.MainWindowHandle).</param>
        /// <returns>The caption, or an empty text if it can't be read.</returns>
        private static string GetRevitCaption(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero) { return string.Empty; }

            try
            {
                var length = GetWindowTextLength(windowHandle);
                var text = new StringBuilder(length + 1);
                GetWindowText(windowHandle, text, text.Capacity);
                return text.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Asks the user for a line (locked to horizontal or vertical) and a read window, with live preview
        /// over the active view (3 clicks; Esc cancels). Translates the screen pixels into the view's
        /// screen coordinates in feet (X = right, Y = up), so it works in rotated views too.
        /// Works only in the view itself: check IsWorkingOnSheet first (inside a sheet viewport the
        /// zoom corners come in another scale and the result lands in the wrong place).
        /// </summary>
        /// <param name="uiDoc">The active UIDocument.</param>
        /// <returns>A LineWindowResult. Check Cancelled first.</returns>
        public static LineWindowResult PickLineWithWindow(UIDocument uiDoc)
        {
            // Cancelled until the user finishes the 3 clicks
            var result = new LineWindowResult();
            result.Cancelled = true;

            if (uiDoc is null) { return result; }

            var view = uiDoc.ActiveGraphicalView;

            // The UIView is the view's window on the screen: the one with the active view's Id.
            // If the same view is also activated in a viewport of an open sheet, 2 windows have this Id;
            // tested (2026-10-22): with the view in front, it is the first one in the list.
            UIView uiView = null;

            foreach (var openView in uiDoc.GetOpenUIViews())
            {
                if (openView.ViewId == view.Id)
                {
                    uiView = openView;
                    break;
                }
            }

            if (uiView is null) { return result; }

            // Where the view is on the screen (pixels) and which part of the model it shows (2 opposite corners)
            var rectangle = uiView.GetWindowRectangle();
            var zoomCorners = uiView.GetZoomCorners();

            // The overlay: 3 clicks, returns 3 points in physical screen pixels
            var formResult = Custom.PickLineWithWindow(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom, uiDoc.Application.MainWindowHandle);
            if (formResult.Cancelled) { return result; }

            var pixels = formResult.Objects
                .Cast<System.Windows.Point>()
                .ToList();

            // The visible area in screen coordinates (feet): its X and Y ranges
            var corner1 = View_Utilities.ToViewCoordinates(zoomCorners[0], view);
            var corner2 = View_Utilities.ToViewCoordinates(zoomCorners[1], view);
            var minX = Math.Min(corner1.X, corner2.X);
            var maxX = Math.Max(corner1.X, corner2.X);
            var minY = Math.Min(corner1.Y, corner2.Y);
            var maxY = Math.Max(corner1.Y, corner2.Y);
            var depth = View_Utilities.ToViewCoordinates(view.Origin, view).Z;

            // Rule of three: how many feet each pixel is worth, in X and in Y
            var feetPerPixelX = (maxX - minX) / (rectangle.Right - rectangle.Left);
            var feetPerPixelY = (maxY - minY) / (rectangle.Bottom - rectangle.Top);

            // Local function: a small method that only exists inside this one and sees its variables.
            // Screen pixels grow DOWN, the view's Y grows UP: that's why Y starts from maxY and subtracts.
            XYZ PixelToView(System.Windows.Point pixel)
            {
                var x = minX + (pixel.X - rectangle.Left) * feetPerPixelX;
                var y = maxY - (pixel.Y - rectangle.Top) * feetPerPixelY;
                return new XYZ(x, y, depth);
            }

            var lineStart = PixelToView(pixels[0]);
            var lineEnd = PixelToView(pixels[1]);
            var extent = PixelToView(pixels[2]);

            result.LineStartInView = lineStart;
            result.LineEndInView = lineEnd;
            result.ExtentInView = extent;

            // The overlay already locked the line: it changes either in X (horizontal) or in Y (vertical)
            if (Math.Abs(lineEnd.X - lineStart.X) >= Math.Abs(lineEnd.Y - lineStart.Y))
            {
                result.LineOrientation = OrientationInView.Horizontal;

                // Window: as long as the line in X, from the line to the 3rd click in Y
                result.WindowMinInView = new XYZ(Math.Min(lineStart.X, lineEnd.X), Math.Min(lineStart.Y, extent.Y), depth);
                result.WindowMaxInView = new XYZ(Math.Max(lineStart.X, lineEnd.X), Math.Max(lineStart.Y, extent.Y), depth);
            }
            else
            {
                result.LineOrientation = OrientationInView.Vertical;

                // Window: as long as the line in Y, from the line to the 3rd click in X
                result.WindowMinInView = new XYZ(Math.Min(lineStart.X, extent.X), Math.Min(lineStart.Y, lineEnd.Y), depth);
                result.WindowMaxInView = new XYZ(Math.Max(lineStart.X, extent.X), Math.Max(lineStart.Y, lineEnd.Y), depth);
            }

            result.Cancelled = false;
            return result;
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
