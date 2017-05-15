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
    [ValidParent(ParentType = typeof(CanopyPhotosynthesis))]
    public class LeafLightUseEfficiency : Model
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("C3/C4")]
        public string pathway { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Temp"></param>
        /// <param name="fCO2"></param>
        /// <returns></returns>
        public double Value(double Temp, double fCO2)
        {
            double fCO2PhotoCmp0, fCO2PhotoCmp, fEffPAR;


            //Check wheather a C3 or C4 crop
            if (pathway == "C3")   //C3 plants
                fCO2PhotoCmp0 = 38.0; //vppm
            else if (pathway == "C4")
                fCO2PhotoCmp0 = 0.0;
            else
                throw new ApsimXException(this, "Need to be C3 or C4");


            //Efect of Temperature
            fCO2PhotoCmp = fCO2PhotoCmp0 * Math.Pow(2.0, (Temp - 20.0) / 10.0);

            //Efect of CO2 concentration
            if (fCO2 < 0.0) fCO2 = 350;

            fCO2 = Math.Max(fCO2, fCO2PhotoCmp);

            //   fEffPAR   = fMaxLUE*(fCO2-fCO2PhotoCmp)/(fCO2+2*fCO2PhotoCmp);
            //   fEffPAR = fMaxLUE*(1.0 - exp(-0.00305*fCO2-0.222))/(1.0 - exp(-0.00305*340.0-0.222));
            float Ft = (float)(0.6667 - 0.0067 * Temp);
            fEffPAR = Ft * (1.0 - Math.Exp(-0.00305 * fCO2 - 0.222)) / (1.0 - Math.Exp(-0.00305 * 340.0 - 0.222));

            return fEffPAR;
        }
    }
}