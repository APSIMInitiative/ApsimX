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
        private Weather myWeather = null;

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

        /// <summary>The daily gross assimilate calculated by a another photosynthesis model</summary>
        [Link(IsOptional = true)]
        private IFunction GrossDailyAssimilate = null;

        /// <summary>The mean temperature impact on RUE</summary>
        [Link(IsOptional = true)]
        private IFunction FT = null;
        //------------------------------------------------------------------------------------------------

        /// <summary>The photosynthesis type (net/gross)</summary>
        [Description("The photosynthesis type (net or gross)")]
        public string Type { get; set; } = "net";

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

        /// <summary>The KDIF</summary>
        [Description("The multiplier by which the 'net' TEC must be multiplied for gross assimilate")]
        public double TECIncForGross { get; set; } = 1.2;

        /// <summary>The KDIF</summary>
        [Description("The weight of maximim temperature for calculating mean temperature (negative to use hourly data)")]
        public double MaximumTempWeight { get; set; } = -1;

        /// <summary>The KDIF</summary>
        [Description("KDIF")]
        public double KDIF { get; set; } = 0.7;

        /// <summary>LUE at low light at 340ppm and 20C </summary>
        [Description("LUE at low light at 340ppm and 20C (kgCO2/ha/h / J/m2/s)")]
        public double LUEref { get; set; } = 0.6;

        /// <summary>Leaf gross photosynthesis rate at 340ppm CO2 </summary>
        [Description("Maximum gross photosynthesis rate Pmax (kgCO2/ha/h)")]
        public double Pgmmax { get; set; } = 45;

        /// <summary>Photosynthesis pathway (C3/C4)</summary>
        [Description("Photosynthesis pathway C3/C4")]
        public string pathway { get; set; } = "C3";
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
        private List<double> hourlyPotB; 
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
                if (!string.Equals(Type, "net"))
                    transpEffCoef *= TECIncForGross;

                CalcTemperature();
                CalcRadiation();
                CalcSVP();
                CalcVPD();
                CalcRUE();
                CalcPotAssimilate();
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
            TempResponseFunc.Y = new double[4] { 0, 1, 1, 0 };

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

        private void CalcPotAssimilate()
        {
            // Calculates hourlyPotTr as the product of hourlyRUE, hourlyVPD and transpEffCoef
            hourlyPotB = new List<double>();

            if (string.Equals(Type, "net"))
            {
                for (int i = 0; i < 24; i++) hourlyPotB.Add(hourlyRad[i] * hourlyRUE[i]);
            }
            else
            {
                hourlyPotB = DailyGrossPhotosythesis(myPlant.Canopy.LAI, myWeather.Latitude,
                                                      myClock.Today.DayOfYear, myWeather.Radn,
                                                      myWeather.MaxT, myWeather.MinT,
                                                      myWeather.CO2, myWeather.DiffuseFraction, 1.0);
            }
        }
        //------------------------------------------------------------------------------------------------

        private void CalcPotTr()
        {
            // Calculates hourlyPotTr as the product of hourlyRUE, hourlyVPD and transpEffCoef
            hourlyPotTr = new List<double>();
            for (int i = 0; i < 24; i++) hourlyPotTr.Add(hourlyPotB[i] * hourlyVPD[i] / transpEffCoef);
        }
        //------------------------------------------------------------------------------------------------

        private void CalcVPDCappedTr()
        {
            // Calculates hourlyVPDCappedTr as the product of hourlyRUE, Math.Min(hourlyVPD, MaxVPD) and transpEffCoef
            hourlyVPDCappedTr = new List<double>();
            for (int i = 0; i < 24; i++) hourlyVPDCappedTr.Add(hourlyPotB[i] * Math.Min(hourlyVPD[i], MaxVPD) / transpEffCoef);
        }
        //------------------------------------------------------------------------------------------------

        private void CalcTrCappedTr()
        {
            // Calculates hourlyTrCappedTr by capping hourlyVPDCappedTr at MaxTr
            hourlyTrCappedTr = new List<double>();
            foreach (double hDemand in hourlyVPDCappedTr) hourlyTrCappedTr.Add(Math.Min(MaxTr, hDemand));
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

        private List<double> DailyGrossPhotosythesis(double LAI, double Latitude, int Day, double Radn, double Tmax,
            double Tmin, double CO2, double DifFr, double Fact)
        {
            List<double> HourlyGrossB = new List<double>(24);
            double GrossPs, Temp, PAR, PARDIF, PARDIR;

            //double GlobalRadiation, SinHeight, Dayl, AtmTrans, DifFr;
            double GlobalRadiation, SinHeight, Dayl, AtmTrans;
            double Dec, Sin, Cos, Rsc, SolarConst, DailySin, DailySinE, Hour, RadExt;
            double LUE, PgMax;

            //double[] xGauss = { 0.0469101, 0.2307534, 0.5000000, 0.7692465, 0.9530899 };
            //double[] wGauss = { 0.1184635, 0.2393144, 0.2844444, 0.2393144, 0.1184635 };
            List<double> wGauss = new List<double>(24);
            double PI = Math.PI;
            double RAD = PI / 180.0;

            GlobalRadiation = (double)Radn * 1E6;    //J/m2.d

            //===========================================================================================
            //Dailenght, Solar constant and daily extraterrestrial radiation
            //===========================================================================================
            //Declination of the sun as function of Daynumber (vDay)

            Dec = -Math.Asin(Math.Sin(23.45 * RAD) * Math.Cos(2.0 * PI * ((double)Day + 10.0) / 365.0));

            //vSin, vCos and vRsc are intermediate variables
            Sin = Math.Sin(RAD * Latitude) * Math.Sin(Dec);
            Cos = Math.Cos(RAD * Latitude) * Math.Cos(Dec);
            Rsc = Sin / Cos;

            //Astronomical daylength (hr)
            Dayl = 12.0 * (1.0 + 2.0 * Math.Asin(Sin / Cos) / PI);

            double riseHour = 0.5 * (24 - Dayl);
            double setHour = riseHour + Dayl;            

            for (int t = 1; t <= 24; t++)
            {
                if (t < riseHour) wGauss.Add(0);
                else if (t > riseHour && t - 1 < riseHour) wGauss.Add(t - riseHour);
                else if (t >= riseHour && t < setHour) wGauss.Add(1);
                else if (t > setHour && t - 1 < setHour) wGauss.Add(1 - (t - setHour));
                else if (t > setHour) wGauss.Add(0);
            }

            //Sine of solar height(vDailySin), inegral of vDailySin(vDailySin) and integrel of vDailySin
            //with correction for lower atmospheric transmission at low solar elevations (vDailySinE)
            DailySin = 3600.0 * (Dayl * Sin + 24.0 * Cos * Math.Sqrt(1.0 - Rsc * Rsc) / PI);
            DailySinE = 3600.0 * (Dayl * (Sin + 0.4 * (Sin * Sin + Cos * Cos * 0.5))
                     + 12.0 * Cos * (2.0 + 3.0 * 0.4 * Sin) * Math.Sqrt(1.0 - Rsc * Rsc) / PI);

            //Solar constant(vSolarConst) and daily extraterrestrial (vRadExt)
            SolarConst = 1370.0 * (1.0 + 0.033 * Math.Cos(2.0 * PI * (double)Day / 365.0));   //J/m2.d
            RadExt = SolarConst * DailySin * 1E-6;               //MJ/m2.d

            //===========================================================================================
            //Daily photosynthesis
            //===========================================================================================

            for (int i = 0; i < 24; i++)
            {
                GrossPs = 0;
                Hour = i + 0.5;

                //Sine of solar elevation
                SinHeight = Math.Max(0.0, Sin + Cos * Math.Cos(2.0 * PI * (Hour + 12.0) / 24.0));
                //Diffuse light fraction (vDifFr) from atmospheric transmission (vAtmTrans)
                PAR = 0.5 * GlobalRadiation * SinHeight * (1.0 + 0.4 * SinHeight) / DailySinE;

                if (PAR>0)
                {
                    AtmTrans = PAR / (0.5 * SolarConst * SinHeight);

                    if (DifFr < 0)
                    {
                        if (AtmTrans <= 0.22)
                            DifFr = 1.0;
                        else
                        {
                            if ((AtmTrans > 0.22) && (AtmTrans <= 0.35))
                                DifFr = 1.0 - 6.4 * (AtmTrans - 0.22) * (AtmTrans - 0.22);
                            else
                                DifFr = 1.47 - 1.66 * AtmTrans;
                        }
                        DifFr = Math.Max(DifFr, 0.15 + 0.85 * (1.0 - Math.Exp(-0.1 / SinHeight)));
                    }

                    if (DifFr < 0 | DifFr > 1)
                        throw new Exception("Diffuse fraction should be between 0 and 1.");

                    //Diffuse PAR (PARDIF) and direct PAR (PARDIR)
                    PARDIF = Math.Min(PAR, SinHeight * DifFr * AtmTrans * 0.5 * SolarConst);
                    PARDIR = PAR - PARDIF;

                    if (MaximumTempWeight > 1)
                        throw new Exception("MaximumTempWeight fraction should be between 0 and 1, or negative to be ignored");

                    if (MaximumTempWeight>0)
                        Temp = MaximumTempWeight * myWeather.MaxT + (1 - MaximumTempWeight) * myWeather.MinT;
                    else
                        Temp = hourlyTemp[i];

                    //Light response parameters
                    PgMax = MaxGrossPhotosynthesis(CO2, Temp, Fact);
                    LUE = LightUseEfficiency(Temp, CO2);

                    //Canopy gross photosynthesis
                    GrossPs = HourlyGrossPhotosythesis(PgMax, LUE, LAI, Latitude, Day, Hour, PARDIR, PARDIF);
                    GrossPs *= wGauss[i];
                }

                HourlyGrossB.Add(GrossPs * 30 / 44 * 0.1);
            }
            return HourlyGrossB;
        }
        //------------------------------------------------------------------------------------------------

        private double HourlyGrossPhotosythesis(double fPgMax, double fLUE, double fLAI,
                              double fLatitude, int nDay, double fHour, double fPARdir, double fPARdif)
        {
            int i, j;
            double PMAX, EFF, vLAI, PARDIR, PARDIF, SINB;
            double SQV, REFH, REFS, CLUSTF, KDIRBL, KDIRT, FGROS, LAIC, VISDF, VIST, VISD, VISSHD, FGRSH;
            double FGRSUN, VISSUN, VISPP, FGRS, FSLLA, FGL, LAT, DAY, HOUR, DEC, vSin, vCos;
            int nGauss = 5;
            double[] xGauss = { 0.0469101, 0.2307534, 0.5000000, 0.7692465, 0.9530899 };
            double[] wGauss = { 0.1184635, 0.2393144, 0.2844444, 0.2393144, 0.1184635 };

            double PI = Math.PI;
            double RAD = PI / 180.0;

            double SCV = 0.20;    //Scattering coefficient of leaves for visible radiation (PAR)

            PMAX = fPgMax;
            EFF = fLUE;
            vLAI = fLAI;
            PARDIR = fPARdir;
            PARDIF = fPARdif;
            LAT = fLatitude;
            HOUR = fHour;
            DAY = nDay;

            //===========================================================================================
            //Sine of the solar height
            //===========================================================================================
            //Declination of the sun as function of Daynumber (vDay)
            DEC = -Math.Asin(Math.Sin(23.45 * RAD) * Math.Cos(2.0 * PI * (DAY + 10.0) / 365.0));

            //vSin, vCos and vRsc are intermediate variables
            vSin = Math.Sin(RAD * LAT) * Math.Sin(DEC);
            vCos = Math.Cos(RAD * LAT) * Math.Cos(DEC);

            SINB = Math.Max(0.0, vSin + vCos * Math.Cos(2.0 * PI * (HOUR + 12.0) / 24.0));

            //===========================================================================================
            //Reflection of horizontal and spherical leaf angle distribution
            SQV = Math.Sqrt(1.0 - SCV);
            REFH = (1.0 - SQV) / (1.0 + SQV);
            REFS = REFH * 2.0 / (1.0 + 2.0 * SINB);

            //Extinction coefficient for direct radiation and total direct flux
            CLUSTF = KDIF / (0.8 * SQV);
            KDIRBL = (0.5 / SINB) * CLUSTF;
            KDIRT = KDIRBL * SQV;

            //===========================================================================================
            //Selection of depth of canopy, canopy assimilation is set to zero
            FGROS = 0;
            for (i = 0; i < nGauss; i++)
            {
                LAIC = vLAI * xGauss[i];

                //Absorbed fluxes per unit leaf area: diffuse flux, total direct
                //flux, direct component of direct flux.
                VISDF = (1.0 - REFH) * PARDIF * KDIF * Math.Exp(-KDIF * LAIC);
                VIST = (1.0 - REFS) * PARDIR * KDIRT * Math.Exp(-KDIRT * LAIC);
                VISD = (1.0 - SCV) * PARDIR * KDIRBL * Math.Exp(-KDIRBL * LAIC);

                //Absorbed flux (J/M2 leaf/s) for shaded leaves and assimilation of shaded leaves
                VISSHD = VISDF + VIST - VISD;
                if (PMAX > 0.0)
                    FGRSH = PMAX * (1.0 - Math.Exp(-VISSHD * EFF / PMAX));
                else
                    FGRSH = 0.0;

                //Direct flux absorbed by leaves perpendicular on direct beam and
                //assimilation of sunlit leaf area
                VISPP = (1.0 - SCV) * PARDIR / SINB;

                FGRSUN = 0.0;
                for (j = 0; j < nGauss; j++)
                {
                    VISSUN = VISSHD + VISPP * xGauss[j];

                    if (PMAX > 0.0)
                        FGRS = PMAX * (1.0 - Math.Exp(-VISSUN * EFF / PMAX));
                    else
                        FGRS = 0.0;

                    FGRSUN = FGRSUN + FGRS * wGauss[j];
                }

                //Fraction sunlit leaf area (FSLLA) and local assimilation rate (FGL)
                FSLLA = CLUSTF * Math.Exp(-KDIRBL * LAIC);
                FGL = FSLLA * FGRSUN + (1.0 - FSLLA) * FGRSH;

                //Integration of local assimilation rate to canopy assimilation (FGROS)
                FGROS = FGROS + FGL * wGauss[i];
            }

            FGROS = FGROS * vLAI;
            return FGROS;
        }
        //------------------------------------------------------------------------------------------------

        private double LightUseEfficiency(double Temp, double fCO2)
        {
            double CO2PhotoCmp0, CO2PhotoCmp, EffPAR;

            //Efect of CO2 concentration
            if (fCO2 < 0.0) fCO2 = 350;

            //Check wheather a C3 or C4 crop
            if (pathway == "C3")   //C3 plants
            {
                CO2PhotoCmp0 = 38.0; //vppm
                CO2PhotoCmp = CO2PhotoCmp0 * Math.Pow(2.0, (Temp - 20.0) / 10.0); //Efect of Temperature on CO2PhotoCmp
                fCO2 = Math.Max(fCO2, CO2PhotoCmp);

                //--------------------------------------------------------------------------------------------------------------
                //Original SPASS version based on Goudriaan & van Laar (1994)
                //LUEref is the LUE at reference temperature of 20C and CO2=340ppm, i.e., LUEref = 0.6 kgCO2/ha/h / J/m2/s
                //EffPAR   = LUEref * (fCO2-fCO2PhotoCmp)/(fCO2+2*fCO2PhotoCmp);

                //--------------------------------------------------------------------------------------------------------------
                //The following equations were from Bauman et al (2001) ORYZA2000 
                //The tempeature function was standardised to 20C.
                //LUEref is the LUE at reference temperature of 20C and CO2=340ppm, i.e., LUEref = 0.48 kgCO2/ha/h / J/m2/s
                double Ft = (0.6667 - 0.0067 * Temp) / (0.6667 - 0.0067 * 20);
                EffPAR = LUEref * Ft * (1.0 - Math.Exp(-0.00305 * fCO2 - 0.222)) / (1.0 - Math.Exp(-0.00305 * 340.0 - 0.222));
            }

            else if (pathway == "C4")
            {
                CO2PhotoCmp = 0.0;

                //--------------------------------------------------------------------------------------------------------------
                //Original SPASS version based on Goudriaan & van Laar (1994)
                //LUEref is the LUE at reference temperature of 20C and CO2=340ppm, i.e., LUEref = 0.5 kgCO2/ha/h / J/m2/s
                EffPAR = LUEref * (fCO2 - CO2PhotoCmp) / (fCO2 + 2 * CO2PhotoCmp);

            }
            else
                throw new ApsimXException(this, "Need to be C3 or C4");

            return EffPAR;
        }
        //------------------------------------------------------------------------------------------------

        private double MaxGrossPhotosynthesis(double CO2, double Temp, double Fact)
        {
            double CO2I, CO2I340, CO2Func, PmaxGross;
            double CO2ref, TempFunc;
            double CO2Cmp, CO2R;

            //------------------------------------------------------------------------
            //Efect of CO2 concentration of the air
            if (CO2 < 0.0) CO2 = 350;

            //Check wheather a C3 or C4 crop
            if (pathway == "C3")   //C3 plants
            {
                CO2Cmp = 50; //CO2 compensation point (vppm), value based on Wang (1997) SPASS Table 3.4 
                CO2R = 0.7;  //CO2 internal/external ratio of leaf(C3= 0.7, C4= 0.4)
                CO2ref = 340;

                CO2 = Math.Max(CO2, CO2Cmp);
                CO2I = CO2R * CO2;
                CO2I340 = CO2R * CO2ref;

                // For C3 crop, Original Code SPASS
                //   fCO2Func= min((float)2.3,(fCO2I-fCO2Cmp)/(fCO2I340-fCO2Cmp)); //For C3 crops
                //   fCO2Func= min((float)2.3,pow((fCO2I-fCO2Cmp)/(fCO2I340-fCO2Cmp),0.5)); //For C3 crops

                //For C3 rice, based on Bouman et al (2001) ORYZA2000: modelling lowland rice, IRRI Publication
                CO2Func = (49.57 / 34.26) * (1.0 - Math.Exp(-0.208 * (CO2 - 60.0) / 49.57));
            }
            else if (pathway == "C4")
            {
                CO2Cmp = 5; //CO2 compensation point (vppm), value based on Wang (1997) SPASS Table 3.4 
                CO2R = 0.4;  //CO2 internal/external ratio of leaf(C3= 0.7, C4= 0.4)
                CO2ref = 380;

                CO2 = Math.Max(CO2, CO2Cmp);
                CO2I = CO2R * CO2;

                //For C4 crop, AgPasture Proposed by Cullen et al. (2009) based on FACE experiments
                CO2Func = CO2 / (CO2 + 150) * (CO2ref + 150) / CO2ref;
            }

            else
                throw new ApsimXException(this, "Need to be C3 or C4");

            //------------------------------------------------------------------------
            //Temperature response and Efect of daytime temperature
            TempFunc = WangEngelTempFunction(Temp);

            //------------------------------------------------------------------------
            //Maximum leaf gross photosynthesis
            PmaxGross = Math.Max((float)1.0, Pgmmax * (CO2Func * TempFunc * Fact));

            return PmaxGross;
        }
        //------------------------------------------------------------------------------------------------

        private double WangEngelTempFunction(double Temp, double MinTemp=0, double OptTemp=27.5, double MaxTemp=40, double RefTemp=27.5)
        {
            double RelEff = 0.0;
            double RelEffRefTemp = 1.0;
            double p = 0.0;

            if ((Temp > MinTemp) && (Temp < MaxTemp))
            {
                p = Math.Log(2.0) / Math.Log((MaxTemp - MinTemp) / (OptTemp - MinTemp));
                RelEff = (2 * Math.Pow(Temp - MinTemp, p) * Math.Pow(OptTemp - MinTemp, p) - Math.Pow(Temp - MinTemp, 2 * p)) / Math.Pow(OptTemp - MinTemp, 2 * p);
            }

            if ((RefTemp > MinTemp) && (RefTemp < MaxTemp))
            {
                p = Math.Log(2.0) / Math.Log((MaxTemp - MinTemp) / (OptTemp - MinTemp));
                RelEffRefTemp = (2 * Math.Pow(RefTemp - MinTemp, p) * Math.Pow(OptTemp - MinTemp, p) - Math.Pow(RefTemp - MinTemp, 2 * p)) / Math.Pow(OptTemp - MinTemp, 2 * p);
            }
            return RelEff / RelEffRefTemp;
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