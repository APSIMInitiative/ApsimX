using System;
using Models.Core;
using Models.PMF.Phen;
using Models.Functions;

namespace Models.PMF.Struct
{
    /// <summary> # [Name]
    /// Sets the number of buds on each mains stem to the valud of it child on the [SetStage] </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Structure))]
    public class BudNumberFunction : Model
    {
        [Link]
        Plant Plant = null;

        [Link]
        Structure structure = null;

        [Link]
        private IFunction FractionOfBudBurst = null;

        /// <summary>The stage on which bud number is set</summary>
        [Description("The event that triggers setting of the bud number")]
        public string SetStage { get; set; }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == structure.CohortInitialisationStage)
                structure.PrimaryBudNo = Plant.SowingData.BudNumber;
            if (phaseChange.StageName == structure.LeafInitialisationStage)
            {
                structure.PrimaryBudNo = Plant.SowingData.BudNumber * FractionOfBudBurst.Value();
                structure.TotalStemPopn = structure.MainStemPopn;
            }
        }
    }
}
