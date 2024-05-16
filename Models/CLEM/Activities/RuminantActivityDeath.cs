using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System.Text.Json.Serialization;
using Models.CLEM.Groupings;
using APSIM.Shared.Utilities;
using System.ComponentModel.DataAnnotations;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using Models.GrazPlan;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant death activity</summary>
    /// <summary>This activity determines the death of individuals in the herd</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Determines death of ruminants.")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantDeath.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantActivityDeath : CLEMRuminantActivityBase, IValidatableObject
    {
        private IEnumerable<IRuminantDeathGroup> filterGroups;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityDeath()
        {
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            InitialiseHerd(true, true);
            filterGroups = FindAllChildren<IRuminantDeathGroup>();
        }

        /// <summary>Function to determine which animals have died and remove from the population</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalDeath")]
        private void OnCLEMAnimalDeath(object sender, EventArgs e)
        {
            if (!TimingOK) return;
            Status = ActivityStatus.NotNeeded;

            List<Ruminant> individuals = CurrentHerd().ToList();

            foreach (var group in filterGroups)
            {
                IEnumerable<Ruminant> individualsToCheck = (group as RuminantGroup).Filter(individuals); 
                group.DetermineDeaths(individualsToCheck);
                foreach (var individual in individualsToCheck)
                    individuals.Remove(individual);
            }

            // remove individuals that died from the herd.
            var died = CurrentHerd().Where(a => a.Died);
            if (died.Any())
            {
                Status = ActivityStatus.Success;
                HerdResource.RemoveRuminant(died, this);
            }

            // if any individuals not checked
            if (individuals.Any())
            {
                string warn = $"Some specified individuals not considered in {NameWithParent}{Environment.NewLine}SOLUTION: Ensure [FilterGroups] include all individuals";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
            }
        }

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // if no filter groups error
            var filtergroups = FindAllChildren<IRuminantDeathGroup>();
            if(!filtergroups.Any())
            {
                string[] memberNames = new string[] { "Missing FilterGroup" };
                results.Add(new ValidationResult($"At least one [RuminantDeathGroup] is required as a child of the [a=RuminantActivityDeath] component [{NameWithParent}]", memberNames));
            }


            return results;
        }
        #endregion

    }
}
