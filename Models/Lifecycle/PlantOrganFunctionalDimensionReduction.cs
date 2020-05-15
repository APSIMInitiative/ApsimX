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
    /// For Pests/Diseases that reduce the functional area or length of an organ without removing biomass
    /// for example clogging of vescles or growing spots on leaf surfaces to block radiation interception
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCyclePhase))]
    public class PlantOrganFunctionalDimensionReduction : Model
    {
        /// <summary>Returns the potential damage that an individual can cause per day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g")]
        private IFunction RateOfOrganObstructionPerIndividual = null;

        [Link(Type = LinkType.Ancestor)]
        private LifeCyclePhase ParentStage = null;

        /// <summary>Host plant that Pest/Disease bothers</summary>
        [Description("Select host plant that Pest/Disease may bother")]
        [Display(Type = DisplayType.Model, ModelType = typeof(IPlantDamage))]
        public IPlantDamage HostPlant { get; set; }
        
        /// <summary> </summary>
        [Description("Select host organ that Pest/Disease may Obstruct")]
        [Display(Type = DisplayType.Model, ModelType = typeof(IOrganDamage))]
        public IOrganDamage HostOrgan { get; set; }

        [EventSubscribe("DoPestDiseaseDamage")]
        private void DoPestDiseaseDamage(object sender, EventArgs e)
        {
            HostPlant.ReduceCanopy(ParentStage.TotalPopulation * RateOfOrganObstructionPerIndividual.Value());
        }
    }
}
