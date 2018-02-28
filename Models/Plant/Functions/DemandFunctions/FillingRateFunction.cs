// ----------------------------------------------------------------------
// <copyright file="FillingRateFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions.DemandFunctions
{
    using Models.Core;
    using System;

    /// <summary>
    /// # [Name]
    /// Filling rate is calculated from grain number, a maximum mass to be filled and the duration of the filling process.
    /// </summary>
    [Serializable]
    public class FillingRateFunction : BaseFunction
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];

        /// <summary>The partition fraction</summary>
        [Link]
        IFunction FillingDuration = null;

        /// <summary>The filling rate</summary>
        [Link]
        [Units("grains/m2")]
        IFunction NumberFunction = null;

        /// <summary>The arbitrator</summary>
        [Link]
        IFunction ThermalTime = null;

        /// <summary>The maximum weight or maximum amount of N incremented for individual grains in a given phase</summary>
        [Link]
        [Units("g/kernal")]
        IFunction PotentialSizeIncrement = null;

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            returnValue[0] = (PotentialSizeIncrement.Value() / FillingDuration.Value()) * ThermalTime.Value() * NumberFunction.Value();
            return returnValue;
        }

    }
}


