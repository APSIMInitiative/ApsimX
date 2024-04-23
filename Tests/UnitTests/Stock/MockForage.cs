using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using System;
using System.Collections.Generic;

namespace UnitTests.Stock
{
    public class MockForage : Model, IHasDamageableBiomass
    {
        
        public MockForage() { }
        public MockForage(double liveWt)
        {
            var material = new List<DamageableBiomass>();
            material.Add(new DamageableBiomass("MockForage", new Biomass() { StorageWt = liveWt }, true));
            material.Add(new DamageableBiomass("MockForage", new Biomass() { StorageWt = 0 }, false));
            Material = material;
        }

        public IEnumerable<DamageableBiomass> Material { get; set; }

        /// <summary>Remove biomass from organ.</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="deadToRemove">Fraction of dead biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="deadToResidue">Fraction of dead biomass to remove and send to residue pool(0-1).</param>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        public double RemoveBiomass(double liveToRemove = 0, double deadToRemove = 0, double liveToResidue = 0, double deadToResidue = 0)
        {
            throw new NotImplementedException();
        }

    }

    public class MockOrgan : IOrganDamage
    {
        public MockOrgan(string name, double liveWt, double deadWt = 0)
        {
            Name = name;
            Live = new Biomass() { StructuralWt = liveWt };
            Dead = new Biomass() { StructuralWt = deadWt };
        }

        public string Name { get; set; }

        public Biomass Live { get; set; }

        public Biomass Dead { get; set; }

        public bool IsAboveGround { get { return true; } }
    }
}
