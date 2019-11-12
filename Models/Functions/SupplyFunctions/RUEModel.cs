using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;

namespace Models.Functions.SupplyFunctions
{
    /// <summary>
    /// # [Name]
    /// Biomass fixation is modelled as the product of intercepted radiation and its conversion efficiency, the radiation use efficiency (RUE) ([Monteith1977]).  
    ///   This approach simulates net photosynthesis rather than providing separate estimates of growth and respiration.  
    ///   The potential photosynthesis calculated using RUE is then adjusted according to stress factors, these account for plant nutrition (FN), air temperature (FT), vapour pressure deficit (FVPD), water supply (FW) and atmospheric CO<sub>2</sub> concentration (FCO2).  
    ///   NOTE: RUE in this model is expressed as g/MJ for a whole plant basis, including both above and below ground growth.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(ILeaf))]
    public class RUEModel : Model, IFunction
    {
        /// <summary>The rue</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction RUE = null;

        /// <summary>The fc o2</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction FCO2 = null;

        /// <summary>The function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction FN = null;

        /// <summary>The ft</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction FT = null;

        /// <summary>The fw</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction FW = null;

        /// <summary>The FVPD</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction FVPD = null;

        /// <summary>The met data</summary>
        [Link]
        IWeather MetData = null;

        /// <summary>The radiation interception data</summary>
        [Link(Type = LinkType.Child, ByName = true)]
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
                if (MetData != null)
                    return MetData.VPD;
                else
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
                double RueReductionFactor = Math.Min(FT.Value(), Math.Min(FN.Value(), FVPD.Value())) * FW.Value() * FCO2.Value();
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
        #endregion
    }
}