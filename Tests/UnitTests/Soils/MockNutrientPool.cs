namespace UnitTests.Soils
{
    using Models.Core;
    using Models.Soils.Nutrients;

    internal class MockNutrientPool : Model, INutrientPool
    {
        public double[] C { get; set; }

        public double[] CNRatio { get; set; }

        public double[] LayerFraction { get; set; }
        public double[] N { get; set; }

        double[] INutrientPool.C => throw new System.NotImplementedException();

        double[] INutrientPool.N => throw new System.NotImplementedException();

        double[] INutrientPool.P => throw new System.NotImplementedException();

        double[] INutrientPool.LayerFraction => throw new System.NotImplementedException();

        public void Add(double[] CAdded, double[] NAdded)
        {
            throw new System.NotImplementedException();
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }

        void INutrientPool.Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}