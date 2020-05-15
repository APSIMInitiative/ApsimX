using System;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Library;
using Models.Interfaces;
using System.Xml.Serialization;

namespace Models.PMF.Organs
{
    /// <summary>
    /// A harvest index reproductive organ
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    public class HIReproductiveOrgan : Model, IOrgan, IArbitration, IOrganDamage
    {
        /// <summary>The surface organic matter model</summary>
        [Link]
        public ISurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>The plant</summary>
        [Link]
        protected Plant parentPlant = null;

        /// <summary>Gets or sets the above ground.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction AboveGroundWt = null;

        /// <summary>The water content</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction WaterContent = null;
        /// <summary>The hi increment</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction HIIncrement = null;
        /// <summary>The n conc</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction NConc = null;

        /// <summary>Link to biomass removal model</summary>
        [Link(Type = LinkType.Child)]
        public BiomassRemoval biomassRemovalModel = null;

        /// <summary>The dry matter potentially being allocated</summary>
        public BiomassPoolType potentialDMAllocation { get; set; }

        /// <summary>The daily growth</summary>
        private double DailyGrowth = 0;

        /// <summary>The live biomass</summary>
        public Biomass Live { get; set; }

        /// <summary>The dead biomass</summary>
        public Biomass Dead { get; set; }

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return true; } }

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

        /// <summary>Structural nitrogen demand</summary>
        public BiomassPoolType NDemand { get; set; }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemandPriorityFactor { get; set; }

        /// <summary>The dry matter supply</summary>
        public BiomassSupplyType DMSupply { get; set; }

        /// <summary>The nitrogen supply</summary>
        public BiomassSupplyType NSupply { get;  set; }

        /// <summary>Sets the dm potential allocation.</summary>
        /// <summary>Sets the dry matter potential allocation.</summary>
         public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter) { }
        
        /// <summary>Gets or sets the n fixation cost.</summary>
        [XmlIgnore]
         public double NFixationCost { get { return 0; } }

        /// <summary>Minimum N concentration</summary>
        [XmlIgnore]
         public double MinNconc { get { return 0; } }




        /// <summary>Gets the live f wt.</summary>
        /// <value>The live f wt.</value>
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

        /// <summary>Initializes a new instance of the <see cref="HIReproductiveOrgan"/> class.</summary>
        public HIReproductiveOrgan()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive || parentPlant.IsEnding)
                ClearBiomassFlows();
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
            Allocated = new Biomass();
            Senesced = new Biomass();
            Detached = new Biomass();
            Removed = new Biomass();
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

        /// <summary>Gets the hi.</summary>
        /// <value>The hi.</value>
        public double HI
        {
            get
            {
                double CurrentWt = (Live.Wt + Dead.Wt);
                if (AboveGroundWt.Value() > 0)
                    return CurrentWt / AboveGroundWt.Value();
                else
                    return 0.0;
            }
        }

        /// <summary>Sets the dry matter allocation.</summary>
        public void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            Live.StructuralWt += dryMatter.Structural; DailyGrowth = dryMatter.Structural;
        }

        /// <summary>Sets the n allocation.</summary>
        public  void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            Live.StructuralN += nitrogen.Structural;
        }

        /// <summary>Gets the total biomass</summary>
        public Biomass Total { get { return Live + Dead; } }

        /// <summary>Gets the total grain weight</summary>
        [Units("g/m2")]
        public double Wt { get { return Total.Wt; } }

        /// <summary>Gets the total grain N</summary>
        [Units("g/m2")]
        public double N { get { return Total.N; } }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        [EventSubscribe("SetDMDemand")]
        private void SetDMDemand(object sender, EventArgs e)
        {
            double currentWt = (Live.Wt + Dead.Wt);
            double newHI = HI + HIIncrement.Value();
            double newWt = newHI * AboveGroundWt.Value();
            double demand = Math.Max(0.0, newWt - currentWt);
            DMDemand.Structural = demand;
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        [EventSubscribe("SetNDemand")]
        private void SetNDemand(object sender, EventArgs e)
        {
            double demand = Math.Max(0.0, (NConc.Value() * Live.Wt) - Live.N);
            NDemand.Structural = demand;
        }

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
            Live.Clear();
            Dead.Clear();
            DMDemand.Clear();
            NDemand.Clear();
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
