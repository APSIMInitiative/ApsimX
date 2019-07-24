// -----------------------------------------------------------------------
// <copyright file="ISoilCrop.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Soils
{
    using Models.Core;

    /// <summary>
    /// A soil crop interface
    /// </summary>
    public interface ISoilCrop
    {
        /// <summary>
        /// Name of the crop
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the KL value.
        /// </summary>
        double[] KL { get; set; }

        /// <summary>
        /// Gets or sets the exploration factor
        /// </summary>
        double[] XF { get; set; }
    }
}
