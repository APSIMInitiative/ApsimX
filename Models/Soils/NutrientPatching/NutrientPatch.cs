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
        Dictionary<string, ISolute> solutes = new Dictionary<string, ISolute>();

        /// <summary>Constructor.</summary>
        public NutrientPatch()
        {
            var simulations = FileFormat.ReadFromString<Simulations>(ReflectionUtilities.GetResourceAsString("Models.Resources.Nutrient.json"), out List<Exception> exceptions);
            if (simulations.Children.Count != 1 || !(simulations.Children[0] is Nutrient))
                throw new Exception("Cannot create nutrient model in NutrientPatchManager");
            Nutrient = simulations.Children[0] as Nutrient;
            Nutrient.IsHidden = true;

            // Find all solutes.
            foreach (ISolute solute in Apsim.Children(Nutrient, typeof(ISolute)))
                solutes.Add(solute.Name, solute);
            if (!solutes.ContainsKey("PlantAvailableNO3"))
            {
                solutes.Add("PlantAvailableNO3", new Solute() { Name = "PlantAvailableNO3" });
                solutes.Add("PlantAvailableNH4", new Solute() { Name = "PlantAvailableNH4" });
            }
        }
        /// <summary>Nutrient model.</summary>

        public Nutrient Nutrient { get; }

        /// Relative area of this patch (0-1)
        public double RelativeArea { get; } = 1.0;

        /// <summary>Get the value of a solute (kg/ha).</summary>
        /// <param name="name">The name of the solute.</param>
        /// <returns></returns>
        public double[] GetSoluteKgHa(string name)
        {
            var values = GetSoluteObject(name).kgha;
            if (values != null)
                values = MathUtilities.Multiply_Value(values, RelativeArea);
            return values;
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

        /// <summary>Calculate actual decomposition</summary>
        public SurfaceOrganicMatterDecompType CalculateActualSOMDecomp()
        {
            var somDecomp = Nutrient.CalculateActualSOMDecomp();
            somDecomp.Multiply(RelativeArea);
            return somDecomp;
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
