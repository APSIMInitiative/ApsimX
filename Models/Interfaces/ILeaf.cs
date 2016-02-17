// -----------------------------------------------------------------------
// <copyright file="ICanopy.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Interfaces
{
    using System;

    /// <summary>This interface describes interface for leaf interaction with Structure.</summary>
    public interface ILeaf
    {
        /// <summary>
        /// 
        /// </summary>
        bool CohortsInitialised { get; }
        /// <summary>
        /// 
        /// </summary>
        double PlantAppearedLeafNo { get; }
    }
}
