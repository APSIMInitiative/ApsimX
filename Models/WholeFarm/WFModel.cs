using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm
{
	///<summary>
	/// WholeFarm base model
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public abstract class WFModel: Model
	{
		private string id = "00001";

		/// <summary>
		/// Model identifier
		/// </summary>
		public string ID { get { return id; } }
	}
}
