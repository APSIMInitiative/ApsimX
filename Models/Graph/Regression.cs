namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Storage;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Drawing;

    /// <summary>
    /// A regression model.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Series))]
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

        /// <summary>
        /// Gets or sets a value indicating whether a regression should be shown for each series.
        /// </summary>
        /// <value><c>true</c> if [for each series]; otherwise, <c>false</c>.</value>
        [Description("Display 1:1 line?")]
        public bool showOneToOne { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether a regression should be shown for each series.
        /// </summary>
        /// <value><c>true</c> if [for each series]; otherwise, <c>false</c>.</value>
        [Description("Display equation?")]
        public bool showEquation { get; set; } = true;

        /// <summary>Called by the graph presenter to get a list of all actual series to put on the graph.</summary>
        /// <param name="definitions">A list of definitions to add to.</param>
        /// <param name="storage">Storage service</param>
        /// <param name="simulationsFilter">Unused simulation names filter.</param>
        public void GetSeriesToPutOnGraph(IStorageReader storage, List<SeriesDefinition> definitions, List<string> simulationsFilter = null)
        {
            stats.Clear();
            equationColours.Clear();

            int checkpointNumber = 0;
            foreach (var checkpointName in storage.CheckpointNames)
            {
                // Get all x/y data
                List<double> x = new List<double>();
                List<double> y = new List<double>();
                foreach (SeriesDefinition definition in definitions)
                {
                    if (definition.CheckpointName == checkpointName)
                        if (definition.X is double[] && definition.Y is double[])
                        {
                            x.AddRange(definition.X as IEnumerable<double>);
                            y.AddRange(definition.Y as IEnumerable<double>);
                        }
                }

                if (ForEachSeries)
                {
                    // Display a regression line for each series.
                    int numDefinitions = definitions.Count;
                    for (int i = 0; i < numDefinitions; i++)
                    {
                        if (definitions[i].X is double[] && definitions[i].Y is double[])
                        {
                            PutRegressionLineOnGraph(definitions, definitions[i].X, definitions[i].Y, definitions[i].Colour, null);
                            equationColours.Add(definitions[i].Colour);
                        }
                    }
                }
                else
                {
                    var regresionLineName = "Regression line";
                    if (checkpointName != "Current")
                        regresionLineName = "Regression line (" + checkpointName + ")";

                    // Display a single regression line for all data.
                    PutRegressionLineOnGraph(definitions, x, y, ColourUtilities.ChooseColour(checkpointNumber), regresionLineName);
                    equationColours.Add(ColourUtilities.ChooseColour(checkpointNumber));
                }

                if (showOneToOne)
                    Put1To1LineOnGraph(definitions, x, y);

                checkpointNumber++;
            }
        }
        
        /// <summary>Return a list of extra fields that the definition should read.</summary>
        /// <param name="seriesDefinition">The calling series definition.</param>
        /// <returns>A list of fields - never null.</returns>
        public IEnumerable<string> GetExtraFieldsToRead(SeriesDefinition seriesDefinition)
        {
            return new string[0];
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
            MathUtilities.RegrStats stat = MathUtilities.CalcRegressionStats(title, y, x);
            if (stat != null)
            {
                stats.Add(stat);
                double minimumX = MathUtilities.Min(x);
                double maximumX = MathUtilities.Max(x);
                double minimumY = MathUtilities.Min(y);
                double maximumY = MathUtilities.Max(y);
                double lowestAxisScale = Math.Min(minimumX, minimumY);
                double largestAxisScale = Math.Max(maximumX, maximumY);

                var regressionDefinition = new SeriesDefinition
                    (title, colour,
                     new double[] { minimumX, maximumX },
                     new double[] { stat.Slope * minimumX + stat.Intercept, stat.Slope * maximumX + stat.Intercept });
                definitions.Add(regressionDefinition);
            }
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

            var oneToOne = new SeriesDefinition
                ("1:1 line", Color.Empty,
                new double[] { lowestAxisScale, largestAxisScale },
                new double[] { lowestAxisScale, largestAxisScale },
                LineType.Dash, MarkerType.None);
            definitions.Add(oneToOne);
        }

        /// <summary>Called by the graph presenter to get a list of all annotations to put on the graph.</summary>
        /// <param name="annotations">A list of annotations to add to.</param>
        public void GetAnnotationsToPutOnGraph(List<Annotation> annotations)
        {
            if (showEquation)
            {
                for (int i = 0; i < stats.Count; i++)
                {
                    // Add an equation annotation.
                    TextAnnotation equation = new TextAnnotation();
                    equation.text = string.Format("y = {0:F2} x + {1:F2}, r2 = {2:F2}, n = {3:F0}\r\n" +
                                                        "NSE = {4:F2}, ME = {5:F2}, MAE = {6:F2}\r\n" +
                                                        "RSR = {7:F2}, RMSD = {8:F2}",
                                                        new object[] {stats[i].Slope,   stats[i].Intercept,   stats[i].R2,
                                                                  stats[i].n,   stats[i].NSE, stats[i].ME,
                                                                  stats[i].MAE, stats[i].RSR, stats[i].RMSE});
                    equation.colour = equationColours[i];
                    equation.leftAlign = true;
                    equation.textRotation = 0;
                    equation.x = double.MinValue;
                    equation.y = double.MinValue;
                    annotations.Add(equation);
                }
            }
        }

    }
}
