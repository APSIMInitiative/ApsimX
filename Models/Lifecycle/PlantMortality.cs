using System;
using Models.Core;
using Models.Functions;
using Models.LifeCycle;
using Models.PMF;
using Models.PMF.Interfaces;
namespace Models.LifeCycle
{
    /// <summary>
    /// # [Name]
    /// Specifies the killing of plants by Pest/Disease.  The biomass and dimensions of organs will be reduced
    /// in Proportion to the number of plants killed.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCyclePhase))]
    public class PlantMortality : Model
    {
        /// <summary>Returns the potential damage that an individual can cause per day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g")]
        private IFunction RateOfPlantMortalityPerIndividual = null;

        [Link(Type = LinkType.Ancestor)]
        private LifeCyclePhase ParentStage = null;

        /// <summary> Select host plant that Pest/Disease may bother </summary>
        [Description("Select host plant that Pest/Disease may bother")]
        [Display(Type = DisplayType.Model, ModelType = typeof(IPlantDamage))]
        public IPlantDamage HostPlant { get; set; }

        [EventSubscribe("DoPestDiseaseDamage")]
        private void DoPestDiseaseDamage(object sender, EventArgs e)
        {
            HostPlant.ReducePopulation(ParentStage.TotalPopulation * RateOfPlantMortalityPerIndividual.Value());
        }
    }
}
