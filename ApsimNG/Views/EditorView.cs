namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using EventArguments;
    using Gtk;
    using Mono.TextEditor;
    using Utility;

    /// <summary>
    /// This is IEditorView interface
    /// </summary>
    public interface IEditorView
    {
        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        event EventHandler<NeedContextItemsArgs> ContextItemsNeeded;

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
    public class EditorView : ViewBase,  IEditorView
    {
        /// <summary>
        /// The completion form
        /// </summary>
        private Window CompletionForm;

        /// <summary>
        /// The find-and-replace form
        /// </summary>
        private FindAndReplaceForm _findForm = new FindAndReplaceForm();

        /// <summary>
        /// The completion list
        /// </summary>
        private TreeView CompletionView;

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        public event EventHandler<NeedContextItemsArgs> ContextItemsNeeded;

        /// <summary>
        /// Invoked when the user changes the text in the editor.
        /// </summary>
        public event EventHandler TextHasChangedByUser;

        /// <summary>
        /// Invoked when the user leaves the text editor.
        /// </summary>
        public event EventHandler LeaveEditor;

        private ScrolledWindow scroller;
        private Mono.TextEditor.MonoTextEditor textEditor;
        private ListStore completionModel;
        private Gdk.Pixbuf functionPixbuf;
        private Gdk.Pixbuf propertyPixbuf;

        /// <summary>
        /// Default constructor that configures the Completion form.
        /// </summary>
        public EditorView(ViewBase owner) : base(owner)
        {
            scroller = new ScrolledWindow();
            textEditor = new MonoTextEditor();
            scroller.Add(textEditor);
            _mainWidget = scroller;
            TextEditorOptions options = new TextEditorOptions();
            options.EnableSyntaxHighlighting = true;
            options.ColorScheme = "Visual Studio";
            options.HighlightCaretLine = true;
            textEditor.Options = options;
            textEditor.Document.LineChanged += OnTextHasChanged;
            textEditor.LeaveNotifyEvent += OnTextBoxLeave;
            _mainWidget.Destroyed += _mainWidget_Destroyed;

            CompletionForm = new Window(WindowType.Toplevel);
            CompletionForm.Decorated = false;
            CompletionForm.SkipPagerHint = true;
            CompletionForm.SkipTaskbarHint = true;
            Frame completionFrame = new Frame();
            CompletionForm.Add(completionFrame);
            ScrolledWindow completionScroller = new ScrolledWindow();
            completionFrame.Add(completionScroller);
            completionModel = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
            CompletionView = new TreeView(completionModel);
            completionScroller.Add(CompletionView);
            TreeViewColumn column = new TreeViewColumn();
            CellRendererPixbuf iconRender = new Gtk.CellRendererPixbuf();
            column.PackStart(iconRender, false);
            CellRendererText textRender = new Gtk.CellRendererText();
            textRender.Editable = false;
            column.PackStart(textRender, true);
            column.SetAttributes(iconRender, "pixbuf", 0);
            column.SetAttributes(textRender, "text", 1);
            column.Title = "Item";
            column.Resizable = true;
            CompletionView.AppendColumn(column);
            textRender = new CellRendererText();
            column = new TreeViewColumn("Units", textRender, "text", 2);
            column.Resizable = true;
            CompletionView.AppendColumn(column);
            textRender = new CellRendererText();
            column = new TreeViewColumn("Type", textRender, "text", 3);
            column.Resizable = true;
            CompletionView.AppendColumn(column);
            textRender = new CellRendererText();
            column = new TreeViewColumn("Descr", textRender, "text", 4);
            column.Resizable = true;
            CompletionView.AppendColumn(column);
            functionPixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.Function.png", 16, 16);
            propertyPixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.Property.png", 16, 16);
            textEditor.TextArea.KeyPressEvent += OnKeyPress;
            CompletionView.HasTooltip = true;
            CompletionView.TooltipColumn = 5;
            CompletionForm.FocusOutEvent += OnLeaveCompletion;
            CompletionView.ButtonPressEvent += OnContextListMouseDoubleClick;
            CompletionView.KeyPressEvent += OnContextListKeyDown;
            CompletionView.KeyReleaseEvent += CompletionView_KeyReleaseEvent;
            IntelliSenseChars = ".";
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            textEditor.Document.LineChanged -= OnTextHasChanged;
            textEditor.LeaveNotifyEvent -= OnTextBoxLeave;
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            textEditor.TextArea.KeyPressEvent -= OnKeyPress;
            CompletionForm.FocusOutEvent -= OnLeaveCompletion;
            CompletionView.ButtonPressEvent -= OnContextListMouseDoubleClick;
            CompletionView.KeyReleaseEvent -= CompletionView_KeyReleaseEvent;
            if (CompletionForm.IsRealized)
                CompletionForm.Destroy();
            _findForm.Destroy();
        }

        /// <summary>
        /// Gets or sets the text property to get and set the content of the editor.
        /// </summary>
        public string Text
        {
            get
            {
                return textEditor.Text;
            }
            set
            {
                textEditor.Text = value;
                textEditor.Document.MimeType = "text/x-csharp";
                textEditor.Options.EnableSyntaxHighlighting = true;
            }
        }

        /// <summary>
        /// Gets or sets the lines in the editor.
        /// </summary>
        public string[] Lines
        {
            get
            {
                string text = textEditor.Text.TrimEnd("\r\n".ToCharArray());
                return text.Split(new string[] { textEditor.EolMarker, "\r\n", "\n" }, StringSplitOptions.None);
            }
            set
            {
                string St = string.Empty;
                if (value != null)
                {
                    foreach (string Value in value)
                    {
                        if (St != string.Empty)
                            St += textEditor.EolMarker;
                        St += Value;
                    }
                }
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
                return textEditor.Caret.Line;
            }
        }

        /// <summary>
        /// Preprocesses key strokes so that the ContextList can be displayed when needed. 
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Key arguments</param>
        [GLib.ConnectBefore] // Otherwise this is handled internally, and we won't see it
        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            char keyChar = (char)Gdk.Keyval.ToUnicode(e.Event.KeyValue);
            if (e.Event.Key == Gdk.Key.F3)
            {
                if (string.IsNullOrEmpty(_findForm.LookFor))
                    _findForm.ShowFor(textEditor, false);
                else
                    _findForm.FindNext(true, (e.Event.State & Gdk.ModifierType.ShiftMask) != 0,
                        string.Format("Search text «{0}» not found.", _findForm.LookFor));
                e.RetVal = true;
            }
            else if ((e.Event.State & Gdk.ModifierType.ControlMask) != 0)
            { 
               if (e.Event.Key == Gdk.Key.F || e.Event.Key == Gdk.Key.f)
               {
                  _findForm.ShowFor(textEditor, false);
                  e.RetVal = true; // true to prevent further processing
                }
            else if (e.Event.Key == Gdk.Key.H || e.Event.Key == Gdk.Key.h)
               {
                  _findForm.ShowFor(textEditor, true);
                  e.RetVal = true;
               }
            }
            // If user one of the IntelliSenseChars, then display contextlist.
            else if (IntelliSenseChars.Contains(keyChar.ToString()) && ContextItemsNeeded != null)
            {
                if (ShowCompletionWindow(keyChar))
                {
                    e.RetVal = false;
                }
            }
            else
            {
                e.RetVal = false;
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
                int PosDelimiter = textEditor.Text.LastIndexOfAny(" \r\n(+-/*".ToCharArray(), Pos - 1);
                return textEditor.Text.Substring(PosDelimiter + 1, Pos - PosDelimiter - 1).TrimEnd(".".ToCharArray());
            }
        }

        private bool initingCompletion = false;
        /// <summary>
        /// Show the context list. Return true if popup box shown
        /// </summary>
        /// <param name="characterPressed">Character pressed</param>
        /// <returns>Completion form showing</returns>        
        private bool ShowCompletionWindow(char characterPressed)
        {
            // Get a list of items to show and put into completion window.
            string TextBeforePeriod = GetWordBeforePosition(textEditor.Caret.Offset);
            List<string> Items = new List<string>();
            List<NeedContextItemsArgs.ContextItem> allitems = new List<NeedContextItemsArgs.ContextItem>();
            ContextItemsNeeded(this, new NeedContextItemsArgs() { ObjectName = TextBeforePeriod, Items = Items, AllItems = allitems });

            completionModel.Clear();
            foreach (NeedContextItemsArgs.ContextItem item in allitems)
            {
                TreeIter iter = completionModel.AppendValues(item.IsEvent ? functionPixbuf : propertyPixbuf, item.Name, item.Units, item.TypeName, item.Descr, item.ParamString);
            }
            if (completionModel.IterNChildren() > 0)
            {
                initingCompletion = true;
                textEditor.TextArea.InsertAtCaret(characterPressed.ToString());

                // Turn readonly on so that the editing window doesn't process keystrokes.
                textEditor.Document.ReadOnly = true;

                // Work out where to put the completion window.
                // This should probably be done a bit more intelligently to detect when we are too near the bottom or right
                // of the screen, and move accordingly. Left as an exercise for the student.
                Cairo.Point p = textEditor.TextArea.LocationToPoint(textEditor.Caret.Location);
                // Need to convert to screen coordinates....
                int x, y;
                int retVal = textEditor.GdkWindow.GetOrigin(out x, out y);
                CompletionForm.TransientFor = MainWidget.Toplevel as Window;
                CompletionForm.Move(p.X + x, p.Y + y + 20);
                CompletionForm.ShowAll();
                CompletionForm.Resize(CompletionView.Requisition.Width, 300);
                if (CompletionForm.GdkWindow != null)
                  CompletionForm.GdkWindow.Focus(0);
                while (Gtk.Application.EventsPending())
                    Gtk.Application.RunIteration();

                CompletionView.SetCursor(new TreePath("0"), null, false);
                initingCompletion = false;
                return true;

            }
            return false; 
        }
        

        /// <summary>
        /// Event handler for when the completion window loses focus
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnLeaveCompletion(object sender, FocusOutEventArgs e)
        {
            if (!initingCompletion)
              HideCompletionWindow();
        }

        /// <summary>
        /// Hide the completion window.
        /// </summary>
        private void HideCompletionWindow()
        {
            CompletionForm.Hide();
            textEditor.Document.ReadOnly = false;
            textEditor.GrabFocus();
        }

        /// <summary>
        /// We handle this because we don't see the return key in the KeyPress event handler
        /// </summary>
        private void CompletionView_KeyReleaseEvent(object o, KeyReleaseEventArgs args)
        {
            if (args.Event.Key == Gdk.Key.Return && CompletionView.Visible)
                InsertCompletionItemIntoTextBox();
        }

        /// <summary>
        /// Key down event handler
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Event arguments</param>
        private void OnContextListKeyDown(object sender, KeyPressEventArgs e)
        {
            // If user clicks ENTER and the context list is visible then insert the currently
            // selected item from the list into the TextBox and close the list.
            if (e.Event.Key == Gdk.Key.Return && CompletionView.Visible)
            {
                InsertCompletionItemIntoTextBox();
                e.RetVal = true;
            }

            // If the user presses ESC and the context list is visible then close the list.
            else if (e.Event.Key == Gdk.Key.Escape && CompletionView.Visible)
            {
                HideCompletionWindow();
                e.RetVal = true;
            }
        }

        /// <summary>
        /// User has double clicked on a completion list item. 
        /// </summary>
        [GLib.ConnectBefore] // Otherwise this is handled internally, and we won't see it
        private void OnContextListMouseDoubleClick(object sender, ButtonPressEventArgs e)
        {
            if (e.Event.Type == Gdk.EventType.TwoButtonPress && e.Event.Button == 1)
               InsertCompletionItemIntoTextBox();
        }

        /// <summary>
        /// Insert the currently selected completion item into the text box.
        /// </summary>
        private void InsertCompletionItemIntoTextBox()
        {
            string insertText = null;
            TreePath selPath;
            TreeViewColumn selCol;
            CompletionView.GetCursor(out selPath, out selCol);
            if (selPath != null)
            { 
                TreeIter iter;
                completionModel.GetIter(out iter, selPath);
                insertText = (string)completionModel.GetValue(iter, 1);
            }
            if (!String.IsNullOrEmpty(insertText))
              textEditor.InsertAtCaret(insertText);
            HideCompletionWindow();
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

        #region Code related to Edit menu

        /// <summary>Performs an action encapsulated in IEditAction.</summary>
        /// <remarks>
        /// There is an implementation of IEditAction for every action that 
        /// the user can invoke using a shortcut key (arrow keys, Ctrl+X, etc.)
        /// The editor control doesn't provide a public funciton to perform one
        /// of these actions directly, so I wrote DoEditAction() based on the
        /// code in TextArea.ExecuteDialogKey(). You can call ExecuteDialogKey
        /// directly, but it is more fragile because it takes a Keys value (e.g.
        /// Keys.Left) instead of the action to perform.
        /// <para/>
        /// Clipboard commands could also be done by calling methods in
        /// editor.ActiveTextAreaControl.TextArea.ClipboardHandler.
        /// </remarks>
        /* TBI
        private void DoEditAction(ICSharpCode.TextEditor.Actions.IEditAction action)
        {
            if (TextBox != null && action != null)
            {
                var area = TextBox.ActiveTextAreaControl.TextArea;
                TextBox.BeginUpdate();
                try
                {
                    lock (TextBox.Document)
                    {
                        action.Execute(area);
                        if (area.SelectionManager.HasSomethingSelected && area.AutoClearSelection /*&& caretchanged*//*)
                        {
                            if (area.Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal)
                            {
                                area.SelectionManager.ClearSelection();
                            }
                        }
                    }
                }
                finally
                {
                    TextBox.EndUpdate();
                    area.Caret.UpdateCaretPosition();
                }
            }
            
        }
        */
        // The following block comes from the example code provided at 
        // http://www.codeproject.com/Articles/30936/Using-ICSharpCode-TextEditor
        // I leave it here because it provides the handlers needed for a popup menu
        // Currently find and replace functions are accessed via keystrokes (e.g, ctrl-F, F3)
        /*
        private void menuEditCut_Click(object sender, EventArgs e)
        {
            if (HaveSelection())
                DoEditAction(new ICSharpCode.TextEditor.Actions.Cut());
        }
        private void menuEditCopy_Click(object sender, EventArgs e)
        {
            if (HaveSelection())
                DoEditAction(new ICSharpCode.TextEditor.Actions.Copy());
        }
        private void menuEditPaste_Click(object sender, EventArgs e)
        {
            DoEditAction(new ICSharpCode.TextEditor.Actions.Paste());
        }
        private void menuEditDelete_Click(object sender, EventArgs e)
        {
            if (HaveSelection())
                DoEditAction(new ICSharpCode.TextEditor.Actions.Delete());
        }

        private bool HaveSelection()
        {
            return TextBox.ActiveTextAreaControl.TextArea.SelectionManager.HasSomethingSelected;
        }

        private void menuEditFind_Click(object sender, EventArgs e)
        {
            _findForm.ShowFor(TextBox, true);
        }

        private void menuFindAgain_Click(object sender, EventArgs e)
        {
            _findForm.FindNext(true, false,
                string.Format("Search text «{0}» not found.", _findForm.LookFor));
        }
        private void menuFindAgainReverse_Click(object sender, EventArgs e)
        {
            _findForm.FindNext(true, true,
                string.Format("Search text «{0}» not found.", _findForm.LookFor));
        }

        private void menuToggleBookmark_Click(object sender, EventArgs e)
        {
                DoEditAction(new ICSharpCode.TextEditor.Actions.ToggleBookmark());
                TextBox.IsIconBarVisible = TextBox.Document.BookmarkManager.Marks.Count > 0;
        }

        private void menuGoToNextBookmark_Click(object sender, EventArgs e)
        {
            DoEditAction(new ICSharpCode.TextEditor.Actions.GotoNextBookmark
                (bookmark => true));
        }

        private void menuGoToPrevBookmark_Click(object sender, EventArgs e)
        {
            DoEditAction(new ICSharpCode.TextEditor.Actions.GotoPrevBookmark
                (bookmark => true));
        }
        */ 

        #endregion
    }
}
