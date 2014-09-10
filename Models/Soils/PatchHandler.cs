using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.Soils
{

    /// <summary>
    /// This partial class contains the various methods to handle patches
    /// </summary>

    public partial class SoilNitrogen
    {

        public class AddSoilCNPatchType
        {
            public String Sender = "";
            public String DepositionType = "";
            public String[] AffectedPatches_nm;
            public Int32[] AffectedPatches_id;
            public Double AreaFraction;
            public String PatchName = "";
            public Double[] Water;
            public Double[] Urea;
            public Double[] NH4;
            public Double[] NO3;
            public Double[] POX;
            public Double[] SO4;
            public Double[] AshAlk;
            public Double[] FOM_C;
            public Double[] FOM_C_pool1;
            public Double[] FOM_C_pool2;
            public Double[] FOM_C_pool3;
            public Double[] FOM_N;
        }

        [Link]
        ISummary Summary = null;






        /// <summary>
        /// Handles the addition of new CNPatches
        /// </summary>
        /// <param name="PatchtoAdd">Patch data</param>
        private void AddNewCNPatch(AddSoilCNPatchType PatchtoAdd)
        {
            // Data passed from OnAddSoilCNPatch event:
            //.Sender: the name of the module that raised this event
            //.DepositionType: the type of deposition:
            //  - ToAllPaddock: No patch is created, add stuff as given to all patches. It is the default;
            //  - ToSpecificPatch: No patch is created, add stuff to given patches;
            //		(recipient patch is given using its index or name; if not supplied, defaults to homogeneous)
            //  - ToNewPatch: create new patch based on an existing patch, add stuff to created patch;
            //		- recipient or base patch is given using index or name; if not supplied, new patch will be based on the base/Patch[0];
            //      - patches are only created is area is larger than a minimum (minPatchArea);
            //      - new areas are proportional to existing patches;
            //  - NewOverlappingPatches: create new patch(es), these overlap with all existing patches, add stuff to created patches;
            //		(new patches are created only if their area is larger than a minimum (minPatchArea))
            //.AffectedPatches_id (AffectedPatchesByIndex): the index of the existing patches to which urine will be added
            //.AffectedPatches_nm (AffectedPatchesByName): the name of the existing patches to which urine will be added
            //.AreaFraction: the relative area of the patch (0-1)
            //.PatchName: the name(s) of the patch)es) being created

            List<int> PatchesToDelete = new List<int>();
            List<int> PatchesJustAdded = new List<int>();


            // get the list of id's of patches which are affected by this addition, and the area affected
            int[] PatchIDs = new int[1];
            double AreaAffected = 0;
            if (PatchtoAdd.DepositionType.ToLower() == "ToNewPatch".ToLower())
            {  // check which patches are affected
                PatchIDs = CheckPatchIDs(PatchtoAdd.AffectedPatches_id, PatchtoAdd.AffectedPatches_nm);
                for (int i = 0; i < PatchIDs.Length; i++)
                    AreaAffected += Patch[PatchIDs[i]].RelativeArea;
            }
            else if (PatchtoAdd.DepositionType.ToLower() == "NewOverlappingPatches".ToLower())
            {  // all patches are affected
                PatchIDs = new int[Patch.Count];
                for (int k = 0; k < Patch.Count; k++)
                    PatchIDs[k] = k;
                AreaAffected = 1.0;
            }

            // check that total area of affected patches is larger than new patch area
            if (AreaAffected < PatchtoAdd.AreaFraction)
            {
                // Existing area is smaller than new patch area, cannot continue
                throw new Exception(" Cannot create new patch, area of selected patches (" + AreaAffected.ToString("#0.00#")
                                   + ") is smaller than area of new patch(" + PatchtoAdd.AreaFraction.ToString("#0.00#") + ")");
            }
            else
            {  // check the area for each patch
                for (int i = 0; i < PatchIDs.Length; i++)
                {
                    double OldPatch_OldArea = Patch[PatchIDs[i]].RelativeArea;
                    double NewPatch_NewArea = PatchtoAdd.AreaFraction * (OldPatch_OldArea / AreaAffected);
                    double OldPatch_NewArea = OldPatch_OldArea - NewPatch_NewArea;
                    if (NewPatch_NewArea < MinimumPatchArea)
                    {  // area to create is too small, patch will not be created
                        Summary.WriteMessage(this, "   attempt to create a new patch with area too small or negative ("
                            + NewPatch_NewArea.ToString("#0.00#") + "). The patch will not be created.");
                    }
                    else if (OldPatch_NewArea < MinimumPatchArea)
                    {  // remaining area is too small or negative, patch will be created but old one will be deleted
                        Summary.WriteMessage(this, " attempt to set the area of existing patch(" + PatchIDs[i].ToString()
                            + ") to a value too small or negative (" + OldPatch_NewArea.ToString("#0.00#")
                            + "). The patch will be eliminated.");

                        // mark old patch for deletion
                        PatchesToDelete.Add(PatchIDs[i]);

                        // create new patch based on old one - uses SplitPatch, the original one will be deleted later
                        SplitPatch(PatchIDs[i]);
                        int k = Patch.Count - 1;
                        if (PatchtoAdd.AreaFraction > 0)
                        {  // a name was supplied
                            Patch[k].PatchName = PatchtoAdd.AreaFraction + "_" + i.ToString();
                        }
                        else
                        {  // use default naming
                            Patch[k].PatchName = "Patch" + k.ToString();
                        }
                        PatchesJustAdded.Add(k);
                    }
                    else
                    {
                        // create new patch by spliting an existing one
                        SplitPatch(PatchIDs[i]);
                        Patch[PatchIDs[i]].RelativeArea = OldPatch_NewArea;
                        int k = Patch.Count - 1;
                        Patch[k].RelativeArea = NewPatch_NewArea;
                        if (PatchtoAdd.PatchName.Length > 0)
                        {  // a name was supplied
                            Patch[k].PatchName = PatchtoAdd.AreaFraction + "_" + i.ToString();
                        }
                        else
                        {  // use default naming
                            Patch[k].PatchName = "Patch" + k.ToString();
                        }
                        PatchesJustAdded.Add(k);
                        Summary.WriteMessage(this, " create new patch, with area = " + NewPatch_NewArea.ToString("#0.00#") + ", based on existing patch("
                            + PatchIDs[i].ToString() + ") - Old area = " + OldPatch_OldArea.ToString("#0.00#") + ", new area = "
                            + OldPatch_NewArea.ToString("#0.00#"));
                    }
                }
            }

            // add the stuff to patches just created
            AddStuffToPatches(PatchesJustAdded, PatchtoAdd);

            // delete the patches in excess
            if (PatchesToDelete.Count > 0)
                DeletePatches(PatchesToDelete);

        }

        /// <summary>
        /// Split an existing patch in two. That is, creates a new patch (k) based on an existing one (j)
        /// </summary>
        /// <param name="j"></param>
        private void SplitPatch(int j)
        {
            // create new patch
            soilCNPatch newPatch = new soilCNPatch(this);
            Patch.Add(newPatch);
            int k = Patch.Count - 1;

            // set the size of arrays
            Patch[k].ResizeLayerArrays(dlayer.Length);

            // set C and N variables to the same state as the 'mother' patch
            for (int layer = 0; layer < dlayer.Length; layer++)
            {
                Patch[k].urea[layer] = Patch[j].urea[layer];
                Patch[k].nh4[layer] = Patch[j].nh4[layer];
                Patch[k].no3[layer] = Patch[j].no3[layer];
                Patch[k].inert_c[layer] = Patch[j].inert_c[layer];
                Patch[k].biom_c[layer] = Patch[j].biom_c[layer];
                Patch[k].biom_n[layer] = Patch[j].biom_n[layer];
                Patch[k].hum_c[layer] = Patch[j].hum_c[layer];
                Patch[k].hum_n[layer] = Patch[j].hum_n[layer];
                Patch[k].fom_c_pool1[layer] = Patch[j].fom_c_pool1[layer];
                Patch[k].fom_c_pool2[layer] = Patch[j].fom_c_pool2[layer];
                Patch[k].fom_c_pool3[layer] = Patch[j].fom_c_pool3[layer];
                Patch[k].fom_n_pool1[layer] = Patch[j].fom_n_pool1[layer];
                Patch[k].fom_n_pool2[layer] = Patch[j].fom_n_pool2[layer];
                Patch[k].fom_n_pool3[layer] = Patch[j].fom_n_pool3[layer];
            }

            // store today's values
            Patch[k].InitCalc();
        }

        /// <summary>
        /// Controls the merging of a group of patches into a single one
        /// </summary>
        /// <param name="PatchesToMerge">List of patches to merge</param>
        private void AmalgamatePatches(List<int> PatchesToMerge)
        {
            while (PatchesToMerge.Count > 1)
            {
                MergePatches(PatchesToMerge[0], PatchesToMerge[1]); // merge patch_1 into patch_0
                PatchesToMerge.RemoveAt(1);                         // delete reference to patch_1
            }
        }

        /// <summary>
        /// Merges two patches
        /// </summary>
        /// <param name="recipient">Patch which will recieve the areas and the status of the disappearing patch</param>
        /// <param name="disappearing">Patch that will no longer exist</param>
        private void MergePatches(int recipient, int disappearing)
        {
            // get the weighted average for each variable and assign to the recipient patch
            double[] newValue = new double[dlayer.Length];
            for (int layer = 0; layer < dlayer.Length; layer++)
            {
                Patch[recipient].urea[layer] = (Patch[recipient].urea[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].urea[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].nh4[layer] = (Patch[recipient].nh4[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].nh4[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].no3[layer] = (Patch[recipient].no3[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].no3[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].inert_c[layer] = (Patch[recipient].inert_c[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].inert_c[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].biom_c[layer] = (Patch[recipient].biom_c[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].biom_c[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].biom_n[layer] = (Patch[recipient].biom_n[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].biom_n[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].hum_c[layer] = (Patch[recipient].hum_c[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].hum_c[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].hum_n[layer] = (Patch[recipient].hum_n[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].hum_n[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].fom_c_pool1[layer] = (Patch[recipient].fom_c_pool1[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].fom_c_pool1[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].fom_c_pool2[layer] = (Patch[recipient].fom_c_pool2[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].fom_c_pool2[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].fom_c_pool3[layer] = (Patch[recipient].fom_c_pool3[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].fom_c_pool3[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].fom_n_pool1[layer] = (Patch[recipient].fom_n_pool1[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].fom_n_pool1[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].fom_n_pool2[layer] = (Patch[recipient].fom_n_pool2[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].fom_n_pool2[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
                Patch[recipient].fom_n_pool3[layer] = (Patch[recipient].fom_n_pool3[layer] * Patch[recipient].RelativeArea
                    + Patch[disappearing].fom_n_pool3[layer] * Patch[disappearing].RelativeArea) / Patch[recipient].RelativeArea;
            }

            // delete disappearing patch
            Patch.RemoveAt(disappearing);
        }

        /// <summary>
        /// Delete patches in the list
        /// </summary>
        /// <param name="PatchesToDelete">List of patches to delete</param>
        private void DeletePatches(List<int> PatchesToDelete)
        {
            // go backwards so that the id of patches to delete do not change after each deletion
            for (int i = PatchesToDelete.Count; i >= 0; i--)
            {
                Patch.RemoveAt(PatchesToDelete[i]);
            }
        }

        /// <summary>
        /// Check the list of patch names and ids passed by 'AddSoilCNPatch' event
        /// </summary>
        /// <remarks>
        /// Verify whether the names correspond to existing patches, verify whether there are replicates
        /// </remarks>
        /// <documentation>
        /// With the AddSoilCNPatch event the user can tell the index or the name of the patch to which urine (or whatever) is added;
        ///  this function will then check whether the patch(es) exist or not and filter out any replicates.
        ///  User can pass ids (indices) or names of patches, or even both. This function will check both, eliminate replicates and
        ///   non-existent references. The output is only the indices of selected patches
        /// </documentation>
        /// <param name="IDsToCheck">IDs or indices of patches</param>
        /// <param name="NamesToCheck">Name of patches</param>
        /// <returns>List of patch IDs, or indices</returns>
        private int[] CheckPatchIDs(int[] IDsToCheck, string[] NamesToCheck)
        {
            // List of patch ids for output
            List<int> SelectedIDs = new List<int>();

            if (Math.Max(IDsToCheck.Length, NamesToCheck.Length) > 0)
            {  // at least one patch has been selected
                if (NamesToCheck.Length > 0)
                {  // at least one name was selected, check value and get id
                    for (int pName = 0; pName < NamesToCheck.Length; pName++)
                    {
                        int IDtoAdd = -1;
                        for (int k = 0; k < Patch.Count; k++)
                        {
                            if (NamesToCheck[pName] == Patch[k].PatchName)
                            {  // found the patch, check for replicates
                                if (SelectedIDs.Count < 1)
                                { // this is the first patch, store the id
                                    IDtoAdd = k;
                                    k = Patch.Count;
                                }
                                else
                                {
                                    for (int i = 0; i <= SelectedIDs.Count; i++)
                                    {
                                        if (SelectedIDs[i] == k)
                                        {  // id already selected
                                            i = SelectedIDs.Count;
                                            k = Patch.Count;
                                        }
                                        else
                                        {  // store the id
                                            IDtoAdd = k;
                                            i = SelectedIDs.Count;
                                            k = Patch.Count;
                                        }
                                    }
                                }
                            }
                            // else
                            // do nothing, continue looking for next name
                        }
                        if (IDtoAdd >= 0)
                        { // a patch was found
                            SelectedIDs.Add(IDtoAdd);
                        }
                        else
                        {  // name passed did not correspond to any patch
                            Summary.WriteMessage(this, "  the patch name '" + NamesToCheck[pName] + "' did not correspond to any existing patch."
                                + " Patch will be ignored.");
                        }
                    }
                }
                if (IDsToCheck.Length > 0)
                {  // at least one ID was selected, check value
                    for (int pId = 0; pId < IDsToCheck.Length; pId++)
                    {
                        int IDtoAdd = -1;
                        for (int k = 0; k < Patch.Count; k++)
                        {
                            if (IDsToCheck[pId] == k)
                            {  // found the patch, check for replicates
                                if (SelectedIDs.Count < 1)
                                { // this is the first patch, store the id
                                    IDtoAdd = k;
                                    k = Patch.Count;
                                }
                                else
                                {
                                    for (int i = 0; i < SelectedIDs.Count; i++)
                                    {
                                        if (SelectedIDs[i] == k)
                                        {  // id already selected
                                            i = SelectedIDs.Count;
                                            k = Patch.Count;
                                        }
                                        else
                                        {  // store the id
                                            IDtoAdd = k;
                                            i = SelectedIDs.Count;
                                            k = Patch.Count;
                                        }
                                    }
                                }
                            }
                            // else
                            // do nothing, continue looking for next ID
                        }
                        if (IDtoAdd >= 0)
                        { // a patch was found
                            SelectedIDs.Add(IDtoAdd);
                        }
                        else
                        {  // id passed did not correspond to any patch
                            Summary.WriteMessage(this, "  the patch id '" + IDsToCheck[pId] + "' did not correspond to any existing patch."
                                + " Patch will be ignored.");
                        }
                    }
                }
            }
            else
            {  // no patch was indicated, use 'base'
                SelectedIDs.Add(0);
            }
            // pass data into an array to return as result
            int[] result = new int[SelectedIDs.Count];
            for (int i = 0; i < SelectedIDs.Count; i++)
                result[i] = SelectedIDs[i];
            return result;
        }

        /// <summary>
        /// Compare the diffs for all existing patches
        /// </summary>
        /// <returns>The IDs of pairs that can be merged</returns>
        private PatchIDs ComparePatches()
        {
            // Note:
            //	The code added so far is only tentative, better rulles to decide which patches are similar need to be developed

            PatchIDs Patches = new PatchIDs();
            List<int> recipient = new List<int>();
            List<int> disappearing = new List<int>();

            for (int k1 = 0; k1 < Patch.Count; k1++)
            {
                for (int k2 = k1 + 1; k2 < Patch.Count; k2++)
                {
                    // go through a series of criteria t evaluate whether the two patches can be merged
                    if (Math.Abs(Patch[k1].carbon_tot[0] - Patch[k2].carbon_tot[0]) < EPSILON)
                    {
                        if (Math.Abs(Patch[k1].no3[0] - Patch[k2].no3[0]) < EPSILON)
                        {
                            recipient.Add(k1);
                            disappearing.Add(k2);
                        }
                    }
                }
            }
            Patches.recipient = recipient;
            Patches.disappearing = disappearing;
            return Patches;
        }

        /// <summary>
        /// Controls the addition of several variables to the especified patches
        /// </summary>
        /// <param name="PatchesToAdd">The list of patches to which the stuff will be added</param>
        /// <param name="StuffToAdd">The values of the variables to add (supplied as deltas)</param>
        private void AddStuffToPatches(List<int> PatchesToAdd, AddSoilCNPatchType StuffToAdd)
        {
            // Data passed from OnAddSoilCNPatch event - these are all considered deltas:
            //.Water: amount of water to add per layer (mm), not handled here
            //.Urea: amount of urea to add per layer (kgN/ha)
            //.Urea: amount of urea to add (per layer) - Do we need other N forms?
            //.NH4: amount of ammonium to add per layer (kgN/ha)
            //.NO3: amount of nitrate to add per layer (kgN/ha)
            //.POX: amount of POx to add per layer (kgP/ha)
            //.SO4: amount of SO4 to add per layer (kgS/ha)
            //.Ashalk: ash amount to add per layer (mol/ha)
            //.FOM_C: amount of carbon in fom (all pools) to add per layer (kgC/ha)  -  If present, the pools will be ignored
            //.FOM_C_Pool1: amount of carbon in fom_pool1 to add per layer (kgC/ha)
            //.FOM_C_Pool2: amount of carbon in fom_pool2 to add per layer (kgC/ha)
            //.FOM_C_Pool3: amount of carbon in fom_pool3 to add per layer (kgC/ha)
            //.FOM_N: amount of nitrogen in fom to add per layer (kgN/ha)

            for (int i = PatchesToAdd.Count - 1; i >= 0; i--)
            {
                if ((StuffToAdd.Urea != null) && (StuffToAdd.Urea.Sum() > 0))
                    Patch[PatchesToAdd[i]].dlt_urea = StuffToAdd.Urea;
                if ((StuffToAdd.NH4 != null) && (StuffToAdd.NH4.Sum() > 0))
                    Patch[PatchesToAdd[i]].dlt_nh4 = StuffToAdd.NH4;
                if ((StuffToAdd.NO3 != null) && (StuffToAdd.NO3.Sum() > 0))
                    Patch[PatchesToAdd[i]].dlt_no3 = StuffToAdd.NO3;
                if ((StuffToAdd.FOM_C != null) && (StuffToAdd.FOM_C.Sum() > 0))
                {
                    Patch[PatchesToAdd[i]].dlt_org_c_pool1 = StuffToAdd.FOM_C;
                    Patch[PatchesToAdd[i]].dlt_org_c_pool2 = StuffToAdd.FOM_C;
                    Patch[PatchesToAdd[i]].dlt_org_c_pool3 = StuffToAdd.FOM_C;
                }
                else
                {
                    if ((StuffToAdd.FOM_C != null) && (StuffToAdd.FOM_C_pool1.Sum() > 0))
                        Patch[PatchesToAdd[i]].dlt_org_c_pool1 = StuffToAdd.FOM_C_pool1;
                    if ((StuffToAdd.FOM_C != null) && (StuffToAdd.FOM_C_pool2.Sum() > 0))
                        Patch[PatchesToAdd[i]].dlt_org_c_pool2 = StuffToAdd.FOM_C_pool2;
                    if ((StuffToAdd.FOM_C != null) && (StuffToAdd.FOM_C_pool3.Sum() > 0))
                        Patch[PatchesToAdd[i]].dlt_org_c_pool3 = StuffToAdd.FOM_C_pool3;
                }

                if ((StuffToAdd.FOM_N != null) && (StuffToAdd.FOM_N.Sum() > 0))
                    Patch[PatchesToAdd[i]].dlt_org_n = StuffToAdd.FOM_N;
            }

        }

        /// <summary>
        /// calculate how the dlt's (C and N) are partitioned amongst patches
        /// </summary>
        /// <param name="incomingDelta">The dlt to be partioned amongst patches</param>
        /// <param name="SoluteName">The solute or pool that is changing</param>
        /// <returns>The values of dlt for each existing patch</returns>
        private double[][] partitionDelta(double[] incomingDelta, string SoluteName, string PartitionType)
        {
            // 1- initialise the result to zero
            double[][] result = new double[Patch.Count][];
            for (int k = 0; k < Patch.Count; k++)
                result[k] = new double[dlayer.Length];

            try
            {
                // 2- gather how much solute is already in the soil
                double[][] alreadyThere = new double[Patch.Count][];
                for (int k = 0; k < Patch.Count; k++)
                {
                    switch (SoluteName)
                    {
                        case "urea":
                            alreadyThere[k] = Patch[k].urea;
                            break;
                        case "nh4":
                            alreadyThere[k] = Patch[k].nh4;
                            break;
                        case "no3":
                            alreadyThere[k] = Patch[k].no3;
                            break;
                        default:
                            throw new Exception(" The solute" + SoluteName
                                + " is not recognised by SoilNitrogen -  solute partition");
                    }
                }

                // 3- calculations are done for each layer 
                for (int layer = 0; layer < (dlayer.Length); layer++)
                {
                    // 3.1- compute the total solute amount, over all patches
                    double totalSolute = 0.0;
                    double[] patchSolute = new double[Patch.Count];
                    if ((PartitionType == "BasedOnLayerConcentration".ToLower()) ||
                        (PartitionType == "BasedOnConcentrationAndDelta".ToLower() & incomingDelta[layer] <= 0))
                    {
                        for (int k = 0; k < Patch.Count; k++)
                        {
                            totalSolute += alreadyThere[k][layer] * Patch[k].RelativeArea;
                            patchSolute[k] += alreadyThere[k][layer];
                        }
                    }
                    else if ((PartitionType == "BasedOnSoilConcentration".ToLower()) ||
                             (PartitionType == "BasedOnConcentrationAndDelta".ToLower() & incomingDelta[layer] > 0))
                    {
                        for (int k = 0; k < Patch.Count; k++)
                            for (int z = layer; z >= 0; z--)
                            {
                                totalSolute += alreadyThere[k][z] * Patch[k].RelativeArea;
                                patchSolute[k] += alreadyThere[k][z];
                            }
                    }

                    // 3.2- calculations for each patch
                    for (int k = 0; k < Patch.Count; k++)
                    {
                        // 3.2.1- compute the weights (based on existing solute amount)
                        double weight = 1.0;
                        if (totalSolute > 0)
                            weight = patchSolute[k] / totalSolute;

                        // 3.2.2- partition the dlt's for each patch
                        result[k][layer] = incomingDelta[layer] * weight;
                        if (result[k][0] < -0.72 && Clock.Today.DayOfYear > 53)
                            weight += 0.0;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(" problems with partitioning " + SoluteName + "- " + e.ToString());
            }
            return result;
        }
    }


}