using Models.Core;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Crop activity task</summary>
    /// <summary>This activity will perform costs and labour for a crop activity</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityManageProduct))]
    [Description("A crop task (e.g. sowing) with associated costs and labour requirements.")]
    [Version(1, 0, 2, "Added per unit of land as labour unit type")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Crop/CropTask.htm")]
    public class CropActivityTask: CLEMActivityBase, IValidatableObject, IHandlesActivityCompanionModels
    {
        private bool timingIssueReported = false;
        private CropActivityManageCrop parentManagementActivity;
        private CropActivityManageProduct parentManageProductActivity;

        double amountToSkip = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        protected CropActivityTask()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed",
                            "per land unit of crop",
                            "per hectare of crop",
                            "per kg harvested",
                            "per land unit harvested",
                            "per hectare harvested",
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
            //relatesToResourceName = this.FindAncestor<CropActivityManageProduct>().StoreItemName;
            parentManagementActivity = FindAncestor<CropActivityManageCrop>();
            parentManageProductActivity = (Parent as CropActivityManageProduct);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            amountToSkip = 0;
            if (TimingOK)
            {
                if (FindAncestor<CropActivityManageProduct>().CurrentlyManaged)
                    Status = ActivityStatus.Success;
                else
                {
                    Status = ActivityStatus.Warning;
                    foreach (var child in FindAllChildren<CLEMActivityBase>())
                    {
                        child.Status = ActivityStatus.Warning;
                    }
                    if (!timingIssueReported)
                    {
                        Summary.WriteMessage(this, $"The harvest timer for crop task [a={this.NameWithParent}] did not allow the task to be performed. This is likely due to insufficient time between rotating to a crop and the next harvest date.", MessageType.Warning);
                        timingIssueReported = true;
                    }
                }
            }

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels.ToList())
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "per land unit of crop":
                        valuesForCompanionModels[valueToSupply.Key] = parentManagementActivity?.Area??0;
                        break;
                    case "per hectare of crop":
                        valuesForCompanionModels[valueToSupply.Key] = (parentManagementActivity?.Area ?? 0) * parentManageProductActivity.UnitsToHaConverter;
                        break;
                    case "per kg harvested":
                        valuesForCompanionModels[valueToSupply.Key] = parentManageProductActivity.AmountHarvested;
                        break;
                    case "per land unit harvested":
                        if (parentManageProductActivity.AmountHarvested > 0)
                            valuesForCompanionModels[valueToSupply.Key] = (parentManagementActivity?.Area ?? 0);
                        else
                            valuesForCompanionModels[valueToSupply.Key] = 0;
                        break;
                    case "per hectare harvested":
                        if (parentManageProductActivity.AmountHarvested > 0)
                            valuesForCompanionModels[valueToSupply.Key] = (parentManagementActivity?.Area ?? 0) * parentManageProductActivity.UnitsToHaConverter;
                        else
                            valuesForCompanionModels[valueToSupply.Key] = 0;
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
                var tagsShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == "").FirstOrDefault();
                //if (tagsShort != null)
                amountToSkip = (1 - tagsShort.Available / tagsShort.Required);
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if(ResourceRequestList.Any())
            {
                SetStatusSuccessOrPartial(amountToSkip > 0);
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
            IModel follow = this.Parent;
            while(!(follow is ActivitiesHolder))
            {
                if(follow is CropActivityManageProduct)
                    return results;

                if(!(follow is ActivityFolder))
                {
                    string[] memberNames = new string[] { "Parent model" };
                    results.Add(new ValidationResult("A [a=CropActivityTask] must be placed immediately below, or within nested [a=ActivityFolders] below, a [a=CropActivityManageProduct] component", memberNames));
                    return results;
                }
                follow = follow.Parent;
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
                if (this.FindAllChildren<ActivityFee>().Count() + this.FindAllChildren<LabourRequirement>().Count() == 0)
                    htmlWriter.Write("<div class=\"errorlink\">This task is not needed as it has no fee or labour requirement</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
