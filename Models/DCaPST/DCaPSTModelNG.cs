using APSIM.Core;
using Models.Climate;
using Models.Core;
using Models.DCAPST.Canopy;
using Models.DCAPST.Environment;
using Models.DCAPST.Interfaces;
using Models.Functions;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Arbitrator;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Display = Models.Core.DisplayAttribute;

namespace Models.DCAPST
{
    /// <summary>
    /// APSIM Next Generation wrapper around the DCaPST model.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(typeof(Zone))]
    public class DCaPSTModelNG : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { get; set; }

        [Link]
        IClock clock = null;

        /// <summary>
        /// Weather provider.
        /// </summary>
        [Link]
        private readonly IWeather weather = null;

        /// <summary>
        /// Soil water balance.
        /// </summary>
        [Link]
        private readonly ISoilWater soilWater = null;

        /// <summary>
        /// Soil water balance.
        /// </summary>
        [Link]
        private readonly IUptakeMethod waterUptakeMethod = null;

        /// <summary>
        /// The chosen crop name.
        /// </summary>
        private string cropName = string.Empty;

        /// <summary>
        /// The plant which is set dynamically, based on the CropName.
        /// </summary>
        IPlant plant;

        /// <summary>
        /// The leaf.
        /// </summary>
        ICanopy leaf;

        /// <summary>
        /// The root shoot ration function
        /// </summary>
        IFunction rootShootRatioFunction;

        /// <summary>
        /// This flag is set to indicate that we have started using DCaPST.
        /// We wait until the Leaf LAI reaches our tolerance before starting to use
        /// DCaPST and the continue to use it until a new sowing event occcurs.
        /// </summary>
        private bool dcapsReachedLAITriggerPoint = false;

        /// <summary>
        /// The leaf LAI tolerence that has to be reached before starting to use DCaPST.
        /// </summary>
        private const double LEAF_LAI_START_USING_DCAPST_TRIGGER = 0.5;

        /// <summary>
        /// Rubisco modifier, defaulted to 1.
        /// </summary>
        private double rubiscoLimitedModifier = 1.0;

        /// <summary>
        /// Electron modifier, defaulted to 1.
        /// </summary>
        private double electronTransportLimitedModifier = 1.0;

        /// <summary>
        ///
        /// </summary>
        private bool includeAc2Pathway = false;

        /// <summary>
        /// The crop against which DCaPST will be run.
        /// </summary>
        [Description("The crop against which DCaPST will run")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetPlantNames))]
        public string CropName
        {
            get
            {
                return cropName;
            }
            set
            {
                // Optimise Handling a Crop Change call so that it only happens if the
                // value has actually changed.
                if (cropName != value)
                {
                    cropName = value;
                    Reset();
                }
            }
        }

        /// <summary>
        /// If true, the AC2 Pathway is included in the C4 Photosynthesis rate calculation.
        /// </summary>
        [JsonIgnore]
        public bool IncludeAc2Pathway
        {
            get => includeAc2Pathway;
            set
            {
                includeAc2Pathway = value;
            }
        }

        /// <summary>
        /// The DCaPST Parameters.
        /// </summary>
        [JsonIgnore]
        public DCaPSTParameters Parameters { get; private set; } = new();

        /// <summary>
        /// Store the model as this is used in different functions after assignment.
        /// </summary>
        [JsonIgnore]
        public DCAPSTModel DcapstModel { get; private set; } = new();

        /// <summary>
        /// Invoked once for each DCaPST calculation interval after the day's
        /// water-limited biomass calculation has completed.
        /// </summary>
        public event EventHandler IntervalStep;

        /// <summary>Date and time of the current DCaPST interval.</summary>
        [JsonIgnore]
        public DateTime IntervalDateTime { get; private set; }

        /// <summary>Hour of the current DCaPST interval.</summary>
        [JsonIgnore]
        [Units("hours")]
        public double Hour { get; private set; }

        /// <summary>Air temperature during the current DCaPST interval.</summary>
        [JsonIgnore]
        [Units("°C")]
        public double AirTemperature { get; private set; }

        /// <summary>Leaf area index of the sunlit canopy during the current interval.</summary>
        [JsonIgnore]
        [Units("m^2/m^2")]
        public double SunlitLAI { get; private set; }

        /// <summary>Leaf area index of the shaded canopy during the current interval.</summary>
        [JsonIgnore]
        [Units("m^2/m^2")]
        public double ShadedLAI { get; private set; }

        /// <summary>LAI-weighted canopy temperature during the current interval.</summary>
        [JsonIgnore]
        [Units("°C")]
        public double CanopyTemperature { get; private set; }

        /// <summary>LAI-weighted canopy vapour pressure deficit during the current interval.</summary>
        [JsonIgnore]
        [Units("kPa")]
        public double CanopyVPD { get; private set; }

        /// <summary>Sunlit canopy assimilation during the current interval.</summary>
        [JsonIgnore]
        [Units("umol CO2/m^2/s")]
        public double SunlitAssimilation { get; private set; }

        /// <summary>Sunlit canopy water use during the current interval.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double SunlitWater { get; private set; }

        /// <summary>Sunlit canopy temperature during the current interval.</summary>
        [JsonIgnore]
        [Units("°C")]
        public double SunlitTemperature { get; private set; }

        /// <summary>Sunlit canopy vapour pressure deficit during the current interval.</summary>
        [JsonIgnore]
        [Units("kPa")]
        public double SunlitVPD { get; private set; }

        /// <summary>Sunlit AC1 pathway assimilation during the current interval.</summary>
        [JsonIgnore]
        [Units("umol CO2/m^2/s")]
        public double SunlitAc1 { get; private set; }

        /// <summary>Sunlit AC2 pathway assimilation during the current interval.</summary>
        [JsonIgnore]
        [Units("umol CO2/m^2/s")]
        public double SunlitAc2 { get; private set; }

        /// <summary>Sunlit AJ pathway assimilation during the current interval.</summary>
        [JsonIgnore]
        [Units("umol CO2/m^2/s")]
        public double SunlitAj { get; private set; }

        /// <summary>Shaded canopy assimilation during the current interval.</summary>
        [JsonIgnore]
        [Units("umol CO2/m^2/s")]
        public double ShadedAssimilation { get; private set; }

        /// <summary>Shaded canopy water use during the current interval.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double ShadedWater { get; private set; }

        /// <summary>Shaded canopy temperature during the current interval.</summary>
        [JsonIgnore]
        [Units("°C")]
        public double ShadedTemperature { get; private set; }

        /// <summary>Shaded canopy vapour pressure deficit during the current interval.</summary>
        [JsonIgnore]
        [Units("kPa")]
        public double ShadedVPD { get; private set; }

        /// <summary>Shaded AC1 pathway assimilation during the current interval.</summary>
        [JsonIgnore]
        [Units("umol CO2/m^2/s")]
        public double ShadedAc1 { get; private set; }

        /// <summary>Shaded AC2 pathway assimilation during the current interval.</summary>
        [JsonIgnore]
        [Units("umol CO2/m^2/s")]
        public double ShadedAc2 { get; private set; }

        /// <summary>Shaded AJ pathway assimilation during the current interval.</summary>
        [JsonIgnore]
        [Units("umol CO2/m^2/s")]
        public double ShadedAj { get; private set; }

        /// <summary>
        /// The biological transpiration limit of a plant
        /// </summary>
        public double Biolimit { get; set; } = 0;

        /// <summary>
        /// Excess water reduction fraction
        /// </summary>
        public double Reduction { get; set; } = 0;

        /// <summary>
        /// Adjusts the AC (Rubisco Limited Photosynthesis) curve by modifying photosynthetic AC variables.
        /// </summary>
        public double RubiscoLimitedModifier
        {
            get => rubiscoLimitedModifier;
            set
            {
                ParameterGenerator.ApplyRubiscoLimitedModifier(cropName, Parameters, value);
            }
        }

        /// <summary>
        /// Adjusts the AJ (Electron Transport Limited Photosynthesis) curve by modifying photosynthetic AJ variables.
        /// </summary>
        public double ElectronTransportLimitedModifier
        {
            get => electronTransportLimitedModifier;
            set
            {
                ParameterGenerator.ApplyElectronTransportLimitedModifier(cropName, Parameters, value);
            }
        }

        /// <summary>
        /// A static crop parameter generation object.
        /// </summary>
        public static ICropParameterGenerator ParameterGenerator { get; set; } = new CropParameterGenerator();

        /// <summary>
        /// Model has been fully created. Initialise.
        /// </summary>
        public override void OnCreated()
        {
            base.OnCreated();
            plant = null;
            SetUpPlant();
        }

        /// <summary>
        /// Reset the default DCaPST parameters according to the type of crop.
        /// </summary>
        public void Reset()
        {
            Parameters = ParameterGenerator.Generate(cropName);
            if (Node != null)  // Can be null during deserialisation. Wait until OnCreated for initialise.
            {
                plant = null;
                SetUpPlant();
            }
        }

        /// <summary>
        /// Performs error checking at start of simulation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs args)
        {
            EnsureCropSelected();
            EnsureAmbientCO2Available();
        }

        private void EnsureCropSelected()
        {
            if (string.IsNullOrEmpty(CropName))
            {
                throw new ArgumentNullException(CropName, "No CropName was specified in DCaPST configuration");
            }
        }

        private void EnsureAmbientCO2Available()
        {
            if (weather is null ||
                weather is not Weather weatherModel ||
                Structure.FindChild<CO2Value>(relativeTo: weatherModel) is null
            )
            {
                throw new Exception($"Invalid DCaPST simulation. No {nameof(CO2Value)} model has been configured in {nameof(Weather)} model.");
            }
        }

        /// <summary>
        /// Called once per day when it's time for dcapst to run.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        [EventSubscribe("DoDCAPST")]
        private void OnDoDCaPST(object sender, EventArgs args)
        {
            SetUpPlant();
            CalculateDcapstTrigger();

            if (!ShouldRunDcapstModel())
            {
                return;
            }

            DcapstModel = SetUpModel(
                includeAc2Pathway,
                Parameters,
                clock.Today.DayOfYear,
                weather,
                Parameters.Rpar,
                Biolimit,
                Reduction
            );

            double sln = GetSln();

            DcapstModel.DailyRun(leaf.LAI, sln);

            // Outputs
            foreach (ICanopy canopy in Structure.FindChildren<ICanopy>(relativeTo: plant as INodeModel))
            {
                canopy.LightProfile = new CanopyEnergyBalanceInterceptionlayerType[1]
                {
                    new()
                    {
                        AmountOnGreen = DcapstModel.InterceptedRadiation
                    }
                };

                canopy.WaterDemand = DcapstModel.WaterDemanded;
            }
        }

        /// <summary>
        /// Creates the DCAPST Model.
        /// </summary>
        private static DCAPSTModel SetUpModel(
            bool includeAc2Pathway,
            DCaPSTParameters dcapstParameters,
            int DOY,
            IWeather weather,
            double rpar,
            double biolimit,
            double reduction
        )
        {
            // Model the solar geometry
            var solarGeometry = new SolarGeometry
            {
                Latitude = weather.Latitude.ToRadians(),
                DayOfYear = DOY
            };

            // Model the solar radiation
            var solarRadiation = new SolarRadiation(solarGeometry)
            {
                Daily = weather.Radn,
                RPAR = rpar
            };

            // Model the environmental temperature
            var temperature = new Temperature(solarGeometry)
            {
                MaxTemperature = weather.MaxT,
                MinTemperature = weather.MinT,
                AtmosphericPressure = 1.01325
            };

            var ambientCO2 = weather.CO2;
            var canopyParameters = dcapstParameters.Canopy;
            var pathwayParameters = dcapstParameters.Pathway;

            // Model the pathways
            var sunlitAc1 = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);
            var sunlitAc2 = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);
            var sunlitAj = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);

            var shadedAc1 = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);
            var shadedAc2 = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);
            var shadedAj = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);

            IAssimilation assimilation = canopyParameters.Type switch
            {
                CanopyType.C3 => new AssimilationC3(dcapstParameters, canopyParameters, pathwayParameters, ambientCO2),
                CanopyType.C4 => new AssimilationC4(dcapstParameters, canopyParameters, pathwayParameters, ambientCO2),
                CanopyType.CCM => new AssimilationCCM(dcapstParameters, canopyParameters, pathwayParameters, ambientCO2),
                _ => throw new ArgumentException($"Unsupported canopy type: {canopyParameters.Type}"),
            };

            var sunlit = new AssimilationArea(includeAc2Pathway, sunlitAc1, sunlitAc2, sunlitAj, assimilation);
            var shaded = new AssimilationArea(includeAc2Pathway, shadedAc1, shadedAc2, shadedAj, assimilation);
            var canopyAttributes = new CanopyAttributes(dcapstParameters, sunlit, shaded);

            // Model the transpiration
            var waterInteraction = new WaterInteraction(temperature);
            var temperatureResponse = new TemperatureResponse(canopyParameters, pathwayParameters);
            var transpiration = new Transpiration(canopyParameters, pathwayParameters, waterInteraction, temperatureResponse, ambientCO2);

            // Model the photosynthesis
            return new DCAPSTModel(
                solarGeometry,
                solarRadiation,
                temperature,
                pathwayParameters,
                canopyAttributes,
                transpiration
            )
            {
                // From here, we can set additional options,
                // such as verbosity, BioLimit, Reduction, etc.
                PrintIntervalValues = false,
                Biolimit = biolimit,
                Reduction = reduction,
            };
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (DcapstModel is null) return;

            double rootShootRatio = rootShootRatioFunction.Value();
            double soilWaterValue = GetSoilWaterAvailable();
            DcapstModel.CalculateBiomass(soilWaterValue, rootShootRatio);

            if (leaf is SorghumLeaf sorghumLeaf)
            {
                if (DcapstModel.InterceptedRadiation > 0)
                {
                    sorghumLeaf.BiomassRUE = DcapstModel.ActualBiomass;
                }
                if (DcapstModel.WaterSupplied > 0)
                {
                    sorghumLeaf.BiomassTE = DcapstModel.ActualBiomass;
                    sorghumLeaf.TranspirationEfficiency = DcapstModel.ActualBiomass / DcapstModel.WaterSupplied;
                }
            }
            else if (leaf is Leaf complexLeaf)
            {
                complexLeaf.DMSupply.Fixation = DcapstModel.ActualBiomass;
            }
            else
            {
                throw new InvalidOperationException($"Unable to set biomass from unknown leaf type {leaf.GetType()}");
            }

            PublishIntervalOutputs();
        }

        /// <summary>
        /// Copies each calculated interval into reportable scalar properties and
        /// raises <see cref="IntervalStep"/> for the report model.
        /// </summary>
        private void PublishIntervalOutputs()
        {
            if (IntervalStep is null || DcapstModel?.Intervals is null)
                return;

            foreach (IntervalValues interval in DcapstModel.Intervals)
            {
                Hour = interval.Time;
                IntervalDateTime = clock.Today.Date.AddHours(Hour);
                AirTemperature = interval.AirTemperature;
                SunlitLAI = interval.SunlitLAI;
                ShadedLAI = interval.ShadedLAI;

                SunlitAssimilation = interval.Sunlit.A;
                SunlitWater = interval.Sunlit.Water;
                SunlitTemperature = interval.Sunlit.Temperature;
                SunlitVPD = interval.Sunlit.VPD;
                SunlitAc1 = interval.Sunlit.Ac1.Assimilation;
                SunlitAc2 = interval.Sunlit.Ac2.Assimilation;
                SunlitAj = interval.Sunlit.Aj.Assimilation;

                ShadedAssimilation = interval.Shaded.A;
                ShadedWater = interval.Shaded.Water;
                ShadedTemperature = interval.Shaded.Temperature;
                ShadedVPD = interval.Shaded.VPD;
                ShadedAc1 = interval.Shaded.Ac1.Assimilation;
                ShadedAc2 = interval.Shaded.Ac2.Assimilation;
                ShadedAj = interval.Shaded.Aj.Assimilation;

                double totalLAI = SunlitLAI + ShadedLAI;
                CanopyTemperature = LAIWeightedMean(SunlitTemperature, SunlitLAI, ShadedTemperature, ShadedLAI, totalLAI);
                CanopyVPD = LAIWeightedMean(SunlitVPD, SunlitLAI, ShadedVPD, ShadedLAI, totalLAI);

                IntervalStep.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Calculates a canopy mean weighted by sunlit and shaded leaf area.</summary>
        private static double LAIWeightedMean(double sunlitValue, double sunlitLAI, double shadedValue, double shadedLAI, double totalLAI)
        {
            if (totalLAI <= 0)
                return 0;

            return (sunlitValue * sunlitLAI + shadedValue * shadedLAI) / totalLAI;
        }

        private double GetSoilWaterAvailable()
        {
            double soilWaterAvailable = soilWater.SW.Sum();

            if (leaf is SorghumLeaf &&
                waterUptakeMethod is C4WaterUptakeMethod c4WaterUptakeMethod)
            {
                soilWaterAvailable = c4WaterUptakeMethod.WatSupply;
            }

            return soilWaterAvailable;
        }

        private bool ShouldRunDcapstModel()
        {
            if (leaf is null) return false;

            return
                leaf.LAI > 0.0 &&
                dcapsReachedLAITriggerPoint;
        }

        private void SetUpPlant()
        {
            if (string.IsNullOrEmpty(cropName)) return;
            if (plant != null) return;

            plant = Structure.Find<IPlant>(CropName);
            rootShootRatioFunction = GetRootShootRatioFunction();
            leaf = GetLeaf();
        }

        private ICanopy GetLeaf()
        {
            if (plant == null) return null;
			ICanopy find = Structure.FindChild<ICanopy>("Leaf", relativeTo: plant as INodeModel);
            if (find == null) throw new ArgumentNullException(nameof(find), "Cannot find leaf configuration");
            return find;
        }

        private IFunction GetRootShootRatioFunction()
        {
            if (plant is null) return null;

            var variable = Structure.GetObject("[ratioRootShoot]", relativeTo:plant as INodeModel);
            if (variable is null) return null;
            if (variable.Value is not IFunction function) return null;

            return function;
        }

        private void CalculateDcapstTrigger()
        {
            if (!dcapsReachedLAITriggerPoint &&
                leaf.LAI >= LEAF_LAI_START_USING_DCAPST_TRIGGER)
            {
                dcapsReachedLAITriggerPoint = true;
                SetMicroClimateForSpecificLeafTypes(1);
            }
        }

        private void SetMicroClimateForSpecificLeafTypes(int microClimateSetting)
        {
            // Sorghum calculates InterceptedRadiation and WaterDemand internally
            // Use the MicroClimateSetting to override.
            if (leaf is SorghumLeaf sorghumLeaf)
            {
                sorghumLeaf.MicroClimateSetting = microClimateSetting;
            }
        }

        /// <summary>Called when crop is being sown</summary>
        /// <param name="sender"></param>
        /// <param name="sowingData"></param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters sowingData)
        {
            // Reset DCAPST trigger point because the crop has just been sown again and we don't want to start using DCAPST
            // until it is triggered again (LAI dependent).
            dcapsReachedLAITriggerPoint = false;
            SetMicroClimateForSpecificLeafTypes(0);
            DcapstModel = new();

            SetCultivarOverrides(sowingData);
        }

        private void SetCultivarOverrides(SowingParameters sowingData)
        {
            // DcAPST allows specific Crop and Cultivar settings to be used.
            // Search and extract the Cultivar if it has been specified.
            var cultivar = SowingParametersParser.GetCultivarFromSowingParameters(this, sowingData);
            if (cultivar is null) return;

            // We've got a Cultivar so apply all of the specified overrides to manipulate this models settings.
            cultivar.Apply(this);
        }

        private double GetSln()
        {
            if (leaf is SorghumLeaf sorghumLeaf)
            {
                return sorghumLeaf.SLN;
            }
            if (leaf is IArbitration arbitration)
            {
                return arbitration.Live.N / leaf.LAI;
            }
            throw new InvalidOperationException($"Unable to calculate SLN from leaf type {leaf.GetType()}");
        }

        /// <summary>
        /// Get the names of all plants in scope.
        /// </summary>
        private IEnumerable<string> GetPlantNames()
        {
            var plants = Structure.FindAll<IPlant>()
                .Select(p => p.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct();

            return plants;
        }
    }
}
