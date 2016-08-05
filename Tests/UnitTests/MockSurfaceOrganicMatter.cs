

namespace UnitTests
{
    using Models;
    using Models.Interfaces;
    using Models.Soils;
    using System;

    class MockSurfaceOrganicMatter : ISurfaceOrganicMatter
    {
        public double Cover { get; set; }

        public void Add(double biomass, double N, double P, string type, string name)
        {
            throw new NotImplementedException();
        }
    }
}
