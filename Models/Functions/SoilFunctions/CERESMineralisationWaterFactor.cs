using System;
using System.Collections.Generic;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Soils;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>Water factor for daily soil organic matter mineralisation</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval fraction of NH4 nitrified.
    [Serializable]
    [Description("Mineralisation Water Factor from CERES-Maize")]
    public class CERESMineralisationWaterFactor : Model, IFunction
    {

        [Link]
        Soil soil = null;

        [Link]
        ISoilWater soilwater = null;

        [Link]
        IPhysical physical = null;

        /// <summary>Boolean to indicate sandy soil</summary>
        private bool isSand = false;

        /// <summary>
        /// Handler method for the start of simulation event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            if (soil.SoilType != null)
                if (soil.SoilType.ToLower() == "sand")
                    isSand = true;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES mineralisation water factor Model");

            double[] SW = soilwater.SW;
            double[] LL15 = physical.LL15;
            double[] DUL = physical.DUL;
            double[] SAT = physical.SAT;

            double WF = 0;
            if (SW[arrayIndex] < LL15[arrayIndex])
                WF = 0;
            else if (SW[arrayIndex] < DUL[arrayIndex])
                    if (isSand)
                        WF = 0.05+0.95*Math.Min(1, 2 * MathUtilities.Divide(SW[arrayIndex] - LL15[arrayIndex], DUL[arrayIndex] - LL15[arrayIndex],0.0));
                    else
                        WF = Math.Min(1, 2 * MathUtilities.Divide(SW[arrayIndex] - LL15[arrayIndex], DUL[arrayIndex] - LL15[arrayIndex],0.0));
            else
                WF = 1 - 0.5 * MathUtilities.Divide(SW[arrayIndex] - DUL[arrayIndex], SAT[arrayIndex] - DUL[arrayIndex],0.0);

            return WF;
        }
    }
}