

namespace Models.PMF.Library
{
    using Models.Core;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class impliments biomass removal from live + dead pools.
    /// </summary>
    [Serializable]
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

        /// <summary>Removes biomass from live and dead biomass pools.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="amount">The fractions of biomass to remove</param>
        /// <param name="Live">Live biomass pool</param>
        /// <param name="Dead">Dead biomass pool</param>
        /// <param name="Removed">The removed pool to add to.</param>
        /// <param name="Detached">The detached pool to add to.</param>
        /// <param name="writeToSummary">Write the biomass removal to summary file?</param>
        public void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amount, 
                                  Biomass Live, Biomass Dead, 
                                  Biomass Removed, Biomass Detached,
                                  bool writeToSummary = true)
        {
            if (amount == null)
                amount = FindDefault(biomassRemoveType);

            if (amount == null)
                throw new Exception("Cannot find biomass removal defaults: " + Parent.Name + ".BiomassRemovalDefaults." + biomassRemoveType);

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

            double totalFractionToRemove = amount.FractionLiveToRemove + amount.FractionDeadToRemove
                                           + amount.FractionLiveToResidue + amount.FractionDeadToResidue;
            
            if (totalFractionToRemove > 0.0)
            {
                double RemainingLiveFraction = 1.0 - (amount.FractionLiveToResidue + amount.FractionLiveToRemove);
                double RemainingDeadFraction = 1.0 - (amount.FractionDeadToResidue + amount.FractionDeadToRemove);

                Biomass detaching = Live * amount.FractionLiveToResidue + Dead * amount.FractionDeadToResidue;
                Removed = Live * amount.FractionLiveToRemove + Dead * amount.FractionDeadToRemove;
                Detached.Add(detaching);

                Live.Multiply(RemainingLiveFraction);
                Dead.Multiply(RemainingDeadFraction);

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
            }
        }

        /// <summary>Finds a specific biomass removal default for the specified name</summary>
        /// <param name="name">Name of the event e.g. cut, prune etc.</param>
        /// <returns>Returns the default or null if not found.</returns>
        public OrganBiomassRemovalType FindDefault(string name)
        {
            return defaults.Find(d => d.Name == name);
        }
    }
}