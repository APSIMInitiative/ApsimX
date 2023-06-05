using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from the specified start stage to the specified end stage
    /// and reaches end when vernalisation saturation occurs.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class VernalisationPhase : Model, IPhase, IPhaseWithTarget
    {
        [Link]
        private IVrn1Expression CAMP = null;

        [Link]
        private Phenology phenology = null;

        private double fractionVrn1AtEmergence = 0.0;

        private bool firstDay = true;

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Is the phase emerged from the ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = true;

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [JsonIgnore]
        public double FractionComplete { get; set; }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Thermal time target to end this phase.</summary>
        [JsonIgnore]
        public double Target { get; set; }

        /// <summary>Summarise gene expression from CAMP into phenological progress</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {


            if (firstDay)
            {
                fractionVrn1AtEmergence = CAMP.Vrn1;
                firstDay = false;
            }
            Target = Math.Max(CAMP.pVrn2, 1.0);
            double RelativeVrn1Expression = Math.Min(1, (CAMP.Vrn1 - fractionVrn1AtEmergence) / (Target - fractionVrn1AtEmergence));

            double HS = phenology.FindChild<IFunction>("HaunStage").Value();
            double RelativeBasicVegetative = Math.Min(1, HS / 1.1);

            FractionComplete = Math.Min(RelativeBasicVegetative, RelativeVrn1Expression);

            ProgressThroughPhase = RelativeVrn1Expression;

            return CAMP.IsVernalised;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            firstDay = true;
            fractionVrn1AtEmergence = 0.0;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Paragraph($"The {Name} phase goes from the {Start} stage to the {End} stage and reaches {End} when vernalisation saturation occurs.");
        }
    }
}
