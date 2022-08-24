using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml;
using APSIM.Shared.Utilities;
using System.Data;
using APSIM.Shared.Documentation;
using Newtonsoft.Json;
using APSIM.Shared.Graphing;
using Graph = APSIM.Shared.Documentation.Graph;
using StandardSeries = APSIM.Shared.Graphing.Series;
using Table = APSIM.Shared.Documentation.Table;

namespace Models.Functions
{
    /// <summary>
    /// This function is calculated from an XY matrix which returns a value for Y 
    /// interpolated from the Xvalue provided.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.XYPairsView")]
    [PresenterName("UserInterface.Presenters.XYPairsPresenter")]
    [Description("Returns a y value from the specified xy maxrix corresponding to the current value of the Xproperty")]
    public class XYPairs : Model, IFunction, IIndexedFunction
    {
        /// <summary>Gets or sets the x.</summary>
        [Description("X")]
        public double[] X { get; set; }

        /// <summary>Gets or sets the y.</summary>
        [Description("Y")]
        public double[] Y { get; set; }

        /// <summary>The name of the x variable. Used in documentation.</summary>
        [Description("Name of X variable (for documentation)")]
        public string XVariableName { get; set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot call Value on XYPairs function. Must be indexed.</exception>
        public double Value(int arrayIndex = -1)
        {
            throw new Exception("Cannot call Value on XYPairs function. Must be indexed.");
        }

        /// <summary>Values the indexed.</summary>
        /// <param name="dX">The d x.</param>
        /// <returns></returns>
        public double ValueIndexed(double dX)
        {
            bool DidInterpolate = false;
            return MathUtilities.LinearInterpReal(dX, X, Y, out DidInterpolate);
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public override IEnumerable<ITag> Document()
        {
            DataTable table = new DataTable(Name);
            // Using the string datatype gives us control over how the numbers
            // are rendered, and allows for empty cells.
            if (XVariableName == null)
                XVariableName = "X";
            table.Columns.Add(XVariableName, typeof(string));
            table.Columns.Add(Parent.Name, typeof(string));
            for (int i = 0; i < Math.Max(X.Length, Y.Length); i++)
            {
                DataRow row = table.NewRow();
                row[0] = i <= X.Length - 1 ? X[i].ToString("F1") : "";
                row[1] = i <= Y.Length - 1 ? Y[i].ToString("F1") : "";
                table.Rows.Add(row);
            }
            yield return new Table(table);

            var series = new APSIM.Shared.Graphing.Series[1];

            // fixme: colour
            series[0] = new LineSeries(Parent.Name, ColourUtilities.ChooseColour(4), false, X, Y, new Line(LineType.Solid, LineThickness.Normal), new Marker(MarkerType.None, MarkerSize.Normal, 1), XVariableName, Name);

            Axis xAxis = new Axis(XVariableName, AxisPosition.Bottom, false, false);
            Axis yAxis = new Axis(Parent.Name, AxisPosition.Left, false, false);

            var legend = new LegendConfiguration(LegendOrientation.Vertical, LegendPosition.TopLeft, true);
            yield return new APSIM.Shared.Documentation.Graph(Parent.Name, FullPath, series, xAxis, yAxis, legend);
        }
    }
}
