// ----------------------------------------------------------------------
// <copyright file="SoilTemperatureDepthFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using System;

    /// <summary>
    /// # [Name]
    /// Return soil temperature (oC) from a specified soil profile layer.
    /// The source of soil temperature array can be either SoilN ("st" property) or SoilTemp ("ave_soil_temp" property)
    /// </summary>
    [Serializable]
    [Description("Return soil temperature (oC) from a specified soil profile layer.  The source of soil temperature array can be either SoilN (st) or SoilTemp (ave_soil_temp) property")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilTemperatureDepthFunction : BaseFunction
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];

        /// <summary>The soil</summary>
        [Link]
        private Soils.Soil soilModel = null;

        /// <summary>The depth</summary>
        [Units("mm")]
        [Description("Depth")]
        public double Depth { get; set; }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            int layer = LayerIndex(Depth, soilModel.Thickness);
            returnValue[0] = soilModel.Temperature[layer];
            return returnValue;
        }

        /// <summary>Returns the soil layer index for a specified soil depth (mm)</summary>
        /// <param name="depth">Soil depth (mm)</param>
        /// <param name="dlayer">Array of soil layer depths in the profile (mm)</param>
        /// <returns>soil layer index</returns>
        /// <exception cref="System.Exception"></exception>
        private int LayerIndex(double depth, double[] dlayer)
        {
            double cumDepth = 0.0;
            for (int i = 0; i < dlayer.Length; i++)
            {
                cumDepth = cumDepth + dlayer[i];
                if (cumDepth >= depth)
                    return i;
            }
            throw new Exception(Name + ": Specified soil depth of " + Depth.ToString() + " mm is greater than profile depth of " + cumDepth.ToString() + " mm");
        }
    }
}