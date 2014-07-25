// -----------------------------------------------------------------------
// <copyright file="SeriesEditorView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using Interfaces;

    /// <summary>
    /// This view allows a single series to be edited.
    /// </summary>
    public partial class SeriesEditorView : UserControl, ISeriesEditorView
    {
        /// <summary>
        /// The color of the selected x y text boxes.
        /// </summary>
        private Color selectedColour = Color.Yellow;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesEditorView" /> class.
        /// </summary>
        public SeriesEditorView()
        {
            InitializeComponent();

            // Colour the x edit box.
            textBox1.BackColor = selectedColour;
        }

        /// <summary>
        /// Invoked when the user changes the series type
        /// </summary>
        public event EventHandler OnSeriesTypeChanged;

        /// <summary>
        /// Invoked when the user changes the series line type
        /// </summary>
        public event EventHandler OnSeriesLineTypeChanged;

        /// <summary>
        /// Invoked when the user changes the series marker type
        /// </summary>
        public event EventHandler OnSeriesMarkerTypeChanged;

        /// <summary>
        /// Invoked when the user changes the color
        /// </summary>
        public event EventHandler OnColourChanged;

        /// <summary>
        /// Invoked when the user changes the regression field
        /// </summary>
        public event EventHandler OnRegressionChanged;

        /// <summary>
        /// Invoked when the user changes the x on top field
        /// </summary>
        public event EventHandler OnXOnTopChanged;

        /// <summary>
        /// Invoked when the user changes the y on right field
        /// </summary>
        public event EventHandler OnYOnRightChanged;

        /// <summary>
        /// Invoked when the user changes the x
        /// </summary>
        public event EventHandler OnXChanged;

        /// <summary>
        /// Invoked when the user changes the y
        /// </summary>
        public event EventHandler OnYChanged;

        /// <summary>
        /// Invoked when the user changes the x2
        /// </summary>
        public event EventHandler OnX2Changed;

        /// <summary>
        /// Invoked when the user changes the y2
        /// </summary>
        public event EventHandler OnY2Changed;

        /// <summary>
        /// Invoked when the user changes the data source
        /// </summary>
        public event EventHandler OnDataSourceChanged;

        /// <summary>
        /// Invoked when the user changes the show in legend
        /// </summary>
        public event EventHandler OnShowInLegendChanged;

        /// <summary>
        /// Invoked when the user changes the separate series
        /// </summary>
        public event EventHandler OnSeparateSeriesChanged;

        /// <summary>
        /// Gets or sets the series type
        /// </summary>
        public string SeriesType
        {
            get
            {
                return comboBox2.Text;
            }

            set
            {
                if (comboBox1.Items.IndexOf(value) != -1)
                {
                    comboBox2.Text = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the series line type
        /// </summary>
        public string SeriesLineType
        {
            get
            {
                return comboBox3.Text;
            }

            set
            {
                comboBox3.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the series marker type
        /// </summary>
        public string SeriesMarkerType
        {
            get
            {
                return comboBox4.Text;
            }

            set
            {
                comboBox4.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the series color.
        /// </summary>
        public Color Colour
        {
            get
            {
                return button1.BackColor;
            }

            set
            {
                button1.BackColor = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether regression is turned on.
        /// </summary>
        public bool Regression
        {
            get
            {
                return checkBox3.Checked;
            }

            set
            {
                checkBox3.Checked = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether x is on top.
        /// </summary>
        public bool XOnTop
        {
            get
            {
                return checkBox1.Checked;
            }

            set
            {
                checkBox1.Checked = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether y is on right.
        /// </summary>
        public bool YOnRight
        {
            get
            {
                return checkBox2.Checked;
            }

            set
            {
                checkBox2.Checked = value;
            }
        }

        /// <summary>
        /// Gets or set the show in legend checkbox
        /// </summary>
        public bool ShowInLegend
        {
            get
            {
                return checkBox4.Checked;
            }

            set
            {
                checkBox4.Checked = value;
            }
        }

        /// <summary>
        /// Gets or set the separate series checkbox
        /// </summary>
        public bool SeparateSeries
        {
            get
            {
                return checkBox5.Checked;
            }

            set
            {
                checkBox5.Checked = value;
            }
        }

        /// <summary>
        /// Gets or sets the x variable name
        /// </summary>
        public string X
        {
            get
            {
                return textBox1.Text;
            }

            set
            {
                textBox1.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the y variable name
        /// </summary>
        public string Y
        {
            get
            {
                return textBox2.Text;
            }

            set
            {
                textBox2.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the second x variable name
        /// </summary>
        public string X2
        {
            get
            {
                return textBox3.Text;
            }

            set
            {
                textBox3.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the second y variable name
        /// </summary>
        public string Y2
        {
            get
            {
                return textBox4.Text;
            }

            set
            {
                textBox4.Text = value;
            }
        }

        /// <summary>
        /// Show the x2 an y2 fields?
        /// </summary>
        /// <param name="show">Indicates whether the fields should be shown</param>
        public void ShowX2Y2(bool show)
        {
            this.textBox3.Visible = show;
            this.textBox4.Visible = show;
            this.label4.Visible = show;
            this.label5.Visible = show;
        }

        /// <summary>
        /// Sets the list of available data sources.
        /// </summary>
        /// <param name="data">The available data sources</param>
        public void SetDataSources(string[] dataSources)
        {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(dataSources);
        }

        /// <summary>
        /// Gets or sets the selected data source name.
        /// </summary>
        public string DataSource
        {
            get
            {
                return comboBox1.Text;
            }

            set
            {
                comboBox1.Text = value;
            }
        }

        /// <summary>
        /// Provides data for the currently selected data source.
        /// </summary>
        /// <param name="data">The data to show</param>
        public void SetData(DataTable data)
        {
            dataGrid.DataSource = data;
        }

        /// <summary>
        /// Scatter type has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnComboBox2Changed(object sender, EventArgs e)
        {
            if (OnSeriesTypeChanged != null)
                OnSeriesTypeChanged.Invoke(sender, e);
        }

        /// <summary>
        /// Line type has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnComboBox3Changed(object sender, EventArgs e)
        {
            if (OnSeriesLineTypeChanged != null)
                OnSeriesLineTypeChanged.Invoke(sender, e);
        }

        /// <summary>
        /// Marker type has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnComboBox4Changed(object sender, EventArgs e)
        {
            if (OnSeriesMarkerTypeChanged != null)
                OnSeriesMarkerTypeChanged.Invoke(sender, e);
        }

        /// <summary>
        /// Colour has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnButton1Click(object sender, EventArgs e)
        {
            colorDialog1.Color = button1.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                button1.BackColor = colorDialog1.Color;
                if (OnColourChanged != null)
                    OnColourChanged.Invoke(sender, e);
            }
        }

        /// <summary>
        /// Regression has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckBox3Changed(object sender, EventArgs e)
        {
            if (OnRegressionChanged != null)
                OnRegressionChanged.Invoke(sender, e);
        }

        /// <summary>
        /// X on top has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckBox1Changed(object sender, EventArgs e)
        {
            if (OnXOnTopChanged != null)
                OnXOnTopChanged.Invoke(sender, e);
        }

        /// <summary>
        /// Y on right has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckBox2Changed(object sender, EventArgs e)
        {
            if (OnYOnRightChanged != null)
                OnYOnRightChanged.Invoke(sender, e);
        }

        /// <summary>
        /// Data source has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnComboBox1Changed(object sender, EventArgs e)
        {
            if (OnDataSourceChanged != null)
                OnDataSourceChanged.Invoke(sender, e);
        }

        /// <summary>
        /// The x edit box has been clicked.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnTextBox1Click(object sender, EventArgs e)
        {
            textBox1.BackColor = selectedColour;
            textBox2.BackColor = SystemColors.Window;
            textBox3.BackColor = SystemColors.Window;
            textBox4.BackColor = SystemColors.Window;
        }

        /// <summary>
        /// The y edit box has been clicked.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnTextBox2Click(object sender, EventArgs e)
        {
            textBox1.BackColor = SystemColors.Window;
            textBox2.BackColor = selectedColour;
            textBox3.BackColor = SystemColors.Window;
            textBox4.BackColor = SystemColors.Window;

        }

        /// <summary>
        /// The x2 edit box has been clicked.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnTextBox3Click(object sender, EventArgs e)
        {
            textBox1.BackColor = SystemColors.Window;
            textBox2.BackColor = SystemColors.Window;
            textBox3.BackColor = selectedColour;
            textBox4.BackColor = SystemColors.Window;

        }

        /// <summary>
        /// The y2 edit box has been clicked.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnTextBox4Click(object sender, EventArgs e)
        {
            textBox1.BackColor = SystemColors.Window;
            textBox2.BackColor = SystemColors.Window;
            textBox3.BackColor = SystemColors.Window;
            textBox4.BackColor = selectedColour;

        }

        /// <summary>
        /// The data grid has had one of its column headers clicked.
        /// </summary>
        /// <param name="headerText">The name of the column that was clicked.</param>
        private void OnDataGridColumnHeaderClicked(string headerText)
        {
            if (textBox1.BackColor == selectedColour)
            {
                textBox1.Text = headerText;
                OnTextBox2Click(null, null);
            }
            else if (textBox2.BackColor == selectedColour)
            {
                textBox2.Text = headerText;
            }
            else if (textBox3.BackColor == selectedColour)
            {
                textBox3.Text = headerText;
            }
            else if (textBox4.BackColor == selectedColour)
            {
                textBox4.Text = headerText;
            }

        }

        /// <summary>
        /// The show in legend checkbox has been clicked.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckBox4Changed(object sender, EventArgs e)
        {
            if (OnShowInLegendChanged != null)
            {
                OnShowInLegendChanged.Invoke(sender, e);
            }
        }

        /// <summary>
        /// The separate series checkbox has been clicked.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckBox5Changed(object sender, EventArgs e)
        {
            if (OnSeparateSeriesChanged != null)
            {
                OnSeparateSeriesChanged.Invoke(sender, e);
            }
        }

        /// <summary>
        /// X value has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (OnXChanged != null)
            {
                OnXChanged.Invoke(sender, e);
            }
        }

        /// <summary>
        /// Y value has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (OnYChanged != null)
            {
                OnYChanged.Invoke(sender, e);
            }

        }

        /// <summary>
        /// X2 value has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (OnX2Changed != null)
            {
                OnX2Changed.Invoke(sender, e);
            }

        }

        /// <summary>
        /// Y2 value has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (OnY2Changed != null)
            {
                OnY2Changed.Invoke(sender, e);
            }
        }
    }
}
