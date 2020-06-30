namespace Models.Soils.NutrientPatching
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Interfaces;
    using Models.Soils.Nutrients;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Encapsulates a nutrient patch.
    /// </summary>
    [Serializable]
    public class NutrientPatch
    {
        /// <summary>Collection of all solutes in this patch.</summary>
        private Dictionary<string, ISolute> solutes = new Dictionary<string, ISolute>();

        /// <summary>The maximum amount of N that is made available to plants in one day.</summary>
        private double[] soilThickness;

        /// <summary>The maximum amount of N that is made available to plants in one day.</summary>
        private double maxTotalNAvailableToPlants;

        /// <summary>Constructor.</summary>
        public NutrientPatch(double[] soilThicknesses, double maxTotalNAvailable)
        {
            soilThickness = soilThicknesses;
            maxTotalNAvailableToPlants = maxTotalNAvailable;
            var simulations = FileFormat.ReadFromString<Simulations>(ReflectionUtilities.GetResourceAsString("Models.Resources.Nutrient.json"), out List<Exception> exceptions);
            if (simulations.Children.Count != 1 || !(simulations.Children[0] is Nutrient))
                throw new Exception("Cannot create nutrient model in NutrientPatchManager");
            Nutrient = simulations.Children[0] as Nutrient;
            Nutrient.IsHidden = true;

            // Find all solutes.
            foreach (ISolute solute in Apsim.Children(Nutrient, typeof(ISolute)))
                solutes.Add(solute.Name, solute);
        }

        /// <summary>Copy constructor.</summary>
        public NutrientPatch(NutrientPatch from)
        {
            soilThickness = from.soilThickness;
            maxTotalNAvailableToPlants = from.maxTotalNAvailableToPlants;
            Nutrient = Apsim.Clone(from.Nutrient) as Nutrient;
            Structure.Add(Nutrient, from.Nutrient.Parent);

            // Find all solutes.
            foreach (ISolute solute in Apsim.Children(Nutrient, typeof(ISolute)))
                solutes.Add(solute.Name, solute);
        }

        /// <summary>Nutrient model.</summary>
        public Nutrient Nutrient { get; }

        /// <summary>Relative area of this patch (0-1).</summary>
        public double RelativeArea { get; set; } = 1.0;

        /// <summary>Name of the patch.</summary>
        public string Name { get; set; }

        /// <summary>Date at which this patch was created.</summary>
        public DateTime CreationDate { get; set; }

        /// <summary>Get the value of a solute (kg/ha).</summary>
        /// <param name="name">The name of the solute.</param>
        /// <returns></returns>
        public double[] GetSoluteKgHaRelativeArea(string name)
        {
            var values = GetSoluteObject(name).kgha;
            if (values != null)
                values = MathUtilities.Multiply_Value(values, RelativeArea);
            return values;
        }

        /// <summary>Get the value of a solute (kg/ha).</summary>
        /// <param name="name">The name of the solute.</param>
        /// <returns></returns>
        public double[] GetSoluteKgHa(string name)
        {
            if (name == "PlantAvailableNO3")
                return CalculateSoluteAvailableToPlants(GetSoluteObject("NO3").kgha);
            else if (name == "PlantAvailableNH4")
                return CalculateSoluteAvailableToPlants(GetSoluteObject("NH4").kgha);
            return GetSoluteObject(name).kgha;
        }

        /// <summary>Set the value of a solute (kg/ha).</summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="name">The name of the solute.</param>
        /// <param name="value">The value to set the solute to.</param>
        /// <returns></returns>
        public void SetSoluteKgHa(SoluteSetterType callingModelType, string name, double[] value)
        {
            GetSoluteObject(name).SetKgHa(callingModelType, value);
        }

        /// <summary>Set the value of a solute (kg/ha).</summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="name">The name of the solute.</param>
        /// <param name="value">The value to set the solute to.</param>
        /// <returns></returns>
        public void AddKgHa(SoluteSetterType callingModelType, string name, double[] value)
        {
            GetSoluteObject(name).AddKgHaDelta(callingModelType, value);
        }

        /// <summary>Calculate actual decomposition</summary>
        public SurfaceOrganicMatterDecompType CalculateActualSOMDecomp()
        {
            var somDecomp = Nutrient.CalculateActualSOMDecomp();
            somDecomp.Multiply(RelativeArea);
            return somDecomp;
        }

        /// <summary>
        /// Add a fraction of solutes and FOM from another patch into this patch.
        /// </summary>
        /// <param name="from">The other patch.</param>
        /// <param name="fractionToAdd">The fraction to add.</param>
        public void Add(NutrientPatch from, double fractionToAdd)
        {
            foreach (var solute in solutes)
            {
                var amountToAdd = MathUtilities.Multiply_Value(from.GetSoluteKgHaRelativeArea(solute.Key), fractionToAdd);
                solute.Value.AddKgHaDelta(SoluteSetterType.Other, amountToAdd);

                // NB The old SoilNitrogen model also added in fractions of 
                // NH3, NO2, TodaysInitialNH4 and TodaysInitialNO3.
                //Patch[k].nh3[layer] += Patch[j].nh3[layer] * MultiplyingFactor;
                //Patch[k].no2[layer] += Patch[j].no2[layer] * MultiplyingFactor;
                //Patch[k].TodaysInitialNH4[layer] += Patch[j].TodaysInitialNH4[layer] * MultiplyingFactor;
                //Patch[k].TodaysInitialNO3[layer] += Patch[j].TodaysInitialNO3[layer] * MultiplyingFactor;


                // NB The old SoilNitrogen model also added in FOM
                //// Organic C and N
                //for (int pool = 0; pool < 3; pool++)
                //{
                //    patches[k].fom_c[pool][layer] += patches[j].fom_c[pool][layer] * MultiplyingFactor;
                //    patches[k].fom_n[pool][layer] += patches[j].fom_n[pool][layer] * MultiplyingFactor;
                //}
                //patches[k].biom_c[layer] += patches[j].biom_c[layer] * MultiplyingFactor;
                //patches[k].biom_n[layer] += patches[j].biom_n[layer] * MultiplyingFactor;
                //patches[k].hum_c[layer] += patches[j].hum_c[layer] * MultiplyingFactor;
                //patches[k].hum_n[layer] += patches[j].hum_n[layer] * MultiplyingFactor;
                //patches[k].inert_c[layer] += patches[j].inert_c[layer] * MultiplyingFactor;
                //patches[k].inert_n[layer] += patches[j].inert_n[layer] * MultiplyingFactor;
            }
        }

        /// <summary>
        /// Add a solutes and FOM.
        /// </summary>
        /// <param name="StuffToAdd">The instance desribing what to add.</param>
        public void Add(AddSoilCNPatchwithFOMType StuffToAdd)
        {
            if ((StuffToAdd.Urea != null) && MathUtilities.IsGreaterThan(Math.Abs(StuffToAdd.Urea.Sum()), 0))
                GetSoluteObject("Urea").AddKgHaDelta(SoluteSetterType.Other, StuffToAdd.Urea);
            if ((StuffToAdd.NH4 != null) && MathUtilities.IsGreaterThan(Math.Abs(StuffToAdd.NH4.Sum()), 0))
                GetSoluteObject("NH4").AddKgHaDelta(SoluteSetterType.Other, StuffToAdd.NH4);
            if ((StuffToAdd.NO3 != null) && MathUtilities.IsGreaterThan(Math.Abs(StuffToAdd.NO3.Sum()), 0))
                GetSoluteObject("NO3").AddKgHaDelta(SoluteSetterType.Other, StuffToAdd.NO3);

            // NB The old SoilNitrogen model also added in FOM.

            //if ((StuffToAdd.FOM != null) && (StuffToAdd.FOM.Pool != null))
            //{
            //    bool SomethingAdded = false;
            //    double[][] CValues = new double[3][];
            //    double[][] NValues = new double[3][];
            //    for (int pool = 0; pool < StuffToAdd.FOM.Pool.Length; pool++)
            //    {
            //        if ((StuffToAdd.FOM.Pool[pool].C != null) && MathUtilities.IsGreaterThan(Math.Abs(StuffToAdd.FOM.Pool[pool].C.Sum()), 0))
            //        {
            //            CValues[pool] = StuffToAdd.FOM.Pool[pool].C;
            //            SomethingAdded = true;
            //        }
            //        if ((StuffToAdd.FOM.Pool[pool].N != null) && MathUtilities.IsGreaterThan(Math.Abs(StuffToAdd.FOM.Pool[pool].N.Sum()), 0))
            //        {
            //            NValues[pool] = StuffToAdd.FOM.Pool[pool].N;
            //            SomethingAdded = true;
            //        }
            //    }
            //    if (SomethingAdded)
            //    {
            //        patches[PatchesToAdd[i]].dlt_fom_c = CValues;
            //        patches[PatchesToAdd[i]].dlt_fom_n = NValues;
            //    }
            //}
        }

        /// <summary>Calculate the amount of solute made available to plants (kgN/ha).</summary>
        /// <param name="solute">The solute to convert to plant available.</param>
        /// <returns>The amount of solute available to the plant.</returns>
        private double[] CalculateSoluteAvailableToPlants(double[] solute)
        {
            double rootDepth = soilThickness.Sum();
            double depthFromSurface = 0.0;
            double[] result = new double[solute.Length];
            double fractionAvailable = Math.Min(1.0,
                MathUtilities.Divide(maxTotalNAvailableToPlants, CalcTotalMineralNInRootZone(), 0.0));
            for (int layer = 0; layer < solute.Length; layer++)
            {
                result[layer] = solute[layer] * fractionAvailable;
                depthFromSurface += soilThickness[layer];
                if (depthFromSurface >= rootDepth)
                    break;
            }
            return result;
        }

        /// <summary>
        /// Computes the amount of NH4 and NO3 in the root zone
        /// </summary>
        private double CalcTotalMineralNInRootZone()
        {
            double rootDepth = soilThickness.Sum();
            var no3 = GetSoluteObject("NO3").kgha;
            var nh4 = GetSoluteObject("NH4").kgha;
            double totalMineralNInRootZone = 0.0;
            double depthFromSurface = 0.0;
            for (int layer = 0; layer < no3.Length; layer++)
            {
                totalMineralNInRootZone += nh4[layer] + no3[layer];
                depthFromSurface += soilThickness[layer];
                if (depthFromSurface >= rootDepth)
                    break;
            }
            return totalMineralNInRootZone;
        }

        /// <summary>Get a solute object under the nutrient model.</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private ISolute GetSoluteObject(string name)
        {
            if (!solutes.TryGetValue(name, out ISolute solute))
                throw new Exception($"Cannot find solute: {name}.");
            return solute;
        }
    }
}
