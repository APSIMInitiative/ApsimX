// The basics are all here, but there are still a few things to be implemented:
// Drag and drop is pinning an object so we can pass its address around as data. Is there a better way?
// (Probably not really, as we go through a native layer, unless we can get by with the serialized XML).
// Shortcuts (accelerators in Gtk terminology) haven't yet been implemented.
// Link doesn't work, but it appears that move and link aren't working in the Windows.Forms implementation either.
// Actually, Move "works" here but doesn't undo correctly
using Gtk;
using GLib;
using System;
using UserInterface.Interfaces;

namespace UserInterface.Views
{


    /// <summary>
    /// An ExplorerView is a "Windows Explorer" like control that displays a virtual tree control on the left
    /// and a user interface on the right allowing the user to modify properties of whatever they
    /// click on in the tree control.
    /// </summary>
    public class ExplorerView : ViewBase, IExplorerView
    {
        private Box rightHandView;
        private Gtk.TreeView treeviewWidget;
        private MarkdownView descriptionView;
        private Paned hpaned;

        /// <summary>Default constructor for ExplorerView</summary>
        public ExplorerView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ExplorerView.glade");
            mainWidget = (Box)builder.GetObject("vbox1");
            ToolStrip = new ToolStripView((Toolbar)builder.GetObject("toolStrip"));
            hpaned = (Paned)builder.GetObject("hpaned1");
            hpaned.AddNotification(OnDividerNotified);

            treeviewWidget = (Gtk.TreeView)builder.GetObject("treeview1");
            treeviewWidget.Realized += OnLoaded;
            Tree = new TreeView(owner, treeviewWidget);
            rightHandView = (Box)builder.GetObject("vbox2");
            //rightHandView.ShadowType = ShadowType.EtchedOut;

            mainWidget.Destroyed += OnDestroyed;
        }

        /// <summary>The current right hand view.</summary>
        public ViewBase CurrentRightHandView { get; private set; }

        /// <summary>The tree on the left side of the explorer view</summary>
        public ITreeView Tree { get; private set; }

        /// <summary>The toolstrip at the top of the explorer view</summary>
        public IToolStripView ToolStrip { get; private set; }

        /// <summary>Position of the divider between the tree and content</summary>
        public int DividerPosition { get; set; }

        /// <summary>Invoked when the divider position is changed</summary>
        public event EventHandler DividerChanged;

        /// <summary>
        /// Add a user control to the right hand panel. If Control is null then right hand panel will be cleared.
        /// </summary>
        /// <param name="control">The control to add.</param>
        public void AddRightHandView(object control)
        {

            // Remove existing right hand view.
            if (CurrentRightHandView != null)
                CurrentRightHandView.Dispose();

            ViewBase view = control as ViewBase;
            if (view != null)
            {
                CurrentRightHandView = view;
                rightHandView.PackEnd(view.MainWidget, true, true, 0);
                rightHandView.ShowAll();
            }
        }

        /// <summary>
        /// Add a description to the right hand view.
        /// </summary>
        /// <param name="description">The description to show.</param>
        public void AddDescriptionToRightHandView(string description)
        {
            if (description == null)
            {
                if (descriptionView != null)
                {
                    descriptionView.Dispose();
                    //Widget descriptionWidget = (descriptionView as ViewBase).MainWidget;
                    //rightHandView.Remove(descriptionWidget);
                    //descriptionWidget.Dispose();
                }
                descriptionView = null;
            }
            else
            {
                if (descriptionView == null)
                {
                    descriptionView = new MarkdownView(this);
                    // Set PropagateNaturalHeight to true, to ensure that the
                    // scrolled window requests enough space to not require a
                    // scrollbar by default. We could change MarkdownView to
                    // always do this, but it's used in lots of other places, so
                    // that may have unintended consequences and would require
                    // more extensive testing.
                    if (descriptionView.MainWidget is ScrolledWindow scroller)
                        scroller.PropagateNaturalHeight = true;
                    rightHandView.PackStart(descriptionView.MainWidget, false, false, 0);
                    // Let Gtk catch up with things; otherwise too much space
                    // is allocated to the new description view. Is there a better way?
                    while (Gtk.Application.EventsPending())
                        Gtk.Application.RunIteration();
                }
                descriptionView.Text = description;
            }
        }

        /// <summary>Get screenshot of right hand panel.</summary>
        public System.Drawing.Image GetScreenshotOfRightHandPanel()
        {

            throw new NotImplementedException();

        }

        /// <summary>Listens to an event of the divider position changing</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnDividerNotified(object sender, NotifyArgs args)
        {
            if (DividerChanged != null)
                DividerChanged.Invoke(sender, new EventArgs());
        }

        /// <summary>
        /// Invoked when the view is drawn on the screen.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnLoaded(object sender, EventArgs args)
        {
            try
            {
                // Context menu keyboard shortcuts are registered when the tree
                // view gains focus. Unfortunately, some views seem to prevent this
                // event from firing, and as a result, the keyboard shortcuts don't
                // work. To fix this, we select the first node in the tree when it
                // is "realized" (rendered).
                TreeIter iter;
                treeviewWidget.Model.GetIterFirst(out iter);
                string firstNodeName = treeviewWidget.Model.GetValue(iter, 0)?.ToString();
                Tree.SelectedNode = "." + firstNodeName;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Widget has been destroyed - clean up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDestroyed(object sender, EventArgs e)
        {
            try
            {
                treeviewWidget.Realized -= OnLoaded;
                if (rightHandView != null)
                {
                    foreach (Widget child in rightHandView.Children)
                    {
                        rightHandView.Remove(child);
                        child.Dispose();
                    }
                }
                ToolStrip.Destroy();
                mainWidget.Destroyed -= OnDestroyed;
                hpaned.RemoveNotification(OnDividerNotified);
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
