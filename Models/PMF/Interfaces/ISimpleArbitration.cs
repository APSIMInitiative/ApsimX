namespace Models.PMF.Interfaces
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Functions;
    using Models.PMF.Organs;
    using System;
    using System.Collections.Generic;
    using System.Linq;



    /// <summary>
    /// An interface that defines what needs to be implemented by an organ
    /// that communicates to the OrganArbitrator.
    /// </summary>
    /// <remarks>
    ///  PFM considers four types of biomass supply, i.e.
    ///  - fixation
    ///  - reallocation
    ///  - uptake
    ///  - retranslocation
    /// PFM considers eight types of biomass allocation, i.e.
    ///  - structural
    ///  - non-structural
    ///  - metabolic
    ///  - retranslocation
    ///  - reallocation
    ///  - respired
    ///  - uptake
    ///  - fixation
    /// </remarks>
    public interface ISubscribeToBiomassArbitration
    {

        /// <summary>Gets the total biomass</summary>
        Biomass Total { get; }

        /// <summary>Gets the live biomass</summary>
        Biomass Live { get; }

        /// <summary>Gets the live biomass</summary>
        Biomass Dead { get; }

        /// <summary>Gets the live biomass at the start of the day</summary>
        Biomass StartLive { get; }

        /// <summary>The supplies, demands and allocations of Carbon</summary>
        IAmTheOrgansCarbonArbitrationAgent Carbon { get; }
        /// <summary> The supplies, demands and allocation of nutrients</summary>
        List<IAmANutrientArbitrationAgent> Nutrients { get; }

        /// <summary>Gets the senescence rate</summary>
        double senescenceRate { get; }

        /// <summary>Gets the DMConversion efficiency</summary>
        double dmConversionEfficiency { get; }
        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        Biomass Allocated { get; }

        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        Biomass Senesced { get;  }

        /// <summary>Gets or sets the minimum nconc.</summary>
        double MinNconc { get; }

    }

    /// <summary>
    /// An interface that defines what needs to be implemented by an organ
    /// that communicates to the OrganArbitrator.
    /// </summary>
    /// <remarks>
    ///  PFM considers four types of biomass supply, i.e.
    ///  - fixation
    ///  - reallocation
    ///  - uptake
    ///  - retranslocation
    /// PFM considers eight types of biomass allocation, i.e.
    ///  - structural
    ///  - non-structural
    ///  - metabolic
    ///  - retranslocation
    ///  - reallocation
    ///  - respired
    ///  - uptake
    ///  - fixation
    /// </remarks>
    public interface IAmTheOrgansCarbonArbitrationAgent
    {
        /// <summary>Returns the organs dry matter demand</summary>
        BiomassPoolType DMDemand { get; }

        /// <summary>Returns the organs dry matter supply</summary>
        BiomassSupplyType DMSupply { get; }

        /// <summary>Returns the DM that can be paritioned to the organ of N is not limited </summary>
        BiomassPoolType potentialDMAllocation { get; }

        /// <summary>Sets the dry matter potential allocation.</summary>
        void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter);

        /// <summary>Sets the dry matter allocation.</summary>
        void SetDryMatterAllocation(BiomassAllocationType dryMatter);

        /// <summary>Clear the agent</summary>
        void Clear();
    }

    /// <summary>
    /// An interface that defines what needs to be implemented by an organ
    /// that communicates to the OrganArbitrator.
    /// </summary>
    /// <remarks>
    ///  PFM considers four types of biomass supply, i.e.
    ///  - fixation
    ///  - reallocation
    ///  - uptake
    ///  - retranslocation
    /// PFM considers eight types of biomass allocation, i.e.
    ///  - structural
    ///  - non-structural
    ///  - metabolic
    ///  - retranslocation
    ///  - reallocation
    ///  - respired
    ///  - uptake
    ///  - fixation
    /// </remarks>
    public interface IAmANutrientArbitrationAgent
    {
        /// <summary>Returns the organs N demand</summary>
        BiomassPoolType NDemand { get; }

        /// <summary>Returns the organs N supply</summary>
        BiomassSupplyType NSupply { get; }

        /// <summary>Sets the n allocation.</summary>
        void SetNitrogenAllocation(BiomassAllocationType nitrogen);

        /// <summary>Clear the agent</summary>
        void Clear();
    }

    #region Arbitrator data types
    /// <summary>Contains the variables for allocation types</summary>
    [Serializable]
    public class AllocationType
    {
        /// <summary>The total supply for the type of allocation</summary>
        public double TotalSupply { get { return Supply.Sum(); } }

        /// <summary>The supply from each organt for the type of allocation</summary>
        public double[] Supply { get; set; }

        /// <summary>The amount of biomass allocated by the allocation event</summary>
        public double TotalAllocated { get { return Allocated.Sum(); } }

        /// <summary>The amount of biomass allocated to each organ</summary>
        public double[] Allocated { get; set; }

        /// <summary> The constructor</summary>
        /// <param name="NumberOfOrgans"></param>
        public AllocationType(int NumberOfOrgans)
        {
            Supply = new double[NumberOfOrgans];
            Allocated = new double[NumberOfOrgans];
        }

        /// <summary> clear the data arrays</summary>
        /// <param name="NumberOfOrgans"></param>
        public void Clear(int NumberOfOrgans)
        {
            Array.Clear(Supply, 0, NumberOfOrgans);
            Array.Clear(Allocated, 0, NumberOfOrgans);
        }


    }

    /// <summary>Contains the variables need for arbitration</summary>
    [Serializable]
    public class BiomassArbitrationStates
    {
        /// <summary>Names of all organs</summary>
        private ISubscribeToBiomassArbitration[] organs;
        
        //Biomass Demand Variables
        /// <summary>Gets or sets the structural demand.</summary>
        /// <value>Demand for structural biomass from each organ</value>
        public double[] StructuralDemand { get; set; }
        /// <summary>Gets or sets the total structural demand.</summary>
        /// <value>Demand for structural biomass from the crop</value>
        public double TotalStructuralDemand { get { return StructuralDemand.Sum(); } }
        /// <summary>Gets or sets the metabolic demand.</summary>
        /// <value>Demand for metabolic biomass from each organ</value>
        public double[] MetabolicDemand { get; set; }
        /// <summary>Gets or sets the total metabolic demand.</summary>
        /// <value>Demand for metabolic biomass from the crop</value>
        public double TotalMetabolicDemand { get { return MetabolicDemand.Sum(); } }
        /// <summary>Gets or sets the non structural demand.</summary>
        /// <value>Demand for non-structural biomass from each organ</value>
        public double[] StorageDemand { get; set; }
        /// <summary>Gets or sets the total non structural demand.</summary>
        /// <value>Demand for non-structural biomass from the crop</value>
        public double TotalStorageDemand { get { return StorageDemand.Sum(); } }
        /// <summary>Gets or sets the total crop demand.</summary>
        /// <value>crop demand for biomass, structural, non-sturctural and metabolic</value>
        public double TotalPlantDemand { get { return TotalStructuralDemand + TotalMetabolicDemand + TotalStorageDemand; } }

        /// <summary>Biomass supplies and allocation from senescience reallocaiton</summary>

        public AllocationType ReAllocation { get; set; }
        /// <summary>Biomass supplies and allocation from uptake</summary>
        public AllocationType Uptake { get; set; }
        /// <summary>Biomass supplies and allocation from fixation</summary>
        public AllocationType Fixation { get; set; }
        /// <summary>Biomass supplies and allocation from Retarnslocation</summary>
        public AllocationType ReTranslocation { get; set; }


        /// <value>Total supply of nutrient</value>
        public double TotalPlantSupply { get { return ReAllocation.TotalSupply + Uptake.TotalSupply + Fixation.TotalSupply + ReTranslocation.TotalSupply; } }

        /// <value>SupplyDemandRatioN</value>
        public double SupplyDemandRatioN { get; set; }

        //Biomass Allocation Variables
        /// <summary>Gets or sets the reallocation.</summary>
        /// <value>The amount of biomass reallocated from each organ as it dies</value>
        public double[] Respiration { get; set; }
        /// <summary>Gets or sets the total respiration.</summary>
        /// <value>Total respiration by the crop</value>
        public double TotalRespiration { get; set; }
        /// <summary>Gets or sets the constrained growth.</summary>
        /// <value>Biomass growth that is possible given nutrient availability and minimum N concentratins of organs</value>
        public double[] ConstrainedGrowth { get; set; }
        /// <summary>Gets or sets the structural allocation.</summary>
        /// <value>The actual amount of structural biomass allocated to each organ</value>
        public double[] StructuralAllocation { get; set; }
        /// <summary>Gets or sets the total structural allocation.</summary>
        /// <value>The total structural biomass allocation to the whole crop</value>
        public double TotalStructuralAllocation { get { return StructuralAllocation.Sum(); } }
        /// <summary>Gets or sets the metabolic allocation.</summary>
        /// <value>The actual meatabilic biomass allocation to each organ</value>
        public double[] MetabolicAllocation { get; set; }
        /// <summary>Gets or sets the total metabolic allocation.</summary>
        /// <value>The metabolic biomass allocation to each organ</value>
        public double TotalMetabolicAllocation { get { return MetabolicAllocation.Sum(); } }
        /// <summary>Gets or sets the non structural allocation.</summary>
        /// <value>The actual non-structural biomass allocation to each organ</value>
        public double[] StorageAllocation { get; set; }
        /// <summary>Gets or sets the total non structural allocation.</summary>
        /// <value>The total non-structural allocationed to the crop</value>
        public double TotalStorageAllocation { get { return StorageAllocation.Sum(); } }
        /// <summary>Gets or sets the total allocation.</summary>
        /// <value>The actual biomass allocation to each organ, structural, non-structural and metabolic</value>
        public double[] TotalAllocation { get; set; }
        /// <summary>Gets or sets the total biomass already allocated within the plant.</summary>
        /// <value>crop biomass already allocated</value>
        public double TotalPlantAllocation { get { return TotalStructuralAllocation + TotalMetabolicAllocation + TotalStorageAllocation; } }
        /// <summary>Gets or sets the total allocated.</summary>
        /// <value>The amount of biomass allocated to the whole crop</value>
        public double Allocated { get; set; }
        /// <summary>Gets or sets the not allocated.</summary>
        /// <value>The biomass available that was not allocated.</value>
        public double NotAllocated { get; set; }
        /// <summary>Gets or sets the sink limitation.</summary>
        /// <value>The amount of biomass that could have been assimilated but was not because the demand from organs was insufficient.</value>
        public double SinkLimitation { get; set; }
        /// <summary>Gets or sets the limitation due to nutrient shortage</summary>
        /// <value>The amount of biomass that could have been assimilated but was not becasue nutrient supply was insufficient to meet organs minimunn N concentrations</value>
        public double NutrientLimitation { get; set; }
        //Error checking variables
        /// <summary>Gets or sets the start.</summary>
        /// <value>The start.</value>
        public double Start { get; set; }
        /// <summary>Gets or sets the end.</summary>
        /// <value>The end.</value>
        public double End { get; set; }
        /// <summary>Gets or sets the balance error.</summary>
        /// <value>The balance error.</value>
        public double BalanceError { get; set; }
        /// <summary>the type of biomass being arbitrated</summary>
        /// <value>The balance error.</value>
        public string BiomassType { get; set; }
        /// <summary>Priority coefficients for structural biomass for each organ.  Only relevent it QPriorityThenRelativeAllocation method used</summary>
        public double[] QStructural { get; set; }
        /// <summary>Priority coefficients for Metabolic biomass for each organ.  Only relevent it QPriorityThenRelativeAllocation method used</summary>
        public double[] QMetabolic { get; set; }
        /// <summary>Priority coefficients for storage biomass for each organ.  Only relevent it QPriorityThenRelativeAllocation method used</summary>
        public double[] QStorage { get; set; }

        //Constructor for Array variables
        /// <summary>Initializes a new instance of the <see cref="BiomassArbitrationType"/> class.</summary>
        /// <param name="type">Type of biomass arbitration</param>
        /// <param name="allOrgans">Names of organs</param>
        public BiomassArbitrationStates(string type, List<ISubscribeToBiomassArbitration> allOrgans)
        {
            BiomassType = type;
            organs = allOrgans.ToArray();
            ReAllocation = new AllocationType(organs.Length);
            Fixation = new AllocationType(organs.Length);
            Uptake = new AllocationType(organs.Length);
            ReTranslocation = new AllocationType(organs.Length);
            StructuralDemand = new double[organs.Length];
            MetabolicDemand = new double[organs.Length];
            StorageDemand = new double[organs.Length];
            QStructural = new double[organs.Length];
            QMetabolic = new double[organs.Length];
            QStorage = new double[organs.Length];
            Respiration = new double[organs.Length];
            ConstrainedGrowth = new double[organs.Length];
            StructuralAllocation = new double[organs.Length];
            MetabolicAllocation = new double[organs.Length];
            StorageAllocation = new double[organs.Length];
            TotalAllocation = new double[organs.Length];
        }

        /// <summary>Setup all supplies</summary>
        /// <param name="suppliesForEachOrgan">The organs supplies.</param>
        /// <param name="totalOfAllOrgans">The total wt or N for all organs</param>
        public void GetSupplies(BiomassSupplyType[] suppliesForEachOrgan, double totalOfAllOrgans)
        {
            Clear();
            Start = totalOfAllOrgans;

            for (int i = 0; i < suppliesForEachOrgan.Length; i++)
            {
                ReAllocation.Supply[i] = suppliesForEachOrgan[i].Reallocation;
                Uptake.Supply[i] = suppliesForEachOrgan[i].Uptake;
                Fixation.Supply[i] = suppliesForEachOrgan[i].Fixation;
                ReTranslocation.Supply[i] = suppliesForEachOrgan[i].Retranslocation;
            }
        }

        /// <summary>Setup all demands</summary>
        /// <param name="demandsForEachOrgan">The organs demands</param>
        public void GetDemands(BiomassPoolType[] demandsForEachOrgan)
        {

            for (int i = 0; i < demandsForEachOrgan.Length; i++)
            {
                if (MathUtilities.IsLessThan(demandsForEachOrgan[i].Structural, 0))
                    throw new Exception((organs[i] as IOrgan).Name + " is returning a negative Structural " + BiomassType + " demand.  Check your parameterisation");
                if (MathUtilities.IsLessThan(demandsForEachOrgan[i].Storage, 0))
                    throw new Exception((organs[i] as IOrgan).Name + " is returning a negative Storage " + BiomassType + " demand.  Check your parameterisation");
                if (MathUtilities.IsLessThan(demandsForEachOrgan[i].Metabolic, 0))
                    throw new Exception((organs[i] as IOrgan).Name + " is returning a negative Metabolic " + BiomassType + " demand.  Check your parameterisation");
                StructuralDemand[i] = demandsForEachOrgan[i].Structural;
                MetabolicDemand[i] = demandsForEachOrgan[i].Metabolic;
                StorageDemand[i] = demandsForEachOrgan[i].Storage;
                ReAllocation.Allocated[i] = 0;
                Uptake.Allocated[i] = 0;
                Fixation.Allocated[i] = 0;
                ReTranslocation.Allocated[i] = 0;
                StructuralAllocation[i] = 0;
                MetabolicAllocation[i] = 0;
                StorageAllocation[i] = 0;
                QStructural[i] = demandsForEachOrgan[i].QStructuralPriority;
                QMetabolic[i] = demandsForEachOrgan[i].QMetabolicPriority;
                QStorage[i] = demandsForEachOrgan[i].QStoragePriority;
            }

            Allocated = 0;
            SinkLimitation = 0;
            NutrientLimitation = 0;
        }




        /// <summary>Clear the arbitration type</summary>
        public void Clear()
        {
            TotalRespiration = 0;
            Allocated = 0;
            NotAllocated = 0;
            SinkLimitation = 0;
            NutrientLimitation = 0;
            Start = 0;
            End = 0;
            BalanceError = 0;

            ReAllocation.Clear(StructuralDemand.Length);
            Fixation.Clear(StructuralDemand.Length);
            Uptake.Clear(StructuralDemand.Length);
            ReTranslocation.Clear(StructuralDemand.Length);
            Array.Clear(StructuralDemand, 0, StructuralDemand.Length);
            Array.Clear(MetabolicDemand, 0, StructuralDemand.Length);
            Array.Clear(StorageDemand, 0, StructuralDemand.Length);
            Array.Clear(Respiration, 0, StructuralDemand.Length);
            Array.Clear(ConstrainedGrowth, 0, StructuralDemand.Length);
            Array.Clear(StructuralAllocation, 0, StructuralDemand.Length);
            Array.Clear(MetabolicAllocation, 0, StructuralDemand.Length);
            Array.Clear(StorageAllocation, 0, StructuralDemand.Length);
            Array.Clear(TotalAllocation, 0, StructuralDemand.Length);
        }

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e)
        {

        }
    }

    /*
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class BiomassPoolType : Model
    {
        /// <summary>Gets or sets the structural.</summary>
        /// <value>The structural.</value>
        [Description("Initial Structural biomass")]
        public double Structural { get; set; }
        /// <summary>Gets or sets the storage.</summary>
        /// <value>The non structural.</value>
        [Description("Initial Storage biomass")]
        public double Storage { get; set; }
        /// <summary>Gets or sets the metabolic.</summary>
        /// <value>The metabolic.</value>
        [Description("Initial Metabolic biomass")]
        public double Metabolic { get; set; }
        /// <summary>Gets or sets the structural Priority.</summary>
        /// <value>The structural Priority.</value>
        [Description("Initial Structural biomass Priority")]
        public double QStructuralPriority { get; set; }
        /// <summary>Gets or sets Storage Priority.</summary>
        /// <value>The Storage Priority.</value>
        [Description("Initial Storage biomass priority")]
        public double QStoragePriority { get; set; }
        /// <summary>Gets or sets the metabolic biomass priority.</summary>
        /// <value>The metabolic.</value>
        [Description("Initial Metabolic biomass priority")]
        public double QMetabolicPriority { get; set; }
        
        /// <summary>Gets the total amount of biomass.</summary>
        public double Total
        { get { return Structural + Metabolic + Storage; } }

        internal void Clear()
        {
            Structural = 0;
            Storage = 0; 
            Metabolic = 0;
            QStructuralPriority = 1;
            QStoragePriority = 1;
            QMetabolicPriority = 1;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class BiomassSupplyType
    {
        /// <summary>Gets or sets the fixation.</summary>
        /// <value>The fixation.</value>
        public double Fixation { get; set; }
        /// <summary>Gets or sets the reallocation.</summary>
        /// <value>The reallocation.</value>
        public double Reallocation { get; set; }
        /// <summary>Gets or sets the uptake.</summary>
        /// <value>The uptake.</value>
        public double Uptake { get; set; }
        /// <summary>Gets or sets the retranslocation.</summary>
        /// <value>The retranslocation.</value>
        public double Retranslocation { get; set; }

        /// <summary>Gets the total supply.</summary>
        public double Total
        { get { return Fixation + Reallocation + Retranslocation + Uptake; } }

        internal void Clear()
        {
            Fixation = 0;
            Reallocation = 0;
            Uptake = 0;
            Retranslocation = 0;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class BiomassAllocationType
    {
        /// <summary>Gets or sets the structural.</summary>
        /// <value>The structural.</value>
        public double Structural { get; set; }
        /// <summary>Gets or sets the non structural.</summary>
        /// <value>The non structural.</value>
        public double Storage { get; set; }
        /// <summary>Gets or sets the metabolic.</summary>
        /// <value>The metabolic.</value>
        public double Metabolic { get; set; }
        /// <summary>Gets or sets the retranslocation.</summary>
        /// <value>The retranslocation.</value>
        public double Retranslocation { get; set; }
        /// <summary>Gets or sets the reallocation.</summary>
        /// <value>The reallocation.</value>
        public double Reallocation { get; set; }
        /// <summary>Gets or sets the respired.</summary>
        /// <value>The respired.</value>
        public double Respired { get; set; }
        /// <summary>Gets or sets the uptake.</summary>
        /// <value>The uptake.</value>
        public double Uptake { get; set; }
        /// <summary>Gets or sets the fixation.</summary>
        /// <value>The fixation.</value>
        public double Fixation { get; set; }
    } */
    #endregion

}
