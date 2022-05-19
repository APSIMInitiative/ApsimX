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
            var samples = soil.FindAllChildren<Sample>().Cast<Sample>().ToArray();
            foreach (Sample sample in samples)
            {
                // Convert sw units to volumetric.
                if (MathUtilities.ValuesInArray(sample.SW))
                    sample.SW = SWVolumetric(sample, soil);
                sample.SWUnits = Sample.SWUnitsEnum.Volumetric;
            }

            var organic = soil.FindChild<Organic>();
            if (organic != null)
            {
                // Convert OC to total (%)
                if (organic.CarbonUnits == Organic.CarbonUnitsEnum.WalkleyBlack && MathUtilities.ValuesInArray(organic.Carbon))
                    organic.Carbon = SoilUtilities.OCWalkleyBlackToTotal(organic.Carbon);
                organic.CarbonUnits = Organic.CarbonUnitsEnum.Total;
            }

            var chemical = soil.FindChild<Chemical>();
            if (chemical != null)
            {
                // Convert PH to water.
                if (chemical.PHUnits == Chemical.PHUnitsEnum.CaCl2 && MathUtilities.ValuesInArray(chemical.PH))
                    chemical.PH = SoilUtilities.PHCaCl2ToWater(chemical.PH);
                chemical.PHUnits = Chemical.PHUnitsEnum.Water;
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
                    var soilPhysical = soil.FindChild<Soils.IPhysical>();
                    double[] bd = Layers.BDMapped(soilPhysical, sample.Thickness);
                    return MathUtilities.Multiply(sample.SW, bd);
                }
                else
                    return MathUtilities.Divide(sample.SW, sample.Thickness); // from mm to mm/mm
            }
        }
    }
}
