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
    /// Parent model of Ruminant Types.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    public class RuminantHerd: ResourceBaseWithTransactions
    {
		/// <summary>
		/// Current state of this resource.
		/// </summary>
		[XmlIgnore]
        public List<Ruminant> Herd;

		/// <summary>
		/// List of requested purchases.
		/// </summary>
		[XmlIgnore]
		public List<Ruminant> PurchaseIndividuals;

		/// <summary>
		/// The last individual to be added or removed (for reporting)
		/// </summary>
		[XmlIgnore]
		public object LastIndividualChanged { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFInitialiseResource")]
        private void OnWFInitialiseResource(object sender, EventArgs e)
        {
			id = 1;
            Herd = new List<Ruminant>();
			PurchaseIndividuals = new List<Ruminant>();
			LastIndividualChanged = new Ruminant();

			// for each Ruminant type 
			foreach (RuminantType rType in this.Children.Where(a => a.GetType() == typeof(RuminantType)).Cast<RuminantType>().ToList())
			{
				foreach (RuminantInitialCohorts ruminantCohorts in rType.Children.Where(a => a.GetType() == typeof(RuminantInitialCohorts)).Cast<RuminantInitialCohorts>().ToList())
				{
					foreach (var ind in ruminantCohorts.CreateIndividuals())
					{
						ind.SaleFlag = HerdChangeReason.InitialHerd;
						AddRuminant(ind);
					}
				}
			}

			//List<IModel> childNodes = Apsim.Children(this, typeof(IModel));

			//         foreach (IModel childModel in childNodes)
			//         {
			//	//cast the generic IModel to a specfic model.
			//	RuminantType ruminantType = childModel as RuminantType;
			//	foreach (var ind in ruminantType.CreateIndividuals())
			//	{
			//		ind.SaleFlag = HerdChangeReason.InitialHerd;
			//		AddRuminant(ind);
			//	}
			//}
		}

		/// <summary>
		/// Add individual/cohort to the the herd
		/// </summary>
		/// <param name="ind">Individual Ruminant to add</param>
		public void AddRuminant(Ruminant ind)
		{
			if (ind.ID == 0)
			{
				ind.ID = this.NextUniqueID;
			}
			Herd.Add(ind);
			LastIndividualChanged = ind;

			ResourceTransaction details = new ResourceTransaction();
			details.Credit = 1;
			details.Activity = "Unknown";
			details.Reason = "Unknown";
			details.ResourceType = this.Name;
			details.ExtraInformation = ind;
			LastTransaction = details;
			TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
			OnTransactionOccurred(te);

			// remove change flag
			ind.SaleFlag = HerdChangeReason.None;
		}

		/// <summary>
		/// Remove individual/cohort from the herd
		/// </summary>
		/// <param name="ind">Individual Ruminant to remove</param>
		public void RemoveRuminant(Ruminant ind)
		{
			// Remove mother ID from any suckling offspring
			if (ind.Gender == Sex.Female)
			{
				foreach (var offspring in (ind as RuminantFemale).SucklingOffspring)
				{
					offspring.Mother = null;
				}
			}
			Herd.Remove(ind);
			LastIndividualChanged = ind;

			ResourceTransaction details = new ResourceTransaction();
			details.Debit = -1;
			details.Activity = "Unknown";
			details.Reason = "Unknown";
			details.ResourceType = this.Name;
			details.ExtraInformation = ind;
			LastTransaction = details;
			TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
			OnTransactionOccurred(te);

			// remove change flag
			ind.SaleFlag = HerdChangeReason.None;
		}

		/// <summary>
		/// Remove list of Ruminants from the herd
		/// </summary>
		/// <param name="list">List of Ruminants to remove</param>
		public void RemoveRuminant(List<Ruminant> list)
		{
			foreach (var ind in list)
			{
				// report removal
				RemoveRuminant(ind);
			}
		}

		/// <summary>
		/// Gte the next unique individual id number
		/// </summary>
		public int NextUniqueID { get { return id++; } }
		private int id = 1;

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
