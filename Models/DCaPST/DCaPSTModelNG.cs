using System;
using System.Linq;
using Models.Core;
using Models.DCAPST.Canopy;
using Models.DCAPST.Environment;
using Models.DCAPST.Interfaces;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Organs;

namespace Models.DCAPST
{
    /// <summary>
    /// APSIM Next Generation wrapper around the DCaPST model.
    /// </summary>
    [Serializable]
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
        /// Canopy parameters, as specified by user.
        /// </summary>
        [Link(Type = LinkType.Child)]
        private DCaPSTParameters parameters = null;

        /// <summary>
        /// Link to sorghum leaf - temp hack, need to consider how to
        /// access SLN in a crop-agnostic manner.
        /// </summary>
        [Link]
        private SorghumLeaf leaf = null;

        /// <summary>
        /// Performs error checking at start of simulation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs args)
        {
            if (string.IsNullOrEmpty(parameters.CropName))
                throw new ArgumentNullException($"No crop was specified in DCaPST configuration");
        }

        /// <summary>
        /// Called once per day when it's time for dcapst to run.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        [EventSubscribe("DoDCAPST")]
        private void OnDoDCaPST(object sender, EventArgs args)
        {
            IModel plant = FindInScope(parameters.CropName);
            double rootShootRatio = ((IFunction)plant.FindByPath("[ratioRootShoot]").Value).Value();
            DCAPSTModel model = SetUpModel(parameters.Canopy,
                                           parameters.Pathway,
                                           clock.Today.DayOfYear,
                                           weather.Latitude,
                                           weather.MaxT,
                                           weather.MinT,
                                           weather.Radn,
                                           parameters.Rpar);
            // From here, we can set additional options,
            // such as verbosity, BioLimit, Reduction, etc.

            // fixme - are we using the right SW??
            if (leaf.LAI > 0)
            {
                model.DailyRun(leaf.LAI, leaf.SLN, soilWater.SW.Sum(), rootShootRatio);

                // Outputs
                leaf.BiomassRUE = leaf.BiomassTE = model.ActualBiomass;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="CP"></param>
        /// <param name="PP"></param>
        /// <param name="DOY"></param>
        /// <param name="latitude"></param>
        /// <param name="maxT"></param>
        /// <param name="minT"></param>
        /// <param name="radn"></param>
        /// <param name="rpar"></param>
        /// <returns></returns>
        public static DCAPSTModel SetUpModel(
            ICanopyParameters CP, 
            IPathwayParameters PP,
            int DOY, 
            double latitude, 
            double maxT, 
            double minT, 
            double radn,
            double rpar)
        {
            // Model the solar geometry
            var SG = new SolarGeometry
            {
                Latitude = latitude.ToRadians(),
                DayOfYear = DOY
            };

            // Model the solar radiation
            var SR = new SolarRadiation(SG)
            {
                Daily = radn,
                RPAR = rpar
            };

            // Model the environmental temperature
            var TM = new Temperature(SG)
            {
                MaxTemperature = maxT,
                MinTemperature = minT,
                AtmosphericPressure = 1.01325
            };

            // Model the pathways
            var SunlitAc1 = new AssimilationPathway(CP, PP);
            var SunlitAc2 = new AssimilationPathway(CP, PP);
            var SunlitAj = new AssimilationPathway(CP, PP);

            var ShadedAc1 = new AssimilationPathway(CP, PP);
            var ShadedAc2 = new AssimilationPathway(CP, PP);
            var ShadedAj = new AssimilationPathway(CP, PP);

            // Model the canopy
            IAssimilation A;
            if (CP.Type == CanopyType.C3)
                A = new AssimilationC3(CP, PP);
            else if (CP.Type == CanopyType.C4)
                A = new AssimilationC4(CP, PP);
            else
                A = new AssimilationCCM(CP, PP);

            var sunlit = new AssimilationArea(SunlitAc1, SunlitAc2, SunlitAj, A);
            var shaded = new AssimilationArea(ShadedAc1, ShadedAc2, ShadedAj, A);
            var CA = new CanopyAttributes(CP, PP, sunlit, shaded);

            // Model the transpiration
            var WI = new WaterInteraction(TM);
            var TR = new TemperatureResponse(CP, PP);
            var TS = new Transpiration(CP, PP, WI, TR);

            // Model the photosynthesis
            var DM = new DCAPSTModel(SG, SR, TM, PP, CA, TS)
            {
                B = 0.409
            };

            return DM;
        }
    }
}