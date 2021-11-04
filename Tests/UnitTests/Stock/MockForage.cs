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
        public MockForage(double liveWt)
        {
            Organs.Add(new MockOrgan(liveWt));
        }
        public List<IOrganDamage> Organs { get; set; } = new List<MockOrgan>().Cast<IOrganDamage>().ToList();

        public IBiomass AboveGround => throw new NotImplementedException();

        public IBiomass AboveGroundHarvestable => throw new NotImplementedException();

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
        public MockOrgan(double liveWt)
        {
            Live = new Biomass() { StructuralWt = liveWt };
            Dead = new Biomass();
        }

        public string Name => throw new NotImplementedException();

        public Biomass Live { get; set; }

        public Biomass Dead { get; set; }

        public bool IsAboveGround { get { return true; } }
    }
}
