using OxyPlot;

using OxyPlot.Legends;
using System.Linq;


namespace APSIM.Interop.Graphing.Extensions
{
    /// <summary>
    /// Extension methods for OxyPlot's plot model which provide a
    /// unified gtk2/3 API.
    /// </summary>
    public static class PlotModelExtensions
    {
        public static void SetLegendBorder(this PlotModel plot, OxyColor colour)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendBorder = colour;
        }

        public static void SetLegendTextColour(this PlotModel plot, OxyColor colour)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendTextColor = colour;
        }

        public static void SetLegendTitleColour(this PlotModel plot, OxyColor colour)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendTitleColor = colour;
        }

        public static void SetLegendBackground(this PlotModel plot, OxyColor colour)
        {
        foreach (LegendBase legend in plot.Legends)
            legend.LegendBackground = colour;
        }

        public static void SetLegendFontSize(this PlotModel plot, double size)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendFontSize = size;
        }

        public static void SetLegendFont(this PlotModel plot, string font)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendFont = font;
        }

        public static void SetLegendPosition(this PlotModel plot, LegendPosition position)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendPosition = position;
        }

        public static void SetLegendOrientation(this PlotModel plot, LegendOrientation orientation)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendOrientation = orientation;
        }

        public static void SetLegendSymbolLength(this PlotModel plot, double length)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendSymbolLength = length;
        }

        public static void SetLegendPlacement(this PlotModel plot, LegendPlacement placement)
        {
            foreach (LegendBase legend in plot.Legends)
                legend.LegendPlacement = placement;
        }

        public static LegendPlacement GetLegendPlacement(this PlotModel plot)
        {
            return plot.Legends.FirstOrDefault()?.LegendPlacement ?? LegendPlacement.Inside;
        }
    }
}
