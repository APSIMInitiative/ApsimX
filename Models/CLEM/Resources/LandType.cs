using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;

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
    public class LandType : CLEMModel, IResourceType, IResourceWithTransactionType
    {
        [Link]
        ISummary Summary = null;

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
        [Description("Buildings - proportion taken up with bldgs, paths (%)")]
        [Required, Percentage]
        public double UnusablePortion { get; set; }

        /// <summary>
        /// Allocate proportion of Total Area
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1.0)]
        [Description("Proportion of Total Area to assign")]
        [Required, Proportion]
        public double ProportionOfTotalArea { get; set; }

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
        private double areaAvailable;

        /// <summary>
        /// Area already used (ha)
        /// </summary>
        [XmlIgnore]
        public double AreaUsed { get { return UsableArea - areaAvailable; } }

        /// <summary>
        /// The total area available 
        /// </summary>
        [XmlIgnore]
        public double UsableArea { get { return (this.LandArea * (1.0 - (UnusablePortion / 100)))*ProportionOfTotalArea; }  }

        /// <summary>
        /// Constructor
        /// </summary>
        public LandType()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Initialise the current state to the starting amount of fodder
        /// </summary>
        public void Initialise()
        {
            if (UsableArea > 0)
            {
                Add(UsableArea, this.Name, "Initialise");
            }
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
            Initialise();
        }

        #region Transactions

        /// <summary>
        /// Add to food store
        /// </summary>
        /// <param name="ResourceAmount"></param>
        /// <param name="ActivityName"></param>
        /// <param name="Reason"></param>
        public void Add(object ResourceAmount, string ActivityName, string Reason)
        {
            if (ResourceAmount.GetType().ToString() != "System.Double")
            {
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", ResourceAmount.GetType().ToString(), this.Name));
            }
            double addAmount = (double)ResourceAmount;
            double amountAdded = addAmount;
            if (this.areaAvailable + addAmount > this.UsableArea )
            {
                amountAdded = this.UsableArea - this.areaAvailable;
                string message = "Tried to add more available land to " + this.Name + " than exists.";
                Summary.WriteWarning(this, message);
                this.areaAvailable = this.UsableArea;
            }
            else
            {
                this.areaAvailable = this.areaAvailable + addAmount;
            }
            ResourceTransaction details = new ResourceTransaction();
            details.Credit = amountAdded;
            details.Activity = ActivityName;
            details.Reason = Reason;
            details.ResourceType = this.Name;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
        }

        /// <summary>
        /// Remove from finance type store
        /// </summary>
        /// <param name="Request">Resource request class with details.</param>
        public void Remove(ResourceRequest Request)
        {
            if (Request.Required == 0) return;
            double amountRemoved = Request.Required;
            // avoid taking too much
            amountRemoved = Math.Min(this.areaAvailable, amountRemoved);
            this.areaAvailable -= amountRemoved;

            Request.Provided = amountRemoved;
            ResourceTransaction details = new ResourceTransaction();
            details.ResourceType = this.Name;
            details.Debit = amountRemoved * -1;
            details.Activity = Request.ActivityModel.Name;
            details.Reason = Request.Reason;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
        }

        /// <summary>
        /// Set amount of land available
        /// </summary>
        /// <param name="NewValue">New value to set land to</param>
        public void Set(double NewValue)
        {
            if ((NewValue < 0) || (NewValue > this.UsableArea))
            {
                Summary.WriteMessage(this, "Tried to Set Available Land to Invalid New Amount." + Environment.NewLine
                    + "New Value must be between 0 and the Land Area.");
            }
            else
            {
                this.areaAvailable = NewValue;
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

    }

}