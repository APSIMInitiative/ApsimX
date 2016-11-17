using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	///<summary>
	/// Store for bank account
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Resources))]
	public class FinanceType : Model, IResourceType
	{
		/// <summary>
		/// Back account transaction occured
		/// </summary>
		public event EventHandler OnTransactionOccurred;

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

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("StartOfSimulation")]
		private void OnStartOfSimulation(object sender, EventArgs e)
		{
			Initialise();
		}

		/// <summary>
		/// Transcation occurred 
		/// </summary>
		/// <param name="e"></param>
		protected virtual void TransactionOccurred(TransactionOcurredEventArgs e)
		{
			if (OnTransactionOccurred != null)
				OnTransactionOccurred(this, e);
		}
	
		/// <summary>
		/// Transaction object
		/// </summary>
		[Serializable]
		public class Transaction
		{
			/// <summary>
			/// Name of sender or activity
			/// </summary>
			public string Sender { get; set; }
			/// <summary>
			/// Reason or cateogry
			/// </summary>
			public string Reason { get; set; }
			/// <summary>
			/// Amount to add
			/// </summary>
			public double Debit { get; set; }
			/// <summary>
			/// Amount to remove
			/// </summary>
			public double Credit { get; set; }
		}

		/// <summary>
		/// Last transaction received
		/// </summary>
		[XmlIgnore]
		public Transaction LastTransaction { get; set; }

		/// <summary>
		/// New transaction event args
		/// </summary>
		public class TransactionOcurredEventArgs : EventArgs
		{
			/// <summary>
			/// Transaction details
			/// </summary>
			[XmlIgnore]
			public Transaction Details { get; set; }
		}

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

				Transaction details = new Transaction();
				details.Debit = AddAmount;
				details.Sender = ActivityName;
				details.Reason = UserName;
				LastTransaction = details;

				TransactionOcurredEventArgs eargs = new TransactionOcurredEventArgs();
				eargs.Details = details;
				TransactionOccurred(eargs);
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

		/// <summary>
		/// Remove money from account
		/// </summary>
		/// <param name="RemoveAmount"></param>
		/// <param name="ActivityName"></param>
		/// <param name="UserName"></param>
		public void Remove(double RemoveAmount, string ActivityName, string UserName)
		{
			if (RemoveAmount > 0)
			{
				RemoveAmount = Math.Round(RemoveAmount, 2, MidpointRounding.ToEven);
				amount -= RemoveAmount;

				Transaction details = new Transaction();
				details.Credit = RemoveAmount;
				details.Sender = ActivityName;
				details.Reason = UserName;
				LastTransaction = details;

				TransactionOcurredEventArgs eargs = new TransactionOcurredEventArgs();
				eargs.Details = details;
				TransactionOccurred(eargs);
			}
		}

		/// <summary>
		/// Set the amount in an account.
		/// </summary>
		/// <param name="NewAmount"></param>
		public void Set(double NewAmount)
		{
			amount = Math.Round(NewAmount, 2, MidpointRounding.ToEven);
		}

		/// <summary>
		/// Initialise resource type
		/// </summary>
		public void Initialise()
		{
			this.amount = 0;
			Add(OpeningBalance, "Bank", "OpeningBalance");
		}
	}
}
