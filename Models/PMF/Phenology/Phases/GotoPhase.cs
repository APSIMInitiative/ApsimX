using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using System.IO;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// #[Name]
    /// When [Start] is reached phenology is rewound to [PhaseNameToGoTo]
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class GotoPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private Phenology phenology = null;


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

        /// <summary>Gets the fraction complete.</summary>
        [JsonIgnore]
        public double FractionComplete { get;}

        /// <summary>Thermal time target</summary>
        [JsonIgnore]
        public double Target { get; set; }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Should not be called in this class</summary>
        public bool DoTimeStep(ref double PropOfDayToUse)
        {
            PropOfDayToUse = 0;
            phenology.SetToStage((double)phenology.IndexFromPhaseName(PhaseNameToGoto)+1);
            return false;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase() {}
    }
}
