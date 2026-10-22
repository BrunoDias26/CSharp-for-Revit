// System
using System.Windows;
using System.Windows.Navigation;

// Autodesk
using Autodesk.Revit.UI;

// Namespace for all things form related
namespace CSharp_for_Revit.Forms
{
    // This is a static class, we will use it later
    public static class Custom
    {
        // File filter for SelectFilePaths: "text the user sees|file patterns".
        // No *.xls (unlike the lesson): ClosedXML can't read the old .xls format.
        // const = a fixed value that never changes.
        public const string FILTER_EXCEL = "Excel Files (*.xlsx;*.xlsm)|*.xlsx;*.xlsm";

        /// <summary>
        /// Processes a MessageBox form.
        /// </summary>
        /// <param name="title">Optional form title.</param>
        /// <param name="message">Optional form message.</param>
        /// <param name="yesNo">Show Yes/No buttons.</param>
        /// <param name="noCancel">Only provide an OK button.</param>
        /// <param name="icon">Optional MessageBoxIcon to use.</param>
        /// <returns>A FormResult.</returns>
        public static FormResult Message(string title = null, string message = null, bool yesNo = false, bool noCancel = false, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            // Base form result
            var formResult = new FormResult(isValid: false);

            // Default values
            title ??= "Message";
            message ??= "No message was provided.";

            // Catch the question icon
            if (yesNo && icon == MessageBoxIcon.None)
            {
                icon = MessageBoxIcon.Question;
            }

            // Set buttons
            var buttons = MessageBoxButtons.OKCancel;

            if (noCancel)
            {
                buttons = MessageBoxButtons.OK;
            }
            else if (yesNo)
            {
                buttons = MessageBoxButtons.YesNo;
            }

            // Run the form
            System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.MessageBox.Show(message, title, buttons, icon);


            // Process the result
            if (dialogResult == DialogResult.Yes || dialogResult == DialogResult.OK)
            {
                formResult.Validate();
            }

            // Return the formresult
            return formResult;
        }

        /// <summary>
        /// Processes a generic object from a dropdown.
        /// </summary>
        /// <param name="keys">A list of keys to display.</param>
        /// <param name="values">A list of values to pass.</param>
        /// <param name="title">An optional title to display.</param>
        /// <param name="message">An optional message to display.</param>
        /// <param name="defaultIndex">An optional index to initialize at.</param>
        /// <returns>A FormResult. The selected value is stored in Object.</returns>
        public static FormResult SelectFromDropdown(List<string>keys, List<object>values, string title = null, string message = null, int defaultIndex = -1)
        {
            var FormResult = new FormResult(isValid: false);

            title ??= "Select from Dropdown";
            message ??= "Select an objct from the dropdown.";

            using (var form = new Bases.BaseDropdown(keys,values, title, message, defaultIndex))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    FormResult.Validate(form.Tag as object);
                }
            }

            return FormResult;

        }

        /// <summary>
        /// Processes generic object(s) from a list.
        /// </summary>
        /// <param name="keys">A list of keys to display.</param>
        /// <param name="values">A list of values to pass.</param>
        /// <param name="title">An optional title to display.</param>
        /// <param name="multiSelect">Select one or more items.</param>
        /// <returns>A FormResult. Multiselect fills Objects, single select fills Object.</returns>
        public static FormResult SelectFromList(List<string> keys, List<object> values, string title = null, bool multiSelect = true)
        {
            var formResult = new FormResult(isValid: false);

            // Default title: only when none was given (null), plural or singular
            if (title is null)
            {
                if (multiSelect == true)
                {
                    title = "Select object(s) from the list:";
                }
                else
                {
                    title = "Select an object from the list";
                }
            }

            using (var form = new Bases.BaseListView(keys, values, title, multiSelect))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Catch multi or single select
                    if (multiSelect)
                    {
                        formResult.Validate(form.Tag as List<object>);
                    }
                    else
                    {
                        formResult.Validate(form.Tag as object);
                    }
                }
            }

            return formResult;
        }

        /// <summary>
        /// Opens the Windows file dialog to select one or more files.
        /// </summary>
        /// <param name="title">An optional title to display.</param>
        /// <param name="filter">An optional file type filter, e.g. Custom.FILTER_EXCEL.</param>
        /// <param name="multiSelect">Select one or more files.</param>
        /// <returns>A FormResult. Multiselect fills Objects (file paths), single select fills Object (one file path).</returns>
        public static FormResult SelectFilePaths(string title = null, string filter = null, bool multiSelect = true)
        {
            var formResult = new FormResult(isValid: false);

            // Default title: only when none was given (null), plural or singular
            if (title is null)
            {
                if (multiSelect == true)
                {
                    title = "Select file(s)";
                }
                else
                {
                    title = "Select a file";
                }
            }

            using (var openFileDialog = new OpenFileDialog())
            {
                if (filter is not null) { openFileDialog.Filter = filter; }

                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = title;
                openFileDialog.Multiselect = multiSelect;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // FileNames = full paths of the chosen files (always a list, even with one file)
                    var filePaths = openFileDialog.FileNames.ToList();

                    if (multiSelect == true)
                    {
                        formResult.Validate(filePaths.Cast<object>().ToList());
                    }
                    else
                    {
                        formResult.Validate(filePaths.First() as object);
                    }
                }
            }

            return formResult;
        }

        /// <summary>
        /// Opens the modern Windows dialog to select one or more folders (WindowsAPICodePack package).
        /// </summary>
        /// <param name="title">An optional title to display.</param>
        /// <param name="multiSelect">Select one or more folders.</param>
        /// <returns>A FormResult. Multiselect fills Objects (folder paths), single select fills Object (one folder path).</returns>
        public static FormResult SelectFolder(string title = null, bool multiSelect = false)
        {
            var formResult = new FormResult(isValid: false);

            // Default title: only when none was given (null), plural or singular
            if (title is null)
            {
                if (multiSelect == true)
                {
                    title = "Select folder(s)";
                }
                else
                {
                    title = "Select folder";
                }
            }

            using (var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog())
            {
                dialog.Title = title;
                dialog.RestoreDirectory = true;
                dialog.IsFolderPicker = true;   // pick folders instead of files
                dialog.Multiselect = multiSelect;

                if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                {
                    if (multiSelect == true)
                    {
                        formResult.Validate(dialog.FileNames.Cast<object>().ToList());
                    }
                    else
                    {
                        formResult.Validate(dialog.FileName as object);
                    }
                }
            }

            return formResult;
        }

        /// <summary>
        /// Opens the line + window overlay (WPF) over a screen rectangle: 3 clicks with live preview.
        /// Clicks 1-2 draw a line locked to horizontal or vertical; click 3 sets the read window. Esc cancels.
        /// </summary>
        /// <param name="screenLeft">Left edge of the area, physical screen pixels.</param>
        /// <param name="screenTop">Top edge, physical screen pixels.</param>
        /// <param name="screenRight">Right edge, physical screen pixels.</param>
        /// <param name="screenBottom">Bottom edge, physical screen pixels.</param>
        /// <param name="ownerHandle">The window the overlay belongs to (e.g. Revit's main window).</param>
        /// <returns>A FormResult. Objects = 3 System.Windows.Point in physical screen pixels:
        /// line start, line end (already locked), window extent.</returns>
        public static FormResult PickLineWithWindow(int screenLeft, int screenTop, int screenRight, int screenBottom, IntPtr ownerHandle)
        {
            var formResult = new FormResult(isValid: false);

            // WPF windows are not IDisposable, so no "using" here (unlike the WinForms forms)
            var overlay = new Bases.BaseLineOverlay(screenLeft, screenTop, screenRight, screenBottom, ownerHandle);

            // ShowDialog in WPF returns bool? (true, false or null): only true means "finished"
            if (overlay.ShowDialog() == true)
            {
                formResult.Validate(overlay.Tag as List<object>);
            }

            return formResult;
        }

        /// <summary>
        /// Processes a Completed form.
        /// </summary>
        /// <param name="message">An optional message.</param>
        /// <returns>Result.Succeeded.</returns>
        public static Result Completed(string message = null)
        {
            // Default message
            message ??= "Task completed.";

            // Show the form
            Message(title: "Task completed",
                message: message,
                noCancel: true,
                icon: MessageBoxIcon.Information);

            // Return the result
            return Result.Succeeded;
        }

        /// <summary>
        /// Processes a Cancelled form.
        /// </summary>
        /// <param name="message">An optional message.</param>
        /// <returns>Result.Cancelled.</returns>
        public static Result Cancelled(string message = null)
        {
            // Default message
            message ??= "Task cancelled.";

            // Show the form
            Message(title: "Task cancelled",
                message: message,
                noCancel: true,
                icon: MessageBoxIcon.Warning);

            // Return the result
            return Result.Cancelled;
        }

        /// <summary>
        /// Processes an Error form.
        /// </summary>
        /// <param name="message">An optional message.</param>
        /// <returns>Result.Cancelled.</returns>
        public static Result Error(string message = null)
        {
            // Default message
            message ??= "Error encountered.";

            // Show the form
            Message(title: "Error encountered",
                message: message,
                noCancel: true,
                icon: MessageBoxIcon.Error);

            // Return the result
            return Result.Cancelled;
        }


    }
}