using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace UserInterface.Views
{
    interface IMemoView
    {
        event EventHandler<EditorArgs> MemoUpdate;

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        void AddContextAction(string ButtonText, System.EventHandler OnClick);

        /// <summary>
        /// Return the current cursor position in the memo.
        /// </summary>
        Point CurrentPosition { get; }

        string MemoText { get; set; }
        string[] MemoLines { get; set; }
        bool ReadOnly { get; set; }
        string LabelText { get; set; }
    }

    /// <summary>
    /// The Presenter for a Memo component.
    /// </summary>
    public partial class MemoView : UserControl, IMemoView
    {
        public event EventHandler<EditorArgs> MemoUpdate;
        public event EventHandler OnPopup;

        public MemoView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set or get the text of the richedit
        /// </summary>
        public string MemoText
        {
            get { return richTextBox1.Text; }
            set { richTextBox1.Text = value; }
        }

        /// <summary>
        /// Set or get the lines of the richedit
        /// </summary>
        public string[] MemoLines
        {
            get { return richTextBox1.Lines; }
            set { richTextBox1.Lines = value; }
        }

        /// <summary>
        /// Get or set the readonly name of the richedit.
        /// </summary>
        public bool ReadOnly 
        {
            get { return richTextBox1.ReadOnly; }
            set { richTextBox1.ReadOnly = value; }
        }

        /// <summary>
        /// Get or set the label text.
        /// </summary>
        public string LabelText 
        {
            get { return label1.Text; }
            set { label1.Text = value; }
        }


        /// <summary>
        /// The memo has been updated and now send the changed text to the model.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBox1_Leave(object sender, EventArgs e)
        {
            if (MemoUpdate != null)
            {
                EditorArgs args = new EditorArgs();
                args.TextString = richTextBox1.Text;
                MemoUpdate(this, args);
            }
        }

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        public void AddContextAction(string buttonText, System.EventHandler onClick)
        {
            contextMenuStrip1.Items.Add(buttonText);
            contextMenuStrip1.Items[0].Click += onClick;
            richTextBox1.ContextMenuStrip = contextMenuStrip1;
        }

        /// <summary>
        /// Return the current cursor position in the memo.
        /// </summary>
        public Point CurrentPosition
        {
            get
            {
                int lineNumber = richTextBox1.GetLineFromCharIndex(richTextBox1.SelectionStart);
                int firstCharOfLine = richTextBox1.GetFirstCharIndexFromLine(lineNumber);
                int colNumber = richTextBox1.SelectionStart - firstCharOfLine;
                return new Point(colNumber, lineNumber);
            }
        }


    }

    /// <summary>
    /// Event arg returned from the view
    /// </summary>
    public class EditorArgs : EventArgs
    {
        public string TextString;
    }
}
