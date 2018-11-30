using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;

namespace Models.CLEM.Resources
{

    /// <summary>
    /// This stores the initialisation parameters for land
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Land))]
    [Description("This resource represents a land type (e.g. Clay region.) This is not necessarily a paddock, but Bunded and interbund land areas must be separated into individual land types.")]
    [Version(1, 0, 1, "Adam Liedloff", "CSIRO", "")]
    public class LandType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        /// <summary>
        /// Total Area
        /// </summary>
        [Description("Land area")]
        [Required, GreaterThanEqualValue(0)]
        public double LandArea { get; set; }

        /// <summary>
        /// Unusable Portion - Buildings, paths etc. (%)
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0.0)]
        [Description("Proportion taken up with buildings etc.")]
        [Required, Proportion]
        public double PortionBuildings
        {
            get; set;
        }

        /// <summary>
        /// Allocate only proportion of Land area
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1.0)]
        [Description("Allocate only proportion of Land area")]
        [Required, Proportion]
        public double ProportionOfTotalArea
        {
            get; set;
        }

        /// <summary>
        /// Soil Type (1-5) 
        /// </summary>
        [Description("Soil type index")]
        [Required]
        public int SoilType { get; set; }

        /// <summary>
        /// Area not currently being used (ha)
        /// </summary>
        [XmlIgnore]
        public double AreaAvailable { get { return areaAvailable; } }
        private double areaAvailable { get { return roundedAreaAvailable; } set { roundedAreaAvailable = Math.Round(value, 9); } }
        private double roundedAreaAvailable;

        /// <summary>
        /// The total area available 
        /// </summary>
        [XmlIgnore]
        public double UsableArea { get { return Math.Round(this.LandArea * ProportionOfTotalArea, 5); } }

        /// <summary>
        /// List of currently allocated land
        /// </summary>
        [XmlIgnore]
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
            {
                Add(UsableArea, this, "Initialise");
            }

            // take away buildings (allows building to change over time. 
            if (PortionBuildings > 0)
            {
                ResourceRequest resourceRequest = new ResourceRequest()
                {
                    ActivityModel = this,
                    AllowTransmutation = false,
                    Reason = "Allocate buildings",
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
        /// <param name="ResourceAmount"></param>
        /// <param name="Activity"></param>
        /// <param name="Reason"></param>
        public new void Add(object ResourceAmount, CLEMModel Activity, string Reason)
        {
            if (ResourceAmount.GetType().ToString() != "System.Double")
            {
                throw new Exception(String.Format("ResourceAmount object of type [{0}] is not supported Add method in [r={1}]", ResourceAmount.GetType().ToString(), this.Name));
            }
            double addAmount = (double)ResourceAmount;
            double amountAdded = addAmount;
            if (this.areaAvailable + addAmount > this.UsableArea )
            {
                amountAdded = this.UsableArea - this.areaAvailable;
                string message = "Tried to add more available land to [r=" + this.Name + "] than exists.";
                Summary.WriteWarning(this, message);
                this.areaAvailable = this.UsableArea;
            }
            else
            {
                this.areaAvailable = this.areaAvailable + addAmount;
            }
            ResourceTransaction details = new ResourceTransaction();
            details.Debit = amountAdded;
            details.Activity = Activity.Name;
            details.ActivityType = Activity.GetType().Name;
            details.Reason = Reason;
            details.ResourceType = this.Name;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);

            if (Reason != "Initialise")
            {
                UpdateLandAllocatedList(Activity, amountAdded, true);
                // adjust activity using all remaining land as well.
                if (ActivityRequestingRemainingLand != null && ActivityRequestingRemainingLand != Activity)
                {
                    UpdateLandAllocatedList(ActivityRequestingRemainingLand, amountAdded, true);
                }
            }
        }

        /// <summary>
        /// Remove from finance type store
        /// </summary>
        /// <param name="Request">Resource request class with details.</param>
        public new void Remove(ResourceRequest Request)
        {
            if (Request.Required == 0) return;
            double amountRemoved = Request.Required;
            // avoid taking too much
            amountRemoved = Math.Min(this.areaAvailable, amountRemoved);

            if (Request.Reason != "Assign unallocated")
            {
                this.areaAvailable -= amountRemoved;
            }
            else
            {
                // activitiy requesting all unallocated land.
                if (ActivityRequestingRemainingLand == null)
                {
                    ActivityRequestingRemainingLand = Request.ActivityModel;
                }
                else if (ActivityRequestingRemainingLand != Request.ActivityModel)
                {
                    // error! more than one activity is requesting all unallocated land.
                    throw new ApsimXException(this, "More than one activity [" + ActivityRequestingRemainingLand.Name + "] and [" + Request.ActivityModel.Name + "] is requesting to use all unallocated land from land type [" + this.Name + "]");
                }
            }

            Request.Provided = amountRemoved;
            ResourceTransaction details = new ResourceTransaction();
            details.ResourceType = this.Name;
            details.Credit = amountRemoved;
            details.Activity = Request.ActivityModel.Name;
            details.ActivityType = Request.ActivityModel.GetType().Name;
            details.Reason = Request.Reason;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);

            UpdateLandAllocatedList(Request.ActivityModel, amountRemoved, false);
            // adjust activity using all remaining land as well.
            if (ActivityRequestingRemainingLand != null && ActivityRequestingRemainingLand != Request.ActivityModel)
            {
                UpdateLandAllocatedList(ActivityRequestingRemainingLand, amountRemoved, false);
            }
        }

        /// <summary>
        /// Set amount of land available
        /// </summary>
        /// <param name="NewValue">New value to set land to</param>
        public new void Set(double NewValue)
        {
            throw new NotImplementedException("Set() method of LandType is not currently implemented. Use add and Remove to modify this resource.");
        }

        private void UpdateLandAllocatedList(CLEMModel Activity, double AmountChanged, bool added)
        {
            if (AllocatedActivitiesList == null)
            {
                AllocatedActivitiesList = new List<LandActivityAllocation>();
            }

            // find activity in list
            LandActivityAllocation allocation = AllocatedActivitiesList.Where(a => a.Activity.Name == Activity.Name).FirstOrDefault();
            if(allocation!= null)
            {
                // modify - remove if added by activity and add if removed or taken for the activity
                allocation.LandAllocated += AmountChanged * (added?-1:1);
                if(allocation.LandAllocated < 0.00001)
                {
                    AllocatedActivitiesList.Remove(allocation);
                }
            }
            else
            {
                // if resource was removed by activity it is added to the activty 
                if(!added & AmountChanged > 0)
                {
                    AllocatedActivitiesList.Add(new LandActivityAllocation()
                    {
                        LandName = this.Name,
                        Activity = Activity,
                        LandAllocated = AmountChanged,
                        ActivityName = (Activity.Name == this.Name)?"Buildings":Activity.Name
                    });
                }
            }
        }

        /// <summary>
        /// Back account transaction occured
        /// </summary>
        public event EventHandler TransactionOccurred;

        /// <summary>
        /// Transcation occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTransactionOccurred(EventArgs e)
        {
            if (TransactionOccurred != null)
                TransactionOccurred(this, e);
        }

        /// <summary>
        /// Last transaction received
        /// </summary>
        [XmlIgnore]
        public ResourceTransaction LastTransaction { get; set; }

        #endregion

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="FormatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool FormatForParentControl)
        {
            string html = "\n<div class=\"activityentry\">";
            html += "This land type has an area of <span class=\"setvalue\">" + (this.LandArea * ProportionOfTotalArea).ToString("#,##0.##") + "</span>";
            if (PortionBuildings > 0)
            {
                html += " of which <span class=\"setvalue\">" + this.PortionBuildings.ToString("0.##%") + "</span> is buildings";
            }
            html += "</div>";
            html += "\n<div class=\"activityentry\">";
            html += "This land has soil of index <span class=\"setvalue\">" + SoilType.ToString() + "</span>";
            html += "\n</div>";
            return html;
        }

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