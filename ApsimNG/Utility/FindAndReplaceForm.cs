using System;
using System.Collections.Generic;
using Gtk;
#if NETFRAMEWORK
using Mono.TextEditor;
#else
using GtkSource;
#endif
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
        }

        public void Destroy()
        {
            window1.Cleanup();
        }

        private void Window1_DeleteEvent(object o, DeleteEventArgs args)
        {
#if NETCOREAPP
            context.Highlight = false;
#endif
            window1.Hide();
            args.RetVal = true;
        }

#if NETCOREAPP
        private SearchContext context;

        private SourceView editor;
        public SourceView Editor
#else

        private TextEditor editor;
        public TextEditor Editor
#endif
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
            md.Cleanup();
        }

        private void UpdateTitleBar()
        {
            string text = ReplaceMode ? "Find & replace" : "Find";
#if NETFRAMEWORK
            if (editor != null && editor.FileName != null)
                text += " - " + System.IO.Path.GetFileName(editor.FileName);
#endif
            if (this.selectionOnly)
                text += " (selection only)";
            window1.Title = text;
        }

#if NETCOREAPP
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
#else
        public void ShowFor(TextEditor editor, bool replaceMode)
        {
            Editor = editor;
            this.selectionOnly = false;
            window1.TransientFor = Editor.Toplevel as Window;
            Mono.TextEditor.Selection selected = Editor.MainSelection;
            if (Editor.SelectedText != null)
            {
                if (selected.MaxLine == selected.MinLine)
                    txtLookFor.Text = Editor.SelectedText;
                else
                {
                    Editor.SearchEngine.SearchRequest.SearchRegion = Editor.SelectionRange;
                    this.selectionOnly = true;
                }
            }
            else
            {
                // Get the current word that the caret is on
                Caret caret = Editor.Caret;
                int start = Editor.GetTextEditorData().FindCurrentWordStart(caret.Offset);
                int end = Editor.GetTextEditorData().FindCurrentWordEnd(caret.Offset);
                txtLookFor.Text = Editor.GetTextBetween(start, end);
            }
            ReplaceMode = replaceMode;
            editor.HighlightSearchPattern = true;

            window1.Parent = editor.Toplevel;
            UpdateTitleBar();
            window1.WindowPosition = WindowPosition.CenterOnParent;
            window1.Show();
            txtLookFor.GrabFocus();
        }
#endif

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

#if NETCOREAPP
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
                result = context.Forward(iter, ref start, ref end, out wrapped);
            else
                result = context.Backward(iter, ref start, ref end, out wrapped);
            if (!result && messageIfNotFound != null)
                ShowMsg(messageIfNotFound);
            else
            {
                context.Buffer.SelectRange(start, end);
                editor.ScrollToIter(start, 0.0, false, 0.0, 0.0);
            }
            return true;
        }
#else
        public SearchResult FindNext(bool viaF3, bool searchForward, string messageIfNotFound)
        {
            Editor.SearchEngine.SearchRequest.SearchPattern = txtLookFor.Text;
            Editor.SearchEngine.SearchRequest.CaseSensitive = chkMatchCase.Active;
            Editor.SearchEngine.SearchRequest.WholeWordOnly = chkMatchWholeWord.Active;
            if (string.IsNullOrEmpty(txtLookFor.Text))
            {
                ShowMsg("No string specified to look for!");
                return null;
            }

            SearchResult range = null;
            if (searchForward)
                range = Editor.FindNext(true);
            else
                range = Editor.FindPrevious(true);
            if (range == null && messageIfNotFound != null)
                ShowMsg(messageIfNotFound);
            else
                Editor.ScrollTo(range.Offset);
            return range;
        }
#endif
        private void BtnHighlightAll_Click(object sender, EventArgs e)
        {
#if NETCOREAPP
            context.Highlight = true;
#else
            editor.HighlightSearchPattern = true;
#endif
            BtnFindNext_Click(sender, e);
        }
        private void BtnCancel_Click(object sender, EventArgs e)
        {
#if NETCOREAPP
            context.Highlight = false;
#endif
            window1.Hide();
        }

#if NETCOREAPP
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
                result = context.Forward(iter, ref start, ref end, out wrapped);
            else
                result = context.Backward(iter, ref start, ref end, out wrapped);
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
#else
        private void BtnReplace_Click(object sender, EventArgs e)
        {
            Editor.SearchEngine.SearchRequest.SearchPattern = txtLookFor.Text;
            Editor.SearchEngine.SearchRequest.CaseSensitive = chkMatchCase.Active;
            Editor.SearchEngine.SearchRequest.WholeWordOnly = chkMatchWholeWord.Active;
            if (!Editor.Replace(txtReplaceWith.Text))
                ShowMsg("Search text not found.");
        }

        private void BtnReplaceAll_Click(object sender, EventArgs e)
        {
            Editor.SearchEngine.SearchRequest.SearchPattern = txtLookFor.Text;
            Editor.SearchEngine.SearchRequest.CaseSensitive = chkMatchCase.Active;
            Editor.SearchEngine.SearchRequest.WholeWordOnly = chkMatchWholeWord.Active;
            int count = Editor.ReplaceAll(txtReplaceWith.Text);
            if (count == 0)
                ShowMsg("No occurrences found.");
            else
                ShowMsg(string.Format("Replaced {0} occurrences.", count));
        }
#endif

        public string LookFor { get { return txtLookFor.Text; } }
    }

}
