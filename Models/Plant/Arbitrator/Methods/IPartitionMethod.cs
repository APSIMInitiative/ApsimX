using Models.PMF;
using Models.PMF.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF.Arbitrator
{
    /// <summary>
    /// Interface for partition methods
    /// </summary>
    public interface IPartitionMethod
    {
        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="ArbitrationMethod">The bat.</param>
        void Calculate(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod ArbitrationMethod);

    }

    /// <summary>
    /// Interface for allocatio methods
    /// </summary>
    public interface IAllocationMethod
    {
        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        void Allocate(IArbitration[] Organs, BiomassArbitrationType BAT);

    }
}
