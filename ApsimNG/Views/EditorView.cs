namespace UserInterface.Views
{
    using System;
    using System.Reflection;
    using EventArguments;
    using Gtk;
    using Mono.TextEditor;
    using Utility;
    using Cairo;
    using System.Globalization;
    using Mono.TextEditor.Highlighting;

    /// <summary>
    /// What sort of text is this editor displaying?
    /// This is used to determine syntax highlighting rules.
    /// We could potentially add more options here in future if, say,
    /// we were to implement a python manager component.
    /// </summary>
    public enum EditorType
    {
        /// <summary>
        /// C# manager script.
        /// </summary>
        ManagerScript,

        /// <summary>
        /// Report.
        /// </summary>
        Report,

        /// <summary>
        /// Anything else - this will disable syntax highlighting.
        /// </summary>
        Other
    };

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
        /// Invoked when the user changes the style.
        /// </summary>
        event EventHandler StyleChanged;

        /// <summary>
        /// Gets or sets the text property to get and set the content of the editor.
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Gets or sets the lines property to get and set the lines in the editor.
        /// </summary>
        string[] Lines { get; set; }

        /// <summary>
        /// Controls syntax highlighting mode.
        /// </summary>
        EditorType Mode { get; set; }

        /// <summary>
        /// Gets or sets the characters that bring up the intellisense context menu.
        /// </summary>
        string IntelliSenseChars { get; set; }

        /// <summary>
        /// Gets the current line number
        /// </summary>
        int CurrentLineNumber { get; }

        /// <summary>
        /// Gets or sets the current location of the caret (column and line)
        /// </summary>
        System.Drawing.Rectangle Location { get; set; }
        
        /// <summary>
        /// Add a separator line to the context menu
        /// </summary>
        MenuItem AddContextSeparator();

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        /// <param name="shortcut">Describes the key to use as the accelerator</param>
        MenuItem AddContextActionWithAccel(string menuItemText, System.EventHandler onClick, string shortcut);

        /// <summary>
        /// Offset of the caret from the beginning of the text editor.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Returns true iff this text editor has the focus
        /// (ie it can receive keyboard input).
        /// </summary>
        bool HasFocus { get; }

        /// <summary>
        /// Inserts text at a given offset in the editor.
        /// </summary>
        /// <param name="text">Text to be inserted.</param>
        void InsertAtCaret(string text);

        /// <summary>
        /// Inserts a new completion option at the caret, potentially overwriting a partially-completed word.
        /// </summary>
        /// <param name="triggerWord">
        /// Word to be overwritten. May be empty.
        /// This function will overwrite the last occurrence of this word before the caret.
        /// </param>
        /// <param name="completionOption">Completion option to be inserted.</param>
        void InsertCompletionOption(string completionOption, string triggerWord);

        /// <summary>
        /// Gets the location (in screen coordinates) of the cursor.
        /// </summary>
        /// <returns>Tuple, where item 1 is the x-coordinate and item 2 is the y-coordinate.</returns>
        System.Drawing.Point GetPositionOfCursor();

        /// <summary>
        /// Redraws the text editor.
        /// </summary>
        void Refresh();
    }

    /// <summary>
    /// This class provides an intellisense editor and has the option of syntax highlighting keywords.
    /// </summary>
    public class EditorView : ViewBase, IEditorView
    {
        /// <summary>
        /// The find-and-replace form
        /// </summary>
        private FindAndReplaceForm findForm = new FindAndReplaceForm();

        /// <summary>
        /// Scrolled window
        /// </summary>
        private ScrolledWindow scroller;

        /// <summary>
        /// The main text editor
        /// </summary>
        private TextEditor textEditor;

        /// <summary>
        /// The popup menu options on the editor
        /// </summary>
        private Menu popupMenu = new Menu();

        /// <summary>
        /// Menu accelerator group
        /// </summary>
        private AccelGroup accel = new AccelGroup();

        /// <summary>
        /// Horizontal scroll position
        /// </summary>
        private int horizScrollPos = -1;

        /// <summary>
        /// Vertical scroll position
        /// </summary>
        private int vertScrollPos = -1;

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

        /// <summary>
        /// Invoked when the user changes the style.
        /// </summary>
        public event EventHandler StyleChanged;

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
                if (Mode == EditorType.ManagerScript)
                    textEditor.Document.MimeType = "text/x-csharp";
                else if (Mode == EditorType.Report)
                {
                    if (SyntaxModeService.GetSyntaxMode(textEditor.Document, "text/x-apsimreport") == null)
                        LoadReportSyntaxMode();
                    textEditor.Document.MimeType = "text/x-apsimreport";
                }
                textEditor.Document.SetNotDirtyState();
            }
        }

        /// <summary>
        /// Performs a one-time registration of the report syntax highlighting rules.
        /// This will only run once, the first time the user clicks on a report node.
        /// </summary>
        private void LoadReportSyntaxMode()
        {
            string resource = "ApsimNG.Resources.SyntaxHighlighting.Report.xml";
            using (System.IO.Stream s = GetType().Assembly.GetManifestResourceStream(resource))
            {
                ProtoTypeSyntaxModeProvider p = new ProtoTypeSyntaxModeProvider(SyntaxMode.Read(s));
                SyntaxModeService.InstallSyntaxMode("text/x-apsimreport", p);
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
                string st = string.Empty;
                if (value != null)
                {
                    foreach (string avalue in value)
                    {
                        if (st != string.Empty)
                            st += textEditor.EolMarker;
                        st += avalue;
                    }
                }
                Text = st;
            }
        }

        /// <summary>
        /// Controls the syntax highlighting scheme.
        /// </summary>
        public EditorType Mode { get; set; }

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

        private MenuItem styleMenu;
        private MenuItem styleSeparator;

        /// <summary>
        /// Gets or sets the current location of the caret (column and line) and the current scrolling position
        /// This isn't really a Rectangle, but the Rectangle class gives us a convenient
        /// way to store these values.
        /// </summary>
        public System.Drawing.Rectangle Location
        {
            get
            {
                DocumentLocation loc = textEditor.Caret.Location;
                return new System.Drawing.Rectangle(loc.Column, loc.Line, Convert.ToInt32(scroller.Hadjustment.Value, CultureInfo.InvariantCulture), Convert.ToInt32(scroller.Vadjustment.Value, CultureInfo.InvariantCulture));
            }

            set
            {
                textEditor.Caret.Location = new DocumentLocation(value.Y, value.X);
                horizScrollPos = value.Width;
                vertScrollPos = value.Height;

                // Unfortunately, we often can't set the scroller adjustments immediately, as they may not have been set up yet
                // We make these calls to set the position if we can, but otherwise we'll just hold on to the values until the scrollers are ready
                Hadjustment_Changed(this, null);
                Vadjustment_Changed(this, null);
            }
        }

        /// <summary>
        /// Offset of the caret from the beginning of the text editor.
        /// </summary>
        public int Offset
        {
            get
            {
                return textEditor.Caret.Offset;
            }
        }

        /// <summary>
        /// Returns true iff this text editor has the focus
        /// (ie it can receive keyboard input).
        /// </summary>
        public bool HasFocus
        {
            get
            {
                return textEditor.HasFocus;
            }
        }

        /// <summary>
        /// Default constructor that configures the Completion form.
        /// </summary>
        /// <param name="owner">The owner view</param>
        public EditorView(ViewBase owner) : base(owner)
        {
            scroller = new ScrolledWindow();
            textEditor = new TextEditor();
            scroller.Add(textEditor);
            mainWidget = scroller;
            Mono.TextEditor.CodeSegmentPreviewWindow.CodeSegmentPreviewInformString = "";
            Mono.TextEditor.TextEditorOptions options = new Mono.TextEditor.TextEditorOptions();
            options.EnableSyntaxHighlighting = true;
            options.ColorScheme = Configuration.Settings.EditorStyleName;
            options.Zoom = Configuration.Settings.EditorZoom;
            options.HighlightCaretLine = true;
            options.EnableSyntaxHighlighting = true;
            options.HighlightMatchingBracket = true;
            textEditor.Options = options;
            textEditor.Options.Changed += EditorOptionsChanged;
            textEditor.Options.ColorScheme = Configuration.Settings.EditorStyleName;
            textEditor.Options.Zoom = Configuration.Settings.EditorZoom;
            textEditor.TextArea.DoPopupMenu = DoPopup;
            textEditor.Document.LineChanged += OnTextHasChanged;
            textEditor.TextArea.FocusInEvent += OnTextBoxEnter;
            textEditor.TextArea.FocusOutEvent += OnTextBoxLeave;
            textEditor.TextArea.KeyPressEvent += OnKeyPress;
            scroller.Hadjustment.Changed += Hadjustment_Changed;
            scroller.Vadjustment.Changed += Vadjustment_Changed;
            mainWidget.Destroyed += _mainWidget_Destroyed;

            AddContextActionWithAccel("Cut", OnCut, "Ctrl+X");
            AddContextActionWithAccel("Copy", OnCopy, "Ctrl+C");
            AddContextActionWithAccel("Paste", OnPaste, "Ctrl+V");
            AddContextActionWithAccel("Delete", OnDelete, "Delete");
            AddContextSeparator();
            AddContextActionWithAccel("Undo", OnUndo, "Ctrl+Z");
            AddContextActionWithAccel("Redo", OnRedo, "Ctrl+Y");
            AddContextActionWithAccel("Find", OnFind, "Ctrl+F");
            AddContextActionWithAccel("Replace", OnReplace, "Ctrl+H");
            styleSeparator = AddContextSeparator();
            styleMenu = AddMenuItem("Use style", null);
            Menu styles = new Menu();

            // find all the editor styles and add sub menu items to the popup
            string[] styleNames = Mono.TextEditor.Highlighting.SyntaxModeService.Styles;
            Array.Sort(styleNames, StringComparer.InvariantCulture);
            foreach (string name in styleNames)
            {
                CheckMenuItem subItem = new CheckMenuItem(name);
                if (string.Compare(name, options.ColorScheme, true) == 0)
                    subItem.Toggle();
                subItem.Activated += OnChangeEditorStyle;
                subItem.Visible = true;
                styles.Append(subItem);
            }
            styleMenu.Submenu = styles;

            IntelliSenseChars = ".";
        }

        /// <summary>
        /// Cleanup events
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                textEditor.Document.LineChanged -= OnTextHasChanged;
                textEditor.TextArea.FocusInEvent -= OnTextBoxEnter;
                textEditor.TextArea.FocusOutEvent -= OnTextBoxLeave;
                textEditor.TextArea.KeyPressEvent -= OnKeyPress;
                scroller.Hadjustment.Changed -= Hadjustment_Changed;
                scroller.Vadjustment.Changed -= Vadjustment_Changed;
                textEditor.Options.Changed -= EditorOptionsChanged;
                mainWidget.Destroyed -= _mainWidget_Destroyed;

                // It's good practice to disconnect all event handlers, as it makes memory leaks
                // less likely. However, we may not "own" the event handlers, so how do we 
                // know what to disconnect?
                // We can do this via reflection. Here's how it currently can be done in Gtk#.
                // Windows.Forms would do it differently.
                // This may break if Gtk# changes the way they implement event handlers.
                foreach (Widget w in popupMenu)
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

                popupMenu.Destroy();
                accel.Dispose();
                textEditor.Destroy();
                textEditor = null;
                findForm.Destroy();
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The vertical position has changed
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void Vadjustment_Changed(object sender, EventArgs e)
        {
            try
            {
                if (vertScrollPos > 0 && vertScrollPos < scroller.Vadjustment.Upper)
                {
                    scroller.Vadjustment.Value = vertScrollPos;
                    scroller.Vadjustment.ChangeValue();
                    vertScrollPos = -1;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The horizontal position has changed
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void Hadjustment_Changed(object sender, EventArgs e)
        {
            try
            {
                if (horizScrollPos > 0 && horizScrollPos < scroller.Hadjustment.Upper)
                {
                    scroller.Hadjustment.Value = horizScrollPos;
                    scroller.Hadjustment.ChangeValue();
                    horizScrollPos = -1;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
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
            try
            {
                e.RetVal = false;
                char keyChar = (char)Gdk.Keyval.ToUnicode(e.Event.KeyValue);
                Gdk.ModifierType ctlModifier = !APSIM.Shared.Utilities.ProcessUtilities.CurrentOS.IsMac ? Gdk.ModifierType.ControlMask
                    //Mac window manager already uses control-scroll, so use command
                    //Command might be either meta or mod1, depending on GTK version
                    : (Gdk.ModifierType.MetaMask | Gdk.ModifierType.Mod1Mask);

                bool controlSpace = IsControlSpace(e.Event);
                bool controlShiftSpace = IsControlShiftSpace(e.Event);
                string textBeforePeriod = GetWordBeforePosition(textEditor.Caret.Offset);
                double x; // unused, but needed as an out parameter.
                if (e.Event.Key == Gdk.Key.F3)
                {
                    if (string.IsNullOrEmpty(findForm.LookFor))
                        findForm.ShowFor(textEditor, false);
                    else
                        findForm.FindNext(true, (e.Event.State & Gdk.ModifierType.ShiftMask) == 0, string.Format("Search text «{0}» not found.", findForm.LookFor));
                    e.RetVal = true;
                }
                // If the text before the period is not a number and the user pressed either one of the intellisense characters or control-space:
                else if (!double.TryParse(textBeforePeriod.Replace(".", ""), out x) && (IntelliSenseChars.Contains(keyChar.ToString()) || controlSpace || controlShiftSpace) )
                {
                    // If the user entered a period, we need to take that into account when generating intellisense options.
                    // To do this, we insert a period manually and stop the Gtk signal from propagating further.
                    e.RetVal = true;
                    if (keyChar == '.')
                    {
                        textEditor.InsertAtCaret(keyChar.ToString());

                        // Process all events in the main loop, so that the period is inserted into the text editor.
                        while (GLib.MainContext.Iteration()) ;
                    }
                    NeedContextItemsArgs args = new NeedContextItemsArgs
                    {
                        Coordinates = GetPositionOfCursor(),
                        Code = textEditor.Text,
                        Offset = this.Offset,
                        ControlSpace = controlSpace,
                        ControlShiftSpace = controlShiftSpace,
                        LineNo = textEditor.Caret.Line,
                        ColNo = textEditor.Caret.Column - 1
                    };

                    ContextItemsNeeded?.Invoke(this, args);
                }
                else if ((e.Event.State & ctlModifier) != 0)
                {
                    switch (e.Event.Key)
                    {
                        case Gdk.Key.Key_0: textEditor.Options.ZoomReset(); e.RetVal = true; break;
                        case Gdk.Key.KP_Add:
                        case Gdk.Key.plus: textEditor.Options.ZoomIn(); e.RetVal = true; break;
                        case Gdk.Key.KP_Subtract:
                        case Gdk.Key.minus: textEditor.Options.ZoomOut(); e.RetVal = true; break;
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Checks whether a keypress is a control+space event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        /// <returns>True iff the event represents a control+space click.</returns>
        private bool IsControlSpace(Gdk.EventKey e)
        {
            return Gdk.Keyval.ToUnicode(e.KeyValue) == ' ' && (e.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask;
        }

        /// <summary>
        /// Checks whether a keypress is a control-shift-space event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        /// <returns>True iff the event represents a control + shift + space click.</returns>
        private bool IsControlShiftSpace(Gdk.EventKey e)
        {
            return IsControlSpace(e) && (e.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;
        }

        /// <summary>
        /// Retrieve the word before the specified character position. 
        /// </summary>
        /// <param name="pos">Position in the editor</param>
        /// <returns>The position of the word</returns>
        private string GetWordBeforePosition(int pos)
        {
            if (pos == 0)
                return string.Empty;

            int posDelimiter = textEditor.Text.LastIndexOfAny(" \r\n(+-/*".ToCharArray(), pos - 1);
            return textEditor.Text.Substring(posDelimiter + 1, pos - posDelimiter - 1).TrimEnd(".".ToCharArray());
        }

        /// <summary>
        /// Gets the location (in screen coordinates) of the cursor.
        /// </summary>
        /// <returns>Tuple, where item 1 is the x-coordinate and item 2 is the y-coordinate.</returns>
        public System.Drawing.Point GetPositionOfCursor()
        {
            Point p = textEditor.TextArea.LocationToPoint(textEditor.Caret.Location);
            p.Y += (int)textEditor.LineHeight;
            // Need to convert to screen coordinates....
            int x, y, frameX, frameY;
            MasterView.MainWindow.GetOrigin(out frameX, out frameY);
            textEditor.TextArea.TranslateCoordinates(mainWidget.Toplevel, p.X, p.Y, out x, out y);

            return new System.Drawing.Point(x + frameX, y + frameY);
        }

        /// <summary>
        /// Redraws the text editor.
        /// </summary>
        public void Refresh()
        {
            textEditor.Options.ColorScheme = Configuration.Settings.EditorStyleName;
            textEditor.QueueDraw();
        }

        /// <summary>
        /// Hide the completion window.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void HideCompletionWindow(object sender, EventArgs e)
        {
            try
            {
                textEditor.Document.ReadOnly = false;
                textEditor.GrabFocus();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Inserts a new completion option at the caret, potentially overwriting a partially-completed word.
        /// </summary>
        /// <param name="triggerWord">
        /// Word to be overwritten. May be empty.
        /// This function will overwrite the last occurrence of this word before the caret.
        /// </param>
        /// <param name="completionOption">Completion option to be inserted.</param>
        public void InsertCompletionOption(string completionOption, string triggerWord)
        {
            if (string.IsNullOrEmpty(completionOption))
                return;

            // If no trigger word provided, insert at caret.
            if (string.IsNullOrEmpty(triggerWord))
            {
                int offset = Offset + completionOption.Length;
                textEditor.InsertAtCaret(completionOption);
                textEditor.Caret.Offset = offset;
                return;
            }

            // If trigger word is entire text, replace the entire text.
            if (textEditor.Text == triggerWord)
            {
                textEditor.Text = completionOption;
                textEditor.Caret.Offset = completionOption.Length;
                return;
            }

            // Overwrite the last occurrence of this word before the caret.
            int index = textEditor.GetTextBetween(0, Offset).LastIndexOf(triggerWord);
            if (index < 0)
                // If text does not contain trigger word, isnert at caret.
                textEditor.InsertAtCaret(completionOption);

            string textBeforeTriggerWord = textEditor.Text.Substring(0, index);

            string textAfterTriggerWord = "";
            if (textEditor.Text.Length > index + triggerWord.Length)
                textAfterTriggerWord = textEditor.Text.Substring(index + triggerWord.Length);

            // Changing the text property of the text editor will reset the scroll
            // position. To work around this, we record the scroll position before
            // we change the text then reset it manually afterwards.
            double verticalPosition = scroller.Vadjustment.Value;
            double horizontalPosition = scroller.Hadjustment.Value;

            textEditor.Text = textBeforeTriggerWord + completionOption + textAfterTriggerWord;
            textEditor.Caret.Offset = index + completionOption.Length;

            scroller.Vadjustment.Value = verticalPosition;
            scroller.Hadjustment.Value = horizontalPosition;
        }

        /// <summary>
        /// Insert the currently selected completion item into the text box.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        public void InsertAtCaret(string text)
        {
            textEditor.Document.ReadOnly = false;
            string textToCaret = textEditor.Text.Substring(0, Offset);
            if (textToCaret.LastIndexOf('.') != Offset - 1)
            {
                string textBeforeCaret = textEditor.Text.Substring(0, Offset);
                // TODO : insert text at the correct location
                // Currently, if we half-type a word, then hit control-space, the word will be inserted at the caret.
                textEditor.Text = textEditor.Text.Substring(0, textEditor.Text.LastIndexOf('.')) + textEditor.Text.Substring(Offset);
            }
            textEditor.InsertAtCaret(text);
            textEditor.GrabFocus();
        }

        /// <summary>
        /// User has changed text. Invoke our OnTextChanged event.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnTextHasChanged(object sender, EventArgs e)
        {
            try
            {
                if (TextHasChangedByUser != null)
                    TextHasChangedByUser(sender, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Entering the textbox event
        /// </summary>
        /// <param name="o">The calling object</param>
        /// <param name="args">The arguments</param>
        private void OnTextBoxEnter(object o, FocusInEventArgs args)
        {
            try
            {
                ((o as Widget).Toplevel as Gtk.Window).AddAccelGroup(accel);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Leaving the textbox event
        /// </summary>
        /// <param name="o">The calling object</param>
        /// <param name="e">The event arguments</param>
        private void OnTextBoxLeave(object o, EventArgs e)
        {
            try
            {
                ((o as Widget).Toplevel as Gtk.Window).RemoveAccelGroup(accel);
                if (LeaveEditor != null)
                    LeaveEditor.Invoke(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        #region Code related to Edit menu

        /// <summary>
        /// Show the popup menu
        /// </summary>
        /// <param name="b">The button</param>
        private void DoPopup(Gdk.EventButton b)
        {
            popupMenu.Popup();
        }

        /// <summary>
        /// Add a menu item to the menu
        /// </summary>
        /// <param name="menuItemText">Menu item caption</param>
        /// <param name="onClick">Event handler</param>
        /// <returns>The menu item that was created</returns>
        public MenuItem AddMenuItem(string menuItemText, System.EventHandler onClick)
        {
            MenuItem item = new MenuItem(menuItemText);
            if (onClick != null)
                item.Activated += onClick;
            popupMenu.Append(item);
            popupMenu.ShowAll();

            return item;
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        public MenuItem AddContextSeparator()
        {
            MenuItem result = new SeparatorMenuItem();
            popupMenu.Append(result);
            return result;
        }

        /// <summary>
        /// Add an action (on context menu) on the text area.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        /// <param name="shortcut">The shortcut string</param>
        public MenuItem AddContextActionWithAccel(string menuItemText, System.EventHandler onClick, string shortcut)
        {
            ImageMenuItem item = new ImageMenuItem(menuItemText);
            if (!string.IsNullOrEmpty(shortcut))
            {
                string keyName = string.Empty;
                Gdk.ModifierType modifier = Gdk.ModifierType.None;
                string[] keyNames = shortcut.Split(new char[] { '+' });
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
            if (onClick != null)
                item.Activated += onClick;
            popupMenu.Append(item);
            popupMenu.ShowAll();
            return item;
        }

        /// <summary>
        /// The cut menu handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnCut(object sender, EventArgs e)
        {
            try
            {
                ClipboardActions.Cut(textEditor.TextArea.GetTextEditorData());
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The Copy menu handler 
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnCopy(object sender, EventArgs e)
        {
            try
            {
                ClipboardActions.Copy(textEditor.TextArea.GetTextEditorData());
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The Past menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnPaste(object sender, EventArgs e)
        {
            try
            {
                ClipboardActions.Paste(textEditor.TextArea.GetTextEditorData());
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The Delete menu handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnDelete(object sender, EventArgs e)
        {
            try
            {
                DeleteActions.Delete(textEditor.TextArea.GetTextEditorData());
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The Undo menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnUndo(object sender, EventArgs e)
        {
            try
            {
                MiscActions.Undo(textEditor.TextArea.GetTextEditorData());
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The Redo menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnRedo(object sender, EventArgs e)
        {
            try
            {
                MiscActions.Redo(textEditor.TextArea.GetTextEditorData());
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The Find menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnFind(object sender, EventArgs e)
        {
            try
            {
                findForm.ShowFor(textEditor, false);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The Replace menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnReplace(object sender, EventArgs e)
        {
            try
            {
                findForm.ShowFor(textEditor, true);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Changing the editor style menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnChangeEditorStyle(object sender, EventArgs e)
        {
            try
            {
                MenuItem subItem = (MenuItem)sender;
                string caption = ((Gtk.Label)(subItem.Children[0])).LabelProp;

                foreach (CheckMenuItem item in ((Menu)subItem.Parent).Children)
                {
                    item.Activated -= OnChangeEditorStyle;  // stop recursion
                    item.Active = (string.Compare(caption, ((Gtk.Label)item.Children[0]).LabelProp, true) == 0);
                    item.Activated += OnChangeEditorStyle;
                }

                Utility.Configuration.Settings.EditorStyleName = caption;
                textEditor.Options.ColorScheme = caption;
                textEditor.QueueDraw();

                StyleChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handle other changes to editor options. All we're really interested in 
        /// here at present is keeping track of the editor zoom level.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void EditorOptionsChanged(object sender, EventArgs e)
        {
            try
            {
                Utility.Configuration.Settings.EditorZoom = textEditor.Options.Zoom;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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
