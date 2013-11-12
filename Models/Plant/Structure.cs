using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Plant.Functions;
using Models.Plant.Organs;
using Models.Plant.Phen;

namespace Models.Plant
{
    [Description("Keeps Track of Plants Structural Development")]
    public class Structure
    {
        #region Class Dependency Links
        [Link]
        protected Function ThermalTime = null;
        [Link]
        Leaf Leaf = null;
        [Link]
        public Phenology Phenology = null;
        #endregion

        #region Class Parameter Function Links
        [Link(NamePath = "MainStemPrimordiaInitiationRate")]
        public Function MainStemPrimordiaInitiationRate = null;
        [Link(NamePath = "MainStemNodeAppearanceRate")]
        public Function MainStemNodeAppearanceRate = null;
        [Link(NamePath = "MainStemFinalNodeNumber")]
        public Function MainStemFinalNodeNumberFunction = null;
        [Link(NamePath = "Height")]
        public Function HeightModel = null;
        [Link(NamePath = "BranchingRate")]
        public Function Branching = null;
        [Link(NamePath = "ShadeInducedBranchMortality", IsOptional = true)]
        public Function ShadeInducedBranchMortality = null;
        [Link(NamePath = "DroughtInducedBranchMortality", IsOptional = true)]
        public Function DroughtInducedBranchMortality = null;
        [Link(NamePath = "PlantMortality", IsOptional = true)]
        public Function PlantMortality = null;

        #endregion

        #region Class Parameter Fields
        
        [Description("The stage name that leaves get initialised.")]
        public string InitialiseStage = "";
        #endregion

        #region Class Properties
        //Population state variables
        
        [Description("Number of plants per meter2")]
        [Units("/m2")]
        public double Population { get; set; }
        
        [Description("Number of mainstem units per plant")]
        [Units("/plant")]
        public double PrimaryBudNo = 1;
        [Description("Number of mainstems per meter")]
        [Units("/m2")]
        public double MainStemPopn { get { return Population * PrimaryBudNo; } }
        
        [Description("Number of stems per meter including main and branch stems")]
        [Units("/m2")]
        public double TotalStemPopn { get; set; }

        //Plant leaf number state variables
        
        [Description("Number of mainstem primordia initiated")]
        public double MainStemPrimordiaNo { get; set; }
        
        [Description("Number of mainstem nodes appeared")]
        public double MainStemNodeNo { get; set; }
        
        [Units("/plant")]
        [Description("Number of leaves appeared per plant including all main stem and branch leaves")]
        public double PlantTotalNodeNo { get; set; }
        
        [Units("/PrimaryBud")]
        [Description("Number of appeared leaves per primary bud unit including all main stem and branch leaves")]
        public double PrimaryBudTotalNodeNo { get { return PlantTotalNodeNo / PrimaryBudNo; } }
        
        [Description("Number of leaves that will appear on the mainstem before it terminates")]
        public double MainStemFinalNodeNo { get { return MainStemFinalNodeNumberFunction.Value; } } //Fixme.  this property is not needed as this value can be obtained dirrect from the function.  Not protocole compliant.  Remove.
        
        [Units("0-1")]
        [Description("Relative progress toward final leaf")]
        public double RelativeNodeApperance
        {
            get
            {
                if (Leaf.CohortsInitialised == false) //FIXME introduced to removed colateral damage during testing.  Need to remove and fix max leaf area parameterisation in potato.xml
                    return 0;
                else
                    return MainStemNodeNo / MainStemFinalNodeNo;
            }
        }
        
        [Description("Number of leaves yet to appear")]
        public double RemainingNodeNo { get { return MainStemFinalNodeNo - MainStemNodeNo; } }

        //Utility Variables
        [Units("mm")]
        //public double Height { get; set; }
        public double Height { get { return HeightModel.Value; } } //This is not protocole compliant.  needs to be changed to a blank get set and hight needs to be set in do potential growth 
        public double ProportionBranchMortality { get; set; }
        public double ProportionPlantMortality { get; set; }
        public double MaximumNodeNumber { get; set; }
        public double DeltaNodeNumber { get; set; }
        #endregion

        #region Top level timestep Functions
        public void DoPotentialDM()
        {
            if (Phenology.OnDayOf(InitialiseStage) == false) // We have no leaves set up and nodes have just started appearing - Need to initialise Leaf cohorts
                if (MainStemPrimordiaInitiationRate.Value > 0.0)
                {
                    MainStemPrimordiaNo += ThermalTime.Value / MainStemPrimordiaInitiationRate.Value;
                }

            double StartOfDayMainStemNodeNo = (int)MainStemNodeNo;

            MainStemFinalNodeNumberFunction.UpdateVariables("");
            MainStemPrimordiaNo = Math.Min(MainStemPrimordiaNo, MaximumNodeNumber);

            if (MainStemNodeNo > 0)
            {
                DeltaNodeNumber = 0;
                if (MainStemNodeAppearanceRate.Value > 0)
                    DeltaNodeNumber = ThermalTime.Value / MainStemNodeAppearanceRate.Value;
                MainStemNodeNo += DeltaNodeNumber;
                MainStemNodeNo = Math.Min(MainStemNodeNo, MainStemFinalNodeNo);
            }

            //Fixme  This is redundant now and could be removed
            //Set stem population at emergence
            if (Phenology.OnDayOf(InitialiseStage))
            {
                TotalStemPopn = MainStemPopn;
            }

            double InitialStemPopn = TotalStemPopn;

            //Increment total stem population if main-stem node number has increased by one.
            if ((MainStemNodeNo - StartOfDayMainStemNodeNo) >= 1.0)
            {
                TotalStemPopn += Branching.Value * MainStemPopn;
            }

            //Reduce plant population incase of mortality
            if (PlantMortality != null)
            {
                double DeltaPopn = Population * PlantMortality.Value;
                Population -= DeltaPopn;
                TotalStemPopn -= DeltaPopn;
                ProportionPlantMortality = PlantMortality.Value;
            }

            //Reduce stem number incase of mortality
            double PropnMortality = 0;
            if (DroughtInducedBranchMortality != null)
                PropnMortality = DroughtInducedBranchMortality.Value;
            if (ShadeInducedBranchMortality != null)
                PropnMortality += ShadeInducedBranchMortality.Value;
            {
                double DeltaPopn = Math.Min(PropnMortality * (TotalStemPopn - MainStemPopn), TotalStemPopn - Population);
                TotalStemPopn -= DeltaPopn;
                ProportionBranchMortality = PropnMortality;
            }
        }
        public void DoActualGrowth()
        {
            //Set PlantTotalNodeNo    
            double n = 0;
            foreach (LeafCohort L in Leaf.Leaves)
                if (L.IsAppeared)
                    n += L.CohortPopulation;
            PlantTotalNodeNo = n / Population;
        }
        #endregion

        #region Component Process Functions
        public void UpdateHeight()
        {
            HeightModel.UpdateVariables("");
        }
        public void ResetStemPopn()
        {
            TotalStemPopn = MainStemPopn;
        }
        #endregion

        #region Event Handlers
        public void Clear()
        {
            MainStemNodeNo = 0;
            MainStemPrimordiaNo = 0;
            TotalStemPopn = 0;
        }
        [EventSubscribe("Initialised")]
        private void OnInitialised(object sender, EventArgs e)
        {
            string initial = "yes";
            MainStemFinalNodeNumberFunction.UpdateVariables(initial);
            MaximumNodeNumber = MainStemFinalNodeNumberFunction.Value;
        }
        [EventSubscribe("Sow")]
        private void OnSow(SowPlant2Type Sow)
        {
            if (Sow.MaxCover <= 0.0)
                throw new Exception("MaxCover must exceed zero in a Sow event.");
            PrimaryBudNo = Sow.BudNumber;
            Population = Sow.Population;
            TotalStemPopn = Population * PrimaryBudNo;

        }
        #endregion
    }

}