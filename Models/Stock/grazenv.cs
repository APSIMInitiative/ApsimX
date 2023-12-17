using StdUnits;
using System;
using System.Linq;


namespace Models.GrazPlan
{
    /// <summary>
    /// Environment interface
    /// </summary>
    public static class GrazEnv
    {
        // Unit conversion constants

        /// <summary>
        /// Convert day-of-year to radians
        /// </summary>
        public const double DAY2RAD = 2 * Math.PI / 365;

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        public const double DEG2RAD = 2 * Math.PI / 360;

        /// <summary>
        /// Convert km/d to m/s
        /// </summary>
        public const double KMD_2_MS = 1.0E3 / (24 * 60 * 60);

        /// <summary>
        /// Convert W/m^2 to MJ/m^2/d
        /// </summary>
        public const double WM2_2_MJM2 = 1.0E6 / (24 * 60 * 60);

        /// <summary>
        /// Convert degrees C to K
        /// </summary>
        public const double C_2_K = 273.15;

        /// <summary>
        /// The herbage albedo
        /// </summary>
        public const double HERBAGE_ALBEDO = 0.23;

        /// <summary>
        /// Reference [CO2] in ppm
        /// </summary>
        public const double REFERENCE_CO2 = 350.0;
    }

    /// <summary>
    /// Evaporation method for Potential ET calculation
    /// </summary>
    public enum EvapMethod
    {
        /// <summary></summary>
        emPropnPan,

        /// <summary></summary>
        emPenman,

        /// <summary></summary>
        emCERES,

        /// <summary></summary>
        emPriestley,

        /// <summary></summary>
        emFAO
    };

    /// <summary>
    /// Daily weather data, used as inputs
    /// </summary>
    public enum TWeatherData
    {
        /// <summary>Maximum air temperature     deg C</summary>
        wdtMaxT,

        /// <summary>Minimum air temperature     deg C</summary>
        wdtMinT,

        /// <summary>Rainfall                    mm</summary>
        wdtRain,

        /// <summary>Snow (rain equivalents)     mm</summary>
        wdtSnow,

        /// <summary>Solar radiation             MJ/m^2/d</summary>
        wdtRadn,

        /// <summary>Actual vapour pressure      kPa</summary>
        wdtVP,

        /// <summary>Average windspeed           m/s</summary>
        wdtWind,

        /// <summary>Pan evaporation             mm</summary>
        wdtEpan,

        /// <summary>Relative humidity           0-1</summary>
        wdtRelH,

        /// <summary>Hours of bright sunshine    hr</summary>
        wdtSunH,

        /// <summary>Terrestrial min. temperature deg C</summary>
        wdtTrMT
    };

    /// <summary>
    /// Class TWeatherHandler
    /// </summary>
    [Serializable]
    public class TWeatherHandler
    {
        /// <summary></summary>
        public const int NO_ELEMENTS = 1 + (int)TWeatherData.wdtTrMT;

        private double FLatitude;                                   // Latitude,  in degrees
        private double FLongitude;                                  // Longitude, in degrees
        private double FElevation;                                  // Elevation, in m
        private double FSlope;                                      // Slope,     in degrees
        private double FAspect;                                     // Aspect,    in degrees

        private double[] FData = new double[NO_ELEMENTS];
        private bool[] FDataKnown = new bool[NO_ELEMENTS];
        private double[] FDaylengths = new double[2]; // T/F index
        private bool FDayLenIncr;
        private double FET_Radn;

        private int FCO2_Order;
        private double[] FCO2_Coeffs = new double[6];                // Coefficients of a polynomial for [CO2]
        private double FCO2_Conc;

        /// <summary>StdDATE.Date</summary>
        protected int FToday;

        /// <summary></summary>
        public double this[TWeatherData D]
        { get { return getData(D); } set { setData(D, value); } }

        /// <summary></summary>
        public double fLatDegrees { get { return FLatitude; } set { FLatitude = value; } }

        /// <summary></summary>
        public double fLongDegrees { get { return FLongitude; } set { FLongitude = value; } }

        /// <summary></summary>
        public double fElevationM { get { return FElevation; } set { FElevation = value; } }

        /// <summary></summary>
        public double fSlopeDegrees { get { return FSlope; } set { FSlope = value; } }

        /// <summary></summary>
        public double fAspectDegrees { get { return FAspect; } set { FAspect = value; } }

        /// <summary></summary>
        public double fCO2_PPM { get { return FCO2_Conc; } }

        // Constants for daylength & extraterrestrial radiation calculations

        /// <summary>Horizon angles</summary>
        public static double[] DAYANGLE = { 90.0 * GrazEnv.DEG2RAD, 90.833 * GrazEnv.DEG2RAD };

        /// <summary>Maximum declination of the sun (rad)</summary>
        public const double MAXDECLIN = 23.45 * GrazEnv.DEG2RAD;

        /// <summary>Day of year for maximum declination</summary>
        public const double DECLINBASE = 172;

        /// <summary>Solar constant (W/m^2)</summary>
        public const double SOLARCONST = 1367;

        /// <summary>Effect of eccentricity of Earth's orbit</summary>
        public const double ECCENTRICITY = 0.033;

        /// <summary>Earth's angular velocity (rad/hr)</summary>
        public const double OMEGA = 2 * Math.PI / 24;

        // Constants for the Parton & Logan model of diurnal T variation
        // Parton WJ & Logan JA (1981), Agric.Meteorol. 23:205-216

        /// <summary></summary>
        public const double PARTON_A = 1.86;

        /// <summary></summary>
        public const double PARTON_B = 2.20;

        /// <summary></summary>
        public const double PARTON_C = -0.17;

        // Constants used in evapotranspiration calculations

        /// <summary>Stefan - Boltzmann constant, MJ / m^2 / d / K^4</summary>
        public const double SIGMA = 4.903E-9;

        /// <summary>von Karman's constant</summary>
        public const double KARMAN = 0.41;

        /// <summary>Convert seconds to days</summary>
        public const double SEC2DAY = 1.0 / 86400.0;

        /// <summary>Molecular weight ratio H20:air</summary>
        public const double EPSILON = 0.622;

        /// <summary>Specific gas constant,     kJ/kg/K</summary>
        public const double GAS_CONST = 0.287;

        /// <summary>
        /// Default parameters for the fEvaporation method
        /// </summary>
        public double[,] EVAPDEFAULTS = {
                    {0.8, 0.0, 0.0 },                       // emPropnPan
                    { GrazEnv.HERBAGE_ALBEDO, 0.12, 70.0 }, // emPenman
                    { GrazEnv.HERBAGE_ALBEDO, 0.0, 0.0 },   // emCERES
                    { GrazEnv.HERBAGE_ALBEDO, 1.26, 0.0 },  // emPriestley
                    { GrazEnv.HERBAGE_ALBEDO, 1.0, 0.0 }    // emFAO
        };

        /// <summary>
        /// Fields required by the various evap calculation methods
        /// </summary>
        public TWeatherData[][] EVAP_REQUIREMENT =
        {
            new TWeatherData[] { TWeatherData.wdtEpan },                                                                                        // emPropnPan
            new TWeatherData[] { TWeatherData.wdtMaxT, TWeatherData.wdtMinT, TWeatherData.wdtRadn, TWeatherData.wdtVP, TWeatherData.wdtWind },  // emPenman
            new TWeatherData[] { TWeatherData.wdtMaxT, TWeatherData.wdtMinT, TWeatherData.wdtRadn },
            new TWeatherData[] { TWeatherData.wdtMaxT, TWeatherData.wdtMinT, TWeatherData.wdtRadn, TWeatherData.wdtVP },
            new TWeatherData[] { TWeatherData.wdtMaxT, TWeatherData.wdtMinT, TWeatherData.wdtRadn, TWeatherData.wdtVP, TWeatherData.wdtWind }
        };

        /// <summary>
        /// TWeatherHandler constructor
        /// </summary>
        public TWeatherHandler()
        {
            FCO2_Order = 0;
            FCO2_Coeffs[0] = GrazEnv.REFERENCE_CO2;
            FCO2_Conc = GrazEnv.REFERENCE_CO2;
        }

        /// <summary>
        /// Horizontal angle of the sun.  All three input angles are in radians
        /// </summary>
        /// <param name="fLat"></param>
        /// <param name="fDeclin"></param>
        /// <param name="fHorizAngle"></param>
        /// <returns></returns>
        private double HASFunc(double fLat, double fDeclin, double fHorizAngle)
        {
            double fCosine;
            double Result;

            fCosine = (Math.Cos(fHorizAngle) - Math.Sin(fLat) * Math.Sin(fDeclin)) / (Math.Cos(fLat) * Math.Cos(fDeclin));
            if (fCosine <= -1.0)
                Result = 12.0 * OMEGA;
            else if (fCosine >= 1.0)
                Result = 0.0;
            else
                Result = Math.Acos(fCosine);

            return Result;
        }


        /// <summary>
        /// Computes daylengths and E-T radiation
        /// </summary>
        /// <param name="iDOY"></param>
        /// <param name="fDL0"></param>
        /// <param name="fDLC"></param>
        /// <param name="fETR"></param>
        public void computeDLandETR(int iDOY, ref double fDL0, ref double fDLC, ref double fETR)
        {
            double fLatRad,
            fSlopeRad,
            fAspectRad,
            fDeclination,                                                                // Declination of the sun (rad)
            fSunRiseRad = 0,                                                             // These are in radians, not hours
            fSunSetRad = 0,
            fEquivLat,                                                                   // Latitude "equivalent" to the slope/aspect
            fHalfDay0,                                                                   // Half-daylength on flat surface (rad)
            fHalfDayE,                                                                   // Ditto  at "equivalent" latitude
            fAlpha,
            fDenom;


            fLatRad = GrazEnv.DEG2RAD * fLatDegrees;                                     // Convert latitude, slope and aspect to
            fSlopeRad = GrazEnv.DEG2RAD * fSlopeDegrees;                                 //   radians
            fAspectRad = GrazEnv.DEG2RAD * fAspectDegrees;

            fDeclination = MAXDECLIN * Math.Cos(GrazEnv.DAY2RAD * (iDOY - DECLINBASE));

            fDenom = Math.Cos(fSlopeRad) * Math.Cos(fLatRad)                             // Trap e.g. flat surface at equator
                      - Math.Cos(fAspectRad) * Math.Sin(fSlopeRad) * Math.Sin(fLatRad);
            if ((Math.Abs(fDenom) < 1.0E-8))
                fAlpha = Math.Sign(Math.Sin(fSlopeRad) * Math.Sin(fAspectRad)) * Math.PI / 2.0;
            else
                fAlpha = Math.Atan(Math.Sin(fSlopeRad) * Math.Sin(fAspectRad) / fDenom);

            fEquivLat = Math.Asin(Math.Sin(fSlopeRad) * Math.Cos(fAspectRad) * Math.Cos(fLatRad)         // Determine the "equivalent latitude"
                                 + Math.Cos(fSlopeRad) * Math.Sin(fLatRad));

            foreach (bool bCivil in new[] { true, false })                           // Do bCivil=FALSE last so that fSunRise
            {                                                                        //   and fSunSet are set for the E-T
                fHalfDay0 = HASFunc(fLatRad, fDeclination, DAYANGLE[Convert.ToInt32(bCivil)]);       //   radiation calculation
                fHalfDayE = HASFunc(fEquivLat, fDeclination, DAYANGLE[Convert.ToInt32(bCivil)]);
                fSunRiseRad = Math.Max(-fHalfDay0, -fHalfDayE - fAlpha);
                fSunSetRad = Math.Min(+fHalfDay0, +fHalfDayE - fAlpha);
                if (bCivil)
                    fDLC = (fSunSetRad - fSunRiseRad) / OMEGA;                                // Convert daylength to hours here
                else
                    fDL0 = (fSunSetRad - fSunRiseRad) / OMEGA;
            }

            fETR = SOLARCONST / GrazEnv.WM2_2_MJM2 / 24.0                                       // Extra-terrestrial radiation
                    * (1.0 + ECCENTRICITY * Math.Cos(GrazEnv.DAY2RAD * iDOY))                           //   calculation
                    * (fDL0 * Math.Sin(fDeclination) * Math.Sin(fEquivLat)
                        + (Math.Sin(fSunSetRad + fAlpha) - Math.Sin(fSunRiseRad + fAlpha)) / OMEGA
                          * Math.Cos(fDeclination) * Math.Cos(fEquivLat));
        }

        /// <summary>
        /// Daylength, in hours. Values are pre-computed when Today is set.
        /// </summary>
        /// <param name="bCivil"></param>
        /// <returns>If TRUE, the daylength value includes civil twilight.</returns>
        public double Daylength(bool bCivil = false)
        {
            return FDaylengths[Convert.ToInt32(bCivil)];
        }

        /// <summary>
        /// TRUE i.f.f. the daylength is increasing. The value is pre-computed when
        /// Today is set.
        /// </summary>
        /// <returns></returns>
        public bool DaylengthIncreasing()
        {
            return FDayLenIncr;
        }

        /// <summary>
        /// Extra-terrestrial radiation, in MJ/m^2/d. The value is pre-computed when
        /// Today is set.
        /// </summary>
        /// <returns></returns>
        public double ExtraT_Radiation()
        {
            return FET_Radn;
        }

        /// <summary>
        /// Mean daily temperature, taken as the average of maximum and minimum T
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public double MeanTemp()
        {
            if (!bInputKnown(new TWeatherData[] { TWeatherData.wdtMaxT, TWeatherData.wdtMinT }))
                throw new Exception("Weather handler: Mean temperature cannot be calculated");
            else
                return 0.5 * (getData(TWeatherData.wdtMaxT) + getData(TWeatherData.wdtMinT));
        }

        /// <summary>
        /// Equation for the mean temperature during daylight hours.  Integrated from a
        /// model of Parton and Logan (1981), Agric.Meteorol. 23:205
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public double MeanDayTemp()
        {
            double fDayLen;

            if (!bInputKnown(new TWeatherData[] { TWeatherData.wdtMaxT, TWeatherData.wdtMinT }))
                throw new Exception("Weather handler: Mean daytime temperature cannot be calculated");

            fDayLen = Daylength(true);
            double Result = getData(TWeatherData.wdtMinT) + (getData(TWeatherData.wdtMaxT) - getData(TWeatherData.wdtMinT))
                                       * (1.0 + 2.0 * PARTON_A / fDayLen)
                                       * (Math.Cos(-PARTON_C / (fDayLen + 2.0 * PARTON_A) * Math.PI)
                                          - Math.Cos((fDayLen - PARTON_C) / (fDayLen + 2.0 * PARTON_A) * Math.PI))
                                       / Math.PI;
            return Result;
        }

        /// <summary>
        /// Returns TRUE i.f.f. the data value for the weather element has been assigned
        /// since Today was last set.
        /// </summary>
        /// <param name="Query"></param>
        /// <returns></returns>
        public bool bInputKnown(TWeatherData Query)
        {
            return FDataKnown[(int)Query];
        }

        /// <summary>
        /// Returns TRUE i.f.f. the data value for all members of Query has been assigned
        /// since Today was last set.
        /// </summary>
        /// <param name="Query"></param>
        /// <returns></returns>
        public bool bInputKnown(TWeatherData[] Query)
        {
            bool Result = true;
            for (int D = 0; D <= Query.Length - 1; D++)
                Result = Result && (FDataKnown[(int)Query[D]]);

            return Result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="iDay"></param>
        /// <param name="iMonth"></param>
        /// <param name="iYear"></param>
        public void setToday(int iDay, int iMonth, int iYear)
        {
            int Dt;         // StdDATE.Date;
            double fOldDayLen;
            double X, XN;
            int Idx;

            Dt = StdDate.DateVal(iDay, iMonth, iYear);

            if ((FToday == 0) || (StdDate.DateShift(FToday, 1, 0, 0) != Dt))
            {
                // Compute yesterday's day length where it isn't already known
                FToday = StdDate.DateShift(Dt, -1, 0, 0);
                computeDLandETR(StdDate.DOY(FToday, true), ref FDaylengths[0], ref FDaylengths[1], ref FET_Radn);   // 0 - false
            }

            FToday = Dt;
            var values = Enum.GetValues(typeof(TWeatherData)).Cast<TWeatherData>().ToArray();
            foreach (var D in values)
                FDataKnown[(int)D] = false;

            fOldDayLen = Daylength();
            computeDLandETR(StdDate.DOY(FToday, true), ref FDaylengths[0], ref FDaylengths[1], ref FET_Radn);
            FDayLenIncr = Daylength() > fOldDayLen;

            FCO2_Conc = FCO2_Coeffs[0];
            if (FCO2_Order > 0)
            {
                // Polynomial time course for [CO2]
                X = StdDate.Interval(StdDate.DateVal(1, 1, 2001), FToday) / 365.25; // Years since reference date
                XN = 1;
                for (Idx = 1; Idx <= FCO2_Order; Idx++)
                {
                    XN = XN * X;
                    FCO2_Conc = FCO2_Conc + FCO2_Coeffs[Idx] * XN;
                }
            }
        }

        /// <summary>
        /// Accessor function to FData
        /// </summary>
        /// <param name="D"></param>
        /// <returns></returns>
        private double getData(TWeatherData D)
        {
            return FData[(int)D];
        }

        /// <summary>
        /// Accessor function to FData
        /// </summary>
        /// <param name="D"></param>
        /// <param name="value"></param>
        private void setData(TWeatherData D, double value)
        {
            if (!double.IsNaN(value))
            {
                FData[(int)D] = value;
                FDataKnown[(int)D] = true;
            }
        }

        /// <summary>
        /// Calculate Potential evapotranspiration (mm H2O)
        /// </summary>
        /// <param name="albedo"></param>
        /// <returns>Potential ET. If input requirements are not met return MISSING</returns>
        public double PotentialET(double albedo)
        {
            double fUpperBound;
            double result = 0.0;

            EvapMethod evapMethod = EvapMethod.emPropnPan;  // **** default ****
            double evapParam = 0.8;

            if (!bInputKnown(EVAP_REQUIREMENT[(int)evapMethod]))
                result = StdMath.DMISSING;
            else if (evapMethod == EvapMethod.emPropnPan)
            {
                if (bInputKnown(new TWeatherData[] { TWeatherData.wdtMaxT, TWeatherData.wdtMinT, TWeatherData.wdtRadn }))
                    fUpperBound = Math.Max(4.0, 1.5 * Evaporation(EvapMethod.emCERES, GrazEnv.HERBAGE_ALBEDO));
                else
                    fUpperBound = 20.0;

                result = Math.Min(Evaporation(EvapMethod.emPropnPan, evapParam), fUpperBound);
            }
            else
                result = Evaporation(evapMethod, albedo);

            return result;
        }

        /// <summary>
        /// Evapotranspiration calculation using various methods.
        ///  *All methods return potential evaporation except pmPenman, which returns
        ///  the estimated evaporation rate for a nominated surface resistance.
        ///  *The meaning of param1, param2 and param3 depends upon the method:
        ///  Method Param.  Meaning Unit  Default
        ///  emPropnPan     1     potential: pan evaporation ratio - 0.8
        ///  emPenman       1     albedo - 0.23
        ///                 2     crop height                      m     0.12
        ///                 3     surface resistance               s/m   70.0
        ///  emCERES        1     albedo - 0.23
        ///  emPriestley    1     albedo - 0.23
        ///                 2     potential: equilibrium ratio     -1.26
        ///  emFAO          1     albedo - 0.23
        ///                 2     crop coefficient                 -1.0
        /// </summary>
        /// <param name="method"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <returns>Potential evaporation</returns>
        private double Evaporation(EvapMethod method, double param1 = 0.0, double param2 = 0.0, double param3 = 0.0)
        {
            if (param1 == 0.0)
                param1 = EVAPDEFAULTS[(int)method, 0];
            if (param2 == 0.0)
                param2 = EVAPDEFAULTS[(int)method, 1];
            if (param3 == 0.0)
                param3 = EVAPDEFAULTS[(int)method, 2];

            double result = 0;
            switch (method) {
                case EvapMethod.emPropnPan:
                    result = param1 * getData(TWeatherData.wdtEpan);
                    if (result >= 12.0)                                    // Large E(pan) values are untrustworthy
                        result = Math.Min(result, CeresPET(GrazEnv.HERBAGE_ALBEDO ));
                    break;
                case EvapMethod.emPenman: result = PenmanPET(param1, param2, param3);
                    break;
                case EvapMethod.emCERES: result = CeresPET(param1);
                    break;
                // TODO:
                /*case EvapMethod.emPriestley: result = PriestleyPET(param1, param2);
                      break;
                  case EvapMethod.emFAO: result = param2 * FAO_ReferenceET(param1);
                      break;*/
                default: result = 0.0;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Potential evaporation estimator using the logic from the CERES suite of crop
        /// growth models. This estimator is a variant of the Priestley - Taylor estimator
        /// </summary>
        /// <param name="albedo"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private double CeresPET(double albedo)
        {
            double fMeanT;
            double fEEQ;
            double fEEQ_FAC;

            if (!bInputKnown(EVAP_REQUIREMENT[(int)EvapMethod.emCERES]))
                throw new Exception("Weather handler: CERES evaporation cannot be calculated");

            double maxt = getData(TWeatherData.wdtMaxT);
            fMeanT = 0.6 * maxt + 0.4 * getData(TWeatherData.wdtMinT);
            fEEQ = getData(TWeatherData.wdtRadn) * 23.8846 * (0.000204 - 0.000183 * albedo) * (29.0 + fMeanT);
            if (maxt >= 5.0)
                fEEQ_FAC = 1.1 + 0.05 * Math.Max(0.0, maxt - 35.0);
            else
                fEEQ_FAC = 0.01 * Math.Exp(0.18 * (maxt + 20.0));

            return fEEQ_FAC * fEEQ;
        }
#pragma warning disable IDE0060
        private const double MEASURE_HEIGHT = 2.0;                                                        // Assumed height of measurement(m)
        private double PenmanPET(double albedo, double cropHeight, double surfaceResist)
        {
#pragma warning restore IDE0060
            double fDelta;
            double fGamma;
            double fVirtualT;                                                 // "Virtual temperature" for gas law, K
            double fRho_Cp;                                                 // Density x specific heat of air, MJ/m^3/K

            double fSoilFlux;           // Soil heat flux G, MJ/ m ^ 2 / d
            double fZeroPlaneDispl;     // Zero plane displacement d, m
            double fRoughLengthMom;     // Roughness length for momentum z(om), m
            double fRoughLengthVapour;  // Roughness length for heat & water vapour z(oh), m
            double fAeroResist;         // Aerodynamic resistance, s/ m and d/ m

            if (!bInputKnown(EVAP_REQUIREMENT[(int)EvapMethod.emPenman]))
                throw new  Exception( "Weather handler: Penman-Monteith evaporation cannot be calculated" );

            double meanTemp = MeanTemp();
            fDelta = SatVPSlope(meanTemp);
            fGamma = PsychrometricConst();

            fVirtualT = 1.01 * (meanTemp + GrazEnv.C_2_K);
            fRho_Cp = fGamma * EPSILON * LatentHeat(meanTemp) / (GAS_CONST * fVirtualT);

            fSoilFlux = 0.0;

            fZeroPlaneDispl = 2 / 3 * Math.Max(0.005, cropHeight);            // Set a floor of 5mm on the height used
            fRoughLengthMom = 0.123 * Math.Max(0.005, cropHeight);            //   in calculating the aerodynamic
            fRoughLengthVapour = 0.1 * Math.Max(0.005, cropHeight);            // resistance
            // Aerodynamic resistance in s / m
            fAeroResist = Math.Log((MEASURE_HEIGHT - fZeroPlaneDispl) / fRoughLengthMom
                            * (MEASURE_HEIGHT - fZeroPlaneDispl) / fRoughLengthVapour)
                            / (Math.Pow(KARMAN, 2) * getData(TWeatherData.wdtWind));

            fAeroResist = fAeroResist * SEC2DAY;                // Convert the resistances to d / m for
            surfaceResist = fAeroResist * SEC2DAY;             // consistency with other units

            double result = (fDelta * (NetRadiation(albedo) - fSoilFlux) + fRho_Cp * VP_Deficit() / fAeroResist)
                            / (fDelta + fGamma * (1.0 + surfaceResist / fAeroResist))
                            / LatentHeat(meanTemp);

            return result;
        }

        //Derivative of the SVP-temperature curve
        private double SatVPSlope(double temperature)
        {
            return 17.27 * 237.3 / Math.Pow(temperature + 237.3, 2) * SaturatedVP(temperature);
        }

        // Psychrometric constant, following Allen et al(1998).                        }
        // * The equation for atmospheric pressure is also taken from Allen et al.      }
        private double PsychrometricConst()
        {
            double fPressure;

            fPressure = 101.3 * Math.Pow(1.0 - 0.0065 * fElevationM / (20.0 + GrazEnv.C_2_K), 5.26); // Atmospheric pressure, in kPa
            return 6.65E-4 * fPressure;
        }

        // Latent heat of vaporization of water, in MJ/kg
        private double LatentHeat(double meanT)
        {
            return 2.501 - 0.002361 * meanT;
        }

        /// <summary>
        ///   Net radiation estimate.
        /// This calculation follows Allen et al (1998)
        /// </summary>
        /// <param name="albedo"></param>
        /// <returns></returns>
        private double NetRadiation(double albedo)
        {
            double fRadClearDay;
            double fRadFract;
            double fNetLongWave;

            fRadClearDay = (0.75 + 2.0e-5 * fElevationM) * ExtraT_Radiation();
            fRadFract = Math.Min(1.0, getData(TWeatherData.wdtRadn) / fRadClearDay);
            fNetLongWave = SIGMA * (Math.Pow(getData(TWeatherData.wdtMaxT) + GrazEnv.C_2_K, 4) + Math.Pow(getData(TWeatherData.wdtMinT) + GrazEnv.C_2_K, 4)) / 2.0
                            * (0.34 - 0.14 * Math.Sqrt( getData(TWeatherData.wdtVP) ))
                            * (1.35 * fRadFract - 0.35);

            return (1.0 - albedo) * getData(TWeatherData.wdtRadn) - fNetLongWave;
        }

        /// <summary>
        /// Saturated vapour pressure at a given temperature, in kPa.
        /// The equation is taken from Allen et al(1998), FAO Irrigation and Drainage
        /// Paper 56.
        /// </summary>
        /// <param name="temperature"></param>
        /// <returns></returns>
        private double SaturatedVP(double temperature)
        {
            return 0.6108 * Math.Exp(17.27 * temperature / (temperature + 237.3));
        }

        /// <summary>
        /// Daily average vapour pressure deficit
        /// * The weighting in the saturated VP calculation follows Jeffrey et al.
        /// (2001), Env.Modelling and Software 16:309
        /// * In the absence of VP or RH data, assumes that dew point can be approximated
        /// by the minimum temperature.
        /// </summary>
        /// <returns></returns>
        private double VP_Deficit()
        {
            double SVP;

            SVP = 0.75 * SaturatedVP(getData(TWeatherData.wdtMaxT)) + 0.25 * SaturatedVP(getData(TWeatherData.wdtMinT));

            if (!bInputKnown(TWeatherData.wdtVP))
                computeVP();
            return SVP - getData(TWeatherData.wdtVP);
        }

        /// <summary>
        /// Computes the daily average vapour pressure, in kPa, and stores it in
        /// Data[wdtVP]. Computation is done as follows:
        /// 1. If relative humidity is available, it is computed from that.
        /// 2. Otherwise, VP is computed by assuming that dew point = min. T.
        /// </summary>
        /// <returns></returns>
        private void computeVP()
        {
            double SVP;
            double dewPoint;

            if (bInputKnown(TWeatherData.wdtRelH))
            {
                SVP = 0.75 * SaturatedVP(getData(TWeatherData.wdtMaxT)) + 0.25 * SaturatedVP(getData(TWeatherData.wdtMinT));
                setData(TWeatherData.wdtVP, getData(TWeatherData.wdtRelH) * SVP);
            }
            else
            {
                dewPoint = getData(TWeatherData.wdtMinT);
                setData(TWeatherData.wdtVP, SaturatedVP(dewPoint));
            }
        }
    }
}
