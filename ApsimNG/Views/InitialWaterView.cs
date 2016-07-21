// -----------------------------------------------------------------------
// <copyright file="InitialWaterView.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    ///using System.Windows.Forms;
    using Interfaces;
    using APSIM.Shared.Utilities;
    using Gtk;
    using Glade;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public class InitialWaterView : ViewBase, IInitialWaterView
    {
        [Widget]
        private HPaned hpaned1 = null;
        [Widget]
        private SpinButton spinbutton1 = null;
        [Widget]
        private Entry entry1 = null;
        [Widget]
        private Entry entry2 = null;
        [Widget]
        private RadioButton radiobutton1 = null;
        [Widget]
        private RadioButton radiobutton2 = null;
        [Widget]
        private ComboBox combobox1 = null;
        private GraphView graphView1 = null;
        private ListStore comboModel = new ListStore(typeof(string));
        private CellRendererText comboRender = new CellRendererText();

        /// <summary>
        /// Initializes a new instance of the <see cref="InitialWaterView" /> class.
        /// </summary>
        public InitialWaterView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.InitialWaterView.glade", "hpaned1");
            gxml.Autoconnect(this);
            _mainWidget = hpaned1;
            combobox1.PackStart(comboRender, false);
            combobox1.AddAttribute(comboRender, "text", 0);
            combobox1.Model = comboModel;
            graphView1 = new GraphView(this);
            hpaned1.Pack2(graphView1.MainWidget, true, true);
            entry1.Changed += OnTextBox1TextChanged;
            entry2.Changed += OnTextBox2TextChanged;
            radiobutton1.Toggled += OnRadioButton1CheckedChanged;
            spinbutton1.Changed += OnNumericUpDown1ValueChanged;
            combobox1.Changed += OnComboBox1SelectedValueChanged;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            entry1.Changed -= OnTextBox1TextChanged;
            entry2.Changed -= OnTextBox2TextChanged;
            radiobutton1.Toggled -= OnRadioButton1CheckedChanged;
            spinbutton1.Changed -= OnNumericUpDown1ValueChanged;
            combobox1.Changed -= OnComboBox1SelectedValueChanged;
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
                return spinbutton1.ValueAsInt;
            }

            set
            {
                this.spinbutton1.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether initial water should be filled from the top.
        /// </summary>
        public bool FilledFromTop
        {
            get
            {
                return this.radiobutton1.Active;
            }

            set
            {
                this.radiobutton1.Active = value;
                this.radiobutton2.Active = !value;
            }
        }

        /// <summary>
        /// Gets or sets the depth of wet soil.
        /// </summary>
        public int DepthOfWetSoil
        {
            get
            {
                return Convert.ToInt32(this.entry1.Text);
            }

            set
            {
                if (value == int.MinValue)
                    this.entry1.Text = "";
                else
                    this.entry1.Text = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the PAW (mm)
        /// </summary>
        public int PAW
        {
            get
            {
                int result = 0;
                try
                {
                    result = Convert.ToInt32(this.entry2.Text);
                }
                catch (Exception)
                {  // If there are any errors, return 0
                }
                return result;
            }

            set
            {
                this.entry2.Text = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the crop that initial was is relative to
        /// </summary>
        public string RelativeTo
        {
            get
            {
                TreeIter iter;
                if (combobox1.GetActiveIter(out iter))
                    return (string)combobox1.Model.GetValue(iter, 0);
                else
                    return "";
            }

            set
            {
                TreeIter iter;
                if (comboModel.GetIterFirst(out iter))
                {
                    string entry = (string)comboModel.GetValue(iter, 0);
                    while (!entry.Equals(value, StringComparison.InvariantCultureIgnoreCase) && comboModel.IterNext(ref iter)) // Should the text matching be case-insensitive?
                        entry = (string)comboModel.GetValue(iter, 0);
                    if (entry == value)
                        combobox1.SetActiveIter(iter);
                    else // Could not find a matching entry
                        combobox1.Active = -1;
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of crops for the relative to field
        /// </summary>
        public string[] RelativeToCrops
        {
            get
            {
                int nNames = comboModel.IterNChildren();
                string[] result = new string[nNames];
                TreeIter iter;
                int i = 0;
                if (combobox1.GetActiveIter(out iter))
                    do
                        result[i++] = (string)comboModel.GetValue(iter, 0);
                    while (comboModel.IterNext(ref iter) && i < nNames);
                return result;
            }
            set
            {
                comboModel.Clear();
                foreach (string text in value)
                    comboModel.AppendValues(text);
                if (comboModel.IterNChildren() > 0)
                    combobox1.Active = 0;
                else
                    combobox1.Active = -1;
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
