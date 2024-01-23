using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// When the specified start phase is reached, phenology is rewound to
    /// a specified phase.
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

        /// <summary>The end stage name.</summary>
        public string End
        {
            get
            {
                if (phenology == null)
                    phenology = FindInScope<Phenology>();
                return phenology.FindChild<IPhase>(PhaseNameToGoto)?.Start;
            }
        }
        /// <summary>Is the phase emerged from the 
        /// ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = true;

        /// <summary>The phase name to goto</summary>
        [Description("PhaseNameToGoto")]
        [Display(Type = DisplayType.CropPhaseName)]
        public string PhaseNameToGoto { get; set; }

        /// <summary>Gets the fraction complete.</summary>
        [JsonIgnore]
        public double FractionComplete { get; }

        /// <summary>Thermal time target</summary>
        [JsonIgnore]
        public double Target { get; set; }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Should not be called in this class</summary>
        public bool DoTimeStep(ref double PropOfDayToUse)
        {
            PropOfDayToUse = 0;
            phenology.SetToStage((double)phenology.IndexFromPhaseName(PhaseNameToGoto) + 1);
            return false;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase() { }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Paragraph($"When the {Start} phase is reached, phenology is rewound to the {PhaseNameToGoto} phase.");
        }
    }
}
