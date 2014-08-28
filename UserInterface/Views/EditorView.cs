namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Windows.Forms;
    using System.Xml;
    using EventArguments;
    using ICSharpCode.TextEditor.Document;
    using ICSharpCode.TextEditor.Gui.InsightWindow;


    /// <summary>
    /// This is IEditorView interface
    /// </summary>
    public interface IEditorView
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
        /// Gets or sets the text property to get and set the content of the editor.
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Gets or sets the lines property to get and set the lines in the editor.
        /// </summary>
        string[] Lines { get; set; }

        /// <summary>
        /// Set the editor to use the specified resource name to syntax highlighting
        /// </summary>
        /// <param name="resourceName">The name of the resource</param>
        void SetSyntaxHighlighter(string resourceName);

        /// <summary>
        /// Gets or sets the characters that bring up the intellisense context menu.
        /// </summary>
        string IntelliSenseChars { get; set; }

        /// <summary>
        /// Gets the current line number
        /// </summary>
        int CurrentLineNumber { get; }
    }

    /// <summary>
    /// This class provides an intellisense editor and has the option of syntax highlighting keywords.
    /// </summary>
    public partial class EditorView : UserControl, IEditorView
    {
        /// <summary>
        /// The completion form
        /// </summary>
        private Form CompletionForm;

        /// <summary>
        /// The completion list
        /// </summary>
        private ListBox CompletionList;

        /// <summary>
        /// The search string for the listbox. 
        /// Reset by the timer or backspace
        /// </summary>
        private string searchValue;

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
        /// Default constructor that configures the Completion form.
        /// </summary>
        public EditorView()
        {
            InitializeComponent();
          
            CompletionForm = new Form();
            CompletionForm.TopLevel = false;
            CompletionForm.FormBorderStyle = FormBorderStyle.None;
            CompletionList = new ListBox();
            CompletionList.Dock = DockStyle.Fill;
            CompletionForm.Controls.Add(this.CompletionList);
            CompletionList.KeyDown += new KeyEventHandler(this.OnContextListKeyDown);
            CompletionList.KeyUp += new KeyEventHandler(this.OnContextListKeyUp);
            CompletionList.MouseDoubleClick += new MouseEventHandler(this.OnComtextListMouseDoubleClick);
            CompletionForm.StartPosition = FormStartPosition.Manual;
            CompletionList.Leave += new EventHandler(this.OnLeaveCompletion);

            TextBox.ActiveTextAreaControl.TextArea.KeyPress += this.OnKeyDown;
            IntelliSenseChars = ".";
            searchValue = string.Empty;
            timer1.Interval = 3000;
        }

        /// <summary>
        /// Gets or sets the text property to get and set the content of the editor.
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
        /// Gets or sets the lines in the editor.
        /// </summary>
        public string[] Lines
        {
            get
            {
                return TextBox.Text.Split(new string[1] { "\r\n" }, StringSplitOptions.None);
            }
            set
            {
                string St = string.Empty;
                foreach (string Value in value)
                    St += Value + "\r\n";
                Text = St;
            }
        }

        /// <summary>
        /// Gets or sets the characters that bring up the intellisense context menu.
        /// </summary>
        public string IntelliSenseChars { get; set; }

        /// <summary>
        /// Gets the current line number
        /// </summary>
        public int CurrentLineNumber
        {
            get
            {
                return TextBox.ActiveTextAreaControl.TextArea.Caret.Line;
            }
        }

        /// <summary>
        /// Set the editor to use the specified resource name to syntax highlighting
        /// </summary>
        /// <param name="resourceName">The name of the resource</param>
        public void SetSyntaxHighlighter(string resourceName)
        {
            ResourceSyntaxModeProvider fsmProvider; // Provider
            fsmProvider = new ResourceSyntaxModeProvider(resourceName); // Create new provider with the highlighting directory.
            HighlightingManager.Manager.AddSyntaxModeFileProvider(fsmProvider); // Attach to the text editor.
            TextBox.SetHighlighting(resourceName); // Activate the highlighting, use the name from the SyntaxDefinition node.
        }
                
        /// <summary>
        /// Preprocesses key strokes so that the ContextList can be displayed when needed. 
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Key arguments</param>
        private void OnKeyDown(object sender, KeyPressEventArgs e)
        {
            // If user one of the IntelliSenseChars, then display contextlist.
            if (IntelliSenseChars.Contains(e.KeyChar) && ContextItemsNeeded != null)
            {
                if (ShowCompletionWindow(e.KeyChar))
                {
                    e.Handled = false;
                }
            }
            else
            {
                e.Handled = false;
            }
        }

        /// <summary>
        /// Retrieve the word before the specified character position. 
        /// </summary>
        /// <param name="Pos">Position in the editor</param>
        /// <returns>The position of the word</returns>
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
        /// <param name="characterPressed">Character pressed</param>
        /// <returns>Completion form showing</returns>
        private bool ShowCompletionWindow(char characterPressed)
        {
            // Get a list of items to show and put into completion window.
            string TextBeforePeriod = GetWordBeforePosition(TextBox.ActiveTextAreaControl.TextArea.Caret.Offset);
            List<string> Items = new List<string>();
            ContextItemsNeeded(this, new NeedContextItems() { ObjectName = TextBeforePeriod, Items = Items });
            CompletionList.Items.Clear();
            CompletionList.Items.AddRange(Items.ToArray());

            if (CompletionList.Items.Count > 0)
            {
                TextBox.ActiveTextAreaControl.TextArea.InsertChar(characterPressed);

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
        /// Event handler for when the completion window loses focus
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnLeaveCompletion(object sender, EventArgs e)
        {
            HideCompletionWindow();
        }

        /// <summary>
        /// Hide the completion window.
        /// </summary>
        private void HideCompletionWindow()
        {
            CompletionForm.Visible = false;
            TextBox.Document.ReadOnly = false;
            this.Focus();
            searchValue = string.Empty;
            timer1.Enabled = false;
        }

        /// <summary>
        /// When the key is entered build a search string before the timer times out.
        /// The item found will become the top most item in the list and will 
        /// be selected.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnContextListKeyUp(object sender, KeyEventArgs e)
        {
            // search the list
            char key = (char)e.KeyValue;
            if (Char.IsLetter(key) || e.KeyCode == Keys.OemMinus)
            {
                timer1.Enabled = false;
                // handle _ (not sure if this is correct)
                if (e.KeyCode == Keys.OemMinus)
                {
                    searchValue = searchValue + '_';
                }
                else
                {
                    searchValue = searchValue + key.ToString().ToLower();
                }

                int i = CompletionList.FindString(searchValue);
                if (i >= 0)
                {
                    CompletionList.SelectedIndex = i;
                    CompletionList.TopIndex = i;
                }
                timer1.Enabled = true;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Back)
            {   // backspace resets the search string
                searchValue = string.Empty;
                timer1.Enabled = false;
            }
        }

        /// <summary>
        /// Key down event handler
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Event arguments</param>
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
            e.SuppressKeyPress = true;  // don't want the list handling selection itself
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

        public class ResourceSyntaxModeProvider : ISyntaxModeFileProvider
        {
            List<SyntaxMode> syntaxModes = null;
            private string resourceName;

            public ICollection<SyntaxMode> SyntaxModes
            {
                get
                {
                    return syntaxModes;
                }
            }

            public ResourceSyntaxModeProvider(string resourceName)
            {
                this.resourceName = resourceName;

                Assembly assembly = Assembly.GetExecutingAssembly();

                string syntaxMode = string.Format("<?xml version=\"1.0\"?>" +
                                                  "<SyntaxModes version=\"1.0\">" +
                                                  "  <Mode extensions=\".apsimx\" file=\"{0}.xshd\" name=\"{0}\"></Mode>" +
                                                  "</SyntaxModes>", resourceName);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(syntaxMode);
                MemoryStream syntaxModeStream = new MemoryStream(bytes);
                
                if (syntaxModeStream != null)
                {
                    syntaxModes = SyntaxMode.GetSyntaxModes(syntaxModeStream);
                }
                else
                {
                    syntaxModes = new List<SyntaxMode>();
                }
            }

            public XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                // load syntax schema  
                byte[] bytes = Properties.Resources.ResourceManager.GetObject(resourceName) as byte[];
                Stream stream = new MemoryStream(bytes);

                return new XmlTextReader(stream);
            }

            public void UpdateSyntaxModeList()
            {
                // resources don't change during runtime  
            }           
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            searchValue = string.Empty;
        }
    }
}
