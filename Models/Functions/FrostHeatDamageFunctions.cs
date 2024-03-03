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

        // Internal variables
        //private string CropType;

        // Define parameters

        /// <summary>Crop to be simulated</summary>
        [Separator("Crop to be simulated, barley, wheat or canola?")]
        // <summary>Crop to be simulated</summary>
        [Description("Crop to be simulated")]
        public string CropType { get; set; }
      
        /// <summary>Frost damage</summary>
        [Separator("Frost damage")]
        // <summary>Lower thereshold</summary>
        [Description("Lower threshold of temperature for frost damage")]
        public double FrostSurvLowT { get; set; }

        /// <summary>Multiplier of lower threshold</summary>
        [Description("Multiplier of frost damage for lower threshold of temperature")]
        public double FrostSurvLowR { get; set; }

        /// <summary>Upper threshold</summary>
        [Description("Upper threshold of temperature for frost damage")]
        public double FrostSurvUpT { get; set; }

        /// <summary>Multiplier of upper threshold</summary>
        [Description("Multiplier of frost damage for upper threshold of temperature")]
        public double FrostSurvUpR { get; set; }


        /// <summary>Sensitivity period of frost damage</summary>
        [Separator("Growth stages to define the sensitivity period of frost damage")]
        // <summary>The first growth stage</summary>
        [Description("The first growth stage")]
        public double FrostSensGrowthStageFirst { get; set; }

        /// <summary>The second growth stage</summary>
        [Description("The second growth stage")]
        public double FrostSensGrowthStageSecond { get; set; }

        /// <summary>The third growth stage</summary>
        [Description("The third growth stage")]
        public double FrostSensGrowthStageThird { get; set; }

        /// <summary>The fourth growth stage</summary>
        [Description("The fourth growth stage")]
        public double FrostSensGrowthStageFourth { get; set; }


        /// <summary>Heat damage</summary>
        [Separator("Heat damage")]
        // <summary>Lower threshold</summary>
        [Description("Lower threshold of temperature for heat damage")]
        public double HeatSurvLowT { get; set; }

        /// <summary>Multiplier of lower threshold</summary>
        [Description("Multiplier of heat damage for lower threshold of temperature")]
        public double HeatSurvLowR { get; set; }

        /// <summary>Upper threshold</summary>
        [Description("Upper threshold of temperature for heat damage")]
        public double HeatSurvUpT { get; set; }

        /// <summary>Multiplier of upper threshold</summary>
        [Description("Multiplier of heat damage for upper threshold of temperature")]
        public double HeatSurvUpR { get; set; }


        /// <summary>Sensitivity period of heat damage</summary>
        [Separator("Growth stages to define the sensitivity period of heat damage")]
        // <summary>The first growth stage</summary>
        [Description("The first growth stage")]
        public double HeatSensGrowthStageFirst { get; set; }

        /// <summary>The second growth stage</summary>
        [Description("The second growth stage")]
        public double HeatSensGrowthStageSecond { get; set; }

        /// <summary>The third growth stage</summary>
        [Description("The third growth stage")]
        public double HeatSensGrowthStageThird { get; set; }

        /// <summary>The fourth growth stage</summary>
        [Description("The fourth growth stage")]
        public double HeatSensGrowthStageFourth { get; set; }


        // Output variables

        /// <summary>Daily multiplier of frost damage.</summary>
        public double FrostSurv { get; set; }

        /// <summary>Daily sensitivity of frost stress.</summary>
        public double FrostSens { get; set; }

        /// <summary>Daily multiplier of heat damage.</summary>
        public double HeatSurv { get; set; }

        /// <summary>Daily sensitivity of heat stress.</summary>
        public double HeatSens { get; set; }

        /// <summary>Daily multiplier of frost and heat combined damage.</summary>
        public double FrostHeatCombinedSurv { get; set; }

        /// <summary>Final multiplier of frost and heat damage.</summary>
        public double FinalStressMultiplier { get; set; }

        /// <summary>Final multiplier of frost damage.</summary>
        public double FinalFrostMultiplier { get; set; }

        /// <summary>Final multiplier of heat damage.</summary>
        public double FinalHeatMultiplier { get; set; }

        /// <summary>Frost- and heat-limiated yield.</summary>
        /// [Units("g/m2")]
        public double FrostHeatYield { get; set; }

        [EventSubscribe("Sowing")]
        private void OnDoSowing(object sender, EventArgs e)
        {
            // initialize
            //CropType = Plant.PlantType;
            //CropType = PlantType.PlantType;

            FinalStressMultiplier = 1.0;
            FinalFrostMultiplier = 1.0;
            FinalHeatMultiplier = 1.0;
            FrostHeatYield = 0;

            FrostSurv = 1.0;
            FrostSens = 0;
            HeatSurv = 1.0;
            HeatSens = 0;
            FrostHeatCombinedSurv = 1.0;
        }

        /// <summary>Caculates daily multiplier of frost stress.</summary>
        private double FrostSurvFun(double t)
        {
            double ratio = 0.0;

            if (t >= FrostSurvUpT)
            {
                ratio = 1.0d;
            }
            else if (t > FrostSurvLowT && t < FrostSurvUpT)
            {
                ratio =
                    t * ((FrostSurvUpR - FrostSurvLowR) / (FrostSurvUpT - FrostSurvLowT))
                    + (FrostSurvLowR * FrostSurvUpT - FrostSurvLowT * FrostSurvUpR)
                        / (FrostSurvUpT - FrostSurvLowT);
            }
            else if (t <= FrostSurvLowT)
            {
                ratio = FrostSurvLowR;
            }
            return ratio;
        }

        /// <summary>Caculates daily sensitivity of frost stress.</summary>
        private double FrostSensFun(double GrowthStage)
        {
            double sens = 0.0;

            if (GrowthStage <= FrostSensGrowthStageFirst)
            {
                sens = 0;
            }
            else if (GrowthStage > FrostSensGrowthStageFirst && GrowthStage < FrostSensGrowthStageSecond)
            {
                sens =
                    GrowthStage * ((1 - 0) / (FrostSensGrowthStageSecond - FrostSensGrowthStageFirst))
                    + (0 * FrostSensGrowthStageSecond - FrostSensGrowthStageFirst * 1) / (FrostSensGrowthStageSecond - FrostSensGrowthStageFirst);
            }
            else if (GrowthStage >= FrostSensGrowthStageSecond && GrowthStage <= FrostSensGrowthStageThird)
            {
                sens = 1.0;
            }
            else if (GrowthStage > FrostSensGrowthStageThird && GrowthStage < FrostSensGrowthStageFourth)
            {
                sens =
                    GrowthStage * ((0 - 1) / (FrostSensGrowthStageFourth - FrostSensGrowthStageThird))
                    + (1 * FrostSensGrowthStageFourth - FrostSensGrowthStageThird * 0) / (FrostSensGrowthStageFourth - FrostSensGrowthStageThird);
            }
            else if (GrowthStage >= FrostSensGrowthStageFourth)
            {
                sens = 0;
            }
            return sens;
        }

        /// <summary>Caculates daily multiplier of heat stress.</summary>
        private double HeatSurvFun(double t)
        {
            double ratio = 1.0;

            if (t <= HeatSurvLowT)
            {
                ratio = 1.0;
            }
            else if (t > HeatSurvLowT && t < HeatSurvUpT)
            {
                ratio =
                    t * ((HeatSurvUpR - HeatSurvLowR) / (HeatSurvUpT - HeatSurvLowT))
                    + (HeatSurvLowR * HeatSurvUpT - HeatSurvLowT * HeatSurvUpR)
                        / (HeatSurvUpT - HeatSurvLowT);
            }
            else if (t >= HeatSurvUpT)
            {
                ratio = HeatSurvUpR;
            }
            return ratio;
        }

        /// <summary>Caculates daily sensitivity of heat stress.</summary>
        private double HeatSensFun(double GrowthStage)
        {
            double sens = 0;

            if (GrowthStage <= HeatSensGrowthStageFirst)
            {
                sens = 0;
            }
            else if (GrowthStage > HeatSensGrowthStageFirst && GrowthStage < HeatSensGrowthStageSecond)
            {
                sens =
                    GrowthStage * ((1 - 0) / (HeatSensGrowthStageSecond - HeatSensGrowthStageFirst))
                    + (0 * HeatSensGrowthStageSecond - HeatSensGrowthStageFirst * 1) / (HeatSensGrowthStageSecond - HeatSensGrowthStageFirst);
            }
            else if (GrowthStage >= HeatSensGrowthStageSecond && GrowthStage <= HeatSensGrowthStageThird)
            {
                sens = 1.0;
            }
            else if (GrowthStage > HeatSensGrowthStageThird && GrowthStage < HeatSensGrowthStageFourth)
            {
                sens =
                    GrowthStage * ((0 - 1) / (HeatSensGrowthStageFourth - HeatSensGrowthStageThird))
                    + (1 * HeatSensGrowthStageFourth - HeatSensGrowthStageThird * 0) / (HeatSensGrowthStageFourth - HeatSensGrowthStageThird);
            }
            else if (GrowthStage >= HeatSensGrowthStageFourth)
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

            // frost survival
            FrostSurv = FrostSurvFun(Weather.MinT);

            // frost sensitivity
            FrostSens = FrostSensFun(GrowthStageToday);

            // heat survival
            HeatSurv = HeatSurvFun(Weather.MaxT);

            // heat sensitivity
            HeatSens = HeatSensFun(GrowthStageToday);

            // combined stress survival
            FrostHeatCombinedSurv = (1 - (1 - FrostSurv) * FrostSens)
                        * (1 - (1 - HeatSurv) * HeatSens);

            // final multiplier
            FinalStressMultiplier = FinalStressMultiplier * FrostHeatCombinedSurv;
            FinalHeatMultiplier = FinalHeatMultiplier * (1 - (1 - HeatSurv) * HeatSens);
            FinalFrostMultiplier = FinalFrostMultiplier * (1 - (1 - FrostSurv) * FrostSens);

            // frost and heat yield
            FrostHeatYield = organs.Wt * FinalStressMultiplier;
        }
    }
}
