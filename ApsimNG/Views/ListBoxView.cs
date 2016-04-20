// -----------------------------------------------------------------------
// <copyright file="ListBoxView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
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

    /// <summary>A list view.</summary>
    public class ListBoxView : ViewBase,  IListBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Invoked when the user double clicks the selection</summary>
        public event EventHandler DoubleClicked;

        public TreeView listview;
        private ListStore listmodel = new ListStore(typeof(string));

        /// <summary>Constructor</summary>
        public ListBoxView(ViewBase owner) : base(owner)
        {
            listview = new TreeView(listmodel);
            _mainWidget = listview;
            CellRendererText textRender = new Gtk.CellRendererText();
            TreeViewColumn column = new TreeViewColumn("Values", textRender, "text", 0);
            listview.AppendColumn(column);
            listview.HeadersVisible = false;
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
                    listmodel.AppendValues(val);
            }
        }

        /// <summary>Gets or sets the selected value.</summary>
        public string SelectedValue
        {
            get
            {
                TreePath selPath;
                TreeViewColumn selCol;
                listview.GetCursor(out selPath, out selCol);
                if (selPath == null)
                    return null;
                else
                {
                    TreeIter iter;
                    listmodel.GetIter(out iter, selPath);
                    return (string)listmodel.GetValue(iter, 0);
                }
            }
            set
            {
                TreePath selPath;
                TreeViewColumn selCol;
                listview.GetCursor(out selPath, out selCol);
                if (selPath != null)
                {
                    TreeIter iter;
                    listmodel.GetIter(out iter, selPath);
                    listmodel.SetValue(iter, 0, value);
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
        private void OnDoubleClick(object sender, EventArgs e)
        {
            if (DoubleClicked != null)
                DoubleClicked.Invoke(this, e);
        }
    }
}
