// ----------------------------------------------------------------------
// <copyright file="FillingRateFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions.DemandFunctions
{
    using Models.Core;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// # [Name]
    /// Filling rate is calculated from grain number, a maximum mass to be filled and the duration of the filling process.
    /// </summary>
    [Serializable]
    public class FillingRateFunction : BaseFunction
    {
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
            double[] returnValue = new double[] { (PotentialSizeIncrement.Value() / FillingDuration.Value()) * ThermalTime.Value() * NumberFunction.Value() };
            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + returnValue[0]);
            return returnValue;
        }

    }
}


