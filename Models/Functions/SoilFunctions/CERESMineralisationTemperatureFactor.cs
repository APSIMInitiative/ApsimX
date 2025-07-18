using System;
using APSIM.Core;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>Temperature function for soil processes except denitrification. Originally taken from CERES.
    /// Functional form is (ST-BaseST)^2/(OptSt-BaseSt)^2</summary>

    [Serializable]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    public class CERESMineralisationTemperatureFactor : Model, IFunction
    {
        private double[] tf;

        [Link]
        readonly ISoilTemperature soiltemperature = null;

        /// <summary>
        /// Base soil temperature for the temperature function in Nutrient. Default = 0.0 C.
        /// </summary>
        [Description("Soil temperature function base temperature (oC)")]
        public double MineralisationSTBase { get; set; } = 0.0;

        /// <summary>
        /// Base soil temperature for the temperature function in Nutrient. Default = 32.0 C.
        /// </summary>
        [Description("Soil temperature function optimum (oC)")]
        public double MineralisationSTOpt { get; set; } = 32.0;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES mineralisation temperature factor Model");

            return tf[arrayIndex];
        }

        /// <summary>Invoked when soil temperature changes.</summary>
        /// <value>The value.</value>
        [EventSubscribe("SoilTemperatureChanged")]
        private void OnSoilTemperatureChanged(object sender, EventArgs e)
        {
            double[] temperature = soiltemperature.Value;

            if (tf == null)
                tf = new double[temperature.Length];

            for (int i = 0; i < temperature.Length; i++)
            {
                if (temperature[i] > MineralisationSTBase)
                    tf[i] = Math.Pow(temperature[i] - MineralisationSTBase, 2) / Math.Pow(MineralisationSTOpt - MineralisationSTBase, 2);
                if (tf[i] > 1) tf[i] = 1;
            }
        }
    }
}