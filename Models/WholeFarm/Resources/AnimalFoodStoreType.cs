using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm.Resources
{

    /// <summary>
    /// This stores the initialisation parameters for a fodder type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(AnimalFoodStore))]
    public class AnimalFoodStoreType : WFModel, IResourceType, IResourceWithTransactionType
    {
        //[Link]
        //ISummary Summary = null;

        /// <summary>
        /// Dry Matter (%)
        /// </summary>
        [Description("Dry Matter (%)")]
        public double DryMatter { get; set; }

		/// <summary>
		/// Dry Matter Digestibility (%)
		/// </summary>
		[Description("Dry Matter Digestibility (%)")]
		public double DMD { get; set; }

		/// <summary>
		/// Nitrogen (%)
		/// </summary>
		[Description("Nitrogen (%)")]
        public double Nitrogen { get; set; }

        /// <summary>
        /// Starting Amount (kg)
        /// </summary>
        [Description("Starting Amount (kg)")]
        public double StartingAmount { get; set; }

		/// <summary>
		/// Determine if this feed is purchased as needed
		/// </summary>
		[Description("Purchase as needed")]
		public bool PurchaseAsNeeded { get; set; }

		/// <summary>
		/// Weight (kg) per unit purchased
		/// </summary>
		[Description("Weight (kg) per unit purchased")]
		public double KgPerUnitPurchased { get; set; }

		/// <summary>
		/// Cost per unit purchased
		/// </summary>
		[Description("Cost per unit purchased")]
		public double CostPerUnitPurchased { get; set; }

		/// <summary>
		/// Labour required per unit purchase
		/// </summary>
		[Description("Labour required per unit purchase")]
		public double LabourPerUnitPurchased { get; set; }

		/// <summary>
		/// Other costs per unit purchased
		/// </summary>
		[Description("Other costs per unit purchased")]
		public double OtherCosts { get; set; }

        /// <summary>
        /// Amount currently available (kg dry)
        /// </summary>
        [XmlIgnore]
        public double Amount { get {return amount;} set { return; } }
		private double amount;

		/// <summary>
		/// Initialise the current state to the starting amount of animal food
		/// </summary>
		public void Initialise()
		{
			this.amount = this.StartingAmount;
		}

		/// <summary>An event handler to allow us to initialise an animal food store.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
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
            this.amount = this.amount + AddAmount;

			ResourceTransaction details = new ResourceTransaction();
			details.Credit = AddAmount;
			details.Activity = ActivityName;
			details.Reason = UserName;
			details.ResourceType = this.Name;
			LastTransaction = details;
			TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
			OnTransactionOccurred(te);
        }

		/// <summary>
		/// Remove from animal food store
		/// </summary>
		/// <param name="Request">Resource request class with details.</param>
		public void Remove(ResourceRequest Request)
		{
			if (Request.Required == 0) return;
			double amountRemoved = Request.Required;
			// avoid taking too much
			amountRemoved = Math.Min(this.amount, amountRemoved);
			this.amount -= amountRemoved;

			AnimalFoodResourceRequestDetails additionalDetails = new AnimalFoodResourceRequestDetails();
			additionalDetails.DMD = this.DMD;
			additionalDetails.PercentN = this.Nitrogen;
			Request.AdditionalDetails = additionalDetails;

			Request.Provided = amountRemoved;
			ResourceTransaction details = new ResourceTransaction();
			details.ResourceType = this.Name;
			details.Debit = amountRemoved * -1;
			details.Activity = Request.ActivityName;
			details.Reason = Request.Reason;
			LastTransaction = details;
			TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
			OnTransactionOccurred(te);
			return;
		}

		///// <summary>
		///// Remove from food store
		///// </summary>
		///// <param name="RemoveAmount">Amount to remove. NOTE: This is a positive value not a negative value.</param>
		///// <param name="ActivityName">Name of activity requesting resource</param>
		///// <param name="UserName">Name of individual requesting resource</param>
		//public double Remove(double RemoveAmount, string ActivityName, string UserName)
  //      {
		//	double amountRemoved = RemoveAmount;
  //          if (this.amount - RemoveAmount < 0)
  //          {
		//		amountRemoved = this.amount;
  //              string message = "Tried to remove more " + this.Name + " than exists." + Environment.NewLine
  //                  + "Current Amount: " + this.amount + Environment.NewLine
  //                  + "Tried to Remove: " + RemoveAmount;
  //              Summary.WriteWarning(this, message);
  //              this.amount = 0;
  //          }
  //          else
  //          {
  //              this.amount = this.amount - RemoveAmount;
  //          }

		//	ResourceTransaction details = new ResourceTransaction();
		//	details.ResourceType = this.Name;
		//	details.Debit = amountRemoved*-1;
		//	details.Activity = ActivityName;
		//	details.Reason = UserName;
		//	LastTransaction = details;
		//	TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
		//	OnTransactionOccurred(te);

		//	return amountRemoved;
		//}

		///// <summary>
		///// Remove Food
		///// If this call is reached we are not going through an arbitrator so provide all possible resource to requestor
		///// </summary>
		///// <param name="RemoveRequest">A feed request object with required information</param>
		//public void Remove(object RemoveRequest)
		//{
		//	RuminantFeedRequest removeRequest = RemoveRequest as RuminantFeedRequest;

		//	// limit by available
		//	removeRequest.Amount = Math.Min(removeRequest.Amount, amount);

		//	// add to intake and update %N and %DMD values
		//	removeRequest.Requestor.AddIntake(removeRequest);

		//	// Remove from resource
		//	Remove(removeRequest.Amount, removeRequest.FeedActivity.Name, removeRequest.Requestor.BreedParams.Name);
		//}

		/// <summary>
		/// Set amount of animal food available
		/// </summary>
		/// <param name="NewValue">New value to set food store to</param>
		public void Set(double NewValue)
        {
            this.amount = NewValue;
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