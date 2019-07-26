namespace Models.WaterModel
{
    using APSIM.Shared.APSoil;

    /// <summary>Interface for a soil.</summary>
    public interface ISoil
    {
        /// <summary>Amount of water in the soil (mm).</summary>
        double[] Water { get; set; }

        /// <summary>Infiltration (mm).</summary>
        double Infiltration { get; }

        /// <summary>Gets todays potential runoff (mm).</summary>
        double PotentialRunoff { get; }

        /// <summary>Provides access to the soil properties.</summary>
        Soil Properties { get; }
    }
}