using Models.PMF.Interfaces;

namespace Models.PMF.Arbitrator
{
    /// <summary>
    /// Interface for the arbitrator partitioning methods
    /// </summary>
    public interface IPartitionMethod
    {
        /// <summary>Calculates the parititions\ining between the different Organs.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="ArbitrationMethod">The bat.</param>
        void Calculate(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod ArbitrationMethod);

    }

    /// <summary>
    /// Interface for the arbitrator allocation methods
    /// </summary>
    public interface IAllocationMethod
    {
        /// <summary>Allocates the BiomassType to the different Organs.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        void Allocate(IArbitration[] Organs, BiomassArbitrationType BAT);

    }
}
