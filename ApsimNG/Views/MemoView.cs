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
        event EventHandler StartEdit;

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        void AddContextAction(string buttonText, EventHandler onClick);

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
        public event EventHandler StartEdit;

        private VBox vbox1 = null;
        private HBox hbox1 = null;
        public TextView TextView { get; set; } = null;
        private LinkButton editLabel = null;
        private Button helpBtn = null;
        
        private class MenuInfo
        {
            public string MenuText { get; set; }
            public EventHandler Action { get; set; }
        }

        List<MenuInfo> menuItemList = new List<MenuInfo>();
        
        public MemoView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.MemoView.glade");
            vbox1 = (VBox)builder.GetObject("vbox1");
            hbox1 = (HBox)builder.GetObject("hbox1");
            TextView = (TextView)builder.GetObject("textView");
            editLabel = (LinkButton)builder.GetObject("label1");

            helpBtn = (Button)builder.GetObject("buttonHelp");
            helpBtn.Image = new Gtk.Image(new Gdk.Pixbuf(null, "ApsimNG.Resources.help.png", 20, 20));
            helpBtn.ImagePosition = PositionType.Right;
            helpBtn.Image.Visible = true;
            helpBtn.Clicked += HelpBtn_Clicked;
            mainWidget = vbox1;
            TextView.ModifyFont(Pango.FontDescription.FromString("monospace"));
            TextView.FocusOutEvent += RichTextBox1_Leave;
            TextView.Buffer.Changed += RichTextBox1_TextChanged;
            TextView.PopulatePopup += TextView_PopulatePopup;
            TextView.ButtonPressEvent += TextView_ButtonPressEvent;
            editLabel.Clicked += Memo_StartEdit;

            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        // Let a right click move the cursor if we're about to display a popup menu,
        // so the popup has the expected context
        [GLib.ConnectBefore] // Otherwise this is handled internally, and we won't see it
        private void TextView_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            try
            {
                if (menuItemList.Count > 0 && args.Event.Button == 3)
                {
                    int x, y;
                    TextView.WindowToBufferCoords(TextWindowType.Text, (int)(args.Event.X), (int)(args.Event.Y), out x, out y);
                    TextIter where = TextView.GetIterAtLocation(x, y);
                    TextView.Buffer.PlaceCursor(where);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                TextView.FocusOutEvent -= RichTextBox1_Leave;
                TextView.Buffer.Changed -= RichTextBox1_TextChanged;
                TextView.PopulatePopup -= TextView_PopulatePopup;
                menuItemList.Clear();
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                editLabel.Clicked -= Memo_StartEdit;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Set or get the text of the richedit
        /// </summary>
        public string MemoText
        {
            get { return TextView.Buffer.Text; }
            set { TextView.Buffer.Text = value; }
        }

        /// <summary>
        /// Set or get the lines of the richedit
        /// </summary>
        public string[] MemoLines
        {
            get
            {
                string contents = TextView.Buffer.Text;
                return contents.Split(new string[] { Environment.NewLine, "\r\n", "\n" }, StringSplitOptions.None);
            }
            set
            {
                TextView.Buffer.Clear();
                TextIter iter = TextView.Buffer.EndIter;
                foreach (string line in value)
                    TextView.Buffer.Insert(ref iter, line + Environment.NewLine);
            }
        }

        /// <summary>
        /// Get or set the readonly name of the richedit.
        /// </summary>
        public bool ReadOnly 
        {
            get { return !TextView.Editable; }
            set { TextView.Editable = !value; }
        }

        /// <summary>
        /// Get or set the label text.
        /// </summary>
        public string LabelText 
        {
            get { return editLabel.Label; }
            set { editLabel.Label = value; }
        }

        public bool WordWrap
        {
            get { return TextView.WrapMode == WrapMode.Word; }
            set { TextView.WrapMode = value ? WrapMode.Word : WrapMode.None;  }
        }

        /// <summary>
        /// The memo has been updated and now send the changed text to the model.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RichTextBox1_Leave(object sender, FocusOutEventArgs e)
        {
            try
            {
                if (MemoLeave != null)
                {
                    EditorArgs args = new EditorArgs();
                    args.TextString = TextView.Buffer.Text;
                    MemoLeave(this, args);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// The Edit option has been clicked and noe start the editing of the markdown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Memo_StartEdit(object sender, EventArgs e)
        {
            try
            {
                if (string.CompareOrdinal(editLabel.Label, "Hide editor") == 0)
                    editLabel.Label = "Edit text";
                else
                    editLabel.Label = "Hide editor";
                StartEdit?.Invoke(this, e);
            }
            catch(Exception err)
            {
                ShowError(err);
            }
        }

        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (MemoChange != null)
                {
                    EditorArgs args = new EditorArgs();
                    args.TextString = TextView.Buffer.Text;
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
            item.MenuText = buttonText;
            item.Action = onClick;
            menuItemList.Add(item);
        }

        private void TextView_PopulatePopup(object o, PopulatePopupArgs args)
        {
            try
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
                        MenuItem menuItem = new MenuItem(item.MenuText);
                        menuItem.Activated += item.Action;
                        menuItem.Visible = true;
                        args.Menu.Append(menuItem);
                    }
                    args.RetVal = true;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Return the current cursor position in the memo.
        /// </summary>
        public Point CurrentPosition
        {
            get
            {
                TextIter cursorIter = TextView.Buffer.GetIterAtMark(TextView.Buffer.InsertMark);
                int lineNumber = cursorIter.Line;
                return new Point(cursorIter.Offset, cursorIter.Line);
            }
        }

        /// <summary>
        /// Return the height of the header panel
        /// </summary>
        /// <returns></returns>
        public int HeaderHeight()
        {
            return hbox1.Allocation.Height;
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
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "https://apsimnextgeneration.netlify.com/usage/memo/";
                process.Start();
                // Forms.HelpForm form = Forms.HelpForm.GetHelpForm();
                // form.Show("https://apsimnextgeneration.netlify.com/usage/memo/");
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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
