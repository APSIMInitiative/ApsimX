using System;
using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Returns the temperature of the surface soil layer with the weighting: " +
    /// 0.25*DayBeforeYesterday + 0.5*Yesterday + 0.25*Today
    /// </summary>
    [Serializable]
    [Description("Returns the temperature of the surface soil layer with the weighting: " +
                 "0.25*DayBeforeYesterday + 0.5*Yesterday + 0.25*Today")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilTemperatureWeightedFunction : Model, IFunction
    {
        /// <summary>constructor</summary>
        public SoilTemperatureWeightedFunction()
        {
            maxt_soil_surface = 15;
        }
        #region Class Data Members

        /// <summary>The day before yesterday</summary>
        private double DayBeforeYesterday = 0;
        /// <summary>The yesterday</summary>
        private double Yesterday = 0;
        /// <summary>The today</summary>
        private double Today = 0;
        /// <summary>Gets or sets the xy pairs.</summary>
        /// <value>The xy pairs.</value>
        [Link(Type = LinkType.Child, ByName = true)]
        private XYPairs XYPairs = null;   // Temperature effect on Growth Interpolation Set

        /// <summary>The maxt_soil_surface</summary>
        [Units("oC")]
        double maxt_soil_surface { get; set; }  //Fixme.  Need to connect to soil temp model when it is working

        #endregion

        /// <summary>EventHandler for OnPrepare.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            DayBeforeYesterday = Yesterday;
            Yesterday = Today;
            Today = maxt_soil_surface;
        }



        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double WeightedTemperature = 0.25 * DayBeforeYesterday + 0.5 * Yesterday + 0.25 * Today;
            return XYPairs.ValueIndexed(WeightedTemperature);
        }
    }
}

