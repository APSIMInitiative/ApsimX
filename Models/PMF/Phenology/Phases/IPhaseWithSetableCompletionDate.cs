using System;
using System.IO;
namespace Models.PMF.Phen
{

    /// <summary>Interface for a function</summary>
    public interface IPhaseWithSetableCompletionDate
    {
        /// <summary> d-mmm date to complete the phase</summary>
        string DateToProgress { get; set; }
    }
}