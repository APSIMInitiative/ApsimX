namespace UserInterface.Views
{
    using System;
    using Gtk;

    /// <summary>An interface for a drop down</summary>
    public interface IDropDownView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Gets or sets the list of valid values.</summary>
        string[] Values { get; set; }

        /// <summary>Gets or sets the selected value.</summary>
        string SelectedValue { get; set; }

        /// <summary>Gets or sets a value indicating if the dropdown is visible.</summary>
        bool IsVisible { get; set; }

        /// <summary>Gets or sets whether the control should be editable.</summary>
        bool IsEditable { get; set; }

        /// <summary>Controls whether the user can change the selected item.</summary>
        bool IsSensitive { get; set; }
    }

    /// <summary>A drop down view.</summary>
    public class DropDownView : ViewBase, IDropDownView
    {
        /// <summary>
        /// The combobox that this class wraps
        /// </summary>
        private ComboBox combobox1;

        /// <summary>
        /// The list model for the combobox
        /// </summary>
        private ListStore comboModel = new ListStore(typeof(string));

        /// <summary>
        /// The renderer
        /// </summary>
        private CellRendererText comboRender = new CellRendererText();

        /// <summary>Constructor which also creates a ComboBox</summary>
        public DropDownView() : base()
        {
        }

        /// <summary>Constructor which also creates a ComboBox</summary>
        public DropDownView(ViewBase owner) : base(owner)
        {
            combobox1 = new ComboBox(comboModel);
            SetupCombo();
        }

        /// <summary>
        /// A method used when a view is wrapping a gtk control.
        /// </summary>
        /// <param name="ownerView">The owning view.</param>
        /// <param name="gtkControl">The gtk control being wrapped.</param>
        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            owner = ownerView;
            combobox1 = (ComboBox)gtkControl;
            SetupCombo();
        }

        /// <summary>
        /// Construct a DropDownView with an existing ComboBox object
        /// </summary>
        /// <param name="owner">The owning view</param>
        /// <param name="combo">The combobox to wrap</param>
        public DropDownView(ViewBase owner, ComboBox combo) : base(owner)
        {
            combobox1 = combo;
            combobox1.Model = comboModel;
            SetupCombo();
        }

        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>
        /// Configuration at construction time
        /// </summary>
        private void SetupCombo()
        {
            mainWidget = combobox1;
            combobox1.Model = comboModel;
            combobox1.PackStart(comboRender, false);
            combobox1.AddAttribute(comboRender, "text", 0);
            combobox1.Changed += OnSelectionChanged;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>
        /// Cleanup the events
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                combobox1.Changed -= OnSelectionChanged;
                comboModel.Dispose();
                comboRender.Destroy();
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Gets or sets the list of valid values.</summary>
        public string[] Values
        {
            get
            {
                int numNames = comboModel.IterNChildren();
                string[] result = new string[numNames];
                TreeIter iter;
                int i = 0;
                if (comboModel.GetIterFirst(out iter))
                    do
                        result[i++] = (string)comboModel.GetValue(iter, 0);
                    while (comboModel.IterNext(ref iter) && i < numNames);
                return result;
            }

            set
            {
                // Avoid possible recursion
                combobox1.Changed -= OnSelectionChanged;
                try
                {
                    comboModel.Clear();
                    foreach (string text in value)
                        comboModel.AppendValues(text);

                    // if (comboModel.IterNChildren() > 0)
                    //     combobox1.Active = 0;
                    // else
                    combobox1.Active = 1;
                }
                finally
                {
                    combobox1.Changed += OnSelectionChanged;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected item for the combo
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return combobox1.Active;
            }

            set
            {
                combobox1.Active = Math.Min(value, comboModel.IterNChildren());
            }
        }

        /// <summary>Gets or sets the selected value.</summary>
        public string SelectedValue
        {
            get
            {
                TreeIter iter;
                if (combobox1.GetActiveIter(out iter))
                    return (string)combobox1.Model.GetValue(iter, 0);
                else
                    return null;
            }

            set
            {
                TreeIter iter;
                if (comboModel.GetIterFirst(out iter))
                {
                    string entry = (string)comboModel.GetValue(iter, 0);
                    while (entry != null && !entry.Equals(value, StringComparison.InvariantCultureIgnoreCase) && comboModel.IterNext(ref iter)) // Should the text matchin be case-insensitive?
                        entry = (string)comboModel.GetValue(iter, 0);
                    if (entry == value)
                        combobox1.SetActiveIter(iter);
                    else // Could not find a matching entry
                        combobox1.Active = -1;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the combobox is visible.
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return combobox1.Visible;
            }

            set
            {
                combobox1.Visible = value;
            }
        }

        /// <summary>Gets or sets a value indicating whether the control should be editable.</summary>
        public bool IsEditable
        { 
            get
            {
                return comboRender.Editable;
            }

            set
            {
                comboRender.Editable = value;
            }
        }

        /// <summary>Controls whether the user can change the selected item.</summary>
        public bool IsSensitive
        {
            get
            {
                return combobox1.Sensitive;
            }
            set
            {
                combobox1.Sensitive = value;
            }
        }

        /// <summary>User has changed the selection.</summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (Changed != null)
                    Changed.Invoke(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Get the index of the string value in the list.
        /// </summary>
        /// <param name="value">The string to search for</param>
        /// <returns>The index 0->n. Returns -1 if not found.</returns>
        public int IndexOf(string value)
        {
            int result = -1;
            TreeIter iter;
            int i = 0;
            
            bool more = comboModel.GetIterFirst(out iter);
            while (more && (result == -1))
            {
                if (string.Compare(value, (string)comboModel.GetValue(iter, 0), false) == 0)
                {
                    result = i;
                }
                i++;

                more = comboModel.IterNext(ref iter);
            }

            return result;
        }
    }
}
