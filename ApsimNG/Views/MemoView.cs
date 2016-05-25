using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
///using System.Windows.Forms;
using Glade;
using Gtk;

namespace UserInterface.Views
{
    interface IMemoView
    {
        event EventHandler<EditorArgs> MemoLeave;
        event EventHandler<EditorArgs> MemoChange;

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

        void Export(int width, int height, Graphics graphics);
    }

    /// <summary>
    /// The Presenter for a Memo component.
    /// </summary>
    public class MemoView : ViewBase, IMemoView
    {
        public event EventHandler<EditorArgs> MemoLeave;
        public event EventHandler<EditorArgs> MemoChange;

        [Widget]
        private VBox vbox1 = null;
        [Widget]
        public TextView textView = null;
        [Widget]
        private Label label1 = null;

        public MemoView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.MemoView.glade", "vbox1");
            gxml.Autoconnect(this);
            _mainWidget = vbox1;
            textView.FocusOutEvent += richTextBox1_Leave;
            textView.Buffer.Changed += richTextBox1_TextChanged;
        }

        /// <summary>
        /// Set or get the text of the richedit
        /// </summary>
        public string MemoText
        {
            get { return textView.Buffer.Text; }
            set { textView.Buffer.Text = value; }
        }

        /// <summary>
        /// Set or get the lines of the richedit
        /// </summary>
        public string[] MemoLines
        {
            get
            {
                string contents = textView.Buffer.Text;
                return contents.Split(new string[] { Environment.NewLine, "\r\n", "\n" }, StringSplitOptions.None);
            }
            set
            {
                textView.Buffer.Clear();
                TextIter iter = textView.Buffer.EndIter;
                foreach (string line in MemoLines)
                    textView.Buffer.Insert(ref iter, line);
            }
        }

        /// <summary>
        /// Get or set the readonly name of the richedit.
        /// </summary>
        public bool ReadOnly 
        {
            get { return !textView.Editable; }
            set { textView.Editable = !value; }
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
        private void richTextBox1_Leave(object sender, FocusOutEventArgs e)
        {
            if (MemoLeave != null)
            {
                EditorArgs args = new EditorArgs();
                args.TextString = textView.Buffer.Text;
                MemoLeave(this, args);
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (MemoChange != null)
            {
                EditorArgs args = new EditorArgs();
                args.TextString = textView.Buffer.Text;
                MemoChange(this, args);
            }
        }

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        public void AddContextAction(string buttonText, System.EventHandler onClick)
        {
            /* TBI
            contextMenuStrip1.Items.Add(buttonText);
            contextMenuStrip1.Items[0].Click += onClick;
            richTextBox1.ContextMenuStrip = contextMenuStrip1;
            */
        }

        /// <summary>
        /// Return the current cursor position in the memo.
        /// </summary>
        public Point CurrentPosition
        {
            get
            {   /* TBI
                int lineNumber = richTextBox1.GetLineFromCharIndex(richTextBox1.SelectionStart);
                int firstCharOfLine = richTextBox1.GetFirstCharIndexFromLine(lineNumber);
                int colNumber = richTextBox1.SelectionStart - firstCharOfLine; */
                return new Point(0, 0); /// TBI colNumber, lineNumber);
            }
        }

        /// <summary>
        /// Export the memo to the specified 'graphics'
        /// </summary>
        public void Export(int width, int height, Graphics graphics)
        {
            float x = 10;
            float y = 0;
            int charpos = 0;
            /* TBI
            while (charpos < richTextBox1.Text.Length)
            {
                if (richTextBox1.Text[charpos] == '\n')
                {
                    charpos++;
                    y += 20;
                    x = 10;
                }
                else if (richTextBox1.Text[charpos] == '\r')
                    charpos++;
                else
                {
                    richTextBox1.Select(charpos, 1);
                    graphics.DrawString(richTextBox1.SelectedText, richTextBox1.SelectionFont,
                                        new SolidBrush(richTextBox1.SelectionColor), x, y);
                    x += graphics.MeasureString(richTextBox1.SelectedText, richTextBox1.SelectionFont).Width;
                    charpos++;
                }
            }
            */
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
