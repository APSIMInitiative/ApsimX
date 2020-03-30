namespace UserInterface.Interfaces
{
    using System;

    /// <summary>
    /// This interface defines the API for talking to an initial water view.
    /// </summary>
    public interface IInitialWaterView
    {
        /// <summary>
        /// Invoked when the user changes the percent full edit box.
        /// </summary>
        event EventHandler OnPercentFullChanged;

        /// <summary>
        /// Invoked when the user changes the FilledFromTop option
        /// </summary>
        event EventHandler OnFilledFromTopChanged;

        /// <summary>
        /// Invoked when the user changes the depth of wet soil
        /// </summary>
        event EventHandler OnDepthWetSoilChanged;

        /// <summary>
        /// Invoked when the user changes PAW
        /// </summary>
        event EventHandler OnPAWChanged;

        /// <summary>
        /// Invoked when the user changes the relative to field.
        /// </summary>
        event EventHandler OnRelativeToChanged;

        /// <summary>
        /// Invoked when the user changes the way starting water is specified
        /// </summary>
        event EventHandler OnSpecifierChanged;

        /// <summary>
        /// Gets or sets the percent full amount.
        /// </summary>
        int PercentFull { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether starting water is specified by the depth of
        /// wet soil. If not, then fraction full will be used
        /// </summary>
        bool FilledByDepth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether initial water should be filled from the top.
        /// </summary>
        bool FilledFromTop { get; set; }

        /// <summary>
        /// Gets or sets the depth of wet soil
        /// </summary>
        int DepthOfWetSoil { get; set; }

        /// <summary>
        /// Gets or sets the PAW (mm)
        /// </summary>
        int PAW { get; set; }

        /// <summary>
        /// Gets or sets the crop that initial was is relative to
        /// </summary>
        string RelativeTo { get; set; }

        /// <summary>
        /// Gets or sets the list of crops for the relative to field
        /// </summary>
        string[] RelativeToCrops { get; set; }

        /// <summary>
        /// Gets the initial water graph.
        /// </summary>
        Views.GraphView Graph { get; }
    }
}