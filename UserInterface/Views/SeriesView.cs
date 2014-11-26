// -----------------------------------------------------------------------
// <copyright file="SeriesView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using Interfaces;

    /// <summary>
    /// A view for adding, removing and editing graph series.
    /// </summary>
    public partial class SeriesView : UserControl, ISeriesView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesView" /> class.
        /// </summary>
        public SeriesView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when a series has been selected by user.
        /// </summary>
        public event EventHandler SeriesSelected;

        /// <summary>
        /// Invoked when a new empty series is added.
        /// </summary>
        public event EventHandler SeriesAdded;

        /// <summary>
        /// Invoked when a series is deleted.
        /// </summary>
        public event EventHandler SeriesDeleted;

        /// <summary>
        /// Invoked when a series is deleted.
        /// </summary>
        public event EventHandler AllSeriesCleared;

        /// <summary>
        /// Invoked when a series is renamed
        /// </summary>
        public event EventHandler SeriesRenamed;

        /// <summary>
        /// Gets the series editor.
        /// </summary>
        public ISeriesEditorView SeriesEditor
        {
            get
            {
                return this.seriesEditorView1;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the series editor is visible.
        /// </summary>
        public bool EditorVisible
        {
            get
            {
                return this.seriesEditorView1.Visible;
            }

            set
            {
                this.seriesEditorView1.Visible = value;
            }
        }

        /// <summary>
        /// Gets or sets the series names.
        /// </summary>
        public string[] SeriesNames
        {
            get
            {
                List<string> names = new List<string>();
                foreach (ListViewItem item in this.listView1.Items)
                {
                    names.Add(item.Text);
                }

                return names.ToArray();
            }

            set
            {
                this.listView1.Items.Clear();
                foreach (string st in value)
                {
                    this.listView1.Items.Add(st);
                }

                if (this.listView1.Items.Count > 0)
                    this.listView1.Items[0].Selected = true;
            }
        }

        /// <summary>
        /// Gets or sets the selected series name.
        /// </summary>
        public string SelectedSeriesName
        {
            get
            {
                if (this.listView1.SelectedItems.Count > 0)
                {
                    return this.listView1.SelectedItems[0].Text;
                }

                return null;
            }

            set
            {
                foreach (ListViewItem item in this.listView1.Items)
                {
                    item.Selected = item.Text == value;
                }
            }
        }

        /// <summary>
        /// User has changed the series.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnListView1SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.SeriesSelected != null)
            {
                this.SeriesSelected.Invoke(sender, e);
            }
        }

        /// <summary>
        /// Add a new series
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnAddToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (this.SeriesAdded != null)
            {
                this.SeriesAdded.Invoke(sender, e);
            }
        }

        /// <summary>
        /// Delete the selected series.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnDeleteToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (this.SeriesDeleted != null)
            {
                this.SeriesDeleted.Invoke(sender, e);
            }
        }

        /// <summary>
        /// Clear all series
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnClearAllSeriesToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (this.AllSeriesCleared != null)
            {
                this.AllSeriesCleared.Invoke(sender, e);
            }
        }

        /// <summary>
        /// User has finished renaming a series name.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSeriesRenameAfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (this.SeriesRenamed != null && e.Label != null)
            {
                listView1.Items[e.Item].Text = e.Label;
                this.SeriesRenamed.Invoke(sender, null);
            }
        }
    }
}
