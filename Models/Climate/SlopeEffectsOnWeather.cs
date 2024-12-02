using Models.Core;
using System;

namespace Models.Climate
{

    /// <summary>
    /// This model can be used to adjust some of the weather variables when simulating a slopping surface (defined by slope and aspect angles)
    /// </summary>
    /// <remarks>
    /// - This include routines to modify the incoming solar radiation as well as minimum and maximum air temperatures;
    /// - Adjusts for rainfall, vp, and windspeed, can be also done, but these are simply relative changes supplied by the user, not calculated.
    /// - Calculations happens on PreparingNewWeatherData event and take also into account the latitude of the site (read from the weather model).
    /// + References:
    ///     Allen, R.G.; Pereira, L.S.; Raes, D.; and Smith, M., 1998. Crop evapotranspiration: guidelines for computing crop water requirements. Irrigation and Drainage Paper No. 56, FAO, Rome, Italy. 300 p.
    ///     Allen, R.G.; Trezza, R.; and Tasumi, M. 2006. Analytical integrated functions for daily solar radiation on slopes. Agricultural and Forest Meteorology, 139(1–2):55-73.
    ///     Almorox, J. and Hontoria, C. 2004. Global solar radiation estimation using sunshine duration in Spain. Energy Conversion and Management, 45(9-10):1529-1535.
    ///     Boland, J.; Scott, L.; and Luther, M. 2001. Modelling the diffuse fraction of global solar radiation on a horizontal surface. Environmetrics, 12(2):103-116.
    ///     Dervishi, S. and Mahdavi, A. 2012. Computing diffuse fraction of global horizontal solar radiation: A model comparison. Solar Energy, 86(6):1796-1802.
    ///     Iqbal, M. 2012. An introduction to solar radiation: Elsevier Science. 408 p.
    /// </remarks>
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SlopeEffectsOnWeather : Model
    {
        /// <summary>Link to APSIM's clock model.</summary>
        [Link]
        private IClock clock = null;

        /// <summary>Link to APSIM's weather file.</summary>
        [Link]
        private Weather weather = null;

        /// <summary>Link to APSIM's summary model.</summary>
        [Link]
        private ISummary summary = null;

        /// <summary>Link to APSIM's zone model.</summary>
        [Link]
        private Zone zone = null;

        /// <summary>Mean value of solar constant (w/m2).</summary>
        private double solarConstant = 1367.0;

        /// <summary>Mean atmospheric pressure at sea level (hPa).</summary>
        private double standardAtmosphericPressure = 101.325;

        /// <summary>Air temperature at standard conditions (Kelvin).</summary>
        private double standardAtmosphericTemperature = 288.15;

        /// <summary>Mean acceleration of gravity at sea level (m/s^2).</summary>
        private double standardGravitationalAcceleration = 9.80665;

        /// <summary>Standard environmental temperature lapse rate in dry air (K/m).</summary>
        private double standardTemperatureLapseRate = 0.00649;

        /// <summary>Mean molar mass of Earth's dry air (kg/mol).</summary>
        private double standardAtmosphericMolarMass = 0.0289644;

        /// <summary>Universal gas constant for air (J/mol/K).</summary>
        private double universalGasConstant = 8.31432;

        /// <summary>A threshold to evaluate significant values.</summary>
        private double epsilon = 1e-10;

        /// <summary>Latitude converted to radians.</summary>
        private double latitudeAngle;

        /// <summary>Slope factor for diffuse and reflected radiation (also called sky view).</summary>
        private double slopeFactor;

        /// <summary>Mean daily solar radiation after correction due to slope and aspect.</summary>
        private double myRadn = 0.0;

        /// <summary>Mean daily vapour pressure after correction imposed by the user.</summary>
        private double myVP = 20.0;

        /// <summary>Mean daily wind speed after correction imposed by the user.</summary>
        private double myWindSpeed = 0.0;

        /// <summary>Hour angle for sunrise/sunset on a horizontal surface.</summary>
        private double sunriseAngleHorizontal = 0.0;

        /// <summary>Hour angle for first sunrise on tilted surface.</summary>
        private double sunriseAngle1Slope = 0.0;

        /// <summary>Hour angle for first sunset on tilted surface.</summary>
        private double sunsetAngle1Slope = 0.0;

        /// <summary>Hour angle for second sunrise on tilted surface.</summary>
        private double sunriseAngle2Slope = 0.0;

        /// <summary>Hour angle for second sunset on tilted surface.</summary>
        private double sunsetAngle2Slope = 0.0;

        /// <summary>Aspect of slope - degrees from south (in radians).</summary>
        private double aspectFromSouthInRadians;

        /// <summary>Albedo of the surrounding environment (0-1).</summary>
        [Separator("General description of the site")]
        [Description("Albedo of the surrounding environment (0-1)")]
        public double SurroundsAlbedo { get; set; } = 0.2;

        /// <summary>Relative change in rainfall.</summary>
        [Separator("Parameters to adjust weather variables (not function of slope)")]
        [Description("Relative change in rainfall")]
        [Units("%")]
        public double dRain { get; set; } = 0.0;

        /// <summary>Relative change in wind.</summary>
        [Description("Relative change in wind")]
        [Units("%")]
        public double dWind { get; set; } = 0.0;

        /// <summary>Relative change in vapour pressure.</summary>
        [Description("Relative change in vapour pressure")]
        [Units("%")]
        public double dVapPressure { get; set; } = 0.0;

        /// <summary>Parameter A for diffuse radiation fraction.</summary>
        public double A_diffuseRadn { get; set; } = -3.664;

        /// <summary>Parameter B for diffuse radiation fraction.</summary>
        public double B_diffuseRadn { get; set; } = 7.011;

        /// <summary>
        /// Mean air turbidity for direct radiation (0-1).
        /// </summary>
        /// <remarks>
        /// The value should be, in practice, between 0.5 (for very dusty/polluted places) and 1.0 (for areas with natural vegetation)
        /// Following Allen et al (2006)
        /// </remarks>
        public double TurbidityCoefficient { get; set; } = 0.95;

        /// <summary>Parameter a of the function for transimissivity index for direct radn.</summary>
        public double a_ki { get; set; } = 0.98;

        /// <summary>Parameter b of the function for transimissivity index for direct radn.</summary>
        public double b_ki { get; set; } = 0.00146;

        /// <summary>parameter c of the function for transimissivity index for direct radn.</summary>
        public double c_ki { get; set; } = 0.075;

        /// <summary>Parameter d of the function for transimissivity index for direct radn.</summary>
        public double d_ki { get; set; } = 0.40;

        /// <summary>Parameter a of the function for precipitable water (mm).</summary>
        public double a_pw { get; set; } = 2.10;

        /// <summary>Parameter b for the function for precipitable water (mm/kPa^2).</summary>
        public double b_pw { get; set; } = 0.14;

        /// <summary>Parameter aT0 of dltTemp × dltRadn function, max rate of change (oC per MJ/m2/day).</summary>
        public double aT0 { get; set; } = 1.61;

        /// <summary>Parameter bT of dltTemp × dltRadn function, non linear coefficient (exponent).</summary>
        public double bT { get; set; } = 0.88;

        /// <summary>Parameter cT of dltTemp × dltRadn function, accounts for wind effects (0-1).</summary>
        public double cT { get; set; } = 0.12;

        /// <summary>Parameter FN of dltTemp × dltRadn function, used when dltRadn is negative (0-1).</summary>
        public double FN { get; set; } = 0.81;

        /// <summary>Parameter FM of dltTemp × dltRadn function, adjust for Tmin (0-1).</summary>
        public double FM { get; set; } = 0.5;

        /// <summary>Original solar radiation input (MJ/m2).</summary>
        [Units("MJ/m2")]
        public double RadnMeasured { get; set; }

        /// <summary>Direct solar radiation (MJ/m2).</summary>
        [Units("MJ/m2")]
        public double RadnDirect { get; set; }

        /// <summary>Diffuse solar radiation (MJ/m2).</summary>
        [Units("MJ/m2")]
        public double RadnDiffuse { get; set; }

        /// <summary>Reflected solar radiation  (MJ/m2).</summary>
        [Units("MJ/m2")]
        public double RadnReflected { get; set; }

        /// <summary>Extraterrestrial solar radiation (MJ/m2).</summary>
        [Units("MJ/m2")]
        public double ExtraterrestrialRadn { get; set; }

        /// <summary>Time length of direct sunlight on a horizontal surface (hrs).</summary>
        [Units("hours")]
        public double MaxDirSunlightLength { get; set; }

        /// <summary>Time length of direct sunlight on tilted surface (hrs).</summary>
        [Units("hours")]
        public double ActualDirSunlightLength { get; set; }

        /// <summary>
        /// Sky clearness index (0-1).
        /// </summary>
        /// <remarks>
        /// Provide an idea of how overcast the day is
        /// </remarks>
        [Units("0-1")]
        public double ClearnessIndex { get; set; }

        /// <summary>Fraction of total radiation that is diffuse (0-1).</summary>
        [Units("0-1")]
        public double DiffuseRadnFraction { get; set; }

        /// <summary>Ratio of direct radiation between slope and horizontal surfaces.</summary>
        [Units("-")]
        public double DirRadnRatio { get; set; }

        /// <summary>Ratio of diffuse radiation between slope and horizontal surfaces.</summary>
        [Units("-")]
        public double DiffRadnRatio { get; set; }

        /// <summary>Fraction of solar radiation that is direct beam (0-1).</summary>
        [Units("0-1")]
        public double FracRadnDirect;

        /// <summary>Fraction of solar radiation that is diffuse (0-1).</summary>
        [Units("0-1")]
        public double FracRadnDiffuse { get; set; }

        /// <summary>Fraction of solar radiation reflected from terrain (0-1).</summary>
        [Units("0-1")]
        public double FracRadnReflected { get; set; }

        /// <summary>Original value of minimum temperature (oC).</summary>
        [Units("oC")]
        public double TminMeasured { get; set; }

        /// <summary>Original value of maximum  temperature (oC).</summary>
        [Units("oC")]
        public double TmaxMeasured { get; set; }

        /// <summary>Actual Tmean value, after adjusts (oC).</summary>
        [Units("oC")]
        public double TmeanActual
        {
            get { return 0.5 * (weather.MaxT + weather.MinT); }
        }

        /// <summary>Variation in Tmin (oC).</summary>
        [Units("oC")]
        public double dltTmin { get; set; }

        /// <summary>Variation in Tmax (oC).</summary>
        [Units("oC")]
        public double dltTmax { get; set; }

        /// <summary>Mean local atmospheric pressure (hPa).</summary>
        [Units("hPa")]
        public double AtmosphericPressure { get; set; } = 0.0;

        /// <summary>Original rain input (mm).</summary>
        [Units("mm")]
        public double RainMeasured { get; set; } = -0.1;

        /// <summary>Original wind speed input (m/s).</summary>
        [Units("m/s")]
        public double WindSpeedMeasured { get; set; } = -0.1;

        /// <summary>Original vapour pressure input (hPa).</summary>
        [Units("hPa")]
        public double VPMeasured { get; set; } = -0.1;

        /// <summary>
        /// Returns the zone's name.
        /// </summary>
        /// <returns></returns>
        public string GetZoneName()
        {
            return zone.Name;
        }

        /// <summary>Invoked at start of simulation.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // Check parameter values
            if ((zone.Slope < 0.0) || (zone.Slope > 90))
                throw new Exception("Slope angle is out of the expected range (0-90 deg)");
            if ((zone.AspectAngle < 0.0) || (zone.AspectAngle > 360))
                throw new Exception("Aspect angle is out of the expected range (0-360 deg)");
            if ((zone.Altitude < -100.0) || (SurroundsAlbedo > 5000))
                throw new Exception("Altitude value is out of bounds (0-1)");
            if ((SurroundsAlbedo < 0.0) || (SurroundsAlbedo > 1.0))
                throw new Exception("Albedo value is out of bounds (0-1)");
            if ((TurbidityCoefficient < 0.0) || (TurbidityCoefficient > 1.0))
                throw new Exception("Turbidity coefficient value is out of bounds (0-1)");

            // Convert and fix some parameters
            // zone.AspectAngle is degrees from north. This algorithm seems to work using aspect from south in radians.
            // Should really check this against original sources of the equations. Seems to make sense.
            aspectFromSouthInRadians = zone.AspectAngle + 180;
            if (aspectFromSouthInRadians > 360)
                aspectFromSouthInRadians -= 360;

            zone.Slope = Math.PI * zone.Slope / 180;
            aspectFromSouthInRadians = Math.PI * aspectFromSouthInRadians / 180;
            latitudeAngle = Math.PI * weather.Latitude / 180;

            // Get the local mean atmospheric pressure, similar to Allen et al (1998)
            double expPressure = standardGravitationalAcceleration * standardAtmosphericMolarMass / (universalGasConstant * standardTemperatureLapseRate);
            AtmosphericPressure = Math.Pow(1 - (standardTemperatureLapseRate * zone.Altitude / standardAtmosphericTemperature), expPressure);
            AtmosphericPressure *= standardAtmosphericPressure;

            // Get the slope factor for correcting reflected radiation, based on Allen et al. (2006)
            slopeFactor = 0.75 + (0.25 * Math.Cos(zone.Slope)) - (0.5 * zone.Slope / Math.PI);

            // Covert the user defined changes from percent into fraction
            dRain = Math.Max(-1.0, 0.01 * dRain);
            dWind = Math.Max(-1.0, 0.01 * dWind);
            dVapPressure = Math.Max(-1.0, 0.01 * dVapPressure);
            // NOTE: dVapPressure has a variable upper bound, it cannot be higher than the saturated VP
            //       this upper limit will be assumed equal to 95% of saturation at daily Tmax

            summary.WriteMessage(this, "     Weather variables will be adjusted for slope and aspect", MessageType.Diagnostic);
            summary.WriteMessage(this, "      - Radiation and temperature adjusted based on the model described by Cichota (2015)", MessageType.Diagnostic);
            summary.WriteMessage(this, "      - Rainfall, wind, and vapour pressure are simple relative changes - not explicitly linked to slope", MessageType.Diagnostic);
            summary.WriteMessage(this, "      - The values of RH, if existent, will be adjusted whenever the temperature or vp change", MessageType.Diagnostic);
        }

        /// <summary>Evaluate whether weather data is to be adjusted due to slope and aspect.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        [EventSubscribe("PreparingNewWeatherData")]
        private void OnPreNewMet(object sender, EventArgs e)
        {
            // Get the basic met values, evaluate their changes and re-set them on MetFile
            // Get and adjust windspeed
            WindSpeedMeasured = weather.Wind;
            myWindSpeed = WindSpeedMeasured;
            if (Math.Abs(dWind) > epsilon)
                myWindSpeed *= 1.0 + dWind;

            // Evaluate changes in VP and RH
            VPMeasured = weather.VP;

            // Evaluate the changes in radiation due to slope and aspect
            RadnMeasured = weather.Radn;
            RadiationOnSlope();

            // Evaluate the changes in temperature
            TmaxMeasured = weather.MaxT;
            TminMeasured = weather.MinT;
            DeltaTemperature();

            // Set the adjusted weather variables
            RainMeasured = weather.Rain;
            if (Math.Abs(dRain) > epsilon)
                weather.Rain *= (float)(1.0 + dRain);

            if (Math.Abs(dltTmax) > epsilon)
                weather.MaxT += (float)dltTmax;
            if (Math.Abs(dltTmin) > epsilon)
            {
                if (weather.MinT + dltTmin > weather.MaxT)
                    weather.MinT = weather.MaxT;
                else
                    weather.MinT += (float)dltTmin;
            }

            if (Math.Abs(weather.Radn - myRadn) > epsilon)
                weather.Radn = (float)myRadn;

            if ((WindSpeedMeasured > 0.0) && (Math.Abs(dWind) > epsilon))
                weather.Wind = myWindSpeed;

            if (Math.Abs(dVapPressure) > epsilon)
                weather.VP = myVP;
        }

        /// <summary>
        /// Computes the solar radiation received by a tilted surface, based on measured values on horizontal.
        /// </summary>
        /// <remarks>
        /// Uses the methodology described by Cichota (2015), adapted from Allen et al. (2006) and Iqbal (2015).
        /// </remarks>
        private void RadiationOnSlope()
        {
            // Extraterrestrial radiation - following classic formulae (Almorox and Hontoria, 2004; Iqbal, 2015)
            double DayAngle = 2 * Math.PI * Math.Min(0.9995, (clock.Today.DayOfYear - 0.5) / 365.25);
            double SolarDeclination = 0.006918 - (0.399912 * Math.Cos(DayAngle))
                                    + (0.070257 * Math.Sin(DayAngle))
                                    - (0.006758 * Math.Cos(2 * DayAngle))
                                    + (0.000907 * Math.Sin(2 * DayAngle))
                                    - (0.002697 * Math.Cos(3 * DayAngle))
                                    + (0.001480 * Math.Sin(3 * DayAngle));
            double EarthEccentricity = 1.00011 + (0.034221 * Math.Cos(DayAngle))
                                     + (0.00128 * Math.Sin(DayAngle))
                                     + (0.000719 * Math.Cos(2 * DayAngle))
                                     + (0.000077 * Math.Sin(2 * DayAngle));
            sunriseAngleHorizontal = Math.Acos(Math.Max(-1, Math.Min(1, -Math.Tan(latitudeAngle) * Math.Tan(SolarDeclination))));
            double RelativeSolarIrradiance = ((Math.Cos(latitudeAngle) * Math.Cos(SolarDeclination) * Math.Sin(sunriseAngleHorizontal))
                                            + (Math.Sin(latitudeAngle) * Math.Sin(SolarDeclination) * sunriseAngleHorizontal)) / Math.PI;
            ExtraterrestrialRadn = RelativeSolarIrradiance * EarthEccentricity * solarConstant * 24 * 3600 / 1000000;

            // Sky clearness index - following typical approach (Boland et al., 2008; Dervishi and Mahdavi, 2012)
            ClearnessIndex = Math.Min(1.0, Math.Max(0.0, weather.Radn / ExtraterrestrialRadn));

            // Diffuse radiation fraction - same approach as Boland et al. (2008), equivalent to Allen et al. (2006)
            DiffuseRadnFraction = 1.0 / (1.0 + Math.Exp(A_diffuseRadn + (B_diffuseRadn * ClearnessIndex)));

            if (zone.Slope > epsilon)
            {
                // Auxiliary variables for radiation (Allen et al., 2006)
                double a_ = Math.Sin(SolarDeclination) * ((Math.Sin(latitudeAngle) * Math.Cos(zone.Slope))
                          - (Math.Cos(latitudeAngle) * Math.Sin(zone.Slope) * Math.Cos(aspectFromSouthInRadians)));
                double b_ = Math.Cos(SolarDeclination) * ((Math.Cos(latitudeAngle) * Math.Cos(zone.Slope))
                          + (Math.Sin(latitudeAngle) * Math.Sin(zone.Slope) * Math.Cos(aspectFromSouthInRadians)));
                double c_ = Math.Cos(SolarDeclination) * (Math.Sin(zone.Slope) * Math.Sin(aspectFromSouthInRadians));
                double g_ = Math.Sin(SolarDeclination) * Math.Sin(latitudeAngle);
                double h_ = Math.Cos(SolarDeclination) * Math.Cos(latitudeAngle);

                // Hour angles for the sunrise/sunset on slope (Cichota, 2015)
                SunriseSunsetOnSlope(a_, b_, c_);

                // Length of daylight, horizontal (max) and slope (actual)
                MaxDirSunlightLength = 24 * sunriseAngleHorizontal / Math.PI;
                ActualDirSunlightLength = 12 * (Math.Max(0.0, sunsetAngle1Slope - sunriseAngle1Slope)
                                + Math.Max(0.0, sunsetAngle2Slope - sunriseAngle2Slope))
                                / Math.PI;

                // Extraterrestrial radiation on slope (Allen et al., 2006)
                double RelativeIrradianceOnSlope = ((a_ * (sunsetAngle1Slope - sunriseAngle1Slope))
                                                 + (b_ * (Math.Sin(sunsetAngle1Slope) - Math.Sin(sunriseAngle1Slope)))
                                                 - (c_ * (Math.Cos(sunsetAngle1Slope) - Math.Cos(sunriseAngle1Slope)))
                                                 + (a_ * (sunsetAngle2Slope - sunriseAngle2Slope))
                                                 + (b_ * (Math.Sin(sunsetAngle2Slope) - Math.Sin(sunriseAngle2Slope)))
                                                 - (c_ * (Math.Cos(sunsetAngle2Slope) - Math.Cos(sunriseAngle2Slope))))
                                                 / (2 * Math.PI);

                // Mean path for direct beam radiation through the atmosphere - for horizontal and slope (Allen et al., 2006)
                double MeanPathHorz = ((2 * sunriseAngleHorizontal * Math.Pow(g_, 2)) + (4 * g_ * h_ * Math.Sin(sunriseAngleHorizontal))
                                   + ((sunriseAngleHorizontal + (0.5 * Math.Sin(2 * sunriseAngleHorizontal))) * Math.Pow(h_, 2)))
                                   / (2 * ((g_ * sunriseAngleHorizontal) + (h_ * Math.Sin(sunriseAngleHorizontal))));
                double MeanPathSlope = 0.0;
                if (ActualDirSunlightLength > epsilon)
                {
                    double aNominator = (((b_ * g_) + (a_ * h_))
                           * (Math.Sin(sunsetAngle1Slope) - Math.Sin(sunriseAngle1Slope) + Math.Sin(sunsetAngle2Slope) - Math.Sin(sunriseAngle2Slope)))
                           - ((c_ * g_)
                           * (Math.Cos(sunsetAngle1Slope) - Math.Cos(sunriseAngle1Slope) + Math.Cos(sunsetAngle2Slope) - Math.Cos(sunriseAngle2Slope)))
                           + (((0.5 * b_ * h_) + (a_ * g_))
                           * (sunsetAngle1Slope - sunriseAngle1Slope + sunsetAngle2Slope - sunriseAngle2Slope))
                           + ((0.25 * b_ * h_)
                           * (Math.Sin(2 * sunsetAngle1Slope) - Math.Sin(2 * sunriseAngle1Slope) + Math.Sin(2 * sunsetAngle2Slope) - Math.Sin(2 * sunriseAngle2Slope)))
                           + ((0.5 * c_ * h_)
                           * (Math.Pow(Math.Sin(sunsetAngle1Slope), 2.0) - Math.Pow(Math.Sin(sunriseAngle1Slope), 2.0)
                           + Math.Pow(Math.Sin(sunsetAngle2Slope), 2.0) - Math.Pow(Math.Sin(sunriseAngle2Slope), 2.0)));
                    double aDenominator = (a_ * (sunsetAngle1Slope - sunriseAngle1Slope + sunsetAngle2Slope - sunriseAngle2Slope))
                           + (b_ * (Math.Sin(sunsetAngle1Slope) - Math.Sin(sunriseAngle1Slope) + Math.Sin(sunsetAngle2Slope) - Math.Sin(sunriseAngle2Slope)))
                           - (c_ * (Math.Cos(sunsetAngle1Slope) - Math.Cos(sunriseAngle1Slope) + Math.Cos(sunsetAngle2Slope) - Math.Cos(sunriseAngle2Slope)));
                    MeanPathSlope = aNominator / aDenominator;
                }

                // amount of precipitable water in the atmosphere  (Allen et al., 2006)
                double PrecipitableWater = a_pw + (b_pw * (myVP * 0.1) * AtmosphericPressure);

                // Transmissivity index for direct beam radiation - horizontal and slope (Allen et al., 2006)
                double KIh = a_ki * Math.Exp((-b_ki * AtmosphericPressure / (TurbidityCoefficient * MeanPathHorz))
                            - (c_ki * Math.Pow(PrecipitableWater / MeanPathHorz, d_ki)));
                double KIs = a_ki * Math.Exp((-b_ki * AtmosphericPressure / (TurbidityCoefficient * MeanPathSlope))
                            - (c_ki * Math.Pow(PrecipitableWater / MeanPathSlope, d_ki)));

                // Direct radiation ratio for slope
                DirRadnRatio = (RelativeIrradianceOnSlope / RelativeSolarIrradiance) * (KIs / KIh);

                // Diffuse radiation ratio for slope
                DiffRadnRatio = ((1.0 - (ClearnessIndex * (1 - DiffuseRadnFraction)))
                              * (1 + (Math.Sqrt(1 - DiffuseRadnFraction) * Math.Pow(Math.Sin(0.5 * zone.Slope), 3.0)))
                              * slopeFactor) + (DirRadnRatio * ClearnessIndex * (1 - DiffuseRadnFraction));

                // Prepare the radiation outputs
                RadnDirect = RadnMeasured * DirRadnRatio * (1 - DiffuseRadnFraction);
                RadnDiffuse = RadnMeasured * DiffRadnRatio * DiffuseRadnFraction;
                RadnReflected = RadnMeasured * SurroundsAlbedo * (1 - slopeFactor);
                myRadn = RadnDirect + RadnDiffuse + RadnReflected;
            }
            else
            {
                // Length of daylight, horizontal (max) and slope (actual)
                MaxDirSunlightLength = 24 * sunriseAngleHorizontal / Math.PI;
                ActualDirSunlightLength = MaxDirSunlightLength;

                // Direct and diffuse radiation ratios for slope
                DirRadnRatio = 1.0;
                DiffRadnRatio = 1.0;

                // Prepare the radiation outputs
                RadnMeasured = weather.Radn;
                RadnDirect = RadnMeasured * (1 - DiffuseRadnFraction);
                RadnDiffuse = RadnMeasured * DiffuseRadnFraction;
                RadnReflected = 0.0;
                myRadn = RadnDirect + RadnDiffuse + RadnReflected;
            }

            if (myRadn > 0.0)
            {
                FracRadnDirect = RadnDirect / myRadn;
                FracRadnDiffuse = RadnDiffuse / myRadn;
                FracRadnReflected = RadnReflected / myRadn;
            }
            else
            {
                FracRadnDirect = 0.0;
                FracRadnDiffuse = 0.0;
                FracRadnReflected = 0.0;
            }
        }

        /// <summary>
        /// Calculate the hour angles for sunrise and sunset on a tilted surface.
        /// </summary>
        /// <param name="a_">Auxiliary parameter a</param>
        /// <param name="b_">Auxiliary parameter b</param>
        /// <param name="c_">Auxiliary parameter c</param>
        private void SunriseSunsetOnSlope(double a_, double b_, double c_)
        {
            double WS1 = -Math.Acos(CosineWS(1, a_, b_, c_));
            double WS2 = Math.Acos(CosineWS(-1, a_, b_, c_));
            double adjWS1 = EvaluateSunAngles(-WS1, -(2 * Math.PI) - WS1, -Math.PI, WS1, a_, b_, c_);
            double adjWS2 = EvaluateSunAngles((2 * Math.PI) - WS2, -WS2, Math.PI, WS2, a_, b_, c_);
            double bSunrise1 = Math.Min(Math.Max(-sunriseAngleHorizontal, adjWS1), sunriseAngleHorizontal);
            double bSunrise2 = -sunriseAngleHorizontal;
            double bSunset1 = Math.Max(Math.Min(sunriseAngleHorizontal, adjWS2), -sunriseAngleHorizontal);
            double bSunset2 = sunriseAngleHorizontal;
            if (c_ < epsilon)
            {
                bSunrise2 = Math.Max(-sunriseAngleHorizontal, Math.Min(sunriseAngleHorizontal, Math.Min(Math.PI, adjWS1 + (2 * Math.PI))));
            }
            if (c_ >= epsilon)
            {
                bSunset2 = Math.Min(sunriseAngleHorizontal, Math.Max(-sunriseAngleHorizontal, Math.Max(-Math.PI, adjWS2 - (2 * Math.PI))));
            }

            // Assign the angles in the right order
            if (bSunset1 > bSunrise1)
            {
                if (bSunset2 > bSunrise2)
                {
                    if (bSunrise1 <= bSunrise2)
                    {
                        sunriseAngle1Slope = bSunrise1;
                        sunsetAngle2Slope = bSunset2;
                        if (bSunrise2 > bSunset1)
                        {
                            sunriseAngle2Slope = bSunrise2;
                            sunsetAngle1Slope = bSunset1;
                        }
                        else
                        {
                            sunriseAngle2Slope = bSunset2;
                            sunsetAngle1Slope = bSunset2;
                        }
                    }
                    else
                    {
                        sunriseAngle1Slope = bSunrise2;
                        sunsetAngle2Slope = bSunset1;
                        if (bSunrise1 > bSunset2)
                        {
                            sunsetAngle1Slope = bSunset2;
                            sunriseAngle2Slope = bSunrise1;
                        }
                        else
                        {
                            sunsetAngle1Slope = bSunset1;
                            sunriseAngle2Slope = bSunset1;
                        }
                    }
                }
                else
                {
                    sunriseAngle1Slope = bSunrise1;
                    sunsetAngle1Slope = bSunset1;
                    sunriseAngle2Slope = bSunset1;
                    sunsetAngle2Slope = bSunset1;
                }
            }
            else
            {
                if (bSunset2 > bSunrise2)
                {
                    sunriseAngle1Slope = bSunrise2;
                    sunsetAngle1Slope = bSunset2;
                    sunriseAngle2Slope = bSunset2;
                    sunsetAngle2Slope = bSunset2;
                }
                else
                {
                    sunriseAngle1Slope = 0.0;
                    sunsetAngle1Slope = 0.0;
                    sunriseAngle2Slope = 0.0;
                    sunsetAngle2Slope = 0.0;
                }
            }
        }

        /// <summary>
        /// Compute the base cosine of ws (sunrise angle) on a tilted surface, uses a quadratic function.
        /// </summary>
        /// <param name="mySwith">Whether the root is positive or negative</param>
        /// <param name="a_">Auxiliary parameter a</param>
        /// <param name="b_">Auxiliary parameter b</param>
        /// <param name="c_">Auxiliary parameter c</param>
        /// <returns>The value of the cosine of ws</returns>
        private double CosineWS(double mySwith, double a_, double b_, double c_)
        {
            double result = 0.0;
            if ((Math.Abs(a_) < epsilon) && (Math.Abs(b_) < epsilon))
            {
                result = mySwith;
            }
            else if (Math.Abs(c_) < epsilon)
            {
                if (Math.Abs(a_) < epsilon)
                    result = 0.0;
                else if (Math.Abs(b_) < epsilon)
                {
                    if (a_ < epsilon)
                        result = -1000.0;
                    else
                        result = 1000.0;
                }
                else
                {
                    result = -a_ / b_;
                }
            }
            else
            {
                result = (-(a_ * b_) + (mySwith * c_ * Math.Sqrt(Math.Max(0.0, -(a_ * a_) + (b_ * b_) + (c_ * c_))))) / ((b_ * b_) + (c_ * c_));
            }

            // limit the result to valid range
            result = Math.Max(-1.0, Math.Min(1.0, result));

            return result;
        }

        /// <summary>
        /// Evaluate the results for sunrise/sunset angle (ws).
        /// </summary>
        /// <param name="WSoption1">Option 1 for ws</param>
        /// <param name="WSoption2">Option 2 for ws</param>
        /// <param name="WSoption3">Option 3 for ws</param>
        /// <param name="WSdefault">Default option for ws</param>
        /// <param name="a_">Auxiliary parameter a</param>
        /// <param name="b_">Auxiliary parameter b</param>
        /// <param name="c_">Auxiliary parameter c</param>
        /// <returns>The appropriate value for ws</returns>
        private double EvaluateSunAngles(double WSoption1, double WSoption2, double WSoption3, double WSdefault, double a_, double b_, double c_)
        {
            double result = 0.0;
            if ((c_ > epsilon) && (Math.Abs(Math.Asin(a_ + (b_ * Math.Cos(WSoption1)) + (c_ * Math.Sin(WSoption1)))) < epsilon))
            {
                result = WSoption1;
            }
            else if ((c_ < -epsilon) && (Math.Abs(Math.Asin(a_ + (b_ * Math.Cos(WSoption2)) + (c_ * Math.Sin(WSoption2)))) < epsilon))
            {
                result = WSoption2;
            }
            else
            {
                if (Math.Asin(a_ + (b_ * Math.Cos(WSdefault)) + (c_ * Math.Sin(WSdefault))) > epsilon)
                {
                    result = WSoption3;
                }
                else if (Math.Asin(a_ + (b_ * Math.Cos(WSdefault)) + (c_ * Math.Sin(WSdefault))) < -epsilon)
                {
                    result = 0.0;
                }
                else
                {
                    if ((Math.Abs(c_) < epsilon) && (b_ < epsilon))
                    {
                        result = WSoption2;
                    }
                    else
                    {
                        result = WSdefault;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Computes the variation in temperature caused by changes in incident radiation in tilted surfaces.
        /// </summary>
        private void DeltaTemperature()
        {
            // Base temperature response to variation in direct radiation, as affected by wind
            double aT = aT0 * Math.Exp(-cT * myWindSpeed);
            double dltRadn = myRadn - RadnMeasured;
            dltTmax = 0.0;
            dltTmin = 0.0;
            if (aT > 0.0)
            {
                if (dltRadn < 0.0)
                {
                    dltTmax = -FN * aT * Math.Pow(Math.Abs(dltRadn), bT);
                    dltTmin = -FN * FM * aT * Math.Pow(Math.Abs(dltRadn), bT);
                }
                else if (dltRadn > 0.0)
                {
                    dltTmax = aT * Math.Pow(dltRadn, bT);
                    dltTmin = FM * aT * Math.Pow(dltRadn, bT);
                }
            }
        }
    }
}