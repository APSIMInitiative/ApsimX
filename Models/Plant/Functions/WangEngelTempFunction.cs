// ----------------------------------------------------------------------
// <copyright file="WangEngelTempFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using Models.Interfaces;
    using System;

    /// <summary>
    /// # [Name]
    /// A function that adds values from child functions
    /// </summary>
    [Serializable]
    [Description("Calculates relative temperature response")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]

    public class WangEngelTempFunction : BaseFunction
    {
        /// <summary>The met data</summary>
        [Link]
        private IWeather weatherData = null;

        /// <summary>Minimum Temperature.</summary>
        [Description("Minimum Temperature")]
        public double MinTemp { get; set; }

        /// <summary>Optimum Temperature</summary>
        [Description("Optimum Temperature")]
        public double OptTemp { get; set; }

        /// <summary>Maximum Temperature</summary>
        [Description("Maximum Temperature")]
        public double MaxTemp { get; set; }

        /// <summary>The met data</summary>
        [Description("Reference Temperature (MinTemp<RefTemp<MaxTemp)")]
        public double RefTemp { get; set; }

        /// <summary>The maximum temperature weighting</summary>
        [Description("Maximum Temperature Weighting")]
        public double MaximumTemperatureWeighting { get; set; }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            double RelEff = 0.0;
            double RelEffRefTemp = 1.0;
            double p = 0.0;
            double Tav = MaximumTemperatureWeighting * weatherData.MaxT + (1 - MaximumTemperatureWeighting) * weatherData.MinT;

            if ((Tav > MinTemp) && (Tav < MaxTemp))
            {
                p = Math.Log(2.0) / Math.Log((MaxTemp - MinTemp) / (OptTemp - MinTemp));
                RelEff = (2 * Math.Pow(Tav - MinTemp, p) * Math.Pow(OptTemp - MinTemp, p) - Math.Pow(Tav - MinTemp, 2 * p)) / Math.Pow(OptTemp - MinTemp, 2 * p);
            }

            if ((RefTemp > MinTemp) && (RefTemp < MaxTemp))
            {
                p = Math.Log(2.0) / Math.Log((MaxTemp - MinTemp) / (OptTemp - MinTemp));
                RelEffRefTemp = (2 * Math.Pow(RefTemp - MinTemp, p) * Math.Pow(OptTemp - MinTemp, p) - Math.Pow(RefTemp - MinTemp, 2 * p)) / Math.Pow(OptTemp - MinTemp, 2 * p);
            }

            return new double[] { RelEff / RelEffRefTemp };
        }
    }
}