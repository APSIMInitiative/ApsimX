using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.GrazPlan
{
    /// <summary>
    /// Encapsulates a collection of grazable material (organs) for a given model.
    /// Stock calls methods to get characteristics of the material and to remove biomass.
    /// </summary>
    [Serializable]
    public class Forage
    {
        private readonly IModel model;
        private readonly List<ForageMaterial> material;

        /// <summary>Constructor.</summary>
        public Forage() { }

        /// <summary>Constructor.</summary>
        /// <param name="model">The model</param>
        /// <param name="forageParameters">The forages parameters</param>
        public Forage(IModel model, ForageParameters forageParameters)
        {
            this.model = model;
            Zone = model.FindAncestor<Zone>();
            material = new List<ForageMaterial>();
            if (model is IPlantDamage plant)
            {
                foreach (var organ in plant.Organs)
                {
                    var materialParameters = forageParameters.Material.FirstOrDefault(p => p.Name.Equals(organ.Name, StringComparison.InvariantCultureIgnoreCase));
                    if (materialParameters == null)
                        throw new Exception($"Cannot find grazing parameters for plant {plant.Name} and organ {organ.Name}");
                    material.Add(new ForageMaterial(plant, organ, materialParameters));
                }
            }
            else
            {
                var materialParameters = forageParameters.Material.FirstOrDefault(p => p.Name.Equals(model.Name, StringComparison.InvariantCultureIgnoreCase));
                if (materialParameters == null)
                    throw new Exception($"Cannot find grazing parameters for model {model.Name}");
                material.Add(new ForageMaterial(null, model as IOrganDamage, materialParameters));
            }
        }

        /// <summary>Name of forage.</summary>
        public string Name => model.Name;

        /// <summary>Collection of forage material (e.g. organs).</summary>
        public IEnumerable<ForageMaterial> Material => material;

        /// <summary>Zone where forage belongs.</summary>
        public Zone Zone { get; }

        /// <summary>Total weight.</summary>
        public double TotalWt => material.Sum(m => m.Live.Wt + m.Dead.Wt);

        /// <summary>Harvestable weight.</summary>
        public double HarvestableWt
        {
            get
            {
                if (model is AgPasture.PastureSpecies agpasture)
                    return agpasture.AboveGroundHarvestable.Wt;
                else
                    return TotalWt;
            }
        }

        /// <summary>Gets the population.</summary>
        public double Population
        {
            get
            {
                if (model is IPlantDamage plant)
                    return plant.Population;
                else
                    return 0;
            }
        }

        /// <summary>Remove material.</summary>
        /// <param name="newPopulation">The new plant population.</param>
        public void ReducePopulation(double newPopulation)
        {
            if (model is IPlantDamage plant)
                plant.ReducePopulation(newPopulation);
        }

        /// <summary>Remove material.</summary>
        /// <param name="amount">The amount to remove.</param>
        public DigestibleBiomass RemoveBiomass(double amount)
        {
            // Calculate a defoliated digestibility.
            if (model is AgPasture.PastureSpecies agpasture)
                DefoliatedDigestibility = agpasture.DefoliatedDigestibility;
            else
                DefoliatedDigestibility = material.Average(m => (m.DigestibilityLive + m.DigestibilityDead) / 2);

            // Remove biomass.
            Biomass defoliatedBiomass;
            if (model is IPlantDamage plant)
                defoliatedBiomass = plant.RemoveBiomass(amount);
            else if (model is Surface.SurfaceOrganicMatter surfaceOM)
                defoliatedBiomass = surfaceOM.RemoveBiomass(amount);
            else
                throw new NotImplementedException();

            // Return defoliated biomass.
            return new DigestibleBiomass(defoliatedBiomass, DefoliatedDigestibility);
        }

        /// <summary>Gets the digestibility of the material that was removed.</summary>
        public double DefoliatedDigestibility { get; private set; }

        /// <summary>Encapsulates forage material.</summary>
        public class ForageMaterial
        {
            private readonly ForageParameters.ForageMaterialParameter parameters;
            private readonly IPlantDamage plant;
            private readonly IOrganDamage organ;

            /// <summary>Constructor.</summary>
            public ForageMaterial(IPlantDamage plant, IOrganDamage organ, ForageParameters.ForageMaterialParameter parameters)
            {
                this.plant = plant;
                this.organ = organ;
                this.parameters = parameters;
            }

            /// <summary>Digestibility of live material (0-1).</summary>
            public double DigestibilityLive => parameters.DigestibilityLive;

            /// <summary>Digestibility of dead material (0-1).</summary>
            public double DigestibilityDead => parameters.DigestibilityDead;

            /// <summary>Fraction of live material that is consumable.</summary>
            public double FractionConsumableLive => parameters.FractionConsumableLive;

            /// <summary>Fraction of dead material that is consumable.</summary>
            public double FractionConsumableDead => parameters.FractionConsumableDead;

            /// <summary>Live material.</summary>
            public Biomass Live => organ.Live * FractionConsumableLive;

            /// <summary>Dead material.</summary>
            public Biomass Dead => organ.Dead * FractionConsumableDead;


            /// <summary>Remove material.</summary>
            /// <param name="biomassRemoveType">The type of removal (e.g. 'Graze').</param>
            /// <param name="amount">The amount to remove.</param>
            public void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amount)
            {
                if (plant != null)
                    plant.RemoveBiomass(organ.Name, biomassRemoveType, amount);
                else if (organ is Surface.SurfaceOrganicMatter surfaceOM)
                    surfaceOM.RemoveBiomass(biomassRemoveType, amount);
                else
                    throw new NotImplementedException();
            }
        }

        /// <summary>A class to hold a mass of biomass and its digestibility.</summary>
        public class DigestibleBiomass
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="biomass">Biomass.</param>
            /// <param name="digestibility">Digestibility of biomass.</param>
            public DigestibleBiomass(Biomass biomass, double digestibility)
            {
                Biomass = biomass;
                Digestibility = digestibility;
            }

            /// <summary>Biomass</summary>
            public Biomass Biomass { get; }

            /// <summary>Digestibility of biomass.</summary>
            public double Digestibility { get; }
        }
    }
}