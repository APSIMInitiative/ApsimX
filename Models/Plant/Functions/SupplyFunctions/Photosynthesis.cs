using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;
using Models.Soils;
using Models.PMF.Organs;
using Models.PMF.Photosynthesis;

namespace Models.PMF.Functions.SupplyFunctions
{
    /// <summary>
    /// Full photosynthesis model; code by Alex Wu, provided by Al Doherty
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(ILeaf))]
    public class Photosynthesis : Model, IFunction
    {
        #region Links
        [Link]
        Clock Clock = null;

        [Link]
        Weather Weather = null;

        [Link]
        Soil Soil = null;

        [Link]
        Leaf Leaf = null;
        #endregion

        #region Functions
        [Link]
        [Description("Use C3 or C4 model")]
        IFunction Cmodel = null;

        [Link]
        [Description("Root Shoot Ratio")]
        IFunction RSR = null;

        [Link]
        [Description("Leaf Angle 0 horizontal, 90 vertical")]
        IFunction LeafAngle = null;

        [Link]
        [Description("B")]
        IFunction B = null;

        [Link]
        [Description("The ratio of SLN concentration at the top as a multiplier on avg SLN from Apsim")]
        IFunction SLNRatioTop = null;

        [Link]
        [Description("Slope of linear relationship between Vmax per leaf are at 25°C and N")]
        IFunction psiVc = null;

        [Link]
        [Description("Slope of linear relationship between Jmax per leaf are at 25°C and N")]
        IFunction psiJ = null;

        [Link]
        [Description("Slope of linear relationship between Rd per leaf are at 25°C and N")]
        IFunction psiRd = null;

        [Link]
        [Description("Psi reduction factor that applies to all psi values. Can use as a genetic factor")]
        IFunction psiFactor = null;

        [Link]
        [Description("Air CO2 partial pressure")]
        IFunction Ca = null;

        [Link]
        [Description("Ratio of Ci to Ca")]
        IFunction CiCaRatio = null;

        [Link]
        [Description("Mesophyll conductance for CO2 at 25degrees")]
        IFunction gm25 = null;

        [Link]
        [Description("Amount of N needed to retain structure. Below this photosynthesis does not occur")]
        IFunction StructuralN = null;

        [Link]
        [Description("Minimum LAI for photosynthesis to occur.")]
        IFunction MinLAI = null;

        // C4 exclusive params
        [Link]
        [Description("")]
        IFunction psiVp = null;

        [Link]
        [Description("")]
        IFunction gbs = null;

        [Link]
        [Description("")]
        IFunction Vpr = null;
        #endregion

        /// <summary>
        /// Return the DM supply.
        /// </summary>
        /// <param name="arrayIndex"></param>
        /// <returns>The DM supply.</returns>
        public double Value(int arrayIndex = -1)
        {
            if ((int)Math.Round(Cmodel.Value()) == 3)
            {
                C3PhotoLink ps = new C3PhotoLink();
                double SLN = Leaf.Live.N / Leaf.LAI;

                if (Leaf.LAI > MinLAI.Value())
                {
                    double[] res = ps.calc(Clock.Today.DayOfYear, Weather.Latitude, Weather.MaxT, Weather.MinT, Weather.Radn, Leaf.LAI, SLN, Soil.PAW.Sum(), RSR.Value(), LeafAngle.Value(),
                        B.Value(), SLNRatioTop.Value(), psiVc.Value(), psiJ.Value(), psiRd.Value(), psiFactor.Value(), Ca.Value(), CiCaRatio.Value(), gm25.Value(), StructuralN.Value());
                    return res[0];
                }

                return 0;
            }
            else if ((int)Math.Round(Cmodel.Value()) == 4)
            {
                C4PhotoLink ps = new C4PhotoLink();
                double SLN = Leaf.Live.N / Leaf.LAI;

                if (Leaf.LAI > 0.5)
                {
                    double[] res = ps.calc(Clock.Today.DayOfYear, Weather.Latitude, Weather.MaxT, Weather.MinT, Weather.Radn, Leaf.LAI, SLN, Soil.PAW.Sum(), B.Value(), RSR.Value(), LeafAngle.Value(), SLNRatioTop.Value(),
                        psiVc.Value(), psiJ.Value(), psiRd.Value(), psiVp.Value(), psiFactor.Value(), Ca.Value(), gbs.Value(), gm25.Value(), Vpr.Value());
                    return res[0];
                }
                return 0;
            }
            return 0;
        }
    }
}