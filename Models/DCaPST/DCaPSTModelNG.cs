using Models.Core;
using Models.DCAPST.Canopy;
using Models.DCAPST.Environment;
using Models.DCAPST.Interfaces;
using Models.Functions;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using System;
using System.Collections.Generic;
using System.Linq;

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
        private IClock clock = null;

        /// <summary>
        /// Weather provider.
        /// </summary>
        [Link]
        private IWeather weather = null;

        /// <summary>
        /// Soil water balance.
        /// </summary>
        [Link]
        private ISoilWater soilWater = null;

        /// <summary>
        /// The chosen crop name.
        /// </summary>
        private string cropName = string.Empty;

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
                    HandleCropChange();
                }
            }
        }

        /// <summary>
        /// The DCaPST Parameters.
        /// </summary>
        public DCaPSTParameters Parameters { get; set; } = new DCaPSTParameters();

        /// <summary>
        /// Performs error checking at start of simulation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs args)
        {
            if (string.IsNullOrEmpty(CropName))
            {
                throw new ArgumentNullException($"No crop was specified in DCaPST configuration");
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
            IPlant plant = FindInScope<IPlant>(CropName);
            double rootShootRatio = GetRootShootRatio(plant);

            DCAPSTModel model = SetUpModel(
                Parameters.Canopy,
                Parameters.Pathway,
                clock.Today.DayOfYear,
                weather.Latitude,
                weather.MaxT,
                weather.MinT,
                weather.Radn,
                Parameters.Rpar
            );

            // From here, we can set additional options,
            // such as verbosity, BioLimit, Reduction, etc.

            // 0. Get SLN, LAI, total avail SW, root shoot ratio
            // 1. Perform internal calculations
            // 2. Set biomass production in leaf
            // 3. Set water demand and potential EP via ICanopy

            // fixme - are we using the right SW??
            ICanopy leaf = plant.FindChild<ICanopy>("Leaf");
            if (leaf == null)
            {
                throw new Exception($"Unable to run DCaPST on plant {plant.Name}: plant has no leaf which implements ICanopy");
            }
            if (leaf.LAI > 0)
            {
                double sln = GetSln(leaf);
                model.DailyRun(leaf.LAI, sln, soilWater.SW.Sum(), rootShootRatio);

                // Outputs
                SetBiomass(leaf, model.ActualBiomass);
                foreach (ICanopy canopy in plant.FindAllChildren<ICanopy>())
                {
                    canopy.LightProfile = new CanopyEnergyBalanceInterceptionlayerType[1]
                    {
                        new CanopyEnergyBalanceInterceptionlayerType()
                        {
                            AmountOnGreen = model.InterceptedRadiation,
                        }
                    };
                    canopy.PotentialEP = canopy.WaterDemand = model.WaterDemanded;
                }
            }
        }

        /// <summary>Called when crop is being sown</summary>
        /// <param name="sender"></param>
        /// <param name="sowingData"></param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters sowingData)
        {
            // DcAPST allows specific Crop and Cultivar settings to be used.
            // Search and extract the Cultivar if it has been specified.
            var cultivar = SowingParametersParser.GetCultivarFromSowingParameters(this, sowingData);
            if (cultivar is null) return;

            // We've got a Cultivar so apply all of the specified overrides to manipulate this models settings.
            cultivar.Apply(this);
        }

        private double GetRootShootRatio(IPlant plant)
        {
            IVariable variable = plant.FindByPath("[ratioRootShoot]");
            if (variable == null)
            {
                return 0;
            }

            IFunction function = variable.Value as IFunction;
            if (function == null)
            {
                return 0;
            }

            return function.Value();
        }

        private void SetBiomass(ICanopy leaf, double actualBiomass)
        {
            if (leaf is SorghumLeaf sorghumLeaf)
            {
                sorghumLeaf.BiomassRUE = actualBiomass;
                sorghumLeaf.BiomassTE = actualBiomass;
            }
            else if (leaf is Leaf complexLeaf)
            {
                complexLeaf.DMSupply.Fixation = actualBiomass;
            }
            else
            {
                throw new InvalidOperationException($"Unable to set biomass from unknown leaf type {leaf.GetType()}");
            }
        }

        private double GetSln(ICanopy leaf)
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
        /// 
        /// </summary>
        /// <param name="canopyParameters"></param>
        /// <param name="pathwayParameters"></param>
        /// <param name="DOY"></param>
        /// <param name="latitude"></param>
        /// <param name="maxT"></param>
        /// <param name="minT"></param>
        /// <param name="radn"></param>
        /// <param name="rpar"></param>
        /// <returns></returns>
        public static DCAPSTModel SetUpModel(
            ICanopyParameters canopyParameters, 
            IPathwayParameters pathwayParameters,
            int DOY, 
            double latitude, 
            double maxT, 
            double minT, 
            double radn,
            double rpar)
        {
            // Model the solar geometry
            var solarGeometry = new SolarGeometry
            {
                Latitude = latitude.ToRadians(),
                DayOfYear = DOY
            };

            // Model the solar radiation
            var solarRadiation = new SolarRadiation(solarGeometry)
            {
                Daily = radn,
                RPAR = rpar
            };

            // Model the environmental temperature
            var temperature = new Temperature(solarGeometry)
            {
                MaxTemperature = maxT,
                MinTemperature = minT,
                AtmosphericPressure = 1.01325
            };

            // Model the pathways
            var sunlitAc1 = new AssimilationPathway(canopyParameters, pathwayParameters);
            var sunlitAc2 = new AssimilationPathway(canopyParameters, pathwayParameters);
            var sunlitAj = new AssimilationPathway(canopyParameters, pathwayParameters);

            var shadedAc1 = new AssimilationPathway(canopyParameters, pathwayParameters);
            var shadedAc2 = new AssimilationPathway(canopyParameters, pathwayParameters);
            var shadedAj = new AssimilationPathway(canopyParameters, pathwayParameters);

            IAssimilation assimilation = canopyParameters.Type switch
            {
                CanopyType.C3 => new AssimilationC3(canopyParameters, pathwayParameters),
                CanopyType.C4 => new AssimilationC4(canopyParameters, pathwayParameters),
                _ => new AssimilationCCM(canopyParameters, pathwayParameters),
            };

            var sunlit = new AssimilationArea(sunlitAc1, sunlitAc2, sunlitAj, assimilation);
            var shaded = new AssimilationArea(shadedAc1, shadedAc2, shadedAj, assimilation);
            var canopyAttributes = new CanopyAttributes(canopyParameters, pathwayParameters, sunlit, shaded);

            // Model the transpiration
            var waterInteraction = new WaterInteraction(temperature);
            var temperatureResponse = new TemperatureResponse(canopyParameters, pathwayParameters);
            var transpiration = new Transpiration(canopyParameters, pathwayParameters, waterInteraction, temperatureResponse);

            // Model the photosynthesis
            var dcapstModel = new DCAPSTModel(solarGeometry, solarRadiation, temperature, pathwayParameters, canopyAttributes, transpiration)
            {
                B = 0.409
            };

            return dcapstModel;
        }

        /// <summary>
        /// Get the names of all plants in scope.
        /// </summary>
        private IEnumerable<string> GetPlantNames() => FindAllInScope<IPlant>().Select(p => p.Name);

        /// <summary>
        /// Reset the default DCaPST parameters according to the type of crop.
        /// </summary>
        private void HandleCropChange()
        {
            var parameterGenerator = new CropParameterGenerator();
            Parameters = parameterGenerator.Generate(cropName) ?? new DCaPSTParameters();
        }
    }
}