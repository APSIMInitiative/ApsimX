using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Functions.SupplyFunctions;
using Models.PMF.Phen;
using System.Xml.Serialization;

namespace Models.PMF.Organs
{
    [Description("Leaf Class")]
    public class Leaf : BaseOrgan, AboveGround
    {
        #region Links
        [Link]
        public Plant Plant = null;
        [Link]
        Summary Summary = null;
        [Link]
        public Arbitrator Arbitrator = null;
        [Link]
        public Structure Structure = null;
        [Link]
        public Phenology Phenology = null;
        #endregion

        #region Structures
        public class InitialLeafValues : Model
        {
            public Function MaxArea { get; set; }
            public Function GrowthDuration { get; set; }
            public Function LagDuration { get; set; }
            public Function SenescenceDuration { get; set; }
            public Function DetachmentLagDuration { get; set; }
            public Function DetachmentDuration { get; set; }
            public Function SpecificLeafAreaMax { get; set; }
            public Function SpecificLeafAreaMin { get; set; }
            public Function StructuralFraction { get; set; }
            public Function MaximumNConc { get; set; }
            public Function MinimumNConc { get; set; }
            public Function StructuralNConc { get; set; }
            public Function InitialNConc { get; set; }
            public Function NReallocationFactor { get; set; }
            public Function DMReallocationFactor { get; set; }
            public Function NRetranslocationFactor { get; set; }
            public Function ExpansionStress { get; set; }
            public Function CriticalNConc { get; set; }
            public Function DMRetranslocationFactor { get; set; }
            public Function ShadeInducedSenescenceRate { get; set; }
            public Function DroughtInducedSenAcceleration { get; set; }
            public Function NonStructuralFraction { get; set; }
            public Function CellDivisionStress { get; set; }
        }
        #endregion

        #region Parameters
        // DeanH: I have removed DroughtInducedSenAcceleration - it can be incorported into the ThermalTime function
        // in the XML. No need for it to be in leaf.

        [XmlElement("InitialLeaf")]
        public List<LeafCohort> InitialLeaves { get; set; }
        public InitialLeafValues LeafCohortParameters { get; set; }
        public RUEModel Photosynthesis { get; set; }
        public Function ThermalTime { get; set; }
        public Function ExtinctionCoeff { get; set; }
        public Function FrostFraction { get; set; }
        public Function ExpansionStress { get; set; }
        public Function CriticalNConc { get; set; }
        public Function MaximumNConc { get; set; }
        public Function MinimumNConc { get; set; }
        public Function StructuralFraction { get; set; }
        public Function DMDemandFunction = null;
        public Biomass Total { get; set; }
        public ArrayBiomass CohortArrayLive { get; set; }
        public ArrayBiomass CohortArrayDead { get; set; }

        [Description("Extinction Coefficient (Dead)")]
        public double KDead { get; set; }

        #endregion

        #region States
        private List<LeafCohort> Leaves = new List<LeafCohort>();

        /// <summary>
        /// Initialise all state variables.
        /// </summary>
        public override void Clear()
        {
            Leaves = new List<LeafCohort>();
            WaterDemand = 0;
            WaterAllocation = 0;
        }

        #endregion

        #region Outputs
        //Note on naming convention.  
        //Variables that represent the number of units per meter of area these are called population (Popn suffix) variables 
        //Variables that represent the number of leaf cohorts (integer) in a particular state on an individual main-stem are cohort variables (CohortNo suffix)
        //Variables that represent the number of primordia or nodes (double) in a particular state on an individual mainstem are called number variables (e.g NodeNo or PrimordiaNo suffix)
        //Variables that the number of leaves on a plant or a primary bud have Plant or Primary bud prefixes

        /// <summary>
        /// Return the 
        /// </summary>
        public double CohortCurrentRankCoverAbove
        {
            get
            {
                return Leaves[CurrentRank-1].CoverAbove;
            }
        }

        public bool CohortsInitialised
        {
            get
            {
                return Leaves.Count > 0;
            }
        }

        [Description("Max cover")]
        [Units("max units")]
        public double MaxCover;

        [Description("Number of leaf cohort objects that have been initialised")] //Note:  InitialisedCohortNo is an interger of Primordia Number, increasing every time primordia increses by one and a new cohort is initialised
        public double InitialisedCohortNo { get { return CohortCounter("IsInitialised"); } }

        [Description("Number of leaf cohort that have appeared")] //Note:  AppearedCohortNo is an interger of AppearedNodeNo, increasing every time AppearedNodeNo increses by one and a new cohort is appeared
        public double AppearedCohortNo
        {
            get
            {
                int Count = CohortCounter("IsAppeared");
                if (FinalLeafAppeared)
                    return Count - (1 - FinalLeafFraction);
                else
                    return Count;
            }
        }

        [Description("If last leaf has appeared, return the fraction of the final part leaf")]
        public double FinalLeafFraction
        {
            get
            {
                int Count = CohortCounter("IsAppeared");
                // DeanH: I don't think this next if statement will ever be true. Isn't MaximumNodeNumber
                // always equal to MainStemFinalNodeNo?
                if (Count == (int)Structure.MainStemFinalNodeNo && Count < Structure.MaximumNodeNumber) 
                    return Leaves[Count-1].FractionExpanded;
                else
                    return 1.0;
            }
        }

        [Description("Returns true if the final leaf has appeared")]
        public bool FinalLeafAppeared
        {
            get
            {
                if (FinalLeafFraction != 1.0)
                    return true;
                else
                    return false;
            }
        }

        [Description("Number of leaf cohorts that have appeared but not yet fully expanded")]
        public double ExpandingCohortNo { get { return CohortCounter("IsGrowing"); } }

        //FIXME ExpandedNodeNo and Expanded Cohort need to be merged
        [Description("Number of leaf cohorts that are fully expanded")]
        public double ExpandedNodeNo
        {
            get
            {
                foreach (LeafCohort L in Leaves)
                    if (!L.IsFullyExpanded)
                        return ExpandedCohortNo + L.FractionExpanded;
                return 0;
            }
        }

        [Description("Number of leaf cohorts that are fully expanded")]
        public double ExpandedCohortNo { get { return Math.Min(CohortCounter("IsFullyExpanded"), Structure.MainStemFinalNodeNo); } }

        [Description("Number of leaf cohorts that are have expanded but not yet fully senesced")]
        public double GreenCohortNo
        {
            get
            {
                int Count = CohortCounter("IsGreen");
                if (FinalLeafAppeared)
                    return Count - (1 - FinalLeafFraction);
                else
                    return Count;
            }
        }

        [Description("Number of leaf cohorts that are Senescing")]
        public double SenescingCohortNo { get { return CohortCounter("IsSenescing"); } }

        [Description("Number of leaf cohorts that have fully Senesced")]
        public double DeadCohortNo { get { return Math.Min(CohortCounter("IsSenescing"), Structure.MainStemFinalNodeNo); } }

        [Units("/plant")]
        [Description("Number of appeared leaves per plant that have appeared but not yet fully senesced on each plant")]
        public double PlantAppearedGreenLeafNo
        {
            get
            {
                double n = 0;
                foreach (LeafCohort L in Leaves)
                    if ((L.IsAppeared) && (!L.Finished))
                        n += L.CohortPopulation;
                return n / Structure.Population;
            }
        }

        [Units("/plant")]
        [Description("Number of leaves per plant that have appeared")]
        public double PlantAppearedLeafNo
        {
            get
            {
                double n = 0;
                foreach (LeafCohort L in Leaves)
                    if (L.IsAppeared)
                        n += L.CohortPopulation;
                return n;
            }
        }

        //Canopy State variables

        [Units("m^2/m^2")]
        public double LAI
        {
            get
            {
                int MM2ToM2 = 1000000; // Conversion of mm2 to m2
                double value = 0;
                foreach (LeafCohort L in Leaves)
                    value = value + L.LiveArea / MM2ToM2;
                return value;
            }
        }

        [Units("m^2/m^2")]
        public double LAIDead
        {
            get
            {
                double value = 0;
                foreach (LeafCohort L in Leaves)
                    value = value + L.DeadArea / 1000000;
                return value;
            }
        }

        [XmlIgnore]
        [Units("g/m^2")]
        public Biomass CohortLive
        {
            get
            {
                Biomass Biomass = new Biomass();
                foreach (LeafCohort L in Leaves)
                    Biomass = Biomass + L.Live;
                return Biomass;
            }
            
        }

        [XmlIgnore]
        [Units("g/m^2")]
        public Biomass CohortDead
        {
            get
            {
                Biomass Biomass = new Biomass();
                foreach (LeafCohort L in Leaves)
                    Biomass = Biomass + L.Dead;
                return Biomass;
            }
        }

        [Units("0-1")]
        public double CoverGreen { get { return MaxCover * (1.0 - Math.Exp(-ExtinctionCoeff.Value * LAI / MaxCover)); } }

        [Units("0-1")]
        public double CoverDead { get { return 1.0 - Math.Exp(-KDead * LAIDead); } }

        [Units("0-1")]
        public double CoverTotal { get { return 1.0 - (1 - CoverGreen) * (1 - CoverDead); } }

        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadIntTot { get { return CoverGreen * MetData.Radn; } }

        [Units("mm^2/g")]
        public double SpecificArea
        {
            get
            {
                if (Live.Wt > 0)
                    return LAI / Live.Wt * 1000000;
                else
                    return 0;
            }
        }
        //Cohort State variable outputs

        public double[] CohortSize
        {
            get
            {
                int i = 0;

                double[] values = new double[(int)Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.Size;
                    i++;
                }

                return values;
            }
        }

        [Units("mm2")]
        public double[] MaxLeafArea
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.MaxArea;
                    i++;
                }

                return values;
            }
        }

        [Units("mm2")]
        public double[] CohortArea
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.LiveArea;
                    i++;
                }

                return values;
            }
        }

        [Units("mm2")]
        public double[] CohortAge
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.Age;
                    i++;
                }

                return values;
            }
        }

        [Units("mm2")]
        public double[] CohortMaxSize
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.MaxSize;
                    i++;
                }

                return values;
            }
        }

        [Units("mm2")]
        public double[] CohortMaxArea
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.MaxArea;
                    i++;
                }

                return values;
            }
        }

        [Units("mm2/g")]
        public double[] CohortSLA
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.SpecificArea;
                    i++;
                }

                return values;
            }
        }

        [Units("0-1")]
        public double[] CohortStructuralFrac
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    if ((L.Live.StructuralWt + L.Live.MetabolicWt + L.Live.NonStructuralWt) > 0.0)
                    {
                        values[i] = L.Live.StructuralWt / (L.Live.StructuralWt + L.Live.MetabolicWt + L.Live.NonStructuralWt);
                        i++;
                    }
                    else
                    {
                        values[i] = 0;
                        i++;
                    }
                }

                return values;
            }
        }

        //General Leaf State variables

        [Units("g/g")]
        public double LiveNConc
        {
            get
            {
                return Live.NConc;
            }
        }

        [Units("g/m^2")]
        public double PotentialGrowth { get { return DMDemand.Structural; } }

        [Units("mm")]
        public double Transpiration { get { return WaterAllocation; } }

        [Units("0-1")]
        public double Fw
        {
            get
            {
                double F = 0;
                if (WaterDemand > 0)
                    F = WaterAllocation / WaterDemand;
                else
                    F = 1;
                return F;
            }
        }

        [Units("0-1")]
        public double Fn
        {
            get
            {
                double F = 1;
                double FunctionalNConc = (LeafCohortParameters.CriticalNConc.Value - (LeafCohortParameters.MinimumNConc.Value * LeafCohortParameters.StructuralFraction.Value)) * (1 / (1 - LeafCohortParameters.StructuralFraction.Value));
                if (FunctionalNConc == 0)
                    F = 1;
                else
                {
                    F = Live.MetabolicNConc / FunctionalNConc;
                    F = Math.Max(0.0, Math.Min(F, 1.0));
                }
                return F;
            }
        }
        #endregion

        #region Functions
        /// <summary>
        /// 1 based rank of the current leaf.
        /// </summary>
        private int CurrentRank
        {
            get
            {
                if (Leaves.Count == 0)
                    return 0;

                // Find the first non appeared leaf.
                int i = 0;
                while (i < Leaves.Count && Leaves[i].IsAppeared)
                    i++;

                if (i == 0)
                    throw new ApsimXException(this.FullPath, "No leaves have appeared. Cannot calculate Leaf.CurrentRank");

                return Leaves[i - 1].Rank;
            }
        }
        private int CohortCounter(string Condition)
        {
            int Count = 0;
            foreach (LeafCohort L in Leaves)
            {
                object o = Utility.Reflection.GetValueOfFieldOrProperty(Condition, L);
                if (o == null)
                    throw new NotImplementedException();
                bool ok = (bool)o;
                if (ok)
                    Count++;
            }
            return Count;
        }
        public void CopyLeaves(LeafCohort[] From, List<LeafCohort> To)
        {
            foreach (LeafCohort Leaf in From)
                To.Add(Leaf.Clone());
        }
        public override void DoPotentialDM()
        {
            WaterAllocation = 0;

            if (FrostFraction.Value > 0)
                foreach (LeafCohort L in Leaves)
                    L.DoFrost(FrostFraction.Value);

            // On the initial day set up initial cohorts and set their properties
            if (Phenology.OnDayOf(Structure.InitialiseStage))
                InitialiseCohorts();

            //When primordia number is 1 more than current cohort number produce a new cohort
            if (Structure.MainStemPrimordiaNo >= Leaves.Count + FinalLeafFraction)
            {
                if (CohortsInitialised == false)
                    throw new Exception("Trying to initialse new cohorts prior to InitialStage.  Check the InitialStage parameter on the leaf object and the parameterisation of NodeInitiationRate.  Your NodeInitiationRate is triggering a new leaf cohort before leaves have been initialised.");

                LeafCohort NewLeaf = InitialLeaves[0].Clone();
                NewLeaf.CohortPopulation = 0;
                NewLeaf.Age = 0;
                NewLeaf.Rank = (int)Math.Truncate(Structure.MainStemNodeNo);
                NewLeaf.Area = 0.0;
                NewLeaf.DoInitialisation();
                Leaves.Add(NewLeaf);
            }

            //When Node number is 1 more than current appeared leaf number make a new leaf appear and start growing
            if ((Structure.MainStemNodeNo >= AppearedCohortNo + FinalLeafFraction) && (FinalLeafFraction > 0.0))
            {

                if (CohortsInitialised == false)
                    throw new Exception("Trying to initialse new cohorts prior to InitialStage.  Check the InitialStage parameter on the leaf object and the parameterisation of NodeAppearanceRate.  Your NodeAppearanceRate is triggering a new leaf cohort before the initial leaves have been triggered.");
                int AppearingNode = (int)(Structure.MainStemNodeNo + (1 - FinalLeafFraction));
                double CohortAge = (Structure.MainStemNodeNo - AppearingNode) * Structure.MainStemNodeAppearanceRate.Value * FinalLeafFraction;
                if (AppearingNode > InitialisedCohortNo)
                    throw new Exception("MainStemNodeNumber exceeds the number of leaf cohorts initialised.  Check primordia parameters to make sure primordia are being initiated fast enough and for long enough");
                int i = AppearingNode - 1;
                Leaves[i].Rank = AppearingNode;
                Leaves[i].CohortPopulation = Structure.TotalStemPopn;
                Leaves[i].Age = CohortAge;
                Leaves[i].DoAppearance(FinalLeafFraction, LeafCohortParameters);
                if (NewLeaf != null)
                    NewLeaf.Invoke();
            }
           
            foreach (LeafCohort L in Leaves)
                L.DoPotentialGrowth(ThermalTime.Value, LeafCohortParameters);
        }
        public virtual void InitialiseCohorts() //This sets up cohorts on the day growth starts (eg at emergence)
        {
            Leaves = new List<LeafCohort>();
            CopyLeaves(InitialLeaves.ToArray(), Leaves);
            foreach (LeafCohort Leaf in Leaves)
            {
                if (Leaf.Area > 0)//If initial cohorts have an area set the are considered to be appeared on day of emergence so we do appearance and count up the appeared nodes on the first day
                {
                    Leaf.CohortPopulation = Structure.TotalStemPopn;

                    Leaf.DoInitialisation();
                    Structure.MainStemNodeNo += 1.0;
                    Leaf.DoAppearance(1.0, LeafCohortParameters);
                }
                else //Leaves are primordia and have not yet emerged, initialise but do not set appeared values yet
                    Leaf.DoInitialisation();
                Structure.MainStemPrimordiaNo += 1.0;
            }
        }
        public override void DoActualGrowth()
        {
            foreach (LeafCohort L in Leaves)
                L.DoActualGrowth(ThermalTime.Value, LeafCohortParameters);

            Structure.UpdateHeight();

            PublishNewCanopyEvent();
        }
        public virtual void ZeroLeaves()
        {
            Structure.MainStemNodeNo = 0;
            Structure.Clear();
            Leaves.Clear();
            Summary.WriteMessage(FullPath, "Removing Leaves from plant");
        }
        /// <summary>
        /// Fractional interception "above" a given node position 
        /// </summary>
        /// <param name="cohortno">cohort position</param>
        /// <returns>fractional interception (0-1)</returns>
        public double CoverAboveCohort(double cohortno)
        {
            int MM2ToM2 = 1000000; // Conversion of mm2 to m2
            double LAIabove = 0;
            for (int i = Leaves.Count - 1; i > cohortno - 1; i--)
                LAIabove += Leaves[i].LiveArea / MM2ToM2;
            return 1 - Math.Exp(-ExtinctionCoeff.Value * LAIabove);
        }

        #endregion

        #region Arbitrator methods

        [Units("g/m^2")]
        public override BiomassPoolType DMDemand
        {
            get
            {
                double StructuralDemand = 0.0;
                double NonStructuralDemand = 0.0;
                double MetabolicDemand = 0.0;

                if (DMDemandFunction != null)
                {
                    StructuralDemand = DMDemandFunction.Value * StructuralFraction.Value;
                    NonStructuralDemand = DMDemandFunction.Value * (1 - StructuralFraction.Value);
                }
                else
                {
                    foreach (LeafCohort L in Leaves)
                    {
                        StructuralDemand += L.StructuralDMDemand;
                        MetabolicDemand += L.MetabolicDMDemand;
                        NonStructuralDemand += L.NonStructuralDMDemand;
                    }
                }
                return new BiomassPoolType { Structural = StructuralDemand, Metabolic = MetabolicDemand, NonStructural = NonStructuralDemand };
            }

        }
        /// <summary>
        /// Daily photosynthetic "net" supply of dry matter for the whole plant (g DM/m2/day)
        /// </summary>
        [Units("g/m^2")]
        public override BiomassSupplyType DMSupply
        {
            get
            {
                double Retranslocation = 0;
                double Reallocation = 0;

                foreach (LeafCohort L in Leaves)
                {
                    Retranslocation += L.LeafStartDMRetranslocationSupply;
                    Reallocation += L.LeafStartDMReallocationSupply;
                }


                return new BiomassSupplyType { Fixation = Photosynthesis.Growth(RadIntTot), Retranslocation = Retranslocation, Reallocation = Reallocation };
            }
        }
        [Units("g/m^2")]
        public override BiomassPoolType DMPotentialAllocation
        {
            set
            {
                //Allocate Potential Structural DM
                if (DMDemand.Structural == 0)
                    if (value.Structural < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of potential DM in" + Name);

                double[] CohortPotentialStructualDMAllocation = new double[Leaves.Count + 2];

                if (value.Structural == 0.0)
                { }// do nothing
                else
                {
                    double DMPotentialsupply = value.Structural;
                    double DMPotentialallocated = 0;
                    double TotalPotentialDemand = 0;
                    foreach (LeafCohort L in Leaves)
                        TotalPotentialDemand += L.StructuralDMDemand;
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double fraction = L.StructuralDMDemand / TotalPotentialDemand;
                        double PotentialAllocation = Math.Min(L.StructuralDMDemand, DMPotentialsupply * fraction);
                        CohortPotentialStructualDMAllocation[i] = PotentialAllocation;
                        DMPotentialallocated += PotentialAllocation;
                    }
                    if ((DMPotentialallocated - value.Structural) > 0.000000001)
                        throw new Exception("the sum of poteitial DM allocation to leaf cohorts is more that that allocated to leaf organ");
                }

                //Allocate Metabolic DM
                if (DMDemand.Metabolic == 0)
                    if (value.Metabolic < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of potential DM in" + Name);

                double[] CohortPotentialMetabolicDMAllocation = new double[Leaves.Count + 2];

                if (value.Metabolic == 0.0)
                { }// do nothing
                else
                {
                    double DMPotentialsupply = value.Metabolic;
                    double DMPotentialallocated = 0;
                    double TotalPotentialDemand = 0;
                    foreach (LeafCohort L in Leaves)
                        TotalPotentialDemand += L.MetabolicDMDemand;
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double fraction = L.MetabolicDMDemand / TotalPotentialDemand;
                        double PotentialAllocation = Math.Min(L.MetabolicDMDemand, DMPotentialsupply * fraction);
                        CohortPotentialMetabolicDMAllocation[i] = PotentialAllocation;
                        DMPotentialallocated += PotentialAllocation;
                    }
                    if ((DMPotentialallocated - value.Metabolic) > 0.000000001)
                        throw new Exception("the sum of poteitial DM allocation to leaf cohorts is more that that allocated to leaf organ");
                }

                //Send allocations to cohorts
                int a = 0;
                foreach (LeafCohort L in Leaves)
                {
                    a++;
                    L.DMPotentialAllocation = new BiomassPoolType
                    {
                        Structural = CohortPotentialStructualDMAllocation[a],
                        Metabolic = CohortPotentialMetabolicDMAllocation[a],
                    };
                }
            }
        }
        [Units("g/m^2")]
        public override BiomassAllocationType DMAllocation
        {
            set
            {
                double[] StructuralDMAllocationCohort = new double[Leaves.Count + 2];

                double check = Live.StructuralWt;
                //Structural DM allocation
                if (DMDemand.Structural == 0)
                    if (value.Structural < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of DM in Leaf");
                if (value.Structural == 0.0)
                { }// do nothing
                else
                {
                    double DMsupply = value.Structural;
                    double DMallocated = 0;
                    double TotalDemand = 0;
                    foreach (LeafCohort L in Leaves)
                        TotalDemand += L.StructuralDMDemand;
                    double DemandFraction = (value.Structural) / TotalDemand;//
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double Allocation = Math.Min(L.StructuralDMDemand * DemandFraction, DMsupply);
                        StructuralDMAllocationCohort[i] = Allocation;
                        DMallocated += Allocation;
                        DMsupply -= Allocation;
                    }
                    if (DMsupply > 0.0000000001)
                        throw new Exception("DM allocated to Leaf left over after allocation to leaf cohorts");
                    if ((DMallocated - value.Structural) > 0.000000001)
                        throw new Exception("the sum of DM allocation to leaf cohorts is more that that allocated to leaf organ");
                }

                //Metabolic DM allocation
                double[] MetabolicDMAllocationCohort = new double[Leaves.Count + 2];

                if (DMDemand.Metabolic == 0)
                    if (value.Metabolic < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of DM in Leaf");
                if (value.Metabolic == 0.0)
                { }// do nothing
                else
                {
                    double DMsupply = value.Metabolic;
                    double DMallocated = 0;
                    double TotalDemand = 0;
                    foreach (LeafCohort L in Leaves)
                        TotalDemand += L.MetabolicDMDemand;
                    double DemandFraction = (value.Metabolic) / TotalDemand;//
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double Allocation = Math.Min(L.MetabolicDMDemand * DemandFraction, DMsupply);
                        MetabolicDMAllocationCohort[i] = Allocation;
                        DMallocated += Allocation;
                        DMsupply -= Allocation;
                    }
                    if (DMsupply > 0.0000000001)
                        throw new Exception("Metabolic DM allocated to Leaf left over after allocation to leaf cohorts");
                    if ((DMallocated - value.Metabolic) > 0.000000001)
                        throw new Exception("the sum of Metabolic DM allocation to leaf cohorts is more that that allocated to leaf organ");
                }

                // excess allocation
                double[] NonStructuralDMAllocationCohort = new double[Leaves.Count + 2];
                double TotalSinkCapacity = 0;
                foreach (LeafCohort L in Leaves)
                    TotalSinkCapacity += L.NonStructuralDMDemand;
                if (value.NonStructural > TotalSinkCapacity)
                    throw new Exception("Allocating more excess DM to Leaves then they are capable of storing");
                if (TotalSinkCapacity > 0.0)
                {
                    double SinkFraction = value.NonStructural / TotalSinkCapacity;
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double Allocation = Math.Min(L.NonStructuralDMDemand * SinkFraction, value.NonStructural);
                        NonStructuralDMAllocationCohort[i] = Allocation;
                    }
                }

                // retranslocation
                double[] DMRetranslocationCohort = new double[Leaves.Count + 2];

                if (value.Retranslocation - DMSupply.Retranslocation > 0.0000000001)
                    throw new Exception(Name + " cannot supply that amount for DM retranslocation");
                if (value.Retranslocation > 0)
                {
                    double remainder = value.Retranslocation;
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double Supply = Math.Min(remainder, L.DMRetranslocationSupply);
                        DMRetranslocationCohort[i] = Supply;
                        remainder -= Supply;
                    }
                    if (remainder > 0.0000000001)
                        throw new Exception(Name + " DM Retranslocation demand left over after processing.");
                }

                // Reallocation
                double[] DMReAllocationCohort = new double[Leaves.Count + 2];
                if (value.Reallocation - DMSupply.Reallocation > 0.000000001)
                    throw new Exception(Name + " cannot supply that amount for DM Reallocation");
                if (value.Reallocation < -0.000000001)
                    throw new Exception(Name + " recieved -ve DM reallocation");
                if (value.Reallocation > 0)
                {
                    double remainder = value.Reallocation;
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double ReAlloc = Math.Min(remainder, L.LeafStartDMReallocationSupply);
                        remainder = Math.Max(0.0, remainder - ReAlloc);
                        DMReAllocationCohort[i] = ReAlloc;
                    }
                    if (!Utility.Math.FloatsAreEqual(remainder, 0.0))
                        throw new Exception(Name + " DM Reallocation demand left over after processing.");
                }

                //Send allocations to cohorts
                int a = 0;
                foreach (LeafCohort L in Leaves)
                {
                    a++;
                    L.DMAllocation = new BiomassAllocationType
                    {
                        Structural = StructuralDMAllocationCohort[a],
                        Metabolic = MetabolicDMAllocationCohort[a],
                        NonStructural = NonStructuralDMAllocationCohort[a],
                        Retranslocation = DMRetranslocationCohort[a],
                        Reallocation = DMReAllocationCohort[a],
                    };
                }
            }
        }
        [XmlIgnore]
        [Units("mm")]
        public override double WaterDemand { get; set; }
        [XmlIgnore]
        public override double WaterAllocation { get; set;}
       
        [Units("g/m^2")]
        public override BiomassPoolType NDemand
        {
            get
            {
                double StructuralDemand = 0.0;
                double MetabolicDemand = 0.0;
                double NonStructuralDemand = 0.0;
                foreach (LeafCohort L in Leaves)
                {
                    StructuralDemand += L.StructuralNDemand;
                    MetabolicDemand += L.MetabolicNDemand;
                    NonStructuralDemand += L.NonStructuralNDemand;
                }
                return new BiomassPoolType { Structural = StructuralDemand, Metabolic = MetabolicDemand, NonStructural = NonStructuralDemand };
            }
        }
        [Units("g/m^2")]
        public override BiomassAllocationType NAllocation
        {
            set
            {

                if (NDemand.Structural == 0)
                    if (value.Structural == 0) { }//All OK  FIXME this needs to be seperated into compoents
                    else
                        throw new Exception("Invalid allocation of N");

                double[] StructuralNAllocationCohort = new double[Leaves.Count + 2];
                double[] MetabolicNAllocationCohort = new double[Leaves.Count + 2];
                double[] NonStructuralNAllocationCohort = new double[Leaves.Count + 2];
                double[] NReallocationCohort = new double[Leaves.Count + 2];
                double[] NRetranslocationCohort = new double[Leaves.Count + 2];
                if ((value.Structural + value.Metabolic + value.NonStructural) == 0.0)
                { }// do nothing
                else
                {


                    //setup allocation variables
                    double[] CohortNAllocation = new double[Leaves.Count + 2];
                    double[] StructuralNDemand = new double[Leaves.Count + 2];
                    double[] MetabolicNDemand = new double[Leaves.Count + 2];
                    double[] NonStructuralNDemand = new double[Leaves.Count + 2];
                    double TotalStructuralNDemand = 0;
                    double TotalMetabolicNDemand = 0;
                    double TotalNonStructuralNDemand = 0;

                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        {
                            i++;
                            CohortNAllocation[i] = 0;
                            StructuralNDemand[i] = L.StructuralNDemand;
                            TotalStructuralNDemand += L.StructuralNDemand;
                            MetabolicNDemand[i] = L.MetabolicNDemand;
                            TotalMetabolicNDemand += L.MetabolicNDemand;
                            NonStructuralNDemand[i] = L.NonStructuralNDemand;
                            TotalNonStructuralNDemand += L.NonStructuralNDemand;
                        }
                    }
                    double NSupplyValue = value.Structural;
                    //double LeafNAllocated = 0;

                    // first make sure each cohort gets the structural N requirement for growth (includes MinNconc for structural growth and MinNconc for nonstructural growth)
                    if ((NSupplyValue > 0) & (TotalStructuralNDemand > 0))
                    {
                        i = 0;
                        foreach (LeafCohort L in Leaves)
                        {
                            i++;
                            //double allocation = 0;
                            //allocation = Math.Min(StructuralNDemand[i], NSupplyValue * (StructuralNDemand[i] / TotalStructuralNDemand));
                            StructuralNAllocationCohort[i] = Math.Min(StructuralNDemand[i], NSupplyValue * (StructuralNDemand[i] / TotalStructuralNDemand));
                            //LeafNAllocated += allocation;
                        }

                    }
                    // then allocate additional N relative to leaves metabolic demands
                    NSupplyValue = value.Metabolic;
                    if ((NSupplyValue > 0) & (TotalMetabolicNDemand > 0))
                    {
                        i = 0;
                        foreach (LeafCohort L in Leaves)
                        {
                            i++;
                            //double allocation = 0;
                            //allocation = Math.Min(MetabolicNDemand[i], NSupplyValue * (MetabolicNDemand[i] / TotalMetabolicNDemand));
                            MetabolicNAllocationCohort[i] = Math.Min(MetabolicNDemand[i], NSupplyValue * (MetabolicNDemand[i] / TotalMetabolicNDemand));
                            //LeafNAllocated += allocation;
                        }
                    }
                    // then allocate excess N relative to leaves N sink capacity
                    NSupplyValue = value.NonStructural;
                    if ((NSupplyValue > 0) & (TotalNonStructuralNDemand > 0))
                    {
                        i = 0;
                        foreach (LeafCohort L in Leaves)
                        {
                            i++;
                            //double allocation = 0;
                            //allocation = Math.Min(NonStructuralNDemand[i], NSupplyValue * (NonStructuralNDemand[i] / TotalNonStructuralNDemand));
                            NonStructuralNAllocationCohort[i] += Math.Min(NonStructuralNDemand[i], NSupplyValue * (NonStructuralNDemand[i] / TotalNonStructuralNDemand));
                            //LeafNAllocated += allocation;
                        }
                        //NSupplyValue = value.Structural - LeafNAllocated;
                    }

                    //if (NSupplyValue > 0.0000000001)
                    //    throw new Exception("N allocated to Leaf left over after allocation to leaf cohorts");
                    //if ((LeafNAllocated - value.Structural) > 0.000000001)
                    //    throw new Exception("the sum of N allocation to leaf cohorts is more that that allocated to leaf organ");

                    //send N allocations to each cohort
                    //i = 0;
                    //foreach (LeafCohort L in Leaves)
                    //{
                    //    i++;
                    //    L.NAllocation = CohortNAllocation[i];
                    //}
                }

                // Retranslocation
                if (value.Retranslocation - NSupply.Retranslocation > 0.000000001)
                    throw new Exception(Name + " cannot supply that amount for N retranslocation");
                if (value.Retranslocation < -0.000000001)
                    throw new Exception(Name + " recieved -ve N retranslocation");
                if (value.Retranslocation > 0)
                {
                    int i = 0;
                    double remainder = value.Retranslocation;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double Retrans = Math.Min(remainder, L.LeafStartNRetranslocationSupply);
                        //L.NRetranslocation = Retrans;
                        NRetranslocationCohort[i] = Retrans;
                        remainder = Math.Max(0.0, remainder - Retrans);
                    }
                    if (!Utility.Math.FloatsAreEqual(remainder, 0.0))
                        throw new Exception(Name + " N Retranslocation demand left over after processing.");
                }

                // Reallocation
                if (value.Reallocation - NSupply.Reallocation > 0.000000001)
                    throw new Exception(Name + " cannot supply that amount for N Reallocation");
                if (value.Reallocation < -0.000000001)
                    throw new Exception(Name + " recieved -ve N reallocation");
                if (value.Reallocation > 0)
                {
                    int i = 0;
                    double remainder = value.Reallocation;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double ReAlloc = Math.Min(remainder, L.LeafStartNReallocationSupply);
                        //L.NReallocation = ReAlloc;
                        NReallocationCohort[i] = ReAlloc;
                        remainder = Math.Max(0.0, remainder - ReAlloc);
                    }
                    if (!Utility.Math.FloatsAreEqual(remainder, 0.0))
                        throw new Exception(Name + " N Reallocation demand left over after processing.");
                }

                //Send allocations to cohorts
                int a = 0;
                foreach (LeafCohort L in Leaves)
                {
                    a++;
                    L.NAllocation = new BiomassAllocationType
                    {
                        Structural = StructuralNAllocationCohort[a],
                        Metabolic = MetabolicNAllocationCohort[a],
                        NonStructural = NonStructuralNAllocationCohort[a],
                        Retranslocation = NRetranslocationCohort[a],
                        Reallocation = NReallocationCohort[a],
                    };
                }
            }
        }
        [Units("g/m^2")]
        public override BiomassSupplyType NSupply
        {
            get
            {
                double RetransSupply = 0;
                double ReallocationSupply = 0;
                foreach (LeafCohort L in Leaves)
                {
                    RetransSupply += Math.Max(0, L.LeafStartNRetranslocationSupply);
                    ReallocationSupply += L.LeafStartNReallocationSupply;
                }

                return new BiomassSupplyType { Retranslocation = RetransSupply, Reallocation = ReallocationSupply };
            }
        }

        public override double MaxNconc
        {
            get
            {
                return LeafCohortParameters.MaximumNConc.Value;
            }
        }
        public override double MinNconc
        {
            get
            {
                return LeafCohortParameters.CriticalNConc.Value;
            }
        }
        #endregion

        #region Event handlers and publishers

        public event NewCanopyDelegate NewCanopy;
        public event NullTypeDelegate NewLeaf;

        [EventSubscribe("Prune")]
        private void OnPrune(PruneType Prune)
        {
            Structure.PrimaryBudNo = Prune.BudNumber;
            ZeroLeaves();
        }

        [EventSubscribe("RemoveLowestLeaf")]
        private void OnRemoveLowestLeaf()
        {
            Summary.WriteMessage(FullPath, "Removing Lowest Leaf");
            Leaves.RemoveAt(0);
        }

        public override void OnSow(SowPlant2Type Sow)
        {
            Clear();
            if (Sow.MaxCover <= 0.0)
                throw new Exception("MaxCover must exceed zero in a Sow event.");
            MaxCover = Sow.MaxCover;
        }

        [EventSubscribe("Canopy_Water_Balance")]
        private void OnCanopy_Water_Balance(CanopyWaterBalanceType CWB)
        {
            if (Plant.InGround)
            {
                Boolean found = false;
                int i = 0;
                while (!found && (i != CWB.Canopy.Length))
                {
                    if (CWB.Canopy[i].name.ToLower() == Plant.Name.ToLower())
                    {
                        WaterDemand = CWB.Canopy[i].PotentialEp;
                        found = true;
                    }
                    else
                        i++;
                }
            }
        }

        [EventSubscribe("KillLeaf")]
        private void OnKillLeaf(KillLeafType KillLeaf)
        {
            //DateTime Today = DateUtility.JulianDayNumberToDateTime(Convert.ToInt32(MetData.today));
            string Indent = "     ";
            string Title = Indent + Clock.Today.ToString("d MMMM yyyy") + "  - Killing " + KillLeaf.KillFraction + " of leaves on " + Plant.Name;
            Summary.WriteMessage(FullPath, Title);
            Summary.WriteMessage(FullPath, Indent + new string('-', Title.Length));

            foreach (LeafCohort L in Leaves)
                L.DoKill(KillLeaf.KillFraction);

        }

        public override void OnCut()
        {
            //DateTime Today = DateUtility.JulianDayNumberToDateTime(Convert.ToInt32(MetData.today));
            string Indent = "     ";
            string Title = Indent + Clock.Today.ToString("d MMMM yyyy") + "  - Cutting " + Name + " from " + Plant.Name;
            Summary.WriteMessage(FullPath, "");
            Summary.WriteMessage(FullPath, Title);
            Summary.WriteMessage(FullPath, Indent + new string('-', Title.Length));

            Structure.MainStemNodeNo = 0;
            Structure.Clear();
            Live.Clear();
            Dead.Clear();
            Leaves.Clear();
            Structure.ResetStemPopn();
            InitialiseCohorts();
            //Structure.ResetStemPopn();
        }

        protected virtual void PublishNewCanopyEvent()
        {
            if (NewCanopy != null)
            {
                NewCanopyType Canopy = new NewCanopyType();
                Canopy.sender = Plant.Name;
                Canopy.lai = (float)LAI;
                Canopy.lai_tot = (float)(LAI + LAIDead);
                Canopy.height = (float)Structure.Height;
                Canopy.depth = (float)Structure.Height;
                Canopy.cover = (float)CoverGreen;
                Canopy.cover_tot = (float)CoverTotal;
                NewCanopy.Invoke(Canopy);
            }
        }

        #endregion


    }
}
