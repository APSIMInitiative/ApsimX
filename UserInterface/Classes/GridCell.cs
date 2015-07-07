// -----------------------------------------------------------------------
// <copyright file="GridCell.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using Interfaces;
    using Views;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Represents a grid cell.
    /// </summary>
    public class GridCell : IGridCell
    {
        /// <summary>
        /// The parent grid that this column belongs to
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
                else
                {
                    return EditorTypeEnum.DropDown;
                }
            }

            set
            {
                object cellValue = this.Value;

                switch (value)
                {
                    case EditorTypeEnum.TextBox:
                        {
                            this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new DataGridViewTextBoxCell();
                            break;
                        }

                    case EditorTypeEnum.Boolean:
                        {
                            this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new DataGridViewCheckBoxCell();
                            break;
                        }

                    case EditorTypeEnum.Colour:
                        {
                            this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new Utility.ColorPickerCell();
                            break;
                        }

                    case EditorTypeEnum.DateTime:
                        {
                            this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new CalendarCell();
                            break;
                        }

                    case EditorTypeEnum.Button:
                        {
                            this.gridView.Grid[this.ColumnIndex, this.RowIndex] = new DataGridViewButtonCell();
                            break;
                        }

                    case EditorTypeEnum.DropDown:
                        {
                            DataGridViewComboBoxCell combo = new DataGridViewComboBoxCell();
                            combo.FlatStyle = FlatStyle.Flat;
                            combo.ToolTipText = this.ToolTip;

                            // Normally you set a cell editor like this:
                            //    Grid[Col, Row] = Combo;
                            // But this doesn't work on MONO OSX. The two lines
                            // below seem to work ok though.
                            if (Environment.OSVersion.Platform == PlatformID.Win32NT ||
                                Environment.OSVersion.Platform == PlatformID.Win32Windows)
                            {
                                this.gridView.Grid[this.ColumnIndex, this.RowIndex] = combo;
                            }
                            else
                            {
                                this.gridView.Grid.Rows[this.RowIndex].Cells.RemoveAt(this.ColumnIndex);
                                this.gridView.Grid.Rows[this.RowIndex].Cells.Insert(this.ColumnIndex, combo);
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
                DataGridViewComboBoxCell combo = this.gridView.Grid[this.ColumnIndex, this.RowIndex] as DataGridViewComboBoxCell;
                if (combo != null)
                {
                    List<string> strings = new List<string>();
                    foreach (DataGridViewComboBoxCell comboItem in combo.Items)
                    {
                        strings.Add(comboItem.ToString());
                    }
                }

                return null;
            }

            set
            {
                DataGridViewComboBoxCell combo = this.gridView.Grid[this.ColumnIndex, this.RowIndex] as DataGridViewComboBoxCell;

                if (combo != null)
                {
                    combo.Items.Clear();
                    foreach (string st in value)
                    {
                        if (st != null)
                        {
                            combo.Items.Add(st);
                        }

                        if (this.Value != null)
                        {
                            combo.Value = this.Value.ToString();
                        }
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
                return this.gridView.Grid[this.ColumnIndex, this.RowIndex].ToolTipText;
            }

            set
            {
                this.gridView.Grid[this.ColumnIndex, this.RowIndex].ToolTipText = value;
                this.gridView.Grid.ShowCellToolTips = true;
            }
        }

        /// <summary>
        /// Gets or sets the cell value
        /// </summary>
        public object Value
        {
            get
            {
                return this.gridView.Grid[this.ColumnIndex, this.RowIndex].Value;
            }

            set
            {
                this.gridView.Grid[this.ColumnIndex, this.RowIndex].Value = value;
            }
        }
    }
}
