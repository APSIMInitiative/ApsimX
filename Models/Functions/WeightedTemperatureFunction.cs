using System;
using APSIM.Services.Documentation;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Interfaces;
using APSIM.Shared.Utilities;
using System.Linq;

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
            yield return new Paragraph($"*{Name} is calculated as a function of daily min and max temperatures, these are weighted toward max temperature according to the specified MaximumTemperatureWeighting factor. A value equal to 1.0 means it will use max temperature, a value of 0.5 means average temperature.*");
            yield return new Paragraph($"*aximumTemperatureWeighting = {MaximumTemperatureWeighting}*");
            // fixme - the graph and table should be next to each other.
            yield return XYPairs.ToTable();
            yield return CreateGraph();
            // yield return new GraphAndTable(XYPairs, string.Empty, "Average temperature (oC)", Name, indent));
        }

        private APSIM.Services.Documentation.Graph CreateGraph(uint indent = 0)
        {
            // fixme - this is basically identical to what we've got in the linear interp code.
            var series = new APSIM.Services.Graphing.Series[1];
            series[0] = new APSIM.Services.Graphing.Series("Weighted temperature value", ColourUtilities.ChooseColour(4), false, XYPairs.X, XYPairs.Y);
            var axes = new APSIM.Services.Graphing.Axis[2];
            axes[0] = new APSIM.Services.Graphing.Axis("Average Temperature (°C)", APSIM.Services.Graphing.AxisPosition.Bottom, false, false);
            axes[1] = new APSIM.Services.Graphing.Axis(Name, APSIM.Services.Graphing.AxisPosition.Left, false, false);
            var legend = new APSIM.Services.Graphing.LegendConfiguration(APSIM.Services.Graphing.LegendOrientation.Vertical, APSIM.Services.Graphing.LegendPosition.TopLeft, true);
            return new APSIM.Services.Documentation.Graph(series, axes, legend);
        }
    }
}
