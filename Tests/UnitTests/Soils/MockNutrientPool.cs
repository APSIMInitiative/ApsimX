using Models.Core;
using Models.Soils.Nutrients;
using System.Collections.Generic;

namespace UnitTests.Soils
{
    internal class MockNutrientPool : Model, INutrientPool
    {
        public double[] C { get; set; }

        public double[] CNRatio { get; set; }

        public double[] LayerFraction { get; set; }
        public double[] N { get; set; }

        public double[] P { get; set; }

        IReadOnlyList<double> INutrientPool.C => C;

        IReadOnlyList<double> INutrientPool.N => N;

        IReadOnlyList<double> INutrientPool.P => P;

        public void Add(double[] CAdded, double[] NAdded)
        {
            throw new System.NotImplementedException();
        }

        public void Add(int index, double c, double n, double p)
        {
            throw new System.NotImplementedException();
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}