// -----------------------------------------------------------------------
// <copyright file="DirectedGraphView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using ApsimNG.Classes.DirectedGraph;
    using Cairo;
    using Gtk;
    using Models.Graph;
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
        public DGObject selectedObject { get; private set; }

        /// <summary>
        /// The currently second selected node/object. (button 3)
        /// </summary>
        public DGObject selected2Object { get; private set; }

        /// <summary>
        /// Keeps track of whether the mouse button is currently down.
        /// </summary>
        private bool isDragging = false;

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


        /// <summary>
        /// When a single object is selected
        /// </summary>
        public event EventHandler<GraphObjectSelectedArgs> OnGraphObjectSelected;

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
            mainWidget.Destroyed += OnDestroyed;
            
            DGObject.DefaultOutlineColour = Utility.Colour.GtkToOxyColor(owner.MainWidget.Style.Foreground(StateType.Normal));
            DGObject.DefaultBackgroundColour = Utility.Colour.GtkToOxyColor(owner.MainWidget.Style.Background(StateType.Normal));
        }


        private void OnDestroyed(object sender, EventArgs e)
        {
            mainWidget.Destroyed -= OnDestroyed;
        }
        /// <summary>The description (nodes & arcs) of the directed graph.</summary>
        public DirectedGraph DirectedGraph
        {
            get
            {
                DirectedGraph graph = new DirectedGraph();
                nodes.ForEach(node => graph.AddNode(node.ToNode()));
                arcs.ForEach(arc => graph.AddArc(arc.ToArc()));
                return graph;
            }
            set
            {
                List<DGNode> selectedNodes = nodes.FindAll(node => node.Selected);
                List<DGArc> selectedArcs = arcs.FindAll(arc => arc.Selected);

                nodes.Clear(); arcs.Clear();
                value.Nodes.ForEach(node => nodes.Add(new DGNode(node)));
                nodes.ForEach(node => { if (selectedNodes.Find(n => n.Name == node.Name) != null) { node.Selected = true; } });

                value.Arcs.ForEach(arc => arcs.Add(new DGArc(arc, nodes)));
                arcs.ForEach(arc => { if (selectedArcs.Find(a => a.Name == arc.Name) != null) { arc.Selected = true; } });

                // Redraw area.
                drawable.QueueDraw();
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
        [GLib.ConnectBefore]
        private void OnMouseButtonPress(object o, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 1)
            {
                // Deselect existing object
                UnSelect();

                // Get the point clicked by the mouse.
                PointD clickPoint = new PointD(args.Event.X, args.Event.Y);

                // Look through nodes for the click point
                DGObject clickedObject = nodes.FindLast(node => node.HitTest(clickPoint));

                // If not found, look through arcs for the click point
                if (clickedObject == null)
                    clickedObject = arcs.FindLast(arc => arc.HitTest(clickPoint));

                // If found object, select it.
                if (clickedObject != null)
                {
                    clickedObject.Selected = true;
                    selectedObject = clickedObject;
                    if (args.Event.Button == 1)
                        isDragging = true;
                    lastPos = clickPoint;
                }
            }

            if (args.Event.Button == 3)
            {
                if (selected2Object != null) selected2Object.Selected = false;
                selected2Object = null;

                // Get the point clicked by the mouse.
                PointD clickPoint = new PointD(args.Event.X, args.Event.Y);

                // Look through nodes for the click point
                DGObject clickedObject = nodes.FindLast(node => node.HitTest(clickPoint));

                // If not found, look through arcs for the click point
                if (clickedObject == null)
                    clickedObject = arcs.FindLast(arc => arc.HitTest(clickPoint));

                // If found object, select it.
                if (clickedObject != null &&  !clickedObject.Equals(selectedObject))
                {
                    selected2Object = clickedObject;
                    selected2Object.Selected = true;
                }
            }

            // Redraw area.
            (o as DrawingArea).QueueDraw();
        }

        /// <summary>Mouse has been moved</summary>
        private void OnMouseMove(object o, MotionNotifyEventArgs args)
        {
            // Get the point clicked by the mouse.
            PointD movePoint = new PointD(args.Event.X, args.Event.Y);

            // If an object is under the mouse then move it
            if (isDragging && selectedObject != null)
            {
                lastPos.X = movePoint.X;
                lastPos.Y = movePoint.Y;
                selectedObject.Location = movePoint;
                // Redraw area.
                (o as DrawingArea).QueueDraw();
            }
        }

        /// <summary>Mouse button has been released</summary>
        [GLib.ConnectBefore]
        private void OnMouseButtonRelease(object o, ButtonReleaseEventArgs args)
        {
            args.RetVal = true;
#if false
            DGObject clickedObject = null;
            if (! isDragging)
            {
                // Get the point clicked by the mouse.
                PointD clickPoint = new PointD(args.Event.X, args.Event.Y);

                // Look through nodes for the click point
                clickedObject = nodes.FindLast(node => node.HitTest(clickPoint));

                // If not found, look through arcs for the click point
                if (clickedObject == null)
                    clickedObject = arcs.FindLast(arc => arc.HitTest(clickPoint));

                //if (clickedObject == null) UnSelect();
            }
#endif
            if (args.Event.Button == 1)
            {
                OnGraphObjectSelected?.Invoke(this, new GraphObjectSelectedArgs(selectedObject, selected2Object)); 
            }
            isDragging = false;
            CheckSizing();
        }

        public void UnSelect()
        {
            Console.WriteLine("Unselected");
            nodes.ForEach(node => {  node.Selected = false; });
            arcs.ForEach(arc => { arc.Selected = false; });
            selectedObject = null;
            selected2Object = null;

            // Redraw area.
            drawable.QueueDraw();
        }
        /// <summary>
        /// Drawing area has been rendered - make sure it has enough space.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnRealized(object sender, EventArgs args)
        {
            CheckSizing();
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

    public class GraphObjectSelectedArgs : EventArgs
    {
        public GraphObjectSelectedArgs()
        {
            Object1 = null;
            Object2 = null;
        }
        public GraphObjectSelectedArgs(DGObject a)
        {
            Object1 = a;
            Object2 = null;
        }
        public GraphObjectSelectedArgs(DGObject a, DGObject b)
        {
            Object1 = a;
            Object2 = b;
        }
        public DGObject Object1 { get; set; }
        public DGObject Object2 { get; set; }
    }
}