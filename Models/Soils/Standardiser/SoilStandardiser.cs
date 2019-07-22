namespace Models.Soils.Standardiser
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System.Linq;

    /// <summary>Methods for standardising an APSIM soil ready for running.</summary>
    public class SoilStandardiser
    {
        /// <summary>Creates an apsim ready soil.</summary>
        /// <param name="soil">The soil.</param>
        public static void Standardise(Soil soil)
        {
            Layers.Standardise(soil);
            SoilUnits.Convert(soil);
            SoilDefaults.FillInMissingValues(soil);
            RemoveInitialWater(soil);
            MergeSamplesIntoOne(soil);
        }

        /// <summary>Removes all but one sample from the specified soil.</summary>
        /// <param name="soil">The soil.</param>
        private static void MergeSamplesIntoOne(Soil soil)
        {
            var samples = Apsim.Children(soil, typeof(Sample)).Cast<Sample>().ToArray();
            for (int i = 1; i < samples.Length; i++)
            {
                if (MathUtilities.ValuesInArray(samples[i].SW))
                    samples[0].SW = samples[i].SW;

                if (MathUtilities.ValuesInArray(samples[i].NO3))
                {
                    samples[0].NO3 = samples[i].NO3;
                    MathUtilities.ReplaceMissingValues(samples[0].NO3, 0.01);
                }

                if (MathUtilities.ValuesInArray(samples[i].NH4))
                {
                    samples[0].NH4 = samples[i].NH4;
                    MathUtilities.ReplaceMissingValues(samples[0].NH4, 0.01);
                }

                if (MathUtilities.ValuesInArray(samples[i].OC))
                {
                    samples[0].OC = samples[i].OC;
                    MathUtilities.ReplaceMissingValues(samples[0].OC, MathUtilities.LastValue(samples[i].OC));
                }

                if (MathUtilities.ValuesInArray(samples[i].PH))
                {
                    samples[0].PH = samples[i].PH;
                    MathUtilities.ReplaceMissingValues(samples[0].PH, 7);
                }

                if (MathUtilities.ValuesInArray(samples[i].ESP))
                {
                    samples[0].ESP = samples[i].ESP;
                    MathUtilities.ReplaceMissingValues(samples[0].ESP, 0);
                }

                if (MathUtilities.ValuesInArray(samples[i].EC))
                {
                    samples[0].EC = samples[i].EC;
                    MathUtilities.ReplaceMissingValues(samples[0].EC, 0);
                }

                if (MathUtilities.ValuesInArray(samples[i].CL))
                {
                    samples[0].CL = samples[i].CL;
                    MathUtilities.ReplaceMissingValues(samples[0].CL, 0);
                }

                soil.Children.Remove(samples[i]);
            }
        }

        /// <summary>Removes the initial water.</summary>
        /// <param name="soil">The soil.</param>
        private static void RemoveInitialWater(Soil soil)
        {
            var initialWater = Apsim.Child(soil, typeof(InitialWater)) as InitialWater;
            if (initialWater != null)
            {
                var sample = Apsim.Child(soil, typeof(Sample)) as Sample;
                if (sample == null)
                {
                    sample = new Sample();
                    sample.Thickness = soil.Thickness;
                    soil.Children.Add(sample);
                }

                sample.SW = initialWater.SW(sample.Thickness, soil.LL15, soil.DUL, null);

                soil.Children.Remove(initialWater);
            }
        }
    }
}
