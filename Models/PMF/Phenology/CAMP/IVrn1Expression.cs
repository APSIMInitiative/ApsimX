namespace Models.PMF.Phen
{

    /// <summary>Interface for a function</summary>
    public interface IVrnExpression
    {
        /// <summary>The plases name</summary>
        string Name { get; }
        
        /// <summary> Vrn1 expression</summary>
        double Vrn1 { get;}

        /// <summary> Vrn 2 expression</summary>
        double Vrn2 { get; }

        /// <summary> Vrn 3 expression</summary>
        double Vrn3 { get; }
        
        /// <summary> base Vrn expression</summary>
        double BaseVrn { get; }
        
        /// <summary> Maximum Vrn expression</summary>
        double MaxVrn { get; }

        /// <summary>The target for phase completion</summary>
        bool IsVernalised { get; }

    }
}