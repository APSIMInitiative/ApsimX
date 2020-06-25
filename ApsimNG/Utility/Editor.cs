using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace Utility
{

    public class NeedContextItems : EventArgs
    {
        public string ObjectName;
        public List<string> Items;
    }

    public interface IEditor
    {
        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        event EventHandler<NeedContextItems> ContextItemsNeeded;

        /// <summary>
        /// Invoked when the user changes the text in the editor.
        /// </summary>
        event EventHandler TextHasChangedByUser;

        /// <summary>
        /// Invoked when the user leaves the text editor.
        /// </summary>
        event EventHandler LeaveEditor;

        /// <summary>
        /// Text property to get and set the content of the editor.
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Lines property to get and set the lines in the editor.
        /// </summary>
        string[] Lines { get; set; }

    }

    /// <summary>
    /// This class provides an intellisense editor and has the option of syntax highlighting keywords.
    /// </summary>
    public partial class Editor : UserControl, IEditor
    {
        private Form CompletionForm;
        private ListBox CompletionList;


        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        public event EventHandler<NeedContextItems> ContextItemsNeeded;

        /// <summary>
        /// Invoked when the user changes the text in the editor.
        /// </summary>
        public event EventHandler TextHasChangedByUser;

        /// <summary>
        /// Invoked when the user leaves the text editor.
        /// </summary>
        public event EventHandler LeaveEditor;

        /// <summary>
        /// Constructor
        /// </summary>
        public Editor()
        {
            InitializeComponent();

            CompletionForm = new Form();
            CompletionForm.TopLevel = false;
            CompletionForm.FormBorderStyle = FormBorderStyle.None;
            CompletionList = new ListBox();
            CompletionList.Dock = DockStyle.Fill;
            CompletionForm.Controls.Add(CompletionList);
            CompletionList.KeyDown += new KeyEventHandler(OnContextListKeyDown);
            CompletionList.MouseDoubleClick += new MouseEventHandler(OnComtextListMouseDoubleClick);
            CompletionForm.StartPosition = FormStartPosition.Manual;

            TextBox.ActiveTextAreaControl.TextArea.KeyDown += OnKeyDown;
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
                TextBox.TextChanged += OnTextHasChanged;
                TextBox.Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy("C#");
            }
        }

        /// <summary>
        /// Lines property to get and set the lines in the editor.
        /// </summary>
        public string[] Lines
        {
            get
            {
                return TextBox.Text.Split(new string[1] { "\r\n" }, StringSplitOptions.None);
            }
            set
            {
                string St = "";
                foreach (string Value in value)
                    St += Value + "\r\n";
                Text = St;
            }
        }

        /// <summary>
        /// Preprocesses key strokes so that the ContextList can be displayed when needed.
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // If user clicks a '.' then display contextlist.
            if (e.KeyCode == Keys.OemPeriod && e.Shift == false && ContextItemsNeeded != null)
            {
                if (ShowCompletionWindow())
                    e.Handled = false;
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
                int PosDelimiter = TextBox.Text.LastIndexOfAny(" \r\n(".ToCharArray(), Pos - 1);
                return TextBox.Text.Substring(PosDelimiter + 1, Pos - PosDelimiter - 1).TrimEnd(".".ToCharArray());
            }
        }

        /// <summary>
        /// Show the context list. Return true if popup box shown
        /// </summary>
        private bool ShowCompletionWindow()
        {
            // Get a list of items to show and put into completion window.
            string TextBeforePeriod = GetWordBeforePosition(TextBox.ActiveTextAreaControl.TextArea.Caret.Offset);
            List<string> Items = new List<string>();
            ContextItemsNeeded(this, new NeedContextItems() { ObjectName = TextBeforePeriod, Items = Items });
            CompletionList.Items.Clear();
            CompletionList.Items.AddRange(Items.ToArray());

            if (CompletionList.Items.Count > 0)
            {
                TextBox.ActiveTextAreaControl.TextArea.InsertChar('.');

                // Turn readonly on so that the editing window doesn't process keystrokes.
                TextBox.Document.ReadOnly = true;

                // Work out where to put the completion window.
                Point p = TextBox.ActiveTextAreaControl.TextArea.Caret.ScreenPosition;
                Point EditorLocation = TextBox.PointToScreen(p);

                Point EditorLocation1 = Application.OpenForms[0].PointToClient(EditorLocation);
                // Display completion window.
                CompletionForm.Parent = Application.OpenForms[0];
                CompletionForm.Left = EditorLocation1.X;
                CompletionForm.Top = EditorLocation1.Y + 20;  // Would be nice not to use a constant number of pixels.
                CompletionForm.Show();
                CompletionForm.BringToFront();
                CompletionForm.Controls[0].Focus();

                if (CompletionList.Items.Count > 0)
                    CompletionList.SelectedIndex = 0;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Hide the completion window.
        /// </summary>
        private void HideCompletionWindow()
        {
            CompletionForm.Visible = false;
            TextBox.Document.ReadOnly = false;
        }

        private void OnContextListKeyDown(object sender, KeyEventArgs e)
        {
            // If user clicks ENTER and the context list is visible then insert the currently
            // selected item from the list into the TextBox and close the list.
            if (e.KeyCode == Keys.Enter && CompletionList.Visible && CompletionList.SelectedIndex != -1)
            {
                InsertCompletionItemIntoTextBox();
                e.Handled = true;
            }

            // If the user presses ESC and the context list is visible then close the list.
            else if (e.KeyCode == Keys.Escape && CompletionList.Visible)
            {
                HideCompletionWindow();
                e.Handled = true;
            }
        }

        /// <summary>
        /// User has double clicked on a completion list item. 
        /// </summary>
        private void OnComtextListMouseDoubleClick(object sender, MouseEventArgs e)
        {
            InsertCompletionItemIntoTextBox();
        }

        /// <summary>
        /// Insert the currently selected completion item into the text box.
        /// </summary>
        private void InsertCompletionItemIntoTextBox()
        {
            int Line = TextBox.ActiveTextAreaControl.TextArea.Caret.Line;
            int Column = TextBox.ActiveTextAreaControl.TextArea.Caret.Column;
            string TextToInsert = CompletionList.SelectedItem as string;
            TextBox.Text = TextBox.Text.Insert(TextBox.ActiveTextAreaControl.TextArea.Caret.Offset, TextToInsert);

            HideCompletionWindow();

            TextBox.ActiveTextAreaControl.TextArea.Caret.Line = Line;
            TextBox.ActiveTextAreaControl.TextArea.Caret.Column = Column + TextToInsert.Length;
        }


        /// <summary>
        /// User has changed text. Invoke our OnTextChanged event.
        /// </summary>
        private void OnTextHasChanged(object sender, EventArgs e)
        {
            if (TextHasChangedByUser != null)
                TextHasChangedByUser(sender, e);
        }

        private void OnTextBoxLeave(object sender, EventArgs e)
        {
            if (LeaveEditor != null)
                LeaveEditor.Invoke(this, e);
        }

       
    }
}
