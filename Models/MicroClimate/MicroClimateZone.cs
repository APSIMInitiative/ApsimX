namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using System;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// A micro climate wrapper around a Zone instance.
    /// </summary>
    /// <remarks>
    /// We need to store Radn, MaxT and MinT in here rather than weather because
    /// of timestep issues. e.g. A manager module (in DoManagement) asks for 
    /// CanopyCover from this zone. It is:
    ///    RadiationIntercepted (yesterdays value) / weather.Radn (todays value)
    /// RadiationIntercepted isn't updated until after DoManagement.
    /// </remarks>
    [Serializable]
    public class MicroClimateZone
    {
        /// <summary>The clock model.</summary>
        private Clock clock;

        /// <summary>Solar radiation.</summary>
        private double Radn;

        /// <summary>Maximum temperature.</summary>
        private double MaxT;

        /// <summary>Maximum temperature.</summary>
        private double MinT;

        /// <summary>Rainfall.</summary>
        private double Rain;

        /// <summary>Vapour pressure.</summary>
        private double VP;

        /// <summary>Air pressure.</summary>
        private double AirPressure;

        /// <summary>Wind.</summary>
        private double Wind;

        /// <summary>Latitude.</summary>
        private double Latitude;

        /// <summary>The surface organic matter model.</summary>
        private ISurfaceOrganicMatter surfaceOM;

        /// <summary>The soil water model.</summary>
        private ISoilWater soilWater;

        /// <summary>Models in the simulation that implement ICanopy.</summary>
        private IEnumerable<ICanopy> canopyModels;

        /// <summary>Models in the simulation that implement IHaveCanopy.</summary>
        private IEnumerable<IHaveCanopy> modelsThatHaveCanopies;

        /// <summary>Canopy emissivity</summary>
        private const double canopyEmissivity = 0.96;

        /// <summary>The soil_emissivity</summary>
        private const double soilEmissivity = 0.96;

        /// <summary>Convert hours to seconds</summary>
        private const double hr2s = 60.0 * 60.0;

        /// <summary>convert degrees to radians</summary>
        private const double deg2Rad = Math.PI / 180.0;

        /// <summary>0 C in Kelvin (k)</summary>
        private const double abs_temp = 273.16;

        /// <summary>constant for cloud effect on longwave radiation</summary>
        private const double c_cloud = 0.1;

        /// <summary>Stefan-Boltzman constant</summary>
        private const double stef_boltz = 5.67E-08;

        /// <summary>von Karman constant</summary>
        private const double vonKarman = 0.41;

        /// <summary>The SVP_ a Teten coefficient</summary>
        private const double svp_A = 6.106;

        /// <summary>The SVP_ b Teten coefficient</summary>
        private const double svp_B = 17.27;

        /// <summary>The SVP_ c Teten coefficient</summary> 
        private const double svp_C = 237.3;

        /// <summary>molecular weight water (kg/mol)</summary>
        private const double mwh2o = 0.018016;

        /// <summary>molecular weight air (kg/mol)</summary>
        private const double mwair = 0.02897;

        /// <summary>molecular fraction of water to air ()</summary>
        private const double molef = mwh2o / mwair;

        /// <summary>Specific heat of air at constant pressure (J/kg/K)</summary>
        private const double cp = 1010.0;

        /// <summary>Density of water (kg/m3)</summary>
        private const double RhoW = 998.0;

        /// <summary>universal gas constant (J/mol/K)</summary>
        private const double r_gas = 8.3143;

        /// <summary>weights vpd towards vpd at maximum temperature</summary>
        private const double svp_fract = 0.66;

        /// <summary>Temperature below which eeq decreases (oC).</summary>
        private const double min_crit_temp = 5.0;

        /// <summary>Temperature above which eeq increases (oC).</summary>
        private const double max_crit_temp = 35.0;

        /// <summary>Albedo at 100% green crop cover (0-1).</summary>
        private const double max_albedo = 0.23;

        /// <summary>Albedo at 100% residue cover (0-1).</summary>
        private const double residue_albedo = 0.23;

        /// <summary>The Albedo of the combined soil-plant system for this zone</summary>
        public double Albedo;

        /// <summary>Emissivity of the combined soil-plant system for this zone.</summary>
        public double Emissivity;

        /// <summary>Net long-wave radiation of the whole system (MJ/m2/day)</summary>
        public double NetLongWaveRadiation;

        /// <summary>The sum rs</summary>
        public double sumRs;

        /// <summary>The incoming rs</summary>
        public double IncomingRs;

        /// <summary>The shortwave radiation reaching the surface</summary>
        public double SurfaceRs;

        /// <summary>The delta z</summary>
        public double[] DeltaZ = new double[-1 + 1];

        /// <summary>The layer ktot</summary>
        public double[] layerKtot = new double[-1 + 1];

        /// <summary>The layer la isum</summary>
        public double[] LAItotsum = new double[-1 + 1];

        /// <summary>The number layers</summary>
        public int numLayers;

        /// <summary>The soil heat flux</summary>
        public double SoilHeatFlux;

        /// <summary>The dry leaf time fraction</summary>
        public double DryLeafFraction;

        /// <summary>The height difference between canopies required for a new layer to be created (m).</summary>
        public double MinimumHeightDiffForNewLayer { get; set; }


        /// <summary>Gets or sets the component data.</summary>
        public List<MicroClimateCanopy> Canopies = new List<MicroClimateCanopy>();

        /// <summary>Constructor.</summary>
        /// <param name="clockModel">The clock model.</param>
        /// <param name="zoneModel">The zone model.</param>
        /// <param name="minHeightDiffForNewLayer">Minimum canopy height diff for new layer.</param>
        public MicroClimateZone(Clock clockModel, Zone zoneModel, double minHeightDiffForNewLayer)
        {
            clock = clockModel;
            Zone = zoneModel;
            MinimumHeightDiffForNewLayer = minHeightDiffForNewLayer;
            canopyModels = Apsim.ChildrenRecursively(Zone, typeof(ICanopy)).Cast<ICanopy>();
            modelsThatHaveCanopies = Apsim.ChildrenRecursively(Zone, typeof(IHaveCanopy)).Cast<IHaveCanopy>();
            soilWater = Apsim.Find(Zone, typeof(ISoilWater)) as ISoilWater;
            surfaceOM = Apsim.Find(Zone, typeof(ISurfaceOrganicMatter)) as ISurfaceOrganicMatter;
        }

        /// <summary>The zone model.</summary>
        public Zone Zone { get; }

        /// <summary>Gets the intercepted precipitation.</summary>
        [Description("Intercepted precipitation")]
        [Units("mm")]
        public double PrecipitationInterception
        {
            get
            {
                double totalInterception = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                        totalInterception += Canopies[j].interception[i];
                return totalInterception;
            }
        }

        /// <summary>Gets the intercepted radiation.</summary>
        [Description("Intercepted radiation")]
        [Units("MJ/m2")]
        public double RadiationInterception
        {
            get
            {
                double totalInterception = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                        totalInterception += Canopies[j].Rs[i];
                return totalInterception;
            }
        }

        /// <summary>Gets the total canopy cover.</summary>
        [Description("Total canopy cover (0-1)")]
        [Units("0-1")]
        public double CanopyCover {  get { return RadiationInterception / Radn; } }

        /// <summary>Gets the radiation term of PET.</summary>
        [Description("Radiation component of PET")]
        [Units("mm")]
        public double petr
        {
            get
            {
                double totalPetr = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                        totalPetr += Canopies[j].PETr[i];
                return totalPetr;
            }
        }

        /// <summary>Gets the aerodynamic term of PET.</summary>
        [Description("Aerodynamic component of PET")]
        [Units("mm")]
        public double peta
        {
            get
            {
                double totalPeta = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                        totalPeta += Canopies[j].PETa[i];
                return totalPeta;
            }
        }

        /// <summary>Gets the total net radiation.</summary>
        [Description("Net all-wave radiation of the whole system")]
        [Units("MJ/m2")]
        public double NetRadiation
        {
            get { return NetShortWaveRadiation + NetLongWaveRadiation; }
        }

        /// <summary>Gets the net short wave radiation.</summary>
        [Description("Net short-wave radiation of the whole system")]
        [Units("MJ/m2")]
        public double NetShortWaveRadiation
        {
            get { return Radn * (1.0 - Albedo); }
        }

        /// <summary>Called at the start of day to initialise the zone for the day.</summary>
        /// <param name="weatherModel">The weather model.</param>
        public void DailyInitialise(IWeather weatherModel)
        {
            Radn = weatherModel.Radn;
            MaxT = weatherModel.MaxT;
            MinT = weatherModel.MinT;
            Rain = weatherModel.Rain;
            VP = weatherModel.VP;
            AirPressure = weatherModel.AirPressure;
            Latitude = weatherModel.Latitude;
            Wind = weatherModel.Wind;

            Albedo = 0;
            Emissivity = 0;
            NetLongWaveRadiation = 0;
            sumRs = 0;
            IncomingRs = 0;
            SurfaceRs = 0.0;
            DeltaZ = new double[-1 + 1];
            layerKtot = new double[-1 + 1];
            LAItotsum = new double[-1 + 1];
            numLayers = 0;
            SoilHeatFlux = 0.0;
            DryLeafFraction = 0.0;

            // Canopies come and go each day so clear the list of canopies and 
            // go find the canopies we need to work with today.
            Canopies.Clear();

            // There are two ways to finding canopies in the simulation.
            // 1. Some models ARE canopies e.g. Leaf, SimpleLeaf.
            foreach (ICanopy canopy in canopyModels)
                if (canopy.Height > 0)
                    Canopies.Add(new MicroClimateCanopy(canopy));

            // 2. Some models HAVE canopies e.g. SurfaceOM.
            foreach (var modelThatHasCanopy in modelsThatHaveCanopies)
                modelThatHasCanopy.Canopies.ForEach(canopy => Canopies.Add(new MicroClimateCanopy(canopy)));
        }

        /// <summary>Canopies the compartments.</summary>
        public void DoCanopyCompartments()
        {
            DefineLayers();
            DivideComponents();
            CalculateLightExtinctionVariables();
        }

        /// <summary>Calculate the overall system energy terms.</summary>
        /// <param name="soilAlbedo">Soil albedo.</param>
        public void CalculateEnergyTerms(double soilAlbedo)
        {
            sumRs = 0.0;
            Albedo = 0.0;
            Emissivity = 0.0;

            for (int i = numLayers - 1; i >= 0; i += -1)
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    Albedo += MathUtilities.Divide(Canopies[j].Rs[i], Radn, 0.0) * Canopies[j].Canopy.Albedo;
                    Emissivity += MathUtilities.Divide(Canopies[j].Rs[i], Radn, 0.0) * canopyEmissivity;
                    sumRs += Canopies[j].Rs[i];
                }

            Albedo += (1.0 - MathUtilities.Divide(sumRs, Radn, 0.0)) * soilAlbedo;
            Emissivity += (1.0 - MathUtilities.Divide(sumRs, Radn, 0.0)) * soilEmissivity;
        }

        /// <summary>
        /// Calculate Net Long Wave Radiation Balance
        /// </summary>
        /// <param name="dayLengthLight">This is the length of time within the day during which evaporation will take place.</param>
        /// <param name="dayLengthEvap">This is the length of time within the day during which the sun is above the horizon.</param>
        public void CalculateLongWaveRadiation(double dayLengthLight, double dayLengthEvap)
        {
            double sunshineHours = CalcSunshineHours(Radn, dayLengthLight, Latitude, clock.Today.DayOfYear);
            double fractionClearSky = MathUtilities.Divide(sunshineHours, dayLengthLight, 0.0);
            double averageT = CalcAverageT(MinT, MaxT);
            NetLongWaveRadiation = LongWave(averageT, fractionClearSky, Emissivity) * dayLengthEvap * hr2s / 1000000.0;             // W to MJ

            // Long Wave Balance Proportional to Short Wave Balance
            // ====================================================
            for (int i = numLayers - 1; i >= 0; i += -1)
                for (int j = 0; j <= Canopies.Count - 1; j++)
                    Canopies[j].Rl[i] = MathUtilities.Divide(Canopies[j].Rs[i], Radn, 0.0) * NetLongWaveRadiation;
        }

        /// <summary>
        /// Calculate Radiation loss to soil heating
        /// </summary>
        /// <param name="SoilHeatFluxFraction">Fraction of solar radiation reaching the soil surface that results in soil heating.</param>
        public void CalculateSoilHeatRadiation(double SoilHeatFluxFraction)
        {
            double radnint = sumRs;   // Intercepted SW radiation
            SoilHeatFlux = CalculateSoilHeatFlux(Radn, radnint, SoilHeatFluxFraction);

            // SoilHeat balance Proportional to Short Wave Balance
            // ====================================================
            for (int i = numLayers - 1; i >= 0; i += -1)
                for (int j = 0; j <= Canopies.Count - 1; j++)
                    Canopies[j].Rsoil[i] = MathUtilities.Divide(Canopies[j].Rs[i], Radn, 0.0) * SoilHeatFlux;
        }

        /// <summary>Calculate the canopy conductance for system compartments</summary>
        /// <param name="dayLengthEvap">This is the length of time within the day during which the sun is above the horizon.</param>
        public void CalculateGc(double dayLengthEvap)
        {
            double Rin = Radn;

            for (int i = numLayers - 1; i >= 0; i += -1)
            {
                double Rflux = Rin * 1000000.0 / (dayLengthEvap * hr2s) * (1.0 - Albedo);
                double Rint = 0.0;

                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    Canopies[j].Gc[i] = CropCanopyConductance(Canopies[j].Canopy.Gsmax, Canopies[j].Canopy.R50, Canopies[j].Fgreen[i], layerKtot[i], LAItotsum[i], Rflux);
                    Rint += Canopies[j].Rs[i];
                }
                // Calculate Rin for the next layer down
                Rin -= Rint;
            }
        }

        /// <summary>Calculate the aerodynamic conductance for system compartments</summary>
        /// <param name="ReferenceHeight">Height of the weather instruments.</param>
        public void CalculateGa(double ReferenceHeight)
        {
            double sumDeltaZ = MathUtilities.Sum(DeltaZ);
            double sumLAI = MathUtilities.Sum(LAItotsum);
            double totalGa = AerodynamicConductanceFAO(Wind, ReferenceHeight, sumDeltaZ, sumLAI);

            for (int i = 0; i <= numLayers - 1; i++)
                for (int j = 0; j <= Canopies.Count - 1; j++)
                    Canopies[j].Ga[i] = totalGa * MathUtilities.Divide(Canopies[j].Rs[i], sumRs, 0.0);
        }

        /// <summary>Calculate the interception loss of water from the canopy</summary>
        /// <param name="a_interception">Multiplier on rainfall to calculate interception losses.</param>
        /// <param name="b_interception">Power on rainfall to calculate interception losses.</param>
        /// <param name="c_interception">Multiplier on LAI to calculate interception losses.</param>
        /// <param name="d_interception">Constant value to add to calculate interception losses.</param>
        public void CalculateInterception(double a_interception, double b_interception, double c_interception, double d_interception)
        {
            double sumLAI = 0.0;
            double sumLAItot = 0.0;
            for (int i = 0; i <= numLayers - 1; i++)
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    sumLAI += Canopies[j].LAI[i];
                    sumLAItot += Canopies[j].LAItot[i];
                }

            double totalInterception = a_interception * Math.Pow(Rain, b_interception) + c_interception * sumLAItot + d_interception;
            totalInterception = Math.Max(0.0, Math.Min(0.99 * Rain, totalInterception));

            for (int i = 0; i <= numLayers - 1; i++)
                for (int j = 0; j <= Canopies.Count - 1; j++)
                    Canopies[j].interception[i] = MathUtilities.Divide(Canopies[j].LAI[i], sumLAI, 0.0) * totalInterception;

            if (soilWater != null)
            {
                soilWater.PotentialInfiltration = Math.Max(0, Rain - totalInterception);
                soilWater.PrecipitationInterception = totalInterception;
            }
        }

        /// <summary>Calculate the Penman-Monteith water demand</summary>
        /// <param name="dayLengthEvap">This is the length of time within the day during which the sun is above the horizon.</param>
        /// <param name="nightInterceptionFraction">The fraction of intercepted rainfall that evaporates at night.</param>
        public void CalculatePM(double dayLengthEvap, double nightInterceptionFraction)
        {
            // zero a few things, and sum a few others
            double sumRl = 0.0;
            double sumRsoil = 0.0;
            double sumInterception = 0.0;
            double freeEvapGa = 0.0;

            for (int j = 0; j <= Canopies.Count - 1; j++)
            {
                sumRl += MathUtilities.Sum(Canopies[j].Rl);
                sumRsoil += MathUtilities.Sum(Canopies[j].Rsoil);
                sumInterception += MathUtilities.Sum(Canopies[j].interception);
                freeEvapGa += MathUtilities.Sum(Canopies[j].Ga);
            }

            double netRadiation = ((1.0 - Albedo) * sumRs + sumRl + sumRsoil) * 1000000.0;   // MJ/J
            netRadiation = Math.Max(0.0, netRadiation);

            double freeEvapGc = freeEvapGa * 1000000.0; // infinite surface conductance
            double freeEvap = CalcPenmanMonteith(netRadiation, MinT, MaxT, VP, AirPressure, dayLengthEvap, freeEvapGa, freeEvapGc);

            DryLeafFraction = 1.0 - MathUtilities.Divide(sumInterception * (1.0 - nightInterceptionFraction), freeEvap, 0.0);
            DryLeafFraction = Math.Max(0.0, DryLeafFraction);

            for (int i = 0; i <= numLayers - 1; i++)
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    netRadiation = 1000000.0 * ((1.0 - Albedo) * Canopies[j].Rs[i] + Canopies[j].Rl[i] + Canopies[j].Rsoil[i]);
                    netRadiation = Math.Max(0.0, netRadiation);

                    Canopies[j].PETr[i] = CalcPETr(netRadiation * DryLeafFraction, MinT, MaxT, AirPressure, Canopies[j].Ga[i], Canopies[j].Gc[i]);
                    Canopies[j].PETa[i] = CalcPETa(MinT, MaxT, VP, AirPressure, dayLengthEvap * DryLeafFraction, Canopies[j].Ga[i], Canopies[j].Gc[i]);
                    Canopies[j].PET[i] = Canopies[j].PETr[i] + Canopies[j].PETa[i];
                }
        }

        /// <summary>Calculate the aerodynamic decoupling for system compartments</summary>
        public void CalculateOmega()
        {
            for (int i = 0; i <= numLayers - 1; i++)
                for (int j = 0; j <= Canopies.Count - 1; j++)
                    Canopies[j].Omega[i] = CalcOmega(MinT, MaxT, AirPressure, Canopies[j].Ga[i], Canopies[j].Gc[i]);
        }

        /// <summary>Send an energy balance event</summary>
        public void SetCanopyEnergyTerms()
        {
            for (int j = 0; j <= Canopies.Count - 1; j++)
                if (Canopies[j].Canopy != null)
                {
                    CanopyEnergyBalanceInterceptionlayerType[] lightProfile = new CanopyEnergyBalanceInterceptionlayerType[numLayers];
                    double totalPotentialEp = 0;
                    double totalInterception = 0.0;
                    for (int i = 0; i <= numLayers - 1; i++)
                    {
                        lightProfile[i] = new CanopyEnergyBalanceInterceptionlayerType();
                        lightProfile[i].thickness = DeltaZ[i];
                        lightProfile[i].amount = Canopies[j].Rs[i] * RadnGreenFraction(j);
                        totalPotentialEp += Canopies[j].PET[i];
                        totalInterception += Canopies[j].interception[i];
                    }
                    Canopies[j].Canopy.PotentialEP = totalPotentialEp;
                    Canopies[j].Canopy.WaterDemand = totalPotentialEp;
                    Canopies[j].Canopy.LightProfile = lightProfile;
                }
        }

        /// <summary>Calculate the amtospheric potential evaporation rate for each zone</summary>
        public void CalculateEo()
        {

            double CoverGreen = 0;
            for (int j = 0; j <= Canopies.Count - 1; j++)
                if (Canopies[j].Canopy != null)
                    CoverGreen += (1 - CoverGreen) * Canopies[j].Canopy.CoverGreen;

            if (soilWater != null && surfaceOM != null)
                soilWater.Eo = AtmosphericPotentialEvaporationRate(Radn,
                                                             MaxT,
                                                             MinT,
                                                             soilWater.Salb,
                                                             surfaceOM.Cover,
                                                             CoverGreen);
        }

        /// <summary>Break the combined Canopy into layers</summary>
        private void DefineLayers()
        {
            double[] nodes = new double[2 * Canopies.Count];
            int numNodes = 1;
            for (int compNo = 0; compNo <= Canopies.Count - 1; compNo++)
            {
                double HeightMetres = Math.Round(Canopies[compNo].Canopy.Height, 5) / 1000.0; // Round off a bit and convert mm to m } }
                double DepthMetres = Math.Round(Canopies[compNo].Canopy.Depth, 5) / 1000.0; // Round off a bit and convert mm to m } }
                double canopyBase = HeightMetres - DepthMetres;
                if (IsNewLayer(nodes, HeightMetres, numNodes))
                {
                    nodes[numNodes] = HeightMetres;
                    numNodes = numNodes + 1;
                }
                if (Array.IndexOf(nodes, canopyBase) == -1)
                {
                    nodes[0] = canopyBase;
                    numNodes = numNodes + 1;
                }
            }
            Array.Resize<double>(ref nodes, numNodes);
            Array.Sort(nodes);
            numLayers = numNodes - 1;
            if (DeltaZ.Length != numLayers)
            {
                // Number of layers has changed; adjust array lengths
                Array.Resize<double>(ref DeltaZ, numLayers);
                Array.Resize<double>(ref layerKtot, numLayers);
                Array.Resize<double>(ref LAItotsum, numLayers);

                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    Array.Resize<double>(ref Canopies[j].Ftot, numLayers);
                    Array.Resize<double>(ref Canopies[j].Fgreen, numLayers);
                    Array.Resize<double>(ref Canopies[j].Rs, numLayers);
                    Array.Resize<double>(ref Canopies[j].Rl, numLayers);
                    Array.Resize<double>(ref Canopies[j].Rsoil, numLayers);
                    Array.Resize<double>(ref Canopies[j].Gc, numLayers);
                    Array.Resize<double>(ref Canopies[j].Ga, numLayers);
                    Array.Resize<double>(ref Canopies[j].PET, numLayers);
                    Array.Resize<double>(ref Canopies[j].PETr, numLayers);
                    Array.Resize<double>(ref Canopies[j].PETa, numLayers);
                    Array.Resize<double>(ref Canopies[j].Omega, numLayers);
                    Array.Resize<double>(ref Canopies[j].interception, numLayers);
                }
            }
            for (int i = 0; i <= numNodes - 2; i++)
                DeltaZ[i] = nodes[i + 1] - nodes[i];
        }

        /// <summary>
        /// Create a new layer for the specified height?
        /// </summary>
        /// <param name="nodes">The existing layer nodes.</param>
        /// <param name="height">The height (m).</param>
        /// <param name="numNodes">Number of nodes in nodes array.</param>
        private bool IsNewLayer(double[] nodes, double height, int numNodes)
        {
            // Find height in nodes array within tolerance (minimumHeightDiffForNewLayer)
            bool found = false;
            for (int i = 1; i < numNodes; i++)
                if (Math.Abs(nodes[i] - height) < MinimumHeightDiffForNewLayer)
                    found = true;

            // If it wasn't found then return true to signal create
            return !found;
        }

        /// <summary>Break the components into layers</summary>
        private void DivideComponents()
        {
            double top = 0.0;
            double bottom = 0.0;
            for (int i = 0; i <= numLayers - 1; i++)
            {
                bottom = top;
                top = top + DeltaZ[i];
                LAItotsum[i] = 0.0;

                // Calculate LAI for layer i and component j
                // ===========================================
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    Array.Resize(ref Canopies[j].LAI, numLayers);
                    Array.Resize(ref Canopies[j].LAItot, numLayers);
                    double HeightMetres = Math.Round(Canopies[j].Canopy.Height, 5) / 1000.0; // Round off a bit and convert mm to m } }
                    double DepthMetres = Math.Round(Canopies[j].Canopy.Depth, 5) / 1000.0; // Round off a bit and convert mm to m } }
                    if ((HeightMetres > bottom) && (HeightMetres - DepthMetres < top))
                    {
                        double Ld = MathUtilities.Divide(Canopies[j].Canopy.LAITotal, DepthMetres, 0.0);
                        Canopies[j].LAItot[i] = Ld * DeltaZ[i];
                        Canopies[j].LAI[i] = Canopies[j].LAItot[i] * MathUtilities.Divide(Canopies[j].Canopy.LAI, Canopies[j].Canopy.LAITotal, 0.0);
                        LAItotsum[i] += Canopies[j].LAItot[i];
                    }
                }
                // Calculate fractional contribution for layer i and component j
                // ====================================================================
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    Canopies[j].Ftot[i] = MathUtilities.Divide(Canopies[j].LAItot[i], LAItotsum[i], 0.0);
                    Canopies[j].Fgreen[i] = MathUtilities.Divide(Canopies[j].LAI[i], LAItotsum[i], 0.0);  // Note: Sum of Fgreen will be < 1 as it is green over total
                }
            }
        }

        /// <summary>Calculate light extinction parameters</summary>
        private void CalculateLightExtinctionVariables()
        {
            // Calculate effective K from LAI and cover
            // =========================================
            for (int j = 0; j <= Canopies.Count - 1; j++)
            {
                if (MathUtilities.FloatsAreEqual(Canopies[j].Canopy.CoverGreen, 1.0, 1E-10))
                    throw new Exception("Unrealistically high cover value in MicroMet i.e. > 0.999999999");

                Canopies[j].K = MathUtilities.Divide(-Math.Log(1.0 - Canopies[j].Canopy.CoverGreen), Canopies[j].Canopy.LAI, 0.0);
                Canopies[j].Ktot = MathUtilities.Divide(-Math.Log(1.0 - Canopies[j].Canopy.CoverTotal), Canopies[j].Canopy.LAITotal, 0.0);
            }

            // Calculate extinction for individual layers
            // ============================================
            for (int i = 0; i <= numLayers - 1; i++)
            {
                layerKtot[i] = 0.0;
                for (int j = 0; j <= Canopies.Count - 1; j++)
                    layerKtot[i] += Canopies[j].Ftot[i] * Canopies[j].Ktot;
            }
        }

        /// <summary>Calculate the proportion of light intercepted by a given component that corresponds to green leaf.</summary>
        private double RadnGreenFraction(int j)
        {
            double klGreen = -Math.Log(1.0 - Canopies[j].Canopy.CoverGreen);
            double klTot = -Math.Log(1.0 - Canopies[j].Canopy.CoverTotal);
            return MathUtilities.Divide(klGreen, klTot, 0.0);
        }

        private double CalcSunshineHours(double rand, double dayLengthLight, double latitude, double day)
        {
            double maxSunHrs = dayLengthLight;
            double relativeDistance = 1.0 + 0.033 * Math.Cos(0.0172 * day);
            double solarDeclination = 0.409 * Math.Sin(0.0172 * day - 1.39);
            double sunsetAngle = Math.Acos(-Math.Tan(latitude * deg2Rad) * Math.Tan(solarDeclination));
            double extraTerrestrialRadn = 37.6 * relativeDistance * (sunsetAngle * Math.Sin(latitude * deg2Rad) * Math.Sin(solarDeclination) + Math.Cos(latitude * deg2Rad) * Math.Cos(solarDeclination) * Math.Sin(sunsetAngle));
            double maxRadn = 0.75 * extraTerrestrialRadn;
            // finally calculate the sunshine hours as the ratio of maximum possible radiation
            return Math.Min(maxSunHrs * Radn / maxRadn, maxSunHrs);
        }

        private double CalcAverageT(double mint, double maxt)
        {
            return 0.75 * maxt + 0.25 * mint;
        }

        /// <summary>
        /// Calculate the net longwave radiation 'in' (W/m2)
        /// <param name="temperature">temperature  (oC)</param>
        /// <param name="fracClearSkyRad">R/Ro, SunshineHrs/DayLength (0-1)</param>
        /// <param name="emmisCanopy">canopy emmissivity</param>
        /// <returns>net longwave radiation 'in' (W/m2)</returns>
        /// </summary>
        private double LongWave(double temperature, double fracClearSkyRad, double emmisCanopy)
        {
            //  Notes: Emissivity of the sky comes from Swinbank, W.C. (1963) Longwave radiation from clear skies Quart. J. Roy. Meteorol. Soc. 89, 339-348.
            fracClearSkyRad = Math.Max(0.0, Math.Min(1.0, fracClearSkyRad));
            double emmisSky = 9.37E-06 * Math.Pow(temperature + abs_temp, 2.0);   // emmisivity of the sky
            double cloudEffect = (c_cloud + (1.0 - c_cloud) * fracClearSkyRad);   // cloud effect on net long wave (0-1)
            return cloudEffect * (emmisSky - emmisCanopy) * stef_boltz * Math.Pow(temperature + abs_temp, 4.0);
        }

        /// <summary>
        /// Calculate the daytime soil heat flux
        /// <param name="radn">(INPUT) Incoming Radiation</param>
        /// <param name="radnint">(INPUT) Intercepted incoming radiation</param>
        /// <param name="soilHeatFluxFraction">(INPUT) Fraction of surface radiation absorbed</param>
        /// </summary>
        private double CalculateSoilHeatFlux(double radn, double radnint, double soilHeatFluxFraction)
        {
            return Math.Max(-radn * 0.1, Math.Min(0.0, -soilHeatFluxFraction * (radn - radnint)));
        }

        /// <summary>
        /// Calculate the crop canopy conductance
        /// <param name="cropGsMax">crop-specific maximum stomatal conductance (m/s)</param>
        /// <param name="cropR50">crop-specific SolRad at which stomatal conductance decreases to 50% (W/m2)</param>
        /// <param name="cropLAIfac">crop-specific LAI fraction of total LAI in current layer (0-1)</param>
        /// <param name="layerK">layer-averaged light extinction coeficient (-)</param>
        /// <param name="layerLAI">LAI within the current layer (m2/m2)</param>
        /// <param name="layerSolRad">solar radiation arriving at the top of the current layer(W/m2)</param>
        /// </summary>
        private double CropCanopyConductance(double cropGsMax, double cropR50, double cropLAIfac, double layerK, double layerLAI, double layerSolRad)
        {
            double numerator = layerSolRad + cropR50;
            double denominator = layerSolRad * Math.Exp(-1.0 * layerK * layerLAI) + cropR50;
            double hyperbolic = Math.Max(1.0, MathUtilities.Divide(numerator, denominator, 0.0));
            return Math.Max(0.0001, MathUtilities.Divide(cropGsMax * cropLAIfac, layerK, 0.0) * Math.Log(hyperbolic));
        }

        /// <summary>
        /// Calculate the aerodynamic conductance using FAO approach
        /// </summary>
        private double AerodynamicConductanceFAO(double windSpeed, double refHeight, double topHeight, double LAItot)
        {
            // Calculate site properties
            double d = 0.666 * topHeight;        // zero plane displacement height (m)
            double Zh = topHeight + refHeight;   // height of humidity measurement (m) - assume reference above canopy
            double Zm = topHeight + refHeight;   // height of wind measurement (m)
            double Z0m = 0.123 * topHeight;      // roughness length governing transfer of momentum (m)
            double Z0h = 0.1 * Z0m;              // roughness length governing transfer of heat and vapour (m)
            // Calcuate conductance
            double mterm = 0.0; // momentum term in Ga calculation
            double hterm = 0.0; // heat term in Ga calculation
            if ((Z0m != 0) && (Z0h != 0))
            {
                mterm = MathUtilities.Divide(vonKarman, Math.Log(MathUtilities.Divide(Zm - d, Z0m, 0.0)), 0.0);
                hterm = MathUtilities.Divide(vonKarman, Math.Log(MathUtilities.Divide(Zh - d, Z0h, 0.0)), 0.0);
            }
            return Math.Max(0.001, windSpeed * mterm * hterm);
        }

        /// <summary>
        /// Calculate the Penman-Monteith water demand
        /// </summary>
        private double CalcPenmanMonteith(double rn, double mint, double maxt, double vp, double airPressure, double day_length, double Ga, double Gc)
        {
            double averageT = CalcAverageT(mint, maxt);
            double nondQsdT = CalcNondQsdT(averageT, airPressure);
            double RhoA = CalcRhoA(averageT, airPressure);
            double lambda = CalcLambda(averageT);
            double specificVPD = CalcSpecificVPD(vp, mint, maxt, airPressure);
            double denominator = nondQsdT + MathUtilities.Divide(Ga, Gc, 0.0) + 1.0;    // unitless
            double PETr = MathUtilities.Divide(nondQsdT * rn, denominator, 0.0) * 1000.0 / lambda / RhoW;
            double PETa = MathUtilities.Divide(RhoA * specificVPD * Ga, denominator, 0.0) * 1000.0 * (day_length * hr2s) / RhoW;
            return PETr + PETa;
        }

        /// <summary>
        /// Calculate Non_dQs_dT - the dimensionless valu for 
        /// d(sat spec humidity)/dT ((kg/kg)/K) FROM TETEN FORMULA
        /// </summary>
        private double CalcNondQsdT(double temperature, double airPressure)
        {
            double esat = CalcSVP(temperature);                                        // saturated vapour pressure (mb)
            double desdt = esat * svp_B * svp_C / Math.Pow(svp_C + temperature, 2.0);  // d(sat VP)/dT : (mb/K)
            double dqsdt = (mwh2o / mwair) * desdt / airPressure;                      // d(sat spec hum)/dT : (kg/kg)/K
            return CalcLambda(temperature) / cp * dqsdt;
        }

        /// <summary>
        /// Calculate the density of air (kg/m3) at a given temperature
        /// </summary>
        private double CalcRhoA(double temperature, double airPressure)
        {
            return MathUtilities.Divide(mwair * airPressure * 100.0, (abs_temp + temperature) * r_gas, 0.0);            // air pressure converted to Pa
        }

        /// <summary>
        /// Calculate the lambda (latent heat of vapourisation for water) (J/kg)
        /// </summary>
        private double CalcLambda(double temperature)
        {
            return (2501.0 - 2.38 * temperature) * 1000.0;  // J/kg
        }

        /// <summary>
        /// Calculate the vapour pressure deficit
        /// <param name="vp">(INPUT) vapour pressure (hPa = mbar)</param>
        /// <param name="mint">(INPUT) minimum temperature (oC)</param>
        /// <param name="maxt">(INPUT) maximum temperature (oC)</param>
        /// <param name="airPressure">(INPUT) Air pressure (hPa)</param>
        /// </summary>
        private double CalcSpecificVPD(double vp, double mint, double maxt, double airPressure)
        {
            double VPD = CalcVPD(vp, mint, maxt);
            return CalcSpecificHumidity(VPD, airPressure);
        }

        /// <summary>
        /// Calculate the saturated vapour pressure for a given temperature
        /// </summary>
        private double CalcSVP(double temperature)
        {
            return svp_A * Math.Exp(svp_B * temperature / (temperature + svp_C));
        }
        
        /// <summary>
        /// Calculate the vapour pressure deficit
        /// <param name="vp">(INPUT) vapour pressure (hPa = mbar)</param>
        /// <param name="mint">(INPUT) minimum temperature (oC)</param>
        /// <param name="maxt">(INPUT) maximum temperature (oC)</param>
        /// </summary>
        private double CalcVPD(double vp, double mint, double maxt)
        {
            double VPDmint = Math.Max(0.0, CalcSVP(mint) - vp);  // VPD at minimum temperature
            double VPDmaxt = Math.Max(0.0, CalcSVP(maxt) - vp);  // VPD at maximum temperature
            return svp_fract * VPDmaxt + (1 - svp_fract) * VPDmint;
        }
        
        /// <summary>
        /// Calculate the radiation-driven term for the Penman-Monteith water demand
        /// </summary>
        private double CalcPETr(double rn, double mint, double maxt, double airPressure, double Ga, double Gc)
        {
            double averageT = CalcAverageT(mint, maxt);
            double nondQsdT = CalcNondQsdT(averageT, airPressure);
            double lambda = CalcLambda(averageT);
            double denominator = nondQsdT + MathUtilities.Divide(Ga, Gc, 0.0) + 1.0;
            return MathUtilities.Divide(nondQsdT * rn, denominator, 0.0) * 1000.0 / lambda / RhoW;

        }

        /// <summary>
        /// Calculate the aerodynamically-driven term for the Penman-Monteith water demand
        /// </summary>
        private double CalcPETa(double mint, double maxt, double vp, double airPressure, double day_length, double Ga, double Gc)
        {
            double averageT = CalcAverageT(mint, maxt);
            double nondQsdT = CalcNondQsdT(averageT, airPressure);
            double lambda = CalcLambda(averageT);
            double denominator = nondQsdT + MathUtilities.Divide(Ga, Gc, 0.0) + 1.0;
            double RhoA = CalcRhoA(averageT, airPressure);
            double specificVPD = CalcSpecificVPD(vp, mint, maxt, airPressure);
            return MathUtilities.Divide(RhoA * specificVPD * Ga, denominator, 0.0) * 1000.0 * (day_length * hr2s) / RhoW;
        }

        /// <summary>
        /// Calculate specific humidity from vapour pressure
        /// <param name="vp">vapour pressure (hPa = mbar)</param>
        /// <param name="airPressure">air pressure (hPa)</param>
        /// </summary>
        private double CalcSpecificHumidity(double vp, double airPressure)
        {
            return (mwh2o / mwair) * vp / airPressure;
        }

        /// <summary>
        /// Calculate the Jarvis and McNaughton decoupling coefficient, omega
        /// </summary>
        private double CalcOmega(double mint, double maxt, double airPressure, double aerodynamicCond, double canopyCond)
        {
            double Non_dQs_dT = CalcNondQsdT((mint + maxt) / 2.0, airPressure);
            return MathUtilities.Divide(Non_dQs_dT + 1.0, Non_dQs_dT + 1.0 + MathUtilities.Divide(aerodynamicCond, canopyCond, 0.0), 0.0);
        }

        private double AtmosphericPotentialEvaporationRate(double Radn, double MaxT, double MinT, double Salb, double residue_cover, double _cover_green_sum)
        {
            // ******* calculate potential evaporation from soil surface (eos) ******

            // find equilibrium evap rate as a
            // function of radiation, albedo, and temp.
            double surface_albedo = Salb + (residue_albedo - Salb) * residue_cover;
            // set surface_albedo to soil albedo for backward compatibility with soilwat
            surface_albedo = Salb;

            double albedo = max_albedo - (max_albedo - surface_albedo) * (1.0 - _cover_green_sum);
            // wt_ave_temp is mean temp, weighted towards max.
            double wt_ave_temp = 0.6 * MaxT + 0.4 * MinT;

            double eeq = Radn * 23.8846 * (0.000204 - 0.000183 * albedo) * (wt_ave_temp + 29.0);
            // find potential evapotranspiration (pot_eo)
            // from equilibrium evap rate
            return eeq * EeqFac(MaxT, MinT);
        }

        private double EeqFac(double MaxT, double MinT)
        {
            //+  Purpose
            //                 calculate coefficient for equilibrium evaporation rate
            if (MaxT > max_crit_temp)
            {
                // at very high max temps eo/eeq increases
                // beyond its normal value of 1.1
                return (MaxT - max_crit_temp) * 0.05 + 1.1;
            }
            else if (MaxT < min_crit_temp)
            {
                // at very low max temperatures eo/eeq
                // decreases below its normal value of 1.1
                // note that there is a discontinuity at tmax = 5
                // it would be better at tmax = 6.1, or change the
                // .18 to .188 or change the 20 to 21.1
                return 0.01 * Math.Exp(0.18 * (MaxT + 20.0));
            }
            else
            {
                // temperature is in the normal range, eo/eeq = 1.1
                return 1.1;
            }
        }
    }
}