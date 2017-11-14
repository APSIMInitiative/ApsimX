using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Organs;
using Models.PMF.Phen;
using System.Xml.Serialization;
using Models.Interfaces;
using Models.PMF.Interfaces;

namespace Models.PMF.Struct
{
    /// <summary>
    /// # Structure #
    /// The structure model simulates morphological development of the plant to inform the Leaf class when 
    ///   and how many leaves appear and to provides a hight estimate for use in calculating potential transpiration.
    /// ## Plant and Main-Stem Population ##
    /// The *Plant.Population* is set at sowing with information sent from a manager script in the Sow method.    
    ///   The *PrimaryBudNumber* is also sent with the Sow method and the main-stem population (*MainStemPopn*) for the crop is calculated as:  
    ///   *MainStemPopn* = *Plant.Population* x *PrimaryBudNumber*
    ///   Primary bud number is > 1 for crops like potato and grape vine where there are more than one main-stem per plant
    ///  ## Main-Stem leaf appearance ##
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
    /// ##Branching and Branch Mortality##
    /// The total population of stems (*TotalStemPopn*) is calculated as:  
    ///   *TotalStemPopn* = *MainStemPopn* + *NewBranches* - *NewlyDeadBranches*   
    ///    Where *NewBranches* = *MainStemPopn* x *BranchingRate*  
    ///    and *BranchingRate* is given by:
    /// [Document BranchingRate]
    ///   *NewlyDeadBranches* is calcualted as:  
    ///   *NewlyDeadBranches* = (*TotalStemPopn* - *MainStemPopn*) x *BranchMortality*  
    ///   where *BranchMortality* is given by:  
    /// [Document BranchMortality]
    /// ##Height##
    ///  The Height of the crop is calculated by the *HeightModel*:
    /// [Document HeightModel]
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    public class Structure : Model
    {
        /// <summary>Occurs when plant Germinates.</summary>
        public event EventHandler InitialiseLeafCohorts;
        /// <summary>Occurs when ever an new vegetative leaf cohort is initiated on the stem apex.</summary>
        public event EventHandler<CohortInitParams> AddLeafCohort;
        /// <summary>The Leaf Appearance Data </summary>
        [XmlIgnore]
        public CohortInitParams InitParams { get; set; }
        /// <summary>Occurs when ever an new leaf tip appears.</summary>
        public event EventHandler<ApparingLeafParams> LeafTipAppearance;
        /// <summary>The Leaf Appearance DAta </summary>
        [XmlIgnore]
        public ApparingLeafParams CohortParams { get; set; }
        /// <summary>The arguments</summary>
        private EventArgs args = new EventArgs();
        /// <summary>CohortToInitialise</summary>
        public int CohortToInitialise { get; set; }
        /// <summary>TipToAppear</summary>
        public int TipToAppear { get; set; }
        //private double FinalLeafDeltaTipNumberonDayOfAppearance { get; set; }
        #region Links
        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;
        /// <summary>The leaf</summary>
        [Link]
        ILeaf Leaf = null;
        /// <summary> Phenology model</summary>
        [Link]
        Phenology Phenology = null;

        #endregion

        #region Parameters
        /// <summary>Gets or sets the primary bud no.</summary>
        /// <value>The primary bud no.</value>
        [Description("Number of mainstem units per plant")]
        [Units("/plant")]
        [XmlIgnore]
        public double PrimaryBudNo {get; set;}

        /// <summary>The thermal time</summary>
        [Link]
        IFunction ThermalTime = null;
        /// <summary>The main stem node appearance rate</summary>
        [Link]
        public IFunction Phyllochron = null;
        
        /// <summary>The main stem final node number</summary>
        [Link]
        public IFunction FinalLeafNumber = null;
        /// <summary>The height model</summary>
        [Link]
        [Units("mm")]
        IFunction HeightModel = null;
        /// <summary>The branching rate</summary>
        [Link]
        [Units("/node")]
        IFunction BranchingRate = null;
        /// <summary>The branch mortality</summary>
        [Link]
        [Units("/d")]
        IFunction BranchMortality = null;
        /// <summary>The maximum age of stem senescence</summary>
        [Link]
        public IFunction StemSenescenceAge = null;
        #endregion

        #region States
        /// <summary>Test if Initialisation done</summary>
        public bool Initialised;
        /// <summary>Test if Initialisation done</summary>
        public bool Germinated;
        /// <summary>Test if Initialisation done</summary>
        public bool Emerged;
        /// <summary>Total apex number in plant.</summary>
        [Description("Total apex number in plant")]
        public double ApexNum { get; set; }

        private double _Height;

        /// <summary>Gets or sets the total stem popn.</summary>
        /// <value>The total stem popn.</value>
        [XmlIgnore]
        [Description("Number of stems per meter including main and branch stems")]
        [Units("/m2")]
        public double TotalStemPopn { get; set; }

        //Plant leaf number state variables
        /// <summary>Gets or sets the main stem node no.</summary>
        /// <value>The main stem node no.</value>
        [XmlIgnore]
        [Description("Number of mainstem nodes which have their tips appeared")]
        public double PotLeafTipsAppeared { get; set; }

        /// <summary>Gets or sets the main stem node no.</summary>
        /// <value>The main stem node no.</value>
        [XmlIgnore]
        [Description("Number of mainstem nodes which have their tips appeared")]
        public double LeafTipsAppeared { get; set; }

        /// <summary>Gets or sets the plant total node no.</summary>
        /// <value>The plant total node no.</value>
        [XmlIgnore]
        [Units("/plant")]
        [Description("Number of leaves appeared per plant including all main stem and branch leaves")]
        public double PlantTotalNodeNo { get; set; }

        //Utility Variables
        /// <summary>Gets or sets the proportion branch mortality.</summary>
        /// <value>The proportion branch mortality.</value>
        [XmlIgnore]
        public double ProportionBranchMortality { get; set; }

        /// <summary>Gets or sets the proportion plant mortality.</summary>
        /// <value>The proportion plant mortality.</value>
        [XmlIgnore]
        public double ProportionPlantMortality { get; set; }

        /// <value>The change in HaunStage each day.</value>
        [XmlIgnore]
        public double DeltaHaunStage { get; set; }

        /// <value>Senscenced by age.</value>
        [XmlIgnore]
        public bool SenescenceByAge { get; set; }


        /// <value>The delta node number.</value>
        [XmlIgnore]
        public double DeltaTipNumber { get; set; }

        /// <summary>The number of branches, used by zadoc class for calcualting zadoc score in the 20's</summary>
        /// <value>number of tillers.</value>
        [XmlIgnore]
        public double BranchNumber { get; set; }

        /// <summary>The relative size of the current cohort.  Is always 1.0 apart for the final cohort where it can be less than 1.0 if final leaf number is not an interger value</summary>
        [XmlIgnore]
        public double NextLeafProportion { get; set; }

        /// <summary>
        /// The change in plant population due to plant mortality set in the plant class
        /// </summary>
        [XmlIgnore]
        public double DeltaPlantPopulation { get; set; }
        
        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            TotalStemPopn = 0;
            PotLeafTipsAppeared = 0;
            PlantTotalNodeNo = 0;
            ProportionBranchMortality = 0;
            ProportionPlantMortality = 0;
            DeltaTipNumber = 0;
            DeltaHaunStage = 0;
            SenescenceByAge = false;
        }

        #endregion

        #region Outputs
        /// <summary>Gets the main stem popn.</summary>
        /// <value>The main stem popn.</value>
        [XmlIgnore]
        [Description("Number of mainstems per meter")]
        [Units("/m2")]
        public double MainStemPopn { get { return Plant.Population * PrimaryBudNo; } }

        /// <summary>Gets the remaining node no.</summary>
        /// <value>The remaining node no.</value>
        [XmlIgnore]
        [Description("Number of leaves yet to appear")]
        public double RemainingNodeNo { get { return FinalLeafNumber.Value() - LeafTipsAppeared; } }

        /// <summary>Gets the height.</summary>
        /// <value>The height.</value>
        [XmlIgnore]
        [Units("mm")]
        public double Height { get { return _Height; } } 

        /// <summary>Gets the primary bud total node no.</summary>
        /// <value>The primary bud total node no.</value>
        
        [Units("/PrimaryBud")]
        [Description("Number of appeared leaves per primary bud unit including all main stem and branch leaves")]
        [XmlIgnore]
        public double PrimaryBudTotalNodeNo { get { return PlantTotalNodeNo / PrimaryBudNo; } }

        /// <summary>Gets the relative node apperance.</summary>
        /// <value>The relative node apperance.</value>
        [Units("0-1")]
        [XmlIgnore]
        [Description("Relative progress toward final leaf")]
        public double RelativeNodeApperance
        {
            get
            {
                return LeafTipsAppeared / FinalLeafNumber.Value();
            }
        }
        #endregion

        #region Top level timestep Functions
        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            DeltaPlantPopulation = 0;
            ProportionPlantMortality = 0;
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (Phenology != null && Phenology.OnDayOf("Emergence"))
                     LeafTipsAppeared = 1.0;
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsGerminated)
            {
                Leaf l = Leaf as Leaf;
                DeltaHaunStage = 0;
                if (Phyllochron.Value() > 0)
                    DeltaHaunStage = ThermalTime.Value() / Phyllochron.Value();
               
                if (Germinated==false) // We have no leaves set up and nodes have just started appearing - Need to initialise Leaf cohorts
                {
                    Germinated = true;
                    //On the day of germination set up the first cohorts
                    if(InitialiseLeafCohorts !=null)
                        InitialiseLeafCohorts.Invoke(this, args);
                        Initialised = true;
                }

                if (Plant.IsEmerged)
                {
                    if(Emerged==false)
                    {
                        NextLeafProportion = 1.0;
                        DoEmergence();
                    }

                    bool AllCohortsInitialised = (Leaf.InitialisedCohortNo >= FinalLeafNumber.Value());
                    bool AllLeavesAppeared = (Leaf.AppearedCohortNo == Leaf.InitialisedCohortNo);
                    bool LastLeafAppearing = ((Math.Truncate(LeafTipsAppeared) + 1)  == Leaf.InitialisedCohortNo);
                    
                    if ((AllCohortsInitialised)&&(LastLeafAppearing))
                    {
                        NextLeafProportion = 1-(Leaf.InitialisedCohortNo - FinalLeafNumber.Value());
                    }
                    else
                    {
                        NextLeafProportion = 1.0;
                    }

                    //Increment MainStemNode Number based on phyllochorn and theremal time
                    if (Emerged == false)
                    {
                        Emerged = true;
                        DeltaTipNumber = 0; //Don't increment node number on day of emergence
                    }
                    else
                    {
                        DeltaTipNumber = DeltaHaunStage; //DeltaTipNumber is only positive after emergence whereas deltaHaunstage is positive from germination
                    }

                    PotLeafTipsAppeared += DeltaTipNumber;
                    //if (PotLeafTipsAppeared > MainStemFinalNodeNumber.Value)
                    //    FinalLeafDeltaTipNumberonDayOfAppearance = PotLeafTipsAppeared - MainStemFinalNodeNumber.Value;
                    LeafTipsAppeared = Math.Min(PotLeafTipsAppeared, FinalLeafNumber.Value());

                    bool TimeForAnotherLeaf = PotLeafTipsAppeared >= (Leaf.AppearedCohortNo + 1);
                    int LeavesToAppear = (int)(LeafTipsAppeared - (Leaf.AppearedCohortNo - (1- NextLeafProportion)));

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
                            TotalStemPopn += BranchingRate.Value() * MainStemPopn;
                            BranchNumber += BranchingRate.Value();
                            DoLeafTipAppearance();
                        }
                        // Apex calculation
                        ApexNum += (BranchingRate.Value() - BranchMortality.Value()) * PrimaryBudNo;

                        if (Phenology.Stage > 4 & !SenescenceByAge)
                        {
                            if(l != null && l.Apex is ApexTiller)
                            {
                                double n = Leaf.ApexNumByAge(StemSenescenceAge.Value());
                                ApexNum -= n;
                                TotalStemPopn -= n * Plant.Population;
                            }
                            else
                                ApexNum -= Leaf.ApexNumByAge(StemSenescenceAge.Value());

                            SenescenceByAge = true;
                        }
                    }

                    //Reduce population if there has been plant mortality 
                    if (DeltaPlantPopulation > 0)
                    {
                        TotalStemPopn -= DeltaPlantPopulation * TotalStemPopn / Plant.Population;
                        // Reduce cohort population in case of plant mortality
                        // RemoveFromCohorts(DeltaPlantPopulation / Plant.Population, 0);
                    }

                    // Reduce stem number incase of mortality
                    double PropnMortality = 0;
                    PropnMortality = BranchMortality.Value();
                    double DeltaPopn = Math.Min(PropnMortality * (TotalStemPopn - MainStemPopn), TotalStemPopn - Plant.Population);

                    if (l != null)
                    {
                        TotalStemPopn -= DeltaPopn;
                        ProportionBranchMortality = PropnMortality;
                        // In case of branch mortality, reduce cohort populations for 
                        // all cohrots whose leaf/stem number is not less than the 
                        // leaf /stem number at the toppest leaf.
                        // This operation is conducted in whole season for ApexStandard,
                        // but only after flag leaf appeared in ApexTiller.
                        if (l.Apex is ApexStandard)
                        {
                            // RemoveFromCohorts(PropnMortality, ApexNum);
                        } else if (l.Apex is ApexTiller & AllLeavesAppeared)
                        {
                            // RemoveFromCohorts(PropnMortality, ApexNum);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove cohort population according to branch mortality
        /// </summary>
        /// <param name="minimum">Minimum value of ApexNum in order to remove stem.</param>
        /// <param name="fraction">The fraction of removing from stem population</param>
        private void RemoveFromCohorts(double fraction, double minimum = 0)
        {
            Leaf leaf = Leaf as Leaf; // This is terrible
            if (leaf == null)
                return;

            foreach (LeafCohort LC in leaf.Leaves)
                if (ApexNum >= minimum)
                    LC.CohortPopulation *= fraction;
        }

        /// <summary>Does the actual growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantPartioning")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            //Set PlantTotalNodeNo    
            if (Plant.IsAlive)
            {
                PlantTotalNodeNo = Leaf.PlantAppearedLeafNo;
            }
        }
        #endregion
        /// <summary>
        /// Called on the day of emergence to get the initials leaf cohorts to appear
        /// </summary>
        public void DoEmergence()
        {
            CohortToInitialise = Leaf.CohortsAtInitialisation;
            for (int i = 1; i <= Leaf.TipsAtEmergence; i++)
            {
                InitParams = new CohortInitParams(); 
                PotLeafTipsAppeared += 1;
                CohortToInitialise += 1;
                InitParams.Rank = CohortToInitialise; 
                if(AddLeafCohort != null)
                    AddLeafCohort.Invoke(this, InitParams);
                    DoLeafTipAppearance();
            }
        }
        #region Component Process Functions
        /// <summary>Method that calculates parameters for leaf cohort to appear and then calls event so leaf calss can make cohort appear</summary>
        public void DoLeafTipAppearance()
        {
            TipToAppear += 1;
            CohortParams = new ApparingLeafParams() { };
            CohortParams.CohortToAppear = TipToAppear;
            CohortParams.TotalStemPopn = TotalStemPopn;
            if ((Math.Truncate(LeafTipsAppeared) + 1) == Leaf.InitialisedCohortNo)
                CohortParams.CohortAge = (PotLeafTipsAppeared - TipToAppear) * Phyllochron.Value();
            else
                CohortParams.CohortAge = (LeafTipsAppeared - TipToAppear) * Phyllochron.Value();
            CohortParams.FinalFraction = NextLeafProportion;
            if(LeafTipAppearance != null)
            LeafTipAppearance.Invoke(this, CohortParams);
        }
        /// <summary>Updates the height.</summary>
        public void UpdateHeight()
        {
            _Height = HeightModel.Value();
        }
        /// <summary>Resets the stem popn.</summary>
        public void ResetStemPopn()
        {
            TotalStemPopn = MainStemPopn;
        }
        #endregion

        #region Event Handlers

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (sender == Plant)
                Clear();
            Germinated = false;
            Emerged = false;
            CohortToInitialise = 0;
            TipToAppear = 0;
            PotLeafTipsAppeared = 0;
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">sender of the event.</param>
        /// <param name="Sow">Sowing data to initialise from.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type Sow)
        {
            if (Sow.Plant == Plant)
            {
                Clear();
                if (Sow.MaxCover <= 0.0)
                    throw new Exception("MaxCover must exceed zero in a Sow event.");
                PrimaryBudNo = Sow.BudNumber;
                ApexNum = PrimaryBudNo;
                TotalStemPopn = MainStemPopn;
            }
        }

        /// <summary>Called when crop is being cut.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Cutting")]
        private void OnCutting(object sender, EventArgs e)
        {
            /*PotLeafTipsAppeared = 0;
            CohortToInitialise = 0;
            TipToAppear = 0;
            Emerged = false;
            Clear();
            ResetStemPopn();
            InitialiseLeafCohorts.Invoke(this, args);
            NextLeafProportion = 1.0;
            DoEmergence();
            Emerged = true;*/
        }
        
        /// <summary>Called when crop is being cut.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Harvesting")]
        private void OnHarvesting(object sender, EventArgs e)
        {
            PotLeafTipsAppeared = 0;
            Clear();
            ResetStemPopn();
            Germinated = false;
            Emerged = false;
            CohortToInitialise = 0;
            TipToAppear = 0;
        }

        /// <summary>Called when crop recieves a remove biomass event from manager</summary>
        /// /// <param name="ProportionRemoved">The cultivar.</param>
        public void doThin(double ProportionRemoved)
        {
            Plant.Population *= (1-ProportionRemoved);
            TotalStemPopn *= (1-ProportionRemoved);
            Leaf.DoThin(ProportionRemoved);
        }
        #endregion

        /// <summary>
        /// Document a specific function
        /// </summary>
        /// <param name="FunctName"></param>
        /// <param name="indent"></param>
        /// <param name="tags"></param>
        public void DocumentFunction(string FunctName, List<AutoDocumentation.ITag> tags, int indent)
        {
            IModel Funct = Apsim.Child(this, FunctName);
            Funct.Document(tags, -1, indent);
        }
    }

}