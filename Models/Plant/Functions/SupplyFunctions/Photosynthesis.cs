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

        /// <summary>
        /// Return the DM supply.
        /// </summary>
        /// <param name="arrayIndex"></param>
        /// <returns>The DM supply.</returns>
        public double Value(int arrayIndex = -1)
        {
            C3PhotoLink ps = new C3PhotoLink();
            double SLN = Leaf.Live.N / Leaf.LAI;
            double RSR = 1.0; //usage looks to be commented out.
            double angle = 0;

            double[] res = ps.calc(Clock.Today.DayOfYear, Weather.Latitude, Weather.MaxT, Weather.MinT, Weather.Radn, Leaf.LAI, SLN, Soil.PAW.Sum(), RSR, angle);
            return res[0];
        }
    }
}