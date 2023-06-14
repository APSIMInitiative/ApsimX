using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;

namespace Models.WaterModel
{

    /// <summary>
    /// Implements the curve number reduction caused by tillage.
    /// Mark Littleboy's tillage effect on runoff (used in PERFECT v2.0)
    /// Littleboy, Cogle, Smith, Yule and Rao(1996).  Soil management and production
    /// of alfisols in the SAT's I. Modelling the effects of soil management on runoff
    /// and erosion.Aust.J.Soil Res. 34: 91-102.
    /// </summary>
    [Serializable]
    public class CNReductionForTillage : Model, IFunction
    {
        // --- Links -------------------------------------------------------------------------

        /// <summary>Link to the weather component.</summary>
        [Link]
        private IWeather weather = null;

        // --- Privates ----------------------------------------------------------------------

        /// <summary>The cumulated amount of rainfall since the tillage date.</summary>
        private double cumWaterSinceTillage = 0;

        // --- Settable properties------------------------------------------------------------

        /// <summary>The amount of rain required to cease curve number reduction.</summary>
        public double tillageCnCumWater { get; set; }

        /// <summary>The amount to reduce curve number by the day after tillage (0-100).</summary>
        public double tillageCnRed { get; set; }

        // --- Outputs -----------------------------------------------------------------------

        /// <summary>Returns the value to subtract from curve number due to tillage.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (tillageCnCumWater > 0.0)
            {
                // Tillage Reduction is biggest (tillageCnRed value) straight after Tillage 
                // and gets smaller and becomes 0 when reaches tillageCnCumWater.
                double tillage_fract = MathUtilities.Divide(cumWaterSinceTillage, tillageCnCumWater, 0.0);
                double tillage_reduction = tillageCnRed * tillage_fract;
                return tillage_reduction;
            }
            else
                return 0;
        }

        // --- Event handlers ----------------------------------------------------------------

        /// <summary>
        /// Called when a tillage event has occurred.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="tillageType">The type of tillage performed.</param>
        [EventSubscribe("TillageCompleted")]
        private void OnTillageCompleted(object sender, Soils.TillageType tillageType)
        {
            tillageCnCumWater = tillageType.cn_rain;
            tillageCnRed = tillageType.cn_red;
            cumWaterSinceTillage = 0;
        }

        /// <summary>
        /// Called at the start of every day.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            // If our cumulated rainfall has reached our target then reset everything
            // so that there won't be a continued reduction in curve number.
            if (cumWaterSinceTillage > tillageCnCumWater)
            {
                cumWaterSinceTillage = 0;
                tillageCnCumWater = 0;
                tillageCnRed = 0;
            }

            if (tillageCnCumWater > 0)
                cumWaterSinceTillage += weather.Rain;
        }
    }
}
