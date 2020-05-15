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
    /// For Pests/Diseases that take assimilate dirrect from the vessels. 
    /// for example aphids
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCyclePhase))]
    public class PlantAssimilateConsumption : Model
    {
        /// <summary>Returns the potential damage that an individual can cause per day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g")]
        private IFunction RateOfPlantAssimilateConsumptionPerIndividual = null;

        [Link(Type = LinkType.Ancestor)]
        private LifeCyclePhase ParentStage = null;

        /// <summary> Select host plant that Pest/Disease may bother </summary>
        [Description("Select host plant that Pest/Disease may bother")]
        [Display(Type = DisplayType.Model, ModelType = typeof(IPlantDamage))]
        public IPlantDamage HostPlant { get; set; }

        [EventSubscribe("DoPestDiseaseDamage")]
        private void DoPestDiseaseDamage(object sender, EventArgs e)
        {
            HostPlant.RemoveAssimilate(ParentStage.TotalPopulation * RateOfPlantAssimilateConsumptionPerIndividual.Value());
        }
    }
}
