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
        }

        /// <summary>
        /// Invoked when the user changes the series type
        /// </summary>
        public event EventHandler SeriesTypeChanged;

        /// <summary>
        /// Invoked when the user changes the series line type
        /// </summary>
        public event EventHandler SeriesLineTypeChanged;

        /// <summary>
        /// Invoked when the user changes the series marker type
        /// </summary>
        public event EventHandler SeriesMarkerTypeChanged;

        /// <summary>
        /// Invoked when the user changes the color
        /// </summary>
        public event EventHandler ColourChanged;

        /// <summary>
        /// Invoked when the user changes the overall regression field
        /// </summary>
        public event EventHandler OverallRegressionChanged;

        /// <summary>
        /// Invoked when the user changes the regression field
        /// </summary>
        public event EventHandler RegressionChanged;

        /// <summary>
        /// Invoked when the user changes the x on top field
        /// </summary>
        public event EventHandler XOnTopChanged;

        /// <summary>
        /// Invoked when the user changes the y on right field
        /// </summary>
        public event EventHandler YOnRightChanged;

        /// <summary>
        /// Invoked when the user changes the cumulative field
        /// </summary>
        public event EventHandler CumulativeChanged;

        /// <summary>
        /// Invoked when the user changes the x
        /// </summary>
        public event EventHandler XChanged;

        /// <summary>
        /// Invoked when the user changes the y
        /// </summary>
        public event EventHandler YChanged;

        /// <summary>
        /// Invoked when the user changes the x2
        /// </summary>
        public event EventHandler X2Changed;

        /// <summary>
        /// Invoked when the user changes the y2
        /// </summary>
        public event EventHandler Y2Changed;

        /// <summary>
        /// Invoked when the user changes the data source
        /// </summary>
        public event EventHandler DataSourceChanged;

        /// <summary>
        /// Invoked when the user changes the show in legend
        /// </summary>
        public event EventHandler ShowInLegendChanged;

        /// <summary>
        /// Invoked when the user changes the 'include in documentation' 
        /// </summary>
        public event EventHandler IncludeInDocumentationChanged;

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
                if (comboBox2.Items.IndexOf(value) != -1)
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
        /// Gets or sets a value indicating whether overall regression is turned on.
        /// </summary>
        public bool OverallRegression
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
        /// Gets or sets a value indicating whether regression is turned on.
        /// </summary>
        public bool Regression
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
        /// Gets or sets a value indicating whether the series is cumulative.
        /// </summary>
        /// <value><c>true</c> if cumulative; otherwise, <c>false</c>.</value>
        public bool Cumulative
        {
            get
            {
                return this.cumulativeCheckBox.Checked;
            }

            set
            {
                this.cumulativeCheckBox.Checked = value;
            }
        }

        /// <summary>
        /// Gets or set the show in legend checkbox
        /// </summary>
        public bool ShowInLegend
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
                return xComboBox.Text;
            }

            set
            {
                xComboBox.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the y variable name
        /// </summary>
        public string Y
        {
            get
            {
                return yComboBox.Text;
            }

            set
            {
                yComboBox.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the second x variable name
        /// </summary>
        public string X2
        {
            get
            {
                return x2ComboBox.Text;
            }

            set
            {
                x2ComboBox.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the second y variable name
        /// </summary>
        public string Y2
        {
            get
            {
                return y2ComboBox.Text;
            }

            set
            {
                y2ComboBox.Text = value;
            }
        }

        /// <summary>
        /// Show the x2 an y2 fields?
        /// </summary>
        /// <param name="show">Indicates whether the fields should be shown</param>
        public void ShowX2Y2(bool show)
        {
            this.x2ComboBox.Visible = show;
            this.y2ComboBox.Visible = show;
            this.label4.Visible = show;
            this.label5.Visible = show;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the graph should be included in documentation.
        /// </summary>
        public bool IncludeInDocumentation
        {
            get
            {
                return checkBox6.Checked;
            }

            set
            {
                checkBox6.Checked = value;
            }
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
        /// Gets or sets a list of field names 
        /// </summary>
        /// <param name="fieldNames">The available field names</param>
        public void SetFieldNames(string[] fieldNames)
        {
            xComboBox.Items.Clear();
            xComboBox.Items.AddRange(fieldNames);
            yComboBox.Items.Clear();
            yComboBox.Items.AddRange(fieldNames);
            x2ComboBox.Items.Clear();
            x2ComboBox.Items.AddRange(fieldNames);
            y2ComboBox.Items.Clear();
            y2ComboBox.Items.AddRange(fieldNames);
        }

        /// <summary>
        /// Scatter type has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnComboBox2Changed(object sender, EventArgs e)
        {
            if (SeriesTypeChanged != null)
                SeriesTypeChanged.Invoke(sender, e);
        }

        /// <summary>
        /// Line type has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnComboBox3Changed(object sender, EventArgs e)
        {
            if (SeriesLineTypeChanged != null)
                SeriesLineTypeChanged.Invoke(sender, e);
        }

        /// <summary>
        /// Marker type has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnComboBox4Changed(object sender, EventArgs e)
        {
            if (SeriesMarkerTypeChanged != null)
                SeriesMarkerTypeChanged.Invoke(sender, e);
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
                if (ColourChanged != null)
                    ColourChanged.Invoke(sender, e);
            }
        }

        /// <summary>
        /// Overall regression has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckBox3Changed(object sender, EventArgs e)
        {
            if (OverallRegressionChanged != null)
                OverallRegressionChanged.Invoke(sender, e);
        }

        /// <summary>
        /// Regression has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckBox4Changed(object sender, EventArgs e)
        {
            if (RegressionChanged != null)
                RegressionChanged.Invoke(sender, e);
        }

        /// <summary>
        /// X on top has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckBox1Changed(object sender, EventArgs e)
        {
            if (XOnTopChanged != null)
                XOnTopChanged.Invoke(sender, e);
        }

        /// <summary>
        /// Y on right has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckBox2Changed(object sender, EventArgs e)
        {
            if (YOnRightChanged != null)
                YOnRightChanged.Invoke(sender, e);
        }

        /// <summary>
        /// Data source has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnComboBox1Changed(object sender, EventArgs e)
        {
            if (DataSourceChanged != null)
                DataSourceChanged.Invoke(sender, e);
        }

        /// <summary>
        /// The show in legend checkbox has been clicked.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckBox5Changed(object sender, EventArgs e)
        {
            if (ShowInLegendChanged != null)
            {
                ShowInLegendChanged.Invoke(sender, e);
            }
        }

        /// <summary>
        /// X value has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void xComboBox_TextChanged(object sender, EventArgs e)
        {
            if (XChanged != null)
            {
                XChanged.Invoke(sender, e);
            }
        }

        /// <summary>
        /// Y value has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void yComboBox_TextChanged(object sender, EventArgs e)
        {
            if (YChanged != null)
            {
                YChanged.Invoke(sender, e);
            }

        }

        /// <summary>
        /// X2 value has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void x2ComboBox_TextChanged(object sender, EventArgs e)
        {
            if (X2Changed != null)
            {
                X2Changed.Invoke(sender, e);
            }

        }

        /// <summary>
        /// Y2 value has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void y2ComboBox_TextChanged(object sender, EventArgs e)
        {
            if (Y2Changed != null)
            {
                Y2Changed.Invoke(sender, e);
            }
        }

        /// <summary>Handles the CheckedChanged event of the cumulativeCheckBox control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnCheckedChanged(object sender, EventArgs e)
        {
            if (this.CumulativeChanged != null)
                this.CumulativeChanged(this, e);
        }

        /// <summary>
        /// Called when include in documentation changed by the user.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnIncludeInDocumentationChanged(object sender, EventArgs e)
        {
            if (this.IncludeInDocumentationChanged != null)
                this.IncludeInDocumentationChanged.Invoke(this, e);
        }
    }
}
