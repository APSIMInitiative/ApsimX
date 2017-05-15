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
    public class LeafMaxGrossPhotosynthesis : Model
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("Gmax")]
        public double Pgmmax { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("CO2comp")]
        public double CO2Cmp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("CO2R")]
        public double CO2R { get; set; }
        /// <summary>
        /// Function to return a temperature factor for a given function
        /// </summary>
        [Link]
        private WangEngelTempFunction TemperatureResponse = null;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CO2"></param>
        /// <param name="Fact"></param>
        /// <returns></returns>
        public double Value(double CO2, double Fact)
        {
            double TempFunc, CO2I, CO2I340, CO2Func, PmaxGross;


            //------------------------------------------------------------------------
            //Efect of CO2 concentration of the air
            if (CO2 < 0.0) CO2 = 350;

            CO2 = Math.Max(CO2, CO2Cmp);

            CO2I = CO2 * CO2R;
            CO2I340 = CO2R * 340.0;

            // Original Code
            //   fCO2Func= min((float)2.3,(fCO2I-fCO2Cmp)/(fCO2I340-fCO2Cmp)); //For C3 crops
            CO2Func = (49.57 / 34.26) * (1.0 - Math.Exp(-0.208 * (CO2 - 60.0) / 49.57));
            //   fCO2Func= min((float)2.3,pow((fCO2I-fCO2Cmp)/(fCO2I340-fCO2Cmp),0.5)); //For C3 crops

            //------------------------------------------------------------------------
            //Temperature response and Efect of daytime temperature

            TempFunc = TemperatureResponse.Value();

            //------------------------------------------------------------------------
            //Maximum leaf gross photosynthesis
            PmaxGross = Math.Max((float)1.0, Pgmmax * (CO2Func * TempFunc * Fact));

            return PmaxGross;
        }
    }
}