using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	/// <summary>
	/// A food pool of given age
	/// </summary>
	[Serializable]
	public class GrazeFoodStorePool : Model, IFeedType
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
		/// Amount (kg)
		/// </summary>
		[XmlIgnore]
		public double Amount { get { return amount; } }
		
		private double amount = 0;

		/// <summary>
		/// Age of pool in months
		/// </summary>
		[XmlIgnore]
		public int Age { get; set; }

		/// <summary>
		/// Current pool grazing limit based on ruminant eating pool
		/// </summary>
		[XmlIgnore]
		public double Limit { get; set; }


		/// <summary>
		/// Amount to set at start
		/// </summary>
		public double StartingAmount { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="AddAmount"></param>
		/// <param name="ActivityName"></param>
		/// <param name="UserName"></param>
		public void Add(double AddAmount, string ActivityName, string UserName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="RemoveAmount"></param>
		/// <param name="ActivityName"></param>
		/// <param name="UserName"></param>
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
		/// 
		/// </summary>
		/// <param name="RemoveRequest"></param>
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
		/// 
		/// </summary>
		/// <param name="NewAmount"></param>
		public void Set(double NewAmount)
		{
			this.amount = NewAmount;
		}

		/// <summary>
		/// 
		/// </summary>
		public void Initialise()
		{
			throw new NotImplementedException();
		}
	}
}