namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;
    using EventArguments;
    using EventArguments.DirectedGraph;
    using ApsimNG.Classes.DirectedGraph;
    using Gtk;
    using Models.Management;
    using Models;
    using Extensions;
    using Utility;

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
        public event EventHandler<GraphChangedEventArgs> OnGraphChanged;

        /// <summary>Invoked when the user adds a node</summary>
        public event EventHandler<AddNodeEventArgs> AddNode;

        /// <summary>Invoked when the user deletes a node</summary>
        public event EventHandler<DelNodeEventArgs> DelNode;

        /// <summary>Invoked when the user adds an arc</summary>
        public event EventHandler<AddArcEventArgs> AddArc;

        /// <summary>Invoked when the user deletes an arc</summary>
        public event EventHandler<DelArcEventArgs> DelArc;

        private Paned vpaned1 = null;
        private ListStore comboModel = new ListStore(typeof(string));
        private CellRendererText comboRender = new CellRendererText();

        private DirectedGraphView graphView;
        private ContextMenuHelper contextMenuHelper;
        private Frame ctxFrame;
        private Widget arcSelWdgt = null;
        public IEditorView RuleList { get; private set; } = null;
        public IEditorView ActionList { get; private set; } = null;
        private Widget nodeSelWdgt = null;
        private Entry nameEntry = null;
        private Entry descEntry = null;
        private ColorButton colourChooser = null;
        private Widget infoWdgt = null;
        private HPaned hpaned1;
        private HPaned hpaned2;

        private Box ctxBox = null;
        private Menu ContextMenu = new Menu();

        private Dictionary<string, List<string>> rules = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> actions = new Dictionary<string, List<string>>();
        private Dictionary<string, string> nodeDescriptions = new Dictionary<string, string>();

        /// <summary>
        /// Properties editor.
        /// </summary>
        public IPropertyView PropertiesView { get; private set; }

        public BubbleChartView(ViewBase owner = null) : base(owner)
        {
            vpaned1 = new VPaned();
            mainWidget = vpaned1;
            mainWidget.Destroyed += OnDestroyed;

            graphView = new DirectedGraphView(this);
            vpaned1.Pack1(graphView.MainWidget, true, true );

            VBox vbox1 = new VBox(false, 0);
            ctxBox = new VBox(false, 0);

            // Arc selection: rules & actions
            VBox arcSelBox = new VBox();

            Label l1 = new Label("Rules");
            arcSelBox.PackStart(l1, true, true, 0); l1.Show();
            RuleList = new EditorView(owner);
            RuleList.TextHasChangedByUser += OnRuleChanged;
            //RuleList.ScriptMode = false;

            ScrolledWindow rules = new ScrolledWindow();
            rules.ShadowType = ShadowType.EtchedIn;
            rules.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
#if NETFRAMEWORK
            rules.AddWithViewport((RuleList as ViewBase).MainWidget);
#else
            rules.Add((RuleList as ViewBase).MainWidget);
#endif
            (RuleList as ViewBase).MainWidget.ShowAll();
            arcSelBox.PackStart(rules, true, true, 0); rules.Show();

            Label l2 = new Label("Actions");
            arcSelBox.PackStart(l2, true, true, 0); l2.Show();
            ActionList = new EditorView(owner);
            ActionList.TextHasChangedByUser += OnActionChanged;
            //ActionList.ScriptMode = false;

            ScrolledWindow actions = new ScrolledWindow();
            actions.ShadowType = ShadowType.EtchedIn;
            actions.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
#if NETFRAMEWORK
            actions.AddWithViewport((ActionList as ViewBase).MainWidget);
#else
            actions.Add((ActionList as ViewBase).MainWidget);
#endif
            (ActionList as ViewBase).MainWidget.ShowAll();
            arcSelBox.PackStart(actions, true, true, 0); actions.Show();
            arcSelWdgt = arcSelBox as Widget;
            arcSelWdgt.Hide();
            ctxBox.PackStart(arcSelWdgt, true, true, 0);

            // Node selection: 
#if NETFRAMEWORK
            Table t1 = new Table(3, 2, false);
#else
            Grid t1 = new Grid();
#endif
            Label l3 = new Label("Name");
            l3.Xalign = 0;
            t1.Attach(l3, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            Label l4 = new Label("Description");
            l4.Xalign = 0;
            t1.Attach(l4, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            Label l5 = new Label("Colour");
            l5.Xalign = 0;
            t1.Attach(l5, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            nameEntry = new Entry();
            nameEntry.Changed += OnNameChanged;
            nameEntry.Xalign = 0;
            // Setting the WidthRequest to 350 will effectively
            // set the minimum size, beyond which it cannot be further
            // shrunk by dragging the HPaned's splitter.
            nameEntry.WidthRequest = 350;
            t1.Attach(nameEntry, 1, 2, 0, 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            descEntry = new Entry();
            descEntry.Xalign = 0;
            descEntry.Changed += OnDescriptionChanged;
            descEntry.WidthRequest = 350;
            t1.Attach(descEntry, 1, 2, 1, 2, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            colourChooser = new ColorButton();
            colourChooser.Xalign = 0;
            colourChooser.ColorSet += OnColourChanged;
            colourChooser.WidthRequest = 350;
            t1.Attach(colourChooser, 1, 2, 2, 3, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            nodeSelWdgt = t1;
            ctxBox.PackStart(t1, true, true, 0);

            // Info
            Label l6 = new Label();
            l6.LineWrap = true;
            l6.Text = "<left-click>: select a node or arc.\n" +
            "<right-click>: shows a context-sensitive menu.\n" +
            "\n" +
            "Once a node/arc is selected, it can be dragged to a new position.\n" +
            "\n" +
            "Nodes are created by right-clicking on a blank area.\n" +
            "\n" +
            "Transition arcs are created by firstly selecting a source node,\n" +
            "then right-clicking over a target node.\n";
            infoWdgt = l6 as Widget;
            infoWdgt.ShowAll();
            Alignment infoWdgtWrapper = new Alignment(0, 0, 0, 0);
            infoWdgtWrapper.Add(infoWdgt);
            //ctxBox.PackStart(infoWdgt, true, true, 0);
            //vbox1.PackStart(ctxBox, false, false, 0);

            PropertiesView = new PropertyView(this);
            // settingsBox = new Table(2, 2, false);
            // settingsBox.Attach(new Label("Initial State"), 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            // combobox1 = new ComboBox();
            // combobox1.PackStart(comboRender, false);
            // combobox1.AddAttribute(comboRender, "text", 0);
            // combobox1.Model = comboModel;
            // settingsBox.Attach(combobox1, 1, 2, 0, 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            // chkVerbose = new CheckButton();
            // chkVerbose.Toggled += OnToggleVerboseMode;
            // settingsBox.Attach(new Label("Verbose Mode"), 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            // settingsBox.Attach(chkVerbose, 1, 2, 1, 2, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            hpaned1 = new HPaned();
            hpaned2 = new HPaned();
            Frame frame1 = new Frame("Rotation Settings");
            frame1.Add(((ViewBase)PropertiesView).MainWidget);
            frame1.ShadowType = ShadowType.In;
            Frame frame2 = new Frame();
            frame2.Add(hpaned2);
            frame2.ShadowType = ShadowType.In;
            ctxFrame = new Frame();
            ctxFrame.Add(ctxBox);
            ctxFrame.ShadowType = ShadowType.In;
            Frame frame4 = new Frame("Instructions");
            frame4.Add(infoWdgtWrapper);
            frame4.ShadowType = ShadowType.In;
            hpaned1.Pack1(frame1, false, false);
            hpaned1.Pack2(frame2, true, false);
            hpaned2.Pack1(ctxFrame, true, false);
            hpaned2.Pack2(frame4, true, false);
            hpaned1.ShowAll();
            Alignment halign = new Alignment(0, 0, 1, 1);
            halign.Add(hpaned1);

            vpaned1.Pack2(halign, false, false);
            vpaned1.Show();

            graphView.OnGraphObjectSelected += OnGraphObjectSelected;
            graphView.OnGraphObjectMoved += OnGraphObjectMoved;
            //combobox1.Changed += OnComboBox1SelectedValueChanged;

            contextMenuHelper = new ContextMenuHelper(graphView.MainWidget);
            contextMenuHelper.ContextMenu += OnPopup;

            ContextMenu.SelectionDone += OnContextMenuDeactivated;
            ContextMenu.Mapped += OnContextMenuRendered;

            // Ensure the menu is populated
            Select(null);
        }

        /// <summary>
        /// Nodes in the directed graph. To change them, use <see cref="SetGraph(List{StateNode}, List{RuleAction})" />.
        /// </summary>
        public List<StateNode> Nodes
        {
            get
            {
                return graphView.DirectedGraph.Nodes.Select(n => new StateNode(n, nodeDescriptions[n.Name])).ToList();
            }
        }

        /// <summary>
        /// Arcs in the directed graph. To change them, use <see cref="SetGraph(List{StateNode}, List{RuleAction})" />.
        /// </summary>
        /// <value></value>
        public List<RuleAction> Arcs
        {
            get
            {
                List<RuleAction> arcs = new List<RuleAction>();
                foreach (var a in graphView.DirectedGraph.Arcs)
                {
                    Arc ga = graphView.DirectedGraph.Arcs.Find(x => x.Name == a.Name);
                    var na = new RuleAction(ga);
                    na.Conditions = rules[na.Name];
                    na.Actions = actions[na.Name];
                    arcs.Add(na);
                }
                return arcs;
            }
        }

        /// <summary>
        /// Set the graph in the view.
        /// </summary>
        /// <param name="nodes">Nodes of the graph.</param>
        /// <param name="arcs">Arcs of the graph.</param>
        public void SetGraph(List<StateNode> nodes, List<RuleAction> arcs)
        {
            rules.Clear();
            actions.Clear();
            nodeDescriptions.Clear();
            comboModel.Clear();
            var graph = new Models.DirectedGraph();

            nodes.ForEach(node =>
            {
                graph.AddNode(node);
                //NodeNames[node.Name] = node.NodeName;
                comboModel.AppendValues (node.Name);
                nodeDescriptions[node.Name] = node.Description;
            });
            arcs.ForEach(arc =>
            {
                rules[arc.Name] = arc.Conditions;
                actions[arc.Name] = arc.Actions;
                graph.AddArc(arc);
            });
            graphView.DirectedGraph = graph;
            graphView.MainWidget.QueueDraw();
        }

        /// <summary>
        /// A graph object has been selected. Make the (middle part of) UI relevant to it
        /// </summary>
        /// <param name="objectName">Name of the object to be selected.</param>
        public void Select(string objectName)
        {
            ctxBox.Foreach(c => ctxBox.Remove(c)); 

            Arc arc = graphView.DirectedGraph.Arcs.Find(a => a.Name == objectName);
            Models.Node node = graphView.DirectedGraph.Nodes.Find(n => n.Name == objectName);
            if (node != null)
            {
                //ctxLabel.Text = "State";
                ctxFrame.Label = $"{node.Name} settings";
                // Need to detach the event handlers before changing the entries.
                // Otherwise a changed event will fire which we don't really want.
                nameEntry.Changed -= OnNameChanged;
                // Setting an entry's text to null doesn't seem to have an effect.
                nameEntry.Text = objectName ?? "";
                nameEntry.Changed += OnNameChanged;
                if (nodeDescriptions.ContainsKey(objectName))
                {
                    descEntry.Changed -= OnDescriptionChanged;
                    descEntry.Text = nodeDescriptions[objectName] ?? "";
                    descEntry.Changed += OnDescriptionChanged;
                }
                colourChooser.ColorSet -= OnColourChanged;
#if NETFRAMEWORK
                colourChooser.Color = Utility.Colour.ToGdk(node.Colour);
#else
                colourChooser.Rgba = node.Colour.ToRGBA();
#endif
                colourChooser.ColorSet += OnColourChanged;

                ctxBox.PackStart(nodeSelWdgt, true, true, 0);
            }
            else if (arc != null)
            {
                ctxFrame.Label = "Transition from " + arc.SourceName + " to " + arc.DestinationName;
                RuleList.Text = String.Join(Environment.NewLine, rules[arc.Name].ToArray()) ;
                ActionList.Text = String.Join(Environment.NewLine, actions[arc.Name].ToArray());
                ctxBox.PackStart(arcSelWdgt, true, true, 0);
            }
            else
            {
                //ctxLabel.Text = "Information";
                //ctxBox.PackStart(infoWdgt, true, true, 0);
                ctxFrame.Label = "";
            }
            ctxBox.ShowAll();
        }

        /// <summary>
        /// Selected graph object will be an arc, node, or null. Make sure the menu is appropriate
        /// </summary>
        private void PopulateMenus()
        {
            ContextMenu.Foreach(mi => ContextMenu.Remove(mi));
            MenuItem item;
            EventHandler handler;
            if (graphView.SelectedObject == null)
            {
                // User has right-clicked in empty space.
                item = new MenuItem("Add Node");
                handler = OnAddNode;
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            else if (graphView.SelectedObject is DGNode)
            {
                // User has right-clicked on a node.
                item = new MenuItem($"Duplicate {graphView.SelectedObject.Name}");
                handler = OnDuplicateNode;
                item.Activated += handler;
                ContextMenu.Append(item);

                string name = graphView.SelectedObject2?.Name ?? graphView.SelectedObject.Name;
                item = new MenuItem($"Add Arc from {graphView.SelectedObject.Name} to {name}");
                handler = OnAddArc;
                item.Activated += handler;
                ContextMenu.Append(item);

                item = new MenuItem($"Delete {graphView.SelectedObject.Name}");
                handler = OnDeleteNode;
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            else if (graphView.SelectedObject is DGArc arc)
            {
                // User has right-clicked on an arc.
                item = new MenuItem($"Duplicate Arc from {arc.Source.Name} to {arc.Target.Name}");
                handler = OnDuplicateArc;
                item.Activated += handler;
                ContextMenu.Append(item);

                item = new MenuItem($"Delete Arc from {arc.Source.Name} to {arc.Target.Name}");
                handler = OnDeleteArc;
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
                mainWidget.Destroyed -= OnDestroyed;
                
                RuleList.TextHasChangedByUser -= OnRuleChanged;
                ActionList.TextHasChangedByUser -= OnActionChanged;

                nameEntry.Changed -= OnNameChanged;
                descEntry.Changed -= OnDescriptionChanged;
                colourChooser.ColorSet -= OnColourChanged;

                graphView.OnGraphObjectSelected -= OnGraphObjectSelected;
                graphView.OnGraphObjectMoved -= OnGraphObjectMoved;

                contextMenuHelper.ContextMenu -= OnPopup;

                ContextMenu.SelectionDone -= OnContextMenuDeactivated;
                ContextMenu.Mapped -= OnContextMenuRendered;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// User has changed a node name.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        public void OnNameChanged(object sender, EventArgs args)
        {
            try
            {
                if (!string.IsNullOrEmpty(nameEntry.Text))
                {
                    // We need to rename the node in the directed graph in order
                    // for the Nodes property to return the correct name for the
                    // changed node. We also need to add a description for the new
                    // name to the dict.
                    nodeDescriptions[nameEntry.Text] = nodeDescriptions[graphView.SelectedObject.Name];
                    graphView.SelectedObject.Name = nameEntry.Text;
                    ctxFrame.Label = $"{nameEntry.Text} settings";
                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs(Arcs, Nodes));
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the user has changed a node's description.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnDescriptionChanged(object sender, EventArgs args)
        {
            try
            {
                if (graphView.SelectedObject != null)
                {
                    nodeDescriptions[graphView.SelectedObject.Name] = descEntry.Text;
                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs(Arcs, Nodes));
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the user has changed a node's colour.
        /// </summary>
        /// <param name="sender">Sending object.</param>
        /// <param name="args">Event data.</param>
        private void OnColourChanged(object sender, EventArgs args)
        {
            try
            {
                if (graphView.SelectedObject != null)
                {
#if NETFRAMEWORK
                    var colour = colourChooser.Color;
#else
                    var colour = colourChooser.Rgba.ToColour().ToGdk();
#endif
                    graphView.SelectedObject.Colour = Utility.Colour.GtkToOxyColor(colour);
                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs(Arcs, Nodes));
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the user has changed a rule.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        public void OnRuleChanged(object sender, EventArgs args)
        {
            try
            {
                if (graphView.SelectedObject != null)
                {
                    rules[graphView.SelectedObject.Name] = RuleList.Text.Split('\n').ToList();
                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs(Arcs, Nodes));
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the user has changed an action.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        public void OnActionChanged(object sender, EventArgs args)
        {
            try
            {
                if (graphView.SelectedObject != null)
                {
                    actions[graphView.SelectedObject.Name] = ActionList.Text.Split('\n').ToList();
                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs(Arcs, Nodes));
                }
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
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnContextMenuDeactivated(object sender, EventArgs args)
        {
            try
            {
                graphView.UnSelect();
                //PopulateMenus();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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
                // Get the point clicked by the mouse.
                // FIXME - need to test if the user right-clicked on an object or the root window
                //Cairo.PointD clickPoint = new PointD(args.X, args.Y);
                // Look through nodes & arcs for the click point
                //DGObject clickedObject = Graph.DirectedGraph.Nodes.FindLast(node => node.HitTest(clickPoint));

                PopulateMenus();
                if (ContextMenu.Children.Length > 0) ContextMenu.Popup();
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
        private void OnGraphObjectMoved(object sender, ObjectMovedArgs args)
        {
            try
            {
                OnGraphChanged?.Invoke(this, new GraphChangedEventArgs(Arcs, Nodes));
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
        private void OnGraphObjectSelected(object o, GraphObjectSelectedArgs args)
        {
            try
            {
                Select(args.Object1?.Name);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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
                // todo: set location to context menu location
                var node = new Models.Node { Name = graphView.DirectedGraph.NextNodeID() };
                StateNode newNode = new StateNode(node);
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
                DelNode?.Invoke(this, new DelNodeEventArgs { nodeNameToDelete = graphView.SelectedObject.Name });
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
        private void OnAddArc(object sender, EventArgs args)
        {
            try
            {
                if (graphView.SelectedObject == null)
                    // This is almost certainly indicative of an internal error, NOT user error.
                    throw new Exception("Unable to add arc - at least one node needs to be selected");

                Arc newArc = new Arc();
                newArc.Name = graphView.DirectedGraph.NextArcID();
                newArc.SourceName = graphView.SelectedObject.Name;
                if (graphView.SelectedObject2 == null)
                    // Loopback arc
                    newArc.DestinationName = graphView.SelectedObject.Name;
                else
                    newArc.DestinationName = graphView.SelectedObject2.Name;

                AddArc?.Invoke(this, new AddArcEventArgs { Arc = new RuleAction(newArc) });
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Callback for the 'delete arc' context menu option.
        /// </summary>
        /// <param name="sender">Sending object.</param>
        /// <param name="args">Event data.</param>
        private void OnDeleteArc(object sender, EventArgs args)
        {
            try
            {
                if (!(graphView.SelectedObject is DGArc))
                    // This is almost certainly indicative of an internal error, NOT user error.
                    throw new Exception("Unable to add arc - no arc is selected");
                DelArc?.Invoke(this, new DelArcEventArgs { arcNameToDelete = graphView.SelectedObject.Name });
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Callback for the 'duplicate node' context menu option.
        /// </summary>
        /// <remarks>
        /// Does this belong in the presenter?
        /// </remarks>
        /// <param name="sender">Sending object.</param>
        /// <param name="args">Event data.</param>
        private void OnDuplicateNode(object sender, EventArgs args)
        {
            try
            {
                if (graphView.SelectedObject is DGNode node)
                {
                    List<StateNode> nodes = Nodes;
                    List<RuleAction> arcs = Arcs;

                    // Create a copy of the existing node.
                    StateNode newNode = new StateNode(node.ToNode());
                    newNode.Location = new System.Drawing.Point(newNode.Location.X + node.Width / 2, newNode.Location.Y);
                    newNode.Name = graphView.DirectedGraph.NextNodeID();
                    if (nodeDescriptions.ContainsKey(node.Name))
                        newNode.Description = nodeDescriptions[node.Name];

                    nodes.Add(newNode);

                    // Copy all arcs moving to/from the existing node.
                    DirectedGraph graph = graphView.DirectedGraph;
                    foreach (var arc in graphView.DirectedGraph.Arcs.FindAll(arc => arc.SourceName == node.Name))
                    {
                        RuleAction newArc = new RuleAction(arc);
                        newArc.Name = graph.NextArcID();
                        newArc.SourceName = newNode.Name;
                        if (rules.ContainsKey(arc.Name))
                            newArc.Conditions = rules[arc.Name];
                        if (actions.ContainsKey(arc.Name))
                            newArc.Actions = actions[arc.Name];
                        arcs.Add(newArc);
                        
                        // Add the arc to the local copy of the directed graph.
                        // Need to do this to ensure that NextArcID() doesn't
                        // generate the same name when we call it multiple times.
                        graph.AddArc(newArc);
                    }
                    foreach (var arc in graphView.DirectedGraph.Arcs.FindAll(arc => arc.DestinationName == graphView.SelectedObject.Name))
                    {
                        RuleAction newArc = new RuleAction(arc);
                        newArc.Name = graph.NextArcID();
                        newArc.DestinationName = newNode.Name;
                        if (rules.ContainsKey(arc.Name))
                            newArc.Conditions = rules[arc.Name];
                        if (actions.ContainsKey(arc.Name))
                            newArc.Actions = actions[arc.Name];
                        arcs.Add(newArc);

                        // Add the arc to the local copy of the directed graph.
                        // Need to do this to ensure that NextArcID() doesn't
                        // generate the same name when we call it multiple times.
                        graph.AddArc(newArc);
                    }

                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs(arcs, nodes));
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Callback for the 'duplicate arc' context menu option.
        /// </summary>
        /// <remarks>
        /// Does this belong in the presenter?
        /// </remarks>
        /// <param name="sender">Sending object.</param>
        /// <param name="args">Event data.</param>
        private void OnDuplicateArc(object sender, EventArgs args)
        {
            try
            {
                if (graphView.SelectedObject is DGArc arc)
                {
                    RuleAction newArc = new RuleAction(arc.ToArc());
                    newArc.Name = graphView.DirectedGraph.NextArcID();
                    newArc.Location = new System.Drawing.Point(newArc.Location.X + 10, newArc.Location.Y);

                    // Copy across rules and actions from selected arc.
                    if (rules.ContainsKey(arc.Name))
                        rules[newArc.Name] = rules[arc.Name];
                    if (actions.ContainsKey(arc.Name))
                        actions[newArc.Name] = actions[arc.Name];
                    
                    List<RuleAction> arcs = Arcs;
                    arcs.Add(newArc);
                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs(arcs, Nodes));
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
