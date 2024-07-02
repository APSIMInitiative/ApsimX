using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using Point = System.Drawing.Point;
using APSIM.Interop.Visualisation;
using APSIM.Shared.Graphing;
using Utility;
using ApsimNG.EventArguments.DirectedGraph;
using Gtk.Sheet;

namespace UserInterface.Views
{
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
        public List<GraphObject> SelectedObjects { get; private set; }

        /// <summary>
        /// The currently hovered node.
        /// </summary>
        public GraphObject HoverObject { get; private set; }

        /// <summary>
        /// Keeps track of whether the user is currently dragging an object.
        /// </summary>
        private bool isDragging = false;

        /// <summary>
        /// Keeps track of whether the mouse button is currently down.
        /// </summary>
        private bool mouseDown = false;

        private Point selectionPoint;
        private DGRectangle selectionRectangle;

        /// <summary>
        /// Keeps track of if an arc is being drawn to screen with the mouse.
        /// </summary>
        public bool isDrawingArc = false;

        /// <summary>
        /// A temporary arc to draw when an arc is being created.
        /// </summary>
        public Arc tempArc = null;

        /// <summary>
        /// Drawing area upon which the graph is rendered.
        /// </summary>
        private DrawingArea drawable;

        /// <summary>
        /// Position of the last moved node.
        /// </summary>
        public Point lastPos;

        /// <summary>
        /// Position of the last moved node.
        /// </summary>
        private Point selectOffset;

        /// <summary>
        /// List of nodes. These are currently circles with text in them.
        /// </summary>
        private List<APSIM.Shared.Graphing.Node> nodes
        {
            get { return DirectedGraph.Nodes; }
        }

        /// <summary>
        /// List of arcs which connect the nodes.
        /// </summary>
        private List<Arc> arcs
        {
            get { return DirectedGraph.Arcs; }
        }

        /// <summary>The description (nodes and arcs) of the directed graph.</summary>
        public DirectedGraph DirectedGraph { get; set; }

        /// <summary>
        /// When a single object is selected
        /// </summary>
        public event EventHandler<GraphObjectsArgs> OnGraphObjectSelected;

        /// <summary>
        /// When an object is moved. Called after the user has finished
        /// moving the object (e.g. on mouse up).
        /// </summary>
        public event EventHandler<GraphObjectsArgs> OnGraphObjectMoved;

        /// <summary>
        /// Called when an arc is finished being placed
        /// </summary>
        public event EventHandler<EventArgs> AddArc;

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

            DirectedGraph = new DirectedGraph();

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
            while (GLib.MainContext.Iteration()) ;

            return window.Pixbuf;
        }

        public void ProcessClick(Point clickPoint) {
            // Look through nodes for the click point
            GraphObject clickedObject = nodes.FindLast(node => node.HitTest(clickPoint));

            // If not found, look through arcs for the click point
            if (clickedObject == null)
                clickedObject = arcs.FindLast(arc => arc.HitTest(clickPoint));

            if (clickedObject == null) 
            {
                UnSelect();
            }
            else 
            {
                Select(new DGRectangle(clickPoint.X, clickPoint.Y, 1, 1), true);
            }
                
        }

        /// <summary>The drawing canvas is being exposed to user.</summary>
        private void OnDrawingAreaExpose(object sender, DrawnArgs args)
        {
            try
            {
                this.drawable = (DrawingArea)sender;

                Cairo.Context context = args.Cr;

                GraphObject.DefaultOutlineColour = this.drawable.StyleContext.GetColor(StateFlags.Normal).ToColour();
                GraphObject.DefaultBackgroundColour = this.drawable.StyleContext.GetColor(StateFlags.Normal).ToColour();

                CairoContext drawingContext = new CairoContext(context, MainWidget);

                if (isDrawingArc)
                    arcs.Add(tempArc);

                DirectedGraphRenderer.Draw(drawingContext, arcs, nodes, selectionRectangle);

                if (isDrawingArc)
                    arcs.Remove(tempArc);

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

                if (args.Event.Button == 1 || args.Event.Button == 3)
                {
                    mouseDown = true;

                    isDragging = false;
                    GraphObject objClicked = null;
                    for (int i = 0; i < nodes.Count && isDragging == false; i++)
                    {
                        if (nodes[i].HitTest(clickPoint))
                        {
                            isDragging = true;
                            objClicked = nodes[i];
                        }
                    }

                    for (int i = 0; i < arcs.Count && isDragging == false; i++)
                    {
                        if (arcs[i].HitTest(clickPoint))
                        {
                            isDragging = true;
                            objClicked = arcs[i];
                        }
                    }

                    if (!isDragging && args.Event.Button == 1) //drawing a selection box
                    {
                        selectionPoint = clickPoint;
                        selectionRectangle = new DGRectangle(selectionPoint.X, selectionPoint.Y, 1, 1);
                        SelectedObjects = new List<GraphObject>();
                    }
                    else
                    {
                        if (objClicked != null)
                        {
                            if (!objClicked.Selected)
                            {
                                DGRectangle rect = new DGRectangle(clickPoint.X, clickPoint.Y, 1, 1);
                                Select(rect, true);

                                // If found object, select it.
                                if (SelectedObjects.Count == 1)
                                {
                                    selectOffset = new Point(clickPoint.X - SelectedObjects[0].Location.X, clickPoint.Y - SelectedObjects[0].Location.Y);
                                    lastPos = new Point(SelectedObjects[0].Location.X, SelectedObjects[0].Location.Y);
                                    OnGraphObjectSelected?.Invoke(this, new GraphObjectsArgs(SelectedObjects));

                                    if (isDrawingArc)
                                        if (SelectedObjects[0] is APSIM.Shared.Graphing.Node)
                                            AddArc?.Invoke(this, new EventArgs());
                                }
                            }
                        }
                    }

                    // Redraw area.
                    (o as DrawingArea).QueueDraw();
                }
                else
                {

                }

                isDrawingArc = false;
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
                // Get the point where the mouse is.
                Point movePoint = new Point((int)args.Event.X - selectOffset.X, (int)args.Event.Y - selectOffset.Y);

                this.MainWidget.HasFocus = true;

                // Delselect existing object
                if (HoverObject != null)
                    HoverObject.Hover = false;

                //Move connected arcs half the distance the node is moved
                Point diff = new Point(movePoint.X - lastPos.X, movePoint.Y - lastPos.Y);

                if (isDrawingArc)
                {
                    int x = tempArc.Location.X + (diff.X / 2);
                    int y = tempArc.Location.Y + (diff.Y / 2);
                    tempArc.Location = new Point(x, y);
                    tempArc.Destination.Location = new Point((int)args.Event.X, (int)args.Event.Y);
                }
                else if (mouseDown)
                {
                    if (isDragging)
                    {
                        if (SelectedObjects != null && SelectedObjects.Count > 0) // If  objects are selected and the mouse is down, then move it
                        {
                            for (int i = 0; i < SelectedObjects.Count; i++)
                            {
                                int x = SelectedObjects[i].Location.X + diff.X;
                                int y = SelectedObjects[i].Location.Y + diff.Y;
                                if (x > drawable.AllocatedWidth)
                                    x = drawable.AllocatedWidth;
                                else if (x < 0)
                                    x = 0;

                                if (y > drawable.AllocatedHeight)
                                    y = drawable.AllocatedHeight;
                                else if (y < 0)
                                    y = 0;

                                SelectedObjects[i].Location = new Point(x, y);

                                if (SelectedObjects[i] is APSIM.Shared.Graphing.Node)
                                {
                                    for (int j = 0; j < arcs.Count; j++)
                                    {
                                        if (arcs[j].Selected == false)
                                        {
                                            APSIM.Shared.Graphing.Node source = arcs[j].Source;
                                            APSIM.Shared.Graphing.Node target = arcs[j].Destination;

                                            if ((SelectedObjects[i] as APSIM.Shared.Graphing.Node) == source || (SelectedObjects[i] as APSIM.Shared.Graphing.Node) == target)
                                            {
                                                x = arcs[j].Location.X + (diff.X / 2);
                                                y = arcs[j].Location.Y + (diff.Y / 2);
                                                arcs[j].Location = new Point(x, y);
                                            }
                                        }
                                    }
                                }
                            }
                            // Redraw area.
                            (o as DrawingArea).QueueDraw();
                        }
                    }
                    else if (selectionRectangle != null)
                    {
                        //drawing selection rectangle
                        int xLower = selectionPoint.X;
                        int xUpper = (int)args.Event.X;
                        if (xUpper < xLower)
                        {
                            xLower = (int)args.Event.X;
                            xUpper = selectionPoint.X;
                        }
                        int yLower = selectionPoint.Y;
                        int yUpper = (int)args.Event.Y;
                        if (yUpper < yLower)
                        {
                            yLower = (int)args.Event.Y;
                            yUpper = selectionPoint.Y;
                        }
                        selectionRectangle = new DGRectangle(xLower, yLower, xUpper - xLower, yUpper - yLower);
                    }
                }
                else
                {
                    //do hover effects
                    // Look through nodes for the click point
                    HoverObject = nodes.FindLast(node => node.HitTest(movePoint));

                    // If not found, look through arcs for the click point
                    if (HoverObject == null)
                        HoverObject = arcs.FindLast(arc => arc.HitTest(movePoint));

                    // If found object, select it.
                    if (HoverObject != null)
                        HoverObject.Hover = true;
                }

                lastPos.X = movePoint.X;
                lastPos.Y = movePoint.Y;

                // Redraw area.
                (o as DrawingArea).QueueDraw();
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
                    {
                        OnGraphObjectMoved?.Invoke(this, new GraphObjectsArgs(SelectedObjects));
                    }
                    else if (selectionRectangle != null)
                    {
                        Select(selectionRectangle, false);
                        OnGraphObjectSelected?.Invoke(this, new GraphObjectsArgs(SelectedObjects));
                        selectionRectangle = null;
                    }
                    else
                    {
                        Point clickPoint = new Point((int)args.Event.X, (int)args.Event.Y);
                        ProcessClick(clickPoint);
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

        public void Select(DGRectangle selectionRect, bool single)
        {
            // Delselect existing objects
            SelectedObjects = new List<GraphObject>();
            for (int i = 0; i < nodes.Count; i++)
                nodes[i].Selected = false;
            for (int i = 0; i < arcs.Count; i++)
                arcs[i].Selected = false;

            //Look through nodes that are in rectangle
            for (int i = 0; i < nodes.Count && (!single || SelectedObjects.Count == 0); i++)
                if (nodes[i].HitTest(selectionRect.GetRectangle()))
                    SelectedObjects.Add(nodes[i]);

            for (int i = 0; i < arcs.Count && (!single || SelectedObjects.Count == 0); i++)
                if (arcs[i].HitTest(selectionRect.GetRectangle()))
                    SelectedObjects.Add(arcs[i]);

            for (int i = 0; i < SelectedObjects.Count; i++)
                SelectedObjects[i].Selected = true;
        }

        /// <summary>
        /// Unselect any selected objects.
        /// </summary> 
        public void UnSelect()
        {
            nodes.ForEach(node => { node.Selected = false; });
            arcs.ForEach(arc => { arc.Selected = false; });
            SelectedObjects = new List<GraphObject>();
            mouseDown = false;
            isDragging = false;
            // Redraw area.
            drawable.QueueDraw();
            OnGraphObjectSelected?.Invoke(this, new GraphObjectsArgs(null));
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
        /// Corrects the position of Nodes and Arcs if they are off the screen.
        /// </summary>
        private void CheckSizing()
        {
            if (nodes != null && nodes.Any())
            {
                for(int i = 0; i < nodes.Count; i++) {
                    int x = nodes[i].Location.X;
                    if (x < 0)
                        x = 0;
                    else if (x > drawable.AllocatedWidth)
                        x = drawable.AllocatedWidth;

                    int y = nodes[i].Location.Y;
                    if (y < 0)
                        y = 0;
                    else if (y > drawable.AllocatedHeight)
                        y = drawable.AllocatedHeight;

                    nodes[i].Location = new Point(x, y);
                }

                for(int i = 0; i < arcs.Count; i++) {
                    int x = arcs[i].Location.X;
                    if (x < 0)
                        x = 0;
                    else if (x > drawable.AllocatedWidth)
                        x = drawable.AllocatedWidth;

                    int y = arcs[i].Location.Y;
                    if (y < 0)
                        y = 0;
                    else if (y > drawable.AllocatedHeight)
                        y = drawable.AllocatedHeight;

                    arcs[i].Location = new Point(x, y);
                }
            }
        }

    }
}
