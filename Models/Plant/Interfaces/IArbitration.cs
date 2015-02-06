// -----------------------------------------------------------------------
// <copyright file="IArbitration.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Interfaces
{
    using System;

    /// <summary>
    /// An interface that defines what needs to be implemented by an organ
    /// that communicates to an OrganArbitrator.
    /// </summary>
    public interface IArbitration
    {
        /// <summary>Sets the dm potential allocation.</summary>
        BiomassPoolType DMPotentialAllocation { set; }

        /// <summary>Gets or sets the dm demand.</summary>
        BiomassPoolType DMDemand { get; set; }

        /// <summary>Gets or sets the dm supply.</summary>
        BiomassSupplyType DMSupply { get; set; }

        /// <summary>Sets the dm allocation.</summary>
        BiomassAllocationType DMAllocation { set; }

        /// <summary>Gets or sets the n demand.</summary>
        BiomassPoolType NDemand { get; set; }

        /// <summary>Gets or sets the n supply.</summary>
        BiomassSupplyType NSupply { get; set; }

        /// <summary>Sets the n allocation.</summary>
        BiomassAllocationType NAllocation { set; }

        /// <summary>Gets or sets the maximum nconc.</summary>
        double MaxNconc { get; set; }

        /// <summary>Gets or sets the minimum nconc.</summary>
        double MinNconc { get; set; }

        /// <summary>Gets or sets the n fixation cost.</summary>
        double NFixationCost { get; set; }

        /// <summary>Gets the total (live + dead) dm (g/m2)</summary>
        double TotalDM { get; }

        /// <summary>Gets the total (live + dead) n (g/m2).</summary>
        double TotalN { get; }
    }


    #region Arbitrator method types
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
        public double NonStructural { get; set; }
        /// <summary>Gets or sets the metabolic.</summary>
        /// <value>The metabolic.</value>
        public double Metabolic { get; set; }
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
        public double NonStructural { get; set; }
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
