using System;
using System.Collections.Generic;
using Models.Core;
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

        private const double vonKarman = 0.41;
        #endregion


        /// <summary>The clock</summary>
        [Link]
        private Clock Clock = null;

        /// <summary>The weather</summary>
        [Link]
        private IWeather weather = null;

        private MicroClimateZone MyZone = new MicroClimateZone();

        /// <summary>Constructor</summary>
        public MicroClimate()
        {
            Reset();
        }
        
        /// <summary>Gets or sets the a_interception.</summary>
        [Description("Multiplier on rainfall to calculate interception losses")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm/mm")]
        public double a_interception { get; set; }

        /// <summary>Gets or sets the b_interception.</summary>
        [Description("Power on rainfall to calculate interception losses")]
        [Bounds(Lower = 0.0, Upper = 5.0)]
        [Units("-")]
        public double b_interception { get; set;}

        /// <summary>Gets or sets the c_interception.</summary>
        [Description("Multiplier on LAI to calculate interception losses")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm")]
        public double c_interception { get; set; }

        /// <summary>Gets or sets the d_interception.</summary>
        [Description("Constant value to add to calculate interception losses")]
        [Bounds(Lower = 0.0, Upper = 20.0)]
        [Units("mm")]
        public double d_interception { get; set; }

        /// <summary>Gets or sets the soil_albedo.</summary>
        [Description("Soil albedo")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("MJ/MJ")]
        public double soil_albedo { get; set; }
        
        /// <summary>The air_pressure</summary>
        [Bounds(Lower = 900.0, Upper = 1100.0)]
        [Units("hPa")]
        [Description("Air pressure")]
        public double air_pressure { get; set; }

        /// <summary>The soil_emissivity</summary>
        [Bounds(Lower = 0.9, Upper = 1.0)]
        [Units("0-1")]
        [Description("Soil emissivity")]
        public double soil_emissivity { get; set; }

        /// <summary>The sun_angle</summary>
        [Bounds(Lower = -20.0, Upper = 20.0)]
        [Units("deg")]
        [Description("Sun angle at twilight")]
        public double sun_angle { get; set; }

        /// <summary>The soil_heat_flux_fraction</summary>
        [Description("Fraction of solar radiation reaching the soil surface that results in soil heating")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("MJ/MJ")]
        public double soil_heat_flux_fraction { get; set; }

        /// <summary>The night_interception_fraction</summary>
        [Description("The fraction of intercepted rainfall that evaporates at night")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double night_interception_fraction { get; set; }

        /// <summary>The windspeed_default</summary>
        [Description("Default windspeed to use if not supplied in the met file")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("m/s")]
        public double windspeed_default { get; set; }

        /// <summary>The refheight</summary>
        [Description("Height of the weather instruments")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("m")]
        public double refheight { get; set; }

        /// <summary>The albedo</summary>
        [Description("Albedo of the combined plant-soil system")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double albedo { get; set; }

        /// <summary>The emissivity</summary>
        [Description("Emissivity of the combined plant-soil system")]
        [Bounds(Lower = 0.9, Upper = 1.0)]
        [Units("0-1")]
        public double emissivity { get; set; }


        /// <summary>Gets the gc.</summary>
        [Description("Canopy conductance of the whole system")]
        [Units("m/s")]
        public double gc
        {
            // Should this be returning a sum or an array instead of just the first value???
            get { return ((MyZone.Canopies.Count > 0) && (MyZone.numLayers > 0)) ? MyZone.Canopies[0].Gc[0] : 0.0; }
        }

        /// <summary>Gets the ga.</summary>
        [Description("Aerodynamic conductance of the whole system")]
        [Units("m/s")]
        public double ga
        {
            // Should this be returning a sum or an array instead of just the first value???
            get { return ((MyZone.Canopies.Count > 0) && (MyZone.numLayers > 0)) ? MyZone.Canopies[0].Ga[0] : 0.0; }
        }



        /// <summary>The proportion of radiation that is intercepted by all canopies</summary>
        [Description("The proportion of radiation that is intercepted by all canopies")]
        [Units("0-1")]
        public double RadIntTotal { get; set; }

        /// <summary>Called when simulation commences.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Reset();

            MyZone.zone = this.Parent as Zone;

            // Create all canopy objects.
            foreach (ICanopy canopy in Apsim.FindAll(this.Parent, typeof(ICanopy)))
                MyZone.Canopies.Add(new CanopyType(canopy));
        }

        /// <summary>Called when the canopy energy balance needs to be calculated.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoEnergyArbitration")]
        private void DoEnergyArbitration(object sender, EventArgs e)
        {
            MetVariables();
            MyZone.DoCanopyCompartments();
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
            MyZone.Reset();
        }


        /// <summary>Mets the variables.</summary>
        private void MetVariables()
        {
            MyZone.averageT = CalcAverageT(weather.MinT, weather.MaxT);

            // This is the length of time within the day during which
            //  Evaporation will take place
            MyZone.dayLength = MathUtilities.DayLength(Clock.Today.Day, sun_angle, weather.Latitude); 

            // This is the length of time within the day during which
            // the sun is above the horizon
            MyZone.dayLengthLight = MathUtilities.DayLength(Clock.Today.Day, SunSetAngle, weather.Latitude);

            MyZone.sunshineHours = CalcSunshineHours(weather.Radn, MyZone.dayLengthLight, weather.Latitude, Clock.Today.Day);

            MyZone.fractionClearSky = MathUtilities.Divide(MyZone.sunshineHours, MyZone.dayLengthLight, 0.0);
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

            for (int i = MyZone.numLayers - 1; i >= 0; i += -1)
            {
                double Rflux = Rin * 1000000.0 / (MyZone.dayLength * hr2s) * (1.0 - MyZone._albedo);
                double Rint = 0.0;

                for (int j = 0; j <= MyZone.Canopies.Count - 1; j++)
                {
                    MyZone.Canopies[j].Gc[i] = CanopyConductance(MyZone.Canopies[j].Canopy.Gsmax, MyZone.Canopies[j].Canopy.R50, MyZone.Canopies[j].Canopy.FRGR, MyZone.Canopies[j].Fgreen[i], MyZone.layerKtot[i], MyZone.layerLAIsum[i], Rflux);
                    Rint += MyZone.Canopies[j].Rs[i];
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
            for (int i = 0; i <= MyZone.numLayers - 1; i++)
            {
                sumDeltaZ += MyZone.DeltaZ[i];
                // top height
                // total lai
                sumLAI += MyZone.layerLAIsum[i];
            }

            double totalGa = AerodynamicConductanceFAO(windspeed, refheight, sumDeltaZ, sumLAI);

            for (int i = 0; i <= MyZone.numLayers - 1; i++)
                for (int j = 0; j <= MyZone.Canopies.Count - 1; j++)
                    MyZone.Canopies[j].Ga[i] = totalGa * MathUtilities.Divide(MyZone.Canopies[j].Rs[i], MyZone.sumRs, 0.0);
        }

        /// <summary>Calculate the interception loss of water from the canopy</summary>
        private void CalculateInterception()
        {
            double sumLAI = 0.0;
            double sumLAItot = 0.0;
            for (int i = 0; i <= MyZone.numLayers - 1; i++)
            {
                for (int j = 0; j <= MyZone.Canopies.Count - 1; j++)
                {
                    sumLAI += MyZone.Canopies[j].layerLAI[i];
                    sumLAItot += MyZone.Canopies[j].layerLAItot[i];
                }
            }

            double totalInterception = a_interception * Math.Pow(weather.Rain, b_interception) + c_interception * sumLAItot + d_interception;

            totalInterception = Math.Max(0.0, Math.Min(0.99 * weather.Rain, totalInterception));

            for (int i = 0; i <= MyZone.numLayers - 1; i++)
                for (int j = 0; j <= MyZone.Canopies.Count - 1; j++)
                    MyZone.Canopies[j].interception[i] = MathUtilities.Divide(MyZone.Canopies[j].layerLAI[i], sumLAI, 0.0) * totalInterception;
        }

        /// <summary>Calculate the Penman-Monteith water demand</summary>
        private void CalculatePM()
        {
            // zero a few things, and sum a few others
            double sumRl = 0.0;
            double sumRsoil = 0.0;
            double sumInterception = 0.0;
            double freeEvapGa = 0.0;
            for (int i = 0; i <= MyZone.numLayers - 1; i++)
            {
                for (int j = 0; j <= MyZone.Canopies.Count - 1; j++)
                {
                    MyZone.Canopies[j].PET[i] = 0.0;
                    MyZone.Canopies[j].PETr[i] = 0.0;
                    MyZone.Canopies[j].PETa[i] = 0.0;
                    sumRl += MyZone.Canopies[j].Rl[i];
                    sumRsoil += MyZone.Canopies[j].Rsoil[i];
                    sumInterception += MyZone.Canopies[j].interception[i];
                    freeEvapGa += MyZone.Canopies[j].Ga[i];
                }
            }

            double netRadiation = ((1.0 - MyZone._albedo) * MyZone.sumRs + sumRl + sumRsoil) * 1000000.0;
            // MJ/J
            netRadiation = Math.Max(0.0, netRadiation);
            double freeEvapGc = freeEvapGa * 1000000.0;
            // =infinite surface conductance
            double freeEvap = CalcPenmanMonteith(netRadiation, weather.MinT, weather.MaxT, weather.VP, air_pressure, MyZone.dayLength, freeEvapGa, freeEvapGc);

            MyZone.dryleaffraction = 1.0 - MathUtilities.Divide(sumInterception * (1.0 - night_interception_fraction), freeEvap, 0.0);
            MyZone.dryleaffraction = Math.Max(0.0, MyZone.dryleaffraction);

            for (int i = 0; i <= MyZone.numLayers - 1; i++)
                for (int j = 0; j <= MyZone.Canopies.Count - 1; j++)
                {
                    netRadiation = 1000000.0 * ((1.0 - MyZone._albedo) * MyZone.Canopies[j].Rs[i] + MyZone.Canopies[j].Rl[i] + MyZone.Canopies[j].Rsoil[i]);
                    // MJ/J
                    netRadiation = Math.Max(0.0, netRadiation);

                    if (j == 39) 
                        netRadiation += 0.0;
                    MyZone.Canopies[j].PETr[i] = CalcPETr(netRadiation * MyZone.dryleaffraction, weather.MinT, weather.MaxT, air_pressure, MyZone.Canopies[j].Ga[i], MyZone.Canopies[j].Gc[i]);
                    MyZone.Canopies[j].PETa[i] = CalcPETa(weather.MinT, weather.MaxT, weather.VP, air_pressure, MyZone.dayLength * MyZone.dryleaffraction, MyZone.Canopies[j].Ga[i], MyZone.Canopies[j].Gc[i]);
                    MyZone.Canopies[j].PET[i] = MyZone.Canopies[j].PETr[i] + MyZone.Canopies[j].PETa[i];
                }
        }

        /// <summary>Gets the interception.</summary>
        [Description("Intercepted rainfall")]
        [Units("mm")]
        public double interception
        {
            get
            {
                double totalInterception = 0.0;
                for (int i = 0; i <= MyZone.numLayers - 1; i++)
                    for (int j = 0; j <= MyZone.Canopies.Count - 1; j++)
                        totalInterception += MyZone.Canopies[j].interception[i];
                return totalInterception;
            }
        }

        /// <summary>Calculate the aerodynamic decoupling for system compartments</summary>
        private void CalculateOmega()
        {
            for (int i = 0; i <= MyZone.numLayers - 1; i++)
                for (int j = 0; j <= MyZone.Canopies.Count - 1; j++)
                    MyZone.Canopies[j].Omega[i] = CalcOmega(weather.MinT, weather.MaxT, air_pressure, MyZone.Canopies[j].Ga[i], MyZone.Canopies[j].Gc[i]);
        }

        /// <summary>Send an energy balance event</summary>
        private void SendEnergyBalanceEvent()
        {
            for (int j = 0; j <= MyZone.Canopies.Count - 1; j++)
            {
                CanopyType componentData = MyZone.Canopies[j];

                if (componentData.Canopy != null)
                {
                    CanopyEnergyBalanceInterceptionlayerType[] lightProfile = new CanopyEnergyBalanceInterceptionlayerType[MyZone.numLayers];
                    double totalPotentialEp = 0;
                    double totalInterception = 0.0;
                    for (int i = 0; i <= MyZone.numLayers - 1; i++)
                    {
                        lightProfile[i] = new CanopyEnergyBalanceInterceptionlayerType();
                        lightProfile[i].thickness = Convert.ToSingle(MyZone.DeltaZ[i]);
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
