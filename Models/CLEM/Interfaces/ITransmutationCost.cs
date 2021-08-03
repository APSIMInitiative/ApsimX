using Models.Core;
using System;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for transmutation costs
    /// </summary>
    public interface ITransmutationCost
    {
        /// <summary>
        ///Resource type to use
        /// </summary>
        [Description("Resource type to use")]
        Type ResourceType { get; set; }

        /// <summary>
        /// Cost per unit
        /// </summary>
        [Description("Cost per unit")]
        double CostPerUnit { get; set; }

        /// <summary>
        /// Name of resource type to use
        /// </summary>
        [Description("Name of Resource Type to use")]
        string ResourceTypeName { get; set; }
    }

}
