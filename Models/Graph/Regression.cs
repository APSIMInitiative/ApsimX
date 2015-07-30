// -----------------------------------------------------------------------
// <copyright file="Regression.cs" company="CSIRO">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Graph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Collections;
    using APSIM.Shared.Utilities;
    using System.Drawing;
    using Models.Core;

    /// <summary>
    /// A regression model.
    /// </summary>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(typeof(Series))]
    public class Regression : Model, IGraphable
    {
        /// <summary>The stats from the regression</summary>
        private List<MathUtilities.RegrStats> stats = new List<MathUtilities.RegrStats>();

        /// <summary>The colours to use for each equation.</summary>
        private List<Color> equationColours = new List<Color>();

        /// <summary>
        /// Gets or sets a value indicating whether a regression should be shown for each series.
        /// </summary>
        /// <value><c>true</c> if [for each series]; otherwise, <c>false</c>.</value>
        [Description("Display regression line and equation for each series?")]
        public bool ForEachSeries { get; set; }

        /// <summary>Called by the graph presenter to get a list of all actual series to put on the graph.</summary>
        /// <param name="definitions">A list of definitions to add to.</param>
        public void GetSeriesToPutOnGraph(List<SeriesDefinition> definitions)
        {
            stats.Clear();
            equationColours.Clear();

            // Get all x/y data
            List<double> x = new List<double>();
            List<double> y = new List<double>();
            foreach (SeriesDefinition definition in definitions)
            {
                if (definition.x is double[] && definition.y is double[])
                {
                    x.AddRange(definition.x as IEnumerable<double>);
                    y.AddRange(definition.y as IEnumerable<double>);
                }
            }

            if (ForEachSeries)
            {
                // Display a regression line for each series.
                int numDefinitions = definitions.Count;
                for (int i = 0; i < numDefinitions; i++)
                {
                    if (definitions[i].x is double[] && definitions[i].y is double[])
                    {
                        PutRegressionLineOnGraph(definitions, definitions[i].x, definitions[i].y, definitions[i].colour, null);
                        equationColours.Add(definitions[i].colour);
                    }
                }
            }
            else
            {
                // Display a single regression line for all data.
                PutRegressionLineOnGraph(definitions, x, y, Color.Black, "Regression line");
                equationColours.Add(Color.Black);
            }

            Put1To1LineOnGraph(definitions, x, y);
        }

        /// <summary>Puts the regression line and 1:1 line on graph.</summary>
        /// <param name="definitions">The definitions.</param>
        /// <param name="x">The x data.</param>
        /// <param name="y">The y data.</param>
        /// <param name="colour">The colour of the regresion line.</param>
        /// <param name="title">The title to put in the legen.</param>
        private void PutRegressionLineOnGraph(List<SeriesDefinition> definitions, IEnumerable x, IEnumerable y, 
                                              Color colour, string title)
        {
            MathUtilities.RegrStats stat = MathUtilities.CalcRegressionStats(x, y);
            stats.Add(stat);
            double minimumX = MathUtilities.Min(x);
            double maximumX = MathUtilities.Max(x);
            double minimumY = MathUtilities.Min(y);
            double maximumY = MathUtilities.Max(y);
            double lowestAxisScale = Math.Min(minimumX, minimumY);
            double largestAxisScale = Math.Max(maximumX, maximumY);

            SeriesDefinition regressionDefinition = new SeriesDefinition();
            regressionDefinition.title = title;
            regressionDefinition.colour = colour;
            regressionDefinition.line = LineType.Solid;
            regressionDefinition.marker = MarkerType.None;
            regressionDefinition.showInLegend = true;
            regressionDefinition.type = SeriesType.Scatter;
            regressionDefinition.xAxis = Axis.AxisType.Bottom;
            regressionDefinition.yAxis = Axis.AxisType.Left;
            regressionDefinition.x = new double[] { minimumX, maximumX };
            regressionDefinition.y = new double[] { stat.m * minimumX + stat.c, stat.m * maximumX + stat.c };
            definitions.Add(regressionDefinition);
        }

        /// <summary>Puts the 1:1 line on graph.</summary>
        /// <param name="definitions">The definitions.</param>
        /// <param name="x">The x data.</param>
        /// <param name="y">The y data.</param>
        private static void Put1To1LineOnGraph(List<SeriesDefinition> definitions, IEnumerable x, IEnumerable y)
        {
            double minimumX = MathUtilities.Min(x);
            double maximumX = MathUtilities.Max(x);
            double minimumY = MathUtilities.Min(y);
            double maximumY = MathUtilities.Max(y);
            double lowestAxisScale = Math.Min(minimumX, minimumY);
            double largestAxisScale = Math.Max(maximumX, maximumY);

            SeriesDefinition oneToOne = new SeriesDefinition();
            oneToOne.title = "1:1 line";
            oneToOne.colour = Color.Black;
            oneToOne.line = LineType.Dot;
            oneToOne.marker = MarkerType.None;
            oneToOne.showInLegend = true;
            oneToOne.type = SeriesType.Scatter;
            oneToOne.xAxis = Axis.AxisType.Bottom;
            oneToOne.yAxis = Axis.AxisType.Left;
            oneToOne.x = new double[] { lowestAxisScale, largestAxisScale };
            oneToOne.y = new double[] { lowestAxisScale, largestAxisScale };
            definitions.Add(oneToOne);
        }

        /// <summary>Called by the graph presenter to get a list of all annotations to put on the graph.</summary>
        /// <param name="annotations">A list of annotations to add to.</param>
        public void GetAnnotationsToPutOnGraph(List<Annotation> annotations)
        {
            for (int i = 0; i < stats.Count; i++)
            {
                // Add an equation annotation.
                Annotation equation = new Annotation();
                equation.text = string.Format("y = {0:F2} x + {1:F2}, r2 = {2:F2}, n = {3:F0}\r\n" +
                                                    "NSE = {4:F2}, ME = {5:F2}, MAE = {6:F2}\r\n" +
                                                    "RSR = {7:F2}, RMSD = {8:F2}",
                                                    new object[] {stats[i].m,   stats[i].c,   stats[i].R2,
                                                                  stats[i].n,   stats[i].NSE, stats[i].ME,
                                                                  stats[i].MAE, stats[i].RSR, stats[i].RMSD});
                equation.colour = equationColours[i];
                annotations.Add(equation);
            }
        }

    }
}
