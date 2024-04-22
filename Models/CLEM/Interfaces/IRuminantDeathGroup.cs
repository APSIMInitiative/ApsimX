using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Ruminant death group interface
    /// </summary>
    public interface IRuminantDeathGroup
    {
        /// <summary>
        /// A method to determine which individuals from a specified list die in the time-step
        /// </summary>
        /// <param name="individuals">Individuals to assess</param>
        /// <returns>A litf of ruminants that died</returns>
        public void DetermineDeaths(IEnumerable<Ruminant> individuals);

    }
}
