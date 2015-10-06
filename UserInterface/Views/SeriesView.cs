// -----------------------------------------------------------------------
// <copyright file="SeriesView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using Interfaces;
    using System.Windows.Forms;

    /// <summary>This view allows a single series to be edited.</summary>
    public partial class SeriesView : UserControl, ISeriesView
    {
        /// <summary>Initializes a new instance of the <see cref="SeriesView" /> class</summary>
        public SeriesView()
        {
            InitializeComponent();
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
