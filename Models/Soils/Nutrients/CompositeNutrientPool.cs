using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.Soils.Nutrients
{
    /// <summary>Encapsulates a collection of nutrient pools and aggregates them as a INutrientPool</summary>
    [Serializable]
    public class CompositeNutrientPool : IOrganicPool
    {
        private readonly IEnumerable<IOrganicPool> nutrientPools;
        private readonly double[] c;
        private readonly double[] n;
        private readonly double[] p;

        /// <summary>Constructor.</summary>
        /// <param name="pools">The enumeration of pools to aggregate.</param>
        public CompositeNutrientPool(IEnumerable<IOrganicPool> pools)
        {
            nutrientPools = pools;
            int numberLayers = nutrientPools.First().C.Count;
            c = new double[numberLayers];
            n = new double[numberLayers];
            p = new double[numberLayers];
        }

        /// <summary>Amount of carbon (kg/ha).</summary>
        public IReadOnlyList<double> C => c;

        /// <summary>Amount of nitrogen (kg/ha).</summary>
        public IReadOnlyList<double> N => n;

        /// <summary>Amount of nitrogen (kg/ha).</summary>
        public IReadOnlyList<double> P => p;

        /// <summary>Calculate C, N, P.</summary>
        public void Calculate()
        {
            Array.Clear(c);
            Array.Clear(n);
            Array.Clear(p);
            foreach (var pool in nutrientPools)
            {
                for (int i = 0; i < pool.C.Count; i++)
                {
                    c[i] += pool.C[i];
                    n[i] += pool.N[i];
                    p[i] += pool.P[i];
                }
            }
        }

        /// <summary>
        /// Add an amount of c, n, p (kg/ha) into a layer.
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <param name="c">Amount of carbon (kg/ha)</param>
        /// <param name="n">Amount of nitrogen (kg/ha)</param>
        /// <param name="p">Amount of phosphorus (kg/ha)</param>
        public void Add(int index, double c, double n, double p)
        {
            throw new InvalidOperationException("Cannot add to a composition nutrient pool");
        }
    }
}