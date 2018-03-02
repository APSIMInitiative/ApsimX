
namespace Models.PMF.Functions.DemandFunctions
{
    using System;
    using Models.Core;
    using Models.Interfaces;
    using APSIM.Shared.Utilities;
    using System.Diagnostics;

    /// <summary>
    /// # [Name]
    /// Water demand is calculated using the Transpiration Efficiency (TE) approach (ie TE=Coefficient/VDP).
    /// </summary>
    [Serializable]
    public class TEWaterDemandFunction : BaseFunction
    {
        /// <summary>Average Daily Vapour Pressure Deficit as a proportion of daily Maximum.</summary>
        [Link]
        IFunction SVPFrac = null;
        /// <summary>The met data</summary>
        [Link]
        public IWeather MetData = null;
        /// <summary>The photosynthesis</summary>
        [Link]
        IFunction Photosynthesis = null;
        /// <summary>Transpiration Efficiency Coefficient to relate TE to daily VPD</summary>
        [Link]
        IFunction TranspirationEfficiencyCoefficient = null;

        /// <summary>Computes the vapour pressure deficit.</summary>
        /// <value>The vapour pressure deficit (hPa?)</value>
        public double VPD
        {
            get
            {
                double SVPmax = MetUtilities.svp(MetData.MaxT) * 0.1;
                double SVPmin = MetUtilities.svp(MetData.MinT) * 0.1;
                return Math.Max(SVPFrac.Value() * (SVPmax - SVPmin), 0.01);
            }
        }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            double returnValue = Photosynthesis.Value() / (TranspirationEfficiencyCoefficient.Value() / VPD / 0.001);
            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + returnValue);
            return new double[] { returnValue };
        }

    }
}


