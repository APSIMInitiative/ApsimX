#nullable enable
using System.Collections.Generic;

namespace APSIM.Workflow
{
    /// <summary>
    /// SplittingRules is a class that represents the rules for splitting an APSIM file into multiple files.
    /// </summary>
    public class SplittingRules
    {
        /// <summary> Output file path </summary>
        public string? OutputPath { get; set; }

        /// <summary> List of groups to split the file into </summary>
        public List<SplittingGroup>? Groups { get; set; }
    }
}