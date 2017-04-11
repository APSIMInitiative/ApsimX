using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm.Resources
{
	///<summary>
	/// Store for bank account
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(ResourcesHolder))]
	public class FinanceType : WFModel, IResourceWithTransactionType, IResourceType
	{
		/// <summary>
		/// Opening balance
		/// </summary>
		[Description("Opening balance")]
		public double OpeningBalance { get; set; }

		/// <summary>
		/// The amount this account can be withdrawn to (-ve)
		/// </summary>
		[Description("The amount this account can be withdrawn to (<0 credit, 0 no credit)")]
		public double WithdrawalLimit { get; set; }

		/// <summary>
		/// Interest rate (%) charged on negative balance
		/// </summary>
		[Description("Interest rate (%) charged on negative balance")]
		public double InterestRateCharged { get; set; }

		/// <summary>
		/// Interest rate (%) paid on positive balance
		/// </summary>
		[Description("Interest rate (%) paid on positive balance")]
		public double InterestRatePaid { get; set; }

		/// <summary>
		/// Current funds available
		/// </summary>
		public double FundsAvailable { get { return amount - WithdrawalLimit; } }

		/// <summary>
		/// Current balance
		/// </summary>
		public double Balance { get { return amount; } }

		private double amount;
		/// <summary>
		/// Current amount of this resource
		/// </summary>
		public double Amount
		{
			get
			{
				return FundsAvailable;
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

		/// <summary>
		/// Initialise resource type
		/// </summary>
		public void Initialise()
		{
			this.amount = 0;
			Add(OpeningBalance, "Bank", "Opening balance");
		}

		#region transactions

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

		/// <summary>
		/// Add money to account
		/// </summary>
		/// <param name="AddAmount"></param>
		/// <param name="ActivityName"></param>
		/// <param name="UserName"></param>
		public void Add(double AddAmount, string ActivityName, string UserName)
		{
			if(AddAmount>0)
			{
				AddAmount = Math.Round(AddAmount, 2, MidpointRounding.ToEven);
				amount += AddAmount;

				ResourceTransaction details = new ResourceTransaction();
				details.Credit = AddAmount;
				details.Activity = ActivityName;
				details.Reason = UserName;
				details.ResourceType = this.Name;
				LastTransaction = details;
				TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
				OnTransactionOccurred(te);
			}
		}

		/// <summary>
		/// Remove money (object) from account
		/// </summary>
		/// <param name="RemoveRequest"></param>
		public void Remove(object RemoveRequest)
		{
			throw new NotImplementedException();
		}

		///// <summary>
		///// Remove money from account
		///// </summary>
		///// <param name="RemoveAmount"></param>
		///// <param name="ActivityName"></param>
		///// <param name="UserName"></param>
		//public double Remove(double RemoveAmount, string ActivityName, string UserName)
		//{
		//	if (RemoveAmount > 0)
		//	{
		//		RemoveAmount = Math.Round(RemoveAmount, 2, MidpointRounding.ToEven);
		//		amount -= RemoveAmount;

		//		ResourceTransaction details = new ResourceTransaction();
		//		details.ResourceType = this.Name;
		//		details.Debit = RemoveAmount * -1;
		//		details.Activity = ActivityName;
		//		details.Reason = UserName;
		//		LastTransaction = details;
		//		TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
		//		OnTransactionOccurred(te);
		//	}
		//	return RemoveAmount;
		//}

		/// <summary>
		/// Remove from finance type store
		/// </summary>
		/// <param name="Request">Resource request class with details.</param>
		public void Remove(ResourceRequest Request)
		{
			if (Request.Required == 0) return;
			double amountRemoved = Math.Round(Request.Required, 2, MidpointRounding.ToEven); 
			// avoid taking too much
			amountRemoved = Math.Min(this.Amount, amountRemoved);
			this.amount -= amountRemoved;

			Request.Provided = amountRemoved;
			ResourceTransaction details = new ResourceTransaction();
			details.ResourceType = this.Name;
			details.Debit = amountRemoved * -1;
			details.Activity = Request.ActivityName;
			details.Reason = Request.Reason;
			LastTransaction = details;
			TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
			OnTransactionOccurred(te);
		}

		/// <summary>
		/// Set the amount in an account.
		/// </summary>
		/// <param name="NewAmount"></param>
		public void Set(double NewAmount)
		{
			amount = Math.Round(NewAmount, 2, MidpointRounding.ToEven);
		}

		#endregion
	}
}
