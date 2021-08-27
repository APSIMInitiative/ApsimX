namespace Models.PMF.Interfaces
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Functions;
    using Models.PMF.Organs;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static Models.PMF.Interfaces.PlantResourceStates;



    /// <summary>
    /// An interface that defines what needs to be implemented by an organ
    /// </summary>
    public interface IAmOrganHearMeRoar
    {
        ///<summary>The Carbon Element</summary>
        Element Carbon { get; }

        ///<summary>The Carbon Element</summary>
        Element Nitrogen { get; }

        /// <summary>Gets the total biomass</summary>
        Biomass Total { get; }

        /// <summary>Gets the live biomass</summary>
        Biomass Live { get; }

        /// <summary>Gets the live biomass</summary>
        Biomass Dead { get; }

        /// <summary>Gets the senescence rate</summary>
        double senescenceRate { get; }

        /// <summary>Gets the DMConversion efficiency</summary>
        double dmConversionEfficiency { get; }
        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        Biomass Allocated { get; }

        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        Biomass Senesced { get; }

        /// <summary> get the organs uptake object if it has one </summary>
        IWaterNitrogenUptake WaterNitrogenUptakeObject { get; }
    }

    #region Arbitrator data types


    /// <summary>Thresholds for nutrient concentrations</summary>
    [Serializable]
    public class NutrientConcentrationThresholds
    {
        /// <summary>Maximum Nutrient Concentration</summary>
        public double Maximum { get; set; }
        /// <summary>Critical Nutrient Concentration</summary>
        public double Critical { get; set; }
        /// <summary>Minimum Nutrient Concentration</summary>
        public double Minimum { get; set; }
    }

    /// <summary>
    /// Daily state of flows into and out of each organ
    /// </summary>
    [Serializable]
    public class OrganResourceStates
    {
        /// <summary> Resource supplied to arbitration by the organ</summary>
        public ResourceSupplies Supplies { get; set; }

        /// <summary> Resource supplied to arbitration by the organ that was allocated</summary>
        public ResourceSupplies SuppliesAllocated { get; set; }

        /// <summary> Resource demanded by the organ through arbitration</summary>
        public ResourcePools Demands { get; set; }

        /// <summary> demands scaled for priority</summary>
        public ResourcePools PriorityScaledDemand { get; set; }

        /// <summary> Resource demands met as a result of arbitration</summary>
        public ResourcePools DemandsAllocated { get; set; }

        /// <summary> demands as yet un met by arbitration</summary>
        public ResourcePools OutstandingDemands
        {
            get
            {
                ResourcePools outstanding = new ResourcePools();
                outstanding.Structural = Demands.Structural - DemandsAllocated.Structural;
                outstanding.Metabolic = Demands.Metabolic - DemandsAllocated.Structural;
                outstanding.Storage = Demands.Storage - DemandsAllocated.Storage;
                return outstanding;
            }
        }

        /// <summary> The minimum Nutrient Concnetration of biomass</summary>
        public double MinimumConcentration { get; set; }

        /// <summary> The maximum possible biomass with Nutrient Allocation</summary>
        public double MaxCDelta { get; set; }

        /// <summary> The Constructor</summary>
        public OrganResourceStates()
        {
            Supplies = new ResourceSupplies();
            SuppliesAllocated = new ResourceSupplies();
            Demands = new ResourcePools();
            PriorityScaledDemand = new ResourcePools();
            DemandsAllocated = new ResourcePools();

        }

        /// <summary>Clear</summary>    
        public void Clear()
        {
            Supplies.Clear();
            SuppliesAllocated.Clear();
            Demands.Clear();
            PriorityScaledDemand.Clear();
            DemandsAllocated.Clear();
        }

    }

    /// <summary>
    /// The daily state of flows throughout the plant
    /// </summary>
    [Serializable]
    public class PlantResourceStates
    {
        /// <summary>The organs on the plant /// </summary>
        public List<OrganResourceStates> organs { get; }

        /// <summary>The total supply of resoure that may be allocated /// </summary>
        public double TotalPlantSupply { get { return organs.Sum(o => o.Supplies.Total); } }

        /// <summary>The total supply from fixation  /// </summary>
        public double TotalReAllocationSupply { get { return organs.Sum(o => o.Supplies.ReAllocation); } }

        /// <summary>The total supply from fixation  /// </summary>
        public double TotalUptakeSupply { get { return organs.Sum(o => o.Supplies.Uptake); } }

        /// <summary>The total supply from fixation  /// </summary>
        public double TotalFixationSupply { get { return organs.Sum(o => o.Supplies.Fixation); } }
        /// <summary>The total supply from Retranslocation  /// </summary>
        public double TotalReTranslocationSupply { get { return organs.Sum(o => o.Supplies.ReTranslocation); } }

        /// <summary>The total demand for resoure  /// </summary>
        public double TotalPlantDemand { get { return organs.Sum(o => o.Demands.Total); } }

        /// <summary>The total demand for resoure  /// </summary>
        public double TotalPlantPriorityScalledDemand { get { return organs.Sum(o => o.PriorityScaledDemand.Total); } }

        /// <summary>The total demand for resoure  /// </summary>
        public double TotalPlantDemandsAllocated { get { return organs.Sum(o => o.DemandsAllocated.Total); } }

        //Error checking variables
        /// <summary>Gets or sets the start.</summary>
        public double Start { get; set; }
        /// <summary>Gets or sets the end.</summary>
        public double End { get; set; }
        /// <summary>Gets or sets the balance error.</summary>
        public double BalanceError { get; set; }

        /// <summary>The constructor</summary>
        public PlantResourceStates(List<OrganResourceStates> orgs)
        {
            organs = new List<OrganResourceStates>();
            foreach (OrganResourceStates org in orgs)
                organs.Add(org);
            Start = new double();
            End = new double();
            BalanceError = new double();
        }

        /// <summary>Clear</summary>    
        public void Clear()
        {
        }

        /// <summary>
        /// The class that holds states of Structural, Metabolic and Storage components of a resource
        /// </summary>
        [Serializable]
        [ViewName("UserInterface.Views.PropertyView")]
        [PresenterName("UserInterface.Presenters.PropertyPresenter")]
        [ValidParent(ParentType = typeof(Organ))]
        [ValidParent(ParentType = typeof(Element))]
        public class ResourcePools : Model
        {
            /// <summary>Gets or sets the structural.</summary>
            [Units("g/m2")]
            public double Structural { get; set; }
            /// <summary>Gets or sets the storage.</summary>
            [Units("g/m2")]
            public double Storage { get; set; }
            /// <summary>Gets or sets the metabolic.</summary>
            [Units("g/m2")]
            public double Metabolic { get; set; }
            /// <summary>Gets the total amount of biomass.</summary>
            [Units("g/m2")]
            public double Total
            { get { return Structural + Metabolic + Storage; } }

            /// <summary>the constructor.</summary>
            public ResourcePools()
            {
                Structural = new double();
                Metabolic = new double();
                Storage = new double();
            }

            /// <summary>Clear</summary>
            public void Clear()
            {
                Structural = 0;
                Storage = 0;
                Metabolic = 0;
            }

            /// <summary>Add Delta</summary>
            public void AddDelta(ResourcePools delta)
            {
                Structural += delta.Structural;
                Metabolic += delta.Metabolic;
                Storage += delta.Storage;
            }

            /// <summary>Add Delta</summary>
            public void SetTo(ResourcePools newValue)
            {
                Structural = newValue.Structural;
                Metabolic = newValue.Metabolic;
                Storage = newValue.Storage;
            }

            /// <summary>Add Delta</summary>
            public void MultiplyBy(ResourcePools multiplier)
            {
                Structural *= multiplier.Structural;
                Metabolic *= multiplier.Metabolic;
                Storage *= multiplier.Storage;
            }
        }

        /// <summary>
        /// This class holds the functions for calculating the absolute demands and priorities for each resource component. 
        /// </summary>
        [Serializable]
        [ValidParent(ParentType = typeof(Element))]
        public class ResourceDemandFunctions : Model, ICustomDocumentation
        {
            /// <summary>The demand for the structural fraction.</summary>
            [Link(Type = LinkType.Child, ByName = true)]
            [Units("g/m2")]
            public IFunction Structural = null;

            /// <summary>The demand for the metabolic fraction.</summary>
            [Link(Type = LinkType.Child, ByName = true)]
            [Units("g/m2")]
            public IFunction Metabolic = null;

            /// <summary>The demand for the storage fraction.</summary>
            [Link(Type = LinkType.Child, ByName = true)]
            [Units("g/m2")]
            public IFunction Storage = null;

            /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
            /// <param name="tags">The list of tags to add to.</param>
            /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
            /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
            public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
            {
                if (IncludeInDocumentation)
                {
                    // add a heading
                    tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                    // get description of this class.
                    tags.Add(new AutoDocumentation.Paragraph("This is the collection of functions for calculating the demands for each of the biomass pools (Structural, Metabolic, and Storage).", indent));

                    // write memos.
                    foreach (IModel memo in this.FindAllChildren<Memo>())
                        AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                    // write children.
                    foreach (IModel child in this.FindAllChildren<IFunction>())
                        AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
                }
            }
        }

        /// <summary>
        /// The class that holds the states for resource supplies from ReAllocation, Uptake, Fixation and ReTranslocation
        /// </summary>
        [Serializable]
        public class ResourceSupplies
        {
            /// <summary>Gets or sets the fixation.</summary>
            public double Fixation { get; set; }
            /// <summary>Gets or sets the reallocation.</summary>
            public double ReAllocation { get; set; }
            /// <summary>Gets or sets the uptake.</summary>
            public double Uptake { get; set; }
            /// <summary>Gets or sets the retranslocation.</summary>
            public double ReTranslocation { get; set; }

            /// <summary>Gets the total supply.</summary>
            public double Total
            { get { return Fixation + ReAllocation + ReTranslocation + Uptake; } }

            /// <summary>The constructor.</summary>
            public ResourceSupplies()
            {
                Fixation = new double();
                ReAllocation = new double();
                Uptake = new double();
                ReTranslocation = new double();
            }

            internal void Clear()
            {
                Fixation = 0;
                ReAllocation = 0;
                Uptake = 0;
                ReTranslocation = 0;
            }
        }

        /// <summary>
        /// This class holds the functions for calculating the absolute supplies for each resource component. 
        /// </summary>
        [Serializable]
        [ValidParent(ParentType = typeof(Element))]
        public class ResourceSupplyFunctions : Model, ICustomDocumentation
        {
            /// <summary>The supply from reallocaiton from senesed material</summary>
            [Link(Type = LinkType.Child, ByName = true)]
            [Units("g/m2")]
            public IFunction ReAllocation = null;

            /// <summary>The supply from uptake</summary>
            [Link(Type = LinkType.Child, ByName = true)]
            [Units("g/m2")]
            public IFunction Uptake = null;

            /// <summary>The supply from fixation.</summary>
            [Link(Type = LinkType.Child, ByName = true)]
            [Units("g/m2")]
            public IFunction Fixation = null;

            /// <summary>The supply from retranslocation of storage</summary>
            [Link(Type = LinkType.Child, ByName = true)]
            [Units("g/m2")]
            public IFunction ReTranslocation = null;

            /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
            /// <param name="tags">The list of tags to add to.</param>
            /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
            /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
            public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
            {
                if (IncludeInDocumentation)
                {
                    // add a heading
                    tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                    // get description of this class.
                    tags.Add(new AutoDocumentation.Paragraph("This is the collection of functions for calculating the demands for each of the biomass pools (Structural, Metabolic, and Storage).", indent));

                    // write memos.
                    foreach (IModel memo in this.FindAllChildren<Memo>())
                        AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                    // write children.
                    foreach (IModel child in this.FindAllChildren<IFunction>())
                        AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
                }
            }
        }


    }
    #endregion

}
