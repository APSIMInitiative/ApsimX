namespace Models.WaterModel
{
    using APSIM.Shared.Utilities;
    using Core;
    using Functions;
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// Water table is the depth (in mm) below the ground surface of the first layer which is above saturation.
    /// </summary>
    [Serializable]
    public class WaterTableModel : Model, IFunction
    {
        /// <summary>The water movement model.</summary>
        [Link]
        private WaterBalance soil = null;

        /// <summary>Depth of water table (mm)</summary>
        [XmlIgnore]
        public double Depth { get; private set; }

        /// <summary>Calculate water table depth.</summary>
        public double Value(int arrayIndex = -1)
        {
            double[] Thickness = soil.Properties.Water.Thickness;
            double[] SW = soil.Water;
            double[] SAT = MathUtilities.Multiply(soil.Properties.Water.SAT, Thickness);
            double[] DUL = MathUtilities.Multiply(soil.Properties.Water.DUL, Thickness);


            // Find the first saturated layer
            int sat_layer = -1;
            for (int i = 0; i < SW.Length; i++)
            {
                // Find the first layer that is above saturation or really close to it. 
                if (MathUtilities.FloatsAreEqual(soil.Water[i], SAT[i]))
                {
                    sat_layer = i;
                    break;
                }
            }

            // No saturated layer means no water table
            if (sat_layer == -1)
            {
                //set the depth of watertable to the total depth of the soil profile
                Depth = MathUtilities.Sum(Thickness);
                return Depth;
            }

            // Do the calculation of the water table if the fully saturated layer is not the top layer AND
            // the layer above the fully saturated layer is saturated.
            if (sat_layer > 0 && SaturatedFraction(sat_layer, soil.Water, DUL, SAT) >= 0.999999 &&
                                    SaturatedFraction(sat_layer - 1, soil.Water, DUL, SAT) > 0.0)
            {
                // layer above is over dul
                double bottom_depth = MathUtilities.Sum(Thickness, 0, sat_layer - 1, 0.0);
                double saturated = SaturatedFraction(sat_layer - 1, soil.Water, DUL, SAT) * Thickness[sat_layer - 1];
                Depth = (bottom_depth - saturated);
            }
            else
            {
                //TODO: I think this maybe wrong, saturated_fraction is the fraction of drainable and drainable_capacity
                //      which means that when you multiply it by dlayer you only get the depth of water going from dul to sw_dep.
                //      I think it should be multiplying sw x dlayer or just using sw_dep. This depth is only subtracted from
                //      bottom depth if sw_dep is above dul_dep however because otherwise it is not part of the watertable it
                //      is just water in the layer above the water table.

                double bottom_depth = MathUtilities.Sum(Thickness, 0, sat_layer, 0.0);
                //double saturated = (1-SaturatedFraction(sat_layer, soil.Water, DUL, SAT)) * Thickness[sat_layer];
                //Depth = bottom_depth - saturated;
                Depth = bottom_depth; // DeanH modified. Bug in original FORTRAN code?
            }

            return Depth;
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
