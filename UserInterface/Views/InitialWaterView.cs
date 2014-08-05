// -----------------------------------------------------------------------
// <copyright file="InitialWaterView.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Windows.Forms;
    using Interfaces;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public partial class InitialWaterView : UserControl, IInitialWaterView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InitialWaterView" /> class.
        /// </summary>
        public InitialWaterView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the user changes the percent full edit box.
        /// </summary>
        public event EventHandler OnPercentFullChanged;

        /// <summary>
        /// Invoked when the user changes the FilledFromTop option
        /// </summary>
        public event EventHandler OnFilledFromTopChanged;

        /// <summary>
        /// Invoked when the user changes the depth of wet soil
        /// </summary>
        public event EventHandler OnDepthWetSoilChanged;

        /// <summary>
        /// Invoked when the user changes PAW
        /// </summary>
        public event EventHandler OnPAWChanged;

        /// <summary>
        /// Invoked when the user changes the relative to field.
        /// </summary>
        public event EventHandler OnRelativeToChanged;
        
        /// <summary>
        /// Gets or sets the percent full amount.
        /// </summary>
        public int PercentFull
        {
            get
            {
                return Convert.ToInt32(this.numericUpDown1.Value);
            }

            set
            {
                this.numericUpDown1.Value = Convert.ToInt32(Utility.Math.Bound(value, 0, 100));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether initial water should be filled from the top.
        /// </summary>
        public bool FilledFromTop
        {
            get
            {
                return this.radioButton1.Checked;
            }

            set
            {
                this.radioButton1.Checked = value;
                this.radioButton2.Checked = !value;
            }
        }

        /// <summary>
        /// Gets or sets the depth of wet soil.
        /// </summary>
        public int DepthOfWetSoil
        {
            get
            {
                return Convert.ToInt32(this.textBox1.Text);
            }

            set
            {
                if (value == int.MinValue)
                    this.textBox1.Text = "";
                else
                    this.textBox1.Text = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the PAW (mm)
        /// </summary>
        public int PAW
        {
            get
            {
                return Convert.ToInt32(this.textBox2.Text);
            }

            set
            {
                this.textBox2.Text = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the crop that initial was is relative to
        /// </summary>
        public string RelativeTo
        {
            get
            {
                return this.comboBox1.Text;
            }

            set
            {
                this.comboBox1.SelectedItem = value;
            }
        }

        /// <summary>
        /// Gets or sets the list of crops for the relative to field
        /// </summary>
        public string[] RelativeToCrops
        {
            get
            {
                string[] items = new string[this.comboBox1.Items.Count];
                for (int i = 0; i < this.comboBox1.Items.Count; i++)
                {
                    items[i] = this.comboBox1.Items[i].ToString();
                }

                return items;
            }

            set
            {
                this.comboBox1.Items.AddRange(value);
            }
        }

        /// <summary>
        /// Gets the initial water graph.
        /// </summary>
        public Views.GraphView Graph
        {
            get
            {
                return this.graphView1;
            }
        }

        /// <summary>
        /// The numeric up/down value has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnNumericUpDown1ValueChanged(object sender, EventArgs e)
        {
            if (this.OnPercentFullChanged != null)
                this.OnPercentFullChanged.Invoke(sender, e);
        }

        /// <summary>
        /// The text box 2 value has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnTextBox2TextChanged(object sender, EventArgs e)
        {
            if (this.OnPAWChanged != null)
                this.OnPAWChanged.Invoke(sender, e);
        }

        /// <summary>
        /// The radio button 1value has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnRadioButton1CheckedChanged(object sender, EventArgs e)
        {
            if (this.OnFilledFromTopChanged != null)
                this.OnFilledFromTopChanged(sender, e);
        }

        /// <summary>
        /// The text box 1 value has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnTextBox1TextChanged(object sender, EventArgs e)
        {
            if (OnDepthWetSoilChanged != null)
                OnDepthWetSoilChanged.Invoke(sender, e);
        }

        /// <summary>
        /// The combo box value has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnComboBox1SelectedValueChanged(object sender, EventArgs e)
        {
            if (OnRelativeToChanged != null)
                OnRelativeToChanged.Invoke(sender, e);
        }
    }
}
