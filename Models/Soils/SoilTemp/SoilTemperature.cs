using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;

namespace Models.Soils.SoilTemp
{
    /// <summary>
    /// The soil temperature model includes functionality for simulating the heat flux and temperatures over
    /// the soil profile, includes temperature on the soil surface. The processes are described below, most
    /// are based on Campbell, 1985. "Soil physics with BASIC: Transport models for soil-plant systems"
    /// </summary>
    /// <remarks>
    /// Since temperature changes rapidly near the soil surface and very little at depth, the best simulation
    /// will be obtained with short elements (shallow layers) near the soil surface and longer ones deeper in
    /// the soil. Ideally, the element lengths should follow a geometric progression...
    /// Ten to twelve nodes are probably sufficient for short-term simulations (daily or weekly). Fifteen nodes
    /// would probably be sufficient for annual cycle simulation where a deeper grid is needed.
    /// p36, Campbell, G.S. (1985) "Soil physics with BASIC: Transport models for soil-plant systems"
    /// --------------------------------------------------------------------------------------------------------
    /// -----------------------------------------------IMPORTANT NOTE-------------------------------------------
    /// Due to FORTRAN's 'flexibility' with arrays that are not present in C#, few modifications have been done
    /// to array sizes in this version of SoilTemp. Here, all arrays are forcibly 0-based, so to deal with the
    /// fact that the original module had both 0- and 1-based arrays, all arrays have been increased in size by
    /// one. With this approach, the indexing does not need to change in those processes that in the FORTRAN code
    /// that processed 1-based arrays, here the first (0th) element would simply not be used...
    /// This is actually rather convenient. In these arrays, the element 0 now refers to the air (airNode),
    /// the element 1 refers to the soil surface (surfaceNode), and from elements 2 (topsoilNode) to numNodes+1
    /// all nodes refer the middle of layers within the soil.
    /// ----------------------------------------------------------------------------------------------------------
    /// </remarks>
    /// <structure>
    /// In the soil temperature model, the soil profile is represented (abstracted) by two schema:
    ///  a) Soil layers, each with a top and bottom boundary and a thickness (mm). Their index ranges from
    ///  one to numLayers;
    ///  b) Temperature nodes, all dimensionless, one at the centre of each layer, plus additional nodes to
    ///  represent a node below the bottom layer and a node above the top layer (in the atmosphere). Index
    ///  ranges from 0 to numLayers + 1;
    /// </structure>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    public class SoilTemperature : Model, ISoilTemperature
    {
        [Link]
        private IWeather weather = null;

        [Link]
        private IClock clock = null;

        [Link(IsOptional = true)]   // Simulations without plants don't have a micro climate instance
        private MicroClimate microClimate = null;

        [Link]
        private Physical physical = null;

        [Link]
        private Organic organic = null;        

        [Link]
        ISoilWater waterBalance = null;

        #region Table of properties for soil constituents   - - - - - - - - - - - - - - - - - - - -

        /// <summary>Particle density of organic matter (Mg/m3), from Campbell (1985)</summary>
        private double pom = 1.3;   // CHECK, should come from soil physical

        /// <summary>Particle density of soil fines (Mg/m3)</summary>
        private double ps = 2.63;   // CHECK, should come from soil physical

        /// <summary>List of names of soil constituents</summary>
        private string[] soilConstituentNames = { "Rocks", "OrganicMatter", "Sand", "Silt", "Clay", "Water", "Ice", "Air" };

        /// <summary>Gets the volumetric specific heat of soil constituents (MJ/m3/K)</summary>
        /// <param name="name">The name of the constituent</param>
        /// <param name="layer">The layer index</param>
        private double volumetricSpecificHeat(string name, int layer)
        {
            double specificHeatRocks = 7.7;
            double specificHeatOM = 0.25;
            double specificHeatSand = 7.7;
            double specificHeatSilt = 2.74;
            double specificHeatClay = 2.92;
            double specificHeatWater = 0.57;
            // CHECK, value seems wrong, wikipedia gives 4.18 MJ/m3/K.
            // Also, water has one of the highest values for specific heat, and here it isn't
            double specificHeatIce = 2.18;
            double specificHeatAir = 0.025;

            double result = 0.0;

            if (name == "Rocks")
            {
                result = specificHeatRocks;
            }
            else if (name == "OrganicMatter")
            {
                result = specificHeatOM;
            }
            else if (name == "Sand")
            {
                result = specificHeatSand;
            }
            else if (name == "Silt")
            {
                result = specificHeatSilt;
            }
            else if (name == "Clay")
            {
                result = specificHeatClay;
            }
            else if (name == "Water")
            {
                result = specificHeatWater;
            }
            else if (name == "Ice")
            {
                result = specificHeatIce;
            }
            else if (name == "Air")
            {
                result = specificHeatAir;
            }
            else
            {
                throw new Exception("Cannot return specific heat for " + name);
            }

            return result;
        }

        /// <summary>Gets the thermal conductance of soil constituents (W/m/K) - CHECK, unit should be W/K</summary>
        /// <param name="name">The name of the constituent</param>
        /// <param name="layer">The layer index</param>
        private double ThermalConductance(string name, int layer)
        {
            double thermalConductanceRocks = 0.182;
            double thermalConductanceOM = 2.50;
            double thermalConductanceSand = 0.182;
            double thermalConductanceSilt = 2.39;
            double thermalConductanceClay = 1.39;
            double thermalConductanceWater = 4.18;  // CHECK, this value seems to be the specific heat
            double thermalConductanceIce = 1.73;
            double thermalConductanceAir = 0.0012;

            double result = 0.0;

            if (name == "Rocks")
            {
                result = thermalConductanceRocks;
            }
            else if (name == "OrganicMatter")
            {
                result = thermalConductanceOM;
            }
            else if (name == "Sand")
            {
                result = thermalConductanceSand;
            }
            else if (name == "Silt")
            {
                result = thermalConductanceSilt;
            }
            else if (name == "Clay")
            {
                result = thermalConductanceClay;
            }
            else if (name == "Water")
            {
                result = thermalConductanceWater;
            }
            else if (name == "Ice")
            {
                result = thermalConductanceIce;
            }
            else if (name == "Air")
            {
                result = thermalConductanceAir;
            }
            else if (name == "Minerals")
            {
                result = Math.Pow(thermalConductanceRocks, volumetricFractionRocks(layer)) *
                         Math.Pow(thermalConductanceSand, volumetricFractionSand(layer)) +
                         Math.Pow(thermalConductanceSilt, volumetricFractionSilt(layer)) +
                         Math.Pow(thermalConductanceClay, volumetricFractionClay(layer));
                // CHECK, this function seem odd (wrong), why power function, why multiply and add???
            }
            else
            {
                throw new Exception("Cannot return thermal conductance for " + name);
            }

            result = volumetricSpecificHeat(name, layer);  // This is wrong, but is was in the code...
            return result;
        }

        /// <summary>Gets the shape factor of soil constituents (W/m/K - CHECK, unit)</summary>
        /// <param name="name">The name of the constituent</param>
        /// <param name="layer">The layer index</param>
        private double shapeFactor(string name, int layer)
        {
            double shapeFactorRocks = 0.182;
            double shapeFactorOM = 0.5;
            double shapeFactorSand = 0.182;
            double shapeFactorSilt = 0.125;
            double shapeFactorClay = 0.007755;
            double shapeFactorWater = 1.0;
            //double shapeFactorIce = 0.0;
            //double shapeFactorAir = double.NaN;

            double result = 0.0;

            if (name == "Rocks")
            {
                result = shapeFactorRocks;
            }
            else if (name == "OrganicMatter")
            {
                result = shapeFactorOM;
            }
            else if (name == "Sand")
            {
                result = shapeFactorSand;
            }
            else if (name == "Silt")
            {
                result = shapeFactorSilt;
            }
            else if (name == "Clay")
            {
                result = shapeFactorClay;
            }
            else if (name == "Water")
            {
                result = shapeFactorWater;
            }
            else if (name == "Ice")
            {
                result = 0.333 - 0.333 * volumetricFractionIce(layer) /
                         (volumetricFractionWater(layer) + volumetricFractionIce(layer) + volumetricFractionAir(layer));
                // CHECK, the value of shapeFactorIce is not used...?
            }
            else if (name == "Air")
            {
                result = 0.333 - 0.333 * volumetricFractionAir(layer) /
                    (volumetricFractionWater(layer) + volumetricFractionIce(layer) + volumetricFractionAir(layer));
                // CHECK, the value of shapeFactorAir is not used...?
            }
            else if (name == "Minerals")
            {
                result = shapeFactorRocks * volumetricFractionRocks(layer) +
                         shapeFactorSand * volumetricFractionSand(layer) +
                         shapeFactorSilt * volumetricFractionSilt(layer) +
                         shapeFactorClay * volumetricFractionClay(layer);
            }
            else
            {
                throw new Exception("Cannot return thermal conductance for " + name);
            }

            result = volumetricSpecificHeat(name, layer);  // This is wrong, but is was in the code...
            return result;
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region constants   - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Internal time-step (minutes)</summary>
        private const int timestep = 1440;

        /// <summary>Latent heat of vapourisation for water (J/kg)</summary>
        /// <remarks>Evaporation of 1.0 mm/day = 1 kg/m^2/day requires 2.45*10^6 J/m^2</remarks>
        const double LAMBDA = 2465000.0;

        /// <summary>The Stefan-Boltzmann constant (W/m2/K4)</summary>
        /// <remarks>Power per unit area emitted by a black body as a function of its temperature</remarks>
        const double STEFAN_BOLTZMANNconst = 0.0000000567;

        /// <summary>Number of days in one year</summary>
        const double DAYSinYear = 365.25;

        /// <summary>Conversion from degrees to radians</summary>
        const double DEG2RAD = Math.PI / 180.0;
        /// <summary>Conversion from radians to degrees</summary>
        const double RAD2DEG = 180.0 / Math.PI;

        /// <summary>Conversion of day of year into radians</summary>
        const double DOY2RAD = DEG2RAD * 360.0 / DAYSinYear;

        /// <summary></summary>
        const double MIN2SEC = 60.0;          // 60 seconds in a minute - conversion minutes to seconds
        /// <summary></summary>
        const double HR2MIN = 60.0;           // 60 minutes in an hour - conversion hours to minutes
        /// <summary></summary>
        const double SEC2HR = 1.0 / (HR2MIN * MIN2SEC);  // conversion seconds to hours
        /// <summary></summary>
        const double DAYhrs = 24.0;           // hours in a day
        /// <summary></summary>
        const double DAYmins = DAYhrs * HR2MIN;           // minutes in a day
        /// <summary></summary>
        const double DAYsecs = DAYmins * MIN2SEC;         // seconds in a day
        /// <summary></summary>
        const double M2MM = 1000.0;           // 1000 mm in a metre -  conversion Metre to millimetre
        /// <summary></summary>
        const double MM2M = 1 / M2MM;           // 1000 mm in a metre -  conversion millimetre to Metre
        /// <summary></summary>
        const double PA2HPA = 1.0 / 100.0;        // conversion of air pressure pascals to hectopascals
        /// <summary></summary>
        const double MJ2J = 1000000.0;            // convert MJ to J
        /// <summary></summary>
        const double J2MJ = 1.0 / MJ2J;            // convert J to MJ

        /// <summary>The index of the node in the atmosphere (aboveground)</summary>
        const int AIRnode = 0;

        /// <summary>The index of the node on the soil surface</summary>
        const int SURFACEnode = 1;

        /// <summary>The index of the first node within the soil (mid of top layer)</summary>
        const int TOPSOILnode = 2;

        /// <summary>The number of phantom nodes below the soil profile</summary>
        /// <remarks>These are needed for the lower BC to work properly, invisible externally</remarks>
        const int NUM_PHANTOM_NODES = 5;

        /// <summary></summary>
        private double volSpecHeatClay = 2.39e6;  // [Joules*m-3*K-1]

        /// <summary></summary>
        private double volSpecHeatOM = 5e6;       // [Joules*m-3*K-1]

        /// <summary></summary>
        private double volSpecHeatWater = 4.18e6; // [Joules*m-3*K-1]

        /// <summary></summary>
        private double maxTTimeDefault = 14.0;

        /// <summary></summary>
        private const double boundaryLayerConductance = 20;

        /// <summary></summary>
        private const double BoundaryLayerConductanceIterations = 1;    // maximum number of iterations to calculate atmosphere boundary layer conductance

        /// <summary>Default wind speed (m/s)</summary>
        private double defaultWindSpeed = 3.0;

        /// <summary>Default altitude (m)</summary>
        private const double defaultAltitude = 18.0;

        /// <summary>Default instrument height (m)</summary>
        private const double defaultInstrumentHeight = 1.2;

        /// <summary>Roughness element height of bare soil (mm)</summary>
        private const double bareSoilHeight = 57;

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region Internal variables for this model   - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Flag whether initialisation is needed - CHECK</summary>
        private bool doInit1Stuff = false;

        /// <summary>Internal time-step (s)</summary>
        private double gDt = 0.0;

        /// <summary>Time of day from midnight (s)</summary>
        private double timeOfDaySecs = 0.0;

        /// <summary>Number of nodes over the soil profile</summary>
        private int numNodes = 0;

        /// <summary>Number of layers in the soil profile</summary>
        private int numLayers = 0;

        /// <summary>Parameter 1 for computing thermal conductivity of soil solids</summary>
        private double[] thermCondPar1;

        /// <summary>Parameter 2 for computing thermal conductivity of soil solids</summary>
        private double[] thermCondPar2;

        /// <summary>Parameter 3 for computing thermal conductivity of soil solids</summary>
        private double[] thermCondPar3;

        /// <summary>Parameter 4 for computing thermal conductivity of soil solids</summary>
        private double[] thermCondPar4;

        /// <summary>Volumetric specific heat of...CHECK (J/m3/K)</summary>
        private double[] volSpecHeatSoil;     // 

        /// <summary>Temperature of each soil layer (oC)</summary>
        private double[] soilTemp;

        /// <summary>Some soil temperature - CHECK</summary>
        private double[] morningSoilTemp;

        /// <summary>CP, heat storage between nodes (W/K) - index is same as upper node</summary>
        private double[] heatStorage;

        /// <summary>K, conductance of element between nodes (CHECK) - index is same as upper node</summary>
        private double[] thermalConductance;

        /// <summary>thermal conductivity (W/m2/K)</summary>
        private double[] thermalConductivity;

        /// <summary>Number of iterations to calculate atmosphere boundary layer conductance</summary>
        private int boundaryLayerConductanceIterations = 0;

        /// <summary>Average daily atmosphere boundary layer conductance</summary>
        private double _boundaryLayerConductance = 0.0;

        /// <summary>Soil temperature at the end of this iteration (oC)</summary>
        private double[] tempNew;

        /// <summary>Depths of nodes (m)</summary>
        private double[] depth;

        /// <summary>Air temperature (oC)</summary>
        private double airTemp = 0.0;

        /// <summary>Maximum daily air temperature (oC)</summary>
        private double maxAirTemp = 0.0;

        /// <summary>Minimum daily air temperature (oC)</summary>
        private double minAirTemp = 0.0;

        /// <summary>Yesterday's maximum daily air temperature (oC)</summary>
        private double maxTempYesterday = 0.0;

        /// <summary>Yesterday's minimum daily air temperature (oC)</summary>
        private double minTempYesterday = 0.0;

        /// <summary>Volumetric water content of each soil layer (mm3/mm3)</summary>
        private double[] soilWater;

        /// <summary>Minimum soil temperature (oC)</summary>
        private double[] minSoilTemp;

        /// <summary>Maximum soil temperature (oC)</summary>
        private double[] maxSoilTemp;

        /// <summary>CHECK</summary>
        private double[] aveSoilTemp;  // FIXME - optional. Allow setting from set in manager or get from input //init to average soil temperature

        /// <summary></summary>
        private double tempStepSec = 0.0;     // this will be set to timestep * 60 at the beginning of each calc. (seconds)

        /// <summary>Soil bulk density (g/cm3)</summary>
        private double[] bulkDensity;

        /// <summary>Thickness of each soil (mm)</summary>
        private double[] thickness;

        /// <summary>Height of instruments (CHECK) above soil surface (m)</summary>
        private double instrumentHeight = 0.0;

        /// <summary>Daily atmosphere pressure (hPa)</summary>
        private double airPressure = 0.0;

        /// <summary>Daily wind run (km)</summary>
        private double windSpeed = 0.0;

        /// <summary>Potential soil evaporation, after modification for green cover residue (mm)</summary>
        private double potSoilEvap = 0.0;

        /// <summary>Potential daily (CHECK) evapotranspiration (mm)</summary>
        private double potEvapotrans = 0.0;

        /// <summary>Actual soil evaporation (mm)</summary>
        private double actualSoilEvap = 0.0;

        /// <summary>Net radiation per internal time-step (MJ)</summary>
        private double netRadiation = 0.0;

        /// <summary>Height of canopy above ground (m)</summary>
        private double canopyHeight = 0.0;

        /// <summary>Height of soil roughness (mm)</summary>
        private double soilRoughnessHeight = 0.0;

        /// <summary>Volumetric fraction of rocks in each soil layer (% - CHECK unit)</summary>
        private double[] rocks;

        /// <summary>Volumetric fraction of carbon (CHECK) in each soil layer (% - CHECK unit)</summary>
        private double[] carbon;

        /// <summary>Volumetric fraction of sand in each soil layer (% - CHECK unit)</summary>
        private double[] sand;

        /// <summary>Volumetric fraction of silt in each soil layer (% - CHECK unit)</summary>
        private double[] silt;

        /// <summary>Volumetric fraction of clay in each soil layer (% - CHECK unit)</summary>
        private double[] clay;

        // this was not an input in old apsim. // [Input]                                //FIXME - optional input
        /// <summary>Time of maximum temperature in hours</summary>
        private double maxTempTime = 0.0;

        // Private cover_tot As Double = 0.0    ' (0-1) total surface cover

        /// <summary>Height of instruments above ground</summary>
        private double instrumHeight = 0.0;

        /// <summary>Altitude at site (m)</summary>
        private double altitude = 0.0;

        /// <summary>Forward/backward differencing coefficient (0-1)</summary>
        /// <remarks>
        /// This is a weighting factor used to determined how the heat flux is to be computed.
        /// The numerical procedure that results from setting this to zero is called a forward difference
        /// of the explicit method. If set to 0.5, the average of the old and new temperatures is used to
        /// compute heat flux. This is called a time-centred or Crank-Nicholson scheme.
        /// The equation for computing T(j+1) is an 'implicit' scheme for any choice of this parameter
        /// (except zero, which is called the 'explicit' scheme). This is because T(j+1) depends on the
        /// values of the new temperatures at the nodes i+1 and i-1.
        /// Most heat flow models use either 0.0 or 0.5. However, the best value for this parameters is
        /// determined by consideration of numerical stability and accuracy.
        /// The explicit scheme predicts more heat transfer between nodes than would actually occur, and
        /// can therefore become unstable if time steps are too large. Stable numerical solutions are only
        /// obtained when deltaT is less than CH*(deltaZ)^2 / 2*lambda (Simonson, 1975).
        /// When using 0.5, stable solutions to the heat flow problem will always be obtained, while if 
        /// set to a too small value, the solutions may oscillate. The reason for this is that simulated
        /// heat transfer between nodes 'overshoots' (thus in the next time step the excess heat must be
        /// transferred back). If the parameter is closer to one, the temperature difference will be too
        /// small and not enough heat will be transferred. Simulated temperatures will never oscillate in
        /// these conditions, but the simulation will underestimate heat flux. Best accuracy is obtained
        /// with a value around 0.4, while best stability is at one. A typical compromise is nu=0.6.
        /// </remarks>
        private double nu = 0.6;

        /// <summary></summary>
        private string boundarLayerConductanceSource = "calc";

        /// <summary></summary>
        private string netRadiationSource = "calc";

        /// <summary>Depth down to the constant temperature (m)</summary>
        [JsonIgnore]
        public double CONSTANT_TEMPdepth { get; set; } = 10.0;

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region Input for this model  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Depth strings. Wrapper around Thickness</summary>
        [Display]
        [Summary]
        [Units("mm")]
        [JsonIgnore]
        public string[] Depth
        {
            get => SoilUtilities.ToDepthStrings(Thickness);
            set => Thickness = SoilUtilities.ToThickness(value);
        }

        /// <summary>Thickness of soil layers (mm)</summary>
        public double[] Thickness { get; set; }

        /// <summary>Initial values</summary>
        [Summary]
        [Display(Format = "N2")]
        [Units("oC")]
        public double[] InitialValues { get; set; }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region Outputs from this model - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Temperature at end of last time-step within a day, i.e. at midnight</summary>
        [Units("oC")]
        public double[] FinalSoilTemp
        {
            get
            {
                double[] result = new double[numLayers];
                Array.ConstrainedCopy(soilTemp, TOPSOILnode, result, 0, numLayers);
                return result;
            }
        }

        /// <summary>Temperature at end of last time-step within a day, i.e. at midnight</summary>
        [Units("oC")]
        public double FinalSoilSurfaceTemp { get { return soilTemp[SURFACEnode]; } }

        /// <summary>Mandatory for ISoilTemperature interface. For now, just return average daily values - CHECK</summary>
        public double[] Value { get { return AverageSoilTemp; } }

        /// <summary>Temperature over soil profile averaged over all time-steps within a day</summary>
        /// <remarks>If called during init1, this will return an array of length 100 with all elements as 0.0</remarks>
        [Units("oC")]
        public double[] AverageSoilTemp
        {
            get
            {
                double[] result = new double[numLayers];
                Array.ConstrainedCopy(aveSoilTemp, TOPSOILnode, result, 0, numLayers);
                return result;
            }
        }

        /// <summary>Temperature at the soil surface averaged over all time-steps within a day</summary>
        [Units("oC")]  // CHECk, description is not right
        public double AverageSoilSurfaceTemp { get { return aveSoilTemp[SURFACEnode]; } }

        /// <summary></summary>
        [Units("oC")]
        public double[] MinSoilTemp
        {
            get
            {
                double[] result = new double[numLayers];
                Array.ConstrainedCopy(minSoilTemp, TOPSOILnode, result, 0, numLayers);
                return result;
            }
        }

        /// <summary></summary>
        [Units("oC")]
        public double minSoilSurfaceTemp { get { return minSoilTemp[SURFACEnode]; } }

        /// <summary></summary>
        [Units("oC")]
        public double[] MaxSoilTemp
        {
            get
            {
                double[] result = new double[numLayers];
                Array.ConstrainedCopy(maxSoilTemp, TOPSOILnode, result, 0, numLayers);
                return result;
            }
        }

        /// <summary></summary>
        [Units("oC")]
        public double MaxSoilSurfaceTemp { get { return maxSoilTemp[SURFACEnode]; } }

        /// <summary></summary>
        [Units("J/sec/m/K")]
        public double BoundaryLayerConductance { get { return _boundaryLayerConductance; } }

        /// <summary></summary>
        [Units("J/sec/m/K")]
        public double[] ThermalConductivity
        {
            get
            {
                double[] result = new double[numNodes];
                Array.ConstrainedCopy(thermalConductivity, 1, result, 0, numNodes);
                return result;
            }
        }

        /// <summary></summary>
        [Units("J/m3/K/s")]
        public double[] HeatCapacity
        {
            get
            {
                double[] result = new double[numNodes];
                Array.ConstrainedCopy(volSpecHeatSoil, SURFACEnode, result, 0, numNodes);  // FIXME - should index be 2?
                return result;
            }
        }

        /// <summary></summary>
        [Units("J/m3/K/s")]
        public double[] HeatStore
        {
            get
            {
                double[] result = new double[numNodes];
                Array.ConstrainedCopy(heatStorage, SURFACEnode, result, 0, numNodes);  // FIXME - should index be 2?
                return result;
            }
        }

        /// <summary></summary>
        [Units("oC")]
        public double[] Thr_profile  // CHECK, needed anywhere??
        {
            get
            {
                double[] result = new double[numNodes + 1 + 1];
                morningSoilTemp.CopyTo(result, 0);
                return result;
            }
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region Events invoked or subscribed by this model  - - - - - - - - - - - - - - - - - - - -

        /// <summary>Event invoke when the soil temperature has changed</summary>
        public event EventHandler SoilTemperatureChanged;

        /// <summary>Performs the tasks to initialise the model</summary>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            doInit1Stuff = true;
            getIniVariables();
            getProfileVariables();
            readParam();
        }

        [EventSubscribe("EndOfSimulation")]
        private void OnEndOfSimulation(object sender, EventArgs e)
        {
            InitialValues = null;
        }
        
        /// <summary>Called when model has been created</summary>
        public override void OnCreated()
        {
            base.OnCreated();
        }

        /// <summary>Performs the tasks to simulate soil temperature</summary>
        [EventSubscribe("DoSoilTemperature")]
        private void OnProcess(object sender, EventArgs e)
        {
            maxTempTime = maxTTimeDefault;
            BoundCheck(maxTempTime, 0.0, DAYhrs, "maxTempTime");

            GetOtherVariables();       // FIXME - note: Need to set yesterday's MaxTg and MinTg to today's at initialisation

            if (doInit1Stuff)
            {
                if (MathUtilities.ValuesInArray(InitialValues))
                {
                    soilTemp = new double[numNodes + 1 + 1];
                    Array.ConstrainedCopy(InitialValues, 0, soilTemp, TOPSOILnode, InitialValues.Length);
                }
                else
                {
                    // set t and tnew values to TAve. soil_temp is currently not used
                    CalcSoilTemp(ref soilTemp);
                    InitialValues = new double[numLayers];
                    Array.ConstrainedCopy(soilTemp, TOPSOILnode, InitialValues, 0, numLayers);
                }
                
                soilTemp[AIRnode] = (maxAirTemp + minAirTemp) * 0.5;
                soilTemp[SURFACEnode] = SurfaceTemperatureInit();

                // initialise the phantom nodes.
                for (int i = numNodes + 1; i < soilTemp.Length; i++)
                    soilTemp[i] = weather.Tav;

                // gT_zb(gNz + 1) = gT_zb(gNz)
                soilTemp.CopyTo(tempNew, 0);

                // 'gTAve = tav
                // 'For node As Integer = AIRNODE To gNz + 1      ' FIXME - need here until variable passing on init2 enabled
                // '    gT_zb(node) = gTAve                  ' FIXME - need here until variable passing on init2 enabled
                // '    gTNew_zb(node) = gTAve                 ' FIXME - need here until variable passing on init2 enabled
                // 'Next node
                maxTempYesterday = weather.MaxT;
                minTempYesterday = weather.MinT;
                doInit1Stuff = false;
            }

            doProcess();

            SoilTemperatureChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -


        /// <summary>Volumetric fraction of rocks in the soil</summary>
        private double volumetricFractionRocks(int layer) => rocks[layer] / 100.0;

        /// <summary>Volumetric fraction of organic matter in the soil</summary>
        private double volumetricFractionOrganicMatter(int layer) => carbon[layer] / 100.0 * 2.5 * bulkDensity[layer] / pom;

        /// <summary>Volumetric fraction of sand in the soil</summary>
        private double volumetricFractionSand(int layer) => (1 - volumetricFractionOrganicMatter(layer) - volumetricFractionRocks(layer)) *
                                                       sand[layer] / 100.0 * bulkDensity[layer] / ps;

        /// <summary>Volumetric fraction of silt in the soil</summary>
        private double volumetricFractionSilt(int layer) => (1 - volumetricFractionOrganicMatter(layer) - volumetricFractionRocks(layer)) *
                                                       silt[layer] / 100.0 * bulkDensity[layer] / ps;

        /// <summary>Volumetric fraction of clay in the soil</summary>
        private double volumetricFractionClay(int layer) => (1 - volumetricFractionOrganicMatter(layer) - volumetricFractionRocks(layer)) *
                                                       clay[layer] / 100.0 * bulkDensity[layer] / ps;

        /// <summary>Volumetric fraction of water in the soil</summary>
        private double volumetricFractionWater(int layer) => (1 - volumetricFractionOrganicMatter(layer)) * soilWater[layer];

        /// <summary>Volumetric fraction of ice in the soil</summary>
        /// <remarks>
        /// Not implemented yet, might be simulated in the future. Something like:
        ///  (1 - VolumetricFractionOrganicMatter(i)) * waterBalance.Ice[i];
        /// </remarks>
        private double volumetricFractionIce(int layer) => 0.0;

        /// <summary>Volumetric fraction of air in the soil</summary>
        private double volumetricFractionAir(int layer)
        {
            return 1.0 - volumetricFractionRocks(layer) -
                         volumetricFractionOrganicMatter(layer) -
                         volumetricFractionSand(layer) -
                         volumetricFractionSilt(layer) -
                         volumetricFractionClay(layer) -
                         volumetricFractionWater(layer) -
                         volumetricFractionIce(layer);
        }

        /// <summary>Maps the initial temperature value over the soil profile</summary>
        /// <param name="targetThickness">Target thickness</param>
        public void Standardise(double[] targetThickness)
        {
            InitialValues = SoilUtilities.MapInterpolation(InitialValues, Thickness, targetThickness, allowMissingValues:true);
        }

        /// <summary>Performs the tasks to reset the model</summary>
        public void Reset(double[] values = null)
        {
            if (values == null)
            {
                Array.ConstrainedCopy(InitialValues, 0, soilTemp, TOPSOILnode, InitialValues.Length);
            }
            else
            {
                int expectedNumValues = soilTemp.Length - NUM_PHANTOM_NODES - 2;
                if (values.Length != expectedNumValues)
                {
                    throw new Exception($"Not enough values specified when resetting soil temperature. There needs to be {expectedNumValues} values.");
                }

                Array.ConstrainedCopy(values, 0, soilTemp, TOPSOILnode, values.Length);
            }
        
            soilTemp[AIRnode] = (maxAirTemp + minAirTemp) * 0.5;
            soilTemp[SURFACEnode] = SurfaceTemperatureInit();
            int firstPhantomNode = TOPSOILnode + InitialValues.Length;
            for (int i = firstPhantomNode; i < firstPhantomNode + NUM_PHANTOM_NODES; i++)
            {
                soilTemp[i] = weather.Tav;
            }

            soilTemp.CopyTo(tempNew, 0);
        }

        /// <summary>Initialise global variables to initial values</summary>
        /// <remarks></remarks>
        private void getIniVariables()
        {
            BoundCheck(weather.Tav, -30.0, 50.0, "tav (oC)");

            if ((instrumHeight > 0.00001))
                instrumentHeight = instrumHeight;
            else
                instrumentHeight = defaultInstrumentHeight;
            BoundCheck(instrumentHeight, 0.0, 5.0, "instrumentHeight (m)");

            BoundCheck(altitude, -100.0, 6000.0, "altitude (m)");
            double AltitudeMetres = 0.0;
            // If (altitude >= 0.0) Then
            // AltitudeMetres = altitude
            // Else
            AltitudeMetres = defaultAltitude;       // FIXME - need to detect if altitude not supplied elsewhere.
                                                    // End If

            airPressure = 101325.0 * Math.Pow((1.0 - 2.25577 * 0.00001 * AltitudeMetres), 5.25588) * PA2HPA;
            BoundCheck(airPressure, 800.0, 1200.0, "air pressure (hPa)");
        }

        /// <summary>Set global variables to new soil profile state</summary>
        /// <remarks>
        /// mapping of layers to nodes:
        ///  3layer - air surface 1 2 ... NumLayers NumLayers+1
        ///  node  - 0   1       2 3 ... Nz        Nz+1
        ///  thus the node 1 is at the soil surface and Nz = NumLayers + 1
        /// </remarks>
        private void getProfileVariables()  // CHECK, probably not needed
        {
            numLayers = physical.Thickness.Length;
            numNodes = numLayers + NUM_PHANTOM_NODES;

            // set internal thickness array, add layers for zone below bottom layer plus one for surface
            thickness = new double[numLayers + NUM_PHANTOM_NODES + 1];
            physical.Thickness.CopyTo(thickness, 1);

            // add enough to make last node at 9-10 meters - should always be enough to assume constant temperature
            double BelowProfileDepth = Math.Max(CONSTANT_TEMPdepth * M2MM - SumOfRange(thickness, 1, numLayers), 1.0 * M2MM);    // Metres. Depth added below profile to take last node to constant temperature zone

            double thicknessForPhantomNodes = BelowProfileDepth * 2.0 / NUM_PHANTOM_NODES; // double depth so that bottom node at mid-point is at the ConstantTempDepth
            int firstPhantomNode = numLayers;
            for (int i = firstPhantomNode; i < firstPhantomNode + NUM_PHANTOM_NODES; i++)
                thickness[i] = thicknessForPhantomNodes;   
            var oldDepth = depth;
            depth = new double[numNodes + 1 + 1];

            // Set the node depths to approx middle of soil layers
            if (oldDepth != null)
                Array.Copy(oldDepth, depth, Math.Min(numNodes + 1 + 1, oldDepth.Length));      // Z_zb dimensioned for nodes 0 to Nz + 1 extra for zone below bottom layer
            depth[AIRnode] = 0.0;
            depth[SURFACEnode] = 0.0;
            depth[TOPSOILnode] = 0.5 * thickness[1] * MM2M;
            for (int node = TOPSOILnode; node <= numNodes; node++)
                depth[node + 1] = (SumOfRange(thickness, 1, node - 1) + 0.5 * thickness[node]) * MM2M;

            // BD
            BoundCheck(physical.BD.Length, numLayers, numLayers, "bd layers");
            var oldBulkDensity = bulkDensity;
            bulkDensity = new double[numLayers + 1 + NUM_PHANTOM_NODES];
            if (oldBulkDensity != null)
                Array.Copy(oldBulkDensity, bulkDensity, Math.Min(numLayers + 1 + NUM_PHANTOM_NODES, oldBulkDensity.Length));     // Rhob dimensioned for layers 1 to gNumlayers + extra for zone below bottom layer
            physical.BD.CopyTo(bulkDensity, 1);
            BoundCheckArray(bulkDensity, 0.0, 2.65, "bulkDensity");
            bulkDensity[numNodes] = bulkDensity[numLayers];
            for (int layer = numLayers+1; layer <= numLayers + NUM_PHANTOM_NODES; layer++)
                bulkDensity[layer] = bulkDensity[numLayers]; 

            // SW
            BoundCheck(waterBalance.SWmm.Length, numLayers, numLayers, "sw layers");
            var oldSoilWater = soilWater;
            soilWater = new double[numLayers + 1 + NUM_PHANTOM_NODES];
            if (oldSoilWater != null)
                Array.Copy(oldSoilWater, soilWater, Math.Min(numLayers + 1 + NUM_PHANTOM_NODES, oldSoilWater.Length));     // SW dimensioned for layers 1 to gNumlayers + extra for zone below bottom layer
            if (waterBalance.SW != null)
            {
                for (int layer = 1; layer <= numLayers; layer++)
                    soilWater[layer] = MathUtilities.Divide(waterBalance.SWmm[layer - 1], thickness[layer], 0);
                for (int layer = numLayers + 1; layer <= numLayers + NUM_PHANTOM_NODES; layer++)
                    soilWater[layer] = soilWater[numLayers];
            }

            // Carbon
            BoundCheck(organic.Carbon.Length, numLayers, numLayers, "carbon layers");
            carbon = new double[numLayers + 1 + NUM_PHANTOM_NODES];
            for (int layer = 1; layer <= numLayers; layer++)
                carbon[layer] = organic.Carbon[layer - 1];
            for (int layer = numLayers+1; layer <= numLayers + NUM_PHANTOM_NODES; layer++)
                carbon[layer] = carbon[numLayers]; 

            // Rocks
            BoundCheck(physical.Rocks.Length, numLayers, numLayers, "rocks layers");
            rocks = new double[numLayers + 1 + NUM_PHANTOM_NODES];
            for (int layer = 1; layer <= numLayers; layer++)
                rocks[layer] = physical.Rocks[layer - 1];
            for (int layer = numLayers+1; layer <= numLayers + NUM_PHANTOM_NODES; layer++)
                rocks[layer] = rocks[numLayers]; 

            // Sand
            BoundCheck(physical.ParticleSizeSand.Length, numLayers, numLayers, "sand layers");
            sand = new double[numLayers + 1 + NUM_PHANTOM_NODES];
            for (int layer = 1; layer <= numLayers; layer++)
                sand[layer] = physical.ParticleSizeSand[layer - 1];
            for (int layer = numLayers+1; layer <= numLayers + NUM_PHANTOM_NODES; layer++)
                sand[layer] = sand[numLayers];    

            // Silt
            BoundCheck(physical.ParticleSizeSilt.Length, numLayers, numLayers, "silt layers");
            silt = new double[numLayers + 1 + NUM_PHANTOM_NODES];
            for (int layer = 1; layer <= numLayers; layer++)
                silt[layer] = physical.ParticleSizeSilt[layer - 1];
            for (int layer = numLayers+1; layer <= numLayers + NUM_PHANTOM_NODES; layer++)
                silt[layer] = silt[numLayers];                               

            // Clay
            BoundCheck(physical.ParticleSizeClay.Length, numLayers, numLayers, "clay layers");
            clay = new double[numLayers + 1 + NUM_PHANTOM_NODES];
            for (int layer = 1; layer <= numLayers; layer++)
                clay[layer] = physical.ParticleSizeClay[layer - 1];
            for (int layer = numLayers+1; layer <= numLayers + NUM_PHANTOM_NODES; layer++)
                clay[layer] = clay[numLayers];                 

            maxSoilTemp = new double[numLayers + 1 + NUM_PHANTOM_NODES];
            minSoilTemp = new double[numLayers + 1 + NUM_PHANTOM_NODES];
            aveSoilTemp = new double[numLayers + 1 + NUM_PHANTOM_NODES];
            volSpecHeatSoil = new double[numNodes + 1];
            soilTemp = new double[numNodes + 1 + 1];
            morningSoilTemp = new double[numNodes + 1 + 1];
            tempNew = new double[numNodes + 1 + 1];
            thermalConductivity = new double[numNodes + 1];
            heatStorage = new double[numNodes + 1];
            thermalConductance = new double[numNodes + 1 + 1];
        }

        /// <summary>Set parameter values and check validity</summary>
        /// <remarks></remarks>
        private void readParam()
        {
            doThermalConductivityCoeffs();

            // set t and tn values to TAve. soil_temp is currently not used
            CalcSoilTemp(ref soilTemp);     // FIXME - returns as zero here because initialisation is not complete.
            soilTemp.CopyTo(tempNew, 0);

            BoundCheck(nu, 0.0, 1.0, "nu");
            BoundCheck(volSpecHeatOM, 1000000.0, 10000000.0, "volSpecHeatOM");
            BoundCheck(volSpecHeatWater, 1000000.0, 10000000.0, "volSpecHeatWater");
            BoundCheck(volSpecHeatClay, 1000000.0, 10000000.0, "volSpecHeatClay");
            BoundCheck(maxTTimeDefault, 0.0, DAYhrs, "maxtTimeDefault");
            BoundCheck(defaultWindSpeed, 0.0, 10.0, "defaultWindSpeed");
            BoundCheck(defaultAltitude, -100.0, 1200.0, "defaultAltitude");
            BoundCheck(defaultInstrumentHeight, 0.0, 5.0, "defaultInstrumentHeight");
            BoundCheck(bareSoilHeight, 0.0, 150.0, "bareSoilHeight");     // "canopy height" when no canopy is present. FIXME - need to test if canopy is absent and set to this.
            soilRoughnessHeight = bareSoilHeight;
            boundarLayerConductanceSource = boundarLayerConductanceSource.ToLower();
            switch (boundarLayerConductanceSource)  // CHECK, this can be deleted
            {
                case "calc":
                case "constant":
                    {
                        break;
                    }

                default:
                    {
                        throw new Exception("bound_layer_cond_source (" + boundarLayerConductanceSource + ") must be either 'calc' or 'constant'");
                    }
            }
            BoundCheck(BoundaryLayerConductanceIterations, 0, 10, "boundaryLayerConductanceIterations");
            BoundCheck(boundaryLayerConductance, 10.0, 40.0, "boundaryLayerConductance");
            boundaryLayerConductanceIterations = System.Convert.ToInt32(BoundaryLayerConductanceIterations);
            netRadiationSource = netRadiationSource.ToLower();
            switch (netRadiationSource)  // CHECK, this can be deleted
            {
                case "calc":
                case "eos":
                    {
                        break;
                    }

                default:
                    {
                        throw new Exception("net_radn_source (" + netRadiationSource + ") must be either 'calc' or 'eos'");
                    }
            }
        }

        /// <summary>Calculate the coefficients for thermal conductivity equation</summary>
        /// <remarks>This is equation 4.20 (Campbell, 1985) for a typical low-quartz, mineral soil</remarks>
        private void doThermalConductivityCoeffs()
        {
            var oldGC1 = thermCondPar1;
            thermCondPar1 = new double[numNodes + 1];  // CHECK, needed these copies??
            if (oldGC1 != null)
                Array.Copy(oldGC1, thermCondPar1, Math.Min(numNodes + 1, oldGC1.Length));     // C1 dimensioned for nodes 0 to Nz
            var oldGC2 = thermCondPar2;
            thermCondPar2 = new double[numNodes + 1];
            if (oldGC2 != null)
                Array.Copy(oldGC2, thermCondPar2, Math.Min(numNodes + 1, oldGC2.Length));     // C2 dimensioned for nodes 0 to Nz
            var oldGC3 = thermCondPar3;
            thermCondPar3 = new double[numNodes + 1];
            if (oldGC3 != null)
                Array.Copy(oldGC3, thermCondPar3, Math.Min(numNodes + 1, oldGC3.Length));     // C3 dimensioned for nodes 0 to Nz
            var oldGC4 = thermCondPar4;
            thermCondPar4 = new double[numNodes + 1];
            if (oldGC4 != null)
                Array.Copy(oldGC4, thermCondPar4, Math.Min(numNodes + 1, oldGC4.Length));     // C4 dimensioned for nodes 0 to Nz

            for (int layer = 1; layer <= numLayers + 1; layer++)
            {
                int element = layer;
                // first coefficient C1 - For many mineral soils, the quartz fraction can be taken as nil, and the equation can
                // be approximated by this equation - 4.27 Campbell.
                thermCondPar1[element] = 0.65 - 0.78 * bulkDensity[layer] + 0.6 * Math.Pow(bulkDensity[layer], 2);      // A approximation to e
                                                                                                                        // The coefficient C2 (B in Campbell 4.25) can be evaluated from data and for mineral soils is approximated by -
                thermCondPar2[element] = 1.06 * bulkDensity[layer];                              // * SW[i]; //B for mineral soil - assume (is there missing text here??)
                                                                                                 // The coefficient C3 (C in Caqmpbell 4.28) determines the water content where thermal conductivity begins to
                                                                                                 // increase rapidly and is highly correlated with clay content. The following correlation appears to fit data well.
                thermCondPar3[element] = 1.0D + MathUtilities.Divide(2.6, Math.Sqrt(clay[layer]), 0);             // C is the water content where co (is there missing text here??)
                                                                                                                  // Coefficient C4 (D in Campbell 4.22) is the thermal conductivity when volumetric water content=0.
                                                                                                                  // For mineral soils with a particle density of 2.65 Mg/m3 the equation becomes the following.
                thermCondPar4[element] = 0.03 + 0.1 * Math.Pow(bulkDensity[layer], 2);           // D assume mineral soil particle d (is there missing text here??)
            }
        }

        /// <summary>Update global variables with external states and check validity of values</summary>
        /// <remarks></remarks>
        private void GetOtherVariables()
        {
            maxAirTemp = weather.MaxT;
            minAirTemp = weather.MinT;
            tempStepSec = Convert.ToDouble(timestep) * MIN2SEC;
            waterBalance.SW.CopyTo(soilWater, 1);
            soilWater[numNodes] = soilWater[numLayers];
            potEvapotrans = waterBalance.Eo;
            potSoilEvap = waterBalance.Eos;
            actualSoilEvap = waterBalance.Es;
            windSpeed = defaultWindSpeed;
            if (microClimate != null)
            {
                canopyHeight = Math.Max(microClimate.CanopyHeight, soilRoughnessHeight) * MM2M;
            }
            else
            {
                canopyHeight = 0.0;
            }

            // Vals HACK. Should be recalculating wind profile.
            instrumentHeight = Math.Max(instrumentHeight, canopyHeight + 0.5);
        }


        /// <summary>Perform actions for current day</summary>
        private void doProcess()
        {
            const int ITERATIONSperDAY = 48;     // number of iterations in a day

            double cva = 0.0;
            double cloudFr = 0.0;
            double[] solarRadn = new double[49];   // Total incoming short wave solar radiation per time-step
            DoNetRadiation(ref solarRadn, ref cloudFr, ref cva, ITERATIONSperDAY);

            // zero the temperature profiles
            MathUtilities.Zero(minSoilTemp);
            MathUtilities.Zero(maxSoilTemp);
            MathUtilities.Zero(aveSoilTemp);
            _boundaryLayerConductance = 0.0;

            double RnTot = 0.0;
            // calc dt
            gDt = Math.Round(tempStepSec / System.Convert.ToDouble(ITERATIONSperDAY));

            // These two call used to be inside the time-step loop. I've taken them outside,
            // as the results do not appear to vary over the course of the day.
            // The results would vary if soil water content were to vary, so if future versions
            // to more communication within sub-daily time steps, these may need to be moved
            // back into the loop. EZJ March 2014
            doVolumetricSpecificHeat();      // RETURNS volSpecHeatSoil() (volumetric heat capacity of nodes)
            doThermConductivity();     // RETURNS gThermConductivity_zb()

            for (int timeStepIteration = 1; timeStepIteration <= ITERATIONSperDAY; timeStepIteration++)
            {
                timeOfDaySecs = gDt * System.Convert.ToDouble(timeStepIteration);
                if (tempStepSec < DAYsecs)
                    airTemp = 0.5 * (maxAirTemp + minAirTemp);
                else
                    airTemp = InterpTemp(timeOfDaySecs * SEC2HR);
                // Convert to hours //most of the arguments in FORTRAN version are global vars so
                // do not need to pass them here, they can be accessed inside InterpTemp
                tempNew[AIRnode] = airTemp;

                netRadiation = RadnNetInterpolate(solarRadn[timeStepIteration], cloudFr, cva);
                RnTot += netRadiation;   // for debugging only

                switch (boundarLayerConductanceSource)
                {
                    case "constant":
                        {
                            thermalConductivity[AIRnode] = boundaryLayerConductanceConst();
                            break;
                        }

                    case "calc":
                        {
                            // When calculating the boundary layer conductance it is important to iterate the entire
                            // heat flow calculation at least once, since surface temperature depends on heat flux to
                            // the atmosphere, but heat flux to the atmosphere is determined, in part, by the surface
                            // temperature.
                            thermalConductivity[AIRnode] = boundaryLayerConductanceF(ref tempNew);
                            for (int iteration = 1; iteration <= boundaryLayerConductanceIterations; iteration++)
                            {
                                doThomas(ref tempNew);        // RETURNS TNew_zb()
                                thermalConductivity[AIRnode] = boundaryLayerConductanceF(ref tempNew);
                            }

                            break;
                        }
                }
                // Now start again with final atmosphere boundary layer conductance
                doThomas(ref tempNew);        // RETURNS gTNew_zb()
                doUpdate(ITERATIONSperDAY);
                if ((RealsAreEqual(timeOfDaySecs, 5.0 * HR2MIN * MIN2SEC)))
                    soilTemp.CopyTo(morningSoilTemp, 0);
            }

            // Next two for DEBUGGING only
            double RnByEos = potSoilEvap * LAMBDA * J2MJ;   // Es*L = latent heat flux LE and Eos*L = net Radiation Rn.
            double LEbyEs = actualSoilEvap * LAMBDA * J2MJ;   // Es*L = latent heat flux LE and Eos*L = net Radiation Rn.

            minTempYesterday = minAirTemp;
            maxTempYesterday = maxAirTemp;
        }

        /// <summary>Calculate the volumetric specific heat capacity (Cv, Joules*m-3*K-1) of each soil layer</summary>
        /// <remarks>
        /// from Campbell, G.S. (1985). Soil physics with BASIC: Transport models for soil-plant systems
        /// </remarks>
        private void doVolumetricSpecificHeatCampbell()
        {
            const double SPECIFICbd = 2.65;   // (g/cc) specific bulk density
            double[] volSpecHeatSoil = new double[numLayers + 1];

            for (int layer = 1; layer <= numLayers; layer++)
            {
                double solidity = bulkDensity[layer] / SPECIFICbd;

                // the Campbell version
                // heatStore[i] = (vol_spec_heat_clay * (1-porosity) + vol_spec_heat_water * SWg[i]) * (zb_z[i+1]-zb_z[i-1])/(2*real(dt));
                volSpecHeatSoil[layer] = volSpecHeatClay * solidity + volSpecHeatWater * soilWater[layer];
            }

            // mapLayer2Node(volSpecHeatSoil, gVolSpecHeatSoil)
            volSpecHeatSoil.CopyTo(this.volSpecHeatSoil, 1);     // map volumetric heat capicity (Cv) from layers to nodes (node 2 in centre of layer 1)
            this.volSpecHeatSoil[1] = volSpecHeatSoil[1];        // assume surface node Cv is same as top layer Cv
        }

        /// <summary>Calculate the volumetric specific heat capacity (Cv, Joules*m-3*K-1) of each soil layer</summary>
        /// <remarks> CHECK
        /// Modified from Campbell, G.S. (1985). Soil physics with BASIC: Transport models for soil-plant systems
        /// </remarks>
        private void doVolumetricSpecificHeat()
        {
            double[] volSpecHeatSoil = new double[numNodes + 1];

            for (int node = 1; node <= numNodes; node++)
            {
                volSpecHeatSoil[node] = 0;
                foreach (var constituentName in soilConstituentNames.Except(new string[] {"Minerals"}))
                {
                    volSpecHeatSoil[node] += volumetricSpecificHeat(constituentName, node) * MJ2J * soilWater[node];
                }
            }
            // now get weighted average for soil elements between the nodes. i.e. map layers to nodes
            mapLayer2Node(volSpecHeatSoil, ref this.volSpecHeatSoil);            
            //volSpecHeatSoil.CopyTo(this.volSpecHeatSoil, 1);     // map volumetric heat capicity (Cv) from layers to nodes (node 2 in centre of layer 1)
            //this.volSpecHeatSoil[1] = volSpecHeatSoil[1];        // assume surface node Cv is same as top layer Cv
        }        

        /// <summary>Calculate the thermal conductivity of each soil layer (CHECK unit)</summary>
        private void doThermConductivity()
        {
            double[] thermCondLayers = new double[numNodes + 1];

            for (int node = 1; node <= numNodes; node++)
            {
                double numerator = 0.0; 
                double denominator = 0.0;
                foreach (var constituentName in soilConstituentNames)
                {
                    double shapeFactorConstituent = shapeFactor(constituentName, node);
                    double thermalConductanceConstituent = ThermalConductance(constituentName, node);
                    double thermalConductanceWater = ThermalConductance("Water", node);
                    double k = (2.0 / 3.0) * Math.Pow(1 + shapeFactorConstituent * (thermalConductanceConstituent / thermalConductanceWater - 1.0), -1) +
                               (1.0 / 3.0) * Math.Pow(1 + shapeFactorConstituent * (thermalConductanceConstituent / thermalConductanceWater - 1.0) * (1 - 2 * shapeFactorConstituent), -1);
                    numerator += thermalConductanceConstituent * soilWater[node] * k;
                    denominator += soilWater[node] * k;
                }

                thermCondLayers[node] = numerator / denominator;
            }

            // now get weighted average for soil elements between the nodes. i.e. map layers to nodes
            mapLayer2Node(thermCondLayers, ref thermalConductivity);
        }

        /// <summary>Calculate the thermal conductivity of each soil layer (W/m2/K, CHECK, should be W/m/K)</summary>
        /// <remarks>
        /// From Campbell, G.S. (1985). Soil physics with BASIC: Transport models for soil-plant systems
        /// Equation 4.20 where Lambda = A + B*Theta - (A-D)*exp[-(C*theta)^E]
        ///  Lambda is the thermal conductivity, theta is volumetric water content and A, B, C, D, E are coefficients.
        ///  When theta = 0, lambda = D. At saturation, the last term becomes zero and Lambda = A + B*theta.
        ///  The constant E can be assigned a value of 4. The constant C determines the water content where thermal
        ///  conductivity begins to increase rapidly and is highly correlated with clay content.
        ///  Here C1=A, C2=B, SW=theta, C3=C, C4=D, 4=E.
        /// </remarks>
        private void doThermConductivityCampbell()
        {
            double temp = 0.0;
            double[] thermCondLayers = new double[numNodes + 1];
            for (int layer = 1; layer <= numNodes; layer++)
            {
                temp = Math.Pow((thermCondPar3[layer] * soilWater[layer]), 4) * (-1);
                thermCondLayers[layer] = thermCondPar1[layer] + (thermCondPar2[layer] * soilWater[layer])
                                       - (thermCondPar1[layer] - thermCondPar4[layer]) * Math.Exp(temp);  // Eqn 4.20 Campbell.
            }

            // now get weighted average for soil elements between the nodes. i.e. map layers to nodes
            mapLayer2Node(thermCondLayers, ref thermalConductivity);
        }

        private void mapLayer2Node(double[] layerArray, ref double[] nodeArray)
        {
            // now get weighted average for soil elements between the nodes. i.e. map layers to nodes
            for (int node = SURFACEnode; node <= numNodes; node++)
            {
                int layer = node - 1;     // node n lies at the centre of layer n-1
                double depthLayerAbove = layer >= 1 ? SumOfRange(thickness, 1, layer) : 0.0;
                double d1 = depthLayerAbove - (depth[node] * M2MM);
                double d2 = depth[node + 1] * M2MM - depthLayerAbove;
                double dSum = d1 + d2;

                nodeArray[node] = MathUtilities.Divide(layerArray[layer] * d1, dSum, 0)
                                + MathUtilities.Divide(layerArray[layer + 1] * d2, dSum, 0);
            }
        }

        /// <summary>Numerical solution of the differential equations based on Thomas algorithm</summary>
        /// <remarks>
        /// Solves the tri_diagonal matrix using the Thomas algorithm
        /// Thomas, L.H. (1946). Elliptic problems in linear difference equations over a network
        /// John Hargreaves' version from Campbell Program 4.1
        /// </remarks>
        private void doThomas(ref double[] newTemps)
        {
            double[] a = new double[numNodes + 1 + 1];    // A; thermal conductance at next node (W/m/K)
            double[] b = new double[numNodes + 1];        // B; heat storage at node (W/K)
            double[] c = new double[numNodes + 1];        // C; thermal conductance at node (W/m/K)
            double[] d = new double[numNodes + 1];        // D; heat flux at node (w/m) and then temperature
                                                          // nu = F; Nz = M; 1-nu = G; T_zb = T; newTemps = TN;

            thermalConductance[AIRnode] = thermalConductivity[AIRnode];
            // The first node gZ_zb(1) is at the soil surface (Z = 0)
            for (int node = SURFACEnode; node <= numNodes; node++)
            {
                double VolSoilAtNode = 0.5 * (depth[node + 1] - depth[node - 1]);   // Volume of soil around node (m^3), assuming area is 1 m^2
                heatStorage[node] = MathUtilities.Divide(volSpecHeatSoil[node] * VolSoilAtNode, gDt, 0);       // Joules/s/K or W/K
                                                                                                               // rate of heat
                                                                                                               // convert to thermal conductance
                double elementLength = depth[node + 1] - depth[node];             // (m)
                thermalConductance[node] = MathUtilities.Divide(thermalConductivity[node], elementLength, 0);  // (W/m/K)
            }

            // Debug test: multiplyArray(gThermalConductance_zb, 2.0)

            // John's version
            double g = 1 - nu;
            for (int node = SURFACEnode; node <= numNodes; node++)
            {
                c[node] = (-nu) * thermalConductance[node];   //
                a[node + 1] = c[node];             // Eqn 4.13
                b[node] = nu * (thermalConductance[node] + thermalConductance[node - 1]) + heatStorage[node];    // Eqn 4.12
                                                                                                                 // Eqn 4.14
                d[node] = g * thermalConductance[node - 1] * soilTemp[node - 1]
                        + (heatStorage[node] - g * (thermalConductance[node] + thermalConductance[node - 1])) * soilTemp[node]
                        + g * thermalConductance[node] * soilTemp[node + 1];
            }
            a[SURFACEnode] = 0.0D;

            // The boundary condition at the soil surface is more complex since convection and radiation may be important.
            // When radiative and latent heat transfer are unimportant, then D(1) = D(1) + nu*K(0)*TN(0).
            // d(SURFACEnode) += nu * thermalConductance(AIRnode) * newTemps(AIRnode)       ' Eqn. 4.16
            double sensibleHeatFlux = nu * thermalConductance[AIRnode] * newTemps[AIRnode];       // Eqn. 4.16

            // When significant radiative and/or latent heat transfer occur they are added as heat sources at node 1
            // to give D(1) = D(1) = + nu*K(0)*TN(0) - Rn + LE, where Rn is net radiation at soil surface and LE is the
            // latent heat flux. Eqn. 4.17

            double RadnNet = 0.0;     // (W/m)
            switch (netRadiationSource)
            {
                case "calc":
                    {
                        RadnNet = MathUtilities.Divide(netRadiation * MJ2J, gDt, 0);       // net Radiation Rn heat flux (J/m2/s or W/m2).
                        break;
                    }

                case "eos":
                    {
                        // If (gEos - gEs) > 0.2 Then
                        RadnNet = MathUtilities.Divide(potSoilEvap * LAMBDA, tempStepSec, 0);    // Eos*L = net Radiation Rn heat flux.
                        break;
                    }
            }
            double LatentHeatFlux = MathUtilities.Divide(actualSoilEvap * LAMBDA, tempStepSec, 0);      // Es*L = latent heat flux LE (W/m)
            double SoilSurfaceHeatFlux = sensibleHeatFlux + RadnNet - LatentHeatFlux;  // from Rn = G + H + LE (W/m)
            d[SURFACEnode] += SoilSurfaceHeatFlux;        // FIXME JNGH testing alternative net radn

            // last line is unfulfilled soil water evaporation
            // The boundary condition at the bottom of the soil column is usually specified as remaining at some constant,
            // measured temperature, TN(M+1). The last value for D is therefore -

            d[numNodes] += nu * thermalConductance[numNodes] * newTemps[numNodes + 1];
            // For a no-flux condition, K(M) = 0, so nothing is added.

            // The Thomas algorithm
            // Calculate coeffs A, B, C, D for intermediate nodes
            for (int node = SURFACEnode; node <= numNodes - 1; node++)
            {
                c[node] = MathUtilities.Divide(c[node], b[node], 0);
                d[node] = MathUtilities.Divide(d[node], b[node], 0);
                b[node + 1] -= a[node + 1] * c[node];
                d[node + 1] -= a[node + 1] * d[node];
            }
            newTemps[numNodes] = MathUtilities.Divide(d[numNodes], b[numNodes], 0);  // do temperature at bottom node

            // Do temperatures at intermediate nodes from second bottom to top in soil profile
            for (int node = numNodes - 1; node >= SURFACEnode; node += -1)
            {
                newTemps[node] = d[node] - c[node] * newTemps[node + 1];
                BoundCheck(newTemps[node], -50.0, 100.0, "newTemps(" + node.ToString() + ")");
            }
        }

        /// <summary>Numerical solution of the differential equations based on Thomas algorithm</summary>
        /// <remarks>
        /// Solves the tri_diagonal matrix using the Thomas algorithm
        /// Thomas, L.H. (1946). Elliptic problems in linear difference equations over a network
        /// Val Snow's original version
        /// </remarks>
        private void doThomas_VS(ref double[] newTemps)
        {
            double[] a = new double[numNodes + 1 + 1];    // A;
            double[] b = new double[numNodes + 1];        // B;
            double[] c = new double[numNodes + 1];        // C;
            double[] d = new double[numNodes + 1];        // D;
            double[] heat = new double[numNodes + 1];     // CP; heat storage between nodes - index is same as upper node
            double[] Therm_zb = new double[numNodes + 1]; // K; conductance between nodes - index is same as upper node
                                                          // nu = F; Nz = M; 1-nu = G; T_zb = T; newTemps = TN;

            Therm_zb[0] = thermalConductivity[0];
            for (int node = 1; node <= numNodes; node++)
            {
                heat[node] = MathUtilities.Divide(volSpecHeatSoil[node] * 0.5 * (depth[node + 1] - depth[node - 1]), gDt, 0);
                // rate of heat
                // convert to thermal conduc
                Therm_zb[node] = MathUtilities.Divide(thermalConductivity[node], depth[node + 1] - depth[node], 0);
            }

            // My version
            a[1] = 0;
            b[1] = nu * Therm_zb[1] + nu * Therm_zb[0] + heat[1];
            c[1] = (-nu) * Therm_zb[1];
            d[1] = soilTemp[0] * (1 - nu) * Therm_zb[0] - soilTemp[1] * (1 - nu) * Therm_zb[1] - soilTemp[1] * (1 - nu) * Therm_zb[0] + soilTemp[1] * heat[1] + soilTemp[2] * (1 - nu) * Therm_zb[1] + Therm_zb[0] * newTemps[0] * nu;

            if ((potSoilEvap - actualSoilEvap) > 0.2)
                d[1] += MathUtilities.Divide((potSoilEvap - actualSoilEvap) * LAMBDA, tempStepSec, 0);
            else
            {
            }

            // last line is unfullfilled soil water evaporation
            // the main loop
            for (int i = 2; i <= numNodes - 1; i++)
            {
                a[i] = (-nu) * Therm_zb[i - 1];
                b[i] = nu * Therm_zb[i] + nu * Therm_zb[i - 1] + heat[i];
                c[i] = (-nu) * Therm_zb[i];
                d[i] = soilTemp[i - 1] * (1 - nu) * Therm_zb[i - 1] - soilTemp[i] * (1 - nu) * Therm_zb[i] - soilTemp[i] * (1 - nu) * Therm_zb[i - 1] + soilTemp[i] * heat[i] + soilTemp[i + 1] * (1 - nu) * Therm_zb[i];
            }

            // lower node
            a[numNodes] = (-nu) * Therm_zb[numNodes - 1];
            a[numNodes + 1] = (-nu) * Therm_zb[numNodes];
            b[numNodes] = nu * Therm_zb[numNodes] + nu * Therm_zb[numNodes - 1] + heat[numNodes];
            c[numNodes] = 0.0D;
            c[numNodes] = (-nu) * Therm_zb[numNodes];
            d[numNodes] = soilTemp[numNodes - 1] * (1 - nu) * Therm_zb[numNodes - 1] - soilTemp[numNodes] * (1 - nu) * Therm_zb[numNodes] - soilTemp[numNodes] * (1 - nu) * Therm_zb[numNodes - 1] + soilTemp[numNodes] * heat[numNodes] + soilTemp[numNodes + 1] * (1 - nu) * Therm_zb[numNodes] + Therm_zb[numNodes] * nu * newTemps[numNodes + 1];

            // the Thomas algorithm
            for (int node = 1; node <= numNodes - 1; node++)
            {
                c[node] = MathUtilities.Divide(c[node], b[node], 0);
                d[node] = MathUtilities.Divide(d[node], b[node], 0);
                b[node + 1] -= a[node + 1] * c[node];
                d[node + 1] -= a[node + 1] * d[node];
            }
            newTemps[numNodes] = MathUtilities.Divide(d[numNodes], b[numNodes], 0);

            for (int node = numNodes - 1; node >= 1; node += -1)
                newTemps[node] = d[node] - c[node] * newTemps[node + 1];
        }

        /// <summary>Interpolate air temperature</summary>
        /// <param name="timeHours">time of day at which air temperature is required</param>
        /// <returns>Interpolated air temperature for specified time of day (oC)</returns>
        /// <remarks>
        ///  Between midnight and MinT_time just a linear interpolation between
        ///  yesterday's midnight temperature and today's MinTg.
        ///  For the rest of the day use a sin function.
        /// Note: This can result in the Midnight temperature being lower than the following minimum.
        /// </remarks>
        private double InterpTemp(double timeHours)
        {
            double time = timeHours / DAYhrs;           // Current time of day as a fraction of a day
            double maxT_time = maxTempTime / DAYhrs;     // Time of maximum temperature as a fraction of a day
            double minT_time = maxT_time - 0.5;       // Time of minimum temperature as a fraction of a day

            if (time < minT_time)
            {
                // Current time of day is between midnight and time of minimum temperature
                double midnightT = Math.Sin((0.0 + 0.25 - maxT_time) * 2.0 * Math.PI)
                                        * (maxTempYesterday - minTempYesterday) / 2.0
                                        + (maxTempYesterday + minTempYesterday) / 2.0;
                double tScale = (minT_time - time) / minT_time;

                // set bounds for t_scale (0 <= tScale <= 1)
                if (tScale > 1.0)
                    tScale = 1.0;
                else if (tScale < 0)
                    tScale = 0;

                double CurrentTemp = minAirTemp + tScale * (midnightT - minAirTemp);
                return CurrentTemp;
            }
            else
            {
                // Current time of day is at time of minimum temperature or after it up to midnight.
                double CurrentTemp = Math.Sin((time + 0.25 - maxT_time) * 2.0 * Math.PI)
                                          * (maxAirTemp - minAirTemp) / 2.0
                                          + (maxAirTemp + minAirTemp) / 2.0;
                return CurrentTemp;
            }
        }

        /// <summary>Determine min, max, and average soil temperature from the half-hourly iterations</summary>
        /// <param name="IterationsPerDay">number of times in a day the function is called</param>
        /// <remarks></remarks>
        private void doUpdate(int IterationsPerDay)
        {
            // Now transfer to old temperature array
            tempNew.CopyTo(soilTemp, 0);

            // initialise the min & max to soil temperature if this is the first iteration
            if (timeOfDaySecs < gDt * 1.2)
            {
                for (int node = SURFACEnode; node <= numNodes; node++)
                {
                    minSoilTemp[node] = soilTemp[node];
                    maxSoilTemp[node] = soilTemp[node];
                }
            }

            for (int node = SURFACEnode; node <= numNodes; node++)
            {
                if (soilTemp[node] < minSoilTemp[node])
                    minSoilTemp[node] = soilTemp[node];
                else if (soilTemp[node] > maxSoilTemp[node])
                    maxSoilTemp[node] = soilTemp[node];
                aveSoilTemp[node] += MathUtilities.Divide(soilTemp[node], System.Convert.ToDouble(IterationsPerDay), 0);
            }
            _boundaryLayerConductance += MathUtilities.Divide(thermalConductivity[AIRnode], System.Convert.ToDouble(IterationsPerDay), 0);
        }

        /// <summary>Calculate the density of air at a given environmental conditions</summary>
        /// <param name="temperature">temperature (oC)</param>
        /// <param name="AirPressure">air pressure (hPa)</param>
        /// <returns>density of air (kg/m3)</returns>
        /// <remarks>From - CHECK</remarks>
        private double RhoA(double temperature, double AirPressure)
        {
            const double MWair = 0.02897;     // molecular weight air (kg/mol)
            const double RGAS = 8.3143;       // universal gas constant (J/mol/K)
            const double HPA2PA = 100.0;      // hectoPascals to Pascals

            return Divide(MWair * AirPressure * HPA2PA , kelvinT(temperature) * RGAS , 0.0);
        }

        /// <summary>Calculate atmospheric boundary layer conductance</summary>
        /// <returns>thermal conductivity of surface layer (W/m2/K) - CHECK units</returns>
        /// <remarks> 
        /// From Program 12.2, p140, Campbell, Soil Physics with Basic.
        /// During first stage drying, evaporation prevents the surface from becoming hot,
        /// so stability corrections are small. Once the surface dries and becomes hot, boundary layer
        /// resistance is relatively unimportant in determining evaporation rate.
        /// A dry soil surface reaches temperatures well above air temperatures during the day, and can be well
        /// below air temperature on a clear night. Thermal stratification on a clear night can be strong enough
        /// to reduce sensible heat exchange between the soil surface and the air to almost nothing. If stability
        /// corrections are not made, soil temperature profiles can have large errors.
        /// </remarks>
        private double boundaryLayerConductanceF(ref double[] TNew_zb)
        {
            const double VONK = 0.41;                 // VK; von Karman's constant
            const double GRAVITATIONALconst = 9.8;    // GR; gravitational constant (m/s/s)
            const double CAPP = 1010.0;               // (J/kg/K) Specific heat of air at constant pressure
            const double EMISSIVITYsurface = 0.98;
            double SpecificHeatAir = CAPP * RhoA(airTemp, airPressure); // CH; volumetric specific heat of air (J/m3/K) (1200 at 200C at sea level)
                                                                        // canopy_height, instrum_ht (Z) = 1.2m, AirPressure = 1010
                                                                        // gTNew_zb = TN; gAirT = TA;

            // Zero plane displacement and roughness parameters depend on the height, density and shape of
            // surface roughness elements. For typical crop surfaces, the following empirical correlations have
            // been obtained. (Extract from Campbell p138.). Canopy height is the height of the roughness elements.
            double RoughnessFacMomentum = 0.13 * canopyHeight;    // ZM; surface roughness factor for momentum
            double RoughnessFacHeat = 0.2 * RoughnessFacMomentum;  // ZH; surface roughness factor for heat
            double d = 0.77 * canopyHeight;                       // D; zero plane displacement for the surface

            double SurfaceTemperature = TNew_zb[SURFACEnode];    // surface temperature (oC)

            // To calculate the radiative conductance term of the boundary layer conductance, we need to account for canopy and residue cover
            // Calculate a diffuce penetration constant (KL Bristow, 1988. Aust. J. Soil Res, 26, 269-80. The Role of Mulch and its Architecture
            // in modifying soil temperature). Here we estimate this using the Soilwat algorithm for calculating EOS from EO and the cover effects,
            // assuming the cover effects on EO are similar to Bristow's diffuse penetration constant - 0.26 for horizontal mulch treatment and 0.44
            // for vertical mulch treatment.
            double PenetrationConstant = Math.Max(0.1, potSoilEvap) / Math.Max(0.1, potEvapotrans);

            // Campbell, p136, indicates the radiative conductance is added to the boundary layer conductance to form a combined conductance for
            // heat transfer in the atmospheric boundary layer. Eqn 12.9 modified for residue and plant canopy cover
            double radiativeConductance = 4.0 * STEFAN_BOLTZMANNconst * EMISSIVITYsurface * PenetrationConstant
                                               * Math.Pow(kelvinT(airTemp), 3);    // Campbell uses air temperature in leiu of surface temperature

            // Zero iteration variables
            double FrictionVelocity = 0.0;        // FV; UStar
            double BoundaryLayerCond = 0.0;       // KH; sensible heat flux in the boundary layer;(OUTPUT) thermal conductivity  (W/m2/K)
            double StabilityParam = 0.0;          // SP; Index of the relative importance of thermal and mechanical turbulence in boundary layer transport.
            double StabilityCorMomentum = 0.0;    // PM; stability correction for momentum
            double StabilityCorHeat = 0.0;        // PH; stability correction for heat
            double HeatFluxDensity = 0.0;         // H; sensible heat flux in the boundary layer

            // Since the boundary layer conductance is a function of the heat flux density, an iterative method must be used to find the boundary layer conductance.
            for (int iteration = 1; iteration <= 3; iteration++)
            {
                // Heat and water vapour are transported by eddies in the turbulent atmosphere above the crop.
                // Boundary layer conductance would therefore be expected to vary depending on the wind speed and level
                // of turbulence above the crop. The level of turbulence, in turn, is determined by the roughness of the surface,
                // the distance from the surface and the thermal stratification of the boundary layer.
                // Eqn 12.11 Campbell
                FrictionVelocity = MathUtilities.Divide(windSpeed * VONK,
                                                        Math.Log(MathUtilities.Divide(instrumentHeight - d + RoughnessFacMomentum,
                                                                                      RoughnessFacMomentum,
                                                                                      0)) + StabilityCorMomentum,
                                                        0);
                // Eqn 12.10 Campbell
                BoundaryLayerCond = MathUtilities.Divide(SpecificHeatAir * VONK * FrictionVelocity,
                                                         Math.Log(MathUtilities.Divide(instrumentHeight - d + RoughnessFacHeat,
                                                                                       RoughnessFacHeat, 0)) + StabilityCorHeat,
                                                         0);

                BoundaryLayerCond += radiativeConductance; // * (1.0 - sunAngleAdjust())

                HeatFluxDensity = BoundaryLayerCond * (SurfaceTemperature - airTemp);
                // Eqn 12.14
                StabilityParam = MathUtilities.Divide(-VONK * instrumentHeight * GRAVITATIONALconst * HeatFluxDensity,
                                                      SpecificHeatAir * kelvinT(airTemp) * Math.Pow(FrictionVelocity, 3.0)
                                                      , 0);

                // The stability correction parameters correct the boundary layer conductance for the effects
                // of buoyancy in the atmosphere. When the air near the surface is hotter than the air above,
                // the atmosphere becomes unstable, and mixing at a given wind speed is greater than would occur
                // in a neutral atmosphere. If the air near the surface is colder than the air above, the atmosphere
                // is unstable and mixing is suppressed.

                if (StabilityParam > 0.0)
                {
                    // Stable conditions, when surface temperature is lower than air temperature, the sensible heat flux
                    // in the boundary layer is negative and stability parameter is positive.
                    // Eqn 12.15
                    StabilityCorHeat = 4.7 * StabilityParam;
                    StabilityCorMomentum = StabilityCorHeat;
                }
                else
                {
                    // Unstable conditions, when surface temperature is higher than air temperature, sensible heat flux in the
                    // boundary layer is positive and stability parameter is negative.
                    StabilityCorHeat = -2.0 * Math.Log((1.0 + Math.Sqrt(1.0 - 16.0 * StabilityParam)) / 2.0);    // Eqn 12.16
                    StabilityCorMomentum = 0.6 * StabilityCorHeat;                // Eqn 12.17
                }
            }
            return BoundaryLayerCond;   // thermal conductivity  (W/m2/K)
        }

        /// <summary>Calculate boundary layer conductance</summary>
        /// <returns>thermal conductivity  (W/m2/K) - CHECK units</returns>
        /// <remarks>From Program 12.2, p140, Campbell, Soil Physics with Basic</remarks>
        private double boundaryLayerConductanceConst() // CHECK, should not be needed...
        {

            // canopy_height, instrum_ht = 1.2m, AirPressure = 1010
            // therm_cond(0) = 20.0 ' boundary layer conductance W m-2 K-1

            return boundaryLayerConductance; // (W/m2/K)
        }

        /// <summary>Convert degrees Celcius to Kelvin</summary>
        /// <param name="celciusT">Temperature (oC)</param>
        /// <returns>Temperature (K)</returns>
        private double kelvinT(double celciusT)
        {
            const double ZEROTkelvin = 273.18;
            return celciusT + ZEROTkelvin;
        }

        /// <summary>Computes the long-wave radiation emitted by a body</summary>
        /// <param name="emissivity">The emissivity of a body</param>
        /// <param name="tDegC">The temperature of the body</param>
        /// <returns>The radiation emitted (W - CHECK unit)</returns>
        private double longWaveRadn(double emissivity, double tDegC)
        {
            return STEFAN_BOLTZMANNconst * emissivity * Math.Pow(kelvinT(tDegC), 4);
        }

        /// <summary>Calculates average soil temperature at the centre of each layer</summary>
        /// <param name="soilTempIO">temperature of each layer in profile</param>
        private void CalcSoilTemp(ref double[] soilTempIO) // CHECK, is byRef a good idea?
        {
            double[] cumulativeDepth = SoilUtilities.ToCumThickness(thickness);
            double w = 2 * Math.PI / (365.25 * 24 * 3600);
            double dh = 0.6;   // this needs to be in mm a default value for a loam at field capacity - consider makeing this settable
            double zd = Math.Sqrt( 2 * dh / w);
            double offset = 0.25;  // moves the "0" and rise in the sin to the spring equinox for southern latitudes 
            if (weather.Latitude > 0.0)  // to cope with the northern summer
                offset = -0.25;

            double[] soilTemp = new double[numNodes + 1 + 1];
            for (int nodes = 1; nodes <= numNodes; nodes++)
            {
                soilTemp[nodes] = weather.Tav + weather.Amp * Math.Exp(-1 * cumulativeDepth[nodes] / zd) * 
                                                              Math.Sin((clock.Today.DayOfYear / 365.0 + offset) * 2.0 * Math.PI - cumulativeDepth[nodes] / zd);
            }

            Array.ConstrainedCopy(soilTemp, 0, soilTempIO, SURFACEnode, numNodes);
        }

        /// <summary>Gets the average soil temperature for each soil layer</summary>
        /// <param name="depthLag">The lag factor for depth (radians)</param>
        /// <param name="alx">The time of a g_year from hottest instance (radians)</param>
        /// <param name="deltaTemp">The change in surface soil temperature since the hottest day (oC)</param>
        /// <returns>The temperature of each soil layer (oC)</returns>
        /// <remarks>
        /// The difference in temperature between surface and subsurface layers is an exponential function
        /// of the ratio of the depth at the bottom of the layer and the temperature damping depth of the soil
        /// </remarks>
        private double LayerTemp(double depthLag, double alx, double deltaTemp)
        {
            return weather.Tav + (weather.Amp / 2.0 * Math.Cos(alx - depthLag) + deltaTemp) * Math.Exp(-depthLag);
        }

        /// <summary>Calculates the rate of change in soil surface temperature with time</summary>
        /// <param name="alx">The time of year in radians from warmest instance</param>
        /// <returns>Change in temperature</returns>
        /// <remarks>
        ///           jngh 24-12-91.  I think this is actually a correction to adjust
        ///           today's normal sinusoidal soil surface temperature to the
        ///           current temperature conditions.
        /// </remarks>
        private double TempDelta(double alx)
        {
            // Get today's top layer temp from yesterdays temp and today's weather conditions.
            // The actual soil surface temperature is affected by current weather conditions.

            // Get today's normal surface soil temperature
            // There is no depth lag, being the surface, and there is no adjustment for the current
            //  temperature conditions as we want the "normal" sinusoidal temperature for this time of year

            double temp_a = LayerTemp(0.0, alx, 0.0);

            // Get the rate of change in soil surface temperature with time.
            // This is the difference between a five-day moving average and
            // today's normal surface soil temperature.

            double dT = SurfaceTemperatureInit() - temp_a;

            // check output
            BoundCheck(dT, -100.0, 100.0, "Initial SoilTemp_dt");

            return dT;
        }

        /// <summary>Calculate initial soil surface temperature</summary>
        /// <returns>The initial soil surface temperature (oC)</returns>
        private double SurfaceTemperatureInit()
        {
            double ave_temp = (maxAirTemp + minAirTemp) * 0.5;
            double surfaceT = (1.0 - waterBalance.Salb) * (ave_temp + (maxAirTemp - ave_temp) * Math.Sqrt(Math.Max(weather.Radn, 0.1) * 23.8846 / 800.0)) + waterBalance.Salb * ave_temp;
            BoundCheck(surfaceT, -100.0, 100.0, "Initial surfaceT");
            return surfaceT;
        }

        /// <summary>Gets the temperature damping depth</summary>
        /// <returns>soil temperature damping depth (mm)</returns>
        /// <remarks>
        /// This is a function of the
        ///             average bulk density of the soil and the amount of water above
        ///             the lower limit. I think the damping depth units are
        ///             mm depth/radian of a year
        ///  Notes
        ///       241091 consulted Brian Wall.  For soil temperature an estimate of
        ///       the water content of the total profile is required, not the plant
        ///       extractable soil water.  Hence the method used here - difference
        ///       total lower limit and total soil water instead of sum of differences
        ///       constrained to and above.  Here the use of lower limit is of no
        ///       significance - it is merely a reference point, just as 0.0 could
        ///       have been used.  jngh
        /// </remarks>
        private double DampingDepth()
        {
            const double SW_AVAIL_TOT_MIN = 0.01;   // minimum available sw (mm water)

            double ave_bd;                // average bulk density over layers (g/cc soil)
            double sw_avail_tot;          // amount of sw above lower limit (mm water)
            double b;                     // intermediate variable
            double cum_depth;             // cumulative depth in profile (mm)
            double damp_depth_max;        // maximum damping depth (potential)(mm soil/radian of a g_year (58 days))
            double f;                     // fraction of potential damping depth discounted by water content of soil (0-1)
            double favbd;                 // a function of average bulk density
            double wcf;                   // a function of water content (0-1)
            double bd_tot;                // total bulk density over profile (g/cc soil)
            double ll_tot;                // total lower limit over profile (mm water)
            double sw_dep_tot;            // total soil water over profile (mm water)
            double wc;                    // water content of profile (0-1)
            double ww;                    // potential sw above lower limit (mm water/mm soil)

            // - Implementation Section ----------------------------------

            // get average bulk density

            bd_tot = sum_products_real_array(bulkDensity, thickness);
            cum_depth = SumOfRange(thickness, 1, numLayers);
            ave_bd = Divide(bd_tot, cum_depth, 0.0);

            // favbd ranges from almost 0 to almost 1
            // damp_depth_max ranges from 1000 to almost 3500
            // It seems damp_depth_max is the damping depth potential.

            favbd = Divide(ave_bd, (ave_bd + 686.0 * Math.Exp(-5.63 * ave_bd)), 0.0);
            damp_depth_max = 1000.0 + 2500.0 * favbd;
            damp_depth_max = Math.Max(damp_depth_max, 0.0);

            // Potential sw above lower limit - mm water/mm soil depth
            // note that this function says that average bulk density
            // can't go above 2.47222, otherwise potential becomes negative.
            // This function allows potential (ww) to go from 0 to .356

            ww = 0.356 - 0.144 * ave_bd;
            ww = Math.Max(ww, 0.0);


            // calculate amount of soil water, using lower limit as the
            // reference point.

            var ll15mm = MathUtilities.Multiply(physical.LL15, physical.Thickness);
            ll_tot = SumOfRange(ll15mm, 0, numLayers - 1);
            sw_dep_tot = SumOfRange(waterBalance.SWmm, 0, numLayers - 1);
            sw_avail_tot = sw_dep_tot - ll_tot;
            sw_avail_tot = Math.Max(sw_avail_tot, SW_AVAIL_TOT_MIN);

            // get fractional water content -

            // wc can range from 0 to 1 while
            // wcf ranges from 1 to 0

            wc = Divide(sw_avail_tot, (ww * cum_depth), 1.0);
            wc = bound(wc, 0.0, 1.0);
            wcf = Divide((1.0 - wc), (1.0 + wc), 0.0);

            // Here b can range from -.69314 to -1.94575
            // and f ranges from 1 to  0.142878
            // When wc is 0, wcf=1 and f=500/damp_depth_max
            // and soiln2_SoilTemp_DampDepth=500
            // When wc is 1, wcf=0 and f=1
            // and soiln2_SoilTemp_DampDepth=damp_depth_max
            // and that damp_depth_max is the maximum.

            b = Math.Log(Divide(500.0, damp_depth_max, 10000000000.0));

            f = Math.Exp(b * Math.Pow(wcf, 2));

            // Get the temperature damping depth. (mm soil/radian of a g_year)
            // discount the potential damping depth by the soil water deficit.
            // Here soiln2_SoilTemp_DampDepth ranges from 500 to almost
            // 3500 mm/58 days.

            return f * damp_depth_max;
        }

        /// <summary>Calculate initial variables for net radiation per time-step</summary>
        /// <param name="solarRadn"></param>
        /// <param name="cloudFr"></param>
        /// <param name="cva"></param>
        /// <param name="ITERATIONSperDAY"></param>
        /// <remarks></remarks>
        private void DoNetRadiation(ref double[] solarRadn, ref double cloudFr, ref double cva, int ITERATIONSperDAY)
        {
            double TSTEPS2RAD = MathUtilities.Divide(DEG2RAD * 360.0, Convert.ToDouble(ITERATIONSperDAY), 0);          // convert timestep of day to radians
            const double SOLARconst = 1360.0;     // W/M^2
            double solarDeclination = 0.3985 * Math.Sin(4.869 + clock.Today.DayOfYear * DOY2RAD + 0.03345 * Math.Sin(6.224 + clock.Today.DayOfYear * DOY2RAD));
            double cD = Math.Sqrt(1.0 - solarDeclination * solarDeclination);
            double[] m1 = new double[ITERATIONSperDAY + 1];
            double m1Tot = 0.0;
            for (int timestepNumber = 1; timestepNumber <= ITERATIONSperDAY; timestepNumber++)
            {
                m1[timestepNumber] = (solarDeclination * Math.Sin(weather.Latitude * DEG2RAD) + cD * Math.Cos(weather.Latitude * DEG2RAD) * Math.Cos(TSTEPS2RAD * (System.Convert.ToDouble(timestepNumber) - System.Convert.ToDouble(ITERATIONSperDAY) / 2.0))) * 24.0 / System.Convert.ToDouble(ITERATIONSperDAY);
                if (m1[timestepNumber] > 0.0)
                    m1Tot += m1[timestepNumber];
                else
                    m1[timestepNumber] = 0.0;
            }

            const double W2MJ = HR2MIN * MIN2SEC * J2MJ;      // convert W to MJ
            double psr = m1Tot * SOLARconst * W2MJ;   // potential solar radiation for the day (MJ/m^2)
            double fr = MathUtilities.Divide(Math.Max(weather.Radn, 0.1), psr, 0);               // ratio of potential to measured daily solar radiation (0-1)
            cloudFr = 2.33 - 3.33 * fr;    // fractional cloud cover (0-1)
            cloudFr = bound(cloudFr, 0.0, 1.0);

            for (int timestepNumber = 1; timestepNumber <= ITERATIONSperDAY; timestepNumber++)
                solarRadn[timestepNumber] = Math.Max(weather.Radn, 0.1) *
                                            MathUtilities.Divide(m1[timestepNumber], m1Tot, 0);

            // cva is vapour concentration of the air (g/m^3)
            cva = Math.Exp(31.3716 - 6014.79 / kelvinT(minAirTemp) - 0.00792495 * kelvinT(minAirTemp)) / kelvinT(minAirTemp);
        }

        /// <summary>Calculate the net radiation at the soil surface</summary>
        /// <param name="solarRadn"></param>
        /// <param name="cloudFr"></param>
        /// <param name="cva"></param>
        /// <returns>Net radiation (SW and LW) for timestep (MJ)</returns>
        /// <remarks></remarks>
        private double RadnNetInterpolate(double solarRadn, double cloudFr, double cva)
        {
            const double EMISSIVITYsurface = 0.96;    // Campbell Eqn. 12.1
            double w2MJ = gDt * J2MJ;      // convert W to MJ

            // Eqns 12.2 & 12.3
            double emissivityAtmos = (1 - 0.84 * cloudFr) * 0.58 * Math.Pow(cva, (1.0 / 7.0)) + 0.84 * cloudFr;
            // To calculate the longwave radiation out, we need to account for canopy and residue cover
            // Calculate a penetration constant. Here we estimate this using the Soilwat algorithm for calculating EOS from EO and the cover effects.
            double PenetrationConstant = MathUtilities.Divide(Math.Max(0.1, potSoilEvap),
                                                              Math.Max(0.1, potEvapotrans), 0);

            // Eqn 12.1 modified by cover.
            double lwRinSoil = longWaveRadn(emissivityAtmos, airTemp) * PenetrationConstant * w2MJ;

            double lwRoutSoil = longWaveRadn(EMISSIVITYsurface, soilTemp[SURFACEnode]) * PenetrationConstant * w2MJ; // _
                                                                                                                     // + longWaveRadn(emissivityAtmos, (gT_zb(SURFACEnode) + gAirT) * 0.5) * (1.0 - PenetrationConstant) * w2MJ

            // Ignore (mulch/canopy) temperature and heat balance
            double lwRnetSoil = lwRinSoil - lwRoutSoil;

            double swRin = solarRadn;
            double swRout = waterBalance.Salb * solarRadn;
            // Dim swRout As Double = (salb + (1.0 - salb) * (1.0 - sunAngleAdjust())) * solarRadn   'FIXME temp test
            double swRnetSoil = (swRin - swRout) * PenetrationConstant;
            return swRnetSoil + lwRnetSoil;
        }

        /// <summary></summary>
        /// <returns></returns>
        private double SunAngleAdjust()
        {
            double solarDeclination = 0.3985 * Math.Sin(4.869 + clock.Today.DayOfYear * DOY2RAD + 0.03345 * Math.Sin(6.224 + clock.Today.DayOfYear * DOY2RAD));
            double zenithAngle = Math.Abs(weather.Latitude - solarDeclination * RAD2DEG);
            double sunAngle = 90.0 - zenithAngle;
            double fr = sunAngle / 90.0;
            return bound(fr, 0.0, 1.0);
        }

        /// <summary>
        ///       Divides one number by another.  If the divisor is zero or overflow
        ///       would occur a specified default is returned.  If underflow would
        ///       occur, nought is returned.
        /// </summary>
        /// <param name="dividend">dividend - quantity to be divided</param>
        /// <param name="divisor">divisor</param>
        /// <param name="defaultValue">default value if overflow, underflow or divide by zero</param>
        /// <returns></returns>
        /// <remarks>
        ///  Definition
        ///     Returns (dividend / divisor) if the division can be done
        ///     without overflow or underflow.  If divisor is zero or
        ///     overflow would have occurred, default is returned.  If
        ///     underflow would have occurred, zero is returned.
        ///     '''
        /// Assumptions
        ///       largest/smallest real number is 1.0e+/-30
        /// </remarks>
        private double Divide(double dividend, double divisor, double defaultValue)
        {
            const double LARGEST = 1.0E+30;   // largest acceptable no. for quotient
            const double NOUGHT = 0.0;      // 0
            const double SMALLEST = 1.0E-30;    // smallest acceptable no. for quotient

            // + Local Variables
            double quotient = 0.0;              // quotient

            if ((RealsAreEqual(dividend, NOUGHT)))
                quotient = NOUGHT;
            else if ((RealsAreEqual(divisor, NOUGHT)))
                quotient = defaultValue;
            else if ((Math.Abs(divisor) < 1.0))
            {
                if ((Math.Abs(dividend) > Math.Abs(LARGEST * divisor)))
                    quotient = defaultValue;
                else
                    quotient = MathUtilities.Divide(dividend, divisor, 0);
            }
            else if ((Math.Abs(divisor) > 1.0))
            {
                if ((Math.Abs(dividend) < Math.Abs(SMALLEST * divisor)))
                    quotient = NOUGHT;
                else
                    quotient = MathUtilities.Divide(dividend, divisor, 0);
            }
            else
                quotient = MathUtilities.Divide(dividend, divisor, 0);

            return quotient;
        }

        /// <summary>
        /// Get the sum of all elements in an array between 'start' and 'end'
        /// </summary>
        /// <param name="array"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private double SumOfRange(double[] array, int start, int end)
        {
            return MathUtilities.Sum(array, start, end);
            /*
            double result = 0;
            for (int i = start; i <= end; i++)
                result += array[i];
            return result; */
        }

        /// <summary>
        ///     checks if a variable lies outside lower and upper bounds.
        ///     Reports an err if it does.
        /// </summary>
        /// <param name="VariableValue">value to be validated</param>
        /// <param name="Lower">lower limit of value</param>
        /// <param name="Upper">upper limit of value</param>
        /// <param name="VariableName">variable name to be validated</param>
        /// <remarks>
        ///  Definition
        ///     This subroutine will issue a warning message using the
        ///     name of "value", "vname", if "value" is greater than
        ///     ("upper" + 2 * error_margin("upper")) or if "value" is less than
        ///     ("lower" - 2 *error_margin("lower")).  If  "lower" is greater
        ///     than ("upper" + 2 * error_margin("upper")) , then a warning
        ///     message will be flagged to that effect.
        ///     '''
        /// Notes
        ///     reports err if value GT upper or value LT lower or lower GT upper
        /// </remarks>
        private void BoundCheck(double VariableValue, double Lower, double Upper, string VariableName)
        {
            const double MARGIN = 0.00001;          // margin for precision err of lower
            double LowerBound = Lower - MARGIN;       // calculate a margin for precision err of lower.
            double UpperBound = Upper + MARGIN;   // calculate a margin for precision err of upper.

            if ((VariableValue > UpperBound | VariableValue < LowerBound))
            {
                if ((LowerBound > UpperBound))
                    throw new Exception("Lower bound (" + Lower.ToString() + ") exceeds upper bound (" + Upper.ToString() + ") in bounds checking: Variable is not checked");
                else
                    throw new Exception(VariableName + " = " + VariableValue.ToString() + " is outside range of " + Lower.ToString() + " to " + Upper.ToString());
            }
            else
            {
            }

            return;
        }

        /// <summary>
        /// Check bounds of values in an array
        /// </summary>
        /// <param name="array">array to be checked</param>
        /// <param name="LowerBound">lower bound of values</param>
        /// <param name="UpperBound">upper bound of values</param>
        /// <param name="ArrayName">key string of array</param>
        /// <remarks>
        ///  Definition
        ///     Each of the "size" elements of "array" should be greater than or equal to
        ///     ("lower" - 2 *error_margin("lower")) and less than or equal to
        ///     ("upper" + 2 * error_margin("upper")).  A warning error using
        ///     the name of "array", "name", will be flagged for each element
        ///     of "array" that fails the above test.  If  "lower" is greater
        ///     than ("upper" + 2 * error_margin("upper")) , then a warning
        ///     message will be flagged to that effect "size" times.
        ///     '''
        ///  Assumptions
        ///     each element has same bounds.
        /// </remarks>
        private void BoundCheckArray(double[] array, double LowerBound, double UpperBound, string ArrayName)
        {
            if (array.Length >= 1)
            {
                for (int index = 0; index < array.Length; index++)
                    BoundCheck(array[index], LowerBound, UpperBound, ArrayName);
            }
            else
            {
            }
        }

        /// <summary>
        /// Tests if two real values are practically equal
        /// </summary>
        /// <param name="double1"></param>
        /// <param name="double2"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool RealsAreEqual(double double1, double double2)
        {
            double precision = Math.Min(double1, double2) * 0.0001;
            return (Math.Abs(double1 - double2) <= precision);
        }
        /// <summary>
        /// constrains a variable within bounds of lower and upper
        /// </summary>
        /// <param name="var">(INPUT) variable to be constrained</param>
        /// <param name="lower">(INPUT) lower limit of variable</param>
        /// <param name="upper">(INPUT) upper limit of variable</param>
        /// <returns>Constrained value</returns>
        /// <remarks>
        /// Returns "lower", if "var" is less than "lower".  Returns "upper" if "var" is greater than "upper".  Otherwise returns "var".
        /// A warning error is flagged if "lower" is greater than "upper".
        /// If the lower bound is > the upper bound, the variable remains unconstrained.
        /// </remarks>
        private double bound(double var, double lower, double upper)
        {
            double high = 0.0;                  // temporary variable constrained to upper limit of variable

            // check that lower & upper bounds are valid

            if ((lower > upper))
            {
                // bounds invalid, don't constrain variable
                throw new Exception("Lower bound (" + lower.ToString() + ") exceeds upper bound (" + upper.ToString() + ") in bounds checking: Variable (value=" + var.ToString() + ") is not constrained between bounds");
            }
            else
            {
                // bounds valid, now constrain variable
                high = Math.Min(var, upper);
                return Math.Max(high, lower);
            }
        }
        /// <summary>
        /// returns sum_of of products of arrays var1 and var2, up to level limit.
        /// each level of one array is multiplied by the corresponding level of the other.
        /// </summary>
        /// <param name="var1">(INPUT) first array for multiply</param>
        /// <param name="var2">(INPUT) 2nd array for multiply</param>
        /// <returns>Returns sum of  ("var1"(j) * "var2"(j))   for all j in  1 .. upperBound.</returns>
        /// <remarks></remarks>
        private double sum_products_real_array(double[] var1, double[] var2)
        {
            if ((var1.GetUpperBound(0) == var2.GetUpperBound(0)))
            {
                double tot = 0.0;
                for (int level = 0; level <= var1.GetUpperBound(0); level++)
                    tot = tot + var1[level] * var2[level];

                return tot;
            }
            else
                throw new Exception("sum_products_real_array must have same size arrays. Array1 size =" + var1.GetLength(0).ToString() + ". Array2 sixe =" + var2.GetLength(0).ToString());
        }

        /// <summary>
        /// Multiplies array by specified multiplier
        /// </summary>
        /// <param name="array">(INPUT/OUTPUT)</param>
        /// <param name="multiplier"></param>
        /// <remarks></remarks>
        private void multiplyArray(double[] array, double multiplier)
        {
            for (int level = 0; level <= array.GetUpperBound(0); level++)
                array[level] = array[level] * multiplier;
        }

        /// <summary>
        ///  adds or subtracts specified days to/from day of year number
        /// </summary>
        /// <param name="iyr">(INPUT) year</param>
        /// <param name="doy">(INPUT) day of year number</param>
        /// <param name="ndays">(INPUT) number of days to adjust by</param>
        /// <returns>New day of year</returns>
        /// <remarks> Returns the day of year for the day "ndays" after the day specified by the day of year, "doy", in the year, "iyr".
        ///  "ndays" may well be negative.
        /// </remarks>
        private int offsetDayOfYear(int iyr, int doy, int ndays)
        {
            DateTime newdate = new DateTime(iyr, 1, 1).AddDays(doy - 1 + ndays);
            return newdate.DayOfYear;
        }
    }
}
