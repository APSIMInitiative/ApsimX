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
    public class RUEModel : Model, IFunction
    {
        /// <summary>The rue</summary>
        [Link]
        IFunction RUE = null;

        /// <summary>The fc o2</summary>
        [Link]
        IFunction FCO2 = null;

        /// <summary>The function</summary>
        [Link]
        IFunction FN = null;

        /// <summary>The ft</summary>
        [Link]
        public IFunction FT = null;

        /// <summary>The fw</summary>
        [Link]
        IFunction FW = null;

        /// <summary>The FVPD</summary>
        [Link]
        public IFunction FVPD = null;

        /// <summary>The met data</summary>
        [Link]
        IWeather MetData = null;
        
        /// <summary>The radiation interception data</summary>
        [Link]
        public IFunction RadnInt = null;
        
        #region Class Data Members
        //[Input]
        //public NewMetType MetData;

        #endregion

        #region Associated variables

        /// <summary>Gets the VPD.</summary>
        /// <value>The VPD.</value>
        public double VPD
        {
            get
            {
                const double SVPfrac = 0.66;
                if (MetData != null)
                {
                    double VPDmint = MetUtilities.svp((float)MetData.MinT) - MetData.VP;
                    VPDmint = Math.Max(VPDmint, 0.0);

                    double VPDmaxt = MetUtilities.svp((float)MetData.MaxT) - MetData.VP;
                    VPDmaxt = Math.Max(VPDmaxt, 0.0);

                    return SVPfrac * VPDmaxt + (1 - SVPfrac) * VPDmint;
                }
                return 0;
            }
        }

        /// <summary>
        /// Total plant "actual" radiation use efficiency (for the day) corrected by reducing factors (g biomass/MJ global solar radiation) CHCK-EIT
        /// </summary>
        /// <value>The rue act.</value>
        [Units("gDM/MJ")]
        public double RueAct
        {
            get
            {
                double RueReductionFactor = Math.Min(FT.Value, Math.Min(FN.Value, FVPD.Value)) * FW.Value * FCO2.Value;
                return RUE.Value * RueReductionFactor;
            }
        }
        /// <summary>Daily growth increment of total plant biomass</summary>
        /// <returns>g dry matter/m2 soil/day</returns>
        public double Value
        {
            get
            {
                return RadnInt.Value * RueAct;
            }
        }
        #endregion
    }
}