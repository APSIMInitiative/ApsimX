using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UserInterface.Views
{
    interface IPredictedObservedView
    {
        event EventHandler PredictedTableNameChanged;
        event EventHandler ObservedTableNameChanged;


        /// <summary>
        /// Property to provide access to the grid.
        /// </summary>
        IGridView GridView { get; }

        /// <summary>
        /// Get or set the observed table name
        /// </summary>
        string ObservedTableName { get; set; }

        /// <summary>
        /// Get or set the predicted table name
        /// </summary>
        string PredictedTableName { get; set; }

        /// <summary>
        /// Get or set the table names.
        /// </summary>
        string[] TableNames { get; set; }
    }

    public partial class PredictedObservedView : UserControl, Views.IPredictedObservedView
    {
        public event EventHandler PredictedTableNameChanged;
        public event EventHandler ObservedTableNameChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public PredictedObservedView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Property to provide access to the grid.
        /// </summary>
        public IGridView GridView { get { return Grid; } }

        /// <summary>
        /// Get or set the predicted table name
        /// </summary>
        public string PredictedTableName
        {
            get
            {
                return PredictedCombo.Text;
            }
            set
            {
                if (PredictedCombo.Text != value)
                    PredictedCombo.Text = value;
            }
        }

        /// <summary>
        /// Get or set the observed table name
        /// </summary>
        public string ObservedTableName
        {
            get
            {
                return ObservedCombo.Text;
            }
            set
            {
                if (ObservedCombo.Text != value)
                    ObservedCombo.Text = value;
            }
        }

        /// <summary>
        /// Get or set the table names.
        /// </summary>
        public string[] TableNames
        {
            get
            {
                return ObservedCombo.Items.OfType<string>().ToArray();
            }
            set
            {
                ObservedCombo.Items.Clear();
                PredictedCombo.Items.Clear();
                ObservedCombo.Items.AddRange(value);
                PredictedCombo.Items.AddRange(value);
            }
        }

        private void OnPredictedComboTextChanged(object sender, EventArgs e)
        {
            if (PredictedTableNameChanged != null)
                PredictedTableNameChanged.Invoke(this, new EventArgs());
        }

        private void OnObservedComboTextChanged(object sender, EventArgs e)
        {
            if (ObservedTableNameChanged != null)
                ObservedTableNameChanged.Invoke(this, new EventArgs());
        }
    }

}
