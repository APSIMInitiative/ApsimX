namespace Models.PMF.Interfaces
{
    using Models.Core;
    using System;

    /// <summary> Inerface for arbitrators </summary>
    public interface IArbitrator
    {
        /// <summary>The DM data class  </summary>
        BiomassArbitrationType DM { get; }

        /// <summary>The N data class  </summary>
        BiomassArbitrationType N { get; }
    }

    /// <summary>
    /// An interface that defines what needs to be implemented by an organ
    /// that communicates to the OrganArbitrator.
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
    public interface IArbitration
    {
        /// <summary>Returns the organs dry matter demand</summary>
        BiomassPoolType DMDemand { get; }

        /// <summary>Returns the organs dry matter demand</summary>
        BiomassPoolType DMDemandPriorityFactor { get; }

        /// <summary>Returns the organs dry matter supply</summary>
        BiomassSupplyType DMSupply { get; }

        /// <summary>Returns the organs N demand</summary>
        BiomassPoolType NDemand { get; }

        /// <summary>Returns the organs N supply</summary>
        BiomassSupplyType NSupply { get; }

        /// <summary>Returns the DM that can be paritioned to the organ of N is not limited </summary>
        BiomassPoolType potentialDMAllocation { get; }

        /// <summary>Sets the dry matter potential allocation.</summary>
        void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter);

        /// <summary>Sets the dry matter allocation.</summary>
        void SetDryMatterAllocation(BiomassAllocationType dryMatter);

        /// <summary>Sets the n allocation.</summary>
        void SetNitrogenAllocation(BiomassAllocationType nitrogen);

        /// <summary>Gets or sets the minimum nconc.</summary>
        double MinNconc { get; }

        /// <summary>Gets or sets the n fixation cost.</summary>
        double NFixationCost { get; }

        /// <summary>Gets the total biomass</summary>
        Biomass Total { get; }

        /// <summary>Gets the live biomass</summary>
        Biomass Live { get; }

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        double MaintenanceRespiration { get; }

        /// <summary>Remove maintenance respiration from live component of organs.</summary>
        void RemoveMaintenanceRespiration(double respiration);

    }


    #region Arbitrator data types
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class BiomassPoolType : Model
    {
        /// <summary>Gets or sets the structural.</summary>
        /// <value>The structural.</value>
        [Description("Initial Structural biomass")]
        public double Structural { get; set; }
        /// <summary>Gets or sets the non structural.</summary>
        /// <value>The non structural.</value>
        [Description("Initial Storage biomass")]
        public double Storage { get; set; }
        /// <summary>Gets or sets the metabolic.</summary>
        /// <value>The metabolic.</value>
        [Description("Initial Metabolic biomass")]
        public double Metabolic { get; set; }

        /// <summary>Gets the total amount.</summary>
        public double Total
        { get { return Structural + Metabolic + Storage; } }

        internal void Clear()
        {
            Structural = 0;
            Storage = 0; 
            Metabolic = 0;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class BiomassSupplyType
    {
        /// <summary>Gets or sets the fixation.</summary>
        /// <value>The fixation.</value>
        public double Fixation { get; set; }
        /// <summary>Gets or sets the reallocation.</summary>
        /// <value>The reallocation.</value>
        public double Reallocation { get; set; }
        /// <summary>Gets or sets the uptake.</summary>
        /// <value>The uptake.</value>
        public double Uptake { get; set; }
        /// <summary>Gets or sets the retranslocation.</summary>
        /// <value>The retranslocation.</value>
        public double Retranslocation { get; set; }

        /// <summary>Gets the total supply.</summary>
        public double Total
        { get { return Fixation + Reallocation + Retranslocation + Uptake; } }

        internal void Clear()
        {
            Fixation = 0;
            Reallocation = 0;
            Uptake = 0;
            Retranslocation = 0;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class BiomassAllocationType
    {
        /// <summary>Gets or sets the structural.</summary>
        /// <value>The structural.</value>
        public double Structural { get; set; }
        /// <summary>Gets or sets the non structural.</summary>
        /// <value>The non structural.</value>
        public double Storage { get; set; }
        /// <summary>Gets or sets the metabolic.</summary>
        /// <value>The metabolic.</value>
        public double Metabolic { get; set; }
        /// <summary>Gets or sets the retranslocation.</summary>
        /// <value>The retranslocation.</value>
        public double Retranslocation { get; set; }
        /// <summary>Gets or sets the reallocation.</summary>
        /// <value>The reallocation.</value>
        public double Reallocation { get; set; }
        /// <summary>Gets or sets the respired.</summary>
        /// <value>The respired.</value>
        public double Respired { get; set; }
        /// <summary>Gets or sets the uptake.</summary>
        /// <value>The uptake.</value>
        public double Uptake { get; set; }
        /// <summary>Gets or sets the fixation.</summary>
        /// <value>The fixation.</value>
        public double Fixation { get; set; }
    }
    #endregion

}
