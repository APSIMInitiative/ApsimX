using System;
using System.Collections.Generic;
using Gtk;

using GtkSource;

using Cairo;
using UserInterface.Views;
using UserInterface.Extensions;

namespace Utility
{
    public class FindAndReplaceForm
    {
        // Gtk Widgets
        private Window window1 = null;
        private CheckButton chkMatchCase = null;
        private CheckButton chkMatchWholeWord = null;
        private Entry txtLookFor = null;
        private Entry txtReplaceWith = null;
        private Button btnReplace = null;
        private Button btnReplaceAll = null;
        private Button btnHighlightAll = null;
        private Button btnCancel = null;
        private Button btnFindPrevious = null;
        private Button btnFindNext = null;
        private Label lblReplaceWith = null;

        private bool selectionOnly = false;

        public FindAndReplaceForm()
        {
            Builder builder = ViewBase.BuilderFromResource("ApsimNG.Resources.Glade.FindAndReplace.glade");
            window1 = (Window)builder.GetObject("window1");
            chkMatchCase = (CheckButton)builder.GetObject("chkMatchCase");
            chkMatchWholeWord = (CheckButton)builder.GetObject("chkMatchWholeWord");
            txtLookFor = (Entry)builder.GetObject("txtLookFor");
            txtReplaceWith = (Entry)builder.GetObject("txtReplaceWith");
            btnReplace = (Button)builder.GetObject("btnReplace");
            btnReplaceAll = (Button)builder.GetObject("btnReplaceAll");
            btnHighlightAll = (Button)builder.GetObject("btnHighlightAll");
            btnCancel = (Button)builder.GetObject("btnCancel");
            btnFindPrevious = (Button)builder.GetObject("btnFindPrevious");
            btnFindNext = (Button)builder.GetObject("btnFindNext");
            lblReplaceWith = (Label)builder.GetObject("lblReplaceWith");

            btnFindNext.Clicked += BtnFindNext_Click;
            btnFindPrevious.Clicked += BtnFindPrevious_Click;
            btnCancel.Clicked += BtnCancel_Click;
            btnReplace.Clicked += BtnReplace_Click;
            btnReplaceAll.Clicked += BtnReplaceAll_Click;
            btnHighlightAll.Clicked += BtnHighlightAll_Click;
            window1.DeleteEvent += Window1_DeleteEvent;
            window1.Destroyed += Window1_Destroyed;
            AccelGroup agr = new AccelGroup();
            btnCancel.AddAccelerator("activate", agr, new AccelKey(Gdk.Key.Escape, Gdk.ModifierType.None, AccelFlags.Visible));
            window1.AddAccelGroup(agr);
        }


        private void Window1_Destroyed(object sender, EventArgs e)
        {
            btnFindNext.Clicked -= BtnFindNext_Click;
            btnFindPrevious.Clicked -= BtnFindPrevious_Click;
            btnCancel.Clicked -= BtnCancel_Click;
            btnReplace.Clicked -= BtnReplace_Click;
            btnReplaceAll.Clicked -= BtnReplaceAll_Click;
            window1.DeleteEvent -= Window1_DeleteEvent;
            window1.Destroyed -= Window1_Destroyed;
            Utility.GtkUtil.DetachAllHandlers(window1);
        }

        public void Destroy()
        {
            Utility.GtkUtil.DetachAllHandlers(window1);
            window1.Destroy();
            window1.Dispose();
        }

        private void Window1_DeleteEvent(object o, DeleteEventArgs args)
        {

            context.Highlight = false;

            window1.Hide();
            args.RetVal = true;
        }


        private SearchContext context;

        private SourceView editor;
        public SourceView Editor

        {
            get { return editor; }
            set
            {
                editor = value;
            }
        }


        /// <summary>
        /// Show an error message to caller.
        /// </summary>
        public void ShowMsg(string message)
        {
            MessageDialog md = new MessageDialog(Editor.Toplevel as Window, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, message);
            md.Run();
            md.Dispose();
        }

        private void UpdateTitleBar()
        {
            string text = ReplaceMode ? "Find & replace" : "Find";

            if (this.selectionOnly)
                text += " (selection only)";
            window1.Title = text;
        }


        public void ShowFor(SourceView sourceView, SearchContext theContext, bool replaceMode)
        {
            Editor = sourceView;
            this.context = theContext;
            this.selectionOnly = false;
            window1.TransientFor = Editor.Toplevel as Window;

            TextIter start;
            TextIter end;
            if (context.Buffer.GetSelectionBounds(out start, out end))
            {
                if (start.Offset != end.Offset && start.LineIndex == end.LineIndex)
                    txtLookFor.Text = context.Buffer.GetText(start, end, true);
                else
                {
                    // Get the current word that the caret is on
                    if (!start.StartsWord())
                        start.BackwardWordStart();
                    if (!end.EndsWord())
                        end.ForwardWordEnd();
                    txtLookFor.Text = context.Buffer.GetText(start, end, true);
                }
            }
            ReplaceMode = replaceMode;
            context.Highlight = true;

            window1.Parent = editor.Toplevel;
            UpdateTitleBar();
            window1.WindowPosition = WindowPosition.CenterOnParent;
            window1.Show();
            txtLookFor.GrabFocus();
        }


        public bool ReplaceMode
        {
            get { return txtReplaceWith.Visible; }
            set
            {
                window1.Resizable = value;
                btnReplace.Visible = btnReplaceAll.Visible = value;
                lblReplaceWith.Visible = txtReplaceWith.Visible = value;
                btnHighlightAll.Visible = false;  // !value;
                window1.Default = value ? btnReplace : btnFindNext;
            }
        }

        private void BtnFindPrevious_Click(object sender, EventArgs e)
        {
            FindNext(false, false, "Text not found");
        }
        private void BtnFindNext_Click(object sender, EventArgs e)
        {
            FindNext(false, true, "Text not found");
        }


        public bool FindNext(bool viaF3, bool searchForward, string messageIfNotFound)
        {
            context.Settings.SearchText = txtLookFor.Text;
            context.Settings.CaseSensitive = chkMatchCase.Active;
            context.Settings.AtWordBoundaries = chkMatchWholeWord.Active;

            if (string.IsNullOrEmpty(txtLookFor.Text))
            {
                ShowMsg("No string specified to look for!");
                return false;
            }
            TextIter iter = context.Buffer.GetIterAtOffset(context.Buffer.CursorPosition);
            TextIter start, end;
            context.Buffer.GetSelectionBounds(out start, out end);
            // If we're already on a match, move the search iterator forward
            // Otherwise we will just re-find our current position
            if (searchForward && String.Equals(context.Buffer.GetText(start, end, true), txtLookFor.Text, 
                    chkMatchCase.Active ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase))
                iter.ForwardChar();
            bool wrapped;
            bool result = false;
            if (searchForward)
                result = context.Forward(iter, out start, out end, out wrapped);
            else
                result = context.Backward(iter, out start, out end, out wrapped);
            if (!result && messageIfNotFound != null)
                ShowMsg(messageIfNotFound);
            else
            {
                context.Buffer.SelectRange(start, end);
                editor.ScrollToIter(start, 0.0, false, 0.0, 0.0);
            }
            return true;
        }

        private void BtnHighlightAll_Click(object sender, EventArgs e)
        {

            context.Highlight = true;

            BtnFindNext_Click(sender, e);
        }
        private void BtnCancel_Click(object sender, EventArgs e)
        {

            context.Highlight = false;

            window1.Hide();
        }


        private void BtnReplace_Click(object sender, EventArgs e)
        {
            context.Settings.SearchText = txtLookFor.Text;
            context.Settings.CaseSensitive = chkMatchCase.Active;
            context.Settings.AtWordBoundaries = chkMatchWholeWord.Active;
            TextIter iter = context.Buffer.GetIterAtOffset(context.Buffer.CursorPosition);
            TextIter start = context.Buffer.GetIterAtOffset(context.Buffer.CursorPosition);
            TextIter end = context.Buffer.GetIterAtOffset(context.Buffer.CursorPosition);
            bool wrapped;
            bool result = false;
            bool searchForward = true;
            if (searchForward)
                result = context.Forward(iter, out start, out end, out wrapped);
            else
                result = context.Backward(iter, out start, out end, out wrapped);
            if (result)
            {
                editor.ScrollToIter(start, 0.0, false, 0.0, 0.0);
                context.Replace(start, end, txtReplaceWith.Text);
            }
            else
                ShowMsg("Search text not found.");
        }

        private void BtnReplaceAll_Click(object sender, EventArgs e)
        {
            context.Settings.SearchText = txtLookFor.Text;
            context.Settings.CaseSensitive = chkMatchCase.Active;
            context.Settings.AtWordBoundaries = chkMatchWholeWord.Active;
            uint count = context.ReplaceAll(txtReplaceWith.Text);
            if (count == 0)
                ShowMsg("No occurrences found.");
            else
                ShowMsg(string.Format("Replaced {0} occurrences.", count));
        }


        public string LookFor { get { return txtLookFor.Text; } }
    }

}
