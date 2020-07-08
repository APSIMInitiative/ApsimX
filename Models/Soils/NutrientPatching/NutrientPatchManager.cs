namespace Models.Soils.NutrientPatching
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Interfaces;
    using Models.Soils.Nutrients;
    using Models.Utilities;
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
        private Clock clock = null;

        [Link]
        private Soil soil = null;

        [Link]
        private ISummary summary = null;

        private List<NutrientPatch> patches = new List<NutrientPatch>();

        /// <summary>Value to evaluate precision against floating point variables.</summary>
        private readonly double epsilon = 0.000000000001;

        /// <summary>Minimum allowable relative area for a CNpatch (0-1).</summary>
        private double minimumPatchArea = 0.000001;

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
                var somDecomp = patch.CalculateActualSOMDecomp();
                foreach (var pool in somDecomp.Pool)
                    pool.FOM.amount = 0;

                if (returnSOMDecomp == null)
                    returnSOMDecomp = somDecomp;
                else
                    returnSOMDecomp.Add(somDecomp);
            }

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
                    values = patch.GetSoluteKgHaRelativeArea(name);
                else
                    values = MathUtilities.Add(values, patch.GetSoluteKgHaRelativeArea(name));
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
                bool requiresPartitioning = patches.Count > 1 && (callingModelType == SoluteSetterType.Soil || callingModelType == SoluteSetterType.Plant);
                if (requiresPartitioning)
                {
                    // the values require partitioning
                    var deltaN = MathUtilities.Subtract(value, currentValues);
                    double[][] newDelta = PartitionDelta(deltaN, name, callingModelType, NPartitionApproach);

                    for (int i = 0; i < patches.Count; i++)
                    {
                        patches[i].AddKgHa(SoluteSetterType.Other, name, newDelta[i]);
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
            AddSoilCNPatchwithFOMType PatchtoAdd = new AddSoilCNPatchwithFOMType();
            PatchtoAdd.Sender = patch.Sender;
            PatchtoAdd.SuppressMessages = patch.SuppressMessages;
            PatchtoAdd.DepositionType = patch.DepositionType;
            PatchtoAdd.AreaNewPatch = patch.AreaFraction;
            PatchtoAdd.AffectedPatches_id = patch.AffectedPatches_id;
            PatchtoAdd.AffectedPatches_nm = patch.AffectedPatches_nm;
            PatchtoAdd.Urea = patch.Urea;
            PatchtoAdd.NH4 = patch.NH4;
            PatchtoAdd.NO3 = patch.NO3;

            bool isDataOK = true;

            if (PatchtoAdd.DepositionType == DepositionTypeEnum.ToNewPatch)
            {
                if (PatchtoAdd.AffectedPatches_id.Length == 0 && PatchtoAdd.AffectedPatches_nm.Length == 0)
                {
                    summary.WriteMessage(this, " Command to add patch did not supply a valid patch to be used as base for the new one. Command will be ignored.");
                    isDataOK = false;
                }
                else if (PatchtoAdd.AreaNewPatch <= 0.0)
                {
                    summary.WriteMessage(this, " Command to add patch did not supply a valid area fraction for the new patch. Command will be ignored.");
                    isDataOK = false;
                }
            }
            else if (PatchtoAdd.DepositionType == DepositionTypeEnum.ToSpecificPatch)
            {
                if (PatchtoAdd.AffectedPatches_id.Length == 0 && PatchtoAdd.AffectedPatches_nm.Length == 0)
                {
                    summary.WriteMessage(this, " Command to add patch did not supply a valid patch to be used as base for the new one. Command will be ignored.");
                    isDataOK = false;
                }
            }
            else if (PatchtoAdd.DepositionType == DepositionTypeEnum.NewOverlappingPatches)
            {
                if (PatchtoAdd.AreaNewPatch <= 0.0)
                {
                    summary.WriteMessage(this, " Command to add patch did not supply a valid area fraction for the new patch. Command will be ignored.");
                    isDataOK = false;
                }
            }
            else if (PatchtoAdd.DepositionType == DepositionTypeEnum.ToAllPaddock)
            {
                // assume stuff is added homogeneously and with no patch creation, thus no factors are actually required
            }
            else
            {
                summary.WriteMessage(this, " Command to add patch did not supply a valid DepositionType. Command will be ignored.");
                isDataOK = false;
            }

            if (isDataOK)
            {
                List<int> PatchesToAddStuff;

                if ((PatchtoAdd.DepositionType == DepositionTypeEnum.ToNewPatch) ||
                    (PatchtoAdd.DepositionType == DepositionTypeEnum.NewOverlappingPatches))
                { // New patch(es) will be added
                    AddNewCNPatch(PatchtoAdd);
                }
                else if (PatchtoAdd.DepositionType == DepositionTypeEnum.ToSpecificPatch)
                {  // add stuff to selected patches, no new patch will be created

                    // 1. get the list of patch id's to which stuff will be added
                    PatchesToAddStuff = CheckPatchIDs(PatchtoAdd.AffectedPatches_id, PatchtoAdd.AffectedPatches_nm);
                    // 2. add the stuff to patches listed
                    AddStuffToPatches(PatchesToAddStuff, PatchtoAdd);
                }
                else
                {  // add stuff to all existing patches, no new patch will be created
                   // 1. create the list of patches receiving stuff (all)
                    PatchesToAddStuff = new List<int>();
                    for (int k = 0; k < patches.Count; k++)
                        PatchesToAddStuff.Add(k);
                    // 2. add the stuff to patches listed
                    AddStuffToPatches(PatchesToAddStuff, PatchtoAdd);
                }
            }
        }

        /// <summary>At the start of the simulation set up LifeCyclePhases</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // Create a new nutrient patch.
            var newPatch = new NutrientPatch(soil.Thickness, this);
            newPatch.CreationDate = clock.Today;
            newPatch.Name = "base";
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
                    if (Math.Abs(incomingDelta[layer]) > epsilon)
                    {
                        // 3.1- zero and initialise the variables
                        partitionWeight = new double[patches.Count];
                        thisLayerPatchSolute = new double[patches.Count];

                        // 3.2- get the solute amounts for each patch in this layer
                        if (partitionApproach == PartitionApproachEnum.BasedOnLayerConcentration ||
                           (partitionApproach == PartitionApproachEnum.BasedOnConcentrationAndDelta && incomingDelta[layer] < epsilon))
                        {
                            for (int k = 0; k < patches.Count; k++)
                                thisLayerPatchSolute[k] = existingSoluteAmount[k][layer] * patches[k].RelativeArea;
                        }
                        else if (partitionApproach == PartitionApproachEnum.BasedOnSoilConcentration ||
                                (partitionApproach == PartitionApproachEnum.BasedOnConcentrationAndDelta && incomingDelta[layer] >= epsilon))
                        {
                            for (int k = 0; k < patches.Count; k++)
                            {
                                double layerUsed = 0.0;
                                for (int z = layer; z >= 0; z--)        // goes backwards till soil surface (but may stop before that)
                                {
                                    thisLayerPatchSolute[k] += existingSoluteAmount[k][z];
                                    layerUsed += soil.Thickness[z];
                                    if ((LayerForNPartition > epsilon) && (layerUsed >= LayerForNPartition))
                                        // stop if thickness reaches a defined value
                                        z = -1;
                                }
                                thisLayerPatchSolute[k] *= patches[k].RelativeArea;
                            }
                        }

                        // 3.3- get the total solute amount for this layer
                        thisLayersTotalSolute = MathUtilities.Sum(thisLayerPatchSolute);

                        // 3.4- Check whether the existing solute is greater than the incoming delta
                        if (MathUtilities.IsLessThan(thisLayersTotalSolute + incomingDelta[layer], 0))
                            throw new Exception($"Attempt to change {soluteName}[{layer + 1}] to a negative value");

                        // 3.5- Compute the partition weights for each patch
                        for (int k = 0; k < patches.Count; k++)
                        {
                            partitionWeight[k] = 0.0;
                            if (thisLayersTotalSolute >= epsilon)
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
        /// Handles the addition of new CNPatches
        /// </summary>
        /// <param name="PatchtoAdd">Patch data</param>
        private void AddNewCNPatch(AddSoilCNPatchwithFOMType PatchtoAdd)
        {
            List<int> idPatchesJustAdded = new List<int>(); // list of IDs of patches created (exclude patches that would be too small)
            List<int> idPatchesToDelete = new List<int>(); //list of IDs of existing patches that became too small and need to be deleted
            List<int> idPatchesAffected; //list of IDs of patches affected by new addition

            // 1. get the list of id's of patches which are affected by this addition, and the area affected
            double AreaAffected = 0;
            if (PatchtoAdd.DepositionType == DepositionTypeEnum.ToNewPatch)
            {
                // check which patches are affected
                idPatchesAffected = CheckPatchIDs(PatchtoAdd.AffectedPatches_id, PatchtoAdd.AffectedPatches_nm);
                for (int i = 0; i < idPatchesAffected.Count; i++)
                    AreaAffected += patches[idPatchesAffected[i]].RelativeArea;
            }
            else if (PatchtoAdd.DepositionType == DepositionTypeEnum.NewOverlappingPatches)
            {
                // all patches are affected
                idPatchesAffected = new List<int>();
                for (int k = 0; k < patches.Count; k++)
                    idPatchesAffected.Add(k);
                AreaAffected = 1.0;
            }
            else
            {
                idPatchesAffected = new List<int>();
            }

            // check that total area of affected patches is larger than new patch area
            if (AreaAffected - PatchtoAdd.AreaNewPatch < -minimumPatchArea)
            {
                throw new Exception(" AddSoilCNPatch - area of selected patches (" + AreaAffected.ToString("#0.00#")
                                    + ") is smaller than area of new patch(" + PatchtoAdd.AreaNewPatch.ToString("#0.00#") +
                                    "). Command cannot be executed");
            }
            else
            {
                // check the area for each patch
                for (int i = 0; i < idPatchesAffected.Count; i++)
                {
                    double OldPatch_OldArea = patches[idPatchesAffected[i]].RelativeArea;
                    double NewPatch_NewArea = PatchtoAdd.AreaNewPatch * (OldPatch_OldArea / AreaAffected);
                    double OldPatch_NewArea = OldPatch_OldArea - NewPatch_NewArea;
                    if (NewPatch_NewArea < minimumPatchArea)
                    {
                        // area of patch to create is too small, patch will not be created
                        throw new Exception("   attempt to create a new patch with area too small or negative ("
                                            + NewPatch_NewArea.ToString("#0.00#") +
                                            "). The patch will not be created. Command cannot be executed");
                    }
                    else if (OldPatch_NewArea < -minimumPatchArea)
                    {
                        // area of patch to create is too big, patch will not be created
                        throw new Exception("   attempt to create a new patch with area greater than the existing patch area ("
                                            + NewPatch_NewArea.ToString("#0.00#") +
                                            "). The patch will not be created. Command cannot be executed");
                    }
                    else if (OldPatch_NewArea < minimumPatchArea)
                    {
                        // remaining area is too small or negative, patch will be created but old one will be deleted
                        summary.WriteWarning(this, " attempt to set the area of existing patch(" + idPatchesAffected[i].ToString()
                                          + ") to a value too small or negative (" + OldPatch_NewArea.ToString("#0.00#")
                                          + "). The patch will be eliminated.");

                        // mark old patch for deletion
                        idPatchesToDelete.Add(idPatchesAffected[i]);

                        // create new patch based on old one - the original one will be deleted later
                        ClonePatch(idPatchesAffected[i]);
                        int k = patches.Count - 1;
                        patches[k].RelativeArea = NewPatch_NewArea;
                        if (PatchtoAdd.AreaNewPatch > 0)
                        {
                            // a name was supplied
                            patches[k].Name = PatchtoAdd.PatchName + "_" + i.ToString();
                        }
                        else
                        {
                            // use default naming
                            patches[k].Name = "patch" + k.ToString();
                        }
                        patches[k].CreationDate = clock.Today;
                        idPatchesJustAdded.Add(k);
                    }
                    else
                    {
                        // create new patch by spliting an existing one
                        ClonePatch(idPatchesAffected[i]);
                        patches[idPatchesAffected[i]].RelativeArea = OldPatch_NewArea;
                        int k = patches.Count - 1;
                        patches[k].RelativeArea = NewPatch_NewArea;
                        if (PatchtoAdd.PatchName.Length > 0)
                        {
                            // a name was supplied
                            patches[k].Name = PatchtoAdd.PatchName + "_" + i.ToString();
                        }
                        else
                        {
                            // use default naming
                            patches[k].Name = "patch" + k.ToString();
                        }
                        patches[k].CreationDate = clock.Today;
                        idPatchesJustAdded.Add(k);
                        if (!PatchtoAdd.SuppressMessages)
                        {
                            summary.WriteMessage(this, "create new patch, with area = " + NewPatch_NewArea.ToString("#0.00#") +
                                         ", based on existing patch(" + idPatchesAffected[i].ToString() +
                                         ") - Old area = " + OldPatch_OldArea.ToString("#0.00#") +
                                         ", new area = " + OldPatch_NewArea.ToString("#0.00#"));
                        }
                    }
                }
            }

            // add the stuff to patches just created
            AddStuffToPatches(idPatchesJustAdded, PatchtoAdd);

            // delete the patches in excess
            if (idPatchesToDelete.Count > 0)
                DeletePatches(idPatchesToDelete);

        }


        /// <summary>
        /// Check the list of patch names and IDs passed by 'AddSoilCNPatch' event
        /// </summary>
        /// <remarks>
        /// Tasks performed by this method:
        ///  - Verify whether there are replicates in the list given
        ///  - Verify whether the IDs and/or names given correspond to existing patches
        ///  - Eliminate replicates and consolidate lists of IDs and names (merge both)
        /// </remarks>
        /// <param name="IDsToCheck">List of IDs or indices of patches</param>
        /// <param name="NamesToCheck">List of names of patches</param>
        /// <returns>List of patch IDs (negative if no ID is found)</returns>
        private List<int> CheckPatchIDs(int[] IDsToCheck, string[] NamesToCheck)
        {
            // List of patch IDs for output
            List<int> SelectedIDs = new List<int>();

            // 1. Check names
            if (NamesToCheck.Length > 0)
            {  // at least one name was given, check for existence and get ID
                for (int i_name = 0; i_name < NamesToCheck.Length; i_name++)
                {
                    bool isReplicate = false;
                    // check for replicates
                    for (int i = 0; i < i_name; i++)
                        if (NamesToCheck[i] == NamesToCheck[i_name])
                            isReplicate = true;

                    if (!isReplicate)
                    {
                        // Check for patch existence
                        for (int k = 0; k < patches.Count; k++)
                        {
                            if (NamesToCheck[i_name] == patches[k].Name)
                            {
                                // found the patch, add to list
                                SelectedIDs.Add(k);
                                k = patches.Count;
                            }
                            // else{}  continue looking for next name
                        }
                    }
                }
            }
            // else{} No names were given

            // 1. Check IDs
            if (IDsToCheck.Length > 0)
            {  // at least one ID was given, check for existence and get ID
                for (int i_id = 0; i_id < IDsToCheck.Length; i_id++)
                {
                    bool isReplicate = false;
                    if (SelectedIDs.Count > 0)
                    { // there are IDs in the list already, check for replicates
                        for (int i = 0; i < SelectedIDs.Count; i++)
                            if (SelectedIDs[i] == IDsToCheck[i_id])
                            { // already selected
                                isReplicate = true;
                                i = SelectedIDs.Count;
                            }
                    }
                    // check for replicates in list given
                    for (int i = 0; i < i_id; i++)
                        if (IDsToCheck[i] == IDsToCheck[i_id])
                            isReplicate = true;
                    if (!isReplicate)

                        // Check for patch existence
                        for (int k = 0; k < patches.Count; k++)
                        {
                            if (IDsToCheck[i_id] == k)
                            {
                                // found the patch, add to list
                                SelectedIDs.Add(k);
                                k = patches.Count;
                            }
                            // else{}  continue looking for next name
                        }
                }
            }
            // else{} No IDs were given

            if (SelectedIDs.Count == 0)
            { // no valid patch was found, notify user
                summary.WriteMessage(this, " No valid patch was found to base the new patch being added - operation will be ignored");
            }
            return SelectedIDs;
        }

        /// <summary>
        /// Controls the addition of several variables to the especified patches
        /// </summary>
        /// <param name="PatchesToAdd">The list of patches to which the stuff will be added</param>
        /// <param name="StuffToAdd">The values of the variables to add (supplied as deltas)</param>
        private void AddStuffToPatches(List<int> PatchesToAdd, AddSoilCNPatchwithFOMType StuffToAdd)
        {
            for (int i = PatchesToAdd.Count - 1; i >= 0; i--)
                patches[PatchesToAdd[i]].Add(StuffToAdd);
        }

        /// <summary>
        /// Clone an existing patch. That is, creates a new patch (k) based on an existing one (j)
        /// </summary>
        /// <param name="j">id of patch to be cloned</param>
        private void ClonePatch(int j)
        {
            // create new patch
            var newPatch = new NutrientPatch(patches[j]);
            patches.Add(newPatch);
            //int k = patches.Count - 1;

            //// copy the state variables from original patch in to the new one
            //patches[k].SetSolutes(patches[j]);
        }


        /// <summary>
        /// Delete patches in the list
        /// </summary>
        /// <param name="PatchesToDelete">List of patches to delete</param>
        private void DeletePatches(List<int> PatchesToDelete)
        {
            // sort the list
            PatchesToDelete.Sort();
            // go backwards so that the id of patches to delete do not change after each deletion
            for (int i = PatchesToDelete.Count - 1; i >= 0; i--)
            {
                patches.RemoveAt(PatchesToDelete[i]);
            }
        }
    }
}