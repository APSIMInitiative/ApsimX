using Models.Climate;
using Models.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// A simple class that can be used to extract the ambient 
    /// </summary>
    public static class AmbientCO2Provider
    {
        /// <summary>
        /// The default Ambient CO2 Value.
        /// </summary>
        private const double DEFAULT_AMBIENT_CO2 = 363.0;

        /// <summary>
        /// Retrieves either a default, or weather configured, ambient C02 value.
        /// </summary>
        public static double RetrieveAmbientCO2Value(IWeather weather)
        {
            if (weather is null || weather is not Weather weatherModel)
                return DEFAULT_AMBIENT_CO2;

            var co2Value = weatherModel.FindChild<CO2Value>();
            return co2Value?.ConstantValue ?? DEFAULT_AMBIENT_CO2;
        }
    }
}
