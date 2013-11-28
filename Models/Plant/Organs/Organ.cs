using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Organs
{
    #region Class descriptor properties
    public interface AboveGround
    {
    }
    public interface BelowGround
    {
    }
    public interface Reproductive
    {
    }
    #endregion

    #region Arbitrator method types
    public class BiomassPoolType
    {
        public double Structural { get; set; }
        public double NonStructural { get; set; }
        public double Metabolic { get; set; }
    }
    public class BiomassSupplyType
    {
        public double Fixation { get; set; }
        public double Reallocation { get; set; }
        public double Uptake { get; set; }
        public double Retranslocation { get; set; }
    }
    public class BiomassAllocationType
    {
        public double Structural { get; set; }
        public double NonStructural { get; set; }
        public double Metabolic { get; set; }
        public double Retranslocation { get; set; }
        public double Reallocation { get; set; }
        public double Respired { get; set; }
        public double Uptake { get; set; }
        public double Fixation { get; set; }
    }
    #endregion

    abstract public class Organ: Model
    {
        #region Links to other models or compontnets
        public virtual Biomass Live { get; set; }
        public virtual Biomass Dead { get; set; }
        #endregion

        #region Organ - Arbitrator interface methods
        //DryMatter methods
        [XmlIgnore]
        abstract public BiomassPoolType DMPotentialAllocation { set; }
        [XmlIgnore]
        abstract public BiomassPoolType DMDemand { get; set; }
        [XmlIgnore]
        abstract public BiomassSupplyType DMSupply { get; set; }
        [XmlIgnore]
        abstract public BiomassAllocationType DMAllocation { set; }
        //Nitrogen methods
        [XmlIgnore]
        abstract public BiomassPoolType NDemand { get; set; }
        [XmlIgnore]
        abstract public BiomassSupplyType NSupply { get; set; }
        [XmlIgnore]
        abstract public BiomassAllocationType NAllocation { set; }
        //Water methods
        [XmlIgnore]
        abstract public double WaterDemand { get; set; }
        [XmlIgnore]
        abstract public double WaterSupply { get; set; }
        [XmlIgnore]
        abstract public double WaterAllocation { get; set; }
        [XmlIgnore]
        abstract public double WaterUptake { get; set; }
        //Communicated organ variables
        [XmlIgnore]
        abstract public double MaxNconc { get; set; }
        [XmlIgnore]
        abstract public double MinNconc { get; set; }
        [XmlIgnore]
        abstract public double NFixationCost { get; set; }
        #endregion

        #region Top Level Time-step  and event Functions
        //Plant actions
        virtual public void DoWaterUptake(double Demand) { }
        virtual public void DoPotentialDM() { }
        virtual public void DoPotentialNutrient() { }
        virtual public void DoActualGrowth() { }
        // Methods that can be called .e.g from manager
        abstract public void OnSow(SowPlant2Type Sow);
        abstract public void OnHarvest();
        abstract public void OnEndCrop();
        abstract public void OnCut();
        #endregion
    }
}



   
