using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using System.IO;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>Describe the phenological development through a Vernalisation phase</summary>
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
        [XmlIgnore]
        public double FractionComplete { get; set; }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [XmlIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Thermal time target to end this phase.</summary>
        [XmlIgnore]
        public double Target { get; set; }

        /// <summary>Summarise gene expression from CAMP into phenological progress</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            Target = CAMP.Vrn1Target;
            ProgressThroughPhase = CAMP.MethVrn1;
            double HS = (Apsim.Find(phenology, "HaunStage") as IFunction).Value();
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

      
      
