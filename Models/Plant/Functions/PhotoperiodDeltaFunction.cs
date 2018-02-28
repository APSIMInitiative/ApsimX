// ----------------------------------------------------------------------
// <copyright file="PhotoperiodDeltaFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using System;

    /// <summary>
    /// # [Name]
    /// Returns the difference between today's and yesterday's photoperiods in hours.
    /// </summary>
    [Serializable]
    [Description("Returns the difference between today's and yesterday's photoperiods in hours.")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PhotoperiodDeltaFunction : BaseFunction
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];

        /// <summary>The met data</summary>
        [Link]
        private IWeather weatherData = null;

        /// <summary>The clock</summary>
        [Link]
        private Clock clockModel = null;

        /// <summary>The twilight</summary>
        [Description("Twilight")]
        public double Twilight = 0;

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            double photoperiodToday = MathUtilities.DayLength(clockModel.Today.DayOfYear, Twilight, weatherData.Latitude);
            double photoperiodYesterday = MathUtilities.DayLength(clockModel.Today.DayOfYear - 1, Twilight, weatherData.Latitude);
            double photoperiodDelta = photoperiodToday - photoperiodYesterday;
            returnValue[0] = photoperiodDelta;
            return returnValue;
        }
    }
}
