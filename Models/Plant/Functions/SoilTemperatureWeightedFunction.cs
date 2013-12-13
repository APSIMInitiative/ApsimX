using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Returns the temperature of the surface soil layer with the weighting: " +
                 "0.25*DayBeforeYesterday + 0.5*Yesterday + 0.25*Today")]
    public class SoilTemperatureWeightedFunction : Function
    {
        #region Class Data Members

        private double DayBeforeYesterday = 0;
        private double Yesterday = 0;
        private double Today = 0;
        private XYPairs XYPairs { get; set; }   // Temperature effect on Growth Interpolation Set

        [Units("oC")]
        double maxt_soil_surface = 15;  //Fixme.  Need to connect to soil temp model when it is working

        #endregion

        /// <summary>
        /// EventHandler for OnPrepare.
        /// </summary>
        [EventSubscribe("StartOfDay")]
        private void OnPrepare(object sender, EventArgs e)
        {
            DayBeforeYesterday = Yesterday;
            Yesterday = Today;
            Today = maxt_soil_surface;
        }


        
        [Units("deg.day")]
        public override double Value
        {
            get
            {
                double WeightedTemperature = 0.25 * DayBeforeYesterday + 0.5 * Yesterday + 0.25 * Today;
                return XYPairs.ValueIndexed(WeightedTemperature);
            }
        }
    }
}
   
