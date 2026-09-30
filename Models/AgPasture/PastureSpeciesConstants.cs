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
        ////- Potential growth (photosynthesis) >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Reference leaf CO2 assimilation rate for photosynthesis (mg CO2/m^2Leaf/s).</summary>
        [Description("ReferencePhotosyntheticRate")]
        [Units("mg/m^2/s")]
        public double ReferencePhotosyntheticRate { get; set; }

        /// <summary>Leaf photosynthetic efficiency (mg CO2/J).</summary>
        [Description("PhotosyntheticEfficiency")]
        [Units("mg CO2/J")]
        public double PhotosyntheticEfficiency { get; set; }

        /// <summary>Photosynthesis curvature parameter (J/kg/s).</summary>
        [Description("PhotosynthesisCurveFactor")]
        [Units("J/kg/s")]
        public double PhotosynthesisCurveFactor { get; set; }

        /// <summary>Light extinction coefficient (0-1).</summary>
        [Description("LightExtinctionCoefficient")]
        [Units("0-1")]
        public double LightExtinctionCoefficient { get; set; }

        /// <summary>Minimum temperature for growth (oC).</summary>
        [Description("GrowthTminimum")]
        [Units("oC")]
        public double GrowthTminimum { get; set; }

        /// <summary>Optimum temperature for growth (oC).</summary>
        [Description("GrowthToptimum")]
        [Units("oC")]
        public double GrowthToptimum { get; set; }

        /// <summary>Curve parameter for growth response to temperature (>0.0).</summary>
        [Description("GrowthTEffectExponent")]
        [Units("-")]
        public double GrowthTEffectExponent { get; set; }

        /// <summary>Reference CO2 concentration for photosynthesis (ppm).</summary>
        [Description("ReferenceCO2")]
        [Units("ppm")]
        public double ReferenceCO2 { get; set; }

        /// <summary>Scaling parameter for the CO2 effect on photosynthesis (ppm).</summary>
        [Description("CO2EffectScaleFactor")]
        [Units("ppm")]
        public double CO2EffectScaleFactor { get; set; }

        /// <summary>Scaling parameter for the CO2 effects on N requirements (ppm).</summary>
        [Description("CO2EffectOffsetFactor")]
        [Units("ppm")]
        public double CO2EffectOffsetFactor { get; set; }

        /// <summary>Minimum value for the CO2 effect on N requirements (0-1).</summary>
        [Description("CO2EffectMinimum")]
        [Units("0-1")]
        public double CO2EffectMinimum { get; set; }

        /// <summary>Exponent controlling the CO2 effect on N requirements (>0.0).</summary>
        [Description("CO2EffectExponent")]
        [Units("-")]
        public double CO2EffectExponent { get; set; }

        /// <summary>Onset temperature for heat effects on photosynthesis (oC).</summary>
        [Description("HeatOnsetTdemperature")]
        [Units("oC")]
        public double HeatOnsetTemperature { get; set; }

        /// <summary>Temperature for full heat effect on photosynthesis, growth stops (oC).</summary>
        [Description("HeatFullTemperature")]
        [Units("oC")]
        public double HeatFullTemperature { get; set; }

        /// <summary>Cumulative degrees-day for recovery from heat stress (oCd).</summary>
        [Description("HeatRecoverySumDD")]
        [Units("oCd")]
        public double HeatRecoverySumDD { get; set; }

        /// <summary>Reference temperature for recovery from heat stress (oC).</summary>
        [Description("HeatRecoveryTReference")]
        [Units("oC")]
        public double HeatRecoveryTReference { get; set; }

        /// <summary>Onset temperature for cold effects on photosynthesis (oC).</summary>
        [Description("ColdOnsetTemperature")]
        [Units("oC")]
        public double ColdOnsetTemperature { get; set; }

        /// <summary>Temperature for full cold effect on photosynthesis, growth stops (oC).</summary>
        [Description("ColdFullTemperature")]
        [Units("oC")]
        public double ColdFullTemperature { get; set; }

        /// <summary>Cumulative degrees for recovery from cold stress (oCd).</summary>
        [Description("ColdRecoverySumDD")]
        [Units("oCd")]
        public double ColdRecoverySumDD { get; set; }

        /// <summary>Reference temperature for recovery from cold stress (oC).</summary>
        [Description("ColdRecoveryTReference")]
        [Units("oC")]
        public double ColdRecoveryTReference { get; set; }

        ////- Respiration parameters >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Maintenance respiration coefficient (0-1).</summary>
        [Description("MaintenanceRespirationCoefficient")]
        [Units("0-1")]
        public double MaintenanceRespirationCoefficient { get; set; }

        /// <summary>Growth respiration coefficient (0-1).</summary>
        [Description("GrowthRespirationCoefficient")]
        [Units("0-1")]
        public double GrowthRespirationCoefficient { get; set; }

        /// <summary>Reference temperature for maintenance respiration (oC).</summary>
        [Description("RespirationTReference")]
        [Units("oC")]
        public double RespirationTReference { get; set; }

        /// <summary>Exponent controlling the effect of temperature on respiration (>1.0).</summary>
        [Description("RespirationExponent")]
        [Units("-")]
        public double RespirationExponent { get; set; }

        ////- Germination and emergence >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Cumulative degrees-day needed for seed germination (oCd).</summary>
        [Description("DegreesDayForGermination")]
        [Units("oCd")]
        public double DegreesDayForGermination { get; set; }

        ////- Allocation of new growth >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Target, or ideal, shoot-root ratio (>0.0).</summary>
        [Description("TargetShootRootRatio")]
        [Units("-")]
        public double TargetShootRootRatio { get; set; }

        /// <summary>Maximum effect that soil GLFs have on Shoot-Root ratio (0-1).</summary>
        [Description("ShootRootGlfFactor")]
        [Units("0-1")]
        public double ShootRootGlfFactor { get; set; }

        /// <summary>Maximum target allocation of shoot new growth to leaves (0-1).</summary>
        [Description("LeafProportionMaximum")]
        [Units("0-1")]
        public double LeafProportionMaximum { get; set; }

        /// <summary>Minimum target allocation of shoot new growth to leaves (0-1).</summary>
        [Description("LeafProportionMinimum")]
        [Units("0-1")]
        public double LeafProportionMinimum { get; set; }

        /// <summary>Shoot DM at which allocation of new growth to leaves start to decrease (kgDM/ha).</summary>
        [Description("LeafPropDMThreshold")]
        [Units("kg/ha")]
        public double LeafPropDMThreshold { get; set; }

        /// <summary>Shoot DM when allocation to leaves is midway maximum and minimum (kgDM/ha).</summary>
        [Description("LeafPropDMFactor")]
        [Units("kg/ha")]
        public double LeafPropDMFactor { get; set; }

        /// <summary>Exponent of the function controlling the DM allocation to leaves (>0.0).</summary>
        [Description("LeafPropExponent")]
        [Units(">0.0")]
        public double LeafPropExponent { get; set; }

        /// <summary>Specific leaf area (m^2/kgDM).</summary>
        [Description("SpecificLeafArea")]
        [Units("m^2/kg")]
        public double SpecificLeafArea { get; set; }

        /// <summary>Maximum aboveground biomass for considering stems when computing LAI (kgDM/ha).</summary>
        [Description("ShootMaxEffectOnLAI")]
        [Units("kg/ha")]
        public double ShootMaxEffectOnLAI { get; set; }

        /// <summary>Maximum fraction of stem tissue used when computing green LAI (0-1).</summary>
        [Description("MaxStemEffectOnLAI")]
        [Units("0-1")]
        public double MaxStemEffectOnLAI { get; set; }

        ////- Tissue turnover and senescence >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Number of live leaves per tiller (-).</summary>
        [Description("LiveLeavesPerTiller")][Units("-")]
        public double LiveLeavesPerTiller { get; set; }

        /// <summary>Reference daily DM turnover rate for shoot tissues (0-1).</summary>
        /// <remarks>This is closely related to the leaf appearance rate.</remarks>
        [Description("TissueTurnoverRefRateShoot")]
        [Units("0-1")]
        public double TissueTurnoverRefRateShoot { get; set; }

        /// <summary>Reference daily DM turnover rate for root tissues (0-1).</summary>
        [Description("TissueTurnoverRefRateRoot")]
        [Units("0-1")]
        public double TissueTurnoverRefRateRoot { get; set; }

        /// <summary>Reference daily detachment rate for dead tissues (0-1).</summary>
        [Description("DetachmentRefRateShoot")]
        [Units("0-1")]
        public double DetachmentRefRateShoot { get; set; }

        /// <summary>Minimum temperature for tissue turnover (oC).</summary>
        [Description("TurnoverTemperatureMin")]
        [Units("oC")]
        public double TurnoverTemperatureMin { get; set; }

        /// <summary>Reference temperature for tissue turnover (oC).</summary>
        [Description("TurnoverTemperatureRef")]
        [Units("oC")]
        public double TurnoverTemperatureRef { get; set; }

        /// <summary>Exponent of function for temperature effect on tissue turnover (>0.0).</summary>
        [Description("TurnoverTemperatureExponent")]
        [Units("-")]
        public double TurnoverTemperatureExponent { get; set; }

        /// <summary>Maximum increase in tissue turnover due to water deficit (>0.0).</summary>
        [Description("TurnoverDroughtEffectMax")]
        [Units("-")]
        public double TurnoverDroughtEffectMax { get; set; }

        /// <summary>Minimum GLFwater without effect on tissue turnover (0-1).</summary>
        [Description("TurnoverDroughtThreshold")]
        [Units("0-1")]
        public double TurnoverDroughtThreshold { get; set; }

        /// <summary>Exponent of function for the effect of GLFwater on tissue turnover (>1.0).</summary>
        [Description("TurnoverDroughtExponent")]
        [Units("-")]
        public double TurnoverDroughtExponent { get; set; }

        /// <summary>Coefficient controlling detachment rate as function of moisture (>0.0).</summary>
        [Description("DetachmentDroughtCoefficient")]
        [Units("-")]
        public double DetachmentDroughtCoefficient { get; set; }

        /// <summary>Minimum effect of drought on detachment rate (0-1).</summary>
        [Description("DetachmentDroughtEffectMin")]
        [Units("0-1")]
        public double DetachmentDroughtEffectMin { get; set; }

        /// <summary>Coefficient of function increasing the turnover rate due to defoliation (>0.0).</summary>
        /// <remarks>Converts the fraction of biomass removed into potential increase in turnover.</remarks>
        [Description("TurnoverDefoliationMultiplier")]
        [Units("-")]
        public double TurnoverDefoliationMultiplier { get; set; }

        /// <summary>Coefficient of function increasing the turnover rate due to defoliation (>0.0).</summary>
        /// <remarks>Controls the spread of the effect of time, the smaller the more spread the effect.</remarks>
        [Description("TurnoverDefoliationCoefficient")]
        [Units("-")]
        public double TurnoverDefoliationCoefficient { get; set; }

        /// <summary>Coefficient adjusting the effect of defoliation on root turnover rate (0-1).</summary>
        [Description("TurnoverDefoliationEffectOnRoots")]
        [Units("0-1")]
        public double TurnoverDefoliationEffectOnRoots { get; set; }

        ////- N fixation (for legumes) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Minimum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        [Description("MinimumNFixation")]
        [Units("0-1")]
        public double MinimumNFixation { get; set; }

        /// <summary>Maximum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        [Description("MaximumNFixation")]
        [Units("0-1")]
        public double MaximumNFixation { get; set; }

        ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Maximum reduction in plant growth due to water logging (saturated soil) (0-1).</summary>
        [Description("SoilSaturationEffectMax")]
        [Units("0-1")]
        public double SoilSaturationEffectMax { get; set; }

        /// <summary>Maximum daily recovery rate from water logging (0-1).</summary>
        [Description("SoilSaturationRecoveryFactor")]
        [Units("0-1")]
        public double SoilSaturationRecoveryFactor { get; set; }

        /// <summary>Exponent to modify the effect of N deficiency on plant growth (>1.0).</summary>
        [Description("NDilutionCoefficient")]
        [Units("-")]
        public double NDilutionCoefficient { get; set; }

        ////- Plant height >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Minimum plant height (mm).</summary>
        [Description("PlantHeightMinimum")]
        [Units("mm")]
        public double PlantHeightMinimum { get; set; }

        /// <summary>Maximum plant height (mm).</summary>
        [Description("PlantHeightMaximum")]
        [Units("mm")]
        public double PlantHeightMaximum { get; set; }

        /// <summary>DM weight above ground for maximum plant height (kgDM/ha).</summary>
        [Description("PlantHeightMassForMax")]
        [Units("kg/ha")]
        public double PlantHeightMassForMax { get; set; }

        /// <summary>Exponent controlling shoot height as function of DM weight (>1.0).</summary>
        [Description("PlantHeightExponent")]
        [Units(">1.0")]
        public double PlantHeightExponent { get; set; }

    }
}
