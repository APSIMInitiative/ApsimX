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

        private ComboBox ledgerbox = null;
        private ComboBox rowbox = null;
        private ComboBox columnbox = null;
        private ComboBox expressionbox = null;
        private ComboBox valuebox = null;
        private ComboBox pivotbox = null;

        private Button leftbutton = null;
        private Button rightbutton = null;

        /// <summary>
        /// Custom gridview added post-reading the glade file
        /// </summary>
        public GridView gridview { get; set; } = null;      

        /// <summary>
        /// Public access to the current ledgerbox text
        /// </summary>
        public string Ledger
        {
            get
            {
                return ledgerbox.ActiveText;
            }
        }

        /// <summary>
        /// Public access to the current expressionbox text
        /// </summary>
        public string Expression
        {
            get
            {
                return expressionbox.ActiveText;
            }
        }

        /// <summary>
        /// Public access to the current valuebox text
        /// </summary>
        public string Value
        {
            get
            {
                return valuebox.ActiveText;
            }
        }

        /// <summary>
        /// Public access to the current rowbox text
        /// </summary>
        public string Row
        {
            get
            {
                return rowbox.ActiveText;
            }
        }

        /// <summary>
        /// Public access to the current columnbox text
        /// </summary>
        public string Column
        {
            get
            {
                return columnbox.ActiveText;
            }
        }

        /// <summary>
        /// Public access to the current pivotbox text
        /// </summary>
        public string Pivot
        {
            get
            {
                return pivotbox.ActiveText;
            }
        }

        public event EventHandler OnUpdateData;
        public event EventHandler<ChangePivotArgs> OnChangePivot;

        public class ChangePivotArgs : EventArgs
        {            
            public bool Increase { get; }

            public ChangePivotArgs(bool increase)
            {
                Increase = increase;
            }
        }

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

            ledgerbox = (ComboBox)builder.GetObject("ledgerbox");
            expressionbox = (ComboBox)builder.GetObject("expressionbox");
            valuebox = (ComboBox)builder.GetObject("valuebox");
            rowbox = (ComboBox)builder.GetObject("rowbox");
            columnbox = (ComboBox)builder.GetObject("columnbox");            
            pivotbox = (ComboBox)builder.GetObject("pivotbox");

            leftbutton = (Button)builder.GetObject("leftbutton");
            rightbutton = (Button)builder.GetObject("rightbutton");

            // Add text renderers to the boxes
            AddRenderer(ledgerbox);
            AddRenderer(expressionbox);
            AddRenderer(valuebox);
            AddRenderer(rowbox);
            AddRenderer(columnbox);            
            AddRenderer(pivotbox);

            // Add options to the ComboBoxes           
            expressionbox.AppendText("Sum");
            expressionbox.AppendText("Average");
            expressionbox.AppendText("Max");
            expressionbox.AppendText("Min");
            expressionbox.Active = 0;

            valuebox.AppendText("Gain");
            valuebox.AppendText("Loss");
            valuebox.Active = 0;

            AddOptions(rowbox);
            rowbox.Active = 4;

            AddOptions(columnbox);
            columnbox.Active = 5;

            AddOptions(pivotbox);
            pivotbox.Active = 7;

            // Update table when box options are changed
            ledgerbox.Changed += InvokeUpdate;
            rowbox.Changed += InvokeUpdate;
            columnbox.Changed += InvokeUpdate;
            expressionbox.Changed += InvokeUpdate;
            valuebox.Changed += InvokeUpdate;
            pivotbox.Changed += InvokeUpdate;

            // 
            leftbutton.Clicked += ChangePivot;
            rightbutton.Clicked += ChangePivot;

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
        private void AddRenderer(ComboBox combo)
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
        private static void AddOptions(ComboBox combo)
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

            foreach (string option in options) combo.AppendText(option);
        }

        private void BlockOptions(object sender, EventArgs e)
        {
            //int pos = 0;
            int active = ((ComboBox)sender).Active;

            if (sender != rowbox)
            {
                //rowbox.Cells[pos].Sensitive = true;
                rowbox.Cells[active].Sensitive = false;
            }

            if (sender != columnbox)
            {
                //rowbox.Cells[pos].Sensitive = true;
                //throw new Exception(active.ToString());
                columnbox.Cells[active].Sensitive = false;
            }

            if (sender != pivotbox)
            {
                //pivotbox.Cells[pos].Sensitive = true;
                pivotbox.Cells[active].Sensitive = false;
            }

            OnUpdateData.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InvokeUpdate(object sender, EventArgs e)
        {
            OnUpdateData.Invoke(sender, EventArgs.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangePivot(object sender, EventArgs e)
        {            
            ChangePivotArgs args = new ChangePivotArgs(sender == rightbutton);

            OnChangePivot.Invoke(((Button)sender).Name, args);
            InvokeUpdate(sender, EventArgs.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ledger"></param>
        public void AddLedger(string ledger)
        {
            ledgerbox.AppendText(ledger);

            if (ledgerbox.Active < 0) ledgerbox.Active = 0;
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
            ledgerbox.Changed -= InvokeUpdate;
            rowbox.Changed -= InvokeUpdate;
            columnbox.Changed -= InvokeUpdate;
            expressionbox.Changed -= InvokeUpdate;
            valuebox.Changed -= InvokeUpdate;
            pivotbox.Changed -= InvokeUpdate;

            leftbutton.Clicked -= ChangePivot;
            rightbutton.Clicked -= ChangePivot;
        }
    }

}
