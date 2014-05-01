using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Organs;
using Models.PMF.Phen;
using System.Xml.Serialization;

namespace Models.PMF
{
    [Serializable]
    [Description("Keeps Track of Plants Structural Development")]
    public class Structure : ModelCollection
    {
        #region Links
        [Link]
        Plant Plant = null;
        [Link]
        Leaf Leaf = null;
        [Link]
        private Phenology Phenology = null;
        #endregion

        #region Parameters
        [Description("The stage name that leaves get initialised.")]
        public string InitialiseStage { get; set; }

        [Description("Number of mainstem units per plant")]
        [Units("/plant")]
        public double PrimaryBudNo {get; set;}

        [Link] Function ThermalTime = null;
        [Link] Function MainStemPrimordiaInitiationRate = null;
        [Link] public Function MainStemNodeAppearanceRate = null;
        [Link] Function MainStemFinalNodeNumber = null;
        [Link] Function HeightModel = null;
        [Link] Function BranchingRate = null;
        [Link] Function ShadeInducedBranchMortality = null;
        [Link] Function DroughtInducedBranchMortality = null;
        [Link(IsOptional=true)] Function PlantMortality = null;
        #endregion

        #region States


        [XmlIgnore]
        [Description("Number of stems per meter including main and branch stems")]
        [Units("/m2")]
        public double TotalStemPopn { get; set; }

        //Plant leaf number state variables

        [XmlIgnore]
        [Description("Number of mainstem primordia initiated")]
        public double MainStemPrimordiaNo { get; set; }

        [XmlIgnore]
        [Description("Number of mainstem nodes appeared")]
        public double MainStemNodeNo { get; set; }

        [XmlIgnore]
        [Units("/plant")]
        [Description("Number of leaves appeared per plant including all main stem and branch leaves")]
        public double PlantTotalNodeNo { get; set; }

        //Utility Variables
        [XmlIgnore]
        public double ProportionBranchMortality { get; set; }
        [XmlIgnore]
        public double ProportionPlantMortality { get; set; }
        [XmlIgnore]
        public int MaximumNodeNumber { get; set; }
        [XmlIgnore]
        public double DeltaNodeNumber { get; set; }

        public void Clear()
        {
            TotalStemPopn = 0;
            MainStemPrimordiaNo = 0;
            MainStemNodeNo = 0;
            PlantTotalNodeNo = 0;
            ProportionBranchMortality = 0;
            ProportionPlantMortality = 0;
            DeltaNodeNumber = 0;
        }

        #endregion

        #region Outputs
        [XmlIgnore]
        [Description("Number of mainstems per meter")]
        [Units("/m2")]
        public double MainStemPopn { get { return Plant.Population * PrimaryBudNo; } }
        
        [Description("Number of leaves yet to appear")]
        public double RemainingNodeNo { get { return MainStemFinalNodeNo - MainStemNodeNo; } }

        [Units("mm")]
        public double Height { get { return HeightModel.Value; } } //This is not protocole compliant.  needs to be changed to a blank get set and hight needs to be set in do potential growth 

        [XmlIgnore]
        [Units("/PrimaryBud")]
        [Description("Number of appeared leaves per primary bud unit including all main stem and branch leaves")]
        public double PrimaryBudTotalNodeNo { get { return PlantTotalNodeNo / PrimaryBudNo; } }

        [XmlIgnore]
        [Description("Number of leaves that will appear on the mainstem before it terminates")]
        public double MainStemFinalNodeNo { get { return MainStemFinalNodeNumber.Value; } } //Fixme.  this property is not needed as this value can be obtained dirrect from the function.  Not protocole compliant.  Remove.

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

            MainStemFinalNodeNumber.UpdateVariables("");
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
                TotalStemPopn += BranchingRate.Value * MainStemPopn;
            }

            //Reduce plant population incase of mortality
            if (PlantMortality != null)
            {
                double DeltaPopn = Plant.Population * PlantMortality.Value;
                Plant.Population -= DeltaPopn;
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
                double DeltaPopn = Math.Min(PropnMortality * (TotalStemPopn - MainStemPopn), TotalStemPopn - Plant.Population);
                TotalStemPopn -= DeltaPopn;
                ProportionBranchMortality = PropnMortality;
            }
        }
        public void DoActualGrowth()
        {
            //Set PlantTotalNodeNo    
            PlantTotalNodeNo = Leaf.PlantAppearedLeafNo / Plant.Population;
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

        public override void OnCommencing()
        {
            Clear();

        }
        public void OnSow(SowPlant2Type Sow)
        {
            Clear();
            if (Sow.MaxCover <= 0.0)
                throw new Exception("MaxCover must exceed zero in a Sow event.");
            PrimaryBudNo = Sow.BudNumber;
            TotalStemPopn = Sow.Population * PrimaryBudNo;
            string initial = "yes";
            MainStemFinalNodeNumber.UpdateVariables(initial);
            MaximumNodeNumber = (int)MainStemFinalNodeNumber.Value;
        }
        #endregion
    }

}