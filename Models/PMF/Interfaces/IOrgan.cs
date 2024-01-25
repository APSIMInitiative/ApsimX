using Models.Core;

namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Organ interface
    /// </summary>
    public interface IOrgan : IModel
    {
        /// <summary>Harvest the organ.</summary>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        double Harvest();

        /// <summary>
        /// Maintenance respiration.
        /// </summary>
        double MaintenanceRespiration { get; }
    }
}
