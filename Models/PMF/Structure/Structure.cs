using System;
using Models.Core;
using Models.Functions;
using Models.PMF.Phen;
using System.Xml.Serialization;
using Models.Interfaces;
using Models.PMF.Interfaces;

namespace Models.PMF.Struct
{
    /// <summary>
    /// # Structure
    /// The structure model simulates morphological development of the plant to inform the Leaf class when 
    ///   and how many leaves appear and to provide a hight estimate for use in calculating potential transpiration.
    /// ## Plant and Main-Stem Population
    /// The *Plant.Population* is set at sowing with information sent from a manager script in the Sow method.    
    ///   The *PrimaryBudNumber* is also sent with the Sow method and the main-stem population (*MainStemPopn*) for the crop is calculated as:  
    ///   *MainStemPopn* = *Plant.Population* x *PrimaryBudNumber*
    ///   Primary bud number is > 1 for crops like potato and grape vine where there are more than one main-stem per plant
    ///  ## Main-Stem leaf appearance
    ///  Each day the number of main-stem leaf tips appeared (*LeafTipsAppeared*) is calculated as:  
    ///    *LeafTipsAppeared* += *DeltaTips*
    ///  Where *DeltaTips* is calculated as:  
    ///    *DeltaTips* = *ThermalTime*/*Phyllochron*  
    ///    Where *Phyllochron* is the thermal time duration between the appearance of leaf tipx given by: 
    /// [Document Phyllochron]
    ///   and *ThermalTime* is given by:
    /// [Document ThermalTime]
    /// *LeafTipsAppeared* continues to increase until *FinalLeafNumber* is reached where *FinalLeafNumber* is calculated as:  
    /// [Document FinalLeafNumber]
    /// ##Branching and Branch Mortality
    /// The total population of stems (*TotalStemPopn*) is calculated as:  
    ///   *TotalStemPopn* = *MainStemPopn* + *NewBranches* - *NewlyDeadBranches*   
    ///    Where *NewBranches* = *MainStemPopn* x *BranchingRate*  
    ///    and *BranchingRate* is given by:
    /// [Document BranchingRate]
    ///   *NewlyDeadBranches* is calcualted as:  
    ///   *NewlyDeadBranches* = (*TotalStemPopn* - *MainStemPopn*) x *BranchMortality*  
    ///   where *BranchMortality* is given by:  
    /// [Document BranchMortality]
    /// ##Height
    ///  The Height of the crop is calculated by the *HeightModel*:
    /// [Document HeightModel]
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Structure : Model
    {
        // 1. Links
        //-------------------------------------------------------------------------------------------
        [Link]
        private Plant plant = null;

        [Link]
        private ILeaf leaf = null;

        [Link]
        private Phenology phenology = null;

        /// <summary>The thermal time</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction thermalTime = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction phyllochron = null;

        /// <summary>The main stem final node number</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction finalLeafNumber = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction heightModel = null;

        /// <summary>Branching rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction branchingRate = null;

        /// <summary>Branch mortality</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction branchMortality = null;


        /// <summary>The Stage that cohorts are initialised on</summary>
        [Description("The Stage that cohorts are initialised on")]
        public string CohortInitialisationStage { get; set; } = "Germination";

        /// <summary>The Stage that leaves are initialised on</summary>
        [Description("The Stage that leaves are initialised on")]
        public string LeafInitialisationStage { get; set; } = "Emergence";


        // 2. Private fields
        //-------------------------------------------------------------------------------------------

        private bool leavesInitialised;

        private bool cohortsInitialised;

        private bool firstPass;


        // 4. Public Events And Enums
        //-------------------------------------------------------------------------------------------

        /// <summary>Occurs when plant Germinates.</summary>
        public event EventHandler InitialiseLeafCohorts;

        /// <summary>Occurs when ever an new vegetative leaf cohort is initiated on the stem apex.</summary>
        public event EventHandler<CohortInitParams> AddLeafCohort;

        /// <summary>Occurs when ever an new leaf tip appears.</summary>
        public event EventHandler<ApparingLeafParams> LeafTipAppearance;


        // 5. Public properties
        //-------------------------------------------------------------------------------------------
        /// <summary>The Leaf Appearance Data </summary>
        [XmlIgnore]
        public CohortInitParams InitParams { get; set; }

        /// <summary>CohortToInitialise</summary>
        [XmlIgnore]
        public int CohortToInitialise { get; set; }

        /// <summary>TipToAppear</summary>
        [XmlIgnore]
        public int TipToAppear { get; set; }

        /// <summary>Did another leaf appear today?</summary>
        [XmlIgnore]
        public bool TimeForAnotherLeaf { get; set; }

        /// <summary>Have all leaves appeared?</summary>
        [XmlIgnore]
        public bool AllLeavesAppeared { get; set; }

        /// <summary>The Leaf Appearance Data </summary>
        [XmlIgnore]
        public ApparingLeafParams CohortParams { get; set; }

        /// <summary>Gets or sets the primary bud no.</summary>
        [XmlIgnore]
        public double PrimaryBudNo { get; set; }

        /// <summary>Gets or sets the total stem popn.</summary>
        [XmlIgnore]
        public double TotalStemPopn { get; set; }

        //Plant leaf number state variables
        /// <summary>Number of mainstem nodes which have their tips appeared</summary>
        [XmlIgnore]
        public double PotLeafTipsAppeared { get; set; }

        /// <summary>"Number of mainstem nodes which have their tips appeared"</summary>
        [XmlIgnore]
        public double LeafTipsAppeared { get; set; }

        /// <summary>Number of leaves appeared per plant including all main stem and branch leaves</summary>
        [XmlIgnore]
        public double PlantTotalNodeNo { get; set; }

        /// <summary>Gets or sets the proportion branch mortality.</summary>
        [XmlIgnore]
        public double ProportionBranchMortality { get; set; }

        /// <summary>Gets or sets the proportion plant mortality.</summary>
        [XmlIgnore]
        public double ProportionPlantMortality { get; set; }

        /// <value>The change in HaunStage each day.</value>
        [XmlIgnore]
        public double DeltaHaunStage { get; set; }

        /// <value>The delta node number.</value>
        [XmlIgnore]
        public double DeltaTipNumber { get; set; }

        /// <summary>The number of branches, used by zadoc class for calcualting zadoc score in the 20's</summary>
        [XmlIgnore]
        public double BranchNumber { get; set; }

        /// <summary>The relative size of the current cohort.  Is always 1.0 apart for the final cohort where it can be less than 1.0 if final leaf number is not an interger value</summary>
        [XmlIgnore]
        public double NextLeafProportion { get; set; }

        /// <summary> The change in plant population due to plant mortality set in the plant class </summary>
        [XmlIgnore]
        public double DeltaPlantPopulation { get; set; }

        /// <summary>"Number of mainstems per meter"</summary>
        [XmlIgnore]
        public double MainStemPopn { get { return plant.Population * PrimaryBudNo; } }

        /// <summary>Number of leaves yet to appear</summary>
        [XmlIgnore]
        public double RemainingNodeNo { get { return finalLeafNumber.Value() - LeafTipsAppeared; } }

        /// <summary>Gets the height.</summary>
        [XmlIgnore]
        public double Height { get; private set; }

        /// <summary>Number of appeared leaves per primary bud unit including all main stem and branch leaves</summary>
        [XmlIgnore]
        public double PrimaryBudTotalNodeNo { get { return PlantTotalNodeNo / PrimaryBudNo; } }

        /// <summary>Relative progress toward final leaf.</summary>
        [XmlIgnore]
        public double RelativeNodeApperance { get { return LeafTipsAppeared / finalLeafNumber.Value(); } }
        /// <summary>Total number of leaves per shoot .</summary>
        [XmlIgnore]
        public double TotalLeavesPerShoot { get; set; }

        // 6. Public methods
        //-------------------------------------------------------------------------------------------
        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            TotalStemPopn = 0;
            TotalLeavesPerShoot = 0;
            PotLeafTipsAppeared = 0;
            PlantTotalNodeNo = 0;
            ProportionBranchMortality = 0;
            ProportionPlantMortality = 0;
            DeltaTipNumber = 0;
            DeltaHaunStage = 0;
            leavesInitialised = false;
            cohortsInitialised = false;
            firstPass = false;
            Height = 0;
            LeafTipsAppeared = 0;
            BranchNumber = 0;
            NextLeafProportion = 0;
            DeltaPlantPopulation = 0;
        }

        // 7. Private methods
        //-------------------------------------------------------------------------------------------

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            DeltaPlantPopulation = 0;
            ProportionPlantMortality = 0;
        }

        /// <summary>Called when [do daily initialisation].</summary>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (phenology != null && phenology.OnStartDayOf("Emergence"))
                LeafTipsAppeared = 1.0;
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (cohortsInitialised)
            {
                DeltaHaunStage = 0;
                if (phyllochron.Value() > 0)
                    DeltaHaunStage = thermalTime.Value() / phyllochron.Value();
                if (leavesInitialised)
                {
                    bool AllCohortsInitialised = (leaf.InitialisedCohortNo >= finalLeafNumber.Value());
                    AllLeavesAppeared = (leaf.AppearedCohortNo == leaf.InitialisedCohortNo);
                    bool LastLeafAppearing = ((Math.Truncate(LeafTipsAppeared) + 1) == leaf.InitialisedCohortNo);

                    if ((AllCohortsInitialised) && (LastLeafAppearing))
                    {
                        NextLeafProportion = 1 - (leaf.InitialisedCohortNo - finalLeafNumber.Value());
                    }
                    else
                    {
                        NextLeafProportion = 1.0;
                    }

                    //Increment MainStemNode Number based on phyllochorn and theremal time
                    if (firstPass == true)
                    {
                        firstPass = false;
                        DeltaTipNumber = 0; //Don't increment node number on day of emergence
                    }
                    else
                    {
                        DeltaTipNumber = DeltaHaunStage; //DeltaTipNumber is only positive after emergence whereas deltaHaunstage is positive from germination
                    }

                    PotLeafTipsAppeared += DeltaTipNumber;
                    LeafTipsAppeared = Math.Min(PotLeafTipsAppeared, finalLeafNumber.Value());

                    TimeForAnotherLeaf = PotLeafTipsAppeared >= (leaf.AppearedCohortNo + 1);
                    int LeavesToAppear = (int)(LeafTipsAppeared - (leaf.AppearedCohortNo - (1 - NextLeafProportion)));

                    //Each time main-stem node number increases by one or more initiate the additional cohorts until final leaf number is reached
                    if (TimeForAnotherLeaf && (AllCohortsInitialised == false))
                    {
                        int i = 1;
                        for (i = 1; i <= LeavesToAppear; i++)
                        {
                            CohortToInitialise += 1;
                            InitParams = new CohortInitParams() { };
                            InitParams.Rank = CohortToInitialise;
                            if (AddLeafCohort != null)
                                AddLeafCohort.Invoke(this, InitParams);
                        }
                    }

                    //Each time main-stem node number increases by one appear another cohort until all cohorts have appeared
                    if (TimeForAnotherLeaf && (AllLeavesAppeared == false))
                    {
                        int i = 1;
                        for (i = 1; i <= LeavesToAppear; i++)
                        {
                            TotalStemPopn += branchingRate.Value() * MainStemPopn;
                            BranchNumber += branchingRate.Value();

                            TotalLeavesPerShoot += BranchNumber + 1;
                            DoLeafTipAppearance();
                        }
                    }

                    //Reduce population if there has been plant mortality 
                    if (DeltaPlantPopulation > 0)
                        TotalStemPopn -= DeltaPlantPopulation * TotalStemPopn / plant.Population;

                    //Reduce stem number incase of mortality
                    double PropnMortality = 0;
                    PropnMortality = branchMortality.Value();
                    {
                        double DeltaPopn = Math.Min(PropnMortality * (TotalStemPopn - MainStemPopn), TotalStemPopn - plant.Population);
                        TotalStemPopn -= DeltaPopn;
                        ProportionBranchMortality = PropnMortality;

                    }
                    
                }
            }
        }

        /// <summary>Called when [phase changed].</summary>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == CohortInitialisationStage)
            {
                InitialiseLeafCohorts?.Invoke(this, new EventArgs());
                cohortsInitialised = true;
            }

            if (phaseChange.StageName == LeafInitialisationStage)
            {
                NextLeafProportion = 1.0;
                DoLeafInitilisation();
            }
        }

        /// <summary>Does the actual growth.</summary>
        [EventSubscribe("DoActualPlantPartioning")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (plant.IsAlive)
            {
                PlantTotalNodeNo = leaf.PlantAppearedLeafNo;
            }
        }
        /// <summary>Method that calculates parameters for leaf cohort to appear and then calls event so leaf calss can make cohort appear</summary>
        public void DoLeafTipAppearance()
        {
            TipToAppear += 1;
            CohortParams = new ApparingLeafParams() { };
            CohortParams.CohortToAppear = TipToAppear;
            CohortParams.TotalStemPopn = TotalStemPopn;
            CohortParams.CohortAge = 0;
            CohortParams.FinalFraction = NextLeafProportion;
            if (LeafTipAppearance != null)
                LeafTipAppearance.Invoke(this, CohortParams);
        }

        /// <summary> Called on the day of emergence to get the initials leaf cohorts to appear </summary>
        private void DoLeafInitilisation()
        {
            CohortToInitialise = leaf.CohortsAtInitialisation;
            for (int i = 1; i <= leaf.TipsAtEmergence; i++)
            {
                InitParams = new CohortInitParams();
                PotLeafTipsAppeared += 1;
                CohortToInitialise += 1;
                InitParams.Rank = CohortToInitialise;
                AddLeafCohort?.Invoke(this, InitParams);
                DoLeafTipAppearance();
                leavesInitialised = true;
                firstPass = true;
            }
        }

        /// <summary>Updates the height.</summary>
        public void UpdateHeight()
        {
            Height = heightModel.Value();
        }
        /// <summary>Resets the stem popn.</summary>
        public void ResetStemPopn()
        {
            TotalStemPopn = MainStemPopn;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            Clear();
            CohortToInitialise = 0;
            TipToAppear = 0;
            PotLeafTipsAppeared = 0;
            ResetStemPopn();
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type Sow)
        {
            if (Sow.Plant == plant)
            {
                Clear();
                if (Sow.MaxCover <= 0.0)
                    throw new Exception("MaxCover must exceed zero in a Sow event.");
                PrimaryBudNo = Sow.BudNumber;
                TotalStemPopn = MainStemPopn;
            }
        }

        /// <summary>Called when crop recieves a remove biomass event from manager</summary>
        public void doThin(double ProportionRemoved)
        {
            plant.Population *= (1 - ProportionRemoved);
            TotalStemPopn *= (1 - ProportionRemoved);
            leaf.DoThin(ProportionRemoved);
        }

        /// <summary> Removes nodes from main-stem in defoliation event  </summary>
        public void doNodeRemoval(int NodesToRemove)
        {
            //Remove nodes from Structure properties
            LeafTipsAppeared = Math.Max(LeafTipsAppeared - NodesToRemove, 0);
            PotLeafTipsAppeared = Math.Max(PotLeafTipsAppeared - NodesToRemove, 0);

            //Remove corresponding cohorts from leaf
            int NodesStillToRemove = Math.Min(NodesToRemove + leaf.ApicalCohortNo, leaf.InitialisedCohortNo);
            while (NodesStillToRemove > 0)
            {
                TipToAppear -= 1;
                CohortToInitialise -= 1;
                leaf.RemoveHighestLeaf();
                NodesStillToRemove -= 1;
            }
            //TipToAppear = Math.Max(TipToAppear + leaf.CohortsAtInitialisation, 1);
            CohortToInitialise = Math.Max(CohortToInitialise, 1);
            if (CohortToInitialise == LeafTipsAppeared) // If leaf appearance had reached final leaf number need to add another cohort back to get things moving again.
                CohortToInitialise += 1;
            InitParams = new CohortInitParams() { };
            InitParams.Rank = CohortToInitialise;
            if (AddLeafCohort != null)
                AddLeafCohort.Invoke(this, InitParams);
            //Reinitiate apical cohorts ready for regrowth
            if (leaf.InitialisedCohortNo == 0) //If all nodes have been removed initalise again
            {
                leaf.Reset();
                InitialiseLeafCohorts.Invoke(this, new EventArgs());
                DoLeafInitilisation();
            }
        }
    }
}   

    

