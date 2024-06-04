using System;
using Models.Core;
using Models.PMF;
using Models.PMF.Phen;
using Models.PMF.Organs;
using Models.Climate;

namespace Models.Functions
{
    /// <summary> Damage functions of frost and heat stress. </summary>
    [Serializable]
    [Description("Damage functions of frost and heat stresses are provided for barley, canola and wheat by the GRDC FAHMA project.")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class FrostHeatDamageFunctions : Model
    {
        //[Link]
        //Clock Clock;
        [Link]
        Weather Weather = null;
        [Link]
        Zone zone = null;
        [Link]
        Plant Plant = null;

        // Define parameters

        /// <summary>Crop to be simulated</summary>
        [Separator("Crop to be simulated, wheat or canola?")]
        // <summary>Crop to be simulated</summary>
        [Description("Crop to be simulated")]
        public string CropType { get; set; }
      
        /// <summary>Frost damage</summary>
        [Separator("Frost damage")]
        // <summary>Lower thereshold</summary>
        [Description("Lower threshold of air temperature for frost damage")]
        public double FrostLowTT { get; set; }

        /// <summary>Yield reduction at lower threshold</summary>
        [Description("Yield reduction ratio of frost damage induced by lower threshold")]
        public double FrostMaxReductionRatio { get; set; }

        /// <summary>Upper threshold</summary>
        [Description("Upper threshold of air temperature for frost damage")]
        public double FrostUpTT { get; set; }

        /// <summary>Yield reduction at upper threshold</summary>
        [Description("Yield reduction ratio frost damage induced by upper threshold")]
        public double FrostMinReductionRatio { get; set; }


        /// <summary>Sensitive period of frost damage</summary>
        [Separator("Growth stages to define the sensitive period of frost damage")]
        // <summary>The start of sensitive period of frost damage</summary>
        [Description("Start of sensitive period")]
        public double FrostStartSensitiveGS { get; set; }

        /// <summary>The start of the most sensitive period of frost damage</summary>
        [Description("Start of the most sensitive period (i.e., when sensitivity = 1)")]
        public double FrostStartMostSensitiveGS { get; set; }

        /// <summary>The end of the most sensitive period of frost damagee</summary>
        [Description("End of the most sensitive period (i.e., when sensitivity = 1)")]
        public double FrostEndMostSensitiveGS { get; set; }

        /// <summary>The end of sensitive period of frost damage</summary>
        [Description("End of sensitive period")]
        public double FrostEndSensitiveGS { get; set; }


        /// <summary>Heat damage</summary>
        [Separator("Heat damage")]
        // <summary>Lower threshold</summary>
        [Description("Lower threshold of air temperature for heat damage")]
        public double HeatLowTT { get; set; }

        /// <summary>Yield reduction at lower threshold</summary>
        [Description("Yield reduction ratio of heat damage induced by lower threshold")]
        public double HeatMinReductionRatio { get; set; }

        /// <summary>Upper threshold</summary>
        [Description("Upper threshold of air temperature for heat damage")]
        public double HeatUpTT { get; set; }

        /// <summary>Yield reduction at upper threshold</summary>
        [Description("Yield reduction ratio of heat damage induced by upper threshold")]
        public double HeatMaxReductionRatio { get; set; }


        /// <summary>Sensitivity period of heat damage</summary>
        [Separator("Growth stages to define the sensitivity period of heat damage")]
        // <summary>The start of sensitive period of heat damage</summary>
        [Description("Start of sensitive period")]
        public double HeatStartSensitiveGS { get; set; }

        /// <summary>The start of the most sensitive period of heat damage</summary>
        [Description("Start of the most sensitive period (i.e., when sensitivity = 1)")]
        public double HeatStartMostSensitiveGS { get; set; }

        /// <summary>The end of the most sensitive period</summary>
        [Description("End of the most sensitive period (i.e., when sensitivity = 1)")]
        public double HeatEndMostSensitiveGS { get; set; }

        /// <summary>The end of sensitive period of heat damage</summary>
        [Description("End of sensitive period")]
        public double HeatEndSensitiveGS { get; set; }


        // Internal variables
        //private string CropType;

        /// <summary>Overall remainng ratio after frost events.</summary>
        double FrostOverallRemaining;

        /// <summary>Overall remainng ratio after heat events.</summary>
        double HeatOverallRemaining;


        // Output variables
        /// <summary>Daily potential yield reduction ratio by a frost event.</summary>
        public double FrostPotentialReductionRatio { get; set; }

        /// <summary>Daily sensitivity of yield reduction to growth stage when the frost event occurs.</summary>
        public double FrostSensitivity { get; set; }

        /// <summary>Daily actual yield reduction ratio by frost stress.</summary>
        public double FrostReductionRatio { get; set; }

        /// <summary>Daily potential yiled reduction ratio by a heat event.</summary>
        public double HeatPotentialReductionRatio { get; set; }

        /// <summary>Daily sensitivity of yield reduction to growth stage when the heat event occurs.</summary>
        public double HeatSensitivity { get; set; }  
              
        /// <summary>Daily actual yield reduction ratio by heat stress.</summary>
        public double HeatReductionRatio { get; set; }

        /// <summary>Daily actual yield reduction ratio by frost and heat stress.</summary>
        public double FrostHeatReductionRatio { get; set; }

        /// <summary>Cumulative actual yield reduction ratio induced by frost stress.</summary>
        public double CumulativeFrostReductionRatio { get; set; }

        /// <summary>Cumulative actual yield reduction ratio induced by heat stress.</summary>
        public double CumulativeHeatReductionRatio { get; set; }

        /// <summary>Cumulative actual yield reduction ratio induced by frost and heat stress.</summary>
        public double CumulativeFrostHeatReductionRatio { get; set; }

        /// <summary>Frost- and heat-limiated yield.</summary>
        /// [Units("g/m2")]
        public double FrostHeatYield { get; set; }

        [EventSubscribe("Sowing")]
        private void OnDoSowing(object sender, EventArgs e)
        {
            // initialize
            //CropType = Plant.PlantType;
            //CropType = PlantType.PlantType;

            FrostPotentialReductionRatio = 0;
            FrostSensitivity = 0;
            FrostReductionRatio = 0;

            HeatPotentialReductionRatio = 0;
            HeatSensitivity = 0;
            HeatReductionRatio = 0;

            FrostHeatReductionRatio = 0;

            FrostOverallRemaining = 1;
            HeatOverallRemaining = 1;

            CumulativeFrostReductionRatio = 0;
            CumulativeHeatReductionRatio = 0;
            CumulativeFrostHeatReductionRatio = 0;
            FrostHeatYield = 0;
        }

        /// <summary>Caculates daily potential yield reduction ratio induced by a frost event.</summary>
        private double FrostPotentialReductionRatioFun(double t)
        {
            double ratio = 0.0;

            if (t >= FrostUpTT)
            {
                //ratio = 0.0d;
                ratio = FrostMinReductionRatio;
            }
            else if (t > FrostLowTT && t < FrostUpTT)
            {
                ratio =
                    t * ((FrostMinReductionRatio - FrostMaxReductionRatio) / (FrostUpTT - FrostLowTT))
                    + (FrostMaxReductionRatio * FrostUpTT - FrostLowTT * FrostMinReductionRatio)
                        / (FrostUpTT - FrostLowTT);
            }
            else if (t <= FrostLowTT)
            {
                ratio = FrostMaxReductionRatio;
            }
            return ratio;
        }

        /// <summary>Caculates daily sensitivity of yield reduction to growth stage when the frost event occurs.</summary>
        private double FrostSensitivityFun(double GrowthStage)
        {
            double sens = 0.0;

            if (GrowthStage <= FrostStartSensitiveGS)
            {
                sens = 0;
            }
            else if (GrowthStage > FrostStartSensitiveGS && GrowthStage < FrostStartMostSensitiveGS)
            {
                sens =
                    GrowthStage * ((1 - 0) / (FrostStartMostSensitiveGS - FrostStartSensitiveGS))
                    + (0 * FrostStartMostSensitiveGS - FrostStartSensitiveGS * 1) / (FrostStartMostSensitiveGS - FrostStartSensitiveGS);
            }
            else if (GrowthStage >= FrostStartMostSensitiveGS && GrowthStage <= FrostEndMostSensitiveGS)
            {
                sens = 1.0;
            }
            else if (GrowthStage > FrostEndMostSensitiveGS && GrowthStage < FrostEndSensitiveGS)
            {
                sens =
                    GrowthStage * ((0 - 1) / (FrostEndSensitiveGS - FrostEndMostSensitiveGS))
                    + (1 * FrostEndSensitiveGS - FrostEndMostSensitiveGS * 0) / (FrostEndSensitiveGS - FrostEndMostSensitiveGS);
            }
            else if (GrowthStage >= FrostEndSensitiveGS)
            {
                sens = 0;
            }
            return sens;
        }

        /// <summary>Caculates daily potential yield reduction ratio incuded by a heat event.</summary>
        private double HeatPotentialReductionRatioFun(double t)
        {
            double ratio = 0.0;

            if (t <= HeatLowTT)
            {
                //ratio = 0.0;
                ratio = HeatMinReductionRatio;
            }
            else if (t > HeatLowTT && t < HeatUpTT)
            {
                ratio =
                    t * ((HeatMaxReductionRatio - HeatMinReductionRatio) / (HeatUpTT - HeatLowTT))
                    + (HeatMinReductionRatio * HeatUpTT - HeatLowTT * HeatMaxReductionRatio)
                        / (HeatUpTT - HeatLowTT);
            }
            else if (t >= HeatUpTT)
            {
                ratio = HeatMaxReductionRatio;
            }
            return ratio;
        }

        /// <summary>Caculates daily sensitivity of yield reduction when the heat event occurs.</summary>
        private double HeatSensitivityFun(double GrowthStage)
        {
            double sens = 0;

            if (GrowthStage <= HeatStartSensitiveGS)
            {
                sens = 0;
            }
            else if (GrowthStage > HeatStartSensitiveGS && GrowthStage < HeatStartMostSensitiveGS)
            {
                sens =
                    GrowthStage * ((1 - 0) / (HeatStartMostSensitiveGS - HeatStartSensitiveGS))
                    + (0 * HeatStartMostSensitiveGS - HeatStartSensitiveGS * 1) / (HeatStartMostSensitiveGS - HeatStartSensitiveGS);
            }
            else if (GrowthStage >= HeatStartMostSensitiveGS && GrowthStage <= HeatEndMostSensitiveGS)
            {
                sens = 1.0;
            }
            else if (GrowthStage > HeatEndMostSensitiveGS && GrowthStage < HeatEndSensitiveGS)
            {
                sens =
                    GrowthStage * ((0 - 1) / (HeatEndSensitiveGS - HeatEndMostSensitiveGS))
                    + (1 * HeatEndSensitiveGS - HeatEndMostSensitiveGS * 0) / (HeatEndSensitiveGS - HeatEndMostSensitiveGS);
            }
            else if (GrowthStage >= HeatEndSensitiveGS)
            {
                sens = 0;
            }
            return sens;
        }

        /// <summary>Does the calculations of multiplers and sensitivities of frost and heat stresses.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoManagementCalculations")]
        private void OnDoManagementCalculations(object sender, EventArgs e)
        {
            if (!Plant.IsAlive)
            {
                return;
            }

            Phenology phen = (Phenology)zone.Get("[" + CropType + "].Phenology");
            ReproductiveOrgan organs = (ReproductiveOrgan)zone.Get("[" + CropType + "].Grain");

            double GrowthStageToday = 0;
            if (CropType == "Wheat" | CropType == "wheat" | CropType == "Barley" | CropType == "barley" | CropType == "Canola" | CropType == "canola")
            {
                //GrowthStageToday = phen.Zadok;
                GrowthStageToday = phen.Stage;
            }
            //else if (CropType == "Canola" | CropType == "canola")
            //{
            //    GrowthStageToday = phen.Stage;
            //}
            else
            {
                throw new Exception("Crop type not supported!");
            }

            // Daily potential yield reduction ratio by a frost event
            FrostPotentialReductionRatio = FrostPotentialReductionRatioFun(Weather.MinT);

            // Daily sensitivity of yield reduction to the growth stage when a frost event occurs
            FrostSensitivity = FrostSensitivityFun(GrowthStageToday);

            // Daily actual yield reduction by a frost event
            FrostReductionRatio = FrostPotentialReductionRatio * FrostSensitivity;

            // Daily potential yield reduction by a heat event
            HeatPotentialReductionRatio = HeatPotentialReductionRatioFun(Weather.MaxT);

            // Daily sensitivity of yield reduction to the growth stage when a heat frost event occurs
            HeatSensitivity = HeatSensitivityFun(GrowthStageToday);

            // Daily actual yield reduction by a heat event
            HeatReductionRatio = HeatPotentialReductionRatio * HeatSensitivity;

            // Daily actual yield reduction by the frost and heat events
            FrostHeatReductionRatio = 1 - (1 - FrostReductionRatio) * (1 - HeatReductionRatio);

            // Cumulative yield reduction by frost events
            FrostOverallRemaining = FrostOverallRemaining * (1 - FrostReductionRatio);
            CumulativeFrostReductionRatio = 1 - FrostOverallRemaining;

            // Cumulative yield reduction by heat events
            HeatOverallRemaining = HeatOverallRemaining * (1 - HeatReductionRatio);
            CumulativeHeatReductionRatio = 1 - HeatOverallRemaining;

            // Cumulative yield reduction by frost and heat events
            CumulativeFrostHeatReductionRatio = 1 - FrostOverallRemaining * HeatOverallRemaining;

            // Frost- and heat-limited yield
            FrostHeatYield = organs.Wt * FrostOverallRemaining * HeatOverallRemaining;
        }
    }
}
