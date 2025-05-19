using Models.Core;

namespace Models.PMF.Phen
{
    /// <summary>
    /// A phase that represents no phase. Used when a plant has not been sown yet.
    /// </summary>
    public class BlankPhase : Model, IPhase
    {
        /// <summary></summary>
        public string Start { get {return "";} }

        /// <summary></summary>
        public string End { get {return "";} }

        /// <summary></summary>
        public bool IsEmerged { get {return false;} }

        /// <summary></summary>
        public double FractionComplete { get { return 0; } }

        /// <summary></summary>
        public bool DoTimeStep(ref double propOfDayToUse) { return false; }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase() {}
    }
}



