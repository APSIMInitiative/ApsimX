using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml;
using APSIM.Shared.Utilities;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns a y value from the specified xy matrix corresponding to the current value of the Xproperty
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.XYPairsView")]
    [PresenterName("UserInterface.Presenters.XYPairsPresenter")]
    [Description("Returns a y value from the specified xy maxrix corresponding to the current value of the Xproperty")]
    public class XYPairs : Model, IFunction
    {
        /// <summary>Gets or sets the x.</summary>
        /// <value>The x.</value>
        [Description("X")]
        public double[] X { get; set; }
        /// <summary>Gets or sets the y.</summary>
        /// <value>The y.</value>
        [Description("Y")]
        public double[] Y { get; set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot call Value on XYPairs function. Must be indexed.</exception>
        public double Value { get { throw new Exception("Cannot call Value on XYPairs function. Must be indexed."); } }

        /// <summary>Values the indexed.</summary>
        /// <param name="dX">The d x.</param>
        /// <returns></returns>
        public double ValueIndexed(double dX)
        {
            bool DidInterpolate = false;
            return MathUtilities.LinearInterpReal(dX, X, Y, out DidInterpolate);
        }
    }
}
