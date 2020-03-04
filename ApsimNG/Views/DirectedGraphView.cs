namespace UserInterface.Views
{
    using ApsimNG.Classes.DirectedGraph;
    using Cairo;
    using Gtk;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public class DirectedGraphView : ViewBase
    {
        /// <summary>
        /// The currently selected node/object.
        /// </summary>
        private DGObject selectedObject;

        /// <summary>
        /// Keeps track of whether the mouse button is currently down.
        /// </summary>
        private bool mouseDown = false;

        /// <summary>
        /// Drawing area upon which the graph is rendered.
        /// </summary>
        private DrawingArea drawable;

        /// <summary>
        /// Position of the last moved node.
        /// </summary>
        private PointD lastPos;

        /// <summary>
        /// List of nodes. These are currently circles with text in them.
        /// </summary>
        private List<DGNode> nodes = new List<DGNode>();

        /// <summary>
        /// List of arcs which connect the nodes.
        /// </summary>
        private List<DGArc> arcs = new List<DGArc>();

        /// <summary>Initializes a new instance of the <see cref="DirectedGraphView" /> class.</summary>
        public DirectedGraphView(ViewBase owner = null) : base(owner)
        {
            drawable = new DrawingArea();
            drawable.AddEvents(
            (int)Gdk.EventMask.PointerMotionMask
            | (int)Gdk.EventMask.ButtonPressMask
            | (int)Gdk.EventMask.ButtonReleaseMask);

            drawable.ExposeEvent += OnDrawingAreaExpose;
            drawable.ButtonPressEvent += OnMouseButtonPress;
            drawable.ButtonReleaseEvent += OnMouseButtonRelease;
            drawable.MotionNotifyEvent += OnMouseMove;

            ScrolledWindow scroller = new ScrolledWindow(new Adjustment(0, 0, 100, 1, 1, 1), new Adjustment(0, 0, 100, 1, 1, 1))
            {
                HscrollbarPolicy = PolicyType.Always,
                VscrollbarPolicy = PolicyType.Always
            };
            
            scroller.AddWithViewport(drawable);
            
            mainWidget = scroller;
            drawable.Realized += OnRealized;
            if (owner == null)
            {
                DGObject.DefaultOutlineColour = OxyPlot.OxyColors.Black;
            }
            else
            {
                DGObject.DefaultOutlineColour = Utility.Colour.GtkToOxyColor(owner.MainWidget.Style.Foreground(StateType.Normal));
                DGObject.DefaultBackgroundColour = Utility.Colour.GtkToOxyColor(owner.MainWidget.Style.Background(StateType.Normal));
            }
        }

        /// <summary>The description (nodes & arcs) of the directed graph.</summary>
        public DirectedGraph DirectedGraph
        {
            get
            {
                DirectedGraph graph = new DirectedGraph();
                nodes.ForEach(node => graph.Nodes.Add(node.ToNode()));
                arcs.ForEach(arc => graph.Arcs.Add(arc.ToArc()));
                return graph;
            }
            set
            {
                value.Nodes.ForEach(node => nodes.Add(new DGNode(node)));
                value.Arcs.ForEach(arc => arcs.Add(new DGArc(arc, nodes)));
            }
        }

        /// <summary>Export the view to the image</summary>
        public System.Drawing.Image Export()
        {
            int width;
            int height;
            MainWidget.GdkWindow.GetSize(out width, out height);
            Gdk.Pixbuf screenshot = Gdk.Pixbuf.FromDrawable(drawable.GdkWindow, drawable.Colormap, 0, 0, 0, 0, width - 20, height - 20);
            byte[] buffer = screenshot.SaveToBuffer("png");
            MemoryStream stream = new MemoryStream(buffer);
            System.Drawing.Bitmap bitmap = new Bitmap(stream);
            return bitmap;
        }

        /// <summary>The drawing canvas is being exposed to user.</summary>
        private void OnDrawingAreaExpose(object sender, ExposeEventArgs args)
        {
            try
            {
                DrawingArea area = (DrawingArea)sender;

                Cairo.Context context = Gdk.CairoHelper.Create(area.GdkWindow);

                foreach (DGArc tmpArc in arcs)
                    tmpArc.Paint(context);
                foreach (DGNode tmpNode in nodes)
                    tmpNode.Paint(context);

                ((IDisposable)context.Target).Dispose();
                ((IDisposable)context).Dispose();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Mouse button has been pressed</summary>
        private void OnMouseButtonPress(object o, ButtonPressEventArgs args)
        {
            try
            {
                // Get the point clicked by the mouse.
                PointD clickPoint = new PointD(args.Event.X, args.Event.Y);

                // Delselect existing object
                if (selectedObject != null)
                    selectedObject.Selected = false;

                // Look through nodes for the click point
                selectedObject = nodes.FindLast(node => node.HitTest(clickPoint));

                // If not found, look through arcs for the click point
                if (selectedObject == null)
                    selectedObject = arcs.FindLast(arc => arc.HitTest(clickPoint));

                // If found object, select it.
                if (selectedObject != null)
                {
                    selectedObject.Selected = true;
                    mouseDown = true;
                    lastPos = clickPoint;
                }

                // Redraw area.
                (o as DrawingArea).QueueDraw();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Mouse has been moved</summary>
        private void OnMouseMove(object o, MotionNotifyEventArgs args)
        {
            try
            {
                // Get the point clicked by the mouse.
                PointD movePoint = new PointD(args.Event.X, args.Event.Y);

                // If an object is under the mouse then move it
                if (mouseDown && selectedObject != null)
                {
                    lastPos.X = movePoint.X;
                    lastPos.Y = movePoint.Y;
                    selectedObject.Location = movePoint;
                    // Redraw area.
                    (o as DrawingArea).QueueDraw();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Mouse button has been released</summary>
        private void OnMouseButtonRelease(object o, ButtonReleaseEventArgs args)
        {
            try
            {
                mouseDown = false;
                CheckSizing();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Drawing area has been rendered - make sure it has enough space.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnRealized(object sender, EventArgs args)
        {
            try
            {
                CheckSizing();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// If the right-most node is out of the drawing area, doubles the width.
        /// If the bottom-most node is out of the drawing area, doubles the height;
        /// </summary>
        private void CheckSizing()
        {
            if (nodes != null && nodes.Any())
            {
                DGNode rightMostNode = nodes.Aggregate((node1, node2) => node1.Location.X > node2.Location.X ? node1 : node2);
                DGNode bottomMostNode = nodes.Aggregate((node1, node2) => node1.Location.Y > node2.Location.Y ? node1 : node2);
                if (rightMostNode.Location.X + rightMostNode.Width >= drawable.Allocation.Width)
                    drawable.WidthRequest = 2 * drawable.Allocation.Width;
                // I Assume that the nodes are circles such that width = height.
                if (bottomMostNode.Location.Y + bottomMostNode.Width >= drawable.Allocation.Height)
                    drawable.HeightRequest = 2 * drawable.Allocation.Height;
            }
        }
    }
}
