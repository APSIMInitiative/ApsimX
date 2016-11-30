// -----------------------------------------------------------------------
// <copyright file="GridCell.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
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
                else if (this.gridView.Grid[this.ColumnIndex, this.RowIndex] is DataGridViewButtonCell)
                {
                    return EditorTypeEnum.Button;
                }
                */
                else if (gridView.comboLookup.ContainsKey(new Tuple<int, int>(this.RowIndex, this.ColumnIndex)))
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
                            /// TBI this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new DataGridViewTextBoxCell();
                            break;
                        }

                    case EditorTypeEnum.Boolean:
                        {
                            /// TBI this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new DataGridViewCheckBoxCell();
                            break;
                        }

                    case EditorTypeEnum.Colour:
                        {
                            /// TBI this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new Utility.ColorPickerCell();
                            break;
                        }

                    case EditorTypeEnum.DateTime:
                        {
                            /// TBI this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new CalendarCell();
                            break;
                        }

                    case EditorTypeEnum.Button:
                        {
                            /// TBI this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new DataGridViewButtonCell();
                            break;
                        }

                    case EditorTypeEnum.DropDown:
                        {
                            Tuple<int, int> key = new Tuple<int, int>(this.RowIndex, this.ColumnIndex);
                            if (!gridView.comboLookup.ContainsKey(key))
                            {
                                ListStore store = new ListStore(typeof(string));
                                gridView.comboLookup.Add(key, store);
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
                if (gridView.comboLookup.TryGetValue(new Tuple<int, int>(this.RowIndex, this.ColumnIndex), out store))
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
                if (gridView.comboLookup.TryGetValue(new Tuple<int, int>(this.RowIndex, this.ColumnIndex), out store))
                {
                    store.Clear();
                    foreach (string st in value)
                    {
                        store.AppendValues(st);
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
                return ""; /// TBI this.gridView.Grid[this.ColumnIndex, this.RowIndex].ToolTipText;
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
    }
}
