namespace Models;

/// <summary>
/// An interface for models that calculate Eo.
/// </summary>
public interface ICalculateEo
{
    /// <summary>Calculate the atmospheric potential evaporation rate for a zone.</summary>
    /// <param name="zone">The microclimate zone to calculate eo for.</param>
    double Calculate(MicroClimateZone zone);
}