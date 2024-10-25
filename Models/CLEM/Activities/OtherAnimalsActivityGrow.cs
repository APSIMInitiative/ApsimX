using Models.Core;
using Models.CLEM.Resources;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Models.Core.Attributes;
using static Models.GrazPlan.GrazType;
using System.Collections.Generic;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;

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
        private IEnumerable<OtherAnimalsType> otherAnimalsTypes { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate OtherAnimals resource holder
            otherAnimals = Resources.FindResourceGroup<OtherAnimals>();
            if(otherAnimals != null)
                otherAnimalsTypes = otherAnimals.FindAllChildren<OtherAnimalsType>();

            if (otherAnimalsTypes == null)
            {
                string warn = $"No [r=OtherAnimalType] are available for [a={this.NameWithParent}].{Environment.NewLine}This activity will be ignored.";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
            }
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
            // grow all individuals
            foreach (OtherAnimalsTypeCohort cohort in otherAnimals.GetCohorts(null, false))
            {
                cohort.Age++;
                cohort.Weight = cohort.AnimalType.AgeWeightRelationship?.SolveY(cohort.Age) ?? 0.0;
                // death from old age
                if (cohort.Age > cohort.AnimalType.MaxAge)
                {
                    cohort.AdjustedNumber = cohort.Number;
                    cohort.AnimalType.Remove(cohort, this, "Died");
                }
            }
        }
    }
}
