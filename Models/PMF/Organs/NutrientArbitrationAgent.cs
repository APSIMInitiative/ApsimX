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
    public class NutrientArbitrationAgent : Model, IAmANutrientArbitrationAgent, ICustomDocumentation
    {
        ///1. Links
        ///------------------------------------------------------------------------------------------------
        
        /// <summary>Tolerance for biomass comparisons</summary>
        protected double BiomassToleranceValue = 0.0000000001; 

        /// <summary>The parent plant</summary>
        [Link(Type = LinkType.Ancestor)]
        private ISubscribeToBiomassArbitration organ = null;

        /// <summary>The N retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        public IFunction NRetranslocationFactor = null;

        /// <summary>The N reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        protected IFunction nReallocationFactor = null;

        /// <summary>The N demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private BiomassDemandAndPriority nDemands = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        ///3. The Constructor
        /// -------------------------------------------------------------------------------------------------
        /// <summary>Constructor</summary>

        public NutrientArbitrationAgent()
        {
            NDemand = new BiomassPoolType();
            NSupply = new BiomassSupplyType();
            potentialDMAllocation = new BiomassPoolType();
        }

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------

        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------
        /// <summary>The nitrogen supply</summary>
        public BiomassSupplyType NSupply { get; set; }

        /// <summary>Structural nitrogen demand</summary>
        public BiomassPoolType NDemand { get; set; }

        /// <summary>The dry matter potentially being allocated</summary>
        public BiomassPoolType potentialDMAllocation { get; set; }

        ///6. Public methods
        /// --------------------------------------------------------------------------------------------------


        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        [EventSubscribe("SetNSupply")]
        protected virtual void SetNSupply(object sender, EventArgs e)
        {
            NSupply.Reallocation = Math.Max(0, (organ.StartLive.StorageN + organ.StartLive.MetabolicN) * organ.senescenceRate * nReallocationFactor.Value());
            if (NSupply.Reallocation < -BiomassToleranceValue)
                throw new Exception("Negative N reallocation value computed for " + Name);

            NSupply.Retranslocation = Math.Max(0, (organ.StartLive.StorageN + organ.StartLive.MetabolicN) * (1 - organ.senescenceRate) * NRetranslocationFactor.Value()); ;
            if (NSupply.Retranslocation < -BiomassToleranceValue)
                throw new Exception("Negative N retranslocation value computed for " + Name);

            NSupply.Fixation = 0;
            NSupply.Uptake = 0;
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        [EventSubscribe("SetNDemand")]
        protected virtual void SetNDemand(object sender, EventArgs e)
        {
            NDemand.Structural = nDemands.Structural.Value();
            NDemand.Metabolic = nDemands.Metabolic.Value();
            NDemand.Storage = nDemands.Storage.Value();
            NDemand.QStructuralPriority = nDemands.QStructuralPriority.Value();
            NDemand.QStoragePriority = nDemands.QStoragePriority.Value();
            NDemand.QMetabolicPriority = nDemands.QMetabolicPriority.Value();
        }

        /// <summary>Sets the n allocation.</summary>
        /// <param name="nitrogen">The nitrogen allocation</param>
        public virtual void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            organ.Live.StructuralN += nitrogen.Structural;
            organ.Live.StorageN += nitrogen.Storage;
            organ.Live.MetabolicN += nitrogen.Metabolic;

            organ.Allocated.StructuralN += nitrogen.Structural;
            organ.Allocated.StorageN += nitrogen.Storage;
            organ.Allocated.MetabolicN += nitrogen.Metabolic;

            if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, organ.StartLive.StorageN + organ.StartLive.MetabolicN - NSupply.Reallocation))
                throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);

            double storageRetranslocation = Math.Min(organ.Live.StorageN, nitrogen.Retranslocation);
            organ.Live.StorageN -= storageRetranslocation;
            organ.Allocated.StorageN -= storageRetranslocation;

            double metabolicRetranslocation = nitrogen.Retranslocation - storageRetranslocation;
            organ.Live.MetabolicN -= metabolicRetranslocation;
            organ.Allocated.MetabolicN -= metabolicRetranslocation;

            // Reallocation
            double senescedFrac = organ.senescenceRate;
            if (organ.StartLive.Wt * (1.0 - senescedFrac) < BiomassToleranceValue)
                senescedFrac = 1.0;  // remaining amount too small, senesce all

            if (MathUtilities.IsGreaterThan(nitrogen.Reallocation, organ.StartLive.StorageN + organ.StartLive.MetabolicN))
                throw new Exception("N reallocation exceeds storage + metabolic nitrogen in organ: " + Name);
            double StorageNReallocation = Math.Min(nitrogen.Reallocation, organ.StartLive.StorageN * senescedFrac * nReallocationFactor.Value());
            organ.Live.StorageN -= StorageNReallocation;
            organ.Live.MetabolicN -= (nitrogen.Reallocation - StorageNReallocation);
            organ.Allocated.StorageN -= nitrogen.Reallocation;

            // now move the remaining senescing material to the dead pool
            Biomass Loss = new Biomass();
            Loss.StructuralN = organ.StartLive.StructuralN * senescedFrac;
            Loss.StorageN = organ.StartLive.StorageN * senescedFrac - StorageNReallocation;
            Loss.MetabolicN = organ.StartLive.MetabolicN * senescedFrac - (nitrogen.Reallocation - StorageNReallocation);
            Loss.StructuralWt = organ.StartLive.StructuralWt * senescedFrac;
            Loss.MetabolicWt = organ.StartLive.MetabolicWt * senescedFrac;
            Loss.StorageWt = organ.StartLive.StorageWt * senescedFrac;
            organ.Live.Subtract(Loss);
            organ.Dead.Add(Loss);
            organ.Senesced.Add(Loss);
        }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            NSupply.Clear();
            NDemand.Clear();
            potentialDMAllocation.Clear();
        }


        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            NDemand = new BiomassPoolType();
            NSupply = new BiomassSupplyType();
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
