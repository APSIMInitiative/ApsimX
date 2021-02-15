namespace UserInterface.Classes
{
    using global::UserInterface.Extensions;
    using Gtk;
    using Interfaces;
    using System;
    using System.Drawing;
    using Views;

#if NETCOREAPP
    using StateType = Gtk.StateFlags;
#endif

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
            ColumnIndex = columnIndex;
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
                if (this.gridView.Grid.Columns[this.ColumnIndex].Sizing == Gtk.TreeViewColumnSizing.Autosize)
                    return -1;
                else
                    return this.gridView.Grid.Columns[this.ColumnIndex].Width;
            }

            set
            {
                if (value <= 0)
                {
                    this.gridView.Grid.Columns[this.ColumnIndex].Sizing = Gtk.TreeViewColumnSizing.Autosize;
                    this.gridView.Grid.Columns[this.ColumnIndex].Resizable = true;
                }
                else
                {
                    this.gridView.Grid.Columns[this.ColumnIndex].Sizing = Gtk.TreeViewColumnSizing.Fixed;
                    this.gridView.Grid.Columns[this.ColumnIndex].FixedWidth = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum column width in pixels.
        /// </summary>
        public int MinimumWidth
        {
            get
            {
                return gridView.Grid.Columns[this.ColumnIndex].MinWidth;
            }
            set
            {
                gridView.Grid.Columns[this.ColumnIndex].MinWidth = value;
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether the column is left aligned. If not then left is assumed.
        /// </summary>
        public bool LeftJustification
        {
            get
            {
                CellRendererText render = this.gridView.Grid.Columns[this.ColumnIndex].Cells[0] as CellRendererText;
                if (render != null)
                    return render.Alignment == Pango.Alignment.Left;
                else
                    return true;
            }

            set
            {
                CellRendererText render = this.gridView.Grid.Columns[this.ColumnIndex].Cells[0] as CellRendererText;
                float valueAsFloat = value ? 0.5f : 0.95f;
                if (render != null)
                {
                    render.Alignment = value ? Pango.Alignment.Left : Pango.Alignment.Right;
                    render.Xalign = valueAsFloat;
                }
                this.gridView.Grid.Columns[this.ColumnIndex].Alignment = valueAsFloat;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the column is read only.
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                return this.gridView.ColIsReadonly(this.ColumnIndex);
            }

            set
            {
                this.gridView.SetColAsReadonly(this.ColumnIndex, value);
            }
        }

        /// <summary>
        /// Gets or sets the column format e.g. N3
        /// </summary>
        public string Format
        {
            get
            {
                // TBI this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.Format;
                return "";
            }

            set
            {
                // TBI this.gridView.Grid.Columns[this.ColumnIndex].DefaultCellStyle.Format = value;
            }
        }

        /// <summary>
        /// Gets or sets the background color
        /// </summary>
        public Color BackgroundColour
        {
            get
            {
                Gdk.Color bg = this.gridView.ColBackgroundColor(this.ColumnIndex);
                return Color.FromArgb(bg.Red, bg.Green, bg.Blue);
            }

            set
            {
                if (value != Color.Empty)
                {
                    Gdk.Color colour = new Gdk.Color(value.R, value.G, value.B);
                    this.gridView.SetColBackgroundColor(ColumnIndex, colour);
                }
            }
        }

        /// <summary>
        /// Gets or sets the foreground color
        /// </summary>
        public Color ForegroundColour
        {
            get
            {
                Gdk.Color bg = this.gridView.ColForegroundColor(this.ColumnIndex);
                return Color.FromArgb(bg.Red, bg.Green, bg.Blue);
            }

            set
            {
                if (value != Color.Empty)
                {
                    Gdk.Color colour = new Gdk.Color(value.R, value.G, value.B);
                    this.gridView.SetColForegroundColor(ColumnIndex, colour);
                }
            }
        }

        /// <summary>
        /// Gets or sets the column tool tip.
        /// </summary>
        public string ToolTip
        {
            get
            {
                Label label = gridView.GetColumnHeaderLabel(this.ColumnIndex);
                if (label != null)
                {
                    return label.TooltipText;
                }
                return null;
            }

            set
            {
                Label label = gridView.GetColumnHeaderLabel(this.ColumnIndex);
                if (label != null)
                    label.TooltipText = value;
            }
        }

        /// <summary>
        /// Gets or sets the header background color
        /// </summary>
        public System.Drawing.Color HeaderBackgroundColour
        {
            get
            {
                Button button = gridView.GetColumnHeaderButton(this.ColumnIndex);
                if (button != null)
                {
                    Gdk.Color bg = button.GetBackgroundColour(StateType.Normal);

                    return Color.FromArgb(bg.Red, bg.Green, bg.Blue);
                }
                else
                    return System.Drawing.Color.White;
            }

            set
            {
                // I'm not sure why, but this DOES NOT WORK. It appears that buttons, like labels, don't
                // really draw their own background.
                Button button = gridView.GetColumnHeaderButton(this.ColumnIndex);
                if (button != null)
                {
#if NETFRAMEWORK
                    button.ModifyBg(StateType.Normal, new Gdk.Color(value.R, value.G, value.B));
#else
                    throw new NotImplementedException("tbi - gtk3 equivalent");
#endif
                }
            }
        }

        /// <summary>
        /// Gets or sets the header foreground color
        /// </summary>
        public System.Drawing.Color HeaderForegroundColour
        {
            get
            {
                Label label = gridView.GetColumnHeaderLabel(this.ColumnIndex);
                if (label != null)
                {
                    Gdk.Color fg = label.GetForegroundColour(StateType.Normal);
                    return Color.FromArgb(fg.Red, fg.Green, fg.Blue);
                }
                else
                   return System.Drawing.Color.Black;
            }

            set
            {
                Label label = gridView.GetColumnHeaderLabel(this.ColumnIndex);
                if (label != null)
                {
#if NETFRAMEWORK
                    label.ModifyFg(StateType.Normal, new Gdk.Color(value.R, value.G, value.B));
#else
                    throw new NotImplementedException("tbi - gtk3 equivalent");
#endif
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the text of the header
        /// </summary>
        public string HeaderText
        {
            get
            {
                return this.gridView.Grid.Columns[this.ColumnIndex].Title;
            }

            set
            {
                this.gridView.Grid.Columns[this.ColumnIndex].Title = value;
            }
        }

        /// <summary>Gets or sets the left justification of the header</summary>
        public bool HeaderLeftJustification
        {
            get
            {
                return gridView.GetColumnHeaderLabel(this.ColumnIndex).Justify == Justification.Left;
            }

            set
            {
                var label = gridView.GetColumnHeaderLabel(this.ColumnIndex);
                if (value)
                    label.Justify = Justification.Left;
                else
                    label.Justify = Justification.Right;
            }
        }
    }
}
