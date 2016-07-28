using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;

namespace Models.PMF.Functions.SupplyFunctions
{
    /// <summary>
    /// Biomass accumulation is the product of the amount of intercepted radiation and its 
    /// conversion efficiency, the radiation use efficiency (RUE) [Monteith1977].  
    /// This approach simulates net photosynthesis rather than providing separate estimates 
    /// of growth and respiration.  RUE is calculated from a potential value which is discounted 
    /// using stress factors that account for plant nutrition (Fn), air temperature(Ft), vapour pressure deficit (Fvpd), water supply (Fw) and atmospheric CO2 concentration (Fco2).
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ILeaf))]
    public class CanopyPhotosynthesis : Model, IFunction
    {
        /// <summary>The Plant</summary>
        [Link]
        public Plant Plant = null;
        /// <summary>The Canopy</summary>
        [Link]
        public ICanopy Canopy = null;
        /// <summary>The Weather file</summary>
        [Link]
        public Weather Weather = null;
        /// <summary>The Weather file</summary>
        [Link]
        public Clock Clock = null;

        /// <summary>
        /// 
        /// </summary>
        [Description("Gmax")]
        public double fPgmmax { get; set;  }
        /// <summary>
        /// 
        /// </summary>
        [Description("MaxLUE")]
        public double fMaxLUE { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("CO2comp")]
        public double fCO2Cmp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("CO2R")]
        public double fCO2R { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("KDIF")]
        public double fKDIF { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("maximum temperature")]
        public double fMaxTmp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("Optimal temperature")]
        public double fOptTmp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("minimum temperature")]
        public double fMinTmp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("C3/C4")]
        public string pathway { get; set; }


        /// <summary>
        /// The amount of DM that is fixed by photosynthesis
        /// </summary>
        public double GrossPhotosynthesis { get; set; }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                GrossPhotosynthesis = DailyCanopyGrossPhotosythesis(Canopy.LAI,
                                                      Weather.Latitude,
                                                      Clock.Today.DayOfYear,
                                                      Weather.Radn,
                                                      Weather.MaxT,
                                                      Weather.MinT,
                                                      Weather.CO2,
                                                      1.0) * 30/ 44 * 0.1;     
                       

            }
        }

        ///<summary>%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        ///float DLL DailyCanopyGrossPhotosythesis(LPSTR pCrop,float fLAI, float fLatitude,int nDay,
        ///                                        float fRad,float fTmpMax,float fTmpMin,float fCO2,
        ///                                        PZN pLfN,PZRESPONSE pTmpRsp)
        ///Author:   Enli Wang
        ///Date:      10.11.1996
        ///Purpose:   This function calculates the daily canopy photosynthesis rate under optimal water condition
        ///Inputs:   1. pCROP      - Pointer to a string containing the crop name,use the following names:
        ///                       WHEAT,BARLEY,MAIZE,MILLET,SOGHUM,POTATO,SUGARBEET,SOYBEAN,COTTON,C3,C4,CAM
        ///         2. fLAI         - Effective leaf area index (-)
        ///          3. fLatitude   - Location latitude (Degree)
        ///          4. nDay         - Julain day (-)
        ///          5. fRad         - Daily global radiation (MJ/d)
        ///          6. fTmpMax      - Daily maximum air temperature(C)
        ///          7. fTmpMin      - Daily minimum air temperature(C)
        ///          8. fCO2         - Current CO2 concentration in the air (vppm)
        ///         9. pLfN      - Pointer to a ORGANNC structure containing leaf nitrogen concentration
        ///         10. pTmpRsp      - Pointer to a ZRESPONSE structure containing temperature response data for photosynthesis
        ///Outputs:   1. Return      - Calculated daily gross photosynthesis rate of unit leaf area (kgCO2/ha.day)
        ///Functions Called:
        ///         LeafMaxGrossPhotosynthesis
        ///         LeafLightUseEfficiency
        ///         CanopyGrossPhotosynthesis
        ///Comments:   This function checks at first the data contained under pResp. If these data are valid, they will be
        ///         used to construct the temperature response function for photosynthesis. If not, the cardinal temperatures
        ///         at pCardTemp will be used to construct the temperature response function. If pCardTemp equals NULL,
        ///         a minimum, optimum and maximum temperature of 0, 22 and 35C will be assumed respectively.
        ///         If pLfN equals NULL, no nitrogen stress will be considered.
        ///Reference:1. Wang,Enli. xxxx.
        ///%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%% </summary>
        public double DailyCanopyGrossPhotosythesis(double fLAI, double fLatitude, int nDay,
                                                double fRad, double fTmpMax, double fTmpMin, double fCO2,
                                               double nFact)
        {
            int i;
            double vAveGrossPs;
            double vGrossPs;
            double vDailyGrossPs;
            double PAR;
            double PARDIF;
            double PARDIR;
            //double vAveGrossPsMax;

            double vLAI;
            double vGlobalRadiation, vSinHeight, vDayl, vAtmTrans, vDifFr;
            double vLatitude, vDec, vSin, vCos, vRsc, vSolarConst, vDailySin, vDailySinE, vHour, vRadExt;
            double fLUE, fPgMax, fTemp;



            int nGauss = 5;
            double[] xGauss = { 0.0469101, 0.2307534, 0.5000000, 0.7692465, 0.9530899 };
            double[] wGauss = { 0.1184635, 0.2393144, 0.2844444, 0.2393144, 0.1184635 };
            double PI = 3.1415926;
            double RAD = PI / 180.0;



            vLAI = (double)fLAI;
            vGlobalRadiation = (double)fRad * 1E6;    //J/m2.d
            vLatitude = (double)fLatitude;

            //===========================================================================================
            //Dailenght, Solar constant and daily extraterrestrial radiation
            //===========================================================================================
            //Declination of the sun as function of Daynumber (vDay)

            vDec = -Math.Asin(Math.Sin(23.45 * RAD) * Math.Cos(2.0 * PI * ((double)nDay + 10.0) / 365.0));

            //vSin, vCos and vRsc are intermediate variables
            vSin = Math.Sin(RAD * vLatitude) * Math.Sin(vDec);
            vCos = Math.Cos(RAD * vLatitude) * Math.Cos(vDec);
            vRsc = vSin / vCos;

            //Astronomical daylength (hr)
            vDayl = 12.0 * (1.0 + 2.0 * Math.Asin(vSin / vCos) / PI);

            //Sine of solar height(vDailySin), inegral of vDailySin(vDailySin) and integrel of vDailySin
            //with correction for lower atmospheric transmission at low solar elevations (vDailySinE)
            vDailySin = 3600.0 * (vDayl * vSin + 24.0 * vCos * Math.Sqrt(1.0 - vRsc * vRsc) / PI);
            vDailySinE = 3600.0 * (vDayl * (vSin + 0.4 * (vSin * vSin + vCos * vCos * 0.5))
                     + 12.0 * vCos * (2.0 + 3.0 * 0.4 * vSin) * Math.Sqrt(1.0 - vRsc * vRsc) / PI);

            //Solar constant(vSolarConst) and daily extraterrestrial (vRadExt)
            vSolarConst = 1370.0 * (1.0 + 0.033 * Math.Cos(2.0 * PI * (double)nDay / 365.0));   //J/m2.d
            vRadExt = vSolarConst * vDailySin * 1E-6;               //MJ/m2.d

            //===========================================================================================
            //Daily photosynthesis
            //===========================================================================================
            //Assimilation set to zero and three different times of the Day (vHour)
            vAveGrossPs = 0;
            //vAveGrossPsMax = 0;

            //Daytime temperature
            fTemp = 0.71 * fTmpMax + 0.29 * fTmpMin;

            for (i = 0; i < nGauss; i++)
            {
                //At the specified vHour, radiation is computed and used to compute assimilation
                vHour = 12.0 + vDayl * 0.5 * xGauss[i];
                //Sine of solar elevation
                vSinHeight = Math.Max(0.0, vSin + vCos * Math.Cos(2.0 * PI * (vHour + 12.0) / 24.0));
                //Diffuse light fraction (vDifFr) from atmospheric transmission (vAtmTrans)
                PAR = 0.5 * vGlobalRadiation * vSinHeight * (1.0 + 0.4 * vSinHeight) / vDailySinE;
                vAtmTrans = PAR / (0.5 * vSolarConst * vSinHeight);

                if (vAtmTrans <= 0.22)
                    vDifFr = 1.0;
                else
                {
                    if ((vAtmTrans > 0.22) && (vAtmTrans <= 0.35))
                        vDifFr = 1.0 - 6.4 * (vAtmTrans - 0.22) * (vAtmTrans - 0.22);
                    else
                        vDifFr = 1.47 - 1.66 * vAtmTrans;
                }

                vDifFr = Math.Max(vDifFr, 0.15 + 0.85 * (1.0 - Math.Exp(-0.1 / vSinHeight)));

                //Diffuse PAR (PARDIF) and direct PAR (PARDIR)
                PARDIF = Math.Min(PAR, vSinHeight * vDifFr * vAtmTrans * 0.5 * vSolarConst);
                PARDIR = PAR - PARDIF;

                //Light response parameters
                fPgMax = LeafMaxGrossPhotosynthesis(fTemp, fCO2, nFact);

                fLUE = LeafLightUseEfficiency(fTemp, fCO2);

                //Canopy gross photosynthesis
                vGrossPs = CanopyGrossPhotosynthesis(fPgMax, fLUE, fLAI, fKDIF, fLatitude,
                                     nDay, vHour, PARDIR, PARDIF);

                //Integration of assimilation rate to a daily total (vAveGrossPs)
                vAveGrossPs += vGrossPs * wGauss[i];
            }

            vDailyGrossPs = vAveGrossPs * vDayl;

            return vDailyGrossPs;
        }


        ///<summary>%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        ///float DLL LeafMaxGrossPhotosynthesis(float fPgmmax,float fTemp,float fCO2,float fCO2Cmp,float fCO2R,
        ///                                  float fMinTmp,float fOptTmp,float fMaxTmp,PZN pLfN,PZRESPONSE pResp)
        ///Author:   Enli Wang
        ///Date:      10.11.1996
        ///Purpose:   This function calculates the maximum gross photosynthesis of unit leaf area at current temperature
        ///         current CO2 concentration, current leaf N level and light saturation
        ///Inputs:   1. fPgmmax      - Maximum gross leaf photosynthesis rate at optimal temperature, N level, both
        ///                       CO2 and light saturation (kgCO2/ha.hr)
        ///         2. fTemp      - Current air temperature (C)
        ///         3. fCO2         - Current CO2 concentration in the air (vppm)
        ///         4. fCO2Cmp      - Compensation point of CO2 for photosynthesis (vppm)
        ///         5. fCO2R      - Ratio of CO2 concentration in stomatal cavity to that in the air (-)
        ///         6. pResp      - Pointer to a ZRESPONSE structure containing temperature response data for photosynthesis
        ///         7. pCardTemp   - pointer to a CARDTEMP structure containing the cardinal temperature for photosynthesis
        ///         8. pLfN      - Pointer to a ORGANNC structure containing leaf nitrogen concentration
        ///Outputs:   1. Return      - Calculated maximum gross photosynthesis rate of unit leaf area (kgCO2/ha.hr)
        ///Functions Called:
        ///         RelativeTemperatureResponse
        ///         ZFGENERATOR
        ///Comments:   This function checks at first the data contained under pResp. If these data are valid, they will be
        ///         used to construct the temperature response function for photosynthesis. If not, the cardinal temperatures
        ///         at pCardTemp will be used to construct the temperature response function. If pCardTemp equals NULL,
        ///         a minimum, optimum and maximum temperature of 0, 22 and 35C will be assumed respectively.
        ///         If pLfN equals NULL, no nitrogen stress will be considered.
        ///Reference:1. Wang,Enli. xxxx.
        ///%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%% </summary>
        public double LeafMaxGrossPhotosynthesis(double fTemp, double fCO2, double nFact)
        {
            double fTempFunc, fCO2I, fCO2I340, fCO2Func, fPmaxGross;


            //------------------------------------------------------------------------
            //Efect of CO2 concentration of the air
            if (fCO2 < 0.0) fCO2 = 350;

            fCO2 = Math.Max(fCO2, fCO2Cmp);

            fCO2I = fCO2 * fCO2R;
            fCO2I340 = fCO2R * 340.0;

            // Original Code
            //   fCO2Func= min((float)2.3,(fCO2I-fCO2Cmp)/(fCO2I340-fCO2Cmp)); //For C3 crops
            fCO2Func = (49.57 / 34.26) * (1.0- Math.Exp(-0.208 * (fCO2 - 60.0) / 49.57));
            //   fCO2Func= min((float)2.3,pow((fCO2I-fCO2Cmp)/(fCO2I340-fCO2Cmp),0.5)); //For C3 crops

            //------------------------------------------------------------------------
            //Temperature response and Efect of daytime temperature

            fTempFunc = RelativeTemperatureResponse(fTemp, fMinTmp, fOptTmp, fMaxTmp);

            //------------------------------------------------------------------------
            //Maximum leaf gross photosynthesis
            fPmaxGross = Math.Max((float)1.0, fPgmmax * (fCO2Func * fTempFunc * nFact));

            return fPmaxGross;
        }

        ///<summary>%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        ///Function: float DLL RelativeTemperatureResponse(float fTemp,
        ///                                          float fTempMin, float fTempOpt, float fTempMax)
        ///Author:   Enli Wang
        ///Date:      07.11.1996
        ///Purpose:   This function calculates the effect of temperature on the rate of certain plant process with
        ///         a temperature optimum (fOptTemp)
        ///Inputs:   1. fTemp   - Current temperature (C)
        ///         2. fMinTemp   - Minimum temperature for the process (C)
        ///         2. fOptTemp   - Optimum temperature for the process (C)
        ///         2. fMaxTemp   - Maximum temperature for the process (C)
        ///Outputs:   The calculated temperature effect (0-1)  0-fMinTemp,fMaxTemp; 1-fOptTemp
        ///Functions Called:
        ///         None
        ///Reference:1. Wang,Enli. xxxx.
        ///%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%</summary>
        public double RelativeTemperatureResponse(double fTemp, double fMinTemp, double fOptTemp, double fMaxTemp)
        {
            double vTemp, vTmin, vTopt, vTmax, p, vRelEff;

            vTemp = fTemp;
            vTmin = fMinTemp;
            vTopt = fOptTemp;
            vTmax = fMaxTemp;

            if ((fTemp <= fMinTemp) || (fTemp >= fMaxTemp))
                vRelEff = 0.0;
            else
            {
                p = Math.Log(2.0) / Math.Log((vTmax - vTmin) / (vTopt - vTmin));
                vRelEff = (2 * Math.Pow(vTemp - vTmin, p) * Math.Pow(vTopt - vTmin, p) - Math.Pow(vTemp - vTmin, 2 * p)) / Math.Pow(vTopt - vTmin, 2 * p);
            }

            return vRelEff;
        }


        ///<summary>%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        ///float DLL LeafLightUseEfficiency(float fMaxLUE, float fTemp, float fCO2, LPSTR pType)
        ///Author:   Enli Wang
        ///Date:      10.11.1996
        ///Purpose:   This function calculates the light use efficiency of the leaf at low light level and
        ///         current CO2 concentration and temperature
        ///Inputs:   1. fMaxLUE      - Maximum leaf light use efficiency at low light, low temperature and low CO2
        ///                       ((kgCO2/ha leaf.hr)/(W/m2))
        ///         2. fTemp      - Current air temperature (C)
        ///         3. fCO2         - Current CO2 concentration in the air (vppm)
        ///         4. pType      - Pointer to a string containing the type of the plant (C3 or C4)
        ///Outputs:   1. Return      - Calculated light use efficiency at low light ((kgCO2/ha leaf.hr)/(W/m2))
        ///Functions Called:
        ///         None
        ///Comments:   For C3 plants the light use efficiency will decreased if temperature becomes or CO2 concentration
        ///         becomes low due to the increased photorespiration. So that the effect of photorespiration is
        ///         simulated in this function
        ///Reference:1. Wang,Enli. xxxx.
        ///%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%</summary>
        public double LeafLightUseEfficiency(double fTemp, double fCO2)
        {
            double fCO2PhotoCmp0, fCO2PhotoCmp, fEffPAR;


            //Check wheather a C3 or C4 crop
            if (pathway=="C3")   //C3 plants
                fCO2PhotoCmp0 = 38.0; //vppm
            else if (pathway == "C4")
                fCO2PhotoCmp0 = 0.0;
            else
                throw new ApsimXException(this, "Need to be C3 or C4");


            //Efect of Temperature
            fCO2PhotoCmp = fCO2PhotoCmp0 * Math.Pow(2.0, (fTemp - 20.0) / 10.0);

            //Efect of CO2 concentration
            if (fCO2 < 0.0) fCO2 = 350;

            fCO2 = Math.Max(fCO2, fCO2PhotoCmp);

            //   fEffPAR   = fMaxLUE*(fCO2-fCO2PhotoCmp)/(fCO2+2*fCO2PhotoCmp);
            //   fEffPAR = fMaxLUE*(1.0 - exp(-0.00305*fCO2-0.222))/(1.0 - exp(-0.00305*340.0-0.222));
            float Ft = (float)(0.6667 - 0.0067 * fTemp);
            fEffPAR = Ft * (1.0 - Math.Exp(-0.00305 * fCO2 - 0.222)) / (1.0 - Math.Exp(-0.00305 * 340.0 - 0.222));

            return fEffPAR;
        }

        ///<summary>%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        ///float DLL CanopyGrossPhotosynthesis(float fPgMax, float fLUE, float fLAI,
        ///                                float fLatitude,int nDay,float fHour, float fPARdir,float fPARdif)
        ///Author:   Enli Wang
        ///Date:      10.11.1996
        ///Purpose:   This function calculates the canopy gross photosynthesis rate for a given crop with fPgMax,fLUE,fLAI
        ///         at latitude (fLatitude) on day (julian day fDay) at fHour
        ///Inputs:   1. fPgMax   - Maximum leaf gross photosynthesis rate at light saturation (kgCO2/ha.hr)
        ///         2. fLUE      - Light use efficiency of the leaf at current conditions ((kgCO2/ha leaf.hr)/(W/m2))
        ///         3. fLAI      - Effective LAI (-)
        ///         4. fLatitude- Location latitude (Degree)
        ///         5. nDay      - Julain day (-)
        ///         6. fHour   - Current time (Hour)
        ///         7. fPARdir   - Direct component of incident photosynthetic active radiation (W/m2)
        ///         7. fPARdif   - Diffuse component of incident photosynthetic active radiation (W/m2)
        ///Outputs:   1. Return   - Calculated canopy photosynthesis rate (kgCO2/ha.hr)
        ///Functions Called:
        ///         None
        ///Comments:   The input variable fPgMax and fLUE should be calculated using the following functions:
        ///         fPgMax = LeafMaxGrossPhotosynthesis(...);
        ///         fLUE   = LeafLightUseEfficiency(...)
        ///Reference:1. Wang,Enli. xxxx.
        ///%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%</summary>
        public double CanopyGrossPhotosynthesis(double fPgMax, double fLUE, double fLAI, double fKDIF,
                                        double fLatitude, int nDay, double fHour, double fPARdir, double fPARdif)
        {
            int i, j;
            double PMAX, EFF, vLAI, PARDIR, PARDIF, SINB, KDIF;
            double SQV, REFH, REFS, CLUSTF, KDIRBL, KDIRT, FGROS, LAIC, VISDF, VIST, VISD, VISSHD, FGRSH;
            double FGRSUN, VISSUN, VISPP, FGRS, FSLLA, FGL, LAT, DAY, HOUR, DEC, vSin, vCos;
            int nGauss = 5;
            double[] xGauss = { 0.0469101, 0.2307534, 0.5000000, 0.7692465, 0.9530899 };
            double[] wGauss = { 0.1184635, 0.2393144, 0.2844444, 0.2393144, 0.1184635 };

            double PI = 3.1415926;
            double RAD = PI / 180.0;

            double SCV = 0.20;    //Scattering coefficient of leaves for visible radiation (PAR)
            //double k = 0.50;   //The average extinction coefficient for visible and near infrared radiation


            //if WheatAndBarley  KDIF =0.6;
            //if Potato          KDIF =1.0;
            //if SugarBeet       KDIF =0.69;   WAVE, p5-12


            KDIF = fKDIF;   //Extinction coefficient for diffuse light
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

        /// <summary>Daily growth increment of total plant biomass</summary>
        /// <returns>g dry matter/m2 soil/day</returns>
        public double Value
        {
            get
            {
                return GrossPhotosynthesis;
            }
        }
    }
}