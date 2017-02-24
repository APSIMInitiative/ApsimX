using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm.Resources
{
    /// <summary>
    /// This stores the initialisation parameters for a person who can do labour 
    /// who is a family member.
    /// eg. AdultMale, AdultFemale etc.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Labour))]
    public class LabourType : WFModel, IResourceWithTransactionType, IResourceType
	{
        /// <summary>
        /// Get the Clock.
        /// </summary>
        [Link]
        Clock Clock = null;

		/// <summary>
		/// Get the Summary object.
		/// </summary>
		[Link]
        ISummary Summary = null;

        /// <summary>
        /// Age in years.
        /// </summary>
        [Description("Initial Age")]
        public double InitialAge { get; set; }

        /// <summary>
        /// Male or Female
        /// </summary>
        [Description("Gender")]
        public Sex Gender { get; set; }

        /// <summary>
        /// Name of each column in the grid. Used as the column header.
        /// </summary>
        [Description("Column Names")]
        public string[] ColumnNames { get; set; }

        /// <summary>
        /// Maximum Labour Supply (in days) for each month of the year. 
        /// </summary>
        [Description("Max Labour Supply (in days) for each month of the year")]
        public double[] MaxLabourSupply { get; set; }

        /// <summary>
        /// Age in years.
        /// </summary>
        [XmlIgnore]
        public double Age { get; set; }

		/// <summary>
		/// Number of individuals
		/// </summary>
		[Description("Number of individuals")]
		public int Individuals { get; set; }

		/// <summary>
		/// Available Labour (in days) in the current month. 
		/// </summary>
		[XmlIgnore]
        public double AvailableDays { get { return availableDays; } }
        private double availableDays;

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			Initialise();
		}

		/// <summary>
		/// Initialise the current state to the starting time available
		/// </summary>
		public void Initialise()
		{
			this.Age = this.InitialAge;
			Individuals = Math.Max(Individuals, 1);
			ResetAvailabilityEachMonth();
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("StartOfMonth")]
		private void OnStartOfMonth(object sender, EventArgs e)
		{
			ResetAvailabilityEachMonth();
		}

		/// <summary>
		/// Reset the Available Labour (in days) in the current month 
		/// to the appropriate value for this month.
		/// </summary>
		private void ResetAvailabilityEachMonth()
		{
			if (MaxLabourSupply.Length != 12)
			{
				string message = "Invalid number of values provided for MaxLabourSupply for " + this.Name;
				Summary.WriteWarning(this, message);
				throw new Exception("Invalid entry");
			}
			int currentmonth = Clock.Today.Month;
			this.availableDays = Math.Min(30.4, this.MaxLabourSupply[currentmonth - 1])*Individuals;
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
			this.availableDays = this.availableDays + AddAmount;
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
			if (this.availableDays - RemoveAmount < 0)
			{
				string message = "Tried to remove more " + this.Name + " than exists." + Environment.NewLine
					+ "Current Amount: " + this.availableDays + Environment.NewLine
					+ "Tried to Remove: " + RemoveAmount;
				Summary.WriteWarning(this, message);
				amountRemoved = this.availableDays;
				this.availableDays = 0;
			}
			else
			{
				this.availableDays = this.availableDays - RemoveAmount;
			}
			ResourceTransaction details = new ResourceTransaction();
			details.ResourceType = this.Name;
			details.Debit = amountRemoved;
			details.Activity = ActivityName;
			details.Reason = UserName;
			LastTransaction = details;
			TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
			OnTransactionOccurred(te);
			return amountRemoved;
		}

		/// <summary>
		/// Set amount of animal food available
		/// </summary>
		/// <param name="NewValue">New value to set food store to</param>
		public void Set(double NewValue)
		{
			this.availableDays = NewValue;
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

		#endregion

		#region IResourceType

		/// <summary>
		/// Remove labour using a request object
		/// </summary>
		/// <param name="RemoveRequest"></param>
		public void Remove(object RemoveRequest)
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

	}
}