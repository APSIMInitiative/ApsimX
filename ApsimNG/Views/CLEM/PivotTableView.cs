// -----------------------------------------------------------------------
// <copyright file="PivotTableView.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

using Models.Core;
using Models.CLEM;
using Models.CLEM.Reporting;
using System;
using System.Collections.Generic;
using Gtk;
using UserInterface.Views;

namespace ApsimNG.Views.CLEM
{
    /// <summary>
    /// UI for simple pivoting of resource ledger data
    /// </summary>
    class PivotTableView : ViewBase
    {
        /// <summary>
        /// Triggers when the data in the gridview needs to be updated
        /// </summary>
        public event EventHandler UpdateData;

        /// <summary>
        /// Triggers when the data in the gridview needs to be stored
        /// </summary>
        public event EventHandler StoreData;

        /// <summary>
        /// Triggers when changes are made to the view that need to be tracked
        /// </summary>
        public event EventHandler TrackChanges;       

        /// <summary>
        /// Used to simplify interaction with the GtkComboBox objects in a PivotTableView
        /// </summary>
        /// <remarks>
        /// GtkComboBox.Changed does not have the necessary EventArgs, nor
        /// does the sender object have all the required information. We
        /// wrap the ComboBox in this class and invoke the event here so 
        /// that data can be transmitted when the event triggers.
        /// 
        /// The class also simplifies the setup process for each ComboBox.
        /// </remarks>
        public class ViewBox
        {
            /// <summary>
            /// Triggers when the active selection in the box is changed
            /// </summary>
            public event EventHandler Changed;
            
            public string Name { get; set; }

            /// <summary>
            /// Tracks the current active text in the combo box
            /// </summary>
            public string Text
            {
                get
                {
                    return box.ActiveText;
                }
            }

            /// <summary>
            /// Tracks the current active selection in the combo box
            /// </summary>
            public int ID
            {
                get
                {
                    return box.Active;
                }
                set
                {
                    box.Active = value;
                }
            }

            private ComboBox box;

            private PivotTableView parent;

            /// <summary>
            /// Constructor for the ViewBox
            /// </summary>
            /// <param name="name">The name of the viewbox</param>
            /// <param name="parent">The parent view</param>
            /// <param name="builder">The Gtk.Builder used to construct the ComboBox</param>
            public ViewBox(string gladeObject, PivotTableView parent, Builder builder)
            {                
                this.parent = parent;
                box = (ComboBox)builder.GetObject(gladeObject);
                AddRenderer(box);

                Changed += parent.OnInvokeUpdate;
                box.Changed += OnChanged;
            }

            /// <summary>
            /// Adds a text cell to the combo box
            /// </summary>
            /// <param name="text">The text to add</param>
            public void AddText(string text)
            {
                box.AppendText(text);
            }

            /// <summary>
            /// Invokes the ViewBox Changed event
            /// </summary>
            /// <param name="sender">The sending object</param>
            /// <param name="e">The event arguments</param>
            private void OnChanged(object sender, EventArgs e)
            {
                Changed?.Invoke(this, e);
            }

            /// <summary>
            /// Adds a text CellRenderer to a combo box
            /// </summary>
            /// <param name="combo">The box to add the renderer to</param>
            private static void AddRenderer(ComboBox combo)
            {
                // Remove any existing list
                combo.Clear();

                // Create a new renderer for the text in the box
                CellRendererText renderer = new CellRendererText();
                combo.PackStart(renderer, false);
                combo.AddAttribute(renderer, "text", 0);

                // Add a ListStore to the box
                ListStore store = new ListStore(typeof(string));
                combo.Model = store;
            }

            /// <summary>
            /// Detach the events
            /// </summary>
            public void Detach()
            {
                Changed -= parent.OnInvokeUpdate;
                box.Changed -= OnChanged;
            }
        }

        /// <summary>
        /// The Ledger box
        /// </summary>
        public ViewBox LedgerViewBox { get; set; }

        /// <summary>
        /// The Expression box
        /// </summary>
        public ViewBox ExpressionViewBox { get; set; }

        /// <summary>
        /// The Value box
        /// </summary>
        public ViewBox ValueViewBox { get; set; }

        /// <summary>
        /// The Row box
        /// </summary>
        public ViewBox RowViewBox { get; set; }

        /// <summary>
        /// The Column box
        /// </summary>
        public ViewBox ColumnViewBox { get; set; }

        /// <summary>
        /// The Pivot box
        /// </summary>
        public ViewBox FilterViewBox { get; set; }

        /// <summary>
        /// The Time box
        /// </summary>
        public ViewBox TimeViewBox { get; set; }

        /// <summary>
        /// The main widget
        /// </summary>
        private VBox vbox1 = null;

        /// <summary>
        /// Button to cycle the pivot backwards
        /// </summary>
        private Button leftButton = null;

        /// <summary>
        /// Button to cycle the pivot forwards
        /// </summary>
        private Button rightButton = null;

        /// <summary>
        /// Button to store the current gridview in the DataStore
        /// </summary>
        private Button storeButton = null;

        /// <summary>
        /// Custom gridview added post-reading the glade file
        /// </summary>
        public GridView Grid { get; set; } = null;
        
        /// <summary>
        /// Displays the currently selected filter
        /// </summary>
        public Label FilterLabel { get; set; } = null;

        /// <summary>
        /// Instantiate the View
        /// </summary>
        /// <param name="owner">The owner view</param>
        public PivotTableView(ViewBase owner) : base(owner)
        {
            // Read in the glade file
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.PivotTableView.glade");

            // Assign the interactable objects from glade
            vbox1 = (VBox)builder.GetObject("vbox1");

            leftButton = (Button)builder.GetObject("leftbutton");
            rightButton = (Button)builder.GetObject("rightbutton");
            storeButton = (Button)builder.GetObject("storebutton");

            leftButton.Name = "left";
            rightButton.Name = "right";

            FilterLabel = (Label)builder.GetObject("filter");

            // Setup the ViewBoxes
            LedgerViewBox = new ViewBox("ledgerbox", this, builder) { Name = "LedgerViewBox" };
            ExpressionViewBox = new ViewBox("expressionbox", this, builder) { Name = "ExpressionViewBox" };
            ValueViewBox = new ViewBox("valuebox", this, builder) { Name = "ValueViewBox" };
            RowViewBox = new ViewBox("rowbox", this, builder) { Name = "RowViewBox" };
            ColumnViewBox = new ViewBox("columnbox", this, builder) { Name = "ColumnViewBox" };
            FilterViewBox = new ViewBox("filterbox", this, builder) { Name = "FilterViewBox" };
            TimeViewBox = new ViewBox("timebox", this, builder) { Name = "TimeViewBox" };

            // Add text options to the ViewBoxes           
            ExpressionViewBox.AddText("Sum");
            ExpressionViewBox.AddText("Average");
            ExpressionViewBox.AddText("Max");
            ExpressionViewBox.AddText("Min");

            ValueViewBox.AddText("Gain");
            ValueViewBox.AddText("Loss");

            AddOptions(RowViewBox);
            AddOptions(ColumnViewBox);

            FilterViewBox.AddText("None");
            AddOptions(FilterViewBox);

            TimeViewBox.AddText("Daily");
            TimeViewBox.AddText("Monthly");
            TimeViewBox.AddText("Yearly");            

            // Subscribe the left/right buttons to the change pivot event
            leftButton.Clicked += OnInvokeUpdate;
            rightButton.Clicked += OnInvokeUpdate;

            // Subscribe the store button to the store data event 
            storeButton.Clicked += OnStoreData;

            // Add the custom gridview (external to glade)
            Grid = new GridView(owner);
            Grid.ReadOnly = true;
            vbox1.Add(Grid.MainWidget);

            // Let the viewbase know which widget is the main widget
            mainWidget = vbox1;
        }

        /// <summary>
        /// Add the ledger text options to a view box
        /// </summary>
        /// <param name="combo"></param>
        private static void AddOptions(ViewBox box)
        {
            List<string> options = new List<string>()
            {
                "CheckpointID",
                "SimulationID",
                "Zone",
                "Clock.Today",
                "Resource",
                "Activity",
                "ActivityType",
                "Reason"
            };

            foreach (string option in options) box.AddText(option);
        }

        /// <summary>
        /// Invokes the UpdateData and TrackChanges events
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnInvokeUpdate(object sender, EventArgs e)
        {
            // Invoke the UpdateData event
            UpdateData?.Invoke(sender, EventArgs.Empty);

            // Invoke the TrackChanges event             
            TrackChanges?.Invoke(sender, EventArgs.Empty);            
        }

        /// <summary>
        /// Invokes the StoreData event
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnStoreData(object sender, EventArgs e)
        {
            StoreData?.Invoke(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Looks through the simulation for resource ledgers and adds
        /// them as options to the ledger box
        /// </summary>
        /// <param name="table">The table model in the simulation</param>
        public void SetLedgers(PivotTable table)
        {
            // Find a CLEMFolder 
            CLEMFolder folder = new CLEMFolder();
            folder = Apsim.Find(table, typeof(CLEMFolder)) as CLEMFolder;

            // Look for ledgers inside the CLEMFolder
            foreach (var child in folder.Children)
            {
                if (child.GetType() != typeof(ReportResourceLedger)) continue;

                ReportResourceLedger ledger = child as ReportResourceLedger;
                LedgerViewBox.AddText(ledger.Name);
            }

            // Set the active ledger option
            if (LedgerViewBox.ID < 0) LedgerViewBox.ID = 0;
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
            LedgerViewBox.Detach();
            RowViewBox.Detach();
            ColumnViewBox.Detach();
            ExpressionViewBox.Detach();
            ValueViewBox.Detach();
            FilterViewBox.Detach();
            TimeViewBox.Detach();

            leftButton.Clicked -= OnInvokeUpdate;
            rightButton.Clicked -= OnInvokeUpdate;
        }
    }

}
