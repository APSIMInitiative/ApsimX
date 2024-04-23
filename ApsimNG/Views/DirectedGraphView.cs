﻿namespace UserInterface.Views
{
    using Cairo;
    using Extensions;
    using EventArguments;
    using EventArguments.DirectedGraph;
    using Gtk;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using Color = System.Drawing.Color;
    using Point = System.Drawing.Point;
    using APSIM.Interop.Visualisation;
    using APSIM.Shared.Graphing;
    using Utility;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    /// <remarks>
    /// This code should be reworked to better work in the gtk3 way of thinking.
    /// Specifically, the way colours are handled seems to be different between gtk 2/3.
    /// </remarks>
    public class DirectedGraphView : ViewBase
    {
        /// <summary>
        /// The currently selected node.
        /// </summary>
        public DGObject SelectedObject { get; private set; }

        /// <summary>
        /// The currently second selected node/object. (button 3)
        /// </summary>
        /// <remarks>
        /// todo - this maybe shouldn't be public, but that change
        /// will require refactoring the context menu code in
        /// BubbleChartView.
        /// </remarks>
        public DGObject SelectedObject2 { get; private set; }

        /// <summary>
        /// Keeps track of whether the user is currently dragging an object.
        /// </summary>
        private bool isDragging = false;

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
        private Point lastPos;

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

        /// <summary>
        /// When an object is moved. Called after the user has finished
        /// moving the object (e.g. on mouse up).
        /// </summary>
        public event EventHandler<ObjectMovedArgs> OnGraphObjectMoved;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectedGraphView" /> class.
        /// </summary>
        public DirectedGraphView(ViewBase owner = null) : base(owner)
        {
            drawable = new DrawingArea();
            drawable.AddEvents(
            (int)Gdk.EventMask.PointerMotionMask
            | (int)Gdk.EventMask.ButtonPressMask
            | (int)Gdk.EventMask.ButtonReleaseMask);


            drawable.Drawn += OnDrawingAreaExpose;

            drawable.ButtonPressEvent += OnMouseButtonPress;
            drawable.ButtonReleaseEvent += OnMouseButtonRelease;
            drawable.MotionNotifyEvent += OnMouseMove;

            ScrolledWindow scroller = new ScrolledWindow()
            {
                HscrollbarPolicy = PolicyType.Always,
                VscrollbarPolicy = PolicyType.Always
            };


            // In gtk3, a viewport will automatically be added if required.
            scroller.Add(drawable);


            mainWidget = scroller;
            drawable.Realized += OnRealized;
            drawable.SizeAllocated += OnRealized;
            mainWidget.Destroyed += OnDestroyed;
        }

        private void OnDestroyed(object sender, EventArgs e)
        {
            try
            {
                mainWidget.Destroyed -= OnDestroyed;
                drawable.Realized -= OnRealized;
                drawable.SizeAllocated -= OnRealized;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>The description (nodes and arcs) of the directed graph.</summary>
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
                string selectedObjectName = SelectedObject?.Name;
                SelectedObject = null;
                nodes.Clear();
                arcs.Clear();
                value.Nodes.ForEach(node => nodes.Add(new DGNode(node)));
                value.Arcs.ForEach(arc => arcs.Add(new DGArc(arc, nodes)));
                if (!string.IsNullOrEmpty(selectedObjectName))
                {
                    SelectedObject = nodes?.Find(n => n.Name == selectedObjectName);
                    if (SelectedObject == null)
                        SelectedObject = arcs?.Find(a => a.Name == selectedObjectName);
                    if (SelectedObject != null)
                        SelectedObject.Selected = true;
                }
            }
        }

        /// <summary>Export the view to the image</summary>
        public Gdk.Pixbuf Export()
        {

            var window = new OffscreenWindow();
            window.Add(MainWidget);

            // Choose a good size for the image (which is square).
            int maxX = (int)nodes.Max(n => n.Location.X);
            int maxY = (int)nodes.Max(n => n.Location.Y);
            int maxSize = nodes.Max(n => n.Width);
            int size = Math.Max(maxX, maxY) + maxSize;

            MainWidget.WidthRequest = size;
            MainWidget.HeightRequest = size;
            window.ShowAll();
            while (GLib.MainContext.Iteration());

            return window.Pixbuf;
        }

        /// <summary>The drawing canvas is being exposed to user.</summary>
        private void OnDrawingAreaExpose(object sender, DrawnArgs args)
        {
            try
            {
                DrawingArea area = (DrawingArea)sender;

                Cairo.Context context = args.Cr;

                DGObject.DefaultOutlineColour = area.StyleContext.GetColor(StateFlags.Normal).ToColour();
#pragma warning disable 0612
                DGObject.DefaultBackgroundColour = area.StyleContext.GetBackgroundColor(StateFlags.Normal).ToColour();
#pragma warning restore 0612

                CairoContext drawingContext = new CairoContext(context, MainWidget);
                DirectedGraphRenderer.Draw(drawingContext, arcs, nodes);

                ((IDisposable)context.GetTarget()).Dispose();
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
                Point clickPoint = new Point((int)args.Event.X, (int)args.Event.Y);
                
                if (args.Event.Button == 1)
                {
                    mouseDown = true;

                    // Delselect existing object
                    if (SelectedObject != null)
                        SelectedObject.Selected = false;

                    // Look through nodes for the click point
                    SelectedObject = nodes.FindLast(node => node.HitTest(clickPoint));

                    // If not found, look through arcs for the click point
                    if (SelectedObject == null)
                        SelectedObject = arcs.FindLast(arc => arc.HitTest(clickPoint));

                    // If found object, select it.
                    if (SelectedObject != null)
                    {
                        SelectedObject.Selected = true;
                        lastPos = clickPoint;
                        OnGraphObjectSelected?.Invoke(this, new GraphObjectSelectedArgs(SelectedObject));
                    }

                    // Redraw area.
                    (o as DrawingArea).QueueDraw();
                }
                else
                {
                    if (SelectedObject2 != null)
                        SelectedObject2.Selected = false;
                    
                    SelectedObject2 = nodes.FindLast(node => node.HitTest(clickPoint));
                    if (SelectedObject2 == null)
                        SelectedObject2 = arcs.FindLast(arc => arc.HitTest(clickPoint));
                    
                    // If the user has right-clicked in the middle of nowhere, unselect everything.
                    if (SelectedObject2 == null)
                        UnSelect();
                    else if (SelectedObject2 == SelectedObject)
                        SelectedObject2 = null;
                }
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
                Point movePoint = new Point((int)args.Event.X, (int)args.Event.Y);

                // If an object is under the mouse then move it
                if (mouseDown && SelectedObject != null)
                {
                    lastPos.X = movePoint.X;
                    lastPos.Y = movePoint.Y;
                    SelectedObject.Location = movePoint;
                    isDragging = true;
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
                args.RetVal = true;
                mouseDown = false;

                if (args.Event.Button == 1)
                {
                    if (isDragging)
                        OnGraphObjectMoved?.Invoke(this, new ObjectMovedArgs(SelectedObject));
                    else
                    {
                        Point clickPoint = new Point((int)args.Event.X, (int)args.Event.Y);
                        // Look through nodes for the click point
                        DGObject clickedObject = nodes.FindLast(node => node.HitTest(clickPoint));

                        // If not found, look through arcs for the click point
                        if (clickedObject == null)
                            clickedObject = arcs.FindLast(arc => arc.HitTest(clickPoint));

                        if (clickedObject == null)
                            UnSelect();
                        //else
                        //{
                        //    clickedObject.Selected = true;
                        //    OnGraphObjectSelected?.Invoke(this, new GraphObjectSelectedArgs(clickedObject, null)); 
                        //}
                    }
                }
                isDragging = false;
                CheckSizing();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Unselect any selected objects.
        /// </summary>
        public void UnSelect()
        {
            nodes.ForEach(node => {  node.Selected = false; });
            arcs.ForEach(arc => { arc.Selected = false; });
            SelectedObject = null;
            SelectedObject2 = null;
            mouseDown = false;
            isDragging = false;
            // Redraw area.
            drawable.QueueDraw();
            OnGraphObjectSelected?.Invoke(this, new GraphObjectSelectedArgs(null));
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
