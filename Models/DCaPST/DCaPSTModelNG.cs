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
    public class DCaPSTModelNG : Model
    {
        /// <summary>
        /// Clock object reference (dcapst needs to know day of year).
        /// </summary>
        [Link]
        private readonly IClock clock = null;

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
        /// Reset the default DCaPST parameters according to the type of crop.
        /// </summary>
        public void Reset()
        {
            Parameters = ParameterGenerator.Generate(cropName);
            plant = null;
            SetUpPlant();
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
                weatherModel.FindChild<CO2Value>() is null
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
                Parameters.Canopy,
                Parameters.Pathway,
                clock.Today.DayOfYear,
                weather,
                Parameters.Rpar,
                Biolimit,
                Reduction
            );

            double sln = GetSln();

            DcapstModel.DailyRun(leaf.LAI, sln);

            // Outputs
            foreach (ICanopy canopy in plant.FindAllChildren<ICanopy>())
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
            CanopyParameters canopyParameters,
            PathwayParameters pathwayParameters,
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

            // Model the pathways
            var sunlitAc1 = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);
            var sunlitAc2 = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);
            var sunlitAj = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);

            var shadedAc1 = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);
            var shadedAc2 = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);
            var shadedAj = new AssimilationPathway(canopyParameters, pathwayParameters, ambientCO2);

            IAssimilation assimilation = canopyParameters.Type switch
            {
                CanopyType.C3 => new AssimilationC3(canopyParameters, pathwayParameters, ambientCO2),
                CanopyType.C4 => new AssimilationC4(canopyParameters, pathwayParameters, ambientCO2),
                CanopyType.CCM => new AssimilationCCM(canopyParameters, pathwayParameters, ambientCO2),
                _ => throw new ArgumentException($"Unsupported canopy type: {canopyParameters.Type}"),
            };

            var sunlit = new AssimilationArea(includeAc2Pathway, sunlitAc1, sunlitAc2, sunlitAj, assimilation);
            var shaded = new AssimilationArea(includeAc2Pathway, shadedAc1, shadedAc2, shadedAj, assimilation);
            var canopyAttributes = new CanopyAttributes(canopyParameters, pathwayParameters, sunlit, shaded);

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

            plant = FindInScope<IPlant>(CropName);
            rootShootRatioFunction = GetRootShootRatioFunction();
            leaf = GetLeaf();
        }

        private ICanopy GetLeaf()
        {
            if (plant == null) return null;
			ICanopy find = plant.FindChild<ICanopy>("Leaf");
            if (find == null) throw new ArgumentNullException(nameof(find), "Cannot find leaf configuration");
            return find;
        }

        private IFunction GetRootShootRatioFunction()
        {
            if (plant is null) return null;

            IVariable variable = plant.FindByPath("[ratioRootShoot]");
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
            var plants = FindAllInScope<IPlant>()
                .Select(p => p.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct();

            return plants;
        }
    }
}
