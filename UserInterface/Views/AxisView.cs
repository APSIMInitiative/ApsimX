using System;
using System.Windows.Forms;

namespace UserInterface.Views
{
    public delegate void TextChangedDelegate(string NewText);
    public delegate void InvertedhangedDelegate(bool Inverted);

    /// <summary>
    /// Describes an interface for an axis view.
    /// </summary>
    interface IAxisView
    {
        event TextChangedDelegate OnTitleChanged;
        event InvertedhangedDelegate OnInvertedChanged;
        void Populate(string Title, bool Inverted);
        
    }

    /// <summary>
    /// A Windows forms implementation of an AxisView
    /// </summary>
    public partial class AxisView : UserControl, IAxisView
    {
        private string OriginalText;

        public event TextChangedDelegate OnTitleChanged;
        public event InvertedhangedDelegate OnInvertedChanged;

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
        public void Populate(string Title, bool Inverted)
        {
            TitleTextBox.Text = Title;
            InvertedCheckBox.Checked = Inverted;
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

        private void OnCheckedChanged(object sender, EventArgs e)
        {
            if (OnInvertedChanged != null)
                OnInvertedChanged(InvertedCheckBox.Checked);
        }
    }
}
