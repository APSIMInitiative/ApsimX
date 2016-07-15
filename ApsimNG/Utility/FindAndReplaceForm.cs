using System;
using System.Collections.Generic;
using Gtk;
using Glade;
using Mono.TextEditor;
using System.IO;
using Cairo;

namespace Utility
{
    public class FindAndReplaceForm
    {
        [Widget]
        private Window window1 = null;
        [Widget]
        private CheckButton chkMatchCase = null;
        [Widget]
        private CheckButton chkMatchWholeWord = null;
        [Widget]
        private Entry txtLookFor = null;
        [Widget]
        private Entry txtReplaceWith = null;
        [Widget]
        private Button btnReplace = null;
        [Widget]
        private Button btnReplaceAll = null;
        [Widget]
        private Button btnHighlightAll = null;
        [Widget]
        private Button btnCancel = null;
        [Widget]
        private Button btnFindPrevious = null;
        [Widget]
        private Button btnFindNext = null;
        [Widget]
        private Label lblReplaceWith = null;

        private bool selectionOnly = false;

        public FindAndReplaceForm()
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.FindAndReplace.glade", "window1");
            gxml.Autoconnect(this);
            btnFindNext.Clicked += btnFindNext_Click;
            btnFindPrevious.Clicked += btnFindPrevious_Click;
            btnCancel.Clicked += btnCancel_Click;
            btnReplace.Clicked += btnReplace_Click;
            btnReplaceAll.Clicked += btnReplaceAll_Click;
            btnHighlightAll.Clicked += btnHighlightAll_Click;
            window1.DeleteEvent += Window1_DeleteEvent;
            window1.Destroyed += Window1_Destroyed;
        }


        private void Window1_Destroyed(object sender, EventArgs e)
        {
            btnFindNext.Clicked -= btnFindNext_Click;
            btnFindPrevious.Clicked -= btnFindPrevious_Click;
            btnCancel.Clicked -= btnCancel_Click;
            btnReplace.Clicked -= btnReplace_Click;
            btnReplaceAll.Clicked -= btnReplaceAll_Click;
            window1.DeleteEvent -= Window1_DeleteEvent;
            window1.Destroyed -= Window1_Destroyed;
        }

        public void Destroy()
        {
            window1.Destroy();
        }

        private void Window1_DeleteEvent(object o, DeleteEventArgs args)
        {
            window1.Hide();
            args.RetVal = true;
        }

        MonoTextEditor _editor;
        MonoTextEditor Editor
        {
            get { return _editor; }
            set
            {
                _editor = value;
            }
        }

        /// <summary>
        /// Show an error message to caller.
        /// </summary>
        public void ShowMsg(string message)
        {
            MessageDialog md = new MessageDialog(Editor.Toplevel as Window, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, message);
            md.Run();
            md.Destroy();
        }

        private void UpdateTitleBar()
        {
            string text = ReplaceMode ? "Find & replace" : "Find";
            if (_editor != null && _editor.FileName != null)
                text += " - " + System.IO.Path.GetFileName(_editor.FileName);
            if (selectionOnly)
              text += " (selection only)";
            window1.Title = text;
        }

        public void ShowFor(MonoTextEditor editor, bool replaceMode)
        {
            Editor = editor;
            selectionOnly = false;
            window1.TransientFor = Editor.Toplevel as Window;
            Mono.TextEditor.Selection selected = Editor.MainSelection;
            if (Editor.SelectedText != null)
            {
                if (selected.MaxLine == selected.MinLine)
                    txtLookFor.Text = Editor.SelectedText;
                else
                {
                    Editor.SearchEngine.SearchRequest.SearchRegion = Editor.SelectionRange;
                    selectionOnly = true;
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

            window1.Parent = editor.Toplevel;
            UpdateTitleBar();
            window1.Show();
            txtLookFor.GrabFocus();
        }

        public bool ReplaceMode
        {
            get { return txtReplaceWith.Visible; }
            set
            {
                window1.AllowGrow = value;
                window1.AllowShrink = !value;
                btnReplace.Visible = btnReplaceAll.Visible = value;
                lblReplaceWith.Visible = txtReplaceWith.Visible = value;
                btnHighlightAll.Visible = !value;
                window1.Default = value ? btnReplace : btnFindNext;
            }
        }

        private void btnFindPrevious_Click(object sender, EventArgs e)
        {
            FindNext(false, false, "Text not found");
        }
        private void btnFindNext_Click(object sender, EventArgs e)
        {
            FindNext(false, true, "Text not found");
        }

        public bool _lastSearchWasBackward = false;
        public bool _lastSearchLoopedAround;

        
        public SearchResult FindNext(bool viaF3, bool searchBackward, string messageIfNotFound)
        {
            if (string.IsNullOrEmpty(txtLookFor.Text))
            {
                ShowMsg("No string specified to look for!");
                return null;
            }
            _lastSearchWasBackward = searchBackward;
            Editor.SearchEngine.SearchRequest.SearchPattern = txtLookFor.Text;
            Editor.SearchEngine.SearchRequest.CaseSensitive = chkMatchCase.Active;
            Editor.SearchEngine.SearchRequest.WholeWordOnly = chkMatchWholeWord.Active;

            SearchResult range = null;
            if (searchBackward)
                range = Editor.FindNext(true);
            else
                range = Editor.FindPrevious(true);
            if (range == null && messageIfNotFound != null)
                ShowMsg(messageIfNotFound);
            return range;
        }
        
        Dictionary<MonoTextEditor, HighlightGroup> _highlightGroups = new Dictionary<MonoTextEditor, HighlightGroup>();

        private void btnHighlightAll_Click(object sender, EventArgs e)
        {
            if (!_highlightGroups.ContainsKey(_editor))
                _highlightGroups[_editor] = new HighlightGroup(_editor);
            HighlightGroup group = _highlightGroups[_editor];

            if (string.IsNullOrEmpty(LookFor))
                // Clear highlights
                group.ClearMarkers();
            else
            {
                Editor.SearchEngine.SearchRequest.SearchPattern = txtLookFor.Text;
                Editor.SearchEngine.SearchRequest.CaseSensitive = chkMatchCase.Active;
                Editor.SearchEngine.SearchRequest.WholeWordOnly = chkMatchWholeWord.Active;

                int offset = 0, count = 0;
                for (;;)
                {
                    SearchResult range = Editor.SearchEngine.SearchForward(offset);
                    if (range == null || range.SearchWrapped)
                        break;
                    offset = range.Offset + range.Length;
                    count++;

                    HighlightSegmentMarker m = new HighlightSegmentMarker(range.Offset, range.Length);
                    group.AddMarker(m);
                }
                if (count == 0)
                    ShowMsg("Search text not found.");
                else
                    window1.Hide();
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            window1.Hide();
        }

        private void btnReplace_Click(object sender, EventArgs e)
        {
            Editor.SearchEngine.SearchRequest.SearchPattern = txtLookFor.Text;
            Editor.SearchEngine.SearchRequest.CaseSensitive = chkMatchCase.Active;
            Editor.SearchEngine.SearchRequest.WholeWordOnly = chkMatchWholeWord.Active;
            if (!Editor.Replace(txtReplaceWith.Text))
                ShowMsg("Search text not found.");
        }

        private void btnReplaceAll_Click(object sender, EventArgs e)
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
        public string LookFor { get { return txtLookFor.Text; } }
    }

    /// <summary>
    /// Extends the TextSegementMarker class to allow a different background color to be used
    /// Is there a better way to do this? There isn't really much documentation
    /// on the Mono.TextEditor stuff. The source code is the documentation...
    /// </summary>
    public class HighlightSegmentMarker : TextSegmentMarker
    {
        public HighlightSegmentMarker(int Offset, int Length) : base (Offset, Length) {}

        public override void DrawBackground(MonoTextEditor editor, Context cr, LineMetrics metrics, int startOffset, int endOffset)
        {
            int x1 = editor.LocationToPoint(editor.OffsetToLocation(this.Offset), false).X;
            int x2 = editor.LocationToPoint(editor.OffsetToLocation(this.Offset + this.Length), false).X;
            cr.Rectangle(x1, metrics.LineYRenderStartPosition + 0.5, x2 - x1, metrics.LineHeight - 1);
            cr.SetSourceRGB(1.0, 1.0, 0.0);
            cr.Fill();
        }
    }

    /// <summary>Bundles a group of markers together so that they can be cleared 
    /// together.</summary>
    public class HighlightGroup : IDisposable
    {
        List<HighlightSegmentMarker> _markers = new List<HighlightSegmentMarker>();
        Mono.TextEditor.MonoTextEditor _editor;
        TextDocument _document;
        public HighlightGroup(Mono.TextEditor.MonoTextEditor editor)
        {
            _editor = editor;
            _document = editor.Document;
        }
        public void AddMarker(HighlightSegmentMarker marker)
        {
            _markers.Add(marker);
            _document.AddMarker(marker);
        }
        public void ClearMarkers()
        {
            foreach (HighlightSegmentMarker m in _markers)
                _document.RemoveMarker(m);
            _markers.Clear();
            _editor.QueueDraw();
        }
        public void Dispose() { ClearMarkers(); GC.SuppressFinalize(this); }
        ~HighlightGroup() { Dispose(); }

        public IList<HighlightSegmentMarker> Markers { get { return _markers.AsReadOnly(); } }
    }
}
