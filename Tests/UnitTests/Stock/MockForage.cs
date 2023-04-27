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

        public void RemoveBiomass(string organName, string biomassRemoveType, OrganBiomassRemovalType biomassToRemove)
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
