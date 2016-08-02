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
            _mainWidget = checkbutton1;
            checkbutton1.Toggled += OnCheckChanged;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            checkbutton1.Toggled -= OnCheckChanged;
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

        /// <summary>
        /// The checked status has changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckChanged(object sender, EventArgs e)
        {
            if (Changed != null)
                Changed.Invoke(this, e);
        }

        /// <summary>Text property. Needed from designer.</summary>
        public string TextOfLabel
        {
            get { return checkbutton1.Label; }
            set { checkbutton1.Label = value; }
        }
    }
}
