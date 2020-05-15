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
    /// Specifies the removal of organ biomass by Pest/Disease.  If organs implement ICanopy this will remove LAI in 
    /// proportion to the amount of biomass removed.  If organ implements IRoot RLD will be decreased in proportion
    /// biomass removed
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCyclePhase))]
    public class PlantOrganConsumption : Model
    {
        /// <summary>Returns the potential damage that an individual can cause per day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g")]
        private IFunction RateOfOrganConsumptionPerIndividual = null;

        [Link(Type = LinkType.Ancestor)]
        private LifeCyclePhase ParentStage = null;

        /// <summary>Host plant that Pest/Disease bothers</summary>
        [Description("Select host plant that Pest/Disease may bother")]
        [Display(Type = DisplayType.Model, ModelType = typeof(IPlantDamage))]
        public IPlantDamage HostPlant { get; set; }
        
        /// <summary> </summary>
        [Description("Select host organ that Pest/Disease may consume")]
        [Display(Type = DisplayType.Model, ModelType = typeof(IOrganDamage))]
        public IOrganDamage HostOrgan { get; set; }

        [EventSubscribe("DoPestDiseaseDamage")]
        private void DoPestDiseaseDamage(object sender, EventArgs e)
        {
            OrganBiomassRemovalType consumption = new OrganBiomassRemovalType();
            consumption.FractionLiveToRemove = Math.Max(1,ParentStage.TotalPopulation * RateOfOrganConsumptionPerIndividual.Value() * HostOrgan.Live.Wt);
            HostPlant.RemoveBiomass(HostOrgan.Name, "Graze", consumption);
        }
    }
}
