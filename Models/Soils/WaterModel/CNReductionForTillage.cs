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
    [ValidParent(typeof(WaterBalance))]
    public class CNReductionForTillage : Model, IFunction
    {
        // --- Links -------------------------------------------------------------------------

        /// <summary>The water movement model.</summary>
        [Link]
        private WaterBalance soil = null;

        // --- Privates ----------------------------------------------------------------------

        /// <summary>The cumulated amount of rainfall since the tillage date.</summary>
        private double cumWaterSinceTillage = 0;

        // --- Settable properties------------------------------------------------------------

        /// <summary>The amount of rain required to cease curve number reduction.</summary>
        public double tillageCnCumWater { get; set; }

        /// <summary>The amount to reduce curve number by the day after tillage (0-100).</summary>
        public double tillageCnRed { get; set; }

        private RunoffModel runoffModel;

        // --- Outputs -----------------------------------------------------------------------

        /// <summary>Returns the value to subtract from curve number due to tillage.</summary>
        public double Value(int arrayIndex = -1)
        {
            IModel parent = this.Parent;

            if (parent is RunoffModel)
            {
                runoffModel = parent as RunoffModel;

                cumWaterSinceTillage = runoffModel.CumWaterSinceTillage;
                tillageCnCumWater = runoffModel.TillageCnCumWater;
                tillageCnRed = runoffModel.TillageCnRed;
            }
            else
            {
                throw new Exception("Parent model of CNReductionForTillage must be RunoffModel.");
            }

            if (tillageCnCumWater > 0.0)
            {
                // Tillage Reduction is biggest (tillageCnRed value) straight after Tillage 
                // and gets smaller and becomes 0 when reaches tillageCnCumWater.

                // We want the opposite fraction (hence, 1 - x). 
                // If cumWaterSinceTillage = 0, tillage_reduction = tillageCnRed.
                // If cumWaterSinceTillage = 0.3 x tillageCnCumWater, tillage_reduction = 0.7 x tillageCnRed.
                // If cumWaterSinceTillage >= tillageCnCumWater, tillage_reduction = 0 (there won't be a continued reduction).

                double tillage_fract = 1 - Math.Min(1, MathUtilities.Divide(cumWaterSinceTillage, tillageCnCumWater, 0.0));
                double tillage_reduction = tillage_fract * tillageCnRed;
                return tillage_reduction;
            }
            else
                return 0;
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
                cumWaterSinceTillage += soil.PotentialRunoff;
        }
    }
}
