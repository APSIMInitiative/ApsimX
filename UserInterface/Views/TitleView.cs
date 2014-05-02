using System;
using System.Windows.Forms;

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
    public partial class TitleView : UserControl, ITitleView
    {
        private string OriginalText;

        public event TitleChangedDelegate OnTitleChanged;

        /// <summary>
        /// Construtor
        /// </summary>
        public TitleView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Populate the view with the specified title.
        /// </summary>
        public void Populate(string title)
        {
            TitleTextBox.Text = title;
        }

        /// <summary>
        /// When the user 'enters' the position combo box, save the current text value for later.
        /// </summary>
        private void OnTitleTextBoxEnter(object sender, EventArgs e)
        {
            OriginalText = TitleTextBox.Text;
        }

        /// <summary>
        /// When the user changes the combo box check to see if the text has changed. 
        /// If so then invoke the 'OnPositionChanged' event so that the presenter can pick it up.
        /// </summary>
        private void OnPositionComboChanged(object sender, EventArgs e)
        {
            if (OriginalText == null)
                OriginalText = TitleTextBox.Text;
            if (TitleTextBox.Text != OriginalText && OnTitleChanged != null)
            {
                OriginalText = TitleTextBox.Text;
                OnTitleChanged.Invoke(TitleTextBox.Text);
            }
        }
    }
}
