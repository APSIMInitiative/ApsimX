using System.IO;
namespace Models.PMF.Phen
{

    /// <summary>Interface for a function</summary>
    public interface IPhaseWithSetableCompletionDate : IPhase
    {
        /// <summary> ThermalTimeTarget</summary>
        string DateToProgress { get; set; }
    }
}