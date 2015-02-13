// -----------------------------------------------------------------------
// <copyright file="ICrop.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Core
{
    /// <summary>
    /// The ICrop interface specifies the properties and methods that all
    /// crops must have. In effect this interface describes the interactions
    /// between a crop and the other models in APSIM.
    /// </summary>
    public interface ICrop
    {
        /// <summary>
        /// Is the plant alive?
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Gets a list of cultivar names
        /// </summary>
        string[] CultivarNames { get; }

        /// <summary>Sows the plant</summary>
        /// <param name="cultivar">The cultivar.</param>
        /// <param name="population">The population.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="rowSpacing">The row spacing.</param>
        /// <param name="maxCover">The maximum cover.</param>
        /// <param name="budNumber">The bud number.</param>
        void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1);
    }
}