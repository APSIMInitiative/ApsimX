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
    public class BaseOrgan : Model, IOrgan, IArbitration
    {
        #region Links to other models or compontnets
        /// <summary>The live</summary>
        [Link] [DoNotDocument] public Biomass Live = null;
        
        /// <summary>The dead</summary>
        [Link] [DoNotDocument] public Biomass Dead = null;

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

        #region Arbitration methods
        /// <summary>Gets or sets the dm supply.</summary>
        [XmlIgnore]
        virtual public BiomassSupplyType DMSupply { get { return new BiomassSupplyType(); } set { } }
        /// <summary>Sets the dm potential allocation.</summary>
        [XmlIgnore]
        virtual public BiomassPoolType DMPotentialAllocation { set { } }
        /// <summary>Sets the dm allocation.</summary>
        [XmlIgnore]
        virtual public BiomassAllocationType DMAllocation { set { } }
        /// <summary>Gets or sets the dm demand.</summary>
        [XmlIgnore]
        virtual public BiomassPoolType DMDemand { get { return new BiomassPoolType(); } set { } }
        /// <summary>the efficiency with which allocated DM is converted to organ mass.</summary>
        [XmlIgnore]
        virtual public double DMConversionEfficiency { get { return 1; } set { } }

        /// <summary>Gets or sets the n supply.</summary>
        [XmlIgnore]
        virtual public BiomassSupplyType NSupply { get { return new BiomassSupplyType(); } set { } }
        /// <summary>Sets the n allocation.</summary>
        [XmlIgnore]
        virtual public BiomassAllocationType NAllocation { set { } }
        /// <summary>Gets or sets the n fixation cost.</summary>
        [XmlIgnore]
        virtual public double NFixationCost { get { return 0; } set { } }
        /// <summary>Gets or sets the n demand.</summary>
        [XmlIgnore]
        virtual public BiomassPoolType NDemand { get { return new BiomassPoolType(); } set { } }
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
        public double GrowthRespiration { get; set; }

        /// <summary>Gets the total (live + dead) dm (g/m2)</summary>
        public double Wt { get { return Live.Wt + Dead.Wt; } }

        /// <summary>Gets the total (live + dead) n (g/m2)</summary>
        public double N { get { return Live.N + Dead.N; } }

        /// <summary>Gets the total (live + dead) n conc (g/g)</summary>
        public double Nconc { get { return N / Wt; } }

        /// <summary>Gets the dm amount detached (sent to soil/surface organic matter) (g/m2)</summary>
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
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // write description of this class.
            AutoDocumentation.GetClassDescription(this, tags, indent);

            // write a list of constant functions.
            foreach (IModel child in Apsim.Children(this, typeof(Constant)))
                child.Document(tags, -1, indent);

            // write children.
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
            {
                if (child is Constant || child is Biomass || child is CompositeBiomass || child is ArrayBiomass)
                { } // don't document.
                else
                    child.Document(tags, headingLevel + 1, indent);
            }
        }
        /// <summary>Clears this instance.</summary>
        virtual protected void Clear()
        {
            Live.Clear();
            Dead.Clear();
        }

        /// <summary>Does the zeroing of some varibles.</summary>
        virtual protected void DoDailyCleanup()
        {
            Detached.Clear();
            Removed.Clear();
        }

        #endregion
    }
}