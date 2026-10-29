using Autodesk.Revit.UI;

namespace CodeLab.Utilities
{
    // Everything related to exporting from Revit (PDF now, DWG later).
    // The author splits these between File_Utils and ViewSheet_Ext; here they live together by subject.
    public static class Export_Utilities
    {
        /// <summary>
        /// Default PDF export settings (same as the lesson).
        /// </summary>
        /// <param name="hideCrop">Hide crop boundaries.</param>
        /// <returns>A PDFExportOptions object.</returns>
        public static PDFExportOptions DefaultPdfExportOptions(bool hideCrop = true)
        {
            var options = new PDFExportOptions();

            // Not in the lesson: Combine = true makes Revit use options.FileName as the file name.
            // We export one sheet per call, so it's still one PDF per sheet.
            options.Combine = true;

            options.AlwaysUseRaster = false;
            options.ColorDepth = ColorDepthType.Color;
            options.ExportQuality = PDFExportQualityType.DPI300;
            options.HideCropBoundaries = hideCrop;
            options.HideReferencePlane = true;
            options.HideScopeBoxes = true;
            options.HideUnreferencedViewTags = true;
            options.MaskCoincidentLines = true;
            options.PaperFormat = ExportPaperFormat.Default;
            options.PaperOrientation = PageOrientationType.Auto;
            options.RasterQuality = RasterQualityType.High;
            options.ReplaceHalftoneWithThinLines = true;
            options.StopOnError = false;
            options.ViewLinksInBlue = false;
            options.ZoomPercentage = 100;
            options.ZoomType = ZoomType.Zoom;

            return options;
        }

        /// <summary>
        /// Exports one sheet to a PDF file.
        /// </summary>
        /// <param name="sheet">The sheet to export.</param>
        /// <param name="fileName">The file name, without extension.</param>
        /// <param name="directoryPath">The folder to export to.</param>
        /// <param name="doc">The related document. If null, the sheet's own document is used.</param>
        /// <param name="options">Export settings. If null, DefaultPdfExportOptions is used.</param>
        /// <returns>Result.Succeeded if exported; otherwise, Result.Failed.</returns>
        public static Result ExportSheetToPdf(ViewSheet sheet, string fileName, string directoryPath, Document doc = null, PDFExportOptions options = null)
        {
            if (sheet is null || fileName is null || directoryPath is null) { return Result.Failed; }

            if (doc is null)
            {
                doc = sheet.Document;
            }

            if (options is null)
            {
                options = DefaultPdfExportOptions();
            }

            options.FileName = fileName;

            // Export takes a list of views: a list with just this sheet = one PDF per sheet
            var sheetIds = new List<ElementId>() { sheet.Id };

            try
            {
                doc.Export(directoryPath, sheetIds, options);
                return Result.Succeeded;
            }
            catch
            {
                return Result.Failed;
            }
        }
    }
}
