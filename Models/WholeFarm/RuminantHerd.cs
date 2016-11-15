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
        /// Current state of this resource.
        /// </summary>
        [XmlIgnore]
        public List<Ruminant> Herd;


        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Herd = new List<Ruminant>();

            List<IModel> childNodes = Apsim.Children(this, typeof(IModel));

            foreach (IModel childModel in childNodes)
            {
                //cast the generic IModel to a specfic model.
                RuminantType ruminantType = childModel as RuminantType;
                Herd.AddRange(ruminantType.CreateIndividuals());
            }
        }

		/// <summary>
		/// Remove individual/cohort from the herd
		/// </summary>
		/// <param name="ind">Individual Ruminant to remove</param>
		/// <param name="reason">Reason for removal</param>
		public void RemoveRuminant(Ruminant ind, HerdChangeReason reason)
		{
			// report removal

			Herd.Remove(ind);
		}

		/// <summary>
		/// Remove list of Ruminants from the herd
		/// </summary>
		/// <param name="list">List of Ruminants to remove</param>
		/// <param name="reason">Reason for removal</param>
		public void RemoveRuminant(List<Ruminant> list, HerdChangeReason reason)
		{
			foreach (var item in list)
			{
				// report removal

				Herd.Remove(item);
			}
		}


	}

	/// <summary>
	/// Reasons for a change in herd
	/// </summary>
	public enum HerdChangeReason
	{
		/// <summary>
		/// Individual died
		/// </summary>
		Died,
		/// <summary>
		/// Individual born
		/// </summary>
		Born,
		/// <summary>
		/// Individual culled
		/// </summary>
		Culled,
		/// <summary>
		/// Dry breeder sold
		/// </summary>
		DryBreeder,
		/// <summary>
		/// Consumed by household
		/// </summary>
		Consumed,
		/// <summary>
		/// Purchased
		/// </summary>
		Purchased,
		/// <summary>
		/// Sold
		/// </summary>
		Sold
	}


}
