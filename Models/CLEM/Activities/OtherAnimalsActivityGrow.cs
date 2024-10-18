using Models.Core;
using Models.CLEM.Resources;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Models.Core.Attributes;
using static Models.GrazPlan.GrazType;

namespace Models.CLEM.Activities
{
    /// <summary>Other animals grow activity</summary>
    /// <summary>This activity grows other animals and includes aging</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs the growth, aging, and mortality of all other animals")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/OtherAnimalsActivityGrow.htm")]
    public class OtherAnimalsActivityGrow : CLEMActivityBase
    {
        private OtherAnimals otherAnimals { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate OtherAnimals resource holder
            otherAnimals = Resources.FindResourceGroup<OtherAnimals>();
        }

        /// <summary>
        /// Method to age other animals
        /// This needs to be undertaken prior to herd management
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void OnCLEMAgeResources(object sender, EventArgs e)
        {
            if (otherAnimals is null)
                return;

            // grow all individuals
            foreach (OtherAnimalsType otherAnimalType in otherAnimals.FindAllChildren<OtherAnimalsType>())
                foreach (OtherAnimalsTypeCohort cohort in otherAnimalType.Cohorts.OfType<OtherAnimalsTypeCohort>())
                {
                    if (cohort.Number > 0)
                    {
                        Status = ActivityStatus.Success;
                        cohort.Age++;
                        cohort.Weight = otherAnimalType.AgeWeightRelationship?.SolveY(cohort.Age)??0.0;
                        // death from old age
                        if (cohort.Age > otherAnimalType.MaxAge)
                        {
                            otherAnimalType.Remove(cohort, this, "Died");
                        }
                    }
                }
        }
    }
}
