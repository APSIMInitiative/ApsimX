using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils.Nutrients;
using Models.Surface;

namespace Models.Soils.NutrientPatching
{

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

        /// <summary>The nutrient patch manager.</summary>
        private NutrientPatchManager patchManager;

        /// <summary>The lignin pool from the Nutrient model.</summary>
        private OrganicPool lignin;

        /// <summary>The cellulose pool from the Nutrient model.</summary>
        private OrganicPool cellulose;

        /// <summary>The carbohydrate pool from the Nutrient model.</summary>
        private OrganicPool carbohydrate;

        /// <summary>Constructor.</summary>
        /// <param name="soilThicknesses">Soil thicknesses (mm).</param>
        /// <param name="nutrientPatchManager">The nutrient patch manager.</param>
        public NutrientPatch(double[] soilThicknesses, NutrientPatchManager nutrientPatchManager)
        {
            soilThickness = soilThicknesses;
            patchManager = nutrientPatchManager;
            var simulations = FileFormat.ReadFromString<Simulations>(ReflectionUtilities.GetResourceAsString("Models.Resources.Nutrient.json"), e => throw e, false).NewModel as Simulations;
            if (simulations.Children.Count != 1 || !(simulations.Children[0] is Nutrient))
                throw new Exception("Cannot create nutrient model in NutrientPatchManager");
            Nutrient = simulations.Children[0] as Nutrient;
            Nutrient.IsHidden = true;

            CreateSolutes(Nutrient, (patchManager as IModel).FindAncestor<Soil>().FindAllChildren<Solute>());

            // Find all solutes.
            foreach (ISolute solute in Nutrient.FindAllChildren<ISolute>())
                solutes.Add(solute.Name, solute);
            lignin = Nutrient.FindInScope<OrganicPool>("FOMLignin");
            if (lignin == null)
                throw new Exception("Cannot find lignin pool in the nutrient model.");
            cellulose = Nutrient.FindInScope<OrganicPool>("FOMCellulose");
            if (cellulose == null)
                throw new Exception("Cannot find cellulose pool in the nutrient model.");
            carbohydrate = Nutrient.FindInScope<OrganicPool>("FOMCarbohydrate");
            if (carbohydrate == null)
                throw new Exception("Cannot find carbohydrate pool in the nutrient model.");
        }

        /// <summary>Copy constructor.</summary>
        public NutrientPatch(NutrientPatch from)
        {
            soilThickness = from.soilThickness;
            patchManager = from.patchManager;
            Nutrient = Apsim.Clone(from.Nutrient) as Nutrient;
            Nutrient.Name = $"Nutrient{patchManager.NumPatches}";
            Structure.Add(Nutrient, from.Nutrient.Parent);

            // Find all solutes.
            foreach (ISolute solute in Nutrient.FindAllChildren<ISolute>())
                solutes.Add(solute.Name, solute);
            lignin = from.lignin;
            cellulose = from.cellulose;
            carbohydrate = from.carbohydrate;
        }

        private void CreateSolutes(IModel parent, IEnumerable<Solute> solutes)
        {
            foreach (Solute solute in solutes)
            {
                var newSolute = new Solute();
                newSolute.Name = solute.Name;
                newSolute.Thickness = solute.Thickness;
                newSolute.InitialValues = solute.InitialValues;
                newSolute.InitialValuesUnits = solute.InitialValuesUnits;
                newSolute.Parent = parent;
                parent.Children.Add(newSolute);
            }
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
            var values = GetSoluteKgHa(name);
            if (values != null)
                values = MathUtilities.Multiply_Value(values, RelativeArea);
            return values;
        }

        /// <summary>Get the value of a solute (kg/ha).</summary>
        /// <param name="name">The name of the solute.</param>
        /// <returns></returns>
        public double[] GetSoluteKgHa(string name)
        {
            //if (name == "NO3")
            //    return CalculateSoluteAvailableToPlants(GetSoluteObject("NO3").kgha);
            //else if (name == "NH4")
            //    return CalculateSoluteAvailableToPlants(GetSoluteObject("NH4").kgha);
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

            if ((StuffToAdd.FOM != null) && (StuffToAdd.FOM.Pool != null))
            {
                if (StuffToAdd.FOM.Pool.Length != 3)
                    throw new Exception("Expected 3 pools of FOM to be added in PatchManager");
                if (StuffToAdd.FOM.Pool[0].C.Length != lignin.C.Count ||
                    StuffToAdd.FOM.Pool[0].N.Length != lignin.N.Count ||
                    StuffToAdd.FOM.Pool[1].C.Length != cellulose.C.Count ||
                    StuffToAdd.FOM.Pool[1].N.Length != cellulose.N.Count ||
                    StuffToAdd.FOM.Pool[2].C.Length != carbohydrate.C.Count ||
                    StuffToAdd.FOM.Pool[2].N.Length != carbohydrate.N.Count)
                    throw new Exception("Mismatched number of layers of FOM being added in PatchManager");

                for (int i = 0; i < lignin.C.Count; i++)
                {
                    lignin.Add(i, c: StuffToAdd.FOM.Pool[0].C[i] * RelativeArea,
                                  n: StuffToAdd.FOM.Pool[0].N[i] * RelativeArea,
                                  p: StuffToAdd.FOM.Pool[0].P[i] * RelativeArea);
                    cellulose.Add(i, c: StuffToAdd.FOM.Pool[1].C[i] * RelativeArea,
                                     n: StuffToAdd.FOM.Pool[1].N[i] * RelativeArea,
                                     p: StuffToAdd.FOM.Pool[1].P[i] * RelativeArea);
                    carbohydrate.Add(i, c: StuffToAdd.FOM.Pool[2].C[i] * RelativeArea,
                                        n: StuffToAdd.FOM.Pool[2].N[i] * RelativeArea,
                                        p: StuffToAdd.FOM.Pool[2].P[i] * RelativeArea);
                }
            }
        }

        ///// <summary>Calculate the amount of solute made available to plants (kgN/ha).</summary>
        ///// <param name="solute">The solute to convert to plant available.</param>
        ///// <returns>The amount of solute available to the plant.</returns>
        //private double[] CalculateSoluteAvailableToPlants(double[] solute)
        //{
        //    double rootDepth = soilThickness.Sum();
        //    double depthFromSurface = 0.0;
        //    double[] result = new double[solute.Length];
        //    double fractionAvailable = Math.Min(1.0,
        //        MathUtilities.Divide(patchManager.MaximumNitrogenAvailableToPlants, CalcTotalMineralNInRootZone(), 0.0));
        //    for (int layer = 0; layer < solute.Length; layer++)
        //    {
        //        result[layer] = solute[layer] * fractionAvailable;
        //        depthFromSurface += soilThickness[layer];
        //        if (depthFromSurface >= rootDepth)
        //            break;
        //    }
        //    return result;
        //}

        ///// <summary>
        ///// Computes the amount of NH4 and NO3 in the root zone
        ///// </summary>
        //private double CalcTotalMineralNInRootZone()
        //{
        //    double rootDepth = soilThickness.Sum();
        //    var no3 = GetSoluteObject("NO3").kgha;
        //    var nh4 = GetSoluteObject("NH4").kgha;
        //    double totalMineralNInRootZone = 0.0;
        //    double depthFromSurface = 0.0;
        //    for (int layer = 0; layer < no3.Length; layer++)
        //    {
        //        totalMineralNInRootZone += nh4[layer] + no3[layer];
        //        depthFromSurface += soilThickness[layer];
        //        if (depthFromSurface >= rootDepth)
        //            break;
        //    }
        //    return totalMineralNInRootZone;
        //}

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
