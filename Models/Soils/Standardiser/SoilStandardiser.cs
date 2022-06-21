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
            MergeSamplesIntoOne(soil);
            SoilDefaults.FillInMissingValues(soil);
            CreateInitialSample(soil);
        }

        /// <summary>Removes all but one sample from the specified soil.</summary>
        /// <param name="soil">The soil.</param>
        private static void MergeSamplesIntoOne(Soil soil)
        {
            var samples = soil.FindAllChildren<Sample>().Cast<Sample>().ToArray();
            for (int i = 1; i < samples.Length; i++)
            {
                if (MathUtilities.ValuesInArray(samples[i].SW))
                {
                    samples[0].SW = samples[i].SW;
                    samples[0].SWUnits = samples[i].SWUnits;
                }

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
            var soilPhysical = soil.FindChild<Soils.IPhysical>();

            if (initial == null)
            {
                initial = new Sample() { Thickness = soilPhysical.Thickness, Parent = soil };
                soil.Children.Add(initial);
            }
            initial.Name = "Initial";

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
    }
}
