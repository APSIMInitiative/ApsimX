﻿using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;

namespace Models.Soils.SoilTemp
{

    // Note: typing three consecutive comment characters (e.g.///) above a sub or function will generate xml documentation.

    /// <summary>
    /// Since temperature changes rapidly near the soil surface and very little at depth, the best simulation will
    /// be obtained with short elements (shallow layers) near the soil surface and longer ones deeper in the soil.
    /// The element lengths should go in a geometric progression. Ten to twelve nodes are probably sufficient for
    /// short term simulations (daily or weekly). Fifteen nodes would probably be sufficient for annual cycle simulation
    /// where a deeper grid is needed.
    /// p36, Campbell, G.S. (1985) "Soil physics with BASIC: Transport models for soil-plant systems" (Amsterdam, Elsevier)
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    public class SoilTemperature : Model, ISoilTemperature
    {
        [Link]
        private IWeather weather = null;

        [Link]
        private IClock clock = null;

        [Link]
        private MicroClimate microClimate = null;

        [Link]
        private Physical physical = null;

        [Link]
        ISoilWater waterBalance = null;

        // ------------------------------------------------------------------------------------------------------------
        // -----------------------------------------------IMPORTANT NOTE-----------------------------------------------
        // Due to FORTRAN's 'flexibility' with arrays there have been a few modifications to array sizes in this
        // version of SoilTemp. All arrays here are forceably 0-based, so to deal with the fact that this module
        // contains both 0- and 1-based arrays all arrays have been increased in size by 1. All indexing will not
        // change from FORTRAN code, those arrays that are 1-based will simply not use their first (0th) element.
        //
        // This is actually rather convenient. In these arrays, the element 0 (AIRnode) refers to the air,
        // the element 1 (SURFACEnode) refers to the soil surface, and elements 2 (TOPSOILnode) .. numNodes + 1
        // refer to nodes within the soil.
        //
        // ------------------------------------------------------------------------------------------------------------
        #region constants
        private bool doInit1Stuff = false;      // NEW
                                                // Two representations are distinguished for the profile.
                                                // a) Soil layers, each with a top and bottom boundary and a depth. Index range of 1..NumLayer
                                                // b) Temperature nodes, each node being at the centre of each layer and additionally a node below
                                                // the bottom layer and a node above the top layer (in the atmosphere). Index range of 0..NumLayer + 1,
                                                // giving 0 for atmosphere,

        const double LAMBDA = 2465000.0;      // latent heat of vapourisation for water (J/kg); 1 mm evap/day = 1 kg/m^2 day requires 2.45*10^6 J/m^2
        const double STEFAN_BOLTZMANNconst = 0.0000000567;   // defines the power per unit area emitted by a black body as a function of its thermodynamic temperature (W/m^2/K^4).
        const double DAYSinYear = 365.25;    // no of days in one year
        const double DEG2RAD = Math.PI / 180.0;
        const double RAD2DEG = 180.0 / Math.PI;
        const double DOY2RAD = DEG2RAD * 360.0 / DAYSinYear;          // convert day of year to radians
        const double MIN2SEC = 60.0;          // 60 seconds in a minute - conversion minutes to seconds
        const double HR2MIN = 60.0;           // 60 minutes in an hour - conversion hours to minutes
        const double SEC2HR = 1.0 / (HR2MIN * MIN2SEC);  // conversion seconds to hours
        const double DAYhrs = 24.0;           // hours in a day
        const double DAYmins = DAYhrs * HR2MIN;           // minutes in a day
        const double DAYsecs = DAYmins * MIN2SEC;         // seconds in a day
        const double M2MM = 1000.0;           // 1000 mm in a metre -  conversion Metre to millimetre
        const double MM2M = 1 / M2MM;           // 1000 mm in a metre -  conversion millimetre to Metre
        const double PA2HPA = 1.0 / 100.0;        // conversion of air pressure pascals to hectopascals
        const double MJ2J = 1000000.0;            // convert MJ to J
        const double J2MJ = 1.0 / MJ2J;            // convert J to MJ
        const int AIRnode = 0;                // index of atomspheric node
        const int SURFACEnode = 1;            // index of surface node
        const int TOPSOILnode = 2;            // index of first soil layer node

        private double gDt = 0.0;             // Internal timestep in seconds.
        private double timeOfDaySecs = 0;     // Time of day in seconds from midnight.
        private int numNodes = 0;             // number of nodes
        private int numLayers = 0;            // number of soil layers in profile
        private double[] thermCondPar1;       // soil solids thermal conductivity parameter
        private double[] thermCondPar2;       // soil solids thermal conductivity parameter
        private double[] thermCondPar3;       // soil solids thermal conductivity parameter
        private double[] thermCondPar4;       // soil solids thermal conductivity parameter
        private double[] volSpecHeatSoil;     // Joules/m^3/K
        private double[] soilTemp;            // soil temperature
        private double[] morningSoilTemp;     // soil temperature
        private double[] heatStorage;         // CP; heat storage between nodes - index is same as upper node (J/s/K or W/K)
        private double[] thermalConductance;  // K; conductance of element between nodes - index is same as upper node
        private double[] thermalConductivity; // thermal conductivity (W/m^2/K)
        private int boundaryLayerConductanceIterations = 0;  // Number of iterations to calculate atmosphere boundary layer conductance
        private double _boundaryLayerConductance = 0.0;      // Average daily atmosphere boundary layer conductance
        private double[] tempNew;             // soil temperature at the end of this iteration (oC)
        private double[] depth;               // node depths - get from water module, metres
        private double airTemp = 0.0;         // Air temperature (oC)
        private double maxAirTemp = 0.0;      // Max daily Air temperature (oC)
        private double minAirTemp = 0.0;      // Min daily Air temperature (oC)
        private double maxTempYesterday = 0.0;
        private double minTempYesterday = 0.0;
        private double[] soilWater;           // volumetric water content (cc water / cc soil)
        private double[] minSoilTemp;         // minimum soil temperature (oC)
        private double[] maxSoilTemp;         // maximum soil temperature (oC)
        private double tempStepSec = 0.0;     // this will be set to timestep * 60 at the beginning of each calc. (seconds)
        private double[] bulkDensity;         // bd (soil bulk density) is name of the APSIM var for bulk density so set bulkDensity = bd later (g/cc)
        private double[] thickness;           // APSIM soil layer depths as thickness of layers (mm)
        private double instrumentHeight = 0.0;// (m) height of instruments above soil surface
        private double airPressure = 0.0;     // (hPa) Daily air pressure
        private double windSpeed = 0.0;       // (km) daily wind run
        private double potSoilEvap = 0.0;     // (mm) pot sevap after modification for green cover residue wt
        private double potEvapotrans = 0.0;   // (mm) pot evapotranspitation
        private double actualSoilEvap = 0.0;  // actual soil evaporation (mm)
        private double netRadiation = 0.0;    // Net radiation per internal timestep (MJ)
        private double canopyHeight = 0.0;    // (m) height of canopy above ground
        private double soilRoughnessHeight = 0.0;    // (mm) height of soil roughness
        private double[] clay;                // Proportion of clay in each layer of profile (0-1)
        #endregion

        //[Input()]
        private const int timestep = 1440;     // timestep in minutes

        // this was not an input in old apsim. // [Input]                                //FIXME - optional input
        private double maxTempTime = 0.0;      // Time of maximum temperature in hours

        // <Input()> _
        // Private cover_tot As Double = 0.0    ' (0-1) total surface cover
        // <Input()> _
        private double instrumHeight = 0.0;    // (m) height of instruments above ground

        private double altitude = 0.0;         // (m) altitude at site

        private double nu = 0.6;                  // forward/backward differencing coefficient (0-1).
                                                  // A weighting factor which may range from 0 to 1. If nu=0, the flux is determined by the temperature difference
                                                  // at the beginning of the time step. The numerical procedure which results from this choice is called a forward
                                                  // difference of explicit method. If nu=0.5, the average of the old and new temperatures is used to compute heat flux.
                                                  // This is called a time-centered or Crank-Nicholson scheme.
                                                  // The equation for computing T(j+1) is implicit for this choice of nu (and any other except nu=0) since T(j+1) depends
                                                  // on the values of the new temperatures at the nodes i+1 and i-1. Most heat flow models use either nu=0 or nu=0.5.
                                                  // The best value for nu is determined by consideration of numerical stability and accuracy.
                                                  // The explicit scheme with nu=0 predicts more heat transfer between nodes than would actually occur, and can therefore
                                                  // become unstable if time steps are too large. Stable numerical solutions are only obtained when (Simonson, 1975)
                                                  // deltaT < CH*(deltaZ)^2 / 2*lambda.
                                                  // When nu < 0.5, stable solutions to the heat flow problem will always be obtained, but if nu is too small, the solutions
                                                  // may oscillate. The reason for this is that the simulated heat transfer between nodes overshoots. On the next time step
                                                  // the excess heat must be transferrec back, so the predicted temperature at that node oscillates. On the other hand, if nu
                                                  // is too large, the temperature difference will be too small and not enough heat will be transferred. Simulated temperatures
                                                  // will never oscillate under these conditions, but the simulation will understimate heat flux. The best accuracy is obtained
                                                  // with nu around 0.4, while best stability is at nu=1. A typical compromise is nu=0.6.


        private double volSpecHeatClay = 2.39e6;  // [Joules*m-3*K-1]

        private double volSpecHeatOM = 5e6;       // [Joules*m-3*K-1]

        private double volSpecHeatWater = 4.18e6; // [Joules*m-3*K-1]

        private double maxTTimeDefault = 14;

        private double[] aveSoilTemp;  // FIXME - optional. Allow setting from set in manager or get from input //init to average soil temperature

        private string boundarLayerConductanceSource = "calc";

        private const double boundaryLayerConductance = 20;

        private const double BoundaryLayerConductanceIterations = 1;    // maximum number of iterations to calculate atmosphere boundary layer conductance

        private string netRadiationSource = "calc";

        // from met
        private double defaultWindSpeed = 3;      // default wind speed (m/s)

        private const double defaultAltitude = 18;    // default altitude (m)

        private const double defaultInstrumentHeight = 1.2;  // default instrument height (m)

        private const double bareSoilHeight = 57;        // roughness element height of bare soil (mm)

        #region outputs

        /// <summary>
        /// Temperature at end of last time-step within a day - midnight
        /// </summary>
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

        /// <summary>
        /// Temperature at end of last time-step within a day - midnight
        /// </summary>
        [Units("oC")]
        public double FinalSoilSurfaceTemp
        {
            get
            {
                return soilTemp[SURFACEnode];
            }
        }

        /// <summary>
        /// Mandatory for ISoilTemperature interface. For now, just return average daily values
        /// </summary>
        public double[] Value
        {
            get
            {
                return AverageSoilTemp;
            }
        }

        /// <summary>
        /// Temperature averaged over all time-steps within a day
        /// </summary>
        [Units("oC")]
        public double[] AverageSoilTemp
        {
            get
            {
                // if called during init1 this will return an array of length 100 will all elements as '0'
                double[] result = new double[numLayers];
                Array.ConstrainedCopy(aveSoilTemp, TOPSOILnode, result, 0, numLayers);
                return result;
            }
        }

        /// <summary>
        /// Temperature averaged over all time-steps within a day
        /// </summary>
        [Units("oC")]
        public double AverageSoilSurfaceTemp
        {
            get
            {
                return aveSoilTemp[SURFACEnode];
            }
        }

        /// <summary>
        ///
        /// </summary>
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

        /// <summary>
        ///
        /// </summary>
        [Units("oC")]
        public double minSoilSurfaceTemp
        {
            get
            {
                return minSoilTemp[SURFACEnode];
            }
        }

        /// <summary>
        ///
        /// </summary>
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

        /// <summary>
        ///
        /// </summary>
        [Units("oC")]
        public double MaxSoilSurfaceTemp
        {
            get
            {
                return maxSoilTemp[SURFACEnode];
            }
        }

        /// <summary>
        ///
        /// </summary>
        [Units("J/sec/m/K")]
        public double BoundaryLayerConductance
        {
            get
            {
                return _boundaryLayerConductance;
            }
        }

        /// <summary>
        ///
        /// </summary>
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

        /// <summary>
        ///
        /// </summary>
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

        /// <summary>
        ///
        /// </summary>
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

        /// <summary>
        ///
        /// </summary>
        [Units("oC")]
        public double[] Thr_profile
        {
            get
            {
                double[] result = new double[numNodes + 1 + 1];
                morningSoilTemp.CopyTo(result, 0);
                return result;
            }
        }

        #endregion

        #region events

        [EventSubscribe("DoSoilTemperature")]
        private void OnProcess(object sender, EventArgs e)
        {
            maxTempTime = maxTTimeDefault;
            BoundCheck(maxTempTime, 0.0, DAYhrs, "maxTempTime");

            GetOtherVariables();       // FIXME - note: Need to set yesterday's MaxTg and MinTg to today's at initialisation

            if (doInit1Stuff)
            {
                // set t and tnew values to TAve. soil_temp is currently not used
                CalcSoilTemp(ref soilTemp);
                soilTemp[AIRnode] = (maxAirTemp + minAirTemp) * 0.5;
                soilTemp[SURFACEnode] = SurfaceTemperatureInit();
                soilTemp[numNodes + 1] = weather.Tav;
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
            else
            {
            }

            doProcess();
        } // OnProcess



        /// <summary>
        /// Initialise soiltemp module
        /// </summary>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)            // JNGH - changed this from Init1.
        {
            doInit1Stuff = true;
            getIniVariables();
            getProfileVariables();
            readParam();
            doInit1Stuff = true;
        } // OnInit2


        #endregion

        /// <summary>
        /// initialise global variables to initial values
        /// </summary>
        /// <remarks></remarks>
        private void getIniVariables()
        {
            BoundCheck(weather.Tav, -30.0, 40.0, "tav (oC)");
            // 'gTAve = tav

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
        } // getIniVariables

        /// <summary>
        /// Set global variables to new soil profile state
        /// </summary>
        /// <remarks></remarks>
        private void getProfileVariables()
        {
            const double CONSTANT_TEMPdepth = 10.0;    // Metres. Depth to constant temperature zone
                                                       // re-dimension dlayer, bd. These will now be treated as '1-based' so 0th element will not be used
            numLayers = physical.Thickness.Length;
            numNodes = numLayers + 1;

            thickness = new double[numLayers + 1 + 1];     // Dlayer dimensioned for layers 1 to gNumlayers + 1 extra for zone below bottom layer
            physical.Thickness.CopyTo(thickness, 1);
            BoundCheckArray(thickness, 0.0, 1000.0, "thickness");

            // mapping of layers to nodes -
            // layer - air surface 1 2 ... NumLayers NumLayers+1
            // node  - 0   1       2 3 ... Nz        Nz+1
            // thus the node 1 is at the soil surface and Nz = NumLayers + 1.

            // add enough to make last node at 9-10 meters - should always be enough to assume constant temperature
            double BelowProfileDepth = Math.Max(CONSTANT_TEMPdepth * M2MM - SumOfRange(thickness, 1, numLayers), 1.0 * M2MM);    // Metres. Depth added below profile to take last node to constant temperature zone
            thickness[numLayers + 1] = BelowProfileDepth * 2.0;       // double depth so that node at mid-point is at the ConstantTempDepth
            var oldDepth = depth;
            depth = new double[numNodes + 1 + 1];


            // Set the node depths to approx middle of soil layers
            if (oldDepth != null)
                Array.Copy(oldDepth, depth, Math.Min(numNodes + 1 + 1, oldDepth.Length));      // Z_zb dimensioned for nodes 0 to Nz + 1 extra for zone below bottom layer
            depth[AIRnode] = 0.0D;
            depth[SURFACEnode] = 0.0D;
            depth[TOPSOILnode] = 0.5 * thickness[1] * MM2M;
            for (int node = TOPSOILnode; node <= numNodes; node++)
                depth[node + 1] = (SumOfRange(thickness, 1, node - 1) + 0.5 * thickness[node]) * MM2M;

            // BD
            BoundCheck(physical.BD.Length, numLayers, numLayers, "bd layers");
            var oldBulkDensity = bulkDensity;
            bulkDensity = new double[numLayers + 1 + 1];
            if (oldBulkDensity != null)
                Array.Copy(oldBulkDensity, bulkDensity, Math.Min(numLayers + 1 + 1, oldBulkDensity.Length));     // Rhob dimensioned for layers 1 to gNumlayers + 1 extra for zone below bottom layer
            physical.BD.CopyTo(bulkDensity, 1);
            BoundCheckArray(bulkDensity, 0.0, 2.65, "bulkDensity");
            // if bd (rhob) has more or less elements than the number of layers, throw exception

            bulkDensity[numNodes] = bulkDensity[numLayers];
            // Debug test: multiplyArray(gRhob, 2.0)

            // SW
            BoundCheck(waterBalance.SWmm.Length, numLayers, numLayers, "sw layers");
            var oldSoilWater = soilWater;
            soilWater = new double[numLayers + 1 + 1];
            if (oldSoilWater != null)
                Array.Copy(oldSoilWater, soilWater, Math.Min(numLayers + 1 + 1, oldSoilWater.Length));     // SW dimensioned for layers 1 to gNumlayers + 1 extra for zone below bottom layer
            for (int layer = 1; layer <= numLayers; layer++)
                soilWater[layer] = MathUtilities.Divide(waterBalance.SWmm[layer - 1], thickness[layer], 0);
            soilWater[numNodes] = soilWater[numLayers];
            var oldMaxSoilTemp = maxSoilTemp;
            maxSoilTemp = new double[numLayers + 1 + 1];
            if (oldMaxSoilTemp != null)
                Array.Copy(oldMaxSoilTemp, maxSoilTemp, Math.Min(numLayers + 1 + 1, oldMaxSoilTemp.Length));     // MaxTsoil dimensioned for layers 1 to gNumlayers + 1 extra for zone below bottom layer
            var oldMinSoilTemp = minSoilTemp;
            minSoilTemp = new double[numLayers + 1 + 1];
            if (oldMinSoilTemp != null)
                Array.Copy(oldMinSoilTemp, minSoilTemp, Math.Min(numLayers + 1 + 1, oldMinSoilTemp.Length));     // MinTsoil dimensioned for layers 1 to gNumlayers + 1 extra for zone below bottom layer
            var oldGAveTsoil = aveSoilTemp;
            aveSoilTemp = new double[numLayers + 1 + 1];
            if (oldGAveTsoil != null)
                Array.Copy(oldGAveTsoil, aveSoilTemp, Math.Min(numLayers + 1 + 1, oldGAveTsoil.Length));     // AveTsoil dimensioned for layers 1 to gNumlayers + 1 extra for zone below bottom layer
            var oldVolSpecHeatSoil = volSpecHeatSoil;
            volSpecHeatSoil = new double[numNodes + 1];
            if (oldVolSpecHeatSoil != null)
                Array.Copy(oldVolSpecHeatSoil, volSpecHeatSoil, Math.Min(numNodes + 1, oldVolSpecHeatSoil.Length));        // HeatStore dimensioned for layers 1 to gNumlayers + 1 extra for zone below bottom layer
            var oldSoilTemp = soilTemp;
            soilTemp = new double[numNodes + 1 + 1];
            if (oldSoilTemp != null)
                Array.Copy(oldSoilTemp, soilTemp, Math.Min(numNodes + 1 + 1, oldSoilTemp.Length));               // T_zb dimensioned for nodes 0 to Nz + 1 extra for zone below bottom layer
            var oldMorningSoilTemp = morningSoilTemp;
            morningSoilTemp = new double[numNodes + 1 + 1];
            if (oldMorningSoilTemp != null)
                Array.Copy(oldMorningSoilTemp, morningSoilTemp, Math.Min(numNodes + 1 + 1, oldMorningSoilTemp.Length));               // Thr_zb dimensioned for nodes 0 to Nz + 1 extra for zone below bottom layer
            var oldTempNew = tempNew;
            tempNew = new double[numNodes + 1 + 1];
            if (oldTempNew != null)
                Array.Copy(oldTempNew, tempNew, Math.Min(numNodes + 1 + 1, oldTempNew.Length));            // TNew_zb dimensioned for nodes 0 to Nz + 1 extra for zone below bottom layer
            var oldTermalConductivity = thermalConductivity;
            thermalConductivity = new double[numNodes + 1];
            if (oldTermalConductivity != null)
                Array.Copy(oldTermalConductivity, thermalConductivity, Math.Min(numNodes + 1, oldTermalConductivity.Length));   // ThermCond_zb dimensioned for nodes 0 to Nz + 1 extra for zone below bottom layer
            var oldHeatStorage = heatStorage;
            heatStorage = new double[numNodes + 1];
            if (oldHeatStorage != null)
                Array.Copy(oldHeatStorage, heatStorage, Math.Min(numNodes + 1, oldHeatStorage.Length));     // CP; heat storage between nodes - index is same as upper node
            var oldThermalConductance = thermalConductance;
            thermalConductance = new double[numNodes + 1 + 1];
            if (oldThermalConductance != null)
                Array.Copy(oldThermalConductance, thermalConductance, Math.Min(numNodes + 1 + 1, oldThermalConductance.Length)); // K; conductance between nodes - index is same as upper node
        }

        /// <summary>
        /// Set global variables with module parameter values and check validity
        /// </summary>
        /// <remarks></remarks>
        private void readParam()
        {
            //
            // clay//
            // if clay has more or less elements than the number of layers, throw exception
            BoundCheck(physical.ParticleSizeClay.Length, numLayers, numLayers, "clay layers");
            BoundCheckArray(physical.ParticleSizeClay, 0.01, 100.0, "clay");

            double[] clayFrac = new double[numLayers + 1];
            for (int layer = 0; layer <= numLayers - 1; layer++)
                clayFrac[layer] = physical.ParticleSizeClay[layer] / 100.0;// convert from % units ased as user input to proportion  // layer
            var oldGClay = clay;
            clay = new double[numLayers + 1 + 1];
            if (oldGClay != null)
                Array.Copy(oldGClay, clay, Math.Min(numLayers + 1 + 1, oldGClay.Length));     // Clay dimensioned for layers 1 to gNumlayers + 1 extra for zone below bottom layer
            clayFrac.CopyTo(clay, 1);
            clay[numNodes] = clay[numLayers];   // Set zone below bottom layer to the same as bottom layer
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
            switch (boundarLayerConductanceSource)
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
            switch (netRadiationSource)
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

        /// <summary>
        /// Calculate the coefficients for thermal conductivity equation (Campbell 4.20) for a typical low-quartz, mineral soil.
        /// </summary>
        /// <remarks></remarks>
        private void doThermalConductivityCoeffs()
        {
            var oldGC1 = thermCondPar1;
            thermCondPar1 = new double[numNodes + 1];
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
                // first coefficient C1 - For many mineral soils, the quartz fractioncan be taken as nil, and the equation can
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

        /// <summary>
        /// Update global variables with external states and check validity of values.
        /// </summary>
        /// <remarks></remarks>
        private void GetOtherVariables()
        {
            BoundCheck(weather.MaxT, weather.MinT, 100.0, "maxt");
            BoundCheck(weather.MinT, -100.0, weather.MaxT, "mint");
            maxAirTemp = weather.MaxT;
            minAirTemp = weather.MinT;
            tempStepSec = System.Convert.ToDouble(timestep) * MIN2SEC;
            waterBalance.SW.CopyTo(soilWater, 1);
            soilWater[numNodes] = soilWater[numLayers];
            // Debug(test): multiplyArray(gSW, 0.1)

            BoundCheck(waterBalance.Eo, -30.0, 40.0, "eo");
            potEvapotrans = waterBalance.Eo;

            BoundCheck(waterBalance.Eos, -30.0, 40.0, "eos");
            potSoilEvap = waterBalance.Eos;

            BoundCheck(waterBalance.Es, -30.0, 40.0, "es");
            actualSoilEvap = waterBalance.Es;
            // BoundCheck(cover_tot, 0.0, 1.0, "cover_tot")

            //if ((weather.Wind > 0.0))
            //    gWindSpeed = weather.Wind * KM2M / (DAY2HR * HR2SEC);
            //else
            windSpeed = defaultWindSpeed;
            BoundCheck(windSpeed, 0.0, 1000.0, "wind");

            canopyHeight = Math.Max(microClimate.CanopyHeight, soilRoughnessHeight) * MM2M;
            BoundCheck(canopyHeight, 0.0, 20.0, "Height");


            // Vals HACK. Should be recalculating wind profile.
            instrumentHeight = Math.Max(instrumentHeight, canopyHeight + 0.5);

        }


        /// <summary>
        /// Perform actions for current day
        /// </summary>
        private void doProcess()
        {
            const int ITERATIONSperDAY = 48;     // number of iterations in a day

            double cva = 0.0;
            double cloudFr = 0.0;
            double[] solarRadn = new double[49];   // Total incoming short wave solar radiation per timestep
            DoNetRadiation(ref solarRadn, ref cloudFr, ref cva, ITERATIONSperDAY);

            // zero the temperature profiles
            MathUtilities.Zero(minSoilTemp);
            MathUtilities.Zero(maxSoilTemp);
            MathUtilities.Zero(aveSoilTemp);
            _boundaryLayerConductance = 0.0;

            double RnTot = 0.0;
            // calc dt
            gDt = Math.Round(tempStepSec / System.Convert.ToDouble(ITERATIONSperDAY));

            // These two call used to be inside the timestep loop. I've taken them outside,
            // as the results do not appear to vary over the course of the day.
            // The results would vary if soil water content were to vary, so if future versions
            // to more communication within subday time steps, these may need to be moved
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

        /// <summary>
        /// Calculate the volumetric specific heat (volumetric heat capicity Cv) of the soil layer
        /// to Campbell, G.S. (1985) "Soil physics with BASIC: Transport
        /// models for soil-plant systems" (Amsterdam, Elsevier)
        /// RETURNS volSpecHeatSoil()  [Joules*m-3*K-1]
        /// </summary>
        private void doVolumetricSpecificHeat()
        {
            const double SPECIFICbd = 2.65;   // (g/cc) specific bulk density
            double[] volSpecHeatSoil = new double[numLayers + 1];

            for (int layer = 1; layer <= numLayers; layer++)
            {
                double solidity = bulkDensity[layer] / SPECIFICbd;

                // the Campbell version
                // heatStore[i] = (vol_spec_heat_clay * (1-porosity) + vol_spec_heat_water * SWg[i]) * (zb_z[i+1]-zb_z[i-1])/(2*real(dt));
                volSpecHeatSoil[layer] = volSpecHeatClay * solidity
                                       + volSpecHeatWater * soilWater[layer];
            }
            // mapLayer2Node(volSpecHeatSoil, gVolSpecHeatSoil)
            volSpecHeatSoil.CopyTo(this.volSpecHeatSoil, 1);     // map volumetric heat capicity (Cv) from layers to nodes (node 2 in centre of layer 1)
            this.volSpecHeatSoil[1] = volSpecHeatSoil[1];        // assume surface node Cv is same as top layer Cv
        }

        /// <summary>
        /// Calculate the thermal conductivity of the soil layer following,
        /// to Campbell, G.S. (1985) "Soil physics with BASIC: Transport
        /// models for soil-plant systems" (Amsterdam, Elsevier)
        /// Equation 4.20 where Lambda = A + B*Theta - (A-D)*exp[-(C*theta)^E]
        /// Lambda is the thermal conductivity, theta is volumetric water content and A, B, C, D, E are coefficients.
        /// When theta = 0, lambda = D. At saturation, the last term becomes zero and Lambda = A + B*theta.
        /// The constant E can be assigned a value of 4. The constant C determines the water content where thermal
        /// conductivity begins to increase rapidly and is highly correlated with clay content.
        /// Here C1=A, C2=B, SW=theta, C3=C, C4=D, 4=E.
        /// RETURNS gThermConductivity_zb() (W/m2/K)
        /// </summary>
        private void doThermConductivity()
        {
            double temp = 0;

            // no change needed from Campbell to my version
            // Do thermal conductivity for soil layers
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
        /// <summary>
        /// Numerical solution of the differential equations. Solves the
        /// tri_diagonal matrix using the Thomas algorithm, Thomas, L.H. (1946)
        /// "Elliptic problems in linear difference equations over a network"
        /// Watson Sci Comput. Lab. Report., (Columbia University, New York)"
        /// RETURNS newTemps()
        /// </summary>
        /// <remarks>John Hargreaves' version from Campbell Program 4.1</remarks>
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

        /// <summary>
        /// Numerical solution of the differential equations. Solves the
        /// tri_diagonal matrix using the Thomas algorithm, Thomas, L.H. (1946)
        /// "Elliptic problems in linear difference equations over a network"
        /// Watson Sci Comput. Lab. Report., (Columbia University, New York)"
        /// RETURNS newTemps()
        /// </summary>
        /// <remarks>Val Snow's original version</remarks>
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

        /// <summary>
        ///  Interpolate air temperature
        /// </summary>
        /// <param name="timeHours">time of day that air temperature is required</param>
        /// <returns>Interpolated air temperature for specified time of day (oC)</returns>
        /// <remarks>
        /// Notes:
        ///  Between midinight and MinT_time just a linear interpolation between
        ///  yesterday's midnight temperature and today's MinTg. For the rest of
        ///  the day use a sin function.
        /// Note: This can result in the Midnight temperature being lower than the following minimum.
        /// </remarks>
        private double InterpTemp(double timeHours)
        {
            // using global vars:
            // double MaxT_time
            // double MinTg
            // double MaxTg
            // double MinT_yesterday
            // double MaxT_yesterday

            double time = timeHours / DAYhrs;           // Current time of day as a fraction of a day
            double maxT_time = maxTempTime / DAYhrs;     // Time of maximum temperature as a fraction of a day
            double minT_time = maxT_time - 0.5;       // Time of minimum temperature as a fraction of a day

            if (time < minT_time)
            {
                // Current time of day is between midnight and time of minimum temperature
                double midnightT = Math.Sin((0.0D + 0.25 - maxT_time) * 2.0D * Math.PI)
                                        * (maxTempYesterday - minTempYesterday) / 2.0D
                                        + (maxTempYesterday + minTempYesterday) / 2.0D;
                double tScale = (minT_time - time) / minT_time;

                // set bounds for t_scale (0 <= tScale <= 1)
                if (tScale > 1.0D)
                    tScale = 1.0D;
                else if (tScale < 0)
                    tScale = 0;
                else
                {
                }

                double CurrentTemp = minAirTemp + tScale * (midnightT - minAirTemp);
                return CurrentTemp;
            }
            else
            {
                // Current time of day is at time of minimum temperature or after it up to midnight.
                double CurrentTemp = Math.Sin((time + 0.25 - maxT_time) * 2.0D * Math.PI)
                                          * (maxAirTemp - minAirTemp) / 2.0D
                                          + (maxAirTemp + minAirTemp) / 2.0D;
                return CurrentTemp;
            }
        }

        /// <summary>
        /// Determine min, max, and average soil temperature from the
        /// half-hourly iterations.
        /// RETURNS gAveTsoil(); gMaxTsoil(); gMinTsoil()
        /// </summary>
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
            else
            {
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

        /// <summary>
        ///     calculate the density of air (kg/m3) at a given temperature and pressure
        /// </summary>
        /// <param name="temperature">temperature (oC)</param>
        /// <param name="AirPressure">air pressure (hPa)</param>
        /// <returns>density of air</returns>
        /// <remarks></remarks>
        private double RhoA(double temperature, double AirPressure)
        {
            const double MWair = 0.02897;     // molecular weight air (kg/mol)
            const double RGAS = 8.3143;       // universal gas constant (J/mol/K)
            const double HPA2PA = 100.0;      // hectoPascals to Pascals

            return Divide(MWair * AirPressure * HPA2PA
                         , kelvinT(temperature) * RGAS
                         , 0.0);
        }

        /// <summary>
        ///     Calculate atmospheric boundary layer conductance.
        ///     From Program 12.2, p140, Campbell, Soil Physics with Basic.
        /// </summary>
        /// <returns>thermal conductivity of surface layer (W/m2/K)</returns>
        /// <remarks> During first stage drying, evaporation prevents the surface from becoming hot,
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

            // Since the boundary layer conductance is a function of the heat flux density, an iterative metnod must be used to find the boundary layer conductance.
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
                // is unstable and mixing is supressed.

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

        /// <summary>
        ///     Calculate boundary layer conductance.
        ///     From Program 12.2, p140, Campbell, Soil Physics with Basic.
        /// </summary>
        /// <returns>thermal conductivity  (W/m2/K)</returns>
        /// <remarks></remarks>
        private double boundaryLayerConductanceConst()
        {

            // canopy_height, instrum_ht = 1.2m, AirPressure = 1010
            // therm_cond(0) = 20.0 ' boundary layer conductance W m-2 K-1

            return boundaryLayerConductance; // (W/m2/K)
        }

        /// <summary>
        /// Convert deg Celcius to deg Kelvin
        /// </summary>
        /// <param name="celciusT">(INPUT) Temperature in deg Celcius</param>
        /// <returns>Temperature in deg Kelvin</returns>
        /// <remarks></remarks>
        private double kelvinT(double celciusT)
        {
            const double ZEROTkelvin = 273.18;   // Absolute Temperature (oK)
            return celciusT + ZEROTkelvin; // (deg K)
        }
        private double longWaveRadn(double emissivity, double tDegC)
        {
            return STEFAN_BOLTZMANNconst * emissivity * Math.Pow(kelvinT(tDegC), 4);
        }

        /// <summary>
        ///  Purpose
        ///           Calculates average soil temperature at the centre of each layer
        ///           based on the soil temperature model of EPIC (Williams et al 1984)
        /// </summary>
        /// <param name="soilTempIO">(OUTPUT) temperature of each layer in profile</param>
        /// <remarks></remarks>
        private void CalcSoilTemp(ref double[] soilTempIO)
        {
            const double SUMMER_SOLSTICE_NTH = 173.0;        // day of year of nth'rn summer solstice
            const double TEMPERATURE_DELAY = 27.0;    // delay from solstice to warmest day (days)
            const double HOTTEST_DAY_NTH = SUMMER_SOLSTICE_NTH + TEMPERATURE_DELAY;         // warmest day of year of nth hemisphere
            const double SUMMER_SOLSTICE_STH = SUMMER_SOLSTICE_NTH + DAYSinYear / 2.0;     // day of year of sthrn summer solstice
            const double HOTTEST_DAY_STH = SUMMER_SOLSTICE_STH + TEMPERATURE_DELAY; // warmest day of year of sth hemisphere
            const double ANG = DOY2RAD; // length of one day in radians factor to convert day of year to radian fraction of year

            double[] soilTemp = new double[numNodes + 1 + 1];
            Array.ConstrainedCopy(soilTempIO, SURFACEnode, soilTemp, 0, numNodes);

            // Get a factor to calculate "normal" soil temperature from the
            // day of g_year assumed to have the warmest average soil temperature
            // of the g_year.  The normal soil temperature varies as a cosine
            // function of alx.  This is the number of radians (time) of a
            // g_year today is from the warmest soil temp.

            double alx = 0.0;                // time in radians of year from hottest
                                             // instance to current day of year as a
                                             // radian fraction of one year for soil
                                             // temperature calculations

            // check for nth/sth hemisphere
            if ((weather.Latitude > 0.0))
                alx = ANG * (offsetDayOfYear(clock.Today.Year, clock.Today.DayOfYear, System.Convert.ToInt32(-HOTTEST_DAY_NTH)));
            else
                alx = ANG * (offsetDayOfYear(clock.Today.Year, clock.Today.DayOfYear, System.Convert.ToInt32(-HOTTEST_DAY_STH)));

            BoundCheck(alx, 0.0, 6.31, "alx");

            // get change in soil temperature since hottest day. deg c.

            double dlt_temp = TempDelta(alx);    // change in soil temperature

            // get temperature damping depth. (mm per radian of a g_year)

            double damp = DampingDepth();    // temperature damping depth (mm depth/radians time)

            double cum_depth = 0.0;       // cumulative depth in profile

            // Now get the average soil temperature for each layer.
            // The difference in temperature between surface and subsurface
            // layers ( exp(zd)) is an exponential function of the ratio of
            // the depth to the bottom of the layer and the temperature
            // damping depth of the soil.

            double depth_lag = 0.0;             // temperature lag factor in radians for depth

            MathUtilities.Zero(soilTemp);
            for (int layer = 1; layer <= numLayers; layer++)
            {

                // get the cumulative depth to bottom of current layer

                cum_depth = cum_depth + thickness[layer];

                // get the lag factor for depth. This reduces changes in
                // soil temperature with depth. (radians of a g_year)

                depth_lag = Divide(cum_depth, damp, 0.0);

                // allow subsurface temperature changes to lag behind
                // surface temperature changes

                soilTemp[layer] = LayerTemp(depth_lag, alx, dlt_temp);
                BoundCheck(soilTemp[layer], -20.0, 80.0, "soil_temp");
            }
            Array.ConstrainedCopy(soilTemp, 0, soilTempIO, SURFACEnode, numNodes);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="depthLag">(INPUT) lag factor for depth (radians)</param>
        /// <param name="alx">(INPUT) time in radians of a g_year from hottest instance</param>
        /// <param name="deltaTemp">(INPUT) change in surface soil temperature since hottest day (deg c)</param>
        /// <returns></returns>
        /// <remarks></remarks>
        private double LayerTemp(double depthLag, double alx, double deltaTemp)
        {
            // Now get the average soil temperature for the layer.
            // The difference in temperature between surface and subsurface
            // layers ( exp(-depth_lag)) is an exponential function of the ratio of
            // the depth to the bottom of the layer and the temperature
            // damping depth of the soil.

            return weather.Tav + (weather.Amp / 2.0 * Math.Cos(alx - depthLag) + deltaTemp) * Math.Exp(-depthLag);
        }

        /// <summary>
        ///  Purpose
        ///           Calculates  the rate of change in soil surface temperature
        ///           with time.
        /// </summary>
        /// <param name="alx">(INPUT) time of year in radians from warmest instance</param>
        /// <returns>Change in temperature</returns>
        /// <remarks>
        ///           jngh 24-12-91.  I think this is actually a correction to adjust
        ///           today's normal sinusoidal soil surface temperature to the
        ///           current temperature conditions.
        /// </remarks>
        private double TempDelta(double alx)
        {
            // Get today's top layer temp from yesterdays temp and today's
            // weather conditions.
            // The actual soil surface temperature is affected by current
            // weather conditions.

            // Get today's normal surface soil temperature
            // There is no depth lag, being the surface, and there
            // is no adjustment for the current temperature conditions
            // as we want the "normal" sinusoidal temperature for this
            // time of year.

            double temp_a = LayerTemp(0.0, alx, 0.0);

            // Get the rate of change in soil surface temperature with time.
            // This is the difference between a five-day moving average and
            // today's normal surface soil temperature.

            double dT = SurfaceTemperatureInit() - temp_a;

            // check output
            BoundCheck(dT, -100.0, 100.0, "Initial SoilTemp_dt");

            return dT;
        }

        /// <summary>
        /// Calculate initial soil surface temperature
        /// </summary>
        /// <returns> initial soil surface temperature</returns>
        /// <remarks></remarks>
        private double SurfaceTemperatureInit()
        {
            double ave_temp = (maxAirTemp + minAirTemp) * 0.5;
            double surfaceT = (1.0 - waterBalance.Salb) * (ave_temp + (maxAirTemp - ave_temp) * Math.Sqrt(Math.Max(weather.Radn, 0.1) * 23.8846 / 800.0)) + waterBalance.Salb * ave_temp;
            BoundCheck(surfaceT, -100.0, 100.0, "Initial surfaceT");
            return surfaceT;
        }

        /// <summary>
        /// Purpose
        ///           Now get the temperature damping depth. This is a function of the
        ///             average bulk density of the soil and the amount of water above
        ///             the lower limit. I think the damping depth units are
        ///             mm depth/radian of a year
        /// </summary>
        /// <returns>soil temperature damping depth (mm)</returns>
        /// <remarks>
        ///      Notes
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

        /// <summary>
        /// Calculate initial variables for net radiation per timestep
        /// </summary>
        /// <param name="solarRadn">(OUTPUT)</param>
        /// <param name="cloudFr">(OUTPUT)</param>
        /// <param name="cva">(OUTPUT)</param>
        /// <param name="ITERATIONSperDAY"> (INPUT)</param>
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

        /// <summary>
        /// Calculate the net radiation at the soil surface.
        /// </summary>
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
