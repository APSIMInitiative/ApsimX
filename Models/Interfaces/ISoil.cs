// -----------------------------------------------------------------------
// <copyright file="ISolute.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Interfaces
{
    /// <summary>
    /// This interface defines a soil.
    /// </summary>
    public interface ISoil
    {
        /// <summary>Layer thickess</summary>
        double[] Thickness { get; }
    }
}
