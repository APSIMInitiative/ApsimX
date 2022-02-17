using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant shear activity</summary>
    /// <summary>This activity shears the specified ruminants and placed clip in a store</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Perform ruminant shearing and place clip in a specified store")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantShear.htm")]
    public class RuminantActivityShear : CLEMRuminantActivityBase
    {
        private LabourRequirement labourRequirement;

        /// <summary>
        /// Name of Product store to place clip (with Resource Group name appended to the front [separated with a '.'])
        /// </summary>
        [Description("Store to place clip")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(ProductStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Product store type required")]
        public string ProductStoreName { get; set; }

        /// <summary>
        /// Produc store for clip
        /// </summary>
        [JsonIgnore]
        public ProductStoreType StoreType { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityShear()
        {
            TransactionCategory = "Livestock.Shearing";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            this.InitialiseHerd(true, true);

            // locate StoreType resource
            StoreType = Resources.FindResourceType<ProductStore, ProductStoreType>(this, ProductStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <inheritdoc/>
        public override LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            IEnumerable<Ruminant> herd = CurrentHerd(false);
            double adultEquivalents = herd.Sum(a => a.AdultEquivalent);

            double daysNeeded = 0;
            double numberUnits = 0;
            labourRequirement = requirement;
            if (herd.Any())
            {
                switch (requirement.UnitType)
                {
                    case LabourUnitType.Fixed:
                        daysNeeded = requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perHead:
                        int head = herd.Count();
                        numberUnits = head / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                            numberUnits = Math.Ceiling(numberUnits);

                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perAE:
                        numberUnits = adultEquivalents / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                            numberUnits = Math.Ceiling(numberUnits);

                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perKg:
                        daysNeeded = herd.Sum(a => a.Wool) * requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perUnit:
                        numberUnits = herd.Sum(a => a.Wool) / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                            numberUnits = Math.Ceiling(numberUnits);

                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    default:
                        throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
                } 
            }
            return new LabourRequiredArgs(daysNeeded, TransactionCategory, this.PredictedHerdName);
        }

        /// <inheritdoc/>
        public override void AdjustResourcesNeededForActivity()
        {
            //add limit to amount collected based on labour shortfall
            double labourLimit = this.LabourLimitProportion;
            foreach (ResourceRequest item in ResourceRequestList)
                if (item.ResourceType != typeof(LabourType))
                    item.Required *= labourLimit;
            return;
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            IEnumerable<Ruminant> herd = CurrentHerd(false).OrderByDescending(a => a.Wool);
            if (herd.Any())
            {
                double woolTotal = 0;
                if(LabourLimitProportion == 1 | (labourRequirement != null && !labourRequirement.LabourShortfallAffectsActivity))
                {
                    woolTotal = herd.Sum(a => a.Wool);
                    herd.ToList().ForEach(a => a.Wool = 0);
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
                                    break;
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
                                    break;
                                kgTrack += item.Wool;
                                woolTotal += item.Wool;
                                item.Wool = 0;
                            }
                            break;
                        case LabourUnitType.perUnit:
                            // stop shearing when unit limit reached
                            double unitLimit = herd.Sum(a => a.Wool) / labourRequirement.UnitSize  * LabourLimitProportion;
                            if(labourRequirement.WholeUnitBlocks)
                                unitLimit = Math.Floor(unitLimit);

                            kgLimit = unitLimit * labourRequirement.UnitSize;
                            kgTrack = 0;
                            foreach (var item in herd)
                            {
                                if (kgTrack + item.Wool > kgLimit)
                                    break;
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
                (StoreType as IResourceType).Add(woolTotal, this, this.PredictedHerdName, TransactionCategory);
                SetStatusSuccess();
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            string html = "";
            html += "\r\n<div class=\"activityentry\">Shear selected herd and place clip in ";
            html += CLEMModel.DisplaySummaryValueSnippet(ProductStoreName, "Store Type not set");
            html += "</div>";
            return html;
        } 
        #endregion

    }
}
