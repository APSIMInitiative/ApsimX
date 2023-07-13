using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>
    /// This Function calculates a mean daily temperature from Max and Min weighted toward Max according to the specified MaximumTemperatureWeighting factor.  This is then passed into the XY matrix as the x property and the function returns the y value
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class WeightedTemperatureFunction : Model, IFunction
    {
        #region Class Data Members
        /// <summary>Gets the xy pairs.</summary>
        /// <value>The xy pairs.</value>
        [Link(Type = LinkType.Child, ByName = true)]
        private XYPairs XYPairs = null;   // Temperature effect on Growth Interpolation Set

        /// <summary>The maximum temperature weighting</summary>
        [Description("MaximumTemperatureWeighting")]
        public double MaximumTemperatureWeighting { get; set; }

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        #endregion

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double Tav = MaximumTemperatureWeighting * MetData.MaxT + (1 - MaximumTemperatureWeighting) * MetData.MinT;
            return XYPairs.ValueIndexed(Tav);
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Paragraph($"*{Name}* is calculated as a function of daily min and max temperatures, these are weighted toward max temperature according to the specified MaximumTemperatureWeighting factor. A value equal to 1.0 means it will use max temperature, a value of 0.5 means average temperature.");
            yield return new Paragraph($"*MaximumTemperatureWeighting = {MaximumTemperatureWeighting}*");
            // fixme - the graph and table should be next to each other.

            yield return CreateGraph();
            // yield return new GraphAndTable(XYPairs, string.Empty, "Average temperature (oC)", Name, indent));
        }

        private APSIM.Shared.Documentation.Graph CreateGraph()
        {
            // fixme - this is basically identical to what we've got in the linear interp code.
            var series = new APSIM.Shared.Graphing.Series[1];
            string xName = "Weighted air temperature (oC)";
            string yName = Name;

            series[0] = new APSIM.Shared.Graphing.LineSeries("Weighted temperature value",
                ColourUtilities.ChooseColour(4),
                false,
                XYPairs.X,
                XYPairs.Y,
                new APSIM.Shared.Graphing.Line(APSIM.Shared.Graphing.LineType.Solid, APSIM.Shared.Graphing.LineThickness.Normal),
                new APSIM.Shared.Graphing.Marker(APSIM.Shared.Graphing.MarkerType.None, APSIM.Shared.Graphing.MarkerSize.Normal, 1),
                xName,
                yName
            );
            var xAxis = new APSIM.Shared.Graphing.Axis(xName, APSIM.Shared.Graphing.AxisPosition.Bottom, false, false);
            var yAxis = new APSIM.Shared.Graphing.Axis(yName, APSIM.Shared.Graphing.AxisPosition.Left, false, false);
            var legend = new APSIM.Shared.Graphing.LegendConfiguration(APSIM.Shared.Graphing.LegendOrientation.Vertical, APSIM.Shared.Graphing.LegendPosition.TopLeft, true);
            return new APSIM.Shared.Documentation.Graph(Name, FullPath, series, xAxis, yAxis, legend);
        }
    }
}
