using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>Ruminant herd management activity</summary>
	/// <summary>This activity will maintain a breeding herd at the desired levels of age/breeders etc</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]
	public class RuminantActivityTrade : Model
	{
		[Link]
		private Resources Resources = null;
//		[Link]
//		Clock Clock = null;

		/// <summary>
		/// Name of herd to trade
		/// </summary>
		[Description("Name of herd to trade")]
		public string HerdName { get; set; }

		/// <summary>An event handler to call for all herd management activities</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalManage")]
		private void OnWFAnimalManage(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd.Where(a => a.HerdName == HerdName).ToList();

		}
	}
}
