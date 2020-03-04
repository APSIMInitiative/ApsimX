namespace UserInterface.Views
{
    using System;
    using Interfaces;
    using Gtk;
    using System.Globalization;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public class InitialWaterView : ViewBase, IInitialWaterView
    {
        private HPaned hpaned1 = null;
        private SpinButton spinbutton1 = null;
        private Entry entry1 = null;
        private Entry entry2 = null;
        private Frame frame1 = null;
        private Frame frame2 = null;
        private RadioButton frameRadio1 = null;
        private RadioButton frameRadio2 = null;
        private RadioButton radiobutton1 = null;
        private RadioButton radiobutton2 = null;
        private ComboBox combobox1 = null;
        private GraphView graphView1 = null;
        private ListStore comboModel = new ListStore(typeof(string));
        private CellRendererText comboRender = new CellRendererText();

        /// <summary>
        /// Initializes a new instance of the <see cref="InitialWaterView" /> class.
        /// </summary>
        public InitialWaterView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.InitialWaterView.glade");
            hpaned1 = (HPaned)builder.GetObject("hpaned1");
            spinbutton1 = (SpinButton)builder.GetObject("spinbutton1");
            entry1 = (Entry)builder.GetObject("entry1");
            entry2 = (Entry)builder.GetObject("entry2");
            frame1 = (Frame)builder.GetObject("frame1");
            frame2 = (Frame)builder.GetObject("frame2");
            radiobutton1 = (RadioButton)builder.GetObject("radiobutton1");
            radiobutton2 = (RadioButton)builder.GetObject("radiobutton2");
            combobox1 = (ComboBox)builder.GetObject("combobox1");
            mainWidget = hpaned1;
            combobox1.PackStart(comboRender, false);
            combobox1.AddAttribute(comboRender, "text", 0);
            combobox1.Model = comboModel;
            frameRadio1 = new RadioButton(frame1.Label);
            frameRadio1.Active = true;
            frame1.LabelWidget = frameRadio1;
            frameRadio2 = new RadioButton(frameRadio1, frame2.Label);
            frameRadio2.Active = false;
            frame2.LabelWidget = frameRadio2;
            graphView1 = new GraphView(this);
            hpaned1.Pack2(graphView1.MainWidget, true, true);
            entry1.Changed += OnTextBox1TextChanged;
            entry2.Changed += OnTextBox2TextChanged;
            radiobutton1.Toggled += OnRadioButton1CheckedChanged;
            spinbutton1.Changed += OnNumericUpDown1ValueChanged;
            combobox1.Changed += OnComboBox1SelectedValueChanged;
            frameRadio1.Toggled += FrameRadio_Toggled;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                entry1.Changed -= OnTextBox1TextChanged;
                entry2.Changed -= OnTextBox2TextChanged;
                radiobutton1.Toggled -= OnRadioButton1CheckedChanged;
                spinbutton1.Changed -= OnNumericUpDown1ValueChanged;
                combobox1.Changed -= OnComboBox1SelectedValueChanged;
                frameRadio1.Toggled -= FrameRadio_Toggled;
                comboModel.Dispose();
                comboRender.Destroy();
                graphView1.MainWidget.Destroy();
                graphView1 = null;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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
        /// Invoked when the user changes the way starting water is specified
        /// </summary>
        public event EventHandler OnSpecifierChanged;
       
        /// <summary>
        /// Gets or sets the percent full amount.
        /// </summary>
        public int PercentFull
        {
            get
            {
                return this.spinbutton1.ValueAsInt;
            }

            set
            {
                this.spinbutton1.Changed -= OnNumericUpDown1ValueChanged;
                this.spinbutton1.Value = value;
                this.spinbutton1.Changed += OnNumericUpDown1ValueChanged;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether starting water is specified by the depth of
        /// wet soil. If not, then fraction full will be used
        /// </summary>
        public bool FilledByDepth
        {
            get
            {
                return frameRadio2.Active;
            }
            set
            {
                frameRadio2.Active = value;
                frameRadio1.Active = !value;
                frame1.Child.Sensitive = frameRadio1.Active;
                frame2.Child.Sensitive = frameRadio2.Active;
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
                if (!frameRadio2.Active)
                    return int.MinValue;
                else
                {
                    int result = 0;
                    try
                    {
                        result = Convert.ToInt32(this.entry1.Text, CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {  // If there are any errors, return 0
                    }
                    return result;
                }
            }

            set
            {
                if (value == int.MinValue)
                    this.entry1.Text = "";
                else if (!(value == 0 && String.IsNullOrEmpty(this.entry1.Text)))
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
                    result = Convert.ToInt32(this.entry2.Text, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {  // If there are any errors, return 0
                }
                return result;
            }

            set
            {
                if (!(value == 0 && String.IsNullOrEmpty(this.entry2.Text)))
                {
                    entry2.Changed -= OnTextBox2TextChanged;
                    this.entry2.Text = value.ToString();
                    entry2.Changed += OnTextBox2TextChanged;
                }
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

        private bool inToggle = false;

        private void FrameRadio_Toggled(object sender, EventArgs e)
        {
            try
            {
                if (!inToggle)
                {
                    inToggle = true;
                    frame1.Child.Sensitive = frameRadio1.Active;
                    frame2.Child.Sensitive = frameRadio2.Active;
                    if (OnSpecifierChanged != null)
                        OnSpecifierChanged.Invoke(sender, e);
                    inToggle = false;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The numeric up/down value has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnNumericUpDown1ValueChanged(object sender, EventArgs e)
        {
            try
            {
                spinbutton1.Update();
                if (this.OnPercentFullChanged != null)
                    this.OnPercentFullChanged.Invoke(sender, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The text box 2 value has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnTextBox2TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.OnPAWChanged != null)
                    this.OnPAWChanged.Invoke(sender, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The radio button 1value has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnRadioButton1CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.OnFilledFromTopChanged != null)
                    this.OnFilledFromTopChanged(sender, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The text box 1 value has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnTextBox1TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (OnDepthWetSoilChanged != null)
                    OnDepthWetSoilChanged.Invoke(sender, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The combo box value has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnComboBox1SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (OnRelativeToChanged != null)
                    OnRelativeToChanged.Invoke(sender, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
