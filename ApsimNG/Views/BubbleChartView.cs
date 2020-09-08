namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;
    using EventArguments;
    using ApsimNG.Classes.DirectedGraph;
    using EventArguments.DirectedGraph;
    using Gtk;
    using Models.Management;
    using Models;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
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

        /// <summary> Invoked when the user changes the initial state. </summary>
        public event EventHandler<InitialStateEventArgs> OnInitialStateChanged;

        private Paned vpaned1 = null;
        private ComboBox combobox1 = null;
        private ListStore comboModel = new ListStore(typeof(string));
        private CellRendererText comboRender = new CellRendererText();

        private Views.DirectedGraphView graphView;
        private ContextMenuHelper contextMenuHelper;
        private Label ctxLabel = null;
        private Widget arcSelWdgt = null;
        public IEditorView RuleList { get; private set; } = null;
        public IEditorView ActionList { get; private set; } = null;
        private Widget nodeSelWdgt = null;
        private Entry nameEntry = null;
        private Entry descEntry = null;
        private ColorButton colourChooser = null;
        private Widget infoWdgt = null;

        private Box ctxBox = null;
        private Menu ContextMenu = new Menu();

        public System.Drawing.Color defaultBackground;
        public System.Drawing.Color defaultOutline;

        private Dictionary<string, List<string>> Rules = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> Actions = new Dictionary<string, List<string>>();
        private Dictionary<string, string> nodeDescriptions = new Dictionary<string, string>();

        public BubbleChartView(ViewBase owner = null) : base(owner)
        {
            vpaned1 = new VPaned();
            mainWidget = vpaned1;
            mainWidget.Destroyed += OnDestroyed;

            graphView = new DirectedGraphView(this);
            vpaned1.Pack1(graphView.MainWidget, true, true );

            VBox vbox1 = new VBox(false, 0);
            ctxBox = new VBox(false, 0);
            ctxLabel = new Label("Information");

            // Arc selection: rules & actions
            VBox arcSelBox = new VBox();

            Label l1 = new Label("Rules");
            arcSelBox.PackStart(l1, false, false, 0); l1.Show();
            RuleList = new EditorView(owner);
            (RuleList as ViewBase).MainWidget.HeightRequest = 75;
            (RuleList as ViewBase).MainWidget.WidthRequest = 350;
            RuleList.TextHasChangedByUser += OnRuleChanged;
            //RuleList.ScriptMode = false;

            ScrolledWindow rules = new ScrolledWindow();
            rules.ShadowType = ShadowType.EtchedIn;
            rules.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            rules.Add((RuleList as ViewBase).MainWidget);
            (RuleList as ViewBase).MainWidget.ShowAll();
            arcSelBox.PackStart(rules, false, false, 0); rules.Show();

            Label l2 = new Label("Actions");
            arcSelBox.PackStart(l2, false, false, 0); l2.Show();
            ActionList = new EditorView(owner);
            (ActionList as ViewBase).MainWidget.HeightRequest = 75;
            (ActionList as ViewBase).MainWidget.WidthRequest = 350;
            ActionList.TextHasChangedByUser += OnActionChanged;
            //ActionList.ScriptMode = false;

            ScrolledWindow actions = new ScrolledWindow();
            actions.ShadowType = ShadowType.EtchedIn;
            actions.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            actions.Add((ActionList as ViewBase).MainWidget);
            (ActionList as ViewBase).MainWidget.ShowAll();
            arcSelBox.PackStart(actions, false, false, 0); actions.Show();
            arcSelWdgt = arcSelBox as Widget;

            // Node selection: 
            Table t1 = new Table(3, 2, true);
            Label l3 = new Label("Name");
            t1.Attach(l3, 0, 1, 0, 1, AttachOptions.Shrink, AttachOptions.Fill, 0, 0);
            Label l4 = new Label("Description");
            t1.Attach(l4, 0, 1, 1, 2, AttachOptions.Shrink, AttachOptions.Fill, 0, 0);
            Label l5 = new Label("Colour");
            t1.Attach(l5, 0, 1, 2, 3, AttachOptions.Shrink, AttachOptions.Fill, 0, 0);

            nameEntry = new Entry();
            nameEntry.WidthRequest = 350;
            nameEntry.Changed += OnNameChanged;
            t1.Attach(nameEntry, 1, 2, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            descEntry = new Entry();
            descEntry.WidthRequest = 350;
            descEntry.Changed += OnDescriptionChanged;
            t1.Attach(descEntry, 1, 2, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            colourChooser = new ColorButton();
            colourChooser.ColorSet += OnColourChanged;
            t1.Attach(colourChooser, 1, 2, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            //t1.HeightRequest = 75;
            t1.ShowAll();
            nodeSelWdgt = t1;

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

            vbox1.PackStart(ctxBox, false, false, 0);

            vbox1.PackStart(new Label("Initial State"), false, false, 0);
            combobox1 = new ComboBox();
            combobox1.PackStart(comboRender, false);
            combobox1.AddAttribute(comboRender, "text", 0);
            combobox1.Model = comboModel;
            vbox1.PackStart(combobox1, false, false, 0);
            Alignment halign = new Alignment(0, 0, 0, 1);
            halign.Add(vbox1);

            vpaned1.Pack2(halign, false, false);
            vpaned1.Show();

            graphView.OnGraphObjectSelected += OnGraphObjectSelected;
            graphView.OnGraphObjectMoved += OnGraphObjectMoved;
            combobox1.Changed += OnComboBox1SelectedValueChanged;

            // Ensure the menu is populated
            OnGraphObjectSelected(null, new GraphObjectSelectedArgs(null));

            defaultOutline = Utility.Colour.FromGtk(owner.MainWidget.Style.Foreground(StateType.Normal));
            defaultBackground = Utility.Colour.FromGtk(owner.MainWidget.Style.Background(StateType.Normal));

            contextMenuHelper = new ContextMenuHelper(graphView.MainWidget);
            contextMenuHelper.ContextMenu += OnPopup;

            ContextMenu.SelectionDone += OnContextMenuDeactivated;
            ContextMenu.Mapped += OnContextMenuRendered;

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
                    na.testCondition = Rules[na.Name];
                    na.action = Actions[na.Name];
                    arcs.Add(na);
                }
                return arcs;
            }
        }

        /// <summary>
        /// The initial state of the simulation is in a combobox. Allow simple get/set access.
        /// </summary>
        public string InitialState
        {
            get
            {
                if (combobox1.GetActiveIter(out TreeIter iter))
                    return (string)combobox1.Model.GetValue(iter, 0);
                return null;
            }
            set
            {
                if (combobox1.Model.GetIterFirst(out TreeIter iter))
                do
                {
                    GLib.Value thisRow = new GLib.Value();
                    combobox1.Model.GetValue(iter, 0, ref thisRow);
                    if ((thisRow.Val as string).Equals(value))
                    {
                        combobox1.Changed -= OnComboBox1SelectedValueChanged;
                        combobox1.SetActiveIter(iter);
                        combobox1.Changed += OnComboBox1SelectedValueChanged;
                        break;
                    }
                } while (combobox1.Model.IterNext(ref iter));
            }
        }

        /// <summary>
        /// Set the graph in the view.
        /// </summary>
        /// <param name="nodes">Nodes of the graph.</param>
        /// <param name="arcs">Arcs of the graph.</param>
        public void SetGraph(List<StateNode> nodes, List<RuleAction> arcs)
        {
            Rules.Clear();
            Actions.Clear();
            nodeDescriptions.Clear();
            string lastSelected = InitialState; 
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
                Rules[arc.Name] = arc.testCondition;
                Actions[arc.Name] = arc.action;
                graph.AddArc(arc);
            });
            graphView.DirectedGraph = graph;
            InitialState = lastSelected;
            graphView.MainWidget.QueueDraw();
        }

        /// <summary>
        /// A graph object has been selected. Make the (middle part of) UI relevant to it
        /// </summary>
        /// <param name="objectName">Name of the object to be selected.</param>
        public void Select(string objectName)
        {
            ctxBox.Foreach(c => c.Hide()); 
            ctxLabel.Show();
            ctxBox.PackStart(ctxLabel, false, false, 0);

            Arc arc = graphView.DirectedGraph.Arcs.Find(a => a.Name == objectName);
            Node node = graphView.DirectedGraph.Nodes.Find(n => n.Name == objectName);
            if (node != null)
            {
                ctxLabel.Text = "State";
                nameEntry.Changed -= OnNameChanged;
                nameEntry.Text = objectName ?? "";
                nameEntry.Changed += OnNameChanged;
                if (nodeDescriptions.ContainsKey(objectName))
                {
                    descEntry.Changed -= OnDescriptionChanged;
                    // Setting an entry's text to null doesn't seem to have an effect.
                    descEntry.Text = nodeDescriptions[objectName] ?? "";
                    descEntry.Changed += OnDescriptionChanged;
                }
                colourChooser.ColorSet -= OnColourChanged;
                colourChooser.Color = Utility.Colour.ToGdk(node.Colour);
                colourChooser.ColorSet += OnColourChanged;

                nodeSelWdgt.ShowAll();
                ctxBox.PackStart(nodeSelWdgt, false, false, 0);
            }
            else if (arc != null)
            {
                ctxLabel.Text = "Transition from " + arc.SourceName + " to " + arc.DestinationName;
                RuleList.Text = String.Join(Environment.NewLine, Rules[arc.Name].ToArray()) ;
                ActionList.Text = String.Join(Environment.NewLine, Actions[arc.Name].ToArray());
                arcSelWdgt.ShowAll();
                ctxBox.PackStart(arcSelWdgt, false, false, 0);
            }
            else
            {
                ctxLabel.Text = "Information";
                infoWdgt.ShowAll();
                ctxBox.PackStart(infoWdgt, false, false, 0);
            }
            ctxBox.Show();
        }

        /// <summary>
        /// Selected graph object will be an arc, node, or null. Make sure the menu is appropriate
        /// </summary>
        /// <remarks>
        /// fixme - this could be simplified.
        /// </remarks>
        private void PopulateMenus()
        {
            ContextMenu.Foreach(mi => ContextMenu.Remove(mi));
            MenuItem item;
            EventHandler handler;
            if (graphView.SelectedObject == null )
            {
                item = new MenuItem("Add Node");
                handler = delegate (object s, EventArgs x)
                {
                    Node n = new Node { Name = graphView.DirectedGraph.nextNodeID() }; // blecchh
                    StateNode newNode = new StateNode(n) /*{ NodeName = n.Name }*/;/* fixme set location to x,y of menu posting location, use IDs not names  */
                    AddNode?.Invoke(this, new AddNodeEventArgs(newNode));
                };
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            if (graphView.SelectedObject is DGNode && graphView.SelectedObject2 == null)
            {
                item = new MenuItem("Duplicate " + graphView.SelectedObject.Name);
                handler = delegate (object s, EventArgs x)
                {
                    string newName = "Copy of " + graphView.SelectedObject.Name;
                    Node n = new Node { Name = graphView.DirectedGraph.nextNodeID() }; // blecchh
                    /* fixme set location nearby to old node, use IDs not names */
                    AddNode?.Invoke(this, new AddNodeEventArgs(new StateNode(n)));
                    foreach (var arc in graphView.DirectedGraph.Arcs.FindAll(arc => arc.SourceName == graphView.SelectedObject.Name))
                    {
                            Arc newArc = new Arc(arc);
                            newArc.Name = graphView.DirectedGraph.nextArcID();
                            newArc.SourceName = newName;
                            newArc.DestinationName = arc.DestinationName;
                            AddArc?.Invoke(this, new AddArcEventArgs { Arc = new RuleAction( newArc )});
                    }
                    foreach (var arc in graphView.DirectedGraph.Arcs.FindAll(arc => arc.DestinationName == graphView.SelectedObject.Name))
                    {
                            Arc newArc = new Arc(arc);
                            newArc.Name = graphView.DirectedGraph.nextArcID();
                            newArc.SourceName = arc.SourceName;
                            newArc.DestinationName = newName;
                            AddArc?.Invoke(this, new AddArcEventArgs { Arc = new RuleAction(newArc )});
                    }
                };
                item.Activated += handler;
                ContextMenu.Append(item);

                item = new MenuItem("Add Arc from " + graphView.SelectedObject.Name + " to " + graphView.SelectedObject.Name);
                handler = delegate (object s, EventArgs x)
                {
                        Arc newArc = new Arc();
                        newArc.Name = graphView.DirectedGraph.nextArcID();
                        newArc.SourceName = graphView.SelectedObject.Name;
                        newArc.DestinationName = graphView.SelectedObject.Name;
                        AddArc?.Invoke(this, new AddArcEventArgs { Arc = new RuleAction(newArc) });
                };
                item.Activated += handler;
                ContextMenu.Append(item);

                item = new MenuItem("Delete " + graphView.SelectedObject.Name);
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("delete node selected");
                    DelNode?.Invoke(this, new DelNodeEventArgs { nodeNameToDelete = graphView.SelectedObject.Name });
                };
                item.Activated += handler;
                ContextMenu.Append(item);

            }
            if (graphView.SelectedObject is DGNode && graphView.SelectedObject2 is DGNode && graphView.SelectedObject != graphView.SelectedObject2)
            {
                item = new MenuItem("Add Arc from " + graphView.SelectedObject.Name + " to " + graphView.SelectedObject2.Name);
                handler = delegate (object s, EventArgs x)
                {
                        Arc newArc = new Arc();
                        newArc.Name = graphView.DirectedGraph.nextArcID();
                        newArc.SourceName = graphView.SelectedObject.Name;
                        newArc.DestinationName = graphView.SelectedObject2.Name;
                        AddArc?.Invoke(this, new AddArcEventArgs { Arc = new RuleAction(newArc) });
                };
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            if (graphView.SelectedObject?.GetType() == typeof(DGArc) )
            {
                item = new MenuItem("Duplicate Arc from " + (graphView.SelectedObject as DGArc).Source.Name + " to " + (graphView.SelectedObject as DGArc).Target.Name);
                handler = delegate (object s, EventArgs x)
                {
                        Arc newArc = new Arc((graphView.SelectedObject as DGArc).ToArc());
                        newArc.Name = graphView.DirectedGraph.nextArcID();
                        newArc.SourceName = (graphView.SelectedObject as DGArc).Source.Name;
                        newArc.DestinationName = (graphView.SelectedObject as DGArc).Target.Name;
                        AddArc?.Invoke(this, new AddArcEventArgs { Arc = new RuleAction(newArc) });
                    /// fixme - copy across rules & actions from selected arc
                };
                item.Activated += handler;
                ContextMenu.Append(item);

                item = new MenuItem("Delete Arc from " + (graphView.SelectedObject as DGArc).Source.Name + " to " + (graphView.SelectedObject as DGArc).Target.Name);
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("del arc selected");
                    DelArc?.Invoke(this, new DelArcEventArgs { arcNameToDelete = (graphView.SelectedObject as DGArc).Name });
                };
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            ContextMenu.ShowAll();  // This packs the menu objects, but doesn't post it.
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
                combobox1.Changed -= OnComboBox1SelectedValueChanged;

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
                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs { Nodes = Nodes, Arcs = Arcs });
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
                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs { Arcs = Arcs, Nodes = Nodes});
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
                    graphView.SelectedObject.Colour = Utility.Colour.GtkToOxyColor(colourChooser.Color);
                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs { Arcs = Arcs, Nodes = Nodes});
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
                    Rules[graphView.SelectedObject.Name] = RuleList.Text.Split('\n').ToList();
                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs { Arcs = Arcs, Nodes = Nodes });
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
                    Actions[graphView.SelectedObject.Name] = ActionList.Text.Split('\n').ToList();
                    OnGraphChanged?.Invoke(this, new GraphChangedEventArgs { Arcs = Arcs, Nodes = Nodes });
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
            OnGraphChanged?.Invoke(this, new GraphChangedEventArgs(){ Arcs = Arcs, Nodes = Nodes });
        }

        /// <summary>
        /// The user has selected something, so change the UI to present the context for that selection
        /// </summary>
        /// <remarks>This is called from the directed graph code.</remarks>
        /// <param name="o">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnGraphObjectSelected(object o, GraphObjectSelectedArgs args)
        {
            Select(args.Object1?.Name);
        }

        /// <summary>
        /// The selected item in the combo box has changed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnComboBox1SelectedValueChanged(object sender, EventArgs args)
        {
            try
            {
                if (combobox1.GetActiveIter(out TreeIter iter))
                {
                    string selectedText = (string)combobox1.Model.GetValue(iter, 0);
                    OnInitialStateChanged?.Invoke(sender, new InitialStateEventArgs() { initialState = selectedText } );
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
