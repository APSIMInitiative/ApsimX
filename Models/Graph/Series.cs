// -----------------------------------------------------------------------
// <copyright file="Series.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Graph
{
    using System;
    using System.Drawing;
    using System.Xml.Serialization;

    /// <summary>
    /// The class represents a single series on a graph
    /// </summary>
    [Serializable]
    public class Series
    {
        /// <summary>
        /// Constructor for a series
        /// </summary>
        public Series()
        {
            this.XAxis = Axis.AxisType.Bottom;
            this.Colour = Color.Blue;
        }

        /// <summary>
        /// An enumeration for the different types of graph series
        /// </summary>
        public enum SeriesType 
        { 
            /// <summary>
            /// A line series. Kept for compatibility reasons
            /// </summary>
            Line, 

            /// <summary>
            /// A bar series
            /// </summary>
            Bar, 

            /// <summary>
            /// A scatter series
            /// </summary>
            Scatter, 

            /// <summary>
            /// An area series
            /// </summary>
            Area 
        }

        /// <summary>
        /// An enumeration for the different types of markers
        /// </summary>
        public enum MarkerType 
        { 
            /// <summary>
            /// No marker should be display
            /// </summary>
            None, 

            /// <summary>
            /// A circle marker
            /// </summary>
            Circle, 

            /// <summary>
            /// A diamond marker
            /// </summary>
            Diamond, 

            /// <summary>
            /// A square marker
            /// </summary>
            Square, 

            /// <summary>
            /// A triangle marker
            /// </summary>
            Triangle, 

            /// <summary>
            /// A cross marker
            /// </summary>
            Cross, 

            /// <summary>
            /// A plus marker
            /// </summary>
            Plus, 

            /// <summary>
            /// A star marker
            /// </summary>
            Star, 

            /// <summary>
            /// A filled circle marker
            /// </summary>
            FilledCircle, 

            /// <summary>
            /// A filled diamond marker
            /// </summary>
            FilledDiamond, 

            /// <summary>
            /// A filled square marker
            /// </summary>
            FilledSquare, 

            /// <summary>
            /// A filled triangle marker
            /// </summary>
            FilledTriangle 
        }
        
        /// <summary>
        /// An enumeration representing the different types of lines
        /// </summary>
        public enum LineType 
        { 
            /// <summary>
            /// A solid line
            /// </summary>
            Solid, 

            /// <summary>
            /// A dashed line
            /// </summary>
            Dash, 

            /// <summary>
            /// A dotted line
            /// </summary>
            Dot, 

            /// <summary>
            /// A dash dot line
            /// </summary>
            DashDot, 

            /// <summary>
            /// No line
            /// </summary>
            None 
        }
        
        /// <summary>
        /// Gets or sets the series title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the series type
        /// </summary>
        public SeriesType Type { get; set; }
        
        /// <summary>
        /// Gets or sets the associated x axis
        /// </summary>
        public Axis.AxisType XAxis { get; set; }

        /// <summary>
        /// Gets or sets the associated y axis
        /// </summary>
        public Axis.AxisType YAxis { get; set; }

        /// <summary>
        /// Gets or sets the color represented as a red, green, blue integer
        /// </summary>
        public int ColourArgb { get; set; }

        /// <summary>
        /// Gets or sets the color 
        /// </summary>
        [XmlIgnore]
        public Color Colour
        {
            get
            {
                return Color.FromArgb(this.ColourArgb);
            }

            set
            {
                this.ColourArgb = value.ToArgb();
            }
        }

        /// <summary>
        /// Gets or sets the marker to show
        /// </summary>
        public MarkerType Marker { get; set; }

        /// <summary>
        /// Gets or sets the line type to show
        /// </summary>
        public LineType Line { get; set; }

        /// <summary>
        /// Gets or sets the x values
        /// </summary>
        public GraphValues X { get; set; }

        /// <summary>
        /// Gets or sets the y values
        /// </summary>
        public GraphValues Y { get; set; }

        /// <summary>
        /// Gets or sets the second x values
        /// </summary>
        public GraphValues X2 { get; set; }

        /// <summary>
        /// Gets or sets the second y values
        /// </summary>
        public GraphValues Y2 { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a regression line should be shown for this series.
        /// </summary>
        public bool ShowRegressionLine { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the series should be shown in the legend
        /// </summary>
        public bool ShowInLegend { get; set; }

        /// <summary>
        /// Gets or sets a string indicating what the series should be split on e.g. 'simulation' or 'experiment'
        /// </summary>
        public bool Cumulative { get; set; }
    }
}
