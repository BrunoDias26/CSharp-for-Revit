using System.IO;
using Autodesk.Revit.UI;
using CSharp_for_Revit.General;
using Form = System.Windows.Forms.Form;

namespace CSharp_for_Revit.Utilities
{
    public static class File_Utilities
    {
        public static void SetFormIcon(Form form, string iconPath = null)
        {
             iconPath ??= "CSharp_for_Revit.Resources.Icons16.IconList16.ico";

            using (Stream stream = Globals.Assembly.GetManifestResourceStream(iconPath))
            {
                if (stream is not null)
                {
                    form.Icon = new Icon(stream);
                }
            }
        }

        /// <summary>
        /// Checks if a file exists and can be opened for reading (e.g. it is not locked by Excel).
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns>True if the file can be read; otherwise, false.</returns>
        public static bool IsFileReadable(string filePath)
        {
            if (File.Exists(filePath) == false)
            {
                return false;
            }

            // Try to open the file just for reading: if another program locked it, this throws
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Opens a folder in Windows Explorer.
        /// </summary>
        /// <param name="directoryPath">The folder to open.</param>
        /// <returns>Result.Succeeded if opened; otherwise, Result.Failed.</returns>
        public static Result OpenDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath) == false)
            {
                return Result.Failed;
            }

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", directoryPath);
                return Result.Succeeded;
            }
            catch
            {
                return Result.Failed;
            }
        }
    }
}
