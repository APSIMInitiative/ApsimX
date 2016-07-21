// -----------------------------------------------------------------------
// <copyright file="SeriesView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using Interfaces;
    using Gtk;
    using Glade;
    /// using System.Windows.Forms;

    /// <summary>This view allows a single series to be edited.</summary>
    public class SeriesView : ViewBase, ISeriesView
    {
        [Widget]
        private Table table1 = null;
        [Widget]
        private VBox vbox1 = null;
        [Widget]
        private Label label4 = null;
        [Widget]
        private Label label5 = null;

        private DropDownView dropDownView1;
        private DropDownView dropDownView2;
        private DropDownView dropDownView3;
        private DropDownView dropDownView4;
        private DropDownView dropDownView5;
        private DropDownView dropDownView6;
        private DropDownView dropDownView7;
        private DropDownView dropDownView8;
        private ColourDropDownView dropDownView9;
        private DropDownView dropDownView10;
        private DropDownView dropDownView11;
        private CheckBoxView checkBoxView1;
        private CheckBoxView checkBoxView2;
        private CheckBoxView checkBoxView3;
        private CheckBoxView checkBoxView4;
        private CheckBoxView checkBoxView5;
        private CheckBoxView checkBoxView6;
        private GraphView graphView1;
        private EditView editView1;

        /// <summary>Initializes a new instance of the <see cref="SeriesView" /> class</summary>
        public SeriesView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.SeriesView.glade", "vbox1");
            gxml.Autoconnect(this);
            _mainWidget = vbox1;

            graphView1 = new GraphView(this);
            vbox1.PackStart(graphView1.MainWidget, true, true, 0);

            dropDownView1 = new DropDownView(this);
            dropDownView2 = new DropDownView(this);
            dropDownView3 = new DropDownView(this);
            dropDownView4 = new DropDownView(this);
            dropDownView5 = new DropDownView(this);
            dropDownView6 = new DropDownView(this);
            dropDownView7 = new DropDownView(this);
            dropDownView8 = new DropDownView(this);
            dropDownView9 = new ColourDropDownView(this);
            dropDownView10 = new DropDownView(this);
            dropDownView11 = new DropDownView(this);

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

            table1.Attach(dropDownView1.MainWidget, 1, 2, 0, 1, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(dropDownView2.MainWidget, 1, 2, 1, 2, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(dropDownView3.MainWidget, 1, 2, 2, 3, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(dropDownView5.MainWidget, 1, 2, 3, 4, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(dropDownView4.MainWidget, 1, 2, 4, 5, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(dropDownView6.MainWidget, 1, 2, 5, 6, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(dropDownView7.MainWidget, 1, 2, 6, 7, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(dropDownView8.MainWidget, 1, 2, 7, 8, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(dropDownView9.MainWidget, 1, 2, 8, 9, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(editView1.MainWidget, 1, 2, 9, 10, AttachOptions.Fill, 0, 10, 2);

            table1.Attach(checkBoxView1.MainWidget, 2, 3, 1, 2, AttachOptions.Fill, 0, 0, 0);
            table1.Attach(checkBoxView2.MainWidget, 2, 3, 2, 3, AttachOptions.Fill, 0, 0, 0);
            table1.Attach(checkBoxView3.MainWidget, 3, 4, 1, 2, AttachOptions.Fill, 0, 0, 0);
            table1.Attach(checkBoxView4.MainWidget, 3, 4, 2, 3, AttachOptions.Fill, 0, 0, 0);

            table1.Attach(checkBoxView5.MainWidget, 2, 4, 8, 9, AttachOptions.Fill, 0, 0, 0);
            table1.Attach(checkBoxView6.MainWidget, 2, 4, 9, 10, AttachOptions.Fill, 0, 0, 0);

            table1.Attach(dropDownView10.MainWidget, 3, 4, 6, 7, AttachOptions.Fill, 0, 0, 5);
            table1.Attach(dropDownView11.MainWidget, 3, 4, 7, 8, AttachOptions.Fill, 0, 0, 5);
        }

        /// <summary>Data source</summary>
        public IDropDownView DataSource { get { return dropDownView1; } }

        /// <summary>X field</summary>
        public IDropDownView X { get { return dropDownView2; } }

        /// <summary>Y field</summary>
        public IDropDownView Y { get { return dropDownView3; } }

        /// <summary>X2 field</summary>
        public IDropDownView X2 { get { return dropDownView4; } }

        /// <summary>Y2 field</summary>
        public IDropDownView Y2 { get { return dropDownView5; } }

        /// <summary>Series type</summary>
        public IDropDownView SeriesType { get { return dropDownView6; } }

        /// <summary>Line type</summary>
        public IDropDownView LineType { get { return dropDownView7; } }

        /// <summary>MarkerType</summary>
        public IDropDownView MarkerType { get { return dropDownView8; } }

        /// <summary>Line thickness</summary>
        public IDropDownView LineThickness { get { return dropDownView10; } }

        /// <summary>Marker size</summary>
        public IDropDownView MarkerSize { get { return dropDownView11; } }

        /// <summary>Colour</summary>
        public IColourDropDownView Colour { get { return dropDownView9; } }

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
            X2.IsVisible = show;
            Y2.IsVisible = show;
        }


    }
}
