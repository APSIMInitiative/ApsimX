using System;
using System.Linq;
using APSIM.Shared.Utilities;
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
        [Link]
        Zone zone = null;

        /// <summary>Returns the potential damage that an individual can cause per day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g")]
        private IFunction OrganWtConsumptionPerIndividual = null;

        [Link(Type = LinkType.Ancestor)]
        private LifeCyclePhase ParentPhase = null;

        /// <summary> </summary>
        [Description("Select host organ that Pest/Disease may consume")]
        [Display(Type = DisplayType.PlantOrganList)]
        public string HostOrganName { get; set; }

        [EventSubscribe("DoPestDiseaseDamage")]
        private void DoPestDiseaseDamage(object sender, EventArgs e)
        {
            double fractionLiveToConsume = 0;
            double organWtConsumed = 0;
            if (ParentPhase.Cohorts != null)
            {
                var hostOrgan = zone.Get(HostOrganName) as IHasDamageableBiomass;
                if (hostOrgan == null)
                    throw new Exception($"Cannot find host organ: {HostOrganName}");

                foreach (Cohort c in ParentPhase.Cohorts)
                {
                    ParentPhase.CurrentCohort = c;
                    organWtConsumed += c.Population * OrganWtConsumptionPerIndividual.Value();
                    DamageableBiomass live = hostOrgan.Material.FirstOrDefault(m => m.IsLive);
                    fractionLiveToConsume += Math.Min(1, MathUtilities.Divide(organWtConsumed, live.Total.Wt,0));
                }
                // Commented out the line below. The simulation: PotatoPsyllidDamageTest
                // in file Prototypes/Lifecycle/PotatoPests.apsimx has never worked correctly
                // because it was talking to a leaf instance that wasn't attached to a plant.
                // That has been fixed but now the line below throws inside of leaf.
                hostOrgan.RemoveBiomass(Math.Min(1,fractionLiveToConsume));
            }

        }
    }
}
