using System;
using Models.Core;
using Models.Functions;
using Models.PMF.Phen;
using System.Xml.Serialization;
using Models.Interfaces;
using Models.PMF.Organs;
using APSIM.Shared.Utilities;
using System.Linq;
using Newtonsoft.Json;

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
    public class CulmStructure : Model
    {
        // 1. Links
        //-------------------------------------------------------------------------------------------
        [Link]
        private Plant plant = null;

        [Link]
        private SorghumLeaf leaf = null;

        [Link]
        private Phenology phenology = null;

        //[Link]
        //private Phenology phenology = null;

        /// <summary>The thermal time</summary>
        [Link]
        public IFunction thermalTime = null;

        /// <summary>The main stem final node number</summary>
        [Link]
        public IFunction finalLeafNumber = null;

        /// <summary>Leaf initiation rate.</summary>
        [Link]
        public IFunction LeafInitiationRate = null;

        /// <summary>Number of seeds in leaf?</summary>
        [Link]
        public IFunction LeafNumSeed = null;

        /// <summary>Thermal time from floral init.</summary>
        [Link]
        public IFunction TTFi = null;

        /// <summary>FertileTillerNumber</summary>
        public double FinalLeafNo { get; set; }

        /// <summary>Number of leaves at emergence</summary>
        [Link]
        public IFunction LeafNumAtEmergence = null;

        //[Link]
        //private IFunction heightModel = null;

        [Link]
        private IFunction verticalAdjustment = null;

        [Link]
        private IFunction aTillerVert = null;

        /// <summary>The Initial Appearance rate for phyllocron</summary>
        [Link]
        private IFunction initialAppearanceRate = null;
        /// <summary>The Final Appearance rate for phyllocron</summary>
        [Link]
        private IFunction finalAppearanceRate = null;
        /// <summary>The Final Appearance rate for phyllocron</summary>
        [Link]
        private IFunction remainingLeavesForFinalAppearanceRate = null;

        private bool leavesInitialised;
        private double tillersAdded;
        private bool dayofEmergence;
        private double dltTTDayBefore;

        /// <summary>
        /// Target TT from Emergence to Floral Init.
        /// This is variable and is updated daily.
        /// </summary>
        public double TTTargetFI { get; set; }

        //private double dltLeafNo;

        /// <summary>FertileTillerNumber</summary>
        public double FertileTillerNumber { get; set; }

        /// <summary>Used to match NLeaves in old sorghum which is updated with dltLeafNo at the end of the day</summary>
        //[JsonIgnore]
        public double NLeaves { get; private set; }

        /// <summary>CurrentLeafNo</summary>
        [JsonIgnore]
        public double CurrentLeafNo { get; set; }

        /// <summary>Remaining Leaves</summary>
        public double remainingLeaves { get { return FinalLeafNo - CurrentLeafNo; } }

        /// <summary>The Stage that leaves are initialised on</summary>
        [Description("The Stage that leaves are initialised on")]
        public string LeafInitialisationStage { get; set; } = "Emergence";

        [EventSubscribe("EndOfDay")]
        private void UpdateVars(object sender, EventArgs args)
        {
            // In old apsim, NLeaves is only updated at end of day.
            if (leaf?.Culms.Count > 0)
                NLeaves = leaf.Culms[0].CurrentLeafNumber - leaf.Culms[0].DltNewLeafAppeared;
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type Sow)
        {
            if (Sow.Plant == plant)
            {
                Clear();
                //allow structure to clear leaf on sowing event, otherwise leaf will reset the culms after tthey have been initialised
                leaf.Clear();
                if (Sow.MaxCover <= 0.0)
                    throw new Exception("MaxCover must exceed zero in a Sow event.");
                FertileTillerNumber = Sow.BudNumber;
                //TotalStemPopn = MainStemPopn;
                if (leaf.Culms.Count == 0)
                {
                    //first culm is the main culm
                    leaf.AddCulm(new CulmParameters() {
                        Density = Sow.Population,
                        InitialProportion = 1,
                        InitialAppearanceRate = initialAppearanceRate.Value(),
                        FinalAppearanceRate = finalAppearanceRate.Value(),
                        RemainingLeavesForFinalAppearanceRate = remainingLeavesForFinalAppearanceRate.Value(),
                        AMaxIntercept = leaf.AMaxIntercept.Value(),
                        AMaxSlope = leaf.AMaxSlope.Value(),
                        AX0 = leaf.AX0.Value()
                    });
                }

            }
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        [EventSubscribe("PrePhenology")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (MathUtilities.FloatsAreEqual(TTTargetFI, 0))
                TTTargetFI = GetTTFi();

            if (leavesInitialised)
            {
                if (dayofEmergence)
                {
                    CurrentLeafNo = LeafNumAtEmergence.Value();
                    NLeaves = LeafNumAtEmergence.Value();
                    leaf.Culms[0].CurrentLeafNumber = CurrentLeafNo;
                    dayofEmergence = false;
                }

                // Previously, we didn't call calcLeafAppearance() on day of emergence,
                // however that would not be inline with how old apsim does it.
                //finalLeafNo is calculated upon reference to it as a function
                calcLeafAppearance();
            }

            //old version uses the thermaltime from yesterday to calculate leafAppearance.
            //plant->process() calls leaf->CalcNo before phenology->development()
            //remaining functions use todays... potentially a bug
            dltTTDayBefore = thermalTime.Value();
            TTTargetFI = GetTTFi();
        }

        private double GetTTFi()
        {
            return (double)Apsim.Get(this, "[Phenology].TTEmergToFloralInit.Value()");
        }

        /// <summary>Called when [phase changed].</summary>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == LeafInitialisationStage)
            {
                leavesInitialised = true;
                dayofEmergence = true;
            }
        }

        // 6. Public methods
        //-------------------------------------------------------------------------------------------

            /// <summary>Calculate the number of new leaf that will appear today.</summary>
        void calcLeafAppearance()
        {
            if (leaf?.Culms.Count > 0)
            {
                if (!phenology.Beyond("FloralInitiation"))
                    FinalLeafNo = finalLeafNumber.Value();
                leaf.Culms[0].FinalLeafNumber = FinalLeafNo;
                leaf.Culms[0].calcLeafAppearance(dltTTDayBefore); 

                //MathUtilities.Bound(MathUtilities.Divide(dltTTDayBefore, phyllochron.Value(), 0), 0.0, remainingLeaves);
                var newLeafNo = leaf.Culms[0].CurrentLeafNumber;
                var newL = Math.Floor(newLeafNo);
                var curL = Math.Floor(CurrentLeafNo);
                var newLeafAppeared = (int)Math.Floor(newLeafNo) > (int)Math.Floor(CurrentLeafNo);
                if (newLeafAppeared)
                {
                    calcCulmAppearance((int)Math.Floor(newLeafNo));
                }
                for (var i = 1; i < leaf.Culms.Count; ++i)
                {
                    leaf.Culms[i].FinalLeafNumber = FinalLeafNo;
                    leaf.Culms[i].calcLeafAppearance(dltTTDayBefore);
                }
                CurrentLeafNo = newLeafNo;
            }
        }
        /// <summary>Clears this instance.</summary>
        void calcCulmAppearance(int newLeafNo)
        {
            //if there are still more tillers to add
            //and the newleaf is greater than 3
            if (FertileTillerNumber > tillersAdded)
            {
                //tiller emergence is more closely aligned with tip apearance, but we don't track tip, so will use ligule appearance
                //could also use Thermal Time calcs if needed
                //Environmental && Genotypic Control of Tillering in Sorghum ppt - Hae Koo Kim
                //T2=L3, T3=L4, T4=L5, T5=L6

                //logic to add new tillers depends on which tiller, which is defined by FTN (fertileTillerNo)
                //this should be provided at sowing  //what if fertileTillers == 1?
                //2 tillers = T3 + T4
                //3 tillers = T2 + T3 + T4
                //4 tillers = T2 + T3 + T4 + T5
                //more than that is too many tillers - but will assume existing pattern for 3 and 4
                //5 tillers = T2 + T3 + T4 + T5 + T6

                if (newLeafNo >= 3)
                {
                    //tiller 2 emergences with leaf 3, and then adds 1 each time
                    //not sure what I'm supposed to do with tiller 1
                    //if there are only 2 tillers, then t2 is not present - T3 && T4 are
                    //if there is a fraction - between 2 and 3, 
                    //this can be interpreted as a proportion of plants that have 2 and a proportion that have 3. 
                    //to keep it simple, the fraction will be applied to the 2nd tiller
                    double leafAppearance = tillersAdded + 3; // Culms.size() + 2; //first culm added will equal 3
                    double fraction = 1.0;
                    if (FertileTillerNumber > 2 && FertileTillerNumber < 3 && leafAppearance < 4)
                    {
                        fraction = FertileTillerNumber % 1;
                    }
                    else
                    {
                        if (FertileTillerNumber - tillersAdded < 1)
                            fraction = FertileTillerNumber - tillersAdded;
                    }

                    addTiller(leafAppearance, fraction);

                    //bell curve distribution is adjusted horizontally by moving the curve to the left.
                    //This will cause the first leaf to have the same value as the nth leaf on the main culm.
                    //T3&&T4 were defined during dicussion at initial tillering meeting 27/06/12
                    //all others are an assumption
                    //T2 = 3 Leaves
                    //T3 = 4 Leaves
                    //T4 = 5 leaves
                    //T5 = 6 leaves
                    //T6 = 7 leaves
                }
            }
        }
        void addTiller(double leafAtAppearance, double fractionToAdd)
        {
            // get number if tillers 
            // add fractionToAdd 
            // if new tiller is needed add one
            // fraction goes to proportions

            var nCulms = leaf.Culms.Count;
            var lastCulm = leaf.Culms[nCulms - 1];
            double tillerFraction = lastCulm.Proportion;
            
            double fraction = (tillerFraction % 1) + fractionToAdd;
            //a new tiller is created with each new leaf, up the number of fertileTillers
            if (tillerFraction + fractionToAdd > 1)
            {
                var newCulm = leaf.AddCulm(new CulmParameters()
                {
                    CulmNumber = nCulms,
                    Density = leaf.SowingDensity,
                    InitialProportion = fraction,
                    VerticalAdjustment = tillersAdded * aTillerVert.Value() + verticalAdjustment.Value(), //add aMaxVert in calc
                    LeafNoAtAppearance = leafAtAppearance,
                    InitialAppearanceRate = initialAppearanceRate.Value(),
                    FinalAppearanceRate = finalAppearanceRate.Value(),
                    RemainingLeavesForFinalAppearanceRate = remainingLeavesForFinalAppearanceRate.Value(),
                    AMaxIntercept = leaf.AMaxIntercept.Value(),
                    AMaxSlope = leaf.AMaxSlope.Value(),
                    AX0 = leaf.AX0.Value()
                });
                newCulm.FinalLeafNumber = FinalLeafNo;
                newCulm.calcLeafAppearance(dltTTDayBefore);
                //bell curve distribution is adjusted horizontally by moving the curve to the left.
                //This will cause the first leaf to have the same value as the nth leaf on the main culm.
                //T3&&T4 were defined during dicussion at initial tillering meeting 27/06/12
                //all others are an assumption
                //T2 = 3 Leaves
                //T3 = 4 Leaves
                //T4 = 5 leaves
                //T5 = 6 leaves
                //T6 = 7 leaves
            }
            else
            {
                lastCulm.Proportion = fraction;
            }
            tillersAdded += fractionToAdd;

        }
        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            CurrentLeafNo = 0.0;

            //TotalStemPopn = 0;
            //PotLeafTipsAppeared = 0;
            //PlantTotalNodeNo = 0;
            //ProportionBranchMortality = 0;
            //ProportionPlantMortality = 0;
            //DeltaTipNumber = 0;
            //DeltaHaunStage = 0;
            //leavesInitialised = false;
            //cohortsInitialised = false;
            //firstPass = false;
            //Height = 0;
            //LeafTipsAppeared = 0;
            //BranchNumber = 0;
            //NextLeafProportion = 0;
            //DeltaPlantPopulation = 0;
        }
    }
}
