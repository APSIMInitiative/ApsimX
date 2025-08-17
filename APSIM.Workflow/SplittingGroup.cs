#nullable enable
using System.Collections.Generic;

namespace APSIM.Workflow
{
    /// <summary>
    /// SplittingGroup is a class that represents a group of experiments and simulations to be split into separate files.
    /// </summary>
    public class SplittingGroup
    {
        /// <summary>Name of the group.</summary>
        public string? Name { get; set; }

        /// <summary>List of experiments to be included in this group.</summary>
        public List<string>? Experiments { get; set; }

        /// <summary>List of simulations to be included in this group.</summary>
        public List<string>? Simulations { get; set; }
    }
}