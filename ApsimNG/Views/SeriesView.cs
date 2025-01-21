namespace UserInterface.Views
{
    using Interfaces;
    using Gtk;
    using System;
    using Extensions;
    using APSIM.Shared.Utilities;

    /// <summary>This view allows a single series to be edited.</summary>
    public class SeriesView : ViewBase, ISeriesView
    {

        private Grid table1;

        private Box vbox1 = null;
        private Label label4 = null;
        private Label label5 = null;

        private DropDownView dataSourceDropDown;
        private DropDownView xDropDown;
        private DropDownView yDropDown;
        private DropDownView x2DropDown;
        private DropDownView y2DropDown;
        private DropDownView seriesDropDown;
        private DropDownView lineTypeDropDown;
        private DropDownView markerTypeDropDown;
        private ColourDropDownView colourDropDown;
        private DropDownView lineThicknessDropDown;
        private DropDownView markerSizeDropDown;
        private CheckBoxView checkBoxView1;
        private CheckBoxView checkBoxView2;
        private CheckBoxView checkBoxView3;
        private CheckBoxView checkBoxView4;
        private CheckBoxView checkBoxView5;
        private CheckBoxView checkBoxView6;
        private GraphView graphView1;
        private EditView editView1;
        private EventBox helpBox;

        /// <summary>Initializes a new instance of the <see cref="SeriesView" /> class</summary>
        public SeriesView(ViewBase owner) : base(owner)
        {
            // The glade file no longer provides much of use, with all the
            // layout work being done here in code. THe glade file could be
            // eliminated entirely
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.SeriesView.glade");
            vbox1 = (Box)builder.GetObject("vbox1");

            table1 = new Grid();
            table1.ColumnSpacing = 10;
            // Set expand to false on this grid, to ensure that any extra space
            // is allocated to the graph (aka plotview).
            vbox1.PackStart(table1, false, true, 0);
            vbox1.ReorderChild(table1, 0);

            mainWidget = vbox1;

            graphView1 = new GraphView(this);
            vbox1.PackStart(graphView1.MainWidget, true, true, 0);

            dataSourceDropDown = new DropDownView(this);
            xDropDown = new DropDownView(this);
            yDropDown = new DropDownView(this);
            x2DropDown = new DropDownView(this);
            y2DropDown = new DropDownView(this);
            seriesDropDown = new DropDownView(this);
            lineTypeDropDown = new DropDownView(this);
            markerTypeDropDown = new DropDownView(this);
            colourDropDown = new ColourDropDownView(this);
            lineThicknessDropDown = new DropDownView(this);
            markerSizeDropDown = new DropDownView(this);

            checkBoxView1 = new CheckBoxView(this);
            checkBoxView1.TextOfLabel = "on top?";
            checkBoxView2 = new CheckBoxView(this);
            checkBoxView2.TextOfLabel = "on right?";
            checkBoxView3 = new CheckBoxView(this);
            checkBoxView3.TextOfLabel = "cumulative?";
            checkBoxView4 = new CheckBoxView(this);
            checkBoxView4.TextOfLabel = "cumulative?";
            checkBoxView5 = new CheckBoxView(this);
            checkBoxView5.TextOfLabel = "Show in legend?";
            checkBoxView6 = new CheckBoxView(this);
            checkBoxView6.TextOfLabel = "Include series name in legend?";

            editView1 = new EditView(this);

            Image helpImage = new Image(null, "ApsimNG.Resources.MenuImages.Help.svg");
            helpBox = new EventBox();
            helpBox.Add(helpImage);
            helpBox.ButtonReleaseEvent += Help_ButtonPressEvent;
            Box filterBox = new Box(Orientation.Horizontal, 0);
            filterBox.PackStart(editView1.MainWidget, true, true, 0);
            filterBox.PackEnd(helpBox, false, true, 0);

            table1.Attach(new Label("Data Source:") { Xalign = 0 }, 0, 0, 1, 1);
            table1.Attach(new Label("X:") { Xalign = 0 }, 0, 1, 1, 1);
            table1.Attach(new Label("Y:") { Xalign = 0 }, 0, 2, 1, 1);
            label4 = new Label("X2:") { Xalign = 0 };
            label5 = new Label("Y2:") { Xalign = 0 };
            table1.Attach(label4, 0, 3, 1, 1);
            table1.Attach(label5, 0, 4, 1, 1);
            table1.Attach(new Label("Type:") { Xalign = 0 }, 0, 5, 1, 1);
            table1.Attach(new Label("Line type:") { Xalign = 0 }, 0, 6, 1, 1);
            table1.Attach(new Label("Marker type:") { Xalign = 0 }, 0, 7, 1, 1);
            table1.Attach(new Label("Colour:") { Xalign = 0 }, 0, 8, 1, 1);
            table1.Attach(new Label("Filter:") { Xalign = 0 }, 0, 9, 1, 1);

            // fixme: these widgets used to have 10px horizontal padding and 2px vertical padding.
            table1.Attach(dataSourceDropDown.MainWidget, 1, 0, 1, 1/*, 10, 2*/);
            table1.Attach(xDropDown.MainWidget, 1, 1, 1, 1/*10, 2*/);
            table1.Attach(yDropDown.MainWidget, 1, 2, 1, 1/*10, 2*/);
            table1.Attach(x2DropDown.MainWidget, 1, 3, 1, 1/*10, 2*/);
            table1.Attach(y2DropDown.MainWidget, 1, 4, 1, 1/*10, 2*/);
            table1.Attach(seriesDropDown.MainWidget, 1, 5, 1, 1/*10, 2*/);
            table1.Attach(lineTypeDropDown.MainWidget, 1, 6, 1, 1/*10, 2*/);
            table1.Attach(markerTypeDropDown.MainWidget, 1, 7, 1, 1/*10, 2*/);
            table1.Attach(colourDropDown.MainWidget, 1, 8, 1, 1/*10, 2*/);

            table1.Attach(filterBox, 1, 9, 1, 1/*10, 2*/);

            table1.Attach(checkBoxView1.MainWidget, 2, 1, 1, 1);
            table1.Attach(checkBoxView2.MainWidget, 2, 2, 1, 1);
            table1.Attach(checkBoxView3.MainWidget, 3, 1, 1, 1);
            table1.Attach(checkBoxView4.MainWidget, 3, 2, 1, 1);

            table1.Attach(checkBoxView5.MainWidget, 2, 8, 2, 1);
            table1.Attach(checkBoxView6.MainWidget, 2, 9, 2, 1);

            // fixme: These apparently used to have 5px vertical padding.
            table1.Attach(new Label("Line thickness:") { Xalign = 0 }, 2, 6, 1, 1/*, 0, 5*/);
            table1.Attach(lineThicknessDropDown.MainWidget, 3, 6, 1, 1/*, 0, 5*/);
            table1.Attach(new Label("Marker size:") { Xalign = 0 }, 2, 7, 1, 1/*, 0, 5*/);
            table1.Attach(markerSizeDropDown.MainWidget, 3, 7, 1, 1/*, 0, 5*/);
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            try
            {
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                helpBox.ButtonReleaseEvent -= Help_ButtonPressEvent;
                dataSourceDropDown.Dispose();
                xDropDown.Dispose();
                yDropDown.Dispose();
                x2DropDown.Dispose();
                y2DropDown.Dispose();
                seriesDropDown.Dispose();
                lineTypeDropDown.Dispose();
                markerTypeDropDown.Dispose();
                colourDropDown.Dispose();
                lineThicknessDropDown.Dispose();
                markerSizeDropDown.Dispose();
                checkBoxView1.Dispose();
                checkBoxView2.Dispose();
                checkBoxView3.Dispose();
                checkBoxView4.Dispose();
                checkBoxView5.Dispose();
                checkBoxView6.Dispose();
                graphView1.Dispose();
                editView1.Dispose();
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Data source</summary>
        public IDropDownView DataSource { get { return dataSourceDropDown; } }

        /// <summary>X field</summary>
        public IDropDownView X { get { return xDropDown; } }

        /// <summary>Y field</summary>
        public IDropDownView Y { get { return yDropDown; } }

        /// <summary>X2 field</summary>
        public IDropDownView X2 { get { return x2DropDown; } }

        /// <summary>Y2 field</summary>
        public IDropDownView Y2 { get { return y2DropDown; } }

        /// <summary>Series type</summary>
        public IDropDownView SeriesType { get { return seriesDropDown; } }

        /// <summary>Line type</summary>
        public IDropDownView LineType { get { return lineTypeDropDown; } }

        /// <summary>MarkerType</summary>
        public IDropDownView MarkerType { get { return markerTypeDropDown; } }

        /// <summary>Line thickness</summary>
        public IDropDownView LineThickness { get { return lineThicknessDropDown; } }

        /// <summary>Marker size</summary>
        public IDropDownView MarkerSize { get { return markerSizeDropDown; } }

        /// <summary>Colour</summary>
        public IColourDropDownView Colour { get { return colourDropDown; } }

        /// <summary>X on top checkbox.</summary>
        public ICheckBoxView XOnTop { get { return checkBoxView1; } }

        /// <summary>Y on right checkbox.</summary>
        public ICheckBoxView YOnRight { get { return checkBoxView2; } }

        /// <summary>X cumulative checkbox.</summary>
        public ICheckBoxView XCumulative { get { return checkBoxView3; } }

        /// <summary>Y cumulative checkbox.</summary>
        public ICheckBoxView YCumulative { get { return checkBoxView4; } }

        /// <summary>Show in lengend checkbox.</summary>
        public ICheckBoxView ShowInLegend { get { return checkBoxView5; } }

        /// <summary>Include series name in legend.</summary>
        public ICheckBoxView IncludeSeriesNameInLegend { get { return checkBoxView6; } }

        /// <summary>Graph.</summary>
        public IGraphView GraphView { get { return graphView1; } }

        /// <summary>Filter box.</summary>
        public IEditView Filter { get { return editView1; } }

        /// <summary>Show or hide the x2 and y2 drop downs.</summary>
        /// <param name="show"></param>
        public void ShowX2Y2(bool show)
        {
            label4.Visible = show;
            label5.Visible = show;
            X2.Visible = show;
            Y2.Visible = show;
        }

        /// <summary>Show the filter help.</summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void Help_ButtonPressEvent(object sender, ButtonReleaseEventArgs args)
        {
            try
            {
                if (args.Event.Button == 1)
                    ProcessUtilities.ProcessStart("https://apsimnextgeneration.netlify.app/usage/graphs/graphfilters/");
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the user has finished editing the filter.
        /// </summary>
        public void EndEdit()
        {
            editView1.EndEdit();
        }
    }
}
