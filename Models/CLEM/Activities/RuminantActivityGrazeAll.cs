using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;
using Models.Core.ApsimFile;
using Models.CLEM.Groupings;
using System.Linq.Expressions;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant graze activity</summary>
    /// <summary>This activity determines how a ruminant group will graze</summary>
    /// <summary>It is designed to request food via a food store arbitrator</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Perform grazing of all herds and pastures (paddocks)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGraze.htm")]
    public class RuminantActivityGrazeAll : CLEMRuminantActivityBase, IValidatableObject
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;

        /// <summary>
        /// Number of hours grazed
        /// Based on 8 hour grazing days
        /// Could be modified to account for rain/heat walking to water etc.
        /// </summary>
        [Description("Number of hours grazed")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day"), GreaterThanValue(0)]
        public double HoursGrazed { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            bool buildTransactionFromTree = FindAncestor<ZoneCLEM>().BuildTransactionCategoryFromTree;
            string transCat = "";
            if (!(FindAncestor<ZoneCLEM>()?.BuildTransactionCategoryFromTree??false))
                transCat = TransactionCategory;

            bool usingGrowPF = FindInScope<RuminantActivityGrowPF>() is not null;

            GrazeFoodStore grazeFoodStore = Resources.FindResourceGroup<GrazeFoodStore>();
            if (grazeFoodStore is null)
            {
                Summary.WriteMessage(this, $"No GrazeFoodStore is available for the ruminant grazing activity [a={Name}]!", MessageType.Warning);
                return;
            }

            InitialiseHerd(true, true);
            // create activity for each pasture type (and common land) and breed at startup
            // do not include common land pasture..
            //Guid currentUid = UniqueID;
            foreach (GrazeFoodStoreType pastureType in grazeFoodStore.Children.Where(a => a.GetType() == typeof(GrazeFoodStoreType) || a.GetType() == typeof(CommonLandFoodStoreType)))
            {
                Structure.Add(new RuminantActivityGrazePasture(this, pastureType, events, transCat, usingGrowPF), this);

                //string transCat = "";
                //if (!buildTransactionFromTree)
                //    transCat = TransactionCategory;

                //RuminantActivityGrazePasture grazePasture = new()
                //{
                //    ActivitiesHolder = ActivitiesHolder,
                //    CLEMParentName = CLEMParentName,
                //    GrazeFoodStoreTypeName = pastureType.NameWithParent,
                //    HoursGrazed = HoursGrazed,
                //    TransactionCategory = transCat,
                //    GrazeFoodStoreModel = pastureType,
                //    events = events,
                //    Parent = this,
                //    Name = "Graze_" + (pastureType).Name,
                //    OnPartialResourcesAvailableAction = OnPartialResourcesAvailableAction,
                //    Status = ActivityStatus.NoTask
                //};
                //currentUid = ActivitiesHolder.AddToGuID(currentUid, 1);
                //grazePasture.UniqueID = currentUid;
                //grazePasture.SetLinkedModels(Resources);
                //grazePasture.InitialiseHerd(true, true);

                //Guid currentHerdUid = currentUid;
                //foreach (RuminantType herdType in HerdResource.FindAllChildren<RuminantType>())
                //{
                //    RuminantActivityGrazePastureHerd grazePastureHerd = new(usingSCA2012Grow)
                //    {
                //        GrazeFoodStoreTypeName = pastureType.NameWithParent,
                //        RuminantTypeName = herdType.NameWithParent,
                //        GrazeFoodStoreModel = pastureType,
                //        RuminantTypeModel = herdType,
                //        HoursGrazed = HoursGrazed,
                //        Parent = grazePasture,
                //        Name = grazePasture.Name + "_" + herdType.Name,
                //        OnPartialResourcesAvailableAction = OnPartialResourcesAvailableAction,
                //        ActivitiesHolder = ActivitiesHolder,
                //        TransactionCategory = transCat,
                //        Status = ActivityStatus.NoTask
                //    };
                //    currentHerdUid = ActivitiesHolder.AddToGuID(currentHerdUid, 2);
                //    grazePastureHerd.UniqueID = currentHerdUid;
                //    grazePastureHerd.SetLinkedModels(Resources);

                //    if (grazePastureHerd.events == null)
                //        grazePastureHerd.events = events;

                //    // add ruminant activity filter group to ensure correct individuals are selected
                //    RuminantActivityGroup herdGroup = new ()
                //    {
                //        Name = "Filter_" + grazePastureHerd.Name,
                //        Parent = this
                //    };
                //    herdGroup.Children.Add(
                //        new FilterByProperty()
                //        {
                //            PropertyOfIndividual = "HerdName",
                //            Operator = ExpressionType.Equal,
                //            Value = herdType.Name,
                //            Parent = herdGroup
                //        } 
                //    );
                //    grazePastureHerd.Children.Add(herdGroup);
                //    grazePastureHerd.FindChild<RuminantActivityGroup>().InitialiseFilters();

                //    grazePastureHerd.InitialiseHerd(false, false);
                //    grazePasture.Children.Add(grazePastureHerd);
                //}
                //Structure.Add(grazePasture, this);
            }
            this.FindAllDescendants<RuminantActivityGrazePastureHerd>().LastOrDefault().IsHidden = true;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if(Status != ActivityStatus.Partial && Status != ActivityStatus.Critical)
                Status = ActivityStatus.NoTask;
            return;
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // single grazeall
            if(ActivitiesHolder.FindAllDescendants<RuminantActivityGrazeAll>().Count() > 1)
            {
                yield return new ValidationResult($"Only one [a=RuminantActivityGrazeAll] is permitted per [CLEM] component{Environment.NewLine}The GrazeAll activity will manage all possible grazing on the farm", new string[] { "Ruminant graze all activity" });
            }
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">All individuals in managed pastures will graze for ");
            if (HoursGrazed <= 0)
                htmlWriter.Write($"<span class=\"errorlink\">{HoursGrazed:0.#}</span> hours of ");
            else
                htmlWriter.Write(((HoursGrazed == 8) ? "" : $"<span class=\"setvalue\">{HoursGrazed:0.#}</span> hours of "));

            htmlWriter.Write("the maximum 8 hours each day</span>");
            htmlWriter.Write("</div>");
            return htmlWriter.ToString();
        }
        #endregion

    }
}
