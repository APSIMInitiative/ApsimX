namespace UserInterface.EventArguments
{
    using System;

    /// <summary>
    /// Arguments for a legend click
    /// </summary>
    public class LegendClickArgs : EventArgs
    {
        public int SeriesIndex { get; set; }
        public bool ControlKeyPressed { get; set; }
    }
}
