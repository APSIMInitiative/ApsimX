namespace Models.PMF.Phen
{

    /// <summary>Interface for a function</summary>
    public interface IVrn1Expression
    {
        /// <summary>The plases name</summary>
        string Name { get; }

        /// <summary> Fraction of progress through the phase</summary>
        double Vrn1 { get; }

        /// <summary>The target for phase completion</summary>
        double pVrn2 { get; }

        /// <summary>The target for phase completion</summary>
        bool IsVernalised { get; }

    }
}