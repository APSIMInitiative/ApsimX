namespace Models.DCAPST.Interfaces
{
    /// <summary>
    /// Represents a model that simulates a leaf-water interaction
    /// </summary>
    public interface IWaterInteraction
    {
        /// <summary>
        /// Sets the current conditions of the leaf water model
        /// </summary>
        void SetConditions(double gbs, double radiation);

        /// <summary>
        /// Calculates the resistance to water when supply is unlimited
        /// </summary>
        double UnlimitedWaterResistance(double Assimilation, double AirCO2, double IntercellularCO2);

        /// <summary>
        /// Calculates the resistance to water when supply is limited
        /// </summary>
        double LimitedWaterResistance(double wateruse);

        /// <summary>
        /// Finds the total leaf CO2 conductance at a given resistance to water
        /// </summary>
        double TotalCO2Conductance(double resistance);

        /// <summary>
        /// Finds the leaf temperature as a result of the water interaction
        /// </summary>
        double LeafTemperature(double resistance);

        /// <summary>
        /// Finds the water demand of a leaf canopy across an hour
        /// </summary>
        double HourlyWaterUse(double resistance);

        /// <summary>
        /// Leaf temperature.
        /// </summary>
        double LeafTemp { get; set; }

        /// <summary>
        /// Called when the params need to be recalculated.
        /// </summary>
        void RecalculateParams();

        /// <summary> Leaf to air vapour pressure deficit </summary>
        double VPD { get; }
    }
}
