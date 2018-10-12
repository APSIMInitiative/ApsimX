using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;
using Models.PMF.Organs;

using Models.Soils;
using System.Linq;
using System.Threading.Tasks;

namespace Models.Functions.SupplyFunctions
{
    /// <summary>
    /// # [Name]
    /// Biomass accumulation is modeled as the product of intercepted radiation and its conversion efficiency, the radiation use efficiency (RUE) ([Monteith1977]).  
    ///   This approach simulates net photosynthesis rather than providing separate estimates of growth and respiration.  
    ///   RUE is calculated from a potential value which is discounted using stress factors that account for plant nutrition (FN), air temperature (FT), vapour pressure deficit (FVPD), water hourlyTr (FW) and atmospheric CO<sub>2</sub> concentration (FCO2).  
    ///   NOTE: RUE in this model is expressed as g/MJ for a whole plant basis, including both above and below ground growth.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ILeaf))]
    public class MaximumHourlyTrModel : Model, IFunction
    {
        //[Input]
        //public NewMetType myWeather;
        /// <summary>The weather data</summary>
        [Link]
        private IWeather myWeather = null;

        /// <summary>The Clock</summary>
        [Link]
        private Clock myClock = null;

        /// <summary>The Leaf organ</summary>
        [Link]
        private Leaf myLeaf = null;

        /// <summary>The Root organ</summary>
        [Link]
        private Root myRoot = null;

        /// <summary>The radiation use efficiency</summary>
        [Link]
        [Description("hourlyRad use efficiency")]
        private IFunction RUE = null;

        /// <summary>The transpiration efficiency coefficient</summary>
        [Link]
        [Description("Transpiration efficiency coefficient")]
        [Units("kPa/gC/m^2/mm water")]
        private IFunction TEC = null;

        /// <summary>The CO2 impact on RUE</summary>
        [Link]
        private IFunction FCO2 = null;

        /// <summary>The N deficiency impact on RUE</summary>
        [Link]
        private IFunction FN = null;

        /// <summary>The mean temperature impact on RUE</summary>
        [Link]
        private IFunction FT = null;

        /// <summary>The daily radiation intercepted by crop canopy</summary>
        [Link]
        private IFunction RadnInt = null;
        //------------------------------------------------------------------------------------------------

        private double maxLag = 1.86;       // a, Greg=1.5
        private double nightCoef = 2.2;     // b, Greg=4.0
        private double minLag = -0.17;      // c, Greg=1.0
        private double latR;
        private double transpEffCoef;

        private List<double> hourlyTemp;
        private List<double> hourlyRad;
        private List<double> hourlySVP;
        private List<double> hourlyVPD;
        private List<double> hourlyVPDCappedTr;
        private List<double> hourlyTrCappedTr;
        private List<double> hourlyTr;
        private List<double> hourlyDM;
        //------------------------------------------------------------------------------------------------

        /// <summary>The maximum hourlyVPD when hourly transpiration rate cease to further increase</summary>
        [Description("Maximum hourly VPD when hourly transpiration rate cease to further increase")]
        [Bounds(Lower = 0.1, Upper = 1000)]
        [Units("kPa")]
        public double MaxVPD { get; set; } = 999;

        /// <summary>The maximum hourly transpiration rate</summary>
        [Description("Maximum hourly transpiration rate")]
        [Bounds(Lower = 0.01, Upper = 1000.0)]
        [Units("mm/hr")]
        public double MaxTr { get; set; } = 999;
        //------------------------------------------------------------------------------------------------

        /// <summary>Daily growth increment of total plant biomass</summary>
        /// <returns>g dry matter/m2 soil/day</returns>
        public double Value(int arrayIndex = -1)
        {
            double radiationInterception = RadnInt.Value(arrayIndex);
            if (Double.IsNaN(radiationInterception))
                throw new Exception("NaN hourlyRad interception value supplied to RUE model");
            if (radiationInterception < 0)
                throw new Exception("Negative hourlyRad interception value supplied to RUE model");

            if (radiationInterception > 0)
            {
                transpEffCoef = TEC.Value() * 1e3;

                CalcTemperature();
                CalcRadiation();
                CalcSVP();
                CalcVPD();
                CalcVPDCappedTr();
                CalcTrCappedTr();
                CalcSoilLimitedTr();
                CalcBiomass();
                myLeaf.PotentialEP = hourlyTr.Sum();
            }
            else
            {
                hourlyDM = new List<double>();
                for (int i = 0; i < 24; i++) hourlyDM.Add(0.0);
                myLeaf.PotentialEP = 0;
            }
            return hourlyDM.Sum();
        }
        //------------------------------------------------------------------------------------------------

        /// <summary>Total plant "actual" radiation use efficiency (for the day) corrected by reducing factors (g biomass/MJ global solar radiation) CHCK-EIT</summary>
        /// <value>The rue act.</value>
        [Units("gDM/MJ")]
        public double RueAct
        {
            get
            {
                double RueReductionFactor = Math.Min(FT.Value(), FN.Value()) * FCO2.Value();
                return RUE.Value() * RueReductionFactor;
            }
        }
        //------------------------------------------------------------------------------------------------

        private void CalcTemperature()
        {
            hourlyTemp = new List<double>();
            // A Model for Diurnal Variation in mySoil and Air hourlyTemp
            // William J. Parton and Jesse A. Logan : Agricultural Meteorology, 23 (1991) 205-216
            // with corrections

            latR = Math.PI / 180.0 * myWeather.Latitude;      // convert latitude (degrees) to radians

            double SolarDec = CalcSolarDeclination(myClock.Today.DayOfYear);
            double DayLR = CalcDayLength(latR, SolarDec);                   // day length (radians)
            double DayL = (2.0 / 15.0 * DayLR) * (180 / Math.PI);           // day length (hours)
            double nightL = (24.0 - DayL);                                  // night length, hours
                                                                            // determine if the hour is during the day or night
            double sunrise = 12.0 - DayL / 2.0 + minLag;                    // corrected dawn
            double sunset = 12.0 + DayL / 2.0;                              // sundown

            for (int hr = 1; hr <= 24; hr++)
            {
                double tempr;
                if (hr >= sunrise && hr < sunset)  //day
                {
                    double m = 0; // the number of hours after the minimum temperature occurs
                    m = hr - sunrise;
                    tempr = (myWeather.MaxT - myWeather.MinT) * Math.Sin((Math.PI * m) / (DayL + 2 * maxLag)) + myWeather.MinT;
                }
                else  // night
                {
                    double n = 0;                       // the number of hours after sunset
                    if (hr > sunset) n = hr - sunset;
                    if (hr < sunrise) n = (24.0 - sunset) + hr;
                    double ddy = DayL - minLag;         // time of sunset after minimum temperature occurs
                    double tsn = (myWeather.MaxT - myWeather.MinT) * Math.Sin((Math.PI * ddy) / (DayL + 2 * maxLag)) + myWeather.MinT;
                    tempr = myWeather.MinT + (tsn - myWeather.MinT) * Math.Exp(-nightCoef * n / nightL);
                }
                hourlyTemp.Add(tempr);
            }
        }
        //------------------------------------------------------------------------------------------------

        private void CalcRadiation()
        {
            // calculates the ground solar incident radiation per hour and scales it to the actual radiation
            hourlyRad = new List<double>();
            for (int i = 0; i < 24; i++) hourlyRad.Add(0.0);

            // some constants
            double RATIO = 0.75; // Hammer, Wright (Aust. J. Agric. Res., 1994, 45)

            // some calculations
            double SolarDec = CalcSolarDeclination(myClock.Today.DayOfYear);
            double DayLR = CalcDayLength(latR, SolarDec);                   // day length (radians)
            double DayL = (2.0 / 15.0 * DayLR) * (180 / Math.PI);           // day length (hours)
            double Solar = CalcSolarRadn(RATIO, DayLR, latR, SolarDec);     // solar radiation

            //do the radiation calculation zeroing at dawn
            double DuskDawnFract = (DayL - Math.Floor(DayL)) / 2; // the remainder part of the hour at dusk and dawn
            double DawnTime = 12 - (DayL / 2);

            //The first partial hour
            hourlyRad[Convert.ToInt32(Math.Floor(DawnTime))] += GlobalRadiation(DuskDawnFract / DayL, latR, SolarDec, DayL, Solar) * 3600 * DuskDawnFract;

            //Add the next lot
            int iDayLH = Convert.ToInt32(Math.Floor(DayL));

            for (int i = 0; i < iDayLH - 1; i++)
                hourlyRad[(int)DawnTime + i + 1] += GlobalRadiation(DuskDawnFract / DayL + (double)(i + 1) * 1.0 / (double)(int)DayL, latR, SolarDec, DayL, Solar) * 3600;

            //Add the last one
            hourlyRad[(int)DawnTime + (int)DayL + 1] += (GlobalRadiation(1, latR, SolarDec, DayL, Solar) * 3600 * DuskDawnFract);

            // scale to today's radiation
            double TotalRad = hourlyRad.Sum();
            for (int i = 0; i < 24; i++) hourlyRad[i] = hourlyRad[i] / TotalRad * RadnInt.Value();
        }
        //------------------------------------------------------------------------------------------------

        private void CalcSVP()
        {
            hourlySVP = new List<double>();
            // calculates hourlySVP at the air temperature in hPa
            for (int i = 0; i < 24; i++)
                hourlySVP.Add(MetUtilities.svp(hourlyTemp[i]));
        }
        //------------------------------------------------------------------------------------------------

        private void CalcVPD()
        {
            hourlyVPD = new List<double>(); // in kPa
            for (int i = 0; i < 24; i++) hourlyVPD.Add(0.1 * (MetUtilities.svp(hourlyTemp[i]) - MetUtilities.svp(myWeather.MinT)));
        }
        //------------------------------------------------------------------------------------------------

        private void CalcVPDCappedTr()
        {
            hourlyVPDCappedTr = new List<double>();
            for (int i = 0; i < 24; i++)
                hourlyVPDCappedTr.Add((hourlyRad[i] * RueAct) * Math.Min(hourlyVPD[i], MaxVPD) / transpEffCoef);
        }
        //------------------------------------------------------------------------------------------------

        private void CalcTrCappedTr()
        {
            hourlyTrCappedTr = new List<double>();
            foreach (double hDemand in hourlyVPDCappedTr) hourlyTrCappedTr.Add(Math.Min(MaxTr, hDemand));
        }
        //------------------------------------------------------------------------------------------------

        private void CalcSoilLimitedTr()
        {
            // set hourlyTr = hourlyVPDCappedTr (hourlyTr becomes uptake)
            // change to scaling each hour until sum of hourlyTr = dailySupply
            hourlyTr = new List<double>(hourlyTrCappedTr);
            
            while (hourlyTr.Sum() > myRoot.TotalExtractableWater)
            {
                for (int i = 23; i >= 0; i--)
                {
                    double sumTR = 0;
                    for (int j = 0; j < i; j++) sumTR += hourlyTr[j];
                    double remTR = Math.Max(0.0, Math.Min(myRoot.TotalExtractableWater - sumTR, hourlyTr[i]));
                    hourlyTr[i] = remTR;
                    if (remTR > 0) break;
                }
            }

            //double reduction = 0.99;
            //double maxHourlyT = hourlyTr.Max();

            //while (hourlyTr.Sum() > myRoot.TotalExtractableWater)
            //{
            //    maxHourlyT *= reduction;
            //    for (int i = 0; i < 24; i++)
            //        if (hourlyTr[i] >= maxHourlyT)
            //        {
            //            if (myRoot.TotalExtractableWater == 0)
            //                hourlyTr[i] = 0;
            //            else
            //                hourlyTr[i] = maxHourlyT;
            //        }
            //}
        }
        //------------------------------------------------------------------------------------------------

        private void CalcBiomass()
        {
            hourlyDM = new List<double>();
            for (int i = 0; i < 24; i++) hourlyDM.Add(hourlyTr[i] * transpEffCoef / hourlyVPD[i]);
        }
        //------------------------------------------------------------------------------------------------

        private double CalcSolarDeclination(int doy)
        {
            return (23.45 * (Math.PI / 180)) * Math.Sin(2 * Math.PI * (284.0 + doy) / 365.0);
        }
        //------------------------------------------------------------------------------------------------

        private double CalcDayLength(double latR, double solarDec)
        {
            return Math.Acos(-Math.Tan(latR) * Math.Tan(solarDec));
        }
        //------------------------------------------------------------------------------------------------

        private double CalcSolarRadn(double ratio, double dayLR, double latR, double solarDec) // solar radiation
        {
            return (24.0 * 3600.0 * 1360.0 * (dayLR * Math.Sin(latR) * Math.Sin(solarDec) +
                     Math.Cos(latR) * Math.Cos(solarDec) * Math.Sin(dayLR)) / (Math.PI * 1000000.0)) * ratio;
        }
        //------------------------------------------------------------------------------------------------

        private double GlobalRadiation(double oTime, double latitude, double solarDec, double dayLH, double solar)
        {
            double Alpha = Math.Asin(Math.Sin(latitude) * Math.Sin(solarDec) +
                  Math.Cos(latitude) * Math.Cos(solarDec) * Math.Cos((Math.PI / 12.0) * dayLH * (oTime - 0.5)));
            double ITot = solar * (1.0 + Math.Sin(2.0 * Math.PI * oTime + 1.5 * Math.PI)) / (dayLH * 60.0 * 60.0);
            double IDiff = 0.17 * 1360.0 * Math.Sin(Alpha) / 1000000.0;
            if (IDiff > ITot) IDiff = ITot;
            return ITot - IDiff;
        }
        //------------------------------------------------------------------------------------------------       
    }
}