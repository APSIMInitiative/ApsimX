using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant moving activity</summary>
    /// <summary>This activity moves specified ruminants to a given pasture</summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Define the location (pastures) of specified individuals (move) and assign location at the start of the simulation")]
    [Version(1, 0, 2, "Now allows multiple RuminantFilterGroups to identify individuals to be moved")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantMove.htm")]
    public class RuminantActivityMove: CLEMRuminantActivityBase, IHandlesActivityCompanionModels
    {
        private int numberToDo;
        private int numberToSkip;
        private string pastureName = "";
        private IEnumerable<Ruminant> uniqueIndividuals;
        private IEnumerable<RuminantGroup> filterGroups;

        /// <summary>
        /// Managed pasture to move to
        /// </summary>
        [Description("Managed pasture to move to")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Not specified - general yards", typeof(GrazeFoodStore) } })]
        [System.ComponentModel.DefaultValue("Not specified - general yards")]
        public string ManagedPastureName { get; set; }

        /// <summary>
        /// Determines whether this must be performed to setup herds at the start of the simulation
        /// </summary>
        [Description("Move at start of simulation")]
        [Required]
        public bool PerformAtStartOfSimulation { get; set; }

        /// <summary>
        /// Determines whether sucklings are automatically moved with the mother or separated
        /// </summary>
        [Description("Move sucklings with mother")]
        [Required]
        public bool MoveSucklings { get; set; }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "RuminantGroup":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>()
                        );
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                        },
                        measures: new List<string>() {
                            "fixed",
                            "per head"
                        }
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
            this.InitialiseHerd(true, true);
            filterGroups = GetCompanionModelsByIdentifier<RuminantGroup>(true, false);

            // link to graze food store type (pasture) to move to
            // "Not specified" is general yards.
            pastureName = "";
            if (ManagedPastureName.StartsWith("Not specified"))
                pastureName = "";
            else
                pastureName = ManagedPastureName.Split('.')[1];
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("FinalInitialise")]
        private void OnFinalInitialise(object sender, EventArgs e)
        {
            // moved to FinalInitialise so that validation of setup can occur before performed.

            if (PerformAtStartOfSimulation)
            {
                RequestResourcesForTimestep();
                PerformTasksForTimestep();
                if (numberToDo > 0)
                {
                    AddStatusMessage("Moved individuals at start up");
                    Status = ActivityStatus.Success;
                    TriggerOnActivityPerformed();
                }
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            numberToDo = 0;
            numberToSkip = 0;
            IEnumerable<Ruminant> herd = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.AllOnFarm).Where(a => a.Location != pastureName);
            uniqueIndividuals = GetUniqueIndividuals<Ruminant>(filterGroups, herd);
            numberToDo = uniqueIndividuals?.Count() ?? 0;

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                int number = numberToDo;
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "per head":
                        valuesForCompanionModels[valueToSupply.Key] = number;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var moveShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == "Number moved").FirstOrDefault();
                if (moveShort != null)
                {
                    numberToSkip = Convert.ToInt32(numberToDo * (1 - moveShort.Available / moveShort.Required));
                    if (numberToSkip == numberToDo)
                    {
                        Status = ActivityStatus.Warning;
                        AddStatusMessage("Resource shortfall prevented any action");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (numberToDo - numberToSkip > 0)
            {
                int moved = 0;
                foreach (Ruminant ruminant in uniqueIndividuals.SkipLast(numberToSkip).ToList())
                {
                    ruminant.Location = pastureName;

                    // check if sucklings are to be moved with mother
                    if (MoveSucklings && ruminant is RuminantFemale)
                        // check if mother with sucklings
                        foreach (var suckling in (ruminant as RuminantFemale).SucklingOffspringList)
                            suckling.Location = pastureName;

                    moved++;
                }
                SetStatusSuccessOrPartial(moved != numberToDo);
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write($"\r\n<div class=\"activityentry\">Move individuals to {DisplaySummaryResourceTypeSnippet(ManagedPastureName, nullGeneralYards: true)}");
                if (MoveSucklings)
                    htmlWriter.Write(" moving sucklings with mother");
                htmlWriter.Write(".</div>");
                if (PerformAtStartOfSimulation)
                    htmlWriter.Write("\r\n<div class=\"activityentry\">These individuals will be located on the specified pasture at startup</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
