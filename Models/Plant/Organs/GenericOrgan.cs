using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Interfaces;
using APSIM.Shared.Utilities;
using Models.Soils.Arbitrator;
using Models.PMF.Library;

namespace Models.PMF.Organs
{

    /// <summary>
    /// This organ is simulated using a generic organ type.
    ///   
    /// **Dry Matter Demands**
    /// A given fraction of daily DM demand is determined to be structural and the remainder is non-structural.
    /// 
    /// **Dry Matter Supplies**
    /// A given fraction of Nonstructural DM is made available to the arbitrator as DMReTranslocationSupply.
    /// 
    /// **Nitrogen Demands**
    /// The daily nonstructural N demand is the product of Total DM demand and a Maximum N concentration less the structural N demand.
    /// The daily structural N demand is the product of Total DM demand and a Minimum N concentration. 
    /// The Nitrogen demand switch is a multiplier applied to nitrogen demand so it can be turned off at certain phases.
    /// 
    /// **Nitrogen Supplies**
    /// As the organ senesces a fraction of senesced N is made available to the arbitrator as NReallocationSupply.
    /// A fraction of nonstructural N is made available to the arbitrator as NRetranslocationSupply
    /// 
    /// **Biomass Senescence and Detachment**
    /// Senescence is calculated as a proportion of the live dry matter.
    /// Detachment of biomass into the surface organic matter pool is calculated daily as a proportion of the dead DM.
    /// 
    /// **Canopy**
    /// The user can model the canopy by specifying either the LAI and an extinction coefficient, or by specifying the canopy cover directly.  If the cover is specified, LAI is calculated using an inverted Beer-Lambert equation with the specified cover value.
    /// 
    /// The canopies values of Cover and LAI are passed to the MicroClimate module which uses the Penman Monteith equation to calculate potential evapotranspiration for each canopy and passes the value back to the crop.
    /// The effect of growth rate on transpiration is captured using the Fractional Growth Rate (FRGR) function which is parameterised as a function of temperature for the simple leaf. 
    ///
    /// </summary>

    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    public class GenericOrgan : Model, IOrgan, IArbitration
    {
        #region Class Parameter Function Links

        /// <summary>The live</summary>
        [Link]
        [DoNotDocument]
        public Biomass Live = null;

        /// <summary>The dead</summary>
        [Link]
        [DoNotDocument]
        public Biomass Dead = null;

        /// <summary>The plant</summary>
        [Link]
        protected Plant Plant = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        public ISurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>Link to biomass removal model</summary>
        [ChildLink]
        public BiomassRemoval biomassRemovalModel = null;

        /// <summary>The senescence rate function</summary>
        [Link]
        [Units("/d")]
        IFunction SenescenceRate = null;

        /// <summary>The detachment rate function</summary>
        [Link]
        [Units("/d")]
        IFunction DetachmentRateFunction = null;

        /// <summary>The n reallocation factor</summary>
        [Link]
        [Units("/d")]
        IFunction NReallocationFactor = null;

        /// <summary>The n retranslocation factor</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction NRetranslocationFactor = null;
        /// <summary>The nitrogen demand switch</summary>
        [Link(IsOptional = true)]
        IFunction NitrogenDemandSwitch = null;
        /// <summary>The dm retranslocation factor</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction DMRetranslocationFactor = null;
        /// <summary>The structural fraction</summary>
        [Link]
        [Units("g/g")]
        IFunction StructuralFraction = null;
        /// <summary>The dm demand Function</summary>
        [Link]
        [Units("g/m2/d")]
        IFunction DMDemandFunction = null;
        /// <summary>The initial wt function</summary>
        [Link]
        [Units("g/m2")]
        IFunction InitialWtFunction = null;
        /// <summary>The maximum n conc</summary>
        [Link]
        [Units("g/g")]
        public IFunction MaximumNConc = null;
        /// <summary>The minimum n conc</summary>
        [Units("g/g")]
        [Link]
        public IFunction MinimumNConc = null;
        /// <summary>The proportion of biomass repired each day</summary>
        [Link]
        public IFunction MaintenanceRespirationFunction = null;
        /// <summary>Dry matter conversion efficiency</summary>
        [Link]
        public IFunction DMConversionEfficiency = null;
        #endregion

        #region States

        /// <summary>The start n retranslocation supply</summary>
        private double StartNRetranslocationSupply = 0;
        /// <summary>The start n reallocation supply</summary>
        private double StartNReallocationSupply = 0;
        /// <summary>The potential dm allocation</summary>
        protected double PotentialDMAllocation = 0;
        /// <summary>The potential structural dm allocation</summary>
        protected double PotentialStructuralDMAllocation = 0;
        /// <summary>The potential metabolic dm allocation</summary>
        protected double PotentialMetabolicDMAllocation = 0;
        /// <summary>The structural dm demand</summary>
        protected double StructuralDMDemand = 0;
        /// <summary>The non structural dm demand</summary>
        protected double NonStructuralDMDemand = 0;
        /// <summary>The start live</summary>
        private Biomass StartLive = null;

        /// <summary>Clears this instance.</summary>
        protected virtual void Clear()
        {
            Live.Clear();
            Dead.Clear();
            StartNRetranslocationSupply = 0.0;
            StartNReallocationSupply = 0.0;
            PotentialDMAllocation = 0.0;
            PotentialStructuralDMAllocation = 0.0;
            PotentialMetabolicDMAllocation = 0.0;
            StructuralDMDemand = 0.0;
            NonStructuralDMDemand = 0.0;
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
        }

        /// <summary>Does the zeroing of some variables.</summary>
        virtual protected void DoDailyCleanup()
        {
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
        }

        #endregion

        #region Class properties

        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        [XmlIgnore]
        public Biomass Allocated { get; set; }

        /// <summary>Gets the biomass senesced (transferred from live to dead material)</summary>
        [XmlIgnore]
        public Biomass Senesced { get; set; }

        /// <summary>Gets the biomass detached (sent to soil/surface organic matter)</summary>
        [XmlIgnore]
        public Biomass Detached { get; set; }

        /// <summary>Gets the biomass removed from the system (harvested, grazed, etc.)</summary>
        [XmlIgnore]
        public Biomass Removed { get; set; }

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        [XmlIgnore]
        public double MaintenanceRespiration { get; private set; }

        /// <summary>Growth Respiration</summary>
        [XmlIgnore]
        public double GrowthRespiration { get; set; }

        private double BiomassTolleranceValue = 0.0000000001;   // 10E-10

        #endregion

        #region IOrgan interface

        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="value">The fractions of biomass to remove</param>
        virtual public void DoRemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType value)
        {
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, value, Live, Dead, Removed, Detached);
        }

        #endregion

        #region Arbitrator methods

        /// <summary>Gets or sets the dm demand.</summary>
        [XmlIgnore]
        [Units("g/m^2")]
        public virtual BiomassPoolType DMDemand
        {
            get
            {
                //Ssume if DMDamndFunction is 0 then even metabolic demand is 0
                if (DMDemandFunction.Value <= 0)
                {
                    StructuralDMDemand = 0;
                    NonStructuralDMDemand = 0;
                }
                else
                {
                    StructuralDMDemand = DMDemandFunction.Value * StructuralFraction.Value / DMConversionEfficiency.Value;
                    double MaximumDM = (StartLive.StructuralWt + StructuralDMDemand) * 1 / StructuralFraction.Value;
                    MaximumDM = Math.Min(MaximumDM, 10000); // FIXME-EIT Temporary solution: Cealing value of 10000 g/m2 to ensure that infinite MaximumDM is not reached when 0% goes to structural fraction   
                    NonStructuralDMDemand = Math.Max(0.0, MaximumDM - StructuralDMDemand - StartLive.StructuralWt - StartLive.NonStructuralWt);
                    NonStructuralDMDemand /= DMConversionEfficiency.Value;
                }
                return new BiomassPoolType { Structural = StructuralDMDemand, NonStructural = NonStructuralDMDemand };
            }
            set { }
        }
        /// <summary>Sets the dm potential allocation.</summary>
        [XmlIgnore]
        public BiomassPoolType DMPotentialAllocation
        {
            set
            {
                if (DMDemand.Structural == 0)
                    if (value.Structural < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of potential DM in " + Name);
                PotentialMetabolicDMAllocation = value.Metabolic;
                PotentialStructuralDMAllocation = value.Structural;
                PotentialDMAllocation = value.Structural + value.Metabolic;
            }
        }

        /// <summary>Gets or sets the dm supply.</summary>
        [XmlIgnore]
        public virtual BiomassSupplyType DMSupply
        {
            get
            {
                return new BiomassSupplyType
                {
                    Fixation = 0.0,
                    Retranslocation = AvailableDMRetranslocation(),
                    Reallocation = 0.0
                };
            }
            set { }
        }

        /// <summary>Gets the amount of DM available for retranslocation</summary>
        /// <returns>DM available to retranslocate</returns>
        public double AvailableDMRetranslocation()
        {
            if (DMRetranslocationFactor != null)
            {
                double availableDM = StartLive.NonStructuralWt * DMRetranslocationFactor.Value;
                if (availableDM < -BiomassTolleranceValue)
                    throw new Exception("Negative DM retranslocation value computed for " + Name);
                return availableDM;
            }
            else
            { // By default retranslocation is turned off!!!!
                return 0.0;
            }
        }

        /// <summary>Gets or sets the N demand.</summary>
        [XmlIgnore]
        public virtual BiomassPoolType NDemand
        {
            get
            {
                double _NitrogenDemandSwitch = 1;
                if (NitrogenDemandSwitch != null) //Default of 1 means demand is always truned on!!!!
                    _NitrogenDemandSwitch = NitrogenDemandSwitch.Value;
                double NDeficit = Math.Max(0.0, MaximumNConc.Value * (Live.Wt + PotentialDMAllocation) - Live.N);
                NDeficit *= _NitrogenDemandSwitch;
                double StructuralNDemand = Math.Min(NDeficit, PotentialStructuralDMAllocation * MinimumNConc.Value);
                double NonStructuralNDemand = Math.Max(0, NDeficit - StructuralNDemand);
                return new BiomassPoolType { Structural = StructuralNDemand, NonStructural = NonStructuralNDemand };
            }
            set { }
        }
        /// <summary>Gets or sets the N supply.</summary>
        [XmlIgnore]
        public virtual BiomassSupplyType NSupply
        {
            get
            {
                return new BiomassSupplyType()
                {
                    Reallocation = AvailableNReallocation(),
                    Retranslocation = AvailableNRetranslocation(),
                    Uptake = 0.0
                };
            }
            set { }
        }

        /// <summary>Gets or sets the n fixation cost.</summary>
        [XmlIgnore]
        public virtual double NFixationCost { get { return 0; } set { } }

        /// <summary>Gets the N amount available for retranslocation</summary>
        /// <returns>N available to retranslocate</returns>
        public double AvailableNRetranslocation()
        {
            if (NRetranslocationFactor != null)
            {
                double labileN = Math.Max(0.0, StartLive.NonStructuralN - StartLive.NonStructuralWt * MinimumNConc.Value);
                double availableN = Math.Max(0.0, labileN - StartNReallocationSupply) * NRetranslocationFactor.Value;
                if (availableN < -BiomassTolleranceValue)
                    throw new Exception("Negative N retranslocation value computed for " + Name);
                return availableN;
            }
            else
            {  // By default retranslocation is turned off!!!!
                return 0.0;
            }
        }

        /// <summary>Gets the N amount available for reallocation</summary>
        /// <returns>N available to reallocate</returns>
        public double AvailableNReallocation()
        {
            double availableN = SenescenceRate.Value * StartLive.NonStructuralN * NReallocationFactor.Value;
            if (availableN < -BiomassTolleranceValue)
                throw new Exception("Negative N reallocation value computed for " + Name);

            return availableN;
        }

        /// <summary>Sets the dm allocation.</summary>
        [XmlIgnore]
        public virtual BiomassAllocationType DMAllocation
        {
            set
            {
                // get DM lost by respiration (growth respiration)
                GrowthRespiration = 0.0;
                GrowthRespiration += value.Structural * (1.0 - DMConversionEfficiency.Value)
                                  + value.NonStructural * (1.0 - DMConversionEfficiency.Value)
                                  + value.Metabolic * (1.0 - DMConversionEfficiency.Value);

                // allocate structural DM
                Allocated.StructuralWt = Math.Min(value.Structural * DMConversionEfficiency.Value, StructuralDMDemand);
                Live.StructuralWt += Allocated.StructuralWt;
                
                // allocate non structural DM
                if ((value.NonStructural * DMConversionEfficiency.Value - DMDemand.NonStructural) > BiomassTolleranceValue)
                    throw new Exception("Non structural DM allocation to " + Name + " is in excess of its capacity");
                if (DMDemand.NonStructural > 0.0)
                {
                    Allocated.NonStructuralWt = value.NonStructural * DMConversionEfficiency.Value;
                    Live.NonStructuralWt += Allocated.NonStructuralWt;
                }

                // allocate metabolic DM
                Allocated.MetabolicWt = value.Metabolic * DMConversionEfficiency.Value;

                // Retranslocation
                if (value.Retranslocation - StartLive.NonStructuralWt > BiomassTolleranceValue)
                    throw new Exception("Retranslocation exceeds non structural biomass in organ: " + Name);
                Live.NonStructuralWt -= value.Retranslocation;
                Allocated.NonStructuralWt -= value.Retranslocation;
            }
        }
        /// <summary>Sets the n allocation.</summary>
        [XmlIgnore]
        public virtual BiomassAllocationType NAllocation
        {
            set
            {
                Live.StructuralN += value.Structural;
                Live.NonStructuralN += value.NonStructural;
                Live.MetabolicN += value.Metabolic;

                Allocated.StructuralN += value.Structural;
                Allocated.NonStructuralN += value.NonStructural;
                Allocated.MetabolicN += value.Metabolic;

                // Retranslocation
                if (MathUtilities.IsGreaterThan(value.Retranslocation, StartLive.NonStructuralN - StartNRetranslocationSupply))
                    throw new Exception("N retranslocation exceeds non structural nitrogen in organ: " + Name);
                Live.NonStructuralN -= value.Retranslocation;
                Allocated.NonStructuralN -= value.Retranslocation;

                // Reallocation
                if (MathUtilities.IsGreaterThan(value.Reallocation, StartLive.NonStructuralN))
                    throw new Exception("N reallocation exceeds non structural nitrogen in organ: " + Name);
                Live.NonStructuralN -= value.Reallocation;
                Allocated.NonStructuralN -= value.Reallocation;
            }
        }

        /// <summary>Gets the maximum N concentration.</summary>
        public double MaxNconc { get { return MaximumNConc.Value; } }

        /// <summary>Gets the minimum N concentration.</summary>
        public double MinNconc { get { return MinimumNConc.Value; } }

        /// <summary>Gets the total (live + dead) dry matter weight (g/m2)</summary>
        public double Wt { get { return Live.Wt + Dead.Wt; } }

        /// <summary>Gets the total (live + dead) N amount (g/m2)</summary>
        public double N { get { return Live.N + Dead.N; } }

        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        public double Nconc
        {
            get
            {
                if (Wt > 0.0)
                    return N / Wt;
                else
                    return 0.0;
            }
        }

        #endregion

        #region Events and Event Handlers

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// 
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            StartLive = new Biomass();
            Allocated = new Biomass();
            Senesced = new Biomass();
            Detached = new Biomass();
            Removed = new Biomass();
            Clear();
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        virtual protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (Plant.IsAlive)
                DoDailyCleanup();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
            Clear();
        }

        /// <summary>Called when crop is emerging</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Event data</param>
        [EventSubscribe("PlantEmerging")]
        protected void OnPlantEmerging(object sender, EventArgs e)
        {
            //Initialise biomass and nitrogen
            Live.StructuralWt = InitialWtFunction.Value;
            Live.NonStructuralWt = 0.0;
            Live.StructuralN = Live.StructuralWt * MinimumNConc.Value;
            Live.NonStructuralN = (InitialWtFunction.Value * MaximumNConc.Value) - Live.StructuralN;
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                StartLive = Live;
                StartNReallocationSupply = NSupply.Reallocation;
                StartNRetranslocationSupply = NSupply.Retranslocation;
            }
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        protected void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsAlive)
            {
                // Do senescence
                double senescedFrac = SenescenceRate.Value;
                if (Live.Wt * (1.0 - senescedFrac) < BiomassTolleranceValue)
                    senescedFrac = 1.0;  // remaining amount too small, senesce all
                Biomass Loss = Live * senescedFrac;
                Live.Subtract(Loss);
                Dead.Add(Loss);
                Senesced.Add(Loss);

                // Do detachment
                double detachedFrac = DetachmentRateFunction.Value;
                if (Dead.Wt * (1.0 - detachedFrac) < BiomassTolleranceValue)
                    detachedFrac = 1.0;  // remaining amount too small, detach all
                Biomass detaching = Dead * detachedFrac;
                Dead.Multiply(1.0 - detachedFrac);
                if (detaching.Wt > 0.0)
                {
                    Detached.Add(detaching);
                    SurfaceOrganicMatter.Add(detaching.Wt * 10, detaching.N * 10, 0, Plant.CropType, Name);
                }

                // Do maintenance respiration
                MaintenanceRespiration += Live.MetabolicWt * MaintenanceRespirationFunction.Value;
                Live.MetabolicWt *= (1 - MaintenanceRespirationFunction.Value);
                MaintenanceRespiration += Live.NonStructuralWt * MaintenanceRespirationFunction.Value;
                Live.NonStructuralWt *= (1 - MaintenanceRespirationFunction.Value);
            }
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantEnding")]
        protected void DoPlantEnding(object sender, EventArgs e)
        {
            if (Wt > 0.0)
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                SurfaceOrganicMatter.Add(Wt * 10, N * 10, 0, Plant.CropType, Name);
            }

            Clear();
        }

        #endregion
    }
}