// -----------------------------------------------------------------------
// <copyright file="IArbitration.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Interfaces
{
    using System;
    using Models.Soils.Arbitrator;
    using System.Collections.Generic;

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
        /// <summary>
        /// Name of this Organ
        /// </summary>
        string Name { get;}

        /// <summary>Sets the dm potential allocation.</summary>
        BiomassPoolType DMPotentialAllocation { set; }

        /// <summary>Gets the dm demand.</summary>
        BiomassPoolType DMDemand { get; }

        /// <summary>Gets the dm supply.</summary>
        BiomassSupplyType DMSupply { get; }

        /// <summary>Sets the dm allocation.</summary>
        BiomassAllocationType DMAllocation { set; }

        /// <summary>Gets the n demand.</summary>
        BiomassPoolType NDemand { get; }

        /// <summary>Gets the n supply.</summary>
        BiomassSupplyType NSupply { get; }

        /// <summary>Sets the n allocation.</summary>
        BiomassAllocationType NAllocation { set; }

        /// <summary>Gets or sets the minimum nconc.</summary>
        double MinNconc { get; }

        /// <summary>Gets or sets the n fixation cost.</summary>
        double NFixationCost { get; set; }

        /// <summary>Gets the total biomass</summary>
        Biomass Total { get; }
    }


    #region Arbitrator data types
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class BiomassPoolType
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
