using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Phen
{
    [Serializable]
    public class GotoPhase : Phase
    {
        public override double DoTimeStep(double PropOfDayToUse) { throw new Exception("Cannot call rewind class"); }
        public override double FractionComplete { get { throw new Exception("Cannot call rewind class"); } }
        public string PhaseNameToGoto;
    }
}
