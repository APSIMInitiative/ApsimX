using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.CLEM.Groupings;
using System.ComponentModel.DataAnnotations;

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
    public class RuminantActivityDeath : CLEMRuminantActivityBase, IHandlesActivityCompanionModels, IValidatableObject
    {
        private IEnumerable<IRuminantDeathGroup> filterGroups;

        /// <summary>
        /// List of current individuals to consider for death
        /// </summary>
        public List<Ruminant> CurrentIndividuals { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityDeath()
        {
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "RuminantDeathGroup":
                case "RuminantDeathGroupCondition":
                case "RuminantDeathGroupRate":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() 
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            InitialiseHerd(true, true);
            filterGroups = Structure.FindChildren<IRuminantDeathGroup>();
        }

        /// <summary>Function to determine which animals have died and remove from the population</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalDeath")]
        private void OnCLEMAnimalDeath(object sender, EventArgs e)
        {
            if (!TimingOK) return;
            Status = ActivityStatus.NotNeeded;

            CurrentIndividuals = CurrentHerd().ToList();
            List<Ruminant> died  = CurrentIndividuals.Where(a => a.Died).ToList();
            CurrentIndividuals.RemoveAll(a => a.Died);

            foreach (var group in filterGroups)
            {
                died.AddRange(group.DetermineDeaths((group as RuminantGroup).Filter(CurrentIndividuals)));
                CurrentIndividuals.RemoveAll(a => a.Died);
            }

            // remove individuals that died from the herd.
            if (died.Count > 0)
            {
                Status = ActivityStatus.Success;
                HerdResource.RemoveRuminant(died, this);
            }

            //todo: check if any individuals not checked
            // not easy to achieve as we need to remember which individuals have been assessed, probably with another list. 
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // if no filter groups error
            var filterGroups = Structure.FindChildren<IRuminantDeathGroup>();
            if(!filterGroups.Any())
            {
                yield return new ValidationResult($"At least one [RuminantDeathGroup] is required as a child of the [a=RuminantActivityDeath] component [{NameWithParent}]", new string[] { "Missing FilterGroup" });
            }
        }
        #endregion

    }
}
