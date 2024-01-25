using System.IO;
namespace Models.PMF.Phen
{

    /// <summary>Interface for a function</summary>
    public interface IPhaseWithTarget : IPhase
    {
        /// <summary> ThermalTimeTarget</summary>
        double Target { get; }

        /// <summary>Gets the t tin phase.</summary>
        double ProgressThroughPhase { get; set; }
    }
}