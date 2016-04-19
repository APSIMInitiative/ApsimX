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
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;

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
    public partial class ListBoxView : UserControl, IListBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Invoked when the user double clicks the selection</summary>
        public event EventHandler DoubleClicked;

        /// <summary>Constructor</summary>
        public ListBoxView()
        {
            InitializeComponent();
            listBox1.Visible = true;
        }

        /// <summary>Get or sets the list of valid values.</summary>
        public string[] Values
        {
            get
            {
                List<string> items = new List<string>();
                if (listBox1.Items != null)
                    foreach (string item in listBox1.Items)
                        items.Add(item);
                return items.ToArray();
            }
            set
            {
                PopulateListView(value);
            }
        }

        /// <summary>Populate the list view with items.</summary>
        /// <param name="values"></param>
        private void PopulateListView(string[] values)
        {
            listBox1.Items.Clear();
            foreach (string st in values)
            {
                ListViewItem newItem;
                int posLastSlash = st.LastIndexOfAny("\\/".ToCharArray());
                if (posLastSlash != -1)
                    newItem = AddFileNameListItem(st);
                else
                {
                    newItem = new ListViewItem(st);
                    listBox1.View = View.List;
                }
                listBox1.Items.Add(newItem);
            }
        }

        /// <summary>
        /// Add a list item based on a file name
        /// </summary>
        /// <param name="fileName">The filename.</param>
        private ListViewItem AddFileNameListItem(string fileName)
        {

            List<string> resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames().ToList();
            List<string> largeImageNames = resourceNames.FindAll(r => r.Contains(".LargeImages."));

            ListViewItem newItem = new ListViewItem();

            // A filename was detected so add the path as a sub item.
            int posLastSlash = fileName.LastIndexOfAny("\\/".ToCharArray());

            newItem.Text = fileName.Substring(posLastSlash + 1);
            newItem.SubItems.Add(fileName.Substring(0, posLastSlash));

            // Add column headers so the subitems will appear.
            if (listBox1.Columns.Count == 0)
            {
                listBox1.Columns.AddRange(new ColumnHeader[] { new ColumnHeader(), new ColumnHeader(), new ColumnHeader() });
                imageList1.Images.Add(Properties.Resources.apsim_logo32);
            }

            // Add an image index.
            foreach (string largeImageName in largeImageNames)
            {
                string shortImageName = StringUtilities.GetAfter(largeImageName, ".LargeImages.").Replace(".png", "").ToLower();
                if (newItem.Text.ToLower().Contains(shortImageName))
                {
                    newItem.ImageIndex = AddImage(imageList1, largeImageName);
                    break;
                }
            }

            if (newItem.ImageIndex == -1)
                newItem.ImageIndex = 0;

            // Initialize the tile size.
            listBox1.TileSize = new Size(400, 45);

            return newItem;
        }

        /// <summary>Add an image to an image list if it doesn't already exist.</summary>
        /// <param name="imageList">The image list to add to.</param>
        /// <param name="resourceName">The resource name of the image to add.</param>
        private static int AddImage(ImageList imageList, string resourceName)
        {
            int imageIndex = imageList.Images.IndexOfKey(resourceName);
            if (imageIndex == -1)
            {
                imageList.Images.Add(resourceName, new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)));
                imageIndex = imageList.Images.IndexOfKey(resourceName);
            }

            return imageIndex;
        }

        /// <summary>Gets or sets the selected value.</summary>
        public string SelectedValue
        {
            get
            {
                if (listBox1.SelectedItems.Count == 0)
                    return null;
                string name = listBox1.SelectedItems[0].Text;
                if (listBox1.SelectedItems[0].SubItems.Count > 1)
                    name = Path.Combine(listBox1.SelectedItems[0].SubItems[1].Text, name);
                return name;
            }
            set
            {
                if (listBox1.Text != value)
                    listBox1.Text = value;
            }
        }

        /// <summary>Return true if dropdown is visible.</summary>
        public bool IsVisible
        {
            get { return listBox1.Visible; }
            set { listBox1.Visible = value; }
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
        private void OnDoubleClick(object sender, MouseEventArgs e)
        {
            if (DoubleClicked != null)
                DoubleClicked.Invoke(this, e);
        }


    }
}
