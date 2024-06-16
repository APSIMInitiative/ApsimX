using System;
using Models.Core;

namespace Models.PMF.Interfaces
{

    /// <summary> Inerface for arbitrators </summary>
    public interface IArbitrator
    {
        /// <summary>The DM data class  </summary>
        BiomassArbitrationType DM { get; }

        /// <summary>The N data class  </summary>
        BiomassArbitrationType N { get; }

        /// <summary>The total biomass available from photosynthesis  </summary>
        double TotalDMFixationSupply { get; }
    }

    /// <summary>
    /// Interface for Biomass supply from photosynthesis
    /// </summary>
    public interface ITotalCFixationSupply
    {
        /// <summary> The amount of DM fixed by photosynthesis</summary>
        double TotalCFixationSupply { get; }
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
    }


    #region Arbitrator data types
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class BiomassPoolType : Model
    {
        /// <summary>Gets or sets the structural.</summary>
        /// <value>The structural.</value>
        [Description("Initial Structural biomass")]
        public double Structural { get; set; }
        /// <summary>Gets or sets the storage.</summary>
        /// <value>The non structural.</value>
        [Description("Initial Storage biomass")]
        public double Storage { get; set; }
        /// <summary>Gets or sets the metabolic.</summary>
        /// <value>The metabolic.</value>
        [Description("Initial Metabolic biomass")]
        public double Metabolic { get; set; }
        /// <summary>Gets or sets the structural Priority.</summary>
        /// <value>The structural Priority.</value>
        [Description("Initial Structural biomass Priority")]
        public double QStructuralPriority { get; set; }
        /// <summary>Gets or sets Storage Priority.</summary>
        /// <value>The Storage Priority.</value>
        [Description("Initial Storage biomass priority")]
        public double QStoragePriority { get; set; }
        /// <summary>Gets or sets the metabolic biomass priority.</summary>
        /// <value>The metabolic.</value>
        [Description("Initial Metabolic biomass priority")]
        public double QMetabolicPriority { get; set; }

        /// <summary>Gets the total amount of biomass.</summary>
        public double Total
        { get { return Structural + Metabolic + Storage; } }

        internal void Clear()
        {
            Structural = 0;
            Storage = 0;
            Metabolic = 0;
            QStructuralPriority = 1;
            QStoragePriority = 1;
            QMetabolicPriority = 1;
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
        public double ReAllocation { get; set; }
        /// <summary>Gets or sets the uptake.</summary>
        /// <value>The uptake.</value>
        public double Uptake { get; set; }
        /// <summary>Gets or sets the retranslocation.</summary>
        /// <value>The retranslocation.</value>
        public double ReTranslocation { get; set; }

        /// <summary>Gets the total supply.</summary>
        public double Total
        { get { return Fixation + ReAllocation + ReTranslocation + Uptake; } }

        internal void Clear()
        {
            Fixation = 0;
            ReAllocation = 0;
            Uptake = 0;
            ReTranslocation = 0;
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
