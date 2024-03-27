using System;
using System.Collections.Generic;
using UserInterface.Interfaces;
using Gtk;
using Utility;
using APSIM.Shared.Graphing;
using Node = APSIM.Shared.Graphing.Node;
using APSIM.Shared.Utilities;
using Gdk;
using ApsimNG.EventArguments.DirectedGraph;

namespace UserInterface.Views
{
    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    /// <remarks>
    /// todo:
    /// - use IDs not names?
    /// - refactor the mechanism used to generate a unique name for new nodes/arcs.
    /// - reconsider the packing rules. Setting expand and fill both to true might be unnecessary
    /// - should use property presenter rather than manually handle properties like InitialState.
    /// </remarks>
    public class BubbleChartView : ViewBase, IBubbleChartView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler<GraphObjectsArgs> GraphObjectSelected;

        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler<GraphChangedEventArgs> GraphChanged;

        /// <summary>Invoked when the user adds a node</summary>
        public event EventHandler<AddNodeEventArgs> AddNode;

        /// <summary>Invoked when the user deletes a node</summary>
        public event EventHandler<GraphObjectsArgs> DelNode;

        /// <summary>Invoked when the user adds an arc</summary>
        public event EventHandler<AddArcEventArgs> AddArcEnd;

        /// <summary>Invoked when the user deletes an arc</summary>
        public event EventHandler<GraphObjectsArgs> DelArc;

        private HPaned topPaned;
        private HBox chartBox = null;
        private HPaned settingsPaned = null;
        private HBox propertiesBox = null;
        private Frame objectFrame = null;
        private HBox objectBox = null;
        private Label instructionsLabel = null;
        private DirectedGraphView graphView;

        private Menu ContextMenu = new Menu();
        private ContextMenuHelper contextMenuHelper;

        private Dictionary<int, List<string>> rules = new Dictionary<int, List<string>>();
        private Dictionary<int, List<string>> actions = new Dictionary<int, List<string>>();

        private bool isDrawingArc = false;

        /// <summary>
        /// Properties editor.
        /// </summary>
        public IPropertyView PropertiesView { get; private set; }
        /// <summary>
        /// Node Properties editor.
        /// </summary>
        public IPropertyView ObjectPropertiesView { get; private set; }

        public BubbleChartView(ViewBase owner = null) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.BubbleChartView.glade");
            mainWidget = (Widget)builder.GetObject("main_widget");
            mainWidget.Destroyed += OnDestroyed;

            System.Drawing.Rectangle explorererBounds = GtkUtilities.GetBorderOfRightHandView(owner as ExplorerView);
            int topOfWindow = explorererBounds.Y;
            int bottomOfWindow = explorererBounds.Y + explorererBounds.Height;
            int heightOfWindow = explorererBounds.Height;
            int splitterPosition = (int)(heightOfWindow * 0.7);

            int widthOfWindow = explorererBounds.Width;
            int horizontalSplitterPosition = (int)(widthOfWindow * 0.6);

            (mainWidget as VPaned).Position = splitterPosition;

            topPaned = (Gtk.HPaned)builder.GetObject("top_paned");
            topPaned.Position = horizontalSplitterPosition;

            chartBox = (Gtk.HBox)builder.GetObject("chart_box");
            settingsPaned = (Gtk.HPaned)builder.GetObject("settings_paned");
            settingsPaned.Position = (int)(widthOfWindow * 0.4);

            propertiesBox = (Gtk.HBox)builder.GetObject("properties_box");
            objectBox = (Gtk.HBox)builder.GetObject("object_box");
            HBox instructionsBox = (Gtk.HBox)builder.GetObject("instructions_box");

            graphView = new DirectedGraphView(this);
            chartBox.Add(graphView.MainWidget);
            graphView.AddArc += OnAddArcEnd;

            PropertiesView = new PropertyView(this);
            Gtk.Frame propertyFrame = new Gtk.Frame(" Properties: ");
            propertyFrame.LabelXalign = 0.01f;
            propertyFrame.Add(((ViewBase)PropertiesView).MainWidget);
            propertiesBox.PackStart(propertyFrame, true, true, 2);

            ObjectPropertiesView = new PropertyView(this);
            objectFrame = new Gtk.Frame(" Properties: ");
            objectFrame.LabelXalign = 0.01f;
            objectFrame.Add(((ViewBase)ObjectPropertiesView).MainWidget);
            objectBox.PackStart(objectFrame, true, true, 2);

            instructionsLabel = new Gtk.Label();
            instructionsLabel.Text = "<left-click>: select a node or arc.\n" +
            "<right-click>: shows a context-sensitive menu.\n\n" +
            "Once a node/arc is selected, it can be dragged to a new position.\n\n" +
            "Nodes are created by right-clicking on a blank area.\n\n" +
            "Transition arcs are created by firstly selecting a source node,\n" +
            "then right-clicking over a target node.";
            instructionsLabel.Xalign = 0;
            instructionsLabel.Yalign = 0;
            Gtk.Frame instructionsFrame = new Gtk.Frame(" Instructions: ");
            instructionsFrame.Add(instructionsLabel);
            instructionsFrame.LabelXalign = 0.01f;
            instructionsBox.PackEnd(instructionsFrame, true, true, 2);

            graphView.OnGraphObjectSelected += OnGraphObjectSelected;
            graphView.OnGraphObjectMoved += OnGraphObjectMoved;

            contextMenuHelper = new ContextMenuHelper(graphView.MainWidget);
            contextMenuHelper.ContextMenu += OnPopup;

            ContextMenu.SelectionDone += OnContextMenuDeactivated;
            ContextMenu.Mapped += OnContextMenuRendered;

            // Ensure the menu is populated
            Select(0);
    }

        /// <summary>
        /// Nodes in the directed graph. To change them, use <see cref="SetGraph(List{Node}, List{Arc})" />.
        /// </summary>
        public List<Node> Nodes
        {
            get
            {
                return graphView.DirectedGraph.Nodes;
            }
        }

        /// <summary>
        /// Arcs in the directed graph. To change them, use <see cref="SetGraph(List{Node}, List{Arc})" />.
        /// </summary>
        /// <value></value>
        public List<Arc> Arcs
        {
            get
            {
                return graphView.DirectedGraph.Arcs;
            }
        }

        /// <summary>
        /// Set the graph in the view.
        /// </summary>
        /// <param name="nodes">Nodes of the graph.</param>
        /// <param name="arcs">Arcs of the graph.</param>
        public void SetGraph(List<Node> nodes, List<Arc> arcs)
        {
            rules.Clear();
            actions.Clear();

            graphView.DirectedGraph.Nodes = nodes;
            graphView.DirectedGraph.Arcs = arcs;
            foreach (Arc a in arcs)
            {
                a.Source = graphView.DirectedGraph.GetNodeByID(a.SourceID);
                a.Destination = graphView.DirectedGraph.GetNodeByID(a.DestinationID);
            }

            graphView.MainWidget.QueueDraw();
        }

        /// <summary>
        /// A graph object has been selected. Make the (middle part of) UI relevant to it
        /// </summary>
        /// <param name="objectID">ID of the object to be selected.</param>
        public void Select(int objectID)
        {
            if (isDrawingArc)
            {
                isDrawingArc = false;
                mainWidget.Window.Cursor = new Cursor(CursorType.Arrow);
                return;
            }

            if (objectID == 0)
            {
                return;
            }

            for (int i = 0; i < graphView.DirectedGraph.Nodes.Count; i++)
            {
                Node node = graphView.DirectedGraph.Nodes[i];
                if (node.ID == objectID)
                {
                    //The node property is held by the presenter, which also listens for this event and
                    //will update the properties being displayed.
                    objectFrame.Label = $" {node.Name} Properties ";
                    objectBox.Show();
                    return;
                }
            }

            for (int i = 0; i < graphView.DirectedGraph.Arcs.Count; i++)
            {
                Arc arc = graphView.DirectedGraph.Arcs[i];
                if (arc.ID == objectID)
                {
                    //The node property is held by the presenter, which also listens for this event and
                    //will update the properties being displayed.
                    objectFrame.Label = $" {arc.SourceID} to {arc.DestinationID} Properties ";
                    objectBox.Show();
                    return;
                }
            }
        }

        /// <summary>
        /// Selected graph object will be an arc, node, or null. Make sure the menu is appropriate
        /// </summary>
        private void PopulateMenus()
        {
            ContextMenu.Foreach(mi => ContextMenu.Remove(mi));
            MenuItem item;
            EventHandler handler;
            if (graphView.SelectedObjects == null || graphView.SelectedObjects.Count == 0)
            {
                // User has right-clicked in empty space.
                item = new MenuItem("Add Node");
                handler = OnAddNode;
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            else if (graphView.SelectedObjects.Count == 1)
            {
                GraphObject selectedObj = graphView.SelectedObjects[0];
                if (selectedObj is Node)
                {
                    // User has right-clicked on a node.
                    item = new MenuItem($"Add Arc");
                    handler = OnAddArcStart;
                    item.Activated += handler;
                    ContextMenu.Append(item);

                    item = new MenuItem($"Duplicate {selectedObj.Name}");
                    handler = OnDuplicateNode;
                    item.Activated += handler;
                    ContextMenu.Append(item);

                    item = new MenuItem($"Delete {selectedObj.Name}");
                    handler = OnDeleteNode;
                    item.Activated += handler;
                    ContextMenu.Append(item);
                }
                else if (selectedObj is Arc arc)
                {
                    item = new MenuItem($"Delete Arc from {arc.Source.Name} to {arc.Destination.Name}");
                    handler = OnDeleteArc;
                    item.Activated += handler;
                    ContextMenu.Append(item);
                }
            } 
            else
            {
                item = new MenuItem($"Delete Selected");
                handler = OnDeleteNode;
                item.Activated += handler;
                ContextMenu.Append(item);
            }

            // Make the context items visible. This won't actually
            // have an effect until the context menu is realized.
            ContextMenu.ShowAll();
        }

        /// <summary>
        /// Called when the main widget is destroyed.
        /// Need to detach all event handlers to/from native objects
        /// to allow them to be correctly disposed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnDestroyed(object sender, EventArgs args)
        {
            try
            {
                (PropertiesView as ViewBase).Dispose();
                mainWidget.Destroyed -= OnDestroyed;

                graphView.OnGraphObjectSelected -= OnGraphObjectSelected;
                graphView.OnGraphObjectMoved -= OnGraphObjectMoved;

                contextMenuHelper.ContextMenu -= OnPopup;

                ContextMenu.SelectionDone -= OnContextMenuDeactivated;
                ContextMenu.Mapped -= OnContextMenuRendered;
                ContextMenu.Clear();
                ContextMenu.Dispose();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the context menu has been mapped (ie when it becomes
        /// visible on the screen).
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnContextMenuRendered(object sender, EventArgs args)
        {
            try
            {
                PopulateMenus();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called after the user has selected something in the context menu.
        /// Unselects the currently selected node or arc.
        /// This is useful when a new node is created from the menu
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnContextMenuDeactivated(object sender, EventArgs args)
        {
            if (!isDrawingArc)
                graphView.UnSelect();
        }

        /// <summary>
        /// Callback for the context menu helper.
        /// Called when the user has right clicked on something which has
        /// context items associated with it.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnPopup(object sender, ContextMenuEventArgs args)
        {
            try
            {
                //pass right click over to the graph to update selection
                graphView.SelectedObjects.Clear();
                graphView.ProcessClick(new System.Drawing.Point((int)args.X, (int)args.Y));
                
                PopulateMenus();
                if (ContextMenu.Children.Length > 0)
                    ContextMenu.Popup();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The user has moved an object (node or arc) in the directed graph.
        /// </summary>
        /// <remarks>This is called from the directed graph code (not directly by gtk).</remarks>
        /// <param name="sender">Sending object.</param>
        /// <param name="args">Event data.</param>
        private void OnGraphObjectMoved(object sender, GraphObjectsArgs args)
        {
            try
            {
                GraphChanged?.Invoke(this, new GraphChangedEventArgs(Arcs, Nodes));
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The user has selected something, so change the UI to present the context for that selection
        /// </summary>
        /// <remarks>This is called from the directed graph code.</remarks>
        /// <param name="o">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnGraphObjectSelected(object o, GraphObjectsArgs args)
        {
            if (args.Objects != null)
            {
                this.GraphObjectSelected.Invoke(o, args);
                if (args.Objects.Count == 1)
                    Select(args.Objects[0].ID);
                else
                    Select(0);
            }
            else
                Select(0);
        }

        /// <summary>
        /// Callback for the 'add node' context menu option.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnAddNode(object sender, EventArgs args)
        {
            try
            {
                Node newNode = new Node();
                //randomize colour of node
                System.Drawing.Color[] colours = ColourUtilities.Colours;
                Random rand = new Random();
                int randomIndex = rand.Next(colours.Length-1) + 1; //remove black as an option
                newNode.Colour = colours[randomIndex];

                newNode.ID = graphView.DirectedGraph.NextID();
                newNode.Name = $"Node {newNode.ID}";

                newNode.Location = GtkUtilities.GetPositionOfWidgetRelativeToAnotherWidget(sender as Widget, graphView.MainWidget);
                AddNode?.Invoke(this, new AddNodeEventArgs(newNode));
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Callback for the 'delete node' context menu option.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnDeleteNode(object sender, EventArgs args)
        {
            try
            {
                DelNode?.Invoke(this, new GraphObjectsArgs(graphView.SelectedObjects));
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Callback for the 'add arc' context menu option.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnAddArcStart(object sender, EventArgs args)
        {
            if (graphView.SelectedObjects.Count == 1)
            {
                mainWidget.Window.Cursor = new Cursor(CursorType.DiamondCross);
                isDrawingArc = true;

                graphView.isDrawingArc = true;

                Node cursorNode = new Node();
                cursorNode.ID = 0;
                cursorNode.Name = "_cursor_";
                cursorNode.Location = graphView.lastPos;

                //We make a dummy node here so that it doesn't actually link to a real node until it's created
                Node startNode = new Node();
                startNode.ID = graphView.SelectedObjects[0].ID;
                startNode.Name = graphView.SelectedObjects[0].Name;
                startNode.Location = graphView.SelectedObjects[0].Location;

                Arc arc = new Arc();
                arc.ID = 0;
                arc.Name = "Arc";
                arc.SourceID = graphView.SelectedObjects[0].ID;
                arc.DestinationID = 0;
                arc.Location = startNode.Location;
                arc.Source = startNode;
                arc.Destination = cursorNode;
                arc.Conditions = new List<string>();
                arc.Actions = new List<string>();
                arc.BezierPoints = new List<System.Drawing.Point>();

                graphView.tempArc = new Arc(arc);
            }
        }

        /// <summary>
        /// Callback for when an arc is added to the graph
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnAddArcEnd(object sender, EventArgs args)
        {
            if (graphView.SelectedObjects.Count == 1)
            {
                Arc newArc = new Arc();
                newArc.ID = graphView.DirectedGraph.NextID();
                newArc.Name = $"Arc {newArc.ID}";
                newArc.SourceID = graphView.tempArc.SourceID;
                newArc.DestinationID = graphView.SelectedObjects[0].ID;
                newArc.Location = graphView.tempArc.Location;
                newArc.Source = graphView.tempArc.Source;
                newArc.Destination = graphView.tempArc.Destination;
                newArc.Conditions = new List<string>();
                newArc.Actions = new List<string>();
                newArc.BezierPoints = new List<System.Drawing.Point>();

                AddArcEnd?.Invoke(this, new AddArcEventArgs { Arc = new Arc(newArc) });
            }
        }

        /// <summary>
        /// Callback for the 'delete arc' context menu option.
        /// </summary>
        /// <param name="sender">Sending object.</param>
        /// <param name="args">Event data.</param>
        private void OnDeleteArc(object sender, EventArgs args)
        {
            DelArc?.Invoke(this, new GraphObjectsArgs(graphView.SelectedObjects));
        }

        /// <summary>
        /// Callback for the 'duplicate node' context menu option.
        /// </summary>
        /// <param name="sender">Sending object.</param>
        /// <param name="args">Event data.</param>
        private void OnDuplicateNode(object sender, EventArgs args)
        {
            try
            {
                Node node = graphView.SelectedObjects[0] as Node;
                List<int> restrictedIDs = new List<int>();

                Node newNode = new Node(node);
                //randomize colour of node
                System.Drawing.Color[] colours = ColourUtilities.Colours;
                Random rand = new Random();
                int randomIndex = rand.Next(colours.Length - 1) + 1; //remove black as an option
                newNode.Colour = colours[randomIndex];

                newNode.ID = graphView.DirectedGraph.NextID();
                restrictedIDs.Add(newNode.ID);
                newNode.Name += " 2";

                newNode.Location = GtkUtilities.GetPositionOfWidgetRelativeToAnotherWidget(sender as Widget, graphView.MainWidget);
                AddNode?.Invoke(this, new AddNodeEventArgs(newNode));

                List<Arc> newArcs = new List<Arc>();
                foreach (Arc a in Arcs)
                {
                    if (a.SourceID == node.ID || a.DestinationID == node.ID)
                    {
                        Arc newArc = new Arc(a);
                        newArc.ID = graphView.DirectedGraph.NextID(restrictedIDs);
                        newArc.BezierPoints = new List<System.Drawing.Point>();
                        restrictedIDs.Add(newArc.ID);

                        if (a.SourceID == node.ID)
                            newArc.SourceID = newNode.ID;
                        if (a.DestinationID == node.ID)
                            newArc.DestinationID = newNode.ID;

                        //give the new arcs default names if the original arc had a default name
                        if (a.Name.CompareTo($"Arc {a.ID}") == 0)
                            newArc.Name = $"Arc {newArc.ID}";

                        newArcs.Add(newArc);
                    }
                }

                foreach (Arc a in newArcs)
                    AddArcEnd?.Invoke(this, new AddArcEventArgs { Arc = a });
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
