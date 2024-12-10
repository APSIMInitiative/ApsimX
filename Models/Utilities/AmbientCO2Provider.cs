using Models.Climate;
using Models.Core;
using Models.Interfaces;

namespace Models.Utilities
{
    /// <summary>
    /// Provides the ambient CO2 value from the weather data if available, or defaults to a predefined value.
    /// </summary>
    public class AmbientCO2Provider
    {
        /// <summary>
        /// Gets the resultant ambient CO2 value.
        /// </summary>
        public double AmbientCO2Value { get; }

        /// <summary>
        /// The weather object.
        /// </summary>
        private IWeather Weather { get; }

        /// <summary>
        /// The default ambient CO2 value.
        /// </summary>
        private const double DEFAULT_AMBIENT_CO2 = 420.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbientCO2Provider"/> class.
        /// </summary>
        /// <param name="weather">The weather object to retrieve CO2 data from.</param>
        public AmbientCO2Provider(IWeather weather)
        {
            Weather = weather;
            AmbientCO2Value = RetrieveAmbientCO2Value();
        }

        /// <summary>
        /// Calculates the ambient CO2 value.
        /// </summary>
        /// <returns>The ambient CO2 value.</returns>
        private double RetrieveAmbientCO2Value()
        {
            if (Weather is not Model model) return DEFAULT_AMBIENT_CO2;

            var co2Value = model.FindChild<CO2Value>(nameof(CO2Value));

            return co2Value?.ConstantValue > 0.0 ? 
                co2Value.ConstantValue : DEFAULT_AMBIENT_CO2;
        }
    }
}
