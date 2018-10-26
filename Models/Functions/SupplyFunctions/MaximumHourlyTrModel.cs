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
using Models.PMF;

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
        /// <summary>The weather data</summary>
        [Link]
        private IWeather myWeather = null;

        /// <summary>The Clock</summary>
        [Link]
        private Clock myClock = null;

        /// <summary>The Leaf organ</summary>
        [Link]
        private Plant myPlant = null;

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

        /// <summary>The daily radiation intercepted by crop canopy</summary>
        [Link]
        private IFunction RadnInt = null;

        /// <summary>The mean temperature impact on RUE</summary>
        [Link]
        [ChildLinkByName(IsOptional = true)]
        private IFunction FT = null;

        /// <summary>The daily gross assimilate calculated by a another photosynthesis model</summary>
        [ChildLinkByName(IsOptional = true)]
        [Link]
        private IFunction GrossDailyAssimilate = null; 
        //------------------------------------------------------------------------------------------------

        /// <summary>The maximum hourlyVPD when hourly transpiration rate cease to further increase</summary>
        [Description("Maximum hourly VPD when hourly transpiration rate cease to further increase (kPa)")]
        [Bounds(Lower = 0.1, Upper = 1000)]
        [Units("kPa")]
        public double MaxVPD { get; set; } = 999;

        /// <summary>The maximum hourly transpiration rate</summary>
        [Description("Maximum hourly transpiration rate (mm/hr)")]
        [Bounds(Lower = 0.01, Upper = 1000.0)]
        [Units("mm/hr")]
        public double MaxTr { get; set; } = 999;
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
        private List<double> hourlyRUE;
        private List<double> hourlyPotTr;
        private List<double> hourlyVPDCappedTr;
        private List<double> hourlyTrCappedTr;
        private List<double> hourlyTr;
        private List<double> hourlyPotDM;
        private List<double> hourlyDM;

        private XYPairs TempResponseFunc = new XYPairs();

        /// <summary>Total daily assimilation in g/m2</summary>
        public double DailyDM { get; set; }
        //------------------------------------------------------------------------------------------------

        /// <summary>Daily growth increment of total plant biomass</summary>
        /// <returns>g dry matter/m2 soil/day</returns>
        public double Value(int arrayIndex = -1)
        {
            return DailyDM;
        }
        //------------------------------------------------------------------------------------------------

        /// <summary>Daily growth increment of total plant biomass</summary>
        /// <returns>g dry matter/m2 soil/day</returns>
        [EventSubscribe("DoUpdateWaterDemand")]
        private void Calculate(object sender, EventArgs e)
        {
            double radiationInterception = RadnInt.Value();
            if (Double.IsNaN(radiationInterception))
                throw new Exception("NaN daily interception value supplied to RUE model");
            if (radiationInterception < 0)
                throw new Exception("Negative daily interception value supplied to RUE model");

            if (myPlant.IsEmerged && radiationInterception > 0)
            {
                transpEffCoef = TEC.Value() * 1e3;

                CalcTemperature();
                CalcRadiation();
                CalcSVP();
                CalcVPD();
                CalcRUE();
                CalcPotTr();
                CalcVPDCappedTr();
                CalcTrCappedTr();
                CalcSoilLimitedTr();
                CalcBiomass();
                myLeaf.PotentialEP = hourlyTrCappedTr.Sum();
                myLeaf.WaterDemand = hourlyTr.Sum();
            }
            else
            {
                hourlyDM = new List<double>();
                for (int i = 0; i < 24; i++) hourlyDM.Add(0.0);
                myLeaf.PotentialEP = 0;
                myLeaf.WaterDemand = 0;
                DailyDM = 0;
            }
        }
        //------------------------------------------------------------------------------------------------

        private void CalcTemperature()
        {
            hourlyTemp = new List<double>();
            // A Model for Diurnal Variation in mySoil and Air hourlyTemp
            // William J. Parton and Jesse A. Logan : Agricultural Meteorology, 23 (1991) 205-216.
            // Developed by Greg McLean and adapted by Behnam (Ben) Ababaei.

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
            // Calculates the ground solar incident radiation per hour and scales it to the actual radiation
            // Developed by Greg McLean and adapted by Behnam (Ben) Ababaei.

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
            // Calculates hourlySVP at the air temperature in hPa
            hourlySVP = new List<double>();
            for (int i = 0; i < 24; i++)
                hourlySVP.Add(MetUtilities.svp(hourlyTemp[i]));
        }
        //------------------------------------------------------------------------------------------------

        private void CalcVPD()
        {
            // Calculates hourlyVPD at the air temperature in kPa
            hourlyVPD = new List<double>();
            for (int i = 0; i < 24; i++) hourlyVPD.Add(0.1 * (MetUtilities.svp(hourlyTemp[i]) - MetUtilities.svp(myWeather.MinT)));
        }
        //------------------------------------------------------------------------------------------------

        private void CalcRUE()
        {
            // Calculates hourlyRUE as the product of reference RUE, temperature, N and CO2.
            // Hourly total plant "actual" radiation use efficiency corrected by reducing factors
            // (g biomass / MJ global solar radiation)
            double rueReductionFactor = 1;
            double tempResponse = 1;
            hourlyRUE = new List<double>();

            TempResponseFunc.X = new double[4] { 0, 15, 25, 35 };
            TempResponseFunc.Y= new double[4] { 0, 1, 1, 0 };

            for (int i = 0; i < 24; i++)
            {
                if (FT != null)
                    tempResponse = FT.Value();
                else
                    tempResponse = TempResponseFunc.ValueIndexed(hourlyTemp[i]);

                rueReductionFactor = Math.Min(tempResponse, FN.Value()) * FCO2.Value();
                hourlyRUE.Add(RUE.Value() * rueReductionFactor);
            }
        }
        //------------------------------------------------------------------------------------------------

        private void CalcPotTr()
        {
            // Calculates hourlyPotTr as the product of hourlyRUE, hourlyVPD and transpEffCoef
            hourlyPotTr = new List<double>();
            for (int i = 0; i < 24; i++)
                hourlyPotTr.Add(hourlyRad[i] * hourlyRUE[i] * hourlyVPD[i] / transpEffCoef);
        }
        //------------------------------------------------------------------------------------------------

        private void CalcVPDCappedTr()
        {
            // Calculates hourlyVPDCappedTr as the product of hourlyRUE, Math.Min(hourlyVPD, MaxVPD) and transpEffCoef
            hourlyVPDCappedTr = new List<double>();
            for (int i = 0; i < 24; i++)
                hourlyVPDCappedTr.Add(hourlyRad[i] * hourlyRUE[i] * Math.Min(hourlyVPD[i], MaxVPD) / transpEffCoef);
        }
        //------------------------------------------------------------------------------------------------

        private void CalcTrCappedTr()
        {
            // Calculates hourlyTrCappedTr by capping hourlyVPDCappedTr at MaxTr
            hourlyTrCappedTr = new List<double>();
            foreach (double hDemand in hourlyVPDCappedTr)
                hourlyTrCappedTr.Add(Math.Min(MaxTr, hDemand));
        }
        //------------------------------------------------------------------------------------------------

        private void CalcSoilLimitedTr()
        {
            // Sets hourlyTr = hourlyVPDCappedTr and scales value at each hour until sum of hourlyTr = rootWaterSupp
            hourlyTr = new List<double>(hourlyTrCappedTr);

            double rootWaterSupp = myRoot.TotalExtractableWater();
            double reduction = 0.99;
            double maxHourlyT = hourlyTr.Max();

            if (rootWaterSupp < 1e-5)
                for (int i = 0; i < 24; i++) hourlyTr[i] = 0;
            else
            {
                while (hourlyTr.Sum() - rootWaterSupp > 1e-5)
                {
                    maxHourlyT *= reduction;
                    for (int i = 0; i < 24; i++)
                    {
                        double diff = Math.Max(0, hourlyTr.Sum() - rootWaterSupp);
                        if (hourlyTr[i] >= maxHourlyT) hourlyTr[i] = Math.Max(maxHourlyT, hourlyTr[i] - diff);
                        if (hourlyTr.Sum() - rootWaterSupp < 1e-5) break;
                    }
                }
            }

            //while (hourlyTr.Sum() - rootWaterSupp > 1e-5)
            //{
            //    for (int i = 23; i >= 0; i--)
            //    {
            //        double sumTR = 0;
            //        for (int j = 0; j < i; j++) sumTR += hourlyTr[j];
            //        double remTR = Math.Max(0.0, Math.Min(rootWaterSupp - sumTR, hourlyTr[i]));
            //        hourlyTr[i] = remTR;
            //        if (remTR > 0) break;
            //    }
            //}
        }
        //------------------------------------------------------------------------------------------------

        private void CalcBiomass()
        {
            // Calculates hourlyDM and hourlyPotDM as a product of RUE, TR, TEC and hourlyVPD.
            // If there is a link to a GrossDailyAssimilate model, it will be used and its outputs
            // will be scaled to follow the same durnal pattern as that of the hourly RUE model.
            // If no such a link exists, the 'net' daily assimilate is calculated.

            hourlyPotDM = new List<double>();
            for (int i = 0; i < 24; i++) hourlyPotDM.Add(hourlyPotTr[i] * transpEffCoef / hourlyVPD[i]);

            hourlyDM = new List<double>();
            for (int i = 0; i < 24; i++) hourlyDM.Add(hourlyTr[i] * transpEffCoef / hourlyVPD[i]);

            DailyDM = hourlyDM.Sum();
            double DailyPotDM = hourlyPotDM.Sum();

            if (GrossDailyAssimilate != null && hourlyPotDM.Sum() > 0 && hourlyDM.Sum() > 0)
            {
                double DailyDMGross = hourlyDM.Sum() / hourlyPotDM.Sum() * GrossDailyAssimilate.Value();
                if (double.IsNaN(GrossDailyAssimilate.Value()))
                    DailyDMGross = 0;

                if (double.IsNaN(DailyDMGross))
                    DailyDM = 0;
                else
                {
                    DailyDM = hourlyDM.Sum();
                    if (DailyDM > 0 && !double.IsNaN(DailyDMGross))
                        for (int i = 0; i < 24; i++) hourlyDM[i] = hourlyDM[i] / DailyDM * DailyDMGross;
                    else
                        for (int i = 0; i < 24; i++) hourlyDM[i] = 0;
                    DailyDM = hourlyDM.Sum();
                }
            }
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

        private double CalcSolarRadn(double ratio, double dayLR, double latR, double solarDec)
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