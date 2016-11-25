using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>
	/// Off farm labour activities
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]
	public class LabourActivityOffFarm: Model
	{
		[Link]
		private Resources Resources = null;
		/// <summary>
		/// Get the Clock.
		/// </summary>
		[Link]
		Clock Clock = null;

		/// <summary>An event handler to utilise all off farm labour and receive payment</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFRequestResources")]
		private void OnWFRequestResources(object sender, EventArgs e)
		{
			List<LabourOffFarmFilterGroup> taskList = this.Children.Where(a => a.GetType() == typeof(LabourOffFarmFilterGroup)).Cast<LabourOffFarmFilterGroup>().ToList();
			if (taskList.Count > 0)
			{
				int month = Clock.Today.Month - 1;
				foreach (var item in taskList)
				{
					// get family types based on filter

					// request labour for x individuals
					
					// request labour
					LabourRequest request = new LabourRequest();
					request.Activity = this;
					request.Amount = item.DailyRate * item.DaysWorkAvailableEachMonth[month];
					request.Requestor = item;

					// labour.Remove(request);
				}
			}
		}

		/// <summary>An event handler to utilise all off farm labour and receive payment</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFResourcesAllocated")]
		private void OnWFResourcesAllocated(object sender, EventArgs e)
		{
			List<LabourOffFarmFilterGroup> taskList = this.Children.Where(a => a.GetType() == typeof(LabourOffFarmFilterGroup)).Cast<LabourOffFarmFilterGroup>().ToList();
			if (taskList.Count > 0)
			{
				Finance Accounts = Resources.FinanceResource() as Finance;
				FinanceType bankAccount = Accounts.GetFirst();

				if (bankAccount != null)
				{
					// search through arbitrators requests and make payments.

					// otherwise we need to know who had payments made for work.



					int month = Clock.Today.Month - 1;
					foreach (var item in taskList)
					{
						// days provided from labour set in item.AmountProvided

						// receive payment for labour
						bankAccount.Add(item.AmountProvided, this.Name, item.Name);
					}
				}
			}
		}

	}
}
