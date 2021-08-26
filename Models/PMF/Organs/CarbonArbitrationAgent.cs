namespace Models.PMF.Organs
{
    using APSIM.Shared.Utilities;
    using Core;
    using Models.Interfaces;
    using Functions;
    using Interfaces;
    using Library;
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using PMF;

    /// <summary>
    /// This is the basic organ class that contains biomass structures and transfers
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class CarbonArbitrationAgent : Model, IAmTheOrgansCarbonArbitrationAgent, ICustomDocumentation
    {

        ///1. Links
        ///------------------------------------------------------------------------------------------------
        
        /// <summary>The parent plant</summary>
        [Link(Type = LinkType.Ancestor)]
        private ISubscribeToBiomassArbitration organ = null;

        /// <summary>The DM retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        public IFunction dmRetranslocationFactor = null;

        /// <summary>The DM reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction dmReallocationFactor = null;

        /// <summary>The DM demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private BiomassDemandAndPriority dmDemands = null;

        /// <summary>The photosynthesis</summary>
        [Units("g/m2")]
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Photosynthesis = null;

        
        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Tolerance for biomass comparisons</summary>
        protected double BiomassToleranceValue = 0.0000000001;

        /// <summary>The dry matter supply</summary>
        [JsonIgnore]
        public BiomassSupplyType DMSupply { get; set; }

        /// <summary>The dry matter demand</summary>
        [JsonIgnore] 
        public BiomassPoolType DMDemand { get;  set; }

        /// <summary>The dry matter potentially being allocated</summary>
        [JsonIgnore]
        public BiomassPoolType potentialDMAllocation { get; set; }

        /// <summary>Constructor</summary>

        ///3. The Constructor
        /// -------------------------------------------------------------------------------------------------
        public CarbonArbitrationAgent()
        {
            DMDemand = new BiomassPoolType();
            DMSupply = new BiomassSupplyType();
            potentialDMAllocation = new BiomassPoolType();

        }

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------

        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        /// <summary>Gets the biomass retranslocation.</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double RetranslocationWt { get; private set; }


        /// <summary>Gets the potential DM allocation for this computation round.</summary>
        public BiomassPoolType DMPotentialAllocation { get { return potentialDMAllocation; } }


        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Computes the amount of DM available for retranslocation.</summary>
        public double AvailableDMRetranslocation()
        {
            double availableDM = (organ.StartLive.StorageWt - DMSupply.Reallocation) * dmRetranslocationFactor.Value();
            if (availableDM < -BiomassToleranceValue)
                throw new Exception("Negative DM reallocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>Computes the amount of DM available for reallocation.</summary>
        public double AvailableDMReallocation()
        {
            double availableDM = organ.StartLive.StorageWt * organ.senescenceRate * dmReallocationFactor.Value();
            if (availableDM < -BiomassToleranceValue)
                throw new Exception("Negative DM reallocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        [EventSubscribe("SetDMSupply")]
        protected virtual void SetDMSupply(object sender, EventArgs e)
        {
            DMSupply.Reallocation = AvailableDMReallocation();
            DMSupply.Retranslocation = AvailableDMRetranslocation();         
            DMSupply.Fixation = Photosynthesis.Value();
            DMSupply.Uptake = 0;
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        [EventSubscribe("SetDMDemand")]
        protected virtual void SetDMDemand(object sender, EventArgs e)
        {
            double dMCE = organ.dmConversionEfficiency;
            if (dMCE > 0.0)
            {
                DMDemand.Structural = (dmDemands.Structural.Value() / dMCE); 
                DMDemand.Storage = Math.Max(0, dmDemands.Storage.Value() / dMCE) ;
                DMDemand.Metabolic = Math.Max(0, dmDemands.Metabolic.Value() / dMCE) ;
                DMDemand.QStructuralPriority = dmDemands.QStructuralPriority.Value();
                DMDemand.QMetabolicPriority = dmDemands.QMetabolicPriority.Value();
                DMDemand.QStoragePriority = dmDemands.QStoragePriority.Value();
            }
            else
            { // Conversion efficiency is zero!!!!
                DMDemand.Structural = 0;
                DMDemand.Storage = 0;
                DMDemand.Metabolic = 0;
            }
        }

        /// <summary>Sets the dry matter potential allocation.</summary>
        /// <param name="dryMatter">The potential amount of drymatter allocation</param>
        public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            potentialDMAllocation.Structural = dryMatter.Structural;
            potentialDMAllocation.Metabolic = dryMatter.Metabolic;
            potentialDMAllocation.Storage = dryMatter.Storage;
        }


        /// <summary>Sets the dry matter allocation.</summary>
        /// <param name="dryMatter">The actual amount of drymatter allocation</param>
        public virtual void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            // get DM lost by respiration (growth respiration)
            // GrowthRespiration with unit CO2 
            // GrowthRespiration is calculated as 
            // Allocated CH2O from photosynthesis "1 / DMConversionEfficiency.Value()", converted 
            // into carbon through (12 / 30), then minus the carbon in the biomass, finally converted into 
            // CO2 (44/12).
            double dMCE = organ.dmConversionEfficiency;
            
            RetranslocationWt = dryMatter.Retranslocation;

            // allocate structural DM
            organ.Allocated.StructuralWt = Math.Min(dryMatter.Structural * dMCE, DMDemand.Structural);
            organ.Live.StructuralWt += organ.Allocated.StructuralWt;
            
            // allocate non structural DM
            if ((dryMatter.Storage * dMCE - DMDemand.Storage) > BiomassToleranceValue)
                throw new Exception("Non structural DM allocation to " + Name + " is in excess of its capacity");
            
            // Check retranslocation
            if (MathUtilities.IsGreaterThan(dryMatter.Retranslocation, organ.StartLive.StorageWt))
                throw new Exception("Retranslocation exceeds non structural biomass in organ: " + Name);

            double diffWt = dryMatter.Storage - dryMatter.Retranslocation;
            if (diffWt > 0)
            {
                diffWt *= organ.dmConversionEfficiency;
            }
            organ.Allocated.StorageWt = diffWt;
            organ.Live.StorageWt += diffWt;
            // allocate metabolic DM
            organ.Allocated.MetabolicWt = dryMatter.Metabolic * organ.dmConversionEfficiency;
            organ.Live.MetabolicWt += organ.Allocated.MetabolicWt;
        }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            DMSupply.Clear();
            DMDemand.Clear();
            potentialDMAllocation.Clear();
        }


        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            DMDemand = new BiomassPoolType();
            DMSupply = new BiomassSupplyType();
            potentialDMAllocation = new BiomassPoolType();
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading, the name of this organ
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write the basic description of this class, given in the <summary>
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write the memos
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

            }
        }
    }
}
