namespace UserInterface.Views
{
    using EventArguments;
    using Gtk;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;
    using APSIM.Shared.Utilities;
    using global::UserInterface.Extensions;
    using Utility;

    /// <summary>
    /// GTK# based view of the PropertyCategorisedPresenter to display a tree view of categories and sub-categories to assit filtering properties
    /// Uses Category attribute of property (Category and SubCategory values) to define list and modify SimplePropertyPresenter filter rule on selection
    /// A right hand panel is used to display the property presenter
    /// </summary>
    public class PropertyCategorisedView : ViewBase, IPropertyCategorisedView
    {
        /// <summary>The previously selected node path.</summary>
        private string previouslySelectedNodePath;

        private Gtk.TreeView treeview1 = null;
        private Viewport rightHandView = null;

        private Menu popup = new Menu();

        private TreeStore treemodel = new TreeStore(typeof(string), typeof(Gdk.Pixbuf), typeof(string));
        private CellRendererText textRender;
        private AccelGroup accel = new AccelGroup();

        private const string modelMime = "application/x-model-component";

        System.Timers.Timer timer = new System.Timers.Timer();

        /// <summary>Default constructor for ExplorerView</summary>
        public PropertyCategorisedView(ViewBase owner) : base(owner)
        {
            Builder builder = ViewBase.BuilderFromResource("ApsimNG.Resources.Glade.PropertyCategoryView.glade");
            Gtk.Paned hpaned = (Gtk.Paned)builder.GetObject("hpaned1"); 
            treeview1 = (Gtk.TreeView)builder.GetObject("treeview1");
            rightHandView = (Viewport)builder.GetObject("RightHandView");
            mainWidget = hpaned;
            rightHandView.BorderWidth = 7;



            treeview1.Model = treemodel;
            TreeViewColumn column = new TreeViewColumn();
            CellRendererPixbuf iconRender = new Gtk.CellRendererPixbuf();
            column.PackStart(iconRender, false);
            textRender = new Gtk.CellRendererText();
            textRender.Editable = false;

            column.PackStart(textRender, true);
            column.SetAttributes(iconRender, "pixbuf", 1);
            column.SetAttributes(textRender, "text", 0);
            treeview1.AppendColumn(column);
            treeview1.TooltipColumn = 2;

            treeview1.CursorChanged += OnAfterSelect;
            treeview1.ButtonReleaseEvent += OnButtonUp;
            treeview1.ButtonPressEvent += OnButtonPress;
            treeview1.RowActivated += OnRowActivated;

            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            if (rightHandView != null)
            {
                foreach (Widget child in rightHandView.Children)
                {
                    rightHandView.Remove(child);
                    child.Dispose();
                }
            }
            popup.Clear();
            popup.Dispose();
            treeview1.CursorChanged -= OnAfterSelect;
            treeview1.ButtonReleaseEvent -= OnButtonUp;
            treeview1.ButtonPressEvent -= OnButtonPress;
            treeview1.RowActivated -= OnRowActivated;
            
            treeview1.FocusInEvent -= Treeview1_FocusInEvent;
            treeview1.FocusOutEvent -= Treeview1_FocusOutEvent;

            mainWidget.Destroyed -= _mainWidget_Destroyed;
            owner = null;
        }

        /// <summary>
        /// This event will be invoked when a node is selected not by the user
        /// but by an Undo command.
        /// </summary>
        public event EventHandler<NodeSelectedArgs> SelectedNodeChanged;

        /// <summary>Refreshes the entire tree from the specified descriptions.</summary>
        /// <param name="nodeDescriptions">The nodes descriptions.</param>
        public void Refresh(TreeViewNode nodeDescriptions)
        {
            treemodel.Clear();
            TreeIter iter = treemodel.AppendNode();
            RefreshNode(iter, nodeDescriptions);
            treeview1.ShowAll();
            treeview1.ExpandRow(new TreePath("0"), false);
        }

        /// <summary>Gets or sets the currently selected node.</summary>
        /// <value>The selected node.</value>
        public string SelectedNode
        {
            get
            {
                TreePath selPath;
                TreeViewColumn selCol;
                treeview1.GetCursor(out selPath, out selCol);
                if (selPath != null)
                {
                    return this.FullPath(selPath);
                }
                else
                {
                    return string.Empty;
                }
            }

            set
            {
                if (SelectedNode != value && value != string.Empty)
                {
                    TreePath pathToSelect = treemodel.GetPath(FindNode(value));
                    if (pathToSelect != null)
                    {
                        treeview1.SetCursor(pathToSelect, treeview1.GetColumn(0), false);
                    }
                }
            }
        }

        /// <summary>
        /// Add a user control (aka GUI) to the right hand panel. If Control is null then right hand panel will be cleared.
        /// </summary>
        /// <param name="control">The control to add.</param>
        public void AddRightHandView(object control)
        {
            //remove existing Right Hand View
            foreach (Widget child in rightHandView.Children)
            {
                rightHandView.Remove(child);
                child.Dispose();
            }
            //create new Right Hand View
            ViewBase view = control as ViewBase;
            if (view != null)
            {
                rightHandView.Add(view.MainWidget);
                rightHandView.ShowAll();
            }
        }

        /// <summary>Get screenshot of right hand panel.</summary>
        public System.Drawing.Image GetScreenshotOfRightHandPanel()
        {

            throw new NotImplementedException("tbi - gtk3 equivalent");

        }

        /// <summary>Show the wait cursor</summary>
        /// <param name="wait">If true will show the wait cursor otherwise the normal cursor.</param>
        public void ShowWaitCursor(bool wait)
        {
            MainView.MasterView.WaitCursor = wait;
        }

        /// <summary>Gets or sets the width of the tree view.</summary>
        public Int32 TreeWidth
        {
            get { return treeview1.Allocation.Width; } 
            set { treeview1.WidthRequest = value; }
        }

#region Protected & Privates

        /// <summary>
        /// Configure the specified tree node using the fields in 'Description'.
        /// Recursively descends through all child nodes as well.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="description">The description.</param>
        private void RefreshNode(TreeIter node, TreeViewNode description)
        {
            Gdk.Pixbuf pixbuf;
            if (MainView.MasterView.HasResource(description.ResourceNameForImage))
            {
                pixbuf = new Gdk.Pixbuf(null, description.ResourceNameForImage);
            }
            else
            {
                // Search for image based on resource name including model name from namespace
                string[] splitResourceName = description.ResourceNameForImage.Split('.');
                string resourceNameOnly = "ApsimNG.Resources.TreeViewImages." + splitResourceName[splitResourceName.Length - 2] + "." + splitResourceName[splitResourceName.Length - 1];
                if (MainView.MasterView.HasResource(resourceNameOnly))
                {
                    pixbuf = new Gdk.Pixbuf(null, resourceNameOnly);
                }
                else
                {
                    // Is there something else we could use as a default?
                    pixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.TreeViewImages.Simulations.svg");
                }
            }

            treemodel.SetValues(node, description.Name, pixbuf, description.ToolTip);

            for (int i = 0; i < description.Children.Count; i++)
            {
                TreeIter iter = treemodel.AppendNode(node);
                RefreshNode(iter, description.Children[i]);
            }
        }

        /// <summary>Return a full path for the specified node.</summary>
        /// <param name="path">The node.</param>
        private string FullPath(TreePath path)
        {
            string result = "";
            if (path != null)
            {
                int[] ilist = path.Indices;
                TreeIter iter;
                treemodel.IterNthChild(out iter, ilist[0]);
                result = "." + (string)treemodel.GetValue(iter, 0);
                for (int i = 1; i < ilist.Length; i++)
                {
                   treemodel.IterNthChild(out iter, iter, ilist[i]);
                   result += "." + (string)treemodel.GetValue(iter, 0);
                }
            }
            return result;
        }

        /// <summary>
        /// Find a specific node with the node path.
        /// NodePath format: .Parent.Child.SubChild
        /// </summary>
        /// <param name="namePath">The name path.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Invalid name path ' + namePath + '</exception>
        private TreeIter FindNode(string namePath)
        {
            if (!namePath.StartsWith(".", StringComparison.CurrentCulture))
            {
                throw new Exception("Invalid name path '" + namePath + "'");
            }

            namePath = namePath.Remove(0, 1); // Remove the leading '.'

            string[] namePathBits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            TreeIter result = TreeIter.Zero;
            TreeIter iter;
            treemodel.GetIterFirst(out iter);

            foreach (string pathBit in namePathBits)
            {
                string nodeName = (string)treemodel.GetValue(iter, 0);
                while (nodeName != pathBit && treemodel.IterNext(ref iter))
                {
                    nodeName = (string)treemodel.GetValue(iter, 0);
                }

                if (nodeName == pathBit)
                {
                    result = iter;
                    TreePath path = treemodel.GetPath(iter);
                    if (!treeview1.GetRowExpanded(path))
                    {
                        treeview1.ExpandRow(path, false);
                    }

                    treemodel.IterChildren(out iter, iter);
                }
                else
                {
                    return TreeIter.Zero;
                }
            }
            return result;         
        }

#endregion

#region Events
        /// <summary>User has selected a node. Raise event for presenter.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments instance containing the event data.</param>
        private void OnAfterSelect(object sender, EventArgs e)
        {
            if (SelectedNodeChanged != null)
            {
                NodeSelectedArgs selectionChangedData = new NodeSelectedArgs();
                selectionChangedData.OldNodePath = previouslySelectedNodePath;
                TreePath selPath;
                TreeViewColumn selCol;
                treeview1.GetCursor(out selPath, out selCol);
                selectionChangedData.NewNodePath = FullPath(selPath);
                if (selectionChangedData.NewNodePath != selectionChangedData.OldNodePath)
                {
                    SelectedNodeChanged.Invoke(this, selectionChangedData);
                }

                previouslySelectedNodePath = selectionChangedData.NewNodePath;
            }
        }

        /// <summary>
        /// Handle loss of focus by removing the accelerators from the popup menu
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void Treeview1_FocusOutEvent(object o, FocusOutEventArgs args)
        {
            (treeview1.Toplevel as Gtk.Window).RemoveAccelGroup(accel);
        }

        /// <summary>
        /// Handle receiving focus by adding accelerators for the popup menu
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void Treeview1_FocusInEvent(object o, FocusInEventArgs args)
        {
            (treeview1.Toplevel as Gtk.Window).AddAccelGroup(accel);
        }

        /// <summary>
        /// Handle button press events to possibly begin editing an item name.
        /// This is in an attempt to rather slavishly follow Windows conventions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [GLib.ConnectBefore]
        private void OnButtonPress(object sender, ButtonPressEventArgs e)
        {
            timer.Stop();
            if (e.Event.Button == 1 && e.Event.Type == Gdk.EventType.ButtonPress)
            {
                TreePath path;
                TreeViewColumn col;
                // Get the clicked location
                if (treeview1.GetPathAtPos((int)e.Event.X, (int)e.Event.Y, out path, out col))
                {
                    // See if the click was on the current selection
                    TreePath selPath;
                    TreeViewColumn selCol;
                    treeview1.GetCursor(out selPath, out selCol);
                    if (selPath != null && path.Compare(selPath) == 0)
                    {
                        // Check where on the row we are located, allowing 16 pixels for the image, and 2 for its border
                        Gdk.Rectangle rect = treeview1.GetCellArea(path, col);
                        if (e.Event.X > rect.X + 18)
                        {
                            timer.Interval = Settings.Default.DoubleClickTime + 10;  // We want this to be a bit longer than the double-click interval, which is normally 250 milliseconds
                            timer.AutoReset = false;
                            timer.Start();
                        }
                    }
                }
            }
        }

        private void OnRowActivated(object sender, RowActivatedArgs e)
        {
            timer.Stop();
            if (treeview1.GetRowExpanded(e.Path))
            {
                treeview1.CollapseRow(e.Path);
            }
            else
            {
                treeview1.ExpandRow(e.Path, false);
            }

            e.RetVal = true;
        }

        /// <summary>
        /// Displays the popup menu when the right mouse button is released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonUp(object sender, ButtonReleaseEventArgs e)
        {
            if (e.Event.Button == 3)
            {
                popup.Popup();
            }
        }

        /// <summary>
        /// Get whatever text is currently on a specific clipboard.
        /// </summary>
        /// <param name="clipboardName">Name of the clipboard.</param>
        /// <returns></returns>
        public string GetClipboardText(string clipboardName)
        {
            Gdk.Atom modelClipboard = Gdk.Atom.Intern(clipboardName, false);
            Clipboard cb = Clipboard.Get(modelClipboard);
            
            return cb.WaitForText();
        }

        /// <summary>
        /// Place text on a specific clipboard.
        /// </summary>
        /// <param name="text">Text to place on the clipboard.</param>
        /// <param name="clipboardName">Name of the clipboard.</param>
        public void SetClipboardText(string text, string clipboardName)
        {
            Gdk.Atom modelClipboard = Gdk.Atom.Intern(clipboardName, false);
            Clipboard cb = Clipboard.Get(modelClipboard);
            cb.Text = text;            
        }

#endregion
    }
}
