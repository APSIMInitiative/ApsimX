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
  //      /// <summary>
  //      /// Get the Clock.
  //      /// </summary>
  //      [Link]
  //      Clock Clock = null;

		///// <summary>
		///// Get the Summary object.
		///// </summary>
		//[Link]
  //      ISummary Summary = null;

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

        ///// <summary>
        ///// Name of each column in the grid. Used as the column header.
        ///// </summary>
        //[Description("Column Names")]
        //public string[] ColumnNames { get; set; }

        /// <summary>
        /// Maximum Labour Supply (in days) for each month of the year. 
        /// </summary>
        [Description("Max Labour Supply (in days) for each month of the year")]
        public double[] MaxLabourSupply { get; set; }

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
		public int Individuals { get; set; }

		/// <summary>
		/// The unique id of the last activity request for this labour type
		/// </summary>
		[XmlIgnore]
		public Guid LastActivityRequestID { get; set; }

		/// <summary>
		/// Available Labour (in days) in the current month. 
		/// </summary>
		[XmlIgnore]
        public double AvailableDays { get { return availableDays; } }
        private double availableDays;

		/// <summary>
		/// Reset the available days for a given month
		/// </summary>
		/// <param name="month"></param>
		public void SetAvailableDays(int month)
		{
			availableDays = Math.Min(30.4, this.MaxLabourSupply[month - 1]);
		}

		///// <summary>An event handler to allow us to initialise ourselves.</summary>
		///// <param name="sender">The sender.</param>
		///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		//[EventSubscribe("Commencing")]
		//private void OnSimulationCommencing(object sender, EventArgs e)
		//{
		//	Initialise();
		//}

		/// <summary>
		/// Initialise the current state to the starting time available
		/// </summary>
		public void Initialise()
		{
		}

		///// <summary>An event handler to allow us to initialise ourselves.</summary>
		///// <param name="sender">The sender.</param>
		///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		//[EventSubscribe("StartOfMonth")]
		//private void OnStartOfMonth(object sender, EventArgs e)
		//{
		//	ResetAvailabilityEachMonth();
		//}

		///// <summary>
		///// Reset the Available Labour (in days) in the current month 
		///// to the appropriate value for this month.
		///// </summary>
		//public void ResetAvailabilityEachMonth()
		//{
		//	if (MaxLabourSupply.Length != 12)
		//	{
		//		string message = "Invalid number of values provided for MaxLabourSupply for " + this.Name;
		//		Summary.WriteWarning(this, message);
		//		throw new Exception("Invalid entry");
		//	}
		//	int currentmonth = Clock.Today.Month;
		//	this.availableDays = Math.Min(30.4, this.MaxLabourSupply[currentmonth - 1])*Individuals;
		//}

		///// <summary>Age individuals</summary>
		///// <param name="sender">The sender.</param>
		///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		//[EventSubscribe("WFAgeResources")]
		//private void ONWFAgeResources(object sender, EventArgs e)
		//{
		//	if((Parent as Labour).AllowAging)
		//	{
		//		AgeInMonths++;
		//	}
		//}

		#region Transactions

		/// <summary>
		/// Add to labour store of this type
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
			this.availableDays = this.availableDays + addAmount;
			ResourceTransaction details = new ResourceTransaction();
			details.Credit = addAmount;
			details.Activity = ActivityName;
			details.Reason = Reason;
			details.ResourceType = this.Name;
			LastTransaction = details;
			TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
			OnTransactionOccurred(te);
		}

		/// <summary>
		/// Remove from labour store
		/// </summary>
		/// <param name="Request">Resource request class with details.</param>
		public void Remove(ResourceRequest Request)
		{
			if (Request.Required == 0) return;
			double amountRemoved = Request.Required;
			// avoid taking too much
			amountRemoved = Math.Min(this.availableDays, amountRemoved);
			this.availableDays -= amountRemoved;
			Request.Provided = amountRemoved;
			LastActivityRequestID = Request.ActivityID;
			ResourceTransaction details = new ResourceTransaction();
			details.ResourceType = this.Name;
			details.Debit = amountRemoved * -1;
			details.Activity = Request.ActivityModel.Name;
			details.Reason = Request.Reason;
			LastTransaction = details;
			TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
			OnTransactionOccurred(te);
			return;
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
		/// Labour type transaction occured
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