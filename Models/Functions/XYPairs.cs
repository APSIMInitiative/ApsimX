using System;
using System.Collections.Generic;
using System.Data;
using APSIM.Shared.Documentation;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Utilities;
using Newtonsoft.Json;
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
    [Description("Returns the corresponding Y value for a given X value, based on the line shape defined by the specified XY matrix.")]
    public class XYPairs : Model, IFunction, IIndexedFunction
    {
        /// <summary>Gets or sets the x.</summary>
        [Description("X")]
        [Display]
        public double[] X { get; set; }

        /// <summary>Gets or sets the y.</summary>
        [Description("Y")]
        [Display]
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

                const double tolerance = 1e-9;
                const int minDecimalPlaces = 1; //minimum 1 decimal place
                const int maxDecimalPlaces = 6; //max 5 decimal places
                string xDigits = "F" + minDecimalPlaces.ToString();
                string yDigits = "F" + minDecimalPlaces.ToString();
                double xDeci = Math.Round(X[i], minDecimalPlaces);
                double yDeci = Math.Round(Y[i], minDecimalPlaces);
                for (int j = minDecimalPlaces + 1; j <= maxDecimalPlaces; j++)
                {
                    if (Math.Abs(Math.Round(X[i], j) - xDeci) > tolerance)
                    {
                        xDigits = "F" + j.ToString();
                        xDeci = Math.Round(X[i], j);
                    }
                    if (Math.Abs(Math.Round(Y[i], j) - yDeci) > tolerance)
                    {
                        yDigits = "F" + j.ToString();
                        yDeci = Math.Round(Y[i], j);
                    }
                }

                row[0] = i <= X.Length - 1 ? X[i].ToString(xDigits) : "";
                row[1] = i <= Y.Length - 1 ? Y[i].ToString(yDigits) : "";
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
