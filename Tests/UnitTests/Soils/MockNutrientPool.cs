using Models.Core;
using Models.Soils.Nutrients;

namespace UnitTests.Soils
{

    internal class MockNutrientPool : Model, INutrientPool
    {
        public double[] C { get; set; }

        public double[] CNRatio { get; set; }

        public double[] LayerFraction { get; set; }
        public double[] N { get; set; }

        public void Add(double[] CAdded, double[] NAdded)
        {
            throw new System.NotImplementedException();
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}