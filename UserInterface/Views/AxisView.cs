using System;
using System.Windows.Forms;

namespace UserInterface.Views
{
    public delegate void TextChangedDelegate(string NewText);
        
    /// <summary>
    /// Describes an interface for an axis view.
    /// </summary>
    interface IAxisView
    {
        event TextChangedDelegate OnTitleChanged;
        void Populate(string Title);
        
    }

    /// <summary>
    /// A Windows forms implementation of an AxisView
    /// </summary>
    public partial class AxisView : UserControl, IAxisView
    {
        private string OriginalText;

        public event TextChangedDelegate OnTitleChanged;

        /// <summary>
        /// Construtor
        /// </summary>
        public AxisView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Populate the view with the specified title.
        /// </summary>
        public void Populate(string Title)
        {
            TitleTextBox.Text = Title;
        }

        /// <summary>
        /// When the user 'enters' the title text box, save the current text value for later.
        /// </summary>
        private void OnTitleTextBoxEnter(object sender, EventArgs e)
        {
            OriginalText = TitleTextBox.Text;
        }

        /// <summary>
        /// When the user 'leaves' the title text box, check to see if the text has changed. 
        /// If so then invoke the 'OnTitleChanged' event so that the presenter can pick it up.
        /// </summary>
        private void OnTitleTextBoxLeave(object sender, EventArgs e)
        {
            if (TitleTextBox.Text != OriginalText && OnTitleChanged != null)
                OnTitleChanged.Invoke(TitleTextBox.Text);
        }
    }
}
