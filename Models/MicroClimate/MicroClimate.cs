using System;
using System.Collections.Generic;
using Models.Core;
using Models.Interfaces;
using APSIM.Shared.Utilities;
using System.Xml.Serialization;

namespace Models
{
    /// <summary>
    /// # [Name]
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

        #region Constants
        /// <summary>The SVP_ a Teten coefficient</summary>
        private const double svp_A = 6.106;
        /// <summary>The SVP_ b Teten coefficient</summary>
        private const double svp_B = 17.27;
        /// <summary>The SVP_ c Teten coefficient</summary> 
        private const double svp_C = 237.3;
        /// <summary>0 C in Kelvin (k)</summary>
        private const double abs_temp = 273.16;
        /// <summary>universal gas constant (J/mol/K)</summary>
        private const double r_gas = 8.3143;
        /// <summary>molecular weight water (kg/mol)</summary>
        private const double mwh2o = 0.018016;
        /// <summary>molecular weight air (kg/mol)</summary>
        private const double mwair = 0.02897;
        /// <summary>molecular fraction of water to air ()</summary>
        private const double molef = mwh2o / mwair;
        /// <summary>Specific heat of air at constant pressure (J/kg/K)</summary>
        private const double Cp = 1010.0;
        /// <summary>Stefan-Boltzman constant</summary>
        private const double stef_boltz = 5.67E-08;
        /// <summary>constant for cloud effect on longwave radiation</summary>
        private const double c_cloud = 0.1;
        /// <summary>convert degrees to radians</summary>
        private const double Deg2Rad = Math.PI / 180.0;
        /// <summary>Density of water (kg/m3)</summary>
        private const double RhoW = 998.0;
        /// <summary>weights vpd towards vpd at maximum temperature</summary>
        private const double svp_fract = 0.66;
        /// <summary>The sun set angle (degrees)</summary>
        private const double SunSetAngle = 0.0;
        /// <summary>The sun angle for net positive radiation (degrees)</summary>
        private const double SunAngleNetPositiveRadiation = 15;
        /// <summary>Convert hours to seconds</summary>
        private const double hr2s = 60.0 * 60.0;
        /// <summary>von Karman constant</summary>
        private const double vonKarman = 0.41;
        /// <summary>Canopy emissivity</summary>
        private const double CanopyEmissivity = 0.96;
        /// <summary>The soil_emissivity</summary>
        private const double SoilEmissivity = 0.96;

        #endregion


        /// <summary>The clock</summary>
        [Link]
        private Clock Clock = null;

        /// <summary>The weather</summary>
        [Link]
        private IWeather weather = null;

        /// <summary>List of uptakes</summary>
        private List<MicroClimateZone> microClimateZones = new List<MicroClimateZone>();

        /// <summary>Constructor</summary>
        public MicroClimate()
        {
        }

        /// <summary>This is the length of time within the day during which evaporation will take place</summary>
        private double dayLengthEvap;
        /// <summary>This is the length of time within the day during which the sun is above the horizon</summary>
        private double dayLengthLight;

        /// <summary>Gets or sets the a_interception.</summary>
        [Description("Multiplier on rainfall to calculate interception losses")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm/mm")]
        public double a_interception { get; set; }

        /// <summary>Gets or sets the b_interception.</summary>
        [Description("Power on rainfall to calculate interception losses")]
        [Bounds(Lower = 0.0, Upper = 5.0)]
        [Units("-")]
        public double b_interception { get; set; }

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

        /// <summary>Fraction of solar radiation reaching the soil surface that results in soil heating</summary>
        [Description("Fraction of solar radiation reaching the soil surface that results in soil heating")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("MJ/MJ")]
        public double SoilHeatFluxFraction { get; set; }

        /// <summary>The fraction of intercepted rainfall that evaporates at night</summary>
        [Description("The fraction of intercepted rainfall that evaporates at night")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double NightInterceptionFraction { get; set; }

        /// <summary>Height of the weather instruments</summary>
        [Description("Height of the weather instruments")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("m")]
        public double ReferenceHeight { get; set; }

        /// <summary>Called when simulation commences.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (Zone newZone in Apsim.ChildrenRecursively(this.Parent, typeof(Zone)))
                CreateMCZone(newZone);
            if (microClimateZones.Count == 0)
                CreateMCZone(this.Parent as Zone);
        }

        /// <summary>
        /// Create a new MicroClimateZone for a given simulation zone
        /// </summary>
        /// <param name="newZone"></param>
        private void CreateMCZone(Zone newZone)
        {
            MicroClimateZone myZone = new MicroClimateZone();
            myZone.zone = newZone;
            myZone.Reset();
            foreach (ICanopy canopy in Apsim.ChildrenRecursively(newZone, typeof(ICanopy)))
                myZone.Canopies.Add(new CanopyType(canopy));
            microClimateZones.Add(myZone);
        }

        /// <summary>Called when the canopy energy balance needs to be calculated.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoEnergyArbitration")]
        private void DoEnergyArbitration(object sender, EventArgs e)
        {
            dayLengthEvap = MathUtilities.DayLength(Clock.Today.DayOfYear, SunAngleNetPositiveRadiation, weather.Latitude);
            dayLengthLight = MathUtilities.DayLength(Clock.Today.DayOfYear, SunSetAngle, weather.Latitude);

            foreach (MicroClimateZone MCZone in microClimateZones)
            {
                MCZone.DoCanopyCompartments();
                CalculateShortWaveRadiation(MCZone);
                CalculateEnergyTerms(MCZone);
                CalculateLongWaveRadiation(MCZone);
                CalculateSoilHeatRadiation(MCZone);
                CalculateGc(MCZone);
                CalculateGa(MCZone);
                CalculateInterception(MCZone);
                CalculatePM(MCZone);
                CalculateOmega(MCZone);
                SetCanopyEnergyTerms(MCZone);
            }
        }

        /// <summary>Calculate the canopy conductance for system compartments</summary>
        private void CalculateGc(MicroClimateZone MCZone)
        {
            double Rin = weather.Radn;

            for (int i = MCZone.numLayers - 1; i >= 0; i += -1)
            {
                double Rflux = Rin * 1000000.0 / (dayLengthEvap * hr2s) * (1.0 - MCZone.Albedo);
                double Rint = 0.0;

                for (int j = 0; j <= MCZone.Canopies.Count - 1; j++)
                {
                    MCZone.Canopies[j].Gc[i] = CanopyConductance(MCZone.Canopies[j].Canopy.Gsmax, MCZone.Canopies[j].Canopy.R50, MCZone.Canopies[j].Canopy.FRGR, MCZone.Canopies[j].Fgreen[i], MCZone.layerKtot[i], MCZone.LAItotsum[i], Rflux);
                    Rint += MCZone.Canopies[j].Rs[i];
                }
                // Calculate Rin for the next layer down
                Rin -= Rint;
            }
        }

        /// <summary>Calculate the aerodynamic conductance for system compartments</summary>
        private void CalculateGa(MicroClimateZone MCZone)
        {
            double sumDeltaZ = MathUtilities.Sum(MCZone.DeltaZ);
            double sumLAI = MathUtilities.Sum(MCZone.LAItotsum);
            double totalGa = AerodynamicConductanceFAO(weather.Wind, ReferenceHeight, sumDeltaZ, sumLAI);

            for (int i = 0; i <= MCZone.numLayers - 1; i++)
                for (int j = 0; j <= MCZone.Canopies.Count - 1; j++)
                    MCZone.Canopies[j].Ga[i] = totalGa * MathUtilities.Divide(MCZone.Canopies[j].Rs[i], MCZone.sumRs, 0.0);
        }

        /// <summary>Calculate the interception loss of water from the canopy</summary>
        private void CalculateInterception(MicroClimateZone MCZone)
        {
            double sumLAI = 0.0;
            double sumLAItot = 0.0;
            for (int i = 0; i <= MCZone.numLayers - 1; i++)
                for (int j = 0; j <= MCZone.Canopies.Count - 1; j++)
                {
                    sumLAI += MCZone.Canopies[j].LAI[i];
                    sumLAItot += MCZone.Canopies[j].LAItot[i];
                }

            double totalInterception = a_interception * Math.Pow(weather.Rain, b_interception) + c_interception * sumLAItot + d_interception;
            totalInterception = Math.Max(0.0, Math.Min(0.99 * weather.Rain, totalInterception));

            for (int i = 0; i <= MCZone.numLayers - 1; i++)
                for (int j = 0; j <= MCZone.Canopies.Count - 1; j++)
                    MCZone.Canopies[j].interception[i] = MathUtilities.Divide(MCZone.Canopies[j].LAI[i], sumLAI, 0.0) * totalInterception;
        }

        /// <summary>Calculate the Penman-Monteith water demand</summary>
        private void CalculatePM(MicroClimateZone MCZone)
        {
            // zero a few things, and sum a few others
            double sumRl = 0.0;
            double sumRsoil = 0.0;
            double sumInterception = 0.0;
            double freeEvapGa = 0.0;

            for (int j = 0; j <= MCZone.Canopies.Count - 1; j++)
            {
                sumRl += MathUtilities.Sum(MCZone.Canopies[j].Rl);
                sumRsoil += MathUtilities.Sum(MCZone.Canopies[j].Rsoil);
                sumInterception += MathUtilities.Sum(MCZone.Canopies[j].interception);
                freeEvapGa += MathUtilities.Sum(MCZone.Canopies[j].Ga);
            }

            double netRadiation = ((1.0 - MCZone.Albedo) * MCZone.sumRs + sumRl + sumRsoil) * 1000000.0;   // MJ/J
            netRadiation = Math.Max(0.0, netRadiation);

            double freeEvapGc = freeEvapGa * 1000000.0; // infinite surface conductance
            double freeEvap = CalcPenmanMonteith(netRadiation, weather.MinT, weather.MaxT, weather.VP, weather.AirPressure, dayLengthEvap, freeEvapGa, freeEvapGc);

            MCZone.dryleaffraction = 1.0 - MathUtilities.Divide(sumInterception * (1.0 - NightInterceptionFraction), freeEvap, 0.0);
            MCZone.dryleaffraction = Math.Max(0.0, MCZone.dryleaffraction);

            for (int i = 0; i <= MCZone.numLayers - 1; i++)
                for (int j = 0; j <= MCZone.Canopies.Count - 1; j++)
                {
                    netRadiation = 1000000.0 * ((1.0 - MCZone.Albedo) * MCZone.Canopies[j].Rs[i] + MCZone.Canopies[j].Rl[i] + MCZone.Canopies[j].Rsoil[i]);
                    netRadiation = Math.Max(0.0, netRadiation);

                    MCZone.Canopies[j].PETr[i] = CalcPETr(netRadiation * MCZone.dryleaffraction, weather.MinT, weather.MaxT, weather.AirPressure, MCZone.Canopies[j].Ga[i], MCZone.Canopies[j].Gc[i]);
                    MCZone.Canopies[j].PETa[i] = CalcPETa(weather.MinT, weather.MaxT, weather.VP, weather.AirPressure, dayLengthEvap * MCZone.dryleaffraction, MCZone.Canopies[j].Ga[i], MCZone.Canopies[j].Gc[i]);
                    MCZone.Canopies[j].PET[i] = MCZone.Canopies[j].PETr[i] + MCZone.Canopies[j].PETa[i];
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
                for (int i = 0; i <= microClimateZones[0].numLayers - 1; i++)
                    for (int j = 0; j <= microClimateZones[0].Canopies.Count - 1; j++)
                        totalInterception += microClimateZones[0].Canopies[j].interception[i];
                return totalInterception;
            }
        }

        /// <summary>Calculate the aerodynamic decoupling for system compartments</summary>
        private void CalculateOmega(MicroClimateZone MCZone)
        {
            for (int i = 0; i <= MCZone.numLayers - 1; i++)
                for (int j = 0; j <= MCZone.Canopies.Count - 1; j++)
                    MCZone.Canopies[j].Omega[i] = CalcOmega(weather.MinT, weather.MaxT, weather.AirPressure, MCZone.Canopies[j].Ga[i], MCZone.Canopies[j].Gc[i]);
        }

        /// <summary>Send an energy balance event</summary>
        private void SetCanopyEnergyTerms(MicroClimateZone MCZone)
        {
            for (int j = 0; j <= MCZone.Canopies.Count - 1; j++)
                if (MCZone.Canopies[j].Canopy != null)
                {
                    CanopyEnergyBalanceInterceptionlayerType[] lightProfile = new CanopyEnergyBalanceInterceptionlayerType[MCZone.numLayers];
                    double totalPotentialEp = 0;
                    double totalInterception = 0.0;
                    for (int i = 0; i <= MCZone.numLayers - 1; i++)
                    {
                        lightProfile[i] = new CanopyEnergyBalanceInterceptionlayerType();
                        lightProfile[i].thickness = MCZone.DeltaZ[i];
                        lightProfile[i].amount = MCZone.Canopies[j].Rs[i] * MCZone.RadnGreenFraction(j);
                        totalPotentialEp += MCZone.Canopies[j].PET[i];
                        totalInterception += MCZone.Canopies[j].interception[i];
                    }
                    MCZone.Canopies[j].Canopy.PotentialEP = totalPotentialEp;
                    MCZone.Canopies[j].Canopy.LightProfile = lightProfile;
                }
        }
    }
}
