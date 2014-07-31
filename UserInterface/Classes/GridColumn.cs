// -----------------------------------------------------------------------
// <copyright file="GridColumn.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Classes
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using Interfaces;
    using Views;

    /// <summary>
    /// Represents a grid column.
    /// </summary>
    public class GridColumn : IGridColumn
    {
        /// <summary>
        /// The parent grid that this column belongs to
        /// </summary>
        private GridView gridView;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridColumn" /> class.
        /// </summary>
        /// <param name="gridView">The grid that the column belongs to</param>
        /// <param name="columnIndex">The column index of the column</param>
        public GridColumn(GridView gridView, int columnIndex)
        {
            this.gridView = gridView;
            this.ColumnIndex = columnIndex;
        }

        /// <summary>
        /// Gets the column index of the column
        /// </summary>
        public int ColumnIndex { get; private set; }

        /// <summary>
        /// Gets or sets the column width in pixels. A value of -1 indicates auto sizing.
        /// </summary>
        public int Width
        {
            get
            {
                if (this.gridView.Grid.Columns[this.ColumnIndex].AutoSizeMode == DataGridViewAutoSizeColumnMode.None)
                {
                    return this.gridView.Grid.Columns[this.ColumnIndex].Width;
                }
                else
                {
                    return -1;
                }
            }

            set
            {
                if (value == -1)
                {
                    Graphics graphics = this.gridView.Grid.CreateGraphics();
                    Font font = this.gridView.Grid.Font;
                    int newWidth;

                    for (int j = 0; j < this.gridView.Grid.Columns.Count; j++)
                    {
                        // Limit number of rows to check to no more than 20
                        for (int i = 0; i <= Math.Min(20, this.gridView.Grid.Rows.Count - 1); i++)
                        {
                            // Add 40 pixels to cover the dropdown target.
                            if (this.gridView.Grid.Rows[i].Cells[j].Value != null)
                            {
                                newWidth = (int)graphics.MeasureString(this.gridView.Grid.Rows[i].Cells[j].Value.ToString(), font).Width + 40;
                                if (this.gridView.Grid.Columns[j].Width < newWidth)
                                {
                                    this.gridView.Grid.Columns[j].Width = newWidth;
                                }
                            }
                        }
                    }
                }
                else
                {
                    this.gridView.Grid.Columns[this.ColumnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    this.gridView.Grid.Columns[this.ColumnIndex].Width = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the column is left aligned. If not then right is assumed.
        /// </summary>
        public bool LeftAlignment
        {
            get
            {
                return this.gridView.Grid.Columns[this.ColumnIndex].HeaderCell.Style.Alignment == DataGridViewContentAlignment.MiddleLeft;
            }

            set
            {
                if (value)
                {
                    // Left align
                    this.gridView.Grid.Columns[this.ColumnIndex].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
                else
                {
                    // Right align
                    this.gridView.Grid.Columns[this.ColumnIndex].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the column is read only.
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                return this.gridView.Grid.Columns[this.ColumnIndex].ReadOnly;
            }

            set
            {
                this.gridView.Grid.Columns[this.ColumnIndex].ReadOnly = value;
                if (value)
                {
                    this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.ForeColor = Color.DarkMagenta;
                }
                else
                {
                    this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.ForeColor = SystemColors.WindowText;
                }
            }
        }

        /// <summary>
        /// Gets or sets the column format e.g. N3
        /// </summary>
        public string Format
        {
            get
            {
                return this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.Format;
            }

            set
            {
                this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.Format = value;
            }
        }

        /// <summary>
        /// Gets or sets the background color
        /// </summary>
        public System.Drawing.Color BackgroundColour
        {
            get
            {
                return this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.BackColor;
            }

            set
            {
                this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.BackColor = value;
            }
        }

        /// <summary>
        /// Gets or sets the foreground color
        /// </summary>
        public System.Drawing.Color ForegroundColour
        {
            get
            {
                return this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.ForeColor;
            }

            set
            {
                this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.ForeColor = value;
            }
        }

        /// <summary>
        /// Gets or sets the column tool tip.
        /// </summary>
        public string ToolTip
        {
            get
            {
                if (this.gridView.RowCount > 0)
                {
                    return this.gridView.Grid.Rows[0].Cells[this.ColumnIndex].ToolTipText;
                }

                return null;
            }

            set
            {
                for (int row = 0; row < this.gridView.Grid.RowCount; row++)
                {
                    if (row < value.Length)
                    {
                        this.gridView.Grid.Rows[row].Cells[this.ColumnIndex].ToolTipText = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the header background color
        /// </summary>
        public System.Drawing.Color HeaderBackgroundColour
        {
            get
            {
                return this.gridView.Grid.Columns[this.ColumnIndex].HeaderCell.Style.BackColor;
            }

            set
            {
                this.gridView.Grid.EnableHeadersVisualStyles = false;
                this.gridView.Grid.Columns[this.ColumnIndex].HeaderCell.Style.BackColor = value;
            }
        }

        /// <summary>
        /// Gets or sets the header foreground color
        /// </summary>
        public System.Drawing.Color HeaderForegroundColour
        {
            get
            {
                return this.gridView.Grid.Columns[this.ColumnIndex].HeaderCell.Style.ForeColor;
            }

            set
            {
                this.gridView.Grid.EnableHeadersVisualStyles = false;
                this.gridView.Grid.Columns[this.ColumnIndex].HeaderCell.Style.ForeColor = value;
            }
        }

        /// <summary>
        /// Gets or sets the text of the header
        /// </summary>
        public string HeaderText
        {
            get
            {
                return this.gridView.Grid.Columns[this.ColumnIndex].HeaderText;
            }

            set
            {
                this.gridView.Grid.Columns[this.ColumnIndex].HeaderText = value;
            }
        }
    }
}
