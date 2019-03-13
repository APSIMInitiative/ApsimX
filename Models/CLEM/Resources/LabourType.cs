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
    /// This stores the initialisation parameters for a land type person who can do labour 
    /// who is a family member.
    /// eg. AdultMale, AdultFemale etc.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Labour))]
    [Description("This resource represents a labour type (e.g. Joe, 36 years old, male).")]
    [Version(1, 0, 1, "")]
    public class LabourType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        /// <summary>
        /// Age in years.
        /// </summary>
        [Description("Initial Age")]
        [Required, GreaterThanEqualValue(0)]
        public double InitialAge { get; set; }

        /// <summary>
        /// Male or Female
        /// </summary>
        [Description("Gender")]
        [Required]
        public Sex Gender { get; set; }

        /// <summary>
        /// Age in years.
        /// </summary>
        [XmlIgnore]
        public double Age { get { return Math.Floor(AgeInMonths/12); } }

        /// <summary>
        /// Age in months.
        /// </summary>
        [XmlIgnore]
        public double AgeInMonths { get; set; }

        /// <summary>
        /// Number of individuals
        /// </summary>
        [Description("Number of individuals")]
        [Required, GreaterThanEqualValue(1)]
        public int Individuals { get; set; }

        /// <summary>
        /// The unique id of the last activity request for this labour type
        /// </summary>
        [XmlIgnore]
        public Guid LastActivityRequestID { get; set; }

        /// <summary>
        /// The amount of labour supplied to the last activity for this labour type
        /// </summary>
        [XmlIgnore]
        public double LastActivityRequestAmount { get; set; }

        /// <summary>
        /// The number of hours provided to the current activity
        /// </summary>
        [XmlIgnore]
        public double LastActivityLabour { get; set; }

        /// <summary>
        /// Available Labour (in days) in the current month. 
        /// </summary>
        [XmlIgnore]
        public double AvailableDays { get { return availableDays; } }
        private double availableDays;

        /// <summary>
        /// Link to the current labour availability for this person
        /// </summary>
        [XmlIgnore]
        public LabourSpecificationItem LabourAvailability { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourType()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Determines the amount of labour up to a max available for the specified Activity.
        /// </summary>
        /// <param name="activityID">Unique activity ID</param>
        /// <param name="maxLabourAllowed">Max labour allowed</param>
        /// <returns></returns>
        public double LabourCurrentlyAvailableForActivity(Guid activityID, double maxLabourAllowed)
        {
            if(activityID == LastActivityRequestID)
            {
                return Math.Max(0, maxLabourAllowed - LastActivityLabour);
            }
            else
            {
                return Amount;
            }
        }

        /// <summary>
        /// Reset the available days for a given month
        /// </summary>
        /// <param name="month"></param>
        public void SetAvailableDays(int month)
        {
            availableDays = 0;
            if(LabourAvailability != null)
            {
                availableDays = Math.Min(30.4, LabourAvailability.GetAvailability(month - 1));
            }
        }

        #region Transactions

        /// <summary>
        /// Add to labour store of this type
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="reason">Name of individual adding resource</param>
        public new void Add(object resourceAmount, CLEMModel activity, string reason)
        {
            if (resourceAmount.GetType().ToString() != "System.Double")
            {
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", resourceAmount.GetType().ToString(), this.Name));
            }
            double addAmount = (double)resourceAmount;
            this.availableDays = this.availableDays + addAmount;
            ResourceTransaction details = new ResourceTransaction();
            details.Gain = addAmount;
            details.Activity = activity.Name;
            details.ActivityType = activity.GetType().Name;
            details.Reason = reason;
            details.ResourceType = this.Name;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
        }

        /// <summary>
        /// Remove from labour store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public new void Remove(ResourceRequest request)
        {
            if (request.Required == 0)
            {
                return;
            }

            double amountRemoved = request.Required;
            // avoid taking too much
            amountRemoved = Math.Min(this.availableDays, amountRemoved);
            this.availableDays -= amountRemoved;
            request.Provided = amountRemoved;
            LastActivityRequestID = request.ActivityID;
            ResourceTransaction details = new ResourceTransaction();
            details.ResourceType = this.Name;
            details.Loss = amountRemoved;
            details.Activity = request.ActivityModel.Name;
            details.ActivityType = request.ActivityModel.GetType().Name;
            details.Reason = request.Reason;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
            return;
        }

        /// <summary>
        /// Set amount of animal food available
        /// </summary>
        /// <param name="newValue">New value to set food store to</param>
        public new void Set(double newValue)
        {
            this.availableDays = newValue;
        }

        /// <summary>
        /// Labour type transaction occured
        /// </summary>
        public event EventHandler TransactionOccurred;

        /// <summary>
        /// Transcation occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTransactionOccurred(EventArgs e)
        {
            TransactionOccurred?.Invoke(this, e);
        }

        #endregion

        #region IResourceType

        /// <summary>
        /// Implemented Initialise method
        /// </summary>
        public void Initialise()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Last transaction received
        /// </summary>
        [XmlIgnore]
        public ResourceTransaction LastTransaction { get; set; }

        /// <summary>
        /// Current amount of labour required.
        /// </summary>
        public double Amount
        {
            get
            {
                return this.availableDays;
            }
        }

        #endregion

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if (formatForParentControl == false)
            {
                html = "<div class=\"activityentry\">";
                if (this.Individuals == 0)
                {
                    html += "No individuals are proivded for this labour type";
                }
                else
                {
                    if (this.Individuals > 1)
                    {
                        html += "<span class=\"setvalue\">"+this.Individuals.ToString()+"</span> x ";
                    }
                    html += "<span class=\"setvalue\">" + string.Format("{0}", this.InitialAge)+"</span> year old ";
                    html += "<span class=\"setvalue\">" + string.Format("{0}", this.Gender.ToString().ToLower())+"</span>";
                }
                html += "</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            if (formatForParentControl)
            {
                return "";
            }
            else
            {
                return base.ModelSummaryClosingTags(formatForParentControl);
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            if (formatForParentControl)
            {
                return "";
            }
            else
            {
                return base.ModelSummaryOpeningTags(formatForParentControl);
            }
        }

    }
}