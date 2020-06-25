using System;
using Gtk;

namespace UserInterface.Views
{
    /// <summary>An interface for a check box.</summary>
    public interface ICheckBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Gets or sets whether the checkbox is checked.</summary>
        bool IsChecked { get; set; }

        /// <summary>Gets or sets whether the checkbox can be changed by the user.</summary>
        bool IsSensitive { get; set; }
    }


    /// <summary>A checkbox view.</summary>
    public class CheckBoxView : ViewBase, ICheckBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        private CheckButton checkbutton1;

        /// <summary>Constructor</summary>
        public CheckBoxView(ViewBase owner) : base(owner)
        {
            checkbutton1 = new CheckButton();
            mainWidget = checkbutton1;
            checkbutton1.Toggled += OnCheckChanged;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                checkbutton1.Toggled -= OnCheckChanged;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Gets or sets whether the checkbox is checked.</summary>
        public bool IsChecked
        {
            get
            {
                return checkbutton1.Active;
            }
            set
            {
                checkbutton1.Active = value;
            }
        }

        /// <summary>Gets or sets whether the checkbox can be changed by the user.</summary>
        public bool IsSensitive
        {
            get
            {
                return checkbutton1.Sensitive;
            }
            set
            {
                checkbutton1.Sensitive = value;
            }
        }

        /// <summary>
        /// The checked status has changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckChanged(object sender, EventArgs e)
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

        /// <summary>Text property. Needed from designer.</summary>
        public string TextOfLabel
        {
            get { return checkbutton1.Label; }
            set { checkbutton1.Label = value; }
        }
    }
}
