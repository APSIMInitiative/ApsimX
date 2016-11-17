

namespace Models.PMF.Library
{
    using Models.Core;
    using Models.Interfaces;
    using Interfaces;
    using Soils;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class impliments biomass removal from live + dead pools.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class BiomassRemoval : Model
    {
        [Link]
        Plant plant = null;

        [Link]
        ISurfaceOrganicMatter surfaceOrganicMatter = null;

        [Link]
        Summary summary = null;

        /// <summary>Biomass removal defaults for different event types e.g. prune, cut etc.</summary>
        [ChildLink]
        public List<OrganBiomassRemovalType> defaults = null;

        /// <summary>Invoked when fresh organic matter needs to be incorporated into soil</summary>
        public event FOMLayerDelegate IncorpFOM;

        /// <summary>Removes biomass from live and dead biomass pools and send to surface organic matter</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="amount">The fractions of biomass to remove</param>
        /// <param name="Live">Live biomass pool</param>
        /// <param name="Dead">Dead biomass pool</param>
        /// <param name="Removed">The removed pool to add to.</param>
        /// <param name="Detached">The detached pool to add to.</param>
        /// <param name="writeToSummary">Write the biomass removal to summary file?</param>
        /// <returns>The remaining live fraction.</returns>
        public double RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amount, 
                                    Biomass Live, Biomass Dead, 
                                    Biomass Removed, Biomass Detached,
                                    bool writeToSummary = true)
        {
            if (amount == null)
                amount = FindDefault(biomassRemoveType);

            double totalFractionToRemove = amount.FractionLiveToRemove + amount.FractionDeadToRemove
                                           + amount.FractionLiveToResidue + amount.FractionDeadToResidue;
            
            if (totalFractionToRemove > 0.0)
            {
                Biomass detaching;
                double remainingLiveFraction = RemoveBiomassFromLiveAndDead(amount, Live, Dead, out Removed, Detached, out detaching);

                // Add the detaching biomass to surface organic matter model.
                //TODO: theoretically the dead material is different from the live, so it should be added as a separate pool to SurfaceOM
                surfaceOrganicMatter.Add(detaching.Wt * 10, detaching.N * 10, 0.0, plant.CropType, Name);

                if (writeToSummary)
                {
                    double toResidue = (amount.FractionLiveToResidue + amount.FractionDeadToResidue) / totalFractionToRemove * 100;
                    double removedOff = (amount.FractionLiveToRemove + amount.FractionDeadToRemove) / totalFractionToRemove * 100;
                    summary.WriteMessage(this, "Removing " + (totalFractionToRemove * 100).ToString("0.0")
                                             + "% of " + Parent.Name + " Biomass from " + plant.Name
                                             + ".  Of this " + removedOff.ToString("0.0") + "% is removed from the system and "
                                             + toResidue.ToString("0.0") + "% is returned to the surface organic matter");
                }
                return remainingLiveFraction;
            }

            return 1;
        }


        /// <summary>Removes biomass from live and dead biomass pools and send to soil</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="removal">The fractions of biomass to remove</param>
        /// <param name="Live">Live biomass pool</param>
        /// <param name="Dead">Dead biomass pool</param>
        /// <param name="Removed">The removed pool to add to.</param>
        /// <param name="Detached">The detached pool to add to.</param>
        public void RemoveBiomassToSoil(string biomassRemoveType, OrganBiomassRemovalType removal,
                                        Biomass[] Live, Biomass[] Dead,
                                        Biomass Removed, Biomass Detached)
        {
            if (removal == null)
                removal = FindDefault(biomassRemoveType);

            //NOTE: roots don't have dead biomass
            double totalFractionToRemove = removal.FractionLiveToRemove + removal.FractionLiveToResidue;

            if (totalFractionToRemove > 0)
            {
                //NOTE: at the moment Root has no Dead pool
                FOMLayerLayerType[] FOMLayers = new FOMLayerLayerType[Live.Length];
                double remainingFraction = 1.0 - (removal.FractionLiveToResidue + removal.FractionLiveToRemove);
                for (int layer = 0; layer < Live.Length; layer++)
                {
                    Biomass detaching;
                    double remainingLiveFraction = RemoveBiomassFromLiveAndDead(removal, Live[layer], Dead[layer], out Removed, Detached, out detaching);

                    FOMType fom = new FOMType();
                    fom.amount = (float)(detaching.Wt * 10);
                    fom.N = (float)(detaching.N * 10);
                    fom.C = (float)(0.40 * detaching.Wt * 10);
                    fom.P = 0.0;
                    fom.AshAlk = 0.0;

                    FOMLayerLayerType Layer = new FOMLayerLayerType();
                    Layer.FOM = fom;
                    Layer.CNR = 0.0;
                    Layer.LabileP = 0.0;
                    FOMLayers[layer] = Layer;
                }
                FOMLayerType FomLayer = new FOMLayerType();
                FomLayer.Type = plant.CropType;
                FomLayer.Layer = FOMLayers;
                IncorpFOM.Invoke(FomLayer);
            }
        }


        /// <summary>Finds a specific biomass removal default for the specified name</summary>
        /// <param name="name">Name of the event e.g. cut, prune etc.</param>
        /// <returns>Returns the default or null if not found.</returns>
        public OrganBiomassRemovalType FindDefault(string name)
        {
            OrganBiomassRemovalType amount = defaults.Find(d => d.Name == name);

            if (amount == null)
                throw new Exception("Cannot find biomass removal defaults: " + Parent.Name + ".BiomassRemovalDefaults." + name);

            if (amount.FractionLiveToRemove + amount.FractionLiveToResidue > 1.0)
                throw new Exception("The sum of FractionToResidue and FractionToRemove for "
                                    + Parent.Name
                                    + " is greater than 1 for live biomass.  Had this execption not triggered you would be removing more biomass from "
                                    + Name + " than there is to remove");
            if (amount.FractionDeadToRemove + amount.FractionDeadToResidue > 1.0)
                throw new Exception("The sum of FractionToResidue and FractionToRemove for "
                                    + Parent.Name
                                    + " is greater than 1 for dead biomass.  Had this execption not triggered you would be removing more biomass from "
                                    + Name + " than there is to remove");
            return amount;
        }


        /// <summary>Removes biomass from live and dead biomass pools</summary>
        /// <param name="amount">The fractions of biomass to remove</param>
        /// <param name="Live">Live biomass pool</param>
        /// <param name="Dead">Dead biomass pool</param>
        /// <param name="Removed">The removed pool to add to.</param>
        /// <param name="Detached">The detached pool to add to.</param>
        /// <param name="detaching">The amount of detaching material</param>
        private static double RemoveBiomassFromLiveAndDead(OrganBiomassRemovalType amount, Biomass Live, Biomass Dead, out Biomass Removed, Biomass Detached, out Biomass detaching)
        {
            double remainingLiveFraction = 1.0 - (amount.FractionLiveToResidue + amount.FractionLiveToRemove);
            double remainingDeadFraction = 1.0 - (amount.FractionDeadToResidue + amount.FractionDeadToRemove);

            detaching = Live * amount.FractionLiveToResidue + Dead * amount.FractionDeadToResidue;
            Removed = Live * amount.FractionLiveToRemove + Dead * amount.FractionDeadToRemove;
            Detached.Add(detaching);

            Live.Multiply(remainingLiveFraction);
            Dead.Multiply(remainingDeadFraction);
            return remainingLiveFraction;
        }
    }
}