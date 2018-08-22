using System;
using Gtk;
using UserInterface.EventArguments;

namespace UserInterface.Views
{
    /// <summary>An interface for a drop down</summary>
    public interface IEditView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>
        /// Invoked when the user needs intellisense items.
        /// Currently this is only triggered by pressing control-space.
        /// </summary>
        event EventHandler<NeedContextItemsArgs> IntellisenseItemsNeeded;

        /// <summary>Gets or sets the Text</summary>
        string Value { get; set; }

        /// <summary>Return true if dropdown is visible.</summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// Gets the offset of the cursor in the textbox.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Inserts the selected text at the cursor.
        /// </summary>
        /// <param name="text"></param>
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
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>
        /// Invoked when the user needs intellisense items.
        /// Currently this is only triggered by pressing control-space.
        /// </summary>
        public event EventHandler<NeedContextItemsArgs> IntellisenseItemsNeeded;

        private Entry textentry1;
        
        /// <summary>Constructor</summary>
        public EditView(ViewBase owner) : base(owner)
        {
            textentry1 = new Entry();
            _mainWidget = textentry1;
            textentry1.FocusOutEvent += OnSelectionChanged;
            textentry1.KeyPressEvent += OnKeyPress;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
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
            textentry1.FocusOutEvent -= OnSelectionChanged;
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
        }

        private string lastText = String.Empty;

        /// <summary>Gets or sets the Text.</summary>
        public string Value
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
        public bool IsVisible
        {
            get { return textentry1.Visible; }
            set { textentry1.Visible = value; }
        }

        /// <summary>User has changed the selection.</summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (Changed != null && textentry1.Text != lastText)
            {
                lastText = textentry1.Text;
                Changed.Invoke(this, e);
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
            if ((args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask && args.Event.Key == Gdk.Key.space)
            {
                /*
                Point p = textEditor.TextArea.LocationToPoint(textEditor.Caret.Location);
                p.Y += (int)textEditor.LineHeight;
                // Need to convert to screen coordinates....
                int x, y, frameX, frameY;
                MasterView.MainWindow.GetOrigin(out frameX, out frameY);
                textEditor.TextArea.TranslateCoordinates(_mainWidget.Toplevel, p.X, p.Y, out x, out y);
                */
                if (IntellisenseItemsNeeded != null)
                {
                    int x, y;
                    textentry1.GdkWindow.GetOrigin(out x, out y);
                    Tuple<int, int> coordinates = new Tuple<int, int>(x, y + textentry1.SizeRequest().Height);
                    NeedContextItemsArgs e = new NeedContextItemsArgs()
                    {
                        Coordinates = coordinates,
                        Code = textentry1.Text,
                        Offset = Offset
                    };
                    lastText = textentry1.Text;
                    IntellisenseItemsNeeded.Invoke(this, e);
                }
            }
            else if ((args.Event.Key & Gdk.Key.Return) == Gdk.Key.Return)
            {
                OnSelectionChanged(this, EventArgs.Empty);
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
                textentry1.Text = text;
                textentry1.Position = text.Length;
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
                    textentry1.Text = textBeforeWord + text + textAfterWord + textAfterCursor;
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
        /// <param name="text"></param>
        public void InsertAtCursor(string text)
        {
            int offset = IndexOfNot(textentry1.Text.Substring(0, Offset), '[');
            if (offset < 0)
                offset = Offset;
            textentry1.Text = textentry1.Text.Substring(0, offset) + text + textentry1.Text.Substring(Offset);
            textentry1.Position = offset + text.Length;
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
                OnSelectionChanged(this, null);
        }
    }
}
