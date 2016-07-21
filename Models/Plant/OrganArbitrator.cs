using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using Models.Interfaces;
using APSIM.Shared.Utilities;


namespace Models.PMF
{
    ///<summary>    /// The Arbitrator class determines the allocation of dry matter (DM) and Nitrogen between each of the organs in the crop model. Each organ can have up to three differnt pools of biomass:    ///     /// * **Structural biomass** which remains within an organ once it is partitioned there    /// * **Metabolic biomass** which generally remains within an organ but is able to be re-allocated when the organ senesses and may be re-translocated when demand is high relative to supply.    /// * **Non-structural biomass** which is partitioned to organs when supply is high relative to demand and is available for re-translocation to other organs whenever supply from uptake, fixation and re-allocation is lower than demand .    ///     /// The process followed for biomass arbitration is shown in Figure 1. Arbitration responds to events broadcast daily by the central APSIM infrastructure:     ///     /// 1. **doPotentialPlantGrowth**.  When this event is broadcast each organ class executes code to determine their potential growth, biomass supplies and demands.  In addition to demands for structural, non-structural and metabolic biomass (DM and N) each organ may have the following biomass supplies:     /// 	* **Fixation supply**.  From photosynthesis (DM) or symbiotic fixation (N)    /// 	* **Uptake supply**.  Typically uptake of N from the soil by the roots but could also be uptake by other organs (eg foliage application of N).    /// 	* **Retranslocation supply**.  Non-structural biomass that may be moved from organs to meet demands of other organs.    /// 	* **Reallocation supply**. Biomass that can be moved from senescing organs to meet the demands of other organs.    /// 2. **doPotentialPlantPartitioning.** On this event the Arbitrator first executes the DoDMSetup() method to establish the DM supplies and demands from each organ.  It then executes the DoPotentialDMAllocation() method which works out how much biomass each organ would be allocated assuming N supply is not limiting and sends these allocations to the organs.  Each organ then uses their potential DM allocation to determine their N demand (how much N is needed to produce that much DM) and the arbitrator calls DoNSetup() establish N supplies and Demands and begin N arbitration.  Firstly DoNReallocation() is called to redistribute N that the plant has available from senescing organs.  After this step any unmet N demand is considered the plants demand for N uptake from the soil (N Uptake Demand).    /// 3. **doNutrientArbitration.** When this event is broadcast by the model framework the soil arbitrator gets the N uptake demands from each plant (where multiple plants are growing in competition) and their potential uptake from the soil and determines how nuch of their demand that the soil is able to provide.  This value is then passed back to each plant instance as their Nuptake and doNUptakeAllocation() is called to distribute this N between organs.      /// 4. **doActualPlantPartitioning.**  On this event the arbitrator call DoNRetranslocation() and DoNFixation() to satisify any unmet N demands from these sources.  Finally, DoActualDMAllocation is called where DM allocations to each organ are reduced if the N allocation is insufficient to achieve the organs minimum N conentration and final allocations are sent to organs.     /// 
    /// ![Alt Text](..\\..\\Documentation\\Images\\ArbitrationDiagram.PNG)    ///     /// **Figure 1.**  Schematic showing procedure for arbitration of biomass partitioning.  Pink boxes are events that are broadcast each day by the model infrastructure and their numbering shows the order of procedure. Blue boxes are methods that are called when these events are broadcast.  Orange boxes contain properties that make up the organ/arbitrator interface.  Green boxes are organ specific properties.    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class OrganArbitrator : Model, IUptake
    {
        #region Links and Input parameters


        /// <summary>APSIMs clock model</summary>
        [Link]
        public Clock Clock = null;

        /// <summary>The top level plant object in the Plant Modelling Framework</summary>
        [Link]
        public Plant Plant = null;

        /// <summary>The soil</summary>
        [Link]
        public Soils.Soil Soil = null;

        /// <summary>
        /// The list of posible methods that can be used for arbitrating biomass allocation
        /// </summary>
        public enum ArbitrationType
        {
            /// <summary>
            /// Arbitration is performed in two passes for for each of the biomass supply sources.  On the first pass, structural and metabolic biomass is allocated to each organ based on their demand relative to the demand from all organs.  On the second pass any remaining biomass is allocated to non-structural demands based on the organ's relative demand.
            /// </summary>
            RelativeAllocation,
            /// <summary>
            /// Arbitration is performed in two passes for for each of the biomass supply sources.  On the first pass, structural and metabolic biomass is allocated to each organ based on their order of priority with higher priority organs recieving their full demand first. On the second pass any remaining biomass is allocated to non-structural demands based on the relative demand from all organs.
            /// </summary>
            PriorityAllocation,
            /// <summary>
            /// Arbitration is performed in two passes for for each of the biomass supply sources.  On the first pass, structural and metabolic biomass is allocated to each organ based on their order of priority with higher priority organs recieving their full demand first.  On the second pass any remaining biomass is allocated to non-structural demands based on the same order of priority.
            /// </summary>
            PriorityThenRelativeAllocation,
            /// <summary>
            /// Partitions biomass between organs based on their relative demand in a single pass so non-structural always gets some if there is a non-structural demand
            /// </summary>
            RelativeAllocationSinglePass,
        }

        /// <summary>The method used to arbitrate N allocations</summary>
        [Description("Select method used for Nitrogen Arbitration")]
        public ArbitrationType NArbitrationOption { get; set; }
        
        /// <summary>The mentod used to arbitrate DM allocations </summary>
        [Description("Select method used for Dry Matter Arbitration")]
        public ArbitrationType DMArbitrationOption { get; set; }
        
        /// <summary>The nutrient drivers</summary>
        [Description("List of nutrients that the arbitrator will consider")]
        public string[] NutrientDrivers = null;

        /// <summary>The kgha2gsm</summary>
        private const double kgha2gsm = 0.1;

        /// <summary>The list of organs</summary>
        private IArbitration[] Organs;

        /// <summary>Contains the variables need for arbitration</summary>
        [Serializable]
        public class BiomassArbitrationType
        {
            //Biomass Demand Variables
            /// <summary>Gets or sets the structural demand.</summary>
            /// <value>Demand for structural biomass from each organ</value>
            public double[] StructuralDemand { get; set; }
            /// <summary>Gets or sets the total structural demand.</summary>
            /// <value>Demand for structural biomass from the crop</value>
            public double TotalStructuralDemand { get; set; }
            /// <summary>Gets or sets the metabolic demand.</summary>
            /// <value>Demand for metabolic biomass from each organ</value>
            public double[] MetabolicDemand { get; set; }
            /// <summary>Gets or sets the total metabolic demand.</summary>
            /// <value>Demand for metabolic biomass from the crop</value>
            public double TotalMetabolicDemand { get; set; }
            /// <summary>Gets or sets the non structural demand.</summary>
            /// <value>Demand for non-structural biomass from each organ</value>
            public double[] NonStructuralDemand { get; set; }
            /// <summary>Gets or sets the total non structural demand.</summary>
            /// <value>Demand for non-structural biomass from the crop</value>
            public double TotalNonStructuralDemand { get; set; }
            /// <summary>Gets or sets the total demand.</summary>
            /// <value>Total biomass demand from each oragen, structural, non-sturctural and metabolic</value>
            public double[] TotalDemand { get; set; }
            /// <summary>Gets or sets the relative structural demand.</summary>
            /// <value>Structural biomass demand relative to total biomass demand</value>
            public double[] RelativeStructuralDemand { get; set; }
            /// <summary>Gets or sets the relative metabolic demand.</summary>
            /// <value>Metabolic biomass demand relative to total biomass demand</value>
            public double[] RelativeMetabolicDemand { get; set; }
            /// <summary>Gets or sets the relative non structural demand.</summary>
            /// <value>Non-structural biomass demand relative to total biomass demand</value>
            public double[] RelativeNonStructuralDemand { get; set; }
            /// <summary>Gets or sets the total crop demand.</summary>
            /// <value>crop demand for biomass, structural, non-sturctural and metabolic</value>
            public double TotalPlantDemand { get; set; }
            //Biomass Supply Variables
            /// <summary>Gets or sets the reallocation supply.</summary>
            /// <value>Biomass available for reallocation for each organ as it dies</value>
            public double[] ReallocationSupply { get; set; }
            /// <summary>Gets or sets the total reallocation supply.</summary>
            /// <value>Biomass available for reallocation from the entire crop</value>
            public double TotalReallocationSupply { get; set; }
            /// <summary>Gets or sets the uptake supply.</summary>
            /// <value>Biomass available for uptake from each absorbing organ, generally limited to ntrient uptake in roots</value>
            public double[] UptakeSupply { get; set; }
            /// <summary>Gets or sets the total uptake supply.</summary>
            /// <value>Biomass available for uptake by the crop</value>
            public double TotalUptakeSupply { get; set; }
            /// <summary>Gets or sets the fixation supply.</summary>
            /// <value>Biomass that may be fixed by the crop, eg DM fixed by photosynhesis in the leaves of N fixed by nodules</value>
            public double[] FixationSupply { get; set; }
            /// <summary>Gets or sets the total fixation supply.</summary>
            /// <value>Total fixation by the crop</value>
            public double TotalFixationSupply { get; set; }
            /// <summary>Gets or sets the retranslocation supply.</summary>
            /// <value>Supply of labile biomass that can be retranslocated from each oragn</value>
            public double[] RetranslocationSupply { get; set; }
            /// <summary>Gets or sets the total retranslocation supply.</summary>
            /// <value>The total supply of labile biomass in the crop</value>
            public double TotalRetranslocationSupply { get; set; }
            /// <summary>Gets or sets the total crop supply.</summary>
            /// <value>crop supply from uptake, fixation, reallocation and remobilisation</value>
            public double TotalPlantSupply { get; set; }
            
            //Biomass Allocation Variables
            /// <summary>Gets or sets the reallocation.</summary>
            /// <value>The amount of biomass reallocated from each organ as it dies</value>
            public double[] Reallocation { get; set; }
            /// <summary>Gets or sets the total reallocation.</summary>
            /// <value>The total amount of biomass reallocated by the crop</value>
            public double TotalReallocation { get; set; }
            /// <summary>Gets or sets the uptake.</summary>
            /// <value>The actual uptake of biomass by each organ, generally limited to nutrients in the roots</value>
            public double[] Uptake { get; set; }
            /// <summary>Gets or sets the fixation.</summary>
            /// <value>The actual uptake of biomass by the whole crop</value>
            public double[] Fixation { get; set; }
            /// <summary>Gets or sets the retranslocation.</summary>
            /// <value>The actual retranslocation or biomass from each oragan</value>
            public double[] Retranslocation { get; set; }
            /// <summary>Gets or sets the total retranslocation.</summary>
            /// <value>The total amount of biomass retranslocated by the crop</value>
            public double TotalRetranslocation { get; set; }
            /// <summary>Gets or sets the respiration.</summary>
            /// <value>The amount of biomass respired by each organ</value>
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
            public double TotalStructuralAllocation { get; set; }
            /// <summary>Gets or sets the metabolic allocation.</summary>
            /// <value>The actual meatabilic biomass allocation to each organ</value>
            public double[] MetabolicAllocation { get; set; }
            /// <summary>Gets or sets the total metabolic allocation.</summary>
            /// <value>The metabolic biomass allocation to each organ</value>
            public double TotalMetabolicAllocation { get; set; }
            /// <summary>Gets or sets the non structural allocation.</summary>
            /// <value>The actual non-structural biomass allocation to each organ</value>
            public double[] NonStructuralAllocation { get; set; }
            /// <summary>Gets or sets the total non structural allocation.</summary>
            /// <value>The total non-structural allocationed to the crop</value>
            public double TotalNonStructuralAllocation { get; set; }
            /// <summary>Gets or sets the total allocation.</summary>
            /// <value>The actual biomass allocation to each organ, structural, non-structural and metabolic</value>
            public double[] TotalAllocation { get; set; }
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

            //Constructor for Array variables
            /// <summary>Initializes a new instance of the <see cref="BiomassArbitrationType"/> class.</summary>
            public BiomassArbitrationType()
            { }

            /// <summary>Initializes a new instance of the <see cref="BiomassArbitrationType"/> class.</summary>
            /// <param name="Size">The size.</param>
            public BiomassArbitrationType(int Size)
            {
                StructuralDemand = new double[Size];
                MetabolicDemand = new double[Size];
                NonStructuralDemand = new double[Size];
                RelativeStructuralDemand = new double[Size];
                RelativeMetabolicDemand = new double[Size];
                RelativeNonStructuralDemand = new double[Size];
                TotalDemand = new double[Size];
                ReallocationSupply = new double[Size];
                UptakeSupply = new double[Size];
                FixationSupply = new double[Size];
                RetranslocationSupply = new double[Size];
                Reallocation = new double[Size];
                Uptake = new double[Size];
                Fixation = new double[Size];
                Retranslocation = new double[Size];
                Respiration = new double[Size];
                ConstrainedGrowth = new double[Size];
                StructuralAllocation = new double[Size];
                MetabolicAllocation = new double[Size];
                NonStructuralAllocation = new double[Size];
                TotalAllocation = new double[Size];
            }
        }

        /// <summary>The variables for DM</summary>
        public BiomassArbitrationType DM = null;

        /// <summary>The variables for N</summary>
        public BiomassArbitrationType N = null;
        //private BiomassArbitrationType P = null;
        //private BiomassArbitrationType K = null;

        #endregion

        #region Main outputs

        /// <summary>Gets the dm supply.</summary>
        /// <value>Supply of DM from photosynthesising organs</value>
        [XmlIgnore]
        public double DMSupply
        {
            get
            {
                if (Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (Plant.Phenology.Emerged == true)
                            return DM.TotalFixationSupply;
                        else return 0;
                    }
                    else
                        return DM.TotalFixationSupply;
                }
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the dm demand</summary>
        /// <value>Demand of DM from growing organs</value>
        [XmlIgnore]
        public double DMDemand
        {
            get
            {
                if (Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (Plant.Phenology.Emerged == true)
                            return DM.TotalPlantDemand;
                        else return 0;
                    }
                    else
                        return DM.TotalPlantDemand;
                }
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the dm allocations</summary>
        /// <value>Allocation of DM to each organ</value>
        [XmlIgnore]
        public double DMAllocated
        {
            get
            {
                if (Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (Plant.Phenology.Emerged == true)
                            return DM.Allocated;
                        else return 0;
                    }
                    else
                        return DM.Allocated;
                }
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the sink limitation to growth</summary>
        /// <value>The amount of DM that was not fixed because potential growth from organs did not require it</value>
        [XmlIgnore]
        public double DMSinkLimitation
        {
            get
            {
                if (Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (Plant.Phenology.Emerged == true)
                            return DM.SinkLimitation;
                        else return 0;
                    }
                    else
                        return DM.SinkLimitation;
                }
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the nutrient limitation to growth</summary>
        /// <value>The amount of DM that was not assimilated because there was not enough nutrient to meet minimum concentrations</value>
        [XmlIgnore]
        public double DMNutrientLimitation
        {
            get
            {
                if (Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (Plant.Phenology.Emerged == true)
                            return DM.NutrientLimitation;
                        else return 0;
                    }
                    else
                        return DM.NutrientLimitation;
                }
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the n supply relative to N demand.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        public double FDM
        {
            get
            {
                if (Plant != null && Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (DM != null)
                        {
                            if ((Plant.Phenology.Emerged == true) && (DM.TotalPlantDemand > 0) && (DM.TotalPlantSupply > 0))
                                return DM.TotalPlantSupply / DM.TotalPlantDemand;
                            else return 1;
                        }
                        else return 1;
                    }
                    else
                        return DM.TotalPlantSupply / DM.TotalPlantDemand;
                }
                else
                    return 1;
            }
        }

        /// <summary>Gets the delta wt.</summary>
        /// <value>The delta wt.</value>
        public double DeltaWt
        {
            get
            {
                return DM.End - DM.Start;
            }
        }

        /// <summary>Gets the n demand.</summary>
        /// <value>The n demand.</value>
        [XmlIgnore]
        public double NDemand
        {
            get
            {
                if (Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (Plant.Phenology.Emerged == true)
                          return N.TotalPlantDemand;
                        else return 0;
                    }
                    else
                        return N.TotalPlantDemand;
                }
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the n supply.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        public double NSupply
        {
            get
            {
                if (Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (Plant.Phenology.Emerged == true)
                            return N.TotalPlantSupply;
                        else return 0;
                    }
                    else
                        return N.TotalPlantDemand;
                }
                else
                    return 0.0;
            }
        }
        
        /// <summary>Gets the N allocations</summary>
        /// <value>Allocation of N to each organ</value>
        [XmlIgnore]
        public double NAllocated
        {
            get
            {
                if (Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (Plant.Phenology.Emerged == true)
                            return N.Allocated;
                        else return 0;
                    }
                    else
                        return N.Allocated;
                }
                else
                    return 0.0;
            }
        }
        
        /// <summary>Gets the n supply relative to N demand.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        public double FN
        {
            get
            {
                if (Plant != null && Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (N != null)
                        {
                            if ((Plant.Phenology.Emerged == true) && (N.TotalPlantDemand > 0) && (N.TotalPlantSupply > 0))
                                return N.TotalPlantSupply / N.TotalPlantDemand;
                            else return 1;
                        }
                        else return 1;
                    }
                    else
                        return N.TotalPlantSupply / N.TotalPlantDemand;
                }
                else
                    return 1;
            }
        }

        /// TODO: the water outputs calculation need to be upgraded

        /// <summary>the water supply</summary>
        private double waterSupply = 0.0;
        /// <summary>the water demand</summary>
        private double waterDemand = 0.0;
        /// <summary>the water uptake</summary>
        private double waterUptake = 0.0;

        /// <summary>Gets the water supply.</summary>
        /// <value>The water supply.</value>
        [XmlIgnore]
        public double WSupply
        {
            get
            {
                if (Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (Plant.Phenology.Emerged == true)
                            return waterSupply;
                        else return 0.0;
                    }
                    else
                        return waterSupply;
                }
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        [XmlIgnore]
        public double WDemand
        {
            get
            {
                if (Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (Plant.Phenology.Emerged == true)
                            return waterDemand;
                        else return 0.0;
                    }
                    else
                        return waterDemand;
                }
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the water allocated in the plant (taken up).</summary>
        /// <value>The water uptake.</value>
        [XmlIgnore]
        public double WAllocated
        {
            get
            {
                if (Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (Plant.Phenology.Emerged == true)
                            return waterUptake;
                        else return 0.0;
                    }
                    else
                        return waterUptake;
                }
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the n supply relative to N demand.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        public double FW
        {
            get
            {
                if (Plant != null && Plant.IsAlive)
                {
                    if (Plant.Phenology != null)
                    {
                        if (N != null)
                        {
                            if ((Plant.Phenology.Emerged == true) && (waterDemand > 0) && (waterSupply > 0))
                                return waterSupply / waterDemand;
                            else return 1;
                        }
                        else return 1;
                    }
                    else
                        return waterSupply / waterDemand;
                }
                else
                    return 1;
            }
        }


        /// <summary>Gets and Sets NO3 Supply</summary>
        /// <value>NO3 supplies from each soil layer</value>
        [XmlIgnore]
        public double[] PotentialNO3NUptake { get; set; }

        /// <summary>Gets and Sets NH4 Supply</summary>
        /// <value>NH4 supplies from each soil layer</value>
         [XmlIgnore]
        public double[] PotentialNH4NUptake { get; set; }

        //FixME Currently all models are N aware but none are P or K aware.  More programming is needed to make this work! 
        /// <summary>The n aware</summary>
        [XmlIgnore]
        public bool NAware = true;
        
        /// <summary>The p aware</summary>
        [XmlIgnore]
        public bool PAware = false;
        
        /// <summary>The k aware</summary>
        [XmlIgnore]
        public bool KAware = false;

        #endregion

        #region IUptake interface

        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        public List<ZoneWaterAndN> GetSWUptakes(SoilState soilstate)
        {
            if (Plant.IsAlive)
            {
                // Model can only handle one root zone at present
                ZoneWaterAndN MyZone = new ZoneWaterAndN();
                Zone ParentZone = Apsim.Parent(this, typeof(Zone)) as Zone;
                foreach (ZoneWaterAndN Z in soilstate.Zones)
                    if (Z.Name == ParentZone.Name)
                        MyZone = Z;

                double Supply = 0;
                double Demand = 0;
                double[] supply = null;
                foreach (IArbitration o in Organs)
                {
                    double[] organSupply = o.WaterSupply(soilstate.Zones);
                    if (organSupply != null)
                    {
                        supply = organSupply;
                        Supply += MathUtilities.Sum(organSupply);
                    }
                    Demand += o.WaterDemand;
                }

                double FractionUsed = 0;
                if (Supply > 0)
                    FractionUsed = Math.Min(1.0, Demand / Supply);

                // Just send uptake from my zone
                ZoneWaterAndN uptake = new ZoneWaterAndN();
                uptake.Name = MyZone.Name;
                uptake.Water = MathUtilities.Multiply_Value(supply, FractionUsed);
                uptake.NO3N = new double[uptake.Water.Length];
                uptake.NH4N = new double[uptake.Water.Length];

                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                zones.Add(uptake);
                return zones;
            }
            else
                return null;
        }
        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public void SetSWUptake(List<ZoneWaterAndN> zones)
        {
            // Model can only handle one root zone at present
            ZoneWaterAndN MyZone = new ZoneWaterAndN();
            Zone ParentZone = Apsim.Parent(this, typeof(Zone)) as Zone;
            foreach (ZoneWaterAndN Z in zones)
                if (Z.Name == ParentZone.Name)
                    MyZone = Z;

            double[] uptake = MyZone.Water;
            waterSupply = MathUtilities.Sum(uptake);
            waterDemand = 0.0;
            foreach (IArbitration o in Organs)
                waterDemand += o.WaterDemand;

            double fraction = 1;
            if (waterDemand > 0)
                fraction = Math.Min(1.0, waterSupply / waterDemand);

            waterUptake = 0.0;
            foreach (IArbitration o in Organs)
                if (o.WaterDemand > 0)
                {
                    o.WaterAllocation = fraction * o.WaterDemand;
                    waterUptake += o.WaterAllocation;
                }

            Plant.Root.DoWaterUptake(uptake);
        }

        /// <summary>
        /// Calculate the potential sw uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        public List<Soils.Arbitrator.ZoneWaterAndN> GetNUptakes(SoilState soilstate)
        {
            if (Plant.IsAlive)
            {
                // Model can only handle one root zone at present
                ZoneWaterAndN MyZone = new ZoneWaterAndN();
                Zone ParentZone = Apsim.Parent(this, typeof(Zone)) as Zone;
                foreach (ZoneWaterAndN Z in soilstate.Zones)
                    if (Z.Name == ParentZone.Name)
                        MyZone = Z;

                ZoneWaterAndN UptakeDemands = new ZoneWaterAndN();
                if (Plant.Phenology != null)
                {
                    if (Plant.Phenology.Emerged == true)
                    {
                        DoNUptakeDemandCalculations(soilstate);

                        //Pack results into uptake structure
                        UptakeDemands.NO3N = PotentialNO3NUptake;
                        UptakeDemands.NH4N = PotentialNH4NUptake;
                    }
                    else //Uptakes are zero
                    {
                        UptakeDemands.NO3N = new double[MyZone.NO3N.Length];
                        for (int i = 0; i < UptakeDemands.NO3N.Length; i++) { UptakeDemands.NO3N[i] = 0; }
                        UptakeDemands.NH4N = new double[MyZone.NH4N.Length];
                        for (int i = 0; i < UptakeDemands.NH4N.Length; i++) { UptakeDemands.NH4N[i] = 0; }
                    }
                }
                else 
                {
                    DoNUptakeDemandCalculations(soilstate);

                    //Pack results into uptake structure
                    UptakeDemands.NO3N = PotentialNO3NUptake;
                    UptakeDemands.NH4N = PotentialNH4NUptake;
                }

                UptakeDemands.Name = MyZone.Name;
                UptakeDemands.Water = new double[UptakeDemands.NO3N.Length];

                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                zones.Add(UptakeDemands);
                return zones;
            }
            else
                return null;
        }
        
        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public void SetNUptake(List<ZoneWaterAndN> zones)
        {
            if (Plant.IsAlive)
            {
                // Model can only handle one root zone at present
                ZoneWaterAndN MyZone = new ZoneWaterAndN();
                Zone ParentZone = Apsim.Parent(this, typeof(Zone)) as Zone;
                foreach (ZoneWaterAndN Z in zones)
                    if (Z.Name == ParentZone.Name)
                        MyZone = Z;

                if (Plant.Phenology != null)
                {
                    if (Plant.Phenology.Emerged == true)
                    {
                        double[] AllocatedNO3Nuptake = MyZone.NO3N;
                        double[] AllocatedNH4Nuptake = MyZone.NH4N;
                        DoNUptakeAllocations(AllocatedNO3Nuptake, AllocatedNH4Nuptake); //Fixme, needs to send allocations to arbitrator
                        Plant.Root.DoNitrogenUptake(AllocatedNO3Nuptake, AllocatedNH4Nuptake);
                    }
                }
                else
                {
                    double[] AllocatedNO3Nuptake = MyZone.NO3N;
                    double[] AllocatedNH4Nuptake = MyZone.NH4N;
                    DoNUptakeAllocations(AllocatedNO3Nuptake, AllocatedNH4Nuptake); //Fixme, needs to send allocations to arbitrator
                    Plant.Root.DoNitrogenUptake(AllocatedNO3Nuptake, AllocatedNH4Nuptake);
                }
            }
        }
        #endregion

        #region Plant interface methods

        /// <summary>Called when [initialize].</summary>
        [EventSubscribe("Initialised")]
        private void OnInit()
        {
            //NAware Array.Exists(NutrientDrivers, element => element == "Nitrogen");  Fixme  Need to put this into .xml and write code to handle N unaware crops
            PAware = Array.Exists(NutrientDrivers, element => element == "Phosphorus");
            KAware = Array.Exists(NutrientDrivers, element => element == "Potasium");
        }

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
            PotentialNH4NUptake = new double[Soil.Thickness.Length];
            PotentialNO3NUptake = new double[Soil.Thickness.Length];
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
            {
                List<IArbitration> organsToArbitrate = new List<IArbitration>();

                foreach (IOrgan organ in Plant.Organs)
                    if (organ is IArbitration)
                        organsToArbitrate.Add(organ as IArbitration);

                Organs = organsToArbitrate.ToArray();
            }
        }

        /// <summary>Does the water limited dm allocations.  Water constaints to growth are accounted for in the calculation of DM supply
        /// and does initial N calculations to work out how much N uptake is required to pass to SoilArbitrator</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantPartioning")]
        private void OnDoPotentialPlantPartioning(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                DoDMSetup(Organs);                                       //Get DM demands and supplies (with water stress effects included) from each organ
                DoReAllocation(Organs, DM, DMArbitrationOption);         //Allocate supply of reallocated DM to organs
                DoFixation(Organs, DM, DMArbitrationOption);             //Allocate supply of fixed DM (photosynthesis) to organs
                DoRetranslocation(Organs, DM, DMArbitrationOption);      //Allocate supply of retranslocated DM to organs
                SendPotentialDMAllocations(Organs);                      //Tell each organ what their potential growth is so organs can calculate their N demands
                DoNutrientSetUp(Organs, ref N);                          //Get N demands and supplies (excluding uptake supplys) from each organ
                DoReAllocation(Organs, N, NArbitrationOption);           //Allocate N available from reallocation to each organ
            }
        }
        
        /// <summary>Calculates how much N the roots will take up in the absence of competition</summary>
        /// <param name="soilstate">The state of the soil.</param>
        public void DoNUptakeDemandCalculations(SoilState soilstate)
        {
            DoPotentialNutrientUptake(Organs, ref N, soilstate);  //Work out how much N the uptaking organs (roots) would take up in the absence of competition
        }

        /// <summary>Allocates the NUptake that the soil arbitrator has returned</summary>
        /// <param name="AllocatedNO3Nuptake">AllocatedNO3Nuptake</param>
        /// <param name="AllocatedNH4Nuptake">AllocatedNH4Nuptake</param>
        public void DoNUptakeAllocations(double[] AllocatedNO3Nuptake, double[] AllocatedNH4Nuptake)  //Fixme Needs to take N allocation from soil arbitrator
        {
            //Calculation the proportion of potential N uptake from each organ
            double[] proportion = new double[Organs.Length];
            for (int i = 0; i < Organs.Length; i++)
            {
                proportion[i] = N.UptakeSupply[i] / N.TotalUptakeSupply;
            }
            
            //Reset actual uptakes to each organ based on uptake allocated by soil arbitrator and the organs proportion of potential uptake
            for (int i = 0; i < Organs.Length; i++)
            {   //Allocation of n involves resetting UptakeSupply from the potential value calculated on DoNUptakeDemandCalculations to an actual value based on what soil arbitrator allocated
                N.UptakeSupply[i] = MathUtilities.Sum(AllocatedNO3Nuptake) * kgha2gsm * proportion[i];
                N.UptakeSupply[i] += MathUtilities.Sum(AllocatedNH4Nuptake) * kgha2gsm * proportion[i];
            }
            
            //Recalculate totals based on actual supply
            N.TotalUptakeSupply = MathUtilities.Sum(N.UptakeSupply);
            N.TotalPlantSupply = N.TotalReallocationSupply + N.TotalUptakeSupply + N.TotalFixationSupply + N.TotalRetranslocationSupply;
            
            //Allocate N that the SoilArbitrator has allocated the plant to each organ
            DoUptake(Organs, N, NArbitrationOption);                 
        }
        
        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantPartioning")]
        private void OnDoActualPlantPartioning(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                DoFixation(Organs, N, NArbitrationOption);               //Allocate supply of fixable Nitrogen to each organ
                DoRetranslocation(Organs, N, NArbitrationOption);        //Allocate supply of retranslocatable N to each organ
                DoNutrientConstrainedDMAllocation(Organs);               //Work out how much DM can be assimilated by each organ based on allocated nutrients
                SendDMAllocations(Organs);                               //Tell each organ how DM they are getting folling allocation
                SendNutrientAllocations(Organs);                         //Tell each organ how much nutrient they are getting following allocaition
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (sender == Plant)
                Clear();
        }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            DM = null;
            N = null;
        }

        #endregion

        #region Arbitration step functions

        /// <summary>Does the dm setup.</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void DoDMSetup(IArbitration[] Organs)
        {
            //Creat Drymatter variable class
            DM = new BiomassArbitrationType(Organs.Length);
            DM.BiomassType = "DM";

            // GET INITIAL STATE VARIABLES FOR MASS BALANCE CHECKS
            DM.Start = 0;
            // GET SUPPLIES AND CALCULATE TOTAL
            for (int i = 0; i < Organs.Length; i++)
            {
                BiomassSupplyType Supply = Organs[i].DMSupply;
                DM.ReallocationSupply[i] = Supply.Reallocation;
                DM.UptakeSupply[i] = Supply.Uptake;
                DM.FixationSupply[i] = Supply.Fixation;
                DM.RetranslocationSupply[i] = Supply.Retranslocation;
                DM.Start += Organs[i].Wt;
            }

            DM.TotalReallocationSupply = MathUtilities.Sum(DM.ReallocationSupply);
            DM.TotalUptakeSupply = MathUtilities.Sum(DM.UptakeSupply);
            DM.TotalFixationSupply = MathUtilities.Sum(DM.FixationSupply);
            DM.TotalRetranslocationSupply = MathUtilities.Sum(DM.RetranslocationSupply);
            DM.TotalPlantSupply = DM.TotalReallocationSupply + DM.TotalUptakeSupply + DM.TotalFixationSupply + DM.TotalRetranslocationSupply;
            
            // SET OTHER ORGAN VARIABLES AND CALCULATE TOTALS
            for (int i = 0; i < Organs.Length; i++)
            {
                BiomassPoolType Demand = Organs[i].DMDemand;
                DM.StructuralDemand[i] = Demand.Structural;
                DM.MetabolicDemand[i] = Demand.Metabolic;
                DM.NonStructuralDemand[i] = Demand.NonStructural;
                DM.TotalDemand[i] = DM.StructuralDemand[i] + DM.MetabolicDemand[i] + DM.NonStructuralDemand[i];

                DM.Reallocation[i] = 0;
                DM.Uptake[i] = 0;
                DM.Fixation[i] = 0;
                DM.Retranslocation[i] = 0;
                DM.StructuralAllocation[i] = 0;
                DM.MetabolicAllocation[i] = 0;
                DM.NonStructuralAllocation[i] = 0;
            }

            DM.TotalStructuralDemand = MathUtilities.Sum(DM.StructuralDemand);
            DM.TotalMetabolicDemand = MathUtilities.Sum(DM.MetabolicDemand);
            DM.TotalNonStructuralDemand = MathUtilities.Sum(DM.NonStructuralDemand);
            DM.TotalPlantDemand = DM.TotalStructuralDemand + DM.TotalMetabolicDemand + DM.TotalNonStructuralDemand;

            DM.TotalStructuralAllocation = 0;
            DM.TotalMetabolicAllocation = 0;
            DM.TotalNonStructuralAllocation = 0;
            DM.Allocated = 0;
            DM.SinkLimitation = 0;
            DM.NutrientLimitation = 0;

            //Set relative DM demands of each organ
            for (int i = 0; i < Organs.Length; i++)
            {
                if (DM.TotalStructuralDemand > 0)
                    DM.RelativeStructuralDemand[i] = DM.StructuralDemand[i] / DM.TotalStructuralDemand;
                if (DM.TotalMetabolicDemand > 0)
                    DM.RelativeMetabolicDemand[i] = DM.MetabolicDemand[i] / DM.TotalMetabolicDemand;
                if (DM.TotalNonStructuralDemand > 0)
                    DM.RelativeNonStructuralDemand[i] = DM.NonStructuralDemand[i] / DM.TotalNonStructuralDemand;
            }
        }

        /// <summary>Sends the potential dm allocations.</summary>
        /// <param name="Organs">The organs.</param>
        /// <exception cref="System.Exception">Mass Balance Error in Photosynthesis DM Allocation</exception>
        virtual public void SendPotentialDMAllocations(IArbitration[] Organs)
        {
            //  Allocate to meet Organs demands
            DM.TotalStructuralAllocation = MathUtilities.Sum(DM.StructuralAllocation);
            DM.TotalMetabolicAllocation = MathUtilities.Sum(DM.MetabolicAllocation);
            DM.TotalNonStructuralAllocation = MathUtilities.Sum(DM.NonStructuralAllocation);
            DM.Allocated = DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation + DM.TotalNonStructuralAllocation;
            
            // Then check it all adds up
            if (Math.Round(DM.Allocated,8) > Math.Round(DM.TotalPlantSupply,8)) 
                throw new Exception("Potential DM allocation by " + this.Name + " exceeds DM supply.   Thats not really possible so something has gone a miss");
            if (Math.Round(DM.Allocated,8) > Math.Round(DM.TotalPlantDemand,8))
                throw new Exception("Potential DM allocation by " + this.Name + " exceeds DM Demand.   Thats not really possible so something has gone a miss");

            // Send potential DM allocation to organs to set this variable for calculating N demand
            for (int i = 0; i < Organs.Length; i++)
            {
                Organs[i].DMPotentialAllocation = new BiomassPoolType
                {
                    Structural = DM.StructuralAllocation[i],  //Need to seperate metabolic and structural allocations
                    Metabolic = DM.MetabolicAllocation[i],  //This wont do anything currently
                    NonStructural = DM.NonStructuralAllocation[i], //Nor will this do anything
                };
            }
        }
        
        /// <summary>Does the nutrient set up.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="N">The bat.</param>
        virtual public void DoNutrientSetUp(IArbitration[] Organs, ref BiomassArbitrationType N)
        {
            //Creat Biomass variable class
            N = new BiomassArbitrationType(Organs.Length);
            N.BiomassType = "N";

            // GET ALL INITIAL STATE VARIABLES FOR MASS BALANCE CHECKS
            N.Start = 0;

            // GET ALL SUPPLIES AND DEMANDS AND CALCULATE TOTALS
            for (int i = 0; i < Organs.Length; i++)
            {
                BiomassSupplyType Supply = Organs[i].NSupply;
                N.ReallocationSupply[i] = Supply.Reallocation;
                //N.UptakeSupply[i] = Supply.Uptake;             This is done on DoNutrientUptakeCalculations
                N.FixationSupply[i] = Supply.Fixation;
                N.RetranslocationSupply[i] = Supply.Retranslocation;
                N.Start += Organs[i].N;
            }

            N.TotalReallocationSupply = MathUtilities.Sum(N.ReallocationSupply);
            //N.TotalUptakeSupply = MathUtilities.Sum(N.UptakeSupply);       This is done on DoNutrientUptakeCalculations
            N.TotalFixationSupply = MathUtilities.Sum(N.FixationSupply);
            N.TotalRetranslocationSupply = MathUtilities.Sum(N.RetranslocationSupply);
            N.TotalPlantSupply = N.TotalReallocationSupply + N.TotalUptakeSupply + N.TotalFixationSupply + N.TotalRetranslocationSupply;
            
            for (int i = 0; i < Organs.Length; i++)
            {
                BiomassPoolType Demand = Organs[i].NDemand;
                N.StructuralDemand[i] = Organs[i].NDemand.Structural;
                N.MetabolicDemand[i] = Organs[i].NDemand.Metabolic;
                N.NonStructuralDemand[i] = Organs[i].NDemand.NonStructural;
                N.TotalDemand[i] = N.StructuralDemand[i] + N.MetabolicDemand[i] + N.NonStructuralDemand[i];

                N.Reallocation[i] = 0;
                N.Uptake[i] = 0;
                N.Fixation[i] = 0;
                N.Retranslocation[i] = 0;
                N.StructuralAllocation[i] = 0;
                N.MetabolicAllocation[i] = 0;
                N.NonStructuralAllocation[i] = 0;
            }

            N.TotalStructuralDemand = MathUtilities.Sum(N.StructuralDemand);
            N.TotalMetabolicDemand = MathUtilities.Sum(N.MetabolicDemand);
            N.TotalNonStructuralDemand = MathUtilities.Sum(N.NonStructuralDemand);
            N.TotalPlantDemand = N.TotalStructuralDemand + N.TotalMetabolicDemand + N.TotalNonStructuralDemand;

            N.TotalStructuralAllocation = 0;
            N.TotalMetabolicAllocation = 0;
            N.TotalNonStructuralAllocation = 0;

            //Set relative N demands of each organ
            for (int i = 0; i < Organs.Length; i++)
            {
                if (N.TotalStructuralDemand > 0)
                    N.RelativeStructuralDemand[i] = N.StructuralDemand[i] / N.TotalStructuralDemand;
                if (N.TotalMetabolicDemand > 0)
                    N.RelativeMetabolicDemand[i] = N.MetabolicDemand[i] / N.TotalMetabolicDemand;
                if (N.TotalNonStructuralDemand > 0)
                    N.RelativeNonStructuralDemand[i] = N.NonStructuralDemand[i] / N.TotalNonStructuralDemand;
            }
        }
     
        /// <summary>Does the re allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="Option">The option.</param>
        virtual public void DoReAllocation(IArbitration[] Organs, BiomassArbitrationType BAT, ArbitrationType Option)
        {
            double BiomassReallocated = 0;
            if (BAT.TotalReallocationSupply > 0.00000000001)
            {
                //Calculate how much reallocated N (and associated biomass) each demanding organ is allocated based on relative demands
                if (Option == ArbitrationType.RelativeAllocation)
                    RelativeAllocation(Organs, BAT.TotalReallocationSupply, ref BiomassReallocated, BAT);
                if (Option == ArbitrationType.PriorityAllocation)
                    PriorityAllocation(Organs, BAT.TotalReallocationSupply, ref BiomassReallocated, BAT);
                if (Option == ArbitrationType.PriorityThenRelativeAllocation)
                    PrioritythenRelativeAllocation(Organs, BAT.TotalReallocationSupply, ref BiomassReallocated, BAT);
                if (Option == ArbitrationType.RelativeAllocationSinglePass)
                    RelativeAllocationSinglePass(Organs, BAT.TotalReallocationSupply, ref BiomassReallocated, BAT);

                //Then calculate how much biomass is realloced from each supplying organ based on relative reallocation supply
                for (int i = 0; i < Organs.Length; i++)
                {
                    if (BAT.ReallocationSupply[i] > 0)
                    {
                        double RelativeSupply = BAT.ReallocationSupply[i] / BAT.TotalReallocationSupply;
                        BAT.Reallocation[i] += BiomassReallocated * RelativeSupply;
                    }
                }
                BAT.TotalReallocation = MathUtilities.Sum(BAT.Reallocation);
            }
        }
       
        /// <summary>Does the uptake.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="soilstate">The soilstate.</param>
        virtual public void DoPotentialNutrientUptake(IArbitration[] Organs, ref BiomassArbitrationType BAT, SoilState soilstate)
        {
            // Model can only handle one root zone at present
            ZoneWaterAndN MyZone = new ZoneWaterAndN();
            Zone ParentZone = Apsim.Parent(this, typeof(Zone)) as Zone;
            foreach (ZoneWaterAndN Z in soilstate.Zones)
                if (Z.Name == ParentZone.Name)
                    MyZone = Z;

            PotentialNO3NUptake = new double[MyZone.NO3N.Length];
            PotentialNH4NUptake = new double[MyZone.NH4N.Length];

            //Get Nuptake supply from each organ and set the PotentialUptake parameters that are passed to the soil arbitrator
            for (int i = 0; i < Organs.Length; i++)
            {
                double[] organNO3Supply = Organs[i].NO3NSupply(soilstate.Zones);
                if (organNO3Supply != null)
                {
                    PotentialNO3NUptake = MathUtilities.Add(PotentialNO3NUptake, organNO3Supply); //Add uptake supply from each organ to the plants total to tell the Soil arbitrator
                    BAT.UptakeSupply[i] = MathUtilities.Sum(organNO3Supply) * kgha2gsm;    //Populate uptakeSupply for each organ for internal allocation routines
                }

                double[] organNH4Supply = Organs[i].NH4NSupply(soilstate.Zones);
                if (organNH4Supply != null)
                {
                    PotentialNH4NUptake = MathUtilities.Add(PotentialNH4NUptake, organNH4Supply);
                    BAT.UptakeSupply[i] += MathUtilities.Sum(organNH4Supply) * kgha2gsm;
                }
            }
            
            //Calculate plant level supply totals.
            BAT.TotalUptakeSupply = MathUtilities.Sum(BAT.UptakeSupply);
            BAT.TotalPlantSupply = BAT.TotalReallocationSupply + BAT.TotalUptakeSupply + BAT.TotalFixationSupply + BAT.TotalRetranslocationSupply;

            //If NUsupply is greater than uptake (total demand - reallocatio nsupply) reduce the PotentialUptakes that we pass to the soil arbitrator
            if (BAT.TotalUptakeSupply > (BAT.TotalPlantDemand - BAT.TotalReallocation))
            {
                double ratio = Math.Min(1.0, (BAT.TotalPlantDemand - BAT.TotalReallocation) / BAT.TotalUptakeSupply);
                PotentialNO3NUptake = MathUtilities.Multiply_Value(PotentialNO3NUptake, ratio);
                PotentialNH4NUptake = MathUtilities.Multiply_Value(PotentialNH4NUptake, ratio);
            }

        }
        
        /// <summary>Does the uptake.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="Option">The option.</param>
        virtual public void DoUptake(IArbitration[] Organs, BiomassArbitrationType BAT,  ArbitrationType Option)
        {
            double BiomassTakenUp = 0;
            if (BAT.TotalUptakeSupply > 0.00000000001)
            {
                // Calculate how much uptake N each demanding organ is allocated based on relative demands
                if (Option == ArbitrationType.RelativeAllocation)
                    RelativeAllocation(Organs, BAT.TotalUptakeSupply, ref BiomassTakenUp, BAT);
                if (Option == ArbitrationType.PriorityAllocation)
                    PriorityAllocation(Organs, BAT.TotalUptakeSupply, ref BiomassTakenUp, BAT);
                if (Option == ArbitrationType.PriorityThenRelativeAllocation)
                    PrioritythenRelativeAllocation(Organs, BAT.TotalUptakeSupply, ref BiomassTakenUp, BAT);
                if (Option == ArbitrationType.RelativeAllocationSinglePass)
                    RelativeAllocationSinglePass(Organs, BAT.TotalUptakeSupply, ref BiomassTakenUp, BAT);

                // Then calculate how much N is taken up by each supplying organ based on relative uptake supply
                for (int i = 0; i < Organs.Length; i++)
                {
                    if (BAT.UptakeSupply[i] > 0.00000000001)
                    {
                        double RelativeSupply = BAT.UptakeSupply[i] / BAT.TotalUptakeSupply;
                        BAT.Uptake[i] += BiomassTakenUp * RelativeSupply;
                    }
                }
            }
        }
        
        /// <summary>Does the retranslocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="Option">The option.</param>
        virtual public void DoRetranslocation(IArbitration[] Organs, BiomassArbitrationType BAT, ArbitrationType Option)
        {
            double BiomassRetranslocated = 0;
            if (BAT.TotalRetranslocationSupply > 0.00000000001)
            {
                // Calculate how much retranslocation N (and associated biomass) each demanding organ is allocated based on relative demands
                if (Option == ArbitrationType.RelativeAllocation)
                    RelativeAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);
                if (Option == ArbitrationType.PriorityAllocation)
                    PriorityAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);
                if (Option == ArbitrationType.PriorityThenRelativeAllocation)
                    PrioritythenRelativeAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);
                if (Option == ArbitrationType.RelativeAllocationSinglePass)
                    RelativeAllocationSinglePass(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);

                // Then calculate how much N (and associated biomass) is retranslocated from each supplying organ based on relative retranslocation supply
                for (int i = 0; i < Organs.Length; i++)
                {
                    if (BAT.RetranslocationSupply[i] > 0.00000000001)
                    {
                        double RelativeSupply = BAT.RetranslocationSupply[i] / BAT.TotalRetranslocationSupply;
                        BAT.Retranslocation[i] += BiomassRetranslocated * RelativeSupply;
                    }
                }
            }
        }
        
        /// <summary>Does the fixation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="Option">The option.</param>
        /// <exception cref="System.Exception">Crop is trying to Fix excessive amounts of BAT.  Check partitioning coefficients are giving realistic nodule size and that FixationRatePotential is realistic</exception>
        virtual public void DoFixation(IArbitration[] Organs, BiomassArbitrationType BAT, ArbitrationType Option)
        {
            double BiomassFixed = 0;
            if (BAT.TotalFixationSupply > 0.00000000001)
            {
                // Calculate how much fixed resource each demanding organ is allocated based on relative demands
                if (Option == ArbitrationType.RelativeAllocation)
                    RelativeAllocation(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);
                if (Option == ArbitrationType.PriorityAllocation)
                    PriorityAllocation(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);
                if (Option == ArbitrationType.PriorityThenRelativeAllocation)
                    PrioritythenRelativeAllocation(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);
                if (Option == ArbitrationType.RelativeAllocationSinglePass)
                    RelativeAllocationSinglePass(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);

                //Set the sink limitation variable.  BAT.NotAllocated changes after each allocation step so it must be caught here and assigned as sink limitation
                BAT.SinkLimitation = BAT.NotAllocated;
                
                // Then calculate how much resource is fixed from each supplying organ based on relative fixation supply
                if (BiomassFixed > 0)
                {
                    for (int i = 0; i < Organs.Length; i++)
                    {
                        if (BAT.FixationSupply[i] > 0.00000000001)
                        {
                            double RelativeSupply = BAT.FixationSupply[i] / BAT.TotalFixationSupply;
                            BAT.Fixation[i] = BiomassFixed * RelativeSupply;
                            double Respiration = BiomassFixed * RelativeSupply * Organs[i].NFixationCost;  //Calculalte how much respirtion is associated with fixation
                            DM.Respiration[i] = Respiration; // allocate it to the organ
                        }
                        DM.TotalRespiration = MathUtilities.Sum(DM.Respiration);
                    }
                }

                // Work out the amount of biomass (if any) lost due to the cost of N fixation
                if (DM.TotalRespiration <= DM.SinkLimitation)
                { } //Cost of N fixation can be met by DM supply that was not allocated
                else
                {//claw back todays NonStructuralDM allocation to cover the cost
                    double UnallocatedRespirationCost = DM.TotalRespiration - DM.SinkLimitation;
                    if (DM.TotalNonStructuralAllocation > 0)
                    {
                        for (int i = 0; i < Organs.Length; i++)
                        {
                            double proportion = DM.NonStructuralAllocation[i] / DM.TotalNonStructuralAllocation;
                            double Clawback = Math.Min(UnallocatedRespirationCost * proportion, DM.NonStructuralAllocation[i]);
                            DM.NonStructuralAllocation[i] -= Clawback;
                            UnallocatedRespirationCost -= Clawback;
                        }
                    }
                    if (UnallocatedRespirationCost == 0)
                    { }//All cost accounted for
                    else
                    {//Remobilise more Non-structural DM to cover the cost
                        if (DM.TotalRetranslocationSupply > 0)
                        {
                            for (int i = 0; i < Organs.Length; i++)
                            {
                                double proportion = DM.RetranslocationSupply[i] / DM.TotalRetranslocationSupply;
                                double DMRetranslocated = Math.Min(UnallocatedRespirationCost * proportion, DM.RetranslocationSupply[i]);
                                DM.Retranslocation[i] += DMRetranslocated;
                                UnallocatedRespirationCost -= DMRetranslocated;
                            }
                        }
                        if (UnallocatedRespirationCost == 0)
                        { }//All cost accounted for
                        else
                        {//Start cutting into Structural and Metabolic Allocations
                            if ((DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation) > 0)
                            {
                                double Costmet = 0;
                                for (int i = 0; i < Organs.Length; i++)
                                {
                                    if ((DM.StructuralAllocation[i] + DM.MetabolicAllocation[i]) > 0)
                                    {
                                        double proportion = (DM.StructuralAllocation[i] + DM.MetabolicAllocation[i]) / (DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation);
                                        double StructualFraction = DM.StructuralAllocation[i] / (DM.StructuralAllocation[i] + DM.MetabolicAllocation[i]);
                                        double StructuralClawback = Math.Min(UnallocatedRespirationCost * proportion * StructualFraction, DM.StructuralAllocation[i]);
                                        double MetabolicClawback = Math.Min(UnallocatedRespirationCost * proportion * (1 - StructualFraction), DM.MetabolicAllocation[i]);
                                        DM.StructuralAllocation[i] -= StructuralClawback;
                                        DM.MetabolicAllocation[i] -= MetabolicClawback;
                                        Costmet += (StructuralClawback + MetabolicClawback);
                                    }
                                }
                                UnallocatedRespirationCost -= Costmet;
                            }
                        }
                        if (UnallocatedRespirationCost > 0.0000000001)
                            throw new Exception("Crop is trying to Fix excessive amounts of " + BAT.BiomassType +" Check partitioning coefficients are giving realistic nodule size and that FixationRatePotential is realistic");
                    }
                }
            }
        }
        
        /// <summary>Determines Nutrient limitations to DM allocations</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void DoNutrientConstrainedDMAllocation(IArbitration[] Organs)
        {
            double PreNStressDMAllocation = DM.Allocated;
            for (int i = 0; i < Organs.Length; i++)
                N.TotalAllocation[i] = N.StructuralAllocation[i] + N.MetabolicAllocation[i] + N.NonStructuralAllocation[i];

            N.Allocated = MathUtilities.Sum(N.TotalAllocation);

            //To introduce functionality for other nutrients we need to repeat this for loop for each new nutrient type
            // Calculate posible growth based on Minimum N requirement of organs
            for (int i = 0; i < Organs.Length; i++)
            {
                if (N.TotalAllocation[i] >= N.TotalDemand[i])
                    N.ConstrainedGrowth[i] = 100000000; //given high value so where there is no N deficit in organ and N limitation to growth  
                else
                    if (N.TotalAllocation[i] == 0)
                        N.ConstrainedGrowth[i] = 0;
                    else
                        N.ConstrainedGrowth[i] = N.TotalAllocation[i] / Organs[i].MinNconc;
            }

            // Reduce DM allocation below potential if insufficient N to reach Min n Conc or if DM was allocated to fixation
            for (int i = 0; i < Organs.Length; i++)
            {
                if ((DM.MetabolicAllocation[i] + DM.StructuralAllocation[i]) != 0)
                {
                    double MetabolicProportion = DM.MetabolicAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.NonStructuralAllocation[i]);
                    double StructuralProportion = DM.StructuralAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.NonStructuralAllocation[i]);
                    double NonStructuralProportion = DM.NonStructuralAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.NonStructuralAllocation[i]);
                    DM.MetabolicAllocation[i] = Math.Min(DM.MetabolicAllocation[i], N.ConstrainedGrowth[i] * MetabolicProportion);
                    DM.StructuralAllocation[i] = Math.Min(DM.StructuralAllocation[i], N.ConstrainedGrowth[i] * StructuralProportion);  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function
                    DM.NonStructuralAllocation[i] = Math.Min(DM.NonStructuralAllocation[i], N.ConstrainedGrowth[i] * NonStructuralProportion);  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function

                    //Question.  Why do I not restrain non-structural DM allocations.  I think this may be wrong and require further thought HEB 15-1-2015
                }
            }
            //Recalculated DM Allocation totals
            DM.TotalStructuralAllocation = MathUtilities.Sum(DM.StructuralAllocation);
            DM.TotalMetabolicAllocation = MathUtilities.Sum(DM.MetabolicAllocation);
            DM.TotalNonStructuralAllocation = MathUtilities.Sum(DM.NonStructuralAllocation);
            DM.Allocated = DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation + DM.TotalNonStructuralAllocation;
            DM.NutrientLimitation = (PreNStressDMAllocation - DM.Allocated);
        }
       
        /// <summary>Sends the dm allocations.</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void SendDMAllocations(IArbitration[] Organs)
        {
            // Send DM allocations to all Plant Organs
            for (int i = 0; i < Organs.Length; i++)
            {
                Organs[i].DMAllocation = new BiomassAllocationType
                {
                    Respired = DM.Respiration[i],
                    Reallocation = DM.Reallocation[i],
                    Retranslocation = DM.Retranslocation[i],
                    Structural = DM.StructuralAllocation[i],
                    NonStructural = DM.NonStructuralAllocation[i],
                    Metabolic = DM.MetabolicAllocation[i],
                };
            }
        }
        
        /// <summary>Sends the nutrient allocations.</summary>
        /// <param name="Organs">The organs.</param>
        /// <exception cref="System.Exception">
        /// -ve N Allocation
        /// or
        /// N Mass balance violated!!!!.  Daily Plant N increment is greater than N supply
        /// or
        /// N Mass balance violated!!!!  Daily Plant N increment is greater than N demand
        /// or
        /// DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than Photosynthetic DM supply
        /// or
        /// DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than the sum of structural DM demand, metabolic DM demand and NonStructural DM capacity
        /// </exception>
        virtual public void SendNutrientAllocations(IArbitration[] Organs)
        {
            // Send N allocations to all Plant Organs
            for (int i = 0; i < Organs.Length; i++)
            {
                if ((N.StructuralAllocation[i] < -0.00000001) || (N.MetabolicAllocation[i] < -0.00000001) || (N.NonStructuralAllocation[i] < -0.00000001))
                    throw new Exception("-ve N Allocation");
                if (N.StructuralAllocation[i] < 0.0)
                    N.StructuralAllocation[i] = 0.0;
                if (N.MetabolicAllocation[i] < 0.0)
                    N.MetabolicAllocation[i] = 0.0;
                if (N.NonStructuralAllocation[i] < 0.0)
                    N.NonStructuralAllocation[i] = 0.0;
                Organs[i].NAllocation = new BiomassAllocationType
                {
                    Structural = N.StructuralAllocation[i], //This needs to be seperated into components
                    Metabolic = N.MetabolicAllocation[i],
                    NonStructural = N.NonStructuralAllocation[i],
                    Fixation = N.Fixation[i],
                    Reallocation = N.Reallocation[i],
                    Retranslocation = N.Retranslocation[i],
                    Uptake = N.Uptake[i]
                };
            }

            //Finally Check Mass balance adds up
            N.End = 0;
            for (int i = 0; i < Organs.Length; i++)
                N.End += Organs[i].N;
            N.BalanceError = (N.End - (N.Start + N.TotalUptakeSupply + N.TotalFixationSupply));
            if (N.BalanceError > 0.000000001)
                throw new Exception("N Mass balance violated!!!!.  Daily Plant N increment is greater than N supply");
            N.BalanceError = (N.End - (N.Start + N.TotalPlantDemand));
            if (N.BalanceError > 0.000000001)
                throw new Exception("N Mass balance violated!!!!  Daily Plant N increment is greater than N demand");
            DM.End = 0;
            for (int i = 0; i < Organs.Length; i++)
                DM.End += Organs[i].Wt;
            DM.BalanceError = (DM.End - (DM.Start + DM.TotalFixationSupply));
            if (DM.BalanceError > 0.0001)
                throw new Exception("DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than Photosynthetic DM supply");
            DM.BalanceError = (DM.End - (DM.Start + DM.TotalStructuralDemand + DM.TotalMetabolicDemand + DM.TotalNonStructuralDemand));
            if (DM.BalanceError > 0.0001)
                throw new Exception("DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than the sum of structural DM demand, metabolic DM demand and NonStructural DM capacity");
        }
        
        #endregion

        #region Arbitrator generic allocation functions
        
        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        private void RelativeAllocation(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            ////allocate to structural and metabolic Biomass first
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
                double MetabolicRequirement = Math.Max(0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement) > 0.0)
                {
                    double StructuralFraction = BAT.TotalStructuralDemand / (BAT.TotalStructuralDemand + BAT.TotalMetabolicDemand);
                    double StructuralAllocation = Math.Min(StructuralRequirement, TotalSupply * StructuralFraction * BAT.RelativeStructuralDemand[i]);
                    double MetabolicAllocation = Math.Min(MetabolicRequirement, TotalSupply * (1 - StructuralFraction) * BAT.RelativeMetabolicDemand[i]);
                    BAT.StructuralAllocation[i] += StructuralAllocation;
                    BAT.MetabolicAllocation[i] += MetabolicAllocation;
                    NotAllocated -= (StructuralAllocation + MetabolicAllocation);
                    TotalAllocated += (StructuralAllocation + MetabolicAllocation);
                }
            }
            // Second time round if there is still Biomass to allocate let organs take N up to their Maximum
            double FirstPassNotAllocated = NotAllocated;
            for (int i = 0; i < Organs.Length; i++)
            {
                double NonStructuralRequirement = Math.Max(0.0, BAT.NonStructuralDemand[i] - BAT.NonStructuralAllocation[i]); //N needed to take organ up to maximum N concentration, Structural, Metabolic and Luxury N demands
                if (NonStructuralRequirement > 0.0)
                {
                    double NonStructuralAllocation = Math.Min(FirstPassNotAllocated * BAT.RelativeNonStructuralDemand[i], NonStructuralRequirement);
                    BAT.NonStructuralAllocation[i] += Math.Max(0,NonStructuralAllocation);
                    NotAllocated -= NonStructuralAllocation;
                    TotalAllocated += NonStructuralAllocation;
                }
            }
            //Set the amount of biomass not allocated.  Note, that this value is overwritten following by each arbitration step so if it is to be used correctly 
            //it must be caught in that step.  Currently only using to catch DM not allocated so we can report as sink limitaiton
            BAT.NotAllocated = NotAllocated;
        }
        
        /// <summary>Priorities the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        private void PriorityAllocation(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            ////First time round allocate to met priority demands of each organ
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
                double MetabolicRequirement = Math.Max(0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement) > 0.0)
                {
                    double StructuralFraction = BAT.StructuralDemand[i] / (BAT.StructuralDemand[i] + BAT.MetabolicDemand[i]);
                    double StructuralAllocation = Math.Min(StructuralRequirement, NotAllocated * StructuralFraction);
                    double MetabolicAllocation = Math.Min(MetabolicRequirement, NotAllocated * (1 - StructuralFraction));
                    BAT.StructuralAllocation[i] += StructuralAllocation;
                    BAT.MetabolicAllocation[i] += MetabolicAllocation;
                    NotAllocated -= (StructuralAllocation + MetabolicAllocation);
                    TotalAllocated += (StructuralAllocation + MetabolicAllocation);
                }
            }
            // Second time round if there is still N to allocate let organs take N up to their Maximum
            for (int i = 0; i < Organs.Length; i++)
            {
                double NonStructuralRequirement = Math.Max(0, BAT.NonStructuralDemand[i] - BAT.NonStructuralAllocation[i]);
                if (NonStructuralRequirement > 0.0)
                {
                    double NonStructuralAllocation = Math.Min(NonStructuralRequirement, NotAllocated);
                    BAT.NonStructuralAllocation[i] += Math.Max(0, NonStructuralAllocation);
                    NotAllocated -= NonStructuralAllocation;
                    TotalAllocated += NonStructuralAllocation;
                }
            }
        }
        
        /// <summary>Prioritythens the relative allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        private void PrioritythenRelativeAllocation(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            ////First time round allocate to met priority demands of each organ
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0.0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
                double MetabolicRequirement = Math.Max(0.0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement) > 0.0)
                {
                    double StructuralFraction = BAT.StructuralDemand[i] / (BAT.StructuralDemand[i] + BAT.MetabolicDemand[i]);
                    double StructuralAllocation = Math.Min(StructuralRequirement, NotAllocated * StructuralFraction);
                    double MetabolicAllocation = Math.Min(MetabolicRequirement, NotAllocated * (1 - StructuralFraction));
                    BAT.StructuralAllocation[i] += StructuralAllocation;
                    BAT.MetabolicAllocation[i] += MetabolicAllocation;
                    NotAllocated -= (StructuralAllocation + MetabolicAllocation);
                    TotalAllocated += (StructuralAllocation + MetabolicAllocation);
                }
            }
            // Second time round if there is still N to allocate let organs take N up to their Maximum
            double FirstPassNotallocated = NotAllocated;
            for (int i = 0; i < Organs.Length; i++)
            {
                double NonStructuralRequirement = Math.Max(0.0, BAT.NonStructuralDemand[i] - BAT.NonStructuralAllocation[i]); //N needed to take organ up to maximum N concentration, Structural, Metabolic and Luxury N demands
                if (NonStructuralRequirement > 0.0)
                {
                    double NonStructuralAllocation = Math.Min(FirstPassNotallocated * BAT.RelativeNonStructuralDemand[i], NonStructuralRequirement);
                    BAT.NonStructuralAllocation[i] += Math.Max(0, NonStructuralAllocation);
                    NotAllocated -= NonStructuralAllocation;
                    TotalAllocated += NonStructuralAllocation;
                }
            }
        }
        
        /// <summary>Partitions biomass between organs based on their relative demand in a single pass so non-structural always gets some if there is a non-structural demand</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        private void RelativeAllocationSinglePass(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            ////allocate to all pools based on their relative demands
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
                double MetabolicRequirement = Math.Max(0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                double NonStructuralRequirement = Math.Max(0, BAT.NonStructuralDemand[i] - BAT.NonStructuralAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement + NonStructuralRequirement) > 0.0)
                {
                    double StructuralFraction = BAT.TotalStructuralDemand / (BAT.TotalStructuralDemand + BAT.TotalMetabolicDemand + BAT.TotalNonStructuralDemand);
                    double MetabolicFraction = BAT.TotalMetabolicDemand / (BAT.TotalStructuralDemand + BAT.TotalMetabolicDemand + BAT.TotalNonStructuralDemand);
                    double NonStructuralFraction = BAT.TotalNonStructuralDemand / (BAT.TotalStructuralDemand + BAT.TotalMetabolicDemand + BAT.TotalNonStructuralDemand);

                    double StructuralAllocation = Math.Min(StructuralRequirement, TotalSupply * StructuralFraction * BAT.RelativeStructuralDemand[i]);
                    double MetabolicAllocation = Math.Min(MetabolicRequirement, TotalSupply * MetabolicFraction * BAT.RelativeMetabolicDemand[i]);
                    double NonStructuralAllocation = Math.Min(NonStructuralRequirement, TotalSupply * NonStructuralFraction * BAT.RelativeNonStructuralDemand[i]);

                    BAT.StructuralAllocation[i] += StructuralAllocation;
                    BAT.MetabolicAllocation[i] += MetabolicAllocation;
                    BAT.NonStructuralAllocation[i] += Math.Max(0, NonStructuralAllocation);
                    NotAllocated -= (StructuralAllocation + MetabolicAllocation + NonStructuralAllocation);
                    TotalAllocated += (StructuralAllocation + MetabolicAllocation + NonStructuralAllocation);
                }
            }
        }

        #endregion

        /// <summary>Ors the specified p.</summary>
        /// <param name="p">if set to <c>true</c> [p].</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void Or(bool p)
        {
            throw new NotImplementedException();
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            // write description of this class.
            AutoDocumentation.GetClassDescription(this, tags, indent);

            string RelativeDocString = "Arbitration is performed in two passes for each of the biomass supply sources.  On the first pass, structural and metabolic biomass is allocated to each organ based on their demand relative to the demand from all organs.  On the second pass any remaining biomass is allocated to non-structural demands based on the organ's relative demand.";
            string RelativeThenPriorityDocStirng = "Arbitration is performed in two passes for each of the biomass supply sources.  On the first pass, structural and metabolic biomass is allocated to each organ based on their order of priority with higher priority organs recieving their full demand first. On the second pass any remaining biomass is allocated to non-structural demands based on the relative demand from all organs.";
            string PriorityDocString = "Arbitration is performed in two passes for each of the biomass supply sources.  On the first pass, structural and metabolic biomass is allocated to each organ based on their order of priority with higher priority organs recieving their full demand first.  On the second pass any remaining biomass is allocated to non-structural demands based on the same order of priority.";
            string SinglePassDocString = "Arbitration is performed in a single pass for each of the biomass supply sources.  Biomass is partitioned between organs based on their relative demand in a single pass so non-structural demands compete dirrectly with structural demands.";
            if (NArbitrationOption == ArbitrationType.RelativeAllocation)
                tags.Add(new AutoDocumentation.Paragraph(RelativeDocString, indent));
            if (NArbitrationOption == ArbitrationType.PriorityAllocation)
                tags.Add(new AutoDocumentation.Paragraph(PriorityDocString, indent));
            if (NArbitrationOption == ArbitrationType.PriorityThenRelativeAllocation)
                tags.Add(new AutoDocumentation.Paragraph(RelativeThenPriorityDocStirng, indent));
            if (NArbitrationOption == ArbitrationType.RelativeAllocationSinglePass)
                tags.Add(new AutoDocumentation.Paragraph(SinglePassDocString, indent));
        }
    }
}