using System;
using System.Linq;
using Models.Core;
using Models.Functions;
using Models.PMF;
using Models.PMF.Interfaces;

namespace Models.LifeCycle
{
    /// <summary>
    /// Specifies the removal of organ biomass by Pest/Disease.  If organs implement ICanopy this will remove LAI in 
    /// proportion to the amount of biomass removed.  If organ implements IRoot RLD will be decreased in proportion
    /// biomass removed
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCyclePhase))]
    public class PlantOrganConsumption : Model
    {
        /// <summary>Returns the potential damage that an individual can cause per day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g")]
        private IFunction OrganWtConsumptionPerIndividual = null;

        [Link(Type = LinkType.Ancestor)]
        private LifeCyclePhase ParentPhase = null;

        /// <summary> </summary>
        [Description("Select host organ that Pest/Disease may consume")]
        [Display(Type = DisplayType.Model, ModelType = typeof(IOrganDamage))]
        public IHasDamageableBiomass HostOrgan { get; set; }

        [EventSubscribe("DoPestDiseaseDamage")]
        private void DoPestDiseaseDamage(object sender, EventArgs e)
        {
            double fractionLiveToConsume = 0;
            double organWtConsumed = 0;
            if (ParentPhase.Cohorts != null)
            {
                foreach (Cohort c in ParentPhase.Cohorts)
                {
                    ParentPhase.CurrentCohort = c;
                    organWtConsumed += c.Population * OrganWtConsumptionPerIndividual.Value();
                    DamageableBiomass live = HostOrgan.Material.FirstOrDefault(m => m.IsLive);
                    fractionLiveToConsume += Math.Max(1, organWtConsumed / live.Total.Wt);
                }
                HostOrgan.RemoveBiomass(fractionLiveToConsume);
            }

        }
    }
}
