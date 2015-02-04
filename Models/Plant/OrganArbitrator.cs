using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;
using System.Xml.Serialization;

namespace Models.PMF
{
    /// <summary>
    /// This module takes Supplies and Demands of DM and N from each organ in the plant.  Firstly it gets DM Demands form organs
    /// and does a potential DM allocation based on these.  Then it gets N demands and allocates these to organs.  Finally it works
    /// out if N allocations were sufficient to meet minimum N concentratins of the organs and constrain DM allocations to maintain 
    /// minimum N concentrations if N is not sufficient
    /// </summary>
    [Serializable]
    public class OrganArbitrator : Model
    {
        #region Class Members
        // Input paramaters

        /// <summary>The top level plant object in the Plant Modelling Framework</summary>
        [Link]
        public Plant Plant = null;

        /// <summary>APSIMs clock model</summary>
        [Link]
        public Clock Clock = null;

        /// <summary>The method used to arbitrate N allocations</summary>
        [Description("Select method used for Arbitration")]
        public string NArbitrationOption = "";
        /// <summary>The mentod used to arbitrate DM allocations </summary>
        [Description("Select method used for DMArbitration")]
        public string DMArbitrationOption = "";
        /// <summary>The nutrient drivers</summary>
        [Description("List of nutrients that the arbitrator will consider")]
        public string[] NutrientDrivers = null;

        /// <summary>Called when [initialize].</summary>
        [EventSubscribe("Initialised")]
        private void OnInit()
        {
            //NAware Array.Exists(NutrientDrivers, element => element == "Nitrogen");  Fixme  Need to put this into .xml and write code to handle N unaware crops
            PAware = Array.Exists(NutrientDrivers, element => element == "Phosphorus");
            KAware = Array.Exists(NutrientDrivers, element => element == "Potasium");
        }

        /// <summary>Ors the specified p.</summary>
        /// <param name="p">if set to <c>true</c> [p].</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void Or(bool p)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
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


        /// <summary>The dm</summary>
        private BiomassArbitrationType DM = null;
        /// <summary>The n</summary>
        private BiomassArbitrationType N = null;
        //private BiomassArbitrationType P = null;
        //private BiomassArbitrationType K = null;


        /// <summary>Gets the dm supply.</summary>
        /// <value>Supply of DM from photosynthesising organs</value>
        [XmlIgnore]
        public double DMSupply
        {
            get
            {
                if (Plant.PlantInGround)
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
                if (Plant.PlantInGround)
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
                if (Plant.PlantInGround)
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
                if (Plant.PlantInGround)
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
                if (Plant.PlantInGround)
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

        /// <summary>Gets the n demand.</summary>
        /// <value>The n demand.</value>
        [XmlIgnore]
        public double NDemand
        {
            get
            {
                if (Plant.PlantInGround)
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

        /// <summary>Gets the delta wt.</summary>
        /// <value>The delta wt.</value>
        public double DeltaWt
        {
            get
            {
                return DM.End - DM.Start;
            }
        }

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

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            DM = null;
            N = null;
        }

        #region Interface methods called by Plant.cs
        /// <summary>Does the water limited dm allocations.  Water constaints to growth are accounted for in the calculation of DM supply</summary>
        /// <param name="Organs">The organs.</param>
        public void DoWaterLimitedDMAllocations(Organ[] Organs)
        {
            //Work out how much each organ will grow in the absence of nutrient stress, and how much DM they can supply.
            DoDMSetup(Organs);
            //Set potential growth of each organ, assuming adequate nutrient supply.
            DoReAllocation(Organs, DM, DMArbitrationOption);
            DoFixation(Organs, DM, DMArbitrationOption);
            DoRetranslocation(Organs, DM, DMArbitrationOption);
            SendPotentialDMAllocations(Organs);
        }
        /// <summary>Does the nutrient demand set up.</summary>
        /// <param name="Organs">The organs.</param>
        public void DoNutrientDemandSetUp(Organ[] Organs)
        {
                DoNutrientSetUp(Organs, ref N);
                DoReAllocation(Organs, N, NArbitrationOption);
        }
        /// <summary>Sets the nutrient uptake.</summary>
        /// <param name="Organs">The organs.</param>
        public void SetNutrientUptake(Organ[] Organs)
        {
                DoNutrientUptakeSetUp(Organs, ref N);
        }
        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="Organs">The organs.</param>
        public void DoNutrientAllocations(Organ[] Organs)
        {
                DoUptake(Organs, N, NArbitrationOption);
                DoRetranslocation(Organs, N, NArbitrationOption);
                DoFixation(Organs, N, NArbitrationOption);
        }
        /// <summary>Does the nutrient limited growth.</summary>
        /// <param name="Organs">The organs.</param>
        public void DoNutrientLimitedGrowth(Organ[] Organs)
        {
            //Work out how much DM can be assimilated by each organ based on the most limiting nutrient
            DoNutrientConstrainedDMAllocation(Organs);
            //Tell each organ how DM they are getting folling allocation
            SendDMAllocations(Organs);
            //Tell each organ how much nutrient they are getting following allocaition
            SendNutrientAllocations(Organs);
       }
        #endregion

        #region Arbitration step functions
        /// <summary>Does the dm setup.</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void DoDMSetup(Organ[] Organs)
        {
            //Creat Drymatter variable class
            DM = new BiomassArbitrationType(Organs.Length);

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
            DM.Start += Organs[i].Live.Wt + Organs[i].Dead.Wt;
            }

            DM.TotalReallocationSupply = Utility.Math.Sum(DM.ReallocationSupply);
            DM.TotalUptakeSupply = Utility.Math.Sum(DM.UptakeSupply);
            DM.TotalFixationSupply = Utility.Math.Sum(DM.FixationSupply);
            DM.TotalRetranslocationSupply = Utility.Math.Sum(DM.RetranslocationSupply);

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

            DM.TotalStructuralDemand = Utility.Math.Sum(DM.StructuralDemand);
            DM.TotalMetabolicDemand = Utility.Math.Sum(DM.MetabolicDemand);
            DM.TotalNonStructuralDemand = Utility.Math.Sum(DM.NonStructuralDemand);
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
        virtual public void SendPotentialDMAllocations(Organ[] Organs)
        {
            //  Allocate to meet Organs demands
            DM.TotalStructuralAllocation = Utility.Math.Sum(DM.StructuralAllocation);
            DM.TotalMetabolicAllocation = Utility.Math.Sum(DM.MetabolicAllocation);
            DM.TotalNonStructuralAllocation = Utility.Math.Sum(DM.NonStructuralAllocation);
            DM.Allocated = DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation + DM.TotalNonStructuralAllocation;
            DM.SinkLimitation = Math.Max(0.0, DM.TotalFixationSupply + DM.TotalRetranslocationSupply + DM.TotalReallocationSupply - DM.Allocated);
            
            // Then check it all adds up
            DM.BalanceError = Math.Abs((DM.Allocated + DM.SinkLimitation) - (DM.TotalFixationSupply + DM.TotalRetranslocationSupply + DM.TotalReallocationSupply));
            if (DM.BalanceError > 0.0000001 & DM.TotalStructuralDemand > 0)
                throw new Exception("Mass Balance Error in Photosynthesis DM Allocation");

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
        /// <param name="BAT">The bat.</param>
        virtual public void DoNutrientSetUp(Organ[] Organs, ref BiomassArbitrationType BAT)
        {
            //Creat Biomass variable class
            BAT = new BiomassArbitrationType(Organs.Length);

            // GET ALL INITIAL STATE VARIABLES FOR MASS BALANCE CHECKS
            BAT.Start = 0;

            // GET ALL SUPPLIES AND DEMANDS AND CALCULATE TOTALS
            for (int i = 0; i < Organs.Length; i++)
            {
                BiomassSupplyType Supply = Organs[i].NSupply;
                BAT.ReallocationSupply[i] = Supply.Reallocation;
                //BAT.UptakeSupply[i] = Supply.Uptake;
                BAT.FixationSupply[i] = Supply.Fixation;
                BAT.RetranslocationSupply[i] = Supply.Retranslocation;
                BAT.Start += Organs[i].Live.N + Organs[i].Dead.N;
            }

            BAT.TotalReallocationSupply = Utility.Math.Sum(BAT.ReallocationSupply);
            //BAT.TotalUptakeSupply = Utility.Math.Sum(BAT.UptakeSupply);
            BAT.TotalFixationSupply = Utility.Math.Sum(BAT.FixationSupply);
            BAT.TotalRetranslocationSupply = Utility.Math.Sum(BAT.RetranslocationSupply);

            for (int i = 0; i < Organs.Length; i++)
            {
                BiomassPoolType Demand = Organs[i].NDemand;
                BAT.StructuralDemand[i] = Organs[i].NDemand.Structural;
                BAT.MetabolicDemand[i] = Organs[i].NDemand.Metabolic;
                BAT.NonStructuralDemand[i] = Organs[i].NDemand.NonStructural;
                BAT.TotalDemand[i] = BAT.StructuralDemand[i] + BAT.MetabolicDemand[i] + BAT.NonStructuralDemand[i];

                BAT.Reallocation[i] = 0;
                BAT.Uptake[i] = 0;
                BAT.Fixation[i] = 0;
                BAT.Retranslocation[i] = 0;
                BAT.StructuralAllocation[i] = 0;
                BAT.MetabolicAllocation[i] = 0;
                BAT.NonStructuralAllocation[i] = 0;
            }

            BAT.TotalStructuralDemand = Utility.Math.Sum(BAT.StructuralDemand);
            BAT.TotalMetabolicDemand = Utility.Math.Sum(BAT.MetabolicDemand);
            BAT.TotalNonStructuralDemand = Utility.Math.Sum(BAT.NonStructuralDemand);
            BAT.TotalPlantDemand = BAT.TotalStructuralDemand + BAT.TotalMetabolicDemand + BAT.TotalNonStructuralDemand;

            BAT.TotalStructuralAllocation = 0;
            BAT.TotalMetabolicAllocation = 0;
            BAT.TotalNonStructuralAllocation = 0;

            //Set relative N demands of each organ
            for (int i = 0; i < Organs.Length; i++)
            {
                if (BAT.TotalStructuralDemand > 0)
                    BAT.RelativeStructuralDemand[i] = BAT.StructuralDemand[i] / BAT.TotalStructuralDemand;
                if (BAT.TotalMetabolicDemand > 0)
                    BAT.RelativeMetabolicDemand[i] = BAT.MetabolicDemand[i] / BAT.TotalMetabolicDemand;
                if (BAT.TotalNonStructuralDemand > 0)
                    BAT.RelativeNonStructuralDemand[i] = BAT.NonStructuralDemand[i] / BAT.TotalNonStructuralDemand;
            }
        }
        /// <summary>Does the nutrient uptake set up.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        virtual public void DoNutrientUptakeSetUp(Organ[] Organs, ref BiomassArbitrationType BAT)
        {
            //Creat Biomass variable class
            //BAT = new BiomassArbitrationType(Organs.Length);

            for (int i = 0; i < Organs.Length; i++)
            {
                BiomassSupplyType Supply = Organs[i].NSupply;
                BAT.UptakeSupply[i] = Supply.Uptake;
            }
        
            BAT.TotalUptakeSupply = Utility.Math.Sum(BAT.UptakeSupply);
        }
        /// <summary>Does the re allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="Option">The option.</param>
        virtual public void DoReAllocation(Organ[] Organs, BiomassArbitrationType BAT, string Option)
        {
            double BiomassReallocated = 0;
            if (BAT.TotalReallocationSupply > 0.00000000001)
            {
                //Calculate how much reallocated N (and associated biomass) each demanding organ is allocated based on relative demands
                if (string.Compare(Option, "RelativeAllocation", true) == 0)
                    RelativeAllocation(Organs, BAT.TotalReallocationSupply, ref BiomassReallocated, BAT);
                if (string.Compare(Option, "PriorityAllocation", true) == 0)
                    PriorityAllocation(Organs, BAT.TotalReallocationSupply, ref BiomassReallocated, BAT);
                if (string.Compare(Option, "PrioritythenRelativeAllocation", true) == 0)
                    PrioritythenRelativeAllocation(Organs, BAT.TotalReallocationSupply, ref BiomassReallocated, BAT);

                //Then calculate how much biomass is realloced from each supplying organ based on relative reallocation supply
                for (int i = 0; i < Organs.Length; i++)
                {
                    if (BAT.ReallocationSupply[i] > 0)
                    {
                        double RelativeSupply = BAT.ReallocationSupply[i] / BAT.TotalReallocationSupply;
                        BAT.Reallocation[i] += BiomassReallocated * RelativeSupply;
                    }
                }
            }
        }
        /// <summary>Does the uptake.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="Option">The option.</param>
        virtual public void DoUptake(Organ[] Organs, BiomassArbitrationType BAT, string Option)
        {
            double BiomassTakenUp = 0;
            if (BAT.TotalUptakeSupply > 0.00000000001)
            {
                // Calculate how much uptake N each demanding organ is allocated based on relative demands
                if (string.Compare(Option, "RelativeAllocation", true) == 0)
                    RelativeAllocation(Organs, BAT.TotalUptakeSupply, ref BiomassTakenUp, BAT);
                if (string.Compare(Option, "PriorityAllocation", true) == 0)
                    PriorityAllocation(Organs, BAT.TotalUptakeSupply, ref BiomassTakenUp, BAT);
                if (string.Compare(Option, "PrioritythenRelativeAllocation", true) == 0)
                    PrioritythenRelativeAllocation(Organs, BAT.TotalUptakeSupply, ref BiomassTakenUp, BAT);

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
        virtual public void DoRetranslocation(Organ[] Organs, BiomassArbitrationType BAT, string Option)
        {
            double BiomassRetranslocated = 0;
            if (BAT.TotalRetranslocationSupply > 0.00000000001)
            {
                // Calculate how much retranslocation N (and associated biomass) each demanding organ is allocated based on relative demands
                if (string.Compare(Option, "RelativeAllocation", true) == 0)
                    RelativeAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);
                if (string.Compare(Option, "PriorityAllocation", true) == 0)
                    PriorityAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);
                if (string.Compare(Option, "PrioritythenRelativeAllocation", true) == 0)
                    PrioritythenRelativeAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);

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
        virtual public void DoFixation(Organ[] Organs, BiomassArbitrationType BAT, string Option)
        {
            double BiomassFixed = 0;
            if (BAT.TotalFixationSupply > 0.00000000001)
            {
                // Calculate how much fixed resource each demanding organ is allocated based on relative demands
                if (string.Compare(Option, "RelativeAllocation", true) == 0)
                    RelativeAllocation(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);
                if (string.Compare(Option, "PriorityAllocation", true) == 0)
                    PriorityAllocation(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);
                if (string.Compare(Option, "PrioritythenRelativeAllocation", true) == 0)
                    PrioritythenRelativeAllocation(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);

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
                        DM.TotalRespiration = Utility.Math.Sum(DM.Respiration);
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
                            throw new Exception("Crop is trying to Fix excessive amounts of BAT.  Check partitioning coefficients are giving realistic nodule size and that FixationRatePotential is realistic");
                    }
                }
            }
        }
        /// <summary>Determines Nutrient limitations to DM allocations</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void DoNutrientConstrainedDMAllocation(Organ[] Organs)
        {
            double PreNStressDMAllocation = DM.Allocated;
            for (int i = 0; i < Organs.Length; i++)
                N.TotalAllocation[i] = N.StructuralAllocation[i] + N.MetabolicAllocation[i] + N.NonStructuralAllocation[i];

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
                    double proportion = DM.MetabolicAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i]);
                    DM.StructuralAllocation[i] = Math.Min(DM.StructuralAllocation[i], N.ConstrainedGrowth[i] * (1 - proportion));  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function
                    DM.MetabolicAllocation[i] = Math.Min(DM.MetabolicAllocation[i], N.ConstrainedGrowth[i] * proportion);
                //Question.  Why do I not restrain non-structural DM allocations.  I think this may be wrong and require further thought HEB 15-1-2015
                }
            }
            //Recalculated DM Allocation totals
            DM.TotalStructuralAllocation = Utility.Math.Sum(DM.StructuralAllocation);
            DM.TotalMetabolicAllocation = Utility.Math.Sum(DM.MetabolicAllocation);
            DM.TotalNonStructuralAllocation = Utility.Math.Sum(DM.NonStructuralAllocation);
            DM.Allocated = DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation + DM.TotalNonStructuralAllocation;
            DM.NutrientLimitation = (PreNStressDMAllocation - DM.Allocated);
        }
        /// <summary>Sends the dm allocations.</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void SendDMAllocations(Organ[] Organs)
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
        virtual public void SendNutrientAllocations(Organ[] Organs)
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
                N.End += Organs[i].Live.N + Organs[i].Dead.N;
            N.BalanceError = (N.End - (N.Start + N.TotalUptakeSupply + N.TotalFixationSupply));
            if (N.BalanceError > 0.000000001)
                throw new Exception("N Mass balance violated!!!!.  Daily Plant N increment is greater than N supply");
            N.BalanceError = (N.End - (N.Start + N.TotalPlantDemand));
            if (N.BalanceError > 0.000000001)
                throw new Exception("N Mass balance violated!!!!  Daily Plant N increment is greater than N demand");
            DM.End = 0;
            for (int i = 0; i < Organs.Length; i++)
                DM.End += Organs[i].Live.Wt + Organs[i].Dead.Wt;
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
        private void RelativeAllocation(Organ[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
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
            for (int i = 0; i < Organs.Length; i++)
            {
                double NonStructuralRequirement = Math.Max(0.0, BAT.NonStructuralDemand[i] - BAT.NonStructuralAllocation[i]); //N needed to take organ up to maximum N concentration, Structural, Metabolic and Luxury N demands
                if (NonStructuralRequirement > 0.0)
                {
                    double NonStructuralAllocation = Math.Min(NotAllocated * BAT.RelativeNonStructuralDemand[i], NonStructuralRequirement);
                    BAT.NonStructuralAllocation[i] += NonStructuralAllocation;
                    NotAllocated -= NonStructuralAllocation;
                    TotalAllocated += NonStructuralAllocation;
                }
            }
        }
        /// <summary>Priorities the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        private void PriorityAllocation(Organ[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
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
                    BAT.NonStructuralAllocation[i] += NonStructuralAllocation;
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
        private void PrioritythenRelativeAllocation(Organ[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
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
            for (int i = 0; i < Organs.Length; i++)
            {
                double NonStructuralRequirement = Math.Max(0.0, BAT.NonStructuralDemand[i] - BAT.NonStructuralAllocation[i]); //N needed to take organ up to maximum N concentration, Structural, Metabolic and Luxury N demands
                if (NonStructuralRequirement > 0.0)
                {
                    double NonStructuralAllocation = Math.Min(NotAllocated * BAT.RelativeNonStructuralDemand[i], NonStructuralRequirement);
                    BAT.NonStructuralAllocation[i] += NonStructuralAllocation;
                    NotAllocated -= NonStructuralAllocation;
                    TotalAllocated += NonStructuralAllocation;
                }
            }
        }
        #endregion
    }
}