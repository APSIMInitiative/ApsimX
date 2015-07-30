// -----------------------------------------------------------------------
// <copyright file="GridView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Windows.Forms;
    using Classes;
    using DataGridViewAutoFilter;
    using EventArguments;
    using Interfaces;

    /// <summary>
    /// A grid control that implements the grid view interface.
    /// </summary>
    public partial class GridView : UserControl, IGridView
    {
        /// <summary>
        /// Is the user currently editing a cell?
        /// </summary>
        private bool userEditingCell = false;

        /// <summary>
        /// The value before the user starts editing a cell.
        /// </summary>
        private object valueBeforeEdit;

        /// <summary>
        /// The data table that is being shown on the grid.
        /// </summary>
        private DataTable table;

        /// <summary>
        /// A value indicating whether auto filter is turned on.
        /// </summary>
        private bool isAutoFilterOn = false;

        /// <summary>
        /// The default numeric format
        /// </summary>
        private string defaultNumericFormat = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridView" /> class.
        /// </summary>
        public GridView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// This event is invoked when the values of 1 or more cells have changed.
        /// </summary>
        public event EventHandler<GridCellsChangedArgs> CellsChanged;

        /// <summary>
        /// Invoked when a grid cell header is clicked.
        /// </summary>
        public event EventHandler<GridHeaderClickedArgs> ColumnHeaderClicked;

        /// <summary>Occurs when user clicks a button on the cell.</summary>
        public event EventHandler<GridCellsChangedArgs> ButtonClick;

        /// <summary>
        /// Gets or sets the data to use to populate the grid.
        /// </summary>
        public System.Data.DataTable DataSource
        {
            get
            {
                return this.table;
            }
            
            set
            {
                this.table = value;
                this.PopulateGrid();
            }
        }

        /// <summary>
        /// The name of the associated model.
        /// </summary>
        public string ModelName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of rows in grid.
        /// </summary>
        public int RowCount
        {
            get
            {
                return this.Grid.RowCount;
            }
            
            set
            {
                this.Grid.RowCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the numeric grid format e.g. N3
        /// </summary>
        public string NumericFormat
        {
            get
            {
                return this.defaultNumericFormat;
            }

            set
            {
                this.defaultNumericFormat = value;

                if (this.DataSource != null)
                {
                    for (int col = 0; col < this.DataSource.Columns.Count; col++)
                    {
                        if (this.DataSource.Columns[col].DataType == typeof(float) ||
                            this.DataSource.Columns[col].DataType == typeof(double))
                        {
                            this.Grid.Columns[col].DefaultCellStyle.Format = this.defaultNumericFormat;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the grid is read only
        /// </summary>
        public bool ReadOnly 
        { 
            get 
            {
                return this.Grid.ReadOnly; 
            } 
            
            set 
            {
                this.Grid.ReadOnly = value; 
            } 
        }

        /// <summary>
        /// Gets or sets a value indicating whether the grid has an auto filter
        /// </summary>
        public bool AutoFilterOn
        {
            get
            {
                return this.isAutoFilterOn;
            }
            
            set 
            {

                // MONO doesn't seem to like the auto filter option.
                if (Environment.OSVersion.Platform == PlatformID.Win32NT ||
                    Environment.OSVersion.Platform == PlatformID.Win32Windows)
                {
                    this.isAutoFilterOn = value;
                    this.PopulateGrid();
                }    
            }
        }

        /// <summary>
        /// Gets or sets the currently selected cell. Null if none selected.
        /// </summary>
        public IGridCell GetCurrentCell
        {
            get
            {
                if (this.Grid.CurrentCell == null)
                {
                    return null;
                }

                return this.GetCell(this.Grid.CurrentCell.ColumnIndex, this.Grid.CurrentCell.RowIndex);
            }

            set
            {
                if (value != null && 
                    value.ColumnIndex < this.Grid.ColumnCount &&
                    value.RowIndex < this.Grid.RowCount)
                {
                    this.Grid.CurrentCell = this.Grid[value.ColumnIndex, value.RowIndex];
                }
            }
        }

        /// <summary>
        /// Return a particular cell of the grid.
        /// </summary>
        /// <param name="columnIndex">The column index</param>
        /// <param name="rowIndex">The row index</param>
        /// <returns>The cell</returns>
        public IGridCell GetCell(int columnIndex, int rowIndex)
        {
            return new GridCell(this, columnIndex, rowIndex);
        }

        /// <summary>
        /// Return a particular column of the grid.
        /// </summary>
        /// <param name="columnIndex">The column index</param>
        /// <returns>The column</returns>
        public IGridColumn GetColumn(int columnIndex)
        {
            return new GridColumn(this, columnIndex);
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        public void AddContextAction(string menuItemText, System.EventHandler onClick)
        {
            ToolStripItem item = this.popupMenu.Items.Add(menuItemText);
            item.Click += onClick;
        }

        /// <summary>
        /// Clear all presenter defined context items.
        /// </summary>
        public void ClearContextActions()
        {
            while (this.popupMenu.Items.Count > 3)
                this.popupMenu.Items.RemoveAt(3);
        }

        /// <summary>
        /// Loads an image from a supplied bitmap.
        /// </summary>
        /// <param name="bitmap">The image to display.</param>
        public void LoadImage(Bitmap bitmap)
        {
            pictureBox1.Image = bitmap;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
        }

        /// <summary>
        /// Loads an image from a manifest resource.
        /// </summary>
        public void LoadImage()
        {
            System.Reflection.Assembly thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            System.IO.Stream file = thisExe.GetManifestResourceStream("UserInterface.Resources.PresenterPictures." + ModelName + ".png");
            if (file == null)
                pictureBox1.Visible = false;
            else
            {
                pictureBox1.Image = Image.FromStream(file);
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            }
        }

        /// <summary>
        /// Returns true if the grid row is empty.
        /// </summary>
        /// <param name="rowIndex">The row index</param>
        /// <returns>True if the row is empty</returns>
        public bool RowIsEmpty(int rowIndex)
        {
            foreach (DataGridViewColumn column in this.Grid.Columns)
            {
                if (this.Grid[column.Index, rowIndex].Value != null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Populate the grid from the DataSource.
        /// </summary>
        private void PopulateGrid()
        {
            this.Grid.DefaultCellStyle.Font = this.Grid.Font;
            this.popupMenu.Font = this.Font;
            this.Grid.ColumnHeadersDefaultCellStyle.Font = this.Grid.Font;

            // The DataGridViewAutoFilterColumnHeaderCell class needs DataSource to be set.
            this.Grid.DataSource = null;
            this.Grid.Columns.Clear();
            this.Grid.Rows.Clear();

            Cursor.Current = Cursors.WaitCursor;

            if (this.DataSource != null)
            {
                // Under MONO for LINUX, when Grid.EditMode = DataGridViewEditMode.EditOnEnter
                // then the populating code below will cause the grid to go into edit mode.
                // For now turn off edit mode temporarily.
                this.Grid.EditMode = DataGridViewEditMode.EditProgrammatically;

                // The populating code below will cause Grid.CellValueChanged to be invoked
                // Turn this event off temporarily.
                this.Grid.CellValueChanged -= this.OnCellValueChanged;

                // If autofilter is on then use a data bound grid.
                if (this.isAutoFilterOn)
                {
                    this.Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                    this.Grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    this.Grid.DataSource = new BindingSource(this.table, null);
                    foreach (DataGridViewColumn column in this.Grid.Columns)
                    {
                        column.HeaderCell = new DataGridViewAutoFilterColumnHeaderCell(column.HeaderCell);
                        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                        this.NumericFormat = this.defaultNumericFormat;
                    }
                }
                else
                {
                    // Make sure we have the right number of columns.
                    this.Grid.ColumnCount = Math.Max(this.DataSource.Columns.Count, 1);

                    // Turn off autosizing - too slow.
                    foreach (DataGridViewColumn col in this.Grid.Columns)
                    {
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    }

                    // Populate the grid headers.
                    bool headersContainLineFeeds = false;
                    for (int col = 0; col < this.DataSource.Columns.Count; col++)
                    {
                        this.Grid.Columns[col].HeaderText = this.DataSource.Columns[col].ColumnName;
                        if (this.Grid.Columns[col].HeaderText.Contains("\n"))
                            headersContainLineFeeds = true;

                        this.Grid.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        this.Grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        this.Grid.Columns[col].SortMode = DataGridViewColumnSortMode.NotSortable;

                        this.NumericFormat = this.defaultNumericFormat;
                    }

                    // Looks like MONO ignores ColumnHeadersHeightSizeMode property.
                    if (headersContainLineFeeds)
                    {
                        this.Grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
                        this.Grid.ColumnHeadersHeight = Convert.ToInt32(this.Grid.RowTemplate.Height * 3.5);

                    }

                    // Populate the grid cells with new rows.
                    if (this.DataSource.Rows.Count > 0)
                    {
                        this.Grid.RowCount = 1;
                    }

                    for (int row = 0; row < this.DataSource.Rows.Count; row++)
                    {
                        for (int col = 0; col < this.DataSource.Columns.Count; col++)
                        {
                            this.Grid[col, row].Value = this.DataSource.Rows[row][col];
                            //if (row == 0)
                            //{
                            //    this.Grid.Columns[col].Width = Math.Max(this.Grid.Columns[col].MinimumWidth,
                            //                                            this.Grid.Columns[col].GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, true));
                            //}
                        }
                        this.Grid.RowCount = this.DataSource.Rows.Count;
                    }
                }

                // ColIndex doesn't matter since we're resizing all of them.
                this.GetColumn(0).Width = -1;

                // Turn on autosizing.
                foreach (DataGridViewColumn col in this.Grid.Columns)
                {
                    col.Width = Convert.ToInt32(col.GetPreferredWidth(DataGridViewAutoSizeColumnMode.DisplayedCells, true) * 1.2);

                    //col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                    //int newWidth = Convert.ToInt32(col.Width * 1.0);
                    //col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    //col.Width = newWidth;
                }

                // Reinstate Grid.CellValueChanged event.
                this.Grid.CellValueChanged += this.OnCellValueChanged;

                // Reinstate our desired edit mode.
                this.Grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            }
            else
            {
                this.Grid.Columns.Clear();
            }

            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// User is about to edit a cell.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            this.userEditingCell = true;
            this.valueBeforeEdit = this.Grid[e.ColumnIndex, e.RowIndex].Value;
        }

        /// <summary>
        /// User has finished editing a cell.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (this.userEditingCell)
            {
                object oldValue = this.valueBeforeEdit;
                
                this.userEditingCell = false;

                // Make sure our table has enough rows.
                object newValue = this.Grid[e.ColumnIndex, e.RowIndex].Value;
                if (newValue == null)
                {
                    newValue = DBNull.Value;
                }

                while (this.DataSource != null && e.RowIndex >= this.DataSource.Rows.Count)
                {
                    this.DataSource.Rows.Add(this.DataSource.NewRow());
                }

                // Put the new value into the table on the correct row.
                if (this.DataSource != null)
                {
                    this.DataSource.Rows[e.RowIndex][e.ColumnIndex] = newValue;
                }

                if (this.valueBeforeEdit != null && this.valueBeforeEdit.GetType() == typeof(string) && newValue == null)
                {
                    newValue = string.Empty;
                }

                if (this.CellsChanged != null && this.valueBeforeEdit != newValue)
                {
                    GridCellsChangedArgs args = new GridCellsChangedArgs();
                    args.ChangedCells = new List<IGridCell>();
                    args.ChangedCells.Add(this.GetCell(e.ColumnIndex, e.RowIndex));
                    this.CellsChanged(this, args);
                }
            }
        }

        /// <summary>
        /// Called when the window is resized to resize all grid controls.
        /// </summary>
        public void ResizeControls()
        {
            if (Grid.ColumnCount == 0)
                return;

            //resize Grid
            int width = 0;
            int height = 0;

            foreach (DataGridViewColumn col in Grid.Columns)
                width += col.Width;
            foreach (DataGridViewRow row in Grid.Rows)
                    height += row.Height;
            height += Grid.ColumnHeadersHeight;
            if (width + 3 > Grid.Parent.Width)
                Grid.Width = Grid.Parent.Width;
            else
                Grid.Width = width + 3;

            if (height + 25 > (Grid.Parent.Parent == null ? Grid.Parent.Height / 2 : Grid.Parent.Height))
            {
                Grid.Height = Grid.Parent.Parent == null ? Grid.Parent.Height / 2 : Grid.Parent.Height;
                if (width + 25 > Grid.Parent.Width)
                    Grid.Width = Grid.Parent.Width;
                else 
                    Grid.Width += 25; //extra width for scrollbar
            }
            else
                Grid.Height = height + 25;
            Grid.Location = new Point(0, 0);

            if (Grid.RowCount == 0)
            {
                Grid.Width = 0;
                Grid.Height = 0;
                Grid.Visible = false;
            }

            //resize PictureBox
            pictureBox1.Location = new Point(Grid.Width, 0);
            pictureBox1.Height = pictureBox1.Parent.Height;
            pictureBox1.Width = pictureBox1.Parent.Width - pictureBox1.Location.X;
        }

        /// <summary>
        /// Trap any grid data errors, usually as a result of cell values not being
        /// in combo boxes. We'll handle these elsewhere.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnDataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        /// <summary>
        /// User has clicked a cell. 
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnCellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex == -1)
            {
                if (this.ColumnHeaderClicked != null)
                {
                    GridHeaderClickedArgs args = new GridHeaderClickedArgs();
                    args.Column = this.GetColumn(e.ColumnIndex);
                    args.RightClick = e.Button == System.Windows.Forms.MouseButtons.Right;
                    this.ColumnHeaderClicked.Invoke(this, args);
                }
            }
            else if (this.Grid[e.ColumnIndex, e.RowIndex] is Utility.ColorPickerCell)
            {
                ColorDialog dlg = new ColorDialog();

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    this.userEditingCell = true;
                    this.valueBeforeEdit = this.Grid[e.ColumnIndex, e.RowIndex].Value;
                    this.Grid[e.ColumnIndex, e.RowIndex].Value = dlg.Color.ToArgb();
                }
            }
        }

        /// <summary>
        /// We need to trap the EditingControlShowing event so that we can tweak all combo box
        /// cells to allow the user to edit the contents.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (this.Grid.CurrentCell is DataGridViewComboBoxCell)
            {
                DataGridViewComboBoxEditingControl combo = (DataGridViewComboBoxEditingControl)this.Grid.EditingControl;
                combo.DropDownStyle = ComboBoxStyle.DropDown;
            }
        }

        /// <summary>
        /// If the cell being validated is a combo cell then always make sure the cell value 
        /// is in the list of combo items.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnGridCellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (this.Grid.CurrentCell is DataGridViewComboBoxCell)
            {
                DataGridViewComboBoxEditingControl combo = (DataGridViewComboBoxEditingControl)this.Grid.EditingControl;
                if (combo != null && !combo.Items.Contains(e.FormattedValue))
                {
                    combo.Items.Add(e.FormattedValue);
                }
            }
        }

        /// <summary>
        /// Paste from clipboard into grid.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnPasteFromClipboard(object sender, EventArgs e)
        {
            try
            {
                string text = Clipboard.GetText();
                string[] lines = text.Split('\n');
                int rowIndex = this.Grid.CurrentCell.RowIndex;
                int columnIndex = this.Grid.CurrentCell.ColumnIndex;
                List<IGridCell> cellsChanged = new List<IGridCell>();
                foreach (string line in lines)
                {
                    if (rowIndex < this.Grid.RowCount && line.Length > 0)
                    {
                        string[] words = line.Split('\t');
                        for (int i = 0; i < words.GetLength(0); ++i)
                        {
                            if (columnIndex + i < this.Grid.ColumnCount)
                            {
                                DataGridViewCell cell = this.Grid[columnIndex + i, rowIndex];
                                if (!cell.ReadOnly)
                                {
                                    if (cell.Value == null || cell.Value.ToString() != words[i])
                                    {
                                        // We are pasting a new value for this cell. Put the new
                                        // value into the cell.
                                        if (words[i] == string.Empty)
                                        {
                                            cell.Value = null;
                                        }
                                        else
                                        {
                                            cell.Value = Convert.ChangeType(words[i], this.DataSource.Columns[columnIndex + i].DataType);
                                        }

                                        // Make sure there are enough rows in the data source.
                                        while (this.DataSource.Rows.Count <= rowIndex)
                                        {
                                            this.DataSource.Rows.Add(this.DataSource.NewRow());
                                        }

                                        // Put the new value into the data source.
                                        if (cell.Value == null)
                                        {
                                            this.DataSource.Rows[rowIndex][columnIndex + i] = DBNull.Value;
                                        }
                                        else
                                        {
                                            this.DataSource.Rows[rowIndex][columnIndex + i] = cell.Value;
                                        }

                                        // Put a cell into the cells changed member.
                                        cellsChanged.Add(this.GetCell(columnIndex + i, rowIndex));
                                    }
                                }
                            }
                            else
                            { 
                                break; 
                            }
                        }

                        rowIndex++;
                    }
                    else
                    { 
                        break; 
                    }
                }

                // If some cells were changed then send out an event.
                if (cellsChanged.Count > 0 && this.CellsChanged != null)
                {
                    this.CellsChanged.Invoke(this, new GridCellsChangedArgs() { ChangedCells = cellsChanged });
                }
            }
            catch (FormatException)
            {
            }
        }

        /// <summary>
        /// Copy to clipboard
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnCopyToClipboard(object sender, EventArgs e)
        {
            DataObject content = this.Grid.GetClipboardContent();
            Clipboard.SetDataObject(content);
        }

        /// <summary>
        /// Delete was clicked by the user.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnDeleteClick(object sender, EventArgs e)
        {
            List<IGridCell> cellsChanged = new List<IGridCell>();
            foreach (DataGridViewCell cell in this.Grid.SelectedCells)
            {
                // Save change in data source
                if (cell.RowIndex < this.DataSource.Rows.Count)
                {
                    this.DataSource.Rows[cell.RowIndex][cell.ColumnIndex] = DBNull.Value;

                    // Delete cell in grid.
                    this.Grid[cell.ColumnIndex, cell.RowIndex].Value = null;

                    // Put a cell into the cells changed member.
                    cellsChanged.Add(this.GetCell(cell.ColumnIndex, cell.RowIndex));
                }
            }

            // If some cells were changed then send out an event.
            if (cellsChanged.Count > 0 && this.CellsChanged != null)
            {
                this.CellsChanged.Invoke(this, new GridCellsChangedArgs() { ChangedCells = cellsChanged });
            }
        }

        private void GridView_Resize(object sender, EventArgs e)
        {
            ResizeControls();
        }

        /// <summary>
        /// User has clicked a cell.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DataGridViewCellEventArgs"/> instance containing the event data.</param>
        private void OnCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            IGridCell cell = this.GetCell(e.ColumnIndex, e.RowIndex);
            if (cell != null && cell.EditorType == EditorTypeEnum.Button)
            {
                GridCellsChangedArgs cellClicked = new GridCellsChangedArgs();
                cellClicked.ChangedCells = new List<IGridCell>();
                cellClicked.ChangedCells.Add(cell);
                ButtonClick(this, cellClicked);
            }
        }
    }
}
