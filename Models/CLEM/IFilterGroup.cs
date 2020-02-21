using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM
{
    /// <summary>
    /// Interface for all filter groups
    /// </summary>
    public interface IFilterGroup
    {
        /// <summary>
        /// Holds the ML compiled ruleset for the LINQ expression tree
        /// Avoids needing to calculate this value multiple times for improved performance
        /// </summary>
        object CombinedRules { get; set; }
    }
}
