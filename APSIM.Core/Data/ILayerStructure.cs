namespace APSIM.Core;

/// <summary>
/// Defines an object that has a soil layer structure. Used by locator to find soil thickness values.
/// </summary>
public interface ILayerStructure
{
    /// <summary>Soil layer thicknesses (mm).</summary>
    double[] Thickness { get; }
}