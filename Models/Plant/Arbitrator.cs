using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;
using System.Xml.Serialization;

namespace Models.PMF
{
    [Serializable]
    public class Arbitrator : Model
    {
        #region Class Members
        // Input paramaters

        [Link]
        public Plant Plant = null;

        [Description("Select method used for Arbitration")]
        public string NArbitrationOption = "";
        [Description("Select method used for DMArbitration")]
        public string DMArbitrationOption = "";
        [Description("List of nutrients that the arbitrator will consider")]
        public string[] NutrientDrivers = null;

        [EventSubscribe("Initialised")]
        public void OnInit()
        {
            //NAware Array.Exists(NutrientDrivers, element => element == "Nitrogen");  Fixme  Need to put this into .xml and write code to handle N unaware crops
            PAware = Array.Exists(NutrientDrivers, element => element == "Phosphorus");
            KAware = Array.Exists(NutrientDrivers, element => element == "Potasium");
        }

        private void Or(bool p)
        {
            throw new NotImplementedException();
        }

        [Serializable]
        public class BiomassArbitrationType
        {
            //Biomass Demand Variables
            public double[] StructuralDemand { get; set; }
            public double TotalStructuralDemand { get; set; }
            public double[] MetabolicDemand { get; set; }
            public double TotalMetabolicDemand { get; set; }
            public double[] NonStructuralDemand { get; set; }
            public double TotalNonStructuralDemand { get; set; }
            public double[] TotalDemand { get; set; }
            public double[] RelativeStructuralDemand { get; set; }
            public double[] RelativeMetabolicDemand { get; set; }
            public double[] RelativeNonStructuralDemand { get; set; }
            public double TotalPlantDemand { get; set; }
            //Biomass Supply Variables
            public double[] ReallocationSupply { get; set; }
            public double TotalReallocationSupply { get; set; }
            public double[] UptakeSupply { get; set; }
            public double TotalUptakeSupply { get; set; }
            public double[] FixationSupply { get; set; }
            public double TotalFixationSupply { get; set; }
            public double[] RetranslocationSupply { get; set; }
            public double TotalRetranslocationSupply { get; set; }
            //Biomass Allocation Variables
            public double[] Reallocation { get; set; }
            public double TotalReallocation { get; set; }
            public double[] Uptake { get; set; }
            public double[] Fixation { get; set; }
            public double[] Retranslocation { get; set; }
            public double TotalRetranslocation { get; set; }
            public double[] Respiration { get; set; }
            public double TotalRespiration { get; set; }
            public double[] ConstrainedGrowth { get; set; }
            public double[] StructuralAllocation { get; set; }
            public double TotalStructuralAllocation { get; set; }
            public double[] MetabolicAllocation { get; set; }
            public double TotalMetabolicAllocation { get; set; }
            public double[] NonStructuralAllocation { get; set; }
            public double TotalNonStructuralAllocation { get; set; }
            public double[] TotalAllocation { get; set; }
            public double TotalAllocated { get; set; }
            public double NotAllocated { get; set; }
            public double SinkLimitation { get; set; }
            //Error checking variables
            public double Start { get; set; }
            public double End { get; set; }
            public double BalanceError { get; set; }
            //Constructor for Array variables
            public BiomassArbitrationType()
            { }

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

        
        private BiomassArbitrationType DM = null;
        private BiomassArbitrationType N = null;
        //private BiomassArbitrationType P = null;
        //private BiomassArbitrationType K = null;

        
        public double DMSupply
        {
            get
            {
                return DM.TotalFixationSupply;
            }
        }
        
        public double NDemand
        {
            get
            {
                if (Plant.InGround)
                    return N.TotalPlantDemand;
                else
                    return 0.0;
            }
        }
        
        public double DeltaWt
        {
            get
            {
                return DM.End - DM.Start;
            }
        }

        //FixME Currently all models are N aware but none are P or K aware.  More programming is needed to make this work! 
        [XmlIgnore]
        public bool NAware = true;
        [XmlIgnore]
        public bool PAware = false;
        [XmlIgnore]
        public bool KAware = false;

        #endregion

        public void Clear()
        {
            DM = null;
            N = null;
        }


        public void DoDMArbitration(Organ[] Organs)
        {
            //Work out how much each organ will grow in the absence of nutrient stress, and how much DM they can supply.
            DoDMSetup(Organs);
            //Set potential growth of each organ, assuming adequate nutrient supply.
            DoReAllocation(Organs, DM, DMArbitrationOption);
            DoFixation(Organs, DM, DMArbitrationOption);
            DoRetranslocation(Organs, DM, DMArbitrationOption);
            DoPotentialDMAllocation(Organs);
        }
        public void DoNutrientArbitration(Organ[] Organs)
        {
            if (NAware) //Note, currently all models N Aware, I have to write some code to take this out
            {
                DoSetup(Organs, ref N);
                DoReAllocation(Organs, N, NArbitrationOption);
                DoUptake(Organs, N, NArbitrationOption);
                DoRetranslocation(Organs, N, NArbitrationOption);
                DoFixation(Organs, N, NArbitrationOption);
            }
            /* if (PAware) //Note, currently all models on NOT P Aware
             {
                 DoNutrientSetup(Organs, ref P);
                 DoNutrientReAllocation(Organs, P);
                 DoNutrientUptake(Organs, P);
                 DoNutrientRetranslocation(Organs, P);
                 DoNutrientFixation(Organs, P);
             }
             if (KAware) //Note, currently all models on NOT K Aware
             {
                 DoNutrientSetup(Organs, ref K);
                 DoNutrientReAllocation(Organs, K);
                 DoNutrientUptake(Organs, K);
                 DoNutrientRetranslocation(Organs, K);
                 DoNutrientFixation(Organs, K);
             }*/
            //Work out how much DM can be assimilated by each organ based on the most limiting nutrient
            DoActualDMAllocation(Organs);
            //Tell each organ how much nutrient they are getting following allocaition
            DoNutrientAllocation(Organs);
        }

        #region Arbitration step functions
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

            //Set relative N demands of each organ
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
        virtual public void DoPotentialDMAllocation(Organ[] Organs)
        {
            //  Allocate to meet Organs demands
            DM.SinkLimitation = Math.Max(0.0, DM.TotalFixationSupply + DM.TotalReallocationSupply - DM.TotalAllocated);
            DM.TotalStructuralAllocation = Utility.Math.Sum(DM.StructuralAllocation);
            DM.TotalMetabolicAllocation = Utility.Math.Sum(DM.MetabolicAllocation);
            DM.TotalNonStructuralAllocation = Utility.Math.Sum(DM.NonStructuralAllocation);

            // Then check it all adds up
            DM.BalanceError = Math.Abs((DM.TotalAllocated + DM.SinkLimitation) - (DM.TotalFixationSupply + DM.TotalReallocationSupply));
            if (DM.BalanceError > 0.00001 & DM.TotalStructuralDemand > 0)
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
        virtual public void DoSetup(Organ[] Organs, ref BiomassArbitrationType BAT)
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
                BAT.UptakeSupply[i] = Supply.Uptake;
                BAT.FixationSupply[i] = Supply.Fixation;
                BAT.RetranslocationSupply[i] = Supply.Retranslocation;
                BAT.Start += Organs[i].Live.N + Organs[i].Dead.N;
            }

            BAT.TotalReallocationSupply = Utility.Math.Sum(BAT.ReallocationSupply);
            BAT.TotalUptakeSupply = Utility.Math.Sum(BAT.UptakeSupply);
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

            DM.TotalStructuralAllocation = 0;
            DM.TotalMetabolicAllocation = 0;
            DM.TotalNonStructuralAllocation = 0;

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

                /// Then calculate how much N (and associated biomass) is retranslocated from each supplying organ based on relative retranslocation supply
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
        virtual public void DoFixation(Organ[] Organs, BiomassArbitrationType BAT, string Option)
        {
            double BiomassFixed = 0;
            if (BAT.TotalFixationSupply > 0.00000000001)
            {
                // Calculate how much fixation N each demanding organ is allocated based on relative demands
                if (string.Compare(Option, "RelativeAllocation", true) == 0)
                    RelativeAllocation(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);
                if (string.Compare(Option, "PriorityAllocation", true) == 0)
                    PriorityAllocation(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);
                if (string.Compare(Option, "PrioritythenRelativeAllocation", true) == 0)
                    PrioritythenRelativeAllocation(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);

                // Then calculate how much N is fixed from each supplying organ based on relative fixation supply
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
        virtual public void DoActualDMAllocation(Organ[] Organs)
        {
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
            //NutrientLimitatedWtAllocation = 0;
            for (int i = 0; i < Organs.Length; i++)
            {
                if ((DM.MetabolicAllocation[i] + DM.StructuralAllocation[i]) != 0)
                {
                    double proportion = DM.MetabolicAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i]);
                    DM.StructuralAllocation[i] = Math.Min(DM.StructuralAllocation[i], N.ConstrainedGrowth[i] * (1 - proportion));  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function
                    DM.MetabolicAllocation[i] = Math.Min(DM.MetabolicAllocation[i], N.ConstrainedGrowth[i] * proportion);
                }
            }

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
        virtual public void DoNutrientAllocation(Organ[] Organs)
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
            N.BalanceError = (N.End - (N.Start + NDemand));
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
        private void RelativeAllocation(Organ[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            ////allocate to structural and metabolic N first
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