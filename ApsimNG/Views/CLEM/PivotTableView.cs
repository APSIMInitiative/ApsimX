// -----------------------------------------------------------------------
// <copyright file="CustomQueryView.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

using Models.Core;
using Models.CLEM;
using Models.CLEM.Reporting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using Gtk;
using UserInterface.Interfaces;
using UserInterface.Views;
using Utility;

namespace ApsimNG.Views.CLEM
{
    /// <summary>
    /// Displays the result of a Custom SQL Query on a DataTable
    /// </summary>
    class PivotTableView : ViewBase
    {
        // Components of the interface that are interactable
        // Taken from PivotTableView.glade
        private VBox vbox1 = null;

        //private ComboBox ledgerbox = null;
        //private ComboBox rowbox = null;
        //private ComboBox columnbox = null;
        //private ComboBox expressionbox = null;
        //private ComboBox valuebox = null;
        //private ComboBox pivotbox = null;

        private Button leftbutton = null;
        private Button rightbutton = null;
        private Button storebutton = null;

        /// <summary>
        /// Custom gridview added post-reading the glade file
        /// </summary>
        public GridView gridview { get; set; } = null;    

        public event EventHandler UpdateData;
        public event EventHandler StoreData;
        public event EventHandler<TrackChangesArgs> TrackChanges;
        public event EventHandler<ChangePivotArgs> ChangePivot;

        public class TrackChangesArgs : EventArgs
        {
            public string Name { get; }
            public object Value { get;  }

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
            public event EventHandler Changed;

            public string Name { get; set; }

            public string Text
            {
                get
                {
                    return box.ActiveText;
                }
            }

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
            /// 
            /// </summary>
            /// <param name="name"></param>
            /// <param name="parent"></param>
            /// <param name="builder"></param>
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
            /// 
            /// </summary>
            /// <param name="text"></param>
            public void AddText(string text)
            {
                box.AppendText(text);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnChanged(object sender, EventArgs e)
            {
                Changed?.Invoke(this, e);
            }

            /// <summary>
            /// 
            /// </summary>
            public void Detach()
            {
                Changed -= parent.OnInvokeUpdate;
                box.Changed -= OnChanged;                
            }
        }

        public ViewBox Ledger;
        public ViewBox Expression;
        public ViewBox Value;
        public ViewBox Row;
        public ViewBox Column;
        public ViewBox Pivot;

        /// <summary>
        /// Instantiate the View
        /// </summary>
        /// <param name="owner"></param>
        public PivotTableView(ViewBase owner) : base(owner)
        {
            // Read in the glade file
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.PivotTableView.glade");

            // Assign the interactable objects from glade
            vbox1 = (VBox)builder.GetObject("vbox1");

            //ledgerbox = (ComboBox)builder.GetObject("ledgerbox");            
            //expressionbox = (ComboBox)builder.GetObject("expressionbox");
            //valuebox = (ComboBox)builder.GetObject("valuebox");
            //rowbox = (ComboBox)builder.GetObject("rowbox");
            //columnbox = (ComboBox)builder.GetObject("columnbox");            
            //pivotbox = (ComboBox)builder.GetObject("pivotbox");

            // Setup the structs
            Ledger = new ViewBox("Ledger", this, builder);
            Expression = new ViewBox("Expression", this, builder);
            Value = new ViewBox("Value", this, builder);
            Row = new ViewBox("Row", this, builder);
            Column = new ViewBox("Column", this, builder);
            Pivot = new ViewBox("Pivot", this, builder);

            leftbutton = (Button)builder.GetObject("leftbutton");
            rightbutton = (Button)builder.GetObject("rightbutton");
            storebutton = (Button)builder.GetObject("storebutton");            

            // Add text renderers to the boxes
            //AddRenderer(ledgerbox);
            //AddRenderer(expressionbox);
            //AddRenderer(valuebox);
            //AddRenderer(rowbox);
            //AddRenderer(columnbox);            
            //AddRenderer(pivotbox);

            // Add text options to the ComboBoxes           
            Expression.AddText("Sum");
            Expression.AddText("Average");
            Expression.AddText("Max");
            Expression.AddText("Min");
            Expression.ID = 0;

            Value.AddText("Gain");
            Value.AddText("Loss");
            Value.ID = 0;

            AddOptions(Row);
            Row.ID = 4;

            AddOptions(Column);
            Column.ID = 5;

            AddOptions(Pivot);
            Pivot.ID = 7;            

            // Invoke update event when box options are changed
            //ledgerbox.Changed += OnInvokeUpdate;
            //rowbox.Changed += OnInvokeUpdate;
            //columnbox.Changed += OnInvokeUpdate;
            //expressionbox.Changed += OnInvokeUpdate;
            //valuebox.Changed += OnInvokeUpdate;
            //pivotbox.Changed += OnInvokeUpdate;            

            // Invoke the change pivot event when left/right buttons are clicked 
            leftbutton.Clicked += OnChangePivot;
            rightbutton.Clicked += OnChangePivot;

            // Invoke the store data event when the store button is clicked
            storebutton.Clicked += OnStoreData;

            // Add the custom gridview (external to glade)
            gridview = new GridView(owner);
            gridview.ReadOnly = true;
            vbox1.Add(gridview.MainWidget);

            // Let the viewbase know which widget is the main widget
            _mainWidget = vbox1;
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
        /// 
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChangePivot(object sender, EventArgs e)
        {            
            ChangePivotArgs args = new ChangePivotArgs(sender == rightbutton);

            ChangePivot.Invoke(((Button)sender).Name, args);
            OnInvokeUpdate(sender, EventArgs.Empty);
        }

        private void OnStoreData(object sender, EventArgs e)
        {
            if (StoreData != null)
            {
                StoreData.Invoke(sender, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ledger"></param>
        public void AddLedger(string ledger)
        {
            Ledger.AddText(ledger);

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

            leftbutton.Clicked -= OnChangePivot;
            rightbutton.Clicked -= OnChangePivot;
        }
    }

}
