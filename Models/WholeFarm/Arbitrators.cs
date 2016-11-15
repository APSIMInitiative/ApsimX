using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	///<summary>
	/// Manger for all arbitrators available to the model
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Zone))]
	public class Arbitrators : Model
	{
		/// <summary>
		/// List of the all the Arbitrators.
		/// </summary>
		[XmlIgnore]
		private List<IModel> arbitrators;

		/// <summary>
		/// Function to return an activity from the list of available activities.
		/// </summary>
		/// <param name="Name"></param>
		/// <returns>Activity with requested name or null</returns>
		public IModel GetByName(string Name)
		{
			return arbitrators.Find(x => x.Name == Name);
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			arbitrators = Apsim.Children(this, typeof(IModel));
		}

	}
}
