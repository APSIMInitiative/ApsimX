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
        /// <summary>Gets or sets the FRGR.</summary>
        double FRGR { get; set; }

        /// <summary>Does the potential dm.</summary>
        void DoPotentialDM();
        
        /// <summary>Does the potential nutrient.</summary>
        void DoPotentialNutrient();
        
        /// <summary>Does the actual growth.</summary>
        void DoActualGrowth();

        /// <summary>Called when a simulation commences</summary>
        void OnSimulationCommencing();

        /// <summary>Called when crop is sown</summary>
        /// <param name="sowing">Sowing data</param>
        void OnSow(SowPlant2Type sowing);

        /// <summary>Called when the crop is harvested</summary>
        void OnHarvest();

        /// <summary>Called when the crop is cut</summary>
        void OnCut();

        /// <summary>Called when crop ends</summary>
        void OnEndCrop();
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



   
