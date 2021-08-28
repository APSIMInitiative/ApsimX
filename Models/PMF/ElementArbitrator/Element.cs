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
    using static Models.PMF.Interfaces.PlantResourceDeltas;

    /// <summary>
    /// This is the basic organ class that contains biomass structures and transfers
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class Element : Model, ICustomDocumentation
    {

        ///1. Links
        ///------------------------------------------------------------------------------------------------
        
        /// <summary>The parent plant</summary>
        [Link(Type = LinkType.Ancestor)]
        private Organ organ = null;

         /// <summary>The DM demand function</summary>
        [Link(Type = LinkType.Child)]
        [Units("g/m2/d")]
        private BiomassDemandAndPriority demands = null;

        /// <summary>The photosynthesis</summary>
        [Units("g/m2")]
        [Link(Type = LinkType.Child)]
        private ResourceSupplyFunctions supplies = null;

        [Link(IsOptional = true, Type = LinkType.Child)]
        private NutrientConcentrationThresholdFunctions thresholds = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Tolerance for biomass comparisons</summary>
        protected double BiomassToleranceValue = 0.0000000001;


        /// <summary>Sets the type of infestation event</summary>
        [Description("Select the type of infestation event")]
        public TypeOfElement Nutrient { get; set; }

        /// <summary>Options for types of infestation</summary>
        public enum TypeOfElement
        {
            /// <summary>Carbon</summary>
            Carbon,
            /// <summary>Nitrogen</summary>
            Nitrogen,
            /// <summary>Phosphorus</summary>
            Phosphorus,
            /// <summary>Potassium</summary>
            Potassium
        }

        ///3. The Constructor
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Constructor</summary>
        public Element() 
        {
            Deltas = new OrganResourceDeltas();
            State = new ResourcePools();
            StartState = new ResourcePools();
        }

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------

        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------
        /// <summary>The dry matter potentially being allocated</summary>
        public OrganResourceDeltas Deltas { get; set; }

        /// <summary> The live components of the resource</summary>
        public ResourcePools State { get; set; }

        /// <summary> The dead components of the resource</summary>
        public ResourcePools StartState { get; set; }

        /// <summary>The max, crit and min nutirent concentrations</summary>
        public NutrientConcentrationThresholds Thresholds { get; set; }

        

        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        private double ThrowIfNegative (IFunction function)
        {
                if (function.Value() < 0)
                    throw new Exception((function as IModel).FullPath + " is returning a negative supply ("+ function.Value()+ ").  It must be >= 0");
                else
                    return function.Value();
        }

 
        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        public void SetSuppliesAndDemands()
        {
            if (thresholds != null)
            {
                Thresholds.Maximum = thresholds.Maximum.Value();
                Thresholds.Critical = thresholds.Critical.Value();
                Thresholds.Minimum = thresholds.Minimum.Value();
                Deltas.MinimumConcentration = Thresholds.Minimum;
            }

            Deltas.Supplies.ReAllocation = ThrowIfNegative(supplies.ReAllocation);
            Deltas.Supplies.ReTranslocation = ThrowIfNegative(supplies.ReTranslocation);
            Deltas.Supplies.Fixation = ThrowIfNegative(supplies.Fixation);
            Deltas.Supplies.Uptake = ThrowIfNegative(supplies.Uptake);

            double dMCE = organ.dmConversionEfficiency;
            if (dMCE > 0.0)
            {
                Deltas.Demands.Structural = (ThrowIfNegative(demands.Structural) / dMCE);
                Deltas.Demands.Metabolic = (ThrowIfNegative(demands.Metabolic) / dMCE);
                Deltas.Demands.Storage = (ThrowIfNegative(demands.Storage) / dMCE);
                Deltas.PriorityScaledDemand.Structural = demands.Structural.Value() * demands.QStructuralPriority.Value();
                Deltas.PriorityScaledDemand.Metabolic = demands.Metabolic.Value() * demands.QMetabolicPriority.Value();
                Deltas.PriorityScaledDemand.Storage = demands.Storage.Value() * demands.QStoragePriority.Value();
            }
            else
            { // Conversion efficiency is zero!!!!
                Deltas.Demands.Structural = 0;
                Deltas.Demands.Storage = 0;
                Deltas.Demands.Metabolic = 0;
            }
        }

        /// <summary>Set resource values once arbitration finished</summary>
        public void SetBiomassDeltas()
        {
            State.AddDelta(Deltas.DemandsAllocated);
        }
        
        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            Deltas.Clear();
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            Deltas.Clear();
            StartState.SetTo(State);
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
            Thresholds = new NutrientConcentrationThresholds();
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
