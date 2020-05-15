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
            soil.FindChildren();
            Layers.Standardise(soil);
            SoilUnits.Convert(soil);
            MergeSamplesIntoOne(soil);
            SoilDefaults.FillInMissingValues(soil);
            RemoveInitialWater(soil);
            CreateInitialSample(soil);
        }

        /// <summary>Removes all but one sample from the specified soil.</summary>
        /// <param name="soil">The soil.</param>
        private static void MergeSamplesIntoOne(Soil soil)
        {
            var samples = Apsim.Children(soil, typeof(Sample)).Cast<Sample>().ToArray();
            for (int i = 1; i < samples.Length; i++)
            {
                if (MathUtilities.ValuesInArray(samples[i].SW))
                {
                    samples[0].SW = samples[i].SW;
                    samples[0].SWUnits = samples[i].SWUnits;
                }

                if (MathUtilities.ValuesInArray(samples[i].OC))
                {
                    samples[0].OC = samples[i].OC;
                    samples[0].OCUnits = samples[i].OCUnits;
                }

                if (MathUtilities.ValuesInArray(samples[i].PH))
                {
                    samples[0].PH = samples[i].PH;
                    samples[0].PHUnits = samples[i].PHUnits;
                }
                if (MathUtilities.ValuesInArray(samples[i].ESP))
                    samples[0].ESP = samples[i].ESP;

                if (MathUtilities.ValuesInArray(samples[i].EC))
                    samples[0].EC = samples[i].EC;

                if (MathUtilities.ValuesInArray(samples[i].CL))
                    samples[0].CL = samples[i].CL;

                soil.Children.Remove(samples[i]);
            }

        }
        
        /// <summary>Creates an initial sample.</summary>
        /// <param name="soil">The soil.</param>
        private static void CreateInitialSample(Soil soil)
        {
            var soilOrganicMatter = soil.Children.Find(child => child is Organic) as Organic;
            var analysis = soil.Children.Find(child => child is Chemical) as Chemical;
            var initial = soil.Children.Find(child => child is Sample) as Sample;
            if (initial == null)
            {
                initial = new Sample() { Thickness = soil.Thickness, Parent = soil };
                soil.Children.Add(initial);
            }
            initial.Name = "Initial";

            if (analysis.NO3N != null)
                initial.NO3N = soil.ppm2kgha(analysis.NO3N);
            if (analysis.NH4N != null)
                initial.NH4N = soil.ppm2kgha(analysis.NH4N);

            initial.OC = MergeArrays(initial.OC, soilOrganicMatter.Carbon);
            initial.PH = MergeArrays(initial.PH, analysis.PH);
            initial.ESP = MergeArrays(initial.ESP, analysis.ESP);
            initial.EC = MergeArrays(initial.EC, analysis.EC);
            initial.CL = MergeArrays(initial.CL, analysis.CL);

            soilOrganicMatter.Carbon = null;
            //soil.Children.Remove(analysis);
        }


        /// <summary>Merge a secondary array into a primary arrays.</summary>
        /// <param name="primaryArray">The primary array.</param>
        /// <param name="secondaryArray">The secondary array.</param>
        /// <returns>The primary array with missing values copied from the secondary array.</returns>
        private static double[] MergeArrays(double[] primaryArray, double[] secondaryArray)
        {
            if (primaryArray == null)
                return secondaryArray;

            for (int i = 0; i < primaryArray.Length; i++)
            {
                if (double.IsNaN(primaryArray[i]))
                {
                    if (secondaryArray == null || double.IsNaN(secondaryArray[i]))
                    {
                        // No value in either primary or secondary array for this index.
                        if (i == 0)
                            primaryArray[i] = primaryArray.First(value => !double.IsNaN(value));
                        else
                            primaryArray[i] = primaryArray.Last(value => !double.IsNaN(value));
                    }
                    else
                        primaryArray[i] = secondaryArray[i];
                }
            }
            return primaryArray;
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
                    sample.Parent = soil;
                    soil.Children.Add(sample);
                }

                sample.SW = initialWater.SW(sample.Thickness, soil.LL15, soil.DUL, null);

                soil.Children.Remove(initialWater);
            }
        }
    }
}
