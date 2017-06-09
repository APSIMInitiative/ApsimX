namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
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
        /// Add a separator line to the context menu
        /// </summary>
        void AddContextSeparator();

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        /// <param name="shortcut">Describes the key to use as the accelerator</param>
        void AddContextActionWithAccel(string menuItemText, System.EventHandler onClick, string shortcut);

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
    public class EditorView : ViewBase, IEditorView
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
        private Menu Popup = new Menu();
        private AccelGroup accel = new AccelGroup();

        /// <summary>
        /// Default constructor that configures the Completion form.
        /// </summary>
        public EditorView(ViewBase owner) : base(owner)
        {
            scroller = new ScrolledWindow();
            textEditor = new MonoTextEditor();
            scroller.Add(textEditor);
            _mainWidget = scroller;
            Mono.TextEditor.TextEditorOptions options = new Mono.TextEditor.TextEditorOptions();
            options.EnableSyntaxHighlighting = true;
            options.ColorScheme = "Visual Studio";
            options.HighlightCaretLine = true;
            textEditor.Options = options;
            textEditor.TextArea.DoPopupMenu = DoPopup;
            textEditor.Document.LineChanged += OnTextHasChanged;
            textEditor.TextArea.FocusInEvent += OnTextBoxEnter;
            textEditor.TextArea.FocusOutEvent += OnTextBoxLeave;
            _mainWidget.Destroyed += _mainWidget_Destroyed;

            AddContextActionWithAccel("Cut", OnCut, "Ctrl+X");
            AddContextActionWithAccel("Copy", OnCopy, "Ctrl+C");
            AddContextActionWithAccel("Paste", OnPaste, "Ctrl+V");
            AddContextActionWithAccel("Delete", OnDelete, "Delete");
            AddContextSeparator();
            AddContextActionWithAccel("Undo", OnUndo, "Ctrl+Z");
            AddContextActionWithAccel("Redo", OnRedo, "Ctrl+Y");
            AddContextActionWithAccel("Find", OnFind, "Ctrl+F");
            AddContextActionWithAccel("Replace", OnReplace, "Ctrl+H");

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
            textEditor.TextArea.FocusInEvent -= OnTextBoxEnter;
            textEditor.TextArea.FocusOutEvent -= OnTextBoxLeave;
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            textEditor.TextArea.KeyPressEvent -= OnKeyPress;
            CompletionForm.FocusOutEvent -= OnLeaveCompletion;
            CompletionView.ButtonPressEvent -= OnContextListMouseDoubleClick;
            CompletionView.KeyReleaseEvent -= CompletionView_KeyReleaseEvent;
            if (CompletionForm.IsRealized)
                CompletionForm.Destroy();
            // It's good practice to disconnect all event handlers, as it makes memory leaks
            // less likely. However, we may not "own" the event handlers, so how do we 
            // know what to disconnect?
            // We can do this via reflection. Here's how it currently can be done in Gtk#.
            // Windows.Forms would do it differently.
            // This may break if Gtk# changes the way they implement event handlers.
            foreach (Widget w in Popup)
            {
                if (w is MenuItem)
                {
                    PropertyInfo pi = w.GetType().GetProperty("AfterSignals", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (pi != null)
                    {
                        System.Collections.Hashtable handlers = (System.Collections.Hashtable)pi.GetValue(w);
                        if (handlers != null && handlers.ContainsKey("activate"))
                        {
                            EventHandler handler = (EventHandler)handlers["activate"];
                            (w as MenuItem).Activated -= handler;
                        }
                    }
                }
            }
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
                    _findForm.FindNext(true, (e.Event.State & Gdk.ModifierType.ShiftMask) == 0,
                        string.Format("Search text «{0}» not found.", _findForm.LookFor));
                e.RetVal = true;
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
                completionModel.AppendValues(item.IsEvent ? functionPixbuf : propertyPixbuf, item.Name, item.Units, item.TypeName, item.Descr, item.ParamString);
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
                textEditor.GdkWindow.GetOrigin(out x, out y);
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
            {
                textEditor.Document.ReadOnly = false;
                textEditor.InsertAtCaret(insertText);
            }
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

        private void OnTextBoxEnter(object o, FocusInEventArgs args)
        {
            ((o as Widget).Toplevel as Gtk.Window).AddAccelGroup(accel);
        }

        private void OnTextBoxLeave(object o, EventArgs e)
        {
            ((o as Widget).Toplevel as Gtk.Window).RemoveAccelGroup(accel);
            if (LeaveEditor != null)
                LeaveEditor.Invoke(this, e);
        }

        #region Code related to Edit menu

        private void DoPopup(Gdk.EventButton b)
        {
            Popup.Popup();
        }

        public void AddMenuItem(string menuItemText, System.EventHandler onClick)
        {
            MenuItem item = new MenuItem(menuItemText);
            item.Activated += onClick;
            Popup.Append(item);
            Popup.ShowAll();
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        public void AddContextSeparator()
        {
            Popup.Append(new SeparatorMenuItem());
        }

        /// <summary>
        /// Add an action (on context menu) on the text area.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        public void AddContextActionWithAccel(string menuItemText, System.EventHandler onClick, string shortcut)
        {
            ImageMenuItem item = new ImageMenuItem(menuItemText);
            if (!String.IsNullOrEmpty(shortcut))
            {
                string keyName = String.Empty;
                Gdk.ModifierType modifier = Gdk.ModifierType.None;
                string[] keyNames = shortcut.Split(new Char[] { '+' });
                foreach (string name in keyNames)
                {
                    if (name == "Ctrl")
                        modifier |= Gdk.ModifierType.ControlMask;
                    else if (name == "Shift")
                        modifier |= Gdk.ModifierType.ShiftMask;
                    else if (name == "Alt")
                        modifier |= Gdk.ModifierType.Mod1Mask;
                    else if (name == "Del")
                        keyName = "Delete";
                    else
                        keyName = name;
                }
                try
                {
                    Gdk.Key accelKey = (Gdk.Key)Enum.Parse(typeof(Gdk.Key), keyName, false);
                    item.AddAccelerator("activate", accel, (uint)accelKey, modifier, AccelFlags.Visible);
                }
                catch
                {
                }
            }
            item.Activated += onClick;
            Popup.Append(item);
            Popup.ShowAll();
        }

        private void OnCut(object sender, EventArgs e)
        {
            ClipboardActions.Cut(textEditor.TextArea.GetTextEditorData());
        }

        private void OnCopy(object sender, EventArgs e)
        {
            ClipboardActions.Copy(textEditor.TextArea.GetTextEditorData());
        }

        private void OnPaste(object sender, EventArgs e)
        {
            ClipboardActions.Paste(textEditor.TextArea.GetTextEditorData());
        }

        private void OnDelete(object sender, EventArgs e)
        {
            DeleteActions.Delete(textEditor.TextArea.GetTextEditorData());
        }

        private void OnUndo(object sender, EventArgs e)
        {
            MiscActions.Undo(textEditor.TextArea.GetTextEditorData());
        }

        private void OnRedo(object sender, EventArgs e)
        {
            MiscActions.Redo(textEditor.TextArea.GetTextEditorData());
        }

        private void OnFind(object sender, EventArgs e)
        {
            _findForm.ShowFor(textEditor, false);
        }

        private void OnReplace(object sender, EventArgs e)
        {
            _findForm.ShowFor(textEditor, true);
        }

        // The following block comes from the example code provided at 
        // http://www.codeproject.com/Articles/30936/Using-ICSharpCode-TextEditor
        // I leave it here because it provides the handlers needed for a popup menu
        // Currently find and replace functions are accessed via keystrokes (e.g, ctrl-F, F3)
        /*
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
