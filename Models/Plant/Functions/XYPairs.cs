// ----------------------------------------------------------------------
// <copyright file="XYPairs.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;

    /// <summary>
    /// Returns a y value from the specified xy matrix corresponding to the current value of the Xproperty
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.XYPairsView")]
    [PresenterName("UserInterface.Presenters.XYPairsPresenter")]
    [Description("Returns a y value from the specified xy maxrix corresponding to the current value of the Xproperty")]
    public class XYPairs : BaseFunction
    {
        /// <summary>Gets or sets the x.</summary>
        [Description("X")]
        public double[] X { get; set; }

        /// <summary>Gets or sets the y.</summary>
        [Description("Y")]
        public double[] Y { get; set; }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            throw new Exception("Cannot call Value on XYPairs function. Must be indexed.");
        }

        /// <summary>Values the indexed.</summary>
        /// <param name="dX">The d x.</param>
        public double ValueIndexed(double dX)
        {
            bool DidInterpolate = false;
            return MathUtilities.LinearInterpReal(dX, X, Y, out DidInterpolate);
        }
    }
}
