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
        public event EventHandler<TrackChangesArgs> TrackChanges;

        /// <summary>
        /// Triggers when the current pivot focus is changed
        /// </summary>
        public event EventHandler<ChangePivotArgs> ChangePivot;

        /// <summary>
        /// Carries the data which needs to be tracked when changes are made
        /// </summary>
        public class TrackChangesArgs : EventArgs
        {
            public string Name { get; }
            public object Value { get; }

            public TrackChangesArgs(string name, object value)
            {
                Name = name;
                Value = value;
            }

            public TrackChangesArgs(ViewBox box)
            {
                Name = box.Name;
                Value = box.ID;
            }
        }

        /// <summary>
        /// Carries the information about whether the pivot is increasing or decreasing
        /// </summary>
        public class ChangePivotArgs : EventArgs
        {
            public bool Increase { get; }

            public ChangePivotArgs(bool increase)
            {
                Increase = increase;
            }
        }

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

            /// <summary>
            /// The name of the ViewBox
            /// </summary>
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
            public ViewBox(string name, PivotTableView parent, Builder builder)
            {
                Name = name;
                this.parent = parent;
                box = (ComboBox)builder.GetObject($"{name.ToLower()}box");
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
        public ViewBox Ledger { get; set; }

        /// <summary>
        /// The Expression box
        /// </summary>
        public ViewBox Expression { get; set; }

        /// <summary>
        /// The Value box
        /// </summary>
        public ViewBox Value { get; set; }

        /// <summary>
        /// The Row box
        /// </summary>
        public ViewBox Row { get; set; }

        /// <summary>
        /// The Column box
        /// </summary>
        public ViewBox Column { get; set; }

        /// <summary>
        /// The Pivot box
        /// </summary>
        public ViewBox Pivot { get; set; }

        /// <summary>
        /// The Time box
        /// </summary>
        public ViewBox Time { get; set; }

        /// <summary>
        /// The main widget
        /// </summary>
        private VBox vbox1 = null;

        /// <summary>
        /// Button to decrease pivot (cycle left)
        /// </summary>
        private Button leftbutton = null;

        /// <summary>
        /// Button to increase pivot (cycle right)
        /// </summary>
        private Button rightbutton = null;

        /// <summary>
        /// Button to store the current gridview in the DataStore
        /// </summary>
        private Button storebutton = null;

        /// <summary>
        /// Custom gridview added post-reading the glade file
        /// </summary>
        public GridView gridview { get; set; } = null;

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

            leftbutton = (Button)builder.GetObject("leftbutton");
            rightbutton = (Button)builder.GetObject("rightbutton");
            storebutton = (Button)builder.GetObject("storebutton");

            // Setup the ViewBoxes
            Ledger = new ViewBox("Ledger", this, builder);
            Expression = new ViewBox("Expression", this, builder);
            Value = new ViewBox("Value", this, builder);
            Row = new ViewBox("Row", this, builder);
            Column = new ViewBox("Column", this, builder);
            Pivot = new ViewBox("Pivot", this, builder);
            Time = new ViewBox("Time", this, builder);

            // Add text options to the ViewBoxes           
            Expression.AddText("Sum");
            Expression.AddText("Average");
            Expression.AddText("Max");
            Expression.AddText("Min");

            Value.AddText("Gain");
            Value.AddText("Loss");

            AddOptions(Row);
            AddOptions(Column);
            AddOptions(Pivot);

            Time.AddText("Daily");
            Time.AddText("Monthly");
            Time.AddText("Yearly");
            Time.AddText("MonthlyAverage");

            // Subscribe the left/right buttons to the change pivot event
            leftbutton.Clicked += OnChangePivot;
            rightbutton.Clicked += OnChangePivot;

            // Subscribe the store button to the store data event 
            storebutton.Clicked += OnStoreData;

            // Add the custom gridview (external to glade)
            gridview = new GridView(owner);
            gridview.ReadOnly = true;
            vbox1.Add(gridview.MainWidget);

            // Let the viewbase know which widget is the main widget
            _mainWidget = vbox1;
        }

        /// <summary>
        /// Add a standard collection of text options to a view box
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
            if (sender.GetType() == typeof(ViewBox))
            {
                TrackChangesArgs args = new TrackChangesArgs((ViewBox)sender);

                TrackChanges?.Invoke(sender, args);
            }
        }

        /// <summary>
        /// Invokes the ChangePivot event
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnChangePivot(object sender, EventArgs e)
        {
            ChangePivotArgs args = new ChangePivotArgs(sender == rightbutton);

            ChangePivot.Invoke(((Button)sender).Name, args);
            OnInvokeUpdate(sender, EventArgs.Empty);
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
                Ledger.AddText(ledger.Name);
            }

            // Set the active ledger option
            if (Ledger.ID < 0) Ledger.ID = 0;
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
            Ledger.Detach();
            Row.Detach();
            Column.Detach();
            Expression.Detach();
            Value.Detach();
            Pivot.Detach();
            Time.Detach();

            leftbutton.Clicked -= OnChangePivot;
            rightbutton.Clicked -= OnChangePivot;
        }
    }

}
