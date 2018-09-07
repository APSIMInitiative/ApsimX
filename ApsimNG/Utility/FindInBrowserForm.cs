namespace Utility
{
	using System;
    using Gtk;
    using UserInterface.Views;

    public class FindInBrowserForm
    {

		// Gtk Widgets
        private Window window1 = null;
        private CheckButton chkMatchCase = null;
        private CheckButton chkHighlightAll = null;
        private Entry txtLookFor = null;
        private Button btnCancel = null;
        private Button btnFindPrevious = null;
        private Button btnFindNext = null;
        private IBrowserWidget browser { get; set; }


        public FindInBrowserForm()
        {
            Builder builder = ViewBase.MasterView.BuilderFromResource("ApsimNG.Resources.Glade.BrowserFind.glade");
            window1 = (Window)builder.GetObject("window1");
            chkMatchCase = (CheckButton)builder.GetObject("chkMatchCase");
            chkHighlightAll = (CheckButton)builder.GetObject("chkHighlightAll");
            txtLookFor = (Entry)builder.GetObject("txtLookFor");
            btnCancel = (Button)builder.GetObject("btnCancel");
            btnFindPrevious = (Button)builder.GetObject("btnFindPrevious");
            btnFindNext = (Button)builder.GetObject("btnFindNext");

			txtLookFor.Changed += txtLookFor_Changed;
            btnFindNext.Clicked += btnFindNext_Click;
            btnFindPrevious.Clicked += btnFindPrevious_Click;
            btnCancel.Clicked += btnCancel_Click;
            chkHighlightAll.Clicked += chkHighlightAll_Click;
			chkHighlightAll.Visible = false; // Hide this for now...
			chkHighlightAll.NoShowAll = true;
            window1.DeleteEvent += Window1_DeleteEvent;
            window1.Destroyed += Window1_Destroyed;
            AccelGroup agr = new AccelGroup();
            btnCancel.AddAccelerator("activate", agr, new AccelKey(Gdk.Key.Escape, Gdk.ModifierType.None, AccelFlags.Visible));
            window1.AddAccelGroup(agr);
        }


        private void Window1_Destroyed(object sender, EventArgs e)
        {
			txtLookFor.Changed -= txtLookFor_Changed;
            btnFindNext.Clicked -= btnFindNext_Click;
            btnFindPrevious.Clicked -= btnFindPrevious_Click;
            btnCancel.Clicked -= btnCancel_Click;
            chkHighlightAll.Clicked -= chkHighlightAll_Click;
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

        /// <summary>
        /// Show an error message to caller.
        /// </summary>
        public void ShowMsg(string message)
        {
			if (browser != null)
			{
                MessageDialog md = new MessageDialog(browser.HoldingWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, message);
                md.Run();
                md.Destroy();
			}
        }

        public void ShowFor(IBrowserWidget browser)
        {
            this.browser = browser;
            window1.TransientFor = this.browser.HoldingWidget.Toplevel as Window;
            window1.Parent = this.browser.HoldingWidget.Toplevel;
            window1.WindowPosition = WindowPosition.CenterOnParent;
            window1.Show();
            txtLookFor.GrabFocus();
        }

		void txtLookFor_Changed(object sender, EventArgs e)
        {
            // No, this isn't quite right. It keeps searching forward, rather than resting on the current selection
            // when the current selection matches. Disabling until a better way is found
			// Find();
        }

        private void btnFindPrevious_Click(object sender, EventArgs e)
        {
            FindNext(false, "Text not found");
        }

        private void btnFindNext_Click(object sender, EventArgs e)
        {
            FindNext(true, "Text not found");
        }

        public void FindNext(bool searchForward, string messageIfNotFound)
        {
			chkHighlightAll_Click(this, new EventArgs());
            if (string.IsNullOrEmpty(txtLookFor.Text))
            {
                ShowMsg("No string specified to for search!");
                return;
            }
            if (!browser.Search(txtLookFor.Text, searchForward, chkMatchCase.Active, true))
			{
				if (!string.IsNullOrEmpty(messageIfNotFound))
				    ShowMsg(messageIfNotFound);
			}
                    
        }

		public void Find()
		{
			if (!string.IsNullOrEmpty(txtLookFor.Text))
				FindNext(true,"");
		}

		private bool _isHighlighted = false;

        private void chkHighlightAll_Click(object sender, EventArgs e)
        {
			if (browser is TWWebBrowserWK)
			{
				bool highlight = chkHighlightAll.Active;
				if (highlight != _isHighlighted)
				{
					(browser as TWWebBrowserWK).Highlight(txtLookFor.Text, chkMatchCase.Active, highlight);
					_isHighlighted = highlight;
				}
			}
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
			// if (browser is TWWebBrowserWK)
			//	(browser as TWWebBrowserWK).Highlight("", false, false);
            window1.Hide();
        }

        public string LookFor { get { return txtLookFor.Text; } }
    }

}

    
