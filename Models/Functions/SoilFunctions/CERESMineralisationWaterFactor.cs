using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Soils;

namespace Models.Functions
{
    /// <summary>Water factor for daily soil organic matter mineralisation</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval fraction of NH4 nitrified.
    [Serializable]
    [Description("Mineralisation Water Factor from CERES-Maize")]
    public class CERESMineralisationWaterFactor : Model, IFunction
    {
        private double[] wf;

        [Link]
        Soil soil = null;

        [Link]
        Water water = null;

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
        /// <param name="arrayIndex">The index to return.</param>
        public double Value(int arrayIndex = -1)
        {
            if (wf == null)
                return 0;
            else
                return wf[arrayIndex];
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        [EventSubscribe("WaterChanged")]
        public void OnWaterChanged(object sender, EventArgs e)
        {
            double[] SW = water.Volumetric;
            double[] LL15 = physical.LL15;
            double[] DUL = physical.DUL;
            double[] SAT = physical.SAT;
            if (wf == null)
                wf = new double[SW.Length];
            for (int i = 0; i < SW.Length; i++)
            {
                if (SW[i] < LL15[i])
                    wf[i] = 0;
                else if (SW[i] < DUL[i])
                {
                    if (isSand)
                        wf[i] = 0.05 + 0.95 * Math.Min(1, 2 * MathUtilities.Divide(SW[i] - LL15[i], DUL[i] - LL15[i], 0.0));
                    else
                        wf[i] = Math.Min(1, 2 * MathUtilities.Divide(SW[i] - LL15[i], DUL[i] - LL15[i], 0.0));
                }
                else
                    wf[i] = 1 - 0.5 * MathUtilities.Divide(SW[i] - DUL[i], SAT[i] - DUL[i], 0.0);
            }
        }
    }
}