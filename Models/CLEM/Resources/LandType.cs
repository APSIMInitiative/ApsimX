using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the initialisation parameters for land
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Land))]
    [Description("This resource represents a land type (e.g. clay region). Bunded and interbund land areas must be separated into individual land types, but paddocks are managed by activities")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Land/LandType.htm")]
    public class LandType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units")]
        public string Units { get { return (Parent as Land).UnitsOfArea; } }

        /// <summary>
        /// Total Area
        /// </summary>
        [Description("Land area")]
        [Required, GreaterThanValue(0)]
        public double LandArea { get; set; }

        /// <summary>
        /// Unusable Portion - Buildings, paths etc. (%)
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0.0)]
        [Description("Proportion taken up with buildings etc.")]
        [Required, Proportion]
        public double PortionBuildings { get; set; }

        /// <summary>
        /// Allocate only proportion of Land area
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1.0)]
        [Description("Allocate only proportion of Land area")]
        [Required, Proportion, GreaterThanValue(0)]
        public double ProportionOfTotalArea { get; set; }

        /// <summary>
        /// Soil Type (1-5) 
        /// </summary>
        [Description("Land type id")]
        [Required]
        public string SoilType { get; set; }

        /// <summary>
        /// Area not currently being used (ha)
        /// </summary>
        [JsonIgnore]
        public double AreaAvailable { get { return areaAvailable; } }
        private double areaAvailable { get { return roundedAreaAvailable; } set { roundedAreaAvailable = Math.Round(value, 9); } }
        private double roundedAreaAvailable;

        /// <summary>
        /// The total area available 
        /// </summary>
        [JsonIgnore]
        public double UsableArea { get { return Math.Round(this.LandArea * ProportionOfTotalArea, 5); } }

        /// <summary>
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                return Price(PurchaseOrSalePricingStyleType.Sale)?.CalculateValue(Amount);
            }
        }

        /// <summary>
        /// List of currently allocated land
        /// </summary>
        [JsonIgnore]
        public List<LandActivityAllocation> AllocatedActivitiesList;

        private CLEMModel ActivityRequestingRemainingLand;

        /// <summary>
        /// Constructor
        /// </summary>
        public LandType()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Resource available
        /// </summary>
        public double Amount
        {
            get
            {
                return AreaAvailable;
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            if (UsableArea > 0)
                Add(UsableArea, null, null, "Starting value");

            // take away buildings (allows building to change over time. 
            if (PortionBuildings > 0)
            {
                ResourceRequest resourceRequest = new ResourceRequest()
                {
                    ActivityModel = this,
                    AllowTransmutation = false,
                    Category = "Allocate buildings",
                    Required = UsableArea * PortionBuildings,
                    Resource = this as IResourceType,
                    ResourceTypeName = this.Name,
                };
                this.Remove(resourceRequest);
            }
        }

        #region Transactions

        /// <summary>
        /// Add to food store
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            if (resourceAmount.GetType().ToString() != "System.Double")
                throw new Exception(String.Format("ResourceAmount object of type [{0}] is not supported. Add method in [r={1}]", resourceAmount.GetType().ToString(), this.GetType().ToString()));

            double addAmount = (double)resourceAmount;

            if (addAmount > 0)
            {
                double amountAdded = addAmount;
                if (this.areaAvailable + addAmount > this.UsableArea)
                {
                    amountAdded = this.UsableArea - this.areaAvailable;
                    string message = $"Tried to add more available land to [r={this.Name}] than exists.";
                    Summary.WriteMessage(this, message, MessageType.Warning);
                    this.areaAvailable = this.UsableArea;
                }
                else
                    this.areaAvailable += addAmount;

                ReportTransaction(TransactionType.Gain, amountAdded, activity, relatesToResource, category, this);

                if (category != "Starting value")
                {
                    UpdateLandAllocatedList(activity, amountAdded, true);
                    // adjust activity using all remaining land as well.
                    if (ActivityRequestingRemainingLand != null && ActivityRequestingRemainingLand != activity)
                        UpdateLandAllocatedList(ActivityRequestingRemainingLand, amountAdded, false);
                }
            }
        }

        /// <summary>
        /// Remove from finance type store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public new void Remove(ResourceRequest request)
        {
            if (request.Required == 0)
                return;

            double amountRemoved = request.Required;
            // avoid taking too much
            amountRemoved = Math.Min(this.areaAvailable, amountRemoved);

            if (request.Category != "Assign unallocated")
                this.areaAvailable -= amountRemoved;
            else
            {
                // activitiy requesting all unallocated land.
                if (ActivityRequestingRemainingLand == null)
                    ActivityRequestingRemainingLand = request.ActivityModel;
                else if (ActivityRequestingRemainingLand != request.ActivityModel)
                    // error! more than one activity is requesting all unallocated land.
                    throw new ApsimXException(this, "More than one activity [" + ActivityRequestingRemainingLand.Name + "] and [" + request.ActivityModel.Name + "] is requesting to use all unallocated land from land type [" + this.Name + "]");
            }

            request.Provided = amountRemoved;

            if (request.Category != "Assign unallocated")
                ReportTransaction(TransactionType.Loss, amountRemoved, request.ActivityModel, request.RelatesToResource, request.Category, this);

            UpdateLandAllocatedList(request.ActivityModel, amountRemoved, false);
            // adjust activity using all remaining land as well.
            if (ActivityRequestingRemainingLand != null && ActivityRequestingRemainingLand != request.ActivityModel)
                UpdateLandAllocatedList(ActivityRequestingRemainingLand, amountRemoved, true);
        }

        /// <summary>
        /// Set amount of land available
        /// </summary>
        /// <param name="newValue">New value to set land to</param>
        public new void Set(double newValue)
        {
            throw new NotImplementedException("Set() method of LandType is not currently implemented. Use add and Remove to modify this resource.");
        }

        private void UpdateLandAllocatedList(CLEMModel activity, double amountChanged, bool added)
        {
            if (AllocatedActivitiesList == null)
                AllocatedActivitiesList = new List<LandActivityAllocation>();

            // find activity in list
            LandActivityAllocation allocation = AllocatedActivitiesList.Where(a => a.Activity.Name == activity.Name).FirstOrDefault();
            if (allocation != null)
            {
                // modify - remove if added by activity and add if removed or taken for the activity
                allocation.LandAllocated += amountChanged * (added ? -1 : 1);
                if (allocation.LandAllocated < 0.00001)
                    AllocatedActivitiesList.Remove(allocation);
            }
            else
            {
                // if resource was removed by activity it is added to the activty 
                if (!added && amountChanged > 0)
                {
                    AllocatedActivitiesList.Add(new LandActivityAllocation()
                    {
                        LandName = this.Name,
                        Activity = activity,
                        LandAllocated = amountChanged,
                        ActivityName = (activity.Name == this.Name) ? "Buildings" : activity.Name
                    });
                }
            }
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (LandArea == 0)
                    htmlWriter.Write("<span class=\"errorlink\">NO VALUE</span> has been set for the area of this land");
                else
                {
                    if (ProportionOfTotalArea == 0)
                        htmlWriter.Write("The proportion of total area assigned to this land type is <span class=\"errorlink\">0</span> so no area is assigned");
                    else
                    {
                        htmlWriter.Write("This land type has an area of <span class=\"setvalue\">" + (this.LandArea * ProportionOfTotalArea).ToString("#,##0.##") + "</span>");
                        string units = (this as IResourceType).Units;
                        if (units != "NA")
                        {
                            if (units == null || units == "")
                                htmlWriter.Write("");
                            else
                                htmlWriter.Write(" <span class=\"setvalue\">" + units + "</span>");
                        }
                    }
                }

                if (PortionBuildings > 0)
                    htmlWriter.Write(" of which <span class=\"setvalue\">" + this.PortionBuildings.ToString("0.##%") + "</span> is buildings");
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("This land is identified as <span class=\"setvalue\">" + SoilType.ToString() + "</span>");
                htmlWriter.Write("\r\n</div>");
                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            return "";
        }

        #endregion 
    }

    /// <summary>
    /// Class to store land allocation details
    /// </summary>
    [Serializable]
    public class LandActivityAllocation
    {
        /// <summary>
        /// Name of activity using the land
        /// </summary>
        public string LandName { get; set; }
        /// <summary>
        /// Unique activity ID
        /// </summary>
        public CLEMModel Activity { get; set; }
        /// <summary>
        /// Name for activity
        /// </summary>
        public string ActivityName { get; set; }
        /// <summary>
        /// Amount of land allocated
        /// </summary>
        public double LandAllocated { get; set; }
    }

}