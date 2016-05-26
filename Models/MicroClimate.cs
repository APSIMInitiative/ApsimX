using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using Models.Core;
using Models;
using Models.PMF;
using System.Xml.Serialization;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models
{
    /// <summary>
    /// The module MICROMET, described here, has been developed to allow the calculation of 
    /// potential transpiration for multiple competing canopies that can be either layered or intermingled.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public partial class MicroClimate : Model
    {

        #region "Useful constants"
        // Teten coefficients
        /// <summary>The SVP_ a</summary>
        private const double svp_A = 6.106;
        // Teten coefficients
        /// <summary>The SVP_ b</summary>
        private const double svp_B = 17.27;
        // Teten coefficients
        /// <summary>The SVP_ c</summary>
        private const double svp_C = 237.3;
        // 0 C in Kelvin (g_k)
        /// <summary>The abs_temp</summary>
        private const double abs_temp = 273.16;
        // universal gas constant (J/mol/K)
        /// <summary>The r_gas</summary>
        private const double r_gas = 8.3143;
        // molecular weight water (kg/mol)
        /// <summary>The mwh2o</summary>
        private const double mwh2o = 0.018016;
        // molecular weight air (kg/mol)
        /// <summary>The mwair</summary>
        private const double mwair = 0.02897;
        // molecular fraction of water to air ()
        /// <summary>The molef</summary>
        private const double molef = mwh2o / mwair;
        // Specific heat of air at constant pressure (J/kg/K)
        /// <summary>The cp</summary>
        private const double Cp = 1010.0;
        // Stefan-Boltzman constant
        /// <summary>The stef_boltz</summary>
        private const double stef_boltz = 5.67E-08;
        // constant for cloud effect on longwave radiation
        /// <summary>The c_cloud</summary>
        private const double c_cloud = 0.1;
        // convert degrees to radians
        /// <summary>The deg2 RAD</summary>
        private const double Deg2Rad = Math.PI / 180.0;
        // kg/m3
        /// <summary>The rho w</summary>
        private const double RhoW = 998.0;
        // weights vpd towards vpd at maximum temperature
        /// <summary>The svp_fract</summary>
        private const double svp_fract = 0.66;
        /// <summary>The sun set angle</summary>
        private const double SunSetAngle = 0.0;
        // hours to seconds
        /// <summary>The HR2S</summary>
        private const double hr2s = 60.0 * 60.0;
        #endregion


        /// <summary>The clock</summary>
        [Link]
        private Clock Clock = null;

        /// <summary>The weather</summary>
        [Link]
        private IWeather weather = null;

        /// <summary>The _albedo</summary>
        private double _albedo = 0;

        /// <summary>The net long wave</summary>
        private double netLongWave;

        /// <summary>The sum rs</summary>
        private double sumRs;
        
        /// <summary>The average t</summary>
        private double averageT;
        
        /// <summary>The sunshine hours</summary>
        private double sunshineHours;
        
        /// <summary>The fraction clear sky</summary>
        private double fractionClearSky;
        
        /// <summary>The day length</summary>
        private double dayLength;
        
        /// <summary>The day length light</summary>
        private double dayLengthLight;
        
        /// <summary>The delta z</summary>
        private double[] DeltaZ = new double[-1 + 1];
        
        /// <summary>The layer ktot</summary>
        private double[] layerKtot = new double[-1 + 1];
        
        /// <summary>The layer la isum</summary>
        private double[] layerLAIsum = new double[-1 + 1];
        
        /// <summary>The number layers</summary>
        private int numLayers;

        /// <summary>The soil_heat</summary>
        private double soil_heat = 0;

        /// <summary>The dryleaffraction</summary>
        private double dryleaffraction = 0;

        /// <summary>The emissivity</summary>
        private double Emissivity = 0.96;
        
        /// <summary>Gets or sets the component data.</summary>
        private List<CanopyType> Canopies = new List<CanopyType>();


        /// <summary>Constructor</summary>
        public MicroClimate()
        {
            Reset();
        }
        
        /// <summary>Gets or sets the a_interception.</summary>
        [Description("a_interception")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm/mm")]
        public double a_interception { get; set; }

        /// <summary>Gets or sets the b_interception.</summary>
        [Description("b_interception")]
        [Bounds(Lower = 0.0, Upper = 5.0)]
        public double b_interception { get; set;}

        /// <summary>Gets or sets the c_interception.</summary>
        [Description("c_interception")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm")]
        public double c_interception { get; set; }

        /// <summary>Gets or sets the d_interception.</summary>
        [Description("d_interception")]
        [Bounds(Lower = 0.0, Upper = 20.0)]
        [Units("mm")]
        public double d_interception { get; set; }

        /// <summary>Gets or sets the soil_albedo.</summary>
        [Description("soil albedo")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double soil_albedo { get; set; }
        
        /// <summary>The air_pressure</summary>
        [Bounds(Lower = 900.0, Upper = 1100.0)]
        [Units("hPa")]
        [Description("air pressure")]
        public double air_pressure { get; set; }

        /// <summary>The soil_emissivity</summary>
        [Bounds(Lower = 0.9, Upper = 1.0)]
        [Units("")]
        [Description("soil emissivity")]
        public double soil_emissivity { get; set; }

        /// <summary>The sun_angle</summary>
        [Bounds(Lower = -20.0, Upper = 20.0)]
        [Units("deg")]
        [Description("sun angle at twilight")]
        public double sun_angle { get; set; }

        /// <summary>The soil_heat_flux_fraction</summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        public double soil_heat_flux_fraction { get; set; }

        /// <summary>The night_interception_fraction</summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        public double night_interception_fraction { get; set; }

        /// <summary>The windspeed_default</summary>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("m/s")]
        public double windspeed_default { get; set; }

        /// <summary>The refheight</summary>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("m")]
        public double refheight { get; set; }

        /// <summary>The albedo</summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double albedo { get; set; }

        /// <summary>The emissivity</summary>
        [Bounds(Lower = 0.9, Upper = 1.0)]
        [Units("0-1")]
        public double emissivity { get; set; }

        /// <summary>The gsmax</summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("m/s")]
        public double gsmax { get; set; }

        /// <summary>The R50</summary>
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("W/m^2")]
        public double r50 { get; set; }

        /// <summary>Gets the interception.</summary>
        [Units("mm")]
        public double interception
        {
            get
            {
                double totalInterception = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                {
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                    {
                        totalInterception += Canopies[j].interception[i];
                    }
                }
                return totalInterception;
            }
        }

        /// <summary>Gets the gc.</summary>
        public double gc
        {
            // Should this be returning a sum or an array instead of just the first value???
            get { return ((Canopies.Count > 0) && (numLayers > 0)) ? Canopies[0].Gc[0] : 0.0; }
        }

        /// <summary>Gets the ga.</summary>
        public double ga
        {
            // Should this be returning a sum or an array instead of just the first value???
            get { return ((Canopies.Count > 0) && (numLayers > 0)) ? Canopies[0].Ga[0] : 0.0; }
        }

        /// <summary>Gets the petr.</summary>
        public double petr
        {
            get
            {
                double totalPetr = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                {
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                    {
                        totalPetr += Canopies[j].PETr[i];
                    }
                }
                return totalPetr;
            }
        }

        /// <summary>Gets the peta.</summary>
        public double peta
        {
            get
            {
                double totalPeta = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                {
                    for (int j = 0; j <= Canopies.Count - 1; j++)
                    {
                        totalPeta += Canopies[j].PETa[i];
                    }
                }
                return totalPeta;
            }
        }

        /// <summary>Gets the net_radn.</summary>
        public double net_radn
        {
            get { return weather.Radn * (1.0 - _albedo) + netLongWave; }
        }

        /// <summary>Gets the net_rs.</summary>
        public double net_rs
        {
            get { return weather.Radn * (1.0 - _albedo); }
        }

        /// <summary>Gets the net_rl.</summary>
        public double net_rl
        {
            get { return netLongWave; }
        }


        /// <summary>Called when simulation commences.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Reset();

            // Create all canopy objects.
            foreach (ICanopy canopy in Apsim.FindAll(this.Parent, typeof(ICanopy)))
                Canopies.Add(new CanopyType(canopy));
        }

        /// <summary>Called when the canopy energy balance needs to be calculated.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoEnergyArbitration")]
        private void DoEnergyArbitration(object sender, EventArgs e)
        {
            MetVariables();
            CanopyCompartments();
            BalanceCanopyEnergy();

            CalculateGc();
            CalculateGa();
            CalculateInterception();
            CalculatePM();
            CalculateOmega();

            SendEnergyBalanceEvent();
        }

        /// <summary>Reset the MicroClimate model back to its original state.</summary>
        private void Reset()
        {
            a_interception = 0.0;
            b_interception = 1.0;
            c_interception = 0.0;
            d_interception = 0.0;
            soil_albedo = 0.23;
            air_pressure = 1010;
            soil_emissivity = 0.96;
            sun_angle = 15.0;

            soil_heat_flux_fraction = 0.4;
            night_interception_fraction = 0.5;
            windspeed_default = 3.0;
            refheight = 2.0;
            albedo = 0.15;
            emissivity = 0.96;
            gsmax = 0.01;
            r50 = 200;
            soil_heat = 0.0;
            dryleaffraction = 0.0;
            _albedo = albedo;
            netLongWave = 0;
            sumRs = 0;
            averageT = 0;
            sunshineHours = 0;
            fractionClearSky = 0;
            dayLength = 0;
            dayLengthLight = 0;
            numLayers = 0;
            DeltaZ = new double[-1 + 1];
            layerKtot = new double[-1 + 1];
            layerLAIsum = new double[-1 + 1];
            Canopies.Clear();
        }

        /// <summary>Canopies the compartments.</summary>
        private void CanopyCompartments()
        {
            DefineLayers();
            DivideComponents();
            LightExtinction();
        }

        /// <summary>Mets the variables.</summary>
        private void MetVariables()
        {
            // averageT = (maxt + mint) / 2.0;
            averageT = CalcAverageT(weather.MinT, weather.MaxT);

            // This is the length of time within the day during which
            //  Evaporation will take place
            dayLength = CalcDayLength(weather.Latitude, Clock.Today.Day, sun_angle);

            // This is the length of time within the day during which
            // the sun is above the horizon
            dayLengthLight = CalcDayLength(weather.Latitude, Clock.Today.Day, SunSetAngle);

            sunshineHours = CalcSunshineHours(weather.Radn, dayLengthLight, weather.Latitude, Clock.Today.Day);

            fractionClearSky = MathUtilities.Divide(sunshineHours, dayLengthLight, 0.0);
        }

        /// <summary>Break the combined Canopy into layers</summary>
        private void DefineLayers()
        {
            double[] nodes = new double[2 * Canopies.Count];
            int numNodes = 1;
            for (int compNo = 0; compNo <= Canopies.Count - 1; compNo++)
            {
                double height = Canopies[compNo].HeightMetres;
                double canopyBase = height - Canopies[compNo].DepthMetres;
                if (Array.IndexOf(nodes, height) == -1)
                {
                    nodes[numNodes] = height;
                    numNodes = numNodes + 1;
                }
                if (Array.IndexOf(nodes, canopyBase) == -1)
                {
                    nodes[numNodes] = canopyBase;
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
                Array.Resize<double>(ref layerLAIsum, numLayers);

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
            {
                DeltaZ[i] = nodes[i + 1] - nodes[i];
            }
        }

        /// <summary>Break the components into layers</summary>
        private void DivideComponents()
        {
            double[] Ld = new double[Canopies.Count];
            for (int j = 0; j <= Canopies.Count - 1; j++)
            {
                CanopyType componentData = Canopies[j];

                componentData.layerLAI = new double[numLayers];
                componentData.layerLAItot = new double[numLayers];
                Ld[j] = MathUtilities.Divide(componentData.Canopy.LAITotal, componentData.DepthMetres, 0.0);
            }
            double top = 0.0;
            double bottom = 0.0;

            for (int i = 0; i <= numLayers - 1; i++)
            {
                bottom = top;
                top = top + DeltaZ[i];
                layerLAIsum[i] = 0.0;

                // Calculate LAI for layer i and component j
                // ===========================================
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    CanopyType componentData = Canopies[j];

                    if ((componentData.HeightMetres > bottom) && (componentData.HeightMetres - componentData.DepthMetres < top))
                    {
                        componentData.layerLAItot[i] = Ld[j] * DeltaZ[i];
                        componentData.layerLAI[i] = componentData.layerLAItot[i] * MathUtilities.Divide(componentData.Canopy.LAI, componentData.Canopy.LAITotal, 0.0);
                        layerLAIsum[i] += componentData.layerLAItot[i];
                    }
                }

                // Calculate fractional contribution for layer i and component j
                // ====================================================================
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    CanopyType componentData = Canopies[j];

                    componentData.Ftot[i] = MathUtilities.Divide(componentData.layerLAItot[i], layerLAIsum[i], 0.0);
                    // Note: Sum of Fgreen will be < 1 as it is green over total
                    componentData.Fgreen[i] = MathUtilities.Divide(componentData.layerLAI[i], layerLAIsum[i], 0.0);
                }
            }
        }

        /// <summary>Calculate light extinction parameters</summary>
        private void LightExtinction()
        {
            // Calculate effective K from LAI and cover
            // =========================================
            for (int j = 0; j <= Canopies.Count - 1; j++)
            {
                CanopyType componentData = Canopies[j];

                if (MathUtilities.FloatsAreEqual(Canopies[j].Canopy.CoverGreen, 1.0, 1E-05))
                {
                    throw new Exception("Unrealistically high cover value in MicroMet i.e. > -.9999");
                }

                componentData.K = MathUtilities.Divide(-Math.Log(1.0 - componentData.Canopy.CoverGreen), componentData.Canopy.LAI, 0.0);
                componentData.Ktot = MathUtilities.Divide(-Math.Log(1.0 - componentData.Canopy.CoverTotal), componentData.Canopy.LAITotal, 0.0);
            }

            // Calculate extinction for individual layers
            // ============================================
            for (int i = 0; i <= numLayers - 1; i++)
            {
                layerKtot[i] = 0.0;
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    CanopyType componentData = Canopies[j];

                    layerKtot[i] += componentData.Ftot[i] * componentData.Ktot;

                }
            }
        }

        /// <summary>Perform the overall Canopy Energy Balance</summary>
        private void BalanceCanopyEnergy()
        {
            ShortWaveRadiation();
            EnergyTerms();
            LongWaveRadiation();
            SoilHeatRadiation();
        }

        /// <summary>Calculate the canopy conductance for system compartments</summary>
        private void CalculateGc()
        {
            double Rin = weather.Radn;

            for (int i = numLayers - 1; i >= 0; i += -1)
            {
                double Rflux = Rin * 1000000.0 / (dayLength * hr2s) * (1.0 - _albedo);
                double Rint = 0.0;

                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    CanopyType componentData = Canopies[j];

                    componentData.Gc[i] = CanopyConductance(componentData.Canopy.Gsmax, componentData.Canopy.R50, componentData.Canopy.FRGR, componentData.Fgreen[i], layerKtot[i], layerLAIsum[i], Rflux);

                    Rint += componentData.Rs[i];
                }
                // Calculate Rin for the next layer down
                Rin -= Rint;
            }
        }

        /// <summary>Calculate the aerodynamic conductance for system compartments</summary>
        private void CalculateGa()
        {
            double windspeed = weather.Wind;
            double sumDeltaZ = 0.0;
            double sumLAI = 0.0;
            for (int i = 0; i <= numLayers - 1; i++)
            {
                sumDeltaZ += DeltaZ[i];
                // top height
                // total lai
                sumLAI += layerLAIsum[i];
            }

            double totalGa = AerodynamicConductanceFAO(windspeed, refheight, sumDeltaZ, sumLAI);

            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    Canopies[j].Ga[i] = totalGa * MathUtilities.Divide(Canopies[j].Rs[i], sumRs, 0.0);
                }
            }
        }

        /// <summary>Calculate the interception loss of water from the canopy</summary>
        private void CalculateInterception()
        {
            double sumLAI = 0.0;
            double sumLAItot = 0.0;
            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    sumLAI += Canopies[j].layerLAI[i];
                    sumLAItot += Canopies[j].layerLAItot[i];
                }
            }

            double totalInterception = a_interception * Math.Pow(weather.Rain, b_interception) + c_interception * sumLAItot + d_interception;

            totalInterception = Math.Max(0.0, Math.Min(0.99 * weather.Rain, totalInterception));

            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    Canopies[j].interception[i] = MathUtilities.Divide(Canopies[j].layerLAI[i], sumLAI, 0.0) * totalInterception;
                }
            }
        }

        /// <summary>Calculate the Penman-Monteith water demand</summary>
        private void CalculatePM()
        {
            // zero a few things, and sum a few others
            double sumRl = 0.0;
            double sumRsoil = 0.0;
            double sumInterception = 0.0;
            double freeEvapGa = 0.0;
            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    CanopyType componentData = Canopies[j];
                    componentData.PET[i] = 0.0;
                    componentData.PETr[i] = 0.0;
                    componentData.PETa[i] = 0.0;
                    sumRl += componentData.Rl[i];
                    sumRsoil += componentData.Rsoil[i];
                    sumInterception += componentData.interception[i];
                    freeEvapGa += componentData.Ga[i];
                }
            }

            double netRadiation = ((1.0 - _albedo) * sumRs + sumRl + sumRsoil) * 1000000.0;
            // MJ/J
            netRadiation = Math.Max(0.0, netRadiation);
            double freeEvapGc = freeEvapGa * 1000000.0;
            // =infinite surface conductance
            double freeEvap = CalcPenmanMonteith(netRadiation, weather.MinT, weather.MaxT, weather.VP, air_pressure, dayLength, freeEvapGa, freeEvapGc);

            dryleaffraction = 1.0 - MathUtilities.Divide(sumInterception * (1.0 - night_interception_fraction), freeEvap, 0.0);
            dryleaffraction = Math.Max(0.0, dryleaffraction);

            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    CanopyType componentData = Canopies[j];

                    netRadiation = 1000000.0 * ((1.0 - _albedo) * componentData.Rs[i] + componentData.Rl[i] + componentData.Rsoil[i]);
                    // MJ/J
                    netRadiation = Math.Max(0.0, netRadiation);

                    if (j == 39) 
                        netRadiation += 0.0;
                    componentData.PETr[i] = CalcPETr(netRadiation * dryleaffraction, weather.MinT, weather.MaxT, air_pressure, componentData.Ga[i], componentData.Gc[i]);

                    componentData.PETa[i] = CalcPETa(weather.MinT, weather.MaxT, weather.VP, air_pressure, dayLength * dryleaffraction, componentData.Ga[i], componentData.Gc[i]);

                    componentData.PET[i] = componentData.PETr[i] + componentData.PETa[i];
                }
            }
        }

        /// <summary>Calculate the aerodynamic decoupling for system compartments</summary>
        private void CalculateOmega()
        {
            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    CanopyType componentData = Canopies[j];

                    componentData.Omega[i] = CalcOmega(weather.MinT, weather.MaxT, air_pressure, componentData.Ga[i], componentData.Gc[i]);
                }
            }
        }

        /// <summary>Send an energy balance event</summary>
        private void SendEnergyBalanceEvent()
        {
            for (int j = 0; j <= Canopies.Count - 1; j++)
            {
                CanopyType componentData = Canopies[j];

                if (componentData.Canopy != null)
                {
                    CanopyEnergyBalanceInterceptionlayerType[] lightProfile = new CanopyEnergyBalanceInterceptionlayerType[numLayers];
                    double totalPotentialEp = 0;
                    double totalInterception = 0.0;
                    for (int i = 0; i <= numLayers - 1; i++)
                    {
                        lightProfile[i] = new CanopyEnergyBalanceInterceptionlayerType();
                        lightProfile[i].thickness = Convert.ToSingle(DeltaZ[i]);
                        lightProfile[i].amount = Convert.ToSingle(componentData.Rs[i] * RadnGreenFraction(j));
                        totalPotentialEp += componentData.PET[i];
                        totalInterception += componentData.interception[i];
                    }

                    componentData.Canopy.PotentialEP = totalPotentialEp;
                    componentData.Canopy.LightProfile = lightProfile;
                }
            }
        }



        /// <summary>Wraps a canopy object.</summary>
        [Serializable]
        private class CanopyType
        {
            /// <summary>The canopy.</summary>
            public ICanopy Canopy;

            /// <summary>The ktot</summary>
            public double Ktot;

            /// <summary>The k</summary>
            public double K;

            /// <summary>The height</summary>
            public double HeightMetres
            {
                get
                {
                    return Math.Round(Canopy.Height, 5) / 1000.0; // Round off a bit and convert mm to m } }
                }
            }

            /// <summary>The depth</summary>
            public double DepthMetres
            {
                get
                {
                    return Math.Round(Canopy.Depth, 5) / 1000.0; // Round off a bit and convert mm to m } }
                }
            }
                        
            /// <summary>The layer lai</summary>
            public double[] layerLAI;

            /// <summary>The layer la itot</summary>
            public double[] layerLAItot;

            /// <summary>The ftot</summary>
            public double[] Ftot;

            /// <summary>The fgreen</summary>
            public double[] Fgreen;

            /// <summary>The rs</summary>
            public double[] Rs;

            /// <summary>The rl</summary>
            public double[] Rl;

            /// <summary>The rsoil</summary>
            public double[] Rsoil;

            /// <summary>The gc</summary>
            public double[] Gc;

            /// <summary>The ga</summary>
            public double[] Ga;

            /// <summary>The pet</summary>
            public double[] PET;

            /// <summary>The pe tr</summary>
            public double[] PETr;

            /// <summary>The pe ta</summary>
            public double[] PETa;

            /// <summary>The omega</summary>
            public double[] Omega;

            /// <summary>The interception</summary>
            public double[] interception;

            /// <summary>Constructor</summary>
            /// <param name="canopy">The canopy to wrap.</param>
            public CanopyType(ICanopy canopy)
            {
                Canopy = canopy;
            }
        }
    }
}
