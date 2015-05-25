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
    /// \pre A \ref Models.PMF.Plant "Plant" model has to exist to access 
    /// sowing data, e.g. population, bud number.
    /// \pre A \ref Models.PMF.Organs.Leaf "Leaf" model has to exist to 
    /// access leaf initialisation an number.
    /// \pre A \ref Models.PMF.Phen.Phenology "Phenology" model has to exist 
    /// to check whether plant is initialised.
    /// \param InitialiseStage <b>(Constant)</b> The initialise stage. 
    /// The primordia number, node number and stem population start to 
    /// increase since initialise stage.
    /// \param PrimaryBudNo <b>(Constant)</b> The primary bud number in each node. 
    /// However, PrimaryBudNo is reset at sowing using sowing script 
    /// with default value 1.
    /// 
    /// \param ThermalTime <b>(IFunction)</b> The daily thermal time (<sup>o</sup>Cd).
    /// \param MainStemPrimordiaInitiationRate <b>(IFunction)</b> The initiation rate 
    /// of primordia in main stem (# <sup>o</sup>Cd<sup>-1</sup>).
    /// \param MainStemNodeAppearanceRate <b>(IFunction)</b> The appearance rate 
    /// of node in main stem (# <sup>o</sup>Cd<sup>-1</sup>).
    /// \param MainStemFinalNodeNumber <b>(IFunction)</b> The maximum node number
    /// in main stem (#). The maximum node is calculated by model 
    /// MainStemFinalNodeNumberFunction if MainStemFinalNodeNumber is 
    /// specified to MainStemFinalNodeNumberFunction.
    /// \param HeightModel <b>(IFunction)</b> Plant height (mm).
    /// \param BranchingRate <b>(IFunction)</b> The rate of new branching (#).
    /// \param ShadeInducedBranchMortality <b>(IFunction)</b> 
    /// The branch mortality induced by shading (0-1).
    /// \param DroughtInducedBranchMortality <b>(IFunction)</b> 
    /// The branch mortality induced by drought (0-1).
    /// \param PlantMortality <b>(IFunction, Optional)</b> The plant mortality 
    /// to reduce plant population.
    /// 
    /// \retval TotalStemPopn Number of stems including main and branch stems 
    /// (m<sup>-2</sup>).
    /// \retval MainStemPrimordiaNo Number of primordia initiated in main stem.
    /// \retval MainStemNodeNo Number of nodes appeared in main stem.
    /// \retval PlantTotalNodeNo Number of leaves appeared per plant including 
    /// all main stem and branch leaves.
    /// \retval ProportionBranchMortality Proportion of branch mortality.
    /// \retval ProportionPlantMortality Proportion of plant mortality.
    /// \retval DeltaNodeNumber The daily changes of node number.
    /// 
    /// \retval MainStemPopn Number of main stem per square meter (m<sup>-2</sup>).
    /// \retval RemainingNodeNo Number of leaves yet to appear.
    /// \retval Height Plant height (mm).
    /// \retval PrimaryBudTotalNodeNo Number of appeared bud (leaves) per primary 
    /// bud unit including all main stem and branch leaves.
    /// \retval MainStemFinalNodeNo The main stem final node number.
    /// \retval RelativeNodeApperance Relative progress toward final leaf (0-1).
    /// 
    /// <remarks>
    /// The Structure model predicts the development and mortality of primordia, node (leaf) and branch
    /// (tiller). The primary bud number assumes the same in seed and all axillary bud and set in the 
    /// sowing data with default value 1 (Not sure the assumption is correct).
    /// 
    /// ## Simulation commencing (OnSimulationCommencing)
    /// Local variables are reset to 0 including total stem population, number of 
    /// main stem primordia, main stem node and plant total node;
    /// proportion of branch and plant mortality, changes of node number.
    /// 
    /// ## Sowing (OnPlantSowing)
    /// The primary bud number and population of main stem are initialised from 
    /// sowing date. Primary bud number equals to bud number bud number at 
    /// sowing data (default value equals to 1). The population of main stem
    /// equals to bud number multiplying sowing population.
    /// 
    /// The maximum node (leaf) number in main stem is calculated 
    ///     by MainStemFinalNodeNumber. 
    ///     \ref Models.PMF.Functions.StructureFunctions.MainStemFinalNodeNumberFunction "MainStemFinalNodeNumberFunction" is called if 
    ///     MainStemFinalNodeNumber specifies to MainStemFinalNodeNumberFunction.
    ///     
    /// Plant height is calculated by HeightModel.
    /// 
    /// ## Plant ending (OnPlantEnding)
    /// See simulation commencing
    /// 
    /// ## Potential growth (OnDoPotentialPlantGrowth)
    /// Daily changes of primordia (\f$\Delta N_{p}\f$) and node (leaf) (\f$\Delta N_{n}\f$) 
    /// number in main stem are calculated using plastochron (\f$P_{pla}\f$) 
    /// and phyllochron (\f$P_{phy}\f$), respectively, after plant initialisation.
    /// 
    /// \f[
    ///     \Delta N_{p}=\frac{\Delta TT_{d}}{P_{pla}}
    /// \f]
    /// 
    /// \f[
    ///     \Delta N_{n}=\frac{\Delta TT_{d}}{P_{phy}}
    /// \f]
    /// where, \f$\Delta TT_{d}\f$ is the daily thermal time, which calculates 
    /// from parameter ThermalTime. \f$P_{pla}\f$ and \f$P_{phy}\f$ calculates
    /// from parameter MainStemPrimordiaInitiationRate and MainStemNodeAppearanceRate,
    /// respectively.
    /// 
    /// Total number of primordia \f$N_{p}\f$ and node \f$N_{n}\f$ in main stem are summarised since initialisation.
    /// \f[
    ///     N_{p}=\sum_{t=T_{i}}^{T}\Delta N_{p}
    /// \f]
    /// 
    /// \f[
    ///     N_{n}=\sum_{t=T_{i}}^{T}\Delta N_{n}
    /// \f]
    /// where, \f$T_{i}\f$ is day of plant initialisation. \f$T\f$ is today.
    /// 
    /// Plant population (\f$P\f$) is daily reduced by the plant mortality (\f$\Delta P\f$).
    /// \f[
    ///     P=P_{0}\prod_{t=T_{i}}^{T}(1-\Delta P)
    /// \f]
    /// where, \f$P_{0}\f$ is the sown population, which initialised at sowing.
    /// 
    /// Total stem (main stem + branching) number (\f$N_{s}\f$) is initialised 
    /// according plant population (\f$P\f$) and primary bud number (\f$N_b\f$).
    /// \f[
    ///     N_{s}=P \times N_b
    /// \f]
    /// The total stem number is calculated by the daily increase of branching 
    /// (\f$\Delta N_{s}\f$), and daily decrease caused by drought 
    /// (\f$\Delta N_{drought}\f$) and shade mortality (\f$\Delta N_{shade}\f$).
    /// \f[
    /// N_{s}=PN_{b}[1+\sum_{t=t_{i}}^{T}\Delta N_{s}+\prod_{t=T_{i}}^{T}(1-\Delta N_{drought}-\Delta N_{shade})]
    /// \f]
    /// Daily increase of branching (\f$\Delta N_{s}\f$) is calculated by parameter
    /// BranchingRate. The mortalities, caused by drought (\f$\Delta N_{drought}\f$)  
    /// and shade (\f$\Delta N_{shade}\f$) are calculated by parameters 
    /// DroughtInducedBranchMortality and ShadeInducedBranchMortality, respectively.
    /// 
    /// ## Actual growth (OnDoActualPlantGrowth)
    /// Plant total node number is updated according to appeared node (leaf) number 
    /// and population.
    /// </remarks>
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
        [Units("mm")]
        IFunction HeightModel = null;
        /// <summary>The branching rate</summary>
        [Link]
        [Units("/node")]
        IFunction BranchingRate = null;
        /// <summary>The shade induced branch mortality</summary>
        [Link]
        [Units("0-1")]
        IFunction ShadeInducedBranchMortality = null;
        /// <summary>The drought induced branch mortality</summary>
        [Link]
        [Units("0-1")]
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