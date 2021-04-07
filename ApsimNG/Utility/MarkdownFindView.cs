using System;
using System.Collections.Generic;
using Gtk;
using Cairo;
using UserInterface.Views;
using System.Linq;
using UserInterface.Extensions;

namespace Utility
{
    public class MarkdownFindView
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

        private TextView currentView;

        public MarkdownFindView()
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

            // We use the same glade form as the FindAndReplaceForm, but we don't
            // allow for replacing text (the view is readonly). Therefore we need
            // to hide the controls related to the text replace functionality.
            btnReplace.Visible = btnReplaceAll.Visible = false;
            lblReplaceWith.Visible = txtReplaceWith.Visible = false;
            btnHighlightAll.Visible = false;  // !value;

            btnFindNext.Clicked += BtnFindNext_Click;
            btnFindPrevious.Clicked += BtnFindPrevious_Click;
            btnCancel.Clicked += BtnCancel_Click;
            btnHighlightAll.Clicked += BtnHighlightAll_Click;
            window1.DeleteEvent += Window1_DeleteEvent;
            window1.Destroyed += Window1_Destroyed;
            AccelGroup agr = new AccelGroup();
            
            // Allow the text input widget to activate the default widget and make
            // the 'find next instance' button the default widget for its toplevel.
            // This means that when the user presses return while the text input
            // has focus, it will activate the default widget (the 'find next' button).
            btnFindNext.HasDefault = true;
            txtLookFor.ActivatesDefault = true;

            // Add some extra keyboard shortcuts for the various buttons:
            // F3                               - find next
            // Shift + F3, Shift + Return       - find previous
            // Escape                           - Close the dialog
            btnFindNext.AddAccelerator("activate", agr, new AccelKey(Gdk.Key.F3, Gdk.ModifierType.None, AccelFlags.Visible));
            btnFindPrevious.AddAccelerator("activate", agr, new AccelKey(Gdk.Key.F3, Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
            btnFindPrevious.AddAccelerator("activate", agr, new AccelKey(Gdk.Key.Return, Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
            btnCancel.AddAccelerator("activate", agr, new AccelKey(Gdk.Key.Escape, Gdk.ModifierType.None, AccelFlags.Visible));
            window1.AddAccelGroup(agr);
        }

        private void Window1_Destroyed(object sender, EventArgs e)
        {
            btnFindNext.Clicked -= BtnFindNext_Click;
            btnFindPrevious.Clicked -= BtnFindPrevious_Click;
            btnCancel.Clicked -= BtnCancel_Click;
            window1.DeleteEvent -= Window1_DeleteEvent;
            window1.Destroyed -= Window1_Destroyed;
        }

        public void Destroy()
        {
            window1.Cleanup();
        }

        private void Window1_DeleteEvent(object o, DeleteEventArgs args)
        {
            window1.Hide();
            args.RetVal = true;
        }

        /// <summary>
        /// 
        /// </summary>
        private MarkdownView View { get; set; }

        /// <summary>
        /// Show an error message to caller.
        /// </summary>
        public void ShowMsg(string message)
        {
            MessageDialog md = new MessageDialog(window1, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, message);
            md.Run();
            md.Cleanup();
        }

        private void UpdateTitleBar()
        {
            string text = "Find";
            // todo : add context to title?
            //if (View.MainWidget != null && editor.FileName != null)
            //    text += " - " + System.IO.Path.GetFileName(editor.FileName);
            if (this.selectionOnly)
                text += " (selection only)";
            window1.Title = text;
        }

        public void ShowFor(MarkdownView markdown)
        {
            View = markdown;
            window1.TransientFor = View.MainWidget.Toplevel as Window;
            TextView[] editors = GetEditorsInView();
            if (editors.Length == 1 && editors[0].Buffer.GetSelectionBounds(out TextIter start, out TextIter end))
            {
                selectionOnly = true;
                txtLookFor.Text = editors[0].Buffer.GetText(start, end, false);
            }
            else
                selectionOnly = false;

            //window1.Parent = View.MainWidget.Toplevel;
            UpdateTitleBar();
            window1.WindowPosition = WindowPosition.CenterOnParent;
            window1.Show();
            txtLookFor.GrabFocus();
        }

        private void BtnFindPrevious_Click(object sender, EventArgs e)
        {
            try
            {
                FindNext(false, false, "Text not found");
            }
            catch (Exception err)
            {
                ShowMsg(err.ToString());
            }
        }
        private void BtnFindNext_Click(object sender, EventArgs e)
        {
            try
            {
                FindNext(false, true, "Text not found");
            }
            catch (Exception err)
            {
                ShowMsg(err.ToString());
            }
        }

        private TextView[] GetEditorsInView()
        {
            if (View.MainWidget is TextView textView)
                return new TextView[1] { textView };
            else if (View.MainWidget is Container container)
                return container.Children.OfType<TextView>().ToArray();
            return new TextView[0];
        }


        public void FindNext(bool viaF3, bool searchForward, string messageIfNotFound)
        {
            if (string.IsNullOrEmpty(txtLookFor.Text))
            {
                ShowMsg("No string specified to look for!");
                return;
            }

            TextView[] editors = GetEditorsInView();
            if (currentView == null)
                currentView = editors.FirstOrDefault(e => e.HasFocus) ?? editors.FirstOrDefault();
            if (currentView == null)
                return;

            TextIter matchStart = TextIter.Zero;
            TextIter matchEnd = TextIter.Zero;
            bool matchFound = false;
            int startIndex = Array.IndexOf(editors, currentView);

            while (!matchFound)
            {
                TextIter startPos;
                if (currentView.Buffer.CursorPosition < currentView.Buffer.Text.Length)
                    startPos = currentView.Buffer.GetIterAtOffset(currentView.Buffer.CursorPosition + 1);
                else
                    startPos = searchForward ? currentView.Buffer.StartIter : currentView.Buffer.EndIter;
                if (searchForward)
                    //matchFound = startPos.ForwardSearch(txtLookFor.Text, TextSearchFlags.VisibleOnly, out matchStart, out matchEnd, currentView.Buffer.EndIter);
                    matchFound = Find(startPos, currentView.Buffer.EndIter, LookFor, chkMatchCase.Active ? SearchType.CaseSensitive : SearchType.Normal, out matchStart, out matchEnd);
                else
                    //matchFound = startPos.BackwardSearch(txtLookFor.Text, TextSearchFlags.VisibleOnly, out matchStart, out matchEnd, currentView.Buffer.StartIter);
                    matchFound = Find(startPos, currentView.Buffer.StartIter, LookFor, chkMatchCase.Active ? SearchType.CaseSensitive : SearchType.Normal, out matchStart, out matchEnd);
                
                if (!matchFound)
                {
                    int index = Array.IndexOf(editors, currentView);
                    int nextIndex = (index + 1) % editors.Length;
                    if (index < 0 || index >= editors.Length)
                        // Invalid index - the editors array doesn't contain the editor we were
                        // just searching in. Unsure how this would occur, but if it does, just
                        // start searching from the first editor.
                        index = 0;
                    else if (nextIndex == startIndex)
                        // We've searched in every textview, but found no matches - time to give up.
                        break;
                    else
                        // Try the next text editor.
                        currentView = editors[nextIndex];
                }
            }
            if (matchFound)
            {
                currentView.Buffer.SelectRange(matchStart, matchEnd);
                currentView.ScrollToIter(matchStart, 0, false, 0, 0);
            }
            else
            {
                if (messageIfNotFound != null)
                    ShowMsg(messageIfNotFound);
                return;
            }
        }

        /// <summary>
        /// The gtk2 textview (well, textiter really) suppoorts searching, but doesn't
        /// support case-insensitive or regex-based searches. Therefore I've implemented
        /// this on the managed side.
        /// 
        /// If the end position is earlier in the buffer than the start position, then
        /// a backwards search will occur (that is, the position of the last match will
        /// be returned, iff a match is found).
        /// </summary>
        /// <param name="start">Position from which to start the search.</param>
        /// <param name="end">Position to which the search will be limited.</param>
        /// <param name="lookFor">Search query.</param>
        /// <param name="searchKind">Search options (ignore case, regex, etc).</param>
        /// <param name="matchStart">Output paramter - the start position of the match (if a match is found).</param>
        /// <param name="matchEnd">Output parameter - the end position of the match (if a match is found).</param>
        /// <returns>True iff a match is found.</returns>
        private bool Find(TextIter start, TextIter end, string lookFor, SearchType searchKind, out TextIter matchStart, out TextIter matchEnd)
        {
            string text;
            if (start.Offset > end.Offset)
                text = start.Buffer.GetText(end, start, false);
            else
                text = start.Buffer.GetText(start, end, false);
            Console.WriteLine($"Searching from {start.Offset} to {end.Offset}. Total buffer length = {start.Buffer.Text.Length}");

            if (searchKind == SearchType.Regex)
                throw new NotImplementedException();
            else
            {
                StringComparison comparisonType = searchKind == SearchType.CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                int index;
                if (start.Offset < end.Offset)
                    index = text.IndexOf(lookFor, comparisonType);
                else
                    index = text.LastIndexOf(lookFor, comparisonType);

                if (index < 0)
                {
                    matchStart = TextIter.Zero;
                    matchEnd = TextIter.Zero;
                    return false;
                }
                else
                {
                    int netOffset;
                    if (start.Offset > end.Offset)
                        netOffset = end.Offset + index;
                    else
                        netOffset = start.Offset + index;
                    Console.WriteLine($"Found {lookFor} at ({netOffset} - {netOffset + lookFor.Length})");
                    matchStart = start.Buffer.GetIterAtOffset(netOffset);
                    matchEnd = start.Buffer.GetIterAtOffset(netOffset + lookFor.Length);
                    return true;
                }
            }
        }

        private void BtnHighlightAll_Click(object sender, EventArgs e)
        {
            // tbi - probably need gtksourceview
            //editor.HighlightSearchPattern = true;
            //BtnFindNext_Click(sender, e);
        }
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            window1.Hide();
        }

        public string LookFor { get { return txtLookFor.Text; } }

        private enum SearchType
        {
            Normal,
            CaseSensitive,
            Regex
        }
    }
}
