using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.ForageDigestibility
{
    /// <summary>Encapsulates a model that has zero or more digestible biomass instances.</summary>
    public class ModelWithDigestibleBiomass
    {
        private readonly IHasDamageableBiomass forageModel;
        private readonly IEnumerable<ForageMaterialParameters> parameters;

        /// <summary>Constructor.</summary>
        /// <param name="forageModel">A model that has damageable biomass.</param>
        /// <param name="parameters">Parameters for this forage.</param>
        public ModelWithDigestibleBiomass(IHasDamageableBiomass forageModel, IEnumerable<ForageMaterialParameters> parameters)
        {
            this.forageModel = forageModel;
            this.parameters = parameters;
        }

        /// <summary>Name of forage model.</summary>
        public string Name => forageModel.Name;

        /// <summary>Zone that forage is located in.</summary>
        public Zone Zone => (forageModel as IModel).FindAncestor<Zone>();

        /// <summary>A collection of digestible material that can be grazed.</summary>
        public IEnumerable<DigestibleBiomass> Material
        {
            get
            {
                foreach (var material in forageModel.Material)
                {
                    var fullName = $"{Name}.{material.Name}";
                    var materialParameters = parameters.FirstOrDefault(p => p.Name.Equals(fullName, StringComparison.InvariantCultureIgnoreCase) 
                                                                            && p.IsLive == material.IsLive);
                    if (materialParameters == null)
                        throw new Exception($"Cannot find forage parameters for {fullName}");
                    if (materialParameters.FractionConsumable > 0)
                    {
                        var consumableBiomass = material.Biomass * 10;  // g/m2 to kg/ha
                        consumableBiomass.StructuralWt = Math.Max(0.0, consumableBiomass.StructuralWt * materialParameters.FractionConsumable - materialParameters.MinimumAmount);
                        
                        yield return new DigestibleBiomass(new DamageableBiomass(material.Name, consumableBiomass, material.IsLive),
                                                           materialParameters);
                    }
                }
            }
        }

        /// <summary>
        /// Remove biomass from an organ.
        /// </summary>
        /// <param name="materialName">Name of organ.</param>
        /// <param name="biomassToRemove">Biomass to remove.</param>
        public void RemoveBiomass(string materialName, OrganBiomassRemovalType biomassToRemove)
        {
            forageModel.RemoveBiomass(materialName, "Graze", biomassToRemove);
        }

        /// <summary>Removes a given amount of biomass (and N) from the plant.</summary>
        /// <param name="amountToRemove">The amount of biomass to remove (kg/ha)</param>
        /// <param name="PreferenceForGreenOverDead">Relative preference for live over dead material during graze (>0.0).</param>
        /// <param name="PreferenceForLeafOverStems">Relative preference for leaf over stem-stolon material during graze (>0.0).</param>
        /// <param name="summary">Optional summary object.</param>
        public DigestibleBiomass RemoveBiomass(double amountToRemove,
                                               double PreferenceForGreenOverDead = 1.0,
                                               double PreferenceForLeafOverStems = 1.0,
                                               ISummary summary = null)
        {
            if (!MathUtilities.FloatsAreEqual(amountToRemove, 0.0))
            {
                var allMaterial = Material.ToList();

                // get existing DM and N amounts
                double harvestableWt = allMaterial.Sum(m => m.Biomass.Wt);
                double preRemovalDMShoot = harvestableWt;
                double preRemovalNShoot = allMaterial.Sum(m => m.Biomass.N);

                // Compute the fraction of each tissue to be removed
                var fracRemoving = new List<FractionDigestibleBiomass>();
                if (!MathUtilities.FloatsAreEqual(amountToRemove - harvestableWt, 0.0))
                {
                    // All existing DM is removed
                    amountToRemove = harvestableWt;
                    foreach (var material in allMaterial)
                    {
                        double frac = MathUtilities.Divide(material.Biomass.Wt, harvestableWt, 0.0);
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
                                frac = material.Biomass.Wt * PreferenceForGreenOverDead * PreferenceForLeafOverStems;
                            else
                                frac = material.Biomass.Wt * PreferenceForGreenOverDead;
                        }
                        else
                        {
                            if (material.Name == "Leaf")
                                frac = material.Biomass.Wt * PreferenceForLeafOverStems;
                            else
                                frac = material.Biomass.Wt;
                        }
                        fracRemoving.Add(new FractionDigestibleBiomass(material, frac));
                    }

                    // Normalise the fractions of each tissue to be removed, they should add to one
                    double totalFrac = fracRemoving.Sum(m => m.Fraction);
                    foreach (var f in fracRemoving)
                    {
                        double fracRemovable = f.Material.Biomass.Wt / amountToRemove;
                        f.Fraction = Math.Min(fracRemovable, f.Fraction / totalFrac);
                    }

                    // Iterate until sum of fractions to remove is equal to one
                    //  The initial normalised fractions are based on preference and existing DM. Because the value of fracRemoving is limited
                    //   to fracRemovable, the sum of fracRemoving may not be equal to one, as it should be. We need to iterate adjusting the
                    //   values of fracRemoving until we get a sum close enough to one. The previous values are used as weighting factors for
                    //   computing new ones at each iteration.
                    int count = 1;
                    totalFrac = totalFrac = fracRemoving.Sum(m => m.Fraction);
                    while (!MathUtilities.FloatsAreEqual(1.0 - totalFrac, 0.0))
                    {
                        count += 1;
                        foreach (var f in fracRemoving)
                        {
                            double fracRemovable = f.Material.Biomass.Wt / amountToRemove;
                            f.Fraction = Math.Min(fracRemovable, f.Fraction / totalFrac);
                        }
                        totalFrac = totalFrac = fracRemoving.Sum(m => m.Fraction);
                        if (count > 1000)
                        {
                            summary?.WriteWarning(forageModel as IModel, "SimpleGrazing could not remove or graze all the DM required for " + forageModel.Name);
                            break;
                        }
                    }
                }

                // Get digestibility of DM being harvested (do this before updating pools)
                double defoliatedDigestibility = fracRemoving.Sum(m => m.Material.Digestibility * m.Fraction);

                // Iterate through all live material, find the associated dead material and then
                // tell the forage model to remove it.
                foreach (var live in fracRemoving.Where(f => f.Material.IsLive))
                {
                    var dead = fracRemoving.Find(frac => frac.Material.Name == live.Material.Name &&
                                                                 !frac.Material.IsLive);
                    if (dead == null)
                        throw new Exception("Cannot find associated dead material while removing biomass in SimpleGrazing");

                    RemoveBiomass(live.Material.Name, new OrganBiomassRemovalType()
                    {
                        FractionLiveToRemove = Math.Max(0.0, MathUtilities.Divide(amountToRemove * live.Fraction, live.Material.Biomass.Wt, 0.0)),
                        FractionDeadToRemove = Math.Max(0.0, MathUtilities.Divide(amountToRemove * dead.Fraction, dead.Material.Biomass.Wt, 0.0))
                    });
                }

                // Set outputs and check balance
                var defoliatedDM = preRemovalDMShoot - allMaterial.Sum(m => m.Biomass.Wt);
                var defoliatedN = preRemovalNShoot - allMaterial.Sum(m => m.Biomass.N);
                if (!MathUtilities.FloatsAreEqual(defoliatedDM, amountToRemove))
                    throw new Exception("Removal of DM resulted in loss of mass balance");
                else
                    summary?.WriteMessage(forageModel as IModel, "Biomass removed from " + forageModel.Name + " by grazing: " + defoliatedDM.ToString("#0.0") + "kg/ha");

                return new DigestibleBiomass(new DamageableBiomass(Name, new Biomass()
                {
                    StructuralWt = defoliatedDM,
                    StructuralN = defoliatedN,
                }, isLive: true), defoliatedDigestibility);
            }
            return null;
        }

        /// <summary>Removes plant material simulating a graze event.</summary>
        /// <param name="type">The type of amount being defined (SetResidueAmount or SetRemoveAmount)</param>
        /// <param name="amount">The DM amount (kg/ha)</param>
        /// <param name="summary">Optional summary object.</param>
        public void RemoveBiomass(string type, double amount, ISummary summary = null)
        {
            double harvestableWt = Material.Sum(m => m.Biomass.Wt);
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
                    RemoveBiomass(amountToRemove);

            }
            else
                summary.WriteWarning(forageModel as IModel, "Could not graze due to lack of DM available");
        }

        private class FractionDigestibleBiomass
        {
            public FractionDigestibleBiomass(DigestibleBiomass biomass, double frac)
            {
                Material = biomass;
                Fraction = frac;
            }
            public DigestibleBiomass Material;
            public double Fraction;
        }
    }
}
