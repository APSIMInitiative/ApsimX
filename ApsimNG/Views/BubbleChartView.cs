// -----------------------------------------------------------------------
// <copyright file="RotBubbleChartView.cs" company="UQ">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
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
    /// 
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

        private Label ctxLabel = null;
        private Widget arcSelWdgt = null;
        public IEditorView RuleList { get; private set; } = null;
        public IEditorView ActionList { get; private set; } = null;
        private Widget nodeSelWdgt = null;
        private Entry nameEntry = null;
        private Entry descEntry = null;
        private Entry colEntry = null;
        private Widget infoWdgt = null;

        private Box ctxBox = null;
        private Menu ContextMenu = new Menu();

        public System.Drawing.Color defaultBackground;
        public System.Drawing.Color defaultOutline;

        private Dictionary<string, List<string>> Rules = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> Actions = new Dictionary<string, List<string>>();
        private Dictionary<string, string> NodeNames = new Dictionary<string, string>();

        public BubbleChartView(ViewBase owner = null) : base(owner)
        {
            vpaned1 = new VPaned();
            mainWidget = vpaned1;

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
            t1.Attach(l3, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0); l3.Show();
            Label l4 = new Label("Description");
            t1.Attach(l4, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0); l4.Show();
            Label l5 = new Label("Colour");
            t1.Attach(l5, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0); l5.Show();
            nameEntry = new Entry();
            nameEntry.WidthRequest = 350;
            nameEntry.Changed += OnNameChanged;
            t1.Attach(nameEntry, 1, 2, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0); nameEntry.Show();
            descEntry = new Entry();
            descEntry.WidthRequest = 350;
            //descEntry.Changed += OnDescChanged;
            t1.Attach(descEntry, 1, 2, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0); descEntry.Show();
            colEntry = new Entry();
            t1.Attach(colEntry, 1, 2, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0); colEntry.Show();
            t1.HeightRequest = 75;

            nodeSelWdgt = t1 as Widget;

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

            ContextMenuHelper contextMenuHelper = new ContextMenuHelper(graphView.MainWidget);
            contextMenuHelper.ContextMenu += OnPopup;

            ContextMenu.SelectionDone += ContextMenu_Deactivated;
            (ContextMenu as MenuShell).Mapped+= RotBubbleChartView_ActivateCurrent;

            OnSelect(selectMode.info, null);
        }
#if false
        // I don't think this is being used...
        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            graph.OnGraphObjectSelected -= OnGraphObjectSelected;
            combobox1.Changed -= OnComboBox1SelectedValueChanged;
            graph.MainWidget.Destroy();
            graph = null;
            mainWidget.Destroyed -= _mainWidget_Destroyed;
            owner = null;
        }
#endif
        // Context sensitive controls for editing arcs / node on the graph
        public enum selectMode { arc, node, info };
        private string selectedObjectName = "";

        // User has changed a node property
        public void OnNameChanged(object o, EventArgs args)
        {
            // Console.WriteLine("OnNmaeChanged: s=" + selectedObjectName + ", t=" + (o as Entry).Text);
            if (selectedObjectName != "")
                NodeNames[selectedObjectName] = (o as Entry).Text;

            OnGraphChanged?.Invoke(this, new GraphChangedEventArgs { Nodes = Nodes, Arcs = Arcs });
        }

        // User has changed a arc property
        public void OnRuleChanged(object o, EventArgs args)
        {
            if (selectedObjectName != "")
                Rules[selectedObjectName] = RuleList.Text.Split('\n').ToList();
            OnGraphChanged?.Invoke(this, new GraphChangedEventArgs { Arcs = Arcs, Nodes = Nodes });
        }

        // User has changed a arc property
        public void OnActionChanged(object o, EventArgs args)
        {
            if (selectedObjectName != "")
                Actions[selectedObjectName] = ActionList.Text.Split('\n').ToList();
            OnGraphChanged?.Invoke(this, new GraphChangedEventArgs { Arcs = Arcs, Nodes = Nodes });
        }

        /// <summary>
        /// A graph object has been selected. Make the (middle part of) UI relevant to it
        /// </summary>
        /// <param name="mode">Selection mode</param>
        /// <param name="ObjectName">Object name</param>
        public void OnSelect(selectMode mode, string ObjectName)
        {
            ctxBox.Foreach(c => c.Hide()); 
            ctxLabel.Show();
            ctxBox.PackStart(ctxLabel, false, false, 0);
            selectedObjectName = ObjectName;
            switch (mode)
            {
                case selectMode.arc:
                    {
                        graphView.DirectedGraph.Arcs.ForEach(arc => {
                            if (arc.Name == ObjectName)
                            {
                                ctxLabel.Text = "Transition from " + arc.SourceName + " to " + arc.DestinationName;
                                RuleList.Text = String.Join("\n", Rules[arc.Name].ToArray()) ;
                                ActionList.Text = String.Join("\n", Actions[arc.Name].ToArray());
                            }
                        });

                        arcSelWdgt.Show();
                        ctxBox.PackStart(arcSelWdgt, false, false, 0);
                        break;
                    }
                case selectMode.node:
                    {
                        ctxLabel.Text = "State";
                        if (ObjectName != "")
                            nameEntry.Text = NodeNames[ObjectName];
                        else
                            nameEntry.Text = "";
                        //descEntry.Text = selectedObject.???;

                        nodeSelWdgt.Show();
                        ctxBox.PackStart(nodeSelWdgt, false, false, 0);
                        break;
                    }
                case selectMode.info:
                    {
                        ctxLabel.Text = "Information";
                        infoWdgt.Show();
                        ctxBox.PackStart(infoWdgt, false, false, 0);
                        break;
                    }
                default:
                    throw new Exception($"Unknown selection mode {mode}");
            }
            ctxBox.Show();
        }

        private void RotBubbleChartView_ActivateCurrent(object o, EventArgs args)
        {
            //Console.WriteLine("Activating menu");
            PopulateMenus();
        }

        private void ContextMenu_Deactivated(object sender, EventArgs e)
        {
            //Console.WriteLine("Deactivating menu");
            graphView.UnSelect();
            //PopulateMenus();
        }

        private void OnPopup(object o, ContextMenuEventArgs args)
        {
            // Get the point clicked by the mouse.
            // FIXME - need to test if the user right-clicked on an object or the root window
            //Cairo.PointD clickPoint = new PointD(args.X, args.Y);
            // Look through nodes & arcs for the click point
            //DGObject clickedObject = Graph.DirectedGraph.Nodes.FindLast(node => node.HitTest(clickPoint));

            PopulateMenus();
            if (ContextMenu.Children.Length > 0) ContextMenu.Popup();
        }
// vvvvv REFACTOR THIS fixme
        /// <summary>
        /// Selected graph object will be an arc, node, or null. Make sure the menu is appropriate
        /// </summary>
        private void PopulateMenus()
        {
            ContextMenu.Foreach(mi => ContextMenu.Remove(mi));
            MenuItem item;
            EventHandler handler;
            if (graphView.selectedObject == null )
            {
                item = new MenuItem("Add Node");
                handler = delegate (object s, EventArgs x)
                {
                    Node n = new Node { Name = graphView.DirectedGraph.nextNodeID() }; // blecchh
                    StateNode newNode = new StateNode(n) { NodeName = n.Name };/* fixme set location to x,y of menu posting location, use IDs not names  */
                    AddNode?.Invoke(this, new AddNodeEventArgs { Node = newNode });
                };
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            if (graphView.selectedObject?.GetType() == typeof(DGNode) && graphView.selected2Object == null)
            {
                item = new MenuItem("Duplicate " + graphView.selectedObject.Name);
                handler = delegate (object s, EventArgs x)
                {
                    string newName = "Copy of " + graphView.selectedObject.Name;
                    Node n = new Node { Name = graphView.DirectedGraph.nextNodeID() }; // blecchh
                    /* fixme set location nearby to old node, use IDs not names */
                    AddNode?.Invoke(this, new AddNodeEventArgs { Node = new StateNode(n) { NodeName = newName } });
                    foreach (var arc in graphView.DirectedGraph.Arcs.FindAll(arc => arc.SourceName == graphView.selectedObject.Name))
                    {
                            Arc newArc = new Arc(arc);
                            newArc.Name = graphView.DirectedGraph.nextArcID();
                            newArc.SourceName = newName;
                            newArc.DestinationName = arc.DestinationName;
                            AddArc?.Invoke(this, new AddArcEventArgs { Arc = new RuleAction( newArc )});
                    }
                    foreach (var arc in graphView.DirectedGraph.Arcs.FindAll(arc => arc.DestinationName == graphView.selectedObject.Name))
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

                item = new MenuItem("Add Arc from " + graphView.selectedObject.Name + " to " + graphView.selectedObject.Name);
                handler = delegate (object s, EventArgs x)
                {
                        Arc newArc = new Arc();
                        newArc.Name = graphView.DirectedGraph.nextArcID();
                        newArc.SourceName = graphView.selectedObject.Name;
                        newArc.DestinationName = graphView.selectedObject.Name;
                        AddArc?.Invoke(this, new AddArcEventArgs { Arc = new RuleAction(newArc) });
                };
                item.Activated += handler;
                ContextMenu.Append(item);

                item = new MenuItem("Delete " + graphView.selectedObject.Name);
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("delete node selected");
                    DelNode?.Invoke(this, new DelNodeEventArgs { nodeNameToDelete = graphView.selectedObject.Name });
                };
                item.Activated += handler;
                ContextMenu.Append(item);

            }
            if (graphView.selectedObject?.GetType() == typeof(DGNode) && graphView.selected2Object?.GetType() == typeof(DGNode))
            {
                item = new MenuItem("Add Arc from " + graphView.selectedObject.Name + " to " + graphView.selected2Object.Name);
                handler = delegate (object s, EventArgs x)
                {
                        Arc newArc = new Arc();
                        newArc.Name = graphView.DirectedGraph.nextArcID();
                        newArc.SourceName = graphView.selectedObject.Name;
                        newArc.DestinationName = graphView.selected2Object.Name;
                        AddArc?.Invoke(this, new AddArcEventArgs { Arc = new RuleAction(newArc) });
                };
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            if (graphView.selectedObject?.GetType() == typeof(DGArc) )
            {
                item = new MenuItem("Duplicate Arc from " + (graphView.selectedObject as DGArc).Source.Name + " to " + (graphView.selectedObject as DGArc).Target.Name);
                handler = delegate (object s, EventArgs x)
                {
                        Arc newArc = new Arc((graphView.selectedObject as DGArc).ToArc());
                        newArc.Name = graphView.DirectedGraph.nextArcID();
                        newArc.SourceName = (graphView.selectedObject as DGArc).Source.Name;
                        newArc.DestinationName = (graphView.selectedObject as DGArc).Target.Name;
                        AddArc?.Invoke(this, new AddArcEventArgs { Arc = new RuleAction(newArc) });
                    /// fixme - copy across rules & actions from selected arc
                };
                item.Activated += handler;
                ContextMenu.Append(item);

                item = new MenuItem("Delete Arc from " + (graphView.selectedObject as DGArc).Source.Name + " to " + (graphView.selectedObject as DGArc).Target.Name);
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("del arc selected");
                    DelArc?.Invoke(this, new DelArcEventArgs { arcNameToDelete = (graphView.selectedObject as DGArc).Name });
                };
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            ContextMenu.ShowAll();  // This packs the menu objects, but doesn't post it.
        }

        private void OnGraphObjectMoved(object sender, ObjectMovedArgs args)
        {
            OnGraphChanged?.Invoke(this, new GraphChangedEventArgs(){ Arcs = Arcs, Nodes = Nodes });
        }

        /// <summary>
        /// The user has selected something, so change the UI to present the context for that selection
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void OnGraphObjectSelected(object o, GraphObjectSelectedArgs args)
        {
            if (args.Object1 == null)
                OnSelect(selectMode.info, "");

            if (args.Object1?.GetType() == typeof(DGNode))
            {
                AddNode(o, new AddNodeEventArgs { Node = new StateNode((args.Object1 as DGNode).ToNode()) { NodeName = NodeNames[(args.Object1 as DGNode).Name] } }); 
                OnSelect(selectMode.node, (args.Object1 as DGNode).Name);
                OnGraphChanged?.Invoke(this, new GraphChangedEventArgs { Arcs = Arcs, Nodes = Nodes });
            }
            if (args.Object1?.GetType() == typeof(DGArc))
            {
                AddArc(o, new AddArcEventArgs { Arc = new RuleAction((args.Object1 as DGArc).ToArc()) });
                OnSelect(selectMode.arc, (args.Object1 as DGArc).Name);
                OnGraphChanged?.Invoke(this, new GraphChangedEventArgs { Arcs = Arcs, Nodes = Nodes });
            }
        }

        // ^^^^^ REFACTOR THIS        
        /// <summary>
        /// The initial state of the simulation is in a combobox. Allow simple get/set access
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
                        combobox1.SetActiveIter(iter);
                        break;
                    }
                } while (combobox1.Model.IterNext(ref iter));
            }
        }

        // The combobox has told us a new initial state has been chosen
        private void OnComboBox1SelectedValueChanged(object sender, EventArgs e)
        {
            if (combobox1.GetActiveIter(out TreeIter iter))
            {
                string selectedText = (string)combobox1.Model.GetValue(iter, 0);
                OnInitialStateChanged?.Invoke(sender, new InitialStateEventArgs() { initialState = selectedText } );
            }
        }

        /// <summary>
        /// Set the graph in the view.
        /// </summary>
        /// <param name="nodes">Nodes of the graph.</param>
        /// <param name="arcs">Arcs of the graph.</param>
        public void SetGraph(List<Models.Management.StateNode> nodes, List<Models.Management.RuleAction> arcs)
        {
            Rules.Clear();
            Actions.Clear();
            string lastSelected = InitialState; 
            comboModel.Clear();
            var graph = new Models.DirectedGraph();

            nodes.ForEach(node => {
                graph.AddNode(node);
                NodeNames[node.Name] = node.NodeName;
                comboModel.AppendValues (node.Name);
            });
            arcs.ForEach(arc =>
            {
                Rules[arc.Name] = arc.testCondition;
                Actions[arc.Name] = arc.action;
                graph.AddArc(arc);
            });
            graphView.DirectedGraph = graph;
            InitialState = lastSelected;
        }

        public List<StateNode> Nodes
        {
            get
            {
                List<StateNode> nodes = new List<StateNode>();
                foreach (var node in graphView.DirectedGraph.Nodes)
                    nodes.Add(new StateNode(node) { NodeName = NodeNames[node.Name] });
                return nodes;
            }
        }

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
        /*
        /// <summary>The description (nodes & arcs) of the directed graph. 
        /// Split the get/set access between the rule/action dictionaries and 
        /// the embedded graph.
        /// </summary>
        public RBGraph Graph
        {
            get
            {
                RBGraph resultgraph = new RBGraph();
                DirectedGraph graph = graphView.DirectedGraph;
                graph.Nodes.ForEach(n =>
                {
                    resultgraph.Nodes.Add(new StateNode(n) { NodeName = NodeNames[n.Name] });
                });
                graph.Arcs.ForEach(a =>
                {
                    Arc ga = graph.Arcs.Find(x => x.Name == a.Name);
                    var na = new RuleAction(ga);
                    na.testCondition = Rules[na.Name];
                    na.action = Actions[na.Name];
                    resultgraph.Arcs.Add(na);
                });
                return resultgraph;
            }
            set
            {
                Rules.Clear(); Actions.Clear();
                string lastSelected = InitialState; 
                comboModel.Clear();
                DirectedGraph graph = new DirectedGraph();

                value.Nodes.ForEach(node => {
                    graph.AddNode(node);
                    NodeNames[node.Name] = node.NodeName;
                    comboModel.AppendValues (node.Name);
                });
                value.Arcs.ForEach(arc =>
                {
                    Rules[arc.Name] = arc.testCondition;
                    Actions[arc.Name] = arc.action;
                    graph.AddArc(arc);
                });
                graphView.DirectedGraph = graph;
                InitialState = lastSelected;
            }
        }
        */
    }
}
