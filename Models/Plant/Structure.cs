using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Organs;
using Models.PMF.Phen;
using System.Xml.Serialization;
using Models.PMF.Functions.StructureFunctions;

namespace Models.PMF
{
    /// <summary>
    /// A structure model for plant
    /// </summary>
    [Serializable]
    [Description("Keeps Track of Plants Structural Development")]
    public class Structure : Model
    {
        private double _MainStemFinalNodeNo;
        private double _Height;

        #region Links
        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;
        /// <summary>The leaf</summary>
        [Link]
        Leaf Leaf = null;
        /// <summary>The phenology</summary>
        [Link]
        private Phenology Phenology = null;
        #endregion

        #region Parameters
        /// <summary>Gets or sets the initialise stage.</summary>
        /// <value>The initialise stage.</value>
        [Description("The stage name that leaves get initialised.")]
        public string InitialiseStage { get; set; }

        /// <summary>Gets or sets the primary bud no.</summary>
        /// <value>The primary bud no.</value>
        [Description("Number of mainstem units per plant")]
        [Units("/plant")]
        public double PrimaryBudNo {get; set;}

        /// <summary>The thermal time</summary>
        [Link]
        IFunction ThermalTime = null;
        /// <summary>The main stem primordia initiation rate</summary>
        [Link]
        IFunction MainStemPrimordiaInitiationRate = null;
        /// <summary>The main stem node appearance rate</summary>
        [Link]
        public IFunction MainStemNodeAppearanceRate = null;
        /// <summary>The main stem final node number</summary>
        [Link]
        public IFunction MainStemFinalNodeNumber = null;
        /// <summary>The height model</summary>
        [Link]
        IFunction HeightModel = null;
        /// <summary>The branching rate</summary>
        [Link]
        IFunction BranchingRate = null;
        /// <summary>The shade induced branch mortality</summary>
        [Link]
        IFunction ShadeInducedBranchMortality = null;
        /// <summary>The drought induced branch mortality</summary>
        [Link]
        IFunction DroughtInducedBranchMortality = null;
        /// <summary>The plant mortality</summary>
        [Link(IsOptional = true)]
        IFunction PlantMortality = null;
        #endregion

        #region States


        /// <summary>Gets or sets the total stem popn.</summary>
        /// <value>The total stem popn.</value>
        [XmlIgnore]
        [Description("Number of stems per meter including main and branch stems")]
        [Units("/m2")]
        public double TotalStemPopn { get; set; }

        //Plant leaf number state variables

        /// <summary>Gets or sets the main stem primordia no.</summary>
        /// <value>The main stem primordia no.</value>
        [XmlIgnore]
        [Description("Number of mainstem primordia initiated")]
        public double MainStemPrimordiaNo { get; set; }

        /// <summary>Gets or sets the main stem node no.</summary>
        /// <value>The main stem node no.</value>
        [XmlIgnore]
        [Description("Number of mainstem nodes appeared")]
        public double MainStemNodeNo { get; set; }

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
        /// <summary>Gets or sets the maximum node number.</summary>
        /// <value>The maximum node number.</value>
        [XmlIgnore]
        public int MaximumNodeNumber { get; set; }
        /// <summary>Gets or sets the delta node number.</summary>
        /// <value>The delta node number.</value>
        [XmlIgnore]
        public double DeltaNodeNumber { get; set; }

        /// <summary>Clears this instance.</summary>
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
        /// <summary>Gets the main stem popn.</summary>
        /// <value>The main stem popn.</value>
        [XmlIgnore]
        [Description("Number of mainstems per meter")]
        [Units("/m2")]
        public double MainStemPopn { get { return Plant.Population * PrimaryBudNo; } }

        /// <summary>Gets the remaining node no.</summary>
        /// <value>The remaining node no.</value>
        [Description("Number of leaves yet to appear")]
        public double RemainingNodeNo { get { return MainStemFinalNodeNo - MainStemNodeNo; } }

        /// <summary>Gets the height.</summary>
        /// <value>The height.</value>
        [Units("mm")]
        public double Height { get { return _Height; } } 

        /// <summary>Gets the primary bud total node no.</summary>
        /// <value>The primary bud total node no.</value>
        [XmlIgnore]
        [Units("/PrimaryBud")]
        [Description("Number of appeared leaves per primary bud unit including all main stem and branch leaves")]
        public double PrimaryBudTotalNodeNo { get { return PlantTotalNodeNo / PrimaryBudNo; } }

        /// <summary>Gets the main stem final node no.</summary>
        /// <value>The main stem final node no.</value>
        [XmlIgnore]
        [Description("Number of leaves that will appear on the mainstem before it terminates")]
        public double MainStemFinalNodeNo { get { return _MainStemFinalNodeNo; } } 

        /// <summary>Gets the relative node apperance.</summary>
        /// <value>The relative node apperance.</value>
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
        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Phenology.OnDayOf(InitialiseStage) == false) // We have no leaves set up and nodes have just started appearing - Need to initialise Leaf cohorts
                if (MainStemPrimordiaInitiationRate.Value > 0.0)
                {
                    MainStemPrimordiaNo += ThermalTime.Value / MainStemPrimordiaInitiationRate.Value;
                }

            double StartOfDayMainStemNodeNo = (int)MainStemNodeNo;

            _MainStemFinalNodeNo = MainStemFinalNodeNumber.Value;
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
        /// <summary>Does the actual growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantPartioning")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            //Set PlantTotalNodeNo    
            PlantTotalNodeNo = Leaf.PlantAppearedLeafNo / Plant.Population;
        }
        #endregion

        #region Component Process Functions
        /// <summary>Updates the height.</summary>
        public void UpdateHeight()
        {
            _Height = HeightModel.Value;
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
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="Sow">Sowing data to initialise from.</param>
        [EventSubscribe("Sowing")]
        private void OnSowing(object sender, SowPlant2Type Sow)
        {
            if (Sow.Plant == Plant)
            {
                Clear();
                if (Sow.MaxCover <= 0.0)
                    throw new Exception("MaxCover must exceed zero in a Sow event.");
                PrimaryBudNo = Sow.BudNumber;
                TotalStemPopn = Sow.Population * PrimaryBudNo;
                if (MainStemFinalNodeNumber is MainStemFinalNodeNumberFunction)
                    _MainStemFinalNodeNo = (MainStemFinalNodeNumber as MainStemFinalNodeNumberFunction).MaximumMainStemNodeNumber;
                else
                    _MainStemFinalNodeNo = MainStemFinalNodeNumber.Value;
                MaximumNodeNumber = (int)_MainStemFinalNodeNo;
                _Height = HeightModel.Value;
            }
        }
        
        #endregion
    }

}