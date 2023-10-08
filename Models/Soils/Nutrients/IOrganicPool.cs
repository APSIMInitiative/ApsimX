using System.Collections.Generic;

namespace Models.Soils.Nutrients
{
    /// <summary>Interface for a nutrient pool.</summary>
    public interface IOrganicPool
    {
        /// <summary>Amount of carbon (kg/ha)</summary>
        IReadOnlyList<double> C { get; }

        /// <summary>Amount of nitrogen (kg/ha)</summary>
        IReadOnlyList<double> N { get; }

        /// <summary>Amount of phosphorus (kg/ha)</summary>
        IReadOnlyList<double> P { get; }

        /// <summary>
        /// Add an amount of c, n, p (kg/ha) into a layer.
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <param name="c">Amount of carbon (kg/ha)</param>
        /// <param name="n">Amount of nitrogen (kg/ha)</param>
        /// <param name="p">Amount of phosphorus (kg/ha)</param>
        void Add(int index, double c, double n, double p);
    }
}