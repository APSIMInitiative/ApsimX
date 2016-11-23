using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{

    /// <summary>
    /// This stores the initialisation parameters for a fodder type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(AnimalFoodStore))]
    public class AnimalFoodStoreType : Model, IResourceType, IFeedType, IFeedPurchaseType
    {
        [Link]
        ISummary Summary = null;

        event EventHandler FodderChanged;

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
        /// Amount (kg)
        /// </summary>
        [XmlIgnore]
        public double Amount { get {return amount;} }

		private double amount;

		/// <summary>
		/// Add Food
		/// </summary>
		/// <param name="AddAmount">Amount to add to resource</param>
		/// <param name="ActivityName">Name of activity requesting resource</param>
		/// <param name="UserName">Name of individual requesting resource</param>
		public void Add(double AddAmount, string ActivityName, string UserName)
        {
            this.amount = this.amount + AddAmount;

            if (FodderChanged != null)
                FodderChanged.Invoke(this, new EventArgs()); 
        }

		/// <summary>
		/// Remove Food
		/// </summary>
		/// <param name="RemoveAmount">nb. This is a positive value not a negative value.</param>
		/// <param name="ActivityName">Name of activity requesting resource</param>
		/// <param name="UserName">Name of individual requesting resource</param>
		public void Remove(double RemoveAmount, string ActivityName, string UserName)
        {
            if (this.amount - RemoveAmount < 0)
            {
                string message = "Tried to remove more " + this.Name + " than exists." + Environment.NewLine
                    + "Current Amount: " + this.amount + Environment.NewLine
                    + "Tried to Remove: " + RemoveAmount;
                Summary.WriteWarning(this, message);
                this.amount = 0;
            }
            else
            {
                this.amount = this.amount - RemoveAmount;
            }

            if (FodderChanged != null)
                FodderChanged.Invoke(this, new EventArgs());
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
			removeRequest.Amount = Math.Min(removeRequest.Amount, amount);

			// add to intake and update %N and %DMD values
			removeRequest.Requestor.AddIntake(removeRequest);

			// Remove from resource
			Remove(removeRequest.Amount, removeRequest.FeedActivity.Name, removeRequest.Requestor.BreedParams.Name);
		}

		/// <summary>
		/// Set Amount of Food
		/// </summary>
		/// <param name="NewValue"></param>
		public void Set(double NewValue)
        {
            this.amount = NewValue;

            if (FodderChanged != null)
                FodderChanged.Invoke(this, new EventArgs());
        }


        /// <summary>
        /// Initialise the current state to the starting amount of fodder
        /// </summary>
        public void Initialise()
        {
 //           this.Age = this.StartingAge;
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
	}

 
}