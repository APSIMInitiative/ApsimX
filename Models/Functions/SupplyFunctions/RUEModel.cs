using System;
using APSIM.Core;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions.SupplyFunctions
{
    /// <summary>
    /// Potential daily photosynthesis is calculated as the product of intercepted short wave radiation and its conversion efficiency, the radiation use efficiency (RUE) ([#Monteith1977]).  _NOTE: RUE in this model is expressed as g/MJ for a whole plant basis, including both above and below ground growth._
    /// The radiation use efficienty is adjusted from a base value appropriate for historical levels of atmospheric CO2 concentration (ie 350ppm - see previous section).  Daily values of potential photosynthesis are then modified for effects of plant nitrogen status, temperature and atmospheric vapour pressure deficit.  These same relative growth factors are provided to the MicroClimate model to moderate the stomatal conductance terms incorporated into the Penman-Monteith formulation.
    /// Finally, the daily growth rate is moderated in response to the relative water supply:demand ratio (F<sub>W</sub>) to capture the effect of daily plant water status.
    /// 
    /// This calculation for photosynthesis is then provided to the organ arbitrator as a potential daily DM fixation supply for arbitration with all other DM supplies and demands.
    /// 
    /// ```
    /// DMFixationSupply = RUE x PhotosynthesisCO2Modifier x Min(F<sub>T</sub>, F<sub>N</sub>, F<sub>VPD</sub>) x F<sub>W</sub>
    /// ```
    /// 
    /// where
    /// 
    /// **RUE ({[RUEModel].RUE.GetType().Name})**
    /// 
    /// Radiation Use Efficiency for potential daily growth (g/MJ/m~2~)
    /// 
    /// {[RUEModel].RUE}
    /// 
    /// **F~T~ ({[RUEModel].FT.GetType().Name})**
    /// 
    /// Relative growth rate factor for Temperature (0-1)
    /// 
    /// {[RUEModel].FT}
    /// 
    /// **F~N~ ({[RUEModel].FN.GetType().Name})**
    /// 
    /// Relative growth rate factor for Nitrogen status (0-1)
    /// {[RUEModel].FN}
    /// 
    /// **F~VPD~ ({[RUEModel].FVPD.GetType().Name})**
    /// 
    /// Relative growth rate factor for Vapour Pressure Deficit (0-1)
    /// {[RUEModel].FVPD}
    /// 
    /// **F~W~ ({[RUEModel].FW.GetType().Name})**
    /// 
    /// Relative growth rate factor for plant water status (0-1)
    /// {[RUEModel].FW}
    /// 
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(ILeaf))]
    public class RUEModel : Model, IFunction
    {
        /// <summary>The rue</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/MJ")]
        IFunction RUE = null;

        /// <summary>The fc o2</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("0-1")]
        IFunction FCO2 = null;

        /// <summary>The function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("0-1")]
        IFunction FN = null;

        /// <summary>The ft</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("0-1")]
        public IFunction FT = null;

        /// <summary>The fw</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("0-1")]
        IFunction FW = null;

        /// <summary>The FVPD</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("0-1")]
        public IFunction FVPD = null;

        /// <summary>The met data</summary>
        [Link]
        IWeather MetData = null;

        /// <summary>The radiation interception data</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("MJ/m^2/d")]
        public IFunction RadnInt = null;


        /// <summary>Gets the VPD.</summary>
        /// <value>The VPD.</value>
        public double VPD
        {
            get
            {
                if (MetData != null)
                    return MetData.VPD;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Combined stress effects on biomass production
        /// </summary>
        /// <value>The rue act.</value>
        public double RueReductionFactor
        {
            get
            {
                return Math.Min(FT.Value(), Math.Min(FN.Value(), FVPD.Value())) * FW.Value() * FCO2.Value();
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
                return RUE.Value() * RueReductionFactor;
            }
        }
        /// <summary>Daily growth increment of total plant biomass</summary>
        /// <returns>g dry matter/m2 soil/day</returns>
        public double Value(int arrayIndex = -1)
        {
            double radiationInterception = RadnInt.Value(arrayIndex);
            if (Double.IsNaN(radiationInterception))
                throw new Exception("NaN Radiation interception value supplied to RUE model");
            if (radiationInterception < -0.000000000001)
                throw new Exception("Negative Radiation interception value supplied to RUE model");
            return radiationInterception * RueAct;
        }
    }
}