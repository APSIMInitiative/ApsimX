namespace UnitTests.Stock
{
    using Models.Core;
    using Models.PMF;
    using Models.PMF.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MockForage : Model, IPlantDamage
    {
        public MockForage() { }
        public MockForage(double liveWt)
        {
            Organs.Add(new MockOrgan("", liveWt));
        }
        public List<IOrganDamage> Organs { get; set; } = new List<MockOrgan>().Cast<IOrganDamage>().ToList();

        public Biomass AboveGround => throw new NotImplementedException();

        public Biomass AboveGroundHarvestable => throw new NotImplementedException();

        public double Population => throw new NotImplementedException();

        public double LAI => throw new NotImplementedException();

        public double AssimilateAvailable => throw new NotImplementedException();

        public void ReduceCanopy(double deltaLAI)
        {
            throw new NotImplementedException();
        }

        public void ReducePopulation(double newPlantPopulation)
        {
            throw new NotImplementedException();
        }

        public void ReduceRootLengthDensity(double deltaRLD)
        {
            throw new NotImplementedException();
        }

        public void RemoveAssimilate(double deltaAssimilate)
        {
            throw new NotImplementedException();
        }

        public Biomass RemoveBiomass(double amount)
        {
            throw new NotImplementedException();
        }

        public void RemoveBiomass(string organName, string biomassRemoveType, OrganBiomassRemovalType biomassToRemove)
        {
            throw new NotImplementedException();
        }

        public bool IsAlive { get; set; } = true;
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
