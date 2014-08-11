
using Models.Core;
using System.Xml.Serialization;
using System.Data;
using System.Drawing;
using System;
using System.Collections.Generic;
namespace Models.Graph
{
    [Serializable]
    public class Series
    {
        public Series()
        {
            XAxis = Axis.AxisType.Bottom;
            Colour = Color.Blue;
            SplitOn = "Simulation";
        }

        public string Title { get; set; }

        public enum SeriesType { Line, Bar, Scatter, Area };
        public SeriesType Type { get; set; }
        
        public Axis.AxisType XAxis { get; set; }
        public Axis.AxisType YAxis { get; set; }
        public int ColourArgb { get; set; }

        [XmlIgnore]
        public Color Colour
        {
            get
            {
                return Color.FromArgb(ColourArgb);
            }
            set
            {
                ColourArgb = value.ToArgb();
            }
        }

        public enum MarkerType { None, Circle, Diamond, Square, Triangle, Cross, Plus, Star, FilledCircle, FilledDiamond, FilledSquare, FilledTriangle }
        public MarkerType Marker { get; set; }

        public enum LineType { Solid, Dash, Dot, DashDot, None };
        public LineType Line { get; set; }

        public GraphValues X { get; set; }
        public GraphValues Y { get; set; }
        public GraphValues X2 { get; set; }
        public GraphValues Y2 { get; set; }

        public bool ShowRegressionLine { get; set; }
        public bool ShowInLegend { get; set; }
        public string SplitOn { get; set; }

    }
}
