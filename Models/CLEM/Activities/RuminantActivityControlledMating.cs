using APSIM.Shared.Utilities;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.CLEM.Timers;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Adds artificial insemination to Ruminant breeding
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityBreed))]
    [Description("Adds controlled mating details to ruminant breeding")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantControlledMating.htm")]
    [Version(1, 0, 1, "")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantActivityBreed) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
    public class RuminantActivityControlledMating : CLEMRuminantActivityBase, IHandlesActivityCompanionModels
    {
        private List<ISetAttribute> attributeList;
        private ActivityTimerBreedForMilking milkingTimer;
        private RuminantActivityBreed breedingParent;
        private IEnumerable<RuminantFemale> uniqueIndividuals;
        private IEnumerable<RuminantGroup> filterGroups;

        private int numberToDo;
        private int numberToSkip;
        private int amountToSkip;
        private int amountToDo;

        /// <summary>
        /// Maximum age for mating
        /// </summary>
        [Description("Maximum female age for mating")]
        [Category("Farm", "Breeding")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier MaximumAgeMating { get; set; } = new int[] { 20, 0, 0 };

        /// <summary>
        /// Number joinings per male before male genetics replaced
        /// </summary>
        [Description("Joinings per individual male (genetics)")]
        [Category("Farm", "Genetics")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1)]
        public int JoiningsPerMale { get; set; }

        /// <summary>
        /// The available attributes for the breeding sires
        /// </summary>
        public List<ISetAttribute> SireAttributes => attributeList;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityControlledMating()
        {
            SetDefaults();
            ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

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
                            "Number mated",
                            "Number conceived"
                        },
                        measures: new List<string>() {
                            "fixed",
                            "per head",
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private new void OnStartOfSimulation(object sender, EventArgs e)
        {
            attributeList = FindAllDescendants<ISetAttribute>().ToList();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            AllocationStyle = ResourceAllocationStyle.Manual;
            InitialiseHerd(false, true);
            filterGroups = GetCompanionModelsByIdentifier<RuminantGroup>(false, true);

            milkingTimer = FindChild<ActivityTimerBreedForMilking>();

            // check that timer exists for controlled mating
            if (!TimingExists)
                Summary.WriteMessage(this, $"Breeding with controlled mating [a={Parent.Name}].[a={Name}] requires a Timer otherwise breeding will be undertaken every time-step", MessageType.Warning);

            // get details from parent breeding activity
            breedingParent = Parent as RuminantActivityBreed;
        }

        /// <summary>
        /// Provide the list of all breeders currently available
        /// </summary>
        /// <returns>A list of breeders to work with before returning to the breed activity</returns>
        private IEnumerable<RuminantFemale> GetBreeders()
        {
            // return the full list of breeders currently able to breed
            // controlled mating includes a max breeding age property, so reduces numbers mated
            var fullSetBreeders = milkingTimer != null
                ? milkingTimer.IndividualsToBreed
                : CurrentHerd(true).OfType<RuminantFemale>()
                    .Where(a => a.IsAbleToBreed & a.AgeInDays <= MaximumAgeMating.InDays);

            return fullSetBreeders;
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            amountToDo = 0;
            amountToSkip = 0;
            numberToDo = 0;
            numberToSkip = 0;
            IEnumerable<RuminantFemale> herd = GetBreeders();
            uniqueIndividuals = GetUniqueIndividuals<RuminantFemale>(filterGroups, herd);
            numberToDo = uniqueIndividuals?.Count() ?? 0;
            amountToDo = numberToDo;

            // ensure a conception rate is provided for all females
            // calculate conception rate for all individuals
            foreach (RuminantFemale female in uniqueIndividuals)
            {
                if (female.BreedDetails.ConceptionModel is null)
                    throw new ApsimXException(this, $"No conception details were found for [r={female.BreedDetails.Name}]\r\nPlease add a conception component below the [r=RuminantType]");
                female.ActivityDeterminedConceptionRate = female.BreedDetails.ConceptionModel.ConceptionRate(female);
                // identify successful matings by a positive value of rate
                female.ActivityDeterminedConceptionRate *= (RandomNumberGenerator.Generator.NextDouble() <= female.ActivityDeterminedConceptionRate) ? 1 : -1;
            }

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                switch (valueToSupply.Key.identifier)
                {
                    case "Number mated":
                        switch (valueToSupply.Key.unit)
                        {
                            case "fixed":
                                valuesForCompanionModels[valueToSupply.Key] = 1;
                                break;
                            case "per head":
                                valuesForCompanionModels[valueToSupply.Key] = numberToDo;
                                break;
                            default:
                                throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                        }
                        break;
                    case "Number conceived":
                        switch (valueToSupply.Key.unit)
                        {
                            case "fixed":
                                valuesForCompanionModels[valueToSupply.Key] = 1;
                                break;
                            case "per head":
                                // count successful conceptions
                                amountToDo = uniqueIndividuals.Where(a => a.ActivityDeterminedConceptionRate>0).Count();
                                valuesForCompanionModels[valueToSupply.Key] = amountToDo;
                                break;
                            default:
                                throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                        }
                        break;
                    default:
                        throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key));
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
                var numberShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == "Number mated").FirstOrDefault();
                if (numberShort != null)
                {
                    numberToSkip = Convert.ToInt32(numberToDo * (1 - numberShort.Available / numberShort.Required));
                    if (numberToSkip == numberToDo)
                    {
                        Status = ActivityStatus.Warning;
                        AddStatusMessage("Resource shortfall prevented any mating");
                    }

                    // set skipped individual activitymanagedconception to 0 // unmated
                    foreach (RuminantFemale female in uniqueIndividuals.Skip(numberToDo - numberToSkip))
                        female.ActivityDeterminedConceptionRate = 0;
                }

                var amountShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == "Number conceived").FirstOrDefault();
                if (amountShort != null)
                {
                    amountToSkip = Convert.ToInt32(amountToDo * (1 - amountShort.Available / amountShort.Required));
                    if (amountToSkip > 0)
                    {
                        Status = ActivityStatus.Warning;
                        AddStatusMessage("Resource shortfall prevented any mating");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            int mated = 0;
            int conceived = amountToDo - amountToSkip;
            List<RuminantFemale> selectedBreeders = uniqueIndividuals.SkipLast(numberToSkip).ToList();
            foreach (RuminantFemale ruminant in selectedBreeders)
            {
                mated++;
                // if more conceptions allowed
                if (conceived > 0)
                {
                    if (MathUtilities.IsPositive(ruminant.ActivityDeterminedConceptionRate ?? -1))
                        conceived--;
                }
                else
                    ruminant.ActivityDeterminedConceptionRate = 0;
            }
            uniqueIndividuals = selectedBreeders;
            if (mated > 0 || numberToSkip > 0 || amountToSkip > 0)
            {
                if (mated > 0)
                {
                    AddStatusMessage($"{mated} mated where {amountToDo - amountToSkip - conceived} conceived");
                }
                SetStatusSuccessOrPartial(numberToSkip + amountToSkip > 0);
            }
        }

        /// <summary>
        /// Provide the list of breeders to mate accounting for the controlled mating failure rate, and required resources
        /// </summary>
        /// <returns>A list of breeders for the breeding activity to work with</returns>
        public IEnumerable<RuminantFemale> BreedersToMate()
        {
            // fire all processes needed to account for resources required
            ManageActivityResourcesAndTasks();
            // return resulting list with conception precalculated back to the breeding activity.
            return uniqueIndividuals;
        }

        //#region validation
        ///// <summary>
        ///// Validate model
        ///// </summary>
        ///// <param name="validationContext"></param>
        ///// <returns></returns>
        //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        //{
        //    var results = new List<ValidationResult>();

        //    if (breedingParent is null)
        //    {
        //        string[] memberNames = new string[] { "Controlled mating parent" };
        //        results.Add(new ValidationResult($"Invalid parent component of [a={Name}]. Expecting [a=RuminantActivityBreed].[a=RuminantActivityControlledMating]", memberNames));
        //    }
        //    return results;
        //}
        //#endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            // set attribute with value
            IEnumerable<SetAttributeWithValue> attributeSetters = FindAllChildren<SetAttributeWithValue>();
            if (attributeSetters.Any())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write($"The Attributes of the sire are {(attributeSetters.Any() ? "specified below" : "selected at random ofrm the herd")} to ensure inheritance to offpsring");
                htmlWriter.Write("</div>");
            }
            else
            {
                // need to check for mandatory attributes
                var mandatoryAttributes = FindAncestor<Zone>().FindAllDescendants<SetAttributeWithValue>().Where(a => a.Mandatory).Select(a => a.AttributeName).Distinct();
                if (mandatoryAttributes.Any())
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write($"The mandatory attributes <span class=\"setvalue\">{string.Join("</span>,<span class=\"setvalue\">", mandatoryAttributes)}</span> required from the breeding males will be randomally selected from the herd");
                    htmlWriter.Write("</div>");
                }
            }
            return htmlWriter.ToString();
        }
        #endregion


    }
}
