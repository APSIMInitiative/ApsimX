using System;
using Gtk;

namespace UserInterface.Views
{
    public delegate void TitleChangedDelegate(string NewText);

    /// <summary>
    /// A Gtk# implementation of an TitleView
    /// </summary>
    public class TitleView : ViewBase, ITitleView
    {
        private string originalText;

        public event TitleChangedDelegate OnTitleChanged;

        private Box hbox1 = null;
        private Entry entry1 = null;

        /// <summary>
        /// Construtor
        /// </summary>
        public TitleView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.TitleView.glade");
            hbox1 = (Box)builder.GetObject("hbox1");
            entry1 = (Entry)builder.GetObject("entry1");
            mainWidget = hbox1;
            entry1.Changed += OnPositionComboChanged;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                entry1.Changed -= OnPositionComboChanged;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Populate the view with the specified title.
        /// </summary>
        public void Populate(string title)
        {
            entry1.Text = title;
        }

        /// <summary>
        /// When the user changes the combo box check to see if the text has changed. 
        /// If so then invoke the 'OnPositionChanged' event so that the presenter can pick it up.
        /// </summary>
        private void OnPositionComboChanged(object sender, EventArgs e)
        {
            try
            {
                if (originalText == null)
                    originalText = entry1.Text;
                if (entry1.Text != originalText && OnTitleChanged != null)
                {
                    originalText = entry1.Text;
                    OnTitleChanged.Invoke(entry1.Text);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }

    /// <summary>
    /// Describes an interface for an axis view.
    /// </summary>
    interface ITitleView
    {
        event TitleChangedDelegate OnTitleChanged;

        void Populate(string title);
    }
}
