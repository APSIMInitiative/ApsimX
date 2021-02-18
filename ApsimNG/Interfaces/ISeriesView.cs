namespace UserInterface.Interfaces
{
    using Views;

    /// <summary>
    /// This interface defines the API for talking to an initial water view.
    /// </summary>
    public interface ISeriesView
    {
        /// <summary>Data source</summary>
        IDropDownView DataSource { get; }

        /// <summary>X field</summary>
        IDropDownView X { get; }

        /// <summary>Y field</summary>
        IDropDownView Y { get; }

        /// <summary>X2 field</summary>
        IDropDownView X2 { get; }

        /// <summary>Y2 field</summary>
        IDropDownView Y2 { get; }

        /// <summary>Series type</summary>
        IDropDownView SeriesType { get; }

        /// <summary>Line type</summary>
        IDropDownView LineType { get; }

        /// <summary>MarkerType</summary>
        IDropDownView MarkerType { get; }

        /// <summary>Line thickness</summary>
        IDropDownView LineThickness { get; }

        /// <summary>Marker size</summary>
        IDropDownView MarkerSize { get; }

        /// <summary>Colour</summary>
        IColourDropDownView Colour { get; }

        /// <summary>X on top checkbox.</summary>
        ICheckBoxView XOnTop { get; }

        /// <summary>Y on right checkbox.</summary>
        ICheckBoxView YOnRight { get; }

        /// <summary>X cumulative checkbox.</summary>
        ICheckBoxView XCumulative { get; }

        /// <summary>Y cumulative checkbox.</summary>
        ICheckBoxView YCumulative { get; }

        /// <summary>Show in lengend checkbox.</summary>
        ICheckBoxView ShowInLegend { get; }

        /// <summary>Include series name in legend.</summary>
        ICheckBoxView IncludeSeriesNameInLegend { get; }

        /// <summary>Graph.</summary>
        IGraphView GraphView { get; }

        /// <summary>Filter box.</summary>
        IEditView Filter { get; }

        /// <summary>Show or hide the x2 and y2 drop downs.</summary>
        /// <param name="show"></param>
        void ShowX2Y2(bool show);

        /// <summary>
        /// If editing is in progress, stop it and store the current value
        /// </summary>
        void EndEdit();
    }
}