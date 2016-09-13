// -----------------------------------------------------------------------
// <copyright file="ForestryView.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Drawing;
    using System.Data;
    using Gtk;
    using Glade;
    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.GtkSharp;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public class TreeProxyView : ViewBase
    {
        private OxyPlot.GtkSharp.PlotView pBelowGround;
        private OxyPlot.GtkSharp.PlotView pAboveGround;
        private GridView gridView1;

        /// <summary>
        /// A list to hold all plots to make enumeration easier.
        /// </summary>
        private List<PlotView> plots = new List<PlotView>();

        /// <summary>
        /// Overall font size for the graph.
        /// </summary>
        private const double FontSize = 14;

        /// <summary>
        /// A table to hold tree data which is bound to the grid.
        /// </summary>
        private DataTable table;

        /// <summary>
        /// Overall font to use.
        /// </summary>
        private const string Font = "Calibri Light";

        /// <summary>
        /// Margin to use
        /// </summary>
        private const int TopMargin = 75;

        /// <summary>The smallest date used on any axis.</summary>
        private DateTime smallestDate = DateTime.MaxValue;

        /// <summary>The largest date used on any axis</summary>
        private DateTime largestDate = DateTime.MinValue;

        /// <summary>Current grid cell.</summary>
        private int[] currentCell = new int[2] { -1, -1 };

        /// <summary>
        /// Depth midpoints of the soil layers
        /// </summary>
        public double[] SoilMidpoints;

        [Widget]
        VPaned vpaned1 = null;

        [Widget]
        Alignment alignment1 = null;

        [Widget]
        HBox hbox1 = null;

        [Widget]
        TreeView treeview1 = null;

        [Widget]
        TreeView treeview2 = null;

        private ListStore heightModel = new ListStore(typeof(string));
        private ListStore gridModel = new ListStore(typeof(string));

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeProxyView" /> class.
        /// </summary>
        public TreeProxyView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.TreeProxyView.glade", "vpaned1");
            gxml.Autoconnect(this);
            _mainWidget = vpaned1;
            this.pBelowGround = new OxyPlot.GtkSharp.PlotView();
            this.pAboveGround = new OxyPlot.GtkSharp.PlotView();
            this.pAboveGround.Model = new PlotModel();
            this.pBelowGround.Model = new PlotModel();
            plots.Add(pAboveGround);
            plots.Add(pBelowGround);
            pAboveGround.SetSizeRequest(-1, 100);
            hbox1.PackStart(pAboveGround, true, true, 0);
            pBelowGround.SetSizeRequest(-1, 100);
            hbox1.PackStart(pBelowGround, true, true, 0);

            smallestDate = DateTime.MaxValue;
            largestDate = DateTime.MinValue;
            this.LeftRightPadding = 40;
            this.gridView1 = new Views.GridView(this);
            alignment1.Add(this.gridView1.MainWidget);
            smallestDate = DateTime.MaxValue;
            largestDate = DateTime.MinValue;
            treeview2.CursorChanged += GridCursorChanged;
            MainWidget.ShowAll();
        }

        /// <summary>
        /// Constants grid.
        /// </summary>
        public GridView ConstantsGrid { get { return gridView1; } }

        /// <summary>
        /// Update the graph data sources; this causes the axes minima and maxima to be calculated
        /// </summary>
        public void UpdateView()
        {
            foreach (PlotView plotView in plots)
            {
                IPlotModel theModel = plotView.Model as IPlotModel;
                if (theModel != null)
                    theModel.Update(true);
            }
        }

        /// <summary>
        /// Stub method for interface. This method is not used as the plots are not editable.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="xAxisType"></param>
        /// <param name="yAxisType"></param>
        /// <param name="colour"></param>
        /// <param name="lineType"></param>
        /// <param name="markerType"></param>
        /// <param name="lineThickness">The line thickness</param>
        /// <param name="markerSize">The size of the marker</param>
        /// <param name="showInLegend">Show in legend?</param>
        /// <param name="showOnLegend"></param>
        public void DrawLineAndMarkers(
     string title,
     IEnumerable x,
     IEnumerable y,
     Models.Graph.Axis.AxisType xAxisType,
     Models.Graph.Axis.AxisType yAxisType,
     Color colour,
     Models.Graph.LineType lineType,
     Models.Graph.MarkerType markerType,
     Models.Graph.LineThicknessType lineThickness,
     Models.Graph.MarkerSizeType markerSize,
     bool showOnLegend)
        {
        }

        /// <summary>
        /// Stub method for interface. This method is not used as the plots are not editable.
        /// </summary>
        /// <param name="text">The text for the footer</param>
        /// <param name="italics">Italics?</param>
        public void FormatCaption(string text, bool italics)
        {
        }

        /// <summary>
        /// Export the graph to the specified 'bitmap'
        /// </summary>
        /// <param name="bitmap">Bitmap to write to</param>
        /// <param name="legendOutside">Put legend outside of graph?</param>
        public void Export(ref Bitmap bitmap, bool legendOutside)
        {
            //TODO: This will only save the last bitmap. Might need to change the interface.
            foreach (PlotView plot in plots)
            {
                ///DockStyle saveStyle = plot.Dock;
                ///plot.Dock = DockStyle.None;
                ///plot.Width = bitmap.Width;
                ///plot.Height = bitmap.Height;

                LegendPosition savedLegendPosition = LegendPosition.RightTop;
                if (legendOutside)
                {
                    savedLegendPosition = plot.Model.LegendPosition;
                    plot.Model.LegendPlacement = LegendPlacement.Outside;
                    plot.Model.LegendPosition = LegendPosition.RightTop;
                }

                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                PngExporter pngExporter = new PngExporter();
                pngExporter.Width = bitmap.Width;
                pngExporter.Height = bitmap.Height;
                pngExporter.Export(plot.Model, stream);
                bitmap = new Bitmap(stream);

                if (legendOutside)
                {
                    plot.Model.LegendPlacement = LegendPlacement.Inside;
                    plot.Model.LegendPosition = savedLegendPosition;
                }

                // plot.Dock = saveStyle;
            }
        } 

        public void ExportToClipboard()
        {
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            PngExporter pngExporter = new PngExporter();
            pngExporter.Width = 800;
            pngExporter.Height = 600;
            pngExporter.Export(plots[0].Model, stream);
            Clipboard cb = MainWidget.GetClipboard(Gdk.Selection.Clipboard);
            cb.Image = new Gdk.Pixbuf(stream);
        }

        /// <summary>
        /// Invoked when the user finishes editing a cell.
        /// </summary>
        public event EventHandler OnCellEndEdit;

        /// <summary>
        /// Left margin in pixels.
        /// </summary>
        public int LeftRightPadding { get; set; }

        /// <summary>
        /// Clear the graphs of everything.
        /// </summary>
        public void Clear()
        {
            foreach (PlotView p in plots)
            {
                p.Model.Series.Clear();
                p.Model.Axes.Clear();
                p.Model.Annotations.Clear();
            }
        }

        /// <summary>
        /// Refresh the graph.
        /// </summary>
        public void Refresh()
        {
            foreach (PlotView p in plots)
            {
                p.Model.DefaultFont = Font;
                p.Model.DefaultFontSize = FontSize;

                p.Model.PlotAreaBorderThickness = new OxyThickness(0.0);
                p.Model.LegendBorder = OxyColors.Transparent;
                p.Model.LegendBackground = OxyColors.White;
                p.Model.InvalidatePlot(true);

                if (this.LeftRightPadding != 0)
                    p.Model.Padding = new OxyThickness(this.LeftRightPadding, 0, this.LeftRightPadding, 0);

                foreach (OxyPlot.Axes.Axis axis in p.Model.Axes)
                {
                    this.FormatAxisTickLabels(axis);
                }

                p.Model.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Draw a bar series with the specified arguments.
        /// </summary>
        /// <param name="title">The series title</param>
        /// <param name="x">The x values for the series</param>
        /// <param name="y">The y values for the series</param>
        /// <param name="xAxisType">The axis type the x values are related to</param>
        /// <param name="yAxisType">The axis type the y values are related to</param>
        /// <param name="colour">The series color</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawBar(
            string title,
            IEnumerable x,
            IEnumerable y,
            Models.Graph.Axis.AxisType xAxisType,
            Models.Graph.Axis.AxisType yAxisType,
            Color colour,
            bool showOnLegend)
        {
        }

        /// <summary>
        /// Draw an  area series with the specified arguments. A filled polygon is
        /// drawn with the x1, y1, x2, y2 coordinates.
        /// </summary>
        /// <param name="title">The series title</param>
        /// <param name="x1">The x1 values for the series</param>
        /// <param name="y1">The y1 values for the series</param>
        /// <param name="x2">The x2 values for the series</param>
        /// <param name="y2">The y2 values for the series</param>
        /// <param name="xAxisType">The axis type the x values are related to</param>
        /// <param name="yAxisType">The axis type the y values are related to</param>
        /// <param name="colour">The series color</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawArea(
            string title,
            IEnumerable x1,
            IEnumerable y1,
            IEnumerable x2,
            IEnumerable y2,
            Models.Graph.Axis.AxisType xAxisType,
            Models.Graph.Axis.AxisType yAxisType,
            Color colour,
            bool showOnLegend)
        {
        }

        /// <summary>
        /// Draw text on the graph at the specified coordinates.
        /// </summary>
        /// <param name="text">The text to put on the graph</param>
        /// <param name="x">The x position in graph coordinates</param>
        /// <param name="y">The y position in graph coordinates</param>
        /// <param name="leftAlign">Left align the text?</param>
        /// <param name="textRotation">Text rotation angle</param>
        /// <param name="xAxisType">The axis type the x value relates to</param>
        /// <param name="yAxisType">The axis type the y value are relates to</param>
        /// <param name="colour">The color of the text</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawText(
            string text,
            object x,
            object y,
            bool leftAlign,
            double textRotation,
            Models.Graph.Axis.AxisType xAxisType,
            Models.Graph.Axis.AxisType yAxisType,
            Color colour)
        {
        }

        /// <summary>
        /// Draw line on the graph at the specified coordinates.
        /// </summary>
        /// <param name="x1">The x1 position in graph coordinates</param>
        /// <param name="y1">The y1 position in graph coordinates</param>
        /// <param name="x2">The x2 position in graph coordinates</param>
        /// <param name="y2">The y2 position in graph coordinates</param>
        /// <param name="type">Line type</param>
        /// <param name="textRotation">Text rotation</param>
        /// <param name="thickness">Line thickness</param>
        /// <param name="colour">The color of the text</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawLine(
            object x1,
            object y1,
            object x2,
            object y2,
            Models.Graph.LineType type,
            Models.Graph.LineThicknessType thickness,
            Color colour)
        {
        }

        /// <summary>
        /// Format the specified axis.
        /// </summary>
        /// <param name="axisType">The axis type to format</param>
        /// <param name="title">The axis title. If null then a default axis title will be shown</param>
        /// <param name="inverted">Invert the axis?</param>
        /// <param name="minimum">Minimum axis scale</param>
        /// <param name="maximum">Maximum axis scale</param>
        /// <param name="interval">Axis scale interval</param>
        public void FormatAxis(
            Models.Graph.Axis.AxisType axisType,
            string title,
            bool inverted,
            double minimum,
            double maximum,
            double interval)
        {
            OxyPlot.Axes.Axis oxyAxis = this.GetAxis(axisType);
            if (oxyAxis != null)
            {
                oxyAxis.Title = title;
                oxyAxis.MinorTickSize = 0;
                oxyAxis.AxislineStyle = LineStyle.Solid;
                oxyAxis.AxisTitleDistance = 10;
                if (inverted)
                {
                    oxyAxis.StartPosition = 1;
                    oxyAxis.EndPosition = 0;
                }
                else
                {
                    oxyAxis.StartPosition = 0;
                    oxyAxis.EndPosition = 1;
                }
                if (!double.IsNaN(minimum))
                    oxyAxis.Minimum = minimum;
                if (!double.IsNaN(maximum))
                    oxyAxis.Maximum = maximum;
                if (!double.IsNaN(interval) && interval > 0)
                    oxyAxis.MajorStep = interval;
            }
        }

        /// <summary>
        /// Format the legend.
        /// </summary>
        /// <param name="legendPositionType">Position of the legend</param>
        public void FormatLegend(Models.Graph.Graph.LegendPositionType legendPositionType)
        {
            LegendPosition oxyLegendPosition;
            if (Enum.TryParse<LegendPosition>(legendPositionType.ToString(), out oxyLegendPosition))
            {
                foreach (PlotView p in plots)
                {
                    p.Model.LegendFont = Font;
                    p.Model.LegendFontSize = FontSize;
                    p.Model.LegendPosition = oxyLegendPosition;
                    p.Model.LegendSymbolLength = 30;
                }
            }
        }

        /// <summary>
        /// Format the title.
        /// </summary>
        /// <param name="text">Text of the title</param>
        public void FormatTitle(string text)
        {
        }

        /// <summary>
        /// Format the footer.
        /// </summary>
        /// <param name="text">The text for the footer</param>
        public void FormatCaption(string text)
        {
        }

        /// <summary>
        /// Show the specified editor.
        /// </summary>
        /// <param name="editor">The editor to show</param>
        public void ShowEditorPanel(object editorObj, string label)
        {
        }

        /// <summary>
        /// Export the graph to the specified 'bitmap'
        /// </summary>
        /// <param name="bitmap">Bitmap to write to</param>
        public void Export(Bitmap bitmap)
        {
            /* TBI
            int i = 0;
            foreach (PlotView p in plots)
            {
                p.Dock = DockStyle.None;
                p.Width = bitmap.Width;
                p.Height = bitmap.Height / 2;
                p.DrawToBitmap(bitmap, new Rectangle(0, p.Height * i, bitmap.Width, bitmap.Height / 2));
                p.Dock = DockStyle.Fill;
                i++;
            }
            */
        }

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        /// <param name="menuText">Menu item text</param>
        /// <param name="ticked">Menu ticked?</param>
        /// <param name="onClick">Event handler for menu item click</param>
        public void AddContextAction(string menuText, bool ticked, System.EventHandler onClick)
        {
        }

        /// <summary>
        /// Gets the interval (major step) of the specified axis.
        /// </summary>
        public double AxisMajorStep(Models.Graph.Axis.AxisType axisType)
        {
            OxyPlot.Axes.Axis axis = GetAxis(axisType);

            if (axis != null)
            {
                return axis.IntervalLength;
            }
            else
                return double.NaN;
        }

        /// <summary>
        /// Event handler for when user clicks close
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCloseEditorPanel(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Format axis tick labels so that there is a leading zero on the tick
        /// labels when necessary.
        /// </summary>
        /// <param name="axis">The axis to format</param>
        private void FormatAxisTickLabels(OxyPlot.Axes.Axis axis)
        {
            axis.IntervalLength = 100;

            if (axis is DateTimeAxis)
            {
                DateTimeAxis dateAxis = axis as DateTimeAxis;

                int numDays = (largestDate - smallestDate).Days;
                if (numDays < 100)
                    dateAxis.IntervalType = DateTimeIntervalType.Days;
                else if (numDays <= 366)
                {
                    dateAxis.IntervalType = DateTimeIntervalType.Months;
                    dateAxis.StringFormat = "dd-MMM";
                }
                else
                    dateAxis.IntervalType = DateTimeIntervalType.Years;
            }

            if (axis is LinearAxis &&
                (axis.ActualStringFormat == null || !axis.ActualStringFormat.Contains("yyyy")))
            {
                // We want the axis labels to always have a leading 0 when displaying decimal places.
                // e.g. we want 0.5 rather than .5

                // Use the current culture to format the string.
                string st = axis.ActualMajorStep.ToString(System.Globalization.CultureInfo.InvariantCulture);

                // count the number of decimal places in the above string.
                int pos = st.IndexOfAny(".,".ToCharArray());
                if (pos != -1)
                {
                    int numDecimalPlaces = st.Length - pos - 1;
                    axis.StringFormat = "F" + numDecimalPlaces.ToString();
                }
            }
        }

        /// <summary>
        /// Populate the specified DataPointSeries with data from the data table.
        /// </summary>
        /// <param name="x">The x values</param>
        /// <param name="y">The y values</param>
        /// <param name="xAxisType">The x axis the data is associated with</param>
        /// <param name="yAxisType">The y axis the data is associated with</param>
        /// <returns>A list of 'DataPoint' objects ready to be plotted</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        private List<DataPoint> PopulateDataPointSeries(
            IEnumerable x,
            IEnumerable y,
            Models.Graph.Axis.AxisType xAxisType,
            Models.Graph.Axis.AxisType yAxisType)
        {
            return null;
        }

        /// <summary>
        /// Return an axis that has the specified AxisType. Returns null if not found.
        /// </summary>
        /// <param name="axisType">The axis type to retrieve </param>
        /// <returns>The axis</returns>
        private OxyPlot.Axes.Axis GetAxis(Models.Graph.Axis.AxisType axisType)
        {
            return null;
        }

        /// <summary>
        /// Gets the maximum scale of the specified axis.
        /// </summary>
        public double AxisMaximum(Models.Graph.Axis.AxisType axisType)
        {
            OxyPlot.Axes.Axis axis = GetAxis(axisType);
            if (axis != null)
            {
                return axis.ActualMaximum;
            }
            else
                return double.NaN;
        }

        /// <summary>
        /// Gets the minimum scale of the specified axis.
        /// </summary>
        public double AxisMinimum(Models.Graph.Axis.AxisType axisType)
        {
            foreach (PlotView p in plots)
                p.InvalidatePlot(true);
            OxyPlot.Axes.Axis axis = GetAxis(axisType);

            if (axis != null)
            {
                return axis.ActualMinimum;
            }
            else
                return double.NaN;
        }

        /// <summary>Gets the series names.</summary>
        /// <returns></returns>
        public string[] GetSeriesNames()
        {
            List<string> names = new List<string>();
            foreach (OxyPlot.Series.Series series in this.pAboveGround.Model.Series)
            {
                names.Add("AG" + series.Title);
            }
            foreach (OxyPlot.Series.Series series in this.pBelowGround.Model.Series)
            {
                names.Add("BG" + series.Title);
            }
            return names.ToArray();
        }

        public void SetupGrid(List<List<string>> data)
        {
            while (treeview2.Columns.Length > 0)
            {
                TreeViewColumn col = treeview2.GetColumn(0);
                foreach (CellRenderer render in col.CellRenderers)
                    if (render is CellRendererText)
                    {
                        CellRendererText textRender = render as CellRendererText;
                        textRender.Edited -= GridCellEdited;
                        col.SetCellDataFunc(textRender, (CellLayoutDataFunc)null);
                    }
                treeview2.RemoveColumn(treeview2.GetColumn(0));
            }

            int nCols = data[0].Count;
            Type[] colTypes = new Type[nCols];
            for (int i = 0; i < nCols; i++)
            {
                colTypes[i] = typeof(string);
                CellRendererText textRender = new Gtk.CellRendererText();

                textRender.Editable = i > 0;
                textRender.Edited += GridCellEdited;
                textRender.Xalign = i == 0 ? 0.0f : 1.0f; // For right alignment of text cell contents; left align the first column

                TreeViewColumn column = new TreeViewColumn(data[0][i], textRender, "text", i);
                column.Sizing = TreeViewColumnSizing.Autosize;

                column.Resizable = true;
                column.Alignment = 0.5f; // For centered alignment of the column header
                column.SetCellDataFunc(textRender, OnSetGridData);
                treeview2.AppendColumn(column);
            }

            // Add an empty column at the end; auto-sizing will give this any "leftover" space
            TreeViewColumn fillColumn = new TreeViewColumn();
            fillColumn.Sizing = TreeViewColumnSizing.Autosize;
            treeview2.AppendColumn(fillColumn);

            // Now let's add some padding to the column headers, to avoid having very narrow columns
            for (int i = 0; i < nCols; i++)
            {
                Label label = GetColumnHeaderLabel(i, treeview2);
                label.Justify = Justification.Center;
                label.SetPadding(10, 0);
            }

            gridModel = new ListStore(colTypes);

            for (int i = 0; i < data[1].Count; i++)
            {
                string[] row = new string[nCols];
                for (int j = 1; j <= nCols; j++)
                    row[j - 1] = data[j][i];

                gridModel.AppendValues(row);
            }
            treeview2.Model = gridModel;

            table = new DataTable();

            // data[0] holds the column names
            foreach (string s in data[0])
            {
                table.Columns.Add(new DataColumn(s, typeof(string)));
            }

            for (int i = 0; i < data[1].Count; i++)
            {
                string[] row = new string[table.Columns.Count];
                for (int j = 1; j < table.Columns.Count + 1; j++)
                {
                    row[j - 1] = data[j][i];
                }
                table.Rows.Add(row);
            }
            SetupGraphs();
        }

        private void GridCursorChanged(object sender, EventArgs e)
        {
            TreeSelection selection = treeview2.Selection;
            TreeModel model;
            TreeIter iter;
            // The iter will point to the selected row
            if (selection.GetSelected(out model, out iter))
            {
                Gtk.TreePath path = gridModel.GetPath(iter);
                int row = path.Indices[0];
                bool editable = row < 1 || row > 2;
                for (int i = 1; i < treeview2.Columns.Count() - 1; i++)
                    (treeview2.Columns[i].CellRenderers[0] as CellRendererText).Editable = editable;
            }
        }

        private void GridCellEdited(object o, EditedArgs args)
        {
            Gtk.TreeIter iter;
            Gtk.TreePath path = new Gtk.TreePath(args.Path);
            gridModel.GetIter(out iter, path);
            int row = path.Indices[0];
            int col;
            for (col = 0; col < treeview2.Columns.Count(); col++)
                if (treeview2.Columns[col].CellRenderers[0] == o)
                    break;
            if (col == treeview2.Columns.Count())
                return;  // Could not locate the column!
            string value = args.NewText.Trim();
            if (value == gridModel.GetValue(iter, col) as string)
                return;
            double numval;
            if (Double.TryParse(args.NewText, out numval))
            {
                // It seems a bit silly to have two parallel data stores -
                // a Gtk.ListStore and a System.Data.Table. However, we want a
                // ListStore for the GUI, and don't want the Presenter to 
                // have to know about Gtk.
                    gridModel.SetValue(iter, col, value);
                table.Rows[row][col] = value;
                if (OnCellEndEdit != null)
                    OnCellEndEdit.Invoke(this, new EventArgs());
            }
        }

        public void OnSetGridData(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
        {
            TreePath path = model.GetPath(iter);
            int row = path.Indices[0];
            if (cell is CellRendererText)
            {
                if (row == 1 || row == 2)
                    (cell as CellRendererText).Background = "lightgray";
                else
                    (cell as CellRendererText).Background = "white";
            }
        }

        public void SetReadOnly()
        {
        }

        public void SetupHeights(DateTime[] dates, double[] heights, double[] NDemands, double[] CanopyWidths, double[] TreeLeafAreas)
        {
            while (treeview1.Columns.Length > 0)
            {
                TreeViewColumn col = treeview1.GetColumn(0);
                foreach (CellRenderer render in col.CellRenderers)
                    if (render is CellRendererText)
                    {
                        CellRendererText textRender = render as CellRendererText;
                        textRender.Edited -= HeightCellEdited;
                    }
                treeview1.RemoveColumn(treeview1.GetColumn(0));
            }
            string[] colLabels = new string[] { "Date", "Height (m)", "N Demands (g/m2)", "Canopy Width (m)", "Tree Leaf Area (m2)" };
            // Begin by creating a new ListStore with the appropriate number of
            // columns. Use the string column type for everything.
            Type[] colTypes = new Type[5];
            for (int i = 0; i < 5; i++)
            {
                colTypes[i] = typeof(string);
                CellRendererText textRender = new Gtk.CellRendererText();

                textRender.Editable = true;
                textRender.Edited += HeightCellEdited;
                textRender.Xalign = i == 0 ? 0.0f : 1.0f; // For right alignment of text cell contents; left align the first column

                TreeViewColumn column = new TreeViewColumn(colLabels[i], textRender, "text", i);
                column.Sizing = TreeViewColumnSizing.Autosize;
                column.Resizable = true;
                column.Alignment = 0.5f; // For centered alignment of the column header
                treeview1.AppendColumn(column);
            }
            // Add an empty column at the end; auto-sizing will give this any "leftover" space
            TreeViewColumn fillColumn = new TreeViewColumn();
            fillColumn.Sizing = TreeViewColumnSizing.Autosize;
            treeview1.AppendColumn(fillColumn);

            heightModel = new ListStore(colTypes);

            for (int i = 0; i < dates.Count(); i++)
            {
                heightModel.AppendValues(dates[i].ToShortDateString(), (heights[i] / 1000).ToString(), NDemands[i].ToString(), CanopyWidths[i].ToString(), TreeLeafAreas[i].ToString());
            }
            // Add an empty row to allow for adding new values
            heightModel.Append();
            treeview1.Model = heightModel;
        }

        private void HeightCellEdited(object o, EditedArgs args)
        {
            Gtk.TreeIter iter;
            Gtk.TreePath path = new Gtk.TreePath(args.Path);
            heightModel.GetIter(out iter, path);
            int row = path.Indices[0];
            int col;
            for (col = 0; col < treeview1.Columns.Count(); col++)
                if (treeview1.Columns[col].CellRenderers[0] == o)
                    break;
            if (col == treeview1.Columns.Count())
                return;  // Could not locate the column!
            string value = args.NewText.Trim();
            if (value == (string)heightModel.GetValue(iter, col))
                return;
            if (value == String.Empty)
                heightModel.SetValue(iter, col, value);
            else if (col == 0)
            {
                DateTime dateval;
                if (DateTime.TryParse(args.NewText, out dateval))
                    heightModel.SetValue(iter, col, value);
            }
            else
            {
                double numval;
                if (Double.TryParse(args.NewText, out numval))
                    heightModel.SetValue(iter, col, value);
            }
            if (!String.IsNullOrEmpty(value) && row == heightModel.IterNChildren() - 1)  // Entry on the last row? Add a new blank one
                heightModel.Append();
        }

        private void SetupGraphs()
        {
            double[] x = { 0, 0.5, 1, 1.5, 2, 2.5, 3, 4, 5, 6 };
            try
            {
                pAboveGround.Model.Axes.Clear();
                pAboveGround.Model.Series.Clear();
                pBelowGround.Model.Axes.Clear();
                pBelowGround.Model.Series.Clear();
                pAboveGround.Model.Title = "Above Ground";
                pAboveGround.Model.PlotAreaBorderColor = OxyColors.White;
                pAboveGround.Model.LegendBorder = OxyColors.Transparent;
                LinearAxis agxAxis = new LinearAxis();
                agxAxis.Title = "Multiple of Tree Height";
                agxAxis.AxislineStyle = LineStyle.Solid;
                agxAxis.AxisDistance = 2;
                agxAxis.Position = AxisPosition.Top;

                LinearAxis agyAxis = new LinearAxis();
                agyAxis.Title = "%";
                agyAxis.AxislineStyle = LineStyle.Solid;
                agyAxis.AxisDistance = 2;
                Utility.LineSeriesWithTracker seriesShade = new Utility.LineSeriesWithTracker();
                List<DataPoint> pointsShade = new List<DataPoint>();
                DataRow rowShade = table.Rows[0];
                DataColumn col = table.Columns[0];
                double[] yShade = new double[table.Columns.Count - 1];

                pAboveGround.Model.Axes.Add(agxAxis);
                pAboveGround.Model.Axes.Add(agyAxis);

                for (int i = 1; i < table.Columns.Count; i++)
                {
                    if (rowShade[i].ToString() == "")
                        return;
                    yShade[i - 1] = Convert.ToDouble(rowShade[i]);
                }

                for (int i = 0; i < x.Length; i++)
                {
                    pointsShade.Add(new DataPoint(x[i], yShade[i]));
                }
                seriesShade.Title = "Shade";
                seriesShade.ItemsSource = pointsShade;
                pAboveGround.Model.Series.Add(seriesShade);
            }
            //don't draw the series if the format is wrong
            catch (FormatException)
            {
                pBelowGround.Model.Series.Clear();
            }

            /////////////// Below Ground
            try
            {
                pBelowGround.Model.Title = "Below Ground";
                pBelowGround.Model.PlotAreaBorderColor = OxyColors.White;
                pBelowGround.Model.LegendBorder = OxyColors.Transparent;
                LinearAxis bgxAxis = new LinearAxis();
                LinearAxis bgyAxis = new LinearAxis();
                List<Utility.LineSeriesWithTracker> seriesList = new List<Utility.LineSeriesWithTracker>();

                bgyAxis.Position = AxisPosition.Left;
                bgxAxis.Position = AxisPosition.Top;
                bgyAxis.Title = "Depth (mm)";

                bgxAxis.Title = "Root Length Density (cm/cm3)";
                bgxAxis.Minimum = 0;
                bgxAxis.MinorTickSize = 0;
                bgxAxis.AxislineStyle = LineStyle.Solid;
                bgxAxis.AxisDistance = 2;
                pBelowGround.Model.Axes.Add(bgxAxis);

                bgyAxis.StartPosition = 1;
                bgyAxis.EndPosition = 0;
                bgyAxis.MinorTickSize = 0;
                bgyAxis.AxislineStyle = LineStyle.Solid;
                pBelowGround.Model.Axes.Add(bgyAxis);

                for (int i = 1; i < table.Columns.Count; i++)
                {
                    Utility.LineSeriesWithTracker series = new Utility.LineSeriesWithTracker();
                    series.Title = table.Columns[i].ColumnName;
                    double[] data = new double[table.Rows.Count - 4];
                    for (int j = 4; j < table.Rows.Count; j++)
                    {
                        data[j - 4] = Convert.ToDouble(table.Rows[j].Field<string>(i));
                    }

                    List<DataPoint> points = new List<DataPoint>();

                    for (int j = 0; j < data.Length; j++)
                    {
                        points.Add(new DataPoint(data[j], SoilMidpoints[j]));
                    }
                    series.ItemsSource = points;
                    pBelowGround.Model.Series.Add(series);
                }
            }
            //don't draw the series if the format is wrong
            catch (FormatException)
            {
                pBelowGround.Model.Series.Clear();
            }
            finally
            {
                pAboveGround.InvalidatePlot(true);
                pBelowGround.InvalidatePlot(true);
            }
        }


        public DataTable GetTable()
        {
            return table;
        }

        public DateTime[] SaveDates()
        {
            List<DateTime> dates = new List<DateTime>();
            foreach (object[] row in heightModel)
            {
                if (!String.IsNullOrEmpty((string)row[0]))
                   dates.Add(DateTime.Parse((string)row[0]));
            }
            return dates.ToArray();
        }

        public double[] SaveHeights()
        {
            List<double> heights = new List<double>();
            foreach (object[] row in heightModel)
            {
                if (!String.IsNullOrEmpty((string)row[1]))
                    heights.Add(Convert.ToDouble((string)row[1]) * 1000.0);
            }
            return heights.ToArray();
        }

        public double[] SaveNDemands()
        {
            List<double> NDemands = new List<double>();
            foreach (object[] row in heightModel)
            {
                if (!String.IsNullOrEmpty((string)row[2]))
                    NDemands.Add(Convert.ToDouble((string)row[2]));
            }
            return NDemands.ToArray();
        }

        public double[] SaveCanopyWidths()
        {
            List<double> CanopyWidths = new List<double>();
            foreach (object[] row in heightModel)
            {
                if (!String.IsNullOrEmpty((string)row[3]))
                    CanopyWidths.Add(Convert.ToDouble((string)row[3]));
            }
            return CanopyWidths.ToArray();
        }

        public double[] SaveTreeLeafAreas()
        {
            List<double> TreeLeafAreas = new List<double>();
            foreach (object[] row in heightModel)
            {
                if (!String.IsNullOrEmpty((string)row[4]))
                    TreeLeafAreas.Add(Convert.ToDouble((string)row[4]));
            }
            return TreeLeafAreas.ToArray();
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (currentCell[0] != -1)
            {
                /// TBI Grid.CurrentCell = Grid[currentCell[0], currentCell[1]];
                currentCell = new int[2] { -1, -1 };
            }
        }

        public Label GetColumnHeaderLabel(int colNo, TreeView view)
        {
            int i = 0;
            foreach (Widget widget in view.AllChildren)
            {
                if (widget.GetType() != (typeof(Gtk.Button)))
                    continue;
                else if (i++ == colNo)
                {
                    foreach (Widget child in ((Gtk.Button)widget).AllChildren)
                    {
                        if (child.GetType() != (typeof(Gtk.HBox)))
                            continue;
                        foreach (Widget grandChild in ((Gtk.HBox)child).AllChildren)
                        {
                            if (grandChild.GetType() != (typeof(Gtk.Alignment)))
                                continue;
                            foreach (Widget greatGrandChild in ((Gtk.Alignment)grandChild).AllChildren)
                            {
                                if (greatGrandChild.GetType() != (typeof(Gtk.Label)))
                                    continue;
                                else
                                    return greatGrandChild as Label;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private void Grid_KeyUp(object sender, /* TBI Key */EventArgs e)
        {
            /* TBI
            //TODO: Get this working with data copied from other cells in the grid.
            //      Also needs to work with blank cells and block deletes.
            //source: https://social.msdn.microsoft.com/Forums/windows/en-US/e9cee429-5f36-4073-85b4-d16c1708ee1e/how-to-paste-ctrlv-shiftins-the-data-from-clipboard-to-datagridview-datagridview1-c?forum=winforms
            DataGridView grid = sender as DataGridView;
            if ((e.Shift && e.KeyCode == Keys.Insert) || (e.Control && e.KeyCode == Keys.V))
            {
                string[] rowSplitter = { Environment.NewLine };
                char[] columnSplitter = { '\t' };
                //get the text from clipboard
                IDataObject dataInClipboard = Clipboard.GetDataObject();
                string stringInClipboard = (string)dataInClipboard.GetData(DataFormats.Text);
                //split it into lines
                string[] rowsInClipboard = stringInClipboard.Split(rowSplitter, StringSplitOptions.None);
                //get the row and column of selected cell in Grid
                int r = grid.SelectedCells[0].RowIndex;
                int c = grid.SelectedCells[0].ColumnIndex;
                //add rows into Grid to fit clipboard lines
                if (grid.Rows.Count < (r + rowsInClipboard.Length))
                    grid.Rows.Add(r + rowsInClipboard.Length - grid.Rows.Count);
                // loop through the lines, split them into cells and place the values in the corresponding cell.
                for (int iRow = 0; iRow < rowsInClipboard.Length; iRow++)
                {
                    //split row into cell values
                    string[] valuesInRow = rowsInClipboard[iRow].Split(columnSplitter);
                    //cycle through cell values
                    for (int iCol = 0; iCol < valuesInRow.Length; iCol++)
                        //assign cell value, only if it within columns of the Grid
                        if (grid.ColumnCount - 1 >= c + iCol)
                            grid.Rows[r + iRow].Cells[c + iCol].Value = valuesInRow[iCol];
                }
            }

            if (e.KeyCode == Keys.Delete)
            {
                foreach ( DataGridViewCell cell in grid.SelectedCells)
                    cell.Value = string.Empty;
            }
            */
        }
    }
}