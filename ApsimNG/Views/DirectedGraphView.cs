using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using Point = System.Drawing.Point;
using APSIM.Interop.Visualisation;
using APSIM.Shared.Graphing;
using Utility;
using ApsimNG.EventArguments.DirectedGraph;

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
        public List<DGObject> SelectedObjects { get; private set; }

        /// <summary>
        /// The currently hovered node.
        /// </summary>
        public DGObject HoverObject { get; private set; }

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
        public DGArc tempArc = null;

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
        private List<DGNode> nodes = new List<DGNode>();

        /// <summary>
        /// List of arcs which connect the nodes.
        /// </summary>
        private List<DGArc> arcs = new List<DGArc>();

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
                List<int> selectedObjectIDs = new List<int>();
                if (SelectedObjects != null)
                    foreach (DGObject obj in SelectedObjects)
                        selectedObjectIDs.Add(obj.ID);
                SelectedObjects = new List<DGObject>();
                nodes.Clear();
                arcs.Clear();

                value.Nodes.ForEach(node => nodes.Add(new DGNode(node)));
                value.Arcs.ForEach(arc => arcs.Add(new DGArc(arc, nodes)));

                foreach (int id in selectedObjectIDs)
                {
                    DGObject SelectedObject = nodes?.Find(n => n.ID == id);
                    if (SelectedObject == null)
                        SelectedObject = arcs?.Find(a => a.ID == id);
                    if (SelectedObject != null)
                        SelectedObjects.Add(SelectedObject);
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
                    DGObject objClicked = null;
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
                        SelectedObjects = new List<DGObject>();
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
                                        if (SelectedObjects[0] is DGNode)
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
                // Delselect existing object
                if (HoverObject != null)
                    HoverObject.Hover = false;

                // Get the point where the mouse is.
                Point movePoint = new Point((int)args.Event.X - selectOffset.X, (int)args.Event.Y - selectOffset.Y);

                //Move connected arcs half the distance the node is moved
                Point diff = new Point(movePoint.X - lastPos.X, movePoint.Y - lastPos.Y);

                if (isDrawingArc)
                {
                    int x = tempArc.Location.X + (diff.X / 2);
                    int y = tempArc.Location.Y + (diff.Y / 2);
                    tempArc.Location = new Point(x, y);
                    tempArc.Target.Location = new Point((int)args.Event.X, (int)args.Event.Y);
                }
                else if (mouseDown)
                {
                    if (isDragging)
                    {
                        if (SelectedObjects.Count > 0) // If an object is under the mouse and the mouse is down, then move it
                        {
                            for (int i = 0; i < SelectedObjects.Count; i++)
                            {
                                int x = SelectedObjects[i].Location.X + diff.X;
                                int y = SelectedObjects[i].Location.Y + diff.Y;
                                SelectedObjects[i].Location = new Point(x, y);

                                if (SelectedObjects[i] is DGNode)
                                {
                                    for (int j = 0; j < arcs.Count; j++)
                                    {
                                        if (arcs[j].Selected == false)
                                        {
                                            DGNode source = arcs[j].Source;
                                            DGNode target = arcs[j].Target;

                                            if ((SelectedObjects[i] as DGNode) == source || (SelectedObjects[i] as DGNode) == target)
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
                        // Look through nodes for the click point
                        DGObject clickedObject = nodes.FindLast(node => node.HitTest(clickPoint));

                        // If not found, look through arcs for the click point
                        if (clickedObject == null)
                            clickedObject = arcs.FindLast(arc => arc.HitTest(clickPoint));

                        if (clickedObject == null)
                            UnSelect();
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
            SelectedObjects = new List<DGObject>();

            //Look through nodes that are in rectangle
            for(int i = 0; i < nodes.Count; i++)
                if (nodes[i].HitTest(selectionRect.GetRectangle()))
                    if (!single || (single && SelectedObjects.Count == 0))
                        SelectedObjects.Add(nodes[i]);

            if (!single || (single && SelectedObjects.Count == 0))
                // Look through arcs for the click point
                for (int i = 0; i < arcs.Count; i++)
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
            nodes.ForEach(node => {  node.Selected = false; });
            arcs.ForEach(arc => { arc.Selected = false; });
            SelectedObjects = new List<DGObject>();
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
