using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    interface IPredictedObservedView
    {
        event EventHandler PredictedTableNameChanged;
        event EventHandler ObservedTableNameChanged;
        event EventHandler FieldNameChanged;

        /// <summary>
        /// Get or set the observed table name
        /// </summary>
        string ObservedTableName { get; set; }

        /// <summary>
        /// Get or set the predicted table name
        /// </summary>
        string PredictedTableName { get; set; }

        /// <summary>
        /// Gets or sets the field name
        /// </summary>
        string FieldName { get; set; }

        /// <summary>
        /// Get or set the table names.
        /// </summary>
        string[] TableNames { get; set; }

        /// <summary>
        /// Gets or sets the list of possible field names.
        /// </summary>
        string[] FieldNames { get; set; }
    }

    public partial class PredictedObservedView : UserControl, Views.IPredictedObservedView
    {
        public event EventHandler PredictedTableNameChanged;
        public event EventHandler ObservedTableNameChanged;
        public event EventHandler FieldNameChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public PredictedObservedView()
        {
            InitializeComponent();
        }

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
        /// Gets or sets the field name
        /// </summary>
        public string FieldName
        {
            get
            {
                return ColumnNameCombo.Text;
            }

            set
            {
                ColumnNameCombo.Text = value;
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

        /// <summary>
        /// Gets or sets the list of possible field names.
        /// </summary>
        public string[] FieldNames
        {
            get
            {
                return ColumnNameCombo.Items.OfType<string>().ToArray();
            }
            set
            {
                ColumnNameCombo.Items.Clear();
                ColumnNameCombo.Items.AddRange(value);
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

        private void ColumnNameCombo_TextChanged(object sender, EventArgs e)
        {
            if (FieldNameChanged != null)
                FieldNameChanged.Invoke(this, new EventArgs());
        }
    }

}
