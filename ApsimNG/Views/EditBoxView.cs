using System;
using Gtk;

namespace UserInterface.Views
{
    /// <summary>An interface for a drop down</summary>
    public interface IEditView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Gets or sets the Text</summary>
        string Value { get; set; }

        /// <summary>Return true if dropdown is visible.</summary>
        bool IsVisible { get; set; }
    }

    /// <summary>A drop down view.</summary>
    public class EditView : ViewBase, IEditView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        private Entry textentry1;
        
        /// <summary>Constructor</summary>
        public EditView(ViewBase owner) : base(owner)
        {
            textentry1 = new Entry();
            _mainWidget = textentry1;
            textentry1.FocusOutEvent += OnSelectionChanged;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            textentry1.FocusOutEvent -= OnSelectionChanged;
        }

        /// <summary>Gets or sets the Text.</summary>
        public string Value
        {
            get
            {
                return textentry1.Text;
            }
            set
            {
                textentry1.Text = value;
            }
        }

        /// <summary>Return true if dropdown is visible.</summary>
        public bool IsVisible
        {
            get { return textentry1.Visible; }
            set { textentry1.Visible = value; }
        }

        /// <summary>User has changed the selection.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectionChanged(object sender, FocusOutEventArgs e)
        {
            if (Changed != null)
                Changed.Invoke(this, e);
        }

    }
}
