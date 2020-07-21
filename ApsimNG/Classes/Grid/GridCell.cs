namespace UserInterface.Classes
{
    using System;
    using Interfaces;
    using Gtk;
    using Views;

    /// <summary>
    /// Represents a grid cell.
    /// </summary>
    public class GridCell : IGridCell
    {
        /// <summary>
        /// The parent grid that this column belongs to.
        /// </summary>
        private GridView gridView;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridCell" /> class.
        /// </summary>
        /// <param name="gridView">The grid that the cell belongs to</param>
        /// <param name="columnIndex">The column index of the cell</param>
        /// <param name="rowIndex">The row index of the cell</param>
        public GridCell(GridView gridView, int columnIndex, int rowIndex)
        {
            this.gridView = gridView;
            this.ColumnIndex = columnIndex;
            this.RowIndex = rowIndex;
        }

        /// <summary>
        /// Gets the column index of the column
        /// </summary>
        public int ColumnIndex { get; private set; }

        /// <summary>
        /// Gets the row index of the column
        /// </summary>
        public int RowIndex { get; private set; }

        /// <summary>
        /// Gets or sets the editor type for the cell
        /// </summary>
        public EditorTypeEnum EditorType
        {
            get
            {
                if (this.ColumnIndex < 0 || this.RowIndex < 0)
                    return EditorTypeEnum.TextBox;
                /* TBI
                if (this.gridView.Grid[this.ColumnIndex, this.RowIndex] == null)
                {
                    return EditorTypeEnum.TextBox;
                }
                else if (this.gridView.Grid[this.ColumnIndex, this.RowIndex] is DataGridViewCheckBoxCell)
                {
                    return EditorTypeEnum.Boolean;
                }
                else if (this.gridView.Grid[this.ColumnIndex, this.RowIndex] is Utility.ColorPickerCell)
                {
                    return EditorTypeEnum.Colour;
                }
                else if (this.gridView.Grid[this.ColumnIndex, this.RowIndex] is CalendarCell)
                {
                    return EditorTypeEnum.DateTime;
                }
                */
                Tuple<int, int> key = new Tuple<int, int>(this.RowIndex, this.ColumnIndex);
                if (gridView.ButtonList.Contains(key))
                {
                    return EditorTypeEnum.Button;
                }
                
                else if (gridView.ComboLookup.ContainsKey(key))
                    return EditorTypeEnum.DropDown;
                else
                    return EditorTypeEnum.TextBox;
            }

            set
            {
                object cellValue = this.Value;

                switch (value)
                {
                    case EditorTypeEnum.TextBox:
                        {
                            // TBI this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new DataGridViewTextBoxCell();
                            break;
                        }

                    case EditorTypeEnum.Boolean:
                        {
                            // TBI this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new DataGridViewCheckBoxCell();
                            break;
                        }

                    case EditorTypeEnum.Colour:
                        {
                            // TBI this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new Utility.ColorPickerCell();
                            break;
                        }

                    case EditorTypeEnum.DateTime:
                        {
                            // TBI this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new CalendarCell();
                            break;
                        }
                    case EditorTypeEnum.MultiFiles:
                    case EditorTypeEnum.DirectoryChooser:
                    case EditorTypeEnum.Button:
                        {
                            Tuple<int, int> key = new Tuple<int, int>(this.RowIndex, this.ColumnIndex);
                            if (!gridView.ButtonList.Contains(key))
                            {
                                gridView.ButtonList.Add(key);
                            }
                            break;
                        }

                    case EditorTypeEnum.DropDown:
                        {
                            Tuple<int, int> key = new Tuple<int, int>(this.RowIndex, this.ColumnIndex);
                            if (!gridView.ComboLookup.ContainsKey(key))
                            {
                                ListStore store = new ListStore(typeof(string), typeof(string));
                                gridView.ComboLookup.Add(key, store);
                            }
                            break;
                        }
                }

                this.Value = cellValue;
            }
        }

        /// <summary>
        /// Gets or sets the strings to be used in the drop down editor for this cell
        /// </summary>
        public string[] DropDownStrings
        {
            get
            {
                ListStore store;
                if (gridView.ComboLookup.TryGetValue(new Tuple<int, int>(this.RowIndex, this.ColumnIndex), out store))
                {
                    int nNames = store.IterNChildren();
                    string[] result = new string[nNames];
                    TreeIter iter;
                    int i = 0;
                    if (store.GetIterFirst(out iter))
                        do
                            result[i++] = (string)store.GetValue(iter, 0);
                        while (store.IterNext(ref iter) && i < nNames);
                    return result;
                }
                return null;
            }

            set
            {
                ListStore store;
                if (gridView.ComboLookup.TryGetValue(new Tuple<int, int>(this.RowIndex, this.ColumnIndex), out store))
                {
                    store.Clear();
                    foreach (string st in value)
                    {
                        // Warning: this is using the pipe character to delimit display text from tooltip text.
                        // This could potentially be problematic if the display text is meant to include the character.
                        // This also currently throws everything away after any second pipe character.
                        string[] strings = st.Split('|');
                        store.AppendValues(strings[0], strings.Length > 1 ? strings[1] : null);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a cell's tooltip.
        /// </summary>
        public string ToolTip
        {
            get
            {
                // TBI this.gridView.Grid[this.ColumnIndex, this.RowIndex].ToolTipText;
                return "";
            }

            set
            {
                /* TBI
                this.gridView.Grid[this.ColumnIndex, this.RowIndex].ToolTipText = value;
                this.gridView.Grid.ShowCellToolTips = true;
                */
            }
        }

        /// <summary>
        /// Gets or sets the cell value
        /// </summary>
        public object Value
        {
            get
            {
                return this.gridView.DataSource.Rows[this.RowIndex][this.ColumnIndex];
            }

            set
            {
                this.gridView.DataSource.Rows[this.RowIndex][this.ColumnIndex] = value;
            }
        }

        /// <summary>
        /// Gets or sets the cell readonly status
        /// </summary>
        public bool IsRowReadonly
        {
            get
            {
                return gridView.IsRowReadonly(RowIndex);
            }
            set
            {
                gridView.SetRowAsReadonly(RowIndex, value);
            }
        }
    }
}
