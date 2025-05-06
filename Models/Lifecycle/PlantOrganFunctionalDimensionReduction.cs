using System;
using APSIM.Shared.Utilities;
using System.Linq;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;

namespace Models.LifeCycle
{
    /// <summary>
    /// For Pests/Diseases that reduce the functional area or length of an organ without removing biomass
    /// for example clogging of vescles or growing spots on leaf surfaces to block radiation interception
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LifeCyclePhase))]
    public class PlantOrganFunctionalDimensionReduction : Model
    {
        [Link]
        Zone zone = null;

        /// <summary>Returns the potential damage that an individual can cause per day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g")]
        private IFunction DimensionReductionPerIndividual = null;

        /// <summary> </summary>
        [Description("Select host organ that Pest/Disease may consume")]
        [Display(Type = DisplayType.PlantOrganList)]
        public string HostOrganName { get; set; }

        [Link(Type = LinkType.Ancestor)]
        private LifeCyclePhase ParentPhase = null;

        [EventSubscribe("DoPestDiseaseDamage")]
        private void DoPestDiseaseDamage(object sender, EventArgs e)
        {
            if (ParentPhase.Cohorts != null)
            {
                var hostOrgan = zone.Get(HostOrganName) as IHasDamageableBiomass;
                if (hostOrgan == null)
                    throw new Exception($"Cannot find host organ: {HostOrganName}");

                double DimensionReduction = 0;

                foreach (Cohort c in ParentPhase.Cohorts)
                {
                    ParentPhase.CurrentCohort = c;
                    DimensionReduction += c.Population * DimensionReductionPerIndividual.Value();
                }

                if (hostOrgan is ICanopy canopy)
                    canopy.LAI -= DimensionReduction;
                else if (hostOrgan is IRoot root)
                    root.RootLengthDensityModifierDueToDamage = DimensionReduction;
                else
                    throw new Exception("FunctionalDimensionReduction is only possible for organs implementing ICanopy or IRoot interfaces");
            }
        }
    }
}
