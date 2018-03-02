// ----------------------------------------------------------------------
// <copyright file="SoilTemperatureWeightedFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// # [Name]
    /// Returns the temperature of the surface soil layer with the weighting: " +
    /// 0.25*DayBeforeYesterday + 0.5*Yesterday + 0.25*Today
    /// </summary>
    [Serializable]
    [Description("Returns the temperature of the surface soil layer with the weighting: " +
                 "0.25*DayBeforeYesterday + 0.5*Yesterday + 0.25*Today")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilTemperatureWeightedFunction : BaseFunction
    {
        /// <summary>The value for the day before yesterday</summary>
        private double dayBeforeYesterday = 0;

        /// <summary>The value yesterday</summary>
        private double yesterday = 0;

        /// <summary>The value today</summary>
        private double today = 0;

        /// <summary>Gets or sets the xy pairs.</summary>
        [ChildLink]
        private XYPairs xyPairs = null;   // Temperature effect on Growth Interpolation Set

        /// <summary>The maxt_soil_surface</summary>
        [Units("oC")]
        double maxt_soil_surface { get; set; }  //Fixme.  Need to connect to soil temp model when it is working

        /// <summary>constructor</summary>
        public SoilTemperatureWeightedFunction()
        {
            maxt_soil_surface = 15;
        }

        /// <summary>EventHandler for OnPrepare.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            dayBeforeYesterday = yesterday;
            yesterday = today;
            today = maxt_soil_surface;
        }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            double weightedTemperature = 0.25 * dayBeforeYesterday + 0.5 * yesterday + 0.25 * today;
            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + xyPairs.ValueIndexed(weightedTemperature));
            return new double[] { xyPairs.ValueIndexed(weightedTemperature) };
        }
    }
}
   
