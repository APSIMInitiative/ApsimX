using System;
using APSIM.Numerics;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using Newtonsoft.Json;

namespace Models.WaterModel
{
    /// <summary>
    /// Water table is the depth (in mm) below the ground surface of the first layer which is above saturation.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(WaterBalance))]
    public class WaterTableModel : Model, IWaterCalculation
    {
        /// <summary>The water movement model.</summary>
        [Link]
        private WaterBalance waterBalance = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical physical = null;

        /// <summary>Depth of water table (mm)</summary>
        [JsonIgnore]
        public double Depth { get; private set; }

        /// <summary>
        /// Calculate water table depth and store in model.
        /// </summary>
        /// <param name="swmm">soil water in mm</param>
        /// <returns>null</returns>
        public void Calculate(double[] swmm)
        {
            double[] thickness = physical.Thickness;
            double[] dul = MathUtilities.Multiply(physical.DUL, thickness);
            double[] sat = MathUtilities.Multiply(physical.SAT, thickness);
            Depth = CalculateDepth(thickness, swmm, dul, sat);
        }

        private static double CalculateDepth(double[] thickness, double[] swmm, double[] dul, double[] sat)
        {
            // Find the first saturated layer
            int sat_layer = -1;
            for (int i = 0; i < swmm.Length; i++)
            {
                // Find the first layer that is above saturation or really close to it.
                if (MathUtilities.FloatsAreEqual(swmm[i], sat[i]))
                {
                    sat_layer = i;
                    break;
                }
            }

            // No saturated layer means no water table
            if (sat_layer == -1)
            {
                //set the depth of watertable to the total depth of the soil profile
                return MathUtilities.Sum(thickness);
            }
            // Do the calculation of the water table if the fully saturated layer is not the top layer AND
            // the layer above the fully saturated layer is saturated.
            else if (sat_layer > 0 && SaturatedFraction(sat_layer, swmm, dul, sat) >= 0.999999 &&
                                    SaturatedFraction(sat_layer - 1, swmm, dul, sat) > 0.0)
            {
                // layer above is over dul
                double bottom_depth = MathUtilities.Sum(thickness, 0, sat_layer, 0.0);
                double saturated = SaturatedFraction(sat_layer - 1, swmm, dul, sat) * thickness[sat_layer - 1];
                return bottom_depth - saturated;
            }
            else
            {
                //TODO: I think this maybe wrong, saturated_fraction is the fraction of drainable and drainable_capacity
                //      which means that when you multiply it by dlayer you only get the depth of water going from dul to sw_dep.
                //      I think it should be multiplying sw x dlayer or just using sw_dep. This depth is only subtracted from
                //      bottom depth if sw_dep is above dul_dep however because otherwise it is not part of the watertable it
                //      is just water in the layer above the water table.

                double bottom_depth = MathUtilities.Sum(thickness, 0, sat_layer, 0.0);
                //double saturated = (1-SaturatedFraction(sat_layer, soil.Water, DUL, SAT)) * thickness[sat_layer];
                //Depth = bottom_depth - saturated;
                return bottom_depth; // DeanH modified. Bug in original FORTRAN code?
            }
        }

        /// <summary>
        /// Sets the water table.
        /// </summary>
        /// <param name="initialDepth">The initial depth.</param>
        public void Set(double initialDepth)
        {
            double[] Thickness = physical.Thickness;
            double[] SAT = MathUtilities.Multiply(physical.SAT, Thickness);
            double[] DUL = MathUtilities.Multiply(physical.DUL, Thickness);

            double fraction;
            double top = 0.0;
            double bottom = 0.0;

            for (int i = 0; i < waterBalance.Water.Length; i++)
            {
                top = bottom;
                bottom = bottom + physical.Thickness[i];

                if (initialDepth >= bottom)
                {
                    //do nothing;
                }
                else if (initialDepth > top)
                {
                    //! top of water table is in this layer
                    var drainableCapacity = SAT[i] - DUL[i];
                    fraction = (bottom - initialDepth) / (bottom - top);
                    waterBalance.Water[i] = DUL[i] + fraction * drainableCapacity;
                }
                else
                {
                    waterBalance.Water[i] = SAT[i];
                }
            }

            Depth = initialDepth;
        }

        /// <summary>Calculate the saturated fraction for the specified layer index.</summary>
        /// <param name="layerIndex">The layer number</param>
        /// <param name="Water">The water values.</param>
        /// <param name="DUL">The drained upper limit values.</param>
        /// <param name="SAT">The saturation values.</param>
        /// <returns></returns>
        private static double SaturatedFraction(int layerIndex, double[] Water, double[] DUL, double[] SAT)
        {
            double drainable = Water[layerIndex] - DUL[layerIndex];
            double drainableCapacity = SAT[layerIndex] - DUL[layerIndex];
            return MathUtilities.Divide(drainable, drainableCapacity, 0.0);
        }
    }
}
