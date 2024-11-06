using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;

namespace Models.ForageDigestibility
{
    /// <summary>Encapsulates a model that has zero or more digestible biomass instances.</summary>
    public class ModelWithDigestibleBiomass
    {
        private readonly Forages forages;
        private readonly IHasDamageableBiomass forageModel;
        private readonly IEnumerable<ForageMaterialParameters> parameters;

        /// <summary>Constructor.</summary>
        /// <param name="forages">Instance of forages model.</param>
        /// <param name="forageModel">A model that has damageable biomass.</param>
        /// <param name="parameters">Parameters for this forage.</param>
        public ModelWithDigestibleBiomass(Forages forages, IHasDamageableBiomass forageModel, IEnumerable<ForageMaterialParameters> parameters)
        {
            this.forages = forages;
            this.forageModel = forageModel;
            this.parameters = parameters;
        }

        /// <summary>Name of forage model.</summary>
        public string Name => forageModel.Name;

        /// <summary>Zone that forage is located in.</summary>
        public Zone Zone => (forageModel as IModel).FindAncestor<Zone>();

        /// <summary>A collection of digestible material that can be grazed.</summary>
        public IEnumerable<DamageableBiomass> Material
        {
            get
            {
                foreach (var material in forageModel.Material)
                {
                    var fractionConsumable = forages.GetFractionConsumable(material);
                    if (fractionConsumable > 0)
                    {
                        var minimumConsumable = forages.GetMinimumConsumable(material) / 10; // kg/ha to g/m2
                        var consumableAmount = Math.Max(0.0, material.Total.Wt * fractionConsumable - minimumConsumable);
                        var consumableFraction = MathUtilities.Divide(consumableAmount, material.Total.Wt, 1.0);

                        yield return new DamageableBiomass(material.Name, material.Total, consumableFraction, material.IsLive, material.DigestibilityFromModel);
                    }
                }
            }
        }

        /// <summary>Remove biomass from organ.</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="deadToRemove">Fraction of dead biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="deadToResidue">Fraction of dead biomass to remove and send to residue pool(0-1).</param>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        public double RemoveBiomass(double liveToRemove = 0, double deadToRemove = 0, double liveToResidue = 0, double deadToResidue = 0)
        {
            return forageModel.RemoveBiomass(liveToRemove, deadToRemove, liveToResidue, deadToResidue);
        }

        /// <summary>Removes a given amount of biomass (and N) from the plant.</summary>
        /// <param name="amountToRemove">The amount of biomass to remove (g/m2).</param>
        /// <param name="PreferenceForGreenOverDead">Relative preference for live over dead material during graze (>0.0).</param>
        /// <param name="PreferenceForLeafOverStems">Relative preference for leaf over stem-stolon material during graze (>0.0).</param>
        /// <param name="summary">Optional summary object.</param>
        public Forages.MaterialRemoved RemoveBiomass(double amountToRemove,
                                             double PreferenceForGreenOverDead = 1.0,
                                             double PreferenceForLeafOverStems = 1.0,
                                             ISummary summary = null)
        {
            if (!MathUtilities.FloatsAreEqual(amountToRemove, 0.0, 0.000000001))
            {
                var allMaterial = Material.ToList();

                // get existing DM and N amounts
                double harvestableWt = allMaterial.Sum(m => m.Consumable.Wt);
                double preRemovalDMShoot = allMaterial.Sum(m => m.Total.Wt);
                double preRemovalNShoot = allMaterial.Sum(m => m.Total.N);

                // Compute the fraction of each tissue to be removed
                var fracRemoving = new List<FractionDigestibleBiomass>();
                if (MathUtilities.FloatsAreEqual(amountToRemove - harvestableWt, 0.0, 0.000000001))
                {
                    // All existing DM is removed
                    amountToRemove = harvestableWt;
                    foreach (var material in allMaterial)
                    {
                        double frac = MathUtilities.Divide(material.Consumable.Wt, harvestableWt, 0.0);
                        fracRemoving.Add(new FractionDigestibleBiomass(material, frac));
                    }
                }
                else
                {
                    // Initialise the fractions to be removed (these will be normalised later)
                    foreach (var material in allMaterial)
                    {
                        double frac;
                        if (material.IsLive)
                        {
                            if (material.Name == "Leaf")
                                frac = material.Consumable.Wt * PreferenceForGreenOverDead * PreferenceForLeafOverStems;
                            else
                                frac = material.Consumable.Wt * PreferenceForGreenOverDead;
                        }
                        else
                        {
                            if (material.Name == "Leaf")
                                frac = material.Consumable.Wt * PreferenceForLeafOverStems;
                            else
                                frac = material.Consumable.Wt;
                        }
                        fracRemoving.Add(new FractionDigestibleBiomass(material, frac));
                    }

                    // Normalise the fractions of each tissue to be removed, they should add to one
                    double totalFrac = fracRemoving.Sum(m => m.Fraction);
                    foreach (var f in fracRemoving)
                    {
                        double fracRemovable = f.Material.Consumable.Wt / amountToRemove;
                        f.Fraction = Math.Min(fracRemovable, f.Fraction / totalFrac);
                    }

                    // Iterate until sum of fractions to remove is equal to one
                    //  The initial normalised fractions are based on preference and existing DM. Because the value of fracRemoving is limited
                    //   to fracRemovable, the sum of fracRemoving may not be equal to one, as it should be. We need to iterate adjusting the
                    //   values of fracRemoving until we get a sum close enough to one. The previous values are used as weighting factors for
                    //   computing new ones at each iteration.

                    // NB: Does this while loop do anything at all! I'm not sure what it is supposed to do. (Dean)
                    int count = 1;
                    totalFrac = fracRemoving.Sum(m => m.Fraction);
                    while (!MathUtilities.FloatsAreEqual(1.0 - totalFrac, 0.0, 0.000000001))
                    {
                        count += 1;
                        foreach (var f in fracRemoving)
                        {
                            double fracRemovable = f.Material.Consumable.Wt / amountToRemove;
                            f.Fraction = Math.Min(fracRemovable, f.Fraction / totalFrac);
                        }
                        totalFrac = fracRemoving.Sum(m => m.Fraction);
                        if (count > 1000)
                        {
                            summary?.WriteMessage(forageModel as IModel, "SimpleGrazing could not remove or graze all the DM required for " + forageModel.Name, MessageType.Warning);
                            break;
                        }
                    }
                }

                // Get digestibility of DM being harvested (do this before updating pools)
                double defoliatedDigestibility = fracRemoving.Sum(m => forages.GetDigestibility(m.Material) * m.Fraction);

                // Iterate through all live material, find the associated dead material and then
                // tell the forage model to remove it.
                var liveMaterial = fracRemoving.Where(f => f.Material.IsLive).ToList();
                foreach (var live in liveMaterial)
                {
                    var dead = fracRemoving.Find(frac => frac.Material.Name == live.Material.Name && !frac.Material.IsLive);
                    if (dead == null)
                    {
                        throw new Exception("Cannot find associated dead material while removing biomass in SimpleGrazing");
                    }

                    forageModel.RemoveBiomass(liveToRemove: Math.Max(0.0, MathUtilities.Divide(amountToRemove * live.Fraction, live.Material.Total.Wt, 0.0)),
                                              deadToRemove: Math.Max(0.0, MathUtilities.Divide(amountToRemove * dead.Fraction, dead.Material.Total.Wt, 0.0)));

                    //forageModel.RemoveBiomass(liveToRemove: live.Fraction, deadToRemove: dead.Fraction);
                }

                if (liveMaterial.Count == 0)
                {
                    var deadMaterial = fracRemoving.Where(f => !f.Material.IsLive).ToList();
                    foreach (var dead in deadMaterial)
                    {
                        // This can happen for surface organic matter which only has dead material.
                        forageModel.RemoveBiomass(liveToRemove: 0,
                                                  deadToRemove: Math.Max(0.0, MathUtilities.Divide(amountToRemove * dead.Fraction, dead.Material.Consumable.Wt, 0.0)));
                    }
                }

                // Set outputs and check balance
                var defoliatedDM = preRemovalDMShoot - Material.Sum(m => m.Total.Wt);
                var defoliatedN = preRemovalNShoot - Material.Sum(m => m.Total.N);
                if (!MathUtilities.FloatsAreEqual(defoliatedDM, amountToRemove, 0.000001))
                {
                    throw new Exception("Removal of DM resulted in loss of mass balance");
                }
                else
                {
                    summary?.WriteMessage(forageModel as IModel, "Biomass removed from " + forageModel.Name + " by grazing: " + (defoliatedDM * 10).ToString("#0.0") + "kg/ha", MessageType.Information);
                }
 
                return new Forages.MaterialRemoved(defoliatedDM * 10, defoliatedN * 10, defoliatedDigestibility); // convert mass from g/m2 to kg/ha
            }
            return null;
        }

        /// <summary>Removes plant material simulating a graze event.</summary>
        /// <param name="type">The type of amount being defined (SetResidueAmount or SetRemoveAmount)</param>
        /// <param name="amount">The DM amount (g/m2)</param>
        /// <param name="summary">Optional summary object.</param>
        public void RemoveBiomass(string type, double amount, ISummary summary = null)
        {
            double harvestableWt = Material.Sum(m => m.Consumable.Wt);
            if (!MathUtilities.FloatsAreEqual(harvestableWt, 0.0))
            {
                // Get the amount required to remove
                double amountRequired;
                if (type.ToLower() == "setresidueamount")
                {
                    // Remove all DM above given residual amount
                    amountRequired = Math.Max(0.0, harvestableWt - amount);
                }
                else if (type.ToLower() == "setremoveamount")
                {
                    // Remove a given amount
                    amountRequired = Math.Max(0.0, amount);
                }
                else
                {
                    throw new Exception("Type of amount to remove on graze not recognized (use \'SetResidueAmount\' or \'SetRemoveAmount\'");
                }

                // Get the actual amount to remove
                double amountToRemove = Math.Max(0.0, Math.Min(amountRequired, harvestableWt));

                // Do the actual removal
                if (!MathUtilities.FloatsAreEqual(amountToRemove, 0, 0.0001))
                    RemoveBiomass(amountToRemove: amountToRemove);

            }
            else
                summary?.WriteMessage(forageModel as IModel, "Could not graze due to lack of DM available", MessageType.Warning);
        }

        private class FractionDigestibleBiomass
        {
            public FractionDigestibleBiomass(DamageableBiomass biomass, double frac)
            {
                Material = biomass;
                Fraction = frac;
            }
            public DamageableBiomass Material { get; set; }
            public double Fraction { get; set; }
        }
    }
}
