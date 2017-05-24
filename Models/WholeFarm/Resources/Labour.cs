using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models.Core;

namespace Models.WholeFarm.Resources
{
    ///<summary>
    /// Parent model of Labour Person models.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    public class Labour: ResourceBaseWithTransactions
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
		/// Labour types currently available.
		/// </summary>
		[XmlIgnore]
        public List<LabourType> Items;

		/// <summary>
		/// Allows indiviuals to age each month
		/// </summary>
		[Description("Allow individuals to age")]
		public bool AllowAging { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Items = new List<LabourType>();
            foreach (LabourType labourChildModel in Apsim.Children(this, typeof(IModel)).Cast<LabourType>().ToList())
            {
				for (int i = 0; i < Math.Max(labourChildModel.Individuals, 1); i++)
				{
					LabourType labour = new LabourType()
					{
						Gender = labourChildModel.Gender,
						Individuals = 1,
						InitialAge = labourChildModel.InitialAge,
						AgeInMonths = labourChildModel.InitialAge*12,
						MaxLabourSupply = labourChildModel.MaxLabourSupply,
						Parent = this,
						Name = labourChildModel.Name // + ((labourChildModel.Individuals>1)?i.ToString():"")
					};
					labour.TransactionOccurred += Resource_TransactionOccurred;
					Items.Add(labour);
				}
            }
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFInitialiseResource")]
		private void OnWFInitialiseResource(object sender, EventArgs e)
		{
			if (Clock.Today.Day != 1)
			{
				OnStartOfMonth(this, null);
			}
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("StartOfMonth")]
		private void OnStartOfMonth(object sender, EventArgs e)
		{
			int currentmonth = Clock.Today.Month;
			foreach (var item in Items)
			{
				if (item.MaxLabourSupply.Length != 12)
				{
					string message = "Invalid number of values provided for MaxLabourSupply for " + item.Name;
					Summary.WriteWarning(this, message);
					throw new Exception("Invalid entry");
				}
				item.SetAvailableDays(currentmonth);
			}
		}

		/// <summary>Age individuals</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAgeResources")]
		private void ONWFAgeResources(object sender, EventArgs e)
		{
			if(AllowAging)
			{
				foreach (var item in Items)
				{
					item.AgeInMonths++;
				}
			}
		}

		#region Transactions

		// Must be included away from base class so that APSIM Event.Subscriber can find them 

		/// <summary>
		/// Override base event
		/// </summary>
		protected new void OnTransactionOccurred(EventArgs e)
		{
			EventHandler invoker = TransactionOccurred;
			if (invoker != null) invoker(this, e);
		}

		/// <summary>
		/// Override base event
		/// </summary>
		public new event EventHandler TransactionOccurred;

		private void Resource_TransactionOccurred(object sender, EventArgs e)
		{
			LastTransaction = (e as TransactionEventArgs).Transaction;
			OnTransactionOccurred(e);
		}

		#endregion

	}
}
