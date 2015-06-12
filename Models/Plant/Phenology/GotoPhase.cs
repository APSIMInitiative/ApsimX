using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Phen
{
    /// <summary>
    /// A special phase that jumps to another phase.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class GotoPhase : Phase
    {
        /// <summary>
        /// This function increments thermal time accumulated in each phase
        /// and returns a non-zero value if the phase target is met today so
        /// the phenology class knows to progress to the next phase and how
        /// much tt to pass it on the first day.
        /// </summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot call rewind class</exception>
        public override double DoTimeStep(double PropOfDayToUse) { throw new Exception("Cannot call rewind class"); }
        /// <summary>Gets the fraction complete.</summary>
        /// <value>The fraction complete.</value>
        /// <exception cref="System.Exception">Cannot call rewind class</exception>
        public override double FractionComplete { get { throw new Exception("Cannot call rewind class"); } }

        /// <summary>The phase name to goto</summary>
        [Description("PhaseNameToGoto")]
        public string PhaseNameToGoto { get; set; }
    }
}
