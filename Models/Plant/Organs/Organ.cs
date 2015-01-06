using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Organs
{
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
    /*!
    <summary>
    The base model of organ
    </summary>
    \param Live The live biomass of organ (g), which is a model inherited from Biomass model.
    \param Dead The dead biomass of organ (g), which is a model inherited from Biomass model.
    <remarks>
    The biomass pool is split into three components, i.e.
    - non-structural
    - structural
    - metabolic
    PFM considers four types of biomass supply, i.e.
    - fixation
    - reallocation
    - uptake
    - retranslocation
    PFM considers eight types of biomass allocation, i.e.
    - structural
    - non-structural
    - metabolic
    - retranslocation
    - reallocation
    - respired
    - uptake
    - fixation
    </remarks>
     */

    /// <summary>
    /// Base organ model
    /// </summary>
    [Serializable]
    abstract public class Organ : Model
    {
        #region Links to other models or compontnets
        /// <summary>The live</summary>
        [Link] public Biomass Live = null;
        /// <summary>The dead</summary>
        [Link] public Biomass Dead = null;
        #endregion

        #region Organ - Arbitrator interface methods
        //DryMatter methods
        /// <summary>Sets the dm potential allocation.</summary>
        /// <value>The dm potential allocation.</value>
        [XmlIgnore]
        abstract public BiomassPoolType DMPotentialAllocation { set; }
        /// <summary>Gets or sets the dm demand.</summary>
        /// <value>The dm demand.</value>
        [XmlIgnore]
        abstract public BiomassPoolType DMDemand { get; set; }
        /// <summary>Gets or sets the dm supply.</summary>
        /// <value>The dm supply.</value>
        [XmlIgnore]
        abstract public BiomassSupplyType DMSupply { get; set; }
        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        [XmlIgnore]
        abstract public BiomassAllocationType DMAllocation { set; }
        //Nitrogen methods
        /// <summary>Gets or sets the n demand.</summary>
        /// <value>The n demand.</value>
        [XmlIgnore]
        abstract public BiomassPoolType NDemand { get; set; }
        /// <summary>Gets or sets the n supply.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        abstract public BiomassSupplyType NSupply { get; set; }
        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        [XmlIgnore]
        abstract public BiomassAllocationType NAllocation { set; }
        //Water methods
        /// <summary>Gets or sets the water demand.</summary>
        /// <value>The water demand.</value>
        [XmlIgnore]
        abstract public double WaterDemand { get; set; }
        /// <summary>Gets or sets the water supply.</summary>
        /// <value>The water supply.</value>
        [XmlIgnore]
        abstract public double WaterSupply { get; set; }
        /// <summary>Gets or sets the water allocation.</summary>
        /// <value>The water allocation.</value>
        [XmlIgnore]
        abstract public double WaterAllocation { get; set; }
        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        [XmlIgnore]
        abstract public double WaterUptake { get; set; }
        /// <summary>Gets or sets the FRGR.</summary>
        /// <value>The FRGR.</value>
        [XmlIgnore]
        abstract public double FRGR {get; set;}
        //Communicated organ variables
        /// <summary>Gets or sets the maximum nconc.</summary>
        /// <value>The maximum nconc.</value>
        [XmlIgnore]
        abstract public double MaxNconc { get; set; }
        /// <summary>Gets or sets the minimum nconc.</summary>
        /// <value>The minimum nconc.</value>
        [XmlIgnore]
        abstract public double MinNconc { get; set; }
        /// <summary>Gets or sets the n fixation cost.</summary>
        /// <value>The n fixation cost.</value>
        [XmlIgnore]
        abstract public double NFixationCost { get; set; }
        #endregion

        #region Top Level Time-step  and event Functions
        //Plant actions
        /// <summary>Clears this instance.</summary>
        abstract public void Clear();
        /// <summary>Does the water uptake.</summary>
        /// <param name="Demand">The demand.</param>
        virtual public void DoWaterUptake(double Demand) { }
        /// <summary>Does the potential dm.</summary>
        virtual public void DoPotentialDM() { }
        /// <summary>Does the potential nutrient.</summary>
        virtual public void DoPotentialNutrient() { }
        /// <summary>Does the actual growth.</summary>
        virtual public void DoActualGrowth() { }
        // Methods that can be called .e.g from manager
        /// <summary>Called when [sow].</summary>
        /// <param name="Sow">The sow.</param>
        abstract public void OnSow(SowPlant2Type Sow);
        /// <summary>Called when [harvest].</summary>
        abstract public void OnHarvest();
        /// <summary>Called when [end crop].</summary>
        abstract public void OnEndCrop();
        /// <summary>Called when [cut].</summary>
        abstract public void OnCut();
        #endregion
    }
}



   
