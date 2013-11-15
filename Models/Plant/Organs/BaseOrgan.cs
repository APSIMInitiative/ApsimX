using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;


namespace Models.PMF.Organs
{
    public class BaseOrgan : Organ
    {
        [Link]
        public Clock Clock = null;

        [Link]
        public WeatherFile MetData = null;

        [XmlIgnore]
        public override BiomassSupplyType DMSupply { get { return new BiomassSupplyType(); } set { } }
        [XmlIgnore]
        public override BiomassPoolType DMPotentialAllocation { set { } }
        [XmlIgnore]
        public override BiomassAllocationType DMAllocation { set { } }
        [XmlIgnore]
        public override BiomassPoolType DMDemand { get { return new BiomassPoolType(); } set { } }

        [XmlIgnore]
        public override BiomassSupplyType NSupply { get { return new BiomassSupplyType(); } set { } }
        [XmlIgnore]
        public override BiomassAllocationType NAllocation { set { } }
        [XmlIgnore]
        public override double NFixationCost { get { return 0; } set { } }
        [XmlIgnore]
        public override BiomassPoolType NDemand { get { return new BiomassPoolType(); } set { } }

        [XmlIgnore]
        public override double WaterDemand { get { return 0; } set { } }
        [XmlIgnore]
        public override double WaterSupply { get { return 0; } set { } }
        [XmlIgnore]
        public override double WaterUptake
        {
            get { return 0; }
            set { throw new Exception("Cannot set water uptake for " + Name); }
        }
        [XmlIgnore]
        public override double WaterAllocation
        {
            get { return 0; }
            set { throw new Exception("Cannot set water allocation for " + Name); }
        }
        public override void DoWaterUptake(double Demand) { }
        public override void DoPotentialDM() { }
        public override void DoPotentialNutrient() { }
        public override void DoActualGrowth() { }

        [XmlIgnore]
        public override double MaxNconc { get { return 0; } set { } }
        [XmlIgnore]
        public override double MinNconc { get { return 0; } set { } }



        // Provide some variables for output until we get a better REPORT component that
        // can do structures e.g. NSupply.Fixation

        
        [Units("g/m^2")]
        public double DMSupplyPhotosynthesis { get { return DMSupply.Fixation; } }

        
        [Units("g/m^2")]
        public double NSupplyUptake { get { return NSupply.Uptake; } }

        // Methods that can be called from manager
        public override void OnSow(SowPlant2Type SowData) { }
        public override void OnHarvest() { }
        public override void OnEndCrop() { }
    }
}