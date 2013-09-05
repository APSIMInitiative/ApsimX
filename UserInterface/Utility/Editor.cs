using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Utility
{
    /// <summary>
    /// This class provides an intellisense editor and has the option of syntax highlighting keywords.
    /// </summary>
    public partial class Editor : UserControl
    {
        private ListBox ContextList = new ListBox();

        public class NeedContextItems : EventArgs
        {
            public string ObjectName;
            public List<string> Items;
        }
        public event EventHandler<NeedContextItems> ContextItemsNeeded;
        public event EventHandler TextHasChangedByUser;

        public SyntaxHighlighter SyntaxHighlighter { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public Editor()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Text property to get and set the content of the editor.
        /// </summary>
        public new string Text
        {
            get
            {
                return TextBox.Text;
            }
            set
            {
                TextBox.TextChanged -= OnTextHasChanged;
                TextBox.Text = value;
                if (SyntaxHighlighter != null)
                {
                    SyntaxHighlighter.Update(this);
                    SyntaxHighlighter.DoSyntaxHightlight_AllLines(this);
                }
                TextBox.TextChanged += OnTextHasChanged;
            }
        }

        /// <summary>
        /// Lines property to get and set the lines in the editor.
        /// </summary>
        public string[] Lines
        {
            get
            {
                return TextBox.Lines;
            }
            set
            {
                TextBox.TextChanged -= OnTextHasChanged;
                TextBox.Lines = value;
                if (SyntaxHighlighter != null)
                {
                    SyntaxHighlighter.Update(this);
                    SyntaxHighlighter.DoSyntaxHightlight_AllLines(this);
                }
                TextBox.TextChanged += OnTextHasChanged;
            }
        }

        /// <summary>
        /// Preprocesses key strokes so that the ContextList can be displayed when needed.
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // If user clicks a '.' then display contextlist.
            if (e.KeyCode == Keys.OemPeriod && ContextItemsNeeded != null)
            {
                string TextBeforePeriod = GetWordBeforePosition(TextBox.SelectionStart);
                List<string> Items = new List<string>();
                ContextItemsNeeded(this, new NeedContextItems() { ObjectName = TextBeforePeriod, Items = Items });
                ShowList(Items.ToArray());
            }

            // If user clicks UP arrow and the context list is visible then move the selected
            // item in the list up.
            else if (e.KeyCode == Keys.Up && ContextList.Visible && ContextList.SelectedIndex > 0)
            {
                ContextList.SelectedIndex--;
                e.Handled = true;
            }

            // If user clicks DOWN arrow and the context list is visible then move the selected
            // item in the list down.
            else if (e.KeyCode == Keys.Down && ContextList.Visible && ContextList.SelectedIndex < ContextList.Items.Count - 1)
            {
                ContextList.SelectedIndex++;
                e.Handled = true;
            }

            // If user clicks ENTER and the context list is visible then insert the currently
            // selected item from the list into the TextBox and close the list.
            else if (e.KeyCode == Keys.Enter && ContextList.Visible && ContextList.SelectedIndex != -1)
            {
                int SavedSelectionStart = TextBox.SelectionStart;
                string TextToInsert = ContextList.SelectedItem as string;
                TextBox.Text = TextBox.Text.Insert(TextBox.SelectionStart, TextToInsert);
                ContextList.Visible = false;
                TextBox.SelectionStart = SavedSelectionStart + TextToInsert.Length;
                e.Handled = true;
            }

            // If the user presses ESC and the context list is visible then close the list.
            else if (e.KeyCode == Keys.Escape && ContextList.Visible)
            {
                ContextList.Visible = false;
                e.Handled = true;
            }

            else
                e.Handled = false;
        }

        /// <summary>
        /// Retrieve the word before the specified character position.
        /// </summary>
        private string GetWordBeforePosition(int Pos)
        {
            if (Pos == 0)
                return "";
            else
            {
                int PosDelimiter = TextBox.Text.LastIndexOfAny(" \r\n".ToCharArray(), Pos - 1);
                return TextBox.Text.Substring(PosDelimiter + 1, Pos - PosDelimiter - 1);
            }
        }

        /// <summary>
        /// Show the context list.
        /// </summary>
        private void ShowList(string[] Values)
        {
            ContextList.Items.Clear();
            ContextList.Items.AddRange(Values);
            TextBox.Controls.Add(ContextList);
            Point p = TextBox.GetPositionFromCharIndex(TextBox.SelectionStart);
            ContextList.Left = p.X;
            ContextList.Top = p.Y + 20;  // Would be nice not to use a constant number of pixels.

            ContextList.Show();
            if (ContextList.Items.Count > 0)
                ContextList.SelectedIndex = 0;
        }

        /// <summary>
        /// User has changed text. Invoke our OnTextChanged event.
        /// </summary>
        private void OnTextHasChanged(object sender, EventArgs e)
        {
            if (SyntaxHighlighter != null)
                SyntaxHighlighter.DoSyntaxHightlight_CurrentLine(this);

            if (TextHasChangedByUser != null)
                TextHasChangedByUser(sender, e);
        }

        #region Functions needed by the SyntaxHighlighter
        public string GetLastWord()
        {
            int pos = TextBox.SelectionStart;

            while (pos > 1)
            {
                string substr = Text.Substring(pos - 1, 1);

                if (Char.IsWhiteSpace(substr, 0))
                {
                    return Text.Substring(pos, TextBox.SelectionStart - pos);
                }

                pos--;
            }

            return Text.Substring(0, TextBox.SelectionStart);
        }
        public string GetLastLine()
        {
            int charIndex = TextBox.SelectionStart;
            int currentLineNumber = TextBox.GetLineFromCharIndex(charIndex);

            // the carriage return hasn't happened yet... 
            //      so the 'previous' line is the current one.
            string previousLineText;
            if (TextBox.Lines.Length <= currentLineNumber)
                previousLineText = TextBox.Lines[TextBox.Lines.Length - 1];
            else
                previousLineText = TextBox.Lines[currentLineNumber];

            return previousLineText;
        }
        public string GetCurrentLine()
        {
            int charIndex = TextBox.SelectionStart;
            int currentLineNumber = TextBox.GetLineFromCharIndex(charIndex);

            if (currentLineNumber < TextBox.Lines.Length)
            {
                return TextBox.Lines[currentLineNumber];
            }
            else
            {
                return string.Empty;
            }
        }
        public int GetCurrentLineStartIndex()
        {
            return TextBox.GetFirstCharIndexOfCurrentLine();
        }
        public int SelectionStart
        {
            get
            {
                return TextBox.SelectionStart;
            }
            set
            {
                TextBox.SelectionStart = value;
            }
        }
        public int SelectionLength
        {
            get
            {
                return TextBox.SelectionLength;
            }
            set
            {
                TextBox.SelectionLength = value;
            }
        }
        public Color SelectionColor
        {
            get
            {
                return TextBox.SelectionColor;
            }
            set
            {
                TextBox.SelectionColor = value;
            }
        }
        public string SelectedText
        {
            get
            {
                return TextBox.SelectedText;
            }
            set
            {
                TextBox.SelectedText = value;
            }
        }
        #endregion
    }
}
