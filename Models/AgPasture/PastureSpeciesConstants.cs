using System;
using Models.Core;

namespace Models.AgPasture
{
    /// <summary>
    /// Holds all the constants for a PastureSpecies so they can be set in a resource
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(PastureSpecies))]
    public class PastureSpeciesConstants : Model
    {
        /// <summary>Family type for this plant species (grass/legume/forb).</summary>
        [Separator("Species Parameters")]
        [Description("SpeciesFamily")]
        [Units("-")]
        public PastureSpecies.PlantFamilyType SpeciesFamily { get; set; } = PastureSpecies.PlantFamilyType.Grass;

        /// <summary>Species metabolic pathway of C fixation during photosynthesis (C3/C4).</summary>
        [Description("PhotosyntheticPathway")]
        public PastureSpecies.PhotosynthesisPathwayType PhotosyntheticPathway { get; set; } = PastureSpecies.PhotosynthesisPathwayType.C3;

        ////- Potential growth (photosynthesis) >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Reference leaf CO2 assimilation rate for photosynthesis (mg CO2/m^2Leaf/s).</summary>
        [Separator("Potential growth")]
        [Description("ReferencePhotosyntheticRate")]
        [Units("mg/m^2/s")]
        public double ReferencePhotosyntheticRate { get; set; } = 1;

        /// <summary>Leaf photosynthetic efficiency (mg CO2/J).</summary>
        [Description("PhotosyntheticEfficiency")]
        [Units("mg CO2/J")]
        public double PhotosyntheticEfficiency { get; set; } = 0.01;

        /// <summary>Photosynthesis curvature parameter (J/kg/s).</summary>
        [Description("PhotosynthesisCurveFactor")]
        [Units("J/kg/s")]
        public double PhotosynthesisCurveFactor { get; set; } = 0.8;

        /// <summary>Light extinction coefficient (0-1).</summary>
        [Description("LightExtinctionCoefficient")]
        [Units("0-1")]
        public double LightExtinctionCoefficient { get; set; } = 0.5;

        /// <summary>Minimum temperature for growth (oC).</summary>
        [Description("GrowthTminimum")]
        [Units("oC")]
        public double GrowthTminimum { get; set; } = 1.0;

        /// <summary>Optimum temperature for growth (oC).</summary>
        [Description("GrowthToptimum")]
        [Units("oC")]
        public double GrowthToptimum { get; set; } = 20.0;

        /// <summary>Curve parameter for growth response to temperature (>0.0).</summary>
        [Description("GrowthTEffectExponent")]
        [Units("-")]
        public double GrowthTEffectExponent { get; set; } = 1.7;

        /// <summary>Reference CO2 concentration for photosynthesis (ppm).</summary>
        [Description("ReferenceCO2")]
        [Units("ppm")]
        public double ReferenceCO2 { get; set; } = 380.0;

        /// <summary>Scaling parameter for the CO2 effect on photosynthesis (ppm).</summary>
        [Description("CO2EffectOnPhotoScaleFactor")]
        [Units("ppm")]
        public double CO2EffectOnPhotoScaleFactor { get; set; } = 700.0;

        /// <summary>Scaling parameter for the CO2 effects on N requirements (ppm).</summary>
        [Description("CO2EffectOnNConcScaleFactor")]
        [Units("ppm")]
        public double CO2EffectOnNConcScaleFactor { get; set; } = 600.0;

        /// <summary>Exponent controlling the CO2 effect on N requirements (>0.0).</summary>
        [Description("CO2EffectOnNConcExponent")]
        [Units("-")]
        public double CO2EffectOnNConcExponent { get; set; } = 2.0;

        /// <summary>Onset temperature for heat effects on photosynthesis (oC).</summary>
        [Description("HeatOnsetTdemperature")]
        [Units("oC")]
        public double HeatOnsetTemperature { get; set; } = 28.0;

        /// <summary>Temperature for full heat effect on photosynthesis, growth stops (oC).</summary>
        [Description("HeatFullTemperature")]
        [Units("oC")]
        public double HeatFullTemperature { get; set; } = 35.0;

        /// <summary>Cumulative degrees-day for recovery from heat stress (oCd).</summary>
        [Description("HeatRecoverySumDD")]
        [Units("oCd")]
        public double HeatRecoverySumDD { get; set; } = 30.0;

        /// <summary>Reference temperature for recovery from heat stress (oC).</summary>
        [Description("HeatRecoveryTReference")]
        [Units("oC")]
        public double HeatRecoveryTReference { get; set; } = 25.0;

        /// <summary>Onset temperature for cold effects on photosynthesis (oC).</summary>
        [Description("ColdOnsetTemperature")]
        [Units("oC")]
        public double ColdOnsetTemperature { get; set; } = 1.0;

        /// <summary>Temperature for full cold effect on photosynthesis, growth stops (oC).</summary>
        [Description("ColdFullTemperature")]
        [Units("oC")]
        public double ColdFullTemperature { get; set; } = -5.0;

        /// <summary>Cumulative degrees for recovery from cold stress (oCd).</summary>
        [Description("ColdRecoverySumDD")]
        [Units("oCd")]
        public double ColdRecoverySumDD { get; set; } = 25.0;

        /// <summary>Reference temperature for recovery from cold stress (oC).</summary>
        [Description("ColdRecoveryTReference")]
        [Units("oC")]
        public double ColdRecoveryTReference { get; set; } = 0.0;

        ////- Respiration parameters >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Maintenance respiration coefficient (0-1).</summary>
        [Separator("Respiration Parameters")]
        [Description("MaintenanceRespirationCoefficient")]
        [Units("0-1")]
        public double MaintenanceRespirationCoefficient { get; set; } = 0.03;

        /// <summary>Growth respiration coefficient (0-1).</summary>
        [Description("GrowthRespirationCoefficient")]
        [Units("0-1")]
        public double GrowthRespirationCoefficient { get; set; } = 0.25;

        /// <summary>Reference temperature for maintenance respiration (oC).</summary>
        [Description("RespirationTReference")]
        [Units("oC")]
        public double RespirationTReference { get; set; } = 20.0;

        /// <summary>Exponent controlling the effect of temperature on respiration (>1.0).</summary>
        [Description("RespirationExponent")]
        [Units("-")]
        public double RespirationExponent { get; set; } = 1.5;

        ////- Germination and emergence >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Cumulative degrees-day needed for seed germination (oCd).</summary>
        [Separator("Germination and Emergence Parameters")]
        [Description("DegreesDayForGermination")]
        [Units("oCd")]
        public double DegreesDayForGermination { get; set; } = 125;

        ////- Allocation of new growth >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Target, or ideal, shoot-root ratio (>0.0).</summary>
        [Separator("Allocation of New Growth Parameters")]
        [Description("TargetShootRootRatio")]
        [Units("-")]
        public double TargetShootRootRatio { get; set; } = 4.0;

        /// <summary>Maximum effect that soil GLFs have on Shoot-Root ratio (0-1).</summary>
        [Description("ShootRootGlfFactor")]
        [Units("0-1")]
        public double ShootRootGlfFactor { get; set; } = 0.50;

        /// <summary>Maximum target allocation of shoot new growth to leaves (0-1).</summary>
        [Description("LeafProportionTargetMax")]
        [Units("0-1")]
        public double LeafProportionTargetMax { get; set; } = 0.7;

        /// <summary>Minimum target allocation of shoot new growth to leaves (0-1).</summary>
        [Description("LeafProportionTargetMin")]
        [Units("0-1")]
        public double LeafProportionTargetMin { get; set; } = 0.7;

        /// <summary>Shoot DM at which allocation of new growth to leaves start to decrease (kgDM/ha).</summary>
        [Description("LeafPropDMThreshold")]
        [Units("kg/ha")]
        public double LeafPropDMThreshold { get; set; } = 500;

        /// <summary>Shoot DM when allocation to leaves is midway maximum and minimum (kgDM/ha).</summary>
        [Description("LeafPropDMForHalfEffect")]
        [Units("kg/ha")]
        public double LeafPropDMForHalfEffect { get; set; } = 2000;

        /// <summary>Exponent of the function controlling the DM allocation to leaves (>0.0).</summary>
        [Description("LeafPropExponent")]
        [Units(">0.0")]
        public double LeafPropExponent { get; set; } = 3.0;

        /// <summary>Target allocation of shoot new shoot growth to stolons (0-1).</summary>
        [Description("StolonProportionTarget")]
        [Units("0-1")]
        public double StolonProportionTarget { get; set; } = 0.0;

        /// <summary>Specific leaf area (m^2/kgDM).</summary>
        [Description("SpecificLeafArea")]
        [Units("m^2/kg")]
        public double SpecificLeafArea { get; set; } = 25.0;

        /// <summary>Fraction of stolon tissue used when computing green LAI (0-1).</summary>
        [Units("0-1")]
        [Description("StolonEffectOnLAI")]
        public double StolonEffectOnLAI { get; set; } = 0.0;

        /// <summary>Aboveground biomass below which stems are used for computing LAI (kgDM/ha).</summary>
        [Description("ShootDMThresholdForLAI")]
        [Units("kg/ha")]
        public double ShootDMThresholdForLAI { get; set; } = 0;

        /// <summary>Maximum fraction of stem tissue used when computing green LAI (0-1).</summary>
        [Description("StemMaxEffectOnLAI")]
        [Units("0-1")]
        public double StemMaxEffectOnLAI { get; set; } = 1.0;

        ////- Tissue turnover and senescence >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Number of live leaves per tiller (-).</summary>
        [Separator("Tissue Turnover and Senescence Parameters")]
        [Description("LiveLeavesPerTiller")][Units("-")]
        public double LiveLeavesPerTiller { get; set; } = 3.0;

        /// <summary>Reference daily DM turnover rate for shoot tissues (0-1).</summary>
        /// <remarks>This is closely related to the leaf appearance rate.</remarks>
        [Description("TissueTurnoverRefRateShoot")]
        [Units("0-1")]
        public double TissueTurnoverRefRateShoot { get; set; } = 0.05;

        /// <summary>Reference daily DM turnover rate for root tissues (0-1).</summary>
        [Description("TissueTurnoverRefRateRoot")]
        [Units("0-1")]
        public double TissueTurnoverRefRateRoot { get; set; } = 0.02;

        /// <summary>Reference daily detachment rate for dead tissues (0-1).</summary>
        [Description("DetachmentRefRateShoot")]
        [Units("0-1")]
        public double DetachmentRefRateShoot { get; set; } = 0.08;

        /// <summary>Minimum temperature for tissue turnover (oC).</summary>
        [Description("TurnoverTemperatureMin")]
        [Units("oC")]
        public double TurnoverTemperatureMin { get; set; } = 1.0;

        /// <summary>Reference temperature for tissue turnover (oC).</summary>
        [Description("TurnoverTemperatureRef")]
        [Units("oC")]
        public double TurnoverTemperatureRef { get; set; } = 16.0;

        /// <summary>Exponent of function for temperature effect on tissue turnover (>0.0).</summary>
        [Description("TurnoverTemperatureExponent")]
        [Units("-")]
        public double TurnoverTemperatureExponent { get; set; } = 1.5;

        /// <summary>Maximum increase in tissue turnover due to water deficit (>0.0).</summary>
        [Description("TurnoverDroughtEffectMax")]
        [Units("-")]
        public double TurnoverDroughtEffectMax { get; set; } = 1.0;

        /// <summary>Minimum GLFwater without effect on tissue turnover (0-1).</summary>
        [Description("TurnoverDroughtThreshold")]
        [Units("0-1")]
        public double TurnoverDroughtThreshold { get; set; } = 0.6;

        /// <summary>Exponent of function for the effect of GLFwater on tissue turnover (>1.0).</summary>
        [Description("TurnoverDroughtExponent")]
        [Units("-")]
        public double TurnoverDroughtExponent { get; set; } = 2.0;

        /// <summary>Coefficient controlling detachment rate as function of moisture (>0.0).</summary>
        [Description("DetachmentDroughtCoefficient")]
        [Units("-")]
        public double DetachmentDroughtCoefficient { get; set; } = 3.0;

        /// <summary>Minimum effect of drought on detachment rate (0-1).</summary>
        [Description("DetachmentDroughtEffectMin")]
        [Units("0-1")]
        public double DetachmentDroughtEffectMin { get; set; } = 0.1;

        /// <summary>Coefficient of function increasing the turnover rate due to defoliation (>0.0).</summary>
        /// <remarks>Converts the fraction of biomass removed into potential increase in turnover.</remarks>
        [Description("TurnoverDefoliationMultiplier")]
        [Units("-")]
        public double TurnoverDefoliationMultiplier { get; set; } = 1.0;

        /// <summary>Coefficient of function increasing the turnover rate due to defoliation (>0.0).</summary>
        /// <remarks>Controls the spread of the effect of time, the smaller the more spread the effect.</remarks>
        [Description("TurnoverDefoliationCoefficient")]
        [Units("-")]
        public double TurnoverDefoliationCoefficient { get; set; } = 0.5;

        /// <summary>Coefficient adjusting the effect of defoliation on root turnover rate (0-1).</summary>
        [Description("TurnoverDefoliationEffectOnRoots")]
        [Units("0-1")]
        public double TurnoverDefoliationEffectOnRoots { get; set; } = 0.1;

        ////- N fixation (for legumes) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Minimum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        [Separator("N fixation Parameters (for legumes)")]
        [Description("MinimumNFixation")]
        [Units("0-1")]
        public double MinimumNFixation { get; set; } = 0.0;

        /// <summary>Maximum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        [Description("MaximumNFixation")]
        [Units("0-1")]
        public double MaximumNFixation { get; set; } = 0.0;

        ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Maximum reduction in plant growth due to water logging (saturated soil) (0-1).</summary>
        [Separator("Growth Limiting Factors")]
        [Description("SoilSaturationEffectMax")]
        [Units("0-1")]
        public double SoilSaturationEffectMax { get; set; } = 0.1;

        /// <summary>Maximum daily recovery rate from water logging (0-1).</summary>
        [Description("SoilSaturationRecoveryFactor")]
        [Units("0-1")]
        public double SoilSaturationRecoveryFactor { get; set; } = 0.25;

        /// <summary>Exponent to modify the effect of N deficiency on plant growth (>1.0).</summary>
        [Description("NDilutionCoefficient")]
        [Units("-")]
        public double NDilutionCoefficient { get; set; } = 2.0;

        ////- Plant height >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Minimum plant height (mm).</summary>
        [Separator("Plant Height Parameters")]
        [Description("PlantHeightMinimum")]
        [Units("mm")]
        public double PlantHeightMinimum { get; set; } = 25.0;

        /// <summary>Maximum plant height (mm).</summary>
        [Description("PlantHeightMaximum")]
        [Units("mm")]
        public double PlantHeightMaximum { get; set; } = 600.0;

        /// <summary>DM weight above ground for maximum plant height (kgDM/ha).</summary>
        [Description("PlantHeightMassForMax")]
        [Units("kg/ha")]
        public double PlantHeightMassForMax { get; set; } = 10000;

        /// <summary>Exponent controlling shoot height as function of DM weight (>1.0).</summary>
        [Description("PlantHeightExponent")]
        [Units(">1.0")]
        public double PlantHeightExponent { get; set; } = 2.8;

        ////- Harvest limits and preferences >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Minimum above ground green DM, leaf and stems (kgDM/ha).</summary>
        [Separator("Harvest Limits and Preferences")]
        [Description("MinimumGreenWt")]
        [Units("kg/ha")]
        public double MinimumGreenWt { get; set; } = 100;
    }
}
