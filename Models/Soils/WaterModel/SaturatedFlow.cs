using System;
using APSIM.Numerics;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using Newtonsoft.Json;

namespace Models.WaterModel
{

    /// <summary>
    /// When water content in any layer is below SAT but above DUL, a fraction of the water drains to the next
    /// deepest layer each day.
    ///
    /// Flux = SWCON x (SW - DUL)
    ///
    /// Infiltration or water movement into any layer that exceeds the saturation capacity of the layer automatically
    /// cascades to the next layer.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(WaterBalance))]
    public class SaturatedFlowModel : Model, IWaterCalculation
    {
        /// <summary>The water movement model.</summary>
        [Link]
        private WaterBalance waterBalance = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical physical = null;

        /// <summary>Calculated Flux</summary>
        [Link]
        public double[] Flux {get; private set;}

        /// <summary>Amount of water (mm) backed up.</summary>
        [JsonIgnore]
        public double backedUpSurface { get; private set; }

        /// <summary>
        /// Perform the movement of water.
        /// </summary>
        /// <param name="swmm">soil water depth</param>
        /// <returns>A double[] of the calcuted flux value from saturated flow</returns>
        public void Calculate(double[] swmm)
        {
            double[] thickness = physical.Thickness;
            double[] dul = MathUtilities.Multiply(physical.DUL, thickness);
            double[] sat = MathUtilities.Multiply(physical.SAT, thickness);
            double[] KS = physical.KS;

            var values = CalculateFlux(swmm, dul, sat, waterBalance.SWCON, KS);
            backedUpSurface = values.Item1;
            Flux = values.Item2;
        }

        /// <summary>Perform the movement of water and return </summary>
        private static (double, double[]) CalculateFlux(double[] swmm, double[] dul, double[] sat, double[] swcon, double[] soilKS)
        {
            double backedUpSurface = 0.0;
            double w_in = 0.0;   // water coming into layer (mm)
            double w_out;        // water going out of layer (mm)
            double[] flux = new double[swmm.Length];
            double[] newSWmm = new double[swmm.Length];
            for (int i = 0; i < swmm.Length; i++)
            {
                double w_tot = swmm[i] + w_in;

                // Calculate EXCESS Amount (above SAT)

                // get excess water above saturation & then water left
                // to drain between sat and dul.  Only this water is
                // subject to swcon. The excess is not - treated as a
                // bucket model. (mm)

                double w_excess;               // amount above saturation(overflow)(mm)
                if (w_tot > sat[i])
                {
                    w_excess = w_tot - sat[i];
                    w_tot = sat[i];
                }
                else
                    w_excess = 0.0;

                // Calculate water draining by gravity (mm) (between SAT and DUL)
                double w_drain;
                if (w_tot > dul[i])
                    w_drain = (w_tot - dul[i]) * swcon[i];
                else
                    w_drain = 0.0;

                // Calculate EXCESS Flow and DRAIN Flow (combined into Flux)
                // if there is EXCESS Amount,
                if (w_excess > 0.0)
                {
                    if (soilKS == null || soilKS.Length == 0)
                    {
                        //! all this excess goes on down
                        w_out = w_excess + w_drain;
                        flux[i] = w_out;
                    }
                    else
                    {
                        // Calculate amount of water to backup and push down
                        // Firstly top up this layer (to saturation)
                        double add = Math.Min(w_excess, w_drain);
                        w_excess = w_excess - add;
                        newSWmm[i] = sat[i] - w_drain + add;

                        // partition between flow back up and flow down
                        // 'excessDown' is the amount above saturation(overflow) that moves down (mm)
                        double excess_down = Math.Min(soilKS[i] - w_drain, w_excess);
                        double backup = w_excess - excess_down;

                        w_out = excess_down + w_drain;
                        flux[i] = w_out;

                        // Starting from the layer above the current layer,
                        // Move up to the surface, layer by layer and use the
                        // backup to fill the space still remaining between
                        // the new sw_dep (that you calculated on the way down)
                        // and sat for that layer. Once the backup runs out
                        // it will keep going but you will be adding 0.
                        if (i > 0)
                        {
                            for (int j = i - 1; j >= 0; j--)
                            {
                                flux[j] = flux[j] - backup;
                                add = Math.Min(sat[j] - newSWmm[j], backup);
                                newSWmm[j] = newSWmm[j] + add;
                                backup = backup - add;
                            }
                        }

                        backedUpSurface = backedUpSurface + backup;
                    }
                }
                else
                {
                    // there is no EXCESS Amount so only do DRAIN Flow
                    w_out = w_drain;
                    flux[i] = w_drain;
                    newSWmm[i] = swmm[i] + w_in - w_out;
                }

                // drainage out of this layer goes into next layer down
                w_in = w_out;
            }

            return (backedUpSurface, flux);
        }
    }
}
