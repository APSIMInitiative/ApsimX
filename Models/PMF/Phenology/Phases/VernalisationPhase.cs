using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using System.IO;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// /// # [Name] Phase
    /// The [Name] phase goes from [Start] stage to [End] stage and reaches [End] when
    /// vernalisation saturation occurs
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class VernalisationPhase : Model, IPhase, IPhaseWithTarget
    {
        [Link]
        private IVrn1Expression CAMP = null;

        [Link]
        private Phenology phenology = null;

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Description("End")]
        public string End { get; set; }

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
            Target = CAMP.VrnSatThreshold;
            ProgressThroughPhase = CAMP.MethColdVrn1;
            double HS = phenology.FindChild<IFunction>("HaunStage").Value();
            double RelativeBasicVegetative = Math.Max(1, HS / 1.1);
            double RelativeVrn1Expression = Math.Max(1, ProgressThroughPhase / Target);
            FractionComplete = Math.Min(RelativeBasicVegetative, RelativeVrn1Expression);
            
            return CAMP.IsVernalised;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase() { ProgressThroughPhase = 0.0; }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }
    }
}

      
      
