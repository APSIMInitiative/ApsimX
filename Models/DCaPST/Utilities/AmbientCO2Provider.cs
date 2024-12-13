using Models.Climate;
using Models.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// A simple class that can be used to extract the ambient CO2.
    /// </summary>
    public class AmbientCO2Provider
    {
        /// <summary>
        /// The default Ambient CO2 Value.
        /// </summary>
        private const double DEFAULT_AMBIENT_CO2 = 420.0;

        /// <summary>
        /// Indicates whether the weather model has a CO2Value component.
        /// </summary>
        private readonly bool hasCO2Value;

        /// <summary>
        /// Reference to the weather model.
        /// </summary>
        private readonly Weather weatherModel;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AmbientCO2Provider(IWeather weather)
        {
            if (weather is Weather model)
            {
                weatherModel = model;
                hasCO2Value = weatherModel.FindChild<CO2Value>() != null;
            }
            else
            {
                hasCO2Value = false;
            }
        }

        /// <summary>
        /// Retrieves either a default, or weather-configured, ambient CO2 value.
        /// </summary>
        public double RetrieveAmbientCO2Value()
        {
            if (!hasCO2Value || weatherModel is null)
                return DEFAULT_AMBIENT_CO2;

            // If we get here, the weather has a CO2 value component, which means that the weather CO2
            // would've been set correctly (using either the yearly value from the csv or the constant value).
            return weatherModel.CO2;
        }
    }
}