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
    //using Cairo;
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

        /// <summary>Invoked when the user adds an arc</summary>
        public event EventHandler<AddArcEventArgs> AddArc;

        /// <summary>Invoked when the user deletes an arc</summary>
        public event EventHandler<DelArcEventArgs> DelArc;

        /// <summary>Invoked when the user deletes an arc</summary>
        public event EventHandler<DupArcEventArgs> DupArc;

        public Views.DirectedGraphView graph;

        private Paned vpaned1 = null;
        private ComboBox combobox1 = null;
        private ListStore comboModel = new ListStore(typeof(string));
        private CellRendererText comboRender = new CellRendererText();

        private Label ctxLabel = null;
        private Widget arcSelWdgt = null;
        private EditorView textbox1 = null;
        private EditorView textbox2 = null;
        private Widget nodeSelWdgt = null;
        private Entry nameEntry = null;
        private Entry descEntry = null;
        private Entry colEntry = null;
        private Widget infoWdgt = null;

        private Box ctxBox = null;
        public Box MainWidget;
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
            ctxBox = new ctxView(this, new VBox(false, 0));
            vbox1.PackStart(ctxBox.MainWidget, false, false, 0);

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
            // Ensure the menu is populated
            OnGraphObjectSelected(null, new GraphObjectSelectedArgs(null));

            defaultOutline = Utility.Colour.FromGtk(owner.MainWidget.Style.Foreground(StateType.Normal));
            defaultBackground = Utility.Colour.FromGtk(owner.MainWidget.Style.Background(StateType.Normal));

            ContextMenuHelper contextMenuHelper = new ContextMenuHelper(graph.MainWidget);
            contextMenuHelper.ContextMenu += OnPopup;

            ContextMenu.SelectionDone += ContextMenu_Deactivated;
            (ContextMenu as MenuShell).Mapped+= RotBubbleChartView_ActivateCurrent;
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
        // Context sensitive controls for editing arcs / node

            public void ctxView(ViewBase view, Box parent)
            {
                ctxLabel = new Label("Information");

                // Arc selection: rules & actions
                VBox arcSelBox = new VBox();

                Label l1 = new Label("Rules");
                arcSelBox.PackStart(l1, false, false, 0); l1.Show();
                textbox1 = new EditorView(view);
                textbox1.MainWidget.HeightRequest = 75;
                ScrolledWindow rules = new ScrolledWindow();
                rules.ShadowType = ShadowType.EtchedIn;
                rules.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
                rules.Add(textbox1.MainWidget); textbox1.MainWidget.Show();
                arcSelBox.PackStart(rules, false, false, 0); rules.Show();

                Label l2 = new Label("Actions");
                arcSelBox.PackStart(l2, false, false, 0); l2.Show();
                textbox2 = new EditorView(view);
                textbox2.MainWidget.HeightRequest = 75;
                ScrolledWindow actions = new ScrolledWindow();
                actions.ShadowType = ShadowType.EtchedIn;
                actions.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
                actions.Add(textbox2.MainWidget); textbox2.MainWidget.Show();
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
                nameEntry.Changed += OnNameChanged;
                t1.Attach(nameEntry, 1, 2, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0); nameEntry.Show();
                descEntry = new Entry();
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

                MainWidget = parent;

                OnSelect(selectMode.info, null);
            }
            public void OnNameChanged(object o, EventArgs args)
            {
                if (selectedObject != null)
                    selectedObject.Name = (o as Entry).Text;
            }
            private DGObject selectedObject = null;
            public void OnSelect(selectMode mode, DGObject o)
            {
                MainWidget.Foreach(c => c.Hide()); // .HideAll();
                ctxLabel.Show();
                MainWidget.PackStart(ctxLabel, false, false, 0);
                selectedObject = o;
                switch (mode)
                {
                    case selectMode.arc:
                        {
                            ctxLabel.Text = "Transition from " + (selectedObject as DGArc).sourceNode.Name + " to " + (selectedObject as DGArc).targetNode.Name;
                            arcSelWdgt.Show();
                            MainWidget.PackStart(arcSelWdgt, false, false, 0);
                            break;
                        }
                    case selectMode.node:
                        {
                            ctxLabel.Text = "State";
                            nameEntry.Text = selectedObject.Name;
                            //descEntry.Text = selectedObject.???;

                            nodeSelWdgt.Show();
                            MainWidget.PackStart(nodeSelWdgt, false, false, 0);
                            break;
                        }
                    case selectMode.info:
                        {
                            ctxLabel.Text = "Information";
                            infoWdgt.Show();
                            MainWidget.PackStart(infoWdgt, false, false, 0);
                            break;
                        }
                }
                MainWidget.Show();
            }
        }
        public enum selectMode { arc, node, info };

        private void RotBubbleChartView_ActivateCurrent(object o, EventArgs args)
        {
            
            Console.WriteLine("Activating menu");
            PopulateMenus();
        }

        private void ContextMenu_Deactivated(object sender, EventArgs e)
        {
            Console.WriteLine("Deactivating menu");
            graph.UnSelect();
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


        /// <summary>
        /// Selected graph object will be an arc, node, or null. Make sure the menu is appropriate
        /// </summary>
        private void PopulateMenus()
        {
            ContextMenu.Foreach(mi => ContextMenu.Remove(mi));
            Console.WriteLine("Creating menu");
            MenuItem item;
            EventHandler handler;
            if (graph.selectedObject == null )
            {
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
            if (graph.selectedObject?.GetType() == typeof(DGNode) && graph.selected2Object == null)
            {
                item = new MenuItem("Duplicate " + graph.selectedObject.Name);
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("duplicate node selected");
                    if (DupNode != null) { DupNode(this, new DupNodeEventArgs { nodeNameToDuplicate = graph.selectedObject.Name }); }
                };
                item.Activated += handler;
                ContextMenu.Append(item);

                item = new MenuItem("Add Arc from " + graph.selectedObject.Name + " to " + graph.selectedObject.Name);
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("add arc 1 selected");
                    if (AddArc != null) { AddArc (this, new AddArcEventArgs { Source = graph.selectedObject.Name, Dest = graph.selectedObject.Name }); }
                };
                item.Activated += handler;
                ContextMenu.Append(item);

                item = new MenuItem("Delete " + graph.selectedObject.Name);
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("delete node selected");
                    if (DelNode != null) { DelNode(this, new DelNodeEventArgs { nodeNameToDelete = graph.selectedObject.Name }); }
                };
                item.Activated += handler;
                ContextMenu.Append(item);

            }
            if (graph.selectedObject?.GetType() == typeof(DGNode) && graph.selected2Object?.GetType() == typeof(DGNode))
            {
                item = new MenuItem("Add Arc from " + graph.selectedObject.Name + " to " + graph.selected2Object.Name);
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("add arc 2 selected");
                    if (AddArc != null) { AddArc(this, new AddArcEventArgs { Source = graph.selectedObject.Name, Dest = graph.selected2Object.Name }); }
                };
                item.Activated += handler;
                ContextMenu.Append(item);
            }
            if (graph.selectedObject?.GetType() == typeof(DGArc) )
            {
                item = new MenuItem("Duplicate Arc from " + (graph.selectedObject as DGArc).sourceNode.Name + " to " + (graph.selectedObject as DGArc).targetNode.Name);
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("dup arc selected");
                    if (DupArc != null) { DupArc(this, new DupArcEventArgs { arcNameToDuplicate = graph.selectedObject.Name }); }
                };
                item.Activated += handler;
                ContextMenu.Append(item);

                item = new MenuItem("Delete Arc from " + (graph.selectedObject as DGArc).sourceNode.Name + " to " + (graph.selectedObject as DGArc).targetNode.Name);
                handler = delegate (object s, EventArgs x)
                {
                    Console.WriteLine("del arc selected");
                    if (DelArc != null) { DelArc(this, new DelArcEventArgs { arcNameToDelete = (graph.selectedObject as DGArc).Name}); }
                };
                item.Activated += handler;
                ContextMenu.Append(item);
            }

            ContextMenu.ShowAll();  // This packs the menu objects, but doesn't post it.
        }


        private void OnGraphObjectSelected(object o, GraphObjectSelectedArgs args)
        {
            Console.WriteLine("Object selected");

            if (args.Object1 == null)
                middlebox.OnSelect(selectMode.info, null);

            if (args.Object1?.GetType() == typeof(DGNode))
                middlebox.OnSelect(selectMode.node, args.Object1);
            
            if (args.Object1?.GetType() == typeof(DGArc))
                middlebox.OnSelect(selectMode.arc, args.Object1);
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
    }
}
