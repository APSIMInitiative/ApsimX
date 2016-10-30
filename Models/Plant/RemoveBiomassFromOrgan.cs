using Models.PMF;
using Models.PMF.Organs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF
{
    class RemoveBiomassFromOrgan
    {
        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="value">The fractions of biomass to remove</param>
        /// <param name="organ">Organ to remove biomass from.</param>
        /// <param name="plant">Parent plant.</param>
        static public void DoRemoveBiomass(OrganBiomassRemovalType value, BaseOrgan organ, Models.PMF.Plant plant)
        {
            double totalFractionToRemove = value.FractionLiveToRemove + value.FractionDeadToRemove
                                           + value.FractionLiveToResidue + value.FractionDeadToResidue;

            double totalLiveFractionToRemove = value.FractionLiveToRemove + value.FractionLiveToResidue;
            double totalDeadFractionToRemove = value.FractionDeadToRemove + value.FractionDeadToResidue;

            if (totalLiveFractionToRemove > 1.0)
            {
                throw new Exception("The sum of FractionToResidue and FractionToRemove for "
                                    + value.Name
                                    + " is greater than 1 for live biomass.  Had this execption not triggered you would be removing more biomass from "
                                    + organ.Name + " than there is to remove");
            }
            if (totalDeadFractionToRemove > 1.0)
            {
                throw new Exception("The sum of FractionToResidue and FractionToRemove for "
                                    + value.Name
                                    + " is greater than 1 for dead biomass.  Had this execption not triggered you would be removing more biomass from "
                                    + organ.Name + " than there is to remove");
            }
            if (totalFractionToRemove > 0.0)
            {
                double RemainingLiveFraction = 1.0 - (value.FractionLiveToResidue + value.FractionLiveToRemove);
                double RemainingDeadFraction = 1.0 - (value.FractionDeadToResidue + value.FractionDeadToRemove);

                double detachingWt = organ.Live.Wt * value.FractionLiveToResidue + organ.Dead.Wt * value.FractionDeadToResidue;
                double detachingN = organ.Live.N * value.FractionLiveToResidue + organ.Dead.N * value.FractionDeadToResidue;
                organ.RemovedWt += organ.Live.Wt * value.FractionLiveToRemove + organ.Dead.Wt * value.FractionDeadToRemove;
                organ.RemovedN += organ.Live.N * value.FractionLiveToRemove + organ.Dead.N * value.FractionDeadToRemove;
                organ.DetachedWt += detachingWt;
                organ.DetachedN += detachingN;

                organ.Live.StructuralWt *= RemainingLiveFraction;
                organ.Live.NonStructuralWt *= RemainingLiveFraction;
                organ.Live.MetabolicWt *= RemainingLiveFraction;
                organ.Dead.StructuralWt *= RemainingDeadFraction;
                organ.Dead.NonStructuralWt *= RemainingDeadFraction;
                organ.Dead.MetabolicWt *= RemainingDeadFraction;

                organ.Live.StructuralN *= RemainingLiveFraction;
                organ.Live.NonStructuralN *= RemainingLiveFraction;
                organ.Live.MetabolicN *= RemainingLiveFraction;
                organ.Dead.StructuralN *= RemainingDeadFraction;
                organ.Dead.NonStructuralN *= RemainingDeadFraction;
                organ.Dead.MetabolicN *= RemainingDeadFraction;

                organ.SurfaceOrganicMatter.Add(detachingWt * 10, detachingN * 10, 0.0, plant.CropType, organ.Name);
                //TODO: theoretically the dead material is different from the live, so it should be added as a separate pool to SurfaceOM

                double toResidue = (value.FractionLiveToResidue + value.FractionDeadToResidue) / totalFractionToRemove * 100;
                double removedOff = (value.FractionLiveToRemove + value.FractionDeadToRemove) / totalFractionToRemove * 100;
                organ.Summary.WriteMessage(organ, "Removing " + (totalFractionToRemove * 100).ToString("0.0")
                                         + "% of " + organ.Name + " Biomass from " + plant.Name
                                         + ".  Of this " + removedOff.ToString("0.0") + "% is removed from the system and "
                                         + toResidue.ToString("0.0") + "% is returned to the surface organic matter");
            }
        }

    }
}
