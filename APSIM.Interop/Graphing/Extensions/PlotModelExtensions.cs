using OxyPlot;
#if NETCOREAPP
using OxyPlot.Legends;
using System.Linq;
#endif

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
#if NETFRAMEWORK
                plot.LegendBorder = OxyColors.Transparent;
#else
                foreach (LegendBase legend in plot.Legends)
                    legend.LegendBorder = colour;
#endif
        }

        public static void SetLegendTextColour(this PlotModel plot, OxyColor colour)
        {
#if NETFRAMEWORK
                plot.LegendTextColor = colour;
#else
                foreach (LegendBase legend in plot.Legends)
                    legend.LegendTextColor = colour;
#endif
        }

        public static void SetLegendTitleColour(this PlotModel plot, OxyColor colour)
        {
#if NETFRAMEWORK
                plot.LegendTitleColor = colour;
#else
                foreach (LegendBase legend in plot.Legends)
                    legend.LegendTitleColor = colour;
#endif
        }

        public static void SetLegendBackground(this PlotModel plot, OxyColor colour)
        {
#if NETFRAMEWORK
            plot.LegendBackground = colour;
#else
            foreach (LegendBase legend in plot.Legends)
                legend.LegendBackground = colour;
#endif
        }

        public static void SetLegendFontSize(this PlotModel plot, double size)
        {
#if NETFRAMEWORK
                plot.LegendFontSize = size;
#else
                foreach (LegendBase legend in plot.Legends)
                    legend.LegendFontSize = size;
#endif
        }

        public static void SetLegendFont(this PlotModel plot, string font)
        {
#if NETFRAMEWORK
                plot.LegendFont = font;
#else
                foreach (LegendBase legend in plot.Legends)
                    legend.LegendFont = font;
#endif
        }

        public static void SetLegendPosition(this PlotModel plot, LegendPosition position)
        {
#if NETFRAMEWORK
                plot.LegendPosition = position;
#else
                foreach (LegendBase legend in plot.Legends)
                    legend.LegendPosition = position;
#endif
        }

        public static void SetLegendOrientation(this PlotModel plot, LegendOrientation orientation)
        {
#if NETFRAMEWORK
                plot.LegendOrientation = orientation;
#else
                foreach (LegendBase legend in plot.Legends)
                    legend.LegendOrientation = orientation;
#endif
        }

        public static void SetLegendSymbolLength(this PlotModel plot, double length)
        {
#if NETFRAMEWORK
                plot.LegendSymbolLength = length;
#else
                foreach (LegendBase legend in plot.Legends)
                    legend.LegendSymbolLength = length;
#endif
        }

        public static void SetLegendPlacement(this PlotModel plot, LegendPlacement placement)
        {
#if NETFRAMEWORK
                plot.LegendPlacement = placement;
#else
                foreach (LegendBase legend in plot.Legends)
                    legend.LegendPlacement = placement;
#endif
        }

        public static LegendPlacement GetLegendPlacement(this PlotModel plot)
        {
#if NETFRAMEWORK
                return plot.LegendPlacement;
#else
                return plot.Legends.FirstOrDefault()?.LegendPlacement ?? LegendPlacement.Inside;
#endif
        }
    }
}
