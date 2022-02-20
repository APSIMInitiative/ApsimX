using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.CLEM.Groupings;
using Models.Core.Attributes;
using Models.CLEM.Reporting;
using System.Globalization;
using System.IO;
using Models.CLEM.Interfaces;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant wean activity</summary>
    /// <summary>This activity will wean the herd</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manages weaning of suckling ruminant individuals based on age and/or weight")]
    [Version(1, 1, 0, "Implements new activity control")]
    [Version(1, 0, 2, "Weaning style added. Allows decision rule (age, weight, or both to be considered.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantWean.htm")]
    public class RuminantActivityWean: CLEMRuminantActivityBase, IValidatableObject, ICanHandleIdentifiableChildModels
    {
        [Link]
        private Clock clock = null;

        private string grazeStore;
        private IEnumerable<Ruminant> uniqueIndividuals;
        private int numberToSkip = 0;
        private int numberToWean = 0;

        /// <summary>
        /// Style of weaning rule
        /// </summary>
        [Description("Weaning rule")]
        public WeaningStyle Style { get; set; }

        /// <summary>
        /// Weaning age (months)
        /// </summary>
        [Description("Weaning age (months)")]
        [Required, GreaterThanEqualValue(0)]
        public double WeaningAge { get; set; }

        /// <summary>
        /// Weaning weight (kg)
        /// </summary>
        [Description("Weaning weight (kg)")]
        [Required, GreaterThanEqualValue(0)]
        public double WeaningWeight { get; set; }

        /// <summary>
        /// Name of GrazeFoodStore (paddock) to place weaners (leave blank for general yards)
        /// </summary>
        [Description("Name of GrazeFoodStore (paddock) to place weaners in")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Not specified - general yards", "Leave at current location", typeof(GrazeFoodStore) } })]
        [System.ComponentModel.DefaultValue("Leave at current location")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Weaned individual's location required")]
        public string GrazeFoodStoreName { get; set; }
      
        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityWean()
        {
            this.SetDefaults();
            TransactionCategory = "Livestock.Manage";
        }

        /// <inheritdoc/>
        public override List<string> DefineIdentifiableChildModelIdentifiers<T>()
        {
            switch (typeof(T).Name)
            {
                case "RuminantGroup":
                    return new List<string>() {
                        "SelectIndividualsToCheck",
                    };
                    case "RuminantActivityFee":
                    //case "LabourRequirement":
                    return new List<string>() {
                        "NumberWeaned",
                        "NumberChecked",
                    };
                default:
                    return new List<string>();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(true, true);

            // activity is performed in ManageAnimals
            this.AllocationStyle = ResourceAllocationStyle.Manual;

            // check GrazeFoodStoreExists
            grazeStore = "";
            if (GrazeFoodStoreName != null && !GrazeFoodStoreName.StartsWith("Not specified"))
            {
                grazeStore = GrazeFoodStoreName.Split('.').Last();
            }
            else
            {
                var ah = this.FindInScope<ActivitiesHolder>();
                if (ah.FindAllDescendants<PastureActivityManage>().Count() != 0)
                    Summary.WriteMessage(this, String.Format("Individuals weaned by [a={0}] will be placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until moved and will require feeding while in yards.\r\nSolution: Set the [GrazeFoodStore to place weaners in] located in the properties.", this.Name), MessageType.Warning);
            }
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalMark")]
        protected override void OnGetResourcesPerformActivity(object sender, EventArgs e)
        {
            ManageActivityResourcesAndTasks();
        }

        /// <inheritdoc/>
        protected override List<ResourceRequest> DetermineResourcesForActivity()
        {
            numberToSkip = 0;
            numberToWean = 0;
            IEnumerable<Ruminant> fullherd = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.AllOnFarm).Where(a => a.Weaned == false);
            uniqueIndividuals = new List<Ruminant>();
            IEnumerable<RuminantGroup> filterGroups = GetIdentifiableChildrenByIdentifier<RuminantGroup>("SelectIndividualsToCheck", true);
            if(filterGroups.Any())
            {
                if(filterGroups.Count() >1)
                {
                    foreach (var selectFilter in filterGroups)
                    {
                        uniqueIndividuals = uniqueIndividuals.Union(selectFilter.Filter(fullherd)).DistinctBy(a => a.ID);
                    }
                }
                else
                {
                    uniqueIndividuals = filterGroups.FirstOrDefault().Filter(fullherd);
                }
                numberToWean = uniqueIndividuals?.Count() ?? 0;
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForActivity()
        {
            // if there's a finance or labour reduction
            // work out the number of individuals not weaned.

            //double labourlimit = this.LabourLimitProportion;

            // if there;s a limit reduce number to wean accordingly

            // reduce excess of the other fees and labour accordingly

            //numberToSkip = reduction number;

            //this.Status = ActivityStatus.Partial;

            return;
        }

        /// <inheritdoc/>
        protected override void PerformTasksForActivity()
        {
            if (numberToWean > 0)
            {
                foreach (Ruminant ind in uniqueIndividuals.SkipLast(numberToSkip).ToList())
                {
                    bool readyToWean = false;
                    string reason = "";
                    switch (Style)
                    {
                        case WeaningStyle.AgeOrWeight:
                            readyToWean = (ind.Age >= WeaningAge || ind.Weight >= WeaningWeight);
                            reason = (ind.Age >= WeaningAge) ? ((ind.Weight >= WeaningWeight) ? "AgeAndWeight" : "Age") : "Weight";
                            break;
                        case WeaningStyle.AgeOnly:
                            readyToWean = (ind.Age >= WeaningAge);
                            reason = "Age";
                            break;
                        case WeaningStyle.WeightOnly:
                            readyToWean = (ind.Weight >= WeaningWeight);
                            reason = "Weight";
                            break;
                    }

                    if (readyToWean)
                    {
                        ind.Wean(true, reason);

                        // leave where weaned or move to specified location
                        if (GrazeFoodStoreName != "Leave at current location")
                        if(GrazeFoodStoreName == "Not specified -general yards")
                            ind.Location = "";
                        else
                            ind.Location = grazeStore;

                        // report wean. If mother has died create temp female with the mother's ID for reporting only
                        ind.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Weaned, ind.Mother ?? new RuminantFemale(ind.BreedParams, -1, 999) { ID = ind.MotherID }, clock.Today, ind));
                    }
                }
                if (numberToSkip == 0)
                    SetStatusSuccess();
            }
            else
                this.Status = ActivityStatus.NotNeeded;
        }

        ///// <summary>
        ///// Determine the labour required for this activity based on LabourRequired items in tree
        ///// </summary>
        ///// <param name="requirement">Labour requirement model</param>
        ///// <returns></returns>
        //protected override LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        //{
        //    IEnumerable<Ruminant> herd = CurrentHerd(false);
        //    double daysNeeded = 0;
        //    var returnArgs = new LabourRequiredArgs(daysNeeded, TransactionCategory, this.PredictedHerdName);
        //    if (requirement.UnitType == LabourUnitType.Fixed)
        //        returnArgs.DaysNeeded = requirement.LabourPerUnit;
        //    else
        //    {
        //        foreach (RuminantGroup item in filterGroups)
        //        {
        //            int head = item.Filter(GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.NotMarkedForSale).Where(a => a.Weaned == false)).Count();
        //            switch (requirement.UnitType)
        //            {
        //                case LabourUnitType.Fixed:
        //                    break;
        //                case LabourUnitType.perHead:
        //                    daysNeeded += head * requirement.LabourPerUnit;
        //                    break;
        //                case LabourUnitType.perUnit:
        //                    double numberUnits = head / requirement.UnitSize;
        //                    if (requirement.WholeUnitBlocks)
        //                        numberUnits += Math.Ceiling(numberUnits);

        //                    daysNeeded = numberUnits * requirement.LabourPerUnit;
        //                    break;
        //                default:
        //                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
        //            }
        //        }
        //    }
        //    return returnArgs;
        //}

        #region validation
        /// <summary>
        /// Validate this model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (!FindAllChildren<RuminantGroup>().Any())
            {
                string[] memberNames = new string[] { "Specify individuals" };
                results.Add(new ValidationResult($"No individuals have been specified by [f=RuminantGroup] to be considered for weaning in [a={Name}]. Provide at least an empty RuminantGroup to consider all individuals.", memberNames));
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Individuals are weaned at ");
                if (Style == WeaningStyle.AgeOrWeight | Style == WeaningStyle.AgeOnly)
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + WeaningAge.ToString("#0.#") + "</span> months");
                    if (Style == WeaningStyle.AgeOrWeight)
                        htmlWriter.Write(" or  ");
                }
                if (Style == WeaningStyle.AgeOrWeight | Style == WeaningStyle.WeightOnly)
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + WeaningWeight.ToString("##0.##") + "</span> kg");
                }
                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">Weaned individuals will ");
                if (GrazeFoodStoreName == "Leave at current location")
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">remain at the location they were weaned");
                }
                else
                {
                    htmlWriter.Write("be place in ");
                    if (GrazeFoodStoreName == null || GrazeFoodStoreName == "" || GrazeFoodStoreName == "Not specified - general yards")
                        htmlWriter.Write("<span class=\"resourcelink\">Not specified - general yards</span>");
                    else
                        htmlWriter.Write("<span class=\"resourcelink\">" + GrazeFoodStoreName + "</span>");
                }
                htmlWriter.Write("</div>");
                // warn if natural weaning will take place
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
