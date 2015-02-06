// -----------------------------------------------------------------------
// <copyright file="IOrgan.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Organs
{
    /// <summary>
    /// Base organ model
    /// </summary>
    /// <remarks>
    ///  PFM considers four types of biomass supply, i.e.
    ///  - fixation
    ///  - reallocation
    ///  - uptake
    ///  - retranslocation
    /// PFM considers eight types of biomass allocation, i.e.
    ///  - structural
    ///  - non-structural
    ///  - metabolic
    ///  - retranslocation
    ///  - reallocation
    ///  - respired
    ///  - uptake
    ///  - fixation
    /// </remarks>
    public interface Organ
    {
        #region Top Level Time-step  and event Functions

        /// <summary>Gets or sets the FRGR.</summary>
        double FRGR { get; set; }

        /// <summary>Gets or sets the water demand.</summary>
        double WaterDemand { get; set; }

        /// <summary>Gets or sets the water supply.</summary>
        double WaterSupply { get; set; }

        /// <summary>Gets or sets the water allocation.</summary>
        double WaterAllocation { get; set; }

        /// <summary>Gets or sets the water uptake.</summary>
        double WaterUptake { get; set; }

        /// <summary>Does the water uptake.</summary>
        /// <param name="Demand">The demand.</param>
        void DoWaterUptake(double Demand);

        /// <summary>Does the potential dm.</summary>
        void DoPotentialDM();
        
        /// <summary>Does the potential nutrient.</summary>
        void DoPotentialNutrient();
        
        /// <summary>Does the actual growth.</summary>
        void DoActualGrowth();

        /// <summary>Called when crop is sown</summary>
        /// <param name="sowing">Sowing data</param>
        void OnSow(SowPlant2Type sowing);

        /// <summary>Called when the crop is harvested</summary>
        void OnHarvest();

        /// <summary>Called when the crop is cut</summary>
        void OnCut();

        /// <summary>Called when crop ends</summary>
        void OnEndCrop();

        void Clear();

        #endregion
    }

    #region Class descriptor properties
    /// <summary>
    /// 
    /// </summary>
    public interface AboveGround
    {
    }
    /// <summary>
    /// 
    /// </summary>
    public interface BelowGround
    {
    }
    /// <summary>
    /// 
    /// </summary>
    public interface Reproductive
    {
    }
    /// <summary>
    /// 
    /// </summary>
    public interface Transpiring
    {
    }
    #endregion


}



   
