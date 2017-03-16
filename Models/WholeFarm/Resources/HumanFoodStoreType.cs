using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm.Resources
{

    /// <summary>
    /// This stores the initialisation parameters for a Home Food Store type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(HumanFoodStore))]
    public class HumanFoodStoreType : WFModel, IResourceType, IResourceWithTransactionType
    {
        [Link]
        ISummary Summary = null;

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
        /// Starting Age of the Fodder (Months)
        /// </summary>
        [Description("Starting Age of Human Food (Months)")]
        public double StartingAge { get; set; }

        /// <summary>
        /// Starting Amount (kg)
        /// </summary>
        [Description("Starting Amount (kg)")]
        public double StartingAmount { get; set; }

        /// <summary>
        /// Age of this Human Food (Months)
        /// </summary>
        [XmlIgnore]
        public double Age { get; set; } 

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [XmlIgnore]
        public double Amount { get { return amount; } }
        private double amount;

		/// <summary>
		/// Initialise the current state to the starting amount of fodder
		/// </summary>
		public void Initialise()
		{
			this.Age = this.StartingAge;
			this.amount = this.StartingAmount;
		}


		/// <summary>An event handler to allow us to initialise ourselves.</summary>
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
		/// Remove from food store
		/// </summary>
		/// <param name="RemoveAmount">Amount to remove. NOTE: This is a positive value not a negative value.</param>
		/// <param name="ActivityName">Name of activity requesting resource</param>
		/// <param name="UserName">Name of individual requesting resource</param>
		public double Remove(double RemoveAmount, string ActivityName, string UserName)
		{
			double amountRemoved = RemoveAmount;
			if (this.amount - RemoveAmount < 0)
			{
				string message = "Tried to remove more " + this.Name + " than exists." + Environment.NewLine
					+ "Current Amount: " + this.amount + Environment.NewLine
					+ "Tried to Remove: " + RemoveAmount;
				Summary.WriteWarning(this, message);
				amountRemoved = this.amount;
				this.amount = 0;
			}
			else
			{
				this.amount = this.amount - RemoveAmount;
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
		/// </summary>
		/// <param name="RemoveRequest">A feed request object with required information</param>
		public void Remove(object RemoveRequest)
		{
			throw new NotImplementedException();
		}

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