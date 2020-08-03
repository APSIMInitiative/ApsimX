using System;
using Models.Core;
using Models.Functions;
using Models.PMF.Phen;
using Models.PMF.Interfaces;
using System.Xml.Serialization;
using Models.PMF.Library;
using Models.Interfaces;

namespace Models.PMF.Organs
{
    /// <summary>
    /// # [Name] 
    /// This organ uses a generic model for plant reproductive components.  Yield is calculated from its components in terms of organ number and size (for example, grain number and grain size).  
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    public class ReproductiveOrgan : Model, IOrgan, IArbitration, IOrganDamage
    {
        #region Parameter Input Classes
        /// <summary>The surface organic matter model</summary>
        [Link]
        public ISurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>The plant</summary>
        [Link]
        protected Plant parentPlant = null;

        /// <summary>The summary</summary>
        [Link]
        public ISummary Summary = null;

        /// <summary>The phenology</summary>
        [Link]
        protected Phenology Phenology = null;

        /// <summary>Growth Respiration</summary>
        /// [Units("CO_2")]
        public double GrowthRespiration { get; set; }


        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        [XmlIgnore]
        public Biomass Allocated { get; set; }

        /// <summary>Gets the biomass senesced (transferred from live to dead material)</summary>
        [XmlIgnore]
        public Biomass Senesced { get; set; }

        /// <summary>Gets the DM amount detached (sent to soil/surface organic matter) (g/m2)</summary>
        [XmlIgnore]
        public Biomass Detached { get; set; }

        /// <summary>Gets the DM amount removed from the system (harvested, grazed, etc) (g/m2)</summary>
        [XmlIgnore]
        public Biomass Removed { get; set; }

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        virtual public double MaintenanceRespiration { get { return 0; } set { } }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemand { get; set; }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemandPriorityFactor { get; set; }

        /// <summary>Structural nitrogen demand</summary>
        public BiomassPoolType NDemand { get; set; }

        /// <summary>The dry matter supply</summary>
        public BiomassSupplyType DMSupply { get; set; }

        /// <summary>The nitrogen supply</summary>
        public BiomassSupplyType NSupply { get; set; }

        /// <summary>Gets or sets the n fixation cost.</summary>
        [XmlIgnore]
        public double NFixationCost { get { return 0; } }

        /// <summary>The water content</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        [Description("Water content used to calculate a fresh weight.")]
        IFunction WaterContent = null;
        
        /// <summary>The Maximum potential size of individual grains</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/grain")]
        IFunction MaximumPotentialGrainSize = null;
        
        /// <summary>The number function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/m2")]
        IFunction NumberFunction = null;
        /// <summary>The n filling rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        IFunction NFillingRate = null;
        /// <summary>The maximum n conc</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        IFunction MaximumNConc = null;
        /// <summary>The minimum n conc</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        IFunction MinimumNConc = null;
        /// <summary>Carbon concentration</summary>
        /// [Units("-")]
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction CarbonConcentration = null;

        /// <summary>The dm demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        IFunction DMDemandFunction = null;

        /// <summary>Link to biomass removal model</summary>
        [Link(Type = LinkType.Child)]
        public BiomassRemoval biomassRemovalModel = null;

        /// <summary>Dry matter conversion efficiency</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction DMConversionEfficiency = null;

        /// <summary>The proportion of biomass repired each day</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public IFunction MaintenanceRespirationFunction = null;

        /// <summary>The cost for remobilisation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction RemobilisationCost = null;

        #endregion

        #region Class Fields
        /// <summary>The ripe stage</summary>
        [Description("Stage at which this organ becomes ripe")]
        public string RipeStage { get; set; }
        /// <summary>The _ ready for harvest</summary>
        protected bool _ReadyForHarvest = false;
        
        /// <summary>The dry matter potentially being allocated</summary>
        public BiomassPoolType potentialDMAllocation { get; set; }
        #endregion

        #region Class Properties

        /// <summary>The live biomass</summary>
        public Biomass Live { get; set; }

        /// <summary>The dead biomass</summary>
        public Biomass Dead { get; set; }
        
        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return true; } }

        /// <summary>The number</summary>
        [XmlIgnore]
        [Units("/m^2")]
        public double Number { get; set; }

        /// <summary>The maximum potential size of grains</summary>
        [XmlIgnore]
        [Units("/m^2")]
        public double MaximumSize { get; set; }

        /// <summary>Gets the live fresh weight of grains.</summary>
        [Units("g/m^2")]
        public double LiveFWt
        {
            get
            {
                if (WaterContent != null)
                    return Live.Wt / (1 - WaterContent.Value());
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the individual grain size.</summary>
        [Units("g")]
        public double Size
        {
            get
            {
                if (Number > 0)
                    return Live.Wt / Number;
                else
                    return 0;
            }
        }

        /// <summary>Gets the size of grain using the fresh weight (including water content).</summary>
        [Units("g")]
        private double FSize
        {
            get
            {
                if (Number > 0)
                {
                    if (WaterContent != null)
                        return (Live.Wt / Number) / (1 - WaterContent.Value());
                    else
                        return 0.0;
                }
                else
                    return 0;
            }
        }

        /// <summary>Gets the ready for harvest.</summary>
        public int ReadyForHarvest
        {
            get
            {
                if (_ReadyForHarvest)
                    return 1;
                else
                    return 0;
            }
        }
        #endregion

        #region Functions

        /// <summary>Initializes a new instance of the <see cref="ReproductiveOrgan"/> class.</summary>
        public ReproductiveOrgan()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {
                Number = NumberFunction.Value();
                MaximumSize = MaximumPotentialGrainSize.Value();
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == parentPlant)
            {
                Clear();
                ClearBiomassFlows();
            }
        }

        #endregion

        #region Event handlers

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive || parentPlant.IsEnding)
                ClearBiomassFlows();
        }

        /// <summary>Called when crop is being cut.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Cutting")]
        private void OnCutting(object sender, EventArgs e)
        {
                Summary.WriteMessage(this, "Cutting " + Name + " from " + parentPlant.Name);

                Live.Clear();
                Dead.Clear();
                Number = 0;
                _ReadyForHarvest = false;
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (Wt > 0.0)
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                SurfaceOrganicMatter.Add(Wt * 10, N * 10, 0, parentPlant.CropType, Name);
            }

            Clear();
        }
        #endregion

        #region Arbitrator methods
        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        [EventSubscribe("SetDMDemand")]
        private void SetDMDemand(object sender, EventArgs e)
        {
            DMDemand.Structural = DMDemandFunction.Value() / DMConversionEfficiency.Value();
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        [EventSubscribe("SetNDemand")]
        private void SetNDemand(object sender, EventArgs e)
        {
            double demand = NFillingRate.Value();
            demand = Math.Min(demand, MaximumNConc.Value() * potentialDMAllocation.Structural);
            NDemand.Structural = demand;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            Live = new Biomass();
            Dead = new Biomass();
            DMDemand = new BiomassPoolType();
            DMDemandPriorityFactor = new BiomassPoolType();
            DMDemandPriorityFactor.Structural = 1.0;
            DMDemandPriorityFactor.Metabolic = 1.0;
            DMDemandPriorityFactor.Storage = 1.0;
            NDemand = new BiomassPoolType();
            DMSupply = new BiomassSupplyType();
            NSupply = new BiomassSupplyType();
            potentialDMAllocation = new BiomassPoolType();
            Allocated = new Biomass();
            Senesced = new Biomass();
            Detached = new Biomass();
            Removed = new Biomass();
        }


        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (Phenology.OnStartDayOf(RipeStage))
                _ReadyForHarvest = true;

            //Do Maintenance respiration
            MaintenanceRespiration = 0;
            if (MaintenanceRespirationFunction != null && (Live.MetabolicWt + Live.StorageWt) > 0)
            {
                MaintenanceRespiration += Live.MetabolicWt * MaintenanceRespirationFunction.Value();
                MaintenanceRespiration += Live.StorageWt * MaintenanceRespirationFunction.Value();
            }
        }
        /// <summary>Sets the dry matter potential allocation.</summary>
        public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            if (DMDemand.Structural == 0)
                if (dryMatter.Structural < 0.000000000001) { }//All OK
                else
                    throw new Exception("Invalid allocation of potential DM in" + Name);
            potentialDMAllocation.Structural = dryMatter.Structural;
            // PotentialDailyGrowth = value.Structural;
        }
        /// <summary>Sets the dry matter allocation.</summary>
        public void SetDryMatterAllocation(BiomassAllocationType value)
        {
            // GrowthRespiration with unit CO2 
            // GrowthRespiration is calculated as 
            // Allocated CH2O from photosynthesis "1 / DMConversionEfficiency.Value()", converted 
            // into carbon through (12 / 30), then minus the carbon in the biomass, finally converted into 
            // CO2 (44/12).
            double growthRespFactor = ((1.0 / DMConversionEfficiency.Value()) * (12.0 / 30.0) - 1.0 * CarbonConcentration.Value()) * 44.0 / 12.0;
            GrowthRespiration = (value.Structural) * growthRespFactor;

            Live.StructuralWt += value.Structural * DMConversionEfficiency.Value();
            Allocated.StructuralWt = value.Structural * DMConversionEfficiency.Value();
        }
        /// <summary>Sets the n allocation.</summary>
        public void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            Live.StructuralN += nitrogen.Structural;
            Allocated.StructuralN = nitrogen.Structural;
        }
        /// <summary>Gets or sets the maximum nconc.</summary>
        public double MaxNconc
        {
            get
            {
                return MaximumNConc.Value();
            }
        }
        /// <summary>Gets or sets the minimum nconc.</summary>
        public double MinNconc
        {
            get
            {
                return MinimumNConc.Value();
            }
        }

        /// <summary>Gets the total biomass</summary>
        public Biomass Total { get { return Live + Dead; } }

        /// <summary>Gets the total grain weight</summary>
        [Units("g/m2")]
        public double Wt { get { return Total.Wt; } }

        /// <summary>Gets the total grain N</summary>
        [Units("g/m2")]
        public double N { get { return Total.N; } }


        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        [Units("g/g")]
        public double Nconc
        {
            get
            {
                if (Total.Wt > 0.0)
                    return N / Wt;
                else
                    return 0.0;
            }
        }

        #endregion
        
        /// <summary>Remove maintenance respiration from live component of organs.</summary>
        /// <param name="respiration">The respiration to remove</param>
        public virtual void RemoveMaintenanceRespiration(double respiration)
        {
            double total = Live.MetabolicWt + Live.StorageWt;
            if (respiration > total)
            {
                throw new Exception("Respiration is more than total biomass of metabolic and storage in live component.");
            }
            Live.MetabolicWt = Live.MetabolicWt - (respiration * Live.MetabolicWt / total);
            Live.StorageWt = Live.StorageWt - (respiration * Live.StorageWt / total);
        }

        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="amountToRemove">The fractions of biomass to remove</param>
        public void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amountToRemove)
        {
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, amountToRemove, Live, Dead, Removed, Detached);
        }

        /// <summary>Clears this instance.</summary>
        private void Clear()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>Clears the transferring biomass amounts.</summary>
        private void ClearBiomassFlows()
        {
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
        }
    }
}
