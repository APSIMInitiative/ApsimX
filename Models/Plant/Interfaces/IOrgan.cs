// -----------------------------------------------------------------------
// <copyright file="IOrgan.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Base organ model
    /// </summary>
    public interface IOrgan
    {
        /// <summary>Does the potential nutrient.</summary>
        void DoPotentialNutrient();
    }

    /// <summary>An above ground interface</summary>
    public interface AboveGround { }

    /// <summary>A below ground interface</summary>
    public interface BelowGround { }

    /// <summary>Indicates the organ is a reproductive one.</summary>
    public interface Reproductive { }

    /// <summary>Indicates the organ transpires</summary>
    public interface Transpiring { }

}



   
