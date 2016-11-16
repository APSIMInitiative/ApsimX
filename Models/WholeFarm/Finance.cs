using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	///<summary>
	/// Parent model of finance models.
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Resources))]
	public class Finance : Model
	{
		/// <summary>
		/// Get resource by name
		/// </summary>
		/// <param name="Name"></param>
		/// <returns></returns>
		public FinanceType GetByName(string Name)
		{
			return this.Children.Where(a => a.Name == Name).FirstOrDefault() as FinanceType;
		}

		/// <summary>
		/// Get main/first account
		/// </summary>
		/// <returns></returns>
		public FinanceType GetFirst()
		{
			if(this.Children.Count() > 0 )
			{
				return this.Children.FirstOrDefault() as FinanceType;
			}
			return null;
		}


	}
}
