using System;
using System.Drawing;
using Gtk;
using System.Collections.Generic;

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
        bool WordWrap { get; set; }

        void Export(int width, int height, Graphics graphics);
    }

    /// <summary>
    /// The Presenter for a Memo component.
    /// </summary>
    public class MemoView : ViewBase, IMemoView
    {
        public event EventHandler<EditorArgs> MemoLeave;
        public event EventHandler<EditorArgs> MemoChange;

        private VBox vbox1 = null;
        public TextView textView = null;
        private Label label1 = null;
        private Button helpBtn = null;

        private class MenuInfo
        {
            public string menuText;
            public EventHandler action;
        }

        List<MenuInfo> menuItemList = new List<MenuInfo>();
        
        public MemoView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.MemoView.glade");
            vbox1 = (VBox)builder.GetObject("vbox1");
            textView = (TextView)builder.GetObject("textView");
            label1 = (Label)builder.GetObject("label1");
            helpBtn = (Button)builder.GetObject("buttonHelp");
            helpBtn.Image = new Gtk.Image(new Gdk.Pixbuf(null, "ApsimNG.Resources.help.png", 20, 20));
            helpBtn.ImagePosition = PositionType.Right;
            helpBtn.Image.Visible = true;
            helpBtn.Clicked += HelpBtn_Clicked;
            _mainWidget = vbox1;
            textView.ModifyFont(Pango.FontDescription.FromString("monospace"));
            textView.FocusOutEvent += richTextBox1_Leave;
            textView.Buffer.Changed += richTextBox1_TextChanged;
            textView.PopulatePopup += TextView_PopulatePopup;
            textView.ButtonPressEvent += TextView_ButtonPressEvent;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        // Let a right click move the cursor if we're about to display a popup menu,
        // so the popup has the expected context
        [GLib.ConnectBefore] // Otherwise this is handled internally, and we won't see it
        private void TextView_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (menuItemList.Count > 0 && args.Event.Button == 3)
            {
                int x, y;
                textView.WindowToBufferCoords(TextWindowType.Text, (int)(args.Event.X), (int)(args.Event.Y), out x, out y);
                TextIter where = textView.GetIterAtLocation(x, y);
                textView.Buffer.PlaceCursor(where);
            }
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            textView.FocusOutEvent -= richTextBox1_Leave;
            textView.Buffer.Changed -= richTextBox1_TextChanged;
            textView.PopulatePopup -= TextView_PopulatePopup;
            menuItemList.Clear();
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
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
                foreach (string line in value)
                    textView.Buffer.Insert(ref iter, line + Environment.NewLine);
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

        public bool WordWrap
        {
            get { return textView.WrapMode == WrapMode.Word; }
            set { textView.WrapMode = value ? WrapMode.Word : WrapMode.None;  }
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
            try
            {
                if (MemoChange != null)
                {
                    EditorArgs args = new EditorArgs();
                    args.TextString = textView.Buffer.Text;
                    MemoChange(this, args);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        public void AddContextAction(string buttonText, System.EventHandler onClick)
        {
            MenuInfo item = new MenuInfo();
            item.menuText = buttonText;
            item.action = onClick;
            menuItemList.Add(item);
        }

        private void TextView_PopulatePopup(object o, PopulatePopupArgs args)
        {
            if (menuItemList.Count > 0)
            {
                foreach (Widget w in args.Menu)
                {
                    args.Menu.Remove(w);
                    w.Destroy();
                }
                foreach (MenuInfo item in menuItemList)
                {
                    MenuItem menuItem = new MenuItem(item.menuText);
                    menuItem.Activated += item.action;
                    menuItem.Visible = true;
                    args.Menu.Append(menuItem);
                }
                args.RetVal = true;
            }
        }

        /// <summary>
        /// Return the current cursor position in the memo.
        /// </summary>
        public Point CurrentPosition
        {
            get
            {
                TextIter cursorIter = textView.Buffer.GetIterAtMark(textView.Buffer.InsertMark);
                int lineNumber = cursorIter.Line;
                return new Point(cursorIter.Offset, cursorIter.Line);
            }
        }

        /// <summary>
        /// Export the memo to the specified 'graphics'
        /// </summary>
        public void Export(int width, int height, Graphics graphics)
        {
            /* TBI
            float x = 10;
            float y = 0;
            int charpos = 0;
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
        private void HelpBtn_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "https://apsimnextgeneration.netlify.com/usage/memo/";
            process.Start();
            // Forms.HelpForm form = Forms.HelpForm.GetHelpForm();
            // form.Show("https://apsimnextgeneration.netlify.com/usage/memo/");
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
