// -----------------------------------------------------------------------
// <copyright file="RotBubbleChartView.cs" company="UQ">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using Interfaces;
    using EventArguments;
    using ApsimNG.Classes.DirectedGraph;
    using Gtk;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    /// 
    public class RotBubbleChartView : ViewBase, IRotBubbleChartView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        //public event EventHandler<SelectNodeEventArgs> SelectNode;

        /// <summary>Invoked when the user adds a node</summary>
        public event EventHandler<AddNodeEventArgs> AddNode;

        /// <summary>Invoked when the user duplicates a node</summary>
        public event EventHandler<DupNodeEventArgs> DupNode;

        /// <summary>Invoked when the user deletes a node</summary>
        public event EventHandler<DelNodeEventArgs> DelNode;

        public Views.DirectedGraphView graph;

        private Paned vpaned1 = null;
        private ComboBox combobox1 = null;
        private ListStore comboModel = new ListStore(typeof(string));
        private CellRendererText comboRender = new CellRendererText();
        private ctxView middlebox = null;
        private Menu ContextMenu = new Menu();

        public System.Drawing.Color defaultBackground;
        public System.Drawing.Color defaultOutline;


        public RotBubbleChartView(ViewBase owner = null) : base(owner)
        {
            vpaned1 = new VPaned();
            mainWidget = vpaned1;

            graph = new DirectedGraphView(this);
            vpaned1.Pack1(graph.MainWidget, true, true );

            VBox vbox1 = new VBox(false, 0);
            middlebox = new ctxView(new VBox(false, 0));
            vbox1.PackStart(middlebox.MainWidget, false, false, 0);

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

            graph.OnGraphObjectSelected += OnGraphObjectSelected;
            combobox1.Changed += OnComboBox1SelectedValueChanged;

            defaultOutline = Utility.Colour.FromGtk(owner.MainWidget.Style.Foreground(StateType.Normal));
            defaultBackground = Utility.Colour.FromGtk(owner.MainWidget.Style.Background(StateType.Normal));

            ContextMenuHelper contextMenuHelper = new ContextMenuHelper(graph.MainWidget);
            contextMenuHelper.ContextMenu += OnPopup;
        }
        private void OnPopup(object o, /*PopupMenuArgs */ ContextMenuEventArgs args)
        {
            Console.WriteLine("Posting menu");
            ContextMenu.Popup();
        }

        /// <summary>
        /// Selected graph object will be an arc, node, or null. Make sure the menu is appropriate
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void OnGraphObjectSelected(object o, GraphObjectSelectedArgs args)
        {
            ContextMenu.Foreach(mi => ContextMenu.Remove(mi));
            Console.WriteLine("Creating menu");
            MenuItem item;
            EventHandler handler;
            if (args.Object1 == null)
            {
                middlebox.OnSelect(selectMode.info);
                item = new MenuItem("Add Node");
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("Add node selected");
                    int n = Graph.DirectedGraph.Nodes.Count;
                    if (AddNode != null) { AddNode(this, new AddNodeEventArgs { Name = "State " + n, Background = defaultBackground, Outline = defaultOutline }); }
                };
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            if (args.Object1?.GetType() == typeof(DGNode) && args.Object2 == null)
            {
                middlebox.OnSelect(selectMode.node);
                item = new MenuItem("Duplicate");
                handler = delegate (object s, EventArgs x) {
                    Console.WriteLine("duplicate node selected");
                    if (DupNode != null) { DupNode(this, new DupNodeEventArgs { nodeNameToDuplicate = args.Object1.Name }); }
                };
                item.Activated += handler;
                ContextMenu.Append(item);

                item = new MenuItem("Delete");
                handler = delegate (object s, EventArgs x) {
                    Console.WriteLine("delete node selected");
                    if (DelNode != null) { DelNode(this, new DelNodeEventArgs { nodeNameToDelete = args.Object1.Name }); }
                };
                item.Activated += handler;
                ContextMenu.Append(item);

            }
            if (args.Object1?.GetType() == typeof(DGNode) && args.Object2?.GetType() == typeof(DGNode))
            {
                middlebox.OnSelect(selectMode.info);
                item = new MenuItem("Add Arc");
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("add arc selected");
                    //if (DupNode != null) { DupNode(this, new DupNodeEventArgs { nodeNameToDuplicate = args.Object1.Name }); }
                };
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            if (args.Object1?.GetType() == typeof(DGArc) )
                middlebox.OnSelect(selectMode.arc);

            ContextMenu.ShowAll(); 
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            graph.OnGraphObjectSelected -= OnGraphObjectSelected;
            combobox1.Changed -= OnComboBox1SelectedValueChanged;
            graph.MainWidget.Destroy();
            graph = null;
            mainWidget.Destroyed -= _mainWidget_Destroyed;
            owner = null;
        }

        private void OnComboBox1SelectedValueChanged(object sender, EventArgs e)
        {
            if (OnInitialStateChanged != null)
                OnInitialStateChanged.Invoke(sender, e);
        }

        /// <summary>
        /// Invoked when the user changes the relative to field.
        /// </summary>
        public event EventHandler OnInitialStateChanged;

        /// <summary>
        /// Gets the directed graph.
        /// </summary>
        public Views.DirectedGraphView Graph
        {
            get
            {
                return this.graph;
            }
        }

        // Context sensitive controls for editing arcs / node
        public class ctxView
        {
            public Box MainWidget;

            private Label ctxLabel = null;
            private Widget arcSelWdgt = null;
            private TextView textbox1 = null;
            private TextView textbox2 = null;
            private Widget nodeSelWdgt = null;
            private Entry nameEntry = null;
            private Entry descEntry = null;
            private Entry colEntry = null;
            private Widget infoWdgt = null;

            public ctxView(Box parent)
            {
                ctxLabel = new Label("Information");

                // Arc selection: rules & actions
                VBox arcSelBox = new VBox();

                Label l1 = new Label("Rules");
                arcSelBox.PackStart(l1, false, false, 0);
                textbox1 = new TextView();
                textbox1.HeightRequest = 75;
                //textbox1.
                ScrolledWindow rules = new ScrolledWindow();
                rules.ShadowType = ShadowType.EtchedIn;
                rules.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
                rules.Add(textbox1);
                arcSelBox.PackStart(rules, false, false, 0);

                Label l2 = new Label("Actions");
                arcSelBox.PackStart(l2, false, false, 0);
                textbox2 = new TextView();
                textbox2.HeightRequest = 75;
                ScrolledWindow actions = new ScrolledWindow();
                actions.ShadowType = ShadowType.EtchedIn;
                actions.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
                actions.Add(textbox2);
                arcSelBox.PackStart(actions, false, false, 0);
                arcSelWdgt = arcSelBox as Widget;

                // Node selection: 
                Table t1 = new Table(3, 2, true);
                Label l3 = new Label("Name");
                t1.Attach(l3, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                Label l4 = new Label("Description");
                t1.Attach(l4, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                Label l5 = new Label("Colour");
                t1.Attach(l5, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                nameEntry = new Entry();
                t1.Attach(nameEntry, 1, 2, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                descEntry = new Entry();
                t1.Attach(descEntry, 1, 2, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                colEntry = new Entry();
                t1.Attach(colEntry, 1, 2, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                nodeSelWdgt = t1 as Widget;

                // Info
                Label l6 = new Label();
                l6.LineWrap = true;
                l6.Text = "<left-click>: select a node or arc.\n"+
            "<right-click>: shows a context-sensitive menu.\n"+
            "\n"+
            "Once a node/arc is selected, it can be dragged to a new position.\n"+
            "\n"+
            "Nodes are created by right-clicking on a blank area.\n"+
            "\n"+
            "Transition arcs are created by firstly selecting a source node,\n"+
            "then right-clicking over a target node.\n";
                infoWdgt = l6 as Widget;

                MainWidget = parent;

                OnSelect(selectMode.info);
            }
            public void OnSelect(selectMode mode)
            {
                MainWidget.Foreach(c => c.Hide()); // .HideAll();
                ctxLabel.Show();
                MainWidget.PackStart(ctxLabel, false, false, 0);

                switch (mode)
                {
                    case selectMode.arc: {
                            ctxLabel.Text = "Transition from x to y";
                            arcSelWdgt.Show();
                            MainWidget.PackStart(arcSelWdgt, false, false, 0);
                            break;
                        }
                    case selectMode.node: {
                            ctxLabel.Text = "State xxx";
                            nodeSelWdgt.Show();
                            MainWidget.PackStart(nodeSelWdgt, false, false, 0);
                            break;
                        }
                    case selectMode.info: {
                            ctxLabel.Text = "Information";
                            infoWdgt.Show();
                            MainWidget.PackStart(infoWdgt,false, false, 0);
                            break;
                        }
                }
                MainWidget.Show();
            }
        }
        public enum selectMode { arc, node, info };
    }
}
