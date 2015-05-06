using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using Models.Interfaces;


namespace Models.PMF.Organs
{
    /// <summary>
    /// This class represents a base organ
    /// </summary>
    [Serializable]
    public class BaseOrgan : Model, IOrgan, IArbitration
    {
        #region Links to other models or compontnets
        /// <summary>The live</summary>
        [Link] public Biomass Live = null;
        /// <summary>The dead</summary>
        [Link] public Biomass Dead = null;
        #endregion

        /// <summary>The clock</summary>
        [Link]
        public Clock Clock = null;

        /// <summary>The met data</summary>
        [Link]
        public IWeather MetData = null;

        /// <summary>Gets or sets the dm supply.</summary>
        /// <value>The dm supply.</value>
        [XmlIgnore]
        virtual public BiomassSupplyType DMSupply { get { return new BiomassSupplyType(); } set { } }
        /// <summary>Sets the dm potential allocation.</summary>
        /// <value>The dm potential allocation.</value>
        [XmlIgnore]
        virtual public BiomassPoolType DMPotentialAllocation { set { } }
        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        [XmlIgnore]
        virtual public BiomassAllocationType DMAllocation { set { } }
        /// <summary>Gets or sets the dm demand.</summary>
        /// <value>The dm demand.</value>
        [XmlIgnore]
        virtual public BiomassPoolType DMDemand { get { return new BiomassPoolType(); } set { } }

        /// <summary>Gets or sets the n supply.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        virtual public BiomassSupplyType NSupply { get { return new BiomassSupplyType(); } set { } }
        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        [XmlIgnore]
        virtual public BiomassAllocationType NAllocation { set { } }
        /// <summary>Gets or sets the n fixation cost.</summary>
        /// <value>The n fixation cost.</value>
        [XmlIgnore]
        virtual public double NFixationCost { get { return 0; } set { } }
        /// <summary>Gets or sets the n demand.</summary>
        /// <value>The n demand.</value>
        [XmlIgnore]
        virtual public BiomassPoolType NDemand { get { return new BiomassPoolType(); } set { } }


        /// <summary>Gets the NO3 supply for the given N state.</summary>
        virtual public double[] NO3NSupply(List<ZoneWaterAndN> zones) { return null; }

        /// <summary>Gets the NH4 supply for the given N state.</summary>
        virtual public double[] NH4NSupply(List<ZoneWaterAndN> zones) { return null; }

        /// <summary>Gets or sets the water demand.</summary>
        /// <value>The water demand.</value>
        [XmlIgnore]
        virtual public double WaterDemand { get { return 0; } set { } }

        /// <summary>Gets the water supply for the given water state.</summary>
        virtual public double[] WaterSupply(List<ZoneWaterAndN> zones) { return null; }

        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        /// <exception cref="System.Exception">Cannot set water uptake for  + Name</exception>
        [XmlIgnore]
        virtual public double WaterUptake
        {
            get { return 0; }
            set { throw new Exception("Cannot set water uptake for " + Name); }
        }

        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        /// <exception cref="System.Exception">Cannot set water uptake for  + Name</exception>
        [XmlIgnore]
        virtual public double NUptake
        {
            get { return 0; }
            set { throw new Exception("Cannot set water uptake for " + Name); }
        }
        
        /// <summary>Gets or sets the water allocation.</summary>
        /// <value>The water allocation.</value>
        /// <exception cref="System.Exception">Cannot set water allocation for  + Name</exception>
        [XmlIgnore]
        virtual public double WaterAllocation
        {
            get { return 0; }
            set { throw new Exception("Cannot set water allocation for " + Name); }
        }
        /// <summary>Does the water uptake.</summary>
        /// <param name="uptake">The uptake.</param>
        virtual public void DoWaterUptake(double[] uptake) { }
        
        /// <summary>Does the N uptake.</summary>
        /// <param name="NO3NUptake">The NO3NUptake.</param>
        /// <param name="NH4Uptake">The NH4Uptake.</param>
        virtual public void DoNitrogenUptake(double[] NO3NUptake, double[] NH4Uptake) { }

        /// <summary>Does the potential nutrient.</summary>
        virtual public void DoPotentialNutrient() { }
        
        /// <summary>Gets or sets the maximum nconc.</summary>
        /// <value>The maximum nconc.</value>
        [XmlIgnore]
        virtual public double MaxNconc { get { return 0; } set { } }
        
        /// <summary>Gets or sets the minimum nconc.</summary>
        /// <value>The minimum nconc.</value>
        [XmlIgnore]
        virtual public double MinNconc { get { return 0; } set { } }



        // Provide some variables for output until we get a better REPORT component that
        // can do structures e.g. NSupply.Fixation


        /// <summary>Gets the dm supply photosynthesis.</summary>
        /// <value>The dm supply photosynthesis.</value>
        [Units("g/m^2")]
        virtual public double DMSupplyPhotosynthesis { get { return DMSupply.Fixation; } }


        /// <summary>Gets the n supply uptake.</summary>
        /// <value>The n supply uptake.</value>
        [Units("g/m^2")]
        virtual public double NSupplyUptake { get { return NSupply.Uptake; } }


        /// <summary>Gets the total (live + dead) dm (g/m2)</summary>
        public double TotalDM { get { return Live.Wt + Dead.Wt; } }

        /// <summary>Gets the total (live + dead) n (g/m2)</summary>
        public double TotalN { get { return Live.N + Dead.N; } }

        /// <summary>Clears this instance.</summary>
        virtual protected void Clear()
        {
            Live.Clear();
            Dead.Clear();
        }
    }
}