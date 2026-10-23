using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace CSharp_for_Revit.Forms.Bases
{
    /// <summary>
    /// Transparent overlay placed over the Revit view, with live preview:
    /// clicks 1 and 2 draw a line locked to horizontal or vertical; click 3 opens a read window from that line.
    /// Look (2026-10-22): Windows 11 blue, rounded dashed window, round line ends, dots on the clicked points,
    /// and a hint "pill" with a step counter. Colors live in the XAML resources.
    /// Knows nothing about Revit: receives the screen rectangle in physical pixels and returns 3 screen points.
    /// Result: Tag = List&lt;object&gt; with 3 System.Windows.Point (physical screen pixels):
    /// line start, line end (already locked), window extent. DialogResult = true when finished, false on Esc.
    /// </summary>
    public partial class BaseLineOverlay : Window
    {
        // The 3 steps of the interaction
        private const int STEP_LINE_START = 0;
        private const int STEP_LINE_END = 1;
        private const int STEP_WINDOW = 2;

        // A line or window smaller than this (in WPF units, ~pixels) is an accidental click: ignored
        private const double MIN_SIZE_UNITS = 4.0;

        // One mouse wheel notch zooms in (or out) by this factor
        private const double ZOOM_STEP_FACTOR = 1.25;

        /// <summary>
        /// Optional: called when the user turns the mouse wheel over the overlay (before the 1st click), so the caller can zoom
        /// the view behind it. Arguments: the zoom factor (bigger than 1 = zoom in, smaller = zoom out) and
        /// the cursor position in physical screen pixels. The overlay only reports the wheel: it knows nothing
        /// about Revit. If nobody sets it, the wheel does nothing.
        /// </summary>
        public Action<double, Point> ZoomRequested { get; set; }

        // The Revit view rectangle on the screen, in physical pixels
        private readonly int _screenLeft;
        private readonly int _screenTop;
        private readonly int _screenWidth;
        private readonly int _screenHeight;

        // Current step and the points so far, in WPF units (for drawing)
        private int _step = STEP_LINE_START;
        private Point _lineStart;
        private Point _lineEnd;
        private bool _isHorizontal;

        // The clicked points in physical screen pixels (the result)
        private readonly List<Point> _screenPoints = new List<Point>();

        /// <summary>
        /// Creates the overlay over a screen rectangle.
        /// </summary>
        /// <param name="screenLeft">Left edge of the Revit view, physical pixels.</param>
        /// <param name="screenTop">Top edge, physical pixels.</param>
        /// <param name="screenRight">Right edge, physical pixels.</param>
        /// <param name="screenBottom">Bottom edge, physical pixels.</param>
        /// <param name="ownerHandle">Revit's main window: the overlay stays on top of it and never falls behind.</param>
        public BaseLineOverlay(int screenLeft, int screenTop, int screenRight, int screenBottom, IntPtr ownerHandle)
        {
            InitializeComponent();

            _screenLeft = screenLeft;
            _screenTop = screenTop;
            _screenWidth = screenRight - screenLeft;
            _screenHeight = screenBottom - screenTop;

            // Owner = Revit's window (a "handle" is Windows' Id of a window)
            var interopHelper = new System.Windows.Interop.WindowInteropHelper(this);
            interopHelper.Owner = ownerHandle;

            // Events: += subscribes the method to the event (the "YouTube channel" analogy)
            SourceInitialized += OnSourceInitialized;
            Loaded += OnLoaded;
            DrawingCanvas.MouseMove += OnMouseMove;
            DrawingCanvas.MouseLeftButtonDown += OnMouseLeftButtonDown;
            DrawingCanvas.MouseWheel += OnMouseWheel;
            KeyDown += OnKeyDown;

            UpdateHint();
        }

        #region Window setup

        /// <summary>
        /// Places the overlay exactly over the Revit view.
        /// Revit gives physical pixels; WPF positions windows in logical units (1/96 inch).
        /// With Windows scaling at 125% or 150% they differ, so the rectangle is converted.
        /// </summary>
        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var source = PresentationSource.FromVisual(this);

            // TransformFromDevice = "physical pixels -> WPF units" (a pure scale, e.g. ÷ 1.25 at 125%)
            var fromDevice = source.CompositionTarget.TransformFromDevice;
            var topLeft = fromDevice.Transform(new Point(_screenLeft, _screenTop));
            var size = fromDevice.Transform(new Point(_screenWidth, _screenHeight));

            Left = topLeft.X;
            Top = topLeft.Y;
            Width = size.X;
            Height = size.Y;
        }

        /// <summary>
        /// Takes the keyboard focus, so Esc works right away.
        /// </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Activate();
            Focus();
        }

        #endregion

        #region Mouse and keyboard

        /// <summary>
        /// Live preview: the line follows the mouse (step 2), then the window (step 3).
        /// </summary>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var mouse = e.GetPosition(DrawingCanvas);

            if (_step == STEP_LINE_END)
            {
                _lineEnd = LockToAxis(mouse);
                DrawLine();
                PlaceDot(EndDot, _lineEnd);
            }
            else if (_step == STEP_WINDOW)
            {
                DrawWindow(mouse);
            }
        }

        /// <summary>
        /// Each click closes one step. The 3rd click finishes and closes the overlay.
        /// </summary>
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mouse = e.GetPosition(DrawingCanvas);

            if (_step == STEP_LINE_START)
            {
                _lineStart = mouse;
                _lineEnd = mouse;
                _screenPoints.Add(DrawingCanvas.PointToScreen(_lineStart));

                OuterRowLine.Visibility = System.Windows.Visibility.Visible;
                DrawLine();

                // A dot on the 1st point, and one that follows the line's end until the 2nd click
                StartDot.Visibility = System.Windows.Visibility.Visible;
                EndDot.Visibility = System.Windows.Visibility.Visible;
                PlaceDot(StartDot, _lineStart);
                PlaceDot(EndDot, _lineEnd);

                _step = STEP_LINE_END;
            }
            else if (_step == STEP_LINE_END)
            {
                _lineEnd = LockToAxis(mouse);

                // A line without length defines nothing: wait for a real 2nd click
                var lineLength = (_lineEnd - _lineStart).Length;
                if (lineLength < MIN_SIZE_UNITS) { return; }

                // PointToScreen gives physical screen pixels: the same unit Revit uses
                _screenPoints.Add(DrawingCanvas.PointToScreen(_lineEnd));

                // The end dot stays on the 2nd point
                PlaceDot(EndDot, _lineEnd);

                ReadWindow.Visibility = System.Windows.Visibility.Visible;
                DrawWindow(mouse);
                _step = STEP_WINDOW;
            }
            else if (_step == STEP_WINDOW)
            {
                // A window without depth reads nothing: wait for a real 3rd click
                if (DistanceFromLine(mouse) < MIN_SIZE_UNITS) { return; }

                _screenPoints.Add(DrawingCanvas.PointToScreen(mouse));

                // Result in Tag (same pattern as the WinForms forms); DialogResult = true closes the window
                Tag = _screenPoints
                    .Cast<object>()
                    .ToList();

                DialogResult = true;
                return;
            }

            UpdateHint();
        }

        /// <summary>
        /// Mouse wheel: asks the caller (through ZoomRequested) to zoom the view behind the overlay,
        /// around the cursor. Wheel forward = zoom in; backward = zoom out. Only before the 1st click.
        /// </summary>
        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ZoomRequested is null) { return; }

            // After the 1st click the wheel does nothing: the clicked points are kept in pixels, and a new zoom
            // would move them to another place of the model (the caller converts all of them with the final zoom)
            if (_step != STEP_LINE_START) { return; }

            double factor;

            // Delta > 0 = wheel turned forward (away from the user)
            if (e.Delta > 0)
            {
                factor = ZOOM_STEP_FACTOR;
            }
            else
            {
                factor = 1.0 / ZOOM_STEP_FACTOR;
            }

            // Same unit as the clicks: physical screen pixels
            var cursorPixel = DrawingCanvas.PointToScreen(e.GetPosition(DrawingCanvas));

            ZoomRequested(factor, cursorPixel);

            // The wheel was used here: no other control should react to it
            e.Handled = true;
        }

        /// <summary>
        /// Esc cancels: DialogResult = false closes the overlay without a result.
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }

        #endregion

        #region Geometry and drawing

        /// <summary>
        /// Locks the line to horizontal or vertical: the axis where the mouse moved more wins.
        /// Also remembers the choice in _isHorizontal (used by the window).
        /// </summary>
        private Point LockToAxis(Point mouse)
        {
            var movedX = Math.Abs(mouse.X - _lineStart.X);
            var movedY = Math.Abs(mouse.Y - _lineStart.Y);

            if (movedX >= movedY)
            {
                _isHorizontal = true;
                return new Point(mouse.X, _lineStart.Y);
            }
            else
            {
                _isHorizontal = false;
                return new Point(_lineStart.X, mouse.Y);
            }
        }

        /// <summary>
        /// How far the mouse is from the line, perpendicular to it (the window's depth).
        /// </summary>
        private double DistanceFromLine(Point mouse)
        {
            if (_isHorizontal == true)
            {
                return Math.Abs(mouse.Y - _lineStart.Y);
            }
            else
            {
                return Math.Abs(mouse.X - _lineStart.X);
            }
        }

        /// <summary>
        /// Draws the red line from the 1st point to the (locked) current end.
        /// </summary>
        private void DrawLine()
        {
            OuterRowLine.X1 = _lineStart.X;
            OuterRowLine.Y1 = _lineStart.Y;
            OuterRowLine.X2 = _lineEnd.X;
            OuterRowLine.Y2 = _lineEnd.Y;
        }

        /// <summary>
        /// Draws the read window: as long as the line, and from the line to the mouse.
        /// </summary>
        private void DrawWindow(Point mouse)
        {
            double left;
            double top;
            double width;
            double height;

            if (_isHorizontal == true)
            {
                left = Math.Min(_lineStart.X, _lineEnd.X);
                width = Math.Abs(_lineEnd.X - _lineStart.X);
                top = Math.Min(_lineStart.Y, mouse.Y);
                height = Math.Abs(mouse.Y - _lineStart.Y);
            }
            else
            {
                top = Math.Min(_lineStart.Y, _lineEnd.Y);
                height = Math.Abs(_lineEnd.Y - _lineStart.Y);
                left = Math.Min(_lineStart.X, mouse.X);
                width = Math.Abs(mouse.X - _lineStart.X);
            }

            System.Windows.Controls.Canvas.SetLeft(ReadWindow, left);
            System.Windows.Controls.Canvas.SetTop(ReadWindow, top);
            ReadWindow.Width = width;
            ReadWindow.Height = height;
        }

        /// <summary>
        /// Centers a dot on a point. Canvas positions an element by its top-left corner,
        /// so half the size is subtracted.
        /// </summary>
        private void PlaceDot(System.Windows.Shapes.Ellipse dot, Point point)
        {
            System.Windows.Controls.Canvas.SetLeft(dot, point.X - dot.Width / 2.0);
            System.Windows.Controls.Canvas.SetTop(dot, point.Y - dot.Height / 2.0);
        }

        /// <summary>
        /// The hint "pill" at the bottom, for the current step: the counter and the text.
        /// (The ESC key and "cancel" are fixed in the XAML.)
        /// </summary>
        private void UpdateHint()
        {
            if (_step == STEP_LINE_START)
            {
                StepText.Text = "1/3";
                HintText.Text = "CLICK: 1st point of the line";
            }
            else if (_step == STEP_LINE_END)
            {
                StepText.Text = "2/3";
                HintText.Text = "CLICK: 2nd point (locks horizontal / vertical)";
            }
            else
            {
                StepText.Text = "3/3";
                HintText.Text = "CLICK: read window end (the largest dimension goes here)";
            }
        }

        #endregion
    }
}
