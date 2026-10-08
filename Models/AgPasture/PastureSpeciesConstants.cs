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
        /// <summary>Functional group for this plant species (grass/legume/forb).</summary>
        [Separator("Species parameters")]
        [Description("Plant functional group")]
        public PastureSpecies.PlantFamilyType SpeciesFamily { get; set; } = PastureSpecies.PlantFamilyType.Grass;

        /// <summary>Species metabolic pathway of C fixation during photosynthesis (C3/C4).</summary>
        [Description("Photosynthetic pathway")]
        public PastureSpecies.PhotosynthesisPathwayType PhotosyntheticPathway { get; set; } = PastureSpecies.PhotosynthesisPathwayType.C3;

        ////- Potential growth (photosynthesis) >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Reference leaf CO2 assimilation rate for photosynthesis (mg CO2/m^2Leaf/s).</summary>
        [Separator("Parameters controlling potential growth")]
        [Description("Reference photosynthetic rate")]
        [Units("mg/m^2/s")]
        public double ReferencePhotosyntheticRate { get; set; } = 1;

        /// <summary>Leaf photosynthetic efficiency (mg CO2/J).</summary>
        [Description("Photosynthetic efficiency")]
        [Units("mg CO2/J")]
        public double PhotosyntheticEfficiency { get; set; } = 0.01;

        /// <summary>Photosynthesis curvature parameter (J/kg/s).</summary>
        [Description("Photosynthesis curve factor")]
        [Units("J/kg/s")]
        public double PhotosynthesisCurveFactor { get; set; } = 0.8;

        /// <summary>Minimum temperature for growth (oC).</summary>
        [Description("Minimum temperature for growth")]
        [Units("oC")]
        public double GrowthTminimum { get; set; } = 1.0;

        /// <summary>Optimum temperature for growth (oC).</summary>
        [Description("Optimum temperature for growth")]
        [Units("oC")]
        public double GrowthToptimum { get; set; } = 20.0;

        /// <summary>Curve parameter for growth response to temperature (>0.0).</summary>
        [Description("Exponent of growth response to temperature")]
        [Units("-")]
        public double GrowthTEffectExponent { get; set; } = 1.7;

        /// <summary>Reference CO2 concentration for photosynthesis (ppm).</summary>
        [Description("Reference CO2 concentration")]
        [Units("ppm")]
        public double ReferenceCO2 { get; set; } = 380.0;

        /// <summary>Scaling parameter for the CO2 effect on photosynthesis (ppm).</summary>
        [Description("Scaling parameter for the CO2 effect on photosynthesis")]
        [Units("ppm")]
        public double CO2EffectOnPhotoScaleFactor { get; set; } = 700.0;

        /// <summary>Scaling parameter for the CO2 effects on N requirements (ppm).</summary>
        [Description("Scaling parameter for the CO2 effects on N requirements")]
        [Units("ppm")]
        public double CO2EffectOnNConcScaleFactor { get; set; } = 600.0;

        /// <summary>Exponent controlling the CO2 effect on N requirements (>0.0).</summary>
        [Description("Exponent of CO2 effects on N requirements")]
        [Units("-")]
        public double CO2EffectOnNConcExponent { get; set; } = 2.0;

        /// <summary>Onset temperature for heat effects on photosynthesis (oC).</summary>
        [Separator("Parameters controlling heat stress")]
        [Description("Temperature for onset of heat stress")]
        [Units("oC")]
        public double HeatOnsetTemperature { get; set; } = 28.0;

        /// <summary>Temperature for full heat effect on photosynthesis, growth stops (oC).</summary>
        [Description("Temperature for full heat stress")]
        [Units("oC")]
        public double HeatFullTemperature { get; set; } = 35.0;

        /// <summary>Cumulative degrees-day for recovery from heat stress (oCd).</summary>
        [Description("Degrees-day for recovery from heat stress")]
        [Units("oCd")]
        public double HeatRecoverySumDD { get; set; } = 30.0;

        /// <summary>Reference temperature for recovery from heat stress (oC).</summary>
        [Description("Reference temperature for heat stress recovery")]
        [Units("oC")]
        public double HeatRecoveryTReference { get; set; } = 25.0;

        /// <summary>Onset temperature for cold effects on photosynthesis (oC).</summary>
        [Separator("Parameters controlling cold stress")]
        [Description("Temperature for onset of cold stress")]
        [Units("oC")]
        public double ColdOnsetTemperature { get; set; } = 1.0;

        /// <summary>Temperature for full cold effect on photosynthesis, growth stops (oC).</summary>
        [Description("Temperature for full cold effect")]
        [Units("oC")]
        public double ColdFullTemperature { get; set; } = -5.0;

        /// <summary>Cumulative degrees-day for recovery from cold stress (oCd).</summary>
        [Description("Degrees-day for recovery from cold stress")]
        [Units("oCd")]
        public double ColdRecoverySumDD { get; set; } = 25.0;

        /// <summary>Reference temperature for recovery from cold stress (oC).</summary>
        [Description("Reference temperature for cold stress recovery")]
        [Units("oC")]
        public double ColdRecoveryTReference { get; set; } = 0.0;

        ////- Respiration parameters >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Maintenance respiration coefficient (0-1).</summary>
        [Separator("Parameters controlling respiration")]
        [Description("Maintenance respiration coefficient")]
        [Units("0-1")]
        public double MaintenanceRespirationCoefficient { get; set; } = 0.03;

        /// <summary>Growth respiration coefficient (0-1).</summary>
        [Description("Growth respiration coefficient")]
        [Units("0-1")]
        public double GrowthRespirationCoefficient { get; set; } = 0.25;

        /// <summary>Reference temperature for maintenance respiration (oC).</summary>
        [Description("Reference temperature for maintenance respiration")]
        [Units("oC")]
        public double RespirationTReference { get; set; } = 20.0;

        /// <summary>Exponent controlling the effect of temperature on respiration (>1.0).</summary>
        [Description("Exponent for the effects of temperature on respiration")]
        [Units("-")]
        public double RespirationExponent { get; set; } = 1.5;

        ////- Germination and emergence >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Cumulative degrees-day needed for seed germination (oCd).</summary>
        [Separator("Parameters for germination and emergence")]
        [Description("Degrees-day for germination")]
        [Units("oCd")]
        public double DegreesDayForGermination { get; set; } = 125;

        ////- Allocation of new growth >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Target, or ideal, shoot-root ratio (>0.0).</summary>
        [Separator("Parameters controlling allocation of new growth")]
        [Description("Target shoot:root ratio")]
        [Units("kg/kg")]
        public double TargetShootRootRatio { get; set; } = 4.0;

        /// <summary>Maximum effect that soil GLFs have on shoot-root ratio (0-1).</summary>
        [Description("Maximum effect of GLFs on shoot:root ratio")]
        [Units("0-1")]
        public double ShootRootGlfFactor { get; set; } = 0.50;

        /// <summary>Maximum target allocation of shoot new growth to leaves (0-1).</summary>
        [Description("Maximum target allocation to leaves")]
        [Units("0-1")]
        public double LeafProportionTargetMax { get; set; } = 0.7;

        /// <summary>Minimum target allocation of shoot new growth to leaves (0-1).</summary>
        [Description("Minimum target allocation to leaves")]
        [Units("0-1")]
        public double LeafProportionTargetMin { get; set; } = 0.7;

        /// <summary>Shoot DM at which allocation of new growth to leaves start to decrease (kgDM/ha).</summary>
        [Description("Shoot DM at which allocation to leaves start to decrease")]
        [Units("kg/ha")]
        public double LeafPropDMThreshold { get; set; } = 500;

        /// <summary>Shoot DM when allocation to leaves is midway maximum and minimum (kgDM/ha).</summary>
        [Description("Shoot DM at which allocation to leaves is half way to minimum")]
        [Units("kg/ha")]
        public double LeafPropDMForHalfEffect { get; set; } = 2000;

        /// <summary>Exponent of the function controlling the DM allocation to leaves (>0.0).</summary>
        [Description("Exponent of function allocating DM to leaves")]
        [Units("-")]
        public double LeafPropExponent { get; set; } = 3.0;

        /// <summary>Target allocation of shoot new shoot growth to stolons (0-1).</summary>
        [Description("Target allocation to stolons")]
        [Units("0-1")]
        public double StolonProportionTarget { get; set; } = 0.0;

        ////- Leaf area and related properties >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Light extinction coefficient (0-1).</summary>
        [Separator("Parameters controlling leaf properties")]
        [Description("Light extinction coefficient")]
        [Units("0-1")]
        public double LightExtinctionCoefficient { get; set; } = 0.5;

        /// <summary>Specific leaf area (m^2/kgDM).</summary>
        [Description("Specific leaf area")]
        [Units("m^2/kg")]
        public double SpecificLeafArea { get; set; } = 25.0;

        /// <summary>Fraction of stolon tissue used when computing green LAI (0-1).</summary>
        [Units("0-1")]
        [Description("Fraction of stolon DM used for LAI")]
        public double StolonEffectOnLAI { get; set; } = 0.0;

        /// <summary>Aboveground biomass below which stems are used for computing LAI (kgDM/ha).</summary>
        [Description("Shoot DM below which stems are used for LAI")]
        [Units("kg/ha")]
        public double ShootDMThresholdForLAI { get; set; } = 0;

        /// <summary>Maximum fraction of stem tissue used when computing green LAI (0-1).</summary>
        [Description("Maximum fraction of stem DM used for LAI")]
        [Units("0-1")]
        public double StemMaxEffectOnLAI { get; set; } = 1.0;

        ////- Tissue turnover and senescence >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Number of live leaves per tiller (-).</summary>
        [Separator("Parameters for tissue turnover and senescence")]
        [Description("Number of live leaves per tiller")][Units("-")]
        public double LiveLeavesPerTiller { get; set; } = 3.0;

        /// <summary>Reference daily DM turnover rate for shoot tissues (0-1).</summary>
        /// <remarks>This is closely related to the leaf appearance rate.</remarks>
        [Description("Reference turnover rate for shoot tissues")]
        [Units("0-1")]
        public double TissueTurnoverRefRateShoot { get; set; } = 0.05;

        /// <summary>Reference daily DM turnover rate for root tissues (0-1).</summary>
        [Description("Reference turnover rate for root tissues")]
        [Units("0-1")]
        public double TissueTurnoverRefRateRoot { get; set; } = 0.02;

        /// <summary>Reference daily detachment rate for dead tissues (0-1).</summary>
        [Description("Reference detachment rate for dead shoot tissues")]
        [Units("0-1")]
        public double DetachmentRefRateShoot { get; set; } = 0.08;

        /// <summary>Minimum temperature for tissue turnover (oC).</summary>
        [Description("Minimum temperature for tissue turnover")]
        [Units("oC")]
        public double TurnoverTemperatureMin { get; set; } = 1.0;

        /// <summary>Reference temperature for tissue turnover (oC).</summary>
        [Description("Reference temperature for tissue turnover")]
        [Units("oC")]
        public double TurnoverTemperatureRef { get; set; } = 16.0;

        /// <summary>Exponent of function for temperature effect on tissue turnover (>0.0).</summary>
        [Description("Exponent for the temperature effect on tissue turnover")]
        [Units("-")]
        public double TurnoverTemperatureExponent { get; set; } = 1.5;

        /// <summary>Maximum increase in tissue turnover due to water deficit (>0.0).</summary>
        [Description("Maximum increase in turnover due to water deficit")]
        [Units("-")]
        public double TurnoverDroughtEffectMax { get; set; } = 1.0;

        /// <summary>Minimum GLFwater without effect on tissue turnover (0-1).</summary>
        [Description("GLF water below which turnover increases")]
        [Units("0-1")]
        public double TurnoverDroughtThreshold { get; set; } = 0.6;

        /// <summary>Exponent of function for the effect of GLFwater on tissue turnover (>1.0).</summary>
        [Description("Exponent for the effect of GLFwater on turnover")]
        [Units("-")]
        public double TurnoverDroughtExponent { get; set; } = 2.0;

        /// <summary>Coefficient controlling detachment rate as function of moisture (>0.0).</summary>
        [Description("Exponent of function reducing detachment due to drought")]
        [Units("-")]
        public double DetachmentDroughtCoefficient { get; set; } = 3.0;

        /// <summary>Minimum effect of drought on detachment rate (0-1).</summary>
        [Description("Minimum effect of drought on detachment")]
        [Units("0-1")]
        public double DetachmentDroughtEffectMin { get; set; } = 0.1;

        /// <summary>Coefficient of function increasing the turnover rate due to defoliation (>0.0).</summary>
        /// <remarks>Converts the fraction of biomass removed into potential increase in turnover.</remarks>
        [Description("Coefficient increasing turnover due to defoliation")]
        [Units("-")]
        public double TurnoverDefoliationMultiplier { get; set; } = 1.0;

        /// <summary>Multiplier of function increasing the turnover rate due to defoliation (>0.0).</summary>
        /// <remarks>Controls the spread of the effect of time, the smaller the more spread the effect.</remarks>
        [Description("Multiplier of the function increasing root turnover after defoliation")]
        [Units("-")]
        public double TurnoverDefoliationCoefficient { get; set; } = 0.5;

        /// <summary>Exponent adjusting the effect of defoliation on root turnover rate (0-1).</summary>
        [Description("Exponent of the function increasing root turnover after defoliation")]
        [Units("-")]
        public double TurnoverDefoliationEffectOnRoots { get; set; } = 0.1;

        ////- N fixation (for legumes) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        private bool isLegume
        { get { return SpeciesFamily == PastureSpecies.PlantFamilyType.Legume; } }

        /// <summary>Minimum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        [Separator("Parameters controlling N fixation (for legumes)")]
        [Display(VisibleCallback = nameof(isLegume))]
        [Description("Minimum proportion of N fixation")]
        [Units("0-1")]
        public double MinimumNFixation { get; set; } = 0.0;

        /// <summary>Maximum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        [Display(VisibleCallback = nameof(isLegume))]
        [Description("Maximum proportion of N fixation")]
        [Units("0-1")]
        public double MaximumNFixation { get; set; } = 0.0;

        ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Maximum reduction in plant growth due to water logging (saturated soil) (0-1).</summary>
        [Separator("Parameters modifying growth limiting factors")]
        [Description("Maximum reduction in growth due to water logging")]
        [Units("0-1")]
        public double SoilSaturationEffectMax { get; set; } = 0.1;

        /// <summary>Maximum daily recovery rate from water logging (0-1).</summary>
        [Description("Maximum recovery rate from water logging")]
        [Units("0-1")]
        public double SoilSaturationRecoveryFactor { get; set; } = 0.25;

        /// <summary>Exponent to modify the effect of N deficiency on plant growth (>1.0).</summary>
        [Description("Exponent modifying the effect of N deficiency on growth")]
        [Units("-")]
        public double NDilutionCoefficient { get; set; } = 2.0;

        ////- Plant height >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Minimum plant height (mm).</summary>
        [Separator("Parameters controlling plant height")]
        [Description("Minimum plant height")]
        [Units("mm")]
        public double PlantHeightMinimum { get; set; } = 25.0;

        /// <summary>Maximum plant height (mm).</summary>
        [Description("Maximum plant height")]
        [Units("mm")]
        public double PlantHeightMaximum { get; set; } = 600.0;

        /// <summary>DM weight above ground for maximum plant height (kgDM/ha).</summary>
        [Description("Shoot DM for maximum plant height")]
        [Units("kg/ha")]
        public double PlantHeightMassForMax { get; set; } = 10000;

        /// <summary>Exponent controlling shoot height as function of DM weight (>1.0).</summary>
        [Description("Exponent of shoot height function")]
        [Units("-")]
        public double PlantHeightExponent { get; set; } = 2.8;

        ////- Harvest limits and preferences >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Minimum above ground green DM, leaf and stems (kgDM/ha).</summary>
        [Separator("Harvest limits and preferences")]
        [Description("Minimum above ground green DM")]
        [Units("kg/ha")]
        public double MinimumGreenWt { get; set; } = 100;
    }
}
