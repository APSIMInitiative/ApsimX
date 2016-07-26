// -----------------------------------------------------------------------
// <copyright file="ListBoxView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Gtk;

    /// <summary>An interface for a list box</summary>
    public interface IListBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Invoked when the user double clicks the selection</summary>
        event EventHandler DoubleClicked;

        /// <summary>Get or sets the list of valid values.</summary>
        string[] Values { get; set; }

        /// <summary>Gets or sets the selected value.</summary>
        string SelectedValue { get; set; }

        /// <summary>Return true if dropdown is visible.</summary>
        bool IsVisible { get; set; }
    }

    public class IkonView : IconView
    {
        public IkonView(TreeModel model) : base(model) { }

        public int ItemPadding
        {
            get { return (int)GetProperty("item-padding"); }
            set { SetProperty("item-padding", new GLib.Value(value)); }
        }
    }

    /// <summary>A list view.</summary>
    public class ListBoxView : ViewBase,  IListBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Invoked when the user double clicks the selection</summary>
        public event EventHandler DoubleClicked;

        public IkonView listview;
        private ListStore listmodel = new ListStore(typeof(string), typeof(Gdk.Pixbuf), typeof(string));

        /// <summary>Constructor</summary>
        public ListBoxView(ViewBase owner) : base(owner)
        {
            listview = new IkonView(listmodel);
            //listview = new TreeView(listmodel);
            _mainWidget = listview;
            listview.TextColumn = 0;
            listview.PixbufColumn = 1;
            listview.TooltipColumn = 2;
            listview.SelectionMode = SelectionMode.Browse;
            listview.Orientation = Gtk.Orientation.Horizontal;
            listview.RowSpacing = 0;
            listview.ColumnSpacing = 0;
            listview.ItemPadding = 0;
            //CellRendererText textRender = new Gtk.CellRendererText();
            //TreeViewColumn column = new TreeViewColumn("Values", textRender, "text", 0);
            //listview.AppendColumn(column);
            //listview.HeadersVisible = false;
            //listview.CursorChanged += OnSelectionChanged;
            listview.SelectionChanged += OnSelectionChanged;
            listview.ButtonPressEvent += OnDoubleClick;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            //listview.CursorChanged -= OnSelectionChanged;
            listview.SelectionChanged -= OnSelectionChanged;
            listview.ButtonPressEvent -= OnDoubleClick;
        }

        /// <summary>Get or sets the list of valid values.</summary>
        public string[] Values
        {
            get
            {
                List<string> items = new List<string>();
                foreach (object[] row in listmodel)
                    items.Add((string)row[0]);
                return items.ToArray();
            }
            set
            {
                listmodel.Clear();
                foreach (string val in value)
                {
                    string text = val;
                    Gdk.Pixbuf image = null;
                    int posLastSlash = val.LastIndexOfAny("\\/".ToCharArray());
                    if (posLastSlash != -1)
                    {
                        text = AddFileNameListItem(val, ref image);
                    }
                    listmodel.AppendValues(text, image, val);
                }
            }
        }

        /// <summary>
        /// Add a list item based on a file name
        /// </summary>
        /// <param name="fileName">The filename.</param>
        private string AddFileNameListItem(string fileName, ref Gdk.Pixbuf image)
        {
            List<string> resourceNames = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames().ToList();
            List<string> largeImageNames = resourceNames.FindAll(r => r.Contains(".LargeImages."));


            // A filename was detected so add the path as a sub item.
            int posLastSlash = fileName.LastIndexOfAny("\\/".ToCharArray());

            string result = fileName.Substring(posLastSlash + 1);

            listview.ItemPadding = 6; // Restore padding if we have images to display

            image = null;
            // Add an image index.
            foreach (string largeImageName in largeImageNames)
            {
                string shortImageName = StringUtilities.GetAfter(largeImageName, ".LargeImages.").Replace(".png", "").ToLower();
                if (result.ToLower().Contains(shortImageName))
                {
                    image = new Gdk.Pixbuf(null, largeImageName);
                    break;
                }
            }
            if (image == null)
               image = new Gdk.Pixbuf(null, "ApsimNG.Resources.apsim logo32.png");
            return result;
        }

        /// <summary>Gets or sets the selected value.</summary>
        public string SelectedValue
        {
            get
            {
                TreePath[] selPath = listview.SelectedItems;
                //TreePath selPath;
                //TreeViewColumn selCol;
                //listview.GetCursor(out selPath, out selCol);
                if (selPath == null)
                    return null;
                else
                {
                    TreeIter iter;
                    listmodel.GetIter(out iter, selPath[0]);
                    if (listmodel.GetValue(iter, 1) != null)
                        return (string)listmodel.GetValue(iter, 2);
                    else
                        return (string)listmodel.GetValue(iter, 0);
                }
            }
            set
            {
                TreePath[] selPath = listview.SelectedItems;
                //TreePath selPath;
                //TreeViewColumn selCol;
                //listview.GetCursor(out selPath, out selCol);
                if (selPath != null)
                {
                    TreeIter iter;
                    listmodel.GetIter(out iter, selPath[0]);
                    listmodel.SetValue(iter, 0, value);
                    listmodel.SetValue(iter, 2, value);
                }
            }
        }

        /// <summary>Return true if the listview is visible.</summary>
        public bool IsVisible
        {
            get { return listview.Visible; }
            set { listview.Visible = value; }
        }

        /// <summary>User has changed the selection.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (Changed != null)
                Changed.Invoke(this, e);
        }

        /// <summary>User has double clicked the list box.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [GLib.ConnectBefore] // Otherwise this is handled internally, and we won't see it
        private void OnDoubleClick(object sender, ButtonPressEventArgs e)
        {
            if (e.Event.Type == Gdk.EventType.TwoButtonPress && e.Event.Button == 1 && DoubleClicked != null)
                DoubleClicked.Invoke(this, e);
        }
    }
}
