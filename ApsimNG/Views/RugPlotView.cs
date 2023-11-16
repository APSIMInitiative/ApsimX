namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;
    using EventArguments;
    using EventArguments.DirectedGraph;
    using APSIM.Interop.Visualisation;
    using Gtk;
    using Models.Management;
    using Models;
    using Extensions;
    using Utility;
    using APSIM.Shared.Graphing;
    using Node = APSIM.Shared.Graphing.Node;
    

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class RugPlotView : ViewBase
    {
        private Box vbox1 = null;
        private ListStore stateList = new ListStore(typeof(string));
        private ListStore ruleTree = new ListStore(typeof(string));
        private DateTime earliestDate;
        private Label earliestDateLabel;
        private DateTime lastDate;
        private Label lastDateLabel;
        private DateTime selectedDate;
        private Label selectedDateLabel;

        Button dateminus = null;
        Button dateplus = null;

        /// <summary>
        /// Properties editor.
        /// </summary>
        public IPropertyView PropertiesView { get; private set; }
        public RugPlotView(ViewBase owner = null) : base(owner)
        {
            vbox1 = new VBox(true, 0);
            mainWidget = vbox1;
            mainWidget.Destroyed += OnDestroyed;

            HBox hbox1 = new HBox();

            Label l1 = new Label("Start Date:");
            hbox1.PackStart(l1, false, false, 5 );

            earliestDateLabel = new Label("Some Date Here");
            hbox1.PackStart(earliestDateLabel, false, false, 5);

            Label l3 = new Label("End Date:");   
            hbox1.PackStart(l3, false, false, 5);

            lastDateLabel = new Label("Some Date Here");
            hbox1.PackStart(lastDateLabel, false, false, 5);

            Label l5 = new Label("Selected");
            hbox1.PackStart(l5, false, false, 5);

            selectedDateLabel = new Label("Selected Date Here");
            hbox1.PackStart(selectedDateLabel, false, false, 5);
            
            dateminus = new Button(new Image(Stock.Remove, IconSize.Button));
            dateminus.Clicked += onMinusButtonClicked;
            hbox1.PackStart(dateminus, false, false, 5);

            dateplus = new Button(new Image(Stock.Add, IconSize.Button));
            dateplus.Clicked += onPlusButtonClicked;
            hbox1.PackStart(dateplus, false, false, 5);

            vbox1.PackStart(hbox1, true, false, 10);

            HPaned hpane2 = new HPaned();
             
            VBox vbox2a = new VBox();
            VBox vbox2b = new VBox();
            VPaned vpane2b = new VPaned();
            
            ScrolledWindow StateLegend = new ScrolledWindow();
            StateLegend.ShadowType = ShadowType.EtchedIn;
            StateLegend.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            //StateLegend.Add((stateList as ViewBase).MainWidget);

            ScrolledWindow RVTree = new ScrolledWindow();
            RVTree.ShadowType = ShadowType.EtchedIn;
            RVTree.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            //RVTree.Add((CropList as ViewBase).MainWidget);
            vpane2b.Pack1(StateLegend, true, true );
            vpane2b.Pack2(RVTree, true, true );
            vpane2b.ShowAll();

            hpane2.Pack1(vbox2a, true, true );
            hpane2.Pack2(vbox2b, true, true );

            vbox1.PackStart(hpane2, true, false , 0);
            vbox1.ShowAll();


            PropertiesView = new PropertyView(this);
            ((ScrolledWindow)((ViewBase)PropertiesView).MainWidget).HscrollbarPolicy = PolicyType.Never;


            //graphView.OnGraphObjectSelected += OnGraphObjectSelected;
            //graphView.OnGraphObjectMoved += OnGraphObjectMoved;
            //combobox1.Changed += OnComboBox1SelectedValueChanged;

            // Ensure the menu is populated
            Select(null);
        }

        /// <summary>
        /// Set the graph in the view.
        /// </summary>
        /// <param name="RVs">Nodes of the graph.</param>
        /// <param name="RVIndices">Arcs of the graph.</param>
        public void SetGraph(List<RVPair> RVs, Dictionary<DateTime, int> RVIndices)
        {
            ruleTree.Clear();

            if (RVIndices != null) {
                earliestDate = RVIndices.Keys.Min();
                earliestDateLabel.Text = earliestDate.ToString("d MMM yyyy");

                selectedDate = earliestDate;
                selectedDateLabel.Text = selectedDate.ToString("d MMM yyyy");

                lastDate = RVIndices.Keys.Max();
                lastDateLabel.Text = lastDate.ToString("d MMM yyyy");
            }
            //var graph = new DirectedGraph();
            //graphView.DirectedGraph = graph;
            //graphView.MainWidget.QueueDraw();
        }

        /// <summary>
        /// A graph object has been selected. Make the (middle part of) UI relevant to it
        /// </summary>
        /// <param name="objectName">Name of the object to be selected.</param>
        public void Select(string objectName)
        {
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
                dateminus.Clicked -= onMinusButtonClicked;
                dateplus.Clicked -= onPlusButtonClicked;
                
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }


        private void onPlusButtonClicked( object obj, EventArgs args )
        {
            selectedDate = selectedDate.AddDays(1);
            if (selectedDate > lastDate) selectedDate = lastDate;
            selectedDateLabel.Text = selectedDate.ToString("d MMM yyyy");
        }
        private void onMinusButtonClicked( object obj, EventArgs args )
        {
            selectedDate = selectedDate.AddDays(-1);
            if (selectedDate < earliestDate) selectedDate = earliestDate;
            selectedDateLabel.Text = selectedDate.ToString("d MMM yyyy");
        }

        private void onDateSelected(object source, System.EventArgs args)
        {
            //selectedDate = ???
            System.Console.WriteLine("Fixme ");
        }

    }
}
