namespace Models.PMF.Organs
{
    using Core;
    using Functions;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using PMF;
    using Newtonsoft.Json;


    /// <summary>
    /// This is the basic organ class that contains biomass structures and transfers
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class OrganNutrientDelta : Model, ICustomDocumentation
    {

        ///1. Links
        ///------------------------------------------------------------------------------------------------

        /// <summary>The parent plant</summary>
        [Link(Type = LinkType.Ancestor)]
        private Organ organ = null;

         /// <summary>The demand of nutrients by the organ from the arbitrator</summary>
        [Link(Type = LinkType.Child)]
        [Units("g/m2/d")]
        private NutrientDemandFunctions demandFunctions = null;

        /// <summary>The supply of nutrients from the organ to the arbitrator</summary>
        [Units("g/m2")]
        [Link(Type = LinkType.Child)]
        private NutrientSupplyFunctions supplyFunctions = null;

        /// <summary>The concentrations of nutrients at cardinal thresholds</summary>
        [Units("g Nutrient/g dWt")]
        [Link(Type = LinkType.Child)]
        private IConcentratinOrProportion concentrationOrProportionFunction = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Tolerance for biomass comparisons</summary>
        protected double BiomassToleranceValue = 0.0000000001;


        ///3. The Constructor
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Constructor</summary>
        public OrganNutrientDelta() 
        {
            //demandFunctions = new NutrientDemandFunctions();
            //supplyFunctions = new NutrientSupplyFunctions();
            //thresholds = new NutrientConcentrationFunctions();
            Supplies = new OrganNutrientSupplies();
            SuppliesAllocated = new OrganNutrientSupplies();
            Demands = new NutrientPoolStates();
            PriorityScaledDemand = new NutrientPoolStates();
            DemandsAllocated = new NutrientPoolStates();
        }

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------

        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------
        /// <summary>The dry matter potentially being allocated</summary>
 
        /// <summary>The max, crit and min nutirent concentrations</summary>
        [JsonIgnore]
        public NutrientPoolStates ConcentrationOrProportion { get; set; }

        /// <summary> Resource supplied to arbitration by the organ</summary>
        /// [JsonIgnore]
        public OrganNutrientSupplies Supplies { get; set; }

        /// <summary> Resource supplied to arbitration by the organ that was allocated</summary>
        [JsonIgnore]
        public OrganNutrientSupplies SuppliesAllocated { get; set; }

        /// <summary> Resource demanded by the organ through arbitration</summary>
        [JsonIgnore]
        public NutrientPoolStates Demands { get; set; }

        /// <summary> demands scaled for priority</summary>
        [JsonIgnore]
        public NutrientPoolStates PriorityScaledDemand { get; set; }

        /// <summary> Resource demands met as a result of arbitration</summary>
        [JsonIgnore]
        public NutrientPoolStates DemandsAllocated { get; set; }

        /// <summary> demands as yet un met by arbitration</summary>
        [JsonIgnore]
        public NutrientPoolStates OutstandingDemands
        {
            get
            {
                NutrientPoolStates outstanding = new NutrientPoolStates();
                outstanding.Structural = Demands.Structural - DemandsAllocated.Structural;
                outstanding.Metabolic = Demands.Metabolic - DemandsAllocated.Metabolic;
                outstanding.Storage = Demands.Storage - DemandsAllocated.Storage;
                return outstanding;
            }
        }

        /// <summary> The maximum possible biomass with Nutrient Allocation</summary>
        
        public double MaxCDelta { get; set; }


        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        private double ThrowIfNegative (IFunction function)
        {
                if (function.Value() < -0.0000000000001)
                    throw new Exception((function as IModel).FullPath + " is returning a negative supply ("+ function.Value()+ ").  It must be >= 0");
                else
                    return function.Value();
        }

        private NutrientPoolStates ThrowIfNegative(NutrientPoolFunctions functions)
        {
            NutrientPoolStates returns = new NutrientPoolStates();
            returns.Structural = ThrowIfNegative(functions.Structural);
            returns.Metabolic = ThrowIfNegative(functions.Metabolic);
            returns.Storage = ThrowIfNegative(functions.Storage);
            return returns;
        }


        private void setConcentrationsOrProportions()
        {
            ConcentrationOrProportion = concentrationOrProportionFunction.ConcentrationsOrProportions;
            if (this.Name == "Carbon")
                if ((ConcentrationOrProportion.Total > 1.01) || (ConcentrationOrProportion.Total < 0.99))
                    throw new Exception("Concentrations of Carbon must add to 1 to keep demands entire");
        }
        
        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        public void SetSuppliesAndDemands()
        { 
            setConcentrationsOrProportions();
            Supplies.ReAllocation = ThrowIfNegative(supplyFunctions.ReAllocation);
            Supplies.ReTranslocation = ThrowIfNegative(supplyFunctions.ReTranslocation ) * (1 - organ.senescenceRate);
            Supplies.Fixation = ThrowIfNegative(supplyFunctions.Fixation);
            Supplies.Uptake = ThrowIfNegative(supplyFunctions.Uptake);

            double dMCE = organ.dmConversionEfficiency;
            if (dMCE > 0.0)
            {
                Demands.Structural = (ThrowIfNegative(demandFunctions.Structural) / dMCE);
                Demands.Metabolic = (ThrowIfNegative(demandFunctions.Metabolic) / dMCE);
                Demands.Storage = (ThrowIfNegative(demandFunctions.Storage) / dMCE);
                PriorityScaledDemand.Structural = demandFunctions.Structural.Value() * demandFunctions.QStructuralPriority.Value();
                PriorityScaledDemand.Metabolic = demandFunctions.Metabolic.Value() * demandFunctions.QMetabolicPriority.Value();
                PriorityScaledDemand.Storage = demandFunctions.Storage.Value() * demandFunctions.QStoragePriority.Value();
            }
            else
            { // Conversion efficiency is zero!!!!
                Demands.Structural = 0;
                Demands.Storage = 0;
                Demands.Metabolic = 0;
            }
        }

       
        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            Supplies.Clear();
            SuppliesAllocated.Clear();
            Demands.Clear();
            PriorityScaledDemand.Clear();
            DemandsAllocated.Clear();
        }

        /// <summary>Called when [Sowing] is broadcast</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Sowing")]
        protected void OnSowing(object sender, EventArgs e)
        {
            setConcentrationsOrProportions();
        }
        
        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            Clear();
            if (organ.parentPlant.IsAlive)
                SetSuppliesAndDemands();
        }

            /// <summary>Called when [simulation commencing].</summary>
            /// <param name="sender">The sender.</param>
            /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
            [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            // Deltas = new OrganResourceStates();
            // Live = new ResourcePools();
            //  Dead = new ResourcePools();
            ConcentrationOrProportion = new NutrientPoolStates();
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
