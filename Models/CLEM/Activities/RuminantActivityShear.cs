using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant shear activity</summary>
    /// <summary>This activity shears the specified ruminants and placed clip in a store</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs ruminant shearing based upon the current herd filtering and places clip in a specified store.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantShear.htm")]
    public class RuminantActivityShear : CLEMRuminantActivityBase
    {
        private LabourRequirement labourRequirement;

        /// <summary>
        /// Name of Porcust store to place clip (with Resource Group name appended to the front [separated with a '.'])
        /// </summary>
        [Description("Store to place clip")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(ProductStore) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Product store type required")]
        public string ProductStoreName { get; set; }

        /// <summary>
        /// Feed type
        /// </summary>
        [JsonIgnore]
        public ProductStoreType StoreType { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            this.InitialiseHerd(true, true);

            // locate StoreType resource
            StoreType = Resources.GetResourceItem(this, ProductStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as ProductStoreType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            List<Ruminant> herd = CurrentHerd(false);
            int head = herd.Count();
            double adultEquivalents = herd.Sum(a => a.AdultEquivalent);

            double daysNeeded = 0;
            double numberUnits = 0;
            labourRequirement = requirement;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perAE:
                    numberUnits = adultEquivalents / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perKg:
                    daysNeeded = herd.Sum(a => a.Wool) * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perUnit:
                    numberUnits = herd.Sum(a => a.Wool) / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Shearing", this.PredictedHerdName);
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            //add limit to amount collected based on labour shortfall
            double labourLimit = this.LabourLimitProportion;
            foreach (ResourceRequest item in ResourceRequestList)
            {
                if (item.ResourceType != typeof(LabourType))
                {
                    item.Required *= labourLimit;
                }
            }
            return;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            List<Ruminant> herd = CurrentHerd(false).OrderByDescending(a => a.Wool).ToList<Ruminant>();
            if (herd != null && herd.Count > 0)
            {
                double woolTotal = 0;
                if(LabourLimitProportion == 1 | (labourRequirement != null && !labourRequirement.LabourShortfallAffectsActivity))
                {
                    woolTotal = herd.Sum(a => a.Wool);
                    herd.ForEach(a => a.Wool = 0);
                }
                else
                {
                    // limits clip based on labour shortfall
                    switch (labourRequirement.UnitType)
                    {
                        case LabourUnitType.Fixed:
                            // no clip taken
                            break;
                        case LabourUnitType.perHead:
                            int numberShorn = Convert.ToInt32(herd.Count() * LabourLimitProportion, CultureInfo.InvariantCulture);
                            woolTotal = herd.Take(numberShorn).Sum(a => a.Wool);
                            herd.Take(numberShorn).ToList().ForEach(a => a.Wool = 0);
                            break;
                        case LabourUnitType.perAE:
                            // stop when AE reached
                            double aELimit = herd.Sum(a => a.AdultEquivalent) * LabourLimitProportion;
                            double aETrack = 0;
                            foreach (var item in herd)
                            {
                                if(aETrack + item.AdultEquivalent > aELimit)
                                {
                                    break;
                                }
                                aETrack += item.AdultEquivalent;
                                woolTotal += item.Wool;
                                item.Wool = 0;
                            }
                            break;
                        case LabourUnitType.perKg:
                            // stop shearing when limit reached
                            double kgLimit = herd.Sum(a => a.Wool) * LabourLimitProportion;
                            double kgTrack = 0;
                            foreach (var item in herd)
                            {
                                if (kgTrack + item.Wool > kgLimit)
                                {
                                    break;
                                }
                                kgTrack += item.Wool;
                                woolTotal += item.Wool;
                                item.Wool = 0;
                            }
                            break;
                        case LabourUnitType.perUnit:
                            // stop shearing when unit limit reached
                            double unitLimit = herd.Sum(a => a.Wool) / labourRequirement.UnitSize  * LabourLimitProportion;
                            if(labourRequirement.WholeUnitBlocks)
                            {
                                unitLimit = Math.Floor(unitLimit);
                            }
                            kgLimit = unitLimit * labourRequirement.UnitSize;
                            kgTrack = 0;
                            foreach (var item in herd)
                            {
                                if (kgTrack + item.Wool > kgLimit)
                                {
                                    break;
                                }
                                kgTrack += item.Wool;
                                woolTotal += item.Wool;
                                item.Wool = 0;
                            }
                            break;
                        default:
                            throw new ApsimXException(this, "Labour requirement type " + labourRequirement.UnitType.ToString() + " is not supported in DoActivity method of [a=" + this.Name + "]");
                    }
                    this.Status = ActivityStatus.Partial;
                }

                // place clip in selected store
                (StoreType as IResourceType).Add(woolTotal, this, this.PredictedHerdName, "Shear");
                SetStatusSuccess();
            }
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\r\n<div class=\"activityentry\">Shear selected herd and place clip in ";

            if (ProductStoreName == null || ProductStoreName == "")
            {
                html += "<span class=\"errorlink\">[Store TYPE NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + ProductStoreName + "</span>";
            }
            html += "</div>";
            return html;
        } 
        #endregion

    }
}
