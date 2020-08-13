using System;
using Gdk;
using Gtk;
using UserInterface.EventArguments;
using UserInterface.Extensions;

namespace UserInterface.Views
{
    /// <summary>An interface for a drop down</summary>
    public interface IEditView
    {
        /// <summary>Invoked when the edit box loses focus.</summary>
        event EventHandler Leave;

        /// <summary>Invoked when the user changes the text in the edit box.</summary>
        event EventHandler Changed;

        /// <summary>
        /// Invoked when the user needs intellisense items.
        /// Currently this is only triggered by pressing control-space.
        /// </summary>
        event EventHandler<NeedContextItemsArgs> IntellisenseItemsNeeded;

        /// <summary>Gets or sets the Text</summary>
        string Text { get; set; }

        /// <summary>Return true if dropdown is visible.</summary>
        bool Visible { get; set; }

        /// <summary>
        /// Gets the offset of the cursor in the textbox.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Inserts the selected text at the cursor, replacing all text
        /// before the cursor and after the most recent character which
        /// is not an opening square bracket.
        /// </summary>
        /// <param name="text">The text to be inserted.</param>
        void InsertAtCursorInSquareBrackets(string text);

        /// <summary>
        /// Insert text at the cursor.
        /// </summary>
        /// <param name="text">The text to be inserted.</param>
        void InsertAtCursor(string text);

        /// <summary>
        /// Inserts a completion option, replacing the half-typed trigger word
        /// for which we have generated completion options.
        /// </summary>
        /// <param name="text">Text to be inserted.</param>
        /// <param name="triggerWord">Incomplete word to be replaced.</param>
        void InsertCompletionOption(string text, string triggerWord);
    }

    /// <summary>A drop down view.</summary>
    public class EditView : ViewBase, IEditView
    {
        /// <summary>Invoked when the edit box loses focus.</summary>
        public event EventHandler Leave;
        
        /// <summary>Invoked when the user changes the text in the edit box.</summary>
        public event EventHandler Changed;
        
        /// <summary>
        /// Invoked when the user needs intellisense items.
        /// Currently this is only triggered by pressing control-space.
        /// </summary>
        public event EventHandler<NeedContextItemsArgs> IntellisenseItemsNeeded;

        private Entry textentry1;

        /// <summary>Constructor</summary>
        public EditView() { }

        /// <summary>Constructor</summary>
        public EditView(ViewBase owner) : base(owner)
        {
            Initialise(owner, new Entry());
        }

        /// <summary>Constructor</summary>
        public EditView(ViewBase owner, Entry e) : base(owner)
        {
            Initialise(owner, e);
        }

        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            textentry1 = (Gtk.Entry)gtkControl;
            mainWidget = textentry1;
            textentry1.Changed += OnChanged;
            textentry1.FocusOutEvent += OnLeave;
            textentry1.KeyPressEvent += OnKeyPress;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>
        /// Gets the offset of the cursor in the textbox.
        /// </summary>
        public int Offset
        {
            get
            {
                return textentry1.CursorPosition;
            }
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                textentry1.FocusOutEvent -= OnLeave;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                textentry1.Changed -= OnChanged;
                textentry1.FocusOutEvent -= OnLeave;
                textentry1.KeyPressEvent -= OnKeyPress;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private string lastText = String.Empty;

        /// <summary>Gets or sets the Text.</summary>
        public string Text
        {
            get
            {
                lastText = textentry1.Text;
                return textentry1.Text;
            }
            set
            {
                if (value == null)
                    value = String.Empty;
                textentry1.Text = value;
                lastText = value;
            }
        }

        /// <summary>Return true if dropdown is visible.</summary>
        public bool Visible
        {
            get { return textentry1.Visible; }
            set { textentry1.Visible = value; }
        }

        /// <summary>User has changed the selection.</summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnLeave(object sender, EventArgs e)
        {
            try
            {
                if (Leave != null && textentry1.Text != lastText)
                {
                    lastText = textentry1.Text;
                    Leave.Invoke(this, e);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user presses a key while the input text box has focus.
        /// Invokes the intellisense handler if the user pressed one of the 
        /// intellisense keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [GLib.ConnectBefore]
        private void OnKeyPress(object sender, KeyPressEventArgs args)
        {
            try
            {
                bool controlSpace = (args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask && args.Event.Key == Gdk.Key.space;
                bool controlShiftSpace = controlSpace && (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;
                bool isPeriod = args.Event.Key == Gdk.Key.period;
                if (isPeriod || controlSpace || controlShiftSpace)
                {
                    if (IntellisenseItemsNeeded != null)
                    {
                        int x, y;
                        textentry1.GetGdkWindow().GetOrigin(out x, out y);
                        System.Drawing.Point coordinates = new System.Drawing.Point(x, y + textentry1.HeightRequest);
                        NeedContextItemsArgs e = new NeedContextItemsArgs()
                        {
                            Coordinates = coordinates,
                            Code = textentry1.Text,
                            ControlSpace = controlSpace,
                            ControlShiftSpace = controlShiftSpace,
                            Offset = Offset,
                            ColNo = this.textentry1.CursorPosition
                        };
                        lastText = textentry1.Text;
                        IntellisenseItemsNeeded.Invoke(this, e);
                    }
                }
                else if ((args.Event.Key & Gdk.Key.Return) == Gdk.Key.Return)
                {
                    OnLeave(this, EventArgs.Empty);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// User has left the edit box.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void OnLeave(object o, FocusOutEventArgs args)
        {
            try
            {
                OnLeave(o, new EventArgs());
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void OnChanged(object sender, EventArgs e)
        {
            try
            {
                Changed?.Invoke(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Inserts a completion option, replacing the half-typed trigger word
        /// for which we have generated completion options.
        /// </summary>
        /// <param name="text">Text to be inserted.</param>
        /// <param name="triggerWord">Incomplete word to be replaced.</param>
        public void InsertCompletionOption(string text, string triggerWord)
        {
            if (string.IsNullOrEmpty(text))
                return;
            if (string.IsNullOrEmpty(triggerWord))
            {
                textentry1.Text += text;
                textentry1.Position = textentry1.Text.Length;
            }
            else if (!textentry1.Text.Contains(triggerWord))
                textentry1.Text += text;
            else
            {
                string textBeforeCursor = textentry1.Text.Substring(0, Offset);
                string textAfterCursor = textentry1.Text.Substring(Offset);
                int index = textBeforeCursor.LastIndexOf(triggerWord);
                if (index >= 0)
                {
                    string textBeforeWord = textBeforeCursor.Substring(0, index);
                    string textAfterWord = textBeforeCursor.Substring(index + triggerWord.Length);
                    textentry1.Text = textBeforeWord + triggerWord + text + textAfterWord + textAfterCursor;
                    textentry1.Position = textBeforeWord.Length + text.Length;
                }
                else
                {
                    // I can't imagine how this could ever happen, but it doesn't hurt to be prepared.
                    textentry1.Text = textBeforeCursor + text + textAfterCursor;
                }
            }
        }

        /// <summary>
        /// Inserts the selected text at the cursor, replacing all text
        /// before the cursor and after the most recent character which
        /// is not an opening square bracket.
        /// </summary>
        /// <param name="text">The text to be inserted.</param>
        public void InsertAtCursorInSquareBrackets(string text)
        {
            int offset = IndexOfNot(textentry1.Text.Substring(0, Offset), '[');
            if (offset < 0)
                offset = Offset;
            textentry1.Text = textentry1.Text.Substring(0, offset) + text + textentry1.Text.Substring(Offset);
            textentry1.Position = offset + text.Length;
        }

        /// <summary>
        /// Insert text at the cursor.
        /// </summary>
        /// <param name="text">The text to be inserted.</param>
        public void InsertAtCursor(string text)
        {
            int pos = Offset + text.Length;
            textentry1.Text = textentry1.Text.Substring(0, Offset) + text + textentry1.Text.Substring(Offset);
            textentry1.Position = pos;
        }

        /// <summary>
        /// Gets the index of the first character in a string which is
        /// not a specific character.
        /// </summary>
        /// <param name="word">String to check.</param>
        /// <param name="charToAvoid">Get index of first character which is not this.</param>
        /// <returns>Index or -1 if nothing found.</returns>
        private int IndexOfNot(string word, char charToAvoid)
        {
            if (string.IsNullOrEmpty(word))
                return -1;

            for (int i = 0; i < word.Length; i++)
            {
                if (word[i] != charToAvoid)
                    return i;
            }
            return -1;
        }

        public void EndEdit()
        {
            if (textentry1.IsFocus)
                OnLeave(this, (EventArgs)null);
        }
    }
}
