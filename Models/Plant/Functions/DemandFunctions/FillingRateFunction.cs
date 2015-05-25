using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions.DemandFunctions
{
    /// <summary>
    /// Partition fraction demand function
    /// </summary>
    [Serializable]
    [Description("Filling rate is calculated from grain number, a maximum mass to be filled and the duration of the filling process.")]
    public class FillingRateFunction : Model, IFunction
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
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                return (PotentialSizeIncrement.Value / FillingDuration.Value) * ThermalTime.Value * NumberFunction.Value;
            }
        }

    }
}


