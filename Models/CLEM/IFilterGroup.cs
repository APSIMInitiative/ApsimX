using Models.CLEM.Resources;
using Models.Core;
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
    public interface IFilterGroup : IModel
    {
        /// <summary>
        /// Holds the ML compiled ruleset for the LINQ expression tree
        /// Avoids needing to calculate this value multiple times for improved performance
        /// </summary>
        object CombinedRules { get; set; }

        /// <summary>
        /// Proportion of group to use
        /// </summary>
        double Proportion { get; set; }
    }
}
