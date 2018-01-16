using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using Models.Interfaces;
using Models.PMF.Functions;


namespace Models.PMF.Organs
{
    /// <summary>
    /// This class represents a base organ
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    public abstract class BaseOrgan : Model, IOrgan
    {
        #region Links to other models or compontnets

        /// <summary>The plant</summary>
        [Link]
        protected Plant Plant = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        public ISurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>The summary</summary>
        [Link]
        public ISummary Summary = null;
        #endregion

        /// <summary>The dry matter supply</summary>
        protected BiomassSupplyType dryMatterSupply = new BiomassSupplyType();

        /// <summary>The nitrogen supply</summary>
        protected BiomassSupplyType nitrogenSupply = new BiomassSupplyType();

        /// <summary>The dry matter demand</summary>
        protected BiomassPoolType dryMatterDemand = new BiomassPoolType();

        /// <summary>Structural nitrogen demand</summary>
        protected BiomassPoolType nitrogenDemand = new BiomassPoolType();

        #region Arbitration methods

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        public virtual BiomassSupplyType CalculateDryMatterSupply()
        {
            return dryMatterSupply;
        }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        public virtual BiomassSupplyType CalculateNitrogenSupply()
        {
            return nitrogenSupply;
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        public virtual BiomassPoolType CalculateDryMatterDemand()
        {
            return dryMatterDemand;
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        public virtual BiomassPoolType CalculateNitrogenDemand()
        {
            return nitrogenDemand;
        }



        /// <summary>Gets or sets the dm supply.</summary>
        [XmlIgnore]
        virtual public BiomassSupplyType DMSupply { get { return dryMatterSupply; } }
        /// <summary>Sets the dm potential allocation.</summary>
        /// <summary>Sets the dry matter potential allocation.</summary>
        virtual public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter){ }
        /// <summary>Sets the dry matter allocation.</summary>
        public virtual void SetDryMatterAllocation(BiomassAllocationType value) { }
        /// <summary>Gets or sets the dm demand.</summary>
        [XmlIgnore]
        virtual public BiomassPoolType DMDemand { get { return dryMatterDemand; } }
        /// <summary>the efficiency with which allocated DM is converted to organ mass.</summary>

        /// <summary>Gets or sets the n supply.</summary>
        [XmlIgnore]
        virtual public BiomassSupplyType NSupply { get { return nitrogenSupply; } }
        /// <summary>Sets the n allocation.</summary>
        public virtual void SetNitrogenAllocation(BiomassAllocationType nitrogen) {  }
        /// <summary>Gets or sets the n fixation cost.</summary>
        [XmlIgnore]
        virtual public double NFixationCost { get { return 0; } }
        /// <summary>Gets or sets the n demand.</summary>
        [XmlIgnore]
        virtual public BiomassPoolType NDemand { get { return nitrogenDemand; } }
        /// <summary>Gets or sets the minimum nconc.</summary>
        [XmlIgnore]
        virtual public double MinNconc { get { return 0; } }

        #endregion

        #region Soil Arbitrator interface

        /// <summary>Gets the n supply uptake.</summary>
        [Units("g/m^2")]
        virtual public double NSupplyUptake { get { return NSupply.Uptake; } }
        #endregion

        #region Organ properties

        /// <summary>Growth Respiration</summary>
        /// [Units("CO_2")]
        public double GrowthRespiration { get; set; }

        /// <summary>Carbon concentration</summary>
        /// [Units("-")]
        public double CarbonConcentration { get; set; }



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
        
        /// <summary>Gets the dm supply photosynthesis.</summary>
        [Units("g/m^2")]
        virtual public double DMSupplyPhotosynthesis { get { return DMSupply.Fixation; } }

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        virtual public double MaintenanceRespiration { get { return 0; }  set { } }

        #endregion

        #region Biomass removal
        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="value">The fractions of biomass to remove</param>
        virtual public void DoRemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType value)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Management event methods

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            Allocated = new PMF.Biomass();
            Senesced = new Biomass();
            Detached = new Biomass();
            Removed = new Biomass();
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

        /// <summary>Do harvest logic for this organ</summary>
        virtual public void DoHarvest() { }

        /// <summary>Do Cutting logic for this organ</summary>
        virtual public void DoCut() { }

        /// <summary>Do Graze logic for this organ</summary>
        virtual public void DoGraze() { }

        /// <summary>
        /// Do prune logic for this organ
        /// </summary>
        virtual public void DoPrune() { }
        #endregion
        
        #region Organ functions

        /// <summary>Does the zeroing of some variables.</summary>
        virtual protected void DoDailyCleanup()
        {
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
        }

        #endregion
    }
}