using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm.Resources
{

    /// <summary>
    /// This stores the initialisation parameters for land
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Land))]
    public class LandType : WFModel, IResourceType, IResourceWithTransactionType
	{
        [Link]
        ISummary Summary = null;

        /// <summary>
        /// Total Area (ha)
        /// </summary>
        [Description("Land Area (ha)")]
        public double LandArea { get; set; }

        /// <summary>
        /// Unusable Portion - Buildings, paths etc. (%)
        /// </summary>
        [Description("Buildings - proportion taken up with bldgs, paths (%)")]
        public double UnusablePortion { get; set; }

        /// <summary>
        /// Portion Bunded (%)
        /// </summary>
        [Description("Portion bunded (%)")]
        public double BundedPortion { get; set; }

        /// <summary>
        /// Soil Type (1-5) 
        /// </summary>
        [Description("Soil Type (1-5)")]
        public int SoilType { get; set; }

        /// <summary>
        /// Fertility - N Decline Yield
        /// </summary>
        [Description("Fertility - N Decline yld")]
        public double NDecline { get; set; }

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
        public double AreaUsed { get { return this.LandArea - areaAvailable; } }

		/// <summary>
		/// Initialise the current state to the starting amount of fodder
		/// </summary>
		public void Initialise()
		{
			Add(this.LandArea, this.Name, "Initialise");
//			this.areaAvailable = this.LandArea;
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
		[EventSubscribe("StartOfSimulation")]
		private void OnStartOfSimulation(object sender, EventArgs e)
		{
			Initialise();
		}

		#region Transactions

		/// <summary>
		/// Add to food store
		/// </summary>
		/// <param name="AddAmount">Amount to add to resource</param>
		/// <param name="ActivityName">Name of activity adding resource</param>
		/// <param name="UserName">Name of individual radding resource</param>
		public void Add(double AddAmount, string ActivityName, string UserName)
		{
			double amountAdded = AddAmount;
			if (this.areaAvailable + AddAmount > this.LandArea)
			{
				amountAdded = this.LandArea - this.areaAvailable;
				string message = "Tried to add more available land to " + this.Name + " than exists." + Environment.NewLine
					+ "Current Amount: " + this.areaAvailable + Environment.NewLine
					+ "Tried to Remove: " + AddAmount;
				Summary.WriteWarning(this, message);
				this.areaAvailable = this.LandArea;
			}
			else
			{
				this.areaAvailable = this.areaAvailable + AddAmount;
			}
			ResourceTransaction details = new ResourceTransaction();
			details.Credit = amountAdded;
			details.Activity = ActivityName;
			details.Reason = UserName;
			details.ResourceType = this.Name;
			LastTransaction = details;
			TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
			OnTransactionOccurred(te);
		}

		/// <summary>
		/// Remove from food store
		/// </summary>
		/// <param name="RemoveAmount">Amount to remove. NOTE: This is a positive value not a negative value.</param>
		/// <param name="ActivityName">Name of activity requesting resource</param>
		/// <param name="UserName">Name of individual requesting resource</param>
		public double Remove(double RemoveAmount, string ActivityName, string UserName)
		{
			double amountRemoved = RemoveAmount;
			if (this.areaAvailable - RemoveAmount < 0)
			{
				amountRemoved = this.areaAvailable;
				string message = "Tried to remove more available land from " + this.Name + " than exists." + Environment.NewLine
					+ "Current Amount: " + this.areaAvailable + Environment.NewLine
					+ "Tried to Remove: " + RemoveAmount;
				Summary.WriteWarning(this, message);
				this.areaAvailable = 0;
			}
			else
			{
				this.areaAvailable = this.areaAvailable - RemoveAmount;
			}
			ResourceTransaction details = new ResourceTransaction();
			details.ResourceType = this.Name;
			details.Debit = amountRemoved * -1;
			details.Activity = ActivityName;
			details.Reason = UserName;
			LastTransaction = details;
			TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
			OnTransactionOccurred(te);
			return amountRemoved;
		}

		/// <summary>
		/// Remove Food
		/// If this call is reached we are not going through an arbitrator so provide all possible resource to requestor
		/// </summary>
		/// <param name="RemoveRequest">A feed request object with required information</param>
		public void Remove(object RemoveRequest)
		{
			RuminantFeedRequest removeRequest = RemoveRequest as RuminantFeedRequest;

			// limit by available
			removeRequest.Amount = Math.Min(removeRequest.Amount, areaAvailable);

			// add to intake and update %N and %DMD values
			removeRequest.Requestor.AddIntake(removeRequest);

			// Remove from resource
			Remove(removeRequest.Amount, removeRequest.FeedActivity.Name, removeRequest.Requestor.BreedParams.Name);
		}

		/// <summary>
		/// Set amount of animal food available
		/// </summary>
		/// <param name="NewValue">New value to set food store to</param>
		public void Set(double NewValue)
		{
			if ((NewValue < 0) || (NewValue > this.LandArea))
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