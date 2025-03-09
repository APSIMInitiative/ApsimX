using OxyPlot;
using OxyPlot.Legends;
using System.Linq;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// Extension methods for OxyPlot's plot model which provide a
    /// unified gtk2/3 API.
    /// </summary>
    public static class PlotModelExtensions
    {
        /// <summary></summary>
        public static void SetLegendBorder(this PlotModel plot, OxyColor colour)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendBorder = colour;
        }

        /// <summary></summary>
        public static void SetLegendTextColour(this PlotModel plot, OxyColor colour)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendTextColor = colour;
        }

        /// <summary></summary>
        public static void SetLegendTitleColour(this PlotModel plot, OxyColor colour)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendTitleColor = colour;
        }

        /// <summary></summary>
        public static void SetLegendBackground(this PlotModel plot, OxyColor colour)
        {
        foreach (LegendBase legend in plot.Legends)
            legend.LegendBackground = colour;
        }

        /// <summary></summary>
        public static void SetLegendFontSize(this PlotModel plot, double size)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendFontSize = size;
        }

        /// <summary></summary>
        public static void SetLegendFont(this PlotModel plot, string font)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendFont = font;
        }

        /// <summary></summary>
        public static void SetLegendPosition(this PlotModel plot, LegendPosition position)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendPosition = position;
        }

        /// <summary></summary>
        public static void SetLegendOrientation(this PlotModel plot, LegendOrientation orientation)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendOrientation = orientation;
        }

        /// <summary></summary>
        public static void SetLegendSymbolLength(this PlotModel plot, double length)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendSymbolLength = length;
        }

        /// <summary></summary>
        public static void SetLegendPlacement(this PlotModel plot, LegendPlacement placement)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendPlacement = placement;
        }

        /// <summary></summary>
        public static LegendPlacement GetLegendPlacement(this PlotModel plot)
        {
            return plot.Legends.FirstOrDefault()?.LegendPlacement ?? LegendPlacement.Inside;
        }
    }
}
