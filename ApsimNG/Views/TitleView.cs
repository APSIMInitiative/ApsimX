using System;
using Glade;
using Gtk;

namespace UserInterface.Views
{
    public delegate void TitleChangedDelegate(string NewText);

    /// <summary>
    /// Describes an interface for an axis view.
    /// </summary>
    interface ITitleView
    {
        event TitleChangedDelegate OnTitleChanged;
        void Populate(string title);
        
    }

    /// <summary>
    /// A Windows forms implementation of an AxisView
    /// </summary>
    public class TitleView : ViewBase, ITitleView
    {
        private string OriginalText;

        public event TitleChangedDelegate OnTitleChanged;

        [Widget]
        private HBox hbox1;
        [Widget]
        private Entry entry1;
        /// <summary>
        /// Construtor
        /// </summary>
        public TitleView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.TitleView.glade", "hbox1");
            gxml.Autoconnect(this);
            _mainWidget = hbox1;
            entry1.Changed += OnPositionComboChanged;
        }

        /// <summary>
        /// Populate the view with the specified title.
        /// </summary>
        public void Populate(string title)
        {
            entry1.Text = title;
        }

        /// <summary>
        /// When the user 'enters' the position combo box, save the current text value for later.
        /// </summary>
        private void OnTitleTextBoxEnter(object sender, EventArgs e)
        {
            OriginalText = entry1.Text;
        }

        /// <summary>
        /// When the user changes the combo box check to see if the text has changed. 
        /// If so then invoke the 'OnPositionChanged' event so that the presenter can pick it up.
        /// </summary>
        private void OnPositionComboChanged(object sender, EventArgs e)
        {
            if (OriginalText == null)
                OriginalText = entry1.Text;
            if (entry1.Text != OriginalText && OnTitleChanged != null)
            {
                OriginalText = entry1.Text;
                OnTitleChanged.Invoke(entry1.Text);
            }
        }
    }
}
