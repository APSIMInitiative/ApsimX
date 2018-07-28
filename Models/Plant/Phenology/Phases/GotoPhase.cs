using System;
using Models.Core;
using Models.Functions;
using System.IO;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>
    /// A special phase that jumps to another phase.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class GotoPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>The phase name to goto</summary>
        [Description("PhaseNameToGoto")]
        public string PhaseNameToGoto { get; set; }

        /// <summary>Gets the tt for today.</summary>
        [XmlIgnore]
        public double TTForTimeStep { get; set; }

        /// <summary>Gets the tt in phase.</summary>
        [XmlIgnore]
        public double TTinPhase { get; set; }

        /// <summary>Gets the fraction complete.</summary>
        [XmlIgnore]
        public double FractionComplete { get; set; }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Should not be called in this class</summary>
        public bool DoTimeStep(ref double PropOfDayToUse) { throw new Exception("Cannot call rewind class"); }

        /// <summary>Writes the summary.</summary>
        public void WriteSummary(TextWriter writer) { writer.WriteLine("      " + Name); }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase() {}
    }
}
