// ----------------------------------------------------------------------
// <copyright file="SoilWaterScale.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Soils;
    using System;

    /// <summary>
    /// # [Name]
    /// A simple scale to convert soil water content into a value between 0 and 2
    /// </summary>
    [Serializable]
    public class SoilWaterScale : BaseFunction
    {
        [Link]
        private Soil soilModel = null;

        /// <summary>Gets the value of the function.</summary>
        public override double[] Values()
        {
            double[] sw = soilModel.SoilWater.SW;
            double[] dul = soilModel.DUL;
            double[] ll15 = soilModel.LL15;
            double[] sat = soilModel.SAT;

            double[] wfd = new double[sw.Length];
            for (int i = 0; i < wfd.Length; i++)
            {
                // temporary water factor (0-1)
                if (sw[i] > dul[i])
                {
                    // saturated
                    wfd[i] = Math.Max(1.0, Math.Min(2.0, 1.0 +
                             MathUtilities.Divide(sw[i] - dul[i], sat[i] - dul[i], 0.0)));
                }
                else
                {
                    // unsaturated
                    // assumes rate of mineralisation is at optimum rate until soil moisture midway between dul and ll15
                    wfd[i] = Math.Max(0.0, Math.Min(1.0, 
                             MathUtilities.Divide(sw[i] - ll15[i], dul[i] - ll15[i], 0.0)));
                }
            }

            return wfd;
        }

    }
}