using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.Plant.Organs
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
        public Biomass Live { get; set; }
        public Biomass Dead { get; set; }
        #endregion

        #region Fields
        #endregion

        #region Organ - Arbitrator interface methods
        //DryMatter methods
        abstract public BiomassPoolType DMPotentialAllocation { set; }
        abstract public BiomassPoolType DMDemand { get; set; }
        abstract public BiomassSupplyType DMSupply { get; set; }
        abstract public BiomassAllocationType DMAllocation { set; }
        //Nitrogen methods
        abstract public BiomassPoolType NDemand { get; set; }
        abstract public BiomassSupplyType NSupply { get; set; }
        abstract public BiomassAllocationType NAllocation { set; }
        //Water methods
        abstract public double WaterDemand { get; set; }
        abstract public double WaterSupply { get; set; }
        abstract public double WaterAllocation { get; set; }
        abstract public double WaterUptake { get; set; }
        //Communicated organ variables
        abstract public double MaxNconc { get; set; }
        abstract public double MinNconc { get; set; }
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
        #endregion
    }
}



   
