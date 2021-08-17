using System;
using APSIM.Services.Graphing;
using OxyPlot;
using MarkerType = APSIM.Services.Graphing.MarkerType;
using LegendOrientation = APSIM.Services.Graphing.LegendOrientation;
using LegendPosition = APSIM.Services.Graphing.LegendPosition;

namespace APSIM.Interop.Graphing
{
    /// <summary>
    /// Extension methods for converting apsim graphing enumerations
    /// into their oxyplot equivalents.
    /// </summary>
    public static class EnumerationExtensions
    {
        /// <summary>
        /// Convert an apsim line style to an oxyplot line style.
        /// </summary>
        /// <param name="lineType">Apsim line type/style.</param>
        public static LineStyle ToOxyPlotLineStyle(this LineType lineType)
        {
            switch (lineType)
            {
                case LineType.Dash:
                    return LineStyle.Dash;
                case LineType.DashDot:
                    return LineStyle.DashDot;
                case LineType.Dot:
                    return LineStyle.Dot;
                case LineType.None:
                    return LineStyle.None;
                case LineType.Solid:
                    return LineStyle.Solid;
                default:
                    throw new NotImplementedException($"Unknown line type: {lineType}");
            }
        }

        /// <summary>
        /// Convert an apsim line thickness into an oxyplot line thickness.
        /// </summary>
        /// <param name="thickness">Apsim line thickness.</param>
        public static double ToOxyPlotThickness(this LineThickness thickness)
        {
            switch (thickness)
            {
                case LineThickness.Normal:
                    return 0.5;
                case LineThickness.Thin:
                    // todo: test this
                    return 0.25;
                default:
                    throw new NotImplementedException($"Unknown line thickness: {thickness}");
            }
        }

        /// <summary>
        /// Convert an apsim marker size into an oxyplot marker size.
        /// </summary>
        /// <param name="thickness">Apsim marker size.</param>
        public static double ToOxyPlotMarkerSize(this MarkerSize size)
        {
            switch (size)
            {
                case MarkerSize.VerySmall:
                    return 3;
                case MarkerSize.Small:
                    return 5;
                case MarkerSize.Normal:
                    return 7;
                case MarkerSize.Large:
                    return 9;
                default:
                    throw new NotImplementedException($"Unknown marker size: {size}");
            }
        }

        /// <summary>
        /// Convert an apsim marker type into an oxyplot marker type.
        /// </summary>
        /// <param name="marker">Marker type.</param>
        public static OxyPlot.MarkerType ToOxyPlotMarkerType(this MarkerType marker)
        {
            switch (marker)
            {
                case MarkerType.Circle:
                case MarkerType.FilledCircle:
                    return OxyPlot.MarkerType.Circle;
                case MarkerType.Cross:
                    return OxyPlot.MarkerType.Cross;
                case MarkerType.Diamond:
                case MarkerType.FilledDiamond:
                    return OxyPlot.MarkerType.Diamond;
                case MarkerType.None:
                    return OxyPlot.MarkerType.None;
                case MarkerType.Plus:
                    return OxyPlot.MarkerType.Plus;
                case MarkerType.Square:
                case MarkerType.FilledSquare:
                    return OxyPlot.MarkerType.Square;
                case MarkerType.Star:
                    return OxyPlot.MarkerType.Star;
                case MarkerType.Triangle:
                case MarkerType.FilledTriangle:
                    return OxyPlot.MarkerType.Triangle;
                default:
                    throw new NotImplementedException($"Unknown marker type: {marker}");
            }
        }

        /// <summary>
        /// Convert an apsim legend orientation to an oxyplot legend orientation.
        /// </summary>
        /// <param name="orientation">An apsim legend orientation.</param>
        public static OxyPlot.LegendOrientation ToOxyPlotLegendOrientation(this LegendOrientation orientation)
        {
            switch (orientation)
            {
                case LegendOrientation.Horizontal:
                    return OxyPlot.LegendOrientation.Horizontal;
                case LegendOrientation.Vertical:
                    return OxyPlot.LegendOrientation.Vertical;
                default:
                    throw new NotImplementedException($"Unknown legend orientation: {orientation}");
            }
        }

        /// <summary>
        /// Convert an apsim legend position to an oxyplot legend position.
        /// </summary>
        /// <param name="position">An apsim legend position.</param>
        public static OxyPlot.LegendPosition ToOxyPlotLegendPosition(this LegendPosition position)
        {
            switch (position)
            {
                case LegendPosition.TopLeft:
                    return OxyPlot.LegendPosition.TopLeft;
                case LegendPosition.TopCenter:
                    return OxyPlot.LegendPosition.TopCenter;
                case LegendPosition.TopRight:
                    return OxyPlot.LegendPosition.TopRight;
                case LegendPosition.LeftTop:
                    return OxyPlot.LegendPosition.LeftTop;
                case LegendPosition.LeftMiddle:
                    return OxyPlot.LegendPosition.LeftMiddle;
                case LegendPosition.LeftBottom:
                    return OxyPlot.LegendPosition.LeftBottom;
                case LegendPosition.RightTop:
                    return OxyPlot.LegendPosition.RightTop;
                case LegendPosition.RightMiddle:
                    return OxyPlot.LegendPosition.RightMiddle;
                case LegendPosition.RightBottom:
                    return OxyPlot.LegendPosition.RightBottom;
                case LegendPosition.BottomLeft:
                    return OxyPlot.LegendPosition.BottomLeft;
                case LegendPosition.BottomCenter:
                    return OxyPlot.LegendPosition.BottomCenter;
                case LegendPosition.BottomRight:
                    return OxyPlot.LegendPosition.BottomRight;
                default:
                    throw new NotImplementedException($"Unknown legend position: {position}");
            }
        }

        /// <summary>
        /// Convert a System.Drawing.Color to an OxyColor.
        /// </summary>
        /// <param name="colour">The colour to be converted.</param>
        public static OxyColor ToOxyColour(this System.Drawing.Color colour)
        {
            return OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
        }
    }
}
