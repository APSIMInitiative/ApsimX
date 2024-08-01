using APSIM.Shared.Utilities;
using ApsimNG.Interfaces;
using Cairo;
using Gtk;
using OxyPlot.GtkSharp;
using System;
using System.Collections.Generic;
using UserInterface.Extensions;
using Utility;

namespace UserInterface.Views
{
    /// <summary>
    /// Shows a ternary graph.
    /// </summary>
    internal class TernaryGraphView : ViewBase, ITernaryGraphView
    {
        // Data
        private double x;
        private double y;
        private double z;

        // Display settings
        private double sideLength = 100;
        private double offsetX = 0;
        private double offsetY = 0;
        private double markerRadius = 10;

        // Gtk objects
        private DrawingArea chart;
        private Label xlabel;
        private Label ylabel;
        private Label zlabel;

        // State variables
        private bool dragging = false;

        public TernaryGraphView(ViewBase owner) : base(owner)
        {
            Box container = new Box(Orientation.Horizontal, 0);
            chart = new DrawingArea();
            chart.AddEvents((int)Gdk.EventMask.ExposureMask
            | (int)Gdk.EventMask.PointerMotionMask
            | (int)Gdk.EventMask.ButtonPressMask
            | (int)Gdk.EventMask.ButtonReleaseMask);


            chart.Drawn += OnDrawChart;

            chart.ButtonPressEvent += OnMouseButtonPress;
            chart.ButtonReleaseEvent += OnMouseButtonRelease;
            chart.MotionNotifyEvent += OnMouseMove;

            container.PackStart(chart, true, true, 0);

            xlabel = new Label();
            ylabel = new Label();
            zlabel = new Label();

            Box labels = new Box(Orientation.Vertical, 0);
            labels.PackStart(xlabel, false, false, 0);
            labels.PackStart(ylabel, false, false, 0);
            labels.PackStart(zlabel, false, false, 0);

            container.PackStart(labels, true, true, 0);

            mainWidget = container;
            mainWidget.Hide();
        }

        public void Detach()
        {
            try
            {

                chart.Drawn -= OnDrawChart;

                chart.ButtonPressEvent -= OnMouseButtonPress;
                chart.ButtonReleaseEvent -= OnMouseButtonRelease;
                chart.MotionNotifyEvent -= OnMouseMove;
            }
            catch (NullReferenceException)
            {
                // To keep Neil happy
            }
        }

        /// <summary>
        /// Show the graph.
        /// </summary>
        public void Show()
        {
            mainWidget.ShowAll();
        }

        /// <summary>
        /// The value of one of the variables to be shown.
        /// </summary>
        public double X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
                xlabel.Text = $"x: {x:F2}";
                Refresh(false);
            }
        }

        /// <summary>
        /// The value of one of the variables to be shown.
        /// </summary>
        public double Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
                ylabel.Text = $"y: {y:F2}";
                Refresh(false);
            }
        }

        /// <summary>
        /// The value of one of the variables to be shown.
        /// </summary>
        public double Z
        {
            get
            {
                return z;
            }
            set
            {
                z = value;
                zlabel.Text = $"z: {z:F2}";
                Refresh(false);
            }
        }

        /// <summary>
        /// X, Y, and Z must add to this value.
        /// </summary>
        public double Total { get; set; }

        private void DrawTriangle(Cairo.Context context)
        {
            double height = sideLength * Math.Sqrt(3) / 2;

            double x1 = offsetX;
            double y1 = height + offsetY;

            double x2 = 0.5 * sideLength + offsetX;
            double y2 = 0 + offsetY;

            double x3 = sideLength + offsetX;
            double y3 = height + offsetY;

            context.NewPath();
            context.SetSourceColor(Utility.Colour.ToOxy(owner.MainWidget.StyleContext.GetColor(StateFlags.Normal).ToColour().ToGdk()));

            context.MoveTo(x1, y1);

            context.LineTo(x2, y2);
            context.StrokePreserve();
            context.LineTo(x3, y3);
            context.StrokePreserve();
            context.LineTo(x1, y1);
            context.StrokePreserve();
        }

        private PointD MarkerPosition()
        {
            // In gtk/gdk coordinates, (0, 0) is the top-left corner.
            double markerX = 0.5 * (2 * Y + Z) / Total;
            //double markerY = (Math.Sqrt(3) / 2) * (Z / Total);
            double markerY = (Math.Sqrt(3) / 2) * (1 - Z / Total);

            markerX = markerX * sideLength + offsetX;
            markerY = markerY * sideLength + offsetY;

            return new PointD(markerX, markerY);
        }

        /// <summary>
        /// Translates cartesian to barycentric coordinates and updates
        /// the marker location, label text and internal variable value
        /// but does update the model/presenter.
        /// </summary>
        /// <param name="point"></param>
        private void MoveTo(PointD point)
        {
            // Coordinates must be adjusted according to offset/scale.
            double posX = (point.X - offsetX) / sideLength;
            double posY = (point.Y - offsetY) / sideLength;

            Z = MathUtilities.Bound(Total * (1 - 2 * posY / Math.Sqrt(3)), 0, Total);
            Y = MathUtilities.Bound((2 * posX * Total - Z) / 2, 0, Total - Z);
            X = MathUtilities.Bound(Total - Y - Z, 0, Total);

            chart.QueueDraw();
        }

        private void DrawMarker(Context context)
        {
            PointD p = MarkerPosition();

            context.NewPath();
            context.Arc(p.X, p.Y, markerRadius, 0, 2 * Math.PI);
            context.StrokePreserve();
            context.SetSourceColor(owner.MainWidget.StyleContext.GetColor(StateFlags.Normal).ToColour().ToOxy());
            context.Fill();
        }

        private void Refresh(bool drawTriangle)
        {

            bool isPaintable = chart.AppPaintable;

            if (isPaintable && chart.Visible)
            {

                Gdk.DrawingContext drawingContext = chart.Window.BeginDrawFrame(chart.Window.VisibleRegion);
                Context context = drawingContext.CairoContext;


                if (drawTriangle)
                    DrawTriangle(context);
                DrawMarker(context);
            }
        }

        /// <summary>
        /// Calculates euclidean distance between two points.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        public static double Distance(PointD point1, PointD point2)
        {
            double a = point2.X - point1.X;
            double b = point2.Y - point1.Y;

            return Math.Sqrt(a * a + b * b);
        }

        /// <summary>
        /// Checks if a point is in the marker.
        /// </summary>
        /// <param name="clickPoint">Point to check.</param>
        private bool InMarker(PointD clickPoint)
        {
            return Distance(clickPoint, MarkerPosition()) < markerRadius;
        }

        /// <summary>
        /// Invoked when the chart is rendered. Handles drawing of the
        /// lines of the triangle.
        /// </summary>
        /// <param name="o">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnDrawChart(object o, DrawnArgs args)
        {
            try
            {
                Refresh(true);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Mouse button has been pressed down. If the cursor is on the marker,
        /// set state variables correspondingly.
        /// </summary>
        /// <param name="o">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnMouseButtonPress(object o, ButtonPressEventArgs args)
        {
            try
            {
                // Get the point clicked by the mouse.
                PointD clickPoint = new PointD(args.Event.X, args.Event.Y);

                // Check if the mouse cursor is on the marker.
                if (InMarker(clickPoint))
                    dragging = true;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Mouse has moved. Move marker and update labels if LMB is held down.
        /// </summary>
        /// <param name="o">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnMouseMove(object o, MotionNotifyEventArgs args)
        {
            try
            {
                if (dragging)
                {
                    // Get the point clicked by the mouse.
                    PointD movePoint = new PointD(args.Event.X, args.Event.Y);
                    MoveTo(movePoint);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Mouse button has been released. Fire off an event for the presenter.
        /// </summary>
        /// <param name="o">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnMouseButtonRelease(object o, ButtonReleaseEventArgs args)
        {
            try
            {
                dragging = false;
                // todo : fire off a changed event for the presenter
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
