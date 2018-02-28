// ----------------------------------------------------------------------
// <copyright file="RUEModel.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions.SupplyFunctions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using System;

    /// <summary>
    /// # [Name]
    /// Biomass accumulation is modeled as the product of intercepted radiation and its conversion efficiency, the radiation use efficiency (RUE) ([Monteith1977]).  
    ///   This approach simulates net photosynthesis rather than providing separate estimates of growth and respiration.  
    ///   RUE is calculated from a potential value which is discounted using stress factors that account for plant nutrition (FN), air temperature (FT), vapour pressure deficit (FVPD), water supply (FW) and atmospheric CO<sub>2</sub> concentration (FCO2).  
    ///   NOTE: RUE in this model is expressed as g/MJ for a whole plant basis, including both above and below ground growth.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(ILeaf))]
    public class RUEModel : BaseFunction
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];

        /// <summary>The rue</summary>
        [ChildLinkByName]
        private IFunction RUE = null;

        /// <summary>The fc o2</summary>
        [ChildLinkByName]
        private IFunction FCO2 = null;

        /// <summary>The function</summary>
        [ChildLinkByName]
        private IFunction FN = null;

        /// <summary>The ft</summary>
        [ChildLinkByName]
        public IFunction FT = null;

        /// <summary>The fw</summary>
        [ChildLinkByName]
        private IFunction FW = null;

        /// <summary>The FVPD</summary>
        [ChildLinkByName]
        public IFunction FVPD = null;

        /// <summary>The met data</summary>
        [Link]
        private IWeather MetData = null;
        
        /// <summary>The radiation interception data</summary>
        [Link]
        public IFunction RadnInt = null;

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
                double RueReductionFactor = Math.Min(FT.Value(), Math.Min(FN.Value(), FVPD.Value())) * FW.Value() * FCO2.Value();
                return RUE.Value() * RueReductionFactor;
            }
        }
        /// <summary>Daily growth increment of total plant biomass</summary>
        /// <returns>g dry matter/m2 soil/day</returns>
        public override double[] Values()
        {
            double radiationInterception = RadnInt.Value();
            if (Double.IsNaN(radiationInterception))
                throw new Exception("NaN Radiation interception value supplied to RUE model");
            if (radiationInterception < 0)
                throw new Exception("Negative Radiation interception value supplied to RUE model");
            returnValue[0] = radiationInterception * RueAct;

            return returnValue;
        }
        #endregion
    }
}