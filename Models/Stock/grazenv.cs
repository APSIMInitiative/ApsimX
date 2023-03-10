namespace Models.GrazPlan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using StdUnits;
   // using static Models.GrazPlan.PastureUtil;

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
    /// Daily weather data, used as inputs
    /// </summary>
    public enum TWeatherData                                                  
    {
        wdtMaxT,                                                    //   Maximum air temperature     deg C   
        wdtMinT,                                                    //   Minimum air temperature     deg C   
        wdtRain,                                                    //   Rainfall                    mm      
        wdtSnow,                                                    //   Snow (rain equivalents)     mm      
        wdtRadn,                                                    //   Solar radiation             MJ/m^2/d
        wdtVP,                                                      //   Actual vapour pressure      kPa     
        wdtWind,                                                    //   Average windspeed           m/s     
        wdtEpan,                                                    //   Pan evaporation             mm      
        wdtRelH,                                                    //   Relative humidity           0-1     
        wdtSunH,                                                    //   Hours of bright sunshine    hr      
        wdtTrMT                                                     //   Terrestrial min. temperature deg C  
    };
   

    /// <summary>
    /// Class TWeatherHandler
    /// </summary>
    public class TWeatherHandler
    {
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
        
        protected int FToday;      // StdDATE.Date;

        public double this[TWeatherData D]
        { get { return getData(D); } set { setData(D, value); } }

        public double fLatDegrees { get { return FLatitude; } set { FLatitude = value; } }
        public double fLongDegrees { get { return FLongitude; } set { FLongitude = value; } }
        public double fElevationM { get { return FElevation; } set { FElevation = value; } }
        public double fSlopeDegrees { get { return FSlope; } set { FSlope = value; } }
        public double fAspectDegrees { get { return FAspect; } set { FAspect = value; } }
        public double fCO2_PPM { get { return FCO2_Conc; } }

        // Constants for daylength & extraterrestrial radiation calculations            
      
        public static double[] DAYANGLE = { 90.0 * GrazEnv.DEG2RAD, 90.833 * GrazEnv.DEG2RAD };     // Horizon angles                        
        public const double MAXDECLIN = 23.45 * GrazEnv.DEG2RAD;                                    // Maximum declination of the sun (rad)  
        public const double DECLINBASE = 172;                                                       // Day of year for maximum declination   

        //  SOLARCONST   = 1360;                                                                    // Solar constant (W/m^2)                
        //  ECCENTRICITY = 0.035;                                                                   // Effect of eccentricity of Earth's orbit
        public const double SOLARCONST = 1367;                                                      // Solar constant (W/m^2)                
        public const double ECCENTRICITY = 0.033;                                                   // Effect of eccentricity of Earth's orbit
        public const double OMEGA = 2 * Math.PI / 24;                                               // Earth's angular velocity (rad/hr)     

        // Constants for the Parton & Logan model of diurnal T variation                
        // Parton WJ & Logan JA (1981), Agric.Meteorol. 23:205-216                      

        public const double PARTON_A = 1.86;
        public const double PARTON_B = 2.20;
        public const double PARTON_C = -0.17;

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
        public double fDaylength(bool bCivil = false)
        {
            return FDaylengths[Convert.ToInt32(bCivil)];
        }

        /// <summary>
        /// TRUE i.f.f. the daylength is increasing. The value is pre-computed when      
        /// Today is set.
        /// </summary>
        /// <returns></returns>
        public bool bDaylengthIncreasing()
        {
            return FDayLenIncr;
        }

        /// <summary>
        /// Extra-terrestrial radiation, in MJ/m^2/d. The value is pre-computed when     
        /// Today is set.                                                                
        /// </summary>
        /// <returns></returns>
        public double fExtraT_Radiation()
        {
            return FET_Radn;
        }
  
        /// <summary>
        /// Mean daily temperature, taken as the average of maximum and minimum T         
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public double fMeanTemp()
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
        public double fMeanDayTemp()
        {
            double fDayLen;

            if (!bInputKnown(new TWeatherData[] { TWeatherData.wdtMaxT, TWeatherData.wdtMinT }))
                throw new Exception("Weather handler: Mean daytime temperature cannot be calculated");

            fDayLen = fDaylength(true);
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

        public void setToday(int iDay, int iMonth, int iYear)
        {
            int Dt;         // StdDATE.Date;
            double fOldDayLen;
            double X, XN;
            int Idx;

            Dt = StdDate.DateVal(iDay, iMonth, iYear);

            if ((FToday == 0) || (StdDate.DateShift(FToday, 1, 0, 0) != Dt))                      // Compute yesterday's day length where  
            {                                                                        // it isn't already known              
                FToday = StdDate.DateShift(Dt, -1, 0, 0);
                computeDLandETR(StdDate.DOY(FToday, true), ref FDaylengths[0], ref FDaylengths[1], ref FET_Radn);   // 0 - false
            }

            FToday = Dt; 
            var values = Enum.GetValues(typeof(TWeatherData)).Cast<TWeatherData>().ToArray();
            foreach (var D in values)
                FDataKnown[(int)D] = false;

            fOldDayLen = fDaylength();
            computeDLandETR(StdDate.DOY(FToday, true), ref FDaylengths[0], ref FDaylengths[1], ref FET_Radn);
            FDayLenIncr = fDaylength() > fOldDayLen;

            FCO2_Conc = FCO2_Coeffs[0];
            if (FCO2_Order > 0)                                                      // Polynomial time course for [CO2]      
            {
                X = StdDate.Interval(StdDate.DateVal(1, 1, 2001), FToday) / 365.25; // Years since reference date            
                XN = 1;
                for (Idx = 1; Idx <= FCO2_Order; Idx++)
                {
                    XN = XN * X;
                    FCO2_Conc = FCO2_Conc + FCO2_Coeffs[Idx] * XN;
                }
            }
        }

        public double getData(TWeatherData D)
        {
            return FData[(int)D];
        }

        public void setData(TWeatherData D, double fValue)
        {
            FData[(int)D] = fValue;
            FDataKnown[(int)D] = true;
        }

    }
}
