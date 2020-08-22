
namespace Models.PMF.Phen
{
    using System.IO;
    
    /// <summary>Interface for a function</summary>
    public interface IVrn1Expression
    {
        /// <summary>The plases name</summary>
        string Name { get; }
        
        /// <summary> Fraction of progress through the phase</summary>
        double MethColdVrn1 { get;}

        /// <summary>The target for phase completion</summary>
        double VrnSatThreshold { get; }

        /// <summary>The target for phase completion</summary>
        bool IsVernalised { get; }

    }
}