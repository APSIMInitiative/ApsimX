using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;


namespace Models.PMF.Organs
{
    /// <summary>
    /// This class represents a base organ
    /// </summary>
    [Serializable]
    public class BaseOrgan : Organ
    {
        /// <summary>The clock</summary>
        [Link]
        public Clock Clock = null;

        /// <summary>The met data</summary>
        [Link]
        public Weather MetData = null;

        /// <summary>Gets or sets the dm supply.</summary>
        /// <value>The dm supply.</value>
        [XmlIgnore]
        public override BiomassSupplyType DMSupply { get { return new BiomassSupplyType(); } set { } }
        /// <summary>Sets the dm potential allocation.</summary>
        /// <value>The dm potential allocation.</value>
        [XmlIgnore]
        public override BiomassPoolType DMPotentialAllocation { set { } }
        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        [XmlIgnore]
        public override BiomassAllocationType DMAllocation { set { } }
        /// <summary>Gets or sets the dm demand.</summary>
        /// <value>The dm demand.</value>
        [XmlIgnore]
        public override BiomassPoolType DMDemand { get { return new BiomassPoolType(); } set { } }

        /// <summary>Gets or sets the n supply.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        public override BiomassSupplyType NSupply { get { return new BiomassSupplyType(); } set { } }
        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        [XmlIgnore]
        public override BiomassAllocationType NAllocation { set { } }
        /// <summary>Gets or sets the n fixation cost.</summary>
        /// <value>The n fixation cost.</value>
        [XmlIgnore]
        public override double NFixationCost { get { return 0; } set { } }
        /// <summary>Gets or sets the n demand.</summary>
        /// <value>The n demand.</value>
        [XmlIgnore]
        public override BiomassPoolType NDemand { get { return new BiomassPoolType(); } set { } }

        /// <summary>Gets or sets the water demand.</summary>
        /// <value>The water demand.</value>
        [XmlIgnore]
        public override double WaterDemand { get { return 0; } set { } }
        /// <summary>Gets or sets the water supply.</summary>
        /// <value>The water supply.</value>
        [XmlIgnore]
        public override double WaterSupply { get { return 0; } set { } }
        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        /// <exception cref="System.Exception">Cannot set water uptake for  + Name</exception>
        [XmlIgnore]
        public override double WaterUptake
        {
            get { return 0; }
            set { throw new Exception("Cannot set water uptake for " + Name); }
        }
        /// <summary>Gets or sets the water allocation.</summary>
        /// <value>The water allocation.</value>
        /// <exception cref="System.Exception">Cannot set water allocation for  + Name</exception>
        [XmlIgnore]
        public override double WaterAllocation
        {
            get { return 0; }
            set { throw new Exception("Cannot set water allocation for " + Name); }
        }
        /// <summary>Does the water uptake.</summary>
        /// <param name="Demand">The demand.</param>
        public override void DoWaterUptake(double Demand) { }
        /// <summary>Gets or sets the FRGR.</summary>
        /// <value>The FRGR.</value>
        [XmlIgnore]
        public override double FRGR { get { return 10000; } set { } } //Defalt is a rediculious value so Organs that don't over ride this with something sensible can be screaned easily
        /// <summary>Does the potential dm.</summary>
        public override void DoPotentialDM() { }
        /// <summary>Does the potential nutrient.</summary>
        public override void DoPotentialNutrient() { }
        /// <summary>Does the actual growth.</summary>
        public override void DoActualGrowth() { }

        /// <summary>Gets or sets the maximum nconc.</summary>
        /// <value>The maximum nconc.</value>
        [XmlIgnore]
        public override double MaxNconc { get { return 0; } set { } }
        /// <summary>Gets or sets the minimum nconc.</summary>
        /// <value>The minimum nconc.</value>
        [XmlIgnore]
        public override double MinNconc { get { return 0; } set { } }



        // Provide some variables for output until we get a better REPORT component that
        // can do structures e.g. NSupply.Fixation


        /// <summary>Gets the dm supply photosynthesis.</summary>
        /// <value>The dm supply photosynthesis.</value>
        [Units("g/m^2")]
        public double DMSupplyPhotosynthesis { get { return DMSupply.Fixation; } }


        /// <summary>Gets the n supply uptake.</summary>
        /// <value>The n supply uptake.</value>
        [Units("g/m^2")]
        public double NSupplyUptake { get { return NSupply.Uptake; } }

        /// <summary>Clears this instance.</summary>
        public override void Clear()
        {
            Live.Clear();
            Dead.Clear();
        }

        // Methods that can be called from manager
        /// <summary>Called when [sow].</summary>
        /// <param name="SowData">The sow data.</param>
        public override void OnSow(SowPlant2Type SowData) { Clear(); }
        /// <summary>Called when [harvest].</summary>
        public override void OnHarvest() { }
        /// <summary>Called when [end crop].</summary>
        public override void OnEndCrop() 
        {
            Clear();
        }
        /// <summary>Called when [cut].</summary>
        public override void OnCut() { }
    }
}