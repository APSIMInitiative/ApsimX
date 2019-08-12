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
            // Convert soil organic matter OC to total %
            if (soil.SoilOrganicMatter != null)
            {
                soil.SoilOrganicMatter.OC = OCTotalPercent(soil.SoilOrganicMatter.OC, soil.SoilOrganicMatter.OCUnits);
                soil.SoilOrganicMatter.OCUnits = Sample.OCSampleUnitsEnum.Total;
            }

            // Convert analysis.
            var analysis = Apsim.Child(soil, typeof(Analysis)) as Analysis;
            if (analysis != null)
            {
                analysis.PH = PHWater(analysis.PH, analysis.PHUnits);
                analysis.PHUnits = Sample.PHSampleUnitsEnum.Water;
            }

            // Convert all samples.
            var samples = Apsim.Children(soil, typeof(Sample)).Cast<Sample>().ToArray();
            foreach (Sample sample in samples)
            {
                // Convert sw units to volumetric.
                if (MathUtilities.ValuesInArray(sample.SW))
                    sample.SW = SWVolumetric(sample, soil);
                sample.SWUnits = Sample.SWUnitsEnum.Volumetric;

                // Convert no3 units to ppm.
                if (sample.NO3N != null && !sample.NO3N.StoredAsPPM)
                {
                    var ppm = sample.NO3N.PPM;
                    sample.NO3N.PPM = ppm;
                }
                sample.NO3Units = Sample.NUnitsEnum.ppm;

                // Convert nh4 units to ppm.
                if (sample.NH4N != null && !sample.NH4N.StoredAsPPM)
                {
                    var ppm = sample.NH4N.PPM;
                    sample.NH4N.PPM = ppm;
                }
                sample.NH4Units = Sample.NUnitsEnum.ppm;

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

        /// <summary>Converts n values to ppm.</summary>
        /// <param name="n">The n values to convert.</param>
        /// <param name="thickness">The thickness of the values..</param>
        /// <param name="nunits">The current units of n.</param>
        /// <param name="bd">The related bulk density.</param>
        /// <returns>ppm values.</returns>
        private static double[] Nppm(double[] n, double[] thickness, Sample.NUnitsEnum nunits, double[] bd)
        {
            if (nunits == Sample.NUnitsEnum.ppm || n == null)
                return n;

            // kg/ha to ppm
            double[] newN = new double[n.Length];
            for (int i = 0; i < n.Length; i++)
            {
                if (Double.IsNaN(n[i]))
                    newN[i] = double.NaN;
                else
                    newN[i] = n[i] * 100 / (bd[i] * thickness[i]);
            }

            return newN;
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
