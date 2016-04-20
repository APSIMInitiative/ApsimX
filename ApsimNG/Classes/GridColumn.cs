// -----------------------------------------------------------------------
// <copyright file="GridColumn.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Classes
{
    using System;
    using System.Drawing;
    using Gtk;
    /// using System.Windows.Forms;
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
                if (this.gridView.gridview.Columns[this.ColumnIndex].Sizing == Gtk.TreeViewColumnSizing.Autosize)
                    return -1;
                else
                    return this.gridView.gridview.Columns[this.ColumnIndex].Width;
            }

            set
            {
                if (value == -1)
                {
                    this.gridView.gridview.Columns[this.ColumnIndex].Sizing = Gtk.TreeViewColumnSizing.Autosize;
                    this.gridView.gridview.Columns[this.ColumnIndex].Resizable = true;
                }
                else
                {
                    this.gridView.gridview.Columns[this.ColumnIndex].Sizing = Gtk.TreeViewColumnSizing.Fixed;
                    this.gridView.gridview.Columns[this.ColumnIndex].FixedWidth = value;
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
                CellRendererText render = this.gridView.gridview.Columns[this.ColumnIndex].Cells[0] as CellRendererText;
                if (render != null)
                    return render.Alignment == Pango.Alignment.Left;
                else
                    return false;
            }

            set
            {
                CellRendererText render = this.gridView.gridview.Columns[this.ColumnIndex].Cells[0] as CellRendererText;
                if (render != null)
                    render.Alignment = value ? Pango.Alignment.Left : Pango.Alignment.Right;
                this.gridView.gridview.Columns[this.ColumnIndex].Alignment = value ? 0.5F : 0.95F;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the column is read only.
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                CellRendererText render = this.gridView.gridview.Columns[this.ColumnIndex].Cells[0] as CellRendererText;
                if (render != null)
                    return render.Editable == false;
                else
                    return true;
            }

            set
            {
                CellRendererText render = this.gridView.gridview.Columns[this.ColumnIndex].Cells[0] as CellRendererText;
                if (render != null)
                {
                    render.Editable = !value;
                    if (value)
                        render.Foreground = "darkmagenta";
                    else
                        render.Foreground = "black"; // Probably not how this should be done..
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
                return ""; /// TBI this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.Format;
            }

            set
            {
                /// TBI this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.Format = value;
            }
        }

        /// <summary>
        /// Gets or sets the background color
        /// </summary>
        public System.Drawing.Color BackgroundColour
        {
            get
            {
                CellRendererText render = this.gridView.gridview.Columns[this.ColumnIndex].Cells[0] as CellRendererText;
                if (render != null)
                {
                    Gdk.Color bg = render.BackgroundGdk;
                    return Color.FromArgb(bg.Red, bg.Green, bg.Blue);
                }
                else
                    return System.Drawing.Color.White;
            }

            set
            {
                CellRendererText render = this.gridView.gridview.Columns[this.ColumnIndex].Cells[0] as CellRendererText;
                if (render != null)
                    render.BackgroundGdk = new Gdk.Color(value.R, value.G, value.B);
            }
        }

        /// <summary>
        /// Gets or sets the foreground color
        /// </summary>
        public System.Drawing.Color ForegroundColour
        {
            get
            {
                CellRendererText render = this.gridView.gridview.Columns[this.ColumnIndex].Cells[0] as CellRendererText;
                if (render != null)
                {
                    Gdk.Color fg = render.ForegroundGdk;
                    return Color.FromArgb(fg.Red, fg.Green, fg.Blue);
                }
                else
                    return System.Drawing.Color.Black;
            }

            set
            {
                CellRendererText render = this.gridView.gridview.Columns[this.ColumnIndex].Cells[0] as CellRendererText;
                if (render != null)
                    render.ForegroundGdk = new Gdk.Color(value.R, value.G, value.B);
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
                    return ""; /// TBI this.gridView.Grid.Rows[0].Cells[this.ColumnIndex].ToolTipText;
                }

                return null;
            }

            set
            {
                /* TBI
                for (int row = 0; row < this.gridView.Grid.RowCount; row++)
                {
                    if (row < value.Length)
                    {
                        this.gridView.Grid.Rows[row].Cells[this.ColumnIndex].ToolTipText = value;
                    }
                }
                */
            }
        }

        /// <summary>
        /// Gets or sets the header background color
        /// </summary>
        public System.Drawing.Color HeaderBackgroundColour
        {
            get
            {
                return System.Drawing.Color.White; /// TBI this.gridView.Grid.Columns[this.ColumnIndex].HeaderCell.Style.BackColor;
            }

            set
            {
                /* TBI
                this.gridView.Grid.EnableHeadersVisualStyles = false;
                this.gridView.Grid.Columns[this.ColumnIndex].HeaderCell.Style.BackColor = value;
                */
            }
        }

        /// <summary>
        /// Gets or sets the header foreground color
        /// </summary>
        public System.Drawing.Color HeaderForegroundColour
        {
            get
            {
                return System.Drawing.Color.Black; /// TBI this.gridView.Grid.Columns[this.ColumnIndex].HeaderCell.Style.ForeColor;
            }

            set
            {
                /* TBI
                this.gridView.Grid.EnableHeadersVisualStyles = false;
                this.gridView.Grid.Columns[this.ColumnIndex].HeaderCell.Style.ForeColor = value;
                */
            }
        }

        /// <summary>
        /// Gets or sets the text of the header
        /// </summary>
        public string HeaderText
        {
            get
            {
                return this.gridView.gridview.Columns[this.ColumnIndex].Title;
            }

            set
            {
                this.gridView.gridview.Columns[this.ColumnIndex].Title = value;
            }
        }
    }
}
