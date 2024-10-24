using Models.Core;
using Models.CLEM.Resources;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using DocumentFormat.OpenXml.Drawing.Charts;
using SixLabors.ImageSharp;
using Models.CLEM.Interfaces;
using Models.PMF.Organs;
using System.Collections.Generic;
using Models.LifeCycle;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Other animals breed activity</summary>
    /// <summary>This activity handles breeding in other animals types</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manages the breeding of a specified type of other animal")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/OtherAnimalsActivityBreed.htm")]
    public class OtherAnimalsActivityBreed : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        private int malebreeders = 0;
        private int breeders = 0;

        /// <summary>
        /// name of other animal type
        /// </summary>
        [Description("Other animal type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of other animal type required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(OtherAnimals) } })]
        public string AnimalTypeName { get; set; }

        /// <summary>
        /// Offspring per female breeder
        /// </summary>
        [Description("Offspring per female breeder")]
        [Required, GreaterThanValue(0)]
        public double OffspringPerBreeder { get; set; }

        /// <summary>
        /// Breeding female age
        /// </summary>
        [Description("Breeding age (months)")]
        [Required, GreaterThanEqualValue(1)]
        public int BreedingAge { get; set; }

        /// <summary>
        /// Use local males for breeding
        /// </summary>
        [Description("Use local males for breeding")]
        [Required]
        public bool UseLocalMales { get; set; }

        /// <summary>
        /// The Other animal type this group points to
        /// </summary>
        [JsonIgnore]
        public OtherAnimalsType SelectedOtherAnimalsType;

        /// <summary>
        /// Month this timer is next due.
        /// </summary>
        [JsonIgnore]
        public DateTime NextDueDate { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OtherAnimalsActivityBreed()
        {
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get other animal type model
            SelectedOtherAnimalsType = Resources.FindResourceType<OtherAnimals, OtherAnimalsType>(this, AnimalTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "LabourRequirement":
                case "ActivityFee":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                            "Number to breed"
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


        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
            IEnumerable<OtherAnimalsTypeCohort> cohorts = SelectedOtherAnimalsType.GetCohorts(null, false).ToList();
            malebreeders = cohorts.Where(a => a.Age >= this.BreedingAge && a.Sex == Sex.Male).Sum(b => b.Number);
            breeders = 0;
            if (!UseLocalMales | malebreeders > 0)
            {
                breeders = cohorts.Where(a => a.Age >= this.BreedingAge && a.Sex == Sex.Female).Sum(b => b.Number);
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            Status = ActivityStatus.NotNeeded;
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                switch (valueToSupply.Key.type)
                {
                    case "OtherAnimalsGroup":
                        valuesForCompanionModels[valueToSupply.Key] = breeders;
                        break;
                    case "LabourRequirement":
                    case "ActivityFee":
                        switch (valueToSupply.Key.identifier)
                        {
                            case "Number to buy":
                                switch (valueToSupply.Key.unit)
                                {
                                    case "fixed":
                                        valuesForCompanionModels[valueToSupply.Key] = 1;
                                        break;
                                    case "per head":
                                        valuesForCompanionModels[valueToSupply.Key] = breeders;
                                        break;
                                    default:
                                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                                }
                                break;
                            default:
                                throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key));
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
                // get greatest shortfall by proportion
                var buyShort = shortfalls.OrderBy(a => a.Provided / a.Required).FirstOrDefault();
                int reduce = Convert.ToInt32(breeders * buyShort.Provided / buyShort.Required);
                breeders -= reduce;
                this.Status = ActivityStatus.Partial;
            }
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalBreeding")]
        private void OnCLEMAnimalBreeding(object sender, EventArgs e)
        {
            if (TimingOK)
            {
                ManageActivityResourcesAndTasks();

                if (breeders > 0)
                {
                    double newbysex = breeders * this.OffspringPerBreeder / 2.0;
                    int singlesex = 0;

                    // apply stochasticity to determine proportional numbers to integers
                    if (newbysex - Math.Truncate(newbysex) > RandomNumberGenerator.Generator.Next())
                        singlesex = Convert.ToInt32(Math.Ceiling(newbysex));
                    else
                        singlesex = Convert.ToInt32(Math.Floor(newbysex));

                    double newweight = SelectedOtherAnimalsType.AgeWeightRelationship?.SolveY(0.0) ?? 0.0;
                    if (singlesex > 1)
                    {
                        OtherAnimalsTypeCohort newmales = new OtherAnimalsTypeCohort()
                        {
                            Age = 0,
                            Weight = newweight,
                            Sex = Sex.Male,
                            Number = singlesex,
                            AdjustedNumber = singlesex,
                            SaleFlag = HerdChangeReason.Born
                        };
                        SelectedOtherAnimalsType.Add(newmales, this, null, "Births");
                        if (Status != ActivityStatus.Partial)
                            Status = ActivityStatus.Success;
                    }

                    if (newbysex - Math.Truncate(newbysex) > RandomNumberGenerator.Generator.NextDouble())
                        singlesex = Convert.ToInt32(Math.Ceiling(newbysex));
                    else
                        singlesex = Convert.ToInt32(Math.Floor(newbysex));

                    if (singlesex > 1)
                    {
                        OtherAnimalsTypeCohort newfemales = new OtherAnimalsTypeCohort()
                        {
                            Age = 0,
                            Weight = newweight,
                            Sex = Sex.Female,
                            Number = singlesex,
                            AdjustedNumber = singlesex,
                            SaleFlag = HerdChangeReason.Born
                        };
                        SelectedOtherAnimalsType.Add(newfemales, this, null, "Births");
                        if (Status != ActivityStatus.Partial)
                            Status = ActivityStatus.Success;
                    }
                }
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new ())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write($"[r={DisplaySummaryValueSnippet(AnimalTypeName, "No Other Animal Type", HTMLSummaryStyle.Resource)}] individuals must be {DisplaySummaryValueSnippet(BreedingAge, "Mature age not set", HTMLSummaryStyle.Default)} months of age to breed.");
                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (UseLocalMales)
                    htmlWriter.Write("Breeding will only occur when adult males are present in the local population.");
                else
                    htmlWriter.Write("Breeding will only regardless of whether adult males are present in the local population.");
                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write($"Each breeding female will produce {DisplaySummaryValueSnippet(OffspringPerBreeder, "Offspring not set", HTMLSummaryStyle.Default)} offspring with an equal sex ratio and rounded to whole individuals.");
                htmlWriter.Write("</div>");

                return htmlWriter.ToString();
            }
        }
        #endregion

    }
}
