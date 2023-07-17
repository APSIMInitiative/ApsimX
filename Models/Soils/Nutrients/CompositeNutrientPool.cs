using System.Collections.Generic;

namespace Models.Soils.Nutrients
{

    /// <summary>
    /// Encapsulates a collection of nutrient pools and aggregates them
    /// as a INutrientPool
    /// </summary>
    public class CompositeNutrientPool : INutrientPool
    {
        private IEnumerable<INutrientPool> nutrientPools;

        /// <summary>Constructor.</summary>
        /// <param name="pools">The enumeration of pools to aggregate.</param>
        public CompositeNutrientPool(IEnumerable<INutrientPool> pools)
        {
            nutrientPools = pools;
        }

        /// <summary>Amount of carbon (kg/ha).</summary>
        public double[] C
        {
            get
            {
                double[] values = null;
                foreach (var pool in nutrientPools)
                {
                    if (values == null)
                        values = new double[pool.C.Length];
                    for (int i = 0; i < values.Length; i++)
                        values[i] += pool.C[i];
                }
                return values;
            }
        }

        /// <summary>Amount of nitrogen (kg/ha).</summary>
        public double[] N
        {
            get
            {
                double[] values = null;
                foreach (var pool in nutrientPools)
                {
                    if (values == null)
                        values = new double[pool.N.Length];
                    for (int i = 0; i < values.Length; i++)
                        values[i] += pool.N[i];
                }
                return values;
            }
        }
    }
}