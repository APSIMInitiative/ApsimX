using APSIM.Shared.Soils;

namespace Models.WaterModel
{
    /// <summary>Interface for a soil.</summary>
    public interface ISoil
    {
        /// <summary>Amount of water in the soil (mm).</summary>
        double[] Water { get; set; }

        /// <summary>Nitrate in the soil (kg/ha).</summary>
        double[] NO3 { get; set; }

        /// <summary>Ammonia in the soil (kg/ha).</summary>
        double[] NH4 { get; set; }

        /// <summary>Infiltration (mm).</summary>
        double Infiltration { get; }

        /// <summary>Gets todays potential runoff (mm).</summary>
        double PotentialRunoff { get; }

        /// <summary>Provides access to the soil properties.</summary>
        SoilProperties Properties { get; }
    }
}