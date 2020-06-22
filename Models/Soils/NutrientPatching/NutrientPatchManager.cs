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
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Encapsulates a cohort of Nutrient models i.e. patching.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class NutrientPatchManager : Model, INutrient, INutrientPatchManager
    {
        [Link]
        private Soil soil = null;

        private List<NutrientPatch> patches = new List<NutrientPatch>();

        /// <summary>The maximum amount of N that is made available to plants in one day (kg/ha/day).</summary>
        public double MaximumNitrogenAvailableToPlants { get; set; }

        /// <summary>The approach used for partitioning the N between patches.</summary>
        public PartitionApproachEnum NPartitionApproach { get; set; } = PartitionApproachEnum.BasedOnConcentrationAndDelta;

        /// <summary>Approach to use when comparing patches for AutoAmalagamation.</summary>
        public AutoAmalgamationApproachEnum AutoAmalgamationApproach { get; set; }

        /// <summary>Approach to use when defining the base patch.</summary>
        /// <remarks>
        /// This is used to define the patch considered the 'base'. It is only used when comparing patches during
        /// potential auto-amalgamation (comparison against base are more lax)
        /// </remarks>
        public BaseApproachEnum basePatchApproach { get; set; }

        /// <summary>Allow force amalgamation of patches based on age?</summary>
        public bool AllowPatchAmalgamationByAge { get; set; }

        /// <summary>Age of patch at which merging is enforced (years).</summary>
        public double PatchAgeForForcedMerge { get; set; }

        /// <summary>Layer thickness to consider when N partition between patches is BasedOnSoilConcentration (mm).</summary>
        [Units("mm")]
        public double LayerForNPartition { get; set; } = -99;

        /// <summary>Calculate actual decomposition</summary>
        public SurfaceOrganicMatterDecompType CalculateActualSOMDecomp()
        {
            // Note:
            //   - If there wasn't enough mineral N to decompose, the rate will be reduced to zero !!  - MUST CHECK THE VALIDITY OF THIS

            SurfaceOrganicMatterDecompType returnSOMDecomp = null;
            foreach (var patch in patches)
            {
                var somDecomp = patch.Nutrient.CalculateActualSOMDecomp();
                if (returnSOMDecomp == null)
                    returnSOMDecomp = somDecomp;
                else
                    returnSOMDecomp.Add(somDecomp);
            }
            //foreach (var residue in somDecomp.Pool)
            //{
            //    residue.FOM.amount = 0;
            //    residue.FOM.C *= patch.RelativeArea;
            //    residue.FOM.N *= patch.RelativeArea;
            //}


            return returnSOMDecomp;
        }

        /// <summary>
        /// Called by solutes to get the value of a solute.
        /// </summary>
        /// <param name="name">The name of the solute to get.</param>
        /// <returns></returns>
        internal double[] GetSoluteKgha(string name)
        {
            double[] values = null;
            foreach (var patch in patches)
            {
                if (values == null)
                    values = patch.GetSoluteKgHa(name);
                else
                    MathUtilities.Add(values, patch.GetSoluteKgHa(name));
            }
            return values;
        }

        /// <summary>
        /// Called by solutes to set the value of a solute.
        /// </summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="name">The name of the solute to get.</param>
        /// <param name="value">The value of the solute.</param>
        /// <returns></returns>
        internal void SetSoluteKgha(SoluteSetterType callingModelType, string name, double[] value)
        {

            // Determine if value is different from the current value of the solute.
            var currentValues = GetSoluteKgha(name);
            bool hasChanges = currentValues == null || !MathUtilities.AreEqual(currentValues, value);

            // check partitioning and pass the appropriate values to patches
            if (hasChanges)
            {
                bool requiresPartitioning = patches.Count > 1 && callingModelType == SoluteSetterType.Soil || callingModelType == SoluteSetterType.Plant;
                if (requiresPartitioning)
                {
                    // the values require partitioning
                    var deltaN = MathUtilities.Subtract(value, currentValues);
                    double[][] newDelta = PartitionDelta(deltaN, name, callingModelType, NPartitionApproach);

                    for (int i = 0; i < patches.Count; i++)
                    {
                        var currentPatchValues = patches[i].GetSoluteKgHa(name);
                        var values = MathUtilities.Add(currentPatchValues, newDelta[i]);
                        patches[i].SetSoluteKgHa(SoluteSetterType.Other, name, values);
                    }
                }
                else
                {
                    // the values do not require partitioning - perform solute set.
                    foreach (var patch in patches)
                        patch.SetSoluteKgHa(SoluteSetterType.Other, name, value);
                }
            }
        }

        /// <summary>
        /// Incorporate fresh organic matter.
        /// </summary>
        /// <param name="FOMdata">Amount to incorporate.</param>
        public void DoIncorpFOM(FOMLayerType FOMdata)
        {
            foreach (var patch in patches)
                patch.Nutrient.DoIncorpFOM(FOMdata);
        }

        /// <summary>Reset all pools.</summary>
        public void Reset()
        {
            foreach (var patch in patches)
                patch.Nutrient.Reset();
        }

        /// <summary>
        /// Add a new patch.
        /// </summary>
        /// <param name="patch">Details of the patch to add.</param>
        public void Add(AddSoilCNPatchType patch)
        {

        }

        /// <summary>At the start of the simulation set up LifeCyclePhases</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // Create a new nutrient patch.
            var newPatch = new NutrientPatch();
            patches.Add(newPatch);
            Structure.Add(newPatch.Nutrient, this);
        }

        /// <summary>
        /// calculate how the dlt's (C and N) are partitioned amongst patches
        /// </summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="incomingDelta">The dlt to be partioned amongst patches</param>
        /// <param name="soluteName">The solute or pool that is changing</param>
        /// <param name="partitionApproach">The type of partition to be used</param>
        /// <returns>The values of dlt partitioned for each existing patch</returns>
        private double[][] PartitionDelta(double[] incomingDelta, string soluteName, SoluteSetterType callingModelType, PartitionApproachEnum partitionApproach)
        {
            int numberLayers = incomingDelta.Length;

            // 1. initialise the result array
            double[][] Result = new double[patches.Count][];
            for (int k = 0; k < patches.Count; k++)
                Result[k] = new double[numberLayers];

            try
            {
                // 1.5 If the calling model is a plant and the solute is NO3 or NH4 then use the 'PlantAvailable' solutes instead.
                if (callingModelType == SoluteSetterType.Plant && (soluteName == "NO3" || soluteName == "NH4"))
                    soluteName = "PlantAvailable" + soluteName;

                // 2- gather how much solute is already in the soil
                double[][] existingSoluteAmount = new double[patches.Count][];
                for (int k = 0; k < patches.Count; k++)
                    existingSoluteAmount[k] = patches[k].GetSoluteKgHa(soluteName);

                // 3- calculate partition weighting factors, done for each layer based on existing solute amount
                double[] partitionWeight;
                double[] thisLayerPatchSolute;
                double thisLayersTotalSolute;
                for (int layer = 0; layer < numberLayers; layer++)
                {
                    if (!MathUtilities.FloatsAreEqual(incomingDelta[layer], 0))
                    {
                        // 3.1- zero and initialise the variables
                        partitionWeight = new double[patches.Count];
                        thisLayerPatchSolute = new double[patches.Count];

                        // 3.2- get the solute amounts for each patch in this layer
                        if (partitionApproach == PartitionApproachEnum.BasedOnLayerConcentration ||
                           (partitionApproach == PartitionApproachEnum.BasedOnConcentrationAndDelta && MathUtilities.IsLessThan(incomingDelta[layer], 0)))
                        {
                            for (int k = 0; k < patches.Count; k++)
                                thisLayerPatchSolute[k] = existingSoluteAmount[k][layer] * patches[k].RelativeArea;
                        }
                        else if (partitionApproach == PartitionApproachEnum.BasedOnSoilConcentration ||
                                (partitionApproach == PartitionApproachEnum.BasedOnConcentrationAndDelta && MathUtilities.IsLessThanOrEqual(incomingDelta[layer], 0)))
                        {
                            for (int k = 0; k < patches.Count; k++)
                            {
                                double layerUsed = 0.0;
                                for (int z = layer; z >= 0; z--)        // goes backwards till soil surface (but may stop before that)
                                {
                                    thisLayerPatchSolute[k] += existingSoluteAmount[k][z];
                                    layerUsed += soil.Thickness[z];
                                    if (MathUtilities.IsGreaterThan(LayerForNPartition, 0) && layerUsed >= LayerForNPartition)
                                        // stop if thickness reaches a defined value
                                        z = -1;
                                }
                                thisLayerPatchSolute[k] *= patches[k].RelativeArea;
                            }
                        }

                        // 3.3- get the total solute amount for this layer
                        thisLayersTotalSolute = MathUtilities.Sum(thisLayerPatchSolute);

                        // 3.4- Check whether the existing solute is greater than the incoming delta
                        if (!MathUtilities.FloatsAreEqual(thisLayersTotalSolute + incomingDelta[layer], 0))
                            throw new Exception($"Attempt to change {soluteName}[{layer + 1}] to a negative value");

                        // 3.5- Compute the partition weights for each patch
                        for (int k = 0; k < patches.Count; k++)
                        {
                            partitionWeight[k] = 0.0;
                            if (MathUtilities.IsLessThanOrEqual(thisLayersTotalSolute, 0))
                                partitionWeight[k] = MathUtilities.Divide(thisLayerPatchSolute[k], thisLayersTotalSolute, 0.0);
                        }

                        // 4- Compute the partitioned values for each patch
                        for (int k = 0; k < patches.Count; k++)
                            Result[k][layer] = (incomingDelta[layer] * partitionWeight[k]) / patches[k].RelativeArea;
                    }
                    else
                    { 
                        // there is no incoming solute for this layer
                        for (int k = 0; k < patches.Count; k++)
                            Result[k][layer] = 0.0;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Problems with partitioning {soluteName} - {e}");
            }

            return Result;
        }




        /// <summary>
        /// Encapsulates a nutrient patch.
        /// </summary>
        [Serializable]
        private class NutrientPatch
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
}