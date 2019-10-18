namespace Models.Soils.Standardiser
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Linq;

    /// <summary>Convert soil units to APSIM standard.</summary>
    public class SoilUnits
    {
        /// <summary>Convert soil units to APSIM standard.</summary>
        /// <param name="soil">The soil.</param>
        public static void Convert(Soil soil)
        {
            // Convert all samples.
            var samples = Apsim.Children(soil, typeof(Sample)).Cast<Sample>().ToArray();
            foreach (Sample sample in samples)
            {
                // Convert sw units to volumetric.
                if (MathUtilities.ValuesInArray(sample.SW))
                    sample.SW = SWVolumetric(sample, soil);
                sample.SWUnits = Sample.SWUnitsEnum.Volumetric;

                // Convert OC to total (%)
                if (MathUtilities.ValuesInArray(sample.OC))
                    sample.OC = OCTotalPercent(sample.OC, sample.OCUnits);
                sample.OCUnits = Sample.OCSampleUnitsEnum.Total;

                // Convert PH to water.
                if (MathUtilities.ValuesInArray(sample.PH))
                    sample.PH = PHWater(sample.PH, sample.PHUnits);
                sample.PHUnits = Sample.PHSampleUnitsEnum.Water;
            }
        }

        /// <summary>Calculates volumetric soil water for the given sample.</summary>
        /// <param name="sample">The sample.</param>
        /// <param name="soil">The soil.</param>
        /// <returns>Volumetric water (mm/mm)</returns>
        private static double[] SWVolumetric(Sample sample, Soil soil)
        {
            if (sample.SWUnits == Sample.SWUnitsEnum.Volumetric || sample.SW == null)
                return sample.SW;
            else
            {
                // convert the numbers
                if (sample.SWUnits == Sample.SWUnitsEnum.Gravimetric)
                {
                    double[] bd = Layers.BDMapped(soil, sample.Thickness);
                    return MathUtilities.Multiply(sample.SW, bd);
                }
                else
                    return MathUtilities.Divide(sample.SW, sample.Thickness); // from mm to mm/mm
            }
        }

        /// <summary>Converts OC to total %</summary>
        /// <param name="oc">The oc.</param>
        /// <param name="units">The current units.</param>
        /// <returns>The converted values</returns>
        private static double[] OCTotalPercent(double[] oc, Sample.OCSampleUnitsEnum units)
        {
            if (units == Sample.OCSampleUnitsEnum.Total || oc == null)
                return oc;

            // convert the numbers
            return MathUtilities.Multiply_Value(oc, 1.3);
        }

        /// <summary>Converst PH to water units.</summary>
        /// <param name="ph">The ph.</param>
        /// <param name="units">The current units.</param>
        /// <returns>The converted values</returns>
        private static double[] PHWater(double[] ph, Sample.PHSampleUnitsEnum units)
        {
            if (units == Sample.PHSampleUnitsEnum.Water || ph == null)
                return ph;

            // pH in water = (pH in CaCl X 1.1045) - 0.1375
            return MathUtilities.Subtract_Value(MathUtilities.Multiply_Value(ph, 1.1045), 0.1375);
        }
    }
}
