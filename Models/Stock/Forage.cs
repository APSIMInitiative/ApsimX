using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.GrazPlan
{
    /// <summary>Encapsulates a forage.</summary>
    [Serializable]
    public class Forage
    {
        private readonly IPlantDamage plant;
        private readonly List<ForageMaterial> material;

        /// <summary>Constructor.</summary>
        public Forage() { }

        /// <summary>Constructor.</summary>
        /// <param name="plant">The plant</param>
        /// <param name="forageParameters">The forages parameters</param>
        public Forage(IPlantDamage plant, ForageParameters forageParameters)
        {
            this.plant = plant;
            Zone = (plant as IModel).FindDescendant<Zone>();
            material = new List<ForageMaterial>();
            foreach (var organ in plant.Organs)
            {
                var materialParameters = forageParameters.Parameters.FirstOrDefault(p => p.Name.Equals(organ.Name, StringComparison.InvariantCultureIgnoreCase));
                if (materialParameters == null)
                    throw new Exception($"Cannot find grazing parameters for plant {plant.Name} and organ {organ.Name}");
                material.Add(new ForageMaterial(plant, organ, materialParameters));
            }
        }

        /// <summary>Name of forage.</summary>
        public string Name => plant.Name;

        /// <summary>Collection of forage material (e.g. organs).</summary>
        public IEnumerable<ForageMaterial> Material { get; set; }

        /// <summary>Zone where forage belongs.</summary>
        public Zone Zone { get; }

        /// <summary>Encapsulates forage material.</summary>
        public class ForageMaterial
        {
            private readonly ForageMaterialParameter parameters;
            private readonly IPlantDamage plant;
            private readonly IOrganDamage organ;

            /// <summary>Constructor.</summary>
            public ForageMaterial(IPlantDamage plant, IOrganDamage organ, ForageMaterialParameter parameters)
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
                plant.RemoveBiomass(organ.Name, biomassRemoveType, amount);
            }
        }
    }
}