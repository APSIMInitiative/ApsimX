using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.Collections;  //enumerator
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models.Core;



namespace Models.WholeFarm
{

    ///<summary>
    /// Parent model of Ruminant Types.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Resources))]
    public class RuminantHerd: Model
    {
		/// <summary>
		/// Individual added or removed from herd
		/// </summary>
		public event EventHandler OnHerdChanged;

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
		public Ruminant LastIndividualChanged { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
			id = 1;
            Herd = new List<Ruminant>();
			PurchaseIndividuals = new List<Ruminant>();
			LastIndividualChanged = new Ruminant();

            List<IModel> childNodes = Apsim.Children(this, typeof(IModel));

            foreach (IModel childModel in childNodes)
            {
				//cast the generic IModel to a specfic model.
				RuminantType ruminantType = childModel as RuminantType;
				foreach (var ind in ruminantType.CreateIndividuals())
				{
					ind.SaleFlag = Common.HerdChangeReason.InitialHerd;
					AddRuminant(ind);
				}
			}
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
			HerdChangedEventArgs args = new HerdChangedEventArgs();
			args.Details = ind;
			HerdChanged(args);
			// remove change flag
			ind.SaleFlag = Common.HerdChangeReason.None;
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
			HerdChangedEventArgs args = new HerdChangedEventArgs();
			args.Details = ind;
			HerdChanged(args);
			// remove change flag
			ind.SaleFlag = Common.HerdChangeReason.None;
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

		/// <summary>
		/// Herd change occurred 
		/// </summary>
		/// <param name="e"></param>
		protected virtual void HerdChanged(HerdChangedEventArgs e)
		{
			if (OnHerdChanged != null)
				OnHerdChanged(this, e);
		}

		/// <summary>
		/// Herd changed  event args
		/// </summary>
		public class HerdChangedEventArgs : EventArgs
		{
			/// <summary>
			/// individual details
			/// </summary>
			public Ruminant Details { get; set; }
		}

	}



}
